using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class AssetAdOn
    {
        public string id            { get; set; }   //	The unique ID of the add-on
        public string assetId       { get; set; }   //  The unique ID of the asset to which the item is added.
        public string componentId   { get; set; }   //	The unique ID of the component to which the item is added, or null.
        public string type          { get; set; }   //	The type of add add-on. Example add-ons for wind turbines are all related to components: Vortex Generators, Gurney Flaps, Stall Strips, Serrated Edges, Copper Cap, Copper Strip.

    }
}
