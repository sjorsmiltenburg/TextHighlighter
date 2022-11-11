using System;
using System.IO;
using Windows.Storage;

namespace TextHighlightTest
{
    public class AudioSample
    {
        public int Id { get; set; }
        public Uri WavFileUri { get; set; }
        public string WavFileLanguage { get; set; }

        public string Name { get { return Path.GetFileName(WavFileUri.AbsolutePath); } }

        public string WavFilePath
        {
            get
            {
                StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                var wavPath = InstallationFolder.Path + WavFileUri.AbsolutePath.Replace('/', '\\');
                return wavPath;
            }
        }
    }
}
