using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ITGeoTagger
{
    public partial class UploadRelease : Form
    {
        public string workOrderNumber { get; set; }
        public string site { get; set; }
        public string assetName { get; set; }
        public string blade { get; set; }
        public string processor { get; set; }

        public UploadRelease(string won_in,string site_in,string asset_in,string blade_in,string processor_in)
        {

            InitializeComponent();

            TXT_ASSET_NAME.Text = asset_in;
            TXT_BLADE.Text = blade_in;
            TXT_WON.Text = won_in;
            TXT_SITE.Text = site_in;
            TXT_PROCESSOR.Text = processor_in;

        }

        private void BUT_OK_Click(object sender, EventArgs e)
        {
            this.workOrderNumber = TXT_WON.Text;
            this.site = TXT_SITE.Text;
            this.assetName = TXT_ASSET_NAME.Text;
            this.blade = TXT_BLADE.Text;
            this.processor = TXT_PROCESSOR.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BUT_CANCEL_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
