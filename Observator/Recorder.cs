using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpAvi.Output;

namespace Observator
{
    public class Recorder
    {
        AviWriter writer;
        RecorderParams Params;
        IAviVideoStream videoStream;
        Thread screenThread;
        ManualResetEvent stopThread = new ManualResetEvent(false);
        bool IsPaused = false;

        public Recorder(RecorderParams Params)
        {
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
            if (IsPaused) Resume();

            stopThread.Set();
            screenThread.Join();

            writer.Close();

            stopThread.Dispose();
        }

        public void Pause()
        {
            if (!IsPaused)
            {
                screenThread.Suspend();
                IsPaused = true;
            }
        }

        public void Resume()
        {
            if (IsPaused)
            {
                screenThread.Resume();
                IsPaused = false;
            }
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

                try
                {
                    Screenshot(buffer);
                } 
                catch (Exception e)
                {

                }

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

        public void Screenshot(byte[] Buffer)
        {
            using (var BMP = new Bitmap(Params.Width, Params.Height))
            {
                using (var g = Graphics.FromImage(BMP))
                {
                    g.CopyFromScreen(new Point(Params.CurrentScreen.Bounds.X, Params.CurrentScreen.Bounds.Y), Point.Empty, Params.CurrentScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

                    g.Flush();

                    var bits = BMP.LockBits(new Rectangle(0, 0, Params.Width, Params.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                    Marshal.Copy(bits.Scan0, Buffer, 0, Buffer.Length);
                    BMP.UnlockBits(bits);
                }
            }
        }
    }
}