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

    public class DebugClient
    {
        private BinaryWriter clientWriter;
        private Task connectTask;

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

        public event EventHandler<EventArgs> Connected;

        public event EventHandler<LogEventArgs> LogReceived;

        public event EventHandler<TraceEventArgs> TraceReceived;

        public event EventHandler<ProfileEventArgs> ProfileReceived;

        public void SendOptions(bool plan, bool results, bool pause)
        {
            var data = new OptionsMessage
            {
                Plan = plan, Results = results, Pause = pause
            };

            var json = JsonConvert.SerializeObject(data);
            this.Send(json);
        }

        private void Send(string message)
        {
            var length = Encoding.UTF8.GetByteCount(message);
            var buffer = new byte[4 + length];
            Encoding.UTF8.GetBytes(message, 0, message.Length, buffer, 4);
            BitConverter.GetBytes(length).CopyTo(buffer, 0);

            lock (this.clientWriter)
            {
                try
                {
                    this.clientWriter.Write(buffer);
                }
                catch (Exception ex)
                {
                    if (!(ex is IOException || ex is ObjectDisposedException))
                    {
                        throw;
                    }

                    this.clientWriter.Close();
                }
            }
        }

        private async Task ConnectToServer(string address, int port)
        {
            while (true)
            {
                // disable Nagle algorithm to prevent small commands from waiting for the segment to fill up
                using (var client = new TcpClient() { NoDelay = true })
                {
                    try
                    {
                        await client.ConnectAsync(address, port);
                        this.clientWriter = new BinaryWriter(client.GetStream());

                        var handler = this.Connected;
                        if (handler != null)
                        {
                            handler(this, EventArgs.Empty);
                        }

                        await Task.Run(() => this.ReceiveMessages(client));
                    }
                    catch (SocketException)
                    {
                        continue;
                    }
                }
            }
        }

        private void ReceiveMessages(TcpClient client)
        {
            var serializer = new JsonSerializer();

            using (var reader = new BinaryReader(client.GetStream()))
            {
                while (true)
                {
                    string text;
                    try
                    {
                        var length = reader.ReadInt32();
                        var data = reader.ReadBytes(length);
                        text = Encoding.UTF8.GetString(data);
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    var message = JObject.Parse(text);
                    var jsonReader = message.CreateReader();
                    switch (message.Value<string>("Type"))
                    {
                        case LogMessage.Type:
                            var logMessage = serializer.Deserialize<LogMessage>(jsonReader);
                            this.OnLogReceived(logMessage);
                            break;

                        case TraceMessage.Type:
                            var traceMessage = serializer.Deserialize<TraceMessage>(jsonReader);
                            this.OnTraceReceived(traceMessage);
                            break;

                        case ProfileMessage.Type:
                            var profileMessage = serializer.Deserialize<ProfileMessage>(jsonReader);
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
