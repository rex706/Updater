using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    // Download class to hold the urls and file names. 
    public class Download
    {
        public string url { get; set; }

        public string file { get; set; }
    }

    public partial class MainWindow : Window
    {
        private WebClient webClient;               // Our WebClient that will be doing the downloading for us.
        private Stopwatch sw = new Stopwatch();    // The stopwatch which we will be using to calculate the download speed.

        public MainWindow()
        {
            InitializeComponent();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            Loaded += MainWindow_LoadedAsync;
        }

        private async void MainWindow_LoadedAsync(object sender, RoutedEventArgs e)
        {
            Title = "Updater " + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            string[] args = Environment.GetCommandLineArgs();

            string manifestUrl = null;

            foreach(string arg in args)
            {
                if (Uri.TryCreate(arg, UriKind.Absolute, out _) && !File.Exists(arg))
                {
                    manifestUrl = arg;
                    break;
                }
            }

            if (manifestUrl == null)
            {
                MessageBox.Show("No valid argument found.\nPlease provide update manifest.");
                Close();
                return;
            }

            ConsoleBox.AppendText("Reading update manifest . . .\n");

            List<Download> downloads = new List<Download>();

            // Open the text file using a stream reader.
            using (WebClient client = new WebClient())
            {
                // Open the text file using a stream reader.
                using (Stream stream = await client.OpenReadTaskAsync(manifestUrl))
                {
                    StreamReader reader = new StreamReader(stream);

                    // Initialize variables.
                    Version latest = null;
                    string executable = null;

                    try
                    {
                        // First two expected items in manifest will be the new version and the starting executable.
                        string v = await reader.ReadLineAsync();
                        latest = Version.Parse(v);
                        executable = await reader.ReadLineAsync();
                    }
                    catch (Exception m)
                    {
                        MessageBox.Show("Error parsing new version number or launch executable.\n\n" + m.Message + "\n\nManifest: " + manifestUrl);
                        Close();
                        return;
                    }

                    // Load manifest urls and file names.
                    while (!reader.EndOfStream)
                    {
                        try
                        {
                            // Expected in pairs of two.
                            string downloadUrl = await reader.ReadLineAsync();
                            string downloadFile = await reader.ReadLineAsync();

                            if (downloadUrl == null || downloadFile == null)
                            {
                                MessageBox.Show("Uneven update manifest.\nPlease review format.");
                                Close();
                                return;
                            }

                            // If a new version of the updater is being acquired, rename it to be handled in UpdateCheck.cs
                            if (downloadFile == "Updater.exe")
                            {
                                downloads.Add(new Download { url = downloadUrl, file = "Updater_new.exe" });
                            }
                            else
                            {
                                downloads.Add(new Download { url = downloadUrl, file = downloadFile });
                            }
                        }
                        catch (Exception m)
                        {
                            MessageBox.Show("Uneven update manifest.\nPlease review format.");
                            Close();
                            return;
                        }
                    }
                    if (downloads.Count == 0)
                    {
                        MessageBox.Show("No download arguments found in manifest.");
                        Close();
                        return;
                    }

                    Update(executable, downloads);
                }
            }
        }

        private async void Update(string executable, List<Download> downloads)
        {
            // Download new files
            foreach (Download download in downloads)
            {
                await DownloadFile(download.url, download.file);
            }

            ConsoleBox.AppendText("Update complete!\n");
            //MessageBox.Show("Update complete!");

            // First argument is the program to be opened when the update is complete.
            if (executable != null && executable.Length > 1 && executable.Contains(".exe"))
            {
                ConsoleBox.AppendText("Attempting to start" + executable + " . . .\n");

                try
                {
                    Process.Start(executable);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            
            // Close updater.
            Close();
        }

        public async Task DownloadFile(string urlAddress, string fileName)
        {
            using (webClient = new WebClient())
            {
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;

                string path = AppDomain.CurrentDomain.BaseDirectory + fileName;

                // Delete files to update.
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                // The variable that will be holding the url address (making sure it starts with http://).
                Uri URL = new Uri(urlAddress);

                // Start the stopwatch which will be used to calculate the download speed.
                sw.Start();
                ConsoleBox.AppendText("Downloading '" + fileName + "' . . .\n");

                try
                {
                    // Start downloading the file.
                    await webClient.DownloadFileTaskAsync(URL, path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar_ValueChanged(sender, e);
        }

        private void ProgressBar_ValueChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Calculate download speed and output it to labelSpeed.
                labelSpeed.Content = string.Format("{0} kb/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));

                // Update the progressbar percentage only when the value is not the same.
                progressBar.Value = e.ProgressPercentage;

                // Show the percentage on our label.
                labelPerc.Content = e.ProgressPercentage.ToString() + "%";

                // Update the label with how much data has been downloaded so far and the total size of the file we are currently downloading.
                labelDownloaded.Content = string.Format("{0} MBs / {1} MBs", (e.BytesReceived / 1024d / 1024d).ToString("0.00"), (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));
            });
        }

        // The event that will trigger when the WebClient is completed.
        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            // Reset the stopwatch.
            sw.Reset();

            if (e.Cancelled == true)
            {
                ConsoleBox.AppendText("Download has been cancelled.\n");
                MessageBox.Show("Download has been canceled.");
            }
        }
    }
}
