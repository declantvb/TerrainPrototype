using Delaunay;
using Delaunay.Geo;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class BiomeGenerator
{
	public float scaleFactor = 2;

	public int textureSize = 256;
	public float tempScale = 64;
	public float tempOffset = -258;
	public float rainScale = 64;
	public float rainOffset = 654;
	public float blendDist = 10;

	private List<Vector2> poisson;
	private Voronoi voronoi;
	private List<LineSegment> diagram;
	private Dictionary<Biome, BiomeDescriptor> biomeMap = new Dictionary<Biome, BiomeDescriptor> {
		{ Biome.Tundra                  , new BiomeDescriptor { Color = new Color(0.094f, 0.396f, 0.603f), Height = 0.000f * 2} },
		{ Biome.Taiga                   , new BiomeDescriptor { Color = new Color(0.000f, 0.635f, 0.355f), Height = 0.001f * 2} },
		{ Biome.Grassland               , new BiomeDescriptor { Color = new Color(0.484f, 0.365f, 0.290f), Height = 0.002f * 2} },
		{ Biome.Desert                  , new BiomeDescriptor { Color = new Color(0.484f, 0.190f, 0.000f), Height = 0.003f * 2} },
		{ Biome.Woodlands               , new BiomeDescriptor { Color = new Color(0.645f, 0.111f, 0.000f), Height = 0.004f * 2} },
		{ Biome.TemperateForest         , new BiomeDescriptor { Color = new Color(0.871f, 0.556f, 0.000f), Height = 0.005f * 2} },
		{ Biome.TropicalForest          , new BiomeDescriptor { Color = new Color(0.548f, 0.635f, 0.032f), Height = 0.006f * 2} },
		{ Biome.TemperateRainForest     , new BiomeDescriptor { Color = new Color(0.032f, 0.619f, 0.000f), Height = 0.007f * 2} },
		{ Biome.TropicalRainForest      , new BiomeDescriptor { Color = new Color(0.172f, 0.411f, 0.082f), Height = 0.008f * 2} },
	};

	public void Display(Material mat)
	{
		foreach (var region in regions)
		{
			var sites = region.Triangles.SelectMany(t => t.sites);
			var biome = biomeMap[region.Biome];
			var color = biome.Color;

			var m = new Mesh();

			m.vertices = sites.Select(s => new Vector3(s.x, biome.Height, s.y)).ToArray();
			m.triangles = sites.Select((s, i) => i).ToArray();
			m.colors = sites.Select(p => color).ToArray();

			Graphics.DrawMesh(m, Matrix4x4.identity, mat, 0);
		}
	}

	private System.Random random = new System.Random();
	private int xOffset;
	private int yOffset;
	private List<Region> regions;

	public void Generate()
	{
		var poissonEnum = Poisson.generate_poisson(new System.Random(), textureSize, 20, 20);
		var n = new OpenSimplexNoise();

		poisson = new List<Vector2>();

		while (poissonEnum.MoveNext())
		{
			poisson.Add(poissonEnum.Current);
		}

		voronoi = new Voronoi(poisson, null, new Rect(-textureSize, -textureSize, textureSize * 2, textureSize * 2));
		diagram = voronoi.VoronoiDiagram();

		xOffset = random.Next(-500000, 500000);
		yOffset = random.Next(-500000, 500000);

		regions = new List<Region>();
		foreach (var regionCentre in poisson)
		{
			var x = (int)regionCentre.x;
			var y = (int)regionCentre.y;

			var temp = GetTemp(n, x + xOffset, y + yOffset);
			var rain = GetRain(n, x + xOffset, y + yOffset);

			var biome = GetBiome(rain, temp);

			var points = voronoi.Region(regionCentre);

			float minX = float.MaxValue,
				minY = float.MaxValue,
				maxX = float.MinValue,
				maxY = float.MinValue;

			foreach (var point in points)
			{
				if (point.x < minX) minX = point.x;
				if (point.y < minY) minY = point.y;
				if (point.x > maxX) maxX = point.x;
				if (point.y > maxY) maxY = point.y;
			}

			var bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
			var v = new Voronoi(points, null, bounds);
			var d = v.DelaunayTriangulation();

			var triangles = v.Triangles();

			regions.Add(new Region
			{
				Centre = regionCentre,
				Points = points,
				Biome = biome,
				Triangles = triangles,
				Bounds = bounds,
			});
		}
	}

	private float GetRain(OpenSimplexNoise n, int i, int j)
	{
		return (float)n.eval(i / rainScale + rainOffset, j / rainScale + rainOffset) / 2f + 0.5f;
	}

	private float GetTemp(OpenSimplexNoise n, int i, int j)
	{
		return (float)n.eval(i / tempScale + tempOffset, j / tempScale + tempOffset) / 2f + 0.5f;
	}

	private Biome GetBiome(float rain, float temp)
	{
		if (temp < 0.25f)
		{
			return Biome.Tundra;
		}
		else if (temp < 0.78125f)
		{
			if (rain < 0.125f)
			{
				return Biome.Grassland;
			}

			if (temp < 0.421875f)
			{
				return Biome.Taiga;
			}

			if (rain < 0.28125f)
			{
				return Biome.Woodlands;
			}
			else if (rain < 0.453125f)
			{
				return Biome.TemperateForest;
			}
			else
			{
				return Biome.TemperateRainForest;
			}
		}
		else
		{
			if (rain < 0.1875f)
			{
				return Biome.Desert;
			}
			else if (rain < 0.5625f)
			{
				return Biome.TropicalForest;
			}
			else
			{
				return Biome.TropicalRainForest;
			}
		}
	}

	public List<BiomeProportion> GetBiomesAt(float u, float v)
	{
		var point = new Vector2(u, v);
		var list = new List<BiomeProportion>();

		foreach (var region in regions)
		{
			if (region.Bounds.Overlaps(new Rect(point.x - blendDist, point.y - blendDist, blendDist * 2, blendDist * 2)))
			{
				foreach (var triangle in region.Triangles)
				{
					var within = triangle.Contains(point);
					var dist = triangle.DistToClosestEdge(point);
					if (dist <= blendDist)
					{
						if (within)
						{
							dist = -dist;
						}
						var rawr = dist / blendDist / 2f + 0.5f;

						float proportion = Mathf.Cos(rawr * Mathf.PI) / 2f + 0.5f;
						list.Add(new BiomeProportion(biomeMap[region.Biome], proportion));
					}
					else if (within)
					{
						// that's all folks
						return new List<BiomeProportion>
						{
							new BiomeProportion(biomeMap[region.Biome], 1)
						};
					}
				}
			}
		}

		//normalise
		var sum = list.Sum(x => x.Proportion);
		foreach (var item in list)
		{
			item.Proportion = item.Proportion / sum;
		}

		return list;
	}

	public BiomeDescriptor GetBiomeAt(float u, float v)
	{
		var point = new Vector2(u, v);
		foreach (var region in regions)
		{
			if (region.Bounds.Contains(point))
			{
				foreach (var triangle in region.Triangles)
				{
					if (triangle.Contains(point))
					{
						return biomeMap[region.Biome];
					}
				}
			}
		}

		return null;
	}
}
