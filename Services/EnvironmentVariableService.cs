using System;
using System.Collections.Generic;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using EnvironmentSpanner.Models;

namespace EnvironmentSpanner.Services;

public class EnvironmentVariableService : IEnvironmentVariableService
{
    private readonly ILogger<EnvironmentVariableService>? _logger;

    public EnvironmentVariableService(ILogger<EnvironmentVariableService>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("EnvironmentVariableService constructor called");
    }

    public IEnumerable<EnvironmentVariable> GetEnvironmentVariables(EnvironmentVariableTarget target)
    {
        try
        {
            _logger?.LogInformation("GetEnvironmentVariables called for target: {Target}", target);
            var variables = new List<EnvironmentVariable>();
            var envVars = Environment.GetEnvironmentVariables(target);
            _logger?.LogInformation("Retrieved {Count} environment variables from system", envVars.Count);

            foreach (System.Collections.DictionaryEntry entry in envVars)
            {
                variables.Add(new EnvironmentVariable
                {
                    Name = entry.Key?.ToString() ?? string.Empty,
                    Value = entry.Value?.ToString() ?? string.Empty,
                    Target = target
                });
            }

            _logger?.LogInformation("Returning {Count} environment variables", variables.Count);
            return variables;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting environment variables for target: {Target}", target);
            throw;
        }
    }

    public void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target)
    {
        try
        {
            _logger?.LogInformation("SetEnvironmentVariable called: Name={Name}, Target={Target}", name, target);
            Environment.SetEnvironmentVariable(name, value, target);
            _logger?.LogInformation("Environment variable set successfully: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting environment variable: {Name}", name);
            throw;
        }
    }

    public void DeleteEnvironmentVariable(string name, EnvironmentVariableTarget target)
    {
        try
        {
            _logger?.LogInformation("DeleteEnvironmentVariable called: Name={Name}, Target={Target}", name, target);
            Environment.SetEnvironmentVariable(name, null, target);
            _logger?.LogInformation("Environment variable deleted successfully: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error deleting environment variable: {Name}", name);
            throw;
        }
    }

    public bool IsElevated()
    {
        try
        {
            _logger?.LogInformation("Checking elevation status");
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            var isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            _logger?.LogInformation("Elevation status: {IsElevated}", isElevated);
            return isElevated;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking elevation status");
            throw;
        }
    }
}
