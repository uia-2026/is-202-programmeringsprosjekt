using System.Globalization;
using System.Text.RegularExpressions;
using Heimevernet.Models;

namespace Heimevernet.Infrastructure.Notifications;

/// <summary>
/// Builds notification emails. Pure functions without side effects, so they are easy to test.
/// Text typed by users is cleaned before it is placed in a mail.
/// </summary>
public static class NotificationEmails
{
    /// <summary>
    /// A finished email, ready to send.
    /// </summary>
    /// <param name="Subject">Subject line.</param>
    /// <param name="Body">Plain text body.</param>
    public sealed record Mail(string Subject, string Body);

    private static readonly Regex Links = new(
        @"(https?://|www\.)\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly Regex Whitespace = new(
        @"\s+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <summary>The email a provider gets when a Critical need is registered near one of their resources.</summary>
    /// <param name="need">The critical need, with its category loaded.</param>
    /// <param name="recipient">The provider who gets the email.</param>
    /// <param name="resourceTitle">Title of the provider's nearest fitting resource.</param>
    /// <param name="distanceKm">Distance from the resource to the need, in kilometres.</param>
    /// <param name="baseUrl">Public base URL of the app, used for the link.</param>
    /// <returns>The finished <see cref="Mail"/>.</returns>
    public static Mail CriticalNeed(
        Need need, User recipient, string resourceTitle, double distanceKm, string baseUrl)
    {
        var title = Clean(need.Title, 80);
        var subject = $"Critical need near you: {title}";

        var deadline = need.Deadline is { } d
            ? d.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
            : "No deadline given";

        var km = distanceKm.ToString("0.#", CultureInfo.InvariantCulture);
        var link = $"{baseUrl.TrimEnd('/')}/Need/Details/{need.Id}";

        var body = $"""
            Hi {Clean(recipient.Username, 50)},

            A critical need has just been registered near you.

              Need:      {title}
              Category:  {Clean(need.Category.Name, 100)}
              Region:    {Clean(need.Region, 100)}
              Units:     {need.QuantityNeeded}
              Deadline:  {deadline}

            Your resource "{Clean(resourceTitle, 80)}" is about {km} km from it and may fit.
            Sign in to see the details and the contact information:

            {link}

            You receive this email because you offer resources in this category and a critical need was registered nearby.
            You can turn these emails off on your account page.
            """;

        return new Mail(subject, body);
    }

    /// <summary>The email a public actor gets when a resource that may fit one of their open needs is registered nearby.</summary>
    /// <param name="resource">The new resource, with its category loaded.</param>
    /// <param name="recipient">The public actor who gets the email.</param>
    /// <param name="needTitle">Title of the recipient's nearest open need.</param>
    /// <param name="distanceKm">Distance from the resource to the need, in kilometres.</param>
    /// <param name="baseUrl">Public base URL of the app, used for the link.</param>
    /// <returns>The finished <see cref="Mail"/>.</returns>
    public static Mail SuitableResource(
        Resource resource, User recipient, string needTitle, double distanceKm, string baseUrl)
    {
        var title = Clean(resource.Title, 80);
        var subject = $"A resource that may fit your need: {title}";

        var availableFrom = resource.AvailableFrom.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        var availableTo = resource.AvailableTo is { } end
            ? end.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
            : "No end date";

        var km = distanceKm.ToString("0.#", CultureInfo.InvariantCulture);
        var link = $"{baseUrl.TrimEnd('/')}/Resource/Details/{resource.Id}";

        var body = $"""
        Hi {Clean(recipient.Username, 50)},

        A new resource has been registered that may help with your need "{Clean(needTitle, 80)}".

          Resource:        {title}
          Category:        {Clean(resource.Category.Name, 100)}
          Region:          {Clean(resource.Region, 100)}
          Units offered:   {resource.QuantityOffered}
          Available from:  {availableFrom}
          Available until: {availableTo}

        It is about {km} km from your need.
        Sign in to see the details and propose a match:

        {link}

        You receive this email because you have an open need in this category and a resource was registered nearby.
        You can turn these emails off on your account page.
        """;

        return new Mail(subject, body);
    }


    /// <summary>Text from users or external services: one line, no links, limited length.</summary>
    private static string Clean(string text, int maxLength)
    {
        var oneLine = Whitespace.Replace(text, " ").Trim();
        var withoutLinks = Links.Replace(oneLine, "[link removed]");   // before truncating, so no half links are left

        return withoutLinks.Length <= maxLength
            ? withoutLinks
            : withoutLinks[..(maxLength - 3)] + "...";
    }
}