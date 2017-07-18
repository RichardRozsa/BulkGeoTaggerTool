using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class InspectionEventPolygon
    {
        public string id { get; set; }   //The unique ID of the polygon
        public string center { get; set; }   //	The center of the polygon
        public string geometry { get; set; }   //A list of Point objects. Each point object has a x, y, and optionally a z value
        public string name { get; set; }   //A name for the polygon
        public string severity { get; set; }   //An integer value between 1 and 5 inclusive indicating severity
        public string text { get; set; }   //Text to be displayed over the polygon

    }
}
