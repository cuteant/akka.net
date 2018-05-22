﻿//-----------------------------------------------------------------------
// <copyright file="JournalSerializationSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Akka.Persistence.Fsm;
using Akka.Persistence.Journal;
using Akka.Serialization;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt.Text;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.TCK.Serialization
{
    public abstract class JournalSerializationSpec : PluginSpec
    {
        protected JournalSerializationSpec(Config config, string actorSystem, ITestOutputHelper output)
            : base(ConfigurationFactory.ParseString(@"
                akka.actor {
                  serializers {
                    my-payload = ""Akka.Persistence.TCK.Serialization.TestJournal+MyPayloadSerializer, Akka.Persistence.TCK""
                    my-payload2 = ""Akka.Persistence.TCK.Serialization.TestJournal+MyPayload2Serializer, Akka.Persistence.TCK""
                  }
                  serialization-bindings {
                    ""Akka.Persistence.TCK.Serialization.TestJournal+MyPayload, Akka.Persistence.TCK"" = my-payload
                    ""Akka.Persistence.TCK.Serialization.TestJournal+MyPayload2, Akka.Persistence.TCK"" = my-payload2
                    ""Akka.Persistence.TCK.Serialization.TestJournal+MyPayload3, Akka.Persistence.TCK"" = my-payload
                  }
                }
            ").WithFallback(config), actorSystem, output)
        {
        }

        protected IActorRef Journal => Extension.JournalFor(null);

        [Fact]
        public virtual void Journal_should_serialize_Persistent()
        {
            var probe = CreateTestProbe();
            var persistentEvent = new Persistent(new TestJournal.MyPayload("a"), 1L, Pid, null, false, null, WriterGuid);

            var messages = new List<AtomicWrite>
            {
                new AtomicWrite(persistentEvent)
            };

            Journal.Tell(new WriteMessages(messages, probe.Ref, ActorInstanceId));
            probe.ExpectMsg<WriteMessagesSuccessful>();
            probe.ExpectMsg<WriteMessageSuccess>(m => m.ActorInstanceId == ActorInstanceId && m.Persistent.PersistenceId == Pid);

            Journal.Tell(new ReplayMessages(0, long.MaxValue, long.MaxValue, Pid, probe.Ref));
            probe.ExpectMsg<ReplayedMessage>(s => s.Persistent.PersistenceId == Pid
                && s.Persistent.SequenceNr == persistentEvent.SequenceNr
                && s.Persistent.Payload.AsInstanceOf<TestJournal.MyPayload>().Data.Equals(".a."));
            probe.ExpectMsg<RecoverySuccess>();
        }

        [Fact]
        public virtual void Journal_should_serialize_Persistent_with_string_manifest()
        {
            var probe = CreateTestProbe();
            var persistentEvent = new Persistent(new TestJournal.MyPayload2("b", 5), 1L, Pid, null, false, null, WriterGuid);

            var messages = new List<AtomicWrite>
            {
                new AtomicWrite(persistentEvent)
            };

            Journal.Tell(new WriteMessages(messages, probe.Ref, ActorInstanceId));
            probe.ExpectMsg<WriteMessagesSuccessful>();
            probe.ExpectMsg<WriteMessageSuccess>(m => m.ActorInstanceId == ActorInstanceId && m.Persistent.PersistenceId == Pid);

            Journal.Tell(new ReplayMessages(0, long.MaxValue, long.MaxValue, Pid, probe.Ref));
            probe.ExpectMsg<ReplayedMessage>(s => s.Persistent.PersistenceId == persistentEvent.PersistenceId
                && s.Persistent.SequenceNr == persistentEvent.SequenceNr
                && s.Persistent.Payload.AsInstanceOf<TestJournal.MyPayload2>().Data.Equals(".b."));
            probe.ExpectMsg<RecoverySuccess>();
        }

        [Fact]
        public virtual void Journal_should_serialize_Persistent_with_EventAdapter_manifest()
        {
            var probe = CreateTestProbe();
            var persistentEvent = new Persistent(new TestJournal.MyPayload3("item1"), 1L, Pid, null, false, null, WriterGuid);

            var messages = new List<AtomicWrite>
            {
                new AtomicWrite(persistentEvent)
            };

            Journal.Tell(new WriteMessages(messages, probe.Ref, ActorInstanceId));
            probe.ExpectMsg<WriteMessagesSuccessful>();
            probe.ExpectMsg<WriteMessageSuccess>(m => m.ActorInstanceId == ActorInstanceId && m.Persistent.PersistenceId == Pid);

            Journal.Tell(new ReplayMessages(0, long.MaxValue, long.MaxValue, Pid, probe.Ref));
            probe.ExpectMsg<ReplayedMessage>(s => s.Persistent.PersistenceId == persistentEvent.PersistenceId
                                                  && s.Persistent.SequenceNr == persistentEvent.SequenceNr
                                                  && s.Persistent.Payload.AsInstanceOf<TestJournal.MyPayload3>().Data.Equals(".item1.")
                                                  && s.Persistent.Manifest == "First-Manifest");
            probe.ExpectMsg<RecoverySuccess>();
        }

        [Fact]
        public virtual void Journal_should_serialize_StateChangeEvent()
        {
            var probe = CreateTestProbe();
            var stateChangeEvent = new PersistentFSM.StateChangeEvent("init", TimeSpan.FromSeconds(342));

            var messages = new List<AtomicWrite>
            {
                new AtomicWrite(new Persistent(stateChangeEvent, 1, Pid))
            };

            Journal.Tell(new WriteMessages(messages, probe.Ref, ActorInstanceId));
            probe.ExpectMsg<WriteMessagesSuccessful>();
            probe.ExpectMsg<WriteMessageSuccess>(m => m.ActorInstanceId == ActorInstanceId && m.Persistent.PersistenceId == Pid);

            Journal.Tell(new ReplayMessages(0, 1, long.MaxValue, Pid, probe.Ref));
            probe.ExpectMsg<ReplayedMessage>();
            probe.ExpectMsg<RecoverySuccess>();
        }
    }

    internal static class TestJournal
    {
        public class MyPayload
        {
            public MyPayload(string data) => Data = data;

            public string Data { get; }
        }

        public class MyPayload2
        {
            public MyPayload2(string data, int n)
            {
                Data = data;
                N = n;
            }

            public string Data { get; }
            public int N { get; }
        }

        public class MyPayload3
        {
            public MyPayload3(string data) => Data = data;

            public string Data { get; }
        }

        public class MyPayloadSerializer : Serializer
        {
            public MyPayloadSerializer(ExtendedActorSystem system) : base(system) { }

            public override int Identifier => 77123;
            public override bool IncludeManifest => true;

            public override byte[] ToBinary(object obj)
            {
                switch (obj)
                {
                    case MyPayload myPayload:
                        return Encoding.UTF8.GetBytes("." + myPayload.Data);
                    case MyPayload3 myPayload3:
                        return Encoding.UTF8.GetBytes("." + myPayload3.Data);
                    default:
                        throw new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{nameof(MyPayloadSerializer)}]");
                }
            }

            private static readonly Dictionary<Type, Func<byte[], object>> s_FromBinaryMap = new Dictionary<Type, Func<byte[], object>>()
            {
                { typeof(MyPayload), bytes => new MyPayload($"{Encoding.UTF8.GetString(bytes)}.") },
                { typeof(MyPayload3), bytes => new MyPayload3($"{Encoding.UTF8.GetString(bytes)}.") },
            };
            public override object FromBinary(byte[] bytes, Type type)
            {
                if (s_FromBinaryMap.TryGetValue(type, out var factory))
                {
                    return factory(bytes);
                }
                throw new ArgumentException($"Unimplemented deserialization of message with manifest [{type}] in serializer {nameof(MyPayloadSerializer)}");
            }
        }

        public class MyPayload2Serializer : SerializerWithStringManifest
        {
            private static readonly string _manifestV1 = typeof(MyPayload).TypeQualifiedName();
            private const string _manifestV2 = "MyPayload-V2";
            private static readonly byte[] _manifestV2Bytes = StringHelper.UTF8NoBOM.GetBytes(_manifestV2);

            public MyPayload2Serializer(ExtendedActorSystem system) : base(system)
            {
            }

            public override int Identifier => 77125;

            public override byte[] ToBinary(object obj)
            {
                if (obj is MyPayload2 payload)
                {
                    return Encoding.UTF8.GetBytes($".{payload.Data}:{payload.N}");
                }

                return null;
            }

            public override string Manifest(object o)
            {
                return _manifestV2;
            }
            /// <inheritdoc />
            public override byte[] ManifestBytes(object o)
            {
                return _manifestV2Bytes;
            }

            public override object FromBinary(byte[] bytes, string manifest)
            {
                if (string.Equals(manifest, _manifestV2, StringComparison.Ordinal))
                {
                    var parts = Encoding.UTF8.GetString(bytes).Split(':');
                    return new MyPayload2(parts[0] + ".", int.Parse(parts[1]));
                }
                if (string.Equals(manifest, _manifestV1, StringComparison.Ordinal))
                {
                    return new MyPayload2(Encoding.UTF8.GetString(bytes) + ".", 0);
                }

                throw new ArgumentException($"unexpected manifest {manifest}");
            }
        }

        public class MyWriteAdapter : IWriteEventAdapter
        {
            public string Manifest(object evt)
            {
                switch (evt)
                {
                    case MyPayload3 p when string.Equals(p.Data, "item1", StringComparison.Ordinal):
                        return "First-Manifest";
                    default:
                        return string.Empty;
                }
            }

            public object ToJournal(object evt)
            {
                return evt;
            }
        }
    }
}
