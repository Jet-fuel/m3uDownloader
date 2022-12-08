using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Shapes;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using System.Threading.Channels;
using System.Data;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using static System.Net.WebRequestMethods;
using System.ComponentModel;
using Microsoft.VisualBasic;
using static m3uDownloader.MainWindow;
using System.Threading;
using System.Security.Policy;
using System.Runtime.InteropServices;
using System.Collections;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using Media = LibVLCSharp.Shared.Media;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace m3uDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public ObservableCollection<fileStatus> collection = new ObservableCollection<fileStatus>();
        public MainWindow()
        {
            InitializeComponent();
            dg_Status.ItemsSource = collection;
        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {

        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // ini 읽기
                StringBuilder sb_Path = new StringBuilder(255);
                GetPrivateProfileString("DownloaderSetting", "path", "", sb_Path, sb_Path.Capacity, $"{AppDomain.CurrentDomain.BaseDirectory}Setting.ini");
                tb_Path.Text = sb_Path.ToString();
            }            
            catch (Exception ee)
            {

                MessageBox.Show(ee.Message);
            }

        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //ini 쓰기
            try
            {
                WritePrivateProfileString("DownloaderSetting", "path", tb_Path.Text, $"{AppDomain.CurrentDomain.BaseDirectory}Setting.ini");
            }
            catch (Exception ee)
            {

                MessageBox.Show(ee.Message);
            }
        }
        private void btn_Add_Click(object sender, RoutedEventArgs e)
        {
            //파싱해서 배열로 저장
            string[] Urls = tb_Url.Text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            //데이터그리드 추가
            foreach (var item in Urls)
            {
                if (!string.IsNullOrEmpty(item))
                    collection.Add(new fileStatus { name = item, status = "wait" });
            }
            //텍스트 클리어
            tb_Url.Text = "";
        }
        void PlayUri(string uri,string destination)
        {
            try
            {
                if (!string.IsNullOrEmpty(destination))
                {
                    //Sout File Name Setting
                    string FileName = "";
                    try
                    {
                        //FileName = string.Format("{0}.mp4", System.IO.Path.GetFileNameWithoutExtension(uri));
                        //부적절한 파일명 제거
                        FileName = string.Concat($"{System.IO.Path.GetFileNameWithoutExtension(uri)}.mp4".Split(System.IO.Path.GetInvalidFileNameChars()));
                    }
                    catch (Exception)
                    {

                        using (var crypto = new RNGCryptoServiceProvider())
                        {
                            var bits = (16 * 6);
                            var byte_size = ((bits + 7) / 8);
                            var bytesarray = new byte[byte_size];
                            crypto.GetBytes(bytesarray);
                            FileName = $"{Convert.ToBase64String(bytesarray)}.mp4";
                        }
                    }
                    
                    destination = System.IO.Path.Combine(destination, FileName);

                    // Load native libvlc library
                    Core.Initialize();

                    LibVLC libvlc = new LibVLC();
                    MediaPlayer mediaPlayer = new MediaPlayer(libvlc);
                    Media media = new Media(libvlc, new Uri(uri));

                    // Redirect log output to the console(Debug)
                    //libvlc.Log += (sender, e) => Debug.WriteLine($"[{e.Level}] {e.Module}:{e.Message}");
                    //mediaPlayer.EndReached += (sender, e) =>
                    //{
                    //    Debug.WriteLine("Recorded file is located at " + destination);
                    //};

                    media.AddOption(":sout=#file{dst=" + destination + "}");
                    media.AddOption(":sout-keep");

                    // Start recording
                    mediaPlayer.Play(media);

                    //레코딩 완료까지 돌림
                    while (mediaPlayer.State != VLCState.Ended)
                    {
                       // Debug.WriteLine($"Recording in {destination}..");
                    }

                    //레코딩 완료
                    mediaPlayer.Stop();
                    Debug.WriteLine($"Finished {destination}!");

                    if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
                    {

                        Dispatcher.Invoke(() => collection.Where(x => x.name == uri).Select(c => { c.status = "Finished"; return c; }).ToList());
                        Dispatcher.Invoke(() => dg_Status.Items.Refresh());
                    }
                    else
                    {
                        collection.Where(x => x.name == uri).Select(c => { c.status = "Finished"; return c; }).ToList();
                        dg_Status.Items.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                
            }

           

        }
      

        private void btn_Path_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            //dialog.InitialDirectory = "";
            dialog.IsFolderPicker = true;
            dialog.Title = "저장될 폴더를 선택하여 주세요.";
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tb_Path.Text = dialog.FileName;
            }
            

        }

        private async void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            string path = tb_Path.Text;

            foreach (var item in collection.Where(x => x.status == "wait"))
            {
                new Thread(() => PlayUri(item.name, path)).Start();

                item.status = "encoding";
                
            }
            dg_Status.Items.Refresh();

        }
    }
}
