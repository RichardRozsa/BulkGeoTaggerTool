using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class Group
    {
        public string id{ get; set; }   //	The unique ID of the group
        public string applicationId	{ get; set; }   //The unique ID of the related application
        public string name	{ get; set; }   //The name of the group
        public string key { get; set; }   //A unique program-usable ID for the group
    }
}
