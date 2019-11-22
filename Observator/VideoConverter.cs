using NReco.VideoConverter;
using System;
using System.IO;
using System.Threading;

namespace Observator
{
    class VideoConverter
    {
        Thread convertThread;
        string videoName;
        string[] subtitleFiles;
        string[] subtitleNames;

        public VideoConverter(string videoName, string[] subtitleFiles, string[] subtitleNames) 
        {
            this.videoName = videoName;
            this.subtitleFiles = subtitleFiles;
            this.subtitleNames = subtitleNames;

            convertThread = new Thread(ConvertVideo)
            {
                Name = typeof(Recorder).Name + ".ConvertVideo",
                IsBackground = true
            };

            convertThread.Start();
        }

        public void Close()
        {
            convertThread.Join();
        }

        private void ConvertVideo()
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

                ffMpeg.Invoke(generateCommmand());

                File.Delete(videoName + ".avi");
                foreach(string file in subtitleFiles)
                {
                    File.Delete(file + ".srt");
                }
            }
        }

        private string generateCommmand()
        {
            string command = "-i \"" + videoName + ".avi\" ";
            foreach (string file in subtitleFiles)
            {
                command += "-i \"" + file + ".srt\" ";
            }
            command += "-map 0:v -map 0:a? ";
            for (int i = 0; i < subtitleFiles.Length; i++)
            {
                command += "-map " + (i + 1) + " ";
            }
            command += "-c:v copy -c:a copy -c:s srt ";
            for (int i = 0; i < subtitleNames.Length; i++)
            {
                command += "-metadata:s:s:" + i + " title=\"" + subtitleNames[i] + "\" ";
            }

            return command + "\"" + videoName + ".mkv\"";
        }

        private void AppendEmptySubstitleFile(string file)
        {
            string[] lines = { "1", "00:00:00,000 --> 00:00:00,000", "" };
            File.AppendAllLines(file, lines);
        }
    }
}
