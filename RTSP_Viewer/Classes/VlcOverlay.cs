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
                    Visible = true,
                    Size = new System.Drawing.Size(20, 20),
                    Location = new System.Drawing.Point((i * 23) + 5, this.Height - 30),
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                };
                btnPtzPreset[i].Click += PtzPreset_Click;
                btnPtzPreset[i].MouseEnter += PtzPreset_MouseEnter;
                //Controls.Add(btnPtzPreset[i]);
            }
        }

        private void PtzPreset_MouseEnter(object sender, System.EventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void PtzPreset_Click(object sender, System.EventArgs e)
        {
            Button b = (Button)sender;
            MessageBox.Show("Preset " + b.TabIndex);
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
    }
}
