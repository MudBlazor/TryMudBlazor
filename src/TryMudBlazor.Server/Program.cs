using MudBlazor.Examples.Data;

namespace TryMudBlazor.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddScoped<IPeriodicTableService, PeriodicTableService>();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://www.mudblazor.com", "https://mudblazor.com");
            });
        });
        builder.Services.AddControllers();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCors();

        // Needed for wasm project
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapControllers();

        var version = GetVersion();
        var cacheBusting = $"v{version.Major}.{version.Minor}.{version.Build}";
        app.MapFallbackToFile("client/index.html")
            .CacheOutput(policy => policy.SetVaryByQuery("cachebust", cacheBusting));

        app.Run();
    }

    private static Version GetVersion()
    {
        var version = typeof(MudBlazor._Imports).Assembly.GetName().Version;

        return version ?? new Version(0, 0, 0);
    }
}