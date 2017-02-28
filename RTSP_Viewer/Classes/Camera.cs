﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Net;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.IO;

namespace SDS.Video
{
    public class Camera
    {
        private int CameraNumber;
        private string cameraIP;
        private int StreamIndex;
        private int DeviceIndex;
        public string Manufacturer { get; set; }
        private bool isConnected = false;
        private bool dataLoaded = false;

        private string User;
        private string Password;
        public static string DefaultManufacturer { get; set; } = "Bosch";  // Not sure we want this to be a static field

        private static Dictionary<int, Camera> cameraSet = new Dictionary<int, Camera>();

        private Camera()
        {
            // Private constructor to prevent direct instantiation. use GetCamera method.
        }

        public static Camera GetCamera(int cameraNumber)
        {
            return GetCamera(cameraNumber, 0);
        }

        public static Camera GetCamera(int cameraNumber, int preset)
        {
            Camera cam;
            if (cameraSet.ContainsKey(cameraNumber))
            {
                cam = cameraSet[cameraNumber];
                cam.CameraPreset = preset;
                return cam;
            }
            else
            {
                log4net.ILog logger;
                logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.Error("Tried to call up camera " + cameraNumber + " but it is not in the database.");
                return null;
            }
        }

        /// <summary>
        /// Get RTSP URI for the requested camera number. 
        /// Based on Manufacturer and other info from XML file
        /// </summary>
        /// <param name="cameraNumber">Camera to get RTSP URI for</param>
        /// <returns>RTSP URI that can be used to display live video</returns>
        public static string GetRtspUri(int cameraNumber)
        {
            Camera cam = GetCamera(cameraNumber);
            int rtspPort = 554;
            string uri = null;

            if (cam.User != null && cam.Password != null)
                uri = string.Format("rtsp://{0}:{1}@", cam.User, cam.Password);
            else
                uri = "rstp://";


            if (cam.Manufacturer.Equals("Bosch", StringComparison.CurrentCultureIgnoreCase))
            {
                uri = string.Format("{0}{1}:{2}/?h26x={3}&line={4}&inst={5}", uri, cam.IP, rtspPort, 4, cam.Device, cam.Stream);
            }
            else if (cam.Manufacturer.Equals("Axis", StringComparison.CurrentCultureIgnoreCase))
            {
                //uri = string.Format("rtsp://{0}:{1}@{2}:{3}/onvif-media/media.amp", "onvif", "Sierra123", cam.IP, rtspPort);
                uri = string.Format("{0}{1}:{2}/onvif-media/media.amp", uri, cam.IP, rtspPort);
            }
            else if (cam.Manufacturer.Equals("Pelco", StringComparison.CurrentCultureIgnoreCase))
            {
                uri = string.Format("rtsp://{0}:{1}/stream{2}", cam.IP, rtspPort, cam.Stream);
            }
            else if (cam.Manufacturer.Equals("Samsung", StringComparison.CurrentCultureIgnoreCase))
            {
                //uri = string.Format("rtsp://{0}:{1}@{2}:{3}/onvif/profile{4}/media.smp", "onvif", "Sierra123", cam.IP, rtspPort, cam.Stream);
                uri = string.Format("{0}{1}:{2}/onvif/profile{3}/media.smp", uri, cam.IP, rtspPort, cam.Stream);
            }
            else
            {
                throw new Exception(string.Format("Camera manufacturer '{0}' not recognized.", cam.Manufacturer),
                    new Exception(string.Format("Unable to create RTSP URI for manufacturer '{0}'.", cam.Manufacturer)));
            }

            return uri;
        }

