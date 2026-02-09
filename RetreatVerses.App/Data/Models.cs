using System;

namespace RetreatVerses.App.Data
{
    public sealed class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }

    public sealed class Verse
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public sealed class Registration
    {
        public Guid GroupId { get; set; }
        public Guid VerseId { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime? RecitedAt { get; set; }
    }

    public sealed class GuardWordEntry
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public string VerseText { get; set; } = string.Empty;
        public string Word { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
