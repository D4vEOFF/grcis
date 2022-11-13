using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;
using CircleCanvas;
using MathSupport;

namespace _082circles
{
  public struct Complex
  {
    /// <summary>
    /// Squared absolute value of complex number.
    /// </summary>
    public double AbsSquared => Re * Re + Im * Im;
    /// <summary>
    /// Absolute value of complex number.
    /// </summary>
    public double Abs => Math.Sqrt(AbsSquared);
    /// <summary>
    /// Conjugate of complex number.
    /// </summary>
    public Complex Conjugate => new Complex(Re, - Im);
    /// <summary>
    /// Real part of complex number.
    /// </summary>
    public double Re { get; set; }
    /// <summary>
    /// Imaginary part of complex number.
    /// </summary>
    public double Im { get; set; }
    /// <summary>
    /// Creates a new complex number.
    /// </summary>
    /// <param name="real">Real part of the complex number-</param>
    /// <param name="imag">Imaginary part of the complex number.</param>
    public Complex (double real = 0, double imag = 0)
    {
      Re = real;
      Im = imag;
    }
    public static bool operator == (Complex left, Complex right) => left.Re == right.Re &&
      left.Im == right.Im;
    public static bool operator == (Complex left, double right)
    {
      Complex r = new Complex(right, 0);
      return left == r;
    }
    public static bool operator == (double left, Complex right)
    {
      Complex l = new Complex(left, 0);
      return l == right;
    }
    public static bool operator != (Complex left, Complex right) => !(left == right);
    public static bool operator != (Complex left, double right) => !(left == right);
    public static bool operator != (double left, Complex right) => !(left == right);

    public static Complex operator + (Complex left, Complex right) => new Complex(left.Re +
      right.Re, left.Im + right.Im);
    public static Complex operator + (double left, Complex right) => right + new Complex(left, 0);
    public static Complex operator + (Complex left, double right) => left + new Complex(right, 0);

    public static Complex operator - (Complex left, Complex right) => new Complex(left.Re -
      right.Re, left.Im - right.Im);
    public static Complex operator - (double left, Complex right) => right + new Complex(left, 0);
    public static Complex operator - (Complex left, double right) => left + new Complex(right, 0);

    public static Complex operator * (Complex left, Complex right) => new Complex(left.Re *
      right.Re - left.Im * right.Im, left.Re * right.Im + left.Im * right.Re);
    public static Complex operator * (double left, Complex right) => right * new Complex(left, 0);
    public static Complex operator * (Complex left, double right) => left * new Complex(right, 0);
    public static Complex operator / (Complex left, Complex right)
    {
      Complex res = left * right.Conjugate;
      double absVal = right.AbsSquared;

      res.Re /= absVal;
      res.Im /= absVal;

      return res;
    }
    public static Complex operator / (double left, Complex right) => right / new Complex(left, 0);
    public static Complex operator / (Complex left, double right) => left / new Complex(right, 0);

    public override string ToString ()
    {
      if (Im == 0)
        return Re.ToString();
      if (Math.Abs(Im) == 1)
        return Re.ToString() + (Im == 1 ? "+i" : "-i");
      return $"{Re}" + (Im >= 0 ? "+" : "") + $"{Im }i";
    }
  }
  public class Circles
  {
    /// <summary>
    /// Form data initialization.
    /// </summary>
    /// <param name="name">Your first-name and last-name.</param>
    /// <param name="wid">Initial image width in pixels.</param>
    /// <param name="hei">Initial image height in pixels.</param>
    /// <param name="param">Optional text to initialize the form's text-field.</param>
    /// <param name="tooltip">Optional tooltip = param help.</param>
    public static void InitParams (out string name, out int wid, out int hei, out string param, out string tooltip)
    {
      name    = "David Weber";
      wid     = 800;
      hei     = 520;
      param   = "100,5";
      tooltip = "<uint>,<uint> ... a pair of values: MAXIMUM NUMBER OF ITERATIONS " +
        "and STEP (10 by default)";
    }

