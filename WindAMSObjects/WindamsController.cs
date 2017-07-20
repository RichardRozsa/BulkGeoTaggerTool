using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Drawing.Imaging;
using System.Collections.Specialized;
using ITGeoTagger;


namespace ITGeoTagger.WindAMSObjects
{
    
    class WindamsController
    {
        public HttpClient windAMSClient;
        WorkOrder myWorkOrder = new WorkOrder();
        Site mySite = new Site();
        Asset myAsset = new Asset();
        ITGeoTagger.WindAMSObjects.Component myComponent = new ITGeoTagger.WindAMSObjects.Component();
        AssetInspection myAssetInspection = new AssetInspection();
        ComponentInspection myComponentInspection = new ComponentInspection();

        //################# INITIALIZATION      ####################
        public WindamsController(string baseURL) {
            this.windAMSClient = new HttpClient();
            this.windAMSClient.BaseAddress = new Uri(baseURL);
        }
        //##################################  HighLevel Functions #####################################

        async public Task<bool> WorkOrderExists(string workOrderNumber) {
            try
            {//search for workorder
                WorkOrder tmpWorkOrder = await this.GetWorkOrderAsync(workOrderNumber);
                if (myWorkOrder != null)
                {
                    myWorkOrder = tmpWorkOrder;
                    return true;
                }
                else {
                    return false;
                }

            }
            catch
            {
                return false;
            }
        }
        async public Task<bool> UploadToWindAMS(string workOrderNumber, string SiteName, string AssetName, string Blade, string processor, int pass, int sequence, ImageLocationAndExtraInfo ExtraInfo, ImageBladeGroup ImageGroup)
        {

            string assetName = AssetName;
            string componentType = "Blade"+Blade;
            WorkOrder myWorkOrder = new WorkOrder();
            string myprocessor = processor;
            try
            {//search for workorder
                myWorkOrder = await GetWorkOrderAsync(workOrderNumber);
            }
            catch
            {
                //MessageBox.Show("error no results");
                return false;
            }
                
            //check if asset exists in the site
            AssetSearchBean ASB = new AssetSearchBean();
            ASB.siteId = myWorkOrder.siteId;
            ASB.assetType = "Wind_Turbine_Tower";
            ASB.name = assetName;
            Asset[] myAssets = await SearchAssetsAsync(ASB);
            Asset myAsset = new Asset();
                
            if (myAssets != null)
            {
                if (myAssets.Length > 0)
                {
                    myAsset = myAssets[0];
                    if (myAsset.attributes == null) //check for atribute type patch // we can remove this once all offending object have been reverted
                    {
                        myAsset.attributes = new Dictionary<AssetAttributeType, string>();
                        myAsset = await UpdateAssetAsync(myAsset);
                    }
                }
                else
                {
                    myAsset = new Asset();
                    myAsset.attributes = new Dictionary<AssetAttributeType,string>();
                    myAsset.dateOfInstall = null;
                    myAsset.id = System.Guid.NewGuid().ToString();
                    myAsset.location = new GeoPoint();
                    myAsset.location.accuracy = 1;     // not sure how to use this
                    myAsset.location.altitude = 0;   // should be based on turbine info imported
                    myAsset.location.latitude = Double.Parse(ImageGroup.Latitude);   // should be based on turbine info imported
                    myAsset.location.longitude = Double.Parse(ImageGroup.Longitude); // should be based on turbine info imported
                    myAsset.make = "";
                    myAsset.model = "";
                    myAsset.name = assetName;
                    myAsset.serialNumber = "";
                    myAsset.siteId = myWorkOrder.siteId;
                    myAsset.type = "Wind_Turbine_Tower";
                    myAsset = await CreateAssetAsync(myAsset);
                }
            }
            else
            {
                //MessageBox.Show("error no results");
                return false;
            }

            //search for asset inspections

            AssetInspection[] myAssetInspections;

            AssetInspectionSearchBean AISB = new AssetInspectionSearchBean();
            AISB.siteId = myWorkOrder.siteId;
            AISB.assetId = myAsset.id;
            AISB.orderNumber = myWorkOrder.orderNumber;

            myAssetInspections = await SearchAssetInspectionsAsync(AISB);
            AssetInspection myAssetInspection = new AssetInspection();

            if (myAssetInspections != null)
            {
                if (myAssetInspections.Length > 0)
                {
                    myAssetInspection = myAssetInspections[0];
                    if (myAssetInspection.attributes == null) {
                        myAssetInspection.attributes = new Dictionary<AssetInspectionAttributeType, string>();

                        myAssetInspection = await UpdateAssetInspectionAsync(myAssetInspection);
                    }
                }
                else
                {
                    //MessageBox.Show("error no results");

                    myAssetInspection.assetId = myAsset.id;
                    myAssetInspection.attributes = new Dictionary<AssetInspectionAttributeType, string>();
                    myAssetInspection.dateOfInspection = this.DateTimeToWindamsDateString(ExtraInfo.Time);  // date of inspection needs to be defined 
                    myAssetInspection.id = System.Guid.NewGuid().ToString();
                    myAssetInspection.orderNumber = myWorkOrder.orderNumber;
                    myAssetInspection.processedBy = myprocessor; //needs to be defined on upload or sign in
                    myAssetInspection.resources = null;   
                    myAssetInspection.type = "DroneBladeInspection";
                    myAssetInspection.siteId = myWorkOrder.siteId;
                    myAssetInspection.status = "In_Process";

                    myAssetInspection = await CreateAssetInspectionAsync(myAssetInspection);
                }
            }
            else
            {
                //MessageBox.Show("error no results");
                return false;
            }

            //find component or create component

            WindAMSObjects.Component myComponent = new WindAMSObjects.Component();
            ComponentSearchBean CSB = new ComponentSearchBean();

            CSB.assetId = myAsset.id;
            CSB.componentType = componentType;
            CSB.siteId = myWorkOrder.siteId;

            WindAMSObjects.Component[] myComponents;

            myComponents = await SearchComponentsAsync(CSB);

            if (myComponents != null)
            {
                if (myComponents.Length > 0)
                {
                    myComponent = myComponents[0];
                    if (myComponent.attributes == null) {
                        myComponent.attributes = new Dictionary<ComponentAttributeType, string>();
                        myComponent = await UpdateComponentAsync(myComponent);
                    }
                }
                else
                {
                    //MessageBox.Show("error no results");

                    myComponent.assetId = myAsset.id;
                    myComponent.attributes = new Dictionary<ComponentAttributeType,string>();
                    myComponent.type = componentType;
                    myComponent.id = System.Guid.NewGuid().ToString();
                    myComponent.make = ""; //not sure if we shoud use this 
                    myComponent.siteId = myWorkOrder.siteId;

                    myComponent = await CreateComponentAsync(myComponent);
                }
            }
            else
            {
                MessageBox.Show("error no results");
                return false;
            }

            //find componentInspection or create componentInspection

            ComponentInspection myComponentInspection = new ComponentInspection();
            ComponentInspectionSearchBean CISB = new ComponentInspectionSearchBean();

            CISB.assetId = myAsset.id;
            CISB.componentId = myComponent.id;
            CISB.siteId = myWorkOrder.siteId;
            CISB.orderNumber = myWorkOrder.orderNumber;
            CISB.assetInspectionId = myAssetInspection.id;

            ComponentInspection[] myComponentInspections;

            myComponentInspections = await SearchComponentInspectionsAsync(CISB);

            if (myComponentInspections != null)
            {
                if (myComponentInspections.Length > 0)
                {
                    myComponentInspection = myComponentInspections[0];
                    if (myComponentInspection.statusHistory.Count == 0) {
                        myComponentInspection.statusHistory = new List<ComponentInspectionStatusEvent>();

                        ComponentInspectionStatusEvent CISE_InspectionStarted = new ComponentInspectionStatusEvent();
                        CISE_InspectionStarted.status = "InspectionStarted";
                        CISE_InspectionStarted.timestamp = null;
                        CISE_InspectionStarted.userId = null;

                        ComponentInspectionStatusEvent CISE_InspectionCompleted = new ComponentInspectionStatusEvent();
                        CISE_InspectionCompleted.status = "InspectionCompleted";
                        CISE_InspectionCompleted.timestamp = null;
                        CISE_InspectionCompleted.userId = null;

                        myComponentInspection.statusHistory.Add(CISE_InspectionStarted);
                        myComponentInspection.statusHistory.Add(CISE_InspectionCompleted);
                        myComponentInspection = await UpdateComponentInspectionAsync(myComponentInspection);
                    }
                }
                else
                {
                    myComponentInspection.id= System.Guid.NewGuid().ToString();
                    myComponentInspection.assetId = myAsset.id;
                    myComponentInspection.assetInspectionId = myAssetInspection.id;
                    myComponentInspection.componentId = myComponent.id;
                    myComponentInspection.inspectorUserId = System.Guid.NewGuid().ToString();
                    myComponentInspection.orderNumber = myWorkOrder.orderNumber;
                    myComponentInspection.siteId = myWorkOrder.siteId;
                    myComponentInspection.type = "DroneInspection";
                    myComponentInspection.resources = new List<string>();
                    myComponentInspection.statusHistory = new List<ComponentInspectionStatusEvent>();

                    ComponentInspectionStatusEvent CISE_InspectionStarted = new ComponentInspectionStatusEvent();
                    CISE_InspectionStarted.status = "InspectionStarted";
                    CISE_InspectionStarted.timestamp = null;
                    CISE_InspectionStarted.userId = null;
                    
                    ComponentInspectionStatusEvent CISE_InspectionCompleted = new ComponentInspectionStatusEvent();
                    CISE_InspectionCompleted.status = "InspectionCompleted";
                    CISE_InspectionCompleted.timestamp = null;
                    CISE_InspectionCompleted.userId = null;

                    myComponentInspection.statusHistory.Add(CISE_InspectionStarted);
                    myComponentInspection.statusHistory.Add(CISE_InspectionCompleted);

                    myComponentInspection = await CreateComponentInspectionAsync(myComponentInspection);
                }
            }
            else
            {
                MessageBox.Show("error no results");
                return false;
            }

            //create the meta data tags for an image 

            ResourceMetaData myResourceMetaData = new ResourceMetaData();
            myResourceMetaData.assetId = myAsset.id;
            myResourceMetaData.assetInspectionId = myAssetInspection.id;
            myResourceMetaData.componentInspectionId = myComponentInspection.id;
            myResourceMetaData.componentId = myComponent.id;
            myResourceMetaData.contentType = "image/jpeg";
            myResourceMetaData.formId = null;
            myResourceMetaData.location = new GeoPoint();
            myResourceMetaData.location.accuracy = 1;                    // not sure how to use this
            myResourceMetaData.location.altitude = ExtraInfo.Altitude;   // needs to be based on image
            myResourceMetaData.location.latitude = ExtraInfo.Longitude;  // needs to be based on image
            myResourceMetaData.location.longitude = ExtraInfo.Latitude;  // needs to be based on image
            myResourceMetaData.siteId = myWorkOrder.siteId;
            myResourceMetaData.name = Path.GetFileName(ExtraInfo.PathToDestination);
            myResourceMetaData.orderNumber = myWorkOrder.orderNumber;
            myResourceMetaData.pass = pass;   // defined durring upload
            myResourceMetaData.readings = new List<SensorReading>(); //left undefined
            myResourceMetaData.resourceId = this.GetImageGUID(ExtraInfo.PathToDestination);
            myResourceMetaData.sequence = sequence; // defined durring upload
            myResourceMetaData.size = new Dimension();

            List<int> heightWidthofImage = this.GetImageHeightWidth(ExtraInfo.PathToDestination);
            myResourceMetaData.size.width = heightWidthofImage[1]; // needs to be based on image
            myResourceMetaData.size.height = heightWidthofImage[0];; //needs to be based on image
            myResourceMetaData.size.depth =1; //needs to be based on image

            myResourceMetaData.sourceURL = null;
            myResourceMetaData.status = "QueuedForUpload";
            myResourceMetaData.submissionId = null;
            myResourceMetaData.timestamp = DateTimeToWindamsDateTimeString(ExtraInfo.Time);


            await CreateResourceMetaDataAsync(myResourceMetaData);

            HttpStatusCode response = await UploadImageDataAsync(myResourceMetaData.resourceId,ExtraInfo.PathToDestination ,File.ReadAllBytes(ExtraInfo.PathToDestination));


            if (response == HttpStatusCode.OK)
            {
                myResourceMetaData.status = "Processed";
                await CreateResourceMetaDataAsync(myResourceMetaData);
            }




            return true;
        }
        

