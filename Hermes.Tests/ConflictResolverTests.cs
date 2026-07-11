// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests conflict detection stubs.
/// </summary>
public class ConflictResolverTests
{
    // ● private

    static SyncItemState State(string Hash) => new()
    {
        Exists = true,
        ItemType = "File",
        Name = "File1.txt",
        LocalRelativePath = "File1.txt",
        RemoteParentId = "remote-root",
        ContentHash = Hash,
        Size = 42,
    };

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
    /// <summary>
    /// Verifies diff input conflicts are detected through the sync diff classifier.
    /// </summary>
    [Fact]
    public void HasConflictReturnsTrueWhenDiffInputClassifiesAsConflict()
    {
        ConflictResolver Resolver = new();

        bool Result = Resolver.HasConflict(new SyncDiffInput()
        {
            BaseState = State("hash-base"),
            LocalState = State("hash-local"),
            RemoteState = State("hash-remote"),
        });

        Assert.True(Result);
    }
    /// <summary>
    /// Verifies compatible diff input is not reported as a conflict.
    /// </summary>
    [Fact]
    public void HasConflictReturnsFalseWhenDiffInputClassifiesAsCompatible()
    {
        ConflictResolver Resolver = new();

        bool Result = Resolver.HasConflict(new SyncDiffInput()
        {
            BaseState = State("hash-base"),
            LocalState = State("hash-changed"),
            RemoteState = State("hash-changed"),
        });

        Assert.False(Result);
    }
}
