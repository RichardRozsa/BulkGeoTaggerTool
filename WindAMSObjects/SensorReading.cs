using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class SensorReading
    {
        public string type      { get; set; }   // The type of sensor reading. Acceptable values are Barometric and RelativeAltitude. Other types may be added at a later time.
        public string startTime { get; set; }   // The time at which the taking of the reading began.
        public string endTime	{ get; set; }   // The time at which the taking of the reading ended.
        public string value	    { get; set; }   // The value of the reading as a double.
    }
}
