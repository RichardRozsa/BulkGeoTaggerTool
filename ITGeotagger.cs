using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using com.drew.imaging.jpg;
using com.drew.imaging.tiff;
using com.drew.metadata;
using log4net;
using SharpKml.Base;
using SharpKml.Dom;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;
using MissionPlanner.Log;
using MissionPlanner.Utilities;
using System.Threading;
using MissionPlanner;
using ITGeoTagger;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util.TypeEnum;
using Emgu.CV.Shape;


namespace MissionPlanner
{
    public partial class ITGeotagger : Form
    {
        private enum PROCESSING_MODE
        {
            TIME_OFFSET,
            CAM_MSG
        }

        public List<Thread> PostThreads = new List<Thread>();
        List<Thread> PreThreads = new List<Thread>();

        private const string PHOTO_FILES_FILTER = "*.jpg;*.tif";
        private const int JXL_ID_OFFSET = 10;

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // CONSTS
        private const float rad2deg = (float)(180 / Math.PI);
        private const float deg2rad = (float)(1.0 / rad2deg);

        // Key = path of file, Value = object with picture information
        private Dictionary<string, PictureInformation> picturesInfo;

        // Key = time in milliseconds, Value = object with location info and attitude
        private Dictionary<long, VehicleLocation> vehicleLocations;

        private bool useAMSLAlt;
        private int millisShutterLag = 0;

        private Hashtable filedatecache = new Hashtable();
        private CheckBox chk_cammsg;
        private TextBox txt_basealt;
        private Label label28;
        private List<int> JXL_StationIDs = new List<int>();
        public ImageGroupTableInfo ATable;
        private TableLayoutPanel TabOrganizer = new TableLayoutPanel();

        Dictionary<string, TabPage> MainTabs = new Dictionary<string, TabPage>();

        public Thread PreProcessThread;
        public Thread PostProcessThread;

        public ITGeotagger()
        {
            InitializeComponent();

            //CHECK_AMSLAlt_Use.Checked = true;
            //PANEL_TIME_OFFSET.Enabled = false;

            //useAMSLAlt = CHECK_AMSLAlt_Use.Checked;

            MissionPlanner.Utilities.Tracking.AddPage(this.GetType().ToString(), this.Text);

            JXL_StationIDs = new List<int>();

            ATable = new ImageGroupTableInfo(this);

            ATable.Table.Dock = DockStyle.Fill;
            ATable.Table.Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom);
            ATable.Table.BorderStyle = BorderStyle.FixedSingle;
            ATable.Table.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;

            TabOrganize.Controls.Add(ATable.Table, 0, 1);

            BUT_GET_TRIG_OFFSETS.Enabled = false;

            ////test create a new tab

            //TabPage TetsPage = new TabPage();
            //TurbineTab FirstsTAB = new TurbineTab("C:\\Users\\Kevin\\Desktop\\CropTests");

            //FirstsTAB.Parent = TetsPage;
            //FirstsTAB.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            //FirstsTAB.Dock = DockStyle.Fill;

            //MAIN_TAB_CONTROL.TabPages.Add(TetsPage);
            TIMER_THREAD_CHECKER.Interval = 10000;
            TIMER_THREAD_CHECKER.Start();

        }

        private void BUT_GET_DIR_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                TXT_BROWSE_FOLDER.Text = folderBrowserDialog1.SelectedPath;
                List<string> ImageSet = GetAllImageDirs(TXT_BROWSE_FOLDER.Text);

