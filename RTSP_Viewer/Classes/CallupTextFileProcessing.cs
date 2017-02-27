using System;
using System.Threading;
using System.IO;
using My.Extensions;

namespace SDS.Video
{
    class CallupsTxtFile
    {
        private static log4net.ILog logger;

        // You must use a delegate to update the video connection
        // information in the video control.
        //public delegate void SetCameraCallback(int DisplayIndex, int Camera, int Preset);
        public delegate void SetRtspCallupCallback(int DisplayIndex, int Camera, int Preset); // string URI, int DisplayIndex);
        //public delegate void SetCameraScreenshot(int DisplayIndex);
        public delegate void SetSequenceProgram(bool isProgramming, int displayIndex);
        public delegate bool GetSequenceProgram(int displayIndex);

        private SetRtspCallupCallback rtspCallupDelegate;
        //private SetCameraCallback callbackDelegate;
        //private SetCameraScreenshot screenshotDelegate;
        private SetSequenceProgram sequenceSetDelegate;
        private GetSequenceProgram sequenceGetDelegate;

        //private Thread WatchCallupFileThread;
        public FileInfo CallupsFilePath;
        private FileSystemWatcher watcher = new FileSystemWatcher();

        public CallupsTxtFile(SetRtspCallupCallback SetDisplayCamera)
        {
            CallupsFilePath = new FileInfo(@".\callup.txt"); // Global_Values.CallupsFilePath);
            watcher.Path = CallupsFilePath.DirectoryName;
            watcher.IncludeSubdirectories = false;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = CallupsFilePath.Name;
            watcher.Changed += CallupFile_OnChange;
            watcher.EnableRaisingEvents = true;

            rtspCallupDelegate = SetDisplayCamera;
            logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        private void CallupFile_OnChange(object sender, FileSystemEventArgs e)
        {
            if (logger.IsInfoEnabled)
                logger.Info(string.Format("Change Detected in file [{0}]", e.FullPath));

            try
            {
                ReadCallupsTextFile();
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message);
            }
        }

        //public CallupsTxtFile(SetCameraCallback SetDisplayCamera, SetCameraScreenshot SetDisplayScreenshot, SetSequenceProgram SetDisplayProgram, GetSequenceProgram GetDisplayProgram)
        //{
        //    CallupsFilePath = new FileInfo(Global_Values.CallupsFilePath);
        //    this.callbackDelegate = SetDisplayCamera;
        //    this.screenshotDelegate = SetDisplayScreenshot;
        //    this.sequenceSetDelegate = SetDisplayProgram;
        //    this.sequenceGetDelegate = GetDisplayProgram;
        //    logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //}

        //public void StartInterface()
        //{
        //    InitializeCallupsTextFile();

        //    if (WatchCallupFileThread != null)
        //    {
        //        if (WatchCallupFileThread.IsAlive)
        //            return;
        //    }
        //    // Create the HMI update thread
        //    WatchCallupFileThread = new Thread(WatchCallupFile);

        //    //   Make sure the HMI update thread is a background thread. This makes sure the 
        //    //  thread is terminated when the form is closed.
        //    WatchCallupFileThread.IsBackground = true;
        //    WatchCallupFileThread.SetApartmentState(ApartmentState.STA);
        //    WatchCallupFileThread.Start();
        //    if (logger.IsInfoEnabled)
        //        logger.Info("Watch Callup.txt file thread started.");
        //}

        //private static int callbacks = 0;

        private int sleepTime = 100;
        public int SleepTime
        {
            set { sleepTime = value; }
        }

        //public void WatchCallupFile()
        //{
        //    while (true)
        //    {
        //        string filePath = System.Reflection.Assembly.GetEntryAssembly().Location + "callups.txt"; // Global_Values.CallupsFilePath;
        //        try
        //        {
        //            CallupsFilePath.Refresh();
        //            DateTime OldDateTime = CallupsFilePath.LastWriteTimeUtc; // File.GetLastWriteTimeUtc(filePath);
        //            try
        //            {
        //                Thread.Sleep(sleepTime);
        //            }
        //            catch { }

