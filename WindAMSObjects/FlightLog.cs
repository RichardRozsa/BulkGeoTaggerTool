using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class FlightLog
    {
        public string flightLogId { get; set; }
        public string orderNumber { get; set; }
        public string assetName { get; set;}
        public string assetId { get; set; }
        public string componentName { get; set; }
        public string componentId { get; set; }
        public GeoPoint location { get; set; }
        public string inspectionStartTime { get; set; } //formatted date time string 
        public string inspectionEndTime { get; set; } //formatted date time string 
        public string tlogName { get; set; }
        public string tlogResourceId { get; set; }
        public List<FlightLogData> FlightLogExtendedData { get; set; } //includes (coments,copterId,weather.....) i'll define an enum object for the types
    }

    class FlightLogData
    {
        public string type { get; set; } //includes (coments,copterId,weather.....) i'll define an enum object for the types
        public string data { get; set; } // enter value of data here
    }
}
