using System.Reflection;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Try.UserComponents;

namespace TryMudBlazor.Client.Extensions;

internal static class WebAssemblyHostBuilderExtensions
{
    private static readonly MethodInfo ConfigureMethod = ResolveConfigureMethod();

    public static void TryInvokeUserStartup(this WebAssemblyHostBuilder builder)
        => ConfigureMethod?.Invoke(null, [builder]);

    private static MethodInfo ResolveConfigureMethod()
    {
        var assembly = typeof(__Main).Assembly;

        var startupType =
            assembly.GetType("UserStartup", throwOnError: false, ignoreCase: true) ??
            assembly.GetType("Try.UserComponents.UserStartup", throwOnError: false, ignoreCase: true);

        var method = startupType?.GetMethod("Configure", BindingFlags.Static | BindingFlags.Public);
        if (method is null)
            return null;

        var parameters = method.GetParameters();
        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(WebAssemblyHostBuilder))
            return null;

        return method;
    }
}
