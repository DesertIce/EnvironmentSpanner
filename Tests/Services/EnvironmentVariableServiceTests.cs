using EnvironmentSpanner.Models;
using EnvironmentSpanner.Services;
using Xunit;

namespace EnvironmentSpanner.Tests.Services;

public class EnvironmentVariableServiceTests
{
    private readonly EnvironmentVariableService _service = new();

    [Fact]
    public void GetEnvironmentVariables_UserTarget_ReturnsVariables()
    {
        // Act
        var variables = _service.GetEnvironmentVariables(EnvironmentVariableTarget.User);

        // Assert
        Assert.NotNull(variables);
    }

    [Fact]
    public void GetEnvironmentVariables_SystemTarget_ReturnsVariables()
    {
        // Act
        var variables = _service.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);

        // Assert
        Assert.NotNull(variables);
    }

    [Fact]
    public void SetEnvironmentVariable_UserTarget_SetsVariable()
    {
        // Arrange
        var testName = "TEST_VAR_" + Guid.NewGuid().ToString("N")[..8];
        var testValue = "TestValue";

        try
        {
            // Act
            _service.SetEnvironmentVariable(testName, testValue, EnvironmentVariableTarget.User);

            // Assert
            var variables = _service.GetEnvironmentVariables(EnvironmentVariableTarget.User);
            var variable = variables.FirstOrDefault(v => v.Name == testName);
            Assert.NotNull(variable);
            Assert.Equal(testValue, variable.Value);
        }
        finally
        {
            // Cleanup
            _service.DeleteEnvironmentVariable(testName, EnvironmentVariableTarget.User);
        }
    }

    [Fact]
    public void DeleteEnvironmentVariable_UserTarget_RemovesVariable()
    {
        // Arrange
        var testName = "TEST_VAR_" + Guid.NewGuid().ToString("N")[..8];
        var testValue = "TestValue";
        _service.SetEnvironmentVariable(testName, testValue, EnvironmentVariableTarget.User);

        // Act
        _service.DeleteEnvironmentVariable(testName, EnvironmentVariableTarget.User);

        // Assert
        var variables = _service.GetEnvironmentVariables(EnvironmentVariableTarget.User);
        var variable = variables.FirstOrDefault(v => v.Name == testName);
        Assert.Null(variable);
    }

    [Fact]
    public void IsElevated_ReturnsBoolean()
    {
        // Act
        var result = _service.IsElevated();

        // Assert
        Assert.IsType<bool>(result);
    }
}
