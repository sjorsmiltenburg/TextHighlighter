namespace TextHighlightTest
{
    public class MySpeechRecognizeResult
    {
        public int SampleId { get; set; }

        //fields coming back from the api
        public string Text { get; set; } = string.Empty;
        public long OffsetInTicks { get; set; }
        public long DurationInTicks { get; set; }

        //timestamps for this section
        public string SectionText { get; set; }
        public long StartInTicks { get; set; }
        public long EndInTicks { get; set; }

        //location in string for this section
        public int StartCharacterIndex { get; set; }
        public int EndCharacterIndex { get; set; }
    }
}
