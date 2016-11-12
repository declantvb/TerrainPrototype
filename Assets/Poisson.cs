using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class Poisson
{
	// from http://devmag.org.za/2009/05/03/poisson-disk-sampling/
	public static IEnumerator<Vector2> generate_poisson(System.Random rand, int size, float min_dist, int points_per_iteration)
	{
		//Create the grid
		var cellSize = min_dist / Mathf.Sqrt(2);

		var grid = new Dictionary<GridCell, Vector2>();

		//RandomQueue works like a queue, except that it
		//pops a random element from the queue instead of
		//the element at the head of the queue
		var processList = new List<Vector2>();
		var samplePoints = new List<Vector2>();

		//generate the first point randomly
		var firstPoint = new Vector2(
			(float)rand.NextDouble() * size * 2 - size,
			(float)rand.NextDouble() * size * 2 - size);

		//update containers
		processList.Add(firstPoint);
		yield return firstPoint;
		var firstGridPos = imageToGrid(firstPoint, cellSize);
		grid[new GridCell(x: (int)firstGridPos.x, y: (int)firstGridPos.y)] = firstPoint;

		//generate other points from points in queue.
		while (processList.Any())
		{
			var index = rand.Next(0, processList.Count);
			var point = processList.ElementAt(index);
			processList.RemoveAt(index);

			for (int i = 0; i < points_per_iteration; i++)
			{
				var newPoint = generateRandomPointAround(rand, point, min_dist);

				var inBounds =
					newPoint.x < size &&
					newPoint.x > -size &&
					newPoint.y < size &&
					newPoint.y > -size;

				if (inBounds && !inNeighbourhood(grid, newPoint, min_dist, cellSize))
				{
					processList.Add(newPoint);
					yield return newPoint;
					var gridPos = imageToGrid(newPoint, cellSize);
					grid[new GridCell(x: (int)gridPos.x, y: (int)gridPos.y)] = newPoint;
				}
			}
		}
	}

	public static Vector2 imageToGrid(Vector2 point, float cellSize)
	{
		var gridX = (int)(point.x / cellSize);
		var gridY = (int)(point.y / cellSize);
		return new Vector2(gridX, gridY);
	}

	public static Vector2 generateRandomPointAround(System.Random rand, Vector2 point, float mindist)
	{ //non-uniform, favours points closer to the inner ring, leads to denser packings
		var r1 = (float)rand.NextDouble(); //random point between 0 and 1
		var r2 = (float)rand.NextDouble();
		//random radius between mindist and 2 * mindist
		var radius = mindist * (r1 + 1);
		//random angle
		var angle = 2 * Mathf.PI * r2;
		//the new point is generated around the point (x, y)
		var newX = point.x + radius * Mathf.Cos(angle);
		var newY = point.y + radius * Mathf.Sin(angle);
		return new Vector2(newX, newY);
	}

	public static bool inNeighbourhood(Dictionary<GridCell, Vector2> grid, Vector2 point, float mindist, float cellSize)
	{
		var gridPos = imageToGrid(point, cellSize);

		for (int i = (int)gridPos.x - 2; i < (int)gridPos.x + 2; i++)
		{
			for (int j = (int)gridPos.y - 2; j < (int)gridPos.y + 2; j++)
			{
				var cell = new GridCell(x: i, y: j);
				if (grid.ContainsKey(cell) && (grid[cell] - point).magnitude < mindist)
				{
					return true;
				}
			}
		}

		return false;
	}

	public struct GridCell : IEquatable<GridCell>
	{
		private readonly int x;
		private readonly int y;

		public int X { get { return x; } }
		public int Y { get { return y; } }

		public GridCell(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public override int GetHashCode()
		{
			var hash = 67;
			hash = hash * 43 + x;
			hash = hash * 19 + y;
			return hash;
		}

		public override bool Equals(object other)
		{
			return other is GridCell ? Equals((GridCell)other) : false;
		}

		public bool Equals(GridCell other)
		{
			return other.x == x
				&& other.y == y;
		}
	}
}