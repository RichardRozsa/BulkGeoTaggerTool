using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class InspectionEvent
    {
        public string id { get; set; }   //The unique ID of the inspection event
        public string assetId { get; set; }   //	The unique ID of the tower
        public string componentId { get; set; }   //The unique ID of the Component that was inspected.
        public string comment { get; set; }   //	Comment about the event
        public string date { get; set; }   //The date of the event
        public string findingType { get; set; }   //	The type of finding. Acceptable values are Corrosion, Crush, Debonding, Delamination, Dirty, DryLayup, ErosionLight, ErosionModerate, ErosionSignificant, FinishChipped, FinishOxidized, FinishPeeling, FinishPitted, FinishThinning, FinishCrazingOrChecking, GurneyFlaps, Hole, IDPlate, InsectedResidue, LETapeMissingOrDamaged, LightningReceptor, LubricantStained, MoldStains, OldRepair, RainShield, ScratchesLight, ScratchesDeep, Splitting, Spoiler, StallStrips, StressCracking, StructuralCracking or VortexGenerators
        public string inspectionEventResources { get; set; }   //A list of InspectionEventResource objects which show the inspection findings.
        public string location { get; set; }   //The location of the damage. It is an object containing distanceFromRoot and distanceFromLeadingEdge as double values.
        public string name { get; set; }   //	A name for the event.
        public string observationType { get; set; }   //	The type of damage. Acceptable values are Component, Composite, Miscellaneous
        public string orderNumber { get; set; }   //	The unique ID of the work order approving the inspection
        public string severity { get; set; }   //An integer value between 1 and 5 inclusive indicating severity.
        public string size { get; set; }   //The size of the area of incident. An object containing height, width, and possibly depth.
        public string siteId { get; set; }   //The unique ID of the site at where the work is being done
        public string surfaceType { get; set; }   //	The type of surface. Can be LeadingEdge, TrailingEdge, PressureSide, SuctionSide, PressureSuctionSide, Tower, or null
        public string userId { get; set; }   //	The unique ID of the user which reported the event.
        public string primaryInspectionEventResourceId { get; set; }   //The unique ID inspection event resource linking to the resource to display in reports of the damage.

    }
}
