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

        protected async Task<bool> AttemptRunAsync(Func<Task<bool>> task, int attempts = 5)
        {
            for (int i = 0; i < attempts; i++)
            {
                if (await task())
                {
                    return true;
                }
                await Task.Delay(1000);
            }
            return false;
        }
    }
}
