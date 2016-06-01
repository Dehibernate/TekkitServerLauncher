using GalaSoft.MvvmLight;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MCSLauncher.Model
{
    /// <summary>
    /// The NetworkListener's main purpose is to allow remotely starting/stopping the Minecraft server.
    /// The current implementation is still a proof of concept and is currently not optimised or secure.
    /// </summary>
    public class NetworkListener : ObservableObject, IDisposable
    {
        private bool _listen;
        private TcpListener _listener;
        private readonly object _lock = new object();
        private readonly ProcessManager _processManager;
        private bool _running;
        private BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

        public NetworkListener(ProcessManager processManager)
        {
            _processManager = processManager;
        }

        /// <summary>
        /// Indicates whether the NetworkListener server is running and processing network requests
        /// </summary>
        public bool IsRunning
        {
            get { return _running; }
            private set { Set(ref _running, value); }
        }

        public void Dispose()
        {
            Stop();
        }

        public async Task Start()
        {
            lock (_lock)
            {
                if (IsRunning) return;

                _listen = true;

                if (_listener == null)
                    _listener = TcpListener.Create(2025);

                _listener.Start();
                IsRunning = true;
            }

            //Connections are currently processed sequentially (non-parallel)
            while (_listen)
            {
                try
                {
                    using (var sock = await _listener.AcceptSocketAsync())
                    using (var rStream = new NetworkStream(sock))
                    using (var wStream = new NetworkStream(sock))
                    using (var reader = new StreamReader(rStream))
                    using (var writer = new StreamWriter(wStream))
                    {
                        writer.AutoFlush = true;
                        await writer.WriteLineAsync("MCSLauncher v0.1");
                        await writer.WriteLineAsync("Minecraft server status: " + (_processManager.IsRunning == true ? "Online" : "Offline"));

                        var str = await reader.ReadLineAsync();

                        switch (str)
                        {
                            case "start":
                                _processManager.StartServer();
                                break;
                            case "stop":
                                _processManager.StopServer();
                                break;
                        }

                        sock.Disconnect(true);
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }

            IsRunning = false;
        }

        public void Stop()
        {
            lock (_lock)
            {
                _listen = false;
                _listener?.Stop();
                _listener = null;
            }
        }
    }
}