                List<string> TlogImageDirs = Dirsfiltertlog(ImageSet);
                foreach (string folder in TlogImageDirs)
                {
                    ATable.AddRow(folder, GetTlogInDir(folder), GetImagetoTriggerOffset(folder, GetTlogInDir(folder)).ToString("G4"));
                }
                BUT_GET_TRIG_OFFSETS.Enabled = true;
            }


        }

        private float GetImagetoTriggerOffset(string dirPictures, string logFile)
        {
            string logFilePath = Path.Combine(dirPictures, logFile);
            if (!File.Exists(logFilePath))
            {
                MessageBox.Show("tlog file " + logFilePath + " does not exist ");
                return 0;
            }
            if (!Directory.Exists(dirPictures))
            {
                MessageBox.Show("Image directory " + dirPictures + " does not exist ");
                return 0;
            }

            DateTime imsettime = GetFirstSustainedImageTime(dirPictures);
            DateTime tgtime = GetFirstSustainedTriggerTime(logFilePath);

            AppendLogTextBox("\n\nImage time : " + imsettime.ToString());
            AppendLogTextBox("\nTrigger time : " + tgtime.ToString());

            float trig2im = (float)(imsettime - tgtime).TotalSeconds;
            return trig2im;
        }
        private List<string> GetAllImageDirs(string SearchDir)
        {

            List<string> AllDirs = DirSearch(SearchDir); //find all folders

            List<string> ImageDirs = FilterForImageDirs(AllDirs);

            return ImageDirs;

        }
        private List<string> FilterForImageDirs(List<string> AllDirs)
        {
            List<string> ImageDirs = new List<string>();

            foreach (string folder in AllDirs)
            {
                IEnumerable<string> files = Directory.EnumerateFiles(folder, "*.*").Where(s => s.EndsWith(".JPG") || s.EndsWith(".jpg"));
                if (files.Count() > 3)
                {
                    ImageDirs.Add(folder);
                }
            }

            return ImageDirs;

        }

        static List<string> DirSearch(string sDir)
        {
            List<string> DirList = new List<string>();
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    DirList.AddRange(DirSearch(d));
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
            DirList.Add(sDir);
            return DirList;
        }
        private List<string> Dirsfiltertlog(List<string> Dirstofilter)
        {
            List<string> tlogdirs = new List<string>();

            foreach (string folder in Dirstofilter)
            {
                IEnumerable<string> files = Directory.EnumerateFiles(folder, "*.*").Where(s => s.EndsWith(".tlog"));
                if (files.Count() > 0)
                {
                    tlogdirs.Add(folder);
                }
            }
            return tlogdirs;
        }
        private string GetTlogInDir(string dir)
        {

            string tlogname;
            IEnumerable<string> files = Directory.EnumerateFiles(dir, "*.*").Where(s => s.EndsWith(".tlog"));
            tlogname = Path.GetFileName(files.First());
            return tlogname;
        }
        // <summary>
        // Get a photos shutter time
        // </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        private DateTime getPhotoTime(string fn)
        {
            DateTime dtaken = DateTime.MinValue;

            if (filedatecache.ContainsKey(fn))
            {
                return (DateTime)filedatecache[fn];
            }

            try
            {
                Metadata lcMetadata = null;
                try
                {
                    FileInfo lcImgFile = new FileInfo(fn);
                    //Loading all meta data
                    if (fn.ToLower().EndsWith(".jpg"))
                    {
                        lcMetadata = JpegMetadataReader.ReadMetadata(lcImgFile);
                    }
                    else if (fn.ToLower().EndsWith(".tif"))
                    {
                        lcMetadata = TiffMetadataReader.ReadMetadata(lcImgFile);
                    }
                }
                catch (JpegProcessingException e)
                {
                    log.InfoFormat(e.Message);
                    return dtaken;
                }
                catch (TiffProcessingException e)
                {
                    log.InfoFormat(e.Message);
                    return dtaken;
                }

                foreach (AbstractDirectory lcDirectory in lcMetadata)
                {
                    if (lcDirectory.ContainsTag(0x9003))
                    {
                        dtaken = lcDirectory.GetDate(0x9003);
                        log.InfoFormat("does " + lcDirectory.GetTagName(0x9003) + " " + dtaken);

                        filedatecache[fn] = dtaken;

                        break;
                    }
                    if (lcDirectory.ContainsTag(0x9004))
                    {
                        dtaken = lcDirectory.GetDate(0x9004);
                        log.InfoFormat("does " + lcDirectory.GetTagName(0x9004) + " " + dtaken);

                        filedatecache[fn] = dtaken;

                        break;
                    }
                }

                //// old method, works, just slow
                /*
                Image myImage = Image.FromFile(fn);
                PropertyItem propItem = myImage.GetPropertyItem(36867); // 36867  // 306

                Convert date taken metadata to a DateTime object 
                string sdate = Encoding.UTF8.GetString(propItem.Value).Trim();
                string secondhalf = sdate.Substring(sdate.IndexOf(" "), (sdate.Length - sdate.IndexOf(" ")));
                string firsthalf = sdate.Substring(0, 10);
                firsthalf = firsthalf.Replace(":", "-");
                sdate = firsthalf + secondhalf;
                dtaken = DateTime.Parse(sdate);

                myImage.Dispose();
                 */
            }
            catch
            {
            }

            return dtaken;
        }

        public string UseGpsorGPS2()
        {
            return "GPS";
        }

        ///// <summary>
        ///// Return a list of all gps messages with there timestamp from the log
        ///// </summary>
        ///// <param name="fn"></param>
        ///// <returns></returns>
        private Dictionary<long, VehicleLocation> readGPSMsgInLog(string fn)
        {
            Dictionary<long, VehicleLocation> vehiclePositionList = new Dictionary<long, VehicleLocation>();

            // Telemetry Log
            if (fn.ToLower().EndsWith("tlog"))
            {


                System.IO.FileStream logplaybackfile = new System.IO.FileStream(fn, FileMode.Open);


                MAVLinkInterface mine = new MAVLinkInterface(logplaybackfile);


                MissionPlanner.CurrentState cs = new MissionPlanner.CurrentState();

                while (mine.logplaybackfile.BaseStream.Position < mine.logplaybackfile.BaseStream.Length)
                {

                    MAVLink.MAVLinkMessage packet = mine.readPacket();

                    cs.datetime = mine.lastlogread;

                    cs.UpdateCurrentSettings(null, true, mine);

                    VehicleLocation location = new VehicleLocation();
                    location.Time = cs.datetime;
                    location.Lat = cs.lat;
                    location.Lon = cs.lng;
                    location.RelAlt = cs.alt;
                    location.AltAMSL = cs.altasl;

                    location.Roll = cs.roll;
                    location.Pitch = cs.pitch;
                    location.Yaw = cs.yaw;

                    location.SAlt = cs.sonarrange;

                    vehiclePositionList[ToMilliseconds(location.Time)] = location;
                    // 4 5 7
                    //Console.Write((mine.logplaybackfile.BaseStream.Position * 100 /
                    // mine.logplaybackfile.BaseStream.Length) + "    \r");
                }
                mine.logplaybackfile.Close();
                AppendLogTextBox("\nGot Locations from tlog");
            }
            // DataFlash Log
            else
            {
                using (var sr = new CollectionBuffer(File.OpenRead(fn)))
                {
                    // Will hold the last seen Attitude information in order to incorporate them into the GPS Info
                    float currentYaw = 0f;
                    float currentRoll = 0f;
                    float currentPitch = 0f;
                    float currentSAlt = 0f;
                    int a = 0;

                    foreach (var item in sr.GetEnumeratorType(new string[] { "GPS", "GPS2", "ATT", "CTUN", "RFND" }))
                    {
                        // Look for GPS Messages. However GPS Messages do not have Roll, Pitch and Yaw
                        // So we have to look for one ATT message after having read a GPS one

                        var gpstouse = UseGpsorGPS2();

                        if (item.msgtype == gpstouse)
                        {
                            if (!sr.dflog.logformat.ContainsKey(gpstouse))
                                continue;

                            int latindex = sr.dflog.FindMessageOffset(gpstouse, "Lat");
                            int lngindex = sr.dflog.FindMessageOffset(gpstouse, "Lng");
                            int altindex = sr.dflog.FindMessageOffset(gpstouse, "Alt");
                            int raltindex = sr.dflog.FindMessageOffset(gpstouse, "RAlt");
                            if (raltindex == -1)
                                raltindex = sr.dflog.FindMessageOffset(gpstouse, "RelAlt");

                            VehicleLocation location = new VehicleLocation();

                            try
                            {
                                location.Time = item.time;
                                if (latindex != -1)
                                    location.Lat = double.Parse(item.items[latindex], CultureInfo.InvariantCulture);
                                if (lngindex != -1)
                                    location.Lon = double.Parse(item.items[lngindex], CultureInfo.InvariantCulture);
                                if (raltindex != -1)
                                    location.RelAlt = double.Parse(item.items[raltindex], CultureInfo.InvariantCulture);
                                if (altindex != -1)
                                    location.AltAMSL = double.Parse(item.items[altindex], CultureInfo.InvariantCulture);

                                location.Roll = currentRoll;
                                location.Pitch = currentPitch;
                                location.Yaw = currentYaw;

                                location.SAlt = currentSAlt;

                                long millis = ToMilliseconds(location.Time);

                                //System.Diagnostics.Debug.WriteLine("GPS MSG - UTCMillis = " + millis  + "  GPS Week = " + getValueFromStringArray(gpsLineValues, gpsweekpos) + "  TimeMS = " + getValueFromStringArray(gpsLineValues, timepos));

                                if (!vehiclePositionList.ContainsKey(millis) && (location.Time != DateTime.MinValue))
                                    vehiclePositionList[millis] = location;
                            }
                            catch
                            {
                                Console.WriteLine("Bad " + gpstouse + " Line");
                            }
                        }
                        else if (item.msgtype == "ATT")
                        {
                            int Rindex = sr.dflog.FindMessageOffset("ATT", "Roll");
                            int Pindex = sr.dflog.FindMessageOffset("ATT", "Pitch");
                            int Yindex = sr.dflog.FindMessageOffset("ATT", "Yaw");

                            if (Rindex != -1)
                                currentRoll = float.Parse(item.items[Rindex], CultureInfo.InvariantCulture);
                            if (Pindex != -1)
                                currentPitch = float.Parse(item.items[Pindex], CultureInfo.InvariantCulture);
                            if (Yindex != -1)
                                currentYaw = float.Parse(item.items[Yindex], CultureInfo.InvariantCulture);
                        }
                        else if (item.msgtype == "CTUN")
                        {
                            int SAltindex = sr.dflog.FindMessageOffset("CTUN", "SAlt");

                            if (SAltindex != -1)
                            {
                                currentSAlt = float.Parse(item.items[SAltindex]);
                            }
                        }
                        else if (item.msgtype == "RFND")
                        {
                            int SAltindex = sr.dflog.FindMessageOffset("RFND", "Dist1");

                            if (SAltindex != -1)
                            {
                                currentSAlt = float.Parse(item.items[SAltindex]);
                            }
                        }
                    }
                }
            }

            return vehiclePositionList;
        }

        ///// <summary>
        ///// Return a list of all cam messages in a log with timestamp
        ///// </summary>
        ///// <param name="fn"></param>
        ///// <returns></returns>
        private Dictionary<long, VehicleLocation> readCAMMsgInLog(string fn)
        {
            Dictionary<long, VehicleLocation> list = new Dictionary<long, VehicleLocation>();

            // Telemetry Log
            if (fn.ToLower().EndsWith("tlog"))
            {
                AppendLogTextBox("\nWarning: tlogs are not fully supported when using CAM Messages\n");

                using (MissionPlanner.MAVLinkInterface mine = new MissionPlanner.MAVLinkInterface())
                {
                    mine.logplaybackfile =
                        new BinaryReader(File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.Read));
                    mine.logreadmode = true;

                    MissionPlanner.CurrentState cs = new MissionPlanner.CurrentState();

                    while (mine.logplaybackfile.BaseStream.Position < mine.logplaybackfile.BaseStream.Length)
                    {
                        MAVLink.MAVLinkMessage packet = mine.readPacket();

                        cs.datetime = mine.lastlogread;
                        cs.UpdateCurrentSettings(null, true, mine);

                        if (packet.msgid == (uint)MAVLink.MAVLINK_MSG_ID.CAMERA_FEEDBACK)
                        {
                            var msg = (MAVLink.mavlink_camera_feedback_t)packet.data;

                            VehicleLocation location = new VehicleLocation();
                            location.Time = FromUTCTimeMilliseconds((long)(msg.time_usec / 1000));// cs.datetime;
                            location.Lat = msg.lat;
                            location.Lon = msg.lng;
                            location.RelAlt = msg.alt_rel;
                            location.AltAMSL = msg.alt_msl;

                            location.Roll = msg.roll;
                            location.Pitch = msg.pitch;
                            location.Yaw = msg.yaw;

                            location.SAlt = cs.sonarrange;

                            list[ToMilliseconds(location.Time)] = location;

                            Console.Write((mine.logplaybackfile.BaseStream.Position * 100 /
                                           mine.logplaybackfile.BaseStream.Length) + "    \r");
                        }
                    }
                    mine.logplaybackfile.Close();
                }
            }
            // DataFlash Log
            else
            {
                float currentSAlt = 0;
                using (var sr = new CollectionBuffer(File.OpenRead(fn)))
                {
                    foreach (var item in sr.GetEnumeratorType(new string[] { "CAM", "RFND" }))
                    {
                        if (item.msgtype == "CAM")
                        {
                            int latindex = sr.dflog.FindMessageOffset("CAM", "Lat");
                            int lngindex = sr.dflog.FindMessageOffset("CAM", "Lng");
                            int altindex = sr.dflog.FindMessageOffset("CAM", "Alt");
                            int raltindex = sr.dflog.FindMessageOffset("CAM", "RelAlt");

                            int rindex = sr.dflog.FindMessageOffset("CAM", "Roll");
                            int pindex = sr.dflog.FindMessageOffset("CAM", "Pitch");
                            int yindex = sr.dflog.FindMessageOffset("CAM", "Yaw");

                            int gtimeindex = sr.dflog.FindMessageOffset("CAM", "GPSTime");
                            int gweekindex = sr.dflog.FindMessageOffset("CAM", "GPSWeek");

                            VehicleLocation p = new VehicleLocation();

                            p.Time = GetTimeFromGps(int.Parse(item.items[gweekindex], CultureInfo.InvariantCulture),
                                int.Parse(item.items[gtimeindex], CultureInfo.InvariantCulture));

                            p.Lat = double.Parse(item.items[latindex], CultureInfo.InvariantCulture);
                            p.Lon = double.Parse(item.items[lngindex], CultureInfo.InvariantCulture);
                            p.AltAMSL = double.Parse(item.items[altindex], CultureInfo.InvariantCulture);
                            if (raltindex != -1)
                                p.RelAlt = double.Parse(item.items[raltindex], CultureInfo.InvariantCulture);

                            p.Pitch = float.Parse(item.items[pindex], CultureInfo.InvariantCulture);
                            p.Roll = float.Parse(item.items[rindex], CultureInfo.InvariantCulture);
                            p.Yaw = float.Parse(item.items[yindex], CultureInfo.InvariantCulture);

                            p.SAlt = currentSAlt;

                            list[ToMilliseconds(p.Time)] = p;
                        }
                        else if (item.msgtype == "RFND")
                        {
                            int SAltindex = sr.dflog.FindMessageOffset("RFND", "Dist1");

                            if (SAltindex != -1)
                            {
                                currentSAlt = float.Parse(item.items[SAltindex]);
                            }
                        }
                    }
                }
            }
            return list;
        }

        public DateTime FromUTCTimeMilliseconds(long milliseconds)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(milliseconds);
        }

        public DateTime GetTimeFromGps(int weeknumber, int milliseconds)
        {
            int LEAP_SECONDS = 17;

            DateTime datum = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);
            DateTime week = datum.AddDays(weeknumber * 7);
            DateTime time = week.AddMilliseconds(milliseconds);

            return time.AddSeconds(-LEAP_SECONDS);
        }

        public long ToMilliseconds(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalMilliseconds);
        }

        //private double EstimateOffset(string logFile, string dirWithImages)
        //{
        //    if (vehicleLocations == null || vehicleLocations.Count <= 0)
        //    {
        //        if (chk_cammsg.Checked)
        //        {
        //            vehicleLocations = readCAMMsgInLog(logFile);
        //        }
        //        else
        //        {
        //            vehicleLocations = readGPSMsgInLog(logFile);
        //        }
        //    }

        //    if (vehicleLocations == null || vehicleLocations.Count <= 0)
        //        return -1;

        //    List<string> filelist = new List<string>();
        //    string[] exts = PHOTO_FILES_FILTER.Split(';');
        //    foreach (var ext in exts)
        //    {
        //        filelist.AddRange(Directory.GetFiles(dirWithImages, ext));
        //    }

        //    string[] files = filelist.ToArray();

        //    if (files == null || files.Length == 0)
        //        return -1;

        //    Array.Sort(files, compareFileByPhotoTime);

        //    double ans = 0;

        //    TXT_outputlog.Clear();

        //    for (int a = 0; a < 4; a++)
        //    {
        //        // First Photo time
        //        string firstPhoto = files[a];

        //        DateTime photoTime = getPhotoTime(firstPhoto);

        //        AppendLogTextBox((a + 1) + " Picture " + Path.GetFileNameWithoutExtension(firstPhoto) +
        //                                 " with DateTime: " + photoTime.ToString("yyyy:MM:dd HH:mm:ss") + "\n");

        //        // First GPS Message in Log time
        //        List<long> times = new List<long>(vehicleLocations.Keys);
        //        times.Sort();
        //        long firstTimeInGPSMsg = times[a];
        //        DateTime logTime = FromUTCTimeMilliseconds(firstTimeInGPSMsg);

        //        AppendLogTextBox((a + 1) + " GPS Log Msg: " + logTime.ToString("yyyy:MM:dd HH:mm:ss") + "\n");

        //        AppendLogTextBox((a + 1) + " Est: " + (double) (photoTime - logTime).TotalSeconds + "\n");

        //        if (ans == 0)
        //            ans = (double) (photoTime - logTime).TotalSeconds;
        //        else
        //            ans = ans*0.5 + (photoTime - logTime).TotalSeconds*0.5;
        //    }

        //    return ans;
        //}

        private void CreateReportFiles(Dictionary<string, PictureInformation> listPhotosWithInfo, string dirWithImages,
            float offset)
        {
            // Write report files
            Document kmlroot = new Document();
            Folder kml = new Folder("Pins");

            Folder overlayfolder = new Folder("Overlay");

            // add root folder to document
            kmlroot.AddFeature(kml);
            kmlroot.AddFeature(overlayfolder);

            // Clear Stations IDs
            JXL_StationIDs.Clear();

            using (
                StreamWriter swlogloccsv =
                    new StreamWriter(dirWithImages + Path.DirectorySeparatorChar + "loglocation.csv"))
            using (
                StreamWriter swlockml = new StreamWriter(dirWithImages + Path.DirectorySeparatorChar + "location.kml"))
            using (
                StreamWriter swloctxt = new StreamWriter(dirWithImages + Path.DirectorySeparatorChar + "location.txt"))
            using (
                StreamWriter swloctel = new StreamWriter(dirWithImages + Path.DirectorySeparatorChar + "location.tel"))
            using (
                XmlTextWriter swloctrim = new XmlTextWriter(
                    dirWithImages + Path.DirectorySeparatorChar + "location.jxl", Encoding.ASCII))
            {
                swloctrim.Formatting = Formatting.Indented;
                swloctrim.WriteStartDocument(false);
                swloctrim.WriteStartElement("JOBFile");
                swloctrim.WriteAttributeString("jobName", "MPGeoRef");
                swloctrim.WriteAttributeString("product", "Gatewing");
                swloctrim.WriteAttributeString("productVersion", "1.0");
                swloctrim.WriteAttributeString("version", "5.6");
                // enviro
                swloctrim.WriteStartElement("Environment");
                swloctrim.WriteStartElement("CoordinateSystem");
                swloctrim.WriteElementString("SystemName", "Default");
                swloctrim.WriteElementString("ZoneName", "Default");
                swloctrim.WriteElementString("DatumName", "WGS 1984");
                swloctrim.WriteStartElement("Ellipsoid");
                swloctrim.WriteElementString("EarthRadius", "6378137");
                swloctrim.WriteElementString("Flattening", "0.00335281067183");
                swloctrim.WriteEndElement();
                swloctrim.WriteStartElement("Projection");
                swloctrim.WriteElementString("Type", "NoProjection");
                swloctrim.WriteElementString("Scale", "1");
                swloctrim.WriteElementString("GridOrientation", "IncreasingNorthEast");
                swloctrim.WriteElementString("SouthAzimuth", "false");
                swloctrim.WriteElementString("ApplySeaLevelCorrection", "true");
                swloctrim.WriteEndElement();
                swloctrim.WriteStartElement("LocalSite");
                swloctrim.WriteElementString("Type", "Grid");
                swloctrim.WriteElementString("ProjectLocationLatitude", "");
                swloctrim.WriteElementString("ProjectLocationLongitude", "");
                swloctrim.WriteElementString("ProjectLocationHeight", "");
                swloctrim.WriteEndElement();
                swloctrim.WriteStartElement("Datum");
                swloctrim.WriteElementString("Type", "ThreeParameter");
                swloctrim.WriteElementString("GridName", "WGS 1984");
                swloctrim.WriteElementString("Direction", "WGS84ToLocal");
                swloctrim.WriteElementString("EarthRadius", "6378137");
                swloctrim.WriteElementString("Flattening", "0.00335281067183");
                swloctrim.WriteElementString("TranslationX", "0");
                swloctrim.WriteElementString("TranslationY", "0");
                swloctrim.WriteElementString("TranslationZ", "0");
                swloctrim.WriteEndElement();
                swloctrim.WriteStartElement("HorizontalAdjustment");
                swloctrim.WriteElementString("Type", "NoAdjustment");
                swloctrim.WriteEndElement();
                swloctrim.WriteStartElement("VerticalAdjustment");
                swloctrim.WriteElementString("Type", "NoAdjustment");
                swloctrim.WriteEndElement();
                swloctrim.WriteStartElement("CombinedScaleFactor");
                swloctrim.WriteStartElement("Location");
                swloctrim.WriteElementString("Latitude", "");
                swloctrim.WriteElementString("Longitude", "");
                swloctrim.WriteElementString("Height", "");
                swloctrim.WriteEndElement();
                swloctrim.WriteElementString("Scale", "");
                swloctrim.WriteEndElement();

                swloctrim.WriteEndElement();
                swloctrim.WriteEndElement();

                // fieldbook
                swloctrim.WriteStartElement("FieldBook");

                swloctrim.WriteRaw(@"   <CameraDesignRecord ID='00000001'>
                                      <Type>GoPro   </Type>
                                      <HeightPixels>2400</HeightPixels>
                                      <WidthPixels>3200</WidthPixels>
                                      <PixelSize>0.0000022</PixelSize>
                                      <LensModel>Rectilinear</LensModel>
                                      <NominalFocalLength>0.002</NominalFocalLength>
                                    </CameraDesignRecord>
                                    <CameraRecord2 ID='00000002'>
                                      <CameraDesignID>00000001</CameraDesignID>
                                      <CameraPosition>01</CameraPosition>
                                      <Optics>
                                        <IdealAngularMagnification>1.0</IdealAngularMagnification>
                                        <AngleSymmetricDistortion>
                                          <Order3>-0.35</Order3>
                                          <Order5>0.15</Order5>
                                          <Order7>-0.033</Order7>
                                          <Order9> 0</Order9>
                                        </AngleSymmetricDistortion>
                                        <AngleDecenteringDistortion>
                                          <Column>0</Column>
                                          <Row>0</Row>
                                        </AngleDecenteringDistortion>
                                      </Optics>
                                      <Geometry>
                                        <PerspectiveCenterPixels>
                                          <PrincipalPointColumn>-1615.5</PrincipalPointColumn>
                                          <PrincipalPointRow>-1187.5</PrincipalPointRow>
                                          <PrincipalDistance>-2102</PrincipalDistance>
                                        </PerspectiveCenterPixels>
                                        <VectorOffset>
                                          <X>0</X>
                                          <Y>0</Y>
                                          <Z>0</Z>
                                        </VectorOffset>
                                        <BiVectorAngle>
                                          <XX>0</XX>
                                          <YY>0</YY>
                                          <ZZ>-1.5707963268</ZZ>
                                        </BiVectorAngle>
                                      </Geometry>
                                    </CameraRecord2>");

                // 2mm fl
                // res 2400 * 3200 = 7,680,000
                // sensor size = 1/2.5" - 5.70 × 4.28 mm
                // 2.2 μm
                // fl in pixels = fl in mm * res / sensor size

                swloctrim.WriteStartElement("PhotoInstrumentRecord");
                swloctrim.WriteAttributeString("ID", "0000000E");
                swloctrim.WriteElementString("Type", "Aerial");
                swloctrim.WriteElementString("Model", "X100");
                swloctrim.WriteElementString("Serial", "000-000");
                swloctrim.WriteElementString("FirmwareVersion", "v0.0");
                swloctrim.WriteElementString("UserDefinedName", "Prototype");
                swloctrim.WriteEndElement();

                swloctrim.WriteStartElement("AtmosphereRecord");
                swloctrim.WriteAttributeString("ID", "0000000F");
                swloctrim.WriteElementString("Pressure", "");
                swloctrim.WriteElementString("Temperature", "");
                swloctrim.WriteElementString("PPM", "");
                swloctrim.WriteElementString("ApplyEarthCurvatureCorrection", "false");
                swloctrim.WriteElementString("ApplyRefractionCorrection", "false");
                swloctrim.WriteElementString("RefractionCoefficient", "0");
                swloctrim.WriteElementString("PressureInputMethod", "ReadFromInstrument");
                swloctrim.WriteEndElement();

                swloctel.WriteLine("version=1");

                swloctel.WriteLine("#seconds offset - " + offset);
                swloctel.WriteLine("#longitude and latitude - in degrees");
                swloctel.WriteLine("#name	utc	longitude	latitude	height");

                swloctxt.WriteLine("#name latitude/Y longitude/X height/Z yaw pitch roll SAlt");

                AppendLogTextBox("\nStart Processing");

                // Dont know why but it was 10 in the past so let it be. Used to generate jxl file simulating x100 from trimble
                int lastRecordN = JXL_ID_OFFSET;

                // path
                CoordinateCollection coords = new CoordinateCollection();

                foreach (var item in vehicleLocations.Values)
                {
                    if (item != null)
                        coords.Add(new SharpKml.Base.Vector(item.Lat, item.Lon, item.AltAMSL));
                }

                var ls = new LineString() { Coordinates = coords, AltitudeMode = AltitudeMode.Absolute };

                SharpKml.Dom.Placemark pm = new SharpKml.Dom.Placemark() { Geometry = ls, Name = "path" };

                kml.AddFeature(pm);

                foreach (PictureInformation picInfo in listPhotosWithInfo.Values)
                {
                    string filename = Path.GetFileName(picInfo.Path);
                    string filenameWithoutExt = Path.GetFileNameWithoutExtension(picInfo.Path);

                    SharpKml.Dom.Timestamp tstamp = new SharpKml.Dom.Timestamp();

                    tstamp.When = picInfo.Time;

                    kml.AddFeature(
                        new Placemark()
                        {
                            Time = tstamp,
                            Visibility = true,
                            Name = filenameWithoutExt,
                            Geometry = new SharpKml.Dom.Point()
                            {
                                Coordinate = new Vector(picInfo.Lat, picInfo.Lon, picInfo.AltAMSL),
                                AltitudeMode = AltitudeMode.Absolute
                            },
                            Description = new Description()
                            {
                                Text =
                                    "<table><tr><td><img src=\"" + filename.ToLower() +
                                    "\" width=500 /></td></tr></table>"
                            },
                            StyleSelector = new Style()
                            {
                                Balloon = new BalloonStyle() { Text = "$[name]<br>$[description]" }
                            }
                        }
                        );

                    double lat = picInfo.Lat;
                    double lng = picInfo.Lon;
                    double alpha = picInfo.Yaw;// +(double)num_camerarotation.Value;

                    RectangleF rect = getboundingbox(picInfo.Lat, picInfo.Lon, picInfo.AltAMSL, alpha, 30, 20);

                    Console.WriteLine(rect);

                    //http://en.wikipedia.org/wiki/World_file
                    /* using (StreamWriter swjpw = new StreamWriter(dirWithImages + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(filename) + ".jgw"))
                        {
                            swjpw.WriteLine((rect.Height / 2448.0).ToString("0.00000000000000000"));
                            swjpw.WriteLine((0).ToString("0.00000000000000000")); // 
                            swjpw.WriteLine((0).ToString("0.00000000000000000")); //
                            swjpw.WriteLine((rect.Width / -3264.0).ToString("0.00000000000000000")); // distance per pixel
                            swjpw.WriteLine((rect.Left).ToString("0.00000000000000000"));
                            swjpw.WriteLine((rect.Top).ToString("0.00000000000000000"));

                            swjpw.Close();
                        }*/

                    overlayfolder.AddFeature(
                        new GroundOverlay()
                        {
                            Name = filenameWithoutExt,
                            Visibility = false,
                            Time = tstamp,
                            AltitudeMode = AltitudeMode.ClampToGround,
                            Bounds = new LatLonBox()
                            {
                                Rotation = -alpha % 360,
                                North = rect.Bottom,
                                East = rect.Right,
                                West = rect.Left,
                                South = rect.Top,
                            },
                            Icon = new SharpKml.Dom.Icon()
                            {
                                Href = new Uri(filename.ToLower(), UriKind.Relative),
                            },
                        }
                        );

                    swloctxt.WriteLine(filename + " " + picInfo.Lat + " " + picInfo.Lon + " " +
                                       picInfo.getAltitude(useAMSLAlt) + " " + picInfo.Yaw + " " + picInfo.Pitch + " " +
                                       picInfo.Roll + " " + picInfo.SAlt);


                    swloctel.WriteLine(filename + "\t" + picInfo.Time.ToString("yyyy:MM:dd HH:mm:ss") + "\t" +
                                       picInfo.Lon + "\t" + picInfo.Lat + "\t" + picInfo.getAltitude(useAMSLAlt));
                    swloctel.Flush();
                    swloctxt.Flush();

                    lastRecordN = GenPhotoStationRecord(swloctrim, picInfo.Path, picInfo.Lat, picInfo.Lon,
                        picInfo.getAltitude(useAMSLAlt), 0, 0, picInfo.Yaw, picInfo.Width, picInfo.Height, lastRecordN);

                    log.InfoFormat(filename + " " + picInfo.Lon + " " + picInfo.Lat + " " +
                                   picInfo.getAltitude(useAMSLAlt) + "           ");
                }

                Serializer serializer = new Serializer();
                serializer.Serialize(kmlroot);
                swlockml.Write(serializer.Xml);

                //MissionPlanner.Utilities.httpserver.georefkml = serializer.Xml;
                //MissionPlanner.Utilities.httpserver.georefimagepath = dirWithImages + Path.DirectorySeparatorChar;

                writeGPX(dirWithImages + Path.DirectorySeparatorChar + "location.gpx", listPhotosWithInfo);

                // flightmission
                GenFlightMission(swloctrim, lastRecordN);

                swloctrim.WriteEndElement(); // fieldbook
                swloctrim.WriteEndElement(); // job
                swloctrim.WriteEndDocument();

                //AppendLogTextBox("\nDone \n");
            }
        }

        private VehicleLocation LookForLocation(DateTime t, Dictionary<long, VehicleLocation> listLocations,
            int offsettime = 2000)
        {
            long time = ToMilliseconds(t);

            // Time at which the GPS position is actually search and found
            long actualTime = time;
            int millisSTEP = 1;

            // 2 seconds (2000 ms) in the log as absolute maximum
            int maxIteration = offsettime;

            bool found = false;
            int iteration = 0;
            VehicleLocation location = null;

            while (!found && iteration < maxIteration)
            {
                found = listLocations.ContainsKey(actualTime);
                if (found)
                {
                    location = listLocations[actualTime];
                }
                else
                {
                    actualTime += millisSTEP;
                    iteration++;
                }
            }

            /*if (location == null)
                AppendLogTextBox("\nTime not found in log: " + time  + "\n");
            else
                AppendLogTextBox("\nGPS position found " + (actualTime - time) + " ms away\n");*/

            return location;
        }

        public Dictionary<string, PictureInformation> doworkGPSOFFSET(string logFile, string dirWithImages, float offset)
        {
            // Lets start over 
            Dictionary<string, PictureInformation> picturesInformationTemp =
                new Dictionary<string, PictureInformation>();
            //clear messages to work with multiple tlogs
            if (vehicleLocations != null)
            {
                vehicleLocations.Clear();
            }

            // Read Vehicle Locations from log. GPS Messages. Will have to do it anyway
            if (vehicleLocations == null || vehicleLocations.Count <= 0)
            {

                AppendLogTextBox("\nReading log for GPS-ATT Messages");

                vehicleLocations = readGPSMsgInLog(logFile);
            }

            if (vehicleLocations == null)
            {
                AppendLogTextBox("\nLog file problem. Aborting....\n");
                return null;
            }

            AppendLogTextBox("\nLog locations : " + vehicleLocations.Count);

            AppendLogTextBox("\nRead images\n");

            List<string> filelist = new List<string>();
            string[] exts = PHOTO_FILES_FILTER.Split(';');
            foreach (var ext in exts)
            {
                filelist.AddRange(Directory.GetFiles(dirWithImages, ext));
            }

            string[] files = filelist.ToArray();

            AppendLogTextBox("\nImages read : " + files.Length);

            // Check that we have at least one picture
            if (files.Length <= 0)
            {
                AppendLogTextBox("\nNot enough files found.  Aborting..... \n");
                return null;
            }

            AppendLogTextBox("\nSorting images by time taken");
            Array.Sort(files, compareFileByPhotoTime);
            AppendLogTextBox("\nSort Completed");
            // Each file corresponds to one CAM message
            // We assume that picture names are in ascending order in time
            for (int i = 0; i < files.Length; i++)
            {
                string filename = files[i];

                PictureInformation p = new PictureInformation();

                // Fill shot time in Picture
                p.ShotTimeReportedByCamera = getPhotoTime(filename);

                // Look for corresponding Location in vehicleLocationList
                System.TimeSpan tmpchkTS = new System.TimeSpan(0, 0, (int)offset);
                DateTime correctedTime;

                if (p.ShotTimeReportedByCamera.Ticks > tmpchkTS.Ticks)
                {
                    correctedTime = p.ShotTimeReportedByCamera.AddSeconds(-offset);
                }
                else
                {
                    correctedTime = p.ShotTimeReportedByCamera;
                }

                VehicleLocation shotLocation = LookForLocation(correctedTime, vehicleLocations, 5000);

                if (shotLocation == null)
                {
                    AppendLogTextBox("\nPhoto " + Path.GetFileNameWithoutExtension(filename) +
                                             " NOT PROCESSED. No GPS match in the log file. Please take care\n");
                    p.Lat = 1;
                    p.Lon = 1;
                    p.AltAMSL = 1;
                    p.RelAlt = 1;

                    p.Pitch = 1;
                    p.Roll = 1;
                    p.Yaw = 1;

                    p.SAlt = 1;

                    p.Time = getPhotoTime(filename);

                    p.Path = filename;

                    picturesInformationTemp.Add(filename, p);

                }
                else
                {
                    p.Lat = shotLocation.Lat;
                    p.Lon = shotLocation.Lon;
                    p.AltAMSL = shotLocation.AltAMSL;
                    p.RelAlt = shotLocation.RelAlt;

                    p.Pitch = shotLocation.Pitch;
                    p.Roll = shotLocation.Roll;
                    p.Yaw = shotLocation.Yaw;

                    p.SAlt = shotLocation.SAlt;

                    p.Time = shotLocation.Time;

                    p.Path = filename;


                    picturesInformationTemp.Add(filename, p);

                    AppendLogTextBox("\nPhoto " + Path.GetFileNameWithoutExtension(filename) +
                                             " PROCESSED with GPS position found " +
                                             (shotLocation.Time - correctedTime).Milliseconds + " ms away");
                }
            }

            return picturesInformationTemp;
        }

        private int compareFileByPhotoTime(string x, string y)
        {
            if (getPhotoTime(x) < getPhotoTime(y)) return -1;
            if (getPhotoTime(x) > getPhotoTime(y)) return 1;
            return 0;
        }

        public Dictionary<string, PictureInformation> doworkCAM(string logFile, string dirWithImages)
        {
            // Lets start over 
            Dictionary<string, PictureInformation> picturesInformationTemp =
                new Dictionary<string, PictureInformation>();

            AppendLogTextBox("\nUsing AMSL Altitude " + useAMSLAlt + "\n");

            // If we are required to use AMSL then GPS messages should be used until CAM messages includes AMSL in the coming AC versions
            // Or if the user enter shutter lag and thus we have to look for GPS messages ahead in time
            if (useAMSLAlt || millisShutterLag > 0)
            {
                AppendLogTextBox("\nReading log for GPS Messages in order to get AMSL Altitude\n");
                if (vehicleLocations == null || vehicleLocations.Count <= 0)
                {
                    vehicleLocations = readGPSMsgInLog(logFile);

                    if (vehicleLocations == null || vehicleLocations.Count <= 0)
                    {
                        AppendLogTextBox("\nLog file problem. Aborting....\n");
                        return null;
                    }
                }
                AppendLogTextBox("\nLog Read for GPS Messages");
                AppendLogTextBox("\nLog locations : " + vehicleLocations.Count + "\n");

            }

            AppendLogTextBox("\nReading log for CAM Messages");

            var list = readCAMMsgInLog(logFile);

            if (list == null)
            {
                AppendLogTextBox("\nLog file problem. No CAM messages. Aborting....\n");
                return null;
            }

            AppendLogTextBox("\nLog Read with - " + list.Count + " - CAM Messages found\n");

            AppendLogTextBox("\nRead images\n");

            string[] files = Directory.GetFiles(dirWithImages, "*.jpg");

            AppendLogTextBox("\nImages read : " + files.Length);

            // Check that we have same number of CAMs than files
            if (files.Length != list.Count)
            {
                AppendLogTextBox(string.Format("CAM Msgs and Files discrepancy. Check it! files: {0} vs CAM msg: {1}\n", files.Length, list.Count));
                return null;
            }

            Array.Sort(files, compareFileByPhotoTime);

            // Each file corresponds to one CAM message
            // We assume that picture names are in ascending order in time
            int i = -1;
            foreach (var currentCAM in list.Values)
            {
                i++;
                PictureInformation p = new PictureInformation();

                // Fill shot time in Picture
                p.ShotTimeReportedByCamera = getPhotoTime(files[i]);

                DateTime dCAMMsgTime = currentCAM.Time;

                if (millisShutterLag == 0)
                {
                    // Lets puts GPS time
                    p.Time = dCAMMsgTime;

                    p.Lat = currentCAM.Lat;
                    p.Lon = currentCAM.Lon;
                    p.AltAMSL = currentCAM.AltAMSL;
                    p.RelAlt = currentCAM.RelAlt;

                    VehicleLocation cameraLocationFromGPSMsg = null;

                    string logAltMsg = "RelAlt";

                    if (useAMSLAlt)
                    {
                        cameraLocationFromGPSMsg = LookForLocation(p.Time, vehicleLocations);
                        if (cameraLocationFromGPSMsg != null)
                        {
                            logAltMsg = "AMSL Alt " + (cameraLocationFromGPSMsg.Time - p.Time).Milliseconds + " ms away" +
                                        " offset: " + (p.ShotTimeReportedByCamera - dCAMMsgTime).TotalSeconds;
                            p.AltAMSL = cameraLocationFromGPSMsg.AltAMSL;
                        }
                        else
                            logAltMsg = "AMSL Alt NOT found";
                    }


                    p.Pitch = currentCAM.Pitch;
                    p.Roll = currentCAM.Roll;
                    p.Yaw = currentCAM.Yaw;

                    p.SAlt = currentCAM.SAlt;

                    p.Path = files[i];

                    string picturePath = files[i];

                    picturesInformationTemp.Add(picturePath, p);

                    AppendLogTextBox("\nPhoto " + Path.GetFileNameWithoutExtension(picturePath) +
                                             " processed from CAM Msg with " + millisShutterLag + " ms shutter lag. " +
                                             logAltMsg);
                }
                else
                {
                    // Look fot GPS Message ahead
                    DateTime dCorrectedWithLagPhotoTime = dCAMMsgTime;
                    dCorrectedWithLagPhotoTime = dCorrectedWithLagPhotoTime.AddMilliseconds(millisShutterLag);

                    VehicleLocation cameraLocationFromGPSMsg = LookForLocation(dCorrectedWithLagPhotoTime,
                        vehicleLocations);

                    // Check which GPS Position is closer in time.
                    if (cameraLocationFromGPSMsg != null)
                    {
                        System.TimeSpan diffGPSTimeCAMTime = cameraLocationFromGPSMsg.Time - dCAMMsgTime;

                        if (diffGPSTimeCAMTime.Milliseconds > 2 * millisShutterLag)
                        {
                            // Stay with CAM Message as it is closer to CorrectedTime
                            p.Time = dCAMMsgTime;

                            p.Lat = currentCAM.Lat;
                            p.Lon = currentCAM.Lon;
                            p.AltAMSL = currentCAM.AltAMSL;
                            p.RelAlt = currentCAM.RelAlt;

                            string logAltMsg = "RelAlt";

                            cameraLocationFromGPSMsg = null;
                            if (useAMSLAlt)
                            {
                                cameraLocationFromGPSMsg = LookForLocation(p.Time, vehicleLocations);
                                if (cameraLocationFromGPSMsg != null)
                                {
                                    logAltMsg = "AMSL Alt " + (cameraLocationFromGPSMsg.Time - p.Time).Milliseconds +
                                                " ms away";
                                    p.AltAMSL = cameraLocationFromGPSMsg.AltAMSL;
                                }
                                else
                                    logAltMsg = "AMSL Alt NOT found";
                            }


                            AppendLogTextBox("\nPhoto " + Path.GetFileNameWithoutExtension(files[i]) +
                                                     " processed with CAM Msg. Shutter lag too small. " + logAltMsg);
                        }
                        else
                        {
                            // Get GPS Time as it is closer to CorrectedTime
                            // Lets puts GPS time
                            p.Time = cameraLocationFromGPSMsg.Time;

                            p.Lat = cameraLocationFromGPSMsg.Lat;
                            p.Lon = cameraLocationFromGPSMsg.Lon;
                            p.AltAMSL = cameraLocationFromGPSMsg.AltAMSL;
                            p.RelAlt = cameraLocationFromGPSMsg.RelAlt;

                            string logAltMsg = useAMSLAlt ? "AMSL Alt" : "RelAlt";

                            AppendLogTextBox("\nPhoto " + Path.GetFileNameWithoutExtension(files[i]) +
                                                     " processed with GPS Msg : " + diffGPSTimeCAMTime.Milliseconds +
                                                     " ms ahead of CAM Msg. " + logAltMsg);
                        }

                        p.Pitch = currentCAM.Pitch;
                        p.Roll = currentCAM.Roll;
                        p.Yaw = currentCAM.Yaw;

                        p.SAlt = currentCAM.SAlt;

                        p.Path = files[i];

                        string picturePath = files[i];

                        picturesInformationTemp.Add(picturePath, p);
                    }
                    else
                    {
                        AppendLogTextBox("\nPhoto " + Path.GetFileNameWithoutExtension(files[i]) +
                                                 " NOT Processed. Time not found in log. Too large Shutter Lag? Try setting it to 0");
                    }
                }
            }

            return picturesInformationTemp;
        }

        private void GenFlightMission(XmlTextWriter swloctrim, int lastRecordN)
        {
            swloctrim.WriteStartElement("FlightMissionRecord");
            swloctrim.WriteAttributeString("ID", (lastRecordN++).ToString("0000000"));
            swloctrim.WriteElementString("Name", "MP");
            swloctrim.WriteStartElement("FlightBlock");
            swloctrim.WriteStartElement("FlightPlan");
            swloctrim.WriteAttributeString("height", "100");
            swloctrim.WriteAttributeString("percentForwardOverlap", "75");
            swloctrim.WriteAttributeString("percentLateralOverlap", "75");
            //swloctrim.WriteElementString("Node", "");
            //swloctrim.WriteElementString("Node", "");
            //swloctrim.WriteElementString("Node", "");
            //swloctrim.WriteElementString("Node", "");
            swloctrim.WriteEndElement();
            swloctrim.WriteStartElement("StationList");
            foreach (int station in JXL_StationIDs)
            {
                swloctrim.WriteElementString("StationID", station.ToString("0000000"));
            }
            swloctrim.WriteEndElement();
            swloctrim.WriteEndElement();
            swloctrim.WriteEndElement();
        }

        private int GenPhotoStationRecord(XmlTextWriter swloctrim, string imgname, double lat, double lng, double alt,
            double roll, double pitch, double yaw, int imgwidth, int imgheight, int lastRecordN)
        {
            Console.WriteLine("yaw {0}", yaw);

            int photoStationID = lastRecordN++;
            int pointRecordID = lastRecordN++;
            int imageRecordID = lastRecordN++;

            JXL_StationIDs.Add(photoStationID);

            // conver tto rads
            yaw = -yaw * deg2rad;

            swloctrim.WriteStartElement("PhotoStationRecord");
            swloctrim.WriteAttributeString("ID", (photoStationID).ToString("0000000"));

            swloctrim.WriteElementString("StationName", Path.GetFileNameWithoutExtension(imgname));
            swloctrim.WriteElementString("InstrumentHeight", "");

            swloctrim.WriteStartElement("RawInstrumentHeight");
            swloctrim.WriteElementString("MeasurementMethod", "TrueHeight");
            swloctrim.WriteElementString("MeasuredHeight", "0");
            swloctrim.WriteElementString("HorizontalOffset", "0");
            swloctrim.WriteElementString("VerticalOffset", "0");
            swloctrim.WriteEndElement();

            swloctrim.WriteElementString("InstrumentID", "0000000E");
            swloctrim.WriteElementString("AtmosphereID", "0000000F");
            swloctrim.WriteElementString("StationType", "RawSensorValues");

            swloctrim.WriteStartElement("DeviceAxisOrientationData");
            swloctrim.WriteStartElement("DeviceAxisOrientation");
            swloctrim.WriteStartElement("BiVector");
            swloctrim.WriteElementString("XX", roll.ToString());
            swloctrim.WriteElementString("YY", pitch.ToString());
            swloctrim.WriteElementString("ZZ", yaw.ToString());
            swloctrim.WriteEndElement();
            swloctrim.WriteEndElement();
            swloctrim.WriteEndElement();
            swloctrim.WriteEndElement();
            // end PhotoStationRecord

            // pointrecord

            swloctrim.WriteStartElement("PointRecord");
            swloctrim.WriteAttributeString("ID", (pointRecordID).ToString("0000000"));

            swloctrim.WriteElementString("Name", Path.GetFileNameWithoutExtension(imgname));
            swloctrim.WriteElementString("Code", "");
            swloctrim.WriteElementString("Method", "Coordinates");
            swloctrim.WriteElementString("SurveyMethod", "Autonomous");
            swloctrim.WriteElementString("Classification", "Normal");
            swloctrim.WriteElementString("Deleted", "false");
            swloctrim.WriteStartElement("WGS84");
            swloctrim.WriteElementString("Latitude", lat.ToString());
            swloctrim.WriteElementString("Longitude", lng.ToString());
            swloctrim.WriteElementString("Height", alt.ToString());
            swloctrim.WriteEndElement();
            swloctrim.WriteEndElement();

            // end point record

            // imagerecord
            swloctrim.WriteStartElement("ImageRecord");
            swloctrim.WriteAttributeString("ID", (imageRecordID).ToString("0000000"));
            swloctrim.WriteElementString("StationID", (photoStationID).ToString("0000000"));
            swloctrim.WriteElementString("BackBearingID", "");
            swloctrim.WriteElementString("CameraID", "00000002");
            swloctrim.WriteElementString("PointRecordID", "");
            swloctrim.WriteElementString("FileName", Path.GetFileName(imgname));
            swloctrim.WriteElementString("HorizontalAngle", "");
            swloctrim.WriteElementString("VerticalAngle", "");
            swloctrim.WriteElementString("Width", imgwidth.ToString());
            swloctrim.WriteElementString("Height", imgheight.ToString());
            swloctrim.WriteElementString("SourceX", "0");
            swloctrim.WriteElementString("SourceY", "0");
            swloctrim.WriteElementString("SourceWidth", imgwidth.ToString());
            swloctrim.WriteElementString("SourceHeight", imgheight.ToString());
            swloctrim.WriteEndElement();
            /*
    <ImageRecord ID="0000056" TimeStamp="2013-04-12T10:22:21">
      <StationID>0000010</StationID>
      <BackBearingID/>
      <CameraID>00000002</CameraID>
      <PointRecordID/>
      <FileName>R0011726.JPG</FileName>
      <HorizontalAngle/>
      <VerticalAngle/>
      <Width>3648</Width>
      <Height>2736</Height>
      <SourceX>0</SourceX>
      <SourceY>0</SourceY>
      <SourceWidth>3648</SourceWidth>
      <SourceHeight>2736</SourceHeight>
    </ImageRecord>
             * */

            return lastRecordN;
        }

        private RectangleF getboundingbox(double centery, double centerx, double alt, double angle, double width, double height)
        {
            double lat = centery;
            double lng = centerx;
            double alpha = angle;

            var rect = ImageProjection.calc(new PointLatLngAlt(lat, lng, alt), 0, 0, alpha, width, height);



            double minx = 999, miny = 999, maxx = -999, maxy = -999;

            foreach (var pnt in rect)
            {
                maxx = Math.Max(maxx, pnt.Lat);

                minx = Math.Min(minx, pnt.Lat);

                miny = Math.Min(miny, pnt.Lng);

                maxy = Math.Max(maxy, pnt.Lng);
            }

            Console.WriteLine("{0} {1} {2} {3}", minx, maxx, miny, maxy);

            return new RectangleF((float)miny, (float)minx, (float)(maxy - miny), (float)(maxx - minx));
        }

        public static double radians(double val)
        {
            return val * deg2rad;
        }

        public static double degrees(double val)
        {
            return val * rad2deg;
        }

        private void newpos(ref double lat, ref double lon, double bearing, double distance)
        {
            // '''extrapolate latitude/longitude given a heading and distance 
            //   thanks to http://www.movable-type.co.uk/scripts/latlong.html
            //  '''
            // from math import sin, asin, cos, atan2, radians, degrees
            double radius_of_earth = 6378100.0; //# in meters

            double lat1 = radians(lat);
            double lon1 = radians(lon);
            double brng = radians((bearing + 360) % 360);
            double dr = distance / radius_of_earth;

            double lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(dr) +
                                    Math.Cos(lat1) * Math.Sin(dr) * Math.Cos(brng));
            double lon2 = lon1 + Math.Atan2(Math.Sin(brng) * Math.Sin(dr) * Math.Cos(lat1),
                Math.Cos(dr) - Math.Sin(lat1) * Math.Sin(lat2));

            lat = degrees(lat2);
            lon = degrees(lon2);
            //return (degrees(lat2), degrees(lon2));
        }

        private void writeGPX(string filename, Dictionary<string, PictureInformation> pictureList)
        {
            using (
                System.Xml.XmlTextWriter xw =
                    new System.Xml.XmlTextWriter(
                        Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar +
                        Path.GetFileNameWithoutExtension(filename) + ".gpx", Encoding.ASCII))
            {
                xw.WriteStartElement("gpx");

                xw.WriteStartElement("trk");

                xw.WriteStartElement("trkseg");

                foreach (PictureInformation p in pictureList.Values)
                {
                    xw.WriteStartElement("trkpt");
                    xw.WriteAttributeString("lat", p.Lat.ToString(new System.Globalization.CultureInfo("en-US")));
                    xw.WriteAttributeString("lon", p.Lon.ToString(new System.Globalization.CultureInfo("en-US")));

                    // must stay as above

                    xw.WriteElementString("time", p.Time.ToString("yyyy-MM-ddTHH:mm:ssZ"));

                    xw.WriteElementString("ele", p.RelAlt.ToString(new System.Globalization.CultureInfo("en-US")));
                    xw.WriteElementString("course", p.Yaw.ToString(new System.Globalization.CultureInfo("en-US")));

                    xw.WriteElementString("compass", p.Yaw.ToString(new System.Globalization.CultureInfo("en-US")));

                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
                xw.WriteEndElement();
                xw.WriteEndElement();
            }

        }

        //    //private void BUT_browsedir_Click(object sender, EventArgs e)
        //    //{
        //    //    try
        //    //    {
        //    //        folderBrowserDialog1.SelectedPath = Path.GetDirectoryName(TXT_logfile.Text);
        //    //    }
        //    //    catch
        //    //    {
        //    //    }

        //    //    folderBrowserDialog1.ShowDialog();

        //    //    if (folderBrowserDialog1.SelectedPath != "")
        //    //    {
        //    //        TXT_jpgdir.Text = folderBrowserDialog1.SelectedPath;

        //    //        string file = folderBrowserDialog1.SelectedPath + Path.DirectorySeparatorChar + "location.txt";

        //    //        if (File.Exists(file))
        //    //        {
        //    //            try
        //    //            {
        //    //                using (StreamReader sr = new StreamReader(file))
        //    //                {
        //    //                    string cotent = sr.ReadToEnd();

        //    //                    Match match = Regex.Match(cotent, "seconds_offset: ([0-9]+)");

        //    //                    if (match.Success)
        //    //                    {
        //    //                        TXT_offsetseconds.Text = match.Groups[1].Value;
        //    //                    }
        //    //                }
        //    //            }
        //    //            catch
        //    //            {
        //    //            }
        //    //        }
        //    //    }
        //    //}


        //    private void BUT_estoffset_Click(object sender, EventArgs e)
        //    {
        //        //doworkLegacy(TXT_logfile.Text, TXT_jpgdir.Text, 0, true);
        //        double offset = EstimateOffset(TXT_logfile.Text, TXT_jpgdir.Text);

        //        AppendLogTextBox("\nOffset around :  " + offset.ToString(CultureInfo.InvariantCulture) + "\n\n");
        //    }

        public ImageBladeGroup GeotagimagesAndCrop(ImageBladeGroup ImageGroup, ProgressBar PBar,int row)
        {
            try
            {
                // Save file into Geotag folder
                string rootFolder = ImageGroup.BaseDirectory;
                string geoTagFolder = rootFolder + Path.DirectorySeparatorChar + "geotagged";

                string selected = Path.Combine(ImageGroup.BaseDirectory, "selected");

                if (Directory.Exists(selected)) Directory.Delete(selected, true);

                Directory.CreateDirectory(Path.Combine(selected, "1"));
                Directory.CreateDirectory(Path.Combine(selected, "2"));
                Directory.CreateDirectory(Path.Combine(selected, "3"));
                Directory.CreateDirectory(Path.Combine(selected, "4"));
                Directory.CreateDirectory(Path.Combine(selected, "5"));

                bool isExists = System.IO.Directory.Exists(geoTagFolder);

                // delete old files and folder
                if (isExists) Directory.Delete(geoTagFolder, true);

                // create it again
                System.IO.Directory.CreateDirectory(geoTagFolder);

                if (ImageGroup.FullImageList == null)
                {
                    AppendLogTextBox("\no valid match");
                    return ImageGroup;
                }

                try
                {
                    int cnt = 0;
                    foreach (ImageLocationAndExtraInfo ImageLocInfo in ImageGroup.FullImageList)
                    {
                        //check if we are in another tab. if we are pause tempoarily
                        while (GetMainTabIndex() != 0)
                        {
                            GC.Collect();
                            GC.WaitForFullGCComplete();
                            Thread.Sleep(1000);
                        }

                        try
                        {
                            System.GC.Collect();
                            string geofilename;
                            ImageLocationAndExtraInfo imageLoctmp = WriteCoordinatesToImage(ImageGroup.BaseDirectory, ImageLocInfo);
                            ImageLocInfo.RightCrop = imageLoctmp.RightCrop;
                            ImageLocInfo.LeftCrop = imageLoctmp.LeftCrop;
                        }
                        catch (Exception e)
                        {
                            AppendLogTextBox("\nFAILED to Geotag\t" + ImageLocInfo.PathToOrigionalImage);
                            AppendLogTextBox("\n ******ERROR***** \n" + e.ToString());
                        }
                        if (ImageLocInfo.selected)
                        {
                            //create cropped copy in destination folder
                            switch (ImageLocInfo.Type)
                            {
                                case ImageLocationType.Pass1:
                                    ImageLocInfo.PathToDestination = Path.Combine(selected, "1", Path.GetFileName(ImageLocInfo.PathToGeoTaggedImage));
                                    break;
                                case ImageLocationType.Pass2:
                                    ImageLocInfo.PathToDestination = Path.Combine(selected, "2", Path.GetFileName(ImageLocInfo.PathToGeoTaggedImage));
                                    break;
                                case ImageLocationType.Pass3:
                                    ImageLocInfo.PathToDestination = Path.Combine(selected, "3", Path.GetFileName(ImageLocInfo.PathToGeoTaggedImage));
                                    break;
                                case ImageLocationType.Pass4:
                                    ImageLocInfo.PathToDestination = Path.Combine(selected, "4", Path.GetFileName(ImageLocInfo.PathToGeoTaggedImage));
                                    break;
                                case ImageLocationType.Pass5:
                                    ImageLocInfo.PathToDestination = Path.Combine(selected, "5", Path.GetFileName(ImageLocInfo.PathToGeoTaggedImage));
                                    break;
                            }


                            try
                            {
                                AppendLogTextBox("\nCropping \t" + ImageLocInfo.PathToOrigionalImage);
                                using (Cropper Crop = new Cropper())
                                {
                                    BladeCroppingSettings CropSettings = new BladeCroppingSettings(0, 0);
                                    string tmpERRORSTRING = Crop.JustCrop(CropSettings, ImageLocInfo.PathToGeoTaggedImage, ImageLocInfo.PathToDestination, ImageLocInfo.LeftCrop * 10, ImageLocInfo.RightCrop * 10);
                                    if (tmpERRORSTRING != "OK")
                                    {
                                        AppendLogTextBox("\nFAILED to Crop " + Path.GetFileName(ImageLocInfo.PathToGeoTaggedImage) + "\n***ERROR***\n" + tmpERRORSTRING);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                AppendLogTextBox("\nFAILED to Crop\t" + ImageLocInfo.PathToOrigionalImage);
                                AppendLogTextBox("\n ******ERROR***** \n" + e.ToString());
                            }
                        }


                        cnt++;
                        setAProgbar(PBar, ImageGroup.FullImageList.Count, cnt);
                    }
                }
                
                catch (Exception e)
                {
                    AppendLogTextBox("\n ******ERROR***** \n" + e.ToString());
                }


                AppendLogTextBox("\nGeoTagging and cropping thread finished \n");

                EnableDisableButton((Button)this.ATable.Table.GetControlFromPosition(5, row),false,Color.LimeGreen,"Complete");

                return ImageGroup;
            }
            catch (Exception e)
            {
                AppendLogTextBox("\n ******ERROR***** \n" + e.Message);
                return ImageGroup;
            }
        }

        private byte[] coordtobytearray(double coordin)
        {
            double coord = Math.Abs(coordin);

            byte[] output = new byte[sizeof(double) * 3];

            int d = (int)coord;
            int m = (int)((coord - d) * 60);
            double s = ((((coord - d) * 60) - m) * 60);
            /*
            21 00 00 00 01 00 00 00--> 33/1
            18 00 00 00 01 00 00 00--> 24/1
            06 02 00 00 0A 00 00 00--> 518/10
            */

            Array.Copy(BitConverter.GetBytes((uint)d), 0, output, 0, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)1), 0, output, 4, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)m), 0, output, 8, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)1), 0, output, 12, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)(s * 1.0e7)), 0, output, 16, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)1.0e7), 0, output, 20, sizeof(uint));
            /*
            Array.Copy(BitConverter.GetBytes((uint)d * 1.0e7), 0, output, 0, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)1.0e7), 0, output, 4, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)0), 0, output, 8, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)1), 0, output, 12, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)0), 0, output, 16, sizeof(uint));
            Array.Copy(BitConverter.GetBytes((uint)1), 0, output, 20, sizeof(uint));
            */
            return output;
        }

        private ImageLocationAndExtraInfo WriteCoordinatesToImage(string jpgdir, ImageLocationAndExtraInfo imageInfo)
        {

            // Save file into Geotag folder
            string rootFolder = jpgdir;
            string geoTagFolder = rootFolder + Path.DirectorySeparatorChar + "geotagged";

            string outputfilename = geoTagFolder + Path.DirectorySeparatorChar +
                                    Path.GetFileNameWithoutExtension(imageInfo.PathToOrigionalImage) + "_geotag" +
                                    Path.GetExtension(imageInfo.PathToOrigionalImage);
            imageInfo.PathToGeoTaggedImage = outputfilename;

            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(imageInfo.PathToOrigionalImage)))
            {
                AppendLogTextBox("\nGeoTagging \t" + imageInfo.PathToOrigionalImage);
                Application.DoEvents();

                using (Image Pic = Image.FromStream(ms))
                {
                    PropertyItem[] pi = Pic.PropertyItems;

                    bool NeedsUID = true;
                    foreach (PropertyItem item in pi)
                    {

                        if (item.Type == 0xA420)
                        {
                            NeedsUID = false;
                        }
                    }
                    if (NeedsUID)
                    {
                        Guid UID = Guid.NewGuid();
                        pi[0].Id = 0xA420;
                        pi[0].Type = 2;
                        pi[0].Len = 36;
                        pi[0].Value = Encoding.ASCII.GetBytes(UID.ToString());
                        Pic.SetPropertyItem(pi[0]);
                    }

                    pi[0].Id = 0x0004;
                    pi[0].Type = 5;
                    pi[0].Len = sizeof(ulong) * 3;
                    pi[0].Value = coordtobytearray(imageInfo.Longitude);
                    Pic.SetPropertyItem(pi[0]);

                    pi[0].Id = 0x0002;
                    pi[0].Type = 5;
                    pi[0].Len = sizeof(ulong) * 3;
                    pi[0].Value = coordtobytearray(imageInfo.Latitude);
                    Pic.SetPropertyItem(pi[0]);

                    pi[0].Id = 0x0006;
                    pi[0].Type = 5;
                    pi[0].Len = 8;
                    pi[0].Value = new Rational(imageInfo.Altitude).GetBytes();
                    Pic.SetPropertyItem(pi[0]);



                    pi[0].Id = 1;
                    pi[0].Len = 2;
                    pi[0].Type = 2;

                    if (imageInfo.Latitude < 0)
                    {
                        pi[0].Value = new byte[] { (byte)'S', 0 };
                    }
                    else
                    {
                        pi[0].Value = new byte[] { (byte)'N', 0 };
                    }

                    Pic.SetPropertyItem(pi[0]);

                    pi[0].Id = 3;
                    pi[0].Len = 2;
                    pi[0].Type = 2;
                    if (imageInfo.Longitude < 0)
                    {
                        pi[0].Value = new byte[] { (byte)'W', 0 };
                    }
                    else
                    {
                        pi[0].Value = new byte[] { (byte)'E', 0 };
                    }
                    Pic.SetPropertyItem(pi[0]);


                    // Just in case
                    if (NeedsUID)
                    {
                        File.Delete(imageInfo.PathToOrigionalImage);
                        Pic.Save(imageInfo.PathToOrigionalImage);
                    }
                    File.Delete(outputfilename);
                    Pic.Save(outputfilename);

                }
            }

            return imageInfo;
        }

        private void Networklinkgeoref()
        {
            System.Diagnostics.Process.Start(Path.GetDirectoryName(Application.ExecutablePath) +
                                             Path.DirectorySeparatorChar + "m3u" + Path.DirectorySeparatorChar +
                                             "GeoRefnetworklink.kml");
        }

        //    //private void TXT_logfile_TextChanged(object sender, EventArgs e)
        //    //{
        //    //    if (vehicleLocations != null)
        //    //        vehicleLocations.Clear();
        //    //    if (picturesInfo != null)
        //    //        picturesInfo.Clear();

        //    //    BUT_Geotagimages.Enabled = false;
        //    //}

        //    //private void ProcessType_CheckedChanged(object sender, EventArgs e)
        //    //{
        //    //    if (RDIO_CAMMsgSynchro.Checked)
        //    //    {
        //    //        selectedProcessingMode = PROCESSING_MODE.CAM_MSG;
        //    //        PANEL_TIME_OFFSET.Enabled = false;
        //    //        PANEL_SHUTTER_LAG.Enabled = true;
        //    //    }
        //    //    else
        //    //    {
        //    //        selectedProcessingMode = PROCESSING_MODE.TIME_OFFSET;
        //    //        PANEL_TIME_OFFSET.Enabled = true;
        //    //        PANEL_SHUTTER_LAG.Enabled = false;
        //    //    }
        //    //}


        //    //private void TXT_shutterLag_TextChanged(object sender, EventArgs e)
        //    //{
        //    //    bool convertedOK = int.TryParse(TXT_shutterLag.Text, NumberStyles.Integer, CultureInfo.InvariantCulture,
        //    //        out millisShutterLag);

        //    //    if (!convertedOK)
        //    //        TXT_shutterLag.Text = "0";
        //    //}

        //    private void CHECK_AMSLAlt_Use_CheckedChanged(object sender, EventArgs e)
        //    {
        //        useAMSLAlt = ((CheckBox) sender).Checked;

        //        txt_basealt.Enabled = !useAMSLAlt;
        //    }

        //    private void chk_cammsg_CheckedChanged(object sender, EventArgs e)
        //    {
        //        if (vehicleLocations != null)
        //            vehicleLocations.Clear();
        //    }

        //    private void chk_usegps2_CheckedChanged(object sender, EventArgs e)
        //    {
        //        if (vehicleLocations != null)
        //            vehicleLocations.Clear();
        //    }



        private static Regex r = new Regex(":");
        public static DateTime GetDateTakenFromImage(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (Image myImage = Image.FromStream(fs, false, false))
                {
                    PropertyItem propItem = myImage.GetPropertyItem(36867);
                    string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                    return DateTime.Parse(dateTaken);
                }
            }
            catch
            {
                return new DateTime();
            }
        }

        private List<string> GetAllImagesinDirectory(string ImageDir)
        { // get collection of images on the card
            IEnumerable<string> ImageFiles = Directory.EnumerateFiles(ImageDir, "*.JPG", SearchOption.TopDirectoryOnly);
            ImageFiles.Concat(Directory.EnumerateFiles(ImageDir, "*.jpg", SearchOption.TopDirectoryOnly)); // adds other file types

            List<string> tmpImageFileList = ImageFiles.ToList();
            List<string> ImageFileList = new List<string>();
            //filter out tlog images
            foreach (string file in tmpImageFileList)
            {
                if (!file.Contains("tlog"))
                {
                    ImageFileList.Add(file);
                }
            }
            return ImageFileList;
        }

        private DateTime GetFirstSustainedImageTime(string ImageDir)
        {
            DateTime SusImTime = new DateTime();
            List<string> Allimages = GetAllImagesinDirectory(ImageDir);
            if (Allimages != null)
            {
                string[] Images = Allimages.ToArray(); // create image list array
                DateTime[] ModTimes = new DateTime[Images.Length];//array of modified times for sorting 

                for (int i = 0; i < Images.Length; i++) // go through each image and get its modified time
                {
                    ModTimes[i] = GetDateTakenFromImage(Images[i]);  //new FileInfo(Images[i]).LastWriteTime;
                }
                Array.Sort(ModTimes, Images);// sort image list array by time taken

                int cntr = 0;

                DateTime FirstSustained = GetDateTakenFromImage(Images[0]);
                DateTime LastTime = FirstSustained;

                string ImUsed;

                foreach (string Im in Images)
                {
                    DateTime thisTime = GetDateTakenFromImage(Im);

                    if ((thisTime - LastTime).TotalSeconds < 10)
                    {
                        if (cntr > 9)
                        {
                            //AppendLogTextBox("\n\n Image returned on: " + Im);
                            return FirstSustained;
                        }
                        cntr++;
                    }
                    else
                    {
                        cntr = 0;
                        FirstSustained = GetDateTakenFromImage(Im);
                        ImUsed = Im;
                        //AppendLogTextBox("\n\n Image used: " + Im);
                    }

                    LastTime = thisTime;
                }
            }

            return SusImTime;

        }
        private DateTime GetFirstSustainedTriggerTime(string fn)
        {
            DateTime SusTrigTime = new DateTime();

            Dictionary<long, VehicleLocation> vehicletriggerList = new Dictionary<long, VehicleLocation>();
            // Telemetry Log
            if (fn.ToLower().EndsWith("tlog"))
            {

                System.IO.FileStream logplaybackfile = new System.IO.FileStream(fn, FileMode.Open);

                MAVLinkInterface mine = new MAVLinkInterface(logplaybackfile);

                bool WasLastOn = false;

                MissionPlanner.CurrentState cs = new MissionPlanner.CurrentState();

                while (mine.logplaybackfile.BaseStream.Position < mine.logplaybackfile.BaseStream.Length)
                {
                    MAVLink.MAVLinkMessage packet = mine.readPacket();

                    cs.datetime = mine.lastlogread;

                    cs.UpdateCurrentSettings(null, true, mine);

                    VehicleLocation location = new VehicleLocation();
                    location.Time = cs.datetime;
                    bool tmponval = false;


                    if (((cs.ch7in > 1520) || (cs.ch7in < 1480)) && (cs.ch7in != 0)) //if ch7 val is not default we assume camera is on
                    {
                        tmponval = true;
                        if (!WasLastOn)
                        {
                            SusTrigTime = cs.datetime;
                        }
                        else
                        {
                            // logic to check if we have a sustained trigger
                            System.TimeSpan SusTime = cs.datetime - SusTrigTime;
                            double Sustimesec = SusTime.TotalSeconds;

                            if (Sustimesec > 30)
                            {
                                mine.logplaybackfile.Close();
                                return SusTrigTime;
                            }
                        }
                        WasLastOn = true;
                    }
                    else
                    {
                        if (cs.ch7in != 0)
                        {
                            tmponval = false;
                            if (WasLastOn)
                            {
                                // logic to check if we have a sustained trigger
                                System.TimeSpan SusTime = cs.datetime - SusTrigTime;
                                double Sustimesec = SusTime.TotalSeconds;

                                if (Sustimesec > 30)
                                {
                                    mine.logplaybackfile.Close();
                                    return SusTrigTime;
                                }
                            }
                        }
                        WasLastOn = false;
                    }
                }
                mine.logplaybackfile.Close();
            }
            return SusTrigTime;
        }
        private void BUT_TAG_Click(object sender, EventArgs e)
        {
            //if (PostProcessThread != null)
            //{
            //    if (PostProcessThread.IsAlive)
            //    {
            //        MessageBox.Show("Post-Proccessing Thread is already running you must wait for it to complete before starting a new process");
            //        return;
            //    }
            //}
            //    for (int i = 1; i < ATable.Table.RowCount; i++)
            //    {
            //        try
            //        {
            //            CheckBox tmp = (CheckBox)ATable.Table.GetControlFromPosition(3, i);
            //            if (tmp.Checked)
            //            {
            //                if (!MainTabs.ContainsKey(((Label)ATable.Table.GetControlFromPosition(0, i)).Text))
            //                {
            //                    MainTabs.Add(((Label)ATable.Table.GetControlFromPosition(0, i)).Text, new TabPage("+"));
            //                }
            //            }

            //        }
            //        catch (Exception errors)
            //        {
            //            AppendLogTextBox("\n\nFAILED with exemption:\n" + errors.ToString());
            //            return;
            //        }

            //    }

            //    PreThreads.Add(new Thread(TaggingThread));
            //    return;

        }
        private void TaggingThread()
        {
            //AppendLogTextBox("\n\nStarting Process thread");
            //if (ATable.Table.RowCount == 1)
            //{
            //    AppendLogTextBox("\n  WARNING : NO BOXES SELECTED. ");
            //}
            //for (int i = 1; i < ATable.Table.RowCount; i++)
            //{
            //    try
            //    {
            //        CheckBox tmp = (CheckBox)ATable.Table.GetControlFromPosition(3, i);
            //        if (tmp.Checked) TagARow(i);
            //    }
            //    catch (Exception e)
            //    {
            //        AppendLogTextBox("\n\nFAILED with exemption:\n" + e.ToString());
            //        return;
            //    }

            //}
            //AppendLogTextBox("\n\n Tagging thread complete");
        }

        private void TagARow(int row)
        {
            string dirPictures = ATable.Table.GetControlFromPosition(0, row).Text;
            string logFilePath = Path.Combine(dirPictures, ATable.Table.GetControlFromPosition(1, row).Text);
            ProgressBar Progbar = (ProgressBar)ATable.Table.GetControlFromPosition(4, row);
            ProgressBar CroppProgbar = (ProgressBar)ATable.Table.GetControlFromPosition(6, row);

            ImageBladeGroup RowImagesCollection = new ImageBladeGroup();

            if (!File.Exists(logFilePath))
            {
                MessageBox.Show("tlog file " + logFilePath + " does not exist ");
                return;
            }
            if (!Directory.Exists(dirPictures))
            {
                MessageBox.Show("Image directory " + dirPictures + " does not exist ");
                return;
            }

            float secondsOffset = 0;

            if (
                float.TryParse(ATable.Table.GetControlFromPosition(2, row).Text, NumberStyles.Float, CultureInfo.InvariantCulture, out secondsOffset) ==
                false)
            {
                AppendLogTextBox("\nOffset number not in correct format. Use . as decimal separator\n");
                return;
            }

            try
            {
                picturesInfo = doworkGPSOFFSET(logFilePath, dirPictures, secondsOffset);
                if (picturesInfo != null) CreateReportFiles(picturesInfo, dirPictures, secondsOffset);

                RowImagesCollection.FullImageList = CreateImageInfoList(picturesInfo);
                RowImagesCollection.tlogFileName = logFilePath;
                RowImagesCollection.GPStimeOffset = secondsOffset;
                RowImagesCollection.BaseDirectory = dirPictures;
            }
            catch (Exception ex)
            {
                AppendLogTextBox("\nError " + ex.ToString());
            }
            try
            {
                if (!Directory.Exists(Path.Combine(RowImagesCollection.BaseDirectory, "Smalls")))
                {
                    Directory.CreateDirectory(Path.Combine(RowImagesCollection.BaseDirectory, "Smalls"));
                }
                int progCounter = 0;
                foreach (ImageLocationAndExtraInfo IM_LOC in RowImagesCollection.FullImageList)
                {


                    //check if we are in another tab. if we are pause tempoarily
                    while (GetMainTabIndex()!= 0)
                    {
                        GC.Collect();
                        GC.WaitForFullGCComplete();
                        Thread.Sleep(1000);
                    }

                    //check if smalls exist
                    AppendLogTextBox("\nPre-processing " + IM_LOC.PathToOrigionalImage);

                    //if they do not exist create them
                    if (!File.Exists(IM_LOC.PathToSmallImage))
                    {
                        using (Image<Bgr, Byte> currentImage = new Image<Bgr, byte>(IM_LOC.PathToOrigionalImage))
                        {
                            Image<Bgr, Byte> SmallImage = currentImage.Resize(currentImage.Width / 10, currentImage.Height / 10, Inter.Linear);

                            SmallImage.Save(IM_LOC.PathToSmallImage);
                            SmallImage.Dispose();
                        }
                    }
                    using (Image<Bgr, Byte> currentImage = new Image<Bgr, byte>(IM_LOC.PathToSmallImage))
                    {
                        //get crop values here
                        //
                        int[] CropReturn;
                        using (Cropper Crop = new Cropper())
                        {
                            BladeCroppingSettings CropSettings = new BladeCroppingSettings(0, 0);
                            CropReturn = Crop.getCropValues(CropSettings, currentImage);
                            IM_LOC.LeftCrop = CropReturn[0];
                            IM_LOC.RightCrop = CropReturn[1];
                        }
                        Thread.Sleep(100); // Slows down thead to avoid crashing if another process is running. Requires better fix
                    }
                    progCounter++;
                    setAProgbar(Progbar, RowImagesCollection.FullImageList.Count, progCounter);
                }
                RowImagesCollection = SortImagesByPasses(RowImagesCollection);
            }
            catch (Exception e)
            {
                AppendLogTextBox("\n Error occured while pre-processing");
                AppendLogTextBox("\n ******ERROR***** \n" + e.ToString());
            }
            try
            {
                try
                {

                    string ProgFile = Path.Combine(RowImagesCollection.BaseDirectory, "Processed.xml");
                    if (File.Exists(ProgFile))
                    {
                        ImageBladeGroup LastSavedData = new ImageBladeGroup();
                        System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(ImageBladeGroup));
                        System.IO.StreamReader file = new System.IO.StreamReader(ProgFile);
                        LastSavedData = (ImageBladeGroup)reader.Deserialize(file);
                        file.Close();

                        //figure out which parts of the file to keep

                        if (LastSavedData.GPStimeOffset == RowImagesCollection.GPStimeOffset)
                        {
                            AppendLogTextBox("\nUsing values from last session");
                            //if offset is the same replace file entirely
                            RowImagesCollection = LastSavedData;
                        }
                        else
                        {
                            //else import crop data and pass info
                            AppendLogTextBox("\nUpdating croping values from last session. Images must be reselected.");
                            foreach (ImageLocationAndExtraInfo imageinfo in LastSavedData.FullImageList)
                            {
                                try
                                {
                                    //update crop info
                                    ImageLocationAndExtraInfo NewInfo = RowImagesCollection.FullImageList.Find(x => x.PathToOrigionalImage == imageinfo.PathToOrigionalImage);
                                    NewInfo.LeftCrop = imageinfo.LeftCrop;
                                    NewInfo.RightCrop = imageinfo.RightCrop;
                                }
                                catch
                                {
                                    AppendLogTextBox("\n\nWarning: " + imageinfo.PathToOrigionalImage + " last crop values not saved");
                                }

                            }
                        }
                    }
                    System.Xml.Serialization.XmlSerializer Progsavewriter = new System.Xml.Serialization.XmlSerializer(typeof(ImageBladeGroup));
                    System.IO.FileStream wfile = System.IO.File.Create(ProgFile);
                    Progsavewriter.Serialize(wfile, RowImagesCollection);
                    wfile.Close();
                }
                catch (Exception e)
                {
                    AppendLogTextBox("\n Error occured in reading saved proccesing data");
                    AppendLogTextBox("\n ******ERROR***** \n" + e.ToString());
                }

                
                //enable post processing button
                EnableDisableButton((Button)ATable.Table.GetControlFromPosition(5, row),true,Color.Yellow,"Ready");
                EnableDisableButton((Button)ATable.Table.GetControlFromPosition(3, row), true,Color.LimeGreen,"Complete");
            }
            catch (Exception e)
            {
                AppendLogTextBox("\n Error occured in new tab creation");
                AppendLogTextBox("\n ******ERROR***** \n" + e.ToString());
            }
        }

        private void CreateNewBladeTab(string BaseDir, ProgressBar PB,int row ,int timeout = 0)
        {

            timeout++;
            if (timeout > 3)
            {
                Thread.Sleep(100);
                return;
            }

            if (InvokeRequired)
            {
                this.Invoke(new Action<string, ProgressBar,int, int>(CreateNewBladeTab), new object[] { BaseDir, PB,row, timeout });
                return;
            }

            MAIN_TAB_CONTROL.TabPages.Add(MainTabs[BaseDir]);

            TurbineTab TurbTab = new TurbineTab(BaseDir, PB, this,row);

            TurbTab.populatePassImages();

            TurbTab.Parent = MainTabs[BaseDir];
            TurbTab.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            TurbTab.Dock = DockStyle.Fill;


        }


        private List<ImageLocationAndExtraInfo> CreateImageInfoList(Dictionary<string, PictureInformation> PictureData)
        {
            // for each image get geo data


            List<ImageLocationAndExtraInfo> ImageLocationList = new List<ImageLocationAndExtraInfo>();

            double lastHeight = 0;
            DateTime lastImageTime = new DateTime();

            foreach (PictureInformation picInfo in picturesInfo.Values) //create list of usable objects
            {


                ImageLocationAndExtraInfo tmpImgLoc = new ImageLocationAndExtraInfo();

                tmpImgLoc.Latitude = picInfo.Lat;
                tmpImgLoc.Longitude = picInfo.Lon;
                tmpImgLoc.Altitude = picInfo.RelAlt;
                tmpImgLoc.VertVelocity = (picInfo.RelAlt - lastHeight) / (picInfo.ShotTimeReportedByCamera - lastImageTime).TotalSeconds;

                tmpImgLoc.Time = picInfo.ShotTimeReportedByCamera;
                tmpImgLoc.PathToOrigionalImage = picInfo.Path;

                tmpImgLoc.PathToSmallImage = Path.Combine(Path.GetDirectoryName(tmpImgLoc.PathToOrigionalImage), "Smalls", Path.GetFileName(tmpImgLoc.PathToOrigionalImage));
                tmpImgLoc.PathToGreyImage = Path.Combine(Path.GetDirectoryName(tmpImgLoc.PathToOrigionalImage), "Greys", Path.GetFileName(tmpImgLoc.PathToOrigionalImage));

                tmpImgLoc.PathToGeoTaggedImage = Path.GetDirectoryName(tmpImgLoc.PathToOrigionalImage) + Path.DirectorySeparatorChar + "geotagged" + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(tmpImgLoc.PathToOrigionalImage) + "_geotag" + Path.GetExtension(tmpImgLoc.PathToOrigionalImage);

                lastHeight = picInfo.RelAlt;
                lastImageTime = picInfo.ShotTimeReportedByCamera;

                ImageLocationList.Add(tmpImgLoc);
            }


            return ImageLocationList;

        }
        private ImageBladeGroup SortImagesByPasses(ImageBladeGroup ImageGroup)
        {

            ImageGroup.FullImageList = SortImagesByType(ImageGroup.FullImageList);

            ImageGroup.FullImageList = FilterPassGoingUP(ImageGroup.FullImageList, ImageLocationType.Pass1);
            ImageGroup.FullImageList = FilterPassGoingDOWN(ImageGroup.FullImageList, ImageLocationType.Pass2);
            ImageGroup.FullImageList = FilterPassGoingUP(ImageGroup.FullImageList, ImageLocationType.Pass3);
            ImageGroup.FullImageList = FilterPassGoingDOWN(ImageGroup.FullImageList, ImageLocationType.Pass4);

            return ImageGroup;
        }


        private List<ImageLocationAndExtraInfo> SortImagesByType(List<ImageLocationAndExtraInfo> ImageLocationList)
        {
            double hubHeight = FindHubHeight(ImageLocationList);
            double tipHeight = FindTipHeight(ImageLocationList);

            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {

                if (ImageLoc.Altitude > hubHeight - 8) { ImageLoc.Type = ImageLocationType.High; }
                else if ((ImageLoc.Altitude > 10) && (ImageLoc.Altitude < tipHeight + 8)) { ImageLoc.Type = ImageLocationType.Low; }
                else if ((ImageLoc.Altitude < 10)) { ImageLoc.Type = ImageLocationType.Ground; }
            }
            int hubCNT = 0;
            int tipCNT = 0;
            ImageLocationType tempType = ImageLocationType.Pass1;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if (ImageLoc.Type == ImageLocationType.High)
                {
                    hubCNT++;
                }
                else
                {
                    hubCNT = 0;
                }
                if (ImageLoc.Type == ImageLocationType.Low)
                {
                    tipCNT++;
                }
                else
                {
                    tipCNT = 0;
                }

                if ((hubCNT > 4) && (tempType == ImageLocationType.Pass1) && (ImageLoc.VertVelocity < 0))
                {
                    tempType = ImageLocationType.Pass2;
                }
                if ((tipCNT > 4) && (tempType == ImageLocationType.Pass2) && (ImageLoc.VertVelocity > 0))
                {
                    tempType = ImageLocationType.Pass3;
                }
                if ((hubCNT > 4) && (tempType == ImageLocationType.Pass3) && (ImageLoc.VertVelocity < 0))
                {
                    tempType = ImageLocationType.Pass4;
                }
                ImageLoc.Type = tempType;
            }

            double TipShotHeight = 100000;
            //find tip canidates
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Type == ImageLocationType.Pass2) || (ImageLoc.Type == ImageLocationType.Pass3))
                {
                    if (ImageLoc.Altitude < TipShotHeight)
                    {
                        TipShotHeight = ImageLoc.Altitude;
                    }
                }
            }
            double sum = 0;
            double CNT = 0;
            //get average of tip canidates
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Type == ImageLocationType.Pass2) || (ImageLoc.Type == ImageLocationType.Pass3))
                {
                    if (ImageLoc.Altitude < TipShotHeight + 2)
                    {
                        sum = ImageLoc.Altitude + sum;
                        CNT++;
                    }
                }
            }
            //pull tip photos from pass 2&3
            double T_Height = sum / CNT;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Type == ImageLocationType.Pass2) || (ImageLoc.Type == ImageLocationType.Pass3))
                {
                    if (ImageLoc.Altitude < T_Height + .3)
                    {
                        ImageLoc.Type = ImageLocationType.Pass5;
                        ImageLoc.selected = true;
                    }
                }
            }

            return ImageLocationList;
        }
        private List<ImageLocationAndExtraInfo> FilterPassGoingUP(List<ImageLocationAndExtraInfo> ImageLocationList, ImageLocationType PassNum)
        {

            //select items at a 1m interval min
            double LastVal = 0;
            List<ImageLocationAndExtraInfo> tmpList = new List<ImageLocationAndExtraInfo>();
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Type == PassNum) && (ImageLoc.Altitude >= LastVal + 1))
                {
                    LastVal = ImageLoc.Altitude;
                    ImageLoc.selected = true;
                }
            }
            return ImageLocationList;
        }
        private List<ImageLocationAndExtraInfo> FilterPassGoingDOWN(List<ImageLocationAndExtraInfo> ImageLocationList, ImageLocationType PassNum)
        {
            //select items at a 1m interval min
            double LastVal = 9999999;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Type == PassNum) && (ImageLoc.Altitude <= LastVal - 1))
                {
                    LastVal = ImageLoc.Altitude;
                    ImageLoc.selected = true;
                }
            }
            return ImageLocationList;
        }
        private double FindHubHeight(List<ImageLocationAndExtraInfo> ImageLocationList)
        {

            double MaxHubHeight = 0;
            double HubHeight = 0;

            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {

                if (ImageLoc.Altitude > MaxHubHeight) { MaxHubHeight = ImageLoc.Altitude; }
            }
            int cnt = 0;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {

                if (ImageLoc.Altitude > MaxHubHeight - 2)
                {
                    HubHeight = HubHeight + ImageLoc.Altitude;
                    cnt++;
                }
            }
            HubHeight = HubHeight / cnt;
            return HubHeight;

        }

        private double FindTipHeight(List<ImageLocationAndExtraInfo> ImageLocationList)
        {

            double lowestTip = 10;

            double MinTipHeight = 100000;
            double TipHeight = 0;

            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Altitude < MinTipHeight) && (ImageLoc.Altitude > lowestTip) && (ImageLoc.VertVelocity > -.1) && (ImageLoc.VertVelocity < .1)) { MinTipHeight = ImageLoc.Altitude; }
            }
            int cnt = 0;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {

                if (ImageLoc.Altitude < MinTipHeight + 2)
                {
                    TipHeight = TipHeight + ImageLoc.Altitude;
                    cnt++;
                }
            }
            if (cnt < 10)
            {
                Console.WriteLine("Tip Height Selection probably not made well");
            }
            TipHeight = TipHeight / cnt;
            return TipHeight;
        }

        private void CropImages(string PathToDir, ProgressBar PB)
        {

            DateTime FuncStartTime = DateTime.Now;
            string[] ImageFiles = Directory.GetFiles(PathToDir, "*.JPG");
            int cnt = 0;

            foreach (string ImageFile in ImageFiles)
            {
                FuncStartTime = DateTime.Now;
                try
                {
                    AppendLogTextBox("\nCropping " + Path.GetFileName(ImageFile));
                    //AppendLogTextBox("\nMemory Before \t:" + GC.GetTotalMemory(true).ToString());
                    using (Cropper Crop = new Cropper())
                    {
                        BladeCroppingSettings CropSettings = new BladeCroppingSettings(0, 0);
                        Crop.process(CropSettings, ImageFile, ImageFile);
                    }
                    GC.Collect();
                    //AppendLogTextBox("\nMemory After \t:" + GC.GetTotalMemory(true).ToString());
                }
                catch (Exception e)
                {
                    AppendLogTextBox(e.ToString());

                }
                cnt++;
                setAProgbar(PB, ImageFiles.Length, cnt);
                AppendLogTextBox("\nTook " + (DateTime.Now - FuncStartTime).TotalMilliseconds.ToString() + " milliseconds\n\n");
            }

        }
        private void BUT_GET_TRIG_OFFSETS_Click(object sender, EventArgs e)
        {
            for (int i = 1; i < ATable.Table.RowCount; i++)
            {
                string dirPictures = ATable.Table.GetControlFromPosition(0, i).Text;
                string logFilePath = Path.Combine(dirPictures, ATable.Table.GetControlFromPosition(1, i).Text);


                if (!File.Exists(logFilePath))
                {
                    MessageBox.Show("tlog file " + logFilePath + " does not exist ");
                    return;
                }
                if (!Directory.Exists(dirPictures))
                {
                    MessageBox.Show("Image directory " + dirPictures + " does not exist ");
                    return;
                }
                AppendLogTextBox("\n\nRow " + i.ToString());

                DateTime imsettime = GetFirstSustainedImageTime(dirPictures);
                DateTime tgtime = GetFirstSustainedTriggerTime(logFilePath);

                AppendLogTextBox("\nImage time : " + imsettime.ToString());
                AppendLogTextBox("\nTrigger time : " + tgtime.ToString());

                float trig2im = (float)(imsettime - tgtime).TotalSeconds;
                try
                {
                    ATable.Table.GetControlFromPosition(2, i).Text = trig2im.ToString();
                }
                catch
                {


                }

            }

        }
        public void setAProgbar(ProgressBar PB, int max, int val, int timeout = 0)
        {
            timeout++;
            if (timeout > 3)
            {
                return;
            }

            if (InvokeRequired)
            {
                this.Invoke(new Action<ProgressBar, int, int, int>(setAProgbar), new object[] { PB, max, val, timeout });
                return;
            }
            PB.Maximum = max;
            PB.Value = val;
        }
        public void AddATabToMain(TabPage TP, TabControl TC, int timeout = 0)
        {
            timeout++;
            if (timeout > 3)
            {
                return;
            }

            if (InvokeRequired)
            {
                this.Invoke(new Action<TabPage, TabControl, int>(AddATabToMain), new object[] { TP, TC, timeout });
                return;
            }
            TC.TabPages.Add(TP);
        }

        public void AppendLogTextBox(string value, int timeout = 0)
        {
            timeout++;
            if (timeout > 3)
            {
                return;
            }
            if (InvokeRequired)
            {
                this.Invoke(new Action<string, int>(AppendLogTextBox), new object[] { value, timeout });
                return;
            }
            TXT_outputlog.AppendText(value);
            TXT_outputlog.SelectionStart = TXT_outputlog.Text.Length;
            TXT_outputlog.ScrollToCaret();
        }
        public void EnableDisableButton(Button ButtonIn, bool TF , Color tmpColor , string text, int  timeout = 0)
        {
            timeout++;
            if (timeout > 3)
            {
                return;
            }
            if (InvokeRequired)
            {
                this.Invoke(new Action<Button, bool,Color,string , int>(EnableDisableButton), new object[] { ButtonIn,TF,tmpColor,text, timeout });
                return;
            }
            ButtonIn.Enabled = TF;
            ButtonIn.BackColor = tmpColor;
            ButtonIn.Text = text;
        }
        public int GetMainTabIndex(int timeout = 0)
        {
            timeout++;
            if (timeout > 3)
            {
                return 0;
            }
            if (InvokeRequired)
            {
                return (int)this.Invoke( new Func<int>(() => GetMainTabIndex(timeout)));
            }
            int index = MAIN_TAB_CONTROL.SelectedIndex;
            return index;
        }

        public void removeTurbineTabfrom(String ImageFolder)
        {

            TabPage tmpTabPage = MainTabs[ImageFolder];
            MAIN_TAB_CONTROL.TabPages.Remove(tmpTabPage);


        }

        private void TIMER_THREAD_CHECKER_Tick(object sender, EventArgs e)
        {
            TIMER_THREAD_CHECKER.Enabled = false;
            TIMER_THREAD_CHECKER.Interval = 2000;
            if (PreProcessThread == null)
            {
                PreProcessThread = new Thread(DefaultFunction);
            }
            if (PostProcessThread == null)
            {
                PostProcessThread = new Thread(DefaultFunction);
            }

            //check threads
            if (PreThreads.Count > 0) // priotise the prprocessing threads
            {
                
                if ((!PreProcessThread.IsAlive) && (!PostProcessThread.IsAlive))
                    { //check if thread is live or we need a flag here
                        PreProcessThread = PreThreads[0];
                        GC.Collect(); //garbage collection
                        PreProcessThread.Start(); //start new background thread
                        PreThreads.RemoveAt(0);
                    }
            }
            else if (PostThreads.Count > 0)
            {

                if ((!PreProcessThread.IsAlive) && (!PostProcessThread.IsAlive)) //check if thread is alive
                {
                    PostProcessThread = PostThreads[0]; //set thread to new thread
                    GC.Collect(); //garbage collection
                    PostProcessThread.Start(); //start new background thread
                    PostThreads.RemoveAt(0);

                }

            }

            TIMER_THREAD_CHECKER.Enabled = true;
            TIMER_THREAD_CHECKER.Start();
        }

        public void AddPreProcessToQue(object sender,EventArgs e) { 
            //add preprocessing to que

            TableLayoutPanelCellPosition cellpos = this.ATable.Table.GetCellPosition((Control)sender);

            PreThreads.Add( new Thread(() =>TagARow(cellpos.Row)));
            EnableDisableButton((Button)sender, false,Color.Yellow,"Processing");
        }
        public void ShowRowTurbineTab(object sender, EventArgs e)
        {
            TableLayoutPanelCellPosition cellpos = this.ATable.Table.GetCellPosition((Control)sender);
            string dirPictures = ATable.Table.GetControlFromPosition(0, cellpos.Row).Text;
            string logFilePath = Path.Combine(dirPictures, ATable.Table.GetControlFromPosition(1, cellpos.Row).Text);
            ProgressBar CroppProgbar = (ProgressBar)ATable.Table.GetControlFromPosition(6, cellpos.Row);


            if (!MainTabs.ContainsKey(((Label)ATable.Table.GetControlFromPosition(0, cellpos.Row)).Text))
            {
                MainTabs.Add(((Label)ATable.Table.GetControlFromPosition(0, cellpos.Row)).Text, new TabPage("+"));
            }

            CreateNewBladeTab(dirPictures, CroppProgbar, cellpos.Row);

            EnableDisableButton((Button)sender,false,Color.Yellow,"Opened");

        }

         public void DefaultFunction(){}

         private void MAIN_TAB_CONTROL_TabIndexChanged(object sender, EventArgs e)
         {
            //not used
             
         }

         private void textBox1_TextChanged(object sender, EventArgs e)
         {

         }
           
            //private void BUT_CROP_Click(object sender, EventArgs e)
            //{
            //    Thread thread = new Thread(CroppingThread);
            //    thread.Start();

            //    return;
            //}

            //private void CroppingThread()
            //{
            //    AppendLogTextBox("\n\nStarting cropping thread");
            //    if (ATable.Table.RowCount == 1)
            //    {
            //        AppendLogTextBox("\n  WARNING : NO BOXES SELECTED. ");
            //    }
            //    for (int i = 1; i < ATable.Table.RowCount; i++)
            //    {
            //        try
            //        {
            //            CheckBox tmp = (CheckBox)ATable.Table.GetControlFromPosition(5, i);
            //            if (tmp.Checked) CropARow(i);
            //        }
            //        catch (Exception e)
            //        {
            //            AppendLogTextBox("\n\nFAILED with exemption:\n" + e.ToString());
            //            return;
            //        }

            //    }
            //    AppendLogTextBox("\n\n cropping thread complete");
            //}

            //private void CropARow(int row)
            //{

            //    string dirPicturesselected = ATable.Table.GetControlFromPosition(0, row).Text+"\\geotagged\\selected";
            //    string logFilePath = Path.Combine(dirPicturesselected, ATable.Table.GetControlFromPosition(1, row).Text);
            //    ProgressBar CroppProgbar = (ProgressBar)ATable.Table.GetControlFromPosition(6, row);

            //    if (!Directory.Exists(dirPicturesselected))
            //    {
            //        MessageBox.Show("Image directory " + dirPicturesselected + " does not exist ");
            //        return;
            //    }

            //    CropImages(dirPicturesselected, CroppProgbar);

            //}
        }

        public enum ImageLocationType { Pass1, Pass2, Pass3, Pass4, Pass5, Tip, Hub, Ground, Other, High, Low, Default }


        public class ImageBladeGroup
        {
            public List<ImageLocationAndExtraInfo> FullImageList;
            public double GPStimeOffset = 0;
            public string BaseDirectory = "";
            public string tlogFileName = "";

            public ImageBladeGroup()
            {
            }

        }
        public class ImageLocationAndExtraInfo
        {
            public string PathToOrigionalImage = "";
            public string PathToSmallImage = "";
            public string PathToGreyImage = "";
            public string PathToGeoTaggedImage = "";
            public string PathToDestination = "";
            public double Latitude = 0;
            public double Longitude = 0;
            public double Altitude = 0;
            public double VertVelocity = 0;
            public DateTime Time = new DateTime();
            public ImageLocationType Type = ImageLocationType.Default;
            public bool selected = false;
            public int LeftCrop = 0;
            public int RightCrop = 0;

            public ImageLocationAndExtraInfo()
            {
            }

        }
        public class ImageGroupTableInfo
        {
            public TableLayoutPanel Table = new TableLayoutPanel();
            private ITGeotagger Parent;
            public ImageGroupTableInfo(ITGeotagger ITForm)
            {
                this.Parent = ITForm;
                this.Table.ColumnCount = 7;
                this.Table.RowCount = 1;
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
                this.Table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
                this.Table.Controls.Add(new Label() { Text = "Folder Path", Dock = DockStyle.Fill }, 0, 0);
                this.Table.Controls.Add(new Label() { Text = "Tlog", Dock = DockStyle.Fill }, 1, 0);
                this.Table.Controls.Add(new Label() { Text = "Offset", Dock = DockStyle.Fill }, 2, 0);
                this.Table.Controls.Add(new Label() { Text = "Pre-Procesing", Dock = DockStyle.Fill }, 3, 0);
                this.Table.Controls.Add(new Label() { Text = "Progress", Dock = DockStyle.Fill }, 4, 0);
                this.Table.Controls.Add(new Label() { Text = "Post-Procesing", Dock = DockStyle.Fill }, 5, 0);
                this.Table.Controls.Add(new Label() { Text = "Progress", Dock = DockStyle.Fill }, 6, 0);
                this.Table.AutoScroll = true;
            }
            public void AddRow(string jpegpath, string tlogpath, string offset)
            {
                CheckBox TmpCheck = new CheckBox() { Text = "", Dock = DockStyle.Fill, Anchor = ( AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft };
                ProgressBar ProgBar = new ProgressBar() { Text = "", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 };
                ProgressBar ProgBarCrop = new ProgressBar() { Text = "", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 };
                Button PreBUTT = new Button() { Text = "Pre-process", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top),Height=35 };
                Button PostBUTT = new Button() { Text = "Post-process", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top),Height = 35 };

                PreBUTT.Click += new EventHandler(Parent.AddPreProcessToQue);
                PostBUTT.Click += new EventHandler(Parent.ShowRowTurbineTab);

                PostBUTT.Enabled = false;

                this.Table.RowCount++;
                this.Table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
                this.Table.Controls.Add(new Label() { Text = jpegpath, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft, Height = 35 }, 0, this.Table.RowCount - 1);
                this.Table.Controls.Add(new Label() { Text = tlogpath, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft, Height = 35 }, 1, this.Table.RowCount - 1);
                this.Table.Controls.Add(new TextBox() { Text = offset, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 }, 2, this.Table.RowCount - 1);
                this.Table.Controls.Add(PreBUTT, 3, this.Table.RowCount - 1);
                this.Table.Controls.Add(ProgBar, 4, this.Table.RowCount - 1);
                this.Table.Controls.Add(PostBUTT, 5, this.Table.RowCount - 1);
                this.Table.Controls.Add(ProgBarCrop, 6, this.Table.RowCount - 1);

            }

        }
    }