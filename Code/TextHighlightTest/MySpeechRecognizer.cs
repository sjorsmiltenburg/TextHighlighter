using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TextHighlightTest
{
    public class MySpeechRecognizer
    {
        List<MySpeechRecognizeResult> _rawRecognizedResults = new List<MySpeechRecognizeResult>();

        public async Task Run(string wavFilePath, string language, string jsonOutputFilePath)
        {
            await RecognizeSpeech(wavFilePath, language);
            WriteResultToDisk(jsonOutputFilePath);
        }

        private void WriteResultToDisk(string jsonOutputFilePath)
        {
            var jsonResult = JsonSerializer.Serialize(_rawRecognizedResults);
            if (File.Exists(jsonOutputFilePath))
            {
                File.Delete(jsonOutputFilePath);
            }
            File.WriteAllText(jsonOutputFilePath, jsonResult);
        }

        private async Task RecognizeSpeech(string wavFilePath, string language)
        {
            string speechKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
            string speechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION");

            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = language;

            var audioConfig = AudioConfig.FromWavFileInput(wavFilePath);
            var speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

            //speechRecognizer.Recognized += SpeechRecognizer_Recognized;
            speechRecognizer.Recognizing += SpeechRecognizer_Recognizing;
            var speechRecognitionResult = await speechRecognizer.RecognizeOnceAsync();
            OutputSpeechRecognitionResult(speechRecognitionResult);
        }


        private void SpeechRecognizer_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizingSpeech)
            {
                var raw = new MySpeechRecognizeResult()
                {
                    Text = e.Result.Text,
                    OffsetInTicks = e.Result.OffsetInTicks,
                    DurationInTicks = e.Result.Duration.Ticks
                };

                _rawRecognizedResults.Add(raw);
                Console.WriteLine(String.Format("RECOGNIZING: {0}", e.Result.Text));
                Console.WriteLine(String.Format("Offset in Ticks: {0}", e.Result.OffsetInTicks));
                Console.WriteLine(String.Format("Duration in Ticks: {0}", e.Result.Duration.Ticks));
            }
        }

        //private void SpeechRecognizer_Recognized(object? sender, SpeechRecognitionEventArgs e)
        //{
        //    if (e.Result.Reason == ResultReason.RecognizedSpeech)
        //    {
        //        var myResult = new MyRawSpeechRecognizeResult()
        //        {
        //            Text = e.Result.Text,
        //            OffsetInTicks = e.Result.OffsetInTicks,
        //            DurationInTicks = e.Result.Duration.Ticks
        //        };
        //        _rawRecognizedResults.Add(myResult);

        //        Console.WriteLine(String.Format("--RECOGNIZED: {0}", e.Result.Text));
        //        Console.WriteLine(String.Format("--Offset in Ticks: {0}", e.Result.OffsetInTicks));
        //        Console.WriteLine(String.Format("--Duration in Ticks: {0}", e.Result.Duration.Ticks));
        //    }
        //}

        void OutputSpeechRecognitionResult(SpeechRecognitionResult speechRecognitionResult)
        {
            switch (speechRecognitionResult.Reason)
            {
                case ResultReason.RecognizedSpeech:
                    Console.WriteLine($"RECOGNIZED: Text={speechRecognitionResult.Text}");
                    break;
                case ResultReason.NoMatch:
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    break;
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    break;
            }
        }
    }
}
