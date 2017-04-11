using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SDS.Video
{
    /// <summary>
    /// Class based on Panel with extra properties
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class VlcOverlay : Panel
    {
        public bool PtzEnabled { get; set; } = false;
        public int LastCamNum { get; set; }
        public string LastCamUri { get; set; }

        /// <summary>
        /// Used to store the mouse location when the last command was sent
        /// </summary>
        public MouseEventArgs LastMouseArgs { get; set; }

        /// <summary>
        /// Used to send Pan, Tilt, or Zoom commands to the displayed camera
        /// </summary>
        public Onvif.OnvifPtz PtzController { get; set; }

        Button[] btnPtzPreset = new Button[5];

        // Define a delegate that acts as a signature for the function that is called when the event is triggered.
        // The second parameter is of MyEventArgs type. This object will contain information about the triggered event.
        public delegate void GotoPtzPresetEventHandler(object sender, PresetEventArgs e);
        public event GotoPtzPresetEventHandler GotoPtzPreset;

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
                    Location = new System.Drawing.Point((i * 23) + 5, this.Height - 30),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                };
                btnPtzPreset[i].Click += PtzPreset_Click;
                btnPtzPreset[i].MouseEnter += PtzPreset_MouseEnter;
                Controls.Add(btnPtzPreset[i]);
            }
        }

        private void PtzPreset_MouseEnter(object sender, System.EventArgs e)
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
