using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace GlobalDemo.WebJob
{
    class PhotoEditor
    {
        /// <summary>
        /// Resizes the image using a high quality.  
        /// From http://stackoverflow.com/questions/1922040/resize-an-image-c-sharp
        /// </summary>
        /// <param name="imageStream"></param>
        /// <returns></returns>
        internal static MemoryStream ProcessImage(MemoryStream imageStream)
        {
            var height = 100;
            var width = 100;

            Image original = Image.FromStream(imageStream);

            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(original.HorizontalResolution, original.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(original, destRect, 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            MemoryStream ret = new MemoryStream();
            destImage.Save(ret, original.RawFormat);
            ret.FlushAsync().Wait();
            ret.Position = 0;
            return ret;
        }
    }
}
