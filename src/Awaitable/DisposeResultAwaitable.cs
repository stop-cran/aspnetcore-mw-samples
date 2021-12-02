using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SampleApp.Awaitable
{
    public class DisposeResultAwaitable<T> : ITaskAwaitable<T>, IDisposable
    {
        private readonly Task<T> _task;

        public DisposeResultAwaitable(Task<T> task)
        {
            _task = task;
        }

        public TaskAwaiter<T> GetAwaiter() => _task.GetAwaiter();

        public void Dispose()
        {
            if (_task.IsCompletedSuccessfully && _task.Result is IDisposable disposable)
                disposable.Dispose();
        }
    }
}