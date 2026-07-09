// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests result primitives.
/// </summary>
public class ResultTests
{
    // ● public

    /// <summary>
    /// Verifies that success results expose success state.
    /// </summary>
    [Fact]
    public void SuccessCreatesSuccessfulResult()
    {
        Result Result = Result.Success();

        Assert.True(Result.Succeeded);
        Assert.False(Result.Failed);
        Assert.Equal(string.Empty, Result.ErrorText);
    }

    /// <summary>
    /// Verifies that failure results expose failure state.
    /// </summary>
    [Fact]
    public void FailureCreatesFailedResult()
    {
        Result Result = Result.Failure("error");

        Assert.False(Result.Succeeded);
        Assert.True(Result.Failed);
        Assert.Equal("error", Result.ErrorText);
    }

    /// <summary>
    /// Verifies that generic success results expose their value.
    /// </summary>
    [Fact]
    public void GenericSuccessStoresValue()
    {
        Result<string> Result = Result<string>.Success("value");

        Assert.True(Result.Succeeded);
        Assert.Equal("value", Result.Value);
    }
}
