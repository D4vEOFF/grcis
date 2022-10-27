using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata;
using System.Text;
using System.Threading;
using MathSupport;
using OpenTK;
using OpenTK.Input;
using Utilities;

namespace _098svg
{
  public class CmdOptions : Options
  {
    /// <summary>
    /// Put your name here.
    /// </summary>
    public string name = "David Weber";

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static new CmdOptions options = (CmdOptions)(Options.options = new CmdOptions());

    public override void StringStatistics ( long[] result )
    {
      if ( result == null || result.Length < 4 )
        return;

      Util.StringStat( commands, result );
    }

    static CmdOptions ()
    {
      project = "svg098";
      TextPersistence.Register( new CmdOptions(), 0 );

      RegisterMsgModes( "debug" );
    }

    public CmdOptions ()
    {
      // default values of structured members:
      baseDir = @"./";
    }

    public static void Touch ()
    {
      if ( options == null )
        Util.Log( "CmdOptions not initialized!" );
    }

    //--- project-specific options ---

    /// <summary>
    /// Output directory with trailing dir separator.
    /// </summary>
    public string outDir = @"./";

    /// <summary>
    /// Number of maze columns (horizontal size in cells).
    /// </summary>
    public int columns = 12;

    /// <summary>
    /// Number of maze rows (vertical size in cells).
    /// </summary>
    public int rows = 8;

    /// <summary>
    /// Difficulty coefficient (optional).
    /// </summary>
    public double difficulty = 1.0;

    /// <summary>
    /// Maze width in SVG units (for SVG header).
    /// </summary>
    public double width = 600.0;

    /// <summary>
    /// Maze height in SVG units (for SVG header).
    /// </summary>
    public double height = 400.0;

    /// <summary>
    /// RandomJames generator seed, 0 for randomize.
    /// </summary>
    public long seed = 0L;

    /// <summary>
    /// Generate HTML5 file? (else - direct SVG format)
    /// </summary>
    public bool html = false;

    /// <summary>
    /// Start vertex X position.
    /// </summary>
    public int startX = 0;

    /// <summary>
    /// Start vertex Y position.
    /// </summary>
    public int startY = 0;

    /// <summary>
    /// Goal vertex X position.
    /// </summary>
    public int goalX = 0;

    /// <summary>
    /// Goal vertex Y position.
    /// </summary>
    public int goalY = 0;

    /// <summary>
    /// Parse additional keys.
    /// </summary>
    /// <param name="key">Key string (non-empty, trimmed).</param>
    /// <param name="value">Value string (non-null, trimmed).</param>
    /// <returns>True if recognized.</returns>
    public override bool AdditionalKey ( string key, string value, string line )
    {
      if ( base.AdditionalKey( key, value, line ) )
        return true;

      int newInt = 0;
      long newLong;
      double newDouble = 0.0;

      switch ( key )
      {
        case "startX":
          if (int.TryParse(value, out newInt) && newInt >= 0)
            startX = newInt;
          break;

        case "startY":
          if (int.TryParse(value, out newInt) && newInt >= 0)
            startY = newInt;
          break;

        case "goalX":
          if (int.TryParse(value, out newInt) && newInt >= 0)
            goalX = newInt;
          break;

        case "goalY":
          if (int.TryParse(value, out newInt) && newInt >= 0)
            goalY = newInt;
          break;

        case "outDir":
          outDir = value;
          break;

        case "name":
          name = value;
          break;

        case "columns":
          if ( int.TryParse( value, out newInt ) &&
               newInt > 0 )
            columns = newInt;
          break;

        case "rows":
          if ( int.TryParse( value, out newInt ) &&
               newInt > 0 )
            rows = newInt;
          break;

        case "difficulty":
          if ( double.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out newDouble ) &&
               newDouble > 0.0 )
            difficulty = newDouble;
          break;

        case "width":
          if ( double.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out newDouble ) &&
               newDouble > 0 )
            width = newDouble;
          break;