    /// <summary>
    /// Draw the image into the initialized Canvas object.
    /// </summary>
    /// <param name="c">Canvas ready for your drawing.</param>
    /// <param name="param">Optional string parameter from the form.</param>
    public static void Draw (Canvas c, string param)
    {
      // Convert param value
      uint[] input = null;
      try
      {
        input = param.Split(',').Select(uint.Parse).ToArray();
        if (input.Length > 2)
          throw new ArgumentOutOfRangeException("Only two params at most are allowed.");
        if (input.Length == 0)
          throw new ArgumentOutOfRangeException("No params were passed.");
      }
      catch (Exception e)
      {
        MessageBox.Show($"ERROR: {e.Message}");
        return;
      }

      uint maxIterations = input[0];
      uint step = 0;
      if (input.Length == 2)
        step = input[1];
      else
        step = 10;

      Dictionary<uint, List<PointF>> points = new Dictionary<uint, List<PointF>>();

      // Get points belonging to Mandelbrot Set
      uint radius = 5;
      object obj = new object();
      Parallel.For(0, 1 + maxIterations / step, i =>
      {
        uint j = (uint)i * step;
        List<PointF> mandelbrot = GetMandelbrot(j, radius, (uint)c.Width, (uint)c.Height);

        lock (obj)
        {
          points[j] = mandelbrot;
        }
      });

      // Draw Mandelbrot Set
      for (uint i = 0; i <= maxIterations; i += step)
      {
        double hue = 30 + Math.Round((double)120 * i / maxIterations);
        c.SetColor(Arith.HSVToColor(hue, 1, .7));
        foreach (var point in points[i])
          c.FillDisc(point.X, point.Y, radius);
      }
    }
    /// <summary>
    /// Returns a list of points belonging to Mandelbrot Set.
    /// </summary>
    /// <param name="maxIterations">Maximum number of iterations.</param>
    /// <param name="radius">Circle radius.</param>
    /// <param name="width">Canvas width.</param>
    /// <param name="height">Canvas heigth.</param>
    /// <returns>List of points in the set.</returns>
    public static List<PointF> GetMandelbrot(uint maxIterations, uint radius, uint width, uint height)
    {
      // Setup initial drawing params
      float minRe = -2;
      float maxRe = 1;
      float minIm = -1;
      float maxIm = 1;
      float stepRe = radius * (maxRe - minRe) / width;
      float stepIm = radius * (maxIm - minIm) / height;

      // Get Mandelbrot set points
      List<PointF> pointsInSet = new List<PointF>();
      Complex number = new Complex();
      for (float re = minRe; re < maxRe; re += stepRe)
        for (float im = minIm; im < maxIm; im += stepIm)
        {
          number.Re = re;
          number.Im = im;

          uint iterations = BelongsToMandelbrot(number, maxIterations);

          float x = radius * (re - minRe) / stepRe;
          float y = radius * (im - minIm) / stepIm;

          if (iterations == maxIterations)
            pointsInSet.Add(new PointF(x, y));
        }

      return pointsInSet;
    }
    /// <summary>
    /// Draws Mandelbrot Set upon a given canvas.
    /// </summary>
    /// <param name="canvas">Canvas to draw on.</param>
    /// <param name="radius">Radius of circles.</param>
    /// <param name="maxIterations">Maximum number of iterations.</param>
    /// <param name="color">Color of the circles.</param>
    public static void DrawMandelbrot(Canvas canvas, float radius, uint maxIterations, Color color)
    {
      // Setup initial drawing params
      float minRe = -2;
      float maxRe = 1;
      float minIm = -1;
      float maxIm = 1;
      float stepRe = radius * (maxRe - minRe) / canvas.Width;
      float stepIm = radius * (maxIm - minIm) / canvas.Height;

      canvas.SetColor(Color.White);

      // Draw Mandelbrot Set
      Complex number = new Complex();
      canvas.SetColor(color);
      for (float re = minRe; re < maxRe; re += stepRe)
        for (float im = minIm; im < maxIm; im += stepIm)
        {
          number.Re = re;
          number.Im = im;

          uint iterations = BelongsToMandelbrot(number, maxIterations);

          float x = radius * (re - minRe) / stepRe;
          float y = radius * (im - minIm) / stepIm;

          if (iterations == maxIterations)
            canvas.FillDisc(x, y, radius);
        }
    }
    /// <summary>
    /// Number of iterations it takes for the complex number to diverge out the
    /// radius (if ever).
    /// </summary>
    /// <param name="number">Number to check.</param>
    /// <param name="maxIterations">Max number of iterations.</param>
    /// <returns>Number of iterations.</returns>
    public static uint BelongsToMandelbrot(Complex number, uint maxIterations)
    {
      Complex z = new Complex(0, 0);
      uint i = 0;
      while (z.Abs < 2 && i < maxIterations)
      {
        z = z * z + number;
        i++;
      }
      return i;
    }
  }
}
