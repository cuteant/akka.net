using System;
using System.Threading.Tasks;

namespace Akka.Pattern
{
    public static class CircuitBreakerExtensions
    {
        /// <summary>
        /// Wraps invocation of asynchronous calls that need to be protected
        /// </summary>
        /// <typeparam name="TResult">TBD</typeparam>
        /// <param name="circuitBreaker">TBD</param>
        /// <param name="body">Call needing protected</param>
        /// <returns><see cref="Task"/> containing the call result</returns>
        public static Task<TResult> WithCircuitBreaker<TResult>(this CircuitBreaker circuitBreaker, Func<Task<TResult>> body)
        {
            return circuitBreaker.WithCircuitBreaker<TResult>(Runnable.CreateTask(body));
        }

        /// <summary>
        /// Wraps invocation of asynchronous calls that need to be protected
        /// </summary>
        /// <param name="circuitBreaker">TBD</param>
        /// <param name="body">Call needing protected</param>
        /// <returns><see cref="Task"/></returns>
        public static Task WithCircuitBreaker(this CircuitBreaker circuitBreaker, Func<Task> body)
        {
            return circuitBreaker.WithCircuitBreaker(Runnable.CreateTask(body));
        }

        /// <summary>
        /// The failure will be recorded farther down.
        /// </summary>
        /// <param name="circuitBreaker">TBD</param>
        /// <param name="body">TBD</param>
        public static void WithSyncCircuitBreaker(this CircuitBreaker circuitBreaker, Action body)
        {
            circuitBreaker.WithSyncCircuitBreaker(Runnable.Create(body));
        }

        /// <summary>
        /// Wraps invocations of asynchronous calls that need to be protected
        /// If this does not complete within the time allotted, it should return default(<typeparamref name="TResult"/>)
        ///
        /// <code>
        ///  Await.result(
        ///      withCircuitBreaker(try Future.successful(body) catch { case NonFatal(t) ⇒ Future.failed(t) }),
        ///      callTimeout)
        /// </code>
        ///
        /// </summary>
        /// <typeparam name="TResult">TBD</typeparam>
        /// <param name="circuitBreaker">TBD</param>
        /// <param name="body">TBD</param>
        /// <returns><typeparamref name="TResult"/> or default(<typeparamref name="TResult"/>)</returns>
        public static TResult WithSyncCircuitBreaker<TResult>(this CircuitBreaker circuitBreaker, Func<TResult> body)
        {
            return circuitBreaker.WithSyncCircuitBreaker<TResult>(Runnable.Create(body));
        }

        /// <summary>Adds a callback to execute when circuit breaker opens</summary>
        /// <param name="circuitBreaker">TBD</param>
        /// <param name="callback"><see cref="Action"/> Handler to be invoked on state change</param>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnOpen(this CircuitBreaker circuitBreaker, Action callback)
        {
            return circuitBreaker.OnOpen(Runnable.Create(callback));
        }

        /// <summary>Adds a callback to execute when circuit breaker transitions to half-open</summary>
        /// <param name="circuitBreaker">TBD</param>
        /// <param name="callback"><see cref="Action"/> Handler to be invoked on state change</param>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnHalfOpen(this CircuitBreaker circuitBreaker, Action callback)
        {
            return circuitBreaker.OnHalfOpen(Runnable.Create(callback));
        }

        /// <summary>Adds a callback to execute when circuit breaker state closes</summary>
        /// <param name="circuitBreaker">TBD</param>
        /// <param name="callback"><see cref="Action"/> Handler to be invoked on state change</param>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnClose(this CircuitBreaker circuitBreaker, Action callback)
        {
            return circuitBreaker.OnClose(Runnable.Create(callback));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/> containing the call result</returns>
        public static Task<TResult> WithCircuitBreaker<TArg1, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, Task<TResult>> body, TArg1 arg1)
        {
            return circuitBreaker.WithCircuitBreaker<TResult>(Runnable.CreateTask(body, arg1));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/></returns>
        public static Task WithCircuitBreaker<TArg1>(this CircuitBreaker circuitBreaker, Func<TArg1, Task> body, TArg1 arg1)
        {
            return circuitBreaker.WithCircuitBreaker(Runnable.CreateTask(body, arg1));
        }

        /// <summary>The failure will be recorded farther down.</summary>
        public static void WithSyncCircuitBreaker<TArg1>(this CircuitBreaker circuitBreaker, Action<TArg1> body, TArg1 arg1)
        {
            circuitBreaker.WithSyncCircuitBreaker(Runnable.Create(body, arg1));
        }

        /// <summary>
        /// Wraps invocations of asynchronous calls that need to be protected
        /// If this does not complete within the time allotted, it should return default(<typeparamref name="TResult"/>)
        ///
        /// <code>
        ///  Await.result(
        ///      withCircuitBreaker(try Future.successful(body) catch { case NonFatal(t) ⇒ Future.failed(t) }),
        ///      callTimeout)
        /// </code>
        ///
        /// </summary>
        /// <returns><typeparamref name="TResult"/> or default(<typeparamref name="TResult"/>)</returns>
        public static TResult WithSyncCircuitBreaker<TArg1, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TResult> body, TArg1 arg1)
        {
            return circuitBreaker.WithSyncCircuitBreaker<TResult>(Runnable.Create(body, arg1));
        }

