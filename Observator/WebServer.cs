using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Observator
{
    class WebServer
    {
        private readonly HttpListener listener = new HttpListener();
        EventWriter eventWriter;

        public WebServer(string[] prefixes, EventWriter eventWriter)
        {
            this.eventWriter = eventWriter;

            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            }

            if (prefixes == null || prefixes.Length == 0)
            {
                throw new ArgumentException("URI prefixes are required");
            }

            foreach (var s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            listener.Start();

            Task.Factory.StartNew(async () =>
            {
                while (true) await Listen(listener);
            }, TaskCreationOptions.LongRunning);
        }
        private async Task Listen(HttpListener l)
        {
            try
            {
                var context = await l.GetContextAsync();
                var request = context.Request;

                string input;
                using (var reader = new StreamReader(request.InputStream))
                {
                    input = reader.ReadToEnd();
                }
                var jsonObj = JObject.Parse(input);
                eventWriter.WriteEvent(EventWriter.InputEvent.Url, (string)jsonObj["url"]);

                try
                {
                    var response = "OK";
                    var buf = Encoding.UTF8.GetBytes(response);
                    context.Response.ContentLength64 = buf.Length;
                    context.Response.OutputStream.Write(buf, 0, buf.Length);
                }
                catch
                {
                    // ignored
                }
                finally
                {
                    // always close the stream
                    if (context != null)
                    {
                        context.Response.OutputStream.Close();
                    }
                }
            }
            catch (HttpListenerException)
            {
                
            }
        }

        public void Stop()
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }
    }
}