        //            if (OldDateTime != CallupsFilePath.LastWriteTimeUtc) // File.GetLastWriteTimeUtc(filePath))
        //            {
        //                Thread.Sleep(sleepTime); // Global_Values.PollingInterval);
        //                ReadCallupsTextFile();
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            string writetext = "Error loading Callups.txt file ----- Execption Message: " + e.Message;
        //            if (logger.IsInfoEnabled)
        //                logger.Info(writetext);

        //            try
        //            {
        //                Thread.Sleep(15000); // Global_Values.RetryInterval);
        //            }
        //            catch { }
        //        }
        //    }
        //}
        
        public void ReadCallupsTextFile()
        {
            string readText = File.ReadAllText(CallupsFilePath.FullName); // Global_Values.CallupsFilePath);
            readText = readText.Replace("\r", string.Empty);
            readText = readText.Replace("\n", string.Empty);
            if (logger.IsInfoEnabled)
                logger.Info("Change Detected in Callups.txt: " + readText);

            try
            {
                string[] CameraCallups = readText.Split(';', ':', '\r');
                string FirstChar = CameraCallups[0].Left(1);
                if (FirstChar.Equals("M", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string callup in CameraCallups)
                    {
                        try
                        {
                            string[] Callup = callup.Split('M', 'C', 'P', 'm', 'c', 'p', '\r');
                            int Monitor = Convert.ToInt32(Callup[1]); // - 1;
                            int Camera = Convert.ToInt32(Callup[2]);
                            int Preset = Convert.ToInt32(Callup[3]);
                            //callbackDelegate(Monitor, Camera, Preset);
                            rtspCallupDelegate(Monitor, Camera, Preset);

                            string writetext = "Callup Command-- " + readText + " --Processed Correctly -- " + DateTime.Now;
                            File.WriteAllText(CallupsFilePath.Name + ".status", writetext); // Global_Values.CallupsFilePath, writetext);

                            //sequenceSetDelegate(True, Monitor);
                        }
                        catch (Exception e)
                        {
                            string ErrorText = string.Format("Error with Callup String [{0}] ----- \n\n{1}\nCorrect Format for single camera: 'M1 C15 P2' \nMultiple Cameras: 'M1 C15 P2;M2 C12 P1' \nTurning on/off sequence: 'Sequencing:On:M1' or 'Sequencing:Off:M1'", callup, e.Message);
                            if (logger.IsInfoEnabled)
                                logger.Info(ErrorText);
                            System.Windows.Forms.MessageBox.Show(ErrorText, "Callups.txt file Error");
                            File.WriteAllText(CallupsFilePath.Name + ".status", ErrorText); // Global_Values.CallupsFilePath, ErrorText);
                        }
                    }
                }
                else if (CameraCallups[0].Equals("Sequencing", StringComparison.OrdinalIgnoreCase))
                {
                    //There are two types of Sequences that can be set up:
                    //Static sequences:
                    //If you look in the configfiles folder (same location as the config.xml file) there should be a sequences.xml file. 
                    //In there you can define a static sequence. I believe it is just a sequence number and then a set of cameras defined 
                    //as camera number, dwell time, preshot number. To call it up, you just do a regular camera callup 
                    //using the sequence number as the camera number. (From a programmatic standpoint a camera callup is a sequence callup 
                    //with only one camera in the sequence). The program checks the sequences file for that number and if its not there 
                    //then it checks cameras.xml. For that reason, there should be no collision between camera numbers and sequence numbers.
                    //Ex:
                    //<?xml version="1.0"?>
                    //<catalog>
                    //    <sequence number="10000">
                    //        <number>10000</number>
                    //        <sequenceNode>
                    //            <camera>1</camera>
                    //            <preset>1</preset>
                    //            <dwelltime>5000</dwelltime>
                    //        </sequenceNode>
                    //        <sequenceNode>
                    //            <camera>2</camera>
                    //            <preset>1</preset>
                    //            <dwelltime>3000</dwelltime>
                    //        </sequenceNode>
                    //    </sequence>
                    //    <sequence number="10001">
                    //    <number>10001</number>
                    //        <sequenceNode>
                    //            <camera>1</camera>
                    //            <preset>0</preset>
                    //            <dwelltime>10000</dwelltime>
                    //        </sequenceNode>
                    //        <sequenceNode>
                    //            <camera>2</camera>
                    //            <preset>0</preset>
                    //            <dwelltime>1000</dwelltime>
                    //        </sequenceNode>		
                    //    </sequence>
                    //</catalog>

                    //Dynamic sequences:
                    //To turn on/off sequenceing put the appropriate string: 'Sequencing:On:M1' or 'Sequencing:Off:M1' into the callup .txt file
                    //After the 'Sequencing:On:M1' command is set, any camera callups sent to the display for which that command was associated 
                    //will be added to the current sequence of cameras for that display. The dwell time will be the default value 
                    //(Global_Values.DefaultSequenceDwellTime), as set in the config file. If a preset number is sent in the callup command, then
                    //the sequence will also use this preset number. Once the off command ('Sequencing:Off:M1') is sent, the next camera callup 
                    //sent to that monitor will be executed as normal (i.e. it will not switch away from it unless the user calls up another camera).
                    //The defined tour will not be saved.

                    string[] Callup = CameraCallups[2].Split('M', 'm');
                    int Monitor = Convert.ToInt32(Callup[1]) - 1;
                    if (CameraCallups[1].Equals("On", StringComparison.OrdinalIgnoreCase))
                    {
                        sequenceSetDelegate(true, Monitor);
                    }
                    else if (CameraCallups[1].Equals("Off", StringComparison.OrdinalIgnoreCase))
                    {
                        sequenceSetDelegate(false, Monitor);
                    }
                }
                else
                {
                    string ErrorText = "Error with Callup String, incorrect format." + '\n' + "Correct Format for single camera: M1 C15 P2" + '\n' + "Multiple Cameras: M1 C15 P2;M2 C12 P1" + '\n' + "Turning on/off sequence: 'Sequencing:On:M1' or 'Sequencing:Off:M1'";
                    if (logger.IsInfoEnabled)
                        logger.Info(ErrorText);
                    System.Windows.Forms.MessageBox.Show(ErrorText, "Callups.txt file Error");
                    File.WriteAllText(CallupsFilePath.Name + ".status", ErrorText); // Global_Values.CallupsFilePath, ErrorText);
                }
            }
            catch (Exception e)
            {
                string ErrorText = "Error with Callup String ----- " + e.Message + '\n' + "Correct Format for single camera: M1 C15 P2" + '\n' + "Multiple Cameras: M1 C15 P2;M2 C12 P1" + '\n' + "Turning on/off sequence: 'Sequencing:On:M1' or 'Sequencing:Off:M1'";
                if (logger.IsInfoEnabled)
                    logger.Info(ErrorText);
                System.Windows.Forms.MessageBox.Show(ErrorText, "Callups.txt file Error");
                File.WriteAllText(CallupsFilePath.Name + ".status", ErrorText); // Global_Values.CallupsFilePath, ErrorText);
            }
        }

        public void InitializeCallupsTextFile()
        {
            try
            {
                string writetext = "Quad Program Started and can write to the Callups .txt file ----- " + DateTime.Now;
                File.WriteAllText(CallupsFilePath.FullName, writetext); // Global_Values.CallupsFilePath, writetext);
            }
            catch (Exception e)
            {
                string ErrorText = "Error loading Callups .txt file on startup of quad.exe." + e.Message;
                System.Windows.Forms.MessageBox.Show(ErrorText, "Callups.txt file Error");
                if (logger.IsInfoEnabled)
                    logger.Info(ErrorText);
            }
        }
    }
}