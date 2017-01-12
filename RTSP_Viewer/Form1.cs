using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Vlc.DotNet.Forms;

namespace RTSP_Viewer
{
    public partial class Form1 : Form
    {
        private const int NumberOfViews = 4;
        private const int ViewPadding = 1;

        VlcControl[] myVlcControl = new VlcControl[NumberOfViews];
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
            playBtn.Text = "Play";
            playBtn.Location = new Point(10, uri.Top - uri.Height - 10);
            playBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            playBtn.Click += PlayBtn_Click;
            this.Controls.Add(playBtn);

            Button stopBtn = new Button();
            stopBtn.Text = "Stop";
            stopBtn.Location = new Point(playBtn.Right + 20, uri.Top - uri.Height - 10);
            stopBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            stopBtn.Click += StopBtn_Click;
            this.Controls.Add(stopBtn);

            cbxViewSelect.Location = new Point(stopBtn.Right + 20, stopBtn.Top);
            cbxViewSelect.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            cbxViewSelect.Width = 100;
            cbxViewSelect.Text = "Select Viewer #";
            for (int i = 0; i < NumberOfViews; i++)
            {
                cbxViewSelect.Items.Add(i);
            }
            cbxViewSelect.Height = playBtn.Height;
            this.Controls.Add(cbxViewSelect);
            
            //viewSelect.Location = new Point()

            //Debug.Print(myVlcControl.VlcLibDirectory.ToString());

            foreach (VlcControl vc in myVlcControl)
            {
                this.Controls.Add(vc);
            }

            this.Padding = new Padding(5);
            //this.Controls.Add(myVlcControl);

            this.SizeChanged += Form1_ResizeEnd;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (VlcControl vc in myVlcControl)
            {
                vc.Stop();
            }
        }

        private void SetupVlc()
        {
            for (int i = 0; i < NumberOfViews; i++)
            {
                myVlcControl[i] = new VlcControl();
                ((System.ComponentModel.ISupportInitialize)(myVlcControl[i])).BeginInit();

                myVlcControl[i].VlcLibDirectory = GetVlcLibLocation();
                //myVlcControl[i].VlcMediaplayerOptions = new string[] { "--video-filter=deinterlace" };
                // Standalone player
                //Vlc.DotNet.Core.VlcMediaPlayer mp = new Vlc.DotNet.Core.VlcMediaPlayer(VlCLibDirectory);
                //mp.SetMedia(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
                //mp.Play();

                myVlcControl[i].Location = new Point(0, 0);
                //myVlcControl[i].Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom);
                //myVlcControl[i].Size = new Size(800, 600);
                myVlcControl[i].Name = string.Format("VLC Viewer {0}", i);
                myVlcControl[i].Rate = (float)0.0;
                myVlcControl[i].BackColor = Color.Gray;

                myVlcControl[i].BackColor = SystemColors.ButtonShadow;
                myVlcControl[i].TabIndex = i;

                // Events
                myVlcControl[i].Playing += OnVlcPlaying;
                myVlcControl[i].EncounteredError += MyVlcControl_EncounteredError;
                myVlcControl[i].LengthChanged += MyVlcControl_LengthChanged;

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

        private void PlayBtn_Click(object sender, EventArgs e)
        {
            int viewerNum = cbxViewSelect.SelectedIndex;
            if (viewerNum >= 0)
            {
                Debug.Print(myVlcControl[viewerNum].State.ToString());
                //myVlcControl.Play(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
                myVlcControl[viewerNum].Play(new Uri(this.uri.Text), "");
                Debug.Print(myVlcControl[viewerNum].State.ToString());
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
        }

        private void OnVlcPlaying(object sender, Vlc.DotNet.Core.VlcMediaPlayerPlayingEventArgs e)
        {
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

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F11:
                    if (FormBorderStyle != FormBorderStyle.None)
                    {
                        FormBorderStyle = FormBorderStyle.None;
                    }
                    else
                    {
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

    }
}
