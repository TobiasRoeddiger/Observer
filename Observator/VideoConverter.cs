using NReco.VideoConverter;
using System.IO;
using System.Threading;

namespace Observator
{
    class VideoConverter
    {
        Thread convertThread;
        string videoName;
        string[] subtitleFiles;

        public VideoConverter(string videoName, string[] subtitleFiles)
        {
            this.videoName = videoName;
            this.subtitleFiles = subtitleFiles;

            convertThread = new Thread(ConvertVideo)
            {
                Name = typeof(Recorder).Name + ".ConvertVideo",
                IsBackground = true
            };

            convertThread.Start();
        }

        void ConvertVideo()
        {
            if (File.Exists(videoName + ".avi") && File.Exists(subtitleFiles[0] + ".srt") && 
                File.Exists(subtitleFiles[1] + ".srt"))
            {
                var ffMpeg = new FFMpegConverter();

                foreach(string file in subtitleFiles)
                {
                    if (new FileInfo(file + ".srt").Length == 0)
                    {
                        AppendEmptySubstitleFile(file + ".srt");
                    }
                }

                ffMpeg.Invoke("-i " + videoName + ".avi -i " + subtitleFiles[0] + ".srt -i " + 
                    subtitleFiles[1] + ".srt -map 0:v -map 0:a? -map 1 -map 2 -c:v copy -c:a copy -c:s srt " +
                    "-metadata:s:s:0 title=\"Keyboard\" -metadata:s:s:1 title=\"Mouse\" " + 
                    videoName + ".mkv");

                File.Delete(videoName + ".avi");
                foreach(string file in subtitleFiles)
                {
                    File.Delete(file + ".srt");
                }
            }
        }

        private void AppendEmptySubstitleFile(string file)
        {
            string[] lines = { "1", "00:00:00,000 --> 00:00:00,000", "" };
            File.AppendAllLines(file, lines);
        }
    }
}