        //#######################################  Object Bean Search Functions  ##############################################
        async Task<Site[]> GetSitesArrayAsync()
        {
            Site[] Sites = null;
            HttpResponseMessage response = await this.windAMSClient.GetAsync("site");
            if (response.IsSuccessStatusCode)
            {
                Sites = await response.Content.ReadAsAsync<Site[]>();
            }
            return Sites;
        }
        async Task<Site[]> GetSiteByName(string name)
        {
            Site[] Sites = null;
            HttpResponseMessage response = await this.windAMSClient.GetAsync("site/" + name);
            if (response.IsSuccessStatusCode)
            {
                Sites = await response.Content.ReadAsAsync<Site[]>();
            }
            return Sites;
        }
        async Task<Site[]> GetSiteByID(string ID)
        {
            Site[] Sites = null;
            HttpResponseMessage response = await this.windAMSClient.GetAsync("site/" + ID);
            if (response.IsSuccessStatusCode)
            {
                Sites = await response.Content.ReadAsAsync<Site[]>();
            }
            return Sites;
        }
        async Task<Asset[]> SearchAssetsAsync(AssetSearchBean ASB)
        {
            Asset[] siteAssets = null;
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("asset/search", ASB);
            response.EnsureSuccessStatusCode();

            // Return the URI of the created resource.
            siteAssets = await response.Content.ReadAsAsync<Asset[]>();
            return siteAssets;
        }
        async Task<WorkOrder[]> SearchWorkOrdersAsync(WorkOrderSearchBean WOSB)
        {
            WorkOrder[] WorkOrders = null;
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("workOrder/search", WOSB);
            response.EnsureSuccessStatusCode();

            // Return the URI of the created resource.
            WorkOrders = await response.Content.ReadAsAsync<WorkOrder[]>();
            return WorkOrders;
        }
        async Task<AssetInspection[]> SearchAssetInspectionsAsync(AssetInspectionSearchBean AISB)
        {
            AssetInspection[] AssetInspections = null;
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("assetInspection/search", AISB);
            response.EnsureSuccessStatusCode();

            // Return the URI of the created resource.
            AssetInspections = await response.Content.ReadAsAsync<AssetInspection[]>();
            return AssetInspections;
        }
        async Task<ITGeoTagger.WindAMSObjects.Component[]> SearchComponentsAsync(ComponentSearchBean CSB)
        {
            ITGeoTagger.WindAMSObjects.Component[] Components = null;
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("component/search", CSB);
            response.EnsureSuccessStatusCode();

            // Return the URI of the created resource.
            Components = await response.Content.ReadAsAsync<ITGeoTagger.WindAMSObjects.Component[]>();
            return Components;
        }
        async Task<ComponentInspection[]> SearchComponentInspectionsAsync(ComponentInspectionSearchBean CISB)
        {
            ComponentInspection[] AssetInspections = null;
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("componentInspection/search", CISB);
            response.EnsureSuccessStatusCode();

            // Return the URI of the created resource.
            AssetInspections = await response.Content.ReadAsAsync<ComponentInspection[]>();
            return AssetInspections;
        }

