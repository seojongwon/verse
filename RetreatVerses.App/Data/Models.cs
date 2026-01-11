using System;

namespace RetreatVerses.App.Data
{
    public sealed class Group
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
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
}
