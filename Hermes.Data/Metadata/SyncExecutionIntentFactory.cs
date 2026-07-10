// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Data;

/// <summary>
/// Creates executor-facing intents from synchronization execution requests.
/// </summary>
static public class SyncExecutionIntentFactory
{
    // ● private

    static bool Exists(LocalObservedSnapshotRecord Observation) => Observation != null && Observation.ExistsFlag;
    static bool Exists(RemoteObservedSnapshotRecord Observation)
    {
        return Observation != null
            && Observation.ExistsFlag
            && !Observation.Removed;
    }
    static bool HasText(string Text) => !string.IsNullOrWhiteSpace(Text);
    static string RemoteItemId(SyncExecutionRequest Request)
    {
        if (HasText(Request.TrackedItem?.RemoteItemId))
            return Request.TrackedItem.RemoteItemId;

        if (HasText(Request.RemoteObservation?.RemoteItemId))
            return Request.RemoteObservation.RemoteItemId;

        return string.Empty;
    }
    static string LocalPath(SyncExecutionRequest Request)
    {
        if (HasText(Request.LocalObservation?.RelativePath))
            return Request.LocalObservation.RelativePath;

        if (HasText(Request.BaseSnapshot?.LocalRelativePath))
            return Request.BaseSnapshot.LocalRelativePath;

        if (HasText(Request.RemoteObservation?.Name)
            && HasText(Request.RemoteObservation.RemoteParentId)
            && string.Equals(Request.RemoteObservation.RemoteParentId, Request.SyncRoot?.RemoteRootItemId, StringComparison.Ordinal))
            return Request.RemoteObservation.Name;

        if (HasText(Request.RemoteObservation?.Name) && HasText(Request.RemoteParentLocalRelativePath))
            return Request.RemoteParentLocalRelativePath + "/" + Request.RemoteObservation.Name;

        return string.Empty;
    }
    static string RemoteProjectedLocalPath(SyncExecutionRequest Request)
    {
        if (HasText(Request.RemoteObservation?.Name)
            && HasText(Request.RemoteObservation.RemoteParentId)
            && string.Equals(Request.RemoteObservation.RemoteParentId, Request.SyncRoot?.RemoteRootItemId, StringComparison.Ordinal))
            return Request.RemoteObservation.Name;

        if (HasText(Request.RemoteObservation?.Name) && HasText(Request.RemoteParentLocalRelativePath))
            return Request.RemoteParentLocalRelativePath + "/" + Request.RemoteObservation.Name;

        return string.Empty;
    }
    static string SourceLocalPath(SyncExecutionRequest Request)
    {
        if (HasText(Request.LocalObservation?.RelativePath))
            return Request.LocalObservation.RelativePath;

        if (HasText(Request.BaseSnapshot?.LocalRelativePath))
            return Request.BaseSnapshot.LocalRelativePath;

        return string.Empty;
    }
    static string ParentLocalPath(string LocalRelativePath)
    {
        if (!HasText(LocalRelativePath))
            return string.Empty;

        int Index = LocalRelativePath.LastIndexOf('/');
        return Index < 0 ? string.Empty : LocalRelativePath[..Index];
    }
    static bool SameLocalParent(SyncExecutionRequest Request)
    {
        return string.Equals(
            ParentLocalPath(Request.BaseSnapshot?.LocalRelativePath),
            ParentLocalPath(Request.LocalObservation?.RelativePath),
            StringComparison.Ordinal);
    }
    static bool IsFolder(SyncExecutionRequest Request)
    {
        return string.Equals(ItemType(Request), "Folder", StringComparison.OrdinalIgnoreCase);
    }
    static string TrackedItemId(SyncExecutionRequest Request)
    {
        if (HasText(Request.TrackedItem?.Id))
            return Request.TrackedItem.Id;

        if (HasText(Request.Decision?.TrackedItemId))
            return Request.Decision.TrackedItemId;

        return string.Empty;
    }
    static string ItemType(SyncExecutionRequest Request)
    {
        if (HasText(Request.TrackedItem?.ItemType))
            return Request.TrackedItem.ItemType;

        if (HasText(Request.LocalObservation?.ItemType))
            return Request.LocalObservation.ItemType;

        if (HasText(Request.RemoteObservation?.ItemType))
            return Request.RemoteObservation.ItemType;

        if (HasText(Request.BaseSnapshot?.ItemType))
            return Request.BaseSnapshot.ItemType;

        return string.Empty;
    }
    static string Name(SyncExecutionRequest Request, SyncExecutionIntentKind IntentKind)
    {
        if (IntentKind == SyncExecutionIntentKind.UploadToRemote && HasText(Request.LocalObservation?.Name))
            return Request.LocalObservation.Name;

        if (IntentKind == SyncExecutionIntentKind.ApplyLocalNamespaceToRemote && HasText(Request.LocalObservation?.Name))
            return Request.LocalObservation.Name;

        if (HasText(Request.RemoteObservation?.Name))
            return Request.RemoteObservation.Name;

        if (HasText(Request.LocalObservation?.Name))
            return Request.LocalObservation.Name;

        if (HasText(Request.BaseSnapshot?.Name))
            return Request.BaseSnapshot.Name;

        return string.Empty;
    }
    static string RemoteParentId(SyncExecutionRequest Request)
    {
        if (HasText(Request.RemoteObservation?.RemoteParentId))
            return Request.RemoteObservation.RemoteParentId;

        if (HasText(Request.BaseSnapshot?.RemoteParentId))
            return Request.BaseSnapshot.RemoteParentId;

        if (HasText(Request.LocalParentRemoteItemId))
            return Request.LocalParentRemoteItemId;

        if (HasText(Request.SyncRoot?.RemoteRootItemId))
            return Request.SyncRoot.RemoteRootItemId;

        return string.Empty;
    }
    static string ContentHash(SyncExecutionRequest Request, SyncExecutionIntentKind IntentKind)
    {
        if (IntentKind == SyncExecutionIntentKind.UploadToRemote && HasText(Request.LocalObservation?.ContentHash))
            return Request.LocalObservation.ContentHash;

        if (IntentKind == SyncExecutionIntentKind.DownloadToLocal && HasText(Request.RemoteObservation?.ContentHash))
            return Request.RemoteObservation.ContentHash;

        if (HasText(Request.RemoteObservation?.ContentHash))
            return Request.RemoteObservation.ContentHash;

        if (HasText(Request.LocalObservation?.ContentHash))
            return Request.LocalObservation.ContentHash;

        if (HasText(Request.BaseSnapshot?.ContentHash))
            return Request.BaseSnapshot.ContentHash;

        return string.Empty;
    }
    static long? Size(SyncExecutionRequest Request, SyncExecutionIntentKind IntentKind)
    {
        if (IntentKind == SyncExecutionIntentKind.UploadToRemote && Request.LocalObservation?.Size != null)
            return Request.LocalObservation.Size;

        if (IntentKind == SyncExecutionIntentKind.DownloadToLocal && Request.RemoteObservation?.Size != null)
            return Request.RemoteObservation.Size;

        if (Request.RemoteObservation?.Size != null)
            return Request.RemoteObservation.Size;

        if (Request.LocalObservation?.Size != null)
            return Request.LocalObservation.Size;

        return Request.BaseSnapshot?.Size;
    }
    static SyncExecutionIntentKind IntentKind(SyncPlanDecisionKind DecisionKind)
    {
        return DecisionKind switch
        {
            SyncPlanDecisionKind.UploadToRemote => SyncExecutionIntentKind.UploadToRemote,
            SyncPlanDecisionKind.DownloadToLocal => SyncExecutionIntentKind.DownloadToLocal,
            SyncPlanDecisionKind.ApplyRemoteNamespaceToLocal => SyncExecutionIntentKind.ApplyRemoteNamespaceToLocal,
            SyncPlanDecisionKind.ApplyLocalNamespaceToRemote => SyncExecutionIntentKind.ApplyLocalNamespaceToRemote,
            SyncPlanDecisionKind.PropagateLocalDelete => SyncExecutionIntentKind.PropagateLocalDelete,
            SyncPlanDecisionKind.PropagateRemoteDelete => SyncExecutionIntentKind.PropagateRemoteDelete,
            SyncPlanDecisionKind.Conflict => SyncExecutionIntentKind.ResolveConflict,
            SyncPlanDecisionKind.Blocked => SyncExecutionIntentKind.Blocked,
            _ => SyncExecutionIntentKind.Invalid,
        };
    }
    static bool IsRemoteParentLocalPathUnresolved(SyncExecutionRequest Request, string ResolvedLocalPath)
    {
        return !HasText(ResolvedLocalPath)
            && HasText(Request.RemoteObservation?.Name)
            && HasText(Request.RemoteObservation.RemoteParentId)
            && !string.Equals(Request.RemoteObservation.RemoteParentId, Request.SyncRoot?.RemoteRootItemId, StringComparison.Ordinal)
            && !HasText(Request.RemoteParentLocalRelativePath);
    }
    static void Require(bool Condition, SyncExecutionIntent Intent, string Message)
    {
        if (!Condition)
            Intent.ValidationMessages.Add(Message);
    }
    static void ValidateExecutableIntent(SyncExecutionIntent Intent)
    {
        SyncExecutionRequest Request = Intent.Request;

        Require(Request.TrackedItem != null, Intent, "Tracked item is required.");

        switch (Intent.IntentKind)
        {
            case SyncExecutionIntentKind.UploadToRemote:
                Require(Exists(Request.LocalObservation), Intent, "Existing local observation is required.");
                Require(HasText(LocalPath(Request)), Intent, "Local path is required.");
                Require(HasText(RemoteItemId(Request)) || HasText(RemoteParentId(Request)), Intent, "Remote item id or remote parent id is required.");
                break;
            case SyncExecutionIntentKind.DownloadToLocal:
                Require(Exists(Request.RemoteObservation), Intent, "Existing remote observation is required.");
                Require(HasText(LocalPath(Request)), Intent, "Local path is required.");
                break;
            case SyncExecutionIntentKind.ApplyRemoteNamespaceToLocal:
                Require(Exists(Request.LocalObservation), Intent, "Existing local observation is required.");
                Require(Exists(Request.RemoteObservation), Intent, "Existing remote observation is required.");
                Require(HasText(Intent.SourceLocalRelativePath), Intent, "Source local path is required.");
                Require(HasText(Intent.LocalRelativePath), Intent, "Target local path is required.");
                break;
            case SyncExecutionIntentKind.ApplyLocalNamespaceToRemote:
                Require(Exists(Request.LocalObservation), Intent, "Existing local observation is required.");
                Require(Exists(Request.RemoteObservation), Intent, "Existing remote observation is required.");
                Require(HasText(Intent.RemoteItemId), Intent, "Remote item id is required.");
                Require(HasText(Intent.Name), Intent, "Remote target name is required.");
                break;
            case SyncExecutionIntentKind.PropagateLocalDelete:
                Require(HasText(RemoteItemId(Request)), Intent, "Remote item id is required.");
                break;
            case SyncExecutionIntentKind.PropagateRemoteDelete:
                Require(HasText(LocalPath(Request)), Intent, "Local path is required.");
                break;
        }
    }

