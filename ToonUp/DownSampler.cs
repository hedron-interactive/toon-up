using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ToonUp
{
    public class DownSampler
    {
        private delegate Color PixelSampler(int x, int y, ref Color pixel);

        private VideoFormat videoFormat;
        private int sampleFactor;
        private int maskWidth;
        private Color[] mask;
        private PixelSampler[] samplers;

        public DownSampler(VideoFormat format, int downSampleFactor)
        {
            videoFormat = format;

            sampleFactor = downSampleFactor;

            maskWidth = (format.PixelWidth + downSampleFactor - 1) / downSampleFactor;

            mask = new Color[((format.PixelHeight + downSampleFactor - 1) / downSampleFactor) * maskWidth];

            this.samplers = new PixelSampler[]
            {
                PassThroughPixelSampler,
                CellBlurPixelSampler,
                WeightedCellBlurPixelSampler,
                DarkeningPixelSampler,
                BrighteningPixelSampler,
                GrayPixelSampler,
                ColorChannelSwapPixelSampler,
                ColorInversionSwapPixelSampler,
                ColorMorphSwapPixelSampler,
                PureBlackPixelSampler,
                PureWhitePixelSampler,
                QuantizedPixelSampler,
                MoreQuantizedPixelSampler,
                EvenMoreQuantizedPixelSampler,
                WayMoreQuantizedPixelSampler,
                PolarizedPixelSampler,
            };

            this.SamplerIndex = 0;
        }

        public int SamplerIndex
        {
            get;
            private set;
        }

        public string SamplerName
        {
            get
            {
                return samplers[SamplerIndex].Method.Name;
            }
        }

        public Color SamplePixels(int x, int y, ref Color pixel)
        {
            return this.samplers[this.SamplerIndex](x, y, ref pixel);
        }

        #region PassThroughPixelSampler

        private Color PassThroughPixelSampler(int x, int y, ref Color pixel)
        {
            return pixel;
        }

        #endregion

        #region CellBlurPixelSampler

        private Color CellBlurPixelSampler(int x, int y, ref Color pixel)
        {
            if (IsSeed(x, y))
            {
                mask[MapOriginalToSeed(x, y)] = pixel;
                return pixel;
            }
            else
                return CellBlurPixelSampler_ComputeSeedAverage(MapOriginalToSeed(x, y), ref pixel);
        }

        protected Color CellBlurPixelSampler_ComputeSeedAverage(int seed, ref Color cur)
        {
            Color o = mask[seed];

            o.R = (byte)(((int)o.R * 2 + (int)cur.R) / 3);
            o.G = (byte)(((int)o.G * 2 + (int)cur.G) / 3);
            o.B = (byte)(((int)o.B * 2 + (int)cur.B) / 3);

            return o;
        }

        #endregion

        #region WeightedCellBlurPixelSampler

        private Color WeightedCellBlurPixelSampler(int x, int y, ref Color pixel)
        {
            if (IsSeed(x, y))
            {
                mask[MapOriginalToSeed(x, y)] = pixel;
                return pixel;
            }
            else
                return WeightedCellBlurPixelSampler_ComputeSeedAverage(MapOriginalToSeed(x, y), ref pixel);
        }

        protected Color WeightedCellBlurPixelSampler_ComputeSeedAverage(int seed, ref Color cur)
        {
            Color o = mask[seed];

            o.R = (byte)(((int)o.R + (int)cur.R * 2) / 3);
            o.G = (byte)(((int)o.G + (int)cur.G * 2) / 3);
            o.B = (byte)(((int)o.B + (int)cur.B * 2) / 3);

            return o;
        }

        #endregion

        #region DarkeningPixelSampler

        private Color DarkeningPixelSampler(int x, int y, ref Color pixel)
        {
            return Color.FromArgb(pixel.A, (byte)(pixel.R >> 1), (byte)(pixel.G >> 1), (byte)(pixel.B >> 1));
        }

        #endregion

        #region BrighteningPixelSampler

        private Color BrighteningPixelSampler(int x, int y, ref Color pixel)
        {
            return Color.FromArgb(pixel.A, (byte)Math.Min(pixel.R << 1, 255), (byte)Math.Min(pixel.G << 1, 255), (byte)Math.Min(pixel.B << 1, 255));
        }

        #endregion

        #region GrayPixelSampler

        private Color GrayPixelSampler(int x, int y, ref Color pixel)
        {
            int gray = (pixel.R + pixel.G + pixel.B) / 3;
            return Color.FromArgb(pixel.A, (byte)gray, (byte)gray, (byte)gray);
        }

        #endregion

        #region ColorChannelSwapPixelSampler

        private Color ColorChannelSwapPixelSampler(int x, int y, ref Color pixel)
        {
            return Color.FromArgb(pixel.A, pixel.G, pixel.B, pixel.R);
        }

        #endregion

        #region ColorInversionSwapPixelSampler

        private Color ColorInversionSwapPixelSampler(int x, int y, ref Color pixel)
        {
            return Color.FromArgb(pixel.A, (byte)(255 - pixel.R), (byte)(255 -  pixel.G), (byte)(255 - pixel.B));
        }

        #endregion

        #region ColorMorphSwapPixelSampler

        private Color ColorMorphSwapPixelSampler(int x, int y, ref Color pixel)
        {
            float r = pixel.R / 255.0f;
            float g = pixel.G / 255.0f;
            float b = pixel.B / 255.0f;

            return Color.FromArgb(pixel.A, (byte)(r * g * 255.0f), (byte)(g * b * 255.0f), (byte)(b * r * 255.0f));
        }

        #endregion

        #region PureBlackPixelSampler

        private Color PureBlackPixelSampler(int x, int y, ref Color pixel)
        {
            return Color.FromArgb(pixel.A, 0, 0, 0);
        }

        #endregion

        #region PureWhitePixelSampler

        private Color PureWhitePixelSampler(int x, int y, ref Color pixel)
        {
            return Color.FromArgb(pixel.A, 255, 255, 255);
        }

        #endregion

        #region QuantizedPixelSampler

        private Color QuantizedPixelSampler(int x, int y, ref Color pixel)
        {
            int r = (pixel.R / 16) * 16;
            int g = (pixel.G / 16) * 16;
            int b = (pixel.B / 16) * 16;

            return Color.FromArgb(pixel.A, (byte)r, (byte)g, (byte)b);
        }

        #endregion

        #region MoreQuantizedPixelSampler

        private Color MoreQuantizedPixelSampler(int x, int y, ref Color pixel)
        {
            int r = (pixel.R / 32) * 32;
            int g = (pixel.G / 32) * 32;
            int b = (pixel.B / 32) * 32;

            return Color.FromArgb(pixel.A, (byte)r, (byte)g, (byte)b);
        }

        #endregion

        #region EvenMoreQuantizedPixelSampler

        private Color EvenMoreQuantizedPixelSampler(int x, int y, ref Color pixel)
        {
            int r = (pixel.R / 64) * 64;
            int g = (pixel.G / 64) * 64;
            int b = (pixel.B / 64) * 64;

            return Color.FromArgb(pixel.A, (byte)r, (byte)g, (byte)b);
        }

        #endregion

        #region WayMoreQuantizedPixelSampler

        private Color WayMoreQuantizedPixelSampler(int x, int y, ref Color pixel)
        {
            int r = (pixel.R / 128) * 128;
            int g = (pixel.G / 128) * 128;
            int b = (pixel.B / 128) * 128;

            return Color.FromArgb(pixel.A, (byte)r, (byte)g, (byte)b);
        }

        #endregion

        #region PolarizedPixelSampler

        private Color PolarizedPixelSampler(int x, int y, ref Color pixel)
        {
            int gray = (pixel.R + pixel.G + pixel.B) / 3;
            int r, g, b;

            if (gray > 128)
            {
                r = (pixel.R * 2) / 3;
                g = (pixel.G * 2) / 3;
                b = (pixel.B * 2) / 3;
            }
            else
            {
                r = (pixel.R * 2 + 255) / 3;
                g = (pixel.G * 2 + 255) / 3;
                b = (pixel.B * 2 + 255) / 33;
            }

            return Color.FromArgb(pixel.A, (byte)r, (byte)g, (byte)b);
        }

        #endregion

        #region MyRegion

        #endregion

        protected bool IsSeed(int x, int y)
        {
            return (x % sampleFactor == 0) && (y % sampleFactor == 0);
        }

        protected int MapOriginalToSeed(int x, int y)
        {
            x = x / sampleFactor;
            y = y / sampleFactor;

            return y * maskWidth + x;
        }

        public void RotateSampler()
        {
            this.SamplerIndex = (this.SamplerIndex + 1) % this.samplers.Length;
        }
    }
}