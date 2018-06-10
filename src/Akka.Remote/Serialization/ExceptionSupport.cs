//-----------------------------------------------------------------------
// <copyright file="ExceptionSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Akka.Actor;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt.Reflection;
using MessagePack;
#if SERIALIZATION
using System.Runtime.Serialization;
#endif

namespace Akka.Remote.Serialization
{
    internal sealed class ExceptionSupport
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        private readonly ExtendedActorSystem _system;
        private const BindingFlags All = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private HashSet<string> DefaultProperties = new HashSet<string>(StringComparer.Ordinal)
        {
            "ClassName",
            "Message",
            "StackTraceString",
            "Source",
            "InnerException",
            "HelpURL",
            "RemoteStackTraceString",
            "RemoteStackIndex",
            "ExceptionMethod",
            "HResult",
            "Data",
            "TargetSite",
            "HelpLink",
            "StackTrace",
            "WatsonBuckets"
        };

        public ExceptionSupport(ExtendedActorSystem system) => _system = system;

        public byte[] SerializeException(Exception exception)
        {
            return MessagePackSerializer.Serialize(ExceptionToProto(exception), s_defaultResolver);
        }

        internal Protocol.ExceptionData ExceptionToProto(Exception exception)
        {
#if SERIALIZATION
            return ExceptionToProtoNet(exception);
#else
            return ExceptionToProtoNetCore(exception);
#endif
        }

        public Exception DeserializeException(byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.ExceptionData>(bytes, s_defaultResolver);
            return ExceptionFromProto(proto);
        }

        internal Exception ExceptionFromProto(Protocol.ExceptionData proto)
        {
#if SERIALIZATION
            return ExceptionFromProtoNet(proto);
#else
            return ExceptionFromProtoNetCore(proto);
#endif
        }

#if SERIALIZATION
        private readonly FormatterConverter DefaultFormatterConverter = new FormatterConverter();

        public Protocol.ExceptionData ExceptionToProtoNet(Exception exception)
        {
            if (exception == null) { return null; }

            var message = new Protocol.ExceptionData();

            var exceptionType = exception.GetType();

            message.TypeName = exceptionType.TypeQualifiedName();
            message.Message = exception.Message;
            message.StackTrace = exception.StackTrace ?? "";
            message.Source = exception.Source ?? "";
            message.InnerException = ExceptionToProto(exception.InnerException);

            var serializable = exception as ISerializable;
            var serializationInfo = new SerializationInfo(exceptionType, DefaultFormatterConverter);
            serializable.GetObjectData(serializationInfo, new StreamingContext());

            var customFields = new Dictionary<string, Protocol.Payload>();

            foreach (var info in serializationInfo)
            {
                if (DefaultProperties.Contains(info.Name)) continue;
                var preparedValue = WrappedPayloadSupport.PayloadToProto(_system, info.Value);
                customFields.Add(info.Name, preparedValue);
            }
            if (customFields.Count > 0) { message.CustomFields = customFields; }

            return message;
        }

        public Exception ExceptionFromProtoNet(Protocol.ExceptionData proto)
        {
            if (null == proto || string.IsNullOrEmpty(proto.TypeName)) { return null; }

            Type exceptionType = TypeUtils.ResolveType(proto.TypeName);

            var serializationInfo = new SerializationInfo(exceptionType, DefaultFormatterConverter);

            serializationInfo.AddValue("ClassName", proto.TypeName);
            serializationInfo.AddValue("Message", proto.Message);
            serializationInfo.AddValue("StackTraceString", proto.StackTrace);
            serializationInfo.AddValue("Source", proto.Source);
            serializationInfo.AddValue("InnerException", ExceptionFromProto(proto.InnerException));
            serializationInfo.AddValue("HelpURL", string.Empty);
            serializationInfo.AddValue("RemoteStackTraceString", string.Empty);
            serializationInfo.AddValue("RemoteStackIndex", 0);
            serializationInfo.AddValue("ExceptionMethod", string.Empty);
            serializationInfo.AddValue("HResult", int.MinValue);

            var customFields = proto.CustomFields;
            if (customFields != null)
            {
                foreach (var field in customFields)
                {
                    serializationInfo.AddValue(field.Key, WrappedPayloadSupport.PayloadFrom(_system, field.Value));
                }
            }

            Exception obj = null;
            ConstructorInfo constructorInfo = exceptionType.GetConstructor(
                All,
                null,
                new[] { typeof(SerializationInfo), typeof(StreamingContext) },
                null);

            if (constructorInfo != null)
            {
                object[] args = { serializationInfo, new StreamingContext() };
                obj = constructorInfo.Invoke(args).AsInstanceOf<Exception>();
            }

            return obj;
        }
#else
        private TypeInfo ExceptionTypeInfo = typeof(Exception).GetTypeInfo();
        private static readonly Func<Type, object> GetUninitializedObjectDelegate = (Func<Type, object>)
            typeof(string)
                .GetTypeInfo()
                .Assembly
                .GetType("System.Runtime.Serialization.FormatterServices")
                ?.GetTypeInfo()
                ?.GetMethod("GetUninitializedObject", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                ?.CreateDelegate(typeof(Func<Type, object>));

        internal Proto.Msg.ExceptionData ExceptionToProtoNetCore(Exception exception)
        {
            var message = new Proto.Msg.ExceptionData();

            if (exception == null)
                return message;

            var exceptionType = exception.GetType();

            message.TypeName = exceptionType.TypeQualifiedName();
            message.Message = exception.Message;
            message.StackTrace = exception.StackTrace ?? "";
            message.Source = exception.Source ?? "";
            message.InnerException = ExceptionToProto(exception.InnerException);

            // serialize all public properties
            foreach (var property in exceptionType.GetTypeInfo().DeclaredProperties)
            {
                if (DefaultProperties.Contains(property.Name)) continue;
                if (property.SetMethod != null)
                {
                    message.CustomFields.Add(property.Name, _wrappedPayloadSupport.PayloadToProto(property.GetValue(exception)));
                }
            }

            return message;
        }

        internal Exception ExceptionFromProtoNetCore(Proto.Msg.ExceptionData proto)
        {
            if (string.IsNullOrEmpty(proto.TypeName))
                return null;

            Type exceptionType = TypeUtils.ResolveType(proto.TypeName);

            var obj = GetUninitializedObjectDelegate(exceptionType);

            if (!string.IsNullOrEmpty(proto.Message))
                ExceptionTypeInfo?.GetField("_message", All)?.SetValue(obj, proto.Message);

            if (!string.IsNullOrEmpty(proto.StackTrace))
                ExceptionTypeInfo?.GetField("_stackTraceString", All)?.SetValue(obj, proto.StackTrace);

            if (!string.IsNullOrEmpty(proto.Source))
                ExceptionTypeInfo?.GetField("_source", All)?.SetValue(obj, proto.Source);

            if (!string.IsNullOrEmpty(proto.InnerException.TypeName))
                ExceptionTypeInfo?.GetField("_innerException", All)?.SetValue(obj, ExceptionFromProto(proto.InnerException));

            // deserialize all public properties with setters
            foreach (var property in proto.CustomFields)
            {
                if (DefaultProperties.Contains(property.Key)) continue;
                var prop = exceptionType.GetProperty(property.Key, All);
                if (prop.SetMethod != null)
                {
                    prop.SetValue(obj, _wrappedPayloadSupport.PayloadFrom(property.Value));
                }
            }

            return (Exception)obj;
        }
#endif
    }
}
