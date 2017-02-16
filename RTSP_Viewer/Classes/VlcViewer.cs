using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using log4net;
using Vlc.DotNet.Forms;
using System.Windows.Forms;
using System.Drawing;
using SDS.Utilities.IniFiles;

namespace SDS.Video
{
    class VlcViewer
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public VlcControl[] myVlcControl;

        /// <summary>
        /// Get the VLC install location (to reference the required DLLs and plugin folder)
        /// </summary>
        /// <returns>VLC install directory</returns>
        public static DirectoryInfo GetVlcLibLocation()
        {
            DirectoryInfo vlcLibDirectory = null;
            DirectoryInfo vlcLibDirectoryX = null;

            PlatformID platform = Environment.OSVersion.Platform;
            if (platform == PlatformID.Win32NT || platform == PlatformID.Win32S || platform == PlatformID.Win32Windows)
            {
                log.Debug(string.Format("Windows platform [{0}] detected", platform));
                // Check both potential normal install locations
                vlcLibDirectory = new DirectoryInfo("C:\\Program Files\\VideoLAN\\VLC");
                vlcLibDirectoryX = new DirectoryInfo("C:\\Program Files (x86)\\VideoLAN\\VLC");
            }
            else if (platform == PlatformID.Unix)
            {
                log.Debug(string.Format("Unix platform [{0}] detected", platform));
                vlcLibDirectory = new DirectoryInfo("/usr/lib/vlc");
            }

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
                //MessageBox.Show(this, "VLC install folder not found.  Libraries cannot be loaded.\nApplication will now close.", "Libraries not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                log.Fatal("VLC install folder not found.  Libraries cannot be loaded.\nApplication will now close.");
                //Application.Exit();
                throw new DirectoryNotFoundException("VLC install folder not found.  Libraries cannot be loaded.");
            }
        }

        /// <summary>
        /// Make the provided control fill the whole application window
        /// </summary>
        /// <param name="frm">Form containing the control</param>
        /// <param name="vlc">Vlc control to operate on</param>
        public static void SetVlcFullView(Form frm, VlcControl vlc)
        {
            frm.SuspendLayout();
            vlc.Width = frm.ClientSize.Width;
            vlc.Height = frm.ClientSize.Height;
            vlc.Location = new Point(0, 0);
            vlc.BringToFront();
            //statusBg.Visible = true;
            //statusBg.BringToFront();
            frm.ResumeLayout();
        }

        /// <summary>
        /// Loads the last URI displayed on each viewer position
        /// </summary>
        /// <param name="NumberOfViews"></param>
        /// <param name="myVlcControl">Array of all controls</param>
        /// <param name="MyIni">Ini file instance to load info from</param>
        public static void loadLastStream(VlcControl[] myVlcControl, IniFile MyIni)
        {
            for (int i = 0; i < myVlcControl.Length; i++)
            {
                try
                {
                    var uri = MyIni.Read("lastURI", "Viewer_" + i);
                    if (uri != null & uri != "")
                    {
                        myVlcControl[i].Play(new Uri(uri), "");
                        myVlcControl[i].BackColor = Color.Black;
                    }
                }
                catch
                {
                    log.Debug("No lastURI entry found for Viewer_" + i);
                }
            }
        }

        /// <summary>
        /// Pause/Resume playback on the selected Vlc control
        /// </summary>
        /// <param name="vlc">Vlc control to toggle</param>
        public static void TogglePause(VlcControl vlc)
        {
            if (vlc != null)
            {
                if (vlc.State == Vlc.DotNet.Core.Interops.Signatures.MediaStates.Paused)
                {
                    vlc.Play();
                    log.Debug(string.Format("{0} resume playing", vlc.Name));
                }
                else if (vlc.State == Vlc.DotNet.Core.Interops.Signatures.MediaStates.Playing)
                {
                    vlc.Pause();
                    log.Debug(string.Format("{0} pause", vlc.Name));
                }
            }
        }

        /// <summary>
        /// Stop the provided Vlc control
        /// </summary>
        /// <param name="vlc">VlcControl to stop playback</param>
        public static void Disconnect(VlcControl vlc)
        {
            vlc.Stop();
        }

        /// <summary>
        /// Stop all Vlc controls in the provided array
        /// </summary>
        /// <param name="vlc">Array of Vlc Controls to stop playback</param>
        public static void DisconnectAll(VlcControl[] vlc)
        {
            foreach (VlcControl v in vlc)
            {
                Disconnect(v);
            }
        }
    }
}
