namespace SQLiteDebugger
{
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
        
        public void SendMessage(string message)
        {
            var data = MessageWithTimeStamp("*LG*", message);
            this.clients.ForEach(s => this.Send(s, data));
        }

        public void SendQueryStart(string message, int queryId)
        {
            var data = MessageWithTimeStamp("*QS*", message, queryId);
            this.clients.ForEach(s => this.Send(s, data));
        }

        public void SendQueryEnd(string message, int queryId)
        {
            var data = MessageWithTimeStamp("*QE*", message, queryId);
            this.clients.ForEach(s => this.Send(s, data));
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

        private static string MessageWithTimeStamp(string prefix, string message, int id = 0)
        {
            string messageWithTimeStamp = null;
            if (id > 0)
            {
                messageWithTimeStamp = string.Format(CultureInfo.InvariantCulture, "{0}#{1}#{2},{3}", prefix, id, DateTime.Now.Ticks, message);
            }
            else
            {
                messageWithTimeStamp = string.Format(CultureInfo.InvariantCulture, "{0}{1},{2}", prefix, DateTime.Now.Ticks, message);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0},{1}", messageWithTimeStamp.Length, messageWithTimeStamp);
        }
    }
}
