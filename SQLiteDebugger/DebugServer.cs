namespace SQLiteDebugger
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Data;
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
        private List<Socket> clients = new List<Socket>();
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
            }
        }

        private async Task ListenForClients()
        {
            this.listener.Start();
            while (true)
            {
                var socket = await this.listener.AcceptSocketAsync();
                this.clients.Add(socket);
            }
        }
        
        private static MessageJsonConverter<LogMessage> logConverter = new MessageJsonConverter<LogMessage>("log");

        public void SendLog(string message)
        {
            var data = new LogMessage { Database = "db", Time = DateTime.Now, Message = message };
            var json = JsonConvert.SerializeObject(data, logConverter);
            this.clients.ForEach(s => this.Send(s, json));
        }

        private static MessageJsonConverter<TraceMessage> traceConverter = new MessageJsonConverter<TraceMessage>("trace");

        public void SendTrace(string message, int queryId)
        {
            var data = new TraceMessage { Database = "db", Time = DateTime.Now, Id = queryId, Query = message };
            var json = JsonConvert.SerializeObject(data, traceConverter);
            this.clients.ForEach(s => this.Send(s, json));
        }

        private static MessageJsonConverter<ProfileMessage> profileConverter = new MessageJsonConverter<ProfileMessage>("profile");

        public void SendProfile(int queryId, TimeSpan duration)
        {
            var data = new ProfileMessage { Database = "db", Time = DateTime.Now, Id = queryId, Duration = duration };
            var json = JsonConvert.SerializeObject(data, profileConverter);
            this.clients.ForEach(s => this.Send(s, json));
        }

        private void Send(Socket socket, string message)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
            }
            catch (Exception ex)
            {
                if (!(ex is SocketException || ex is ObjectDisposedException))
                {
                    throw;
                }

                socket.Dispose();
                this.clients.Remove(socket);
            }
        }
    }
}
