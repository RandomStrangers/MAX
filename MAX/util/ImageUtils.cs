﻿/*
    Copyright 2015-2024 MCGalaxy
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;

#if !MAX_DOTNET
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
#else
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
#endif
using System.IO;

namespace MAX.Util
{
    public delegate Pixel PixelGet(int x, int y);
    public delegate void PixelSet(int x, int y, Pixel pixel);
    public struct Pixel { public byte A, R, G, B; }

    /// <summary> Represents a 2D image </summary>
    /// <remarks> Backing implementation depends on whether running on dotnet or .NET </remarks>
    public class IBitmap2D : IDisposable
    {
        public int Width, Height;
        public PixelGet Get;
        public PixelSet Set;

        /// <summary> Retrieves the raw underlying image representation </summary>
        public virtual object RawImage { get; }

        public virtual void Decode(byte[] data)
        {
        }

        public virtual void Resize(int width, int height, bool highQuality)
        {
        }

        public virtual void LockBits()
        {
        }

        public virtual void UnlockBits()
        {
        }

        public virtual void Dispose()
        {
        }

#if !MAX_DOTNET
        public static IBitmap2D Create() { return new GDIPlusBitmap(); }
#else
        public static IBitmap2D Create() { return new ImageSharpBitmap(); }
#endif
    }

    public static class ImageUtils
    {
        public static IBitmap2D DecodeImage(byte[] data, Player p)
        {
            IBitmap2D bmp = null;
            try
            {
                bmp = IBitmap2D.Create();
                bmp.Decode(data);
                return bmp;
            }
            catch (ArgumentException ex)
            {
                // GDI+ throws ArgumentException when data is not an image
                // This is a fairly expected error - e.g. when a user tries to /imgprint
                //   the webpage an image is hosted on, instead of the actual image itself. 
                // So don't bother logging a full error for this case
                Logger.Log(LogType.Warning, "Error decoding image: " + ex.Message);
                OnDecodeError(p, bmp);
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error decoding image", ex);
                OnDecodeError(p, bmp);
                return null;
            }
        }

        public static void OnDecodeError(Player p, IBitmap2D bmp)
        {
            bmp?.Dispose();
            // TODO failed to decode the image. make sure you are using the URL of the image directly, not just the webpage it is hosted on              
            p.Message("&WThere was an error reading the downloaded image.");
            p.Message("&WThe url may need to end with its extension (such as .jpg).");
        }
    }


#if !MAX_DOTNET
    public unsafe class GDIPlusBitmap : IBitmap2D
    {
        public Image img;
        public Bitmap bmp;
        public BitmapData data;
        public byte* scan0;
        public int stride;

        public override object RawImage { get { return bmp; } }

        public override void Decode(byte[] data)
        {
            Image tmp = Image.FromStream(new MemoryStream(data));
            SetBitmap(tmp);
        }

        public override void Resize(int width, int height, bool hq)
        {
            Bitmap resized = new Bitmap(width, height);
            // https://photosauce.net/blog/post/image-scaling-with-gdi-part-3-drawimage-and-the-settings-that-affect-it
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = hq ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = hq ? PixelOffsetMode.HighQuality : PixelOffsetMode.None;
                g.DrawImage(bmp, 0, 0, width, height);
            }

            Dispose();
            SetBitmap(resized);
        }

        public void SetBitmap(Image src)
        {
            img = src;
            // although rare, possible src might actually be a Metafile instead
            bmp = (Bitmap)src;

            // NOTE: sometimes Mono will return an invalid bitmap instance that
            //  throws ArgumentNullException when trying to access Width/Height
            Width = src.Width;
            Height = src.Height;
        }

        public override void Dispose()
        {
            UnlockBits();
            img?.Dispose();

            img = null;
            bmp = null;
        }


        public override void LockBits()
        {
            bool fastPath = bmp.PixelFormat == PixelFormat.Format32bppRgb
                         || bmp.PixelFormat == PixelFormat.Format32bppArgb
                         || bmp.PixelFormat == PixelFormat.Format24bppRgb;

            Get = GetGenericPixel;
            Set = SetGenericPixel;
            if (!fastPath) return;
            // We can only use the fast path for 24bpp or 32bpp bitmaps

            Rectangle r = new Rectangle(0, 0, bmp.Width, bmp.Height);
            data = bmp.LockBits(r, ImageLockMode.ReadOnly, bmp.PixelFormat);
            scan0 = (byte*)data.Scan0;
            stride = data.Stride;

            if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
            {
                Get = Get24BppPixel;
            }
            else
            {
                Get = Get32BppPixel;
            }
        }

        public override void UnlockBits()
        {
            if (data != null) bmp.UnlockBits(data);
            data = null;
        }


        public Pixel GetGenericPixel(int x, int y)
        {
            Pixel p;
            int argb = bmp.GetPixel(x, y).ToArgb(); // R/G/B properties incur overhead  

            p.A = (byte)(argb >> 24);
            p.R = (byte)(argb >> 16);
            p.G = (byte)(argb >> 8);
            p.B = (byte)argb;
            return p;
        }

        public void SetGenericPixel(int x, int y, Pixel p)
        {
            bmp.SetPixel(x, y, Color.FromArgb(p.A, p.R, p.G, p.B));
        }

        public Pixel Get24BppPixel(int x, int y)
        {
            Pixel p;
            byte* ptr = scan0 + y * stride + (x * 3);
            p.B = ptr[0]; p.G = ptr[1]; p.R = ptr[2]; p.A = 255;
            return p;
        }

        public Pixel Get32BppPixel(int x, int y)
        {
            Pixel p;
            byte* ptr = scan0 + y * stride + (x * 4);
            p.B = ptr[0]; p.G = ptr[1]; p.R = ptr[2]; p.A = ptr[3];
            return p;
        }
    }
#else
    public unsafe class ImageSharpBitmap : IBitmap2D
    {
        public Image<Rgba32> img;
        
        public override object RawImage { get { return img; } }

        public override void Decode(byte[] data) 
        {
            img = Image.Load<Rgba32>(data);
            UpdateDimensions();
            
            Get = GetPixel;
            Set = SetPixel;
        }

        public override void Resize(int width, int height, bool hq) 
        {
            IResampler resampler = hq ? KnownResamplers.Bicubic : KnownResamplers.NearestNeighbor;
            img.Mutate(x => x.Resize(width, height, resampler));
            UpdateDimensions();
        }

        public void UpdateDimensions() 
        {
            Width  = img.Width;
            Height = img.Height;
        }

        public Pixel GetPixel(int x, int y) 
        {
            Pixel p;
            Rgba32 src = img[x, y];
            
            p.A = src.A;
            p.R = src.R;
            p.G = src.G;
            p.B = src.B; // TODO avoid overhead by direct blit??
            return p;
        }

        public void SetPixel(int x, int y, Pixel p) 
        {
            Rgba32 dst;
            
            dst.A = p.A;
            dst.R = p.R;
            dst.G = p.G;
            dst.B = p.B; // TODO avoid overhead by direct blit??
            img[x, y] = dst;
        }

        public override void Dispose() 
        {
            if (img != null) img.Dispose();
            img = null;
        }


        public override void LockBits() { }
        public override void UnlockBits() { }
    }
#endif
}
