using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITGeoTagger
{
    static class ImagePassSorter
    {
        public static double VerticalImageDistribution = 2;

        public static ImageBladeGroup SortImagesByPasses(ImageBladeGroup ImageGroup)
        {
            ImageGroup.FullImageList = SortImagesByType(ImageGroup.FullImageList);
            ImageGroup.FullImageList = MiddleOutSelect(ImageGroup.FullImageList, ImageLocationType.Pass1);
            ImageGroup.FullImageList = MiddleOutSelect(ImageGroup.FullImageList, ImageLocationType.Pass2);
            ImageGroup.FullImageList = MiddleOutSelect(ImageGroup.FullImageList, ImageLocationType.Pass3);
            ImageGroup.FullImageList = MiddleOutSelect(ImageGroup.FullImageList, ImageLocationType.Pass4);
            ImageGroup.FullImageList = GetTipImages(ImageGroup.FullImageList);

            return ImageGroup;
        }
        private static List<ImageLocationAndExtraInfo> GetListOfPass(List<ImageLocationAndExtraInfo> ImageList, ImageLocationType passToSort)
        {
            List<ImageLocationAndExtraInfo> FilteredList = new List<ImageLocationAndExtraInfo>();
            foreach (ImageLocationAndExtraInfo image in ImageList)
            {
                if (image.Type == passToSort)
                {
                    FilteredList.Add(image);
                }
            }
            return FilteredList;
        }
        private static List<ImageLocationAndExtraInfo> MiddleOutSelect(List<ImageLocationAndExtraInfo> ImageList, ImageLocationType passToSort)
        {
            List<ImageLocationAndExtraInfo> FilteredList = GetListOfPass(ImageList, passToSort);

            //split list in middle
            List<ImageLocationAndExtraInfo> FilteredListFirstHalf = FilteredList.GetRange(0, FilteredList.Count / 2);
            FilteredListFirstHalf.Reverse();
            List<ImageLocationAndExtraInfo> FilteredListSecondHalf = FilteredList.GetRange((FilteredList.Count / 2) - 1, FilteredList.Count / 2); ;


            if ((passToSort == ImageLocationType.Pass1) || (passToSort == ImageLocationType.Pass3))
            {
                //Filter first half in reverse down
                double LastVal = 99999999;
                ImageLocationAndExtraInfo LastSelected = FilteredListFirstHalf.First();
                ImageLocationAndExtraInfo tmpLastValue = FilteredListFirstHalf.First();
                foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListFirstHalf)
                {
                    // select last hub shot
                    if ((ImageLoc.Type == passToSort) && (ImageLoc.Altitude <= LastVal - VerticalImageDistribution))
                    {
                        LastSelected = ImageLoc;
                        LastVal = tmpLastValue.Altitude;
                        tmpLastValue.selected = true;
                    }
                    tmpLastValue = ImageLoc;
                }
                FilteredListFirstHalf.Last().selected = true;
                //filter second half forward up
                LastVal = 0;
                tmpLastValue = FilteredListSecondHalf.First();
                foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListSecondHalf)
                {
                    if ((ImageLoc.Type == passToSort) && (ImageLoc.Altitude >= LastVal + VerticalImageDistribution))
                    {
                        LastSelected = ImageLoc;
                        LastVal = tmpLastValue.Altitude;

                        tmpLastValue.selected = true;
                    }
                    tmpLastValue = ImageLoc;
                }
                LastSelected.selected = true;
            }
            else if ((passToSort == ImageLocationType.Pass2) || (passToSort == ImageLocationType.Pass4))
            {
                //Filter first half in reverse up
                double LastVal = 0;
                ImageLocationAndExtraInfo LastSelected = FilteredListFirstHalf.First();
                ImageLocationAndExtraInfo tmpLastValue = FilteredListFirstHalf.First();
                foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListFirstHalf)
                {
                    if ((ImageLoc.Type == passToSort) && (ImageLoc.Altitude >= LastVal + VerticalImageDistribution))
                    {
                        LastSelected = ImageLoc;
                        LastVal = tmpLastValue.Altitude;
                        tmpLastValue.selected = true;
                    }
                    tmpLastValue = ImageLoc;
                }
                LastSelected.selected = true;
                //filter second half forward down
                LastVal = 99999999;
                tmpLastValue = FilteredListSecondHalf.First();
                foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListSecondHalf)
                {
                    // select last hub shot
                    if ((ImageLoc.Type == passToSort) && (ImageLoc.Altitude <= LastVal - VerticalImageDistribution))
                    {
                        LastSelected = ImageLoc;
                        LastVal = tmpLastValue.Altitude;
                        tmpLastValue.selected = true;
                    }
                    tmpLastValue = ImageLoc;
                }
                LastSelected.selected = true;
            }

            //for each element in the two half lists update the main list
            foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListFirstHalf)
            {
                if (ImageLoc.selected) ImageList.Find(x => x.PathToOrigionalImage == ImageLoc.PathToOrigionalImage).selected = true;
            }
            foreach (ImageLocationAndExtraInfo ImageLoc in FilteredListSecondHalf)
            {
                if (ImageLoc.selected) ImageList.Find(x => x.PathToOrigionalImage == ImageLoc.PathToOrigionalImage).selected = true;
            }


            return ImageList;

        }
        private static List<ImageLocationAndExtraInfo> SortImagesByType(List<ImageLocationAndExtraInfo> ImageLocationList)
        {
            double hubHeight = FindHubHeight(ImageLocationList);
            double tipHeight = FindTipHeight(ImageLocationList);

            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Altitude < 10)) { ImageLoc.Type = ImageLocationType.Ground; }
                else if (ImageLoc.Altitude > hubHeight - 8) { ImageLoc.Type = ImageLocationType.High; }
                else if ((ImageLoc.Altitude > 10) && (ImageLoc.Altitude < tipHeight + 8)) { ImageLoc.Type = ImageLocationType.Low; }

            }
            int hubCNT = 0;
            int tipCNT = 0;
            ImageLocationType tempType = ImageLocationType.Pass1;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if (ImageLoc.Type == ImageLocationType.High)
                {
                    hubCNT++;
                }
                else
                {
                    hubCNT = 0;
                }
                if (ImageLoc.Type == ImageLocationType.Low)
                {
                    tipCNT++;
                }
                else
                {
                    tipCNT = 0;
                }

                if ((hubCNT > 4) && (tempType == ImageLocationType.Pass1) && (ImageLoc.VertVelocity < 0))
                {
                    tempType = ImageLocationType.Pass2;
                }
                if ((tipCNT > 4) && (tempType == ImageLocationType.Pass2) && (ImageLoc.VertVelocity > 0))
                {
                    tempType = ImageLocationType.Pass3;
                }
                if ((hubCNT > 4) && (tempType == ImageLocationType.Pass3) && (ImageLoc.VertVelocity < 0))
                {
                    tempType = ImageLocationType.Pass4;
                }
                if (ImageLoc.Type != ImageLocationType.Ground)
                {
                    ImageLoc.Type = tempType;
                }
            }

            return ImageLocationList;
        }
        private static List<ImageLocationAndExtraInfo> GetTipImages(List<ImageLocationAndExtraInfo> ImageLocationList)
        {

            double TipShotHeight = 100000;
            //find tip canidates
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Type == ImageLocationType.Pass2) || (ImageLoc.Type == ImageLocationType.Pass3))
                {
                    if (ImageLoc.Altitude < TipShotHeight)
                    {
                        TipShotHeight = ImageLoc.Altitude;
                    }
                }
            }
            double sum = 0;
            double CNT = 0;
            //get average of tip canidates
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Type == ImageLocationType.Pass2) || (ImageLoc.Type == ImageLocationType.Pass3))
                {
                    if (ImageLoc.Altitude < TipShotHeight + 2)
                    {
                        sum = ImageLoc.Altitude + sum;
                        CNT++;
                    }
                }
            }
            //pull tip photos from pass 2&3

            List<ImageLocationAndExtraInfo> tmpTipPhotos = new List<ImageLocationAndExtraInfo>();

            double T_Height = sum / CNT;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if (((ImageLoc.Type == ImageLocationType.Pass2) || (ImageLoc.Type == ImageLocationType.Pass3)) && (ImageLoc.selected == false))
                {
                    if (ImageLoc.Altitude < T_Height + .3)
                    {
                        ImageLoc.Type = ImageLocationType.Pass5;
                        tmpTipPhotos.Add(ImageLoc);
                    }
                }
            }
            int totalTipCount = tmpTipPhotos.Count;

            //select best guess 5
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[2].PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[totalTipCount / 4].PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[totalTipCount / 2].PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[(3 * totalTipCount) / 4].PathToOrigionalImage).selected = true;
            if (totalTipCount > 5)
            {
                ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos[totalTipCount - 2].PathToOrigionalImage).selected = true;
            }
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos.First().PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos.First().PathToOrigionalImage).Type = ImageLocationType.Pass2;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos.Last().PathToOrigionalImage).selected = true;
            ImageLocationList.Find(x => x.PathToOrigionalImage == tmpTipPhotos.Last().PathToOrigionalImage).Type = ImageLocationType.Pass3;

            return ImageLocationList;
        }
        private static List<ImageLocationAndExtraInfo> FilterPassGoingUP(List<ImageLocationAndExtraInfo> ImageLocationList, ImageLocationType PassNum)
        {

            //select items at a set vertical interval min

            double LastVal = 0;
            List<ImageLocationAndExtraInfo> tmpList = new List<ImageLocationAndExtraInfo>();
            ImageLocationAndExtraInfo tmpLastValue = ImageLocationList.First();
            bool firstTipSelected = false;
            bool LastTipSelected = false;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                // select last hub shot
                if ((ImageLoc.Type == PassNum) && (!firstTipSelected))
                {
                    firstTipSelected = true;
                    LastVal = tmpLastValue.Altitude;
                }
                else if ((ImageLoc.Type == PassNum) && (firstTipSelected) && (!LastTipSelected))
                {
                    if (tmpLastValue.Altitude < LastVal) LastVal = tmpLastValue.Altitude;
                    if (ImageLoc.Altitude > LastVal + .4)
                    {

                        tmpLastValue.selected = true;
                        LastVal = tmpLastValue.Altitude;
                        LastTipSelected = true;
                    }
                }
                else if ((ImageLoc.Type == PassNum) && (ImageLoc.Altitude >= LastVal + VerticalImageDistribution))
                {
                    LastVal = tmpLastValue.Altitude;
                    tmpLastValue.selected = true;
                }

                tmpLastValue = ImageLoc;
            }
            return ImageLocationList;
        }
        private static List<ImageLocationAndExtraInfo> FilterPassGoingDOWN(List<ImageLocationAndExtraInfo> ImageLocationList, ImageLocationType PassNum)
        {
            //select items at a set interval min
            double LastVal = 9999999;
            ImageLocationAndExtraInfo tmpLastValue = ImageLocationList.First();
            bool firstHubSelected = false;
            bool LastHubSelected = false;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                // select last hub shot
                if ((ImageLoc.Type == PassNum) && (!firstHubSelected))
                {
                    firstHubSelected = true;
                    LastVal = tmpLastValue.Altitude;
                }
                else if ((ImageLoc.Type == PassNum) && (firstHubSelected) && (!LastHubSelected))
                {
                    if (tmpLastValue.Altitude > LastVal) LastVal = tmpLastValue.Altitude;
                    if (ImageLoc.Altitude < LastVal - .3)
                    {

                        tmpLastValue.selected = true;

                        LastVal = tmpLastValue.Altitude;
                        LastHubSelected = true;
                    }
                }
                else if ((ImageLoc.Type == PassNum) && (ImageLoc.Altitude <= LastVal - VerticalImageDistribution))
                {
                    LastVal = tmpLastValue.Altitude;
                    tmpLastValue.selected = true;
                }
                tmpLastValue = ImageLoc;
            }
            return ImageLocationList;
        }
        private static double FindHubHeight(List<ImageLocationAndExtraInfo> ImageLocationList)
        {

            double MaxHubHeight = 0;
            double HubHeight = 0;

            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {

                if (ImageLoc.Altitude > MaxHubHeight) { MaxHubHeight = ImageLoc.Altitude; }
            }
            int cnt = 0;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if (ImageLoc.Altitude > MaxHubHeight - 2)
                {
                    HubHeight = HubHeight + ImageLoc.Altitude;
                    cnt++;
                }
            }
            HubHeight = HubHeight / cnt;
            return HubHeight;

        }
        private static double FindTipHeight(List<ImageLocationAndExtraInfo> ImageLocationList)
        {

            double lowestTip = 10;

            double MinTipHeight = 100000;
            double TipHeight = 0;

            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {
                if ((ImageLoc.Altitude < MinTipHeight) && (ImageLoc.Altitude > lowestTip) && (ImageLoc.VertVelocity > -.3) && (ImageLoc.VertVelocity < .3)) { MinTipHeight = ImageLoc.Altitude; }
            }
            int cnt = 0;
            foreach (ImageLocationAndExtraInfo ImageLoc in ImageLocationList)
            {

                if ((ImageLoc.Altitude < MinTipHeight + 2) && (ImageLoc.Altitude > lowestTip))
                {
                    TipHeight = TipHeight + ImageLoc.Altitude;
                    cnt++;
                }
            }
            if (cnt < 10)
            {
                Console.WriteLine("Tip Height Selection probably not made well");
            }
            TipHeight = TipHeight / cnt;
            return TipHeight;
        }

    }
}
