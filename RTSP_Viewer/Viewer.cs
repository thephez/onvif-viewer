using SDS.Utilities.IniFiles;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using Vlc.DotNet.Forms;
using RTSP_Viewer.Classes;
using SDS.Video;
using log4net;
using System.Text.RegularExpressions;
using System.IO;
using System.ComponentModel;
using SDS.Video.Onvif;

namespace RTSP_Viewer
{
    public partial class Viewer : Form
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int NumberOfViews;
        private const int ViewPadding = 1;
        //private NotifyIcon notification = new NotifyIcon() { Icon = SystemIcons.Application, Visible = true };

        // Create the HMI interface
        CallupsTxtFile hmi;

        VlcControl[] myVlcControl;
        VlcOverlay[] vlcOverlay;
        Panel statusBg = new Panel();

        OpcUaClient tagClient;
        IniFile MyIni;
        TextBox txtUri = new TextBox() { Tag = "Debug", Visible = false };
        ComboBox cbxViewSelect = new ComboBox() { Tag = "Debug", Visible = false };

        BackgroundWorker[] BgPtzWorker;

        public Viewer()
        {
            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo("logger.xml"));
            log.Info("-------------------------");
            log.Info("Application Form loading");

            InitializeComponent();
            this.KeyPreview = true;
            this.FormClosing += Form1_FormClosing;
            this.KeyDown += Form1_KeyDown;

            InitializeForm();

            // Necessary for Samsung cameras.  The "Expect: 100-continue" HTTP header 
            // will prevent a connection to them (usually a 417 error will be reported)
            System.Net.ServicePointManager.Expect100Continue = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Padding = new Padding(5);
            this.SizeChanged += Form1_ResizeEnd;

            OpcInterfaceInit();

            // This handles the size change that occurs after the Vlc controls initialize on startup
            setSizes();
            InitViewerStatus();

