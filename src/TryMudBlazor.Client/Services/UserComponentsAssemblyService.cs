using System.Reflection;
using Try.UserComponents;

namespace TryMudBlazor.Client.Services;

public class UserComponentsAssemblyService
{
    private readonly List<Func<Assembly, Task>> _callbacks = [];

    public Assembly Assembly { get; private set; } = typeof(__Main).Assembly;

    public IDisposable Subscribe(Func<Assembly, Task> callback)
    {
        _callbacks.Add(callback);
        return new Subscription(() => _callbacks.Remove(callback));
    }

    public async Task UpdateAssemblyAsync(Assembly assembly)
    {
        Assembly = assembly;
        foreach (var callback in _callbacks)
        {
            await callback(assembly);
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }
}
