using System;
using System.Windows.Forms;
using System.Drawing;
using log4net;

namespace SDS.Video
{
    /// <summary>
    /// Class based on Panel with extra properties
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class VlcOverlay : Panel
    {
        public bool PtzEnabled { get; set; } = false;
        public string LastCamUri { get; set; }
        public int ScrollSpeed { get; private set; } = 0;

        /// <summary>
        /// Used to store the mouse location when the last command was sent
        /// </summary>
        public MouseEventArgs LastMouseArgs { get; set; }

        /// <summary>
        /// Used to send Pan, Tilt, or Zoom commands to the displayed camera
        /// </summary>
        public Onvif.OnvifPtz PtzController { get; set; }

        // Define a delegate that acts as a signature for the function that is called when the event is triggered.
        // The second parameter is of MyEventArgs type. This object will contain information about the triggered event.
        public delegate void GotoPtzPresetEventHandler(object sender, PresetEventArgs e);
        public event GotoPtzPresetEventHandler GotoPtzPreset;

        public delegate void ToggleMuteEventHandler(object sender, EventArgs e);
        public event ToggleMuteEventHandler ToggleMute;
        
        private System.Timers.Timer MsgDisplayTimer = new System.Timers.Timer();
        private System.Timers.Timer ScrollTimer = new System.Timers.Timer();
        private Button[] btnPtzPreset = new Button[5];
        private Button btnMute = new Button();

        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public VlcOverlay()
        {
            for (int i = 0; i < btnPtzPreset.Length; i++)
            {
                Button b = new Button();
                btnPtzPreset[i] = new Button()
                {
                    Text = (i + 1).ToString(),
                    TabIndex = i + 1,
                    BackColor = System.Drawing.Color.Transparent,
                    Visible = false,
                    Size = new System.Drawing.Size(20, 20),
                    //Location = new System.Drawing.Point((i * 23) + 5, this.Height - 30),
                    //Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                    Location = new System.Drawing.Point(this.Width - 25, (i * 23) + 5),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                };
                btnPtzPreset[i].Click += PtzPreset_Click;
                btnPtzPreset[i].MouseEnter += PtzPreset_MouseEnter;
                Controls.Add(btnPtzPreset[i]);

                Controls.Add(new Label { Name = "Status", Visible = false, Text = "", AutoSize = true, ForeColor = Color.White, BackColor = Color.Black, Anchor = AnchorStyles.Top | AnchorStyles.Left });
                MsgDisplayTimer.Elapsed += MsgDisplayTimer_Elapsed;
            }

            // Add button for muting audio
            btnMute.Size = new Size(22, 22);
            btnMute.Location = new Point(this.Width - 25, this.Height - 25);
            btnMute.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnMute.BackgroundImage = RTSP_Viewer.Properties.Resources.SpeakerOn;
            btnMute.BackgroundImageLayout = ImageLayout.Center;
            btnMute.MouseEnter += Button_Enter;
            btnMute.Click += BtnMute_Click;
            btnMute.Visible = false;
            Controls.Add(btnMute);

            ScrollTimer.Elapsed += ScrollTimer_Elapsed;
        }

        private void BtnMute_Click(object sender, EventArgs e)
        {
            ToggleMute(this, e);
        }

        private void Button_Enter(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void PtzPreset_MouseEnter(object sender, EventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void PtzPreset_Click(object sender, System.EventArgs e)
        {
            Button b = (Button)sender;
            GotoPtzPreset(this, new PresetEventArgs(b.TabIndex));
        }

        /// <summary>
        /// Make the provided control fill the whole application window
        /// </summary>
        /// <param name="frm">Form containing the control</param>
        /// <param name="vlc">Vlc control to operate on</param>
        public static void SetFullView(Form frm, VlcOverlay vlc)
        {
            frm.SuspendLayout();
            vlc.Width = frm.ClientSize.Width;
            vlc.Height = frm.ClientSize.Height;
            vlc.Location = new System.Drawing.Point(0, 0);
            vlc.BringToFront();
            frm.ResumeLayout();
        }

        /// <summary>
        /// Show / hide Ptz Preset buttons on screen
        /// </summary>
        /// <param name="enable">True to enable (display) buttons</param>
        public void EnablePtzPresets(bool enable)
        {
            PtzEnabled = enable;
            foreach (Button b in btnPtzPreset)
            {
                b.Invoke((Action)(() => { b.Visible = enable; }));
            }
        }

        public void EnablePtzPresets(bool enable, int presetCount)
        {
            PtzEnabled = enable;
            foreach (Button b in btnPtzPreset)
            {
                if (b.TabIndex <= presetCount)
                {
                    b.Invoke((Action)(() => { b.Visible = enable; }));
                }
                else
                {
                    b.Invoke((Action)(() => { b.Visible = false; }));
                }
            }
        }

        /// <summary>
        /// Show / hide Mute button on screen
        /// </summary>
        /// <param name="visible">True to enable (display) button</param>
        public void ShowMuteButton(bool visible)
        {
            btnMute.Invoke((Action)(() => { btnMute.Visible = visible; }));
        }

        public void SetMuteState(bool status)
        {
            if (status)
            {
                btnMute.BackColor = Color.Red;
                btnMute.BackgroundImage = RTSP_Viewer.Properties.Resources.SpeakerMute;
                ShowNotification("Audio muted", 5000);
                log.Info(string.Format("Viewer {0} muted", this.Name));
            }
            else
            {
                btnMute.BackColor = default(Color);
                btnMute.BackgroundImage = RTSP_Viewer.Properties.Resources.SpeakerOn;
                ShowNotification("Audio enabled", 5000);
                log.Info(string.Format("Viewer {0} audio enabled", this.Name));
            }
        }

        /// <summary>
        /// Show notification on viewer
        /// </summary>
        /// <param name="message">Message to display</param>
        public void ShowNotification(string message)
        {
            Invoke((Action)(() => { Controls["Status"].Text = message; Controls["Status"].Visible = true; }));
            MsgDisplayTimer.Stop();
        }

        /// <summary>
        /// Show notification on viewer that goes away after the provided display time
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="displayTime">Amount of time to display message (ms)</param>
        public void ShowNotification(string message, int displayTime)
        {
            Invoke((Action)(() => { Controls["Status"].Text = message; Controls["Status"].Visible = true; }));
            MsgDisplayTimer.Interval = displayTime;
            MsgDisplayTimer.Start();
        }

        /// <summary>
        /// Hide notification message
        /// </summary>
        public void HideNotification()
        {
            Invoke((Action)(() => { Controls["Status"].Text = ""; Controls["Status"].Visible = false; }));
            MsgDisplayTimer.Stop();
        }

        private void MsgDisplayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Invoke((Action)(() => { Controls["Status"].Visible = false; }));
            MsgDisplayTimer.Stop();
        }

        private void ScrollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            log.Debug(string.Format("Scroll Timer elapsed: stop timer and send stop command to viewer"));
            Console.WriteLine(string.Format("Scroll Timer elapsed: stop timer and send stop command to viewer {0}", this.Name));
            ScrollTimer.Enabled = false;
            ScrollTimer.Stop();

            if (PtzController != null)
                this.PtzController.Stop();
        }

        /// <summary>
        /// Adjusts the zoom speed based on the direction of the mouse scroll wheel
        /// </summary>
        /// <param name="e"></param>
        public void SetZoomSpeed(MouseEventArgs e)
        {
            if (ScrollTimer.Enabled)
            {
                // Timer still running (i.e. user didn't stop scrolling)
                if (e.Delta > 0)
                {
                    // Add veloctiy
                    if (ScrollSpeed > 0)
                        ScrollSpeed += 1;
                    else
                        ScrollSpeed = 1;
                }
                else
                {
                    // Subtract velocity
                    if (ScrollSpeed < 0)
                        ScrollSpeed -= 1;
                    else
                        ScrollSpeed = -1;
                }
            }
            else
            {
                // Timer already stopped so reset the scroll velocity
                if (e.Delta > 0)
                    ScrollSpeed = 5;
                else if (e.Delta < 0)
                    ScrollSpeed = -5;
            }

            ScrollTimer.Interval = 600;
            ScrollTimer.Enabled = true;
            ScrollTimer.Start();
        }
    }
}

public class PresetEventArgs : EventArgs
{
    public int Preset { get; }
    public PresetEventArgs(int preset)
    {
        Preset = preset;
    }

    public int GetPreset()
    {
        return Preset;
    }
}
