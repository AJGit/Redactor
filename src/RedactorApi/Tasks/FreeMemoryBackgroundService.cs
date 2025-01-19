namespace RedactorApi.Tasks;

public class FreeMemoryBackgroundService : BackgroundService
{
    // This service will run every 60 seconds and force a garbage collection
    // Not to free up memory, but a belt and braces approach to try and minimize
    // the amount of time PII data is in memory.
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            FreeMemory();
            await Task.Delay(60_000, stoppingToken);
        }
    }

    private static void FreeMemory()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
    }
}
