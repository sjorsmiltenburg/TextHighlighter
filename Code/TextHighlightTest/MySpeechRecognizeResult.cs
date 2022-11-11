namespace TextHighlightTest
{
    public class MySpeechRecognizeResult
    {
        public string Text { get; set; } = string.Empty;
        public long OffsetInTicks { get; set; }
        public long DurationInTicks { get; set; }

        public long StartInTicks { get; set; }
        public long EndInTicks { get; set; }
    }
}
