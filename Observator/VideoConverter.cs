using NReco.VideoConverter;
using System.IO;
using System.Threading;

namespace Observator
{
    class VideoConverter
    {
        Thread convertThread;
        string videoName;
        string formatBefore;
        string formatAfter;

        public VideoConverter(string videoName, string formatBefore, string formatAfter)
        {
            this.videoName = videoName;
            this.formatBefore = formatBefore;
            this.formatAfter = formatAfter;

            convertThread = new Thread(ConvertVideo)
            {
                Name = typeof(Recorder).Name + ".ConvertVideo",
                IsBackground = true
            };

            convertThread.Start();
        }

        void ConvertVideo()
        {
            if (File.Exists(videoName + formatBefore))
            {
                var ffMpeg = new FFMpegConverter();
                ffMpeg.ConvertMedia(videoName + formatBefore, videoName + formatAfter, Format.matroska);
                File.Delete(videoName + formatBefore);
            }
        }
    }
}
