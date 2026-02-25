namespace TryMudBlazor.Client.Services
{
    using System;
    using System.Reflection;
    using Try.UserComponents;

    public class UserComponentsAssemblyService
    {
        private Assembly _assembly = typeof(__Main).Assembly;

        public event Action OnAssemblyChanged;

        public Assembly Assembly => _assembly;

        public void UpdateAssembly(Assembly assembly)
        {
            _assembly = assembly;
            OnAssemblyChanged?.Invoke();
        }
    }
}
