using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class Asset
    {
        public string id            { get; set; }   // The unique ID of the asset
        public Dictionary<AssetAttributeType, string> attributes { get; set; }   // A hash list of attributes, unique based on asset type. Current allowed keys are addOns, bladeLength, height and version for wind turbine towers.
        public string dateOfInstall	{ get; set; }   // The date the asset was installed
        public GeoPoint location	{ get; set; }   // A GeoPoint object indicating GPS location of the asset
        public string make          { get; set; }   // The make of the asset
        public string model	        { get; set; }   // The model of the asset
        public string name	        { get; set; }   // The name of the asset which is meaningful to the owner
        public string serialNumber	{ get; set; }   // The serial number of the asset
        public string siteId	    { get; set; }   // The unique ID of the site at which the asset is located
        public string type          { get; set; }   // The type of asset. Currently either Solar_Panel or Wind_Turbine_Tower
    }
}
