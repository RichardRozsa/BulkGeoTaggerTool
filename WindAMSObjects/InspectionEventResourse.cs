using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class InspectionEventResourse
    {
        public string id { get; set; }   //	Unique ID of the inspection event resource.
        public string inspectionEventId { get; set; }   //The unique ID of the inspection event to which the resource is related.
        public string resourceId { get; set; }   //	The unique ID of the multimedia object (image) which show the area of the event.
        public string polygons { get; set; }   //	A list of InspectionEventPolygon objects which enumerates the shapes of actual event areas on the image.
        public string assetId { get; set; }   //The unique ID of the asset inspected
        public string orderNumber { get; set; }   //The unique ID of the work order for the inspection
        public string siteId { get; set; }   //	The unique ID of the site where the asset is located

    }
}
