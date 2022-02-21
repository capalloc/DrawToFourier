using System;
using System.Collections;
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

    // Implements 'IEnumerable' such that it returns lines of the path when iterated
    public class Path : IEnumerable<Line>
    {
        // Returns a Cubic Bezier Function for easily calculating the bezier points between given two lines, and 'lengthToDistanceFactor' determines how far
        // the reference points of p1 and p2 are from p0 and p3, as a factor of the distance between starting and ending points p0 and p3.
        public static Func<double, Point> cubicBezierGenerator(Line startLine, Line endLine, double lengthToDistanceFactor)
        {
            double distance = (endLine.Start - startLine.End).Length;

            Vector[] pointVector = new Vector[] {
                ((Vector)startLine.End),
                ((Vector)startLine.End) + startLine.Normalized * distance * lengthToDistanceFactor,
                ((Vector)endLine.Start) - endLine.Normalized * distance * lengthToDistanceFactor,
                ((Vector)endLine.Start)
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
        public double Length { get { double length = 0; foreach (Line line in this) length += line.Length; return length; } }
        
        private Point origin;
        private LinkedList<Line> lines;

        private Line bezierEndLine;
        private int bezierModePhase; 

        public Path(Point origin)
        {
            this.origin = origin;
            this.lines = new LinkedList<Line>();
            this.LineCount = 0;
            this.bezierEndLine = new Line();
            this.bezierModePhase = 0;
        }

        public LinkedList<Line> AddPoint(Point p)
        {
            LinkedList<Line> newLines = new LinkedList<Line>();
            Line newLine;

            if (this.bezierModePhase == 0) // Bezier mode is disabled
            {
                if (this.lines.Count == 0) // If this is the first point beside the origin (i.e. first edge)
                {
                    newLine = new Line(this.origin, p, true);
                    newLines.AddLast(newLine);
                    this.lines.AddLast(newLine);
                }
                else
                {
                    newLine = new Line(this.lines.Last().End, p, true);
                    newLines.AddLast(newLine);
                    this.lines.AddLast(newLine);
                }

                this.LineCount++;
            } 
            else if (this.bezierModePhase == 2) // Reserve current point at next call for bezier ending edge
            {
                this.bezierEndLine.Start = p;
                this.bezierEndLine.IsSolid = true;
                this.bezierModePhase = 1;
            } 
            else if (this.bezierModePhase == 1) // Complete bezier ending edge and calculate/append bezier curve edges
            {
                this.bezierEndLine.End = p;

                Func<double, Point> bezierFunc = cubicBezierGenerator(this.lines.Last(), this.bezierEndLine, 0.5);

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
                this.bezierModePhase = 0;
                this.bezierEndLine = new Line();
            }

            return newLines;
        }

        // Complete the path by linking the origin and last point
        public LinkedList<Line> Finish(bool isSolid)
        {
            LinkedList<Line> newLines = new LinkedList<Line>();

            if (this.LineCount == 1) // If the path consists of a single line, linearly connect.
            {
                Line newLine = new Line(this.lines.Last().End, this.origin, isSolid);
                newLines.AddLast(newLine);
                this.lines.AddLast(newLine);

                this.LineCount++;
            } 
            else if (this.LineCount > 1) // If the path consists of multiple edges, use bezier to connect.
            {
                Func<double, Point> bezierFunc = cubicBezierGenerator(this.lines.Last(), this.lines.First(), 0.5);
                Line newLine;

                for (int i = 1; i < 20; i++)
                {
                    double t = i * 0.05;
                    newLine = new Line(this.lines.Last().End, bezierFunc(t), isSolid);
                    newLines.AddLast(newLine);
                    this.lines.AddLast(newLine);
                    this.LineCount++;
                }

                newLine = new Line(this.lines.Last().End, this.origin, isSolid);
                newLines.AddLast(newLine);
                this.lines.AddLast(newLine);
                this.LineCount++;
            }

            return newLines;
        }
        // Connect the next edge by bezier curve, starts the bezier handling sequence
        public void SetBezierNext()
        {
            this.bezierModePhase = 2;
        }

        public IEnumerator<Line> GetEnumerator()
        {
            foreach (Line line in this.lines)
                yield return line;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
