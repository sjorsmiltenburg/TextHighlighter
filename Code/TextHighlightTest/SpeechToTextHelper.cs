using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TextHighlightTest
{
    internal class SpeechToTextHelper
    {
        // This example requires environment variables named "SPEECH_KEY" and "SPEECH_REGION"

        public async Task Run()
        {
            var wavLanguage = "en-US";
            var wavFilePath = @"c:\temp\speechtotexttest-en.wav";
            var jsonRawResultsFilePath = @"c:\temp\rawResult.json";
            var jsonCleanedResultsFilePath = @"c:\temp\cleanedResult.json";

            try
            {
                await new MySpeechRecognizer().Run(wavFilePath, wavLanguage, jsonRawResultsFilePath);
                new ResultCleaner().Run(jsonRawResultsFilePath, jsonCleanedResultsFilePath);
            }
            catch (Exception e)
            {
                Debugger.Break();
            }
        }
    }
}
