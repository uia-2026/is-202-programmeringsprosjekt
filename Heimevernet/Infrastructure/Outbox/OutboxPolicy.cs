namespace Heimevernet.Infrastructure.Outbox;

/// <summary>Timing rules for delivery attempts. Kept in one place so tests and the worker agree.</summary>
public static class OutboxPolicy
{
    /// <summary>How long a worker may hold a message before another worker is allowed to take it over.</summary>
    public static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30)
    ];

    /// <summary>The first attempt plus one attempt per retry delay.</summary>
    public static readonly int MaxAttempts = RetryDelays.Length + 1;

    /// <summary>
    /// Tells if a message has used all its attempts.
    /// </summary>
    /// <param name="attempts">Number of attempts made so far.</param>
    /// <returns><see langword="true"/> when no more attempts should be made.</returns>
    public static bool IsExhausted(int attempts) => attempts >= MaxAttempts;

    /// <summary>Delay before the next attempt, given how many attempts have been made so far (1-based).</summary>
    /// <param name="attempts">Number of attempts made so far, starting at 1.</param>
    /// <returns>The delay: 1, 5 or 30 minutes. Values outside the range use the nearest delay.</returns>
    public static TimeSpan DelayAfter(int attempts) =>
        RetryDelays[Math.Clamp(attempts - 1, 0, RetryDelays.Length - 1)];

    /// <summary>How often the worker looks for due messages.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    /// <summary>The most messages one pass handles.</summary>
    public const int BatchSize = 20;
}