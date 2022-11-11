using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TextHighlightTest
{
    public class ResultCleaner
    {
        List<MySpeechRecognizeResult> _rawRecognizedResults = new List<MySpeechRecognizeResult>();
        List<MySpeechRecognizeResult> _cleanedResults = new List<MySpeechRecognizeResult>();

        public void Run(string jsonInputFilePath, string jsonOutputFilePath)
        {
            var rawResultJsonTextFile = File.ReadAllText(jsonInputFilePath);

            var jsonDeserializeResult = JsonSerializer.Deserialize(rawResultJsonTextFile, typeof(List<MySpeechRecognizeResult>));
            if (jsonDeserializeResult != null)
            {
                _rawRecognizedResults = (List<MySpeechRecognizeResult>)jsonDeserializeResult;
            }

            foreach (var item in _rawRecognizedResults)
            {
                Console.WriteLine(item.Text);
            }

            while (_rawRecognizedResults.Count > 0)
            {
                var last = _rawRecognizedResults[_rawRecognizedResults.Count - 1];
                if (_rawRecognizedResults.Count > 1)
                {

                    var beforeLast = _rawRecognizedResults[_rawRecognizedResults.Count - 2];
                    var lastText = last.Text.Substring(beforeLast.Text.Length);

                    var cleaned = new MySpeechRecognizeResult()
                    {
                        Text = lastText,
                        OffsetInTicks = last.OffsetInTicks,
                        DurationInTicks = last.DurationInTicks,
                    };
                    _cleanedResults.Insert(0, cleaned);
                }
                else
                {
                    var cleaned = new MySpeechRecognizeResult()
                    {
                        Text = last.Text,
                        OffsetInTicks = last.OffsetInTicks,
                        DurationInTicks = last.DurationInTicks,
                    };

                    _cleanedResults.Insert(0, cleaned);
                }
                _rawRecognizedResults.RemoveAt(_rawRecognizedResults.Count - 1);
            }

            WriteToConsole();
            WriteResultToDisk(jsonOutputFilePath);
        }

        private void WriteToConsole()
        {
            for (int i = 0; i < _cleanedResults.Count; i++)
            {
                var r = _cleanedResults[i];
                Console.WriteLine($"{r.OffsetInTicks}.{r.DurationInTicks} - {r.Text}");
            }
        }

        private void WriteResultToDisk(string jsonOutputFilePath)
        {
            var jsonResult = JsonSerializer.Serialize(_cleanedResults);
            if (File.Exists(jsonOutputFilePath))
            {
                File.Delete(jsonOutputFilePath);
            }
            File.WriteAllText(jsonOutputFilePath, jsonResult);
        }
    }
}
