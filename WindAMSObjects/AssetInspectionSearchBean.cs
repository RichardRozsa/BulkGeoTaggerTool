using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class AssetInspectionSearchBean
    {
        public string assetId	    { get; set; }   //Unique ID of the asset to query inspections for
        public string orderNumber	{ get; set; }   //Unique order number of the work order to search for
        public string siteId	    { get; set; }   //Unique ID of the site to query inspections for
        public string status 	    { get; set; }   //The status of inspections needed
    }
}
