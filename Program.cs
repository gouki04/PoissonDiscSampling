using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace PoissonDiscSampling
{
    class Program
    {
        static void Main(string[] args)
        {
            var width = 1024;
            var height = 1024;
            var r = 50f;
            var points = Algorithm.Sample2D(width, height, r);

            var image = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(image)) {
                graphics.FillRectangle(Brushes.Black, 0f, 0f, width, height);

                var dot_r = 3f;
                var pen = new Pen(Color.DarkRed, 2f);
                foreach (var p in points) {
                    graphics.FillEllipse(Brushes.Yellow, p.x - dot_r, p.y - dot_r, 2f * dot_r, 2f * dot_r);
                    graphics.DrawEllipse(pen, p.x - r / 2f, p.y - r / 2f, r, r);
                }
            }

            image.Save("out.png");
        }
    }
}
