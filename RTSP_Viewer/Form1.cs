using SDS.Utilities.IniFiles;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vlc.DotNet.Forms;
using RTSP_Viewer.Classes;

namespace RTSP_Viewer
{
    public partial class Form1 : Form
    {
        private const int NumberOfViews = 4;
        private const int ViewPadding = 1;

        VlcControl[] myVlcControl = new VlcControl[NumberOfViews];
        OpcUaClient tagClient;
        IniFile MyIni = new IniFile();
        TextBox uri = new TextBox();
        ComboBox cbxViewSelect = new ComboBox();

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.FormClosing += Form1_FormClosing;
            this.KeyDown += Form1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupVlc();

            uri.Text = "rtsp://127.0.0.1:554/rtsp_tunnel?h26x=4&line=1&inst=1";
            uri.Location = new Point(10, this.Height - 60); // myVlcControl[0].Bottom + 5 + playBtn.Height + 5);
            uri.Width = 600;
            uri.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            this.Controls.Add(uri);

            Button playBtn = new Button();
            playBtn.Text = "Connect";
            playBtn.Location = new Point(10, uri.Top - uri.Height - 10);
            playBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            playBtn.Click += PlayBtn_Click;
            this.Controls.Add(playBtn);

            Button stopBtn = new Button();
            stopBtn.Text = "Disconnect";
            stopBtn.Location = new Point(playBtn.Right + 20, uri.Top - uri.Height - 10);
            stopBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            stopBtn.Click += StopBtn_Click;
            this.Controls.Add(stopBtn);

            cbxViewSelect.Location = new Point(stopBtn.Right + 20, stopBtn.Top);
            cbxViewSelect.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            cbxViewSelect.Width = 100;
            cbxViewSelect.Height = playBtn.Height;
            //cbxViewSelect.Text = "Select Viewer #";
            for (int i = 0; i < NumberOfViews; i++)
            {
                cbxViewSelect.Items.Add("Viewer " + i);
            }

            this.Controls.Add(cbxViewSelect);
            cbxViewSelect.SelectedIndex = 0;
            //viewSelect.Location = new Point()

            Button btnLoadLast = new Button();
            btnLoadLast.Text = "Load Last";
            btnLoadLast.Location = new Point(cbxViewSelect.Right + 20, uri.Top - uri.Height - 10);
            btnLoadLast.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            btnLoadLast.Click += BtnLoadLast_Click; ;
            this.Controls.Add(btnLoadLast);

            foreach (VlcControl vc in myVlcControl)
            {
                this.Controls.Add(vc);
            }

            this.Padding = new Padding(5);

            this.SizeChanged += Form1_ResizeEnd;

            tagClient = new OpcUaClient(CameraCallup);

            // OPC server and path to subscribe to
            string endPointURL = "opc.tcp://admin:admin@127.0.0.1:4840/freeopcua/server/";
            string tagPath = "/0:Tags";
            tagClient.Connect(endPointURL, tagPath);
        }

        private void BtnLoadLast_Click(object sender, EventArgs e)
        {
            loadLastStream();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (VlcControl vc in myVlcControl)
            {
                vc.Stop();
            }

            tagClient.Disconnect();
        }

        private void SetupVlc()
        {
            for (int i = 0; i < NumberOfViews; i++)
            {
                myVlcControl[i] = new VlcControl();
                ((System.ComponentModel.ISupportInitialize)(myVlcControl[i])).BeginInit();

                myVlcControl[i].VlcLibDirectory = GetVlcLibLocation();
                myVlcControl[i].VlcMediaplayerOptions = new string[] { ":network-caching=20" };
                // Standalone player
                //Vlc.DotNet.Core.VlcMediaPlayer mp = new Vlc.DotNet.Core.VlcMediaPlayer(VlCLibDirectory);
                //mp.SetMedia(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
                //mp.Play();

                myVlcControl[i].Location = new Point(0, 0);
                //myVlcControl[i].Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom);
                myVlcControl[i].Name = string.Format("VLC Viewer {0}", i);
                myVlcControl[i].Rate = (float)0.0;
                myVlcControl[i].BackColor = Color.Gray;
                myVlcControl[i].TabIndex = i;

                // Events
                myVlcControl[i].Playing += OnVlcPlaying;
                myVlcControl[i].EncounteredError += MyVlcControl_EncounteredError;
                myVlcControl[i].LengthChanged += MyVlcControl_LengthChanged;
                myVlcControl[i].Buffering += Form1_Buffering;

                // Had to add this line to make work
                ((System.ComponentModel.ISupportInitialize)(myVlcControl[i])).EndInit();
            }

            setSizes();
        }

