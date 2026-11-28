using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlanetHarvest.Core
{
    // Exactly one guest. TCP frames preserve the order of snapshots, edits and commands.
    public sealed class CrewConnection : IDisposable
    {
        private const int MaxBytes = 48 * 1024 * 1024;
        private TcpListener listener;
        private TcpClient socket;
        private Task connecting, writing;
        private float deadline;
        private readonly Queue<byte[]> outgoing = new Queue<byte[]>();
        private ConcurrentQueue<string> incoming;
        private Task reading;
        private NetworkStream stream;
        public bool Hosting => listener != null;
        public bool Joined => stream != null && !Hosting;
        public bool Connected => stream != null;
        public bool Busy => connecting != null;
        public event Action Opened, Closed;
        public event Action<string> Received;

        public void Host(int port)
        {
            Dispose();
            var server = new TcpListener(IPAddress.Any, port);
            server.Start(1);
            listener = server;
        }

        public void Join(string address, int port, float now)
        {
            Dispose();
            socket = new TcpClient();
            connecting = socket.ConnectAsync(address, port);
            deadline = now + 8;
        }

        public void Tick(float now)
        {
            if (listener != null && listener.Pending())
            {
                var candidate = listener.AcceptTcpClient();
                if (Connected) candidate.Close();
                else { socket = candidate; Open(); }
            }
            if (connecting != null)
            {
                if (connecting.IsCompleted)
                {
                    connecting.GetAwaiter().GetResult();
                    connecting = null;
                    Open();
                }
                else if (now > deadline) throw new IOException("Zeitüberschreitung beim Beitritt.");
            }
            if (stream == null) return;
            for (int i = 0; i < 64 && incoming.TryDequeue(out var json); i++) Received?.Invoke(json);
            if (reading.IsCompleted && incoming.IsEmpty)
            {
                reading.GetAwaiter().GetResult();
                DropPeer();
                return;
            }
            if (writing != null && writing.IsCompleted)
            {
                writing.GetAwaiter().GetResult();
                writing = null;
            }
            if (writing == null && outgoing.Count > 0)
            {
                byte[] frame = outgoing.Dequeue();
                writing = stream.WriteAsync(frame, 0, frame.Length);
            }
        }

        private void Open()
        {
            socket.NoDelay = true;
            stream = socket.GetStream();
            incoming = new ConcurrentQueue<string>();
            reading = ReadFrames(stream, incoming);
            Opened?.Invoke();
        }

        private static async Task ReadFrames(NetworkStream source, ConcurrentQueue<string> queue)
        {
            byte[] header = new byte[4];
            while (await ReadExactly(source, header).ConfigureAwait(false))
            {
                int length = BitConverter.ToInt32(header, 0);
                if (length < 1 || length > MaxBytes || queue.Count >= 128) throw new InvalidDataException("Ungültiges Crew-Paket.");
                byte[] bytes = new byte[length];
                if (!await ReadExactly(source, bytes).ConfigureAwait(false)) throw new EndOfStreamException();
                queue.Enqueue(Encoding.UTF8.GetString(bytes));
            }
        }

        private static async Task<bool> ReadExactly(NetworkStream source, byte[] bytes)
        {
            int offset = 0;
            while (offset < bytes.Length)
            {
                int count = await source.ReadAsync(bytes, offset, bytes.Length - offset).ConfigureAwait(false);
                if (count == 0)
                {
                    if (offset != 0) throw new EndOfStreamException();
                    return false;
                }
                offset += count;
            }
            return true;
        }

        public void Send(string json)
        {
            if (!Connected) return;
            int count = Encoding.UTF8.GetByteCount(json);
            if (count > MaxBytes || outgoing.Count >= 128) throw new IOException("Crew-Verbindung überlastet.");
            byte[] frame = new byte[count + 4];
            Buffer.BlockCopy(BitConverter.GetBytes(count), 0, frame, 0, 4);
            Encoding.UTF8.GetBytes(json, 0, json.Length, frame, 4);
            outgoing.Enqueue(frame);
        }

        public void DropPeer()
        {
            bool existed = socket != null;
            socket?.Close();
            socket = null;
            stream = null;
            connecting = writing = reading = null;
            outgoing.Clear();
            if (existed) Closed?.Invoke();
        }

        public void Dispose()
        {
            listener?.Stop();
            listener = null;
            DropPeer();
        }
    }
}
