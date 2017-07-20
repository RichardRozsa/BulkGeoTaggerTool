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
using MissionPlanner;
using MissionPlanner.Log;
using System.Threading;
using System.Threading.Tasks;
using ITGeoTagger;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util.TypeEnum;
using Emgu.CV.Shape;
using ITGeoTagger.WindAMSObjects;


namespace ITGeoTagger
{
    public partial class ITGeotagger : Form
    {
        private enum PROCESSING_MODE
        {
            TIME_OFFSET,
            CAM_MSG
        }

        public string Myprocessor ="";

        public List<Thread> PostThreads = new List<Thread>();
        List<Thread> PreThreads = new List<Thread>();
        int PostThreadReleaseBusyCount = 0;
        int PostThreadReleaseBusyMAX = 2;
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

        public Dictionary<string, TabPage> MainTabs = new Dictionary<string, TabPage>();

        public Thread PreProcessThread;
        public Thread PostProcessThread;

        private float VerticalImageDistribution = 2;
        private float cameraShutterLag = (float)1.5;

        public ITConfigFile appSavedData;

        public ITGeotagger()
        {
            InitializeComponent();
            appSavedData = new ITConfigFile();
            appSavedData.LoadFile();
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

        private async void BUT_GET_DIR_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                AppendLogTextBox("\nSearching for image sets\n");
                TXT_BROWSE_FOLDER.Text = folderBrowserDialog1.SelectedPath;
                List<string> ImageSet = GetAllImageDirs(TXT_BROWSE_FOLDER.Text);

                List<string> TlogImageDirs = Dirsfiltertlog(ImageSet);
                AppendLogTextBox("\nFound " + TlogImageDirs.Count.ToString() + " image sets\n");

                if (TlogImageDirs.Count > 10) {
                    AppendLogTextBox("\nThis process should take at most " + (((TlogImageDirs.Count*4)/60)+1).ToString("G2") + " minutes\n");
                }

                AppendLogTextBox("\nImporting all found image sets\n");
                foreach (string folder in TlogImageDirs)
                {
                    //check if we have a processing file here to import quicker
                    string progFile = Path.Combine(folder, "Processed.xml");
                    if (File.Exists(progFile))
                    {
                        ImageBladeGroup LastSavedData = new ImageBladeGroup();
                        System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(ImageBladeGroup));
                        System.IO.StreamReader file = new System.IO.StreamReader(progFile);
                        LastSavedData = (ImageBladeGroup)reader.Deserialize(file);
                        if ((LastSavedData.Blade == "") || (LastSavedData.AssetName == "") || (LastSavedData.SiteName == "") || (LastSavedData.Latitude == ""))
                        {
                            Dictionary<string, string> InfoData = new Dictionary<string, string>();
                            string infoFile = Path.Combine(folder, "ExtraInfo.txt");
                            if(File.Exists(infoFile)){
                                string[] lines = System.IO.File.ReadAllLines(infoFile);
                                foreach (string line in lines)
                                {
                                    string[] parts = line.Split(':');
                                    if (parts.Length > 1)
                                    {
                                        parts[1] = parts[1].Trim();
                                        parts[0] = parts[0].Trim();
                                        InfoData.Add(parts[0], parts[1]);
                                    }
                                }
                                LastSavedData.AssetName = InfoData["Turbine"];
                                LastSavedData.Blade = InfoData["Blade"];
                                LastSavedData.SiteName = InfoData["Site name"];
                                LastSavedData.Latitude = InfoData["Latitude"];
                                LastSavedData.Longitude = InfoData["Longitude"];
                            }
                        }
                        file.Close();
                        ATable.AddRow(folder, GetTlogInDir(folder), LastSavedData.GPStimeOffset.ToString("G4"), LastSavedData.FullImageList.Count);
                    }
                    else
                    {
                        float tmpTrigTime = await GetImagetoTriggerOffset(folder, GetTlogInDir(folder));
                        ATable.AddRow(folder, GetTlogInDir(folder), tmpTrigTime.ToString("G4"), Directory.GetFiles(folder,"*.JPG").Length);
                    }
                }
            }
        }

      private async Task<float> GetImagetoTriggerOffset(string dirPictures, string logFile)
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

            DateTime imsettime = await GetFirstSustainedImageTime(dirPictures);
            DateTime tmptgtime = await GetFirstSustainedTriggerTime(logFilePath);
            DateTime tgtime = tmptgtime.AddSeconds(this.cameraShutterLag);
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


        private void GenerateNewLocations(Dictionary<string, PictureInformation> listPhotosWithInfo, string dirWithImages,float offset)
        {

                CoordinateCollection coords = new CoordinateCollection();

                foreach (var item in vehicleLocations.Values)
                {
                    if (item != null)
                        coords.Add(new SharpKml.Base.Vector(item.Lat, item.Lon, item.AltAMSL));
                }

                var ls = new LineString() { Coordinates = coords, AltitudeMode = AltitudeMode.Absolute };

                SharpKml.Dom.Placemark pm = new SharpKml.Dom.Placemark() { Geometry = ls, Name = "path" };

                //kml.AddFeature(pm);

                foreach (PictureInformation picInfo in listPhotosWithInfo.Values)
                {
                    string filename = Path.GetFileName(picInfo.Path);
                    string filenameWithoutExt = Path.GetFileNameWithoutExtension(picInfo.Path);

                    SharpKml.Dom.Timestamp tstamp = new SharpKml.Dom.Timestamp();

                    tstamp.When = picInfo.Time;
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

        private int compareFileByPhotoTime(string x, string y)// ToDo rewrite this to not open file each time
        {
            if (getPhotoTime(x) < getPhotoTime(y)) return -1;
            if (getPhotoTime(x) > getPhotoTime(y)) return 1;
            return 0;
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

        public async Task<ImageBladeGroup> GeotagimagesCropandUpload(ImageBladeGroup ImageGroup, ProgressBar PBar,int row,string workOrderNumber,string processedBy)
        {
            try
            {
                this.PostThreadReleaseBusyCount = this.PostThreadReleaseBusyCount+1;

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
                    int sequence = 0;
                    ImageLocationType LastType = ImageLocationType.Pass1; 
                    WindamsController WC = new WindamsController(appSavedData.upload_URL);

                    bool workOrderExists = await WC.WorkOrderExists(workOrderNumber);
                    
                    if (!workOrderExists) {
                        MessageBox.Show("Work order not found, check the work order number or your internet connection");
                    }

                    foreach (ImageLocationAndExtraInfo ImageLocInfo in ImageGroup.FullImageList)
                    {
                        //check if we are in another tab. if we are pause tempoarily
                        while (GetMainTabIndex() != 0)
                        {
                            GC.Collect();
                            GC.WaitForFullGCComplete();
                            Thread.Sleep(1000);
                        }
                        
                        if (ImageLocInfo.selected)
                        {
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
                            try //uncomment to enable upload
                            {
                                if (workOrderExists)
                                {
                                    if (LastType != ImageLocInfo.Type)
                                    {
                                        sequence = 0;
                                    }
                                    int pass = 0;
                                    
                                    switch (ImageLocInfo.Type)
                                    {
                                        case ImageLocationType.Pass1:
                                            pass = 1;
                                            break;
                                        case ImageLocationType.Pass2:
                                            pass = 2;
                                            break;
                                        case ImageLocationType.Pass3:
                                            pass = 3;
                                            break;
                                        case ImageLocationType.Pass4:
                                            pass = 4;
                                            break;
                                        case ImageLocationType.Pass5:
                                            pass = 5;
                                            break;
                                    }
                                    bool UploadSuccess = await WC.UploadToWindAMS(workOrderNumber, ImageGroup.SiteName, ImageGroup.AssetName, ImageGroup.Blade, processedBy, pass, sequence, ImageLocInfo, ImageGroup);
                                    if (UploadSuccess) AppendLogTextBox("\nUploaded\t" + ImageLocInfo.PathToOrigionalImage);
                                    else AppendLogTextBox("\n Image upload failed");
                                    sequence = sequence + 1;
                                    LastType = ImageLocInfo.Type;
                                }
                            }
                            catch (Exception e)
                            {
                                AppendLogTextBox("\nFAILED to Upload\t" + ImageLocInfo.PathToOrigionalImage);
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


                AppendLogTextBox("\nGeoTagging, Cropping and Upload thread finished \n");

                EnableDisableButton((Button)this.ATable.Table.GetControlFromPosition(6, row),false,Color.LimeGreen,"Complete");
                this.PostThreadReleaseBusyCount = this.PostThreadReleaseBusyCount - 1;
                return ImageGroup;
                
            }
            catch (Exception e)
            {
                this.PostThreadReleaseBusyCount = this.PostThreadReleaseBusyCount - 1;
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
                System.Windows.Forms.Application.DoEvents();

                using (Image Pic = Image.FromStream(ms))
                {
                    PropertyItem[] pi = Pic.PropertyItems;

                    bool NeedsUID = true;
                    foreach (PropertyItem item in pi)
                    {
                        if (item.Id == 0xA420)
                        {
                            NeedsUID = false;
                        }
                    }
                    if (NeedsUID)
                    {
                        Guid UID = Guid.NewGuid();
                        pi[0].Id = 0xA420;
                        pi[0].Type = 2;
                        pi[0].Len = 37;
                        string UIDstring = UID.ToString()+"\0";
                        pi[0].Value = Encoding.ASCII.GetBytes(UIDstring);
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
            System.Diagnostics.Process.Start(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) +
                                             Path.DirectorySeparatorChar + "m3u" + Path.DirectorySeparatorChar +
                                             "GeoRefnetworklink.kml");
        }

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

        async private Task<DateTime> GetFirstSustainedImageTime(string ImageDir)
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
        async private Task<DateTime> GetFirstSustainedTriggerTime(string fn)
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

        async private void TagARow(int row)
        {
            string dirPictures = ATable.Table.GetControlFromPosition(0, row).Text;
            string logFilePath = Path.Combine(dirPictures, ATable.Table.GetControlFromPosition(1, row).Text);
            ProgressBar Progbar = (ProgressBar)ATable.Table.GetControlFromPosition(5, row);
            ProgressBar CroppProgbar = (ProgressBar)ATable.Table.GetControlFromPosition(7, row);

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
                float.TryParse(ATable.Table.GetControlFromPosition(3, row).Text, NumberStyles.Float, CultureInfo.InvariantCulture, out secondsOffset) ==
                false)
            {
                AppendLogTextBox("\nOffset number not in correct format. Use . as decimal separator\n");
                return;
            }

            string ProgFile = Path.Combine(dirPictures, "Processed.xml");
            if (File.Exists(ProgFile))
            {
                try
                {
                        ImageBladeGroup LastSavedData = new ImageBladeGroup();
                        System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(ImageBladeGroup));
                        System.IO.StreamReader file = new System.IO.StreamReader(ProgFile);
                        LastSavedData = (ImageBladeGroup)reader.Deserialize(file);
                        file.Close();

                        //need to add some checks here to make sure that the file 

                        if (LastSavedData.GPStimeOffset.ToString("G4") == secondsOffset.ToString("G4"))
                        { //shortcut if we already have all the needed data
                            if (LastSavedData.BaseDirectory == dirPictures)
                            {


                            }
                            else {

                                ImageBladeGroup NewSavedData = new ImageBladeGroup();
                                NewSavedData.GPStimeOffset = LastSavedData.GPStimeOffset;
                                NewSavedData.BaseDirectory = dirPictures;
                                NewSavedData.FullImageList = LastSavedData.FullImageList;
                                NewSavedData.tlogFileName = logFilePath;


                                foreach (ImageLocationAndExtraInfo imginfo in NewSavedData.FullImageList)
                                {
                                    imginfo.PathToOrigionalImage = imginfo.PathToOrigionalImage.Replace(LastSavedData.BaseDirectory, NewSavedData.BaseDirectory);
                                    imginfo.PathToSmallImage = imginfo.PathToSmallImage.Replace(LastSavedData.BaseDirectory, NewSavedData.BaseDirectory);
                                    imginfo.PathToGreyImage = imginfo.PathToGreyImage.Replace(LastSavedData.BaseDirectory, NewSavedData.BaseDirectory);
                                    imginfo.PathToGeoTaggedImage = imginfo.PathToGeoTaggedImage.Replace(LastSavedData.BaseDirectory, NewSavedData.BaseDirectory);
                                    imginfo.PathToDestination = imginfo.PathToDestination.Replace(LastSavedData.BaseDirectory, NewSavedData.BaseDirectory);
                                }
                                System.Xml.Serialization.XmlSerializer Progsavewriter = new System.Xml.Serialization.XmlSerializer(typeof(ImageBladeGroup));
                                System.IO.FileStream wfile = System.IO.File.Create(ProgFile);
                                Progsavewriter.Serialize(wfile, NewSavedData);
                                wfile.Close();
                            
                            }
                            //note to processor
                            AppendLogTextBox("\n\nUsing last saved processing data for " + LastSavedData.BaseDirectory);

                            //fill the progress bar
                            setAProgbar(Progbar, LastSavedData.FullImageList.Count, LastSavedData.FullImageList.Count);
                            //enable post processing button
                            EnableDisableButton((Button)ATable.Table.GetControlFromPosition(6, row), true, Color.Yellow, "Ready");
                            EnableDisableButton((Button)ATable.Table.GetControlFromPosition(4, row), true, Color.LimeGreen, "Complete");
                            return;
                        }
                    }
                    catch(Exception e){

                        AppendLogTextBox("\n\nFailed to load last saved data");

                    }
            }



            try
            {
                picturesInfo = doworkGPSOFFSET(logFilePath, dirPictures, secondsOffset);
                if (picturesInfo != null) GenerateNewLocations(picturesInfo, dirPictures, secondsOffset);

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
                    if (!File.Exists(IM_LOC.PathToGreyImage))
                    {
                        ImageLocationAndExtraInfo tmpInfo = await SaveGrayedOutImage(IM_LOC, IM_LOC.LeftCrop, IM_LOC.RightCrop,IM_LOC.brightnessCorrection);
                        IM_LOC.PathToGreyImage = tmpInfo.PathToGreyImage;
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

                    ProgFile = Path.Combine(RowImagesCollection.BaseDirectory, "Processed.xml");
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
                EnableDisableButton((Button)ATable.Table.GetControlFromPosition(6, row),true,Color.Yellow,"Ready");
                EnableDisableButton((Button)ATable.Table.GetControlFromPosition(4, row), true,Color.LimeGreen,"Complete");
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

            if (!MAIN_TAB_CONTROL.TabPages.Contains(MainTabs[BaseDir]))
            {
                MAIN_TAB_CONTROL.TabPages.Add(MainTabs[BaseDir]);

                TurbineTab TurbTab = new TurbineTab(BaseDir, PB, this, row);

                TurbTab.populatePassImages();

                MainTabs[BaseDir].Text = TurbTab.ImageGroup.AssetName + " " + TurbTab.ImageGroup.Blade;

                TurbTab.Parent = MainTabs[BaseDir];
                TurbTab.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
                TurbTab.Dock = DockStyle.Fill;
            }

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
        private List<ImageLocationAndExtraInfo> GetListOfPass(List<ImageLocationAndExtraInfo> ImageList, ImageLocationType passToSort) {
            List<ImageLocationAndExtraInfo> FilteredList = new List<ImageLocationAndExtraInfo>();
            foreach (ImageLocationAndExtraInfo image in ImageList)
            {
                if (image.Type == passToSort)
                {
                    FilteredList.Add(image);
                }
            }
            return FilteredList;
        }

        private List<ImageLocationAndExtraInfo> MiddleOutSelect(List<ImageLocationAndExtraInfo> ImageList, ImageLocationType passToSort)
        {
            List<ImageLocationAndExtraInfo> FilteredList = GetListOfPass(ImageList, passToSort);

            //split list in middle
            List<ImageLocationAndExtraInfo> FilteredListFirstHalf = FilteredList.GetRange(0, FilteredList.Count / 2);
            FilteredListFirstHalf.Reverse();
            List<ImageLocationAndExtraInfo> FilteredListSecondHalf = FilteredList.GetRange((FilteredList.Count / 2) - 1, FilteredList.Count / 2); ;


            if ((passToSort == ImageLocationType.Pass1) || (passToSort == ImageLocationType.Pass3))
            {
                //Filter first half in reverse down
                double LastVal = 99999999;
                ImageLocationAndExtraInfo LastSelected = FilteredListFirstHalf.First();
                ImageLocationAndExtraInfo tmpLastValue = FilteredListFirstHalf.First();
                foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListFirstHalf)
                {
                    // select last hub shot
                    if ((ImageLoc.Type == passToSort) && (ImageLoc.Altitude <= LastVal - this.VerticalImageDistribution))
                    {
                        LastSelected = ImageLoc;
                        LastVal = tmpLastValue.Altitude;
                        tmpLastValue.selected = true;
                    }
                    tmpLastValue = ImageLoc;
                }
                FilteredListFirstHalf.Last().selected = true;
                //filter second half forward up
                LastVal = 0;
                tmpLastValue = FilteredListSecondHalf.First();
                foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListSecondHalf)
                {
                    if ((ImageLoc.Type == passToSort) && (ImageLoc.Altitude >= LastVal + this.VerticalImageDistribution))
                    {
                        LastSelected = ImageLoc;
                        LastVal = tmpLastValue.Altitude;

                        tmpLastValue.selected = true;
                    }
                    tmpLastValue = ImageLoc;
                }
                LastSelected.selected = true;
            }
            else if ((passToSort == ImageLocationType.Pass2) || (passToSort == ImageLocationType.Pass4))
            {
                //Filter first half in reverse up
                double LastVal = 0;
                ImageLocationAndExtraInfo LastSelected = FilteredListFirstHalf.First();
                ImageLocationAndExtraInfo tmpLastValue = FilteredListFirstHalf.First();
                foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListFirstHalf)
                {
                    if ((ImageLoc.Type == passToSort) && (ImageLoc.Altitude >= LastVal + this.VerticalImageDistribution))
                    {
                        LastSelected = ImageLoc;
                        LastVal = tmpLastValue.Altitude;
                        tmpLastValue.selected = true;
                    }
                    tmpLastValue = ImageLoc;
                }
                LastSelected.selected = true;
                //filter second half forward down
                LastVal = 99999999;
                tmpLastValue = FilteredListSecondHalf.First();
                foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListSecondHalf)
                {
                    // select last hub shot
                    if ((ImageLoc.Type == passToSort) && (ImageLoc.Altitude <= LastVal - this.VerticalImageDistribution))
                    {
                        LastSelected = ImageLoc;
                        LastVal = tmpLastValue.Altitude;
                        tmpLastValue.selected = true;
                    }
                    tmpLastValue = ImageLoc;
                }
                LastSelected.selected = true;
            }

            //for each element in the two half lists update the main list
            foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListFirstHalf)
            {
                if (ImageLoc.selected) ImageList.Find(x => x.PathToOrigionalImage == ImageLoc.PathToOrigionalImage).selected = true;
            }
            foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListSecondHalf)
            {
                if (ImageLoc.selected) ImageList.Find(x => x.PathToOrigionalImage == ImageLoc.PathToOrigionalImage).selected = true;
            }


            return ImageList;

        }

        private ImageBladeGroup SortImagesByPasses(ImageBladeGroup ImageGroup)
        {

            ImageGroup.FullImageList = SortImagesByType(ImageGroup.FullImageList);

            ImageGroup.FullImageList = MiddleOutSelect(ImageGroup.FullImageList, ImageLocationType.Pass1);
            ImageGroup.FullImageList = MiddleOutSelect(ImageGroup.FullImageList, ImageLocationType.Pass2);
            ImageGroup.FullImageList = MiddleOutSelect(ImageGroup.FullImageList, ImageLocationType.Pass3);
            ImageGroup.FullImageList = MiddleOutSelect(ImageGroup.FullImageList, ImageLocationType.Pass4);
            ImageGroup.FullImageList = GetTipImages(ImageGroup.FullImageList);

            return ImageGroup;
        }


        private List<ImageLocationAndExtraInfo> SortImagesByType(List<ImageLocationAndExtraInfo> ImageLocationList)
        {
            double hubHeight = FindHubHeight(ImageLocationList);
            double tipHeight = FindTipHeight(ImageLocationList);

            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Altitude < 10)) { ImageLoc.Type = ImageLocationType.Ground; }
                else if (ImageLoc.Altitude > hubHeight - 8) { ImageLoc.Type = ImageLocationType.High; }
                else if ((ImageLoc.Altitude > 10) && (ImageLoc.Altitude < tipHeight + 8)) { ImageLoc.Type = ImageLocationType.Low; }
                
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
                if (ImageLoc.Type != ImageLocationType.Ground)
                {
                    ImageLoc.Type = tempType;
                }
            }

            return ImageLocationList;
        }

        private List<ImageLocationAndExtraInfo> GetTipImages(List<ImageLocationAndExtraInfo> ImageLocationList) {

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

            List<ImageLocationAndExtraInfo> tmpTipPhotos = new List<ImageLocationAndExtraInfo>();

            double T_Height = sum / CNT;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if (((ImageLoc.Type == ImageLocationType.Pass2) || (ImageLoc.Type == ImageLocationType.Pass3)) && (ImageLoc.selected==false))
                {
                    if (ImageLoc.Altitude < T_Height + .3)
                    {
                        ImageLoc.Type = ImageLocationType.Pass5;
                        tmpTipPhotos.Add(ImageLoc);
                    }
                }
            }
            int totalTipCount = tmpTipPhotos.Count;
            
            //select best guess 5
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[2].PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[totalTipCount / 4].PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[totalTipCount/2].PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[(3*totalTipCount)/4].PathToOrigionalImage).selected = true;
            if (totalTipCount > 5)
            {
                ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[totalTipCount-2].PathToOrigionalImage).selected = true;
            }
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos.First().PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos.First().PathToOrigionalImage).Type = ImageLocationType.Pass2;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos.Last().PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos.Last().PathToOrigionalImage).Type = ImageLocationType.Pass3;

            return ImageLocationList;
        }


        private List<ImageLocationAndExtraInfo> FilterPassGoingUP(List<ImageLocationAndExtraInfo> ImageLocationList, ImageLocationType PassNum)
        {
           
            //select items at a set vertical interval min

            double LastVal = 0;
            List<ImageLocationAndExtraInfo> tmpList = new List<ImageLocationAndExtraInfo>();
            ImageLocationAndExtraInfo tmpLastValue = ImageLocationList.First();
            bool firstTipSelected = false;
            bool LastTipSelected = false;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                // select last hub shot
                if ((ImageLoc.Type == PassNum) && (!firstTipSelected))
                {
                    firstTipSelected = true;
                    LastVal = tmpLastValue.Altitude;
                }
                else if ((ImageLoc.Type == PassNum) && (firstTipSelected) && (!LastTipSelected))
                {
                    if (tmpLastValue.Altitude < LastVal) LastVal = tmpLastValue.Altitude;
                    if (ImageLoc.Altitude > LastVal + .4)
                    {

                        tmpLastValue.selected = true;
                        LastVal = tmpLastValue.Altitude;
                        LastTipSelected = true;
                    }
                }
                else if ((ImageLoc.Type == PassNum) && (ImageLoc.Altitude >= LastVal + this.VerticalImageDistribution))
                {
                    LastVal = tmpLastValue.Altitude;
                    tmpLastValue.selected = true;
                }

                tmpLastValue = ImageLoc;
            }
            return ImageLocationList;
        }
        private List<ImageLocationAndExtraInfo> FilterPassGoingDOWN(List<ImageLocationAndExtraInfo> ImageLocationList, ImageLocationType PassNum)
        {
            //select items at a set interval min
            double LastVal = 9999999;
            ImageLocationAndExtraInfo tmpLastValue = ImageLocationList.First();
            bool firstHubSelected = false;
            bool LastHubSelected = false;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                // select last hub shot
                if((ImageLoc.Type == PassNum)&&(!firstHubSelected)){
                    firstHubSelected = true;
                    LastVal = tmpLastValue.Altitude;
                }
                else if ((ImageLoc.Type == PassNum) && (firstHubSelected) && (!LastHubSelected)) {
                    if (tmpLastValue.Altitude > LastVal) LastVal = tmpLastValue.Altitude;
                    if (ImageLoc.Altitude < LastVal -.3) {

                        tmpLastValue.selected = true;

                        LastVal = tmpLastValue.Altitude;
                        LastHubSelected = true; 
                    }
                }
                else if ((ImageLoc.Type == PassNum) && (ImageLoc.Altitude <= LastVal - this.VerticalImageDistribution))
                {
                    LastVal = tmpLastValue.Altitude;
                    tmpLastValue.selected = true;
                }
                tmpLastValue = ImageLoc;
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
                if ((ImageLoc.Altitude < MinTipHeight) && (ImageLoc.Altitude > lowestTip) && (ImageLoc.VertVelocity > -.3) && (ImageLoc.VertVelocity < .3)) { MinTipHeight = ImageLoc.Altitude; }
            }
            int cnt = 0;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {

                if ((ImageLoc.Altitude < MinTipHeight + 2) && (ImageLoc.Altitude > lowestTip))
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
            TIMER_THREAD_CHECKER.Interval = 1000;
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

                if ((!PreProcessThread.IsAlive) && (!PostProcessThread.IsAlive)&&(this.PostThreadReleaseBusyCount < this.PostThreadReleaseBusyMAX)) //check if thread is alive
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
            ProgressBar CroppProgbar = (ProgressBar)ATable.Table.GetControlFromPosition(7, cellpos.Row);
            
            EnableDisableButton((Button)sender, false , Color.Yellow, "Opened");
            Thread AddTabThread = new Thread(() => ShowRowTurbineTabThread((Button)sender,cellpos.Row,CroppProgbar,dirPictures));
            AddTabThread.Start();
            
        }
        public void ShowRowTurbineTabThread(Button sender, int row, ProgressBar CroppProgbar, string dirPictures)
        {

            if (!MainTabs.ContainsKey(((Label)ATable.Table.GetControlFromPosition(0, row)).Text))
            {
                MainTabs.Add(dirPictures, new TabPage("+"));
            }
            CreateNewBladeTab(dirPictures, CroppProgbar, row);
        }

         public void DefaultFunction(){}

         private void MAIN_TAB_CONTROL_TabIndexChanged(object sender, EventArgs e)
         {
            //not used
             
         }
         public void RemoveTabFromMainTabControl(string baseDirectory) {

             TabPage tmpTabPage = MainTabs[baseDirectory];
             MAIN_TAB_CONTROL.TabPages.Remove(tmpTabPage);
             MainTabs.Remove(baseDirectory);

         }
         private void textBox1_TextChanged(object sender, EventArgs e)
         {

         }
         async public Task<ImageLocationAndExtraInfo> SaveGrayedOutImage(ImageLocationAndExtraInfo imageInfo, int LeftCrop, int RightCrop,int brightnessCorrection)
         {
             Image<Bgr, Byte> GrayedOutImage = new Image<Bgr, Byte>(imageInfo.PathToSmallImage);

             //transform to HSV
             Image<Hsv, Byte> HSVCropImage = GrayedOutImage.Convert<Hsv, Byte>();

             // check boundaries
             // for each pixel in the graayed out region set the saturation to 20
             // divide the value by 3 
                 //convert pixel to grayed out pixels
             if (brightnessCorrection != 0)
             {
                 for (int i = 0; i < GrayedOutImage.Width; i++)
                 {
                     for (int j = 0; j < HSVCropImage.Height; j++)
                     {
                         Byte val = HSVCropImage.Data[j, i, 2];
                         if ((int)val + brightnessCorrection<=0)
                         {
                            HSVCropImage.Data[j, i, 2] = 0;
                         }
                         else if ((int)val+brightnessCorrection>=255) {
                             HSVCropImage.Data[j, i, 2] = 255;
                         }

                         else
                         {
                             val = (Byte)((int)val + brightnessCorrection);
                             HSVCropImage.Data[j, i, 2] = val;
                         }
                     }
                 }
             }

             if ((LeftCrop > 0) && (LeftCrop < RightCrop))
             {
                 //convert pixel to grayed out pixels
                 for (int i = 0; i < LeftCrop; i++)
                 {
                     for (int j = 0; j < HSVCropImage.Height; j++)
                     {

                         HSVCropImage.Data[j, i, 1] = 20;

                         Byte val = HSVCropImage.Data[j, i, 2];
                         val = (Byte)((int)val / 3);
                         HSVCropImage.Data[j, i, 2] = val;
                     }
                 }
             }
             if ((RightCrop < GrayedOutImage.Width) && (LeftCrop < RightCrop))
             {
                 //convert pixel to grayed out pixels
                 for (int i = RightCrop; i < GrayedOutImage.Width; i++)
                 {
                     for (int j = 0; j < HSVCropImage.Height; j++)
                     {
                         GrayedOutImage.Data[j, i, 1] = 20;

                         Byte val = HSVCropImage.Data[j, i, 2];
                         val = (Byte)((int)val / 3);
                         HSVCropImage.Data[j, i, 2] = val;
                     }
                 }
             }


             //transform back to BGR
             GrayedOutImage = HSVCropImage.Convert<Bgr, Byte>();
             if (!Directory.Exists(Path.GetDirectoryName(imageInfo.PathToGreyImage)))
             {
                 Directory.CreateDirectory(Path.GetDirectoryName(imageInfo.PathToGreyImage));
             }

             CvInvoke.PutText(GrayedOutImage, imageInfo.Altitude.ToString("G4"), new System.Drawing.Point(GrayedOutImage.Width - 100, 35), FontFace.HersheyComplex, 1.0, new Bgr(0, 255, 0).MCvScalar);
             GrayedOutImage.Save(imageInfo.PathToGreyImage);
             GrayedOutImage.Dispose();
             return imageInfo;
         }

         private void button1_Click(object sender, EventArgs e) // exists to check functions
         {
             DateTime now = DateTime.Now;
             WindamsController WC = new WindamsController("http://testing.inspectools.net/webservices/");

             string baseDirectory = @"E:\ImageDataBase\Erieau\140\A\6-19-2017\Flight 1";
             string Filepath = Path.Combine(baseDirectory, "ExtraInfo.txt");
             string[] lines = System.IO.File.ReadAllLines(Filepath);

             Dictionary<string, string> InfoData = new Dictionary<string, string>();
             string formattedTime = WC.DateTimeToWindamsDateTimeString(now);
             foreach (string line in lines)
             {
                 string[] parts = line.Split(':');
                 if (parts.Length > 1) {
                     parts[1] = parts[1].Trim();
                     parts[0] = parts[0].Trim();
                     InfoData.Add(parts[0], parts[1]);
                 }

             }
             MessageBox.Show(formattedTime);
             MessageBox.Show("Key{Turbine}Value{" + InfoData["Turbine"] + "}");
             MessageBox.Show("Key{Blade}Value{" + InfoData["Blade"] + "}");
             MessageBox.Show("Key{Site name}Value{" + InfoData["Site name"] + "}");

          }
     }
     public enum ImageLocationType { Pass1, Pass2, Pass3, Pass4, Pass5, Tip, Hub, Ground, Other, High, Low, Default }


     public class ImageBladeGroup
        {
            public List<ImageLocationAndExtraInfo> FullImageList;
            public double GPStimeOffset = 0;
            public string BaseDirectory = "";
            public string tlogFileName = "";
            public string Blade = "";
            public string AssetName = "";
            public string SiteName = "";
            public string Latitude = "";
            public string Longitude = "";

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
            public int brightnessCorrection = 0;

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
                this.Table.ColumnCount = 8;
                this.Table.RowCount = 1;
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
                this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
                this.Table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
                this.Table.Controls.Add(new Label() { Text = "Folder Path", Dock = DockStyle.Fill }, 0, 0);
                this.Table.Controls.Add(new Label() { Text = "Tlog", Dock = DockStyle.Fill }, 1, 0);
                this.Table.Controls.Add(new Label() { Text = "Image Count", Dock = DockStyle.Fill }, 2, 0);
                this.Table.Controls.Add(new Label() { Text = "Offset", Dock = DockStyle.Fill }, 3, 0);
                this.Table.Controls.Add(new Label() { Text = "Pre-Procesing", Dock = DockStyle.Fill }, 4, 0);
                this.Table.Controls.Add(new Label() { Text = "Progress", Dock = DockStyle.Fill }, 5, 0);
                this.Table.Controls.Add(new Label() { Text = "Post-Procesing", Dock = DockStyle.Fill }, 6, 0);
                this.Table.Controls.Add(new Label() { Text = "Progress", Dock = DockStyle.Fill }, 7, 0);
                this.Table.AutoScroll = true;
            }
            
        async public void AddRow(string jpegpath, string tlogpath, string offset,int imageCount ,int timeout=0)
            {

            timeout++;
            if (timeout > 3)
            {
                Thread.Sleep(100);
                return;
            }

            if (this.Parent.InvokeRequired)
            {
                this.Parent.Invoke(new Action<string, string, string, int, int>(AddRow), new object[] { jpegpath, tlogpath, offset, imageCount,timeout });
                return;
            }

                CheckBox TmpCheck = new CheckBox() {Text = "", Dock = DockStyle.Fill, Anchor = ( AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft };
                ProgressBar ProgBar = new ProgressBar() {Text = "", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 };
                ProgressBar ProgBarCrop = new ProgressBar() {Text = "", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 };
                Button PreBUTT = new Button() {Text = "Pre-process", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top),Height=35 };
                Button PostBUTT = new Button() {Text = "Post-process", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top),Height = 35 };

                PreBUTT.Click += new EventHandler(Parent.AddPreProcessToQue);
                PostBUTT.Click += new EventHandler(Parent.ShowRowTurbineTab);

                PostBUTT.Enabled = false;

                this.Table.RowCount++;
                this.Table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
                this.Table.Controls.Add(new Label() { Text = jpegpath, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft, Height = 35 }, 0, this.Table.RowCount - 1);
                this.Table.Controls.Add(new Label() { Text = tlogpath, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft, Height = 35 }, 1, this.Table.RowCount - 1);
                this.Table.Controls.Add(new Label() { Text = imageCount.ToString(), Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft, Height = 35 }, 2, this.Table.RowCount - 1);
                this.Table.Controls.Add(new TextBox() { Text = offset, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 }, 3, this.Table.RowCount - 1);
                this.Table.Controls.Add(PreBUTT, 4, this.Table.RowCount - 1);
                this.Table.Controls.Add(ProgBar, 5, this.Table.RowCount - 1);
                this.Table.Controls.Add(PostBUTT, 6, this.Table.RowCount - 1);
                this.Table.Controls.Add(ProgBarCrop, 7, this.Table.RowCount - 1);
            }

        }
    }