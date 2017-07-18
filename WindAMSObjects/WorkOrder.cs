using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    public class WorkOrder
    {
        public string orderNumber   { get; set; }   //  The unique ID of the work order
        public string siteId        { get; set; }   //  The unique ID of the site at which the work will be done
        public string type          { get; set; }   //	The type of work to be done within the scope of the order. Current, the only available value is DroneBladInspection
        public string status        { get; set; }   //  The status of the work order. Possible values are: Requested, Onsite, ImagesUploaded, ImagesProcessed or Completed
        public string description   { get; set; }   //  A description of the work to be done specific to this particular order
        public string requestDate   { get; set; }   //  date given
        public string scope { get; set; }    //  adHoc, alarm, annual,endOfWarranty,turbineAlarm
    
    }
}
