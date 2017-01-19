using SDS.Utilities.IniFiles;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vlc.DotNet.Forms;
using RTSP_Viewer.Classes;
using log4net;

namespace RTSP_Viewer
{
    public partial class Viewer : Form
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int NumberOfViews;
        private const int ViewPadding = 1;

        VlcControl[] myVlcControl;
        Panel[] vlcOverlay;
        OpcUaClient tagClient;
        IniFile MyIni = new IniFile();
        TextBox uri = new TextBox();
        ComboBox cbxViewSelect = new ComboBox();

        public Viewer()
        {
            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("logger.xml"));
            log.Info("Application Form loading");
            InitializeComponent();
            this.KeyPreview = true;
            this.FormClosing += Form1_FormClosing;
            this.KeyDown += Form1_KeyDown;

            InitializeForm();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Padding = new Padding(5);
            this.SizeChanged += Form1_ResizeEnd;

            OpcInterfaceInit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (VlcControl vc in myVlcControl)
            {
                vc.Stop();
            }

            // Call disconnect (if tagClient is not null)
            tagClient?.Disconnect();
        }

        private void InitializeForm()
        {
            // Remove all controls and recreate
            this.Controls.Clear();

            SetupVlc();
            InitDebugControls();

            foreach (VlcControl vc in myVlcControl)
            {
                this.Controls.Add(vc);
            }
        }

        private void SetupVlc()
        {
            NumberOfViews = GetNumberOfViews();
            myVlcControl = new VlcControl[NumberOfViews];
            vlcOverlay = new Panel[NumberOfViews];

            for (int i = 0; i < NumberOfViews; i++)
            {
                myVlcControl[i] = new VlcControl();
                vlcOverlay[i] = new Panel { Name = "VLC Overlay " + i, BackColor = Color.Transparent, Parent = myVlcControl[i], Dock = DockStyle.Fill, TabIndex = i };
                vlcOverlay[i].MouseDoubleClick += VlcOverlay_MouseDoubleClick;
                vlcOverlay[i].MouseClick += VlcOverlay_MouseClick;

                ((System.ComponentModel.ISupportInitialize)(myVlcControl[i])).BeginInit();

                myVlcControl[i].VlcLibDirectory = GetVlcLibLocation();
                myVlcControl[i].VlcMediaplayerOptions = new string[] { "--network-caching=1000", "--video-filter=deinterlace" };
                // Standalone player
                //Vlc.DotNet.Core.VlcMediaPlayer mp = new Vlc.DotNet.Core.VlcMediaPlayer(VlCLibDirectory);
                //mp.SetMedia(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
                //mp.Play();

                myVlcControl[i].Location = new Point(0, 0);
                myVlcControl[i].Name = string.Format("VLC Viewer {0}", i);
                myVlcControl[i].Rate = (float)0.0;
                myVlcControl[i].BackColor = Color.Gray;
                myVlcControl[i].TabIndex = i;

                // Events
                myVlcControl[i].Playing += OnVlcPlaying;
                myVlcControl[i].EncounteredError += MyVlcControl_EncounteredError;
                myVlcControl[i].LengthChanged += MyVlcControl_LengthChanged;
                myVlcControl[i].Buffering += Form1_Buffering;

                myVlcControl[i].Controls.Add(vlcOverlay[i]);
                // Had to add this line to make work
                ((System.ComponentModel.ISupportInitialize)(myVlcControl[i])).EndInit();
            }

            setSizes();
        }

        private void InitDebugControls()
        {
            uri.Text = "rtsp://127.0.0.1:554/rtsp_tunnel?h26x=4&line=1&inst=1";
            uri.Location = new Point(10, this.Height - 60);
            uri.Width = 600;
            uri.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            this.Controls.Add(uri);

            Button playBtn = new Button();
            playBtn.Text = "Connect";
            playBtn.Location = new Point(10, uri.Top - uri.Height - 10);
            playBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            playBtn.Click += PlayBtn_Click;
            this.Controls.Add(playBtn);

            Button pauseBtn = new Button();
            pauseBtn.Text = "Pause";
            pauseBtn.Location = new Point(playBtn.Right + 20, uri.Top - uri.Height - 10);
            pauseBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            pauseBtn.Click += PauseBtn_Click;
            this.Controls.Add(pauseBtn);

            Button stopBtn = new Button();
            stopBtn.Text = "Disconnect";
            stopBtn.Location = new Point(pauseBtn.Right + 20, uri.Top - uri.Height - 10);
            stopBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            stopBtn.Click += StopBtn_Click;
            this.Controls.Add(stopBtn);

            cbxViewSelect.Location = new Point(stopBtn.Right + 20, stopBtn.Top);
            cbxViewSelect.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            cbxViewSelect.Width = 100;
            cbxViewSelect.Height = playBtn.Height;
            for (int i = 0; i < NumberOfViews; i++)
            {
                cbxViewSelect.Items.Add(string.Format("Viewer {0}", i + 1));
            }

            cbxViewSelect.SelectedIndex = 0;
            this.Controls.Add(cbxViewSelect);
            
            Button btnLoadLast = new Button();
            btnLoadLast.Text = "Load Last";
            btnLoadLast.Location = new Point(cbxViewSelect.Right + 20, uri.Top - uri.Height - 10);
            btnLoadLast.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            btnLoadLast.Click += BtnLoadLast_Click; ;
            this.Controls.Add(btnLoadLast);
        }

        /// <summary>
        /// Establish Opc connection (if enabled) in own thread 
        /// </summary>
        private void OpcInterfaceInit()
        {
            int opcEnable = 0;
            // Read value from ini if present
            Int32.TryParse(getIniValue("OPC_Interface_Enable"), out opcEnable);

            if (opcEnable > 0)
            {
                // Instantiate OPC client and provide delegate function to handle callups
                tagClient = new OpcUaClient(CameraCallup);

                // OPC server and path to subscribe to
                string endPointURL = getIniValue("OPC_Endpoint_URL");
                string tagPath = getIniValue("OPC_Tag_Path");

                // Establish Opc connection/subscription on own thread
                log.Info("Initializing OPC connection");
                tagClient.StartInterface(endPointURL, tagPath);
            }
            else
            {
                log.Info("OPC disabled in ini file");
            }
        }

        public int GetNumberOfViews()
        {
            int views = 0;

            // Read value from ini if present, otherwise default to 1 view and write to ini
            if (!Int32.TryParse(getIniValue("NumberOfViews"), out views))
            {
                views = 1;
                MyIni.Write("NumberOfViews", views.ToString());
            }

            // Force the Number of views to be a power of 2 (1, 2, 4, 16, etc)
            var sqrtInt = Math.Truncate(Math.Sqrt(Convert.ToDouble(views)));
            double sqrt = Convert.ToDouble(sqrtInt);
            views = Convert.ToInt32(Math.Pow(sqrt, Convert.ToDouble(2)));
            return views;
        }

        /// <summary>
        /// Set the size/postion of each VLC control based on the total number
        /// </summary>
        public void setSizes()
        {
            Point[] displayPoint = new Point[NumberOfViews];
            Size[] displaySize = new Size[NumberOfViews];

            // Set the control sizes to fit the set resolution
            int dim = (int)Math.Round(Math.Sqrt(NumberOfViews));
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
            }
            else if (vlcLibDirectoryX.Exists)
            {
                return vlcLibDirectoryX;
            }
            else
            {
                MessageBox.Show(this, "VLC install folder not found.  Libraries cannot be loaded.\nApplication will now close.", "Libraries not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Fatal("VLC install folder not found.  Libraries cannot be loaded.\nApplication will now close.");
                Application.Exit();
                throw new DirectoryNotFoundException("VLC install folder not found.  Libraries cannot be loaded.");
            }
        }

        /// <summary>
        /// Loads the last URI displayed on each viewer position
        /// </summary>
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
                    log.Debug("No lastURI entry found for Viewer_" + i);
                }
            }
        }

        private void TogglePause(int viewerNum)
        {
            if (viewerNum >= 0)
            {
                if (myVlcControl[viewerNum].State == Vlc.DotNet.Core.Interops.Signatures.MediaStates.Paused)
                {
                    myVlcControl[viewerNum].Play();
                    log.Debug(string.Format("{0} resume playing", myVlcControl[viewerNum].Name));
                }
                else if (myVlcControl[viewerNum].State == Vlc.DotNet.Core.Interops.Signatures.MediaStates.Playing)
                {
                    myVlcControl[viewerNum].Pause();
                    log.Debug(string.Format("{0} pause", myVlcControl[viewerNum].Name));
                }
            }
        }

        /// <summary>
        /// Open the provided URI on the provide VLC position
        /// </summary>
        /// <param name="URI">URI to open</param>
        /// <param name="ViewerNum">VLC control to display video on</param>
        private void CameraCallup(string URI, int ViewerNum)
        {
            log.Debug(string.Format("Camera callup for view {0} [{1}]", ViewerNum, URI));
            if (ViewerNum >= 0)
            {
                Debug.Print(myVlcControl[ViewerNum].State.ToString());
                //myVlcControl.Play(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
                myVlcControl[ViewerNum].Play(new Uri(URI), "");
                Debug.Print(myVlcControl[ViewerNum].State.ToString());
                myVlcControl[ViewerNum].UseWaitCursor = true;

                MyIni.Write("lastURI", URI, "Viewer_" + ViewerNum);
            }
        }

        private void PlayBtn_Click(object sender, EventArgs e)
        {
            CameraCallup(this.uri.Text, cbxViewSelect.SelectedIndex);
        }

        private void BtnLoadLast_Click(object sender, EventArgs e)
        {
            loadLastStream();
        }

        private void PauseBtn_Click(object sender, EventArgs e)
        {
            int viewerNum = cbxViewSelect.SelectedIndex;
            TogglePause(viewerNum);
        }
        
        private void StopBtn_Click(object sender, EventArgs e)
        {
            int viewerNum = cbxViewSelect.SelectedIndex;
            if (viewerNum >= 0)
            {
                myVlcControl[viewerNum].Stop();
            }
        }

        private void VlcOverlay_MouseClick(object sender, MouseEventArgs e)
        {
            // Update combobox with selected view
            Panel pan = (Panel)sender;
            cbxViewSelect.SelectedIndex = pan.TabIndex;

            if (e.Button == MouseButtons.Right)
            {
                TogglePause(pan.TabIndex);
            }
        }

        private void VlcOverlay_MouseDoubleClick(object sender, EventArgs e)
        {
            Panel overlay = (Panel)sender;
            VlcControl vlc = (VlcControl)overlay.Parent;
            if (vlc.Width >= this.Bounds.Size.Width)
            {
                setSizes();
            }
            else
            {
                vlc.Width = this.Width;
                vlc.Height = this.Height;
                vlc.Location = new Point(0, 0);
                vlc.BringToFront();
            }
        }

        private void MyVlcControl_LengthChanged(object sender, Vlc.DotNet.Core.VlcMediaPlayerLengthChangedEventArgs e)
        {
            VlcControl vlc = (VlcControl)sender;
            log.Debug(string.Format("{0} media length changed to {1}", vlc.Name, vlc.Length));
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

            var mediaInformations = vlc.GetCurrentMedia().TracksInformations;
            foreach (var mediaInformation in mediaInformations)
            {
                if (mediaInformation.Type == Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Audio)
                {
                    log.Debug(string.Format("{0} Audio info - Codec: {1}, Channels: {2}, Rate: {3}", vlc.Name, mediaInformation.CodecName, mediaInformation.Audio.Channels, mediaInformation.Audio.Rate));
                    //        //myLblAudioCodec.InvokeIfRequired(l => l.Text += mediaInformation.CodecName);
                }
                else if (mediaInformation.Type == Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Video)
                {
                    log.Debug(string.Format("{0} Video info - Codec: {1}, Height: {2}, Width: {3}", vlc.Name, mediaInformation.CodecName, mediaInformation.Video.Height, mediaInformation.Video.Width));
                    //        //        //myLblVideoCodec.InvokeIfRequired(l => l.Text += mediaInformation.CodecName);
                }
            }
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
                case Keys.F5:
                    InitializeForm();
                    break;

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
