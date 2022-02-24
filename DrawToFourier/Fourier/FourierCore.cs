using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DrawToFourier.Fourier
{
    // Represents individual units of Fourier decomposition
    public struct FCircle
    {
        public int Degree { get; }
        public double RealCoeff { get; }
        public double ImgCoeff { get; }
        public double Freq { get; }
        public double Radius { get; }
        public Func<double, Vector> CirclePos { get; }

        public FCircle(int degree, double T, double realCoeff, double imgCoeff)
        {
            this.Degree = degree;
            this.RealCoeff = realCoeff;
            this.ImgCoeff = imgCoeff;
            this.Radius = Math.Sqrt(Math.Pow(realCoeff, 2) + Math.Pow(imgCoeff, 2));
            double w = this.Freq = 2 * Math.PI * degree / T;
            this.CirclePos = (s) =>
            {
                return new Vector(
                    realCoeff * Math.Cos(w * s) - imgCoeff * Math.Sin(w * s), 
                    realCoeff * Math.Sin(w * s) + imgCoeff * Math.Cos(w * s)
                );
            };
        }
    }

    // Business logic of the program, i.e. computations directly related to Fourier resides here.
    public class FourierCore
    {
        public FCircle BaseCircle { get { return this.baseCircle; } }
        public SortedList<double, FCircle> Circles { get { return this.circles; } }

        private FCircle baseCircle;
        private SortedList<double, FCircle> circles;
        private double? nonSolidStart; // Where in the path does non-solid path begins

        public FourierCore(Path path, int circleCount)
        {
            this.circles = new SortedList<double, FCircle>(Comparer<double>.Create((x, y) => y.CompareTo(x)));

            double[] realCoeff = new double[circleCount];
            double[] imgCoeff = new double[circleCount];
            int degreeOffset = circleCount / 2;

            double T = path.Length;
            double cumulativeS = 0;

            foreach (Line line in path)
            {
                Vector a = line.Normalized;
                Vector b = -a * cumulativeS + (Vector)line.Start;
                double s_f = line.Length + cumulativeS;
                double s_s = cumulativeS;
                double fx_f = a.X * s_f + b.X;
                double fx_s = a.X * s_s + b.X;
                double fy_f = a.Y * s_f + b.Y;
                double fy_s = a.Y * s_s + b.Y;

                int k = -degreeOffset;
                int upper_b = 0;

                // Non-zero degree fourier coefficient calculation
                nonzero:
                while (k < upper_b)
                {
                    double w = 2 * Math.PI * k / T;

                    realCoeff[k + degreeOffset] += (
                        (w * fx_f * Math.Sin(w * s_f) + a.X * Math.Cos(w * s_f) - w * fy_f * Math.Cos(w * s_f) + a.Y * Math.Sin(w * s_f))
                        - (w * fx_s * Math.Sin(w * s_s) + a.X * Math.Cos(w * s_s) - w * fy_s * Math.Cos(w * s_s) + a.Y * Math.Sin(w * s_s)))
                        / (Math.Pow(w, 2) * T);

                    imgCoeff[k + degreeOffset] += (
                        (w * fx_f * Math.Cos(w * s_f) - a.X * Math.Sin(w * s_f) + w * fy_f * Math.Sin(w * s_f) + a.Y * Math.Cos(w * s_f))
                        - (w * fx_s * Math.Cos(w * s_s) - a.X * Math.Sin(w * s_s) + w * fy_s * Math.Sin(w * s_s) + a.Y * Math.Cos(w * s_s)))
                        / (Math.Pow(w, 2) * T);

                    k++;
                }

                if (k != circleCount - degreeOffset) // Skip zero
                {
                    k = 1;
                    upper_b = circleCount - degreeOffset;
                    goto nonzero;
                }
                
                // Zero degree fourier coefficient calculation
                realCoeff[degreeOffset] += (
                    (a.X * Math.Pow(s_f, 2) / 2 + b.X * s_f)
                    - (a.X * Math.Pow(s_s, 2) / 2 + b.X * s_s))
                    / T;

                imgCoeff[degreeOffset] += (
                    (a.Y * Math.Pow(s_f, 2) / 2 + b.Y * s_f)
                    - (a.Y * Math.Pow(s_s, 2) / 2 + b.Y * s_s))
                    / T;

                if (!line.IsSolid && this.nonSolidStart == null)
                    this.nonSolidStart = cumulativeS;

                cumulativeS += line.Length;
            }

            for (int k = -degreeOffset; k < circleCount - degreeOffset; k++)
            {
                FCircle newCircle = new FCircle(k, T, realCoeff[k + degreeOffset], imgCoeff[k + degreeOffset]);

                if (k == 0)
                    this.baseCircle = newCircle;
                else
                    this.circles.Add(newCircle.Radius, newCircle);
            }
        }
    }
}