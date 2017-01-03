using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
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

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();

            List<Download> arguments = new List<Download>();

            // Get file download info from command line args.
            for (int i = 1; i < args.Length; i += 2)
            {
                arguments.Add(new Download {url = args[i], file = args[i+1]});

                if (File.Exists(args[i + 1]))
                {
                    try
                    {
                        File.Delete(args[i + 1]);
                    }
                    catch
                    {

                    }
                }  
            }

            Update(arguments);
        }

        private async void Update(List<Download> downloads)
        {
            string startFile = null;

            // Download new files
            foreach (Download download in downloads)
            {
                await DownloadFile(download.url, download.file);
                startFile = download.file;
            }

            // Open updated program
            // The last file to be downloaded will be executed
            try
            {
                Process.Start(startFile);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            // Close updater
            Close();
        }

        public async Task DownloadFile(string urlAddress, string fileName)
        {
            using (webClient = new WebClient())
            {
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressBar_ValueChanged);

                // Delete files to update
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                // The variable that will be holding the url address (making sure it starts with http://)
                Uri URL = new Uri(urlAddress);

                // Start the stopwatch which we will be using to calculate the download speed
                sw.Start();

                try
                {
                    // Start downloading the file
                    await webClient.DownloadFileTaskAsync(URL, AppDomain.CurrentDomain.BaseDirectory + fileName );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void ProgressBar_ValueChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // Calculate download speed and output it to labelSpeed.
            labelSpeed.Content = string.Format("{0} kb/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));

            // Update the progressbar percentage only when the value is not the same.
            progressBar.Value = e.ProgressPercentage;

            // Show the percentage on our label.
            labelPerc.Content = e.ProgressPercentage.ToString() + "%";

            // Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
            labelDownloaded.Content = string.Format("{0} MBs / {1} MBs",
                (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));
        }

        // The event that will trigger when the WebClient is completed
        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            // Reset the stopwatch.
            sw.Reset();

            if (e.Cancelled == true)
            {
                MessageBox.Show("Download has been canceled.");
            }
            else
            {
                MessageBox.Show("Download complete!");
            }
        }
    }
}
