﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Akka.Persistence.Internal {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Akka.Persistence.Internal.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   重写当前线程的 CurrentUICulture 属性
        ///   重写当前线程的 CurrentUICulture 属性。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 Payload of AtomicWrite must not be empty. 的本地化字符串。
        /// </summary>
        internal static string Argument_AtomicWrite {
            get {
                return ResourceManager.GetString("Argument_AtomicWrite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Couldn&apos;t initialize SnapshotStore instance, because associated Persistence extension has not been used in current actor system context. 的本地化字符串。
        /// </summary>
        internal static string Argument_Init_SnapshotStore {
            get {
                return ResourceManager.GetString("Argument_Init_SnapshotStore", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Couldn&apos;t initialize SyncWriteJournal instance, because associated Persistence extension has not been used in current actor system context. 的本地化字符串。
        /// </summary>
        internal static string Argument_Init_SyncWJ {
            get {
                return ResourceManager.GetString("Argument_Init_SyncWJ", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Mode must not be Disabled 的本地化字符串。
        /// </summary>
        internal static string Argument_Mode_NoDisabled {
            get {
                return ResourceManager.GetString("Argument_Mode_NoDisabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 AtLeastOnceDeliverySnapshot expects not null array of unconfirmed deliveries 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_AtLeastOnceDeliverySnapshot {
            get {
                return ResourceManager.GetString("ArgumentNull_AtLeastOnceDeliverySnapshot", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Payload of AtomicWrite must not be null. 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_AtomicWrite {
            get {
                return ResourceManager.GetString("ArgumentNull_AtomicWrite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 DeleteMessagesFailure cause exception cannot be null 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_DeleteMessagesFailure {
            get {
                return ResourceManager.GetString("ArgumentNull_DeleteMessagesFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 DeleteMessagesTo requires persistence id to be provided 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_DeleteMessagesTo {
            get {
                return ResourceManager.GetString("ArgumentNull_DeleteMessagesTo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 DeleteSnapshot requires SnapshotMetadata to be provided 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_DeleteSnapshot {
            get {
                return ResourceManager.GetString("ArgumentNull_DeleteSnapshot", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 maxOldWriters must be &gt; 0 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_maxOldWriters {
            get {
                return ResourceManager.GetString("ArgumentNull_maxOldWriters", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 AsyncWriteTarget.ReplayFailure cause exception cannot be null 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_ReplayFailure {
            get {
                return ResourceManager.GetString("ArgumentNull_ReplayFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 ReplayMessagesFailure cause exception cannot be null 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_ReplayMessagesFailure {
            get {
                return ResourceManager.GetString("ArgumentNull_ReplayMessagesFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 SaveSnapshot requires SnapshotMetadata to be provided 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_SaveSnapshot {
            get {
                return ResourceManager.GetString("ArgumentNull_SaveSnapshot", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 SetStore requires non-null reference to store actor 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_SetStore {
            get {
                return ResourceManager.GetString("ArgumentNull_SetStore", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 UnconfirmedWarning expects not null array of unconfirmed deliveries 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_UnconfirmedWarning {
            get {
                return ResourceManager.GetString("ArgumentNull_UnconfirmedWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 windowSize must be &gt; 0 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_windowSize {
            get {
                return ResourceManager.GetString("ArgumentNull_windowSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 WriteMessageFailure cause exception cannot be null 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_WriteMessageFailure {
            get {
                return ResourceManager.GetString("ArgumentNull_WriteMessageFailure", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 WriteMessageRejected cause exception cannot be null 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_WriteMessageRejected {
            get {
                return ResourceManager.GetString("ArgumentNull_WriteMessageRejected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 WriteMessagesFailed cause exception cannot be null 的本地化字符串。
        /// </summary>
        internal static string ArgumentNull_WriteMessagesFailed {
            get {
                return ResourceManager.GetString("ArgumentNull_WriteMessagesFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 You must call StartWith before calling Initialize. 的本地化字符串。
        /// </summary>
        internal static string IllegalState_call_SW_Init {
            get {
                return ResourceManager.GetString("IllegalState_call_SW_Init", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 You must call StartWith before calling StateData. 的本地化字符串。
        /// </summary>
        internal static string IllegalState_call_SW_SD {
            get {
                return ResourceManager.GetString("IllegalState_call_SW_SD", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 You must call StartWith before calling StateName. 的本地化字符串。
        /// </summary>
        internal static string IllegalState_call_SW_SN {
            get {
                return ResourceManager.GetString("IllegalState_call_SW_SN", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 NextStateData is only available during OnTransition 的本地化字符串。
        /// </summary>
        internal static string IllegalState_NextStateData {
            get {
                return ResourceManager.GetString("IllegalState_NextStateData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Permits must not be negative 的本地化字符串。
        /// </summary>
        internal static string IllegalState_PermitsNeedNonegative {
            get {
                return ResourceManager.GetString("IllegalState_PermitsNeedNonegative", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Cannot persist during replay. Events can be persisted when receiving RecoveryCompleted or later. 的本地化字符串。
        /// </summary>
        internal static string InvalidOperation_Cannot_persist_during_replay {
            get {
                return ResourceManager.GetString("InvalidOperation_Cannot_persist_during_replay", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 You may only call Command-methods when constructing the actor and inside Become(). 的本地化字符串。
        /// </summary>
        internal static string InvalidOperation_Command_methods {
            get {
                return ResourceManager.GetString("InvalidOperation_Command_methods", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 You may only call Recover-methods when constructing the actor and inside Become(). 的本地化字符串。
        /// </summary>
        internal static string InvalidOperation_Recover_methods {
            get {
                return ResourceManager.GetString("InvalidOperation_Recover_methods", resourceCulture);
            }
        }
    }
}
