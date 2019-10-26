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
        string filePath = "";
        string timestamp = "";
        bool isRecording = false;
        int[] mousePosition = new int[2] { 0, 0 };
        int minDistance = 20;

        Recorder recorder;
        VideoConverter converter;
        EventWriter eventWriter;

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
                        eventWriter.WriteEvent(EventWriter.InputEvent.Keyboard, eventString);
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
                            eventWriter.WriteEvent(EventWriter.InputEvent.MouseClick, eventString);
                        } else
                        {
                            if ((mousePosition[0] == 0 && mousePosition[1] == 0) || 
                                Math.Abs(e.Point.x - mousePosition[0]) >= minDistance ||
                                Math.Abs(e.Point.y - mousePosition[1]) >= minDistance)
                            {
                                mousePosition[0] = e.Point.x;
                                mousePosition[1] = e.Point.y;
                                eventWriter.WriteEvent(EventWriter.InputEvent.MouseMove, eventString);
                            }
                        }
                    }
                };

                var clipboardWatcher = eventHookFactory.GetClipboardWatcher();
                clipboardWatcher.Start();
                clipboardWatcher.OnClipboardModified += (s, e) =>
                {
                    eventWriter.WriteEvent(EventWriter.InputEvent.Clipboard, e.Data.ToString());
                };

                var applicationWatcher = eventHookFactory.GetApplicationWatcher();
                applicationWatcher.Start();
                applicationWatcher.OnApplicationWindowChange += (s, e) =>
                {
                    string eventString = string.Format("Application window of '{0}' with the title '{1}' was {2}", 
                        e.ApplicationData.AppName, e.ApplicationData.AppTitle, e.Event);
                    Dispatcher.Invoke(() =>
                    {
                        ApplicationEvents.Content = eventString;
                    });

                    if (isRecording)
                    {
                        eventWriter.WriteEvent(EventWriter.InputEvent.Application, eventString);
                    }
                };

                var printWatcher = eventHookFactory.GetPrintWatcher();
                printWatcher.Start();
                printWatcher.OnPrintEvent += (s, e) =>
                {
                    string eventString = string.Format("Printer '{0}' currently printing {1} pages.",
                        e.EventData.PrinterName, e.EventData.Pages);
                    Dispatcher.Invoke(() =>
                    {
                        PrinterEvents.Content = eventString;
                    });

                    if (isRecording)
                    {
                        eventWriter.WriteEvent(EventWriter.InputEvent.Print, eventString);
                    }
                };
            }
        }

        private void SelectLocationButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
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
            }
            else
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
            timestamp = DateTime.Now.ToString("ddMMyyyy-hhmmss");
            eventWriter = new EventWriter(filePath, timestamp);

            recorder = new Recorder(new RecorderParams(filePath + "\\Record" + timestamp + ".avi", 10, SharpAvi.KnownFourCCs.Codecs.MotionJpeg, 70));

            UpdateRecordButtons();
        }

        private void StopRecording()
        {
            recorder.Dispose();
            recorder = null;
            isRecording = false;

            string[] subtitleFiles = eventWriter.GetAllFiles();
            string[] subtitleNames = eventWriter.GetEventNames();
            eventWriter = null;

            Dispatcher.Invoke(() =>
            {
                UpdateRecordButtons();
            });

            converter = new VideoConverter(filePath + "\\Record" + timestamp, subtitleFiles, subtitleNames);
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

            converter?.Dispose();
            TrackingService.StopListening();
        }
    }
}
