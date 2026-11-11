using Heimevernet.Infrastructure.Email;
using Heimevernet.Infrastructure.Outbox;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Heimevernet.IntegrationTests.Infrastructure;

/// <summary>
/// Test host with email notifications switched on. Emails go to a <see cref="FakeEmailSender"/>, and the
/// background <see cref="OutboxWorker"/> is removed so tests decide when the outbox is processed
/// (see <see cref="DrainOutboxAsync"/>) instead of racing a timer.
/// </summary>
public sealed class NotificationsWebApplicationFactory : HeimevernetWebApplicationFactory
{
    public const string PublicBaseUrl = "https://heimevernet.test";

    public FakeEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Added after the base configuration (and its user secrets), so these values win
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Enabled"] = "true",
                ["Email:Host"] = "smtp.invalid",          // never contacted, the sender is replaced below
                ["Email:Port"] = "25",
                ["Email:UseStartTls"] = "false",
                ["Email:FromAddress"] = "noreply@heimevernet.test",
                ["Notifications:PublicBaseUrl"] = PublicBaseUrl,
                ["Notifications:CriticalNeedRadiusKm"] = "50",
                ["Notifications:ResourceNotificationRadiusKm"] = "50",
                ["Notifications:MaxRecipientsPerEvent"] = "20",
                ["Notifications:MaxEmailsPerUserPerDay"] = "5"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            var worker = services.Single(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(OutboxWorker));
            services.Remove(worker);
        });
    }

    /// <summary>
    /// Does what the worker does, pass after pass, until no message is due. Event messages create email
    /// messages, so one pass is not enough. Messages that fail are scheduled for later and stop being due.
    /// </summary>
    public async Task DrainOutboxAsync(CancellationToken ct = default)
    {
        for (var pass = 0; pass < 10; pass++)
        {
            IReadOnlyList<long> due;
            using (var scope = Services.CreateScope())
            {
                due = await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().GetDueIdsAsync(100, ct);
            }

            if (due.Count == 0)
                return;

            foreach (var id in due)
                await ProcessAsync(id, ct);
        }

        throw new InvalidOperationException("The outbox still had due messages after 10 passes.");
    }

    /// <summary>Processes one message in its own scope, like the worker does.</summary>
    public async Task ProcessAsync(long messageId, CancellationToken ct = default)
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcessOneAsync(messageId, ct);
    }
}
