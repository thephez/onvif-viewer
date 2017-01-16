using System;
using Opc.Ua.Client;
using OPCUA_Integration_Core;
using Opc.Ua.Configuration;

namespace RTSP_Viewer.Classes
{
    class OpcUaClient
    {
        static UaClient opcuaClient = new UaClient();
        public delegate void SetCameraCallup(string URI, int ViewerNum);
        private SetCameraCallup callupDelegate;

        private TagDatabase tagDB = new TagDatabase();
        private string tagPath;

        public OpcUaClient(SetCameraCallup CallupDelegate)
        {
            this.callupDelegate = CallupDelegate;
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
            this.tagPath = tagPath;

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
            opcuaClient.ClientDisconnect();
        }

        private void dataReturned(object sender, EventArgs e)
        {
            MonitoredItem monitoredItem = (MonitoredItem)sender;
            foreach (var value in monitoredItem.DequeueValues())
            {
                string message = string.Format("\n{0}\t{1}\t{2}\t{3}\n", monitoredItem.DisplayName, value.Value, value.SourceTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), value.StatusCode);
                Console.WriteLine(message);
                tagDB.UpdateTagValue(monitoredItem.DisplayName, value.Value.ToString());

                if (monitoredItem.DisplayName.EndsWith("Trigger")) // == "Camera01Trigger")
                {
                    if ((bool)value.Value == true)
                    {
                        // Reset trigger
                        opcuaClient.WriteValueToPath(string.Format("{0}/{1}", tagPath, monitoredItem.DisplayName), false);

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
                            Console.WriteLine(string.Format("Tag not found [{0}].  Callup cannot be performed.", CameraUriTag));
                        }
                    }
                }
            }
        }
    }
}
