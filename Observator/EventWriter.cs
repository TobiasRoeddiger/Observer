using System;
using System.IO;
using System.Linq;

namespace Observator
{
    class EventWriter
    {
        public enum InputEvent { Keyboard, MouseClick, MouseMove, Clipboard, Application, Print, Url };
        const int NumEvents = 7;

        StopWatch stopWatch;
        string filePath;
        int[] eventCounters;
        string timestamp;

        public EventWriter(string filePath, string timestamp)
        {
            this.filePath = filePath;
            this.timestamp = timestamp;

            stopWatch = new StopWatch();
            eventCounters = Enumerable.Repeat(0, NumEvents).ToArray();
            CreateFiles();
        }

        public string[] GetEventNames()
        {
            return Enum.GetNames(typeof(InputEvent));
        }

        public void WriteEvent(InputEvent inputEvent, string message)
        {
            TimeSpan timeSpan = stopWatch.getTimeDifference();
            string time = ParseTime(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
            string nextTime = ParseTime(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds + 1, timeSpan.Milliseconds);

            string[] lines = { IncrementAndGetEventCounter(inputEvent).ToString(), time + " --> " + nextTime, message, "" };
            File.AppendAllLines(filePath + "\\" + GetEventNames()[(int)inputEvent] + timestamp + ".srt", lines);
        }

        public string[] GetAllFiles()
        {
            string[] files = new string[NumEvents];
            string[] names = GetEventNames();
            for (int i = 0; i < NumEvents; i++)
            {
                files[i] = filePath + "\\" + names[i] + timestamp;
            }
            return files;
        }

        public void DeleteAllFiles()
        {
            string[] names = GetEventNames();
            foreach (string name in names)
            {
                if (File.Exists(filePath + "\\" + name + timestamp + ".srt"))
                {
                    File.Delete(filePath + "\\" + name + timestamp + ".srt");
                }
            }
        }

        private void CreateFiles()
        {
            foreach (string name in GetEventNames())
            {
                File.Create(filePath + "\\" + name + timestamp + ".srt");
            }
        }

        private int IncrementAndGetEventCounter(InputEvent inputEvent)
        {
            return ++eventCounters[(int) inputEvent];
        }

        private string ParseTime(int hours, int minutes, int seconds, int milliseconds)
        {
            string hourString = hours > 9 ? hours.ToString() : "0" + hours;
            string minuteString = minutes > 9 ? minutes.ToString() : "0" + minutes;
            string secondString = seconds > 9 ? seconds.ToString() : "0" + seconds;
            string millisecondString;
            if (milliseconds > 99)
            {
                millisecondString = milliseconds.ToString();
            }
            else if (milliseconds > 9)
            {
                millisecondString = "0" + milliseconds;
            }
            else
            {
                millisecondString = "00" + milliseconds;
            }

            return hourString + ":" + minuteString + ":" + secondString + "," + millisecondString;
        }
    }
}
