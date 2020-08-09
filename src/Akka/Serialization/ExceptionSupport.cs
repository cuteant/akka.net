//-----------------------------------------------------------------------
// <copyright file="ExceptionSupport.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Serialization.Protocol;
using Akka.Util;
using Akka.Util.Internal;
using MessagePack;

namespace Akka.Serialization
{
    public static class ExceptionSupport
    {
        private static readonly IFormatterResolver DefaultResolver = MessagePackSerializer.DefaultResolver;

        private static readonly FormatterConverter DefaultFormatterConverter = new FormatterConverter();

        private const BindingFlags All = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static readonly HashSet<string> DefaultProperties = new HashSet<string>(StringComparer.Ordinal)
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
        public static byte[] SerializeException(ExtendedActorSystem system, Exception exception)
        {
            return MessagePackSerializer.Serialize(ExceptionToProto(system, exception), DefaultResolver);
        }

        public static Protocol.ExceptionData ExceptionToProto(ExtendedActorSystem system, Exception exception)
        {
            if (exception == null) { return null; }

            var message = new Protocol.ExceptionData();

            var exceptionType = exception.GetType();

            message.ExceptionType = exceptionType;
            message.Message = exception.Message;
            message.StackTrace = exception.StackTrace ?? "";
            message.Source = exception.Source ?? "";
            message.InnerException = ExceptionToProto(system, exception.InnerException);

            var serializable = exception as ISerializable;
            var serializationInfo = new SerializationInfo(exceptionType, DefaultFormatterConverter);
            serializable.GetObjectData(serializationInfo, new StreamingContext());

            var customFields = new Dictionary<string, Payload>(StringComparer.Ordinal);

            foreach (var info in serializationInfo)
            {
                if (DefaultProperties.Contains(info.Name)) { continue; }
                customFields.Add(info.Name, system.SerializeMessage(info.Value));
            }
            if (customFields.Count > 0) { message.CustomFields = customFields; }

            return message;
        }

        public static Exception DeserializeException(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.ExceptionData>(bytes, DefaultResolver);
            return ExceptionFromProto(system, proto);
        }

        public static Exception ExceptionFromProto(ExtendedActorSystem system, Protocol.ExceptionData proto)
        {
            if (null == proto) { return null; }
            var exceptionType = proto.ExceptionType;
            if (null == proto.ExceptionType) { return null; }

            var serializationInfo = new SerializationInfo(exceptionType, DefaultFormatterConverter);

            serializationInfo.AddValue("ClassName", exceptionType.TypeQualifiedName());
            serializationInfo.AddValue("Message", proto.Message);
            serializationInfo.AddValue("StackTraceString", proto.StackTrace);
            serializationInfo.AddValue("Source", proto.Source);
            serializationInfo.AddValue("InnerException", ExceptionFromProto(system, proto.InnerException));
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
                    serializationInfo.AddValue(field.Key, system.Deserialize(field.Value));
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
    }
}
