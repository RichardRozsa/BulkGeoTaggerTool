using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger
{
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
}
