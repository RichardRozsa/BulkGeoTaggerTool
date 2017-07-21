﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger.WindAMSObjects
{
    class ComponentInspectionSearchBean
    {
        public string assetId           { get; set; }   // Search by the given unique ID of an Asset
        public string assetInspectionId	{ get; set; }   // Search by the given unique ID of an AssetInspection
        public string componentId	    { get; set; }   // Search by the given unique ID of a Component
        public string siteId	        { get; set; }   // Search by the given unique ID of a Site
        public string orderNumber       { get; set; }   // Search by the given order number
    }
}