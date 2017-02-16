using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;
//using Bosch.VideoSDK.AxCameoLib;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.IO;

namespace SDS.Video
{
    public class Camera
    {
        private int CameraNumber;
        private String cameraIP;
        private int StreamIndex;
        private int DeviceIndex;
        public string Manufacturer { get; set; }
        private bool isConnected = false;
        private bool dataLoaded = false;

        //private Bosch.VideoSDK.Live.CameraController controller;

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

        public static void GenerateHashTable()
        {
            Camera c;

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
                    String line = info != null ? info.LineNumber.ToString() : "not known";
                    System.Windows.Forms.MessageBox.Show("Cameras.xml validation failure on line " + line + ": " + vargs.Message.Replace("\t", "").Replace("\n", ""));
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
                              Manufacturer = camera.Element("manufacturer").Value.Trim(),
                          };

            foreach (var cam in cameras)
            {
                c = new Camera(int.Parse(cam.Number));

                c.Stream = 1; //cam.Stream.Equals("default", StringComparison.CurrentCultureIgnoreCase) ? Global_Values.DefaultVideoStream : int.Parse(cam.Stream);
                c.IP = cam.IP;
                c.Device = int.Parse(cam.Device);
                c.dataLoaded = true;
                c.Manufacturer = cam.Manufacturer;

                try
                {
                    cameraSet.Add(c.Number, c);
                }
                catch
                {
                    logger.Error("Error adding camera value to hash table. This may be a collision (a repeated camera number) or " +
                        "an invalid camera number. Camera number used was " + int.Parse(cam.Number));
                }
            }
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

        //public Bosch.VideoSDK.Live.CameraController Controller
        //{
        //    set { controller = value; }
        //    get { return controller; }
        //}

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
            GenerateHashTable();
        }

        public Image TakeScreenshot()
        {
            Image im = null;
            try
            {
                String reqURL = "http://" + IP + "/snap.jpg?JpegSize=XL&JpegCam=" + Device;
                WebRequest req = HttpWebRequest.Create(reqURL);
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
