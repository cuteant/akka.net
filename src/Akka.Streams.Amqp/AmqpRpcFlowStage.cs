﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.IO;
using Akka.Streams.Amqp.Dsl;
using Akka.Streams.Stage;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace Akka.Streams.Amqp
{
    /// <summary>
    /// This stage materializes to a <see cref="T:System.Threading.Tasks.Task`1" />, which is the name of the private exclusive queue used for RPC communication
    /// </summary>
    /// <inheritdoc />
    public class AmqpRpcFlowStage : GraphStageWithMaterializedValue<FlowShape<OutgoingMessage, CommittableIncomingMessage>, Task<string>>
    {
        public static readonly Attributes DefaultAttributes = Attributes.CreateName("AmqpRpcFlow")
            .And(ActorAttributes.CreateDispatcher("akka.stream.default-blocking-io-dispatcher"));

        public AmqpSinkSettings Settings { get; }
        public int BufferSize { get; }
        public int ResponsePerMessage { get; }

        public readonly Inlet<OutgoingMessage> In = new Inlet<OutgoingMessage>("AmqpRpcFlow.in");
        public readonly Outlet<CommittableIncomingMessage> Out = new Outlet<CommittableIncomingMessage>("AmqpRpcFlow.out");

        public AmqpRpcFlowStage(AmqpSinkSettings settings, int bufferSize, int responsePerMessage = 1)
        {
            Settings = settings;
            BufferSize = bufferSize;
            ResponsePerMessage = responsePerMessage;
            Shape = new FlowShape<OutgoingMessage, CommittableIncomingMessage>(In, Out);
        }

        public override FlowShape<OutgoingMessage, CommittableIncomingMessage> Shape { get; }
        protected override Attributes InitialAttributes => DefaultAttributes;

        public override ILogicAndMaterializedValue<Task<string>> CreateLogicAndMaterializedValue(Attributes inheritedAttributes)
        {
            var promise = new TaskCompletionSource<string>();
            return new LogicAndMaterializedValue<Task<string>>(new Logic(this, promise), promise.Task);
        }

        public override string ToString() => "AmqpRpcFlow";

        private class Logic : AmqpConnectorLogic
        {
            private readonly AmqpRpcFlowStage _stage;
            private readonly TaskCompletionSource<string> _promise;
            private readonly Queue<CommittableIncomingMessage> _queue = new Queue<CommittableIncomingMessage>();
            private string _queueName = "";
            private int _unackedMessages;
            private int _outstandingMessages;
            private IBasicConsumer _amqpSourceConsumer;

            public Logic(AmqpRpcFlowStage stage, TaskCompletionSource<string> promise) : base(stage.Shape)
            {
                _stage = stage;
                _promise = promise;
                var exchange = _stage.Settings.Exchange ?? "";
                var routingKey = _stage.Settings.RoutingKey ?? "";

                SetHandler(_stage.Out, new LambdaOutHandler(onPull: () =>
                {
                    if (_queue.Count > 0)
                    {
                        PushMessage(_queue.Dequeue());
                    }
                }));

                SetHandler(_stage.In, new LambdaInHandler(
                    onPush: () =>
                    {
                        var elem = Grab(_stage.In);
                        var props = elem.Properties ?? new BasicProperties();
                        props.ReplyTo = _queueName;
                        Channel.BasicPublish(exchange, elem.RoutingKey ?? routingKey, elem.Mandatory, props, elem.Bytes.ToArray());

                        int ExpectedResponses()
                        {
                            var headers = props.Headers;
                            if (headers == null)
                                return _stage.ResponsePerMessage;

                            if (headers.TryGetValue("expectedReplies", out var r))
                            {
                                if (r != null)
                                {
                                    try
                                    {
                                        return Convert.ToInt32(r);
                                    }
                                    catch (Exception)
                                    {
                                        return _stage.ResponsePerMessage;
                                    }
                                }
                            }

                            return _stage.ResponsePerMessage;
                        }

                        _outstandingMessages += ExpectedResponses();
                        Pull(_stage.In);
                    },
                    onUpstreamFinish: () =>
                    {
                        SetKeepGoing(true);
                        // We don't want to finish since we're still waiting on incoming messages from rabbit. However, if we
                        // haven't processed a message yet, we do want to complete so that we don't hang.
                        if (_queue.Count == 0 && _outstandingMessages == 0 && _unackedMessages == 0)
                            CompleteStage(); //TODO: check if this is right?? JVM implementation: if (queue.isEmpty && outstandingMessages == 0) super.onUpstreamFinish()
                    },
                    onUpstreamFailure: ex =>
                    {
                        SetKeepGoing(true);
                        if (_queue.Count == 0 && _outstandingMessages == 0 && _unackedMessages == 0)
                            CompleteStage(); //TODO: check if this is right?? JVM implementation: if (queue.isEmpty && outstandingMessages == 0) super.onUpstreamFinish()
                    }));
            }

            public override IAmqpConnectorSettings Settings => _stage.Settings;

            public override IConnectionFactory ConnectionFactoryFrom(IAmqpConnectionSettings settings) =>
                AmqpConnector.ConnectionFactoryFrom(settings);

            public override IConnection NewConnection(IConnectionFactory factory, IAmqpConnectionSettings settings) =>
                AmqpConnector.NewConnection(factory, settings);

            public override void WhenConnected()
            {
                var shutdownCallback = GetAsyncCallback<(string consumerTag, ShutdownEventArgs args)>(tuple =>
                {
                    if (tuple.args != null)
                    {
                        var exception = ShutdownSignalException.FromArgs(tuple.args);
                        var ex = new ApplicationException($"Consumer {_queueName} with consumerTag {tuple.consumerTag} shutdown unexpectedly", exception);
                        _promise.SetException(ex);
                        FailStage(ex);
                    }
                    else
                    {
                        var ex = new ApplicationException($"Consumer {_queueName} with consumerTag {tuple.consumerTag} shutdown unexpectedly");
                        _promise.SetException(ex);
                        FailStage(ex);
                    }
                });

                Pull(_stage.In);

                // we have only one consumer per connection so global is ok
                Channel.BasicQos(0, (ushort) _stage.BufferSize, true);

                var consumerCallback = GetAsyncCallback<CommittableIncomingMessage>(HandleDelivery);

                var commitCallback = GetAsyncCallback<CommitCallback>(callback =>
                {
                    switch (callback)
                    {
                        case AckArguments args:
                            try
                            {
                                Channel.BasicAck(args.DeliveryTag, args.Multiple);
                                if (--_unackedMessages == 0 &&
                                    (IsClosed(_stage.Out) || (IsClosed(_stage.In) && _queue.Count == 0 && _outstandingMessages == 0)))
                                    CompleteStage();
                                args.Commit();
                            }
                            catch (Exception ex)
                            {
                                args.Fail(ex);
                            }

                            break;

                        case NackArguments args:
                            try
                            {
                                Channel.BasicNack(args.DeliveryTag, args.Multiple, args.Requeue);
                                if (--_unackedMessages == 0 &&
                                    (IsClosed(_stage.Out) || (IsClosed(_stage.In) && _queue.Count == 0 && _outstandingMessages == 0)))
                                    CompleteStage();
                                args.Commit();
                            }
                            catch (Exception ex)
                            {
                                args.Fail(ex);
                            }

                            break;
                    }
                });

                _amqpSourceConsumer = new DefaultConsumer(consumerCallback, commitCallback, shutdownCallback);

                // Create an exclusive queue with a randomly generated name for use as the replyTo portion of RPC
                _queueName = Channel.QueueDeclare("", false, true, true).QueueName;
                Channel.BasicConsume(_queueName, false, _amqpSourceConsumer);
                _promise.TrySetResult(_queueName);
            }

            private void HandleDelivery(CommittableIncomingMessage message)
            {
                if (IsAvailable(_stage.Out))
                {
                    PushMessage(message);
                }
                else
                {
                    if (_queue.Count + 1 > _stage.BufferSize)
                    {
                        FailStage(new ApplicationException($"Reached maximum buffer size {_stage.BufferSize}"));
                    }
                    else
                    {
                        _queue.Enqueue(message);
                    }
                }
            }

            private void PushMessage(CommittableIncomingMessage message)
            {
                Push(_stage.Out, message);
                _unackedMessages++;
                _outstandingMessages -= 1;
            }

            public override void PostStop()
            {
                _promise.TrySetException(new ApplicationException("stage stopped unexpectedly"));
                base.PostStop();
            }

            public override void OnFailure(Exception ex) => _promise.TrySetException(ex);

            private class DefaultConsumer : DefaultBasicConsumer
            {
                private readonly IHandle<CommittableIncomingMessage> _consumerCallback;
                private readonly IHandle<CommitCallback> _commitCallback;
                private readonly IHandle<(string consumerTag, ShutdownEventArgs args)> _shutdownCallback;

                public DefaultConsumer(IHandle<CommittableIncomingMessage> consumerCallback, IHandle<CommitCallback> commitCallback,
                    IHandle<(string consumerTag, ShutdownEventArgs args)> shutdownCallback)
                {
                    _consumerCallback = consumerCallback;
                    _commitCallback = commitCallback;
                    _shutdownCallback = shutdownCallback;
                }

                public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
                    IBasicProperties properties, byte[] body)
                {
                    var envelope = Envelope.Create(deliveryTag, redelivered, exchange, routingKey);
                    var message = new IncomingMessage(ByteString.CopyFrom(body), envelope, properties);

                    var committableMessage =
                        new CommittableIncomingMessage(
                            message,
                            ack: multiple =>
                            {
                                var promise = new TaskCompletionSource<Done>();
                                _commitCallback.Handle(new AckArguments(deliveryTag, multiple, promise));
                                return promise.Task;
                            },
                            nack: (multiple, requeue) =>
                            {
                                var promise = new TaskCompletionSource<Done>();
                                _commitCallback.Handle(new NackArguments(deliveryTag, multiple, requeue, promise));
                                return promise.Task;
                            });

                    _consumerCallback.Handle(committableMessage);
                }

                public override void HandleBasicCancel(string consumerTag)
                {
                    // non consumer initiated cancel, for example happens when the queue has been deleted.
                    _shutdownCallback.Handle((consumerTag, null));
                }

                public override void HandleModelShutdown(object model, ShutdownEventArgs reason)
                {
                    _shutdownCallback.Handle((ConsumerTag, reason));
                }
            }
        }
    }
}