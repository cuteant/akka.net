﻿//-----------------------------------------------------------------------
// <copyright file="Sources.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Akka.Annotations;
using Akka.Pattern;
using Akka.Streams.Dsl;
using Akka.Streams.Implementation.Stages;
using Akka.Streams.Stage;
using Akka.Streams.Supervision;
using Akka.Streams.Util;
using Akka.Util;
using Akka.Util.Internal;

namespace Akka.Streams.Implementation
{
    /// <summary>
    /// INTERNAL API
    /// </summary>
    /// <typeparam name="TOut">TBD</typeparam>
    [InternalApi]
    public sealed class QueueSource<TOut> : GraphStageWithMaterializedValue<SourceShape<TOut>, ISourceQueueWithComplete<TOut>>
    {
        #region internal classes

        /// <summary>
        /// TBD
        /// </summary>
        public interface IInput { }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        internal sealed class Offer<T> : IInput
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="element">TBD</param>
            /// <param name="completionSource">TBD</param>
            public Offer(T element, TaskCompletionSource<IQueueOfferResult> completionSource)
            {
                Element = element;
                CompletionSource = completionSource;
            }

            /// <summary>
            /// TBD
            /// </summary>
            public T Element { get; }

            /// <summary>
            /// TBD
            /// </summary>
            public TaskCompletionSource<IQueueOfferResult> CompletionSource { get; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class Completion : IInput, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Completion Instance = new Completion();

            private Completion()
            {

            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal sealed class Failure : IInput
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="ex">TBD</param>
            public Failure(Exception ex)
            {
                Ex = ex;
            }

            /// <summary>
            /// TBD
            /// </summary>
            public Exception Ex { get; }
        }

        #endregion  

        private sealed class Logic : GraphStageLogicWithCallbackWrapper<IInput>, IOutHandler, IHandle<IInput>
        {
            private readonly TaskCompletionSource<object> _completion;
            private readonly QueueSource<TOut> _stage;
            private IBuffer<TOut> _buffer;
            private Offer<TOut> _pendingOffer;
            private bool _terminating;

            public Logic(QueueSource<TOut> stage, TaskCompletionSource<object> completion) : base(stage.Shape)
            {
                _completion = completion;
                _stage = stage;

                SetHandler(stage.Out, this);
            }

            public void OnPull()
            {
                if (_stage._maxBuffer == 0)
                {
                    if (_pendingOffer is object)
                    {
                        Push(_stage.Out, _pendingOffer.Element);
                        _pendingOffer.CompletionSource.NonBlockingTrySetResult(QueueOfferResult.Enqueued.Instance);
                        _pendingOffer = null;
                        if (_terminating)
                        {
                            _completion.SetResult(new object());
                            CompleteStage();
                        }
                    }
                }
                else if (_buffer.NonEmpty)
                {
                    Push(_stage.Out, _buffer.Dequeue());
                    if (_pendingOffer is object)
                    {
                        EnqueueAndSuccess(_pendingOffer);
                        _pendingOffer = null;
                    }
                }

                if (_terminating && _buffer.IsEmpty)
                {
                    _completion.SetResult(new object());
                    CompleteStage();
                }
            }

            public void OnDownstreamFinish()
            {
                if (_pendingOffer is object)
                {
                    _pendingOffer.CompletionSource.NonBlockingTrySetResult(QueueOfferResult.QueueClosed.Instance);
                    _pendingOffer = null;
                }
                _completion.SetResult(new object());
                CompleteStage();
            }

            public override void PreStart()
            {
                if (_stage._maxBuffer > 0)
                    _buffer = Buffer.Create<TOut>(_stage._maxBuffer, Materializer);
                InitCallback(Callback());
            }

            public override void PostStop()
            {
                StopCallback(input =>
                {
                    if (input is Offer<TOut> offer)
                    {
                        var promise = offer.CompletionSource;
                        promise.NonBlockingTrySetException(new IllegalStateException("Stream is terminated. SourceQueue is detached."));
                    }
                });
            }

            private void EnqueueAndSuccess(Offer<TOut> offer)
            {
                _buffer.Enqueue(offer.Element);
                offer.CompletionSource.NonBlockingTrySetResult(QueueOfferResult.Enqueued.Instance);
            }

            private void BufferElement(Offer<TOut> offer)
            {
                if (!_buffer.IsFull)
                    EnqueueAndSuccess(offer);
                else
                {
                    switch (_stage._overflowStrategy)
                    {
                        case OverflowStrategy.DropHead:
                            _buffer.DropHead();
                            EnqueueAndSuccess(offer);
                            break;
                        case OverflowStrategy.DropTail:
                            _buffer.DropTail();
                            EnqueueAndSuccess(offer);
                            break;
                        case OverflowStrategy.DropBuffer:
                            _buffer.Clear();
                            EnqueueAndSuccess(offer);
                            break;
                        case OverflowStrategy.DropNew:
                            offer.CompletionSource.NonBlockingTrySetResult(QueueOfferResult.Dropped.Instance);
                            break;
                        case OverflowStrategy.Fail:
                            var bufferOverflowException =
                                new BufferOverflowException($"Buffer overflow (max capacity was: {_stage._maxBuffer})!");
                            offer.CompletionSource.NonBlockingTrySetResult(new QueueOfferResult.Failure(bufferOverflowException));
                            _completion.TrySetException(bufferOverflowException);
                            FailStage(bufferOverflowException);
                            break;
                        case OverflowStrategy.Backpressure:
                            if (_pendingOffer is object)
                                offer.CompletionSource.NonBlockingTrySetException(
                                    new IllegalStateException(
                                        "You have to wait for previous offer to be resolved to send another request."));
                            else
                                _pendingOffer = offer;
                            break;
                    }
                }
            }

            private IHandle<IInput> Callback() => GetAsyncCallback<IInput>(this);

            void IHandle<IInput>.Handle(IInput input)
            {
                switch (input)
                {
                    case Offer<TOut> offer:
                        if (_stage._maxBuffer != 0)
                        {
                            BufferElement(offer);
                            if (IsAvailable(_stage.Out))
                                Push(_stage.Out, _buffer.Dequeue());
                        }
                        else if (IsAvailable(_stage.Out))
                        {
                            Push(_stage.Out, offer.Element);
                            offer.CompletionSource.NonBlockingTrySetResult(QueueOfferResult.Enqueued.Instance);
                        }
                        else if (_pendingOffer is null)
                            _pendingOffer = offer;
                        else
                        {
                            switch (_stage._overflowStrategy)
                            {
                                case OverflowStrategy.DropHead:
                                case OverflowStrategy.DropBuffer:
                                    _pendingOffer.CompletionSource.NonBlockingTrySetResult(QueueOfferResult.Dropped.Instance);
                                    _pendingOffer = offer;
                                    break;
                                case OverflowStrategy.DropTail:
                                case OverflowStrategy.DropNew:
                                    offer.CompletionSource.NonBlockingTrySetResult(QueueOfferResult.Dropped.Instance);
                                    break;
                                case OverflowStrategy.Backpressure:
                                    offer.CompletionSource.NonBlockingTrySetException(
                                        new IllegalStateException(
                                            "You have to wait for previous offer to be resolved to send another request"));
                                    break;
                                case OverflowStrategy.Fail:
                                    var bufferOverflowException =
                                        new BufferOverflowException(
                                            $"Buffer overflow (max capacity was: {_stage._maxBuffer})!");
                                    offer.CompletionSource.NonBlockingTrySetResult(new QueueOfferResult.Failure(bufferOverflowException));
                                    _completion.TrySetException(bufferOverflowException);
                                    FailStage(bufferOverflowException);
                                    break;
                                default:
                                    ThrowHelper.ThrowArgumentOutOfRangeException();
                                    break;
                            }
                        }
                        break;
                    case Completion _:
                        if (_stage._maxBuffer != 0 && _buffer.NonEmpty || _pendingOffer is object)
                        {
                            _terminating = true;
                        }
                        else
                        {
                            _completion.SetResult(new object());
                            CompleteStage();
                        }
                        break;
                    case Failure failure:
                        _completion.TrySetUnwrappedException(failure.Ex);
                        FailStage(failure.Ex);
                        break;
                    default:
                        break;
                }
            }

            internal void Invoke(IInput offer) => InvokeCallbacks(offer);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public sealed class Materialized : ISourceQueueWithComplete<TOut>
        {
            private readonly Action<IInput> _invokeLogic;
            private readonly TaskCompletionSource<object> _completion;

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="invokeLogic">TBD</param>
            /// <param name="completion">TBD</param>
            public Materialized(Action<IInput> invokeLogic, TaskCompletionSource<object> completion)
            {
                _invokeLogic = invokeLogic;
                _completion = completion;
            }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="element">TBD</param>
            /// <returns>TBD</returns>
            public Task<IQueueOfferResult> OfferAsync(TOut element)
            {
                var promise = TaskEx.NonBlockingTaskCompletionSource<IQueueOfferResult>(); // new TaskCompletionSource<IQueueOfferResult>();
                _invokeLogic(new Offer<TOut>(element, promise));
                return promise.Task;
            }

            /// <summary>
            /// TBD
            /// </summary>
            /// <returns>TBD</returns>
            public Task WatchCompletionAsync() => _completion.Task;

            /// <summary>
            /// TBD
            /// </summary>
            public void Complete() => _invokeLogic(Completion.Instance);

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="ex">TBD</param>
            public void Fail(Exception ex) => _invokeLogic(new Failure(ex));
        }

        private readonly int _maxBuffer;
        private readonly OverflowStrategy _overflowStrategy;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="maxBuffer">TBD</param>
        /// <param name="overflowStrategy">TBD</param>
        public QueueSource(int maxBuffer, OverflowStrategy overflowStrategy)
        {
            _maxBuffer = maxBuffer;
            _overflowStrategy = overflowStrategy;
            Shape = new SourceShape<TOut>(Out);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public Outlet<TOut> Out { get; } = new Outlet<TOut>("queueSource.out");

        /// <summary>
        /// TBD
        /// </summary>
        public override SourceShape<TOut> Shape { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="inheritedAttributes">TBD</param>
        /// <returns>TBD</returns>
        public override ILogicAndMaterializedValue<ISourceQueueWithComplete<TOut>> CreateLogicAndMaterializedValue(Attributes inheritedAttributes)
        {
            var completion = new TaskCompletionSource<object>();
            var logic = new Logic(this, completion);
            return new LogicAndMaterializedValue<ISourceQueueWithComplete<TOut>>(logic, new Materialized(t => logic.Invoke(t), completion));
        }
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    [InternalApi]
    public sealed class UnfoldResourceSource<TOut, TSource> : GraphStage<SourceShape<TOut>>
    {
        #region Logic

        private sealed class Logic : OutGraphStageLogic
        {
            private readonly UnfoldResourceSource<TOut, TSource> _stage;
            private readonly Lazy<Decider> _decider;
            private TSource _blockingStream;
            private bool _open;

            public Logic(UnfoldResourceSource<TOut, TSource> stage, Attributes inheritedAttributes) : base(stage.Shape)
            {
                _stage = stage;
                _decider = new Lazy<Decider>(() =>
                {
                    var strategy = inheritedAttributes.GetAttribute<ActorAttributes.SupervisionStrategy>(null);
                    return strategy is object ? strategy.Decider : Deciders.StoppingDecider;
                });

                SetHandler(stage.Out, this);
            }

            public override void OnPull()
            {
                var stop = false;
                while (!stop)
                {
                    try
                    {
                        var data = _stage._readData(_blockingStream);
                        if (data.HasValue)
                            Push(_stage.Out, data.Value);
                        else
                            CloseStage();

                        break;
                    }
                    catch (Exception ex)
                    {
                        var directive = _decider.Value(ex);
                        switch (directive)
                        {
                            case Directive.Stop:
                                _stage._close(_blockingStream);
                                FailStage(ex);
                                stop = true;
                                break;
                            case Directive.Restart:
                                RestartState();
                                break;
                            case Directive.Resume:
                                break;
                            default:
                                ThrowHelper.ThrowArgumentOutOfRangeException();
                                break;
                        }
                    }
                }
            }

            public override void OnDownstreamFinish() => CloseStage();

            public override void PreStart()
            {
                _blockingStream = _stage._create();
                _open = true;
            }

            private void RestartState()
            {
                _stage._close(_blockingStream);
                _blockingStream = _stage._create();
                _open = true;
            }

            private void CloseStage()
            {
                try
                {
                    _stage._close(_blockingStream);
                    _open = false;
                    CompleteStage();
                }
                catch (Exception ex)
                {
                    FailStage(ex);
                }
            }

            public override void PostStop()
            {
                if (_open)
                    _stage._close(_blockingStream);
            }
        }

        #endregion

        private readonly Func<TSource> _create;
        private readonly Func<TSource, Option<TOut>> _readData;
        private readonly Action<TSource> _close;


        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="create">TBD</param>
        /// <param name="readData">TBD</param>
        /// <param name="close">TBD</param>
        public UnfoldResourceSource(Func<TSource> create, Func<TSource, Option<TOut>> readData, Action<TSource> close)
        {
            _create = create;
            _readData = readData;
            _close = close;

            Shape = new SourceShape<TOut>(Out);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override Attributes InitialAttributes { get; } = DefaultAttributes.UnfoldResourceSource;

        /// <summary>
        /// TBD
        /// </summary>
        public Outlet<TOut> Out { get; } = new Outlet<TOut>("UnfoldResourceSource.out");

        /// <summary>
        /// TBD
        /// </summary>
        public override SourceShape<TOut> Shape { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="inheritedAttributes">TBD</param>
        /// <returns>TBD</returns>
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this, inheritedAttributes);

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString() => "UnfoldResourceSource";
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    /// <typeparam name="TOut">TBD</typeparam>
    /// <typeparam name="TSource">TBD</typeparam>
    [InternalApi]
    public sealed class UnfoldResourceSourceAsync<TOut, TSource> : GraphStage<SourceShape<TOut>>
    {
        #region Logic

        private sealed class Logic : OutGraphStageLogic, IHandle<Either<Option<TOut>, Exception>>, IHandle<(Action, Task)>
        {
            private readonly UnfoldResourceSourceAsync<TOut, TSource> _source;
            private readonly Lazy<Decider> _decider;
            private TaskCompletionSource<TSource> _resource;
            private IHandle<Either<Option<TOut>, Exception>> _createdCallback;
            private IHandle<(Action, Task)> _closeCallback;
            private bool _open;

            public Logic(UnfoldResourceSourceAsync<TOut, TSource> source, Attributes inheritedAttributes) : base(source.Shape)
            {
                _source = source;
                _resource = new TaskCompletionSource<TSource>();

                Decider CreateDecider()
                {
                    var strategy = inheritedAttributes.GetAttribute<ActorAttributes.SupervisionStrategy>(null);
                    return strategy is object ? strategy.Decider : Deciders.StoppingDecider;
                }

                _decider = new Lazy<Decider>(CreateDecider);

                SetHandler(source.Out, this);
            }

            public override void OnPull()
            {
                void Ready(TSource source)
                {
                    try
                    {
                        _source._readData(source).ContinueWith(ReadDataContinutionAction, _createdCallback);
                    }
                    catch (Exception ex)
                    {
                        ErrorHandler(ex);
                    }
                }

                OnResourceReady(Ready);
            }

            static readonly Action<Task<Option<TOut>>, object> ReadDataContinutionAction = (t, s) => ReadDataContinution(t, s);
            private static void ReadDataContinution(Task<Option<TOut>> t, object state)
            {
                var createdCallback = (IHandle<Either<Option<TOut>, Exception>>)state;
                if (t.IsSuccessfully())
                {
                    createdCallback.Handle(new Left<Option<TOut>, Exception>(t.Result));
                }
                else
                {
                    createdCallback.Handle(new Right<Option<TOut>, Exception>(t.Exception));
                }
            }

            public override void OnDownstreamFinish() => CloseStage();

            public override void PreStart()
            {
                CreateStream(false);

                _createdCallback = GetAsyncCallback<Either<Option<TOut>, Exception>>(this);

                _closeCallback = GetAsyncCallback<(Action, Task)>(this);
            }

            void IHandle<Either<Option<TOut>, Exception>>.Handle(Either<Option<TOut>, Exception> either) // CreatedHandler
            {
                if (either.IsLeft)
                {
                    var element = either.ToLeft().Value;
                    if (element.HasValue)
                        Push(_source.Out, element.Value);
                    else
                        CloseStage();
                }
                else
                    ErrorHandler(either.ToRight().Value);
            }

            void IHandle<(Action, Task)>.Handle((Action, Task) t) // CloseHandler
            {
                if (t.Item2.IsSuccessfully())
                {
                    _open = false;
                    t.Item1();
                }
                else
                {
                    _open = false;
                    FailStage(t.Item2.Exception);
                }
            }

            private void CreateStream(bool withPull)
            {
                var cb = GetAsyncCallback<Either<TSource, Exception>>(new CreateStreamHandler(this, withPull));

                try
                {
                    void Continue(Task<TSource> t)
                    {

                        if (t.IsSuccessfully())
                            cb.Handle(new Left<TSource, Exception>(t.Result));
                        else
                            cb.Handle(new Right<TSource, Exception>(t.Exception));
                    }

                    _source._create().ContinueWith(Continue);
                }
                catch (Exception ex)
                {
                    FailStage(ex);
                }
            }

            sealed class CreateStreamHandler : IHandle<Either<TSource, Exception>>
            {
                private readonly Logic _logic;
                private readonly bool _withPull;

                public CreateStreamHandler(Logic logic, bool withPull)
                {
                    _logic = logic;
                    _withPull = withPull;
                }

                void IHandle<Either<TSource, Exception>>.Handle(Either<TSource, Exception> either)
                {
                    if (either.IsLeft)
                    {
                        _logic._open = true;
                        _logic._resource.SetResult(either.ToLeft().Value);
                        if (_withPull) { _logic.OnPull(); }
                    }
                    else
                    {
                        _logic.FailStage(either.ToRight().Value);
                    }
                }
            }

            private void OnResourceReady(Action<TSource> action) => _resource.Task.Then(action); //.ContinueWith(t =>
            //{
            //    if (!t.IsFaulted && !t.IsCanceled)
            //        action(t.Result);
            //});

            private void ErrorHandler(Exception ex)
            {
                var directive = _decider.Value(ex);
                switch (directive)
                {
                    case Directive.Stop:
                        OnResourceReady(s => _source._close(s));
                        FailStage(ex);
                        break;
                    case Directive.Resume:
                        OnPull();
                        break;
                    case Directive.Restart:
                        RestartState();
                        break;
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException();
                        break;
                }
            }

            private void CloseAndThen(Action action)
            {
                SetKeepGoing(true);

                void Ready(TSource source)
                {
                    try
                    {
                        _source._close(source).ContinueWith(t => _closeCallback.Handle((action, t)));
                    }
                    catch (Exception ex)
                    {
                        var fail = GetAsyncCallback(() => FailStage(ex));
                        fail.Run();
                    }
                    finally
                    {
                        _open = false;
                    }
                }

                OnResourceReady(Ready);
            }

            private void RestartState()
            {
                void Restart()
                {
                    _resource = new TaskCompletionSource<TSource>();
                    CreateStream(true);
                }

                CloseAndThen(Restart);
            }

            private void CloseStage() => CloseAndThen(CompleteStage);

            public override void PostStop()
            {
                if (_open)
                    CloseStage();
            }
        }

        #endregion

        private readonly Func<Task<TSource>> _create;
        private readonly Func<TSource, Task<Option<TOut>>> _readData;
        private readonly Func<TSource, Task> _close;


        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="create">TBD</param>
        /// <param name="readData">TBD</param>
        /// <param name="close">TBD</param>
        public UnfoldResourceSourceAsync(Func<Task<TSource>> create, Func<TSource, Task<Option<TOut>>> readData, Func<TSource, Task> close)
        {
            _create = create;
            _readData = readData;
            _close = close;

            Shape = new SourceShape<TOut>(Out);
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override Attributes InitialAttributes { get; } = DefaultAttributes.UnfoldResourceSourceAsync;

        /// <summary>
        /// TBD
        /// </summary>
        public Outlet<TOut> Out { get; } = new Outlet<TOut>("UnfoldResourceSourceAsync.out");

        /// <summary>
        /// TBD
        /// </summary>
        public override SourceShape<TOut> Shape { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="inheritedAttributes">TBD</param>
        /// <returns>TBD</returns>
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this, inheritedAttributes);

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString() => "UnfoldResourceSourceAsync";
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    [InternalApi]
    public sealed class LazySource<TOut, TMat> : GraphStageWithMaterializedValue<SourceShape<TOut>, Task<TMat>>
    {
        #region Logic

        private sealed class Logic : OutGraphStageLogic
        {
            private readonly LazySource<TOut, TMat> _stage;
            private readonly TaskCompletionSource<TMat> _completion;
            private readonly Attributes _inheritedAttributes;

            public Logic(LazySource<TOut, TMat> stage, TaskCompletionSource<TMat> completion, Attributes inheritedAttributes) : base(stage.Shape)
            {
                _stage = stage;
                _completion = completion;
                _inheritedAttributes = inheritedAttributes;

                SetHandler(stage.Out, this);
            }

            public override void OnDownstreamFinish()
            {
                _completion.TrySetException(new Exception("Downstream canceled without triggering lazy source materialization"));
                CompleteStage();
            }


            public override void OnPull()
            {
                var source = _stage._sourceFactory();
                var subSink = new SubSinkInlet<TOut>(this, "LazySource");
                subSink.Pull();

                SetHandler(_stage.Out, () => subSink.Pull(), () =>
                {
                    subSink.Cancel();
                    CompleteStage();
                });

                subSink.SetHandler(new LambdaInHandler(() => Push(_stage.Out, subSink.Grab())));

                try
                {
                    var value = SubFusingMaterializer.Materialize(source.ToMaterialized(subSink.Sink, Keep.Left),
                        _inheritedAttributes);
                    _completion.SetResult(value);
                }
                catch (Exception e)
                {
                    subSink.Cancel();
                    FailStage(e);
                    _completion.TrySetUnwrappedException(e);
                }
            }

            public override void PostStop() => _completion.TrySetException(
                new Exception("LazySource stopped without completing the materialized task"));
        }

        #endregion

        private readonly Func<Source<TOut, TMat>> _sourceFactory;

        /// <summary>
        /// Creates a new <see cref="LazySource{TOut,TMat}"/>
        /// </summary>
        /// <param name="sourceFactory">The factory that generates the source when needed</param>
        public LazySource(Func<Source<TOut, TMat>> sourceFactory)
        {
            _sourceFactory = sourceFactory;
            Shape = new SourceShape<TOut>(Out);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public Outlet<TOut> Out { get; } = new Outlet<TOut>("LazySource.out");

        /// <summary>
        /// TBD
        /// </summary>
        public override SourceShape<TOut> Shape { get; }

        /// <summary>
        /// TBD
        /// </summary>
        protected override Attributes InitialAttributes { get; } = DefaultAttributes.LazySource;

        /// <summary>
        /// TBD
        /// </summary>
        public override ILogicAndMaterializedValue<Task<TMat>> CreateLogicAndMaterializedValue(Attributes inheritedAttributes)
        {
            var completion = new TaskCompletionSource<TMat>();
            var logic = new Logic(this, completion, inheritedAttributes);

            return new LogicAndMaterializedValue<Task<TMat>>(logic, completion.Task);
        }

        /// <summary>
        /// Returns the string representation of the <see cref="LazySource{TOut,TMat}"/>
        /// </summary>
        public override string ToString() => "LazySource";
    }

    /// <summary>
    /// API for the <see cref="LazySource{TOut,TMat}"/>
    /// </summary>
    public static class LazySource
    {
        /// <summary>
        /// Creates a new <see cref="LazySource{TOut,TMat}"/> for the given <paramref name="create"/> factory
        /// </summary>
        public static LazySource<TOut, TMat> Create<TOut, TMat>(Func<Source<TOut, TMat>> create) =>
            new LazySource<TOut, TMat>(create);
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    public sealed class EmptySource<TOut> : GraphStage<SourceShape<TOut>>
    {
        private sealed class Logic : OutGraphStageLogic
        {
            public Logic(EmptySource<TOut> stage) : base(stage.Shape) => SetHandler(stage.Out, this);

            public override void OnPull() => CompleteStage();

            public override void PreStart() => CompleteStage();
        }

        public EmptySource()
        {
            Shape = new SourceShape<TOut>(Out);
        }

        public Outlet<TOut> Out { get; } = new Outlet<TOut>("EmptySource.out");

        public override SourceShape<TOut> Shape { get; }

        protected override Attributes InitialAttributes => DefaultAttributes.LazySource;

        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        public override string ToString() => "EmptySource";
    }

    internal sealed class EventWrapper<TDelegate, TEventArgs> : IObservable<TEventArgs>
    {
        #region disposer

        private class Disposer : IDisposable
        {
            private readonly EventWrapper<TDelegate, TEventArgs> _observable;
            private readonly TDelegate _handler;

            public Disposer(EventWrapper<TDelegate, TEventArgs> observable, TDelegate handler)
            {
                _observable = observable;
                _handler = handler;
            }

            public void Dispose()
            {
                _observable._removeHandler(_handler);
            }
        }

        #endregion

        private readonly Action<TDelegate> _addHandler;
        private readonly Action<TDelegate> _removeHandler;
        private readonly Func<Action<TEventArgs>, TDelegate> _conversion;

        /// <summary>
        /// Creates a new instance of EventWrapper - an object wrapping C# events with an observable object.
        /// </summary>
        /// <param name="conversion">Function used to convert given event handler to delegate compatible with underlying .NET event.</param>
        /// <param name="addHandler">Action which attaches given event handler to the underlying .NET event.</param>
        /// <param name="removeHandler">Action which detaches given event handler to the underlying .NET event.</param>
        public EventWrapper(Action<TDelegate> addHandler, Action<TDelegate> removeHandler, Func<Action<TEventArgs>, TDelegate> conversion)
        {
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _conversion = conversion;
        }

        public IDisposable Subscribe(IObserver<TEventArgs> observer)
        {
            var handler = _conversion(observer.OnNext);
            _addHandler(handler);
            return new Disposer(this, handler);
        }
    }

    /// <summary>
    /// INTERNAL API
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class ObservableSourceStage<T> : GraphStage<SourceShape<T>>
    {
        #region internal classes

        private sealed class Logic : GraphStageLogic, IObserver<T>, IHandle<T>, IRunnable
        {
            private readonly ObservableSourceStage<T> _stage;
            private readonly int _bufferCapacity;

            private readonly LinkedList<T> _buffer;
            private readonly Action<T> _onOverflow;
            private readonly IHandle<T> _onEvent;
            private readonly IHandle<Exception> _onError;
            private readonly IRunnable _onCompleted;

            private IDisposable _disposable;

            public Logic(ObservableSourceStage<T> stage) : base(stage.Shape)
            {
                _stage = stage;
                _buffer = new LinkedList<T>();
                _bufferCapacity = stage._maxBufferCapacity;
                _onEvent = GetAsyncCallback<T>(this);
                _onError = GetAsyncCallback<Exception>(new ErrorHandler(this, stage));
                _onCompleted = GetAsyncCallback((IRunnable)this);
                _onOverflow = SetupOverflowStrategy(stage._overflowStrategy);

                SetHandler(stage.Outlet, onPull: () =>
                {
                    if (_buffer.Count > 0)
                    {
                        var element = Dequeue();
                        Push(_stage.Outlet, element);
                    }
                }, onDownstreamFinish: OnCompleted);
            }

            void IHandle<T>.Handle(T e)
            {
                if (IsAvailable(_stage.Outlet))
                {
                    Push(_stage.Outlet, e);
                }
                else
                {
                    if (_buffer.Count >= _bufferCapacity) _onOverflow(e);
                    else Enqueue(e);
                }
            }

            sealed class ErrorHandler : IHandle<Exception>
            {
                private readonly Logic _logic;
                private readonly ObservableSourceStage<T> _stage;

                public ErrorHandler(Logic logic, ObservableSourceStage<T> stage)
                {
                    _logic = logic;
                    _stage = stage;
                }

                void IHandle<Exception>.Handle(Exception e) => _logic.Fail(_stage.Outlet, e);
            }

            void IRunnable.Run() => Complete(_stage.Outlet);

            public void OnNext(T value) => _onEvent.Handle(value);
            public void OnError(Exception error) => _onError.Handle(error);
            public void OnCompleted() => _onCompleted.Run();

            public override void PreStart()
            {
                base.PreStart();
                _disposable = _stage._observable.Subscribe(this);
            }

            public override void PostStop()
            {
                _disposable?.Dispose();
                _buffer.Clear();
                base.PostStop();
            }

            private void Enqueue(T e) => _buffer.AddLast(e);

            private T Dequeue()
            {
                var element = _buffer.First.Value;
                _buffer.RemoveFirst();
                return element;
            }

            private Action<T> SetupOverflowStrategy(OverflowStrategy overflowStrategy)
            {
                switch (overflowStrategy)
                {
                    case OverflowStrategy.DropHead:
                        return DropHeadStrategy;
                    case OverflowStrategy.DropTail:
                        return DropTailStrategy;
                    case OverflowStrategy.DropNew:
                        return DropNewStrategy;
                    case OverflowStrategy.DropBuffer:
                        return DropBufferStrategy;
                    case OverflowStrategy.Fail:
                        return FialStrategy;
                    case OverflowStrategy.Backpressure:
                        return BackpressureStrategy;
                    default: throw ThrowHelper.GetNotSupportedException_UnknownOption(overflowStrategy);
                }
            }

            private void DropHeadStrategy(T message)
            {
                _buffer.RemoveFirst();
                Enqueue(message);
            }

            private void DropTailStrategy(T message)
            {
                _buffer.RemoveLast();
                Enqueue(message);
            }

            private void DropNewStrategy(T message)
            {
                /* do nothing */
            }

            private void DropBufferStrategy(T message)
            {
                _buffer.Clear();
                Enqueue(message);
            }

            private void FialStrategy(T message)
            {
                FailStage(new BufferOverflowException($"{_stage.Outlet} buffer has been overflown"));
            }

            private void BackpressureStrategy(T message)
            {
                throw new NotSupportedException("OverflowStrategy.Backpressure is not supported");
            }
        }

        #endregion

        private readonly IObservable<T> _observable;
        private readonly int _maxBufferCapacity;
        private readonly OverflowStrategy _overflowStrategy;

        public ObservableSourceStage(IObservable<T> observable, int maxBufferCapacity, OverflowStrategy overflowStrategy)
        {
            _observable = observable;
            _maxBufferCapacity = maxBufferCapacity;
            _overflowStrategy = overflowStrategy;

            Shape = new SourceShape<T>(Outlet);
        }

        public Outlet<T> Outlet { get; } = new Outlet<T>("observable.out");
        public override SourceShape<T> Shape { get; }
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);
    }
}
