using Heimevernet.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace Heimevernet.Infrastructure.Outbox;

/// <summary>Background service that delivers outbox messages. Runs in the web application only.</summary>
public sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly EmailOptions _email;
    private readonly ILogger<OutboxWorker> _logger;

    /// <summary>
    /// Creates the worker.
    /// </summary>
    /// <param name="scopes">Creates a new DI scope for each pass and each message.</param>
    /// <param name="email">Email settings; the worker only runs when email is enabled.</param>
    /// <param name="logger">Logger.</param>
    public OutboxWorker(IServiceScopeFactory scopes, IOptions<EmailOptions> email, ILogger<OutboxWorker> logger)
    {
        _scopes = scopes;
        _email = email.Value;
        _logger = logger;
    }

    /// <summary>
    /// Polls for due messages every <see cref="OutboxPolicy.PollInterval"/> until the app stops.
    /// Returns at once when email is disabled.
    /// </summary>
    /// <param name="stoppingToken">Cancelled when the app shuts down.</param>
    /// <returns>A task that runs for the life of the app.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_email.Enabled)
        {
            _logger.LogInformation("Outbox worker is not started because email is disabled.");
            return;
        }

        try
        {
            using var timer = new PeriodicTimer(OutboxPolicy.PollInterval);

            do
            {
                await RunOncePassAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // the application is shutting down
        }
    }

    private async Task RunOncePassAsync(CancellationToken ct)
    {
        try
        {
            IReadOnlyList<long> due;
            using (var scope = _scopes.CreateScope())
            {
                due = await scope.ServiceProvider.GetRequiredService<OutboxProcessor>()
                    .GetDueIdsAsync(OutboxPolicy.BatchSize, ct);
            }

            foreach (var id in due)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // One scope per message: a failed DbContext cannot affect the next message
                    using var scope = _scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcessOneAsync(id, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // For example the database was unreachable. The lease expires and the message is retried.
                    _logger.LogError(ex, "Processing outbox message {MessageId} failed unexpectedly", id);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Outbox pass failed");
        }
    }
}