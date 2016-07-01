namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    public class DebugServer : IDebugTraceSender, IDisposable
    {
        private TcpListener listener;
        private Task listenTask;
        private List<BinaryWriter> clients = new List<BinaryWriter>();

        private StatementInterceptor interceptor;

        public void Listen(int port)
        {
            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException("port", "Invalid port");
            }

            this.listener = new TcpListener(IPAddress.Any, port);
            this.listenTask = this.ListenForClients();

            this.interceptor = new StatementInterceptor(this);
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
                this.listener.Stop();
                lock (this.clients)
                {
                    foreach (var client in this.clients)
                    {
                        client.Close();
                    }
                }
            }
        }

        private async Task ListenForClients()
        {
            this.listener.Start();
            while (true)
            {
                var client = await this.listener.AcceptTcpClientAsync();
                client.NoDelay = true;

                var clientTask = Task.Run(() => this.ReceiveMessages(client));

                var writer = new BinaryWriter(client.GetStream());
                lock (this.clients)
                {
                    this.clients.Add(writer);
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
                        case OptionsMessage.Type:
                            var options = serializer.Deserialize<OptionsMessage>(jsonReader);
                            this.interceptor.CollectPlan = options.Plan;
                            this.interceptor.CollectResults = options.Results;
                            this.interceptor.Pause = options.Pause;
                            break;

                        case QueryMessage.Type:
                            var query = serializer.Deserialize<QueryMessage>(jsonReader);
                            this.interceptor.Exec(query.Connection, query.Filename, query.Query);
                            break;

                        case DebugMessage.Type:
                            var debug = serializer.Deserialize<DebugMessage>(jsonReader);
                            switch (debug.Action)
                            {
                                case DebugAction.Step:
                                    this.interceptor.Step();
                                    break;
                            }

                            break;
                    }
                }
            }
        }
        
        public void SendLog(string message)
        {
            var data = new LogMessage
            {
                Time = DateTime.Now, Message = message
            };

            var json = JsonConvert.SerializeObject(data);
            this.Send(json);
        }

        public void SendOpen(int db, string path)
        {
            var data = new OpenMessage
            {
                Id = db, Filename = path
            };

            var json = JsonConvert.SerializeObject(data);
            this.Send(json);
        }

        public void SendClose(int db)
        {
            var data = new CloseMessage
            {
                Id = db
            };

            var json = JsonConvert.SerializeObject(data);
            this.Send(json);
        }

        public void SendTrace(int id, int db, string query, string plan = null)
        {
            var data = new TraceMessage
            {
                Time = DateTime.Now, Id = id, Connection = db, Query = query, Plan = plan
            };

            var json = JsonConvert.SerializeObject(data);
            this.Send(json);
        }

        public void SendProfile(int id, TimeSpan duration, DataTable results)
        {
            var data = new ProfileMessage
            {
                Time = DateTime.Now, Id = id, Duration = duration, Results = results
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

            lock (this.clients)
            {
                for (var i = 0; i < this.clients.Count; i++)
                {
                    var client = this.clients[i];
                    try
                    {
                        client.Write(buffer);
                    }
                    catch (Exception ex)
                    {
                        if (!(ex is IOException || ex is ObjectDisposedException))
                        {
                            throw;
                        }

                        client.Close();
                        this.clients.RemoveAt(i);
                        i -= 1;
                    }
                }
            }
        }
    }
}
