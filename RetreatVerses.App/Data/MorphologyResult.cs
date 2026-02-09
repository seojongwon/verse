namespace RetreatVerses.App.Data
{
    public sealed class MorphologyResult
    {
        public MorphologyResult(bool isNoun, string? message = null)
        {
            IsNoun = isNoun;
            Message = message;
        }

        public bool IsNoun { get; }
        public string? Message { get; }
    }
}
