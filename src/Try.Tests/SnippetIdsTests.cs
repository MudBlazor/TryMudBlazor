namespace Tests
{
    using System;
    using NUnit.Framework;
    using TryMudBlazor.Server.Utilities;
    using static TryMudBlazor.Server.Utilities.SnippetsEncoder;

    public class SnippetIdsTests
    {
        [Test]
        public void NewIdIsDateAndMillisecondOfDay()
        {
            var id = SnippetIds.New(new DateTime(2026, 9, 4, 13, 5, 7, 89, DateTimeKind.Utc));

            // 13:05:07.089 = 47,107,089 ms into the day
            Assert.That(id, Is.EqualTo("2026090447107089"));
            Assert.That(id, Has.Length.EqualTo(SnippetIds.Length));
        }

        [Test]
        public void EndOfDayStillFitsSixteenDigits()
        {
            var id = SnippetIds.New(new DateTime(2026, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc));

            Assert.That(id, Is.EqualTo("2026123186399999"));
        }

        [Test]
        public void BlobPathSplitsIdIntoDateFolders()
        {
            Assert.That(SnippetIds.BlobPath("2026090447107089"), Is.EqualTo("2026/09/04/47107089"));
        }

        [Test]
        public void BlobPathRejectsWrongLength()
        {
            Assert.Throws<ArgumentException>(() => SnippetIds.BlobPath("20260904"));
        }

        [Test]
        public void IdSurvivesPublicEncoding()
        {
            var id = SnippetIds.New(DateTime.UtcNow);

            Assert.That(DecodeSnippetId(EncodeSnippetId(id)), Is.EqualTo(id));
        }
    }
}
