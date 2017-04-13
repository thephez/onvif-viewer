using RTSP_Viewer.OnvifDeviceManagementServiceReference;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net;
using RTSP_Viewer.OnvifPtzServiceReference;
using SDS.WebServices.WsSecurity;

namespace SDS.Video.Onvif
{
    static class OnvifServices
    {

        public static DeviceClient GetOnvifDeviceClient(string ip, int port, double deviceTimeOffset, string username = "", string password = "")
        {
            EndpointAddress serviceAddress = new EndpointAddress(string.Format("http://{0}:{1}/onvif/device_service", ip, port));

            HttpTransportBindingElement httpBinding = new HttpTransportBindingElement();
            httpBinding.AuthenticationScheme = AuthenticationSchemes.Digest;

            var messageElement = new TextMessageEncodingBindingElement();
            messageElement.MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None);
            CustomBinding bind = new CustomBinding(messageElement, httpBinding);

            DeviceClient deviceClient = new DeviceClient(bind, serviceAddress);

            if (username != string.Empty)
            {
                // Handles adding of SOAP Security header containing User Token (user, nonce, pwd digest)
                PasswordDigestBehavior behavior = new PasswordDigestBehavior(username, password, deviceTimeOffset);
                deviceClient.Endpoint.Behaviors.Add(behavior);
            }

            return deviceClient;
        }

        public static RTSP_Viewer.OnvifMediaServiceReference.MediaClient GetOnvifMediaClient(string Uri, double deviceTimeOffset, string username = "", string password = "")
        {
            EndpointAddress serviceAddress = new EndpointAddress(Uri);

            HttpTransportBindingElement httpBinding = new HttpTransportBindingElement();
            httpBinding.AuthenticationScheme = AuthenticationSchemes.Digest;

            var messageElement = new TextMessageEncodingBindingElement();
            messageElement.MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None);
            CustomBinding bind = new CustomBinding(messageElement, httpBinding);

            RTSP_Viewer.OnvifMediaServiceReference.MediaClient mediaClient = new RTSP_Viewer.OnvifMediaServiceReference.MediaClient(bind, serviceAddress);

            if (username != string.Empty)
            {
                // Handles adding of SOAP Security header containing User Token (user, nonce, pwd digest)
                PasswordDigestBehavior behavior = new PasswordDigestBehavior(username, password, deviceTimeOffset);
                mediaClient.Endpoint.Behaviors.Add(behavior);
            }

            return mediaClient;
        }

        public static PTZClient GetOnvifPTZClient(string Uri, double deviceTimeOffset, string username = "", string password = "")
        {
            EndpointAddress serviceAddress = new EndpointAddress(Uri);

            HttpTransportBindingElement httpBinding = new HttpTransportBindingElement();
            httpBinding.AuthenticationScheme = AuthenticationSchemes.Digest;

            var messageElement = new TextMessageEncodingBindingElement();
            messageElement.MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None);
            CustomBinding bind = new CustomBinding(messageElement, httpBinding);

            PTZClient ptzClient = new PTZClient(bind, serviceAddress);

            if (username != string.Empty)
            {
                // Handles adding of SOAP Security header containing User Token (user, nonce, pwd digest)
                PasswordDigestBehavior behavior = new PasswordDigestBehavior(username, password, deviceTimeOffset);
                ptzClient.Endpoint.Behaviors.Add(behavior);
            }

            return ptzClient;
        }
    }
}
