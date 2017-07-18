using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class OrganizationAddress
    {
        public string id { get; set; }   //The unique ID of the org address
        public string organizationId { get; set; }   //	The unique ID of the related organization
        public string description { get; set; }   //A user readable description for the address. Should be consistent across addresses.
        public string line1 { get; set; }   //First line of the address
        public string line2 { get; set; }   //	Second line of the address
        public string city { get; set; }   //City of the address
        public string region { get; set; }   //	Region of the address
        public string postalCode { get; set; }   //	Postal code of the address
    }
}
