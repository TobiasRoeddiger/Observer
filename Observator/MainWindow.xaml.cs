﻿using EventHook;
using System;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Windows.Media;

namespace Observator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string extensionID = "ebjnhccdoddjldiolpoakneeelhkojie";
        string filePath;
        string timestamp = "";
        bool isRecording = false;
        bool isPaused = false;
        int[] mousePosition;
        int minDistance = 20;
        int currentScreen = 0;

        TransparentWindow transparentWindow;

        Recorder recorder;
        VideoConverter converter;
        EventWriter eventWriter;
        WebServer webServer;

        private readonly EventHookFactory eventHookFactory = new EventHookFactory();
        private readonly KeyboardWatcher keyboardWatcher;
        private readonly ApplicationWatcher applicationWatcher;
        private readonly ClipboardWatcher clipboardWatcher;
        private readonly MouseWatcher mouseWatcher;
        private readonly PrintWatcher printWatcher;

        public object Graphics { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default["filePath"].ToString() != "")
            {
                filePath = Properties.Settings.Default["filePath"].ToString();
            } else
            {
                filePath = Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), @"Videos\Observations");
            }

            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                ScreenBox.Items.Add("Display " + (i + 1));
            }                

            ScreenBox.SelectedIndex = currentScreen;

            ScreenBox.SelectionChanged += ScreenBox_SelectedIndexChanged;
            SelectLocationButton.Click += SelectLocationButton_Click;
            TrayRecordButton.Click += TrayRecordButton_Click;
            SettingsButton.Click += SettingsButton_Click;
            ClosingButton.Click += ClosingButton_Click;
            QuitButton.Click += QuitButton_Click;
            LocationEntry.Text = filePath;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            Hide();

            #region Configure Event Handlers

            keyboardWatcher = eventHookFactory.GetKeyboardWatcher();
            keyboardWatcher.Start();
            keyboardWatcher.OnKeyInput += (s, e) =>
            {
                string eventString = string.Format("Key {0} event of key {1}", e.KeyData.EventType, e.KeyData.Keyname);
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    KeyboardEvents.Content = eventString;
                }));

                if (isRecording)
                {
                    eventWriter.WriteEvent(EventWriter.InputEvent.Keyboard, eventString);
                }
            };

            mouseWatcher = eventHookFactory.GetMouseWatcher();
            mouseWatcher.Start();
            mouseWatcher.OnMouseInput += (s, e) =>
            {
                string eventString = string.Format("Mouse event {0} at point {1},{2}", e.Message.ToString(), e.Point.x, e.Point.y);
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    MouseEvents.Content = eventString;
                }));

                if (isRecording)
                {
                    if (e.Message.ToString() == "WM_LBUTTONDOWN" || e.Message.ToString() == "WM_RBUTTONDOWN")
                    {
                        eventWriter.WriteEvent(EventWriter.InputEvent.MouseClick, eventString);
                    }
                    else
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

            clipboardWatcher = eventHookFactory.GetClipboardWatcher();
            clipboardWatcher.Start();
            clipboardWatcher.OnClipboardModified += (s, e) =>
            {
                eventWriter.WriteEvent(EventWriter.InputEvent.Clipboard, e.Data.ToString());
            };

            applicationWatcher = eventHookFactory.GetApplicationWatcher();
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

            printWatcher = eventHookFactory.GetPrintWatcher();
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

            #endregion

            try
            {
                RegisterChromeExtension();
            } catch (Exception e)
            {
                NotifyIcon.ShowBalloonTip("Exception", e.Message, BalloonIcon.Info);
            }
        }

        private void drawScreenBorder()
        {
            transparentWindow = new TransparentWindow();
            transparentWindow.WindowStartupLocation = WindowStartupLocation.Manual;

            double resHeight = Screen.PrimaryScreen.Bounds.Height;
            double actualHeight = SystemParameters.PrimaryScreenHeight;
            double dpi = resHeight / actualHeight;

            Rectangle viewport = Screen.AllScreens[currentScreen].Bounds;
            transparentWindow.Top = viewport.Y / dpi;
            transparentWindow.Left = viewport.X / dpi;
            transparentWindow.Width = viewport.Width / dpi;
            transparentWindow.Height = viewport.Height / dpi;
            transparentWindow.Show();
        }

        private void removeScreenBorder()
        {
            transparentWindow.Close();
        }

        private void RegisterChromeExtension()
        {
            using (RegistryKey key = 
                Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Google\Chrome"))
            {
                if (key != null)
                {
                    using (RegistryKey key64 =
                        Registry.LocalMachine.CreateSubKey(@"Software\Wow6432Node\Google\Chrome\Extensions"))
                    {
                        key64.CreateSubKey(extensionID).SetValue("update_url", "https://clients2.google.com/service/update2/crx");
                    }
                } else
                {
                    using (RegistryKey key32 =
                        Registry.LocalMachine.CreateSubKey(@"Software\Google\Chrome\Extensions"))
                    {
                        key32.CreateSubKey(extensionID).SetValue("update_url", "https://clients2.google.com/service/update2/crx");
                    }
                }
            }
        }

        private void ScreenBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentScreen = ScreenBox.SelectedIndex;
            removeScreenBorder();
            drawScreenBorder();
        }

        private void SelectLocationButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                LocationEntry.Text = dialog.SelectedPath;
                filePath = dialog.SelectedPath;

                Properties.Settings.Default["filePath"] = filePath;
                Properties.Settings.Default.Save();
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
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            if (!isRecording)
            {
                Show();
                drawScreenBorder();
            }            
        }

        private void ClosingButton_Click(object sender, EventArgs e)
        {
            NotifyIcon.TrayPopupResolved.IsOpen = false;
        }

        private void QuitButton_Click(object sender, EventArgs e)
        {
            if (!isRecording)
            {
                string msg = "Are you sure to close the app?";

                Window window = new Window()
                {
                    Visibility = Visibility.Hidden,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false
                };

                window.Show();

                MessageBoxResult result =
                  System.Windows.MessageBox.Show(
                    msg,
                    "Observator",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                window.Close();
                if (result == MessageBoxResult.Yes)
                {
                    CleanUp();
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        public void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                Pause();
            } else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Resume();
            }
        }

        private void Pause()
        {
            if (isRecording && !isPaused)
            {
                isPaused = true;
                recorder.Pause();
                eventWriter.Pause();
            }
        }

        private void Resume()
        {
            if (isRecording && isPaused)
            {
                isPaused = false;
                recorder.Resume();
                eventWriter.Resume();
            }
        }

        private void StartRecording()
        {
            if (filePath == "" || filePath == null)
            {
                NotifyIcon.ShowBalloonTip("No Path Found", "Please specify file path!", BalloonIcon.Info);
                return;
            }

            try
            {
                int distance = Int32.Parse(MinDistanceText.Text);
                if (distance > 0)
                {
                    minDistance = distance;
                } else
                {
                    NotifyIcon.ShowBalloonTip("Minimal Distance Invalid", "Please specify a valid distance!", BalloonIcon.Info);
                    return;
                }
            }
            catch (FormatException)
            {
                NotifyIcon.ShowBalloonTip("Minimal Distance Invalid", "Please specify a valid distance!", BalloonIcon.Info);
                return;
            }

            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            NotifyIcon.HideBalloonTip();
            mousePosition = new int[] { 0, 0 };
            isRecording = true;
            UpdateRecordingUI();

            timestamp = DateTime.Now.ToString("ddMMyyyy-hhmmss-ffffff");
            eventWriter = new EventWriter(filePath, timestamp);            

            recorder = new Recorder(new RecorderParams(filePath + "\\Record" + timestamp + ".avi", 10, SharpAvi.KnownFourCCs.Codecs.MotionJpeg, 70, currentScreen));

            webServer = new WebServer(new string[] { "http://localhost:8080/url/" }, eventWriter);
            drawScreenBorder();
        }

        private void StopRecording()
        {
            removeScreenBorder();

            recorder.Dispose();
            recorder = null;
            isRecording = false;
            isPaused = false;
            webServer.Stop();
            webServer = null;

            string[] subtitleFiles = eventWriter.GetAllFiles();
            string[] subtitleNames = eventWriter.GetEventNames();
            eventWriter = null;

            Dispatcher.Invoke(() =>
            {
                UpdateRecordingUI();
            });

            converter = new VideoConverter(filePath + "\\Record" + timestamp, subtitleFiles, subtitleNames);
            timestamp = "";
        }

        private void UpdateRecordingUI()
        {
            if (isRecording)
            {
                RecordButtonImage.Source = new BitmapImage(new Uri("/Resources/stop.png", UriKind.Relative));
                MinDistanceText.IsEnabled = false;
                SettingsButton.IsEnabled = false;
                QuitButton.IsEnabled = false;
                ScreenBox.IsEnabled = false;
                SelectLocationButton.IsEnabled = false;
            } else
            {
                RecordButtonImage.Source = new BitmapImage(new Uri("/Resources/play.png", UriKind.Relative));
                MinDistanceText.IsEnabled = true;
                SettingsButton.IsEnabled = true;
                QuitButton.IsEnabled = true;
                ScreenBox.IsEnabled = true;
                SelectLocationButton.IsEnabled = true;
            }
        }

        private void CleanUp()
        {
            keyboardWatcher.Stop();
            mouseWatcher.Stop();
            clipboardWatcher.Stop();
            applicationWatcher.Stop();
            printWatcher.Stop();
            eventHookFactory.Dispose();

            recorder?.Dispose();
            converter?.Close();
            webServer?.Stop();

            if (isRecording)
            {
                eventWriter.DeleteAllFiles();

                if (File.Exists(filePath + "\\Record" + timestamp + ".avi"))
                {
                    File.Delete(filePath + "\\Record" + timestamp + ".avi");
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            removeScreenBorder();
            e.Cancel = true;
            Hide();
        }
    }
}
