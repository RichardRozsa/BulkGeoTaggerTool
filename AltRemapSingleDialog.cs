using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ITGeoTagger
{
    public partial class AltRemapSingleDialog : Form
    {

        public float newValue { get; set; }


        public AltRemapSingleDialog()
        {
            InitializeComponent();

            TXT_ALT_START.Text = "50";

        }

        private void BUT_CANCLE_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BUT_Remap_Click(object sender, EventArgs e)
        {
            float startvalue;
            if(!float.TryParse(TXT_ALT_START.Text,out startvalue)){
                MessageBox.Show("New altitude value is not a number");
                return;
            }
            this.newValue= startvalue;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
    }
}
