using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class ResourceMetaData
    {
        public string   assetInspectionId       { get; set; }   // The unique ID of the Inspection for the asset (tower) for which the resource was created
        public string   componentInspectionId   { get; set; }   // The unique ID of the component (blade) for which the resource was created
        public string   componentId { get; set; }   // The unique ID of the component (blade) for which the resource was created
        public string   resourceId    { get; set; }   // The unique ID of the resource
        public string   contentType	{ get; set; }   // The MIME content type of the asset. Examples: image/jpeg image/png audio/mpeg video/x-sgi-movie
        public string   formId        { get; set; }   // (Deprecated)	The unique ID of the doforms form for which the asset was submitted
        public string   assetId	    { get; set; }   // The unique ID of the asset (tower) for which the resource was created
        public GeoPoint location	{ get; set; }   // A GeoPoint object including geo-stationary information about where the resource was created
        public string   name          { get; set; }   // A user readable name for the resource. Usually the original file name.
        public string   orderNumber	{ get; set; }   // The unique ID of the work order associated with the asset, if any.
        public int   sequence	    { get; set; }   // The sequence in which the resource should be viewed in the viewer. Applicable for image resources only.
        public string   siteId	    { get; set; }   // The unique ID of the site at which the asset was created
        public string   submissionId  { get; set; }   // (deprecated)	The unique ID of the doforms form submission to which the asset was attached
        public string   sourceResourceId { get; set; } //
        public string   sourceURL	    { get; set; }   // A URL from which the resource may be downloaded
        public int   pass	        { get; set; }   // The pass by the drone during which the image was taken.
        public string   processedBy   { get; set; }   // Person who proccessed the data
        public Dimension   size	        { get; set; }   // The size of the multimedia resource. An object which contains height, width, and optionally depth.
        public string   status	    { get; set; }   // The status of the image. Acceptable values are Uploaded, Processed, and Released
        public List<SensorReading> readings { get; set; }   // A list of SensorReading objects detailing sensor reading values.
        public string   timestamp	    { get; set; }   // Date and time the image was taken as indicated in image MetaData.
        public string   zoomifyId     { get; set; }   // Unique ID of the stored zoomify data.
        public string   zoomifyURL    { get; set; }   // A URL from which the zoomify data may be downloaded.
    }
}
