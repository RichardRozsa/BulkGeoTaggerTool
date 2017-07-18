using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class GeoPoint
    {
        public double accuracy      { get; set; }   //	The accuracy of the reading as a double precision floating point number
        public double altitude      { get; set; }   //  The altitude of the reading as a double precision floating point number
        public double latitude      { get; set; }   //  The latitude of the reading as a double precision floating point number
        public double longitude     { get; set; }   //	The longitude of the reading as a double precision floating point number
    }
}
