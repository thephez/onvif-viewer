using System;
using System.Collections.Generic;
using RTSP_Viewer.OnvifMediaServiceReference;
using RTSP_Viewer.OnvifDeviceManagementServiceReference;
using log4net;

namespace SDS.Video.Onvif
{
    public class OnvifCameraData
    {
        /// <summary>
        /// Maximum time difference (in seconds) allowed between PC and device.  Onvif spec uses 5 seconds
        /// </summary>
        private static readonly int MaxTimeOffset = 5;

        public Profile MediaProfile { get; set; }
        public Dictionary<string, string> ServiceUris { get; private set; } = new Dictionary<string, string>();
        public Uri StreamUri { get; private set; }
        public Uri MulticastUri { get; private set; }
        public PTZConfiguration StreamPtzConfig { get { return MediaProfile.PTZConfiguration; } }
        private System.DateTime DeviceTime { get; set; }
        public System.DateTime LastTimeCheck { get; private set; }
        public double DeviceTimeOffset { get; private set; }

        public bool IsOnvifLoaded { get; private set; } = false;
        public bool IsPtz { get; private set; } = false;
        public bool IsPtzEnabled { get; private set; } = false;
        public OnvifPtz PtzController { get; private set; }

        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool LoadOnvifData(Camera cam, int onvifPort, StreamType sType, TransportProtocol tProtocol, int streamIndex)  // Probably should be private and done automatically
        {
            GetDeviceTime(cam, onvifPort);
            GetOnvifUris(cam, onvifPort);
            GetStreamUris(cam, onvifPort, sType, tProtocol, streamIndex);
            
            if (IsPtz)
            {
                PtzController = new OnvifPtz(ServiceUris[OnvifNamespace.MEDIA], ServiceUris[OnvifNamespace.PTZ], DeviceTimeOffset, MediaProfile, cam.User, cam.Password);
                // Get camera presets?
            }
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
        private void GetOnvifUris(Camera cam, int onvifPort)
        {
            ServiceUris.Clear();

            DeviceClient client = OnvifServices.GetOnvifDeviceClient(cam.IP, onvifPort, DeviceTimeOffset, cam.User, cam.Password);
            Service[] svc = client.GetServices(IncludeCapability: false); // Bosch Autodome 800 response can't be deserialized if IncludeCapability enabled
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
        private void GetStreamUris(Camera cam, int onvifPort, StreamType sType, TransportProtocol tProtocol, int StreamIndex)
        {
            //StreamUris.Clear();
            MediaClient mc = OnvifServices.GetOnvifMediaClient(ServiceUris[OnvifNamespace.MEDIA], DeviceTimeOffset, cam.User, cam.Password);
            Profile[] mediaProfiles = mc.GetProfiles();

            StreamSetup ss = new StreamSetup();
            Transport transport = new Transport() { Protocol = tProtocol };
            string uri = string.Empty;

            // Only store the Profile related to the StreamIndex from the XML file
            MediaProfile = mediaProfiles[StreamIndex - 1];

            // Get stream URI for the requested transport/protocol and insert the User/Password if present
            ss.Stream = sType;
            ss.Transport = transport;

            Uri mu = new Uri(mc.GetStreamUri(ss, MediaProfile.token).Uri);
            StreamUri = RTSP_Viewer.Classes.Utilities.InsertUriCredentials(mu, cam.User, cam.Password);

            // Get multicast uri (if available) along with requested protocol/stream type
            MulticastUri = GetMulticastUri(cam, mc, MediaProfile);  // Not being used currently
            MulticastUri = RTSP_Viewer.Classes.Utilities.InsertUriCredentials(MulticastUri, cam.User, cam.Password);

            // A PTZ may not have a PTZ configuration for a particular media profile
            // Disable PTZ access in that case
            IsPtzEnabled = IsPtz;
            if (MediaProfile.PTZConfiguration == null && IsPtz)
            {
                log.Warn(string.Format("Camera #{0} [{1}] Disabling PTZ control based on the PTZConfiguration being null for stream profile {0}", cam.Number, cam.IP, StreamUri));
                IsPtzEnabled = false;
            }
        }

        /// <summary>
        /// Get the device time. Per spec, the time in the Onvif header must be within 5 seconds of the device time
        /// Some devices will fail to authenticate or accept commands if the timestamp difference is too great
        /// </summary>
        /// <param name="ip">IP Address</param>
        /// <param name="onvifPort">Port to connect on (normally HTTP - 80)</param>
        public void GetDeviceTime(Camera cam, int onvifPort)
        {
            DeviceClient client = OnvifServices.GetOnvifDeviceClient(cam.IP, onvifPort, deviceTimeOffset: 0);
            SystemDateTime deviceTime = client.GetSystemDateAndTime();
            DeviceTime = new System.DateTime(
                deviceTime.UTCDateTime.Date.Year,
                deviceTime.UTCDateTime.Date.Month,
                deviceTime.UTCDateTime.Date.Day,
                deviceTime.UTCDateTime.Time.Hour,
                deviceTime.UTCDateTime.Time.Minute,
                deviceTime.UTCDateTime.Time.Second
                );

            LastTimeCheck = System.DateTime.UtcNow;

            DeviceTimeOffset = (DeviceTime - LastTimeCheck).TotalSeconds;
            if (Math.Abs(DeviceTimeOffset) > MaxTimeOffset)
            {
                log.Warn(string.Format("Camera #{0} [{1}] Time difference between PC and client [{2:0.0} seconds] exceeds MaxTimeOffset [{3:0.0} seconds].  ", cam.Number, cam.IP, DeviceTimeOffset, MaxTimeOffset));
            }
        }

        private Uri GetMulticastUri(Camera cam, MediaClient mediaClient, Profile mediaProfile)
        {

            if (mediaProfile?.VideoEncoderConfiguration?.Multicast != null && mediaProfile?.VideoEncoderConfiguration?.Multicast.Port != 0)
            {
                // Check for any URI supporting multicast
                foreach (TransportProtocol protocol in Enum.GetValues(typeof(TransportProtocol)))
                {
                    // Get stream URI for the requested transport/protocol and insert the User/Password if present
                    Transport transport = new Transport() { Protocol = protocol };
                    StreamSetup ss = new StreamSetup() { Stream = StreamType.RTPMulticast };
                    ss.Transport = transport;

                    try
                    {
                        MediaUri mu = mediaClient.GetStreamUri(ss, MediaProfile.token);
                        log.Debug(string.Format("Camera #{0} [{1}] Onvif media profile ({2}) capable of multicast [multicast URI: {3}]", cam.Number, cam.IP, mediaProfile.Name, mu.Uri));
                        return new Uri(mu.Uri);
                    }
                    catch { } // Ignore exception and continue checking for a multicast URI
                }
            }
            else
                log.Debug(string.Format("Camera #{0} [{1}] Onvif media profile ({2}) does not support multicast", cam.Number, cam.IP, mediaProfile.Name));

            return null;
        }

        // Example URI formats for various manufacturers (RTSP port = 554, uri = "rtsp://user:password@...")
        //  Bosch uri   = string.Format("{0}{1}:{2}/?h26x={3}&line={4}&inst={5}", uri, cam.IP, rtspPort, 4, cam.Device, cam.Stream);
        //  Axis uri    = string.Format("{0}{1}:{2}/onvif-media/media.amp", uri, cam.IP, rtspPort);
        //  Pelco uri   = string.Format("rtsp://{0}:{1}/stream{2}", cam.IP, rtspPort, cam.Stream);
        //  Samsung uri = string.Format("{0}{1}:{2}/onvif/profile{3}/media.smp", uri, cam.IP, rtspPort, cam.Stream);
    }
}
