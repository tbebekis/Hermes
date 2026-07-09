// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests conflict detection stubs.
/// </summary>
public class ConflictResolverTests
{
    // ● public

    /// <summary>
    /// Verifies that conflicts are detected when both sides changed.
    /// </summary>
    [Fact]
    public void HasConflictReturnsTrueWhenLocalAndRemoteChanged()
    {
        ConflictResolver Resolver = new();

        Assert.True(Resolver.HasConflict(true, true));
    }

    /// <summary>
    /// Verifies that conflicts are not detected when only one side changed.
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void HasConflictReturnsFalseWhenBothSidesDidNotChange(bool LocalChanged, bool RemoteChanged)
    {
        ConflictResolver Resolver = new();

        Assert.False(Resolver.HasConflict(LocalChanged, RemoteChanged));
    }
}
