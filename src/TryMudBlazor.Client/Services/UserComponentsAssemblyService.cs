using System.Reflection;
using Try.UserComponents;

namespace TryMudBlazor.Client.Services;

public class UserComponentsAssemblyService
{
    public event Action OnAssemblyChanged;

    public Assembly Assembly { get; private set; } = typeof(__Main).Assembly;

    public void UpdateAssembly(Assembly assembly)
    {
        Assembly = assembly;
        OnAssemblyChanged?.Invoke();
    }
}