        //#######################################  Object Creation Functions    ##############################################
        async Task<Site> CreateSiteAsync(Site newSite)
        {
            HttpResponseMessage response = await this.windAMSClient.PutAsJsonAsync("site", newSite);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newSite = await response.Content.ReadAsAsync<Site>();
            return newSite;
        }
        async Task<Asset> CreateAssetAsync(Asset newAsset)
        {
            HttpResponseMessage response = await this.windAMSClient.PutAsJsonAsync("asset", newAsset);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newAsset = await response.Content.ReadAsAsync<Asset>();
            return newAsset;
        }
        async Task<Asset> UpdateAssetAsync(Asset newAsset)
        {
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("asset", newAsset);
            response.EnsureSuccessStatusCode();

            return newAsset;
        }
        async Task<AssetInspection> CreateAssetInspectionAsync(AssetInspection newAssetInspection)
        {
            HttpResponseMessage response = await this.windAMSClient.PutAsJsonAsync("assetInspection", newAssetInspection);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newAssetInspection = await response.Content.ReadAsAsync<AssetInspection>();
            return newAssetInspection;
        }
        async Task<AssetInspection> UpdateAssetInspectionAsync(AssetInspection newAssetInspection)
        {
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("assetInspection", newAssetInspection);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            return newAssetInspection;
        }
        async Task<ITGeoTagger.WindAMSObjects.Component> CreateComponentAsync(ITGeoTagger.WindAMSObjects.Component newComponent)
        {
            HttpResponseMessage response = await this.windAMSClient.PutAsJsonAsync("component", newComponent);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newComponent = await response.Content.ReadAsAsync<ITGeoTagger.WindAMSObjects.Component>();
            return newComponent;
        }
        async Task<ITGeoTagger.WindAMSObjects.Component> UpdateComponentAsync(ITGeoTagger.WindAMSObjects.Component newComponent)
        {
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("component", newComponent);
            response.EnsureSuccessStatusCode();

            return newComponent;
        }
        async Task<ComponentInspection> CreateComponentInspectionAsync(ComponentInspection newComponentInspection)
        {
            HttpResponseMessage response = await this.windAMSClient.PutAsJsonAsync("componentInspection", newComponentInspection);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newComponentInspection = await response.Content.ReadAsAsync<ComponentInspection>();
            return newComponentInspection;
        }
        async Task<ComponentInspection> UpdateComponentInspectionAsync(ComponentInspection newComponentInspection)
        {
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("componentInspection", newComponentInspection);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newComponentInspection = await response.Content.ReadAsAsync<ComponentInspection>();
            return newComponentInspection;
        }

