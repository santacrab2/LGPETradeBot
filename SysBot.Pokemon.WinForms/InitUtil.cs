#if NETFRAMEWORK
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Drawing;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SysBot.Pokemon.WinForms
{
    public static class InitUtil
    {
        public static void InitializeStubs()
        {
            var sav8 = new SAV8SWSH();
            SetUpSpriteCreator(sav8);
        }

        private static void SetUpSpriteCreator(SaveFile sav)
        {
            SpriteUtil.Initialize(sav);
            TradebotSettings.CreateSpriteFile = (code) =>
            {
                int codecount = 0;
                foreach(LetsGoTrades.pictocodes cd in code)
                {
                    SpriteUtil.UseLargeAlways = true;
                  
                    var showdown = new ShowdownSet(cd.ToString());
                    PKM pk = LetsGoTrades.sav.GetLegalFromSet(showdown, out _);
                    Image png = SpriteUtil.GetSprite(pk.Species, 0, 0, 0, 0, false, false,-1,true);
                    png = ResizeImage(png, 137, 130);
                  png.Save($"{System.IO.Directory.GetCurrentDirectory()}//code{codecount}.png");
                   codecount++;
                }
             
            };
        }
        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(-40, -65, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                
            }

            var cutimg = new Bitmap(destImage.Width-40,destImage.Height-65);
            var g = Graphics.FromImage(cutimg);
            g.DrawImage(destImage, 0, 0);
            return cutimg;
        }


    }
}
#endif