        case "height":
          if ( double.TryParse( value, NumberStyles.Float, CultureInfo.InvariantCulture, out newDouble ) &&
               newDouble > 0 )
            height = newDouble;
          break;

        case "seed":
          if ( long.TryParse( value, out newLong ) &&
               newLong >= 0L )
            seed = newLong;
          break;

        case "html":
          html = Util.positive( value );
          break;

        default:
          return false;
      }

      return true;
    }

    /// <summary>
    /// How to handle the "key=" config line?
    /// </summary>
    /// <returns>True if config line was handled.</returns>
    public override bool HandleEmptyValue ( string key )
    {
      switch ( key )
      {
        case "seed":
          seed = 0L;
          return true;
      }

      return false;
    }

    /// <summary>
    /// How to handle the non-key-value config line?
    /// </summary>
    /// <param name="line">The nonempty config line.</param>
    /// <returns>True if config line was handled.</returns>
    public override bool HandleCommand ( string line )
    {
      switch ( line )
      {
        case "generate":
          Program.Generate();
          return true;
      }

      return false;
    }
  }

  enum Direction
  {
    Up,
    Down,
    Left,
    Right
  }
  enum Difficulty
  {
    Peaceful,
    Easy,
    Normal,
    Hard,
    Hardest
  }

  interface IReadOnlyVertex
  {
    Vector2 GridPosition { get; }
    IReadOnlyList<IReadOnlyVertex> Neighbors { get; }
    bool HasNeighbor (Direction direction);
  }

  class Vertex : IEquatable<Vertex>, IReadOnlyVertex
  {
    private HashSet<Vertex> neighbors;
    private Vector2 gridPosition;
    /// <summary>
    /// Neighbor list of the vertex.
    /// </summary>
    public IReadOnlyList<IReadOnlyVertex> Neighbors => neighbors.ToList();
    /// <summary>
    /// Vertex position in the grid.
    /// </summary>
    public Vector2 GridPosition => gridPosition;
    /// <summary>
    /// Creates a new vertex instance.
    /// </summary>
    /// <param name="gridPosition">Vertex position in the grid.</param>
    /// <param name="neighbors">List of vertices the vertex is connected to.</param>
    public Vertex (int x, int y, params Vertex[] neighbors)
    {
      this.gridPosition = new Vector2(x, y);
      this.neighbors = new HashSet<Vertex>(neighbors);
    }
    /// <summary>
    /// Adds a vertex to the neighbor list.
    /// </summary>
    /// <param name="neighbor">Vertex to become a new neighbor.</param>
    /// <returns>True, if vertex not already a neighbor, otherwise false.</returns>
    public bool AddNeighbor (Vertex neighbor)
    {
      return neighbors.Add(neighbor);
    }
    /// <summary>
    /// Removes a vertex from the neighbor list.
    /// </summary>
    /// <param name="neighbor"></param>
    /// <returns>True, if vertex successfully removed, otherwise false.</returns>
    public bool RemoveNeighbor (Vertex neighbor)
    {
      return neighbors.Remove(neighbor);
    }
    /// <summary>
    /// Checks if a neighbor at a given position exists.
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public bool HasNeighbor (Direction direction)
    {
      Vector2 posToCheck;
      Vector2 gridDirection = new Vector2();
      switch (direction)
      {
        case Direction.Up:
          gridDirection.X = 0;
          gridDirection.Y = -1;
          break;
        case Direction.Down:
          gridDirection.X = 0;
          gridDirection.Y = 1;
          break;
        case Direction.Left:
          gridDirection.X = -1;
          gridDirection.Y = 0;
          break;
        case Direction.Right:
          gridDirection.X = 1;
          gridDirection.Y = 0;
          break;
      }
      Vector2.Add(ref gridPosition, ref gridDirection, out posToCheck);

      return !(neighbors.FirstOrDefault(n => n.GridPosition == posToCheck) is null);
    }

    public override string ToString ()
    {
      List<string> neighborPositions = new List<string>();
      foreach (var neighbor in neighbors)
        neighborPositions.Add(neighbor.GridPosition.ToString());
      return neighbors.Count == 0 ? gridPosition.ToString() : $"{gridPosition}: "
        + string.Join(", ", neighborPositions);
    }
    public static bool operator ==(Vertex u, Vertex v)
    {
      return u.Equals(v);
    }
    public static bool operator != (Vertex u, Vertex v)
    {
      return !(u == v);
    }
    public override bool Equals (object obj)
    {
      if (!(obj is Vertex))
        return false;
      return this.Equals((Vertex)obj);
    }
    public bool Equals(Vertex v)
    {
      if (v is null)
        return false;
      return this.gridPosition == v.gridPosition;
    }

    public override int GetHashCode ()
    {
      return 162320100 + gridPosition.GetHashCode();
    }
  }

  class UnionFind
  {
    private Dictionary<Vertex, uint> Depth;
    private Dictionary<Vertex, Vertex> Predecessor;
    public UnionFind (List<Vertex> vertices) : this(vertices.AsReadOnly()) { }
    public UnionFind (IReadOnlyList<Vertex> vertices)
    {
      Depth = new Dictionary<Vertex, uint>();
      Predecessor = new Dictionary<Vertex, Vertex>();
      foreach (var vertex in vertices)
      {
        Depth[vertex] = 0;
        Predecessor[vertex] = null;
      }
    }
    /// <summary>
    /// Return the root of a vertex.
    /// </summary>
    /// <param name="x">Vertex.</param>
    /// <returns>Root of input vertex.</returns>
    public Vertex Root (Vertex x)
    {
      while (!(Predecessor[x] is null))
        x = Predecessor[x];
      return x;
    }
    /// <summary>
    /// Determines whether given vertices belong to the same brush.
    /// </summary>
    /// <param name="u">First vertex.</param>
    /// <param name="v">Second vertex.</param>
    /// <returns>True, if given vertices belong to the same brush, otherwise false.</returns>
    public bool Find(Vertex u, Vertex v)
    {
      return Root(u) == Root(v);
    }
    /// <summary>
    /// Merges brushes of given vertices.
    /// </summary>
    /// <param name="u">First vertex.</param>
    /// <param name="v">Second vertex.</param>
    public void Union(Vertex u, Vertex v)
    {
      Vertex a = Root(u);
      Vertex b = Root(v);

      // Vertices are already in the same brush.
      if (a == b)
        return;

      if (Depth[a] < Depth[b])
        Predecessor[a] = b;
      else if (Depth[a] > Depth[b])
        Predecessor[b] = a;
      else
      {
        Predecessor[b] = a;
        Depth[a] += 1;
      }
    }
  }

  internal sealed class EdgeEqualityComparer : IEqualityComparer<Tuple<Vertex, Vertex>>
  {
    public bool Equals (Tuple<Vertex, Vertex> x, Tuple<Vertex, Vertex> y)
    {
      if (ReferenceEquals(x, y))
        return true;
      if (x is null || y is null)
        return false;

      return (x.Item1 == y.Item1 && x.Item2 == y.Item2) ||
        (x.Item1 == y.Item2 && x.Item2 == y.Item1);
    }

    public int GetHashCode (Tuple<Vertex, Vertex> obj)
    {
      return obj.Item1.GetHashCode() * obj.Item2.GetHashCode();
    }
  }

  class Grid
  {
    private HashSet<Vertex> vertices;
    /// <summary>
    /// Character used to print out the start vertex.
    /// </summary>
    public char StartVertexCharacter { get; set; }
    /// <summary>
    /// Character used to print out the goal vertex.
    /// </summary>
    public char GoalVertexCharacter { get; set; }
    /// <summary>
    /// Character used to print out the grid border.
    /// </summary>
    public char BorderCharacter { get; set; }
    /// <summary>
    /// Character used to print out a empty space in the grid.
    /// </summary>
    public char EmptySpaceCharacter { get; set; }
    /// <summary>
    /// Character used to print out vertically placed edges.
    /// </summary>
    public char VerticalEdgeCharacter { get; set; }
    /// <summary>
    /// Character used to print out horizontally placed edges.
    /// </summary>
    public char HorizontalEdgeCharacter { get; set; }
    /// <summary>
    /// Character used to print out vertices.
    /// </summary>
    public char VertexCharacter { get; set; }
    /// <summary>
    /// Difficulty of the generated maze ('Normal' by default).
    /// </summary>
    public Difficulty Difficulty { set; get; }
    /// <summary>
    /// Start vertex in the grid (null by default).
    /// </summary>
    public Vertex StartVertex { get; private set; }
    /// <summary>
    /// Goal vertex in the grid (null by default).
    /// </summary>
    public Vertex GoalVertex { get; private set; }
    /// <summary>
    /// Width of the grid.
    /// </summary>
    public int Width { get; private set; }
    /// <summary>
    /// Heigth of the grid.
    /// </summary>
    public int Heigth { get; private set; }
    /// <summary>
    /// List of grid vertices.
    /// </summary>
    public IReadOnlyList<IReadOnlyVertex> Vertices => vertices.ToList();
    public Grid(int width, int height)
    {
      // Drawing
      VertexCharacter = '░';
      HorizontalEdgeCharacter = '░';
      VerticalEdgeCharacter = '░';
      EmptySpaceCharacter = '█';
      BorderCharacter = '█';

      StartVertexCharacter = 'S';
      GoalVertexCharacter = 'G';

      // Maze settings
      Width = width;
      Heigth = height;

      vertices = new HashSet<Vertex>();

      StartVertex = null;
      GoalVertex = null;

      Difficulty = Difficulty.Normal;

      CreateNewGrid();
    }
    /// <summary>
    /// Initializes new no-edged grid of given dimensions.
    /// </summary>
    /// <param name="width">Grid width.</param>
    /// <param name="heigth">Grid height.</param>
    public void CreateNewGrid()
    {
      vertices.Clear();
      for (int x = 0; x < Width; x++)
        for (int y = 0; y < Heigth; y++)
          vertices.Add(new Vertex(x, y));
    }
    private Vertex GetVertex(int x, int y)
    {
      return vertices.FirstOrDefault(t => t.GridPosition == new Vector2(x, y));
    }
    private List<Tuple<Vertex, Vertex>> GetAllPossibleEdges ()
    {
      HashSet<Tuple<Vertex, Vertex>> edges = new HashSet<Tuple<Vertex, Vertex>>(new EdgeEqualityComparer());
      for (int x = 0; x < Width; x++)
      {
        for (int y = 0; y < Heigth; y++)
        {
          Vertex v = GetVertex(x, y);

          // Adjacent vertices
          Vertex up = GetVertex(x, y - 1);
          Vertex down = GetVertex(x, y + 1);
          Vertex left = GetVertex(x - 1, y);
          Vertex right = GetVertex(x, y + 1);

          // Add to the list of edges, if existent
          if (!(up is null))
            edges.Add(new Tuple<Vertex, Vertex>(v, up));
          if (!(down is null))
            edges.Add(new Tuple<Vertex, Vertex>(v, down));
          if (!(left is null))
            edges.Add(new Tuple<Vertex, Vertex>(v, left));
          if (!(right is null))
            edges.Add(new Tuple<Vertex, Vertex>(v, right));
        }
      }

      return edges.ToList();
    }
    /// <summary>
    /// Sets goal vertex to null.
    /// </summary>
    public void UnsetGoalVertex ()
    {
      GoalVertex = null;
    }
    /// <summary>
    /// Sets start vertex to null.
    /// </summary>
    public void UnsetStartVertex ()
    {
      StartVertex = null;
    }
    /// <summary>
    /// Set vertex at given position as goal.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    public void SetGoalVertex(int x, int y)
    {
      if (x >= Width || y >= Heigth || x < 0 || y < 0)
        throw new ArgumentOutOfRangeException("Start or goal vertex position out of range.");
      GoalVertex = GetVertex(x, y);
    }
    /// <summary>
    /// Set vertex at given position as start.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    public void SetStartVertex(int x, int y)
    {
      if (x >= Width || y >= Heigth || x < 0 || y < 0)
        throw new ArgumentOutOfRangeException("Start or goal vertex position out of range.");
      StartVertex = GetVertex(x, y);
    }
    /// <summary>
    /// Generates a maze using Kruskal's algorithm.
    /// </summary>
    public void GenerateKruskal (long seed = 0)
    {
      CreateNewGrid();

      UnionFind unionFind = new UnionFind(vertices.ToList());
      RandomJames random;

      // If seed 0, randomize
      random = new RandomJames(seed);
      if (seed == 0)
        random.Randomize();

      // Get all addable edges
      List<Tuple<Vertex, Vertex>> unusedEdges = GetAllPossibleEdges();

      // Apply Kruskal
      while (unusedEdges.Count > 0)
      {
        int randIndex = random.RandomInteger(0, unusedEdges.Count - 1);
        Tuple<Vertex, Vertex> edge = unusedEdges[randIndex];
        unusedEdges.RemoveAt(randIndex);

        Vertex u = edge.Item1;
        Vertex v = edge.Item2;

        if (!unionFind.Find(u, v))
        {
          AddEdge((int)u.GridPosition.X, (int)u.GridPosition.Y, (int)v.GridPosition.X, (int)v.GridPosition.Y);
          unionFind.Union(u, v);
        }
      }
    }
    /// <summary>
    /// Applies the set difficulty upon the generated maze.
    /// </summary>
    public void ApplyDifficulty ()
    {
      float[] chances = new float[] { .3f, .5f, .7f, .9f, 1.1f };
      float breakChance = chances[(int)Difficulty];

      RandomJames random = new RandomJames();
      random.Randomize();
      
      foreach (var vertex in vertices)
      {
        // There's a wall in given direction and the vertex is not on the border.
        float randomNumber;
        Vector2 vertexPos = vertex.GridPosition;
        if (!vertex.HasNeighbor(Direction.Up) && vertexPos.Y > 0)
        {
          randomNumber = random.RandomFloat(0, 1);
          if (randomNumber >= breakChance)
          {
            AddEdge((int)vertexPos.X, (int)vertexPos.Y, (int)vertexPos.X, (int)vertexPos.Y - 1);
            continue;
          }
        }
        if (!vertex.HasNeighbor(Direction.Down) && vertexPos.Y < Heigth - 1)
        {
          randomNumber = random.RandomFloat(0, 1);
          if (randomNumber >= breakChance)
          {
            AddEdge((int)vertexPos.X, (int)vertexPos.Y, (int)vertexPos.X, (int)vertexPos.Y + 1);
            continue;
          }
        }
        if (!vertex.HasNeighbor(Direction.Left) && vertexPos.X > 0)
        {
          randomNumber = random.RandomFloat(0, 1);
          if (randomNumber >= breakChance)
          {
            AddEdge((int)vertexPos.X, (int)vertexPos.Y, (int)vertexPos.X - 1, (int)vertexPos.Y);
            continue;
          }   
        }
        if (!vertex.HasNeighbor(Direction.Right) && vertexPos.X < Width - 1)
        {
          randomNumber = random.RandomFloat(0, 1);
          if (randomNumber >= breakChance)
          {
            AddEdge((int)vertexPos.X, (int)vertexPos.Y, (int)vertexPos.X + 1, (int)vertexPos.Y);
            continue;
          }
        }
      }
    }
    /// <summary>
    /// Adds an edge to the grid connecting specified vertices.
    /// </summary>
    /// <param name="x1">X coordinate of the first vertex.</param>
    /// <param name="y1">Y coordinate of the first vertex.</param>
    /// <param name="x2">X coordinate of the second vertex.</param>
    /// <param name="y2">Y coordinate of the second vertex.</param>
    /// <returns>True, if successfully added, otherwise false.</returns>
    public bool AddEdge(int x1, int y1, int x2, int y2)
    {
      Vertex u = vertices.FirstOrDefault(t => x1 == t.GridPosition.X && y1 == t.GridPosition.Y);
      Vertex v = vertices.FirstOrDefault(t => x2 == t.GridPosition.X && y2 == t.GridPosition.Y);

      // Vertex does not exist
      if (u == null || v == null)
        return false;

      // Edge is already present in the grid
      if (u.Neighbors.Contains(v) || v.Neighbors.Contains(u))
        return false;

      return u.AddNeighbor(v) && v.AddNeighbor(u);
    }
    /// <summary>
    /// Removes an edge from the grid connecting specified vertices.
    /// </summary>
    /// <param name="x1">X coordinate of the first vertex.</param>
    /// <param name="y1">Y coordinate of the first vertex.</param>
    /// <param name="x2">X coordinate of the second vertex.</param>
    /// <param name="y2">Y coordinate of the second vertex.</param>
    /// <returns>True, if successfully removed, otherwise false.</returns>
    public bool RemoveEdge(int x1, int y1, int x2, int y2)
    {
      Vertex u = vertices.First(t => x1 == t.GridPosition.X && y1 == t.GridPosition.Y);
      Vertex v = vertices.First(t => x2 == t.GridPosition.X && y2 == t.GridPosition.Y);

      // Vertex does not exist
      if (u == null || v == null)
        return false;

      // Edge is not present in the grid
      if (!u.Neighbors.Contains(v) && !v.Neighbors.Contains(u))
        return false;

      return u.RemoveNeighbor(v) && v.RemoveNeighbor(u);
    }
    public override string ToString ()
    {
      StringBuilder output = new StringBuilder();

      // Init output with border
      int resWidth = 2 * Width + 1;
      int resHeigth = 2 * Heigth + 1;
      for (int y = 1; y <= resHeigth; y++)
      {
        if (y == 1 || y == resHeigth)
          output.Append(BorderCharacter, resWidth);
        else
        {
          output.Append(BorderCharacter);
          output.Append(EmptySpaceCharacter, resWidth - 2);
          output.Append(BorderCharacter);
        }
        if (y != resHeigth)
          output.Append('\n');
      }

      // Put vertices and edges
      foreach (var vertex in vertices)
      {
        int index = (int)(2 * vertex.GridPosition.X + 1 + (2 * vertex.GridPosition.Y + 1) * (resWidth + 1));
        if (vertex == StartVertex)
          output[index] = StartVertexCharacter;
        else if (vertex == GoalVertex)
          output[index] = GoalVertexCharacter;
        else
          output[index] = VertexCharacter;

        // Print walls around vertex
        output[index - resWidth - 1] = vertex.HasNeighbor(Direction.Up) ? VerticalEdgeCharacter : EmptySpaceCharacter;
        output[index + resWidth + 1] = vertex.HasNeighbor(Direction.Down) ? VerticalEdgeCharacter : EmptySpaceCharacter;
        output[index - 1] = vertex.HasNeighbor(Direction.Left) ? HorizontalEdgeCharacter : EmptySpaceCharacter;
        output[index + 1] = vertex.HasNeighbor(Direction.Right) ? HorizontalEdgeCharacter : EmptySpaceCharacter;
      }

      return output.ToString();
    }
  }

  class Program
  {
    /// <summary>
    /// The 'generate' command was executed at least once..
    /// </summary>
    static bool wasGenerated = false;

    static void Main ( string[] args )
    {
      CmdOptions.Touch();

      if ( args.Length < 1 )
        Console.WriteLine( "Warning: no command-line options, using default values!" );
      else
        for ( int i = 0; i < args.Length; i++ )
          if ( !string.IsNullOrEmpty( args[ i ] ) )
          {
            string opt = args[ i ];
            if ( !CmdOptions.options.ParseOption( args, ref i ) )
              Console.WriteLine( $"Warning: invalid option '{opt}'!" );
          }

      if ( !wasGenerated )
        Generate();
    }

    /// <summary>
    /// Writes one polyline in SVG format to the given output stream.
    /// </summary>
    /// <param name="wri">Opened output stream (must be left open).</param>
    /// <param name="workList">List of vertices.</param>
    /// <param name="x0">Origin - x-coord (will be subtracted from all x-coords).</param>
    /// <param name="y0">Origin - y-coord (will be subtracted from all y-coords)</param>
    /// <param name="color">Line color (default = black).</param>
    static void drawCurve ( StreamWriter wri, List<Vector2> workList, double x0, double y0, string color = "#000" )
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat( CultureInfo.InvariantCulture, "M{0:f2},{1:f2}",
                       workList[ 0 ].X - x0, workList[ 0 ].Y - y0 );
      for ( int i = 1; i < workList.Count; i++ )
        sb.AppendFormat( CultureInfo.InvariantCulture, "L{0:f2},{1:f2}",
                         workList[ i ].X - x0, workList[ i ].Y - y0 );

      wri.WriteLine( "<path d=\"{0}\" stroke=\"{1}\" fill=\"none\"/>", sb.ToString(), color );
    }

    static public void Generate ()
    {
      wasGenerated = true;

      // Generate maze
      Grid maze = new Grid(CmdOptions.options.columns, CmdOptions.options.rows);
      maze.SetStartVertex(CmdOptions.options.startX, CmdOptions.options.startY);
      maze.SetGoalVertex(CmdOptions.options.goalX, CmdOptions.options.goalY);
      maze.GenerateKruskal(CmdOptions.options.seed);
      maze.Difficulty = (Difficulty)CmdOptions.options.difficulty;
      maze.ApplyDifficulty();
      Console.WriteLine(maze.ToString());


      string fileName = CmdOptions.options.outputFileName;
      if ( string.IsNullOrEmpty( fileName ) )
        fileName = CmdOptions.options.html ? "out.html" : "out.svg";
      string outFn = Path.Combine( CmdOptions.options.outDir, fileName );

      // SVG output:
      using ( StreamWriter wri = new StreamWriter( outFn ) )
      {
        if ( CmdOptions.options.html )
        {
          wri.WriteLine( "<!DOCTYPE html>" );
          wri.WriteLine( "<meta charset=\"utf-8\">" );
          wri.WriteLine( $"<title>SVG test ({CmdOptions.options.name})</title>" );
          wri.WriteLine( string.Format( CultureInfo.InvariantCulture, "<svg width=\"{0:f0}\" height=\"{1:f0}\">",
                                        CmdOptions.options.width, CmdOptions.options.height ) );
        }
        else
          wri.WriteLine( string.Format( CultureInfo.InvariantCulture, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{0:f0}\" height=\"{1:f0}\">",
                                        CmdOptions.options.width, CmdOptions.options.height ) );

        List<Vector2> workList = new List<Vector2>();
        RandomJames rnd = new RandomJames();
        if ( CmdOptions.options.seed > 0L )
          rnd.Reset( CmdOptions.options.seed );
        else
          rnd.Randomize();

        for ( int i = 0; i < CmdOptions.options.columns; i++ )
          workList.Add( new Vector2( rnd.RandomFloat( 0.0f, (float)CmdOptions.options.width ),
                                     rnd.RandomFloat( 0.0f, (float)CmdOptions.options.height ) ) );

        drawCurve( wri, workList, 0, 0, string.Format( "#{0:X2}{0:X2}{0:X2}", 0 ) );

        wri.WriteLine( "</svg>" );
      }
    }
  }
}