        async Task<ResourceMetaData> CreateResourceMetaDataAsync(ResourceMetaData newResourseMetaData)
        {
            HttpResponseMessage response = await this.windAMSClient.PostAsJsonAsync("resource", newResourseMetaData);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newResourseMetaData = await response.Content.ReadAsAsync<ResourceMetaData>();
            return newResourseMetaData;
        }
        async Task<WorkOrder> CreateWorkOrderAsync(WorkOrder newWorkOrder)
        {
            HttpResponseMessage response = await this.windAMSClient.PutAsJsonAsync("workOrder", newWorkOrder);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newWorkOrder = await response.Content.ReadAsAsync<WorkOrder>();
            return newWorkOrder;
        }

        //######################################## Get Objects  ######################################
        async Task<WorkOrder> GetWorkOrderAsync(string WorkOrderNumber)
        {
            WorkOrder newWorkOrder = new WorkOrder();
            HttpResponseMessage response = await this.windAMSClient.GetAsync("workOrder/" + WorkOrderNumber);
            response.EnsureSuccessStatusCode();

            // Deserialize the updated product from the response body.
            newWorkOrder = await response.Content.ReadAsAsync<WorkOrder>();
            return newWorkOrder;
        }
        //######################################## Upload Image ######################################
        private async Task<HttpStatusCode> UploadImageDataAsync(string ID, string fileLocation, Byte[] imageFileData)
        {

            HttpStatusCode returnCode = ImageUpload("multimedia/" + ID, fileLocation, imageFileData);

            return returnCode;
        }
        // Perform the equivalent of posting a form with a filename and two files, in HTML:
        // <form action="{url}" method="post" enctype="multipart/form-data">
        //     <input type="image/jpeg" name="filename" />
        // </form>
        private HttpStatusCode ImageUpload(string url, string filename, byte[] fileBytes)
        {
            // Convert each of the three inputs into HttpContent objects

            HttpContent stringContent = new StringContent(filename);
            // examples of converting both Stream and byte [] to HttpContent objects
            // representing input type file
            HttpContent bytesContent = new ByteArrayContent(fileBytes);

            // Submit the form using HttpClient and 
            // create form data as Multipart (enctype="multipart/form-data")

            using (var formData = new MultipartFormDataContent())
            {
                // Add the HttpContent objects to the form data

                // <input type="text" name="filename" />
                formData.Add(bytesContent, "image/jpeg", Path.GetFileName(filename));

                // Actually invoke the request to the server

                // equivalent to (action="{url}" method="post")
                var response = this.windAMSClient.PostAsync(url, formData).Result;

                // equivalent of pressing the submit button on the form
                if (!response.IsSuccessStatusCode)
                {
                    return HttpStatusCode.BadRequest;
                }
                return response.StatusCode;
            }
        }

//##################### Extra Functions ###################################
        
