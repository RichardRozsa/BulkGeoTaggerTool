using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using Manina.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;


namespace ITGeoTagger
{
    public class IT_ImageBoxRenderer : ImageListView.ImageListViewRenderer
    {
        // Returns item size for the given view mode.

        private Font mCaptionFont;
        private int mTileWidth;
        private int mTextHeight;
  
               private Font CaptionFont
               {
                   get
                   {
                       if (mCaptionFont == null)
                           mCaptionFont = new Font(ImageListView.Font, FontStyle.Bold);
                       return mCaptionFont;
                   }
               }

        public override Size MeasureItem(View view)
        {
            if (view == View.Thumbnails)
            {
                Size itemPadding = new Size(18, 10);
                int textHeight = ImageListView.Font.Height;
                Size sz = ImageListView.ThumbnailSize + new Size(0,textHeight)+itemPadding;
                return sz;
            }
            else
                return base.MeasureItem(view);
        }
        // Draws the background of the control.
        public override void DrawBackground(Graphics g, Rectangle bounds)
        {
            if (ImageListView.View == View.Thumbnails)
                g.Clear(Color.FromArgb(32, 32, 32));
            else
                base.DrawBackground(g, bounds);
        }
        // Draws the specified item on the given graphics.
        public override void DrawItem(Graphics g, ImageListViewItem item,
            ItemState state, Rectangle bounds)
        {
            if (ImageListView.View == View.Thumbnails)
            {
                // Black background
                using (Brush b = new SolidBrush(Color.Black))
                {
                    Utility.FillRoundedRectangle(g, b, bounds, 4);
                }
                // Background of selected items
                if ((state & ItemState.Selected) == ItemState.Selected)
                {
                    using (Brush b = new SolidBrush(Color.FromArgb(128,
                                            SystemColors.Highlight)))
                    {
                        Utility.FillRoundedRectangle(g, b, bounds, 4);
                    }
                }
                // Gradient background
                using (Brush b = new LinearGradientBrush(
                    bounds,
                    Color.Transparent,
                    Color.FromArgb(96, 32,32,32),
                    LinearGradientMode.Vertical))
                {
                    Utility.FillRoundedRectangle(g, b, bounds, 4);
                }
                // Light overlay for hovered items
                if ((state & ItemState.Hovered) == ItemState.Hovered)
                {
                    using (Brush b =
                            new SolidBrush(Color.FromArgb(32, SystemColors.Highlight)))
                    {
                        Utility.FillRoundedRectangle(g, b, bounds, 4);
                    }
                }
                // Border
                using (Pen p = new Pen(Color.FromArgb(128,32,32,32)))
                {
                    Utility.DrawRoundedRectangle(g, p, bounds.X, bounds.Y, bounds.Width - 1,
                                            bounds.Height - 1, 4);
                }
                // Image
                Image img = item.ThumbnailImage;
                if (img != null)
                {
                    int x = bounds.Left + (bounds.Width - img.Width) / 2;
                    int y = bounds.Top + (bounds.Height - img.Height) / 2;
                    g.DrawImageUnscaled(item.ThumbnailImage, x, y);
                    // Image border
                    using (Pen p = new Pen(Color.FromArgb(128, 32,32,32)))
                    {
                        g.DrawRectangle(p, x, y, img.Width - 1, img.Height - 1);
                    }
                }
            }
            else
                base.DrawItem(g, item, state, bounds);
        }
        // Draws the selection rectangle.
        public override void DrawSelectionRectangle(Graphics g, Rectangle selection)
        {
            using (Brush b = new HatchBrush(
                HatchStyle.DarkDownwardDiagonal,
                Color.FromArgb(128, Color.Black),
                Color.FromArgb(128, SystemColors.Highlight)))
            {
                g.FillRectangle(b, selection);
            }
            using (Pen p = new Pen(SystemColors.Highlight))
            {
                g.DrawRectangle(p, selection.X, selection.Y,
                    selection.Width, selection.Height);
            }
        }
        
    }
}
