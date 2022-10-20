using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
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
    private float centroidChangeSquared;
    /// <summary>
    /// Cluster centroid.
    /// </summary>
    public Point3D Centroid { get => points[0]; }
    public float CentroidChangeSquared { get => centroidChangeSquared;  }

    /// <summary>
    /// Points included in the cluster.
    /// </summary>
    public IReadOnlyList<Point3D> Points { get => points.AsReadOnly(); }

    public Cluster (Point3D centroid)
    {
      centroidChangeSquared = float.PositiveInfinity;
      points = new List<Point3D> { centroid };
    }
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
      if (points.Count <= 1)
        return;

      Point3D oldCentroid = points[0];

      points[0] = new Point3D();
      for (int i = 1; i < points.Count; i++)
        points[0] += points[i];
      points[0] /= points.Count - 1;

      // Calculate centroid change
      Point3D newCentroid = points[0];
      float dx = newCentroid.X - oldCentroid.X;
      float dy = newCentroid.Y - oldCentroid.Y;
      float dz = newCentroid.Z - oldCentroid.Z;
      centroidChangeSquared = dx * dx + dy * dy + dz * dz;
    }
    public override string ToString () => string.Join(", ", points);
  }

  class Colormap
  {
    // Settings
    static int adjustedWidth = 400;
    static int eightBitClusterCount = 256;
    static int iterationCount = 50;
    static int minImageSize = 100;
    static int xSensitivity = 2;
    static int ySensitivity = 2;

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
      int width  = input.Width;
      int height = input.Height;
      int adjustedHeight = adjustedWidth * height / width;

      // If image too small, maximize sensitivity
      if (width <= minImageSize && height <= minImageSize)
      {
        xSensitivity = 1;
        ySensitivity = 1;
      }

      // Resize the image if bigger
      if (width > adjustedWidth)
      {
        width = adjustedWidth;
        height = adjustedHeight;
        input.SetResolution(width, height);
      }

      List<Point3D> points;
      GetPixels(input, out points);

      ////////////////////////////////////////////////////
      /* Perform clustering using the K-means algorithm */
      ////////////////////////////////////////////////////

      // Add white color if no points are present
      if (points.Count == 0)
        points.Add(new Point3D(255, 255, 255));

      Cluster[] clusters = InitializeClusters(input, points, numCol);

      // Iterate
      for (int iteration = 0; iteration < iterationCount; iteration++)
      {
        AssignPoints(clusters, points, height);
        foreach (var cluster in clusters)
          cluster.CalculateCentroid();
      }

      // Select the most dominant colors
      List<Cluster> clustersSorted = clusters.ToList();
      clustersSorted.Sort((cl1, cl2) => - cl1.Points.Count.CompareTo(cl2.Points.Count));           // Sort clusters according to the amount of included points

      // Convert centroids to colors
      List<Color> colorList = new List<Color>();
      for (int i = 0; i < clustersSorted.Count; i += clustersSorted.Count / numCol)
      {
        Point3D centroid = clustersSorted[i].Centroid;
        colorList.Add(Color.FromArgb(centroid.X, centroid.Y, centroid.Z));
      }

      // Sort colors
      colorList.Sort((c1, c2) => - c1.GetBrightness().CompareTo(c2.GetBrightness()));
      colors = colorList.ToArray();
    }

    /// <summary>
    /// Assigns points to clusters.
    /// </summary>
    /// <param name="clusters">Array of clusters.</param>
    /// <param name="points">Array of points.</param>
    private static void AssignPoints (Cluster[] clusters, List<Point3D> points, int height)
    {
      foreach (var cluster in clusters)
        cluster.Clear();

      for (int i = 0; i < points.Count; i += xSensitivity + ySensitivity * height)
      {
        float smallestDist = float.PositiveInfinity;
        int clusterIndex = -1;
        for (int k = 0; k < clusters.Length; k++)
        {
          float dist = EuclidSquared(points[i], clusters[k].Centroid);
          if (dist < smallestDist)
          {
            smallestDist = dist;
            clusterIndex = k;
          }
        }

        clusters[clusterIndex].Add(points[i]);
      }
    }
    /// <summary>
    /// Initializes clusters.
    /// </summary>
    /// <param name="points">Array of points to choose centroids from.</param>
    /// <returns>Array of one-centroided clusters.</returns>
    private static Cluster[] InitializeClusters (Bitmap input, List<Point3D> points, int numCol)
    {
      int formatSize = Image.GetPixelFormatSize(input.PixelFormat);
      if (formatSize <= 8)
        numCol = eightBitClusterCount;

      Cluster[] clusters = new Cluster[numCol];

      // Translation
      int translate = points.Count / (clusters.Length + 1);

      for (int i = 0; i < clusters.Length; i++)
        clusters[i] = new Cluster(points[i * translate]);

      return clusters;
    }

    /// <summary>
    /// Retrieves pixel color from the bitmap.
    /// </summary>
    /// <param name="bitmap">Bitmap.</param>
    /// <param name="points">Array with the retrieved colors.</param>
    private static void GetPixels (Bitmap bitmap, out List<Point3D> points)
    {
      int width = bitmap.Width;
      int height = bitmap.Height;
      points = new List<Point3D>();
      for (int i = 0; i < width; i++)
        for (int j = 0; j < height; j++)
        {
          Color pixelColor = bitmap.GetPixel(i, j);
          if (pixelColor.A > 5)
            points.Add(new Point3D(pixelColor.R, pixelColor.G, pixelColor.B));
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
