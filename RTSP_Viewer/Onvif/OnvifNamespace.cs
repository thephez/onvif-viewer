namespace SDS.Video.Onvif
{
    /// <summary>
    /// Onvif Namespaces as defined by Onvif Core Specification - Section 5.3
    /// Based on Version 16.12 - December 2016
    /// Available at: https://www.onvif.org/profiles/specifications/
    /// </summary>
    static class OnvifNamespace
    {
        public const string DEVICE = "http://www.onvif.org/ver10/device/wsdl";
        public const string MEDIA = "http://www.onvif.org/ver10/media/wsdl";
        public const string EVENTS = "http://www.onvif.org/ver10/events/wsdl";
        public const string PTZ = "http://www.onvif.org/ver20/ptz/wsdl";
    }
}
