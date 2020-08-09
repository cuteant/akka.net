using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Actor;
using Akka.Pattern;
using Akka.Streams.Dsl;
using Akka.Streams.Implementation;
using Akka.Streams.Implementation.Fusing;
using Akka.Streams.Serialization;
using Akka.Streams.Stage;
using Reactive.Streams;

namespace Akka.Streams
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        actorRef,
        subscriber,
        subscription,
        impl,
        element,
        cause,
        key,
        conversion,
        addHandler,
        removeHandler,
        state,
        name,
        outlet,
        inlets,
        inlet,
        outlets,
        module,
        shape,
        in1,
        in2,
        out1,
        out2,
        value,
        timerKey,
        context,
        onPush,
        onPull,
        n,
        timeout,
        count,
        settings,
        step,
        maxBuffer,
        segmentSize,
        inputPorts,
        bufferSize,
        maximumBurst,
        elements,
        per,
        cost,
        chunkSize,
        startPosition,
        initialBuffer,
        buffer,
        offset,
        increaseStep,
        maxDelay,
        fieldLength,
        secondaryPorts,
        outputPorts,
        junction,
        perProducerBufferSize,
        maxStep,
        inheritedAttributes,
        size,
        outPort,
        inPort,
        oldShape,
        maximumRetries,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
        #region ArgumentNull

        ArgumentNull_RequiresIActorRef,
        ArgumentNull_RequiresIActorRef_PS,
        ArgumentNull_SubscriberIsNull,
        ArgumentNull_OnSubscribeRequire,
        ArgumentNull_OnNextRequire,
        ArgumentNull_OnErrorRequire,
        ArgumentNull_RequireActorImpl,
        ArgumentNull_KeyIsNull,
        ArgumentNull_FlowShape_Inlet,
        ArgumentNull_FlowShape_Outlet,
        ArgumentNull_SinkShape_Inlet,
        ArgumentNull_NameAttrIsNull,
        ArgumentNull_TimerKeyIsNull,
        ArgumentNull_IActorRefFactoryIsNull,
        ArgumentNull_GraphStageLogic_OnPush,
        ArgumentNull_GraphStageLogic_OnPull,

        #endregion

        #region Argument

        Argument_Timeout_NeedNonZero,
        Argument_SegmentSize_NeedGreaterThanZero,
        Argument_InputPorts_NeedGreaterThanOne,
        Argument_BufferCount_PowerofTwo,
        Argument_MaximumBurst_GreaterThanZero,
        Argument_ThrottlePer_NonZero,
        Argument_Rates_NotSupported,
        Argument_Offset_Count_Length,
        Argument_module_cannot_be_added_to_itself,
        Argument_submodule_cannot_be_added_again,
        Argument_Increase_step_non_positive,
        Argument_Maxdelay_less_than_initial,
        Argument_Framing_FieldLength,
        Argument_Merge_must_have_input_ports,
        Argument_MergePreferred_least_one,
        Argument_Priorities_be_positive_int,
        Argument_Broadcast_must_have_outputports,
        Argument_Balance_must_have_outputports,
        Argument_Concat_must_have_inputports,
        Argument_Nomore_outlets_jun,
        Argument_Nomore_inlets_jun,
        Argument_Nomore_inlets_free_jun,
        Argument_Hub_buffer_positive,
        Argument_Hub_buffer_4095,
        Argument_Hub_buffer_power_two,
        Argument_Sample_Max_step,
        Argument_Actor_must_be_publisher,
        Argument_Buffer_must_be_g_t_zero,
        Argument_Buffersize_zero,
        Argument_Subscription_IsNull,
        Argument_EMC_with_empty_stack,
        Argument_PMS_without_context,
        Argument_GraphAssembly_IO,
        Argument_GraphAssembly_O,
        Argument_GraphAssembly_OO,
        Argument_retries_non_negative,
        Argument_FanOut_IdToEn,
        Argument_RMRRB_initialSize,
        Argument_RMRRB_maxSize,
        Argument_ReplaceShape_CopiedModule,
        Argument_ReplaceShape_CombinedModule,
        Argument_ReplaceShape_StructuralInfoModule,
        Argument_ReplaceShape_FusedModule,
        Argument_Inletname_IsNull,
        Argument_subscription_timeout,
        Argument_empty_iterator,
        Argument_GraphStage_ReadMany,
        Argument_GraphStage_PullC,
        Argument_GraphStage_Twice,
        Argument_GraphStage_Grab,

        #endregion

        #region SignalThrew

        SignalThrew_from_cancel,
        SignalThrew_from_request,

        #endregion

        #region IllegalState

        IllegalState_Pump_NonInitialized,
        IllegalState_Pump_CompletedPhaseNeverExec,
        IllegalState_OnNext_NotAllowed,
        IllegalState_OnNext_AfterE,
        IllegalState_OnNext_AfterC,
        IllegalState_OnComplete_AfterE,
        IllegalState_OnComplete_Once,
        IllegalState_OnError_Once,
        IllegalState_OnError_AfterC,
        IllegalState_Inputbuffer_Overrun,
        IllegalState_queue_never_null,
        IllegalState_Descend_EmptyResult,
        IllegalState_Actor_Cannot_Restart,
        IllegalState_signal_err_spec,
        IllegalState_Al_emitting_state,
        IllegalState_AlIn_emitting_state,
        IllegalState_OnPull_empty_enumerator,
        IllegalState_OnPush_not_allowed,
        IllegalState_Substream_Source,
        IllegalState_uninitialized_substream,
        IllegalState_init_mus_first,
        IllegalState_first_msg_init,
        IllegalState_incoming_conn,
        IllegalState_call_materialize,
        IllegalState_sub_called_after_err,
        IllegalState_tried_dequeue,
        IllegalState_Sub_throw_ex,
        IllegalState_Call_NotReached,
        IllegalState_Output_buf_overflow,
        IllegalState_PushToDownStream_A,
        IllegalState_Not_yet_init,
        IllegalState_internal_err,

        #endregion

        #region NotSupported

        NotSupported_Stream_Only_R,
        NotSupported_Stream_Only_W,
        NotSupported_Backpressure_strategy,
        NotSupported_IteratorInterpreter,
        NotSupported_replace_shapeOfSrc,
        NotSupported_replace_shapeOfSink,
        NotSupported_OS_Backpressure,
        NotSupported_replace_EmptyM,
        NotSupported_invalid_CMV,
        NotSupported_EmptyModule,
        NotSupported_replace_shapeOfFM,
        NotSupported_IO_operation,
        NotSupported_NoMaterializer_named,
        NotSupported_NoMaterializer_materialize,
        NotSupported_NoMaterializer_event,
        NotSupported_NoMaterializer_repeatedevent,
        NotSupported_NoMaterializer_excontext,
        NotSupported_AbsorbTermination,

        #endregion

        #region InvalidOperation

        InvalidOperation_SinkFirst_empty_stream,
        InvalidOperation_top_level_module,
        InvalidOperation_empty_module_cannot_be_mat,
        InvalidOperation_Chunk_be_pulled,
        InvalidOperation_Something_wrong,

        #endregion
    }

    #endregion

    partial class ThrowHelper
    {
        #region -- Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException(Exception ex)
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("Log stage can only provide LoggingAdapter when used with ActorMaterializer! Provide a LoggingAdapter explicitly or use the actor based flow materializer.", ex);
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Downcast(IMaterializer materializer)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Expected {typeof(ActorMaterializer)} but got {materializer.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Push_Closed<T>(Outlet<T> outlet)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Cannot push closed port {outlet}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Push_Twice<T>(Outlet<T> outlet)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Cannot push port twice {outlet}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Unzip(object message)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Unable to unzip elements of type {message.GetType().Name}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Fusing_WireOut(OutPort outPort, InPort inPort)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"wiring {outPort} -> {inPort}", nameof(outPort));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Fusing_WireIn(OutPort outPort, InPort inPort)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"wiring {outPort} -> {inPort}", nameof(inPort));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Fusing_RewireIn(Inlet oin, Inlet nin)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"rewiring {oin} -> {nin}", "oldShape");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Fusing_RewireOut(Outlet oout, Outlet nout)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"rewiring {oout} -> {nout}", "oldShape");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_GraphAssembly_In(Inlet inlet)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Inlet {inlet} was shared among multiple stages. That is illegal.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_GraphAssembly_Out(Outlet outlet)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Outlet {outlet} was shared among multiple stages. That is illegal.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Sampling_Step(int nextStep)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Sampling step should be a positive value: {nextStep}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Tcp_Bind(string host)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Couldn't resolve IpAdress for host {host}", nameof(host));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_FanIn_Dequeue(int id)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Can't dequeue from depleted {id}", nameof(id));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_FanIn_Pending(int id)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"No pending input at {id}", nameof(id));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_InvalidStreamId(long id)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Invalid stream identifier: {id}", nameof(id));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UnexpectedModule()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("unexpected module structure");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException(IActorRefFactory context)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"ActorRefFactory context must be a ActorSystem or ActorContext, got [{context.GetType()}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ConnCount(int inletsCount, int outletsCount)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Sum of inlets ({inletsCount}) and outlets ({outletsCount}) must be > 0");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ProposedInlets(ImmutableArray<Inlet> inlets)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Proposed inlets [{string.Join(", ", inlets)}] don't fit BidiShape");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ProposedOutlets(ImmutableArray<Outlet> outlets)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Proposed outlets [{string.Join(", ", outlets)}] don't fit BidiShape");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ProposedInlets1(ImmutableArray<Inlet> inlets)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Proposed inlets [{string.Join(", ", inlets)}] do not fit FanInShape");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ProposedOutlets1(ImmutableArray<Outlet> outlets)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Proposed outlets [{string.Join(", ", outlets)}] do not fit FanInShape");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ProposedInlets2(ImmutableArray<Inlet> inlets)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Proposed inlets [{string.Join(", ", inlets)}] do not fit FanOutShape");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ProposedOutlets2(ImmutableArray<Outlet> outlets)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Proposed outlets [{string.Join(", ", outlets)}] do not fit FanOutShape");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MaxBuffer(int maxBuffer)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"maxBuffer must be >= initialBuffer (was {maxBuffer})", nameof(maxBuffer));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_FitClosedShape(ExceptionArgument argument)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var arguName = GetArgumentName(argument);
                return new ArgumentException($"Proposed {arguName} do not fit ClosedShape", arguName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_FitSourceShape(ExceptionArgument argument)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var arguName = GetArgumentName(argument);
                return new ArgumentException($"Proposed {arguName} do not fit SourceShape", arguName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_FitFlowShape(ExceptionArgument argument)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var arguName = GetArgumentName(argument);
                return new ArgumentException($"Proposed {arguName} do not fit FlowShape", arguName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_FitSinkShape(ExceptionArgument argument)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var arguName = GetArgumentName(argument);
                return new ArgumentException($"Proposed {arguName} do not fit SinkShape", arguName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_GreaterThanZero(ExceptionArgument argument)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var arguName = GetArgumentName(argument);
                return new ArgumentException($"{arguName} must be greater than 0", arguName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_GreaterThanZero<T>(ExceptionArgument argument, T value)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var arguName = GetArgumentName(argument);
                return new ArgumentException($"{arguName} must be greater than 0 (was {value})", arguName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_GreaterThanEqualZero(ExceptionArgument argument)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var arguName = GetArgumentName(argument);
                return new ArgumentException($"{arguName} must be greater than or equal 0", arguName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_GreaterThanEqualZero<T>(ExceptionArgument argument, T value)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var arguName = GetArgumentName(argument);
                return new ArgumentException($"{arguName} must be greater than or equal 0 (was {value})", arguName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_GreaterThanEqualOne(int waitForUpstream)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"WaitForUpstream must be >= 1 (was {waitForUpstream})");
            }
        }

        internal static void ThrowArgumentException_StreamLayout_WireOut(IImmutableDictionary<OutPort, InPort> downstreams, OutPort from)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var message = downstreams.ContainsKey(from)
                    ? $"The output port [{from}] is already connected"
                    : $"The output port [{from}] is not part of underlying graph";
                return new ArgumentException(message);
            }
        }

        internal static void ThrowArgumentException_StreamLayout_WireIn(IImmutableDictionary<InPort, OutPort> upstreams, InPort to)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                var message = upstreams.ContainsKey(to)
                    ? $"The input port [{to}] is already connected"
                    : $"The input port [{to}] is not part of underlying graph";
                return new ArgumentException(message);
            }
        }

        internal static void ThrowArgumentException_StreamLayout_Atomics(StreamLayout.IMaterializedValueNode node)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("Couldn't extract atomics for node " + node.GetType());
            }
        }

        internal static void ThrowArgumentException_KeepAliveConcat_Ctor()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("The buffer keep alive failover size must be greater than 0.", "keepAliveFailoverSize");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_StreamRefMessages(object obj)
        {
            return new ArgumentException($"Can't serialize object of type [{(obj as Type) ?? obj.GetType()}] in [{nameof(StreamRefSerializer)}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_StreamRefMessages(string manifest)
        {
            return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [{nameof(StreamRefSerializer)}]");
        }

        #endregion

        #region -- SignalThrewException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException(ExceptionResource resource, Exception exc)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException(GetResourceString(resource), exc);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException_Sub<T>(ISubscriber<T> subscriber, Exception e)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException($"{subscriber}.OnSubscribe", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException_Sub(IUntypedSubscriber subscriber, Exception e)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException($"{subscriber}.OnSubscribe", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException_N<T>(ISubscriber<T> subscriber, Exception e)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException($"{subscriber}.OnNext", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException_N(IUntypedSubscriber subscriber, Exception e)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException($"{subscriber}.OnNext", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException_E<T>(ISubscriber<T> subscriber, Exception e)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException($"{subscriber}.OnError", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException_E(IUntypedSubscriber subscriber, Exception e)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException($"{subscriber}.OnError", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException_C<T>(ISubscriber<T> subscriber, Exception e)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException($"{subscriber}.OnComplete", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSignalThrewException_C(IUntypedSubscriber subscriber, Exception e)
        {
            throw GetException();
            SignalThrewException GetException()
            {
                return new SignalThrewException($"{subscriber}.OnComplete", e);
            }
        }

        #endregion

        #region -- IllegalStateException --

        const string NoDemand = "spec violation: OnNext was signaled from upstream without demand";
        private static readonly IllegalStateException _NoDemandException = new IllegalStateException(NoDemand);
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_NoDemand()
        {
            throw _NoDemandException;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IllegalStateException GetIllegalStateException_NoDemand()
        {
            return _NoDemandException;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IllegalStateException GetIllegalStateException_Key<TKey>(TKey key)
        {
            return new IllegalStateException($"Cannot open substream for key {key}: too many substreams open");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IllegalStateException GetIllegalStateException_Stream_supervisor_must_be_a_local_actor(ActorMaterializer materializer)
        {
            return new IllegalStateException($"Stream supervisor must be a local actor, was [{materializer.Supervisor.GetType()}]");
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
        internal static void ThrowIllegalStateException(ExceptionResource resource, Exception exc)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException(GetResourceString(resource), exc);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException<T>(Inlet<T> inlet)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"Already reading on inlet {inlet}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_Grab(object subSinkInlet)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"cannot grab element from port {subSinkInlet} when data has not yet arrived");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_PussT(object subSinkInlet)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"cannot pull port {subSinkInlet} twice");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_PussC(object subSinkInlet)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"cannot pull closed port {subSinkInlet}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_Pump_InitialPhase(IPump self)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"Initial state expected NotInitialized, but got {self.TransferState}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_GraphAssembly_In(GraphStageLogic logic, Inlet inlet)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"No handler defined in stage {logic} for port {inlet}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_GraphAssembly_Out(GraphStageLogic logic, Outlet outlet)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"No handler defined in stage {logic} for port {outlet}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_OnPushStrategy(DelayOverflowStrategy strategy)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"Delay buffer must never overflow in {strategy} mode");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_DispatchCommand(SubSink.CommandScheduledBeforeMaterialization newState, SubSink.CommandScheduledBeforeMaterialization command)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"{newState.Command} on subsink is illegal when {command.Command} is still pending");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_ActorOf(IActorRef supervisor)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"Stream supervisor must be a local actor, was [{supervisor.GetType()}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_FM(object message)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"The first message must be [{typeof(ExposedPublisher)}] but was [{message}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_FP(object message)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"The first message must be ExposedPublisher but was {message}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_SL_Valid(List<string> problems)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException(
                    $"module inconsistent, found {problems.Count} problems:\n - {string.Join("\n - ", problems)}");
            }
        }

        #endregion

        #region -- InvalidOperationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static InvalidOperationException GetInvalidOperationException_Expected_to_receive_response_of_type<TOut2>(object reply)
        {
            return new InvalidOperationException($"Expected to receive response of type {nameof(TOut2)}, but got: {reply}");
        }

        #endregion

        #region -- BufferOverflowException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowBufferOverflowException_Queue()
        {
            throw GetException();
            BufferOverflowException GetException()
            {
                return new BufferOverflowException("Queue is full");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static BufferOverflowException GetBufferOverflowException(int size)
        {
            return new BufferOverflowException($"Buffer overflow for Delay combinator (max capacity was: {size})!");
        }

        #endregion

        #region -- NotSupportedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static NotSupportedException GetNotSupportedException_UnknownOption(OverflowStrategy overflowStrategy)
        {
            return new NotSupportedException($"Unknown option: {overflowStrategy}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException(ExceptionResource resource)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException(Supervision.Directive decision)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"PushPullGraphLogic doesn't support supervision directive {decision}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException<T>(OverflowStrategy overflowStrategy)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"Unknown option: {overflowStrategy}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException<T, TMat>(Source<T, TMat> s)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"cannot drop Source of type {s.Module.GetType().Name}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_SL_IsIgnorable(IModule module)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"Module of type {module.GetType()} is not supported by this method");
            }
        }

        #endregion

        #region -- AggregateException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowAggregateException(Exception ex)
        {
            throw GetAggregateException();
            AggregateException GetAggregateException()
            {
                return new AggregateException(ex);
            }
        }

        #endregion

        #region -- IndexOutOfRangeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIndexOutOfRangeException_Framing()
        {
            throw GetArgumentException();
            IndexOutOfRangeException GetArgumentException()
            {
                return new IndexOutOfRangeException("LittleEndianDecoder reached end of byte string");
            }
        }

        #endregion

        #region -- UnexpectedOutputException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowUnexpectedOutputException(object element)
        {
            throw GetUnexpectedOutputException();
            UnexpectedOutputException GetUnexpectedOutputException()
            {
                return new UnexpectedOutputException(element);
            }
        }

        #endregion

        #region -- OutputTruncationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOutputTruncationException()
        {
            throw GetException();
            OutputTruncationException GetException()
            {
                return new OutputTruncationException();
            }
        }

        #endregion

        #region -- OverflowException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflowException()
        {
            throw GetException();
            OverflowException GetException()
            {
                return new OverflowException("Maximum throttle throughput exceeded.");
            }
        }

        #endregion

        #region -- IOException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIOException_Timeout()
        {
            throw GetException();
            IOException GetException()
            {
                return new IOException("Timeout on waiting for new data");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIOException_Timeout(TimeSpan readTimeout)
        {
            throw GetException();
            IOException GetException()
            {
                return new IOException($"Timeout after {readTimeout} waiting  Initialized message from stage");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIOException_OSIsClosed()
        {
            throw GetException();
            IOException GetException()
            {
                return new IOException("OutputStream is closed");
            }
        }

        private static readonly Exception PublisherClosedException = new IOException("Reactive stream is terminated, no writes are possible");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIOException_PublisherClosed()
        {
            throw PublisherClosedException;
        }

        private static readonly Exception SubscriberClosedException = new IOException("Reactive stream is terminated, no reads are possible");
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIOException_SubscriberClosed()
        {
            throw SubscriberClosedException;
        }

        #endregion

        #region -- FramingException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowFramingException(int maximumObjectLength)
        {
            throw GetException();
            Framing.FramingException GetException()
            {
                return new Framing.FramingException($"JSON element exceeded maximumObjectLength ({maximumObjectLength} bytes)!");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowFramingException(int pos, Akka.IO.ByteString buffer)
        {
            throw GetException();
            Framing.FramingException GetException()
            {
                return new Framing.FramingException($"Invalid JSON encountered at position {pos} of {buffer}");
            }
        }

        #endregion

        #region -- StageActorRefNotInitializedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowStageActorRefNotInitializedException()
        {
            throw StageActorRefNotInitializedException.Instance;
        }

        #endregion

        #region -- NothingToReadException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNothingToReadException()
        {
            throw NothingToReadException.Instance;
        }

        #endregion

        #region -- OperationCanceledException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static OperationCanceledException GetOperationCanceledException(CancellationToken token)
        {
            return new OperationCanceledException($"Stage cancelled due to cancellation token request.", token);
        }

        #endregion
    }
}
