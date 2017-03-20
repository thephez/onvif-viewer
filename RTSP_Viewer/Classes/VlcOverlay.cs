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
    }
}
