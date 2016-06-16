namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class DebugClient : IDisposable
    {
        private Socket socket;
        private Task connectTask;
        private CancellationTokenSource cancel = new CancellationTokenSource();

        public void Connect(string address, int port)
        {
            if (Uri.CheckHostName(address) == UriHostNameType.Unknown)
            {
                throw new ArgumentException("Invalid address", "address");
            }

            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException("port", "Invalid port");
            }

            this.connectTask = this.ConnectToServer(address, port);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.cancel.Cancel();
                this.cancel.Dispose();
            }
        }

        public event EventHandler<EventArgs> Connected;

        public event EventHandler<LogEventArgs> LogReceived;

        public event EventHandler<TraceEventArgs> TraceReceived;

        public event EventHandler<ProfileEventArgs> ProfileReceived;

        private static MessageJsonConverter<OptionsMessage> optionsConverter = new MessageJsonConverter<OptionsMessage>("options");

        public async Task SendOptions(OptionsMessage options)
        {
            var message = JsonConvert.SerializeObject(options, optionsConverter);
            var buffer = Encoding.ASCII.GetBytes(message);
            await Task.Factory.FromAsync(
                this.socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                this.socket.EndSend);
        }

        private async Task ConnectToServer(string address, int port)
        {
            var ct = this.cancel.Token;
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                using (this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    try
                    {
                        await Task.Factory.FromAsync(
                            this.socket.BeginConnect(address, port, p => { }, null),
                            this.socket.EndConnect);

                        var handler = this.Connected;
                        if (handler != null)
                        {
                            handler(this, EventArgs.Empty);
                        }

                        await Task.Run(() => this.ReceiveMessages());
                    }
                    catch (SocketException)
                    {
                        this.socket.Close();
                        continue;
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Nested usings")]
        private void ReceiveMessages()
        {
            var serializer = new JsonSerializer();

            using (var stream = new NetworkStream(this.socket))
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader) { SupportMultipleContent = true })
            {
                var ct = this.cancel.Token;
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        if (!jsonReader.Read())
                        {
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    var message = JObject.Load(jsonReader);
                    var reader = message.CreateReader();
                    switch (message.Value<string>("type"))
                    {
                        case "log":
                            var logMessage = serializer.Deserialize<LogMessage>(reader);
                            this.OnLogReceived(logMessage);
                            break;

                        case "trace":
                            var traceMessage = serializer.Deserialize<TraceMessage>(reader);
                            this.OnTraceReceived(traceMessage);
                            break;

                        case "profile":
                            var profileMessage = serializer.Deserialize<ProfileMessage>(reader);
                            this.OnProfileReceived(profileMessage);
                            break;

                        default:
                            var errorMessage = new LogMessage { Message = message.ToString() };
                            this.OnLogReceived(errorMessage);
                            break;
                    }
                }
            }
        }

        private void OnLogReceived(LogMessage message)
        {
            var handler = this.LogReceived;
            if (handler != null)
            {
                handler(this, new LogEventArgs { Message = message });
            }
        }

        private void OnTraceReceived(TraceMessage message)
        {
            var handler = this.TraceReceived;
            if (handler != null)
            {
                handler(this, new TraceEventArgs { Message = message });
            }
        }

        private void OnProfileReceived(ProfileMessage message)
        {
            var handler = this.ProfileReceived;
            if (handler != null)
            {
                handler(this, new ProfileEventArgs { Message = message });
            }
        }
    }

    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "EventArgs")]
    public class LogEventArgs : EventArgs
    {
        public LogMessage Message { get; set; }
    }

    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "EventArgs")]
    public class TraceEventArgs : EventArgs
    {
        public TraceMessage Message { get; set; }
    }

    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "EventArgs")]
    public class ProfileEventArgs : EventArgs
    {
        public ProfileMessage Message { get; set; }
    }
}
