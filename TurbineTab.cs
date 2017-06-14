using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Threading;

using Emgu.CV.Structure;
using Emgu.Util.TypeEnum;
using Emgu.CV.Shape;
using Manina.Windows.Forms;

namespace MissionPlanner
{
    partial class TurbineTab : UserControl
    {

        public int row;
        public ImageBladeGroup ImageGroup = new ImageBladeGroup();
        
        public ImageListView Images_pass1 = new ImageListView();
        public ImageListView Images_pass2 = new ImageListView();
        public ImageListView Images_pass3 = new ImageListView();
        public ImageListView Images_pass4 = new ImageListView();
        public ImageListView Images_pass5 = new ImageListView();
        public ImageListView Images_extra = new ImageListView();

        String PATH_TO_ORIGIONALS = "";
        String PATH_TO_SMALLS = "";
        String PATH_TO_CROP_GRAYS = "";
        String PATH_TO_SAVED_PROG_FILE = "";

        Thread ReleaseThread;
        ITGeotagger ParentForm;
        ProgressBar PostProccessProgresBar;
        
        public TurbineTab(string BaseDir,ProgressBar PB,ITGeotagger ITG,int Row)
        {            
            InitializeComponent();

            this.row = Row;

            LABEL_PATH.Text = BaseDir;

            PATH_TO_SAVED_PROG_FILE = Path.Combine(BaseDir, "Processed.xml");

            try { LoadProgress(); }
            catch {
                MessageBox.Show("Progress file is corrupted or missing");
            }

            ParentForm = ITG;

            PATH_TO_ORIGIONALS = BaseDir;
            PostProccessProgresBar = PB;

            //set up the pass one image box
            Images_pass1.Parent = TAB_PASS_1;
            Images_pass1.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            Images_pass1.Dock = DockStyle.Fill;
            Images_pass1.SetRenderer(new ImageListViewRenderers.XPRenderer());
            Images_pass1.ThumbnailSize = new Size(300, 200);

            //set up the pass two image box
            Images_pass2.Parent = TAB_PASS_2;
            Images_pass2.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            Images_pass2.Dock = DockStyle.Fill;
            Images_pass2.SetRenderer(new ImageListViewRenderers.XPRenderer());
            Images_pass2.ThumbnailSize = new Size(300, 200);
            Images_pass2.SortOrder = SortOrder.Ascending;

            //set up the pass three image box
            Images_pass3.Parent = TAB_PASS_3;
            Images_pass3.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            Images_pass3.Dock = DockStyle.Fill;
            Images_pass3.SetRenderer(new ImageListViewRenderers.XPRenderer());
            Images_pass3.ThumbnailSize = new Size(300, 200);

            //set up the pass four image box
            Images_pass4.Parent = TAB_PASS_4;
            Images_pass4.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            Images_pass4.Dock = DockStyle.Fill;
            Images_pass4.SetRenderer(new ImageListViewRenderers.XPRenderer());
            Images_pass4.ThumbnailSize = new Size(300, 200);

            //set up the pass five image box
            Images_pass5.Parent = TAB_PASS_5;
            Images_pass5.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            Images_pass5.Dock = DockStyle.Fill;
            Images_pass5.SetRenderer(new ImageListViewRenderers.XPRenderer());
            Images_pass5.ThumbnailSize = new Size(300, 200);

            //set up the pass extra image box
            Images_extra.Parent = TAB_EXTRA;
            Images_extra.Anchor = (AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            Images_extra.Dock = DockStyle.Fill;
            Images_extra.SetRenderer(new ImageListViewRenderers.XPRenderer());
            Images_extra.ThumbnailSize = new Size(300, 200);

            Images_pass1.ContextMenu = new ContextMenu();
            Images_pass2.ContextMenu = new ContextMenu();
            Images_pass3.ContextMenu = new ContextMenu();
            Images_pass4.ContextMenu = new ContextMenu();
            Images_pass5.ContextMenu = new ContextMenu();
            Images_extra.ContextMenu = new ContextMenu();

            MenuItem ReCrop = new MenuItem("Re-Crop ...");
            MenuItem LeftReCrop = new MenuItem("Left");
            MenuItem RightReCrop = new MenuItem("Right");

            LeftReCrop.MenuItems.Add("-1500");
            LeftReCrop.MenuItems.Add("-1000");
            LeftReCrop.MenuItems.Add("-500");
            LeftReCrop.MenuItems.Add("0");
            LeftReCrop.MenuItems.Add("500");
            LeftReCrop.MenuItems.Add("1000");
            LeftReCrop.MenuItems.Add("1500");

            RightReCrop.MenuItems.Add("-1500");
            RightReCrop.MenuItems.Add("-1000");
            RightReCrop.MenuItems.Add("-500");
            RightReCrop.MenuItems.Add("0");
            RightReCrop.MenuItems.Add("500");
            RightReCrop.MenuItems.Add("1000");
            RightReCrop.MenuItems.Add("1500");

            ReCrop.MenuItems.Add(RightReCrop);
            ReCrop.MenuItems.Add(LeftReCrop);

            //RightReCrop.Select += new EventHandler(MoveImageToPass);

            foreach (MenuItem Item in LeftReCrop.MenuItems) Item.Click += CropImageLeft;
            foreach (MenuItem Item in RightReCrop.MenuItems) Item.Click += CropImageRight;

            MenuItem MoveImages = new MenuItem("Move to ...");
            MoveImages.MenuItems.Add(new MenuItem("1"));
            MoveImages.MenuItems.Add(new MenuItem("2"));
            MoveImages.MenuItems.Add(new MenuItem("3"));
            MoveImages.MenuItems.Add(new MenuItem("4"));
            MoveImages.MenuItems.Add(new MenuItem("5"));

            foreach (MenuItem Item in MoveImages.MenuItems) Item.Click += MoveImageToPass;

            MenuItem Remove = new MenuItem("Remove");
            Remove.Click += MoveImageToExtra;


            MenuItem AnvancedMenu = new MenuItem("Advanced");
            MenuItem RemappALTS = new MenuItem("Re-map altitudes");
            MenuItem RemappALT = new MenuItem("Change altitude");
            AnvancedMenu.MenuItems.Add(RemappALTS);
            AnvancedMenu.MenuItems.Add(RemappALT);
            RemappALTS.Click += RemapAltDialog;
            RemappALT.Click += RemapSingleAltDialog;
        
            Images_extra.ContextMenu.MenuItems.Add(MoveImages);
            //Images_extra.ContextMenu.MenuItems.Add(ReCrop);
            //Images_extra.ContextMenu.MenuItems.Add("Remove");

            Images_pass1.SelectionChanged += new EventHandler(ShowAltinLowerLeftCorner);
            Images_pass2.SelectionChanged += new EventHandler(ShowAltinLowerLeftCorner);
            Images_pass3.SelectionChanged += new EventHandler(ShowAltinLowerLeftCorner);
            Images_pass4.SelectionChanged += new EventHandler(ShowAltinLowerLeftCorner);
            Images_pass5.SelectionChanged += new EventHandler(ShowAltinLowerLeftCorner);
            Images_extra.SelectionChanged += new EventHandler(ShowAltinLowerLeftCorner);

            Images_pass1.ContextMenu.MenuItems.Add(MoveImages.CloneMenu());
            Images_pass1.ContextMenu.MenuItems.Add(ReCrop);
            Images_pass1.ContextMenu.MenuItems.Add(Remove);
            Images_pass1.ContextMenu.MenuItems.Add(AnvancedMenu);

            Images_pass2.ContextMenu.MenuItems.Add(MoveImages.CloneMenu());
            Images_pass2.ContextMenu.MenuItems.Add(ReCrop.CloneMenu());
            Images_pass2.ContextMenu.MenuItems.Add(Remove.CloneMenu());
            Images_pass2.ContextMenu.MenuItems.Add(AnvancedMenu.CloneMenu());

            Images_pass3.ContextMenu.MenuItems.Add(MoveImages.CloneMenu());
            Images_pass3.ContextMenu.MenuItems.Add(ReCrop.CloneMenu());
            Images_pass3.ContextMenu.MenuItems.Add(Remove.CloneMenu());
            Images_pass3.ContextMenu.MenuItems.Add(AnvancedMenu.CloneMenu());

            Images_pass4.ContextMenu.MenuItems.Add(MoveImages.CloneMenu());
            Images_pass4.ContextMenu.MenuItems.Add(ReCrop.CloneMenu());
            Images_pass4.ContextMenu.MenuItems.Add(Remove.CloneMenu());
            Images_pass4.ContextMenu.MenuItems.Add(AnvancedMenu.CloneMenu());

            Images_pass5.ContextMenu.MenuItems.Add(MoveImages.CloneMenu());
            Images_pass5.ContextMenu.MenuItems.Add(ReCrop.CloneMenu());
            Images_pass5.ContextMenu.MenuItems.Add(Remove.CloneMenu());
            Images_pass5.ContextMenu.MenuItems.Add(AnvancedMenu.CloneMenu());

        }
        public void populatePassImages() {
            try{
            foreach (ImageLocationAndExtraInfo imgInfo in ImageGroup.FullImageList)
            {
                if (imgInfo.selected)
                {
                    ImageLocationAndExtraInfo tmpInfo = SaveGrayedOutImage(imgInfo, imgInfo.LeftCrop, imgInfo.RightCrop);
                    imgInfo.PathToGreyImage = tmpInfo.PathToGreyImage;
                    switch (imgInfo.Type)
                    {
                        case ImageLocationType.Pass1:
                            Images_pass1.Items.Add(imgInfo.PathToGreyImage);
                            break;
                        case ImageLocationType.Pass2:
                            Images_pass2.Items.Add(imgInfo.PathToGreyImage);
                            break;
                        case ImageLocationType.Pass3:
                            Images_pass3.Items.Add(imgInfo.PathToGreyImage);
                            break;
                        case ImageLocationType.Pass4:
                            Images_pass4.Items.Add(imgInfo.PathToGreyImage);
                            break;
                        case ImageLocationType.Pass5:
                            Images_pass5.Items.Add(imgInfo.PathToGreyImage);
                            break;
                    }

                }
                else
                {
                    Images_extra.Items.Add(imgInfo.PathToSmallImage);
                }
            }
            SortPassImagesbyName(Images_pass1);
            SortPassImagesbyName(Images_pass2);
            SortPassImagesbyName(Images_pass3);
            SortPassImagesbyName(Images_pass4);
            SortPassImagesbyName(Images_pass5);
                   

        }
            catch (Exception ER)
            {
                this.ParentForm.AppendLogTextBox("FAILED to populate images into pass tabs \n***ERROR***\n" + ER.Message);
            }
            
        }

        private ImageLocationAndExtraInfo SaveGrayedOutImage(ImageLocationAndExtraInfo imageInfo, int LeftCrop, int RightCrop)
        {
            Image<Bgr, Byte> GrayedOutImage = new Image<Bgr, Byte>(imageInfo.PathToSmallImage);

            //transform to HSV
            Image<Hsv, Byte> HSVCropImage = GrayedOutImage.Convert<Hsv, Byte>();

            // check boundaries

            // for each pixel in the graayed out region set the saturation to 20
            // divide the value by 3 
            if ((LeftCrop > 0) && (LeftCrop < RightCrop))
            {
                //convert pixel to grayed out pixels
                for (int i = 0; i < LeftCrop; i++)
                {
                    for (int j = 0; j < HSVCropImage.Height; j++)
                    {

                        HSVCropImage.Data[j, i, 1] = 20;

                        Byte val = HSVCropImage.Data[j, i, 2];
                        val = (Byte)((int)val / 3);
                        HSVCropImage.Data[j, i, 2] = val;
                    }
                }
            }
            if ((RightCrop < GrayedOutImage.Width) && (LeftCrop < RightCrop))
            {
                //convert pixel to grayed out pixels
                for (int i = RightCrop; i < GrayedOutImage.Width; i++)
                {
                    for (int j = 0; j < HSVCropImage.Height; j++)
                    {
                        GrayedOutImage.Data[j, i, 1] = 20;

                        Byte val = HSVCropImage.Data[j, i, 2];
                        val = (Byte)((int)val / 3);
                        HSVCropImage.Data[j, i, 2] = val;
                    }
                }
            }


            //transform back to BGR
            GrayedOutImage = HSVCropImage.Convert<Bgr, Byte>();
            if (!Directory.Exists(Path.GetDirectoryName(imageInfo.PathToGreyImage)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(imageInfo.PathToGreyImage));
            }

            CvInvoke.PutText(GrayedOutImage, imageInfo.Altitude.ToString("G4"), new System.Drawing.Point(GrayedOutImage.Width-100, 35), FontFace.HersheyComplex, 1.0, new Bgr(0, 255, 0).MCvScalar);
            GrayedOutImage.Save(imageInfo.PathToGreyImage);
            return imageInfo;
        }

        private void CropImageRight(object sender, EventArgs e)
        {
            try{
            MenuItem MI = (MenuItem)sender;

            ImageListView CurrentView = Images_extra;

            switch (TAB_CONTROL_TURBINE.SelectedTab.Name) { 
                case "TAB_PASS_1":
                    CurrentView = Images_pass1;
                    break;
                case "TAB_PASS_2":
                    CurrentView = Images_pass2;
                    break;
                case "TAB_PASS_3":
                    CurrentView = Images_pass3;
                    break;
                case "TAB_PASS_4":
                    CurrentView = Images_pass4;
                    break;
                case "TAB_PASS_5":
                    CurrentView = Images_pass5;
                    break;
                case "TAB_EXTRA":
                    CurrentView = Images_extra;
                    break;
            
            }
            foreach (ImageListViewItem im in CurrentView.SelectedItems)
            {
                ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(im.FileName)));
            
                tmpinfo.RightCrop = tmpinfo.RightCrop + int.Parse(MI.Text) / 10; ;
                if (tmpinfo.RightCrop >= 800)
                {
                    tmpinfo.RightCrop = 799;
                }
                else if (tmpinfo.RightCrop <= tmpinfo.LeftCrop)
                {
                    tmpinfo.RightCrop = tmpinfo.LeftCrop;
                }
                
                tmpinfo = SaveGrayedOutImage(tmpinfo,tmpinfo.LeftCrop,tmpinfo.RightCrop);
                im.Update();

            }
            SaveProgress();
        }
            catch (Exception ER)
            {
                this.ParentForm.AppendLogTextBox("FAILED to recrop images \n***ERROR***\n" + ER.Message);
            }

        }

        private void CropImageLeft(object sender, EventArgs e)
        {
            try
            {
                MenuItem MI = (MenuItem)sender;

                ImageListView CurrentView = Images_extra;

                switch (TAB_CONTROL_TURBINE.SelectedTab.Name)
                {

                    case "TAB_PASS_1":
                        CurrentView = Images_pass1;
                        break;
                    case "TAB_PASS_2":
                        CurrentView = Images_pass2;
                        break;
                    case "TAB_PASS_3":
                        CurrentView = Images_pass3;
                        break;
                    case "TAB_PASS_4":
                        CurrentView = Images_pass4;
                        break;
                    case "TAB_PASS_5":
                        CurrentView = Images_pass5;
                        break;
                    case "TAB_EXTRA":
                        CurrentView = Images_extra;
                        break;
                }

                foreach (ImageListViewItem im in CurrentView.SelectedItems)
                {

                    ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(im.FileName)));

                    tmpinfo.LeftCrop = tmpinfo.LeftCrop + int.Parse(MI.Text) / 10; ;
                    if (tmpinfo.LeftCrop >= tmpinfo.RightCrop)
                    {
                        tmpinfo.LeftCrop = tmpinfo.RightCrop - 1;
                    }
                    else if (tmpinfo.LeftCrop < 0)
                    {
                        tmpinfo.LeftCrop = 0;
                    }


                    tmpinfo = SaveGrayedOutImage(tmpinfo, tmpinfo.LeftCrop, tmpinfo.RightCrop);
                    im.Update();

                }
                SaveProgress();
            }
            catch (Exception ER)
            {
                this.ParentForm.AppendLogTextBox("FAILED to recrop images \n***ERROR***\n" + ER.Message);
            }
        }

        private void MoveImageToPass(object sender, EventArgs e) {
            try
            {
                MenuItem MI = (MenuItem)sender;

                ImageListView CurrentView = Images_extra;

                switch (TAB_CONTROL_TURBINE.SelectedTab.Name)
                {

                    case "TAB_PASS_1":
                        CurrentView = Images_pass1;
                        break;
                    case "TAB_PASS_2":
                        CurrentView = Images_pass2;
                        break;
                    case "TAB_PASS_3":
                        CurrentView = Images_pass3;
                        break;
                    case "TAB_PASS_4":
                        CurrentView = Images_pass4;
                        break;
                    case "TAB_PASS_5":
                        CurrentView = Images_pass5;
                        break;
                    case "TAB_EXTRA":
                        CurrentView = Images_extra;
                        break;

                }

                foreach (ImageListViewItem im in CurrentView.SelectedItems)
                {
                    ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(im.FileName)));

                    tmpinfo.selected = true;

                    SaveGrayedOutImage(tmpinfo, tmpinfo.LeftCrop, tmpinfo.RightCrop);
                    switch (MI.Text)
                    {
                        case "1":
                            tmpinfo.Type = ImageLocationType.Pass1;
                            Images_pass1.Items.Add(tmpinfo.PathToGreyImage);
                            SortPassImagesbyName(Images_pass1);
                            break;
                        case "2":
                            tmpinfo.Type = ImageLocationType.Pass2;
                            Images_pass2.Items.Add(tmpinfo.PathToGreyImage);
                            SortPassImagesbyName(Images_pass2);
                            break;
                        case "3":
                            tmpinfo.Type = ImageLocationType.Pass3;
                            Images_pass3.Items.Add(tmpinfo.PathToGreyImage);
                            SortPassImagesbyName(Images_pass3);
                            break;
                        case "4":
                            tmpinfo.Type = ImageLocationType.Pass4;
                            Images_pass4.Items.Add(tmpinfo.PathToGreyImage);
                            SortPassImagesbyName(Images_pass4);
                            break;
                        case "5":
                            tmpinfo.Type = ImageLocationType.Pass5;
                            Images_pass5.Items.Add(tmpinfo.PathToGreyImage);
                            SortPassImagesbyName(Images_pass5);
                            break;
                    }
                    CurrentView.Items.Remove(im);
                }
                SaveProgress();
            }
            catch (Exception ER) {
                this.ParentForm.AppendLogTextBox("FAILED while moving images to pass tab " + this.ImageGroup.BaseDirectory + "\n***ERROR***\n" + ER.Message);
            }
        
        }
        private void MoveImageToExtra(object sender, EventArgs e)
        {
            try
            {
                MenuItem MI = (MenuItem)sender;

                ImageListView CurrentView = Images_extra;

                switch (TAB_CONTROL_TURBINE.SelectedTab.Name)
                {

                    case "TAB_PASS_1":
                        CurrentView = Images_pass1;
                        break;
                    case "TAB_PASS_2":
                        CurrentView = Images_pass2;
                        break;
                    case "TAB_PASS_3":
                        CurrentView = Images_pass3;
                        break;
                    case "TAB_PASS_4":
                        CurrentView = Images_pass4;
                        break;
                    case "TAB_PASS_5":
                        CurrentView = Images_pass5;
                        break;
                    case "TAB_EXTRA":
                        CurrentView = Images_extra;
                        break;

                }

                foreach (ImageListViewItem im in CurrentView.SelectedItems)
                {
                    ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(im.FileName)));

                    tmpinfo.selected = false;
                    Images_extra.Items.Add(tmpinfo.PathToSmallImage);
                    SortPassImagesbyName(Images_extra);
                    CurrentView.Items.Remove(im);

                }
                SaveProgress();
            }
            catch (Exception ER) { this.ParentForm.AppendLogTextBox("FAILED to move images to extra tab" + this.ImageGroup.BaseDirectory + "\n***ERROR***\n" + ER.Message); }
        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }

        private void BUT_RELEASE_Click(object sender, EventArgs e)
        {
            try
            {
                SaveProgress();

                
                //ImageReleaseForm ReleaseForm = new ImageReleaseForm(,,)

                //open release form input info
                //if it returns with and "ok" we can create the upload file
                //if not cancle


                ReleaseThread = new Thread(ReleaseImages);
                ReleaseThread.Start();
                //run clean up here
                this.ParentForm.removeTurbineTabfrom(this.ImageGroup.BaseDirectory);
                this.ParentForm.EnableDisableButton((Button)this.ParentForm.ATable.Table.GetControlFromPosition(5, row),false,Color.Yellow,"Reviewed");

            }
            catch(Exception error) {
                this.ParentForm.AppendLogTextBox("FAILED while starting post processing thread for " + this.ImageGroup.BaseDirectory + "\n***ERROR***\n" + error.Message);
            }
        }
        private void ReleaseImages() {
            try
            {
                
                this.ParentForm.AppendLogTextBox("\n\nThread to process" + this.ImageGroup.BaseDirectory + " added to que\n" );
                



                this.ParentForm.PostThreads.Add( new Thread(() => this.ParentForm.GeotagimagesAndCrop(ImageGroup, this.PostProccessProgresBar,this.row)));
            }
            catch (Exception e) {
                this.ParentForm.AppendLogTextBox("FAILED while waiting to start post processing thread for " + this.ImageGroup.BaseDirectory + "\n***ERROR***\n" + e.Message );
            }

        }

        private void BUTT_LL_Click(object sender, EventArgs e)
        {
            try
            {
                ImageListView CurrentView = Images_extra;

                switch (TAB_CONTROL_TURBINE.SelectedTab.Name)
                {

                    case "TAB_PASS_1":
                        CurrentView = Images_pass1;
                        break;
                    case "TAB_PASS_2":
                        CurrentView = Images_pass2;
                        break;
                    case "TAB_PASS_3":
                        CurrentView = Images_pass3;
                        break;
                    case "TAB_PASS_4":
                        CurrentView = Images_pass4;
                        break;
                    case "TAB_PASS_5":
                        CurrentView = Images_pass5;
                        break;
                    case "TAB_EXTRA":
                        CurrentView = Images_extra;
                        break;
                }

                foreach (ImageListViewItem im in CurrentView.SelectedItems)
                {

                    ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(im.FileName)));

                    tmpinfo.LeftCrop = tmpinfo.LeftCrop - 50; ;
                    if (tmpinfo.LeftCrop >= tmpinfo.RightCrop)
                    {
                        tmpinfo.LeftCrop = tmpinfo.RightCrop - 1;
                    }
                    else if (tmpinfo.LeftCrop < 0)
                    {
                        tmpinfo.LeftCrop = 0;
                    }


                    tmpinfo = SaveGrayedOutImage(tmpinfo, tmpinfo.LeftCrop, tmpinfo.RightCrop);
                    im.Update();

                }
                SaveProgress();
            }
            catch (Exception error) {
                this.ParentForm.AppendLogTextBox("\n\n****ERROR***\n"+error.Message);
            }
        }

        private void BUTT_LR_Click(object sender, EventArgs e)
        {
            try{
            ImageListView CurrentView = Images_extra;

            switch (TAB_CONTROL_TURBINE.SelectedTab.Name)
            {

                case "TAB_PASS_1":
                    CurrentView = Images_pass1;
                    break;
                case "TAB_PASS_2":
                    CurrentView = Images_pass2;
                    break;
                case "TAB_PASS_3":
                    CurrentView = Images_pass3;
                    break;
                case "TAB_PASS_4":
                    CurrentView = Images_pass4;
                    break;
                case "TAB_PASS_5":
                    CurrentView = Images_pass5;
                    break;
                case "TAB_EXTRA":
                    CurrentView = Images_extra;
                    break;
            }

            foreach (ImageListViewItem im in CurrentView.SelectedItems)
            {

                ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(im.FileName)));

                tmpinfo.LeftCrop = tmpinfo.LeftCrop + 50; ;
                if (tmpinfo.LeftCrop >= tmpinfo.RightCrop)
                {
                    tmpinfo.LeftCrop = tmpinfo.RightCrop - 1;
                }
                else if (tmpinfo.LeftCrop < 0)
                {
                    tmpinfo.LeftCrop = 0;
                }


                tmpinfo = SaveGrayedOutImage(tmpinfo, tmpinfo.LeftCrop, tmpinfo.RightCrop);
                im.Update();

            }
            SaveProgress();
        }
            catch (Exception error) {
                this.ParentForm.AppendLogTextBox("\n\n****ERROR***\n"+error.Message);
            }
        }

        private void BUTT_RL_Click(object sender, EventArgs e)
        {
            try{
            ImageListView CurrentView = Images_extra;

            switch (TAB_CONTROL_TURBINE.SelectedTab.Name)
            {
                case "TAB_PASS_1":
                    CurrentView = Images_pass1;
                    break;
                case "TAB_PASS_2":
                    CurrentView = Images_pass2;
                    break;
                case "TAB_PASS_3":
                    CurrentView = Images_pass3;
                    break;
                case "TAB_PASS_4":
                    CurrentView = Images_pass4;
                    break;
                case "TAB_PASS_5":
                    CurrentView = Images_pass5;
                    break;
                case "TAB_EXTRA":
                    CurrentView = Images_extra;
                    break;

            }
            foreach (ImageListViewItem im in CurrentView.SelectedItems)
            {
                ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(im.FileName)));

                tmpinfo.RightCrop = tmpinfo.RightCrop -50; ;
                if (tmpinfo.RightCrop >= 800)
                {
                    tmpinfo.RightCrop = 799;
                }
                else if (tmpinfo.RightCrop <= tmpinfo.LeftCrop)
                {
                    tmpinfo.RightCrop = tmpinfo.LeftCrop;
                }

                tmpinfo = SaveGrayedOutImage(tmpinfo, tmpinfo.LeftCrop, tmpinfo.RightCrop);
                im.Update();

            }
            SaveProgress();
        }
            catch (Exception error) {
                this.ParentForm.AppendLogTextBox("\n\n****ERROR***\n"+error.Message);
            }
        }

        private void BUTT_RR_Click(object sender, EventArgs e)
        {
            try{
            ImageListView CurrentView = Images_extra;

            switch (TAB_CONTROL_TURBINE.SelectedTab.Name)
            {
                case "TAB_PASS_1":
                    CurrentView = Images_pass1;
                    break;
                case "TAB_PASS_2":
                    CurrentView = Images_pass2;
                    break;
                case "TAB_PASS_3":
                    CurrentView = Images_pass3;
                    break;
                case "TAB_PASS_4":
                    CurrentView = Images_pass4;
                    break;
                case "TAB_PASS_5":
                    CurrentView = Images_pass5;
                    break;
                case "TAB_EXTRA":
                    CurrentView = Images_extra;
                    break;

            }
            foreach (ImageListViewItem im in CurrentView.SelectedItems)
            {
                ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(im.FileName)));

                tmpinfo.RightCrop = tmpinfo.RightCrop + 50 ;
                if (tmpinfo.RightCrop >= 795)
                {
                    tmpinfo.RightCrop = 794;
                }
                else if (tmpinfo.RightCrop <= tmpinfo.LeftCrop)
                {
                    tmpinfo.RightCrop = tmpinfo.LeftCrop;
                }

                tmpinfo = SaveGrayedOutImage(tmpinfo, tmpinfo.LeftCrop, tmpinfo.RightCrop);
                im.Update();

            }
            SaveProgress();
        }
            catch (Exception error) {
                this.ParentForm.AppendLogTextBox("\n\n****ERROR***\n"+error.Message);
            }
        }
        private void SortPassImagesbyHeight(ImageListView ImageView) {
            //CreateComposite(ImageView);
            List<ImageListViewItem> Items = ImageView.Items.ToList();
            //sort by altitude
            List<ImageListViewItem> SortedList = Items.OrderBy(o =>  -this.ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(o.FileName))).Altitude).ToList();
            ImageView.Items.Clear();
            foreach(ImageListViewItem tmpitem in SortedList){
                ImageView.Items.Add(tmpitem.FileName);
            } 
        
        }
        private void SortPassImagesbyName(ImageListView ImageView)
        {

            List<ImageListViewItem> Items = ImageView.Items.ToList();
            //sort by altitude
            List<ImageListViewItem> SortedList = Items.OrderBy(o => o.FileName).ToList();
            ImageView.Items.Clear();
            foreach (ImageListViewItem tmpitem in SortedList)
            {
                ImageView.Items.Add(tmpitem.FileName);
            }

        }


        private void ShowAltinLowerLeftCorner(object sender, EventArgs e) {

            ImageListView ImageView = (ImageListView)sender;
            if (ImageView.SelectedItems.Count > 0) {
                ALT_TEXT_HELP.Text = "Altitude:"+this.ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(ImageView.SelectedItems[0].FileName))).Altitude.ToString("G4");
            }
        
        }
        private void CreateComposite(ImageListView view) {
            int index = 0;
            Image<Rgb, Byte> initImage = new Image<Rgb, Byte>(view.Items[0].FileName);
            Image<Rgb, Byte> compositeImage = new Image<Rgb, Byte>((int)initImage.Width, (int)(initImage.Height * view.Items.Count));
            foreach (ImageListViewItem Image in view.Items) {
                Image<Rgb, Byte> tmpImage = new Image<Rgb, Byte>(Image.FileName);
                
                for (int i = 0; i < tmpImage.Height; i++)
                {
                    for (int j = 0; j < tmpImage.Width; j++)
                    {

                        compositeImage.Data[(int)(index*tmpImage.Height)+i, j, 0] = tmpImage.Data[i, j, 0];
                        compositeImage.Data[(int)(index*tmpImage.Height)+i, j, 1] = tmpImage.Data[i, j, 1];
                        compositeImage.Data[(int)(index*tmpImage.Height)+i, j, 2] = tmpImage.Data[i, j, 2];
        
                    }
                }
                index = index +1;
           
            }
            //IMAGE_COMPOSITE.Image = compositeImage.ToBitmap();
            //IMAGE_COMPOSITE.SizeMode = PictureBoxSizeMode.Normal;

        }

        private void RemapAltDialog(object sender, EventArgs e)
        {

              ImageListView CurrentView = Images_extra;

            switch (TAB_CONTROL_TURBINE.SelectedTab.Name) { 
                case "TAB_PASS_1":
                    CurrentView = Images_pass1;
                    break;
                case "TAB_PASS_2":
                    CurrentView = Images_pass2;
                    break;
                case "TAB_PASS_3":
                    CurrentView = Images_pass3;
                    break;
                case "TAB_PASS_4":
                    CurrentView = Images_pass4;
                    break;
                case "TAB_PASS_5":
                    CurrentView = Images_pass5;
                    break;
                case "TAB_EXTRA":
                    CurrentView = Images_extra;
                    break;
            
            }

            using (var form = new AltRemapDialog())
            {
                //determine which view is open
                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    float start = form.StartValue;            //values preserved after close
                    float end = form.EndValue;

                    RemapAltsforPass(CurrentView, start, end);

                }
            }

            SaveProgress();
        }

        private void RemapSingleAltDialog(object sender, EventArgs e)
        {

            ImageListView CurrentView = Images_extra;

            switch (TAB_CONTROL_TURBINE.SelectedTab.Name)
            {
                case "TAB_PASS_1":
                    CurrentView = Images_pass1;
                    break;
                case "TAB_PASS_2":
                    CurrentView = Images_pass2;
                    break;
                case "TAB_PASS_3":
                    CurrentView = Images_pass3;
                    break;
                case "TAB_PASS_4":
                    CurrentView = Images_pass4;
                    break;
                case "TAB_PASS_5":
                    CurrentView = Images_pass5;
                    break;
                case "TAB_EXTRA":
                    CurrentView = Images_extra;
                    break;

            }

            using (var form = new AltRemapSingleDialog())
            {
                //determine which view is open
                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    float newalt = form.newValue;

                    foreach (ImageListViewItem Item in CurrentView.SelectedItems)
                    {
                        ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(Item.FileName)));
                        tmpinfo.Altitude = newalt;
                        tmpinfo = SaveGrayedOutImage(tmpinfo, tmpinfo.LeftCrop, tmpinfo.RightCrop);
                        Item.Update();
                    }
                }
            }

            SaveProgress();
        }

        private void RemapAltsforPass(ImageListView view, float start, float end) { 
        
        if (!(view.Items.Count>0)){
            return;
        }
        float count = view.Items.Count;
        float step = (end - start)/count;
        float alt = start;
        foreach (ImageListViewItem Item in view.Items) {

            ImageLocationAndExtraInfo tmpinfo = ImageGroup.FullImageList.Find(x => x.PathToSmallImage.Contains(Path.GetFileName(Item.FileName)));

            tmpinfo.Altitude = alt;
            tmpinfo = SaveGrayedOutImage(tmpinfo, tmpinfo.LeftCrop, tmpinfo.RightCrop);
            Item.Update();

            alt = alt + step;
        }
        
        
        
        }

        private void LoadProgress()
        {
            ImageBladeGroup LastSavedData = new ImageBladeGroup();
            System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(ImageBladeGroup));
            System.IO.StreamReader file = new System.IO.StreamReader(PATH_TO_SAVED_PROG_FILE);
            this.ImageGroup = (ImageBladeGroup)reader.Deserialize(file);
            file.Close();
        }

        private void SaveProgress()
        {
            System.Xml.Serialization.XmlSerializer CameraWriter = new System.Xml.Serialization.XmlSerializer(typeof(ImageBladeGroup));
            System.IO.FileStream wfile = System.IO.File.Create(PATH_TO_SAVED_PROG_FILE);
            CameraWriter.Serialize(wfile, this.ImageGroup);
            wfile.Close();
        }
        private void LABEL_PATH_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
        
    }
}
