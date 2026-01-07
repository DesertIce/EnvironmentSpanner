using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf;
using Serilog;
using EnvironmentSpanner.Services;

namespace EnvironmentSpanner;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            Log.Information("Application startup initiated");
            base.OnStartup(e);
            Log.Information("Base OnStartup completed");
            
            Log.Information("Configuring dependency injection");
            var services = new ServiceCollection();
            Log.Information("ServiceCollection created");
            
            Log.Information("Adding application services");
            services.AddApplicationServices();
            Log.Information("Application services added");
            
            Log.Information("Building service provider");
            ServiceProvider = services.BuildServiceProvider();
            Log.Information("Service provider built successfully");
            
            Log.Information("Setting application theme to Dark");
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            Log.Information("Application theme set");
            
            Log.Information("Application startup completed successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error during application startup");
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Application shutdown initiated");
            if (ServiceProvider is IDisposable disposable)
            {
                Log.Information("Disposing service provider");
                disposable.Dispose();
                Log.Information("Service provider disposed");
            }
            Log.Information("Closing and flushing Serilog");
            Log.CloseAndFlush();
            Log.Information("Serilog closed");
            base.OnExit(e);
            Log.Information("Application shutdown completed");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during application shutdown");
            throw;
        }
    }
}
