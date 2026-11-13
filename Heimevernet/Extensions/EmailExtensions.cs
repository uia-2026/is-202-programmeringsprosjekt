using Heimevernet.Infrastructure.Email;
using Heimevernet.Infrastructure.Notifications;
using Heimevernet.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Heimevernet.Extensions;

/// <summary>
/// Startup registration for email, notifications and the outbox.
/// </summary>
public static class EmailExtensions
{
    /// <summary>
    /// Registers email settings, the email sender, notification settings, the outbox handlers,
    /// the outbox writer and the background <see cref="OutboxWorker"/>. Settings are validated at startup.
    /// When email is disabled, <see cref="LoggingEmailSender"/> is used instead of <see cref="SmtpEmailSender"/>.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/>, so calls can be chained.</returns>
    public static IServiceCollection AddEmail(this IServiceCollection services)
    {
        // Validated at start-up, but only when email is switched on
        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .Validate(o => !o.Enabled ||
                           (!string.IsNullOrWhiteSpace(o.Host) && o.Port > 0 && !string.IsNullOrWhiteSpace(o.FromAddress)),
                "Email is enabled but Host, Port or FromAddress is missing.")
            .ValidateOnStart();

        // One sender for the whole application, chosen once from the setting
        services.AddSingleton<IEmailSender>(sp =>
            sp.GetRequiredService<IOptions<EmailOptions>>().Value.Enabled
                ? ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp)
                : ActivatorUtilities.CreateInstance<LoggingEmailSender>(sp));

        services.AddOptions<NotificationOptions>()
            .BindConfiguration(NotificationOptions.SectionName)
            .Validate(o => o.CriticalNeedRadiusKm > 0
                        && o.ResourceNotificationRadiusKm > 0
                        && o.MaxRecipientsPerEvent > 0
                        && o.MaxEmailsPerUserPerDay > 0,
                "Notification radii and limits must be greater than zero.")
            .ValidateOnStart();

        services.AddScoped<IOutboxHandler, CriticalNeedRegisteredHandler>();
        services.AddScoped<IOutboxHandler, NeedsResourcesEmailHandler>();
        services.AddScoped<IOutboxHandler, ResourceRegisteredHandler>();
        services.AddScoped<IOutboxHandler, SuitableResourceEmailHandler>();

        services.AddScoped<IOutbox, OutboxWriter>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxWorker>();

        return services;
    }
}