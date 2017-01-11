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

            ((System.ComponentModel.ISupportInitialize)(this.myVlcControl)).BeginInit();

            var currentDirectory = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
            DirectoryInfo VlCLibDirectory = new DirectoryInfo(currentDirectory);
            myVlcControl.VlcLibDirectory = VlCLibDirectory;

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
            myVlcControl.Text = "vlcRincewindControl1";
            
            // Events
            myVlcControl.Playing += OnVlcPlaying;
            myVlcControl.EncounteredError += MyVlcControl_EncounteredError;
            myVlcControl.LengthChanged += MyVlcControl_LengthChanged;

            // Had to add this line to make work
            ((System.ComponentModel.ISupportInitialize)(this.myVlcControl)).EndInit();

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

            //myVlcControl.Play();
            this.Controls.Add(myVlcControl);
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
            myVlcControl.Play(new Uri(this.uri.Text));
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
                    //myLblAudioCodec.InvokeIfRequired(l => l.Text += mediaInformation.CodecName);
                    //myLblAudioChannels.InvokeIfRequired(l => l.Text += mediaInformation.Audio.Channels);
                    //myLblAudioRate.InvokeIfRequired(l => l.Text += mediaInformation.Audio.Rate);
                }
            //    else if (mediaInformation.Type == Vlc.DotNet.Core.Interops.Signatures.MediaTrackTypes.Video)
            //    {
            //        //myLblVideoCodec.InvokeIfRequired(l => l.Text += mediaInformation.CodecName);
            //        //myLblVideoHeight.InvokeIfRequired(l => l.Text += mediaInformation.Video.Height);
            //        //myLblVideoWidth.InvokeIfRequired(l => l.Text += mediaInformation.Video.Width);
            //    }
            }

            //myCbxAspectRatio.InvokeIfRequired(c =>
            //{
            //    c.Text = myVlcControl.Video.AspectRatio;
            //    c.Enabled = true;
            //});
        }
    }
}