        /// <summary>
        /// Set the size/postion of each VLC control based on the total number
        /// </summary>
        public void setSizes()
        {
            Point[] displayPoint = new Point[NumberOfViews];
            Size[] displaySize = new Size[NumberOfViews];
            
            int dim = (int)Math.Round(Math.Sqrt(NumberOfViews));
            var sz = Screen.FromControl(this).Bounds;

            // Set the control sizes to fit the set resolution
            //int width = Screen.FromControl(this).Bounds.Size.Width / dim;
            //int height = Screen.FromControl(this).Bounds.Size.Height / dim;
            int width = this.Bounds.Size.Width / dim;
            int height = this.Bounds.Size.Height / dim;
            
            for (int j = 0; j < dim; j++)
            {
                for (int i = 0; i < dim; i++)
                {
                    displayPoint[dim * j + i] = new Point(width * i, height * j);
                    displaySize[dim * j + i] = new Size(width - ViewPadding, height - ViewPadding);
                }
            }

            for (int i = 0; i < NumberOfViews; i++)
            {
                myVlcControl[i].Location = displayPoint[i];
                myVlcControl[i].Size = displaySize[i];
            }
        }

        /// <summary>
        /// Get the VLC install location (to reference the required DLLs and plugin folder)
        /// </summary>
        /// <returns>VLC install directory</returns>
        private DirectoryInfo GetVlcLibLocation()
        {
            // Check both potential normal install locations
            DirectoryInfo vlcLibDirectory = new DirectoryInfo("C:\\Program Files\\VideoLAN\\VLC");
            DirectoryInfo vlcLibDirectoryX = new DirectoryInfo("C:\\Program Files (x86)\\VideoLAN\\VLC");

            if (vlcLibDirectory.Exists)
            {
                return vlcLibDirectory;
            } else if (vlcLibDirectoryX.Exists)
            {
                return vlcLibDirectoryX;
            } else
            {
                MessageBox.Show(this, "VLC install folder not found.  Libraries cannot be loaded.\nApplication will now close.", "Libraries not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                throw new DirectoryNotFoundException("VLC install folder not found.  Libraries cannot be loaded.");
            }
        }

        private void loadLastStream()
        {
            for (int i = 0; i < NumberOfViews; i++)
            {
                try
                {
                    var uri = MyIni.Read("lastURI", "Viewer_" + i);
                    if (uri != null & uri != "")
                    {
                        myVlcControl[i].Play(new Uri(uri), "");
                    }
                }
                catch
                {
                    Console.WriteLine("No lastURI entry found for Viewer_" + i);
                }
                
            }
        }

        private void PlayBtn_Click(object sender, EventArgs e)
        {
            CameraCallup(this.uri.Text, cbxViewSelect.SelectedIndex);
        }

        /// <summary>
        /// Open the provided URI on the provide VLC position
        /// </summary>
        /// <param name="URI">URI to open</param>
        /// <param name="ViewerNum">VLC control to display video on</param>
        private void CameraCallup(string URI, int ViewerNum)
        {
            Console.WriteLine(string.Format("Camera callup for view {0} [{1}]", ViewerNum, URI));
            if (ViewerNum >= 0)
            {
                Debug.Print(myVlcControl[ViewerNum].State.ToString());
                //myVlcControl.Play(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
                myVlcControl[ViewerNum].Play(new Uri(URI), "");
                Debug.Print(myVlcControl[ViewerNum].State.ToString());
                myVlcControl[ViewerNum].UseWaitCursor = true;

                MyIni.Write("lastURI", this.uri.Text, "Viewer_" + ViewerNum);
            }
        }

        private void StopBtn_Click(object sender, EventArgs e)
        {
            int viewerNum = cbxViewSelect.SelectedIndex;
            if (viewerNum >= 0)
            {
                myVlcControl[viewerNum].Stop();
            }
        }

        private void MyVlcControl_LengthChanged(object sender, Vlc.DotNet.Core.VlcMediaPlayerLengthChangedEventArgs e)
        {
            Console.WriteLine("Length changed");
        }

        private void MyVlcControl_EncounteredError(object sender, Vlc.DotNet.Core.VlcMediaPlayerEncounteredErrorEventArgs e)
        {
            VlcControl vlc = (VlcControl)sender;
            MessageBox.Show(string.Format("Error encountered on '{0}':\n{1}", vlc.Name, e.ToString()), "VLC Control Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            vlc.UseWaitCursor = false;
        }

        private void OnVlcPlaying(object sender, Vlc.DotNet.Core.VlcMediaPlayerPlayingEventArgs e)
        {
            VlcControl vlc = (VlcControl)sender;
            vlc.UseWaitCursor = false;
            //myLblState.InvokeIfRequired(l => l.Text = "Playing");

            //myLblAudioCodec.InvokeIfRequired(l => l.Text = "Codec: ");
            //myLblAudioChannels.InvokeIfRequired(l => l.Text = "Channels: ");
            //myLblAudioRate.InvokeIfRequired(l => l.Text = "Rate: ");
            //myLblVideoCodec.InvokeIfRequired(l => l.Text = "Codec: ");
            //myLblVideoHeight.InvokeIfRequired(l => l.Text = "Height: ");
            //myLblVideoWidth.InvokeIfRequired(l => l.Text = "Width: ");

            //var mediaInformations = myVlcControl.GetCurrentMedia().TracksInformations;
            //foreach (var mediaInformation in mediaInformations)
            //{
            //    if (mediaInformation.Type == Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Audio)
            //    {
            //        Console.WriteLine(string.Format("Audio info - Codec: {0}, Channels: {1}, Rate: {2}", mediaInformation.CodecName, mediaInformation.Audio.Channels, mediaInformation.Audio.Rate));
            //        //myLblAudioCodec.InvokeIfRequired(l => l.Text += mediaInformation.CodecName);
            //    }
            //    else if (mediaInformation.Type == Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Video)
            //    {
            //        Console.WriteLine(string.Format("Video info - Codec: {0}, Height: {1}, Width: {2}", mediaInformation.CodecName, mediaInformation.Video.Height, mediaInformation.Video.Width));
            //        //        //myLblVideoCodec.InvokeIfRequired(l => l.Text += mediaInformation.CodecName);
            //    }
            //}
        }

        private void Form1_Buffering(object sender, Vlc.DotNet.Core.VlcMediaPlayerBufferingEventArgs e)
        {
            VlcControl vlc = (VlcControl)sender;
            //MessageBox.Show(string.Format("'{0}' buffering:\n{1}", vlc.Name, e.ToString()), "VLC Control Error", MessageBoxButtons.OK, MessageBoxIcon.Info);
            Console.WriteLine(string.Format("{0}\tBuffering: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), vlc.Name));

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F11:
                    if (WindowState != FormWindowState.Maximized)
                    {
                        WindowState = FormWindowState.Maximized;
                        FormBorderStyle = FormBorderStyle.None;
                    }
                    else
                    {
                        WindowState = FormWindowState.Normal;
                        FormBorderStyle = FormBorderStyle.Sizable;
                    }
                    break;
            }
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            // Adjust size and position of VLC controls to match new form size
            setSizes();
        }

        /// <summary>
        /// Read a key value from an Ini file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string getIniValue(string key)
        {
            var MyIni = new IniFile();
            try
            {
                var value = MyIni.Read(key);
                // This guarantees that an Ini file will be created if it doesn't exist
                MyIni.Write(key, value);

                return value;
            }
            catch
            {
                throw new Exception(string.Format("Error reading value for ini key [{0}]", key));
            }
        }
    }
}
