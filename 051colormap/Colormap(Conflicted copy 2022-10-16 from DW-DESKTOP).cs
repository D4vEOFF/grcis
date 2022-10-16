using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MathSupport;

namespace _051colormap
{
  struct Point3D
  {
    private int x;
    private int y;
    private int z;
    /// <summary>
    /// X coordinate.
    /// </summary>
    public int X
    {
      get => x;
      set => x = value;
    }
    /// <summary>
    /// Y coordinate.
    /// </summary>
    public int Y
    {
      get => y;
      set => y = value;
    }
    /// <summary>
    /// Z coordinate.
    /// </summary>
    public int Z
    {
      get => z;
      set => z = value;
    }

    public Point3D (int x = 0, int y = 0, int z = 0)
    {
      this.x = x;
      this.y = y;
      this.z = z;
    }
    public override string ToString () => $"({x}, {y})";
    public static Point3D operator + (Point3D p1, Point3D p2) => new Point3D(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
    public static Point3D operator - (Point3D p1, Point3D p2) => new Point3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
    public static Point3D operator * (int m, Point3D p) => new Point3D(m * p.X, m * p.Y, m * p.Z);
    public static Point3D operator / (Point3D p, int d) => new Point3D(p.X / d, p.Y / d, p.Z / d);
    public static bool operator == (Point3D p1, Point3D p2) => p1.X == p2.X && p1.Y == p2.Y && p1.Z == p2.Z;
    public static bool operator != (Point3D p1, Point3D p2) => !(p1 == p2);
  }

  class Cluster
  {
    private List<Point3D> points;
    /// <summary>
    /// Cluster centroid.
    /// </summary>
    public Point3D Centroid { get => points[0]; }

    /// <summary>
    /// Points included in the cluster.
    /// </summary>
    public IReadOnlyList<Point3D> Points { get => points.AsReadOnly(); }

    public Cluster(Point3D centroid) => points = new List<Point3D> { centroid };
    /// <summary>
    /// Adds a point into the cluster.
    /// </summary>
    /// <param name="point">Point to add.</param>
    public void Add(Point3D point) => points.Add(point);
    /// <summary>
    /// Removes all points (except the centroid) from the cluster.
    /// </summary>
    public void Clear ()
    {
      Point3D centroid = points[0];
      points.Clear();
      points.Add(centroid);
    }
    public void CalculateCentroid ()
    {
      points[0] = new Point3D();
      for (int i = 1; i < points.Count; i++)
        points[0] += points[i];
      points[0] /= points.Count - 1;
    }
    public override string ToString () => string.Join(", ", points);
  }

  class Colormap
  {
    /// <summary>
    /// Form data initialization.
    /// </summary>
    public static void InitForm (out string author)
    {
      author = "David Weber";
    }

    /// <summary>
    /// Generate a colormap based on input image.
    /// </summary>
    /// <param name="input">Input raster image.</param>
    /// <param name="numCol">Required colormap size (ignore it if you must).</param>
    /// <param name="colors">Output palette (array of colors).</param>
    public static void Generate (Bitmap input, int numCol, out Color[] colors)
    {
      colors = new Color[numCol];            // accepting the required palette size..

      // Settings
      int adjustedWidth = 300;
      int iterationCount = 10;
      int xSensitivity = 10;
      int ySensitivity = 10;

      int width  = input.Width;
      int height = input.Height;
      int adjustedHeight = adjustedWidth * height / width;

      // Resize the image if bigger
      if (width > adjustedWidth)
      {
        width = adjustedWidth;
        height = adjustedHeight;
        input.SetResolution(width, height);
      }

      MessageBox.Show($"{input.HorizontalResolution}, {input.VerticalResolution}");

      Point3D[,] points;
      GetPixels(input, out points);

      ////////////////////////////////////////////////////
      /* Perform clustering using the K-means algorithm */
      ////////////////////////////////////////////////////

      Cluster[] clusters = new Cluster[numCol];
      InitializeClusters(clusters, points);

      // Iterate
      for (int iteration = 0; iteration < iterationCount; iteration++)
      {
        AssignPoints(clusters, points, xSensitivity, ySensitivity);
        foreach (var cluster in clusters)
          cluster.CalculateCentroid();
      }

      // Debug
      string output = "";
      for (int i = 0; i < numCol; i++)
        output += $"{i}. CLUSTER: " + clusters[i].ToString() + "\n";
      MessageBox.Show(output);
    }

    private static void AssignPoints (Cluster[] clusters, Point3D[,] points, int xSens, int ySens)
    {
      foreach (var cluster in clusters)
        cluster.Clear();

      for (int i = 0; i < points.GetLength(0); i += xSens)
        for (int j = 0; j < points.GetLength(1); j += ySens)
        {
          Point3D point = points[i, j];
          float smallestDist = float.PositiveInfinity;
          int clusterIndex = -1;
          for (int k = 0; k < clusters.Length; k++)
          {
            float dist = EuclidSquared(point, clusters[k].Centroid);
            if (dist < smallestDist)
            {
              smallestDist = dist;
              clusterIndex = k;
            }
          }

          clusters[clusterIndex].Add(point);
        }
    }
    /// <summary>
    /// Initializes clusters.
    /// </summary>
    /// <param name="clusters">Array of clusters.</param>
    /// <param name="points">Array of points to choose centroids from.</param>
    private static void InitializeClusters (Cluster[] clusters, Point3D[,] points)
    {
      int dimX = points.GetLength(0);
      int dimY = points.GetLength(1);
      Random rand = new Random();
      HashSet<Point3D> usedPoints = new HashSet<Point3D>();

      int i = 0;
      while (i < clusters.Length)
      {
        Point3D point = points[rand.Next(dimX), rand.Next(dimY)];
        if (usedPoints.Contains(point))
          continue;

        usedPoints.Add(point);
        clusters[i] = new Cluster(point);
        i++;
      }
    }

    /// <summary>
    /// Retrieves pixel color from the bitmap.
    /// </summary>
    /// <param name="bitmap">Bitmap.</param>
    /// <param name="points">Array with the retrieved colors.</param>
    private static void GetPixels (Bitmap bitmap, out Point3D[,] points)
    {
      int width = bitmap.Width;
      int height = bitmap.Height;
      points = new Point3D[width, height];
      for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
        {
          Color pixelColor = bitmap.GetPixel(i, j);
          points[i, j] = new Point3D(pixelColor.R, pixelColor.G, pixelColor.B);
        }
    }

    /// <summary>
    /// Calculates a squared Euclidean distance.
    /// </summary>
    /// <param name="p1">First point.</param>
    /// <param name="p2">Second point.</param>
    /// <returns>Squared Euclidean distance.</returns>
    private static float EuclidSquared (Point3D p1, Point3D p2)
    {
      Point3D diff = p1 - p2;
      return diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z;
    }
  }
}
