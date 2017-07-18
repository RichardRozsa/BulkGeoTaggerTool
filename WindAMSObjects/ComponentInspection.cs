using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class ComponentInspection
    {
        public string id	                                        { get; set; }   // Unique ID of the component inspection
        public string assetId                                       { get; set; }   // Unique ID of the related asset
        public string assetInspectionId                             { get; set; }   // Unique ID of the related asset inspection
        public string componentId                                   { get; set; }   // Unique ID of the related component
        public string inspectorUserId                               { get; set; }   // Unique ID of the user which did the inspection. In the case of drone inspections, this is the pilot.
        public string orderNumber                                   { get; set; }   // Unique ID of the related work order.
        public List<string> resources                               { get; set; }   // A list of unique IDs of resources aquired during the inspection of the component
        public string siteId                                        { get; set; }   // Unique ID of the related site
        public List<ComponentInspectionStatusEvent> statusHistory   { get; set; }   // A List of ComponentInspectionStatusEvent objects which list the status history for the inspection. An object will exist for each status, OnsitePending, DataAquisitionStarted, DataAquisitionCompleted, InspectionStarted, InspectionCompleted. For statuses which have not yet been reached, the timestamp and userId fields of the related status event object will be null.
        public string type                                          { get; set; }   // The type of inspection. Possible values are: DroneInspection, ManualInspection
    }
}
