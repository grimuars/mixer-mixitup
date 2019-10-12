using MixItUp.Base.Util;
using System;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public abstract class AsyncServiceBase
    {
        protected async Task RunAsync(Task task) { await AsyncRunner.RunAsync(task); }

        protected async Task<T> RunAsync<T>(Task<T> task) { return await AsyncRunner.RunAsync(task); }

        protected async Task RunAsync(Func<Task> task) { await AsyncRunner.RunAsync(task); }

        protected async Task<T> RunAsync<T>(Func<Task<T>> task) { return await AsyncRunner.RunAsync(task); }
    }
}
