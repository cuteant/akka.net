//-----------------------------------------------------------------------
// <copyright file="ReactiveStreamsCompliance.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Pattern;
using CuteAnt.Collections;
using Reactive.Streams;

namespace Akka.Streams.Implementation
{
    /// <summary>
    /// TBD
    /// </summary>
    public interface ISpecViolation { }

    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public class SignalThrewException : IllegalStateException, ISpecViolation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignalThrewException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="cause">The exception that is the cause of the current exception.</param>
        public SignalThrewException(string message, Exception cause) : base(message, cause) { }

#if SERIALIZATION
        /// <summary>
        /// Initializes a new instance of the <see cref="SignalThrewException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected SignalThrewException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
    }

    /// <summary>
    /// TBD
    /// </summary>
    public static class ReactiveStreamsCompliance
    {
        /// <summary>
        /// TBD
        /// </summary>
        public const string CanNotSubscribeTheSameSubscriberMultipleTimes =
            "can not subscribe the same subscriber multiple times (see reactive-streams specification, rules 1.10 and 2.12)";
        /// <summary>
        /// TBD
        /// </summary>
        public const string SupportsOnlyASingleSubscriber =
            "only supports one subscriber (which is allowed, see reactive-streams specification, rule 1.12)";
        /// <summary>
        /// TBD
        /// </summary>
        public const string NumberOfElementsInRequestMustBePositiveMsg =
            "The number of requested elements must be > 0 (see reactive-streams specification, rule 3.9)";
        /// <summary>
        /// TBD
        /// </summary>
        public const string SubscriberMustNotBeNullMsg = "Subscriber must not be null, rule 1.9";
        /// <summary>
        /// TBD
        /// </summary>
        public const string ExceptionMustNotBeNullMsg = "Exception must not be null, rule 2.13";
        /// <summary>
        /// TBD
        /// </summary>
        public const string ElementMustNotBeNullMsg = "Element must not be null, rule 2.13";
        /// <summary>
        /// TBD
        /// </summary>
        public const string SubscriptionMustNotBeNullMsg = "Subscription must not be null, rule 2.13";

        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Exception NumberOfElementsInRequestMustBePositiveException = new ArgumentException(NumberOfElementsInRequestMustBePositiveMsg);

        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Exception CanNotSubscribeTheSameSubscriberMultipleTimesException = new IllegalStateException(CanNotSubscribeTheSameSubscriberMultipleTimes);

        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Exception ElementMustNotBeNullException = new ArgumentNullException("element", ElementMustNotBeNullMsg);
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowElementMustNotBeNullException()
        {
            throw ElementMustNotBeNullException;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Exception SubscriptionMustNotBeNullException = new ArgumentNullException("subscription", SubscriptionMustNotBeNullMsg);
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSubscriptionMustNotBeNullException()
        {
            throw SubscriptionMustNotBeNullException;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Exception SubscriberMustNotBeNullException = new ArgumentNullException("subscriber", SubscriberMustNotBeNullMsg);
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSubscriberMustNotBeNullException()
        {
            throw SubscriberMustNotBeNullException;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public static readonly Exception ExceptionMustNotBeNullException = new ArgumentNullException("exception", ExceptionMustNotBeNullMsg);
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowExceptionMustNotBeNullException()
        {
            throw ExceptionMustNotBeNullException;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="subscriber">TBD</param>
        /// <param name="subscription">TBD</param>
        /// <exception cref="SignalThrewException">TBD</exception>
        public static void TryOnSubscribe<T>(ISubscriber<T> subscriber, ISubscription subscription)
        {
            try
            {
                subscriber.OnSubscribe(subscription);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException_Sub(subscriber, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscriber">TBD</param>
        /// <param name="subscription">TBD</param>
        /// <exception cref="SignalThrewException">TBD</exception>
        internal static void TryOnSubscribe(IUntypedSubscriber subscriber, ISubscription subscription)
        {
            try
            {
                subscriber.OnSubscribe(subscription);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException_Sub(subscriber, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="subscriber">TBD</param>
        /// <param name="element">TBD</param>
        /// <exception cref="SignalThrewException">TBD</exception>
        public static void TryOnNext<T>(ISubscriber<T> subscriber, T element)
        {
            RequireNonNullElement(element);
            try
            {
                subscriber.OnNext(element);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException_N(subscriber, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscriber">TBD</param>
        /// <param name="element">TBD</param>
        /// <exception cref="SignalThrewException">TBD</exception>
        internal static void TryOnNext(IUntypedSubscriber subscriber, object element)
        {
            RequireNonNullElement(element);
            try
            {
                subscriber.OnNext(element);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException_N(subscriber, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="subscriber">TBD</param>
        /// <param name="cause">TBD</param>
        /// <exception cref="IllegalStateException">TBD</exception>
        /// <exception cref="SignalThrewException">TBD</exception>
        public static void TryOnError<T>(ISubscriber<T> subscriber, Exception cause)
        {
            if (cause is ISpecViolation) ThrowHelper.ThrowIllegalStateException(ExceptionResource.IllegalState_signal_err_spec, cause);

            try
            {
                subscriber.OnError(cause);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException_E(subscriber, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscriber">TBD</param>
        /// <param name="cause">TBD</param>
        /// <exception cref="IllegalStateException">TBD</exception>
        /// <exception cref="SignalThrewException">TBD</exception>
        internal static void TryOnError(IUntypedSubscriber subscriber, Exception cause)
        {
            if (cause is ISpecViolation) ThrowHelper.ThrowIllegalStateException(ExceptionResource.IllegalState_signal_err_spec, cause);

            try
            {
                subscriber.OnError(cause);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException_E(subscriber, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="subscriber">TBD</param>
        /// <exception cref="SignalThrewException">TBD</exception>
        public static void TryOnComplete<T>(ISubscriber<T> subscriber)
        {
            try
            {
                subscriber.OnComplete();
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException_C(subscriber, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscriber">TBD</param>
        /// <exception cref="SignalThrewException">TBD</exception>
        internal static void TryOnComplete(IUntypedSubscriber subscriber)
        {
            try
            {
                subscriber.OnComplete();
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException_C(subscriber, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="subscriber">TBD</param>
        public static void RejectDuplicateSubscriber<T>(ISubscriber<T> subscriber)
        {
            // since it is already subscribed it has received the subscription first
            // and we can emit onError immediately
            TryOnError(subscriber, CanNotSubscribeTheSameSubscriberMultipleTimesException);
        }

        private static readonly CachedReadConcurrentDictionary<string, IllegalStateException> s_illegalStateExCache =
            new CachedReadConcurrentDictionary<string, IllegalStateException>();
        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="subscriber">TBD</param>
        /// <param name="rejector">TBD</param>
        public static void RejectAdditionalSubscriber<T>(ISubscriber<T> subscriber, string rejector)
        {
            TryOnSubscribe(subscriber, CancelledSubscription.Instance);
            var exc = s_illegalStateExCache.GetOrAdd(rejector, err => new IllegalStateException($"{err} {SupportsOnlyASingleSubscriber}"));
            TryOnError(subscriber, exc);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscriber">TBD</param>
        /// <param name="rejector">TBD</param>
        internal static void RejectAdditionalSubscriber(IUntypedSubscriber subscriber, string rejector)
        {
            TryOnSubscribe(subscriber, CancelledSubscription.Instance);
            var exc = s_illegalStateExCache.GetOrAdd(rejector, err => new IllegalStateException($"{err} {SupportsOnlyASingleSubscriber}"));
            TryOnError(subscriber, exc);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="subscriber">TBD</param>
        public static void RejectDueToNonPositiveDemand<T>(ISubscriber<T> subscriber)
        {
            TryOnError(subscriber, NumberOfElementsInRequestMustBePositiveException);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="subscriber">TBD</param>
        public static void RequireNonNullSubscriber<T>(ISubscriber<T> subscriber)
        {
            if (subscriber is null) ThrowSubscriberMustNotBeNullException();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscription">TBD</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="subscription"/> is undefined.
        /// </exception>
        public static void RequireNonNullSubscription(ISubscription subscription)
        {
            if (subscription is null) ThrowSubscriptionMustNotBeNullException();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="exception">TBD</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="exception"/> is undefined.
        /// </exception>
        public static void RequireNonNullException(Exception exception)
        {
            if (exception is null) ThrowExceptionMustNotBeNullException();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="element">TBD</param>
        /// <exception cref="ArgumentNullException">
        /// This exception is thrown when the specified <paramref name="element"/> is undefined.
        /// </exception>
        public static void RequireNonNullElement(object element)
        {
            if (element is null) ThrowElementMustNotBeNullException();
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscription">TBD</param>
        /// <exception cref="SignalThrewException">
        /// This exception is thrown when an exception occurs while canceling the specified <paramref name="subscription"/>.
        /// </exception>
        public static void TryCancel(ISubscription subscription)
        {
            try
            {
                subscription.Cancel();
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException(ExceptionResource.SignalThrew_from_cancel, e);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscription">TBD</param>
        /// <param name="demand">TBD</param>
        /// <exception cref="SignalThrewException">
        /// This exception is thrown when an exception occurs while requesting no events be sent to the specified <paramref name="subscription"/>.
        /// </exception>
        public static void TryRequest(ISubscription subscription, long demand)
        {
            try
            {
                subscription.Request(demand);
            }
            catch (Exception e)
            {
                ThrowHelper.ThrowSignalThrewException(ExceptionResource.SignalThrew_from_request, e);
            }
        }
    }
}
