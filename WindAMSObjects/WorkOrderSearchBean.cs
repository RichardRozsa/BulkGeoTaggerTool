using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class WorkOrderSearchBean
    {
        public string siteId	{ get; set; }   // The unique ID of the site at which the work will be done
        public string type	    { get; set; }   // The type of work to be done within the scope of the order. Current, the only available value is DroneBladInspection
        public string statuses	{ get; set; }   // A list of status of which the matching order must have one of. Possible values are: Requested, Onsite, ImagesUploaded, ImagesProcessed or Completed
    }
}
