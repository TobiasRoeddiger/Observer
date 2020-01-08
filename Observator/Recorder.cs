using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SharpAvi.Output;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Collections;
using System.Collections.Generic;




namespace Observator
{
    public class Recorder
    {
        AviWriter writer;
        RecorderParams Params;
        IAviVideoStream videoStream;
        Thread screenThread;
        ManualResetEvent stopThread = new ManualResetEvent(false);
        bool isMouseTrackingEnabled;

        public Recorder(bool mouseTrackingEnabled, RecorderParams Params)
        {
            isMouseTrackingEnabled = mouseTrackingEnabled;
            this.Params = Params;

            writer = Params.CreateAviWriter();
            videoStream = Params.CreateVideoStream(writer);

            screenThread = new Thread(RecordScreen)
            {
                Name = typeof(Recorder).Name + ".RecordScreen",
                IsBackground = true
            };

            screenThread.Start();
        }

        public void Dispose()
        {
            stopThread.Set();
            screenThread.Join();

            writer.Close();

            stopThread.Dispose();
        }

        void RecordScreen()
        {
            var frameInterval = TimeSpan.FromSeconds(1 / (double)writer.FramesPerSecond);
            var buffer = new byte[Params.Width * Params.Height * 4];
            Task videoWriteTask = null;
            var timeTillNextFrame = TimeSpan.Zero;



            while (!stopThread.WaitOne(timeTillNextFrame))
            {
                var timestamp = DateTime.Now;

                Screenshot(buffer);

                // Wait for the previous frame is written
                videoWriteTask?.Wait();

                // Start asynchronous (encoding and) writing of the new frame
                videoWriteTask = videoStream.WriteFrameAsync(true, buffer, 0, buffer.Length);

                timeTillNextFrame = timestamp + frameInterval - DateTime.Now;
                if (timeTillNextFrame < TimeSpan.Zero)
                    timeTillNextFrame = TimeSpan.Zero;
            }

            // Wait for the last frame is written
            videoWriteTask?.Wait();
        }

        Queue<Point> lastPosition = new Queue<Point>();
        public void Screenshot(byte[] Buffer)
        {
            using (var BMP = new Bitmap(Params.Width, Params.Height))
            {
                using (var g = Graphics.FromImage(BMP))
                {

                    g.CopyFromScreen(Point.Empty, Point.Empty, new Size(Params.Width, Params.Height), CopyPixelOperation.SourceCopy);
                    //if (isMouseTrackingEnabled)
                    //{
                        var mousePosition = System.Windows.Forms.Control.MousePosition;
                        
                        lastPosition.Enqueue(mousePosition);
                        foreach( Point position in lastPosition)
                        {
                            Point savePosition = position;
                            for (int Zaehler = -2; Zaehler <= 2; Zaehler++)
                            {
                                for (int Zaehler2 = -2; Zaehler2 <= 2; Zaehler2++)
                                {
                                    if (savePosition.X + Zaehler >= 0 && savePosition.X + Zaehler < Params.Width
                                        && savePosition.Y + Zaehler2 >= 0 && savePosition.Y + Zaehler2 < Params.Height)
                                    {
                                        BMP.SetPixel(savePosition.X + Zaehler, savePosition.Y + Zaehler2, Color.Red);
                                    }
                                }
                            }
/*
                           for (int Zaehler = 0; Zaehler < 5; Zaehler++)
                          {
                               BMP.SetPixel(savePosition.X, savePosition.Y, Color.Red);
                               savePosition.Y++;
                          }
                            savePosition = lastPosition.Peek();
                            //mousePosition = System.Windows.Forms.Control.MousePosition;
                            for (int Zaehler = 0; Zaehler < 5; Zaehler++)
                            {
                              BMP.SetPixel(savePosition.X, savePosition.Y, Color.Red);
                              savePosition.Y++;
                            }
                           for (int Zaehler = 0; Zaehler < 5; Zaehler++)
                            {
                              BMP.SetPixel(savePosition.X, savePosition.Y, Color.Red);
                              savePosition.X++;
                            }*/
                        }

                        if (lastPosition.Count > 20) 
                            {
                                lastPosition.Dequeue();
                            }
                    //}


                    var bits = BMP.LockBits(new Rectangle(0, 0, Params.Width, Params.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                    Marshal.Copy(bits.Scan0, Buffer, 0, Buffer.Length);
                    BMP.UnlockBits(bits);
                    g.Flush();

                }
            }
        }
    }
}
