using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Configuration;
using Akka.Pattern;
using Akka.Persistence.Journal;
using Akka.Util;
using Akka.Persistence.Serialization;

namespace Akka.Persistence
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        payload,
        store,
        cause,
        windowSize,
        maxOldWriters,
        mode,
        unconfirmedDeliveries,
        persistenceId,
        metadata,
        system,
        config,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
        #region Argument

        Argument_Mode_NoDisabled,
        Argument_Init_SyncWJ,
        Argument_Init_SnapshotStore,
        Argument_AtomicWrite,

        #endregion

        #region ArgumentNull

        ArgumentNull_AtomicWrite,
        ArgumentNull_SetStore,
        ArgumentNull_ReplayFailure,
        ArgumentNull_windowSize,
        ArgumentNull_maxOldWriters,
        ArgumentNull_AtLeastOnceDeliverySnapshot,
        ArgumentNull_UnconfirmedWarning,
        ArgumentNull_DeleteMessagesFailure,
        ArgumentNull_DeleteMessagesTo,
        ArgumentNull_WriteMessagesFailed,
        ArgumentNull_WriteMessageRejected,
        ArgumentNull_WriteMessageFailure,
        ArgumentNull_ReplayMessagesFailure,
        ArgumentNull_SaveSnapshot,
        ArgumentNull_DeleteSnapshot,

        #endregion

        #region IllegalState

        IllegalState_NextStateData,
        IllegalState_call_SW_Init,
        IllegalState_call_SW_SN,
        IllegalState_call_SW_SD,
        IllegalState_PermitsNeedNonegative,

        #endregion

        #region InvalidOperation

        InvalidOperation_Cannot_persist_during_replay,
        InvalidOperation_Recover_methods,
        InvalidOperation_Command_methods,

        #endregion
    }

    #endregion

    partial class ThrowHelper
    {
        #region -- ArgumentNullException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_Eventsourced(IActorRef self)
        {
            throw GetException();
            ArgumentNullException GetException()
            {
                return new ArgumentNullException($"PersistenceId is [null] for PersistentActor [{self.Path}]");
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_AtomicWrite(IImmutableList<IPersistentRepresentation> payload)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"AtomicWrite must contain messages for the same persistenceId, yet difference persistenceIds found: {payload.Select(m => m.PersistenceId).Distinct()}.", nameof(payload));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MissingPlugin(string configPath)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Persistence config is missing plugin config path for: {configPath}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_PluginTypeIsNull(string configPath)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Plugin class name must be defined in config property [{configPath}.class]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UnknownPluginActor(IActorRef journalPluginActor)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Unknown plugin actor {journalPluginActor}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_qualifiedName(string qualifiedName)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("Configured " + qualifiedName + " does not implement any EventAdapter interface!");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_qualifiedName<T>(string qualifiedName)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException(
                    $"Couldn't create instance of [{typeof(T)}] from provided qualified type name [{qualifiedName}], because it's not assignable from it");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_pluginId(string pluginId)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Unknown plugin type: {pluginId}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_pluginId(string pluginId, string key)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"{pluginId}.{key} must be defined.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MessageSerializer(object obj)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var objType = obj is Type t ? t : obj?.GetType();
                return new ArgumentException($"Can't serialize object of type [{objType}] in [{typeof(PersistenceMessageSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MessageSerializerFSM(object obj)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var objType = obj is Type t ? t : obj?.GetType();
                return new ArgumentException($"Can't serialize object of type [{objType}] in [{typeof(PersistentFSMSnapshotSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Serializer_D(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var type = obj as Type;
                var typeQualifiedName = type != null ? type.TypeQualifiedName() : obj?.GetType().TypeQualifiedName();
                return new ArgumentException($"Cannot deserialize object of type [{typeQualifiedName}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_SnapshotSerializer(object obj)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Can't serialize object of type [{obj?.GetType()}] in [{typeof(PersistenceSnapshotSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_SnapshotSerializer(Type type)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with type [{type}] in [{typeof(PersistenceSnapshotSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object ThrowArgumentException_Serializer(int manifest)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [${nameof(PersistenceMessageSerializer)}]");
            }
        }

        #endregion

        #region -- IllegalStateException --

        private static readonly IllegalStateException OnlyReadSideException = new IllegalStateException(
                "CombinedReadEventAdapter must not be used when writing (creating manifests) events!");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_OnlyReadSide()
        {
            throw OnlyReadSideException;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException(ExceptionResource resource)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException(string msg)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException(msg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException(int atomicWriteCount, int count)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"AsyncWriteMessages return invalid number or results. Expected [{atomicWriteCount}], but got [{count}].");
            }
        }

        #endregion

        #region -- NullReferenceException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNullReferenceException()
        {
            throw GetException();
            NullReferenceException GetException()
            {
                return new NullReferenceException("Default journal plugin is not configured");
            }
        }

        #endregion

        #region -- NotSupportedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_Deliver()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException(
                    "Delivering to wildcard actor selections is not supported by AtLeastOnceDelivery. " +
                    "Introduce an mediator Actor which this AtLeastOnceDelivery Actor will deliver the messages to," +
                    "and will handle the logic of fan-out and collecting individual confirmations, until it can signal confirmation back to this Actor.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_AsyncTaskRun()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException("RunTask calls cannot be nested");
            }
        }

        #endregion

        #region -- IOException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIOException(DirectoryInfo dir, Exception exception)
        {
            throw GetException();
            IOException GetException()
            {
                return new IOException("Failed to create snapshot directory " + dir.FullName, exception);
            }
        }

        #endregion

        #region -- SerializationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSerializationException(Type type)
        {
            throw GetException();
            SerializationException GetException()
            {
                return new SerializationException($"Unimplemented deserialization of message with type [{type}] in [{typeof(PersistenceMessageSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSerializationException_FSM(Type type)
        {
            throw GetException();
            SerializationException GetException()
            {
                return new SerializationException($"Unimplemented deserialization of message with type [{type}] in [{typeof(PersistentFSMSnapshotSerializer)}]");
            }
        }

        #endregion

        #region -- MaxUnconfirmedMessagesExceededException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowMaxUnconfirmedMessagesExceededException(IActorContext context, int maxUnconfirmedMessages)
        {
            throw GetException();
            MaxUnconfirmedMessagesExceededException GetException()
            {
                return new MaxUnconfirmedMessagesExceededException(
                    $"{context.Self} has too many unconfirmed messages. Maximum allowed is {maxUnconfirmedMessages}");
            }
        }

        #endregion

        #region -- ConfigurationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException(string replayFilterMode)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Invalid replay-filter.mode [{replayFilterMode}], supported values [off, repair-by-discard-old, fail, warn]");
            }
        }

        #endregion
    }
}
