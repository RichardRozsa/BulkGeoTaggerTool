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
    public partial class ImageReleaseForm : Form
    {
        public string JobNumber { get; set; }
        public string TurbineName { get; set; }
        public string Blade { get; set; }

        public ImageReleaseForm(string job,string turbine,string blade)
        {
            if (blade == "A"){
                DD_Blade.SelectedIndex = 0;
            }
            else if(blade == "B"){
                DD_Blade.SelectedIndex = 1;
            }
            else if (blade == "C")
            {
                DD_Blade.SelectedIndex = 2;
            }
            TXT_JN.Text = job;
            TXT_TURBINE.Text = turbine;

            InitializeComponent();
        }

        private void BUTT_CANCEL_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BUTT_RELEASE_Click(object sender, EventArgs e)
        {
            if (TXT_JN.Text.Length <2)
            {
                MessageBox.Show("No Job Number");
                return;
            }
            if (TXT_TURBINE.Text.Length < 1)
            {
                MessageBox.Show("No Turbine Name");
                return;
            }
            this.JobNumber = TXT_JN.Text;
            this.TurbineName = TXT_TURBINE.Text;
            this.Blade = DD_Blade.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
