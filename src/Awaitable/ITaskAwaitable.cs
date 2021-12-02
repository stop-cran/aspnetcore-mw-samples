using System.Runtime.CompilerServices;

namespace SampleApp.Awaitable
{
    public interface ITaskAwaitable<T>
    {
        TaskAwaiter<T> GetAwaiter();
    }
}