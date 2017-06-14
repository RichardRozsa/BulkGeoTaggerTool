namespace MissionPlanner
{
    partial class ITGeotagger
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.TXT_outputlog = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.BUT_GET_DIR = new System.Windows.Forms.Button();
            this.TXT_BROWSE_FOLDER = new System.Windows.Forms.TextBox();
            this.TabOrganize = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.BUT_GET_TRIG_OFFSETS = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.MAIN_TAB_CONTROL = new System.Windows.Forms.TabControl();
            this.Alltab = new System.Windows.Forms.TabPage();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.TIMER_THREAD_CHECKER = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.TabOrganize.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.MAIN_TAB_CONTROL.SuspendLayout();
            this.Alltab.SuspendLayout();
            this.SuspendLayout();
            // 
            // TXT_outputlog
            // 
            this.TXT_outputlog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TXT_outputlog.Location = new System.Drawing.Point(3, 394);
            this.TXT_outputlog.Name = "TXT_outputlog";
            this.TXT_outputlog.Size = new System.Drawing.Size(962, 98);
            this.TXT_outputlog.TabIndex = 0;
            this.TXT_outputlog.Text = "";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.TXT_outputlog, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.TabOrganize, 0, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 76.6537F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 23.3463F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(968, 495);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 340F));
            this.tableLayoutPanel2.Controls.Add(this.BUT_GET_DIR, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.TXT_BROWSE_FOLDER, 0, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(962, 44);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // BUT_GET_DIR
            // 
            this.BUT_GET_DIR.Location = new System.Drawing.Point(314, 3);
            this.BUT_GET_DIR.Name = "BUT_GET_DIR";
            this.BUT_GET_DIR.Size = new System.Drawing.Size(173, 27);
            this.BUT_GET_DIR.TabIndex = 2;
            this.BUT_GET_DIR.Text = "Browse";
            this.BUT_GET_DIR.UseVisualStyleBackColor = true;
            this.BUT_GET_DIR.Click += new System.EventHandler(this.BUT_GET_DIR_Click);
            // 
            // TXT_BROWSE_FOLDER
            // 
            this.TXT_BROWSE_FOLDER.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TXT_BROWSE_FOLDER.Location = new System.Drawing.Point(3, 3);
            this.TXT_BROWSE_FOLDER.Name = "TXT_BROWSE_FOLDER";
            this.TXT_BROWSE_FOLDER.Size = new System.Drawing.Size(305, 27);
            this.TXT_BROWSE_FOLDER.TabIndex = 1;
            // 
            // TabOrganize
            // 
            this.TabOrganize.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TabOrganize.ColumnCount = 1;
            this.TabOrganize.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TabOrganize.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.TabOrganize.Location = new System.Drawing.Point(3, 53);
            this.TabOrganize.Name = "TabOrganize";
            this.TabOrganize.RowCount = 2;
            this.TabOrganize.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.TabOrganize.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TabOrganize.Size = new System.Drawing.Size(962, 335);
            this.TabOrganize.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.ColumnCount = 5;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 22.79412F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 27.20588F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 365F));
            this.tableLayoutPanel3.Controls.Add(this.BUT_GET_TRIG_OFFSETS, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.textBox1, 4, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(956, 44);
            this.tableLayoutPanel3.TabIndex = 1;
            // 
            // BUT_GET_TRIG_OFFSETS
            // 
            this.BUT_GET_TRIG_OFFSETS.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BUT_GET_TRIG_OFFSETS.Location = new System.Drawing.Point(3, 3);
            this.BUT_GET_TRIG_OFFSETS.Name = "BUT_GET_TRIG_OFFSETS";
            this.BUT_GET_TRIG_OFFSETS.Size = new System.Drawing.Size(128, 38);
            this.BUT_GET_TRIG_OFFSETS.TabIndex = 1;
            this.BUT_GET_TRIG_OFFSETS.Text = "Get Offsets";
            this.BUT_GET_TRIG_OFFSETS.UseVisualStyleBackColor = true;
            this.BUT_GET_TRIG_OFFSETS.Click += new System.EventHandler(this.BUT_GET_TRIG_OFFSETS_Click);
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(591, 3);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(362, 27);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "JobNumber";
            this.textBox1.Visible = false;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // MAIN_TAB_CONTROL
            // 
            this.MAIN_TAB_CONTROL.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MAIN_TAB_CONTROL.Controls.Add(this.Alltab);
            this.MAIN_TAB_CONTROL.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MAIN_TAB_CONTROL.Location = new System.Drawing.Point(1, 0);
            this.MAIN_TAB_CONTROL.Name = "MAIN_TAB_CONTROL";
            this.MAIN_TAB_CONTROL.SelectedIndex = 0;
            this.MAIN_TAB_CONTROL.Size = new System.Drawing.Size(982, 532);
            this.MAIN_TAB_CONTROL.TabIndex = 1;
            this.MAIN_TAB_CONTROL.TabIndexChanged += new System.EventHandler(this.MAIN_TAB_CONTROL_TabIndexChanged);
            // 
            // Alltab
            // 
            this.Alltab.Controls.Add(this.tableLayoutPanel1);
            this.Alltab.Location = new System.Drawing.Point(4, 29);
            this.Alltab.Name = "Alltab";
            this.Alltab.Padding = new System.Windows.Forms.Padding(3);
            this.Alltab.Size = new System.Drawing.Size(974, 499);
            this.Alltab.TabIndex = 0;
            this.Alltab.Text = "Overview";
            this.Alltab.UseVisualStyleBackColor = true;
            // 
            // TIMER_THREAD_CHECKER
            // 
            this.TIMER_THREAD_CHECKER.Tick += new System.EventHandler(this.TIMER_THREAD_CHECKER_Tick);
            // 
            // ITGeotagger
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(986, 534);
            this.Controls.Add(this.MAIN_TAB_CONTROL);
            this.Name = "ITGeotagger";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.TabOrganize.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.MAIN_TAB_CONTROL.ResumeLayout(false);
            this.Alltab.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox TXT_outputlog;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TabControl MAIN_TAB_CONTROL;
        private System.Windows.Forms.TabPage Alltab;
        private System.Windows.Forms.Button BUT_GET_DIR;
        private System.Windows.Forms.TextBox TXT_BROWSE_FOLDER;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel TabOrganize;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Button BUT_GET_TRIG_OFFSETS;
        private System.Windows.Forms.Timer TIMER_THREAD_CHECKER;
        private System.Windows.Forms.TextBox textBox1;
    }
}

