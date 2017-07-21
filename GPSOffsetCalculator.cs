using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using MissionPlanner;

namespace ITGeoTagger
{
    public class GPSOffsetCalculator
    {
        ITGeotagger Parent_ITGeoTagger;
        public GPSOffsetCalculator(ITGeotagger parent) {
            Parent_ITGeoTagger = parent;
        }//constructor

        public  async Task<float> GetImagetoTriggerOffset(string dirPictures, string logFile)
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
            DateTime tgtime = tmptgtime.AddSeconds(this.Parent_ITGeoTagger.cameraShutterLag);
            //AppendLogTextBox("\n\nImage time : " + imsettime.ToString());
            //AppendLogTextBox("\nTrigger time : " + tgtime.ToString());

            float trig2im = (float)(imsettime - tgtime).TotalSeconds;
            return trig2im;
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
    }
}
