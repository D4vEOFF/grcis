using System;
using System.Drawing;
using MathSupport;
using CircleCanvas;
using System.Globalization;
using System.Collections.Generic;
using Utilities;
using System.Linq;
using System.Windows.Forms;

namespace _083animation
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
    public Complex Conjugate => new Complex(Re, -Im);
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
      return $"{Re}" + (Im >= 0 ? "+" : "") + $"{Im}i";
    }
  }
  public class Animation
  {
    /// <summary>
    /// Form data initialization.
    /// </summary>
    /// <param name="name">Your first-name and last-name.</param>
    /// <param name="wid">Initial image width in pixels.</param>
    /// <param name="hei">Initial image height in pixels.</param>
    /// <param name="from">Start time (t0)</param>
    /// <param name="to">End time (for animation length normalization).</param>
    /// <param name="fps">Frames-per-second.</param>
    /// <param name="param">Optional text to initialize the form's text-field.</param>
    /// <param name="tooltip">Optional tooltip = param help.</param>
    public static void InitParams (out string name, out int wid, out int hei, out double from, out double to, out double fps, out string param, out string tooltip)
    {
      // Put your name here.
      name = "David Weber";

      // Frame size in pixels.
      wid = 640;
      hei = 480;

      // Animation.
      from =  0.0;
      to   = 10.0;
      fps  = 25.0;

      // Form params.
      param = "startAngle=0,endAngle=6.283185";
      tooltip = "startAngle=<double>,endAngle=<double>";


    }

    private static bool setParameters = false;

    private static Dictionary<string, double> parameters;         // user set parameters
    private static double angle = 0;                              // angle in 0.7885 * e^(i*a)
    private static double angleStep = 0;                          // step to increase the angle with

    private static Complex C;                                     // f(z) = z^2 + c


    /// <summary>
    /// Global initialization. Called before each animation batch
    /// or single-frame computation.
    /// </summary>
    /// <param name="width">Width of the future canvas in pixels.</param>
    /// <param name="height">Height of the future canvas in pixels.</param>
    /// <param name="start">Start time (t0)</param>
    /// <param name="end">End time (for animation length normalization).</param>
    /// <param name="fps">Required fps.</param>
    /// <param name="param">Text parameter field from the form.</param>
    public static void InitAnimation (int width, int height, double start, double end, double fps, string param)
    {
      // Set parameters for current frame
      if (!setParameters)
      {
        // Parse user params
        parameters = new Dictionary<string, double>();

        try
        {
          parameters["startAngle"] = 0;
          parameters["endAngle"] = 2 * Math.PI;
          foreach (var p in param.Split(',').Select(x => x.Split('=')))
          {
            switch (p.First())
            {
              case "startAngle":
                parameters["startAngle"] = double.Parse(p.Last());
                break;
              case "endAngle":
                parameters["endAngle"] = double.Parse(p.Last());
                break;
            }
          }
        }
        catch (Exception e)
        {
          MessageBox.Show("ERROR: " + e.Message);
        }

        angleStep = (parameters["endAngle"] - parameters["startAngle"]) / (fps * end);
        C = 0.7885 * new Complex(Math.Cos(angle), Math.Sin(angle));

        setParameters = true;
      }

    }

    /// <summary>
    /// Draw single animation frame.
    /// </summary>
    /// <param name="c">Canvas to draw to.</param>
    /// <param name="time">Current time in seconds.</param>
    /// <param name="start">Start time (t0)</param>
    /// <param name="end">End time (for animation length normalization).</param>
    /// <param name="param">Optional string parameter from the form.</param>
    public static void DrawFrame (Canvas c, double time, double start, double end, string param)
    {
      
    }
  }
}
