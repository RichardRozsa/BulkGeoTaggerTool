using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ITGeoTagger
{
    public class ITConfigFile
    {
        public string   upload_URL           { get; set; } // sets the upload URL
        public string   triggerChannel       { get; set; } // string name of the trigger channel, defaults to channel 7
        public string   processorName        { get; set; } // processor name
        public string   workOrderNumber      { get; set; } // processor name
        public int      triggerThreshold     { get; set; } // servo pwm value between the on noff trigger values
        public int      triggerHighOrLow     { get; set; } // 1=HIGH 0=LOW
        private string saveFileName = "IT_CONFIGURATIONS.xml";

        public ITConfigFile() { //initialize object
        }
        public ITConfigFile LoadFile()
        {
            ITConfigFile tmpFile = new ITConfigFile();
            if (File.Exists(saveFileName))
            {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(ITConfigFile));
                System.IO.StreamReader file = new System.IO.StreamReader(saveFileName);

                tmpFile = (ITConfigFile)reader.Deserialize(file);
                file.Close();

            }
            else {
                tmpFile = this.Defaults();
            }
            this.processorName = tmpFile.processorName;
            this.upload_URL = tmpFile.upload_URL;
            this.triggerChannel = tmpFile.triggerChannel;
            this.triggerThreshold = tmpFile.triggerThreshold;
            this.triggerHighOrLow = tmpFile.triggerHighOrLow;
            this.workOrderNumber = tmpFile.workOrderNumber;
            

            return this;
        }
        public bool SaveFile()
        {

            System.Xml.Serialization.XmlSerializer CameraWriter = new System.Xml.Serialization.XmlSerializer(typeof(ITConfigFile));
            System.IO.FileStream wfile = System.IO.File.Create(saveFileName);
            CameraWriter.Serialize(wfile, this);
            wfile.Close();

            return true;
        }
        public ITConfigFile Defaults()
        {
            this.processorName = "not entered";
            this.upload_URL = "https://services.inspectools.net/";
            this.triggerChannel = "7";
            this.triggerThreshold = 1480;
            this.triggerHighOrLow = 0;
            this.workOrderNumber = "not entered";
            this.SaveFile();
            return this;
        }

    }
    
}
