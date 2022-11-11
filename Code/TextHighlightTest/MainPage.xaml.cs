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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TextHighlightTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Uri _wavFileUri = null;
        string _wavFileLanguage = "en-US";
        string _rawResultJsonFilePath = string.Empty;
        string _cleanedResultJsonFilePath = string.Empty;
        MediaPlayer _mediaPlayer;
        List<MySpeechRecognizeResult> raw = new List<MySpeechRecognizeResult>();
        List<MySpeechRecognizeResult> cleaned = new List<MySpeechRecognizeResult>();

        public MainPage()
        {
            this.InitializeComponent();

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            _wavFileUri = new Uri($"ms-appx:///Assets/Sounds/testsample_english.wav");

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
            //show contents of cleaned on screen
            var sb = new StringBuilder();
            foreach (var item in input)
            {
                var timespan = TimeSpan.FromTicks(item.DurationInTicks + item.OffsetInTicks);
                sb.AppendLine($"{item.Text} - {item.OffsetInTicks} - {item.DurationInTicks} - {timespan.TotalSeconds}:{timespan.Milliseconds}");
            }
            return sb.ToString();
        }

        private string GetWavPath(Uri wavUri)
        {
            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var wavPath = InstallationFolder.Path + _wavFileUri.AbsolutePath.Replace('/', '\\');
            return wavPath;
        }

        private async void Button_Click_AnalyseAudio(object sender, RoutedEventArgs e)
        {
            await new MySpeechRecognizer().Run(GetWavPath(_wavFileUri), _wavFileLanguage, _rawResultJsonFilePath);
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

                if (!File.Exists(GetWavPath(_wavFileUri)))
                {
                    new MessageDialog("file does not exist");
                    return;
                }
                _mediaPlayer.Source = MediaSource.CreateFromUri(_wavFileUri);
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
                return (List<MySpeechRecognizeResult>)jsonDeserializeResult;
            }
            return null;
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

        int nrOfLabelUpdates = 0;
        private async void UpdateLabel(long ticks)
        {
            nrOfLabelUpdates++;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MyTbHighlighted.Inlines.Clear();

                var multiplier = _mediaPlayerDetectedDurationInTicks / cleaned.Last().DurationInTicks;

                long totalDuration = 0;

                var sb = new StringBuilder();
                foreach (var item in cleaned)
                {
                    sb.Append(item.Text);
                    totalDuration += item.DurationInTicks * multiplier;
                    totalDuration += item.OffsetInTicks * multiplier;

                    if (totalDuration < ticks)
                    {
                        MyTbHighlighted.Inlines.Add(new Run { Text = item.Text, FontWeight = FontWeights.ExtraBold });
                    }
                    else
                    {
                        MyTbHighlighted.Inlines.Add(new Run { Text = item.Text, FontWeight = FontWeights.Light });
                    }
                }


                var sb2 = new StringBuilder();
                sb2.AppendLine($"nr of label updates: {nrOfLabelUpdates}");
                sb2.AppendLine($"multiplier: {multiplier}");
                var timespan = TimeSpan.FromTicks(_mediaPlayerDetectedDurationInTicks);
                sb2.AppendLine($"Media Player Detected Audio Length: {_mediaPlayerDetectedDurationInTicks} (ticks) {timespan.ToString()}");
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
