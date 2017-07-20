namespace ITGeoTagger
{
    partial class AltRemapDialog
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.TXT_ALT_END = new System.Windows.Forms.TextBox();
            this.TXT_ALT_START = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.BUT_CANCEL = new System.Windows.Forms.Button();
            this.BUT_Remap = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 183F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.TXT_ALT_START, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.TXT_ALT_END, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.BUT_CANCEL, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.BUT_Remap, 1, 2);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(1, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 34.35583F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 65.64417F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 79F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(371, 163);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(182, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Start alt";
            // 
            // TXT_ALT_END
            // 
            this.TXT_ALT_END.Location = new System.Drawing.Point(191, 31);
            this.TXT_ALT_END.Name = "TXT_ALT_END";
            this.TXT_ALT_END.Size = new System.Drawing.Size(100, 22);
            this.TXT_ALT_END.TabIndex = 1;
            // 
            // TXT_ALT_START
            // 
            this.TXT_ALT_START.Location = new System.Drawing.Point(191, 3);
            this.TXT_ALT_START.Name = "TXT_ALT_START";
            this.TXT_ALT_START.Size = new System.Drawing.Size(100, 22);
            this.TXT_ALT_START.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(182, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "End alt";
            // 
            // BUT_CANCEL
            // 
            this.BUT_CANCEL.Location = new System.Drawing.Point(3, 86);
            this.BUT_CANCEL.Name = "BUT_CANCEL";
            this.BUT_CANCEL.Size = new System.Drawing.Size(182, 74);
            this.BUT_CANCEL.TabIndex = 4;
            this.BUT_CANCEL.Text = "Cancel";
            this.BUT_CANCEL.UseVisualStyleBackColor = true;
            this.BUT_CANCEL.Click += new System.EventHandler(this.BUT_CANCLE_Click);
            // 
            // BUT_Remap
            // 
            this.BUT_Remap.Location = new System.Drawing.Point(191, 86);
            this.BUT_Remap.Name = "BUT_Remap";
            this.BUT_Remap.Size = new System.Drawing.Size(171, 74);
            this.BUT_Remap.TabIndex = 5;
            this.BUT_Remap.Text = "Remap";
            this.BUT_Remap.UseVisualStyleBackColor = true;
            this.BUT_Remap.Click += new System.EventHandler(this.BUT_Remap_Click);
            // 
            // AltRemapDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(375, 168);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "AltRemapDialog";
            this.Text = "AltRemapDialog";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox TXT_ALT_START;
        private System.Windows.Forms.TextBox TXT_ALT_END;
        private System.Windows.Forms.Button BUT_CANCEL;
        private System.Windows.Forms.Button BUT_Remap;
    }
}