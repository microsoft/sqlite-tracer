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
    using System.Threading;
    using System.Threading.Tasks;

    public class DebugServer : IDebugTraceSender, IDisposable
    {
        private TcpListener listener;
        private Task listenTask;
        private List<Tuple<Socket, CancellationTokenSource, Task>> clients = new List<Tuple<Socket, CancellationTokenSource, Task>>();
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
                foreach (var client in this.clients)
                {
                    client.Item2.Cancel();
                    client.Item2.Dispose();

                    client.Item1.Close();
                    client.Item1.Dispose();
                }
            }
        }

        private async Task ListenForClients()
        {
            this.listener.Start();
            while (true)
            {
                var socket = await this.listener.AcceptSocketAsync();
                var cancel = new CancellationTokenSource();
                this.clients.Add(Tuple.Create(
                    socket,
                    cancel,
                    Task.Run(() => this.ReceiveMessages(socket, cancel.Token))));
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Nested usings")]
        private void ReceiveMessages(Socket client, CancellationToken ct)
        {
            var serializer = new JsonSerializer();
            using (var stream = new NetworkStream(client))
            using (var streamReader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(streamReader) { SupportMultipleContent = true })
            {
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
                    switch (message.Value<string>("Type"))
                    {
                        case OptionsMessage.Type:
                            var options = serializer.Deserialize<OptionsMessage>(reader);
                            this.interceptor.CollectPlan = options.Plan;
                            this.interceptor.CollectResults = options.Results;
                            break;
                    }
                }
            }
        }
        
        public void SendLog(string message)
        {
            var data = new LogMessage
            {
                Database = "db", Time = DateTime.Now,
                Message = message
            };

            var json = JsonConvert.SerializeObject(data);
            this.clients.ForEach(s => this.Send(s, json));
        }

        public void SendTrace(int id, string query, string plan = null)
        {
            var data = new TraceMessage
            {
                Database = "db", Time = DateTime.Now, Id = id,
                Query = query, Plan = plan
            };

            var json = JsonConvert.SerializeObject(data);
            this.clients.ForEach(s => this.Send(s, json));
        }

        public void SendProfile(int id, TimeSpan duration, DataTable results)
        {
            var data = new ProfileMessage
            {
                Database = "db", Time = DateTime.Now, Id = id,
                Duration = duration, Results = results
            };

            var json = JsonConvert.SerializeObject(data);
            this.clients.ForEach(s => this.Send(s, json));
        }

        private void Send(Tuple<Socket, CancellationTokenSource, Task> client, string message)
        {
            var socket = client.Item1;
            try
            {
                var buffer = Encoding.ASCII.GetBytes(message);
                socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException || ex is ObjectDisposedException))
                {
                    throw;
                }

                socket.Dispose();
                this.clients.Remove(client);
            }
        }
    }
}
