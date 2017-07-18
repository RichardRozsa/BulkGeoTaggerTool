using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class Organization
    {
        public string id { get; set; }   //The unique ID of the group
        public string description { get; set; }   //A user readable description for the organization
        public string key { get; set; }   //	A program-usable unique key for the organization
        public string name { get; set; }   //	The name of the organization
    }
}
