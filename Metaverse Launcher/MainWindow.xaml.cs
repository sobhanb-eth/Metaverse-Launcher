using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Drawing;

namespace Metaverse_Launcher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Start";
                        StatusText.Text = "Welcome to Bioverse";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Retry";
                        StatusText.Text = "Update Failed";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "";
                        StatusText.Text = "Downloading Application:";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "";
                        StatusText.Text = "Downloading Updates:";
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            PlayButton.Visibility = Visibility.Hidden;
            DownloadProgressBar.Visibility = Visibility.Visible;
            BorderProgress.Visibility = Visibility.Visible;

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Build", "Bioverse.exe");
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("Version File Link"));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                        
                        DownloadProgressBar.Visibility = Visibility.Hidden;
                        BorderProgress.Visibility = Visibility.Hidden;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    PlayButton.Visibility = Visibility.Visible;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
                if (Status == LauncherStatus.ready || Status == LauncherStatus.failed)
                {
                    DownloadProgress.Text = "";
                    if (Status == LauncherStatus.ready)
                    {
                        DownloadProgressBar.Visibility = Visibility.Hidden;
                        BorderProgress.Visibility = Visibility.Hidden;
                        PlayButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        DownloadProgressBar.Visibility = Visibility.Visible;
                        BorderProgress.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("Version File Link"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("Game File Link/Build.zip"), gameZip, _onlineVersion);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(WebClient_DownloadProgressChanged);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                DownloadProgressBar.Visibility = Visibility.Visible;
                BorderProgress.Visibility = Visibility.Visible;
                PlayButton.Visibility = Visibility.Visible;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            DownloadProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());

            DownloadProgress.Text = e.ProgressPercentage.ToString() + "%";
            if (DownloadProgress.Text == "100%")
            {
                DownloadProgress.Text = "Completed!";
                DownloadProgressBar.Visibility = Visibility.Hidden;
                BorderProgress.Visibility = Visibility.Hidden;
                PlayButton.Visibility = Visibility.Visible;
                DownloadProgress.Text = "";
            }
           
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                ZipFile.ExtractToDirectory(gameZip, rootPath, true);
                if (File.Exists(gameZip))
                {
                    File.Delete(gameZip);
                    File.WriteAllText(versionFile, onlineVersion);

                    VersionText.Text = onlineVersion;
                    Status = LauncherStatus.ready;
                    DownloadProgressBar.Visibility = Visibility.Hidden;
                    BorderProgress.Visibility = Visibility.Hidden;
                    PlayButton.Visibility = Visibility.Visible;

                }
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                PlayButton.Visibility = Visibility.Visible;
                VersionText.Text = "Error Fetching";
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                DownloadProgressBar.Visibility = Visibility.Hidden;
                BorderProgress.Visibility = Visibility.Hidden;
                PlayButton.Visibility = Visibility.Visible;
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }

        private void PlayButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            PlayButton.Opacity = 100;
        }
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
