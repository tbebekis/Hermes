// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Tests;

/// <summary>
/// Tests synchronization execution intent creation.
/// </summary>
public class SyncExecutionIntentFactoryTests
{
    // ● private

    static SyncPlanDecision Decision(SyncPlanDecisionKind DecisionKind) => new("item-1", SyncDiffKind.LocalChanged, DecisionKind);
    static SyncRootRecord SyncRoot() => new()
    {
        Id = "root-1",
        ProviderName = "Fake",
        ConnectionId = "account-1",
        LocalRootPath = "/local",
        RemoteRootItemId = "remote-root",
        IsEnabled = true,
        CreatedTime = new DateTime(2026, 7, 11, 9, 0, 0, DateTimeKind.Utc),
    };
    static TrackedItemRecord TrackedItem() => new()
    {
        Id = "item-1",
        SyncRootId = "root-1",
        RemoteItemId = "remote-1",
        LocalKey = "File.txt",
        ItemType = "File",
    };
    static BaseSnapshotRecord BaseSnapshot() => new()
    {
        TrackedItemId = "item-1",
        ExistsFlag = true,
        ItemType = "File",
        Name = "File.txt",
        LocalRelativePath = "File.txt",
        RemoteParentId = "remote-root",
        Size = 42,
        ContentHash = "hash-base",
        ProviderVersion = 1,
        Trashed = false,
        CommittedTime = new DateTime(2026, 7, 11, 9, 0, 0, DateTimeKind.Utc),
    };
    static LocalObservedSnapshotRecord LocalObservation(bool ExistsFlag = true) => new()
    {
        TrackedItemId = "item-1",
        ExistsFlag = ExistsFlag,
        RelativePath = "File.txt",
        Name = "File.txt",
        ItemType = "File",
        Size = 42,
        ContentHash = "hash-local",
        ObservedTime = new DateTime(2026, 7, 11, 9, 0, 0, DateTimeKind.Utc),
    };
    static RemoteObservedSnapshotRecord RemoteObservation(bool ExistsFlag = true, bool Removed = false) => new()
    {
        TrackedItemId = "item-1",
        RemoteItemId = "remote-1",
        ExistsFlag = ExistsFlag,
        Removed = Removed,
        Name = "File.txt",
        RemoteParentId = "remote-root",
        ItemType = "File",
        Size = 42,
        ContentHash = "hash-remote",
        ProviderVersion = 2,
        Trashed = false,
        ObservedTime = new DateTime(2026, 7, 11, 9, 0, 0, DateTimeKind.Utc),
    };
    static SyncExecutionRequest Request(SyncPlanDecisionKind DecisionKind) => new()
    {
        Decision = Decision(DecisionKind),
        SyncRoot = SyncRoot(),
        TrackedItem = TrackedItem(),
        BaseSnapshot = BaseSnapshot(),
        LocalObservation = LocalObservation(),
        RemoteObservation = RemoteObservation(),
    };

    // ● public

    /// <summary>
    /// Verifies upload decisions create executable upload intents.
    /// </summary>
    [Fact]
    public void CreateMapsUploadToExecutableIntent()
    {
        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request(SyncPlanDecisionKind.UploadToRemote));

        Assert.Equal(SyncExecutionIntentKind.UploadToRemote, Intent.IntentKind);
        Assert.Equal("item-1", Intent.TrackedItemId);
        Assert.Equal("remote-1", Intent.RemoteItemId);
        Assert.Equal("File.txt", Intent.LocalRelativePath);
        Assert.Equal("File", Intent.ItemType);
        Assert.Equal("File.txt", Intent.Name);
        Assert.Equal("remote-root", Intent.RemoteParentId);
        Assert.Equal("hash-local", Intent.ContentHash);
        Assert.Equal(42, Intent.Size);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies download decisions create executable download intents.
    /// </summary>
    [Fact]
    public void CreateMapsDownloadToExecutableIntent()
    {
        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request(SyncPlanDecisionKind.DownloadToLocal));

