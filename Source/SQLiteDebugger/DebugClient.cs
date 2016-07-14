namespace SQLiteDebugger
{
    using Toolkit;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    public class DebugClient
    {
        private EventAggregator events;

        private BinaryWriter clientWriter;
        private Task connectTask;

        public DebugClient(EventAggregator events)
        {
            this.events = events;
        }

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

        public void SendOptions(bool plan, bool results, bool pause)
        {
            var data = new OptionsMessage
            {
                Plan = plan,
                Results = results,
                Pause = pause
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

                        this.events.Publish<ConnectEvent>(new ConnectEvent());

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
                            this.events.Publish<LogMessage>(logMessage);
                            break;

                        case TraceMessage.Type:
                            var traceMessage = serializer.Deserialize<TraceMessage>(jsonReader);
                            this.events.Publish<TraceMessage>(traceMessage);
                            break;

                        case ProfileMessage.Type:
                            var profileMessage = serializer.Deserialize<ProfileMessage>(jsonReader);
                            this.events.Publish<ProfileMessage>(profileMessage);
                            break;

                        default:
                            var errorMessage = new LogMessage { Message = message.ToString() };
                            this.events.Publish<LogMessage>(errorMessage);
                            break;
                    }
                }
            }
        }
    }
}
