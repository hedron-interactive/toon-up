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
using System.Windows.Media.Imaging;

namespace ToonUp
{
    public class Canny
    {
        /// <summary>   
        /// Does basic edge detection on an image  
        /// </summary>   
        /// <param name="OriginalImage">Image to do edge detection on</param>   
        /// <param name="Threshold">Decides what is considered an edge</param>   
        /// <param name="EdgeColor">Color of the edge</param>   
        /// <returns>A bitmap which has the edges drawn on it</returns>   
        public static void EdgeDetection(byte[] OldBuffer, byte[] NewBuffer, int Width, int Height, int Stride, float Threshold, Color EdgeColor, DownSampler downSampler)
        {
            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    int x2 = x + 1, y2 = y + 1;
                    int index = y * Stride + x;
                    Color current = GetColor(OldBuffer, x, y, Stride);
                    Color sample = downSampler.SamplePixels(x, y, ref current);

                    if (y == Height - 1)
                    {
                        y2 = y;
                    }

                    if (x == Width - 1)
                    {
                        x2 = x;
                    }

                    Color next = GetColor(OldBuffer, x2, y2, Stride);

                    if (IsEdge(ref current, ref next, Threshold))
                    {
                        SetColor(NewBuffer, x, y, Stride, EdgeColor);
                    }
                    else
                    {
                        SetColor(NewBuffer, x, y, Stride, sample);
                    }
                }
            }
        }

        private static bool IsEdge(ref Color color1, ref Color color2, double edgeThreshold)
        {
            int dR = (int)color1.R - (int)color2.R;
            int dG = (int)color1.G - (int)color2.G;
            int dB = (int)color1.B - (int)color2.B;

            return Math.Sqrt((dR * dR) + (dG * dG) + (dB + dB)) >= edgeThreshold;
        }

        private static void SetColor(byte[] pixels, int x, int y, int stride, Color color)
        {
            pixels[y * stride + (x * 4)] = color.B;
            pixels[y * stride + (x * 4) + 1] = color.G;
            pixels[y * stride + (x * 4) + 2] = color.R;
            pixels[y * stride + (x * 4) + 3] = color.A;
        }

        private static Color GetColor(byte[] pixels, int x, int y, int Stride)
        {
            return Color.FromArgb(
                pixels[y * Stride + (x * 4) + 3],
                pixels[y * Stride + (x * 4) + 2],
                pixels[y * Stride + (x * 4) + 1],
                pixels[y * Stride + (x * 4)]);
        }
    }
}
