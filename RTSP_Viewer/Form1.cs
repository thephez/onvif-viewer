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
        VlcControl myVlcControl = new VlcControl();
        TextBox uri = new TextBox();

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            myVlcControl.Stop();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupVlc();

            Button playBtn = new Button();
            playBtn.Text = "Play";
            playBtn.Location = new Point(10, myVlcControl.Bottom + 5);
            playBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            playBtn.Click += PlayBtn_Click;
            this.Controls.Add(playBtn);

            Button stopBtn = new Button();
            stopBtn.Text = "Stop";
            stopBtn.Location = new Point(100, myVlcControl.Bottom + 5);
            stopBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            stopBtn.Click += StopBtn_Click;
            this.Controls.Add(stopBtn);
            
            uri.Text = "rtsp://127.0.0.1:554/rtsp_tunnel?h26x=4&line=1&inst=1";
            uri.Location = new Point(10, myVlcControl.Bottom + 5 + playBtn.Height + 5);
            uri.Width = 600;
            uri.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            this.Controls.Add(uri);

            Debug.Print(myVlcControl.VlcLibDirectory.ToString());

            this.Controls.Add(myVlcControl);
        }

        private void SetupVlc()
        {
            ((System.ComponentModel.ISupportInitialize)(this.myVlcControl)).BeginInit();

            myVlcControl.VlcLibDirectory = GetVlcLibLocation();
            myVlcControl.VlcMediaplayerOptions = new string[] { "--video-filter=deinterlace" };
            // Standalone player
            //Vlc.DotNet.Core.VlcMediaPlayer mp = new Vlc.DotNet.Core.VlcMediaPlayer(VlCLibDirectory);
            //mp.SetMedia(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
            //mp.Play();

            myVlcControl.Location = new Point(0, 0);
            myVlcControl.Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom);
            myVlcControl.Size = new Size(800, 600);
            myVlcControl.Name = "Vlc1";
            myVlcControl.Rate = (float)0.0;
            myVlcControl.BackColor = Color.Gray;

            myVlcControl.BackColor = System.Drawing.SystemColors.ButtonShadow;
            myVlcControl.TabIndex = 0;

            // Events
            myVlcControl.Playing += OnVlcPlaying;
            myVlcControl.EncounteredError += MyVlcControl_EncounteredError;
            myVlcControl.LengthChanged += MyVlcControl_LengthChanged;

            // Had to add this line to make work
            ((System.ComponentModel.ISupportInitialize)(this.myVlcControl)).EndInit();
        }

        /// <summary>
        /// Get the VLC install location (to reference the required DLLs and plugin folder)
        /// </summary>
        /// <returns>VLC install directory</returns>
        private DirectoryInfo GetVlcLibLocation()
        {
            // Check both potential normal install locations
            DirectoryInfo vlcLibDirectory = new DirectoryInfo("C:\\Program Files\\VideoLAN\\VLC");
            DirectoryInfo vlcLibDirectoryX = new DirectoryInfo("C:\\Program Files (x86)\\VideoLAN\\VLCs");

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

        private void StopBtn_Click(object sender, EventArgs e)
        {
            myVlcControl.Stop();
        }

        private void MyVlcControl_LengthChanged(object sender, Vlc.DotNet.Core.VlcMediaPlayerLengthChangedEventArgs e)
        {
            Console.WriteLine("Length changed");
        }

        private void MyVlcControl_EncounteredError(object sender, Vlc.DotNet.Core.VlcMediaPlayerEncounteredErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PlayBtn_Click(object sender, EventArgs e)
        {
            Debug.Print(myVlcControl.State.ToString());
            //myVlcControl.Play(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
            myVlcControl.Play(new Uri(this.uri.Text), "");
            Debug.Print(myVlcControl.State.ToString());
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

            var mediaInformations = myVlcControl.GetCurrentMedia().TracksInformations;
            foreach (var mediaInformation in mediaInformations)
            {
                if (mediaInformation.Type == Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Audio)
                {
                    Console.WriteLine(string.Format("Audio info - Codec: {0}, Channels: {1}, Rate: {2}", mediaInformation.CodecName, mediaInformation.Audio.Channels, mediaInformation.Audio.Rate));
                    //myLblAudioCodec.InvokeIfRequired(l => l.Text += mediaInformation.CodecName);
                }
                else if (mediaInformation.Type == Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Video)
                {
                    Console.WriteLine(string.Format("Video info - Codec: {0}, Height: {1}, Width: {2}", mediaInformation.CodecName, mediaInformation.Video.Height, mediaInformation.Video.Width));
                    //        //myLblVideoCodec.InvokeIfRequired(l => l.Text += mediaInformation.CodecName);
                }
            }
        }
    }
}