        /// <summary>Adds a callback to execute when circuit breaker opens</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnOpen<TArg1>(this CircuitBreaker circuitBreaker, Action<TArg1> callback, TArg1 arg1)
        {
            return circuitBreaker.OnOpen(Runnable.Create(callback, arg1));
        }

        /// <summary>Adds a callback to execute when circuit breaker transitions to half-open</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnHalfOpen<TArg1>(this CircuitBreaker circuitBreaker, Action<TArg1> callback, TArg1 arg1)
        {
            return circuitBreaker.OnHalfOpen(Runnable.Create(callback, arg1));
        }

        /// <summary>Adds a callback to execute when circuit breaker state closes</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnClose<TArg1>(this CircuitBreaker circuitBreaker, Action<TArg1> callback, TArg1 arg1)
        {
            return circuitBreaker.OnClose(Runnable.Create(callback, arg1));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/> containing the call result</returns>
        public static Task<TResult> WithCircuitBreaker<TArg1, TArg2, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, Task<TResult>> body, TArg1 arg1, TArg2 arg2)
        {
            return circuitBreaker.WithCircuitBreaker<TResult>(Runnable.CreateTask(body, arg1, arg2));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/></returns>
        public static Task WithCircuitBreaker<TArg1, TArg2>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, Task> body, TArg1 arg1, TArg2 arg2)
        {
            return circuitBreaker.WithCircuitBreaker(Runnable.CreateTask(body, arg1, arg2));
        }

        /// <summary>The failure will be recorded farther down.</summary>
        public static void WithSyncCircuitBreaker<TArg1, TArg2>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2> body, TArg1 arg1, TArg2 arg2)
        {
            circuitBreaker.WithSyncCircuitBreaker(Runnable.Create(body, arg1, arg2));
        }

