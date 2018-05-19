using System;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;
using VTC.Common.RegionConfig;
using System.Collections.Generic;

namespace VTC.Kernel.Extensions
{
    public static class PointExtensions
    {
        public static double DistanceTo(this Point a, Point b)
        {
            var distanceX = Math.Abs(a.X - b.X);
            var distanceY = Math.Abs(a.Y - b.Y);

            var distance = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));

            return distance;
        }

        public static int NearestNeighborIndex(this Point point, IEnumerable<Point> neighbors)
        {
            // Assumes at least one neighbor

            int nearest = 0;
            double nearestDistance = double.MaxValue;
            var neighborsList = neighbors.ToList();

            for (int i = 0; i < neighborsList.Count; i++)
            {
                var dist = point.DistanceTo(neighborsList[i]);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearest = i;
                }
            }

            return nearest;
        }

        public static Point NearestNeighbor(this Point point, IEnumerable<Point> neighbors)
        {
            // Assumes at least one neighbor

            Point nearest = neighbors.First();
            double nearestDistance = double.MaxValue;

            foreach (var pt in neighbors)
            {
                var dist = point.DistanceTo(pt);
                if (!(dist < nearestDistance)) continue;
                nearestDistance = dist;
                nearest = pt;
            }

            return nearest;
        }

        public static bool PolygonEnclosesPoint(this Point point, Polygon poly)
        {
            if (null == poly || poly.Count < 3 || !poly.PolygonClosed) return false;
            
            // Quick short-circuits
            var minX = poly.Min(p => p.X);
            if (point.X < minX) return false;
            var minY = poly.Min(p => p.Y);
            if (point.Y < minY) return false;
            var maxX = poly.Max(p => p.X);
            if (point.X > maxX) return false;
            var maxY = poly.Max(p => p.Y);
            if (point.Y > maxY) return false;

            var white = new Bgr(Color.White);
            var black = new Bgr(Color.Black);

            var width = maxX + 1;
            var height = maxY + 1;
            var mask = poly.GetMask(width, height, white);
            var pointImage = new Image<Bgr, float>(width, height, black);
            pointImage.Draw(new CircleF(new PointF(point.X, point.Y), 0), white, 0);

            var masked = pointImage.And(mask);

            var maskedPointColor = masked[point].MCvScalar;

            if (maskedPointColor.Equals(new Bgr(Color.White).MCvScalar))
            {
                return true;
            }

            return false;
        }
    }
}
