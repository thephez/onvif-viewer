using System.Collections.Generic;
using RTSP_Viewer.OnvifMediaServiceReference;
using RTSP_Viewer.OnvifDeviceManagementServiceReference;

namespace SDS.Video.Onvif
{
    public class OnvifCameraData
    {
        public Profile MediaProfile { get; set; }
        public Dictionary<string, string> ServiceUris { get; private set; } = new Dictionary<string, string>();
        //public List<string> StreamUris { get; private set; } = new List<string>();
        public string StreamUri { get; set; }

        public bool IsOnvifLoaded { get; private set; } = false;
        public bool IsPtz { get; private set; } = false;

        public bool LoadOnvifData(string IP, int onvifPort, string user, string password, StreamType sType, TransportProtocol tProtocol, int streamIndex)  // Probably should be private and done automatically
        {
            GetOnvifUris(IP, 80, user, password);
            GetStreamUris(IP, 80, user, password, sType, tProtocol, streamIndex);
            IsOnvifLoaded = true;

            return true;
        }

        /// <summary>
        /// Retrieves Onvif service URIs from the device and stores them in the ServiceUris dictionary
        /// </summary>
        /// <param name="ip">IP Address</param>
        /// <param name="onvifPort">Port to connect on (normally HTTP - 80)</param>
        /// <param name="user">User name</param>
        /// <param name="password">User's Password</param>
        private void GetOnvifUris(string ip, int onvifPort, string user, string password)
        {
            ServiceUris.Clear();

            DeviceClient client = OnvifServices.GetOnvifDeviceClient(ip, onvifPort, user, password);
            Service[] svc = client.GetServices(IncludeCapability: true);
            foreach (Service s in svc)
            {
                ServiceUris.Add(s.Namespace, s.XAddr);
            }

            // Check if this is an Onvif enabled PTZ
            if (ServiceUris.ContainsKey(OnvifNamespace.PTZ))
                IsPtz = true;
            else
                IsPtz = false;
        }

        /// <summary>
        /// Retrieves Onvif video stream URIs from the device and stores them in the StreamUris list
        /// </summary>
        /// <param name="onvifPort">Port to connect on (normally HTTP - 80)</param>
        private void GetStreamUris(string ip, int onvifPort, string user, string password, StreamType sType, TransportProtocol tProtocol, int StreamIndex)
        {
            //StreamUris.Clear();
            MediaClient mc = OnvifServices.GetOnvifMediaClient(ServiceUris[OnvifNamespace.MEDIA], user, password);
            Profile[] mediaProfiles = mc.GetProfiles();

            StreamSetup ss = new StreamSetup();
            Transport transport = new Transport() { Protocol = tProtocol };
            string uri = string.Empty;

            // Only store the Profile related to the StreamIndex from the XML file
            MediaProfile = mediaProfiles[StreamIndex - 1];

            // Get stream URI for the requested transport/protocol and insert the User/Password if present
            ss.Stream = sType;
            ss.Transport = transport;

            MediaUri mu = mc.GetStreamUri(ss, MediaProfile.token);
            if (user != "")
                uri = string.Format("{0}{1}:{2}@{3}", mu.Uri.Substring(0, mu.Uri.IndexOf("://") + 3), user, password, mu.Uri.Substring(mu.Uri.IndexOf("://") + 3));
            else
                uri = mu.Uri;

            StreamUri = uri;

            //foreach (Profile p in mediaProfiles)
            //{
            //    // Get stream URI for the requested transport/protocol
            //    ss.Stream = sType;
            //    ss.Transport = transport;
            //    MediaUri mu = mc.GetStreamUri(ss, p.token);
            //    if (User != "")
            //        uri = string.Format("{0}{1}:{2}@{3}", mu.Uri.Substring(0, mu.Uri.IndexOf("://") + 3), User, Password, mu.Uri.Substring(mu.Uri.IndexOf("://") + 3));
            //    else
            //        uri = mu.Uri;

            //    StreamUris.Add(uri);
            //}
        }

        ///// <summary>
        ///// Get RTSP URI for the requested camera number. 
        ///// Based on Manufacturer and other info from XML file
        ///// </summary>
        ///// <param name="cameraNumber">Camera to get RTSP URI for</param>
        ///// <returns>RTSP URI that can be used to display live video</returns>
        //public static string GetRtspUri(int cameraNumber)
        //{
        //    Camera cam = GetCamera(cameraNumber);

        //    int rtspPort = 554;
        //    string uri = null;

        //    if (cam.User != null && cam.Password != null)
        //        uri = string.Format("rtsp://{0}:{1}@", cam.User, cam.Password);
        //    else
        //        uri = "rtsp://";

        //    if (cam.Manufacturer.Equals("Bosch", StringComparison.CurrentCultureIgnoreCase))
        //    {
        //        uri = string.Format("{0}{1}:{2}/?h26x={3}&line={4}&inst={5}", uri, cam.IP, rtspPort, 4, cam.Device, cam.Stream);
        //    }
        //    else if (cam.Manufacturer.Equals("Axis", StringComparison.CurrentCultureIgnoreCase))
        //    {
        //        uri = string.Format("{0}{1}:{2}/onvif-media/media.amp", uri, cam.IP, rtspPort);
        //    }
        //    else if (cam.Manufacturer.Equals("Pelco", StringComparison.CurrentCultureIgnoreCase))
        //    {
        //        uri = string.Format("rtsp://{0}:{1}/stream{2}", cam.IP, rtspPort, cam.Stream);
        //    }
        //    else if (cam.Manufacturer.Equals("Samsung", StringComparison.CurrentCultureIgnoreCase))
        //    {
        //        uri = string.Format("{0}{1}:{2}/onvif/profile{3}/media.smp", uri, cam.IP, rtspPort, cam.Stream);
        //    }
        //    else
        //    {
        //        throw new Exception(string.Format("Camera manufacturer '{0}' not recognized.", cam.Manufacturer),
        //            new Exception(string.Format("Unable to create RTSP URI for manufacturer '{0}'.", cam.Manufacturer)));
        //    }

        //    return uri;
        //}
    }
}
