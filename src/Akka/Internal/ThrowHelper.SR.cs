namespace Akka
{
    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum AkkaExceptionResource
    {
        Argument_ActorRefCompareTo,
        Argument_PropsCreate,
        Argument_BoundedMessageQueue,
        Argument_BoundedMailbox_Capacity,
        Argument_BoundedMailbox_PushTimeout,
        Argument_BackoffOptions_Min,
        Argument_BackoffOptions_Max,
        Argument_BackoffOptions_Random,
        Argument_ConsistentHash_VirtualNodesFactor,
        Argument_ResizablePoolCell_Pool,
        Argument_Futures_Ask,

        ArgumentNull_Mapping,
        ArgumentNull_Type,
        ArgumentNull_Compiler,
        ArgumentNull_CopyAndAdd,
        ArgumentNull_CopyAndRemove,
        ArgumentNull_LogMessage,
        ArgumentNull_LogMessageFormatter,
        ArgumentNull_FastLazyProducer,
        ArgumentNull_FastLazyState,
        ArgumentNull_LogMailbox,
        ArgumentNull_EsSubscriber,
        ArgumentNull_DeadLetterS,
        ArgumentNull_DeadLetterR,
        ArgumentNull_SuppressedDeadLetterS,
        ArgumentNull_SuppressedDeadLetterR,
        ArgumentNull_EnvelopeMsg,
        ArgumentNull_WatchWithMsg,

        IllegalState_FSM_Initialize,
        IllegalState_FSM_StateName,
        IllegalState_FSM_StateData,
        IllegalState_RepointableActorRef_Initialize,
        IllegalState_RepointableActorRef_Underlying,
        IllegalState_RepointableActorRef_IsStarted,

        InvalidActorName_Null,
        InvalidActorName_Empty,

        InvalidOperation_FSM_NextStateData,
        InvalidOperation_ActorCell_IsTerminating,
        InvalidOperation_ActorSystem_AlreadyTerminated,
        InvalidOperation_ActorRefProvider_Reg,
        InvalidOperation_ActorRefProvider_Unreg,
        InvalidOperation_ReceiveActor_Ensure,
        InvalidOperation_ActorTaskScheduler_RunTask,
        InvalidOperation_MatchBuilder_MatchAnyAdded,
        InvalidOperation_MatchBuilder_Built,
        InvalidOperation_Resolve_Produce,

        ActorInitialization_Actor_Ctor,

        NotSupported_Actor_Context,
        NotSupported_ActorPath_Uid,
        NotSupported_LocalActorRef_Bug,
        NotSupported_Can_Not_Serialize_LocalOnlyDecider,
        NotSupported_IsNotByteArray,

        ActorNotFound_ActorSel_Sub,
        ActorNotFound_ActorSel_Exc,

        InvalidMessage_MsgIsNull,
    }
}
