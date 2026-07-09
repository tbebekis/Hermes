// Copyright (c) 2026 Theodoros Bebekis
// Licensed under the MIT License.

namespace Hermes.Core;

/// <summary>
/// Coordinates synchronization planning and execution.
/// </summary>
public class SyncEngine
{
    // ● private

    private readonly IStorageProvider fStorageProvider;
    private readonly SyncPlanner fSyncPlanner;
    private readonly OperationQueue fOperationQueue;
    private readonly MetadataStore fMetadataStore;
    private readonly ILogger<SyncEngine> fLogger;

    // ● constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncEngine"/> class.
    /// </summary>
    public SyncEngine(IStorageProvider StorageProvider, SyncPlanner SyncPlanner, OperationQueue OperationQueue, MetadataStore MetadataStore, ILogger<SyncEngine> Logger)
    {
        fStorageProvider = Guard.NotNull(StorageProvider, nameof(StorageProvider));
        fSyncPlanner = Guard.NotNull(SyncPlanner, nameof(SyncPlanner));
        fOperationQueue = Guard.NotNull(OperationQueue, nameof(OperationQueue));
        fMetadataStore = Guard.NotNull(MetadataStore, nameof(MetadataStore));
        fLogger = Guard.NotNull(Logger, nameof(Logger));
    }

    // ● public

    /// <summary>
    /// Runs one synchronization pass.
    /// </summary>
    public async Task<Result> RunOnceAsync(CancellationToken CancellationToken)
    {
        fLogger.LogInformation("Starting sync pass with provider {ProviderName}.", fStorageProvider.Name);

        string PageToken = await fMetadataStore.LoadDriveStartPageTokenAsync(CancellationToken);
        if (string.IsNullOrWhiteSpace(PageToken))
        {
            Result<string> TokenResult = await fStorageProvider.GetStartPageTokenAsync(CancellationToken);
            if (TokenResult.Failed)
                return Result.Failure(TokenResult.ErrorText);

            await fMetadataStore.SaveDriveStartPageTokenAsync(TokenResult.Value, CancellationToken);
        }

        foreach (SyncOperation Operation in fSyncPlanner.CreateInitialPlan())
            fOperationQueue.Enqueue(Operation);

        return Result.Success();
    }
}
