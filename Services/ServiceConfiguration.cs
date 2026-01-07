using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using EnvironmentSpanner.ViewModels;

namespace EnvironmentSpanner.Services;

public static class ServiceConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        try
        {
            Log.Information("Starting service configuration");
            
            // Configure Serilog
            Log.Information("Configuring Serilog");
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EnvironmentSpanner");
            Log.Information("Log directory path: {AppDataPath}", appDataPath);
            
            Directory.CreateDirectory(appDataPath);
            Log.Information("Log directory created or already exists");
            
            var logPath = Path.Combine(appDataPath, "EnvironmentSpanner_.log");
            Log.Information("Log file path: {LogPath}", logPath);
            
            Log.Information("Creating Serilog logger configuration");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            Log.Information("Serilog logger created successfully");
            
            // Add logging
            Log.Information("Adding logging to service collection");
            services.AddLogging(builder =>
            {
                builder.AddSerilog();
            });
            Log.Information("Logging added to service collection");
            
            Log.Information("Registering IEnvironmentVariableService as singleton");
            services.AddSingleton<IEnvironmentVariableService, EnvironmentVariableService>();
            Log.Information("IEnvironmentVariableService registered");
            
            Log.Information("Registering MainWindowViewModel as transient");
            services.AddTransient<MainWindowViewModel>();
            Log.Information("MainWindowViewModel registered");
            
            Log.Information("Service configuration completed successfully");
            return services;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during service configuration");
            throw;
        }
    }
}
