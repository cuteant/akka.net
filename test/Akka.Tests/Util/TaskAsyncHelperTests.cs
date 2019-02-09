using System.Threading.Tasks;
using Akka.Util;
using CuteAnt.AsyncEx;
using Xunit;

namespace Akka.Tests.Util
{
    public class TaskAsyncHelperTests
    {
        private static readonly Task<int> s_completed = Task.FromResult(2);

        [Fact]
        public async Task ThenActionTest()
        {
            string s = null;
            void LocalAction(int v)
            {
                s = "s".PadRight(v, 's');
            }
            await TaskConstants.Completed.Then(LocalAction, 2);
            Assert.Equal("ss", s);
            await Task.Delay(100).Then(LocalAction, 3);
            Assert.Equal("sss", s);
        }

        [Fact]
        public async Task ThenFuncTest()
        {
            string LocalFunc(int v)
            {
                return "s".PadRight(v, 's');
            }
            var result = await TaskConstants.Completed.Then(LocalFunc, 2);
            Assert.Equal("ss", result);
            result = await Task.Delay(100).Then(LocalFunc, 3);
            Assert.Equal("sss", result);
        }
        [Fact]
        public async Task ThenTaskTest()
        {
            string s = null;
            Task LocalAction(int v)
            {
                s = "s".PadRight(v, 's');
                return TaskConstants.Completed;
            }
            await TaskConstants.Completed.Then(LocalAction, 2);
            Assert.Equal("ss", s);
            await Task.Delay(100).Then(LocalAction, 3);
            Assert.Equal("sss", s);
        }

        [Fact]
        public async Task ThenTaskFuncTest()
        {
            Task<string> LocalFunc(int v)
            {
                return Task.FromResult("s".PadRight(v, 's'));
            }
            var result = await TaskConstants.Completed.Then(LocalFunc, 2);
            Assert.Equal("ss", result);
            result = await Task.Delay(100).Then(LocalFunc, 3);
            Assert.Equal("sss", result);
        }

        [Fact]
        public async Task WithResultThenActionTest()
        {
            string s = null;
            void LocalAction(int v, char c)
            {
                s = c.ToString().PadRight(v, c);
            }
            await s_completed.Then(LocalAction, 's');
            Assert.Equal("ss", s);
            async Task<int> LocalFuncAsync()
            {
                await Task.Delay(100);
                return 3;
            }
            await LocalFuncAsync().Then(LocalAction, 's');
            Assert.Equal("sss", s);
        }

        [Fact]
        public async Task WithResultThenFuncTest()
        {
            string LocalFunc(int v, char c)
            {
                return c.ToString().PadRight(v, c);
            }
            var result = await s_completed.Then(LocalFunc, 's');
            Assert.Equal("ss", result);
            async Task<int> LocalFuncAsync()
            {
                await Task.Delay(100);
                return 3;
            }
            result = await LocalFuncAsync().Then(LocalFunc, 's');
            Assert.Equal("sss", result);
        }
        [Fact]
        public async Task WithResultThenTaskTest()
        {
            string s = null;
            Task LocalAction(int v, char c)
            {
                s = c.ToString().PadRight(v, c);
                return TaskConstants.Completed;
            }
            await s_completed.Then(LocalAction, 's');
            Assert.Equal("ss", s);
            async Task<int> LocalFuncAsync()
            {
                await Task.Delay(100);
                return 3;
            }
            await LocalFuncAsync().Then(LocalAction, 's');
            Assert.Equal("sss", s);
        }

        [Fact]
        public async Task WithResultThenTaskFuncTest()
        {
            Task<string> LocalFunc(int v, char c)
            {
                return Task.FromResult(c.ToString().PadRight(v, c));
            }
            var result = await s_completed.Then(LocalFunc, 's');
            Assert.Equal("ss", result);
            async Task<int> LocalFuncAsync()
            {
                await Task.Delay(100);
                return 3;
            }
            result = await LocalFuncAsync().Then(LocalFunc, 's');
            Assert.Equal("sss", result);
        }