        Assert.Equal(SyncExecutionIntentKind.DownloadToLocal, Intent.IntentKind);
        Assert.Equal("hash-remote", Intent.ContentHash);
        Assert.Equal(42, Intent.Size);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies remote namespace decisions create executable local namespace intents.
    /// </summary>
    [Fact]
    public void CreateMapsRemoteNamespaceToExecutableIntent()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.ApplyRemoteNamespaceToLocal);
        ExecutionRequest.RemoteObservation.Name = "Renamed.txt";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.ApplyRemoteNamespaceToLocal, Intent.IntentKind);
        Assert.Equal("File.txt", Intent.SourceLocalRelativePath);
        Assert.Equal("Renamed.txt", Intent.LocalRelativePath);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies local namespace decisions create executable remote namespace intents.
    /// </summary>
    [Fact]
    public void CreateMapsLocalNamespaceToExecutableIntent()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.LocalObservation.Name = "Renamed.txt";
        ExecutionRequest.LocalObservation.RelativePath = "Renamed.txt";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.ApplyLocalNamespaceToRemote, Intent.IntentKind);
        Assert.Equal("remote-1", Intent.RemoteItemId);
        Assert.Equal("Renamed.txt", Intent.LocalRelativePath);
        Assert.Equal("Renamed.txt", Intent.Name);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies local folder rename decisions create executable remote namespace intents.
    /// </summary>
    [Fact]
    public void CreateMapsLocalFolderRenameToExecutableIntent()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.TrackedItem.ItemType = "Folder";
        ExecutionRequest.BaseSnapshot.ItemType = "Folder";
        ExecutionRequest.BaseSnapshot.Name = "Folder";
        ExecutionRequest.BaseSnapshot.LocalRelativePath = "Folder";
        ExecutionRequest.LocalObservation.ItemType = "Folder";
        ExecutionRequest.LocalObservation.Name = "RenamedFolder";
        ExecutionRequest.LocalObservation.RelativePath = "RenamedFolder";
        ExecutionRequest.RemoteObservation.ItemType = "Folder";
        ExecutionRequest.RemoteObservation.Name = "Folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.ApplyLocalNamespaceToRemote, Intent.IntentKind);
        Assert.Equal("Folder", Intent.SourceName);
        Assert.Equal("RenamedFolder", Intent.Name);
        Assert.Equal("remote-root", Intent.SourceRemoteParentId);
        Assert.Equal("remote-root", Intent.RemoteParentId);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies local folder move decisions create executable remote namespace intents.
    /// </summary>
    [Fact]
    public void CreateMapsLocalFolderMoveToExecutableIntent()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.TrackedItem.ItemType = "Folder";
        ExecutionRequest.BaseSnapshot.ItemType = "Folder";
        ExecutionRequest.BaseSnapshot.Name = "Folder";
        ExecutionRequest.BaseSnapshot.LocalRelativePath = "Folder";
        ExecutionRequest.LocalObservation.ItemType = "Folder";
        ExecutionRequest.LocalObservation.Name = "Folder";
        ExecutionRequest.LocalObservation.RelativePath = "Parent/Folder";
        ExecutionRequest.LocalObservation.ParentRelativePath = "Parent";
        ExecutionRequest.LocalParentRemoteItemId = "remote-parent";
        ExecutionRequest.RemoteObservation.ItemType = "Folder";
        ExecutionRequest.RemoteObservation.Name = "Folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.ApplyLocalNamespaceToRemote, Intent.IntentKind);
        Assert.Equal("Folder", Intent.SourceName);
        Assert.Equal("Folder", Intent.Name);
        Assert.Equal("remote-root", Intent.SourceRemoteParentId);
        Assert.Equal("remote-parent", Intent.RemoteParentId);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies combined local folder rename and move decisions create executable remote namespace intents.
    /// </summary>
    [Fact]
    public void CreateMapsCombinedLocalFolderRenameAndMoveToExecutableIntent()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.TrackedItem.ItemType = "Folder";
        ExecutionRequest.BaseSnapshot.ItemType = "Folder";
        ExecutionRequest.BaseSnapshot.Name = "Folder";
        ExecutionRequest.BaseSnapshot.LocalRelativePath = "Folder";
        ExecutionRequest.LocalObservation.ItemType = "Folder";
        ExecutionRequest.LocalObservation.Name = "RenamedFolder";
        ExecutionRequest.LocalObservation.RelativePath = "Parent/RenamedFolder";
        ExecutionRequest.LocalObservation.ParentRelativePath = "Parent";
        ExecutionRequest.LocalParentRemoteItemId = "remote-parent";
        ExecutionRequest.RemoteObservation.ItemType = "Folder";
        ExecutionRequest.RemoteObservation.Name = "Folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.ApplyLocalNamespaceToRemote, Intent.IntentKind);
        Assert.Equal("Folder", Intent.SourceName);
        Assert.Equal("RenamedFolder", Intent.Name);
        Assert.Equal("remote-root", Intent.SourceRemoteParentId);
        Assert.Equal("remote-parent", Intent.RemoteParentId);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies local move decisions create executable remote namespace intents.
    /// </summary>
    [Fact]
    public void CreateMapsLocalMoveToExecutableIntent()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.LocalObservation.Name = "File.txt";
        ExecutionRequest.LocalObservation.RelativePath = "Folder/File.txt";
        ExecutionRequest.LocalObservation.ParentRelativePath = "Folder";
        ExecutionRequest.LocalParentRemoteItemId = "remote-folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.ApplyLocalNamespaceToRemote, Intent.IntentKind);
        Assert.Equal("remote-root", Intent.SourceRemoteParentId);
        Assert.Equal("remote-folder", Intent.RemoteParentId);
        Assert.Equal("File.txt", Intent.Name);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies combined local rename and move decisions create executable remote namespace intents.
    /// </summary>
    [Fact]
    public void CreateMapsCombinedLocalRenameAndMoveToExecutableIntent()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.LocalObservation.Name = "Renamed.txt";
        ExecutionRequest.LocalObservation.RelativePath = "Folder/Renamed.txt";
        ExecutionRequest.LocalObservation.ParentRelativePath = "Folder";
        ExecutionRequest.LocalParentRemoteItemId = "remote-folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.ApplyLocalNamespaceToRemote, Intent.IntentKind);
        Assert.Equal("File.txt", Intent.SourceName);
        Assert.Equal("Renamed.txt", Intent.Name);
        Assert.Equal("remote-root", Intent.SourceRemoteParentId);
        Assert.Equal("remote-folder", Intent.RemoteParentId);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies local moves are blocked when the target remote parent cannot be resolved.
    /// </summary>
    [Fact]
    public void CreateBlocksLocalMoveWithUnresolvedTargetParent()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.Decision = new SyncPlanDecision("item-1", SyncDiffKind.LocalNamespaceChanged, SyncPlanDecisionKind.ApplyLocalNamespaceToRemote);
        ExecutionRequest.LocalObservation.Name = "File.txt";
        ExecutionRequest.LocalObservation.RelativePath = "Folder/File.txt";
        ExecutionRequest.LocalObservation.ParentRelativePath = "Folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.ApplyLocalNamespaceToRemote, Intent.IntentKind);
        Assert.False(Intent.CanExecute);
        Assert.Contains("Target remote parent id is required.", Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies upload propagation requires a remote item id or remote parent id.
    /// </summary>
    [Fact]
    public void CreateValidatesUploadRemoteTarget()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.UploadToRemote);
        ExecutionRequest.TrackedItem.RemoteItemId = string.Empty;
        ExecutionRequest.BaseSnapshot.RemoteParentId = string.Empty;
        ExecutionRequest.RemoteObservation = null;
        ExecutionRequest.SyncRoot.RemoteRootItemId = string.Empty;

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.UploadToRemote, Intent.IntentKind);
        Assert.False(Intent.CanExecute);
        Assert.Contains("Remote item id or remote parent id is required.", Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies local root uploads can resolve the remote parent from the sync root.
    /// </summary>
    [Fact]
    public void CreateResolvesRootUploadParentFromSyncRoot()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.UploadToRemote);
        ExecutionRequest.TrackedItem.RemoteItemId = string.Empty;
        ExecutionRequest.BaseSnapshot.RemoteParentId = string.Empty;
        ExecutionRequest.RemoteObservation = null;

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.UploadToRemote, Intent.IntentKind);
        Assert.Equal("remote-root", Intent.RemoteParentId);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies nested local uploads can resolve the remote parent from the local parent item.
    /// </summary>
    [Fact]
    public void CreateResolvesNestedUploadParentFromLocalParentRemoteId()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.UploadToRemote);
        ExecutionRequest.TrackedItem.RemoteItemId = string.Empty;
        ExecutionRequest.BaseSnapshot.RemoteParentId = string.Empty;
        ExecutionRequest.RemoteObservation = null;
        ExecutionRequest.LocalParentRemoteItemId = "remote-folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.UploadToRemote, Intent.IntentKind);
        Assert.Equal("remote-folder", Intent.RemoteParentId);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies local delete propagation requires a remote item id.
    /// </summary>
    [Fact]
    public void CreateValidatesLocalDeletePropagation()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.PropagateLocalDelete);
        ExecutionRequest.TrackedItem.RemoteItemId = string.Empty;
        ExecutionRequest.RemoteObservation.RemoteItemId = string.Empty;

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.PropagateLocalDelete, Intent.IntentKind);
        Assert.False(Intent.CanExecute);
        Assert.Contains("Remote item id is required.", Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies download propagation blocks when the remote parent local path is unresolved.
    /// </summary>
    [Fact]
    public void CreateBlocksDownloadWhenRemoteParentLocalPathIsUnresolved()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.DownloadToLocal);
        ExecutionRequest.BaseSnapshot.LocalRelativePath = string.Empty;
        ExecutionRequest.LocalObservation = null;
        ExecutionRequest.RemoteObservation.RemoteParentId = "remote-folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.Blocked, Intent.IntentKind);
        Assert.False(Intent.CanExecute);
        Assert.Contains("Remote parent local path is unresolved.", Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies root remote downloads can resolve the local path from the remote name.
    /// </summary>
    [Fact]
    public void CreateResolvesRootRemoteDownloadPathFromRemoteName()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.DownloadToLocal);
        ExecutionRequest.BaseSnapshot.LocalRelativePath = string.Empty;
        ExecutionRequest.LocalObservation = null;

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.DownloadToLocal, Intent.IntentKind);
        Assert.Equal("File.txt", Intent.LocalRelativePath);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies nested remote downloads can resolve the local path from the parent local path.
    /// </summary>
    [Fact]
    public void CreateResolvesNestedRemoteDownloadPathFromParentLocalPath()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.DownloadToLocal);
        ExecutionRequest.BaseSnapshot.LocalRelativePath = string.Empty;
        ExecutionRequest.LocalObservation = null;
        ExecutionRequest.RemoteObservation.RemoteParentId = "remote-folder";
        ExecutionRequest.RemoteParentLocalRelativePath = "Folder";

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.DownloadToLocal, Intent.IntentKind);
        Assert.Equal("Folder/File.txt", Intent.LocalRelativePath);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies remote delete propagation can use the base local path.
    /// </summary>
    [Fact]
    public void CreateValidatesRemoteDeletePropagationWithBasePath()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.PropagateRemoteDelete);
        ExecutionRequest.LocalObservation = LocalObservation(false);
        ExecutionRequest.LocalObservation.RelativePath = string.Empty;

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal(SyncExecutionIntentKind.PropagateRemoteDelete, Intent.IntentKind);
        Assert.Equal("File.txt", Intent.LocalRelativePath);
        Assert.True(Intent.CanExecute);
        Assert.Empty(Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies conflict decisions are not normal executable intents.
    /// </summary>
    [Fact]
    public void CreateMapsConflictToResolutionIntent()
    {
        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request(SyncPlanDecisionKind.Conflict));

        Assert.Equal(SyncExecutionIntentKind.ResolveConflict, Intent.IntentKind);
        Assert.False(Intent.CanExecute);
        Assert.Contains("Conflict resolution is required.", Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies blocked decisions are not normal executable intents.
    /// </summary>
    [Fact]
    public void CreateMapsBlockedToBlockedIntent()
    {
        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request(SyncPlanDecisionKind.Blocked));

        Assert.Equal(SyncExecutionIntentKind.Blocked, Intent.IntentKind);
        Assert.False(Intent.CanExecute);
        Assert.Contains("Request is blocked.", Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies metadata-only decisions cannot be executed by the executor.
    /// </summary>
    [Fact]
    public void CreateMarksCommitBaseAsInvalid()
    {
        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(Request(SyncPlanDecisionKind.CommitBase));

        Assert.Equal(SyncExecutionIntentKind.Invalid, Intent.IntentKind);
        Assert.False(Intent.CanExecute);
        Assert.Contains("Decision kind cannot be executed.", Intent.ValidationMessages);
    }
    /// <summary>
    /// Verifies resolved intent fields fall back to available metadata.
    /// </summary>
    [Fact]
    public void CreateResolvesFieldsFromFallbackMetadata()
    {
        SyncExecutionRequest ExecutionRequest = Request(SyncPlanDecisionKind.DownloadToLocal);
        ExecutionRequest.TrackedItem = null;
        ExecutionRequest.LocalObservation = null;

        SyncExecutionIntent Intent = SyncExecutionIntentFactory.Create(ExecutionRequest);

        Assert.Equal("item-1", Intent.TrackedItemId);
        Assert.Equal("remote-1", Intent.RemoteItemId);
        Assert.Equal("File.txt", Intent.LocalRelativePath);
        Assert.Equal("File", Intent.ItemType);
        Assert.Equal("File.txt", Intent.Name);
        Assert.Equal("remote-root", Intent.RemoteParentId);
        Assert.Equal("hash-remote", Intent.ContentHash);
        Assert.Equal(42, Intent.Size);
    }
}
