using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chatter.Services;

namespace Tests
{
    [TestClass]
    public class TimingServiceTests
    {
        [TestMethod]
        public void GetNextUtc_ValidCron_ReturnsExpected()
        {
            var from = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            // 01:00 daily
            var cron = "0 1 * * *";
            var next = TimingService.GetNextUtc(cron, from);
            Assert.IsNotNull(next);
            Assert.AreEqual(new DateTime(2025, 12, 1, 1, 0, 0, DateTimeKind.Utc), next.Value);
        }

        [TestMethod]
        public void IsDueSince_WhenOccurrenceBetweenLastRunAndCheck_ReturnsTrue()
        {
            var lastRun = new DateTime(2025, 12, 1, 0, 30, 0, DateTimeKind.Utc);
            var check = new DateTime(2025, 12, 1, 1, 0, 0, DateTimeKind.Utc);
            var cron = "0 1 * * *"; // 01:00
            var due = TimingService.IsDueSince(cron, lastRun, check);
            Assert.IsTrue(due);
        }

        [TestMethod]
        public void IsDueSince_NoOccurrence_ReturnsFalse()
        {
            var lastRun = new DateTime(2025, 12, 1, 1, 0, 0, DateTimeKind.Utc);
            var check = new DateTime(2025, 12, 1, 1, 30, 0, DateTimeKind.Utc);
            var cron = "0 1 * * *"; // next occurrence at 01:00, but lastRun is 01:00 so no new occurrence
            var due = TimingService.IsDueSince(cron, lastRun, check);
            Assert.IsFalse(due);
        }

        [TestMethod]
        public void GetNextUtc_InvalidCron_ReturnsNull()
        {
            var from = DateTime.UtcNow;
            var cron = "not a cron";
            var next = TimingService.GetNextUtc(cron, from);
            Assert.IsNull(next);
        }
    }
}
