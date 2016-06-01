using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MCSLauncher.Model;
using System;
using System.IO;
using System.Windows.Forms;

namespace MCSLauncher.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly NetworkListener _networkListener;

        public MainViewModel()
        {
            if (!IsInDesignMode)
            {
                Uri path;
                Uri.TryCreate(Properties.Settings.Default.ServerPath, UriKind.Absolute, out path);

                ProcessManager = new ProcessManager(path)
                {
                    ShutdownTimeout = Properties.Settings.Default.Timeout 
                
                };

                _networkListener = new NetworkListener(ProcessManager);

                StartNetworkListener = new RelayCommand(async () => await _networkListener.Start(), () => !_networkListener.IsRunning);
                StopNetworkListener = new RelayCommand(_networkListener.Stop, () => _networkListener.IsRunning);

                StartProcess = new RelayCommand(ProcessManager.StartServer, () => ProcessManager?.ServerPath != null && ProcessManager.IsRunning == false);
                StopProcess = new RelayCommand(async () => await ProcessManager.StopServerAsync(), () => ProcessManager?.ServerPath != null && ProcessManager.IsRunning == true);
                BrowsePath = new RelayCommand(_browsePath, () => ProcessManager.IsRunning != true);

                ProcessManager.PropertyChanged += _processManager_PropertyChanged;
                _networkListener.PropertyChanged += _networkListener_PropertyChanged; ;
                System.Windows.Application.Current.Exit += Current_Exit;
            }
        }

        public RelayCommand BrowsePath { get; set; }

        /// <summary>
        /// Path is valid when it extists, and the file it points to also exists
        /// </summary>
        public bool IsPathValid => ProcessManager?.ServerPath != null && File.Exists(ProcessManager.ServerPath.AbsolutePath + "/Tekkit.jar");

        public ProcessManager ProcessManager { get; }
        public RelayCommand StartNetworkListener { get; set; }
        public RelayCommand StartProcess { get; set; }
        public RelayCommand StopNetworkListener { get; set; }
        public RelayCommand StopProcess { get; set; }

        private void _browsePath()
        {
            var dialog = new FolderBrowserDialog()
            {
                SelectedPath = ProcessManager.ServerPath?.ToString(),
                ShowNewFolderButton = true,
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ProcessManager.ServerPath = new Uri(dialog.SelectedPath, UriKind.Absolute);
            }
        }

        private void _networkListener_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_networkListener.IsRunning))
            {
                StartNetworkListener.RaiseCanExecuteChanged();
                StopNetworkListener.RaiseCanExecuteChanged();
            }
        }

        private void _processManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ProcessManager.IsRunning):
                    //Invoke PropertyChanged events on UI thread
                    System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        StartProcess.RaiseCanExecuteChanged();
                        StopProcess.RaiseCanExecuteChanged();
                        BrowsePath.RaiseCanExecuteChanged();

                        RaisePropertyChanged(() => IsPathValid);
                    }));
                    break;

                case nameof(ProcessManager.ServerPath):
                    //Save ServerPath to application settings
                    Properties.Settings.Default.ServerPath = ProcessManager.ServerPath.AbsolutePath;
                    Properties.Settings.Default.Save();

                    //Invoke PropertyChanged events on UI thread
                    System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        StartProcess.RaiseCanExecuteChanged();
                        StopProcess.RaiseCanExecuteChanged();

                        RaisePropertyChanged(() => IsPathValid);
                    }));
                    break;

                case nameof(ProcessManager.ShutdownTimeout):
                    //Save shutdown timeout value to application settings
                    Properties.Settings.Default.Timeout = ProcessManager.ShutdownTimeout;
                    Properties.Settings.Default.Save();
                    break;
            }
        }

        private void Current_Exit(object sender, System.Windows.ExitEventArgs e)
        {
            ProcessManager?.Dispose();
            _networkListener?.Dispose();
        }
    }
}