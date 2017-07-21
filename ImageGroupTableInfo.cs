using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ITGeoTagger
{
    public class ImageGroupTableInfo
    {
        public TableLayoutPanel Table = new TableLayoutPanel();
        private ITGeotagger Parent;
        public ImageGroupTableInfo(ITGeotagger ITForm)
        {
            this.Parent = ITForm;
            this.Table.ColumnCount = 8;
            this.Table.RowCount = 1;
            this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            this.Table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            this.Table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            this.Table.Controls.Add(new Label() { Text = "Folder Path", Dock = DockStyle.Fill }, 0, 0);
            this.Table.Controls.Add(new Label() { Text = "Tlog", Dock = DockStyle.Fill }, 1, 0);
            this.Table.Controls.Add(new Label() { Text = "Image Count", Dock = DockStyle.Fill }, 2, 0);
            this.Table.Controls.Add(new Label() { Text = "Offset", Dock = DockStyle.Fill }, 3, 0);
            this.Table.Controls.Add(new Label() { Text = "Pre-Procesing", Dock = DockStyle.Fill }, 4, 0);
            this.Table.Controls.Add(new Label() { Text = "Progress", Dock = DockStyle.Fill }, 5, 0);
            this.Table.Controls.Add(new Label() { Text = "Post-Procesing", Dock = DockStyle.Fill }, 6, 0);
            this.Table.Controls.Add(new Label() { Text = "Progress", Dock = DockStyle.Fill }, 7, 0);
            this.Table.AutoScroll = true;
        }
        async public void AddRow(string jpegpath, string tlogpath, string offset, int imageCount, int timeout = 0)
        {

            timeout++;
            if (timeout > 3)
            {
                Thread.Sleep(100);
                return;
            }

            if (this.Parent.InvokeRequired)
            {
                this.Parent.Invoke(new Action<string, string, string, int, int>(AddRow), new object[] { jpegpath, tlogpath, offset, imageCount, timeout });
                return;
            }

            CheckBox TmpCheck = new CheckBox() { Text = "", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft };
            ProgressBar ProgBar = new ProgressBar() { Text = "", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 };
            ProgressBar ProgBarCrop = new ProgressBar() { Text = "", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 };
            Button PreBUTT = new Button() { Text = "Pre-process", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 };
            Button PostBUTT = new Button() { Text = "Post-process", Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 };

            PreBUTT.Click += new EventHandler(Parent.AddPreProcessToQue);
            PostBUTT.Click += new EventHandler(Parent.ShowRowTurbineTab);

            PostBUTT.Enabled = false;

            this.Table.RowCount++;
            this.Table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            this.Table.Controls.Add(new Label() { Text = jpegpath, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft, Height = 35 }, 0, this.Table.RowCount - 1);
            this.Table.Controls.Add(new Label() { Text = tlogpath, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft, Height = 35 }, 1, this.Table.RowCount - 1);
            this.Table.Controls.Add(new Label() { Text = imageCount.ToString(), Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Left), TextAlign = ContentAlignment.MiddleLeft, Height = 35 }, 2, this.Table.RowCount - 1);
            this.Table.Controls.Add(new TextBox() { Text = offset, Dock = DockStyle.Fill, Anchor = (AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top), Height = 35 }, 3, this.Table.RowCount - 1);
            this.Table.Controls.Add(PreBUTT, 4, this.Table.RowCount - 1);
            this.Table.Controls.Add(ProgBar, 5, this.Table.RowCount - 1);
            this.Table.Controls.Add(PostBUTT, 6, this.Table.RowCount - 1);
            this.Table.Controls.Add(ProgBarCrop, 7, this.Table.RowCount - 1);
        }

    }
}