            // Initialize the HMI interface
            try
            {
                hmi = new CallupsTxtFile(new CallupsTxtFile.SetRtspCallupCallback(CameraCallup), getIniValue("CallupsFilePath"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error starting application.  Unable to monitor callup file.\nApplication will now exit.\n\nException:\n{0}", ex.Message), "Startup failure", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(1);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            VlcViewer.DisconnectAll(myVlcControl);

            // Call disconnect (if tagClient is not null)
            tagClient?.Disconnect();
            Cursor.Current = Cursors.Default;

            log.Info("Application Form closing");
        }

        private void InitializeForm()
        {
            log.Info("Initializing form");
            // Remove all controls and recreate
            this.Controls.Clear();

            MyIni = new IniFile();

            SetupVlc();
            InitViewerStatus();
            InitDebugControls();

            foreach (VlcControl vc in myVlcControl)
            {
                this.Controls.Add(vc);
            }
            
            // Load values from ini file (default to stream 1 if none provided)
            int defaultStream = int.TryParse(getIniValue("DefaultStream"), out defaultStream) ? defaultStream : 1;
            string cameraFile = getIniValue("CameraFile");
            string cameraSchema = getIniValue("CameraSchemaFile");

            if (File.Exists(cameraFile) && File.Exists(cameraSchema))
            {
                // Load camera xml file and assign default mfgr if one not provided
                Camera.GenerateHashTable("Bosch", defaultStream, cameraFile, cameraSchema);
            }
            else
            {
                log.Error(string.Format("CameraFile [{0}] and/or CameraSchemaFile [{1}] not found.", cameraFile, cameraSchema));
                MessageBox.Show(string.Format("Error starting application.  CameraFile '{0}' and/or CameraSchemaFile '{1}' not found.\nApplication will now exit.", cameraFile, cameraSchema), "Startup failure - Configuration files not found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(2);
            }
        }

        /// <summary>
        /// Configure the VLC Control(s) and overlay(s) that handle mouse events
        /// </summary>
        private void SetupVlc()
        {
            NumberOfViews = GetNumberOfViews();
            myVlcControl = new VlcControl[NumberOfViews];
            vlcOverlay = new VlcOverlay[NumberOfViews];
            string[] vlcMediaOptions = Regex.Split(getIniValue("VlcOptions"), "\\s*,\\s*"); // Split by comma and trim whitespace

            BgPtzWorker = new BackgroundWorker[NumberOfViews];

            for (int i = 0; i < NumberOfViews; i++)
            {
                BgPtzWorker[i] = new BackgroundWorker();
                BgPtzWorker[i].WorkerReportsProgress = true;
                BgPtzWorker[i].WorkerSupportsCancellation = true;
                BgPtzWorker[i].DoWork += BgPtzWorker_DoWork;

                myVlcControl[i] = new VlcControl();
                vlcOverlay[i] = new VlcOverlay { Name = "VLC Overlay " + i, BackColor = Color.Transparent, Parent = myVlcControl[i], Dock = DockStyle.Fill, TabIndex = i };
                vlcOverlay[i].MouseEnter += VlcOverlay_MouseEnter;
                vlcOverlay[i].MouseLeave += VlcOverlay_MouseLeave;
                vlcOverlay[i].MouseDoubleClick += VlcOverlay_MouseDoubleClick;
                vlcOverlay[i].MouseClick += VlcOverlay_MouseClick;
                vlcOverlay[i].MouseMove += VlcOverlay_MouseMove;
                vlcOverlay[i].MouseDown += VlcOverlay_MouseDown;
                vlcOverlay[i].MouseUp += VlcOverlay_MouseUp;
                vlcOverlay[i].MouseWheel += VlcOverlay_MouseWheel;
                vlcOverlay[i].Controls.Add(new Label { Name = "Status", Visible = false, Text = "", AutoSize = true, ForeColor = Color.White, Anchor = AnchorStyles.Top | AnchorStyles.Left });

                ((System.ComponentModel.ISupportInitialize)(myVlcControl[i])).BeginInit();

                myVlcControl[i].VlcLibDirectory = VlcViewer.GetVlcLibLocation();  // Tried to call once outside loop, but it causes in exception on program close
                myVlcControl[i].VlcMediaplayerOptions = vlcMediaOptions; // new string[] { "--network-caching=150", "--video-filter=deinterlace" };
                myVlcControl[i].Location = new Point(0, 0);
                myVlcControl[i].Name = string.Format("VLC Viewer {0}", i);
                myVlcControl[i].Rate = (float)0.0;
                myVlcControl[i].BackColor = Color.Gray;
                myVlcControl[i].TabIndex = i;

                // Events
                myVlcControl[i].Playing += OnVlcPlaying;
                myVlcControl[i].EncounteredError += MyVlcControl_EncounteredError;
                myVlcControl[i].Buffering += Form1_Buffering;

                myVlcControl[i].Controls.Add(vlcOverlay[i]);
                // Had to add this line to make work
                ((System.ComponentModel.ISupportInitialize)(myVlcControl[i])).EndInit();
            }

            setSizes();
        }

        /// <summary>
        /// Creates the status object displayed in the lower right corner which
        /// shows the currently selected View and allows switching between views when a view is full screen
        /// </summary>
        private void InitViewerStatus()
        {
            // Only need this if showing more than 1 viewer
            if (NumberOfViews > 1)
            {
                statusBg.Controls.Clear();
                statusBg.BackColor = Color.Black;
                statusBg.Size = new Size(60, 37);
                statusBg.Location = new Point(this.ClientSize.Width - statusBg.Width - 10, this.ClientSize.Height - statusBg.Height - 10);
                statusBg.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);

                Panel[] viewer = new Panel[NumberOfViews];

                Point[] displayPoint = Utilities.CalculatePointLocations(NumberOfViews, statusBg.Width, statusBg.Height);
                Size displaySize = Utilities.CalculateItemSizes(NumberOfViews, statusBg.Size.Width, statusBg.Size.Height, 1); // ViewPadding);

                for (int i = 0; i < NumberOfViews; i++)
                {
                    viewer[i] = new Panel();
                    viewer[i].Location = displayPoint[i];
                    viewer[i].Size = displaySize;
                    viewer[i].BackColor = Color.Gainsboro;
                    viewer[i].Name = string.Format("Viewer Status {0}", i);
                    viewer[i].TabIndex = i;
                    viewer[i].MouseClick += ViewerStatus_MouseClick;
                    statusBg.Controls.Add(viewer[i]);
                }

                statusBg.Visible = true;
                this.Controls.Add(statusBg);
                statusBg.BringToFront();
            }
            else
            {
                statusBg.Visible = false;
            }
        }

        /// <summary>
        /// Update the viewer status object to set the active view
        /// </summary>
        /// <param name="activeView">Vlc View number to make active</param>
        private void SetViewerStatus(int activeView)
        {
            foreach (Control c in statusBg.Controls)
            {
                if (c.Name == string.Format("Viewer Status {0}", activeView))
                    c.BackColor = Color.Yellow;
                else
                    c.BackColor = Color.Gainsboro;
            }
        }
        
        private void InitDebugControls()
        {
            txtUri.Text = "rtsp://127.0.0.1:554/rtsp_tunnel?h26x=4&line=1&inst=1";
            txtUri.Location = new Point(10, this.Height - 60);
            txtUri.Width = 600;
            txtUri.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            this.Controls.Add(txtUri);

            Button playBtn = new Button() { Tag = "Debug", Visible = false };
            playBtn.Text = "Connect";
            playBtn.Location = new Point(10, txtUri.Top - txtUri.Height - 10);
            playBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            playBtn.Click += PlayBtn_Click;
            this.Controls.Add(playBtn);

            Button pauseBtn = new Button() { Tag = "Debug", Visible = false };
            pauseBtn.Text = "Pause";
            pauseBtn.Location = new Point(playBtn.Right + 20, txtUri.Top - txtUri.Height - 10);
            pauseBtn.Anchor = (AnchorStyles.Left | AnchorStyles.Bottom);
            pauseBtn.Click += PauseBtn_Click;
            this.Controls.Add(pauseBtn);

            Button stopBtn = new Button() { Tag = "Debug", Visible = false };
            stopBtn.Text = "Disconnect";
            stopBtn.Location = new Point(pauseBtn.Right + 20, txtUri.Top - txtUri.Height - 10);
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

            Button btnLoadLast = new Button() { Tag = "Debug", Visible = false };
            btnLoadLast.Text = "Load Last";
            btnLoadLast.Location = new Point(cbxViewSelect.Right + 20, txtUri.Top - txtUri.Height - 10);
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
            log.Info(string.Format("Display normal layout ({0} views)", NumberOfViews));
            SuspendLayout();

            Point[] displayPoint = Utilities.CalculatePointLocations(NumberOfViews, ClientSize.Width, ClientSize.Height);
            Size displaySize = Utilities.CalculateItemSizes(NumberOfViews, ClientSize.Width, ClientSize.Height, ViewPadding);

            for (int i = 0; i < NumberOfViews; i++)
            {
                myVlcControl[i].Location = displayPoint[i];
                myVlcControl[i].Size = displaySize;
            }

            ResumeLayout();
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
                Invoke((Action)(() => { vlcOverlay[ViewerNum].Controls["Status"].Text = "Loading"; vlcOverlay[ViewerNum].Controls["Status"].Visible = true; }));
                myVlcControl[ViewerNum].Play(new Uri(URI), "");
                myVlcControl[ViewerNum].BackColor = Color.Black;
                Debug.Print(myVlcControl[ViewerNum].State.ToString());
                myVlcControl[ViewerNum].UseWaitCursor = true;

                // Store the URI in the ini file
                MyIni.Write("lastURI", URI, "Viewer_" + ViewerNum);
                vlcOverlay[ViewerNum].LastCamUri = URI;
            }
        }

        /// <summary>
        /// Callup the requested camera on the provided display number (preset not implemented)
        /// </summary>
        /// <param name="ViewerNum">Control to display video on</param>
        /// <param name="CameraNum">Camera number to display</param>
        /// <param name="Preset">Camera Preset</param>
        private void CameraCallup(int ViewerNum, int CameraNum, int Preset)
        {
            try
            {
                string URI = Camera.GetRtspUri(CameraNum);
                CameraCallup(URI, ViewerNum);
                
                // Check if PTZ controls should be enabled
                Camera cam = Camera.GetCamera(CameraNum);
                
                if (!cam.IsOnvifLoaded)
                {
                    // Get list of XAddrs via device_service to determine if this is a PTZ
                    cam.GetOnvifUris();
                    if (cam.ServiceUris.ContainsKey("http://www.onvif.org/ver20/ptz/wsdl"))
                        cam.IsPtz = true;
                    else
                        cam.IsPtz = false;
                }

                if (cam.IsPtz)
                    vlcOverlay[ViewerNum].PtzController = new OnvifPtz(cam.ServiceUris[OnvifNamespace.MEDIA], cam.ServiceUris[OnvifNamespace.PTZ], user: cam.User, password: cam.Password); // "admin", "P@ssw0rd");

                // Enable the PTZ functionality on the Overlay if available
                vlcOverlay[ViewerNum].PtzEnabled = cam.IsPtz;
            }
            catch (Exception ex)
            {
                log.Error(string.Format("Unable to callup camera or access PTZ.  Exception: {0}", ex.Message));

                myVlcControl[ViewerNum].Stop();
                myVlcControl[ViewerNum].BackColor = Color.Gray;
                string status = string.Format("Camera #{0} unavailable", CameraNum);
                //notification.ShowBalloonTip(1, this.Name, status, ToolTipIcon.Warning);

                Invoke((Action)(() => { vlcOverlay[ViewerNum].Controls["Status"].Text = status; vlcOverlay[ViewerNum].Controls["Status"].Visible = true; }));
                throw;
            }
        }
        
        private void PtzStop(VlcOverlay overlay)
        {
            // Stop PTZ if moving
            Debug.Print(string.Format("{0} Stop PTZ if necessary ({1})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), overlay.Name));

            // Check if PTZ and enable PTZ controls if necessary
            if (overlay.PtzEnabled && overlay.PtzController != null)
            {
                log.Debug(string.Format("Camera stopping on view {0} [{1}]", overlay.Name, overlay.LastCamUri));
                Debug.Print(string.Format("Camera stopping on view {0} [{1}]", overlay.Name, overlay.LastCamUri));
                overlay.PtzController.Stop();
            }
        }

        private void SetVlcFullView(int viewerIndex)
        {
            log.Info(string.Format("Display full screen layout (View #{0})", viewerIndex));
            foreach (VlcControl vlc in myVlcControl)
            {
                if (vlc.TabIndex == viewerIndex)
                {
                    VlcViewer.SetVlcFullView(this, vlc);
                    statusBg.Visible = true;
                    statusBg.BringToFront();
                    SetViewerStatus(viewerIndex);
                    break;
                }
            }
        }

        /// <summary>
        /// Read a key value from an Ini file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private string getIniValue(string key)
        {
            try
            {
                var value = MyIni.Read(key);

                if (!MyIni.KeyExists(key))
                {
                    // This guarantees that an Ini file will be created if it doesn't exist
                    MyIni.Write(key, value);
                }

                return value;
            }
            catch
            {
                log.Warn(string.Format("Error reading value for ini key [{0}]", key));
                throw new Exception(string.Format("Error reading value for ini key [{0}]", key));
            }
        }

        private void PlayBtn_Click(object sender, EventArgs e)
        {
            CameraCallup(this.txtUri.Text, cbxViewSelect.SelectedIndex);
            vlcOverlay[cbxViewSelect.SelectedIndex].PtzEnabled = true;  // Temporary for testing
        }

        private void BtnLoadLast_Click(object sender, EventArgs e)
        {
            VlcViewer.loadLastStream(myVlcControl, MyIni);
        }

        private void PauseBtn_Click(object sender, EventArgs e)
        {
            int viewerNum = cbxViewSelect.SelectedIndex;
            VlcViewer.TogglePause(myVlcControl[viewerNum]);
        }

        private void StopBtn_Click(object sender, EventArgs e)
        {
            int viewerNum = cbxViewSelect.SelectedIndex;
            if (viewerNum >= 0)
            {
                myVlcControl[viewerNum].Stop();
                myVlcControl[viewerNum].BackColor = Color.Gray;
            }
        }

        private void VlcOverlay_MouseClick(object sender, MouseEventArgs e)
        {
            // Update combobox with selected view
            Panel pan = (Panel)sender;
            cbxViewSelect.SelectedIndex = pan.TabIndex;
            SetViewerStatus(pan.TabIndex);
            txtUri.Text = MyIni.Read("lastURI", "Viewer_" + pan.TabIndex);

            if (e.Button == MouseButtons.Right)
            {
                VlcViewer.TogglePause(myVlcControl[pan.TabIndex]);
            }
            Debug.Print(string.Format("Mouse click ({0})", pan.Name));
        }

        private void VlcOverlay_MouseDoubleClick(object sender, EventArgs e)
        {
            Panel overlay = (Panel)sender;
            VlcControl vlc = (VlcControl)overlay.Parent;
            this.SuspendLayout();
            if (vlc.Width >= this.ClientSize.Width)
            {
                setSizes();
                vlc.SendToBack();
            }
            else
            {
                VlcViewer.SetVlcFullView(this, vlc);
                statusBg.Visible = true;
                statusBg.BringToFront();
            }
            this.ResumeLayout();
        }

        private void VlcOverlay_MouseEnter(object sender, EventArgs e)
        {
            // Select control so the mouse wheel event will go to the proper control
            VlcOverlay overlay = (VlcOverlay)sender;
            overlay.Select();

            log.Debug(string.Format("Mouse entered view {0}", overlay.Name));

            if (!overlay.PtzEnabled | !myVlcControl[overlay.TabIndex].IsPlaying)
            {
                // Disable PTZ actions if not playing
                overlay.PtzEnabled = false;
                this.Cursor = Cursors.Default;
            }
        }

        private void VlcOverlay_MouseLeave(object sender, EventArgs e)
        {
            // This is a terrible way to make sure the PTZ stops - replace with better solution
            VlcOverlay overlay = (VlcOverlay)sender;
            log.Info(string.Format("Mouse exited view {0} [NOTE: REPLACE PTZ STOP ON EXIT WITH BETTER SOLUTION]", overlay.Name));
            PtzStop(overlay);
        }

        private void VlcOverlay_MouseWheel(object sender, MouseEventArgs e)
        {
            VlcOverlay overlay = (VlcOverlay)sender;
            Debug.Print(string.Format("{0} Mouse wheel ({1})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), overlay.Name));

            // Use BackgroundWorker to send command to prevent UI lockup
            if (!BgPtzWorker[overlay.TabIndex].IsBusy)
            {
                object[] args = new object[] { overlay, e };
                BgPtzWorker[overlay.TabIndex].RunWorkerAsync(args);
            }
            else
            {
                //log.Debug(string.Format("Background worker busy.  Ignoring mouse wheel for view {0} [{1}]", overlay.Name, overlay.LastCamUri));
            }
        }

        private void VlcOverlay_MouseDown(object sender, MouseEventArgs e)
        {
            VlcOverlay overlay = (VlcOverlay)sender;
            Debug.Print(string.Format("{0} Mouse down ({1})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), overlay.Name));
            log.Debug(string.Format("Mouse down on view {0}", overlay.Name));

            // Use BackgroundWorker to send command to prevent UI lockup
            if (!BgPtzWorker[overlay.TabIndex].IsBusy)
            {
                object[] args = new object[] { overlay, e };
                BgPtzWorker[overlay.TabIndex].RunWorkerAsync(args);
            }
            else
            {
                log.Debug(string.Format("Background worker busy.  Ignoring mouse down for view {0} [{1}]", overlay.Name, overlay.LastCamUri));
            }
        }

        private void VlcOverlay_MouseUp(object sender, MouseEventArgs e)
        {
            VlcOverlay overlay = (VlcOverlay)sender;

            // Attempt to prevent unstopping PTZ (stop send before PTZ?)
            BgPtzWorker[overlay.TabIndex].CancelAsync();
            PtzStop(overlay);
        }

        /// <summary>
        /// Sends PTZ commands to the relevant camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Object containing the relevant Vlc View overlay and the mouse event args</param>
        private void BgPtzWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];

            VlcOverlay overlay = (VlcOverlay)args[0];
            MouseEventArgs mouseArgs = (MouseEventArgs)args[1];

            if (!myVlcControl[overlay.TabIndex].IsPlaying)
            {
                Debug.Print(string.Format("{0} VLC not playing.  No PTZ command sent.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")));
                //log.Debug(string.Format("VLC not playing.  No PTZ command sent to view {0}", overlay.Name));
                return;
            }

            // Check if PTZ and enable PTZ controls if necessary
            if (overlay.PtzEnabled)
            {
                if (overlay.PtzController == null)
                {
                    log.Warn(string.Format("No PtzController configured for camera stream [{0}]", overlay.LastCamUri));
                    throw new Exception(string.Format("No PtzController configured for camera stream [{0}]", overlay.LastCamUri));
                }

                if (mouseArgs.Delta != 0)
                {
                    if (mouseArgs.Delta > 0)
                    {
                        Debug.Print(string.Format("{0} Zoom in", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")));
                        overlay.PtzController.Zoom((float)0.20);
                    }
                    else if (mouseArgs.Delta < 0)
                    {
                        Debug.Print(string.Format("{0} Zoom out", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")));
                        overlay.PtzController.Zoom((float)-0.20);
                    }

                    // Zoom for the sleep duration and then stop (should be a better way to do this)
                    System.Threading.Thread.Sleep(25);
                    overlay.PtzController.Stop();
                    log.Debug(string.Format("Camera Zoom stopped on view {0} [{1}]", overlay.Name, overlay.LastCamUri));
                    Debug.Print(string.Format("{0} Camera stopped ({1})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), overlay.LastCamUri));
                }
                else
                {
                    // Calculate the speed Pan and Tilt using the mouse location
                    // Uses the center of the control as point 0, 0 (i.e the center)
                    // A negative pan speed moves the camera to the left, positive to the right
                    // A negative tilt speed moves the camera down, positive moves it up
                    // The speed is a value between 0 and 1 (represents a percent of max speed)
                    float panSpeed = (float)(mouseArgs.X - (overlay.Width / 2)) / (float)(overlay.Width / 2);
                    float tiltSpeed = (float)((overlay.Height / 2) - mouseArgs.Y) / (float)(overlay.Height / 2);

                    log.Debug(string.Format("Sending PTZ Command to move [Pan Speed: {0}, Tilt Speed: {1}] on view {2} [{3}]", panSpeed, tiltSpeed, overlay.Name, overlay.LastCamUri));
                    overlay.PtzController.PanTilt(panSpeed, tiltSpeed);
                }
            }
        }

        private void VlcOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            VlcOverlay overlay = (VlcOverlay)sender;

            int minMovePercent = 2;
            if (overlay.LastMouseArgs == null)
                overlay.LastMouseArgs = e;

            int x = overlay.Size.Width / 2;
            int y = overlay.Size.Height / 2;
            string quadrant = "";

            int deltaX = e.X - x;
            int deltaY = y - e.Y;

            float radius = (float)Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            double angle = Math.Atan2(deltaY, deltaX) * (180 / Math.PI);

            if (deltaY >= 0)
                quadrant = "Top";
            else
                quadrant = "Bottom";

            if (deltaX >= 0)
                quadrant += " Right";
            else
                quadrant += " Left";

            if (overlay.PtzEnabled)
                this.Cursor = Utilities.GetPtzCursor(angle);

            Invoke((Action)(() => { overlay.Controls["Status"].Text = string.Format("{0}\nMouse @ ({1}, {2})\nPolar: {3:0.#}@{4:0.##}\nCart.: {5},{6}", quadrant, e.Location.X, e.Location.Y, radius, angle, deltaX, deltaY); overlay.Controls["Status"].Visible = true; }));

            // Change PTZ command based on mouse position (only if left button down)
            if (e.Button == MouseButtons.Left)
            {
                //Debug.Print(string.Format("Mouse Move with button {0} pressed @ {1}, {2}", e.Button, e.X, e.Y));

                if (Math.Abs((overlay.LastMouseArgs.X - e.X)) > (overlay.Width * ((float)minMovePercent / 100)))
                {
                    Debug.Print(string.Format("{0}           {1}", Math.Abs((overlay.LastMouseArgs.X - e.X)), (overlay.Width * ((float)minMovePercent / 100))));
                    Debug.Print(string.Format("Mouse moved horizontally by more than the minimum percentage [{0}] to [{1}, {2}]", minMovePercent, e.X, e.Y));
                    //overlay.LastMouseArgs = e;
                }
                else if (Math.Abs((overlay.LastMouseArgs.Y - e.Y)) > (overlay.Height * ((float)minMovePercent / 100)))
                {
                    Debug.Print(string.Format("{0}           {1}", Math.Abs((overlay.LastMouseArgs.Y - e.Y)), (overlay.Height * ((float)minMovePercent / 100))));
                    Debug.Print(string.Format("Mouse moved vertically by more than the minimum percentage [{0}] to [{1}, {2}]", minMovePercent, e.X, e.Y));
                    //overlay.LastMouseArgs = e;
                }
                else
                {
                    return;
                }

                // Use BackgroundWorker to send command to prevent UI lockup
                if (!BgPtzWorker[overlay.TabIndex].IsBusy)
                {
                    // Only store new mouse position if a command is successfully sent
                    // Otherwise an attempt to send the command should be made the next time the mouse moves
                    overlay.LastMouseArgs = e;
                    object[] args = new object[] { overlay, e };
                    BgPtzWorker[overlay.TabIndex].RunWorkerAsync(args);
                }
                else
                {
                    //log.Debug(string.Format("Background worker busy.  Ignoring mouse down for view {0} [{1}]", overlay.Name, overlay.LastCamUri));
                }
            }
        }

        private void MyVlcControl_EncounteredError(object sender, Vlc.DotNet.Core.VlcMediaPlayerEncounteredErrorEventArgs e)
        {
            VlcControl vlc = (VlcControl)sender;
            Invoke((Action)(() => { vlcOverlay[int.Parse(vlc.Name.Split()[2])].Controls["Status"].Text = "Error"; Visible = true; }));
            log.Error(string.Format("Error encountered on '{0}': {1}", vlc.Name, e.ToString()));

            //MessageBox.Show(string.Format("Error encountered on '{0}':\n{1}", vlc.Name, e.ToString()), "VLC Control Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            vlc.UseWaitCursor = false;
        }

        private void OnVlcPlaying(object sender, Vlc.DotNet.Core.VlcMediaPlayerPlayingEventArgs e)
        {
            VlcControl vlc = (VlcControl)sender;
            vlc.UseWaitCursor = false;

            Invoke((Action)(() => { vlcOverlay[int.Parse(vlc.Name.Split()[2])].Controls["Status"].Visible = false; }));

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
                }
            }
        }

        private void Form1_Buffering(object sender, Vlc.DotNet.Core.VlcMediaPlayerBufferingEventArgs e)
        {
            VlcControl vlc = (VlcControl)sender;

            Console.WriteLine(string.Format("{0}\tBuffering: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), vlc.Name));
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.F5:
                    Cursor.Current = Cursors.WaitCursor;
                    VlcViewer.DisconnectAll(myVlcControl);
                    InitializeForm();
                    Cursor.Current = Cursors.Default;
                    break;

                case Keys.F11:
                    if (WindowState != FormWindowState.Maximized)
                    {
                        // Change border style first or else the ClientSize will be larger than the screen dimensions
                        FormBorderStyle = FormBorderStyle.None;
                        WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        WindowState = FormWindowState.Normal;
                        FormBorderStyle = FormBorderStyle.Sizable;
                    }
                    break;

                case Keys.Control | Keys.D:
                    foreach (Control c in this.Controls)
                    {
                        if (c.Tag?.ToString() == "Debug")
                        {
                            c.Visible = !c.Visible;
                            c.BringToFront();
                        }
                    }
                    break;
            }
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            // Adjust size and position of VLC controls to match new form size
            setSizes();
        }

        private void ViewerStatus_MouseClick(object sender, MouseEventArgs e)
        {
            Panel view = (Panel)sender;

            // Switch to this view if in full screen view
            foreach (VlcControl vc in myVlcControl)
            {
                if (vc.Width >= this.ClientSize.Width)
                {
                    if (view.TabIndex != vc.TabIndex)
                    {
                        // Switch which Vlc control is full screen
                        setSizes(); //Temporary hack
                        vc.SendToBack();
                        SetVlcFullView(view.TabIndex);
                    }
                    break;
                }
            }
        }
    }
}