        /// <summary>
        /// Wraps invocations of asynchronous calls that need to be protected
        /// If this does not complete within the time allotted, it should return default(<typeparamref name="TResult"/>)
        ///
        /// <code>
        ///  Await.result(
        ///      withCircuitBreaker(try Future.successful(body) catch { case NonFatal(t) ⇒ Future.failed(t) }),
        ///      callTimeout)
        /// </code>
        ///
        /// </summary>
        /// <returns><typeparamref name="TResult"/> or default(<typeparamref name="TResult"/>)</returns>
        public static TResult WithSyncCircuitBreaker<TArg1, TArg2, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TResult> body, TArg1 arg1, TArg2 arg2)
        {
            return circuitBreaker.WithSyncCircuitBreaker<TResult>(Runnable.Create(body, arg1, arg2));
        }

        /// <summary>Adds a callback to execute when circuit breaker opens</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnOpen<TArg1, TArg2>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2> callback, TArg1 arg1, TArg2 arg2)
        {
            return circuitBreaker.OnOpen(Runnable.Create(callback, arg1, arg2));
        }

        /// <summary>Adds a callback to execute when circuit breaker transitions to half-open</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnHalfOpen<TArg1, TArg2>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2> callback, TArg1 arg1, TArg2 arg2)
        {
            return circuitBreaker.OnHalfOpen(Runnable.Create(callback, arg1, arg2));
        }

        /// <summary>Adds a callback to execute when circuit breaker state closes</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnClose<TArg1, TArg2>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2> callback, TArg1 arg1, TArg2 arg2)
        {
            return circuitBreaker.OnClose(Runnable.Create(callback, arg1, arg2));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/> containing the call result</returns>
        public static Task<TResult> WithCircuitBreaker<TArg1, TArg2, TArg3, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, Task<TResult>> body, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return circuitBreaker.WithCircuitBreaker<TResult>(Runnable.CreateTask(body, arg1, arg2, arg3));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/></returns>
        public static Task WithCircuitBreaker<TArg1, TArg2, TArg3>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, Task> body, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return circuitBreaker.WithCircuitBreaker(Runnable.CreateTask(body, arg1, arg2, arg3));
        }

        /// <summary>The failure will be recorded farther down.</summary>
        public static void WithSyncCircuitBreaker<TArg1, TArg2, TArg3>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3> body, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            circuitBreaker.WithSyncCircuitBreaker(Runnable.Create(body, arg1, arg2, arg3));
        }

        /// <summary>
        /// Wraps invocations of asynchronous calls that need to be protected
        /// If this does not complete within the time allotted, it should return default(<typeparamref name="TResult"/>)
        ///
        /// <code>
        ///  Await.result(
        ///      withCircuitBreaker(try Future.successful(body) catch { case NonFatal(t) ⇒ Future.failed(t) }),
        ///      callTimeout)
        /// </code>
        ///
        /// </summary>
        /// <returns><typeparamref name="TResult"/> or default(<typeparamref name="TResult"/>)</returns>
        public static TResult WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TResult> body, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return circuitBreaker.WithSyncCircuitBreaker<TResult>(Runnable.Create(body, arg1, arg2, arg3));
        }

        /// <summary>Adds a callback to execute when circuit breaker opens</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnOpen<TArg1, TArg2, TArg3>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return circuitBreaker.OnOpen(Runnable.Create(callback, arg1, arg2, arg3));
        }

        /// <summary>Adds a callback to execute when circuit breaker transitions to half-open</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnHalfOpen<TArg1, TArg2, TArg3>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return circuitBreaker.OnHalfOpen(Runnable.Create(callback, arg1, arg2, arg3));
        }

        /// <summary>Adds a callback to execute when circuit breaker state closes</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnClose<TArg1, TArg2, TArg3>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            return circuitBreaker.OnClose(Runnable.Create(callback, arg1, arg2, arg3));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/> containing the call result</returns>
        public static Task<TResult> WithCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, Task<TResult>> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return circuitBreaker.WithCircuitBreaker<TResult>(Runnable.CreateTask(body, arg1, arg2, arg3, arg4));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/></returns>
        public static Task WithCircuitBreaker<TArg1, TArg2, TArg3, TArg4>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, Task> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return circuitBreaker.WithCircuitBreaker(Runnable.CreateTask(body, arg1, arg2, arg3, arg4));
        }

        /// <summary>The failure will be recorded farther down.</summary>
        public static void WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TArg4>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            circuitBreaker.WithSyncCircuitBreaker(Runnable.Create(body, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Wraps invocations of asynchronous calls that need to be protected
        /// If this does not complete within the time allotted, it should return default(<typeparamref name="TResult"/>)
        ///
        /// <code>
        ///  Await.result(
        ///      withCircuitBreaker(try Future.successful(body) catch { case NonFatal(t) ⇒ Future.failed(t) }),
        ///      callTimeout)
        /// </code>
        ///
        /// </summary>
        /// <returns><typeparamref name="TResult"/> or default(<typeparamref name="TResult"/>)</returns>
        public static TResult WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TResult> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return circuitBreaker.WithSyncCircuitBreaker<TResult>(Runnable.Create(body, arg1, arg2, arg3, arg4));
        }

        /// <summary>Adds a callback to execute when circuit breaker opens</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnOpen<TArg1, TArg2, TArg3, TArg4>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return circuitBreaker.OnOpen(Runnable.Create(callback, arg1, arg2, arg3, arg4));
        }

        /// <summary>Adds a callback to execute when circuit breaker transitions to half-open</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnHalfOpen<TArg1, TArg2, TArg3, TArg4>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return circuitBreaker.OnHalfOpen(Runnable.Create(callback, arg1, arg2, arg3, arg4));
        }

        /// <summary>Adds a callback to execute when circuit breaker state closes</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnClose<TArg1, TArg2, TArg3, TArg4>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            return circuitBreaker.OnClose(Runnable.Create(callback, arg1, arg2, arg3, arg4));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/> containing the call result</returns>
        public static Task<TResult> WithCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task<TResult>> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return circuitBreaker.WithCircuitBreaker<TResult>(Runnable.CreateTask(body, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/></returns>
        public static Task WithCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return circuitBreaker.WithCircuitBreaker(Runnable.CreateTask(body, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>The failure will be recorded farther down.</summary>
        public static void WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            circuitBreaker.WithSyncCircuitBreaker(Runnable.Create(body, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Wraps invocations of asynchronous calls that need to be protected
        /// If this does not complete within the time allotted, it should return default(<typeparamref name="TResult"/>)
        ///
        /// <code>
        ///  Await.result(
        ///      withCircuitBreaker(try Future.successful(body) catch { case NonFatal(t) ⇒ Future.failed(t) }),
        ///      callTimeout)
        /// </code>
        ///
        /// </summary>
        /// <returns><typeparamref name="TResult"/> or default(<typeparamref name="TResult"/>)</returns>
        public static TResult WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return circuitBreaker.WithSyncCircuitBreaker<TResult>(Runnable.Create(body, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>Adds a callback to execute when circuit breaker opens</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnOpen<TArg1, TArg2, TArg3, TArg4, TArg5>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return circuitBreaker.OnOpen(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>Adds a callback to execute when circuit breaker transitions to half-open</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnHalfOpen<TArg1, TArg2, TArg3, TArg4, TArg5>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return circuitBreaker.OnHalfOpen(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>Adds a callback to execute when circuit breaker state closes</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnClose<TArg1, TArg2, TArg3, TArg4, TArg5>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            return circuitBreaker.OnClose(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/> containing the call result</returns>
        public static Task<TResult> WithCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task<TResult>> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return circuitBreaker.WithCircuitBreaker<TResult>(Runnable.CreateTask(body, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/></returns>
        public static Task WithCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, Task> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return circuitBreaker.WithCircuitBreaker(Runnable.CreateTask(body, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>The failure will be recorded farther down.</summary>
        public static void WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            circuitBreaker.WithSyncCircuitBreaker(Runnable.Create(body, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Wraps invocations of asynchronous calls that need to be protected
        /// If this does not complete within the time allotted, it should return default(<typeparamref name="TResult"/>)
        ///
        /// <code>
        ///  Await.result(
        ///      withCircuitBreaker(try Future.successful(body) catch { case NonFatal(t) ⇒ Future.failed(t) }),
        ///      callTimeout)
        /// </code>
        ///
        /// </summary>
        /// <returns><typeparamref name="TResult"/> or default(<typeparamref name="TResult"/>)</returns>
        public static TResult WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return circuitBreaker.WithSyncCircuitBreaker<TResult>(Runnable.Create(body, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>Adds a callback to execute when circuit breaker opens</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnOpen<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return circuitBreaker.OnOpen(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>Adds a callback to execute when circuit breaker transitions to half-open</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnHalfOpen<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return circuitBreaker.OnHalfOpen(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>Adds a callback to execute when circuit breaker state closes</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnClose<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            return circuitBreaker.OnClose(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/> containing the call result</returns>
        public static Task<TResult> WithCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task<TResult>> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return circuitBreaker.WithCircuitBreaker<TResult>(Runnable.CreateTask(body, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>Wraps invocation of asynchronous calls that need to be protected</summary>
        /// <returns><see cref="Task"/></returns>
        public static Task WithCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, Task> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return circuitBreaker.WithCircuitBreaker(Runnable.CreateTask(body, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>The failure will be recorded farther down.</summary>
        public static void WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            circuitBreaker.WithSyncCircuitBreaker(Runnable.Create(body, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// Wraps invocations of asynchronous calls that need to be protected
        /// If this does not complete within the time allotted, it should return default(<typeparamref name="TResult"/>)
        ///
        /// <code>
        ///  Await.result(
        ///      withCircuitBreaker(try Future.successful(body) catch { case NonFatal(t) ⇒ Future.failed(t) }),
        ///      callTimeout)
        /// </code>
        ///
        /// </summary>
        /// <returns><typeparamref name="TResult"/> or default(<typeparamref name="TResult"/>)</returns>
        public static TResult WithSyncCircuitBreaker<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this CircuitBreaker circuitBreaker, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> body, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return circuitBreaker.WithSyncCircuitBreaker<TResult>(Runnable.Create(body, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>Adds a callback to execute when circuit breaker opens</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnOpen<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return circuitBreaker.OnOpen(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>Adds a callback to execute when circuit breaker transitions to half-open</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnHalfOpen<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return circuitBreaker.OnHalfOpen(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>Adds a callback to execute when circuit breaker state closes</summary>
        /// <returns>CircuitBreaker for fluent usage</returns>
        public static CircuitBreaker OnClose<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this CircuitBreaker circuitBreaker, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> callback, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            return circuitBreaker.OnClose(Runnable.Create(callback, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

    }
}
