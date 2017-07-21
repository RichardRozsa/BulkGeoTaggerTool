using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger
{
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
}
