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
        public Point Start { get; set; }
        public Point End { get; set; }
        public bool IsSolid { get; set; }

        public Line(Point start, Point end, bool isSolid)
        {
            this.Start = start;
            this.End = end;
            this.IsSolid = isSolid;
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

        public int LineCount { get; private set; }
        public bool BezierEnabled { get; set; }

        private Point origin;
        private LinkedList<Line> lines;
        private Line bezierEndLine;

        public Path(Point origin)
        {
            this.origin = origin;
            this.lines = new LinkedList<Line>();
            this.LineCount = 0;
            this.BezierEnabled = false;
            this.bezierEndLine.IsSolid = false;
        }

        public LinkedList<Line> addPoint(Point p)
        {
            LinkedList<Line> newLines = new LinkedList<Line>();

            if (this.BezierEnabled)
            {   
                if (this.bezierEndLine.IsSolid == false)
                {
                    this.bezierEndLine.Start = p;
                    this.bezierEndLine.IsSolid = true;
                } 
                else
                {
                    this.bezierEndLine.End = p;

                    Func<double, Point> bezierFunc = cubicBezierGenerator(this.lines.Last().Start, this.lines.Last().End, this.bezierEndLine.Start, this.bezierEndLine.End, 0.5);
                    Line newLine;

                    for (int i = 1; i <= 20; i++)
                    {
                        double t = i * 0.05;
                        newLine = new Line(this.lines.Last().End, bezierFunc(t), true);
                        newLines.AddLast(newLine);
                        this.lines.AddLast(newLine);
                        this.LineCount++;
                    }

                    newLines.AddLast(bezierEndLine);
                    this.lines.AddLast(bezierEndLine);
                    this.LineCount++;

                    this.BezierEnabled = false;
                    this.bezierEndLine = new Line();
                }

                return newLines;
            }

            if (this.lines.Count == 0)
            {
                Line newLine = new Line(this.origin, p, true);
                newLines.AddLast(newLine);
                this.lines.AddLast(newLine);
            }
            else
            {
                Line newLine = new Line(this.lines.Last().End, p, true);
                newLines.AddLast(newLine);
                this.lines.AddLast(newLine);
            }
            
            this.LineCount++;

            return newLines;
        }

        public LinkedList<Line> finishSolid()
        {
            LinkedList<Line> newLines = new LinkedList<Line>();

            if (this.LineCount == 1)
            {
                Line newLine = new Line(this.lines.Last().End, this.origin, true);
                newLines.AddLast(newLine);
                this.lines.AddLast(newLine);

                this.LineCount++;
            } 
            else if (this.LineCount > 1)
            {
                Func<double, Point> bezierFunc = cubicBezierGenerator(this.lines.Last().Start, this.lines.Last().End, this.origin, this.lines.First().End, 0.5);
                Line newLine;

                for (int i = 1; i < 20; i++)
                {
                    double t = i * 0.05;
                    newLine = new Line(this.lines.Last().End, bezierFunc(t), true);
                    newLines.AddLast(newLine);
                    this.lines.AddLast(newLine);
                    this.LineCount++;
                }

                newLine = new Line(this.lines.Last().End, this.origin, true);
                newLines.AddLast(newLine);
                this.lines.AddLast(newLine);
                this.LineCount++;
            }

            return newLines;
        }

        public LinkedList<Line> finishTransparent()
        {
            LinkedList<Line> newLines = new LinkedList<Line>();

            if (this.LineCount == 1)
            {
                Line newLine = new Line(this.lines.Last().End, this.origin, false);
                newLines.AddLast(newLine);
                this.lines.AddLast(newLine);

                this.LineCount++;
            }
            else if (this.LineCount > 1)
            {
                Func<double, Point> bezierFunc = cubicBezierGenerator(this.lines.Last().Start, this.lines.Last().End, this.origin, this.lines.First().End, 0.5);
                Line newLine;

                for (int i = 1; i < 20; i++)
                {
                    double t = i * 0.05;
                    newLine = new Line(this.lines.Last().End, bezierFunc(t), false);
                    newLines.AddLast(newLine);
                    this.lines.AddLast(newLine);
                    this.LineCount++;
                }

                newLine = new Line(this.lines.Last().End, this.origin, false);
                newLines.AddLast(newLine);
                this.lines.AddLast(newLine);
                this.LineCount++;
            }

            return newLines;
        }
    }
}
