using EventHook;
using System;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Hardcodet.Wpf.TaskbarNotification;

namespace Observator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string filePath = "";
        private string timestamp = "";
        private Recorder recorder;
        private bool isRecording = false;
        private StopWatch stopWatch;
        private int keyboardEventCounter = 0;
        private int mouseEventCounter = 0;

        public MainWindow()
        {
            InitializeComponent();

            ConfigureEventHandlers();

            SelectLocationButton.Click += SelectLocationButton_Click;
            RecordStartButton.Click += RecordStartButton_Click;
            RecordStopButton.Click += RecordStopButton_Click;
            TrayRecordButton.Click += TrayRecordButton_Click;
        }

        void ConfigureEventHandlers()
        {
            using (var eventHookFactory = new EventHookFactory())
            {
                var keyboardWatcher = eventHookFactory.GetKeyboardWatcher();
                keyboardWatcher.Start();
                keyboardWatcher.OnKeyInput += (s, e) =>
                {
                    string eventString = string.Format("Key {0} event of key {1}", e.KeyData.EventType, e.KeyData.Keyname);
                    Dispatcher.Invoke(() =>
                    {
                        KeyboardEvents.Content = eventString;
                    });

                    if (isRecording)
                    {
                        WriteEvent(eventString, "Keyboard");
                    }
                };

                var mouseWatcher = eventHookFactory.GetMouseWatcher();
                mouseWatcher.Start();
                mouseWatcher.OnMouseInput += (s, e) =>
                {
                    string eventString = string.Format("Mouse event {0} at point {1},{2}", e.Message.ToString(), e.Point.x, e.Point.y);
                    Dispatcher.Invoke(() =>
                    {
                        MouseEvents.Content = eventString;
                    });

                    if (isRecording)
                    {
                        if (e.Message.ToString() == "WM_LBUTTONDOWN" || e.Message.ToString() == "WM_RBUTTONDOWN")
                        {
                            WriteEvent(eventString, "Mouse");
                        }
                    }

                    if (e.Message.ToString() == "WM_LBUTTONDOWN")
                    {
                        //TakeScreenshot();
                        // left mouse down
                    }
                    else if (e.Message.ToString() == "WM_RBUTTONDOWN")
                    {
                        // right mouse down
                    }
                };

                var clipboardWatcher = eventHookFactory.GetClipboardWatcher();
                clipboardWatcher.Start();
                clipboardWatcher.OnClipboardModified += (s, e) =>
                {
                    Console.WriteLine(e.Data);
                };


                var applicationWatcher = eventHookFactory.GetApplicationWatcher();
                applicationWatcher.Start();
                applicationWatcher.OnApplicationWindowChange += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ApplicationEvents.Content = string.Format("Application window of '{0}' with the title '{1}' was {2}", e.ApplicationData.AppName, e.ApplicationData.AppTitle, e.Event);
                    });
                };

                var printWatcher = eventHookFactory.GetPrintWatcher();
                printWatcher.Start();
                printWatcher.OnPrintEvent += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        PrinterEvents.Content = string.Format("Printer '{0}' currently printing {1} pages.", e.EventData.PrinterName, e.EventData.Pages);
                    });
                };
            }
        }

        private void WriteEvent(string eventString, string name)
        {
            int eventCounter;
            if (name == "Keyboard")
            {
                eventCounter = ++keyboardEventCounter;
            } 
            else if (name == "Mouse")
            {
                eventCounter = ++mouseEventCounter;
            }
            else
            {
                return;
            }

            TimeSpan timeSpan = stopWatch.getTimeDifference();
            string time = ParseTime(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
            string nextTime = ParseTime(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds + 1, timeSpan.Milliseconds);

            string[] lines = { eventCounter.ToString(), time + " --> " + nextTime, eventString, "" };
            File.AppendAllLines(filePath + "\\" + name + timestamp + ".srt", lines);
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

        private void SelectLocationButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                LocationEntry.Text = dialog.SelectedPath;
                filePath = dialog.SelectedPath;
            }
        }

        private void RecordStartButton_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }

        private void RecordStopButton_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();
        }

        private void TrayRecordButton_Click(object sender, EventArgs e)
        {
            if (TrayRecordButton.Content.ToString() == "Start")
            {
                StartRecording();
            } else
            {
                StopRecording();
            }
        }

        private void StartRecording()
        {
            if (filePath == "" || filePath == null)
            {
                NotifyIcon.ShowBalloonTip("No Path Found", "Please specify file path!", BalloonIcon.Info);
                return;
            }

            NotifyIcon.HideBalloonTip();
            isRecording = true;
            stopWatch = new StopWatch();
            keyboardEventCounter = 0;
            mouseEventCounter = 0;
            timestamp = DateTime.Now.ToString("ddMMyyyy-hhmmss");

            File.Create(filePath + "\\Keyboard" + timestamp + ".srt");
            File.Create(filePath + "\\Mouse" + timestamp + ".srt");
            recorder = new Recorder(new RecorderParams(filePath + "\\Record" + timestamp + ".avi", 10, SharpAvi.KnownFourCCs.Codecs.MotionJpeg, 70));

            UpdateRecordButtons();
        }

        private void StopRecording()
        {
            recorder.Dispose();
            isRecording = false;
            stopWatch = null;

            Dispatcher.Invoke(() =>
            {
                UpdateRecordButtons();
            });

            string[] subtitleFiles = { filePath + "\\Keyboard" + timestamp, filePath + "\\Mouse" + timestamp };
            _ = new VideoConverter(filePath + "\\Record" + timestamp, subtitleFiles);
            timestamp = "";
        }

        private void UpdateRecordButtons()
        {
            if (isRecording)
            {
                TrayRecordButton.Content = "Stop";
                RecordStartButton.IsEnabled = false;
                RecordStopButton.IsEnabled = true;
            } else
            {
                TrayRecordButton.Content = "Start";
                RecordStartButton.IsEnabled = true;
                RecordStopButton.IsEnabled = false;
            }
        }

        private void TakeScreenshot()
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (filePath == "" || filePath == null)
                {
                    return;
                }

                double screenLeft = 0;
                double screenTop = 0;
                double screenWidth = SystemParameters.PrimaryScreenWidth * 1.5;
                double screenHeight = SystemParameters.PrimaryScreenHeight * 1.5;

                using (Bitmap bmp = new Bitmap((int)screenWidth,
                    (int)screenHeight))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        String filename = "ScreenCapture-" + DateTime.Now.ToString("ddMMyyyy-hhmmss") + ".png";
                        Opacity = .0;
                        g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bmp.Size);
                        bmp.Save(LocationEntry.Text + "\\" + filename);
                        Opacity = 1;
                    }

                }

            });
            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            TrackingService.StopListening();
        }
    }
}
