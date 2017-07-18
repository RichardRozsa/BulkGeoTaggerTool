using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class Component
    {
        public string id            { get; set; }   // The unique ID of the component
        public string assetId	    { get; set; }   // Unique ID of the related Asset
        public Dictionary<ComponentAttributeType, string> attributes { get; set; }   // A hash list of attributes, unique based on component type. Current allowed keys are color and itemNumber for BladeA, BladeB, BladeC and BladeD.
        public string siteId	    { get; set; }   // Unique ID of the related Site
        public string type          { get; set; }   // The type of component. Valid values are BladeA, BladeB, BladeC, BladeD, BladeE or Gearbox
        public string make	        { get; set; }   // The make of the component
        public string model	        { get; set; }   // The model of the component
        public string serialNumber  { get; set; }   // The serial number of the asset
    }
}
