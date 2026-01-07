using System.Collections.Generic;
using EnvironmentSpanner.Models;

namespace EnvironmentSpanner.Services;

public interface IEnvironmentVariableService
{
    IEnumerable<EnvironmentVariable> GetEnvironmentVariables(EnvironmentVariableTarget target);
    void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target);
    void DeleteEnvironmentVariable(string name, EnvironmentVariableTarget target);
    bool IsElevated();
}
