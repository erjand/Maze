using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Maze
{
	/// <summary>
	/// Problem: Given the following maze what is the least number of pixels that need to be traveled to 
	/// get from the green dot to the red dot without going through any black lines. 
	/// </summary>
	public static class Program
	{
		public const int ImagePixelWidth = 807;
		public const int ImagePixelHeight = 810;
		private const string ImageFilePath = @"C:\Code\My Projects\Maze\maze.png";
		
		private static void Main()
		{
			var bitmap = new Bitmap(ImageFilePath);
			
			var binaryArray = BuildBinaryArray(bitmap);
			
			Graph graph = BuildGraph(binaryArray);

			// Based on visual analysis of the maze, these are likely the two closest nodes in each color-block
			Vertex startVertex = graph.GetVertexAtPosition(new Tuple<int, int>(10, 11));
			Vertex endVertex = graph.GetVertexAtPosition(new Tuple<int, int>(795, 798));

			var path = TraverseGraph(graph, startVertex, endVertex);

			Vertex finalStep = path.Pop();
			var numberOfStepsForShortestPath = finalStep.Distance;

			Console.WriteLine("\r\nLeast number of pixels traveled: " + numberOfStepsForShortestPath);
			Console.ReadLine();
		}

		/// <summary>
		/// Create a 2d binary array out of our Bitmap where 0 is white / passable,
		/// and 1 is black / impassable. The red / green squares are treated as passable.
		/// </summary>
		private static int[,] BuildBinaryArray(Bitmap pBitmap)
		{
			var binaryArray = new int[ImagePixelWidth, ImagePixelHeight];

			for (var i = 0; i < pBitmap.Width; i++)
			{
				for (var j = 0; j < pBitmap.Height; j++)
				{
					Color pixel = pBitmap.GetPixel(i, j);

					if (pixel.R == 255 && pixel.G == 255 && pixel.B == 255)
					{
						binaryArray[i, j] = 0; // White, or 0 (passable)
						continue;
					}

					if (pixel.R == 0 && pixel.G == 0 && pixel.B == 0)
					{
						binaryArray[i, j] = 1; // Black, or 1 (impassable)
						continue;
					}

					binaryArray[i, j] = 0; // Either the Red or Green block (passable)
				}
			}

			return binaryArray;
		}

		/// <summary>
		/// Returns a Graph constructed from the BinaryArray. 
		/// </summary>
		private static Graph BuildGraph(int[,] pBinaryArray)
		{
			var graph = new Graph();

			for (var i = 0; i < ImagePixelWidth; i++)
			{
				for (var j = 0; j < ImagePixelHeight; j++)
				{
					if (pBinaryArray[i, j] == 0)
					{
						var vertex = new Vertex(i, j);
						vertex.AddAdjacentEdges(pBinaryArray);
						graph.Vertices.Add(new Tuple<int, int>(i, j), vertex);
					}

					if (pBinaryArray[i,j] == 1)
					{
						var vertex = new Vertex(i, j); // A Black Vertex (or 1) has no adjacent edges, since it's not passable
						graph.Vertices.Add(new Tuple<int, int>(i, j), vertex);
					}
				}
			}

			return graph;
		}

		/// <summary>
		/// Traverse the Graph using a Breadth-First Search implementation.
		/// </summary>
		private static Stack<Vertex> TraverseGraph(Graph pGraph, Vertex pStartVertex, Vertex pEndVertex)
		{
			foreach (Vertex value in pGraph.Vertices.Values.ToList())
			{
				value.Distance = null;
				value.ParentVertex = null;
			}
			
			var queue = new Queue<Vertex>();
			var path = new Stack<Vertex>();

			pStartVertex.Distance = 0;
			queue.Enqueue(pStartVertex);

			while (queue.Count > 0)
			{
				Vertex currentVertex = queue.Dequeue();
				path.Push(currentVertex);

				// DEBUG Console
				Console.Write(
					"\rDEBUG: Current Distance: " + path.Peek().Distance + "; " +
					"Visiting Vertex (" + currentVertex.Position + ")");
				
				// If we've hit the end Vertex, we've found a path through
				if (Equals(currentVertex.Position, pEndVertex.Position))
				{
					return path;
				}

				foreach (Vertex adjacentVertex in currentVertex.GetAdjacentVertices())
				{
					Vertex vertex = pGraph.GetVertexAtPosition(new Tuple<int, int>(adjacentVertex.Position.Item1, adjacentVertex.Position.Item2));

					if (vertex.Distance == null)
					{
						vertex.Distance = currentVertex.Distance + 1;
						vertex.ParentVertex = currentVertex;
						queue.Enqueue(vertex);
					}
				}
			}

			// No successful route found
			return path;
		}
	}

	/// <summary>
	/// A Graph is a representation of our image comprised of a Dictionary of Vertices, where the
	/// KeyValuePair is the X,Y coordinate and the associated Vertex.
	/// </summary>
	public class Graph
	{
		public readonly Dictionary<Tuple<int, int>, Vertex> Vertices;
		
		public Graph()
		{
			Vertices = new Dictionary<Tuple<int, int>, Vertex>();
		}

		public Vertex GetVertexAtPosition(Tuple <int, int> pCoordinates)
		{
			Vertex vertex;
			Vertices.TryGetValue(pCoordinates, out vertex);
			return vertex;
		}
	}

	/// <summary>
	/// A Vertex, sometimes called a Node, is an X,Y coordinate in our image along with all adjacent Edges.
	/// The Vertex also stores it's Parent Vertex object, and it's Distance from the origin.
	/// </summary>
	public class Vertex
	{
		public readonly Tuple<int, int> Position;
		private readonly List<Edge> _adjacentEdges = new List<Edge>();

		public int? Distance;
		public Vertex ParentVertex;

		public Vertex(int pX, int pY)
		{
			Position = new Tuple<int, int>(pX, pY);
		}

		public void AddAdjacentEdges(int[,] pBinaryArray)
		{
			if (this.Position.Item2 != 0)
			{
				var northEdge = new Vertex(this.Position.Item1, this.Position.Item2 - 1);
				var arrayValue = pBinaryArray[northEdge.Position.Item1, northEdge.Position.Item2];
				if (arrayValue == 0)
				{
					_adjacentEdges.Add(new Edge(this, northEdge));
				}
			}

			if (this.Position.Item2 != Program.ImagePixelHeight - 1)
			{
				var southEdge = new Vertex(this.Position.Item1, this.Position.Item2 + 1);
				var arrayValue = pBinaryArray[southEdge.Position.Item1, southEdge.Position.Item2];
				if (arrayValue == 0)
				{
					_adjacentEdges.Add(new Edge(this, southEdge));
				}
			}

			if (this.Position.Item1 != 0)
			{
				var westEdge = new Vertex(this.Position.Item1 - 1, this.Position.Item2);
				var arrayValue = pBinaryArray[westEdge.Position.Item1, westEdge.Position.Item2];
				if (arrayValue == 0)
				{
					_adjacentEdges.Add(new Edge(this, westEdge));
				}
			}

			if (this.Position.Item1 != Program.ImagePixelWidth - 1)
			{
				var eastEdge = new Vertex(this.Position.Item1 + 1, this.Position.Item2);
				var arrayValue = pBinaryArray[eastEdge.Position.Item1, eastEdge.Position.Item2];
				if (arrayValue == 0)
				{
					_adjacentEdges.Add(new Edge(this, eastEdge));
				}
			}
		}

		public IEnumerable<Vertex> GetAdjacentVertices()
		{
			return _adjacentEdges.Select(edge => edge.GetToVertex()).ToList();
		}
	}

	/// <summary>
	/// An Edge is two connected Vertices.
	/// </summary>
	public class Edge
	{
		private Vertex _fromVertex;
		private readonly Vertex _toVertex;

		public Edge(Vertex pFromVertex, Vertex pToVertex)
		{
			_fromVertex = pFromVertex;
			_toVertex = pToVertex;
		}

		public Vertex GetToVertex()
		{
			return _toVertex;
		}


	}
}