        [Fact]
        public async Task LinkOutcomeActionTest()
        {
            string s = null;
            void LocalAction(Task t, int v)
            {
                s = "s".PadRight(v, 's');
            }
            await TaskConstants.Completed.LinkOutcome(LocalAction, 2);
            Assert.Equal("ss", s);
            await Task.Delay(100).LinkOutcome(LocalAction, 3);
            Assert.Equal("sss", s);
        }

        [Fact]
        public async Task LinkOutcomeFuncTest()
        {
            string LocalFunc(Task t, int v)
            {
                return "s".PadRight(v, 's');
            }
            var result = await TaskConstants.Completed.LinkOutcome(LocalFunc, 2);
            Assert.Equal("ss", result);
            result = await Task.Delay(100).LinkOutcome(LocalFunc, 3);
            Assert.Equal("sss", result);
        }
        [Fact]
        public async Task LinkOutcomeTaskTest()
        {
            string s = null;
            Task LocalAction(Task t, int v)
            {
                s = "s".PadRight(v, 's');
                return TaskConstants.Completed;
            }
            await TaskConstants.Completed.LinkOutcome(LocalAction, 2);
            Assert.Equal("ss", s);
            await Task.Delay(100).LinkOutcome(LocalAction, 3);
            Assert.Equal("sss", s);
        }

        [Fact]
        public async Task LinkOutcomeTaskFuncTest()
        {
            Task<string> LocalFunc(Task t,int v)
            {
                return Task.FromResult("s".PadRight(v, 's'));
            }
            var result = await TaskConstants.Completed.LinkOutcome(LocalFunc, 2);
            Assert.Equal("ss", result);
            result = await Task.Delay(100).LinkOutcome(LocalFunc, 3);
            Assert.Equal("sss", result);
        }

        [Fact]
        public async Task WithResultLinkOutcomeActionTest()
        {
            string s = null;
            void LocalAction(Task<int> v, char c)
            {
                s = c.ToString().PadRight(v.Result, c);
            }
            await s_completed.LinkOutcome(LocalAction, 's');
            Assert.Equal("ss", s);
            async Task<int> LocalFuncAsync()
            {
                await Task.Delay(100);
                return 3;
            }
            await LocalFuncAsync().LinkOutcome(LocalAction, 's');
            Assert.Equal("sss", s);
        }

        [Fact]
        public async Task WithResultLinkOutcomeFuncTest()
        {
            string LocalFunc(Task<int> v, char c)
            {
                return c.ToString().PadRight(v.Result, c);
            }
            var result = await s_completed.LinkOutcome(LocalFunc, 's');
            Assert.Equal("ss", result);
            async Task<int> LocalFuncAsync()
            {
                await Task.Delay(100);
                return 3;
            }
            result = await LocalFuncAsync().LinkOutcome(LocalFunc, 's');
            Assert.Equal("sss", result);
        }
        [Fact]
        public async Task WithResultLinkOutcomeTaskTest()
        {
            string s = null;
            Task LocalAction(Task<int> v, char c)
            {
                s = c.ToString().PadRight(v.Result, c);
                return TaskConstants.Completed;
            }
            await s_completed.LinkOutcome(LocalAction, 's');
            Assert.Equal("ss", s);
            async Task<int> LocalFuncAsync()
            {
                await Task.Delay(100);
                return 3;
            }
            await LocalFuncAsync().LinkOutcome(LocalAction, 's');
            Assert.Equal("sss", s);
        }

        [Fact]
        public async Task WithResultLinkOutcomeTaskFuncTest()
        {
            Task<string> LocalFunc(Task<int> v, char c)
            {
                return Task.FromResult(c.ToString().PadRight(v.Result, c));
            }
            var result = await s_completed.LinkOutcome(LocalFunc, 's');
            Assert.Equal("ss", result);
            async Task<int> LocalFuncAsync()
            {
                await Task.Delay(100);
                return 3;
            }
            result = await LocalFuncAsync().LinkOutcome(LocalFunc, 's');
            Assert.Equal("sss", result);
        }
    }
}
