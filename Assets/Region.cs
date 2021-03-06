﻿using System.Collections.Generic;
using Delaunay;
using UnityEngine;

public class BiomeRegion
{
	public Biome Biome { get; internal set; }
	public Rect Bounds { get; internal set; }
	public Vector2 Centre { get; internal set; }
	public List<Vector2> Neighbours { get; internal set; }
	public List<Vector2> Points { get; internal set; }
	public List<Triangle> Triangles { get; internal set; }
	public List<Road> Roads { get; internal set; }

	public BiomeRegion()
	{
		Roads = new List<Road>();
	}
}