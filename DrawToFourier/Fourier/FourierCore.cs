using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DrawToFourier.Fourier
{
    // Business logic of the program, i.e. computations directly related to Fourier resides here.
    public class FourierCore
    {
        private double[] realCoeff = new double[19];
        private double[] imgCoeff = new double[19];
        private int arrayOffset = 9;

        public FourierCore(Path path)
        {
            double T = 0;

            foreach (Line line in path)
                T += line.Length;

            double cumulativeS = 0;

            foreach (Line line in path)
            {
                Vector a = line.Normalized;
                Vector b = -a * cumulativeS + (Vector)line.Start;

                for (int k = -arrayOffset; k < realCoeff.Length - arrayOffset; k++)
                {
                    double p = 2 * Math.PI * k / T;

                    if (k == 0)
                    {
                        Func<double, double> funcR = (s) =>
                        {
                            return a.X * Math.Pow(s, 2) / 2 + b.X * s;
                        };

                        Func<double, double> funcI = (s) =>
                        {
                            return a.Y * Math.Pow(s, 2) / 2 + b.Y * s;
                        };

                        realCoeff[k + arrayOffset] += (funcR(line.Length + cumulativeS) - funcR(cumulativeS));
                        imgCoeff[k + arrayOffset] += (funcI(line.Length + cumulativeS) - funcI(cumulativeS));
                    } 
                    else
                    {
                        Func<double, double> funcR = (s) =>
                        {
                            double fx = a.X * s + b.X;
                            double fy = a.Y * s + b.Y;
                            return p * fx * Math.Sin(p * s) + a.X * Math.Cos(p * s) - p * fy * Math.Cos(p * s) + a.Y * Math.Sin(p * s);
                        };

                        Func<double, double> funcI = (s) =>
                        {
                            double fx = a.X * s + b.X;
                            double fy = a.Y * s + b.Y;
                            return p * fx * Math.Cos(p * s) - a.X * Math.Sin(p * s) + p * fy * Math.Sin(p * s) + a.Y * Math.Cos(p * s);
                        };

                        realCoeff[k + arrayOffset] += (funcR(line.Length + cumulativeS) - funcR(cumulativeS)) / Math.Pow(p, 2);
                        imgCoeff[k + arrayOffset] += (funcI(line.Length + cumulativeS) - funcI(cumulativeS)) / Math.Pow(p, 2);
                    }
                }

                cumulativeS += line.Length;
            }

            for (int i = 0; i < realCoeff.Length; i++)
            {
                realCoeff[i] /= T;
                imgCoeff[i] /= T;
            }


            string resReal = "";
            string resImg = "";

            resReal += $"{realCoeff[arrayOffset]};";
            resImg += $"{imgCoeff[arrayOffset]};";

            for (int i = 1; i < 10;i++)
            {
                resReal += $"{realCoeff[arrayOffset + i]};";
                resReal += $"{realCoeff[arrayOffset - i]}";
                resImg += $"{imgCoeff[arrayOffset + i]};";
                resImg += $"{imgCoeff[arrayOffset - i]}";

                if (i != 9)
                {
                    resReal += ";";
                    resImg += ";";
                }
            }

            System.Diagnostics.Debug.WriteLine(resReal.Replace(',','.').Replace(';',','));
            System.Diagnostics.Debug.WriteLine(resImg.Replace(',', '.').Replace(';', ','));
            System.Diagnostics.Debug.WriteLine($"{T}");
        }
    }
}
