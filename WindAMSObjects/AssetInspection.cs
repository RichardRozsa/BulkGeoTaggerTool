using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class AssetInspection
    {
        public string id	            { get; set; }   // The unique ID of the asset inspection
        public string assetId	        { get; set; }   // The unique ID of the asset inspected
        public Dictionary<AssetInspectionAttributeType, string> attributes { get; set; }   // A hash list of attributes, unique based on asset type and/or inspection type. Current allowed keys are runHours for Wind Turbine Towers and totalProduction for all.
        public string orderNumber	    { get; set; }   // The unique ID of the work order for the inspection
        public string processedBy       { get; set; }   // Inspector name ID etc...
        public string dateOfInspection  { get; set; }   // The date the asset was inspected
        public List<string> resources	{ get; set; }   // An array of unique IDs of multimedia resources associated with the inspection
        public string type	            { get; set; }   // The scope of the asset inspection. Currently AnnualInspection.
        public string siteId            { get; set; }   // The unique ID of the site where the asset is located
        public string status	        { get; set; }   // The status of the asset inspection. Currently In_Process, Uplaoded, Processed, and Released.
    }
}
