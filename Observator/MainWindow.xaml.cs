using EventHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Observator
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string filePath = "";

        public MainWindow()
        {
            InitializeComponent();

            ConfigureEventHandlers();

            this.SelectLocationButton.Click += SelectLocationButton_Click;
        }

        void ConfigureEventHandlers()
        {
            using (var eventHookFactory = new EventHookFactory())
            {
                var keyboardWatcher = eventHookFactory.GetKeyboardWatcher();
                keyboardWatcher.Start();
                keyboardWatcher.OnKeyInput += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.KeyboardEvents.Content = string.Format("Key {0} event of key {1}", e.KeyData.EventType, e.KeyData.Keyname);
                    });
                   
                };

                var mouseWatcher = eventHookFactory.GetMouseWatcher();
                mouseWatcher.Start();
                mouseWatcher.OnMouseInput += (s, e) =>
                {
                    if (e.Message.ToString() == "WM_LBUTTONDOWN")
                    {
                        TakeScreenshot();
                        // left mouse down
                    }
                    else if (e.Message.ToString() == "WM_RBUTTONDOWN")
                    {
                        // right mouse down
                    }

                    Dispatcher.Invoke(() =>
                    {
                        this.MouseEvents.Content = string.Format("Mouse event {0} at point {1},{2}", e.Message.ToString(), e.Point.x, e.Point.y);
                    });

                    
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
                        this.ApplicationEvents.Content = string.Format("Application window of '{0}' with the title '{1}' was {2}", e.ApplicationData.AppName, e.ApplicationData.AppTitle, e.Event);
                    });
                };

                var printWatcher = eventHookFactory.GetPrintWatcher();
                printWatcher.Start();
                printWatcher.OnPrintEvent += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.PrinterEvents.Content = string.Format("Printer '{0}' currently printing {1} pages.", e.EventData.PrinterName, e.EventData.Pages);
                    });
                };
            }
        }

        private void SelectLocationButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                LocationEntry.Text = dialog.SelectedPath;
                filePath = dialog.SelectedPath;
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
