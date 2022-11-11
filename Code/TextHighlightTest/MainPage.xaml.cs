using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace TextHighlightTest
{
    public sealed partial class MainPage : Page
    {
        string _rawResultJsonFilePath = string.Empty;
        string _cleanedResultJsonFilePath = string.Empty;
        MediaPlayer _mediaPlayer;
        List<MySpeechRecognizeResult> raw = new List<MySpeechRecognizeResult>();
        List<MySpeechRecognizeResult> cleaned = new List<MySpeechRecognizeResult>();

        List<AudioSample> _samples = new List<AudioSample>();

        public MainPage()
        {
            this.InitializeComponent();

            var sample1 = new AudioSample()
            {
                Id = 1,
                WavFileUri = new Uri($"ms-appx:///Assets/Sounds/testsample_dutch.wav"),
                WavFileLanguage = "nl-nl"
            };
            var sample2 = new AudioSample()
            {
                Id = 2,
                WavFileUri = new Uri($"ms-appx:///Assets/Sounds/testsample_english.wav"),
                WavFileLanguage = "en-US"
            };
            var sample3 = new AudioSample()
            {
                Id = 3,
                WavFileUri = new Uri($"ms-appx:///Assets/Sounds/testsample_english_offsettest.wav"),
                WavFileLanguage = "en-US"
            };
            //_samples = new List<AudioSample>() { sample1, sample2, sample3 };

            ComboBox_Samples.Items.Add(sample1);
            ComboBox_Samples.Items.Add(sample2);
            ComboBox_Samples.Items.Add(sample3);
            //.DataContext = _samples;
            ComboBox_Samples.SelectedIndex = 0;

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            _rawResultJsonFilePath = Path.Combine(localFolder.Path, "rawResult.json");
            _cleanedResultJsonFilePath = Path.Combine(localFolder.Path, "cleanedResult.json");

            LoadRaw();
            LoadCleaned();
        }

        private void LoadRaw()
        {
            MyTbRaw.Text = string.Empty;
            var items = ReadJson(_rawResultJsonFilePath);
            if (items != null)
            {
                MyTbRaw.Text = ConcatList(items);
                raw = items;
            }
        }

        private void LoadCleaned()
        {
            MyTbCleaned.Text = string.Empty;
            var items = ReadJson(_cleanedResultJsonFilePath);
            if (items != null)
            {
                MyTbCleaned.Text = ConcatList(items);
                cleaned = items;
            }
        }

        private string ConcatList(List<MySpeechRecognizeResult> input)
        {
            var sb = new StringBuilder();
            foreach (var item in input)
            {
                string text = item.Text;
                if (!string.IsNullOrEmpty(item.SectionText))
                {
                    text = item.SectionText;
                }
                sb.AppendLine($"{text} -  start-end: {new DateTime(item.StartInTicks).ToString("ss:ff")} - {new DateTime(item.EndInTicks).ToString("ss:ff")}");
            }
            return sb.ToString();
        }

        private async void Button_Click_AnalyseAudio(object sender, RoutedEventArgs e)
        {
            var sample = (AudioSample)ComboBox_Samples.SelectedItem;
            await new MySpeechRecognizer().Run(sample.WavFilePath, sample.WavFileLanguage, _rawResultJsonFilePath);
            LoadRaw();
        }

        private async void Button_Click_CleanAnalysis(object sender, RoutedEventArgs e)
        {
            if (!raw.Any())
            {
                await new MessageDialog("first make sure there is a raw result").ShowAsync();
                return;
            }
            new ResultCleaner().Run(_rawResultJsonFilePath, _cleanedResultJsonFilePath);
            LoadCleaned();
        }

        private async void Button_Click_Play(object sender, RoutedEventArgs e)
        {
            try
            {
                MyTbHighlighted.Text = raw.Last().Text; //already show text before first positionchangedEvent fires

                if (!cleaned.Any())
                {
                    await new MessageDialog("first make sure there is a cleaned result").ShowAsync();
                    return;
                }
                if (_mediaPlayer == null)
                {
                    _mediaPlayer = new MediaPlayer();
                    _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
                    _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
                    _mediaPlayer.MediaEnded -= _mediaPlayer_MediaEnded;
                    _mediaPlayer.MediaEnded += _mediaPlayer_MediaEnded;
                    _mediaPlayer.PlaybackSession.NaturalDurationChanged += PlaybackSession_NaturalDurationChanged;
                }
                Pause();

                var sample = (AudioSample)ComboBox_Samples.SelectedItem;
                if (!File.Exists(sample.WavFilePath))
                {
                    new MessageDialog("file does not exist");
                    return;
                }
                _mediaPlayer.Source = MediaSource.CreateFromUri(sample.WavFileUri);
                _mediaPlayer.Play();

            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
        }

        private void PlaybackSession_NaturalDurationChanged(MediaPlaybackSession sender, object args)
        {
            _mediaPlayerDetectedDurationInTicks = _mediaPlayer.PlaybackSession.NaturalDuration.Ticks;
        }

        private long _mediaPlayerDetectedDurationInTicks { get; set; }

        private void Button_Click_Pause(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            long ticks = _mediaPlayer.PlaybackSession.Position.Ticks;
            Pause();
        }

        private List<MySpeechRecognizeResult> ReadJson(string jsonInputFilePath)
        {
            if (!File.Exists(jsonInputFilePath))
            {
                return null;
            }

            var rawResultJsonTextFile = File.ReadAllText(jsonInputFilePath);
            var jsonDeserializeResult = JsonSerializer.Deserialize(rawResultJsonTextFile, typeof(List<MySpeechRecognizeResult>));
            if (jsonDeserializeResult != null)
            {
                var result = (List<MySpeechRecognizeResult>)jsonDeserializeResult;
                Enrich(result);
                return result;
            }
            return null;
        }

        private void Enrich(List<MySpeechRecognizeResult> input)
        {
            long previousItemDurationInTicks = 0;

            foreach (var item in input)
            {
                var segmentDurationInTicks = item.DurationInTicks - previousItemDurationInTicks;
                var startInTicks = previousItemDurationInTicks; //+item.OffsetInTicks;
                var endInTicks = startInTicks + segmentDurationInTicks;
                previousItemDurationInTicks = item.DurationInTicks;
                item.StartInTicks = startInTicks;
                item.EndInTicks = endInTicks;
            }
        }

        private void _mediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            UpdateLabel(sender.Position.Ticks);
        }

        int _nrOfLabelUpdates = 0;
        private async void UpdateLabel(long ticks)
        {
            _nrOfLabelUpdates++;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var finalRecognizeResult = raw.Last().Text; //the last outputted result is the most accurate
                MyTbHighlighted.Inlines.Clear();

                var activeRanges = cleaned.Where(item => ticks > item.StartInTicks && ticks < item.EndInTicks);
                if (activeRanges.Count() == 1)
                {
                    //split text in 3 sections
                    //- start (not highlighted)                                        
                    var activeRange = activeRanges.First();
                    if (activeRange.StartCharacterIndex != 0)
                    {
                        var startSegment = finalRecognizeResult.Substring(0, activeRange.StartCharacterIndex);
                        MyTbHighlighted.Inlines.Add(new Run { Text = startSegment, FontWeight = FontWeights.Light });
                    }

                    //- current section (highlighted)
                    var activeSegment = finalRecognizeResult.Substring(activeRange.StartCharacterIndex, activeRange.SectionText.Length);
                    MyTbHighlighted.Inlines.Add(new Run { Text = activeSegment, FontWeight = FontWeights.ExtraBold });

                    //- end (not highlighted)
                    if (activeRange.EndCharacterIndex != finalRecognizeResult.Length)
                    {
                        var endSegment = finalRecognizeResult.Substring(activeRange.EndCharacterIndex);
                        MyTbHighlighted.Inlines.Add(new Run { Text = endSegment, FontWeight = FontWeights.Light });
                    }
                }
                else if (activeRanges.Count() == 0)
                {
                    //do nothing
                }
                else
                {
                    Debugger.Break();
                }
                //foreach (var item in cleaned)
                //{
                //    if ()
                //    {
                //        MyTbHighlighted.Inlines.Add(new Run { Text = item.SectionText, FontWeight = FontWeights.ExtraBold });
                //    }
                //    else
                //    {
                //        MyTbHighlighted.Inlines.Add(new Run { Text = item.SectionText, FontWeight = FontWeights.Light });
                //    }
                //}

                var sb2 = new StringBuilder();
                sb2.AppendLine($"play position: {new DateTime(ticks).ToString("mm:ss:ff")}");
                sb2.AppendLine($"nr of label updates: {_nrOfLabelUpdates}");
                var timespan = new DateTime(_mediaPlayerDetectedDurationInTicks);
                sb2.AppendLine($"Media Player Detected Audio Length: {timespan.ToString("ss:ff")} = {_mediaPlayerDetectedDurationInTicks} Ticks");
                MyTbDebugInfo.Text = sb2.ToString();
            });
        }

        private void Pause()
        {
            if (_mediaPlayer != null && _mediaPlayer.PlaybackSession != null && _mediaPlayer.PlaybackSession.CanPause)
            {
                _mediaPlayer.Pause();
            }
        }

    }

}
