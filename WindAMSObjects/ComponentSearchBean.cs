using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class ComponentSearchBean
    {
        public string assetId       { get; set; }   // Search by the given unique ID of an Asset
        public string componentId	{ get; set; }   // Search by the given unique ID of a Component
        public string siteId	    { get; set; }   // Search by the given unique ID of a Site
        public string componentType { get; set; }   // The type of component. Valid values are BladeA, BladeB, BladeC, BladeD, BladeE or Gearbox
    }
}
