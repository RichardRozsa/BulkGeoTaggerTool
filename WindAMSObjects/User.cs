using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class User
    {
        public string id { get; set; }   //Unique ID of the user
        public string firstName { get; set; }   //	First name of user
        public string lastName { get; set; }   //Last name of user
        public string middleName { get; set; }   //	Middle name of user
        public string emailAddress { get; set; }   //	The email address of the user
        public string login { get; set; }   //	The user login (Unique within the system)
        public string active { get; set; }   //	A boolean value indicating whether the user is active and may log in

    }
}