        private string GetImageGUID(string Path)
        {
            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(Path)))
            {
                using (Image Pic = Image.FromStream(ms))
                {
                    PropertyItem[] pi = Pic.PropertyItems;

                    bool NeedsUID = true;
                    foreach (PropertyItem item in pi)
                    {
                        if (item.Id == 0xA420)
                        {
                            return System.Text.Encoding.ASCII.GetString(item.Value).TrimEnd('\0');
                        }
                    }
                }
            }
            return "";
        }
        private List<int> GetImageHeightWidth(string Path)
        {
            List<int> HeightWidth = new List<int>();

            using (System.Drawing.Image img = System.Drawing.Image.FromFile(Path))
            {
                HeightWidth.Add(img.Height);
                HeightWidth.Add(img.Width);
            }

            return HeightWidth;
        }
        public string DateTimeToWindamsDateTimeString(DateTime dateTime) {
            return String.Format("{0:yyyyMMdd'T'HHmmss.fff}+0000", dateTime.ToUniversalTime());
        }
        public string DateTimeToWindamsTimeString(DateTime dateTime)
        {
            return String.Format("{0:'T'HHmmss.fff}", dateTime.ToUniversalTime());
        }
        public string DateTimeToWindamsDateString(DateTime dateTime)
        {
            return String.Format("{0:yyyyMMdd}", dateTime.ToUniversalTime());
        }
    }
}