    // ● public

    /// <summary>
    /// Creates an executor-facing intent from a synchronization execution request.
    /// </summary>
    static public SyncExecutionIntent Create(SyncExecutionRequest Request)
    {
        Guard.NotNull(Request, nameof(Request));
        Guard.NotNull(Request.Decision, nameof(Request.Decision));

        SyncExecutionIntentKind ResolvedIntentKind = IntentKind(Request.Decision.DecisionKind);
        string ResolvedLocalPath = ResolvedIntentKind == SyncExecutionIntentKind.ApplyRemoteNamespaceToLocal
            ? RemoteProjectedLocalPath(Request)
            : LocalPath(Request);
        SyncExecutionIntent Result = new()
        {
            Request = Request,
            IntentKind = ResolvedIntentKind,
            TrackedItemId = TrackedItemId(Request),
            RemoteItemId = RemoteItemId(Request),
            SourceLocalRelativePath = SourceLocalPath(Request),
            LocalRelativePath = ResolvedLocalPath,
            ItemType = ItemType(Request),
            Name = Name(Request, ResolvedIntentKind),
            RemoteParentId = RemoteParentId(Request),
            ContentHash = ContentHash(Request, ResolvedIntentKind),
            Size = Size(Request, ResolvedIntentKind),
        };

        if (Result.IntentKind == SyncExecutionIntentKind.Invalid)
            Result.ValidationMessages.Add("Decision kind cannot be executed.");
        else if (Result.IntentKind == SyncExecutionIntentKind.DownloadToLocal && IsRemoteParentLocalPathUnresolved(Request, ResolvedLocalPath))
        {
            Result.IntentKind = SyncExecutionIntentKind.Blocked;
            Result.ValidationMessages.Add("Remote parent local path is unresolved.");
        }
        else if (Result.IntentKind == SyncExecutionIntentKind.ApplyLocalNamespaceToRemote && IsFolder(Request))
        {
            Result.IntentKind = SyncExecutionIntentKind.Blocked;
            Result.ValidationMessages.Add("Local folder namespace changes are not supported yet.");
        }
        else if (Result.IntentKind == SyncExecutionIntentKind.ApplyLocalNamespaceToRemote && !SameLocalParent(Request))
        {
            Result.IntentKind = SyncExecutionIntentKind.Blocked;
            Result.ValidationMessages.Add("Local move propagation is not supported yet.");
        }
        else if (Result.IntentKind == SyncExecutionIntentKind.ResolveConflict)
            Result.ValidationMessages.Add("Conflict resolution is required.");
        else if (Result.IntentKind == SyncExecutionIntentKind.Blocked)
            Result.ValidationMessages.Add("Request is blocked.");
        else
            ValidateExecutableIntent(Result);

        Result.CanExecute = Result.ValidationMessages.Count == 0;

        return Result;
    }
}
