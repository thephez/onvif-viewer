using System;
using Opc.Ua.Client;
using OPCUA_Integration_Core;
using System.Threading;
using log4net;

namespace RTSP_Viewer.Classes
{
    class OpcUaClient
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static UaClient opcuaClient = new UaClient();
        public delegate void SetCameraCallup(Uri URI, int ViewerNum);
        private SetCameraCallup callupDelegate;
        private Thread opcThread;

        private TagDatabase tagDB = new TagDatabase();
        private string EndPointURL;
        private string TagPath;

        public int SleepTime { get; set; }

        public OpcUaClient(SetCameraCallup CallupDelegate)
        {
            this.callupDelegate = CallupDelegate;
            if (SleepTime < 1)
                SleepTime = 5000;
        }

        private void Connect()
        {
            // Use 1000 as default publish interval unless overridden by INI file
            int publishInterval = 250;

            // Instantiate client and set up a connection to server
            opcuaClient.ClientConnect(EndPointURL);

            if (opcuaClient.Session != null)
            {
                // Configure an event handler to process data received back from subscriptions
                opcuaClient.DataReturned += new EventHandler(dataReturned);

                // Example of subscribing to all objects/variables found in the provided path
                opcuaClient.SubscribeToTagsInPath(TagPath, publishInterval);
            }
            else
            {
                log.Error("OPC connection failure. Can't subscribe to tags.");
            }
        }

        /// <summary>
        /// Establishes a session with the provided endpoint (i.e. "opc.tcp://127.0.0.1:4840/freeopcua/server/", etc)
        /// and subscribe to all items found in the tagPath
        /// </summary>
        /// <param name="endPointURL">URI of the OPC server</param>
        /// <param name="tagPath">OPC server path containing items to subscribe to</param>
        public void Connect(string endPointURL, string tagPath)
        {
            // OPC server path to subscribe to (i.e. "/0:Tags")
            this.TagPath = tagPath;

            // Use 1000 as default publish interval unless overridden by INI file
            int publishInterval = 250;

            // Instantiate client and set up a connection to server
            opcuaClient.ClientConnect(endPointURL);

            // Configure an event handler to process data received back from subscriptions
            opcuaClient.DataReturned += new EventHandler(dataReturned);

            // Example of subscribing to all objects/variables found in the provided path
            opcuaClient.SubscribeToTagsInPath(tagPath, publishInterval);
        }

        public void Disconnect()
        {
            // Unsubscribe and disconnect from the server
            if (opcuaClient?.Session != null)
            {
                opcuaClient.ClientDisconnect();
            }
        }

        public void StartInterface(string endPointURL, string tagPath)
        {
            this.EndPointURL = endPointURL;
            this.TagPath = tagPath;
            if (opcThread != null)
            {
                if (opcThread.IsAlive)
                    return;
            }

            opcThread = new Thread(Connect);

            opcThread.IsBackground = true;

            // Need to also monitor the connection and re-establish if disconnected
            opcThread.Start();
            log.Info("OPC Thread started");
        }

        // Doesn't work
        public void MonitorOpc()
        {
            while (true)
            {
                if (opcuaClient?.Session?.Connected != true)
                {
                    try
                    {
                        Connect();
                    }
                    catch (Exception e)
                    {
                        // Connection failed.  Wait and try again
                        Console.WriteLine(string.Format("Connection failed. Waiting {0}ms. {1}", SleepTime, e.Message));
                    }
                    return;
                }
                Thread.Sleep(SleepTime);
            }
        }

        private void dataReturned(object sender, EventArgs e)
        {
            MonitoredItem monitoredItem = (MonitoredItem)sender;
            foreach (var value in monitoredItem.DequeueValues())
            {
                string message = string.Format("{0}={1}\t[Source Time: {2}, Status: {3}]", monitoredItem.DisplayName, value.Value, value.SourceTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), value.StatusCode);
                log.Debug(message);
                tagDB.UpdateTagValue(monitoredItem.DisplayName, value.Value.ToString());

                if (monitoredItem.DisplayName.EndsWith("Trigger")) // == "Camera01Trigger")
                {
                    if ((bool)value.Value == true)
                    {
                        // Reset trigger
                        opcuaClient.WriteValueToPath(string.Format("{0}/{1}", TagPath, monitoredItem.DisplayName), false);

                        string tagRoot = monitoredItem.DisplayName.Substring(0, monitoredItem.DisplayName.Length - "Trigger".Length);
                        string CameraUriTag = tagRoot + "CameraURI";

                        // If the tag exists and has a valid value, attempt a callup
                        if (tagDB.Tags.Exists(x => x.name == CameraUriTag))
                        {
                            var tagValue = tagDB.Tags.Find(item => item.name == CameraUriTag).value;
                            if (tagValue != null && tagValue != "")
                            {
                                int viewerNum = int.Parse(tagRoot.Substring(tagRoot.Length - 2, 2)) - 1;
                                callupDelegate(tagValue, viewerNum);
                            }
                        }
                        else
                        {
                            // Tag not yet assigned
                            log.Warn(string.Format("Tag not found [{0}].  Callup cannot be performed.", CameraUriTag));
                        }
                    }
                }
            }
        }
    }
}