        /// <summary>
        /// Creates a dictionary containing all cameras found in the cameras XML file
        /// </summary>
        /// <param name="defaultManufacturer">Manufacturer to use if not listed in camera XML file</param>
        public static void GenerateHashTable(string defaultManufacturer)
        {
            Camera c;
            int defaultStream = 1; // Global_Values.DefaultVideoStream
            DefaultManufacturer = defaultManufacturer;

            cameraSet.Clear();

            log4net.ILog logger;
            logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            //XDocument doc = XDocument.Load(Global_Values.CameraFile, LoadOptions.SetLineInfo);
            XDocument doc = XDocument.Load("cameras.xml", LoadOptions.SetLineInfo);
            XmlSchemaSet schemas = new XmlSchemaSet();
            //schemas.Add("", XmlReader.Create(new StreamReader(Global_Values.CameraSchema)));
            schemas.Add("", XmlReader.Create(new StreamReader("cameras.xsd")));

            try
            {
                doc.Validate(schemas, (sender, vargs) =>
                {
                    IXmlLineInfo info = sender as IXmlLineInfo;
                    string line = info != null ? info.LineNumber.ToString() : "not known";
                    System.Windows.Forms.MessageBox.Show(string.Format("Cameras.xml validation failure on line {0}: {1}", line, vargs.Message.Replace("\t", "").Replace("\n", "")));
                },
                true);
            }
            catch (XmlSchemaValidationException e)
            {
                System.Windows.Forms.MessageBox.Show("Camera Database Validation Error: " + e.Message, "Validation Error");
            }

            var cameras = from camera in doc.Descendants("camera")
                          select new
                          {
                              IP = camera.Element("ip").Value.Trim(),
                              Stream = camera.Element("stream").Value.Trim(),
                              Device = camera.Element("device").Value.Trim(),
                              Number = camera.Element("number").Value.Trim(),
                              Manufacturer = (string)camera.Element("manufacturer") ?? DefaultManufacturer,
                              User = (string)camera.Element("username"),
                              Password = (string)camera.Element("password"),
                          };

            foreach (var cam in cameras)
            {
                c = new Camera(int.Parse(cam.Number));

                c.Stream = cam.Stream.Equals("default", StringComparison.CurrentCultureIgnoreCase) ? defaultStream : int.Parse(cam.Stream);
                c.IP = cam.IP;
                c.Device = int.Parse(cam.Device);
                c.dataLoaded = true;
                c.Manufacturer = cam.Manufacturer == null ? "Not provided" : cam.Manufacturer;
                c.User = cam.User;
                c.Password = cam.Password;

                try
                {
                    cameraSet.Add(c.Number, c);
                }
                catch (Exception ex)
                {
                    logger.Error(string.Format("Error adding camera value to hash table. This may be a collision (a repeated camera number) " +
                        " or an invalid camera number. Camera number used was {0}.", int.Parse(cam.Number)));
                }
            }
            logger.Info(string.Format("Imported information for {0} camera(s) from xml file", cameraSet.Count));
        }

        private static Camera LookupCamera(int i)
        {
            return cameraSet[i];
        }

        public bool IsDataLoaded
        {
            get { return dataLoaded; }
        }

        public bool IsConnected
        {
            set { isConnected = value; }
            get { return isConnected; }
        }

        public String IP
        {
            set { cameraIP = value; }
            get { return cameraIP; }
        }

        public int Number
        {
            set { CameraNumber = value; }
            get { return CameraNumber; }
        }

        public int Device
        {
            set { DeviceIndex = value; }
            get { return DeviceIndex; }
        }

        public int Stream
        {
            set { StreamIndex = value; }
            get { return StreamIndex; }
        }

        public int CameraPreset
        {
            set;
            get;
        }

        public Camera(int CameraNumber)
        {
            this.CameraNumber = CameraNumber;
        }

        public void reloadData()
        {
            GenerateHashTable(DefaultManufacturer);
        }

        public Image TakeScreenshot()
        {
            Image im = null;
            try
            {
                string reqURL = "http://" + IP + "/snap.jpg?JpegSize=XL&JpegCam=" + Device;
                WebRequest req = WebRequest.Create(reqURL);
                WebResponse resp = req.GetResponse();

                im = Image.FromStream(resp.GetResponseStream());
            }
            catch { }
            return im;
        }

        public override string ToString()
        {
            string str = string.Format("Camera # {0} ({1}, Stream {2}, Device Index {3}, Manufacturer {4})", CameraNumber, cameraIP, StreamIndex, DeviceIndex, Manufacturer);
            return str;
        }

    }
}