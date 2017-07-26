using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util.TypeEnum;
using Emgu.CV.Shape;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using com.drew.metadata;
using com.drew.imaging.jpg;


namespace ITGeoTagger
{
    class Cropper : IDisposable
    {

    public Cropper() { }

    public void Dispose() {     
    }


    public void process(BladeCroppingSettings settings, string inputFile, string outputFile)
    {

        Console.WriteLine("Starting Crop on " + inputFile);
        GC.Collect();
        int SmallerPhotos = settings.getScaleDown(); // save large photos or small. 0 is 10X smaller


        int smoother = 5; //filter value
        float borderfrac = 6; // what fraction of the image is added border

        float scalefactor = 10; //times smaller processed image is

        if (File.Exists(inputFile))
        {
            //System.out.println("Image" + inputFile.getName());
            String imagepath = inputFile;
            Image<Bgr, Byte> image = new Image<Bgr, byte>(imagepath);

            //get image size
            double height = image.Height;// get image dimensions
            double width = image.Width;

            int Rewidth = (int)(width / scalefactor);
            int Reheight = (int)(height / scalefactor);

            //create scaled version of the image
            Image<Bgr, Byte> resizedimage = new Image<Bgr, byte>(Rewidth,Reheight);
            //resize the image for processing

            resizedimage = image.Resize(Rewidth, Reheight,Inter.Linear);

            bool DarkGround = GetGroundDark(resizedimage);// return if the ground is darker or lighter than the blade


            Image<Gray, Byte> thresh_special = SingleLineTurbineFinder(resizedimage, DarkGround);

            int Flag_Special = 0;
            int seq_Special = 0;

            int ThreshVal = 3;

            //image border variables
            int borderLeft = (int)(Rewidth / borderfrac);
            int borderRight = (int)(Rewidth / borderfrac);

            int RightPixel = (int)(Rewidth - borderRight - Rewidth / 8);
            int LeftPixel = (int)(borderLeft + Rewidth / 8);

            int RightPixel_Special = (int)(Rewidth / 2);
            int LeftPixel_Special = (int)(Rewidth / 2 - 1);
            while ((seq_Special < smoother))
            {

                if ((thresh_special[0, LeftPixel].Intensity < (Byte)ThreshVal) && (Flag_Special == 0))
                {
                    seq_Special += 1;
                    if (seq_Special >= smoother)
                    {
                        LeftPixel_Special = LeftPixel;
                        Flag_Special = 1;
                    }
                }
                else if (Flag_Special == 0)
                {
                    seq_Special = 0;
                }
                LeftPixel += 1;

                if (LeftPixel >= width / scalefactor / 2)
                {
                    break;
                }
            }
            //////////////////////////////////////////////////////////RightSide
            Flag_Special = 0;
            seq_Special = 0;

            while ((seq_Special < smoother))
            {

                if ((thresh_special[0, RightPixel].Intensity < ThreshVal) && (Flag_Special == 0))
                {
                    seq_Special += 1;
                    if (seq_Special >= smoother)
                    {
                        RightPixel_Special = RightPixel;
                        Flag_Special = 1;
                    }
                }
                else if (Flag_Special == 0)
                {
                    seq_Special = 0;
                }

                //////////////////////////////
                RightPixel -= 1;

                if (RightPixel <= width / scalefactor / 2)
                {
                    break;
                }
            }
            ////////this section checks for side cases and towers

            if (Math.Abs(LeftPixel_Special - (borderLeft + width / scalefactor / 8)) < 20)
            {
                LeftPixel_Special = (int)(width / scalefactor / 3);
                int tmpval = (int)(thresh_special[(int)(0), (int)(LeftPixel_Special - borderLeft)].Intensity);
                while (tmpval < ThreshVal)
                {
                    tmpval = (int)(thresh_special[(int)(0), (int)(LeftPixel_Special - borderLeft)].Intensity);
                    LeftPixel_Special -= 1;
                    if ((thresh_special[(int)(0), (int)(LeftPixel_Special - borderLeft)].Intensity) > ThreshVal)
                    {
                        break;
                    }
                    else if (LeftPixel_Special <= borderLeft)
                    {
                        LeftPixel_Special = borderLeft + 1;
                        break;
                    }
                }
            }
            if (Math.Abs(RightPixel_Special - width / scalefactor - (borderRight + width / scalefactor / 8)) < 20)
            {
                RightPixel_Special = (int)(2 * width / scalefactor / 3);
                int tmpval = (int)(thresh_special[(int)(0), (int)(RightPixel_Special + borderRight)].Intensity);
                while ((int)(thresh_special[(int)(0), (int)(RightPixel_Special)].Intensity) < ThreshVal)
                {
                    tmpval = (int)(thresh_special[(int)(0), (int)(RightPixel_Special + borderRight)].Intensity);
                    RightPixel_Special += 1;
                    if ((int)(thresh_special[(int)(0), (int)(RightPixel_Special + borderRight)].Intensity) > ThreshVal)
                    {
                        break;
                    }
                    else if (RightPixel_Special > (width / scalefactor) - 1)
                    {
                        RightPixel_Special = (int)(width / scalefactor - borderRight - 1);
                        break;
                    }
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (LeftPixel_Special <= borderLeft)
            {
                LeftPixel_Special = borderLeft;
            }
            if (RightPixel_Special >= width / scalefactor - 1 - borderRight)
            {
                RightPixel_Special = (int)(width / scalefactor - 1 - borderRight);
            }

            double angle  = 0;
            //var task = Task.Factory.StartNew(() => getSkewAngle(resizedimage));
            //if (task.Wait(TimeSpan.FromSeconds(10))) {
            //    angle = task.Result;
            //    Console.WriteLine("got angle"); }
            //else
            //{
            //    angle = 0;
            //}

            //adds extra border on trailing edge side
            //if (angle < 0)
            //{
            //    borderRight = borderRight * 2;
            //}
            //else
            //{
            //    borderLeft = borderLeft * 2;
            //}

            if (LeftPixel_Special <= borderLeft)
            {
                LeftPixel_Special = borderLeft;
            }
            if (RightPixel_Special >= resizedimage.Width - 1 - borderRight)
            {
                RightPixel_Special = (int)(resizedimage.Width - 1 - borderRight);
            }
            Rectangle rectCrop = new Rectangle((int)(LeftPixel_Special - borderLeft), 0, (int)(RightPixel_Special + borderRight + borderLeft - LeftPixel_Special), (int)(resizedimage.Height));

            Image<Bgr, Byte> croppedImage;

            rectCrop.X = (int)(LeftPixel - borderLeft);
            rectCrop.Width = (int)(RightPixel + borderRight + borderLeft - LeftPixel);

            croppedImage = resizedimage.GetSubRect(rectCrop);

            int rows = croppedImage.Rows;
            int cols = croppedImage.Cols;
            int ch = croppedImage.NumberOfChannels;
            Mat M = GetSkewTransform(resizedimage,angle, LeftPixel_Special, RightPixel_Special, borderLeft, borderRight);
            Image<Bgr, Byte> outimage;
            Image<Hsv, Byte> touchedim;
            //if we are saving the large or compressed image
            if (SmallerPhotos == 0)
            {
                Image<Bgr, Byte> tmpimage = new Image<Bgr, Byte>((int)width, (int)height);
                outimage = image;
                CvInvoke.WarpAffine(outimage, tmpimage, M, new Size((int)width, (int)height));
                rectCrop = new Rectangle((int)((LeftPixel_Special - borderLeft) * scalefactor), 0, (int)((RightPixel_Special + borderRight + borderLeft - LeftPixel_Special) * scalefactor), (int)(height));
                outimage.Dispose();
                Image<Bgr, Byte> CroppedImg = tmpimage.GetSubRect(rectCrop);

                tmpimage.Dispose();

                touchedim = CroppedImg.Convert<Hsv, Byte>();
                CroppedImg.Dispose();
                GC.Collect();
            }
            else
            {
                Image<Bgr, Byte> tmpimage = new Image<Bgr, Byte>((int)resizedimage.Width, (int)resizedimage.Height); ;
                outimage = resizedimage;
                CvInvoke.WarpAffine(outimage, tmpimage, M, new Size((int)(width / scalefactor), (int)(height / scalefactor)));
                outimage.Dispose();
                rectCrop = new Rectangle((int)(LeftPixel_Special - borderLeft), 0, (int)(RightPixel_Special + borderRight + borderLeft - LeftPixel_Special), (int)(height / scalefactor));

                Image<Bgr, Byte> CroppedImg = tmpimage.GetSubRect(rectCrop);
                tmpimage.Dispose();
                touchedim = CroppedImg.Convert<Hsv, Byte>();
                CroppedImg.Dispose();
                GC.Collect();
            }

            Image<Bgr, Byte> touchedOutIm = touchedim.Convert<Bgr, Byte>();
            touchedim.Dispose();
            GC.Collect();
            System.IO.Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(outputFile), "[Originals]"));
            if (File.Exists(Path.Combine(Path.Combine(Path.GetDirectoryName(outputFile), "[Originals]"), Path.GetFileName(outputFile)))) { File.Delete(Path.Combine(Path.Combine(Path.GetDirectoryName(outputFile), "[Originals]"), Path.GetFileName(outputFile))); }
            System.IO.File.Move(inputFile, Path.Combine(Path.Combine(Path.GetDirectoryName(outputFile), "[Originals]"), Path.GetFileName(outputFile)));
            touchedOutIm.Save(outputFile);
            touchedOutIm.Dispose();
            GC.Collect();
            //copy the daa and create a xmp fild for acd go
            copyMetaData(Path.Combine(Path.Combine(Path.GetDirectoryName(outputFile), "[Originals]"), Path.GetFileName(outputFile)),outputFile);
            Write_ACDGO_format_file(Path.Combine(Path.Combine(Path.GetDirectoryName(outputFile), "[Originals]"), Path.GetFileName(outputFile)), rectCrop.Right, rectCrop.Left, rectCrop.Width, rectCrop.Height);
            image.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //Thread.Sleep(200);
        }
        else
        {
            //
        }
    }

