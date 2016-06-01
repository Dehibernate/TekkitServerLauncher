using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MCSLauncher.Model
{
    public class ProcessManager : ObservableObject, IDisposable
    {
        private bool _initialised;
        private bool? _isRunning;
        private string _output;
        private int _players;
        private Process _process;
        private Uri _serverPath;
        private int _shutdownTimeout;
        private Timer _shutdownTimer;

        public ProcessManager(Uri serverPath)
        {
            ServerPath = serverPath;

            if (ServerPath != null && File.Exists(ServerPath.AbsolutePath + "/Tekkit.jar"))
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Indicates whether the Minecraft server has started and players can join
        /// </summary>
        public bool Initialised
        {
            get { return _initialised; }
            set { Set(ref _initialised, value); }
        }

        /// <summary>
        /// Indicates whether the ProcessManager is Started/Stopped
        /// </summary>
        public bool? IsRunning
        {
            get { return _isRunning; }
            private set { Set(ref _isRunning, value); }
        }

        /// <summary>
        /// The output from the Minecraft server console as a multi-line string
        /// </summary>
        public string Output
        {
            get { return _output; }
            private set { Set(ref _output, value); }
        }

        /// <summary>
        /// The path to the server executable (JAR) on the local machine
        /// </summary>
        public Uri ServerPath
        {
            get { return _serverPath; }
            set
            {
                if (Set(ref _serverPath, value))
                {
                    if (ServerPath == null)
                        IsRunning = null;
                    else
                        IsRunning = IsRunning ?? false;
                }
            }
        }

        /// <summary>
        /// The period to wait before shutting down the server while the server is idle and empty
        /// </summary>
        public int ShutdownTimeout
        {
            get { return _shutdownTimeout; }
            set { Set(ref _shutdownTimeout, Math.Min(Math.Max(value, 1), 1440)); }
        }

        public void Dispose()
        {
            StopServer();
        }

        public void StartServer()
        {
            if (_process == null && ServerPath != null)
            {
                //Clear the output before starting new server
                Output = string.Empty;
                _players = 0;

                var processInfo = new ProcessStartInfo("java", "-jar Tekkit.jar -nojline")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true
                };
                processInfo.WorkingDirectory = _serverPath.AbsolutePath;

                _process = Process.Start(processInfo);

                if (_process != null && !_process.HasExited)
                {
                    _process.ErrorDataReceived += _process_OutputDataReceived;
                    _process.OutputDataReceived += _process_OutputDataReceived;
                    _process.BeginErrorReadLine();
                    _process.BeginOutputReadLine();

                    IsRunning = true;
                }
                else if (_process != null && _process.HasExited)
                {
                    StopServer();
                }
            }
        }

        public void StopServer()
        {
            _stopTimer();

            if (_process != null)
            {
                IsRunning = null;

                _process.StandardInput.Write("stop\n");

                if ((!Initialised || !_process.WaitForExit(2500)) && !_process.HasExited)
                {
                    _process.Kill();
                }

                _process.Close();

                _process.ErrorDataReceived -= _process_OutputDataReceived;
                _process.OutputDataReceived -= _process_OutputDataReceived;

                _process.Dispose();
                _process = null;

                Initialised = false;
                IsRunning = false;
            }
        }

        public async Task StopServerAsync()
        {
            await Task.Run(() => StopServer());
        }

        private void _process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;

            if (!Initialised)
            {
                //Detect when the server has started and start the shutdown timer
                var str = e.Data.TrimStart("0123456789-: ".ToCharArray());
                if (str.StartsWith("[INFO] [Minecraft-Server] Done (") && str.EndsWith("s)! For help, type \"help\" or \"?\""))
                {
                    Initialised = true;
                    _startTimer();
                }
            }
            else
            {
                //Detect players connecting/disconnecting and start/stop the shutdown timer accordingly
                IEnumerable<char> p = e.Data;
                if (e.Data.EndsWith("joined the game"))
                {
                    if (++_players == 1)
                    {
                        _stopTimer();
                    }
                }
                else if (e.Data.EndsWith("left the game"))
                {
                    if (--_players == 0)
                    {
                        _startTimer();
                    }
                }
            }

            Output += e.Data + "\n";
        }

        private void _startTimer()
        {
            _shutdownTimer = new Timer((state) => StopServer(), null, ShutdownTimeout * 60 * 1000, 0);
        }

        private void _stopTimer()
        {
            _shutdownTimer?.Dispose();
            _shutdownTimer = null;
        }
    }
}