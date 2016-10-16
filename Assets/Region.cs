using System.Collections.Generic;
using Delaunay;
using UnityEngine;

internal class Region
{
	public Biome Biome { get; internal set; }
	public Rect Bounds { get; internal set; }
	public Vector2 Centre { get; internal set; }
	public List<Vector2> Points { get; internal set; }
	public List<Triangle> Triangles { get; internal set; }
}