    public String JustCrop(BladeCroppingSettings settings, string inputFile, string outputFile, int LeftCrop, int RightCrop)
    {
        try
        {
            Image<Bgr, Byte> ImageInput = new Image<Bgr, Byte>(inputFile);

            if (RightCrop < ImageInput.Width)
            {
                RightCrop = ImageInput.Width - 1;
            }

            Rectangle rectCrop = new Rectangle((int)(LeftCrop), 0, (int)(RightCrop - LeftCrop), ImageInput.Height);
            Image<Bgr, Byte> CroppedImg = ImageInput.GetSubRect(rectCrop);
            CroppedImg.Save(outputFile);
            ImageInput.Dispose();
            CroppedImg.Dispose();
            copyMetaData(inputFile, outputFile);
            GC.Collect();
            return "OK";
        }
        catch(Exception e) {
            return e.ToString(); 
        }
    }
    public int[] getCropValues(BladeCroppingSettings settings,Image<Bgr,Byte> image)
    {

        GC.Collect();
        int SmallerPhotos = settings.getScaleDown(); // save large photos or small. 0 is 10X smaller

        if (image.Cols < 100) {

            return new int[] {0,0};
        }

        int smoother = 5; //filter value
        float borderfrac = 6; // what fraction of the image is added border

        float scalefactor = 1; //times smaller processed image is

        //get image size
        double height = image.Height;// get image dimensions
        double width = image.Width;

        int Rewidth = (int)(width / scalefactor);
        int Reheight = (int)(height / scalefactor);

        //create scaled version of the image
        Image<Bgr, Byte> resizedimage = new Image<Bgr, byte>(Rewidth, Reheight);
        //resize the image for processing

        resizedimage = image.Resize(Rewidth, Reheight, Inter.Linear);

        bool DarkGround = GetGroundDark(resizedimage);// return if the ground is darker or lighter than the blade


        Image<Gray, Byte> thresh_special = SingleLineTurbineFinder(resizedimage, DarkGround);

        int Flag_Special = 0;
        int seq_Special = 0;

        int ThreshVal = 3;

        //image border variables
        int borderLeft = (int)(Rewidth / borderfrac);
        int borderRight = (int)(Rewidth / borderfrac);

        int RightPixel = (int)(Rewidth - borderRight - Rewidth / 8);
        int LeftPixel = (int)(borderLeft + Rewidth / 8);

        int RightPixel_Special = (int)(Rewidth / 2);
        int LeftPixel_Special = (int)(Rewidth / 2 - 1);
        while ((seq_Special < smoother))
        {

            if ((thresh_special[0, LeftPixel].Intensity < (Byte)ThreshVal) && (Flag_Special == 0))
            {
                seq_Special += 1;
                if (seq_Special >= smoother)
                {
                    LeftPixel_Special = LeftPixel;
                    Flag_Special = 1;
                }
            }
            else if (Flag_Special == 0)
            {
                seq_Special = 0;
            }
            LeftPixel += 1;

            if (LeftPixel >= width / scalefactor / 2)
            {
                break;
            }
        }
        //////////////////////////////////////////////////////////RightSide
        Flag_Special = 0;
        seq_Special = 0;

        while ((seq_Special < smoother))
        {

            if ((thresh_special[0, RightPixel].Intensity < ThreshVal) && (Flag_Special == 0))
            {
                seq_Special += 1;
                if (seq_Special >= smoother)
                {
                    RightPixel_Special = RightPixel;
                    Flag_Special = 1;
                }
            }
            else if (Flag_Special == 0)
            {
                seq_Special = 0;
            }

            //////////////////////////////
            RightPixel -= 1;

            if (RightPixel <= width / scalefactor / 2)
            {
                break;
            }
        }
        ////////this section checks for side cases and towers

        if (Math.Abs(LeftPixel_Special - (borderLeft + width / scalefactor / 8)) < 20)
        {
            LeftPixel_Special = (int)(width / scalefactor / 3);
            int tmpval = (int)(thresh_special[(int)(0), (int)(LeftPixel_Special - borderLeft)].Intensity);
            while (tmpval < ThreshVal)
            {
                tmpval = (int)(thresh_special[(int)(0), (int)(LeftPixel_Special - borderLeft)].Intensity);
                LeftPixel_Special -= 1;
                if ((thresh_special[(int)(0), (int)(LeftPixel_Special - borderLeft)].Intensity) > ThreshVal)
                {
                    break;
                }
                else if (LeftPixel_Special <= borderLeft)
                {
                    LeftPixel_Special = borderLeft + 1;
                    break;
                }
            }
        }
        if (Math.Abs(RightPixel_Special - width / scalefactor - (borderRight + width / scalefactor / 8)) < 20)
        {
            RightPixel_Special = (int)(2 * width / scalefactor / 3);
            int tmpval = (int)(thresh_special[(int)(0), (int)(RightPixel_Special + borderRight)].Intensity);
            while ((int)(thresh_special[(int)(0), (int)(RightPixel_Special)].Intensity) < ThreshVal)
            {
                tmpval = (int)(thresh_special[(int)(0), (int)(RightPixel_Special + borderRight)].Intensity);
                RightPixel_Special += 1;
                if ((int)(thresh_special[(int)(0), (int)(RightPixel_Special + borderRight)].Intensity) > ThreshVal)
                {
                    break;
                }
                else if (RightPixel_Special > (width / scalefactor) - 1)
                {
                    RightPixel_Special = (int)(width / scalefactor - borderRight - 1);
                    break;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        if (LeftPixel_Special <= borderLeft)
        {
            LeftPixel_Special = borderLeft;
        }
        if (RightPixel_Special >= width / scalefactor - 1 - borderRight)
        {
            RightPixel_Special = (int)(width / scalefactor - 1 - borderRight);
        }

        double angle = 0;
        //var task = Task.Factory.StartNew(() => getSkewAngle(resizedimage));
        //if (task.Wait(TimeSpan.FromSeconds(10))) {
        //    angle = task.Result;
        //    Console.WriteLine("got angle"); }
        //else
        //{
        //    angle = 0;
        //}

        //adds extra border on trailing edge side
        //if (angle < 0)
        //{
        //    borderRight = borderRight * 2;
        //}
        //else
        //{
        //    borderLeft = borderLeft * 2;
        //}

        if (LeftPixel_Special <= borderLeft)
        {
            LeftPixel_Special = borderLeft;
        }
        if (RightPixel_Special >= resizedimage.Width - 1 - borderRight)
        {
            RightPixel_Special = (int)(resizedimage.Width - 1 - borderRight);
        }
        Rectangle rectCrop = new Rectangle((int)(LeftPixel_Special - borderLeft), 0, (int)(RightPixel_Special + borderRight + borderLeft - LeftPixel_Special), (int)(resizedimage.Height));

        Image<Bgr, Byte> croppedImage;

        rectCrop.X = (int)(LeftPixel - borderLeft);
        rectCrop.Width = (int)(RightPixel + borderRight + borderLeft - LeftPixel);

        croppedImage = resizedimage.GetSubRect(rectCrop);

        int rows = croppedImage.Rows;
        int cols = croppedImage.Cols;
        int ch = croppedImage.NumberOfChannels;
        Mat M = GetSkewTransform(resizedimage, angle, LeftPixel_Special, RightPixel_Special, borderLeft, borderRight);
        Image<Bgr, Byte> outimage;
        Image<Hsv, Byte> touchedim;
        //if we are saving the large or compressed image
        if (SmallerPhotos == 0)
        {
            Image<Bgr, Byte> tmpimage = new Image<Bgr, Byte>((int)width, (int)height);
            outimage = image;
            CvInvoke.WarpAffine(outimage, tmpimage, M, new Size((int)width, (int)height));
            rectCrop = new Rectangle((int)((LeftPixel_Special - borderLeft) * scalefactor), 0, (int)((RightPixel_Special + borderRight + borderLeft - LeftPixel_Special) * scalefactor), (int)(height));
            outimage.Dispose();
            Image<Bgr, Byte> CroppedImg = tmpimage.GetSubRect(rectCrop);

            tmpimage.Dispose();

            touchedim = CroppedImg.Convert<Hsv, Byte>();
            CroppedImg.Dispose();
            GC.Collect();
        }
        else
        {
            Image<Bgr, Byte> tmpimage = new Image<Bgr, Byte>((int)resizedimage.Width, (int)resizedimage.Height); ;
            outimage = resizedimage;
            CvInvoke.WarpAffine(outimage, tmpimage, M, new Size((int)(width / scalefactor), (int)(height / scalefactor)));
            outimage.Dispose();
            rectCrop = new Rectangle((int)(LeftPixel_Special - borderLeft), 0, (int)(RightPixel_Special + borderRight + borderLeft - LeftPixel_Special), (int)(height / scalefactor));

            Image<Bgr, Byte> CroppedImg = tmpimage.GetSubRect(rectCrop);
            tmpimage.Dispose();
            touchedim = CroppedImg.Convert<Hsv, Byte>();
            CroppedImg.Dispose();
            GC.Collect();
        }

        Image<Bgr, Byte> touchedOutIm = touchedim.Convert<Bgr, Byte>();
        touchedim.Dispose();
        GC.Collect();
        touchedOutIm.Dispose();
        GC.Collect();
       
        image.Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        int[] cropVals = new int[]{LeftPixel_Special - borderLeft, RightPixel_Special+borderRight};

        return cropVals;

    }
    private void copyMetaData(string sourceFile, string Filename)
    {
      using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(Filename)))
      {
        File.Delete(Filename);

        Image Im = Image.FromFile(sourceFile);
        PropertyItem[] pi = Im.PropertyItems;
        using (Image Pic = Image.FromStream(ms))
        {
          foreach (PropertyItem propitem in pi)
          {
                Pic.SetPropertyItem(propitem);
          }
          if (File.Exists(Filename)) { File.Delete(Filename); }
          Pic.Save(Filename);
        }
      }
      GC.Collect();
    }
    //private double getSkewAngle(Image<Bgr, Byte> inputImage){
    //    double angleSum = 0;
    //    double anglecount = 0;
    //    double avgangle = 0;
    //    Console.WriteLine("Check Point #6z");
    //    Image<Gray, Byte> gray;

    //    Image<Gray, Byte>[] Cropped_H_S_V;

    //        Image<Hsv, Byte> imageHSV = inputImage.Convert<Hsv, Byte>();
    //        Cropped_H_S_V = imageHSV.Split();
    //        gray = Cropped_H_S_V[2];

    //        Mat edges = new Mat();

    //        //CvInvoke.Canny(gray,edges, 10, 30, (int)3,true);
    //        double minLineLength = 100;
    //        double maxLineGap = 10;

    //        UMat cannyEdges = new UMat();
            
    //        CvInvoke.Canny(gray, cannyEdges, 10, 30);
    //        Console.WriteLine("Check Point #6y");
    //        if (cannyEdges.IsEmpty){
    //            return 0;
    //        }
    //        UMat lines = new UMat();
    //        CvInvoke.HoughLines(
    //           cannyEdges,lines,
    //           1, //Distance resolution in pixel-related units
    //           Math.PI / 180, //Angle resolution measured in radians.
    //           20); //gap between lines
    //        Console.WriteLine("Check Point #6x");

    //        Console.WriteLine("Check Point #6w");
    //        for (int j = 0; j < lines.Length; j++)
    //        {

    //            double rho = lines[j].Length;
    //            PointF Dir = lines[j].Direction;
    //            double theta = Math.Atan2(Dir.Y, Dir.X);
    //            double maxconstangle = .1;
    //            if (((Math.Abs(theta) > (Math.PI / 2 - maxconstangle)) && ((Math.Abs(theta) < (Math.PI / 2 + maxconstangle)))) || ((Math.Abs(theta) > (3 * Math.PI / 2 - maxconstangle)) && ((Math.Abs(theta) < (3 * Math.PI / 2 + maxconstangle)))))
    //            {

    //                if (theta < 0)
    //                {
    //                    theta = theta + Math.PI;
    //                }
    //                anglecount += 1;
    //                angleSum += theta;
    //            }

    //        }
    //        Console.WriteLine("Check Point #6v");
    //        if (anglecount != 0)
    //        {
    //            avgangle = angleSum / anglecount * 180 / Math.PI;
    //            //System.out.print( "average angle :");
    //            //System.out.println(avgangle);
    //        }
    //        Console.WriteLine("Check Point #6u");

    //    return avgangle - 90;

    //}
    private Mat GetSkewTransform(Image<Bgr,Byte> InputImage, double angle, int LeftPixel, int RightPixel, int borderLeft, int borderRight)
    {

            //adds extra border on trailing edge side
            if (angle < 0)
            {
                borderLeft = borderLeft * 2;
            }
            else
            {
                borderRight = borderRight * 2;
            }

            if (LeftPixel <= borderLeft)
            {
                LeftPixel = borderLeft;
            }
            if (RightPixel >= InputImage.Width - 1 - borderRight)
            {
                RightPixel = (int)(InputImage.Width - 1 - borderRight);
            }
            // recrop image
            Rectangle rectCrop = new Rectangle((int)(LeftPixel - borderLeft), 0, (int)(RightPixel + borderRight + borderLeft - LeftPixel), (int)(InputImage.Height));

            rectCrop.X = (int)(LeftPixel - borderLeft);
            rectCrop.Width = (int)(RightPixel + borderRight + borderLeft - LeftPixel);

            Image<Bgr,Byte> croppedImage = InputImage.GetSubRect(rectCrop);

            int rows = croppedImage.Rows;
            int cols = croppedImage.Cols;
            int ch = croppedImage.NumberOfChannels;

            PointF[] preTransform = new PointF[] { new PointF(cols / 2, rows / 2), new PointF(cols / 2, rows / 2 + 57), new PointF(cols / 2 + 20, rows / 2 + 57) };
            PointF[] postTransform = new PointF[] { new PointF(cols / 2, rows / 2), new PointF((float)(cols / 2 - angle), rows / 2 + 57), new PointF((float)(cols / 2 - angle + 20), rows / 2 + 57) };

            Mat M = CvInvoke.GetAffineTransform(preTransform, postTransform);

            return M;
    
    }
    private Image<Gray, Byte> SingleLineTurbineFinder(Image<Bgr, Byte> InputImage,bool DarkGround) {

        Image<Bgr, Byte>  scaledim = InputImage.Clone();

        //filter image
        scaledim.SmoothBlur(13, 1);

        //run a median filter (it might be worth finding a better alternative here)
        scaledim.SmoothMedian(51);

        Image<Hsv, Byte> imageHSV;

        imageHSV = scaledim.Convert<Hsv, Byte>();

        //create single line of data for analysis for upper/lower and whole image
        Image<Hsv, Byte> hsvSmushed_upper;
        Image<Hsv, Byte> hsvSmushed_lower;
        Image<Hsv, Byte> hsvSmushed;

        hsvSmushed_upper = imageHSV.GetSubRect(new System.Drawing.Rectangle(0, (int)(imageHSV.Height / 6), (int)(imageHSV.Width), 1));
        hsvSmushed_lower = imageHSV.GetSubRect(new System.Drawing.Rectangle(0, (int)(imageHSV.Height - (imageHSV.Height / 6)), (int)(imageHSV.Width), 1));

        //get average of the upper and lower for the average
        hsvSmushed = (hsvSmushed_upper / 2) + (hsvSmushed_lower / 2);

        //split the image into the Hue Sat Val components
        Image<Gray, Byte>[] lower_HSV_smushed_split = hsvSmushed_lower.Split();
        Image<Gray, Byte>[] upper_HSV_smushed_split = hsvSmushed_upper.Split();
        Image<Gray, Byte>[] HSV_smushed_split = hsvSmushed.Split();

        Image<Gray, Byte> thresh_hue = new Image<Gray, byte>((int)imageHSV.Width, 1);
        Image<Gray, Byte> thresh_val = new Image<Gray, byte>((int)imageHSV.Width, 1); ;
        Image<Gray, Byte> thresh_sat = new Image<Gray, byte>((int)imageHSV.Width, 1); ;

        Image<Gray, Byte> thresh_hue_upper = new Image<Gray, byte>((int)imageHSV.Width, 1); ;
        Image<Gray, Byte> thresh_val_upper = new Image<Gray, byte>((int)imageHSV.Width, 1); ;
        Image<Gray, Byte> thresh_sat_upper = new Image<Gray, byte>((int)imageHSV.Width, 1); ;

        Image<Gray, Byte> thresh_hue_lower = new Image<Gray, byte>((int)imageHSV.Width, 1); ;
        Image<Gray, Byte> thresh_val_lower = new Image<Gray, byte>((int)imageHSV.Width, 1); ;
        Image<Gray, Byte> thresh_sat_lower = new Image<Gray, byte>((int)imageHSV.Width, 1); ;

        // get an adaptive threshold of each line for a total of 9 lines

        double ret1 = CvInvoke.Threshold(HSV_smushed_split[0], thresh_hue, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.Binary);
        double ret2 = CvInvoke.Threshold(HSV_smushed_split[1], thresh_sat, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.BinaryInv);

        double ret3 = CvInvoke.Threshold(HSV_smushed_split[2], thresh_val, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.Binary);
        ret3 = CvInvoke.Threshold(HSV_smushed_split[2], thresh_val, ret3 + 20, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.Binary);

        double ret4 = CvInvoke.Threshold(upper_HSV_smushed_split[0], thresh_hue_upper, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.Binary);
        double ret5 = CvInvoke.Threshold(upper_HSV_smushed_split[1], thresh_sat_upper, 45, 6, Emgu.CV.CvEnum.ThresholdType.Binary);

        double ret6 = CvInvoke.Threshold(upper_HSV_smushed_split[2], thresh_val_upper, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.Binary);
        ret6 = CvInvoke.Threshold(upper_HSV_smushed_split[2], thresh_val_upper, ret6 + 25, 5, Emgu.CV.CvEnum.ThresholdType.Binary);

        double ret7 = CvInvoke.Threshold(lower_HSV_smushed_split[0], thresh_hue_lower, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.BinaryInv);
        double ret8 = CvInvoke.Threshold(lower_HSV_smushed_split[1], thresh_sat_lower, 45, 5, Emgu.CV.CvEnum.ThresholdType.Binary);
        // if the ground is dark blade is brighter and vice versa
        if (DarkGround == true)
        {
            double ret9 = CvInvoke.Threshold(lower_HSV_smushed_split[2], thresh_val_lower, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.BinaryInv);
            ret9 = CvInvoke.Threshold(lower_HSV_smushed_split[2], thresh_val_lower, ret9 - 20, 5, Emgu.CV.CvEnum.ThresholdType.BinaryInv);
        }
        else
        {
            double ret9 = CvInvoke.Threshold(lower_HSV_smushed_split[2], thresh_val_lower, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.Binary);
            ret9 = CvInvoke.Threshold(lower_HSV_smushed_split[2], thresh_val_lower, ret9 + 20, 5, Emgu.CV.CvEnum.ThresholdType.Binary);
        }


        Image<Gray, Byte> Special_values = thresh_sat + thresh_sat_upper + thresh_val_upper + thresh_hue_lower + thresh_sat_lower + thresh_val_lower;

        Image<Gray, Byte> thresh_special = new Image<Gray, byte>((int)imageHSV.Width, 1); ;

        ret3 = CvInvoke.Threshold(Special_values, thresh_special, 0, 5, Emgu.CV.CvEnum.ThresholdType.Otsu | Emgu.CV.CvEnum.ThresholdType.Binary);

        thresh_hue.Dispose();
        thresh_hue_lower.Dispose();
        thresh_hue_upper.Dispose();
        thresh_sat.Dispose();
        thresh_sat_lower.Dispose();
        thresh_sat_upper.Dispose();
        thresh_val.Dispose();
        thresh_val_lower.Dispose();
        thresh_val_upper.Dispose();

        GC.Collect();

        return thresh_special;
    }
    private bool GetGroundDark(Image<Bgr, Byte> inputImage) {

        //create copy of resized image for checking if the ground is bright or not
        Image<Bgr, Byte> vertical_image = inputImage.Clone();

        //blur image horizontally
        vertical_image.SmoothBlur(501, 31);

        //convert to HSV
        Image<Hsv, byte> HSV_Vert_image = vertical_image.Convert<Hsv, Byte>();


        Hsv groundPointHSV = HSV_Vert_image[(int)(inputImage.Height - 10), 10];

        bool DarkGround = false; //variable conatins value of ground brightness  
        //check if the value of a pixel on the ground is high or low
        if (groundPointHSV.Value > 140)
        {//140 is a good guess
            //System.out.println("Bright ground");
            DarkGround = false;
        }
        else
        {
            //System.out.println("Dark Ground");
            DarkGround = true;
        }

        vertical_image.Dispose();

        return DarkGround;
        
    }
    private void Write_ACDGO_format_file(string pathToOrigionalImageFile, int cropPixelRight, int CropPixelLeft, int width, int height)
    {
        string[] lines = System.IO.File.ReadAllLines("SampleXMP.JPG.XMP");
           
        string oldline = "id=\"Crop.Active\"&gt;1&lt;/int&gt;&lt;int id=\"Crop.Version\"&gt;7&lt;/int&gt;&lt;int id=\"Crop.Automatic\"&gt;0&lt;/int&gt;&lt;float id=\"Crop.Left\"&gt;0.249245&lt;/float&gt;&lt;float id=\"Crop.Top\"&gt;0.000000&lt;/float&gt;&lt;float id=\"Crop.Right\"&gt;0.731765&lt;/float&gt;&lt;float id=\"Crop.Bottom\"&gt;1.000000&lt;/float&gt;&lt;int id=\"Crop.ProportionConstrained\"&gt;0&lt;/int&gt;&lt;int id=\"Crop.ProportionConstrainedOriginal\"&gt;1&lt;/int&gt;&lt;int id=\"Crop.ProportionNum\"&gt;7952&lt;/int&gt;&lt;int id=\"Crop.ProportionDen\"&gt;5304&lt;/int&gt;&lt;int id=\"Crop.ProportionOrientation\"&gt;2&lt;/int&gt;&lt;int ";
        string newline = "id=\"Crop.Active\"&gt;1&lt;/int&gt;&lt;int id=\"Crop.Version\"&gt;7&lt;/int&gt;&lt;int id=\"Crop.Automatic\"&gt;0&lt;/int&gt;&lt;float id=\"Crop.Left\"&gt;" + ((float)CropPixelLeft / (float)width).ToString() + "&lt;/float&gt;&lt;float id=\"Crop.Top\"&gt;0.000000&lt;/float&gt;&lt;float id=\"Crop.Right\"&gt;" + ((float)cropPixelRight / (float)width).ToString() + "&lt;/float&gt;&lt;float id=\"Crop.Bottom\"&gt;1.000000&lt;/float&gt;&lt;int id=\"Crop.ProportionConstrained\"&gt;0&lt;/int&gt;&lt;int id=\"Crop.ProportionConstrainedOriginal\"&gt;1&lt;/int&gt;&lt;int id=\"Crop.ProportionNum\"&gt;" + width.ToString() + "&lt;/int&gt;&lt;int id=\"Crop.ProportionDen\"&gt;" + height.ToString() + "&lt;/int&gt;&lt;int id=\"Crop.ProportionOrientation\"&gt;2&lt;/int&gt;&lt;int ";
        
        lines[7] = lines[7].Replace(oldline,newline);
        
        System.IO.File.WriteAllLines(pathToOrigionalImageFile+".XMP",lines);
        }    
    }
    class BladeCroppingSettings {

        private int scaleDown;// 0 = don't scale
        private bool cleanImage;

        public BladeCroppingSettings(int SD, int CI) {
            this.setScaleDown(SD);
            this.setCleanImage(cleanImage);
        }
        public void setScaleDown(int scaleDown)
        {

            this.scaleDown = scaleDown;

        }

        public void setCleanImage(bool cleanImage)
        {

            this.cleanImage = cleanImage;

        }


        public int getScaleDown()
        {

            return scaleDown;

        }
        public bool getCleanImage()
        {
            return cleanImage;
        }

    }
}
