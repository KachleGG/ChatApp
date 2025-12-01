using System;
using Cronos;

namespace Chatter.Services;

/// <summary>
/// Lightweight cron helpers using Cronos. The project must reference the Cronos NuGet package.
/// These helpers assume cron expressions follow standard 5-field cron (minute hour day month day-of-week)
/// and evaluate in UTC.
/// </summary>
public static class TimingService
{
    /// <summary>
    /// Parse a cron expression and return the next occurrence (UTC) after the provided <paramref name="fromUtc"/>.
    /// Returns null if the expression cannot be parsed or no occurrence is available.
    /// </summary>
    public static DateTime? GetNextUtc(string cronExpression, DateTime fromUtc)
    {
        if (string.IsNullOrWhiteSpace(cronExpression)) return null;
        try
        {
            if (fromUtc.Kind != DateTimeKind.Utc) fromUtc = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
            var cron = CronExpression.Parse(cronExpression, CronFormat.Standard);
            var next = cron.GetNextOccurrence(fromUtc, TimeZoneInfo.Utc);
            return next;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns true when the cron expression has an occurrence strictly after <paramref name="lastRunUtc"/>
    /// and less than or equal to <paramref name="checkUtc"/>. Use this to decide if a schedule is due.
    /// </summary>
    public static bool IsDueSince(string cronExpression, DateTime lastRunUtc, DateTime checkUtc)
    {
        if (string.IsNullOrWhiteSpace(cronExpression)) return false;
        try
        {
            // normalize kinds
            if (lastRunUtc.Kind != DateTimeKind.Utc) lastRunUtc = DateTime.SpecifyKind(lastRunUtc, DateTimeKind.Utc);
            if (checkUtc.Kind != DateTimeKind.Utc) checkUtc = DateTime.SpecifyKind(checkUtc, DateTimeKind.Utc);

            // If never run before, treat lastRun as a short time in the past so first scheduled occurrence is considered
            if (lastRunUtc <= DateTime.MinValue.AddSeconds(1))
            {
                lastRunUtc = checkUtc.AddMinutes(-1);
            }

            var cron = CronExpression.Parse(cronExpression, CronFormat.Standard);
            // get the next occurrence after lastRunUtc (exclusive)
            var next = cron.GetNextOccurrence(lastRunUtc, TimeZoneInfo.Utc);
            if (!next.HasValue) return false;
            return next.Value <= checkUtc;
        }
        catch
        {
            return false;
        }
    }
}
