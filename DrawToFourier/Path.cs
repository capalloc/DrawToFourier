using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DrawToFourier.Fourier
{
    public struct Line
    {
        public Point Start { get; }
        public Point End { get; }

        public Line(Point start, Point end)
        {
            this.Start = start;
            this.End = end;
        }

        public Vector Normalized { get { Vector v = End - Start; v.Normalize(); return v; } }
        public double Length { get { Vector v = End - Start; return v.Length; } }
    }

    public class Path
    {
        // Returns a Cubic Bezier Function for easily calculating the bezier points between given two lines, with non-base points being the points of lines where bezier curve is
        // starting and ending (p0 and p3), and base points being the far end of those two lines (used for calculating slopes/sin/cos). 'lengthToDistanceFactor' determines how far
        // the reference points of p1 and p2 are from p0 and p3, as a factor of the distance between starting and ending points p0 and p3.
        public static Func<double, Point> cubicBezierGenerator(Point startPointBase, Point startPoint, Point endPoint, Point endPointBase, double lengthToDistanceFactor)
        {
            double distance = (endPoint - startPoint).Length;

            Vector normalizedStartVector = startPoint - startPointBase;
            Vector normalizedEndVector = endPoint - endPointBase;
            normalizedStartVector.Normalize();
            normalizedEndVector.Normalize();

            Vector[] pointVector = new Vector[] {
                ((Vector)startPoint),
                ((Vector)startPoint) + normalizedStartVector * distance * lengthToDistanceFactor,
                ((Vector)endPoint) + normalizedEndVector * distance * lengthToDistanceFactor,
                ((Vector)endPoint)
            };

            return (double t) => {
                double[] multiplyVector = new double[] {
                    Math.Pow(1 - t, 3),
                    3 * Math.Pow(1 - t, 2) * t,
                    3 * (1 - t) * Math.Pow(t, 2),
                    Math.Pow(t, 3)
                };

                return (Point)pointVector.Zip(multiplyVector, (pV, m) => pV * m).Aggregate(new Vector(0, 0), (prev, next) => prev + next);
            };
        }

        private Point? origin;
        private LinkedList<Line> lines; 

        public Path()
        {
            this.lines = new LinkedList<Line>();
        }

        public void addPoint(double x, double y)
        {
            if (origin == null)
            {
                origin = new Point(x, y);
                return;
            }
                
            Point lastPoint = this.lines.Last().End;
            Line newLine = new Line(new Point(x, y), lastPoint);
            this.lines.AddLast(newLine);
        }
    }
}
