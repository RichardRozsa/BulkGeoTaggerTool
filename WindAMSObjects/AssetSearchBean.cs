using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class AssetSearchBean
    {
         public AssetType assetType    { get; set; }   //	The type of asset required
         public string name	        { get; set; }   //  The name given to the object which is meaningful to the owner
         public string siteId       { get; set; }   //	Unique ID of the site
    }
}
