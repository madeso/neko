namespace Neko
{
    using System;
    using System.Drawing;

    static internal class Vector
    {
        public static PointF Scale(PointF a, double d)
        {
            return new PointF((float)(a.X * d), (float)(a.Y *d));
        }

        public static double Distance(PointF a, PointF b)
        {
            var diff = Diff(a, b);
            return Length(diff);
        }

        public static double Length(PointF diff)
        {
            return Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
        }

        public static PointF Diff(PointF a, PointF b)
        {
            return new PointF(a.X - b.X, a.Y - b.Y);
        }

        public static PointF Add(PointF a, PointF b)
        {
            return new PointF(a.X + b.X, a.Y + b.Y);
        }

        public static PointF Unit(PointF dir)
        {
            return Scale(dir, 1/Length(dir));
        }

        public static double Angle(PointF dir)
        {
            var ang = Math.Asin(dir.Y) * (180 / Math.PI) + 90;
            var rad = dir.X < 0 ? 360 - ang : ang;
            return rad;
        }
    }
}