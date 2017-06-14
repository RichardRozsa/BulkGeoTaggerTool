using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MissionPlanner
{
    public partial class AltRemapDialog : Form
    {

        public float StartValue { get; set; }
        public float EndValue { get; set; }

        public AltRemapDialog()
        {
            InitializeComponent();

            TXT_ALT_START.Text = "80";
            TXT_ALT_END.Text = "40";

        }

        private void BUT_CANCLE_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BUT_Remap_Click(object sender, EventArgs e)
        {
            float startvalue;
            float endvalue;
            if(!float.TryParse(TXT_ALT_END.Text,out endvalue)){
                MessageBox.Show("End altitude is not a number");
                return;
            }
            if(!float.TryParse(TXT_ALT_START.Text,out startvalue)){
                MessageBox.Show("Start altitude is not a number");
                return;
            }
            this.StartValue= startvalue;
            this.EndValue = endvalue;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
    }
}
