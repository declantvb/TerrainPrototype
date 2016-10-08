using Delaunay;
using Delaunay.Geo;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TerrainRenderer : MonoBehaviour
{
	public int textureSize = 256;
	public float tempScale = 64f;
	public float tempOffset = -258;
	public Gradient tempGradient;
	public float rainScale = 64f;
	public float rainOffset = 654;
	public Gradient rainGradient;

	public Texture2D biomeMap;

	private MeshRenderer renderer;

	private List<Vector2> poisson;
	private List<uint> colors;
	private Voronoi voronoi;
	private List<LineSegment> diagram;
	private Dictionary<Biome, Color> biomeColorMap = new Dictionary<Biome, Color> {
		{ Biome.Tundra                  , new Color(0.094f, 0.396f, 0.603f)},
		{ Biome.Taiga                   , new Color(0.000f, 0.635f, 0.355f)},
		{ Biome.Grassland               , new Color(0.484f, 0.365f, 0.290f)},
		{ Biome.Desert                  , new Color(0.484f, 0.190f, 0.000f)},
		{ Biome.Woodlands               , new Color(0.645f, 0.111f, 0.000f)},
		{ Biome.TemperateForest         , new Color(0.871f, 0.556f, 0.000f)},
		{ Biome.TropicalForest          , new Color(0.548f, 0.635f, 0.032f)},
		{ Biome.TemperateRainForest     , new Color(0.032f, 0.619f, 0.000f)},
		{ Biome.TropicalRainForest      , new Color(0.172f, 0.411f, 0.082f)},
	};
	private Dictionary<Vector2, Biome> biomeDict;

	// Use this for initialization
	private void Start()
	{
		biomeMap = Resources.Load<Texture2D>("BiomeMap");

		renderer = GetComponent<MeshRenderer>();
		var poissonEnum = Poisson.generate_poisson(new System.Random(), textureSize / 2, 20, 20);

		poisson = new List<Vector2>();
		colors = new List<uint>();
		biomeDict = new Dictionary<Vector2, Biome>();

		while (poissonEnum.MoveNext())
		{
			poisson.Add(poissonEnum.Current);
			colors.Add(0);
		}

		voronoi = new Voronoi(poisson, colors, new Rect(-textureSize / 2, -textureSize / 2, textureSize, textureSize));
		diagram = voronoi.VoronoiDiagram();
	}

	// Update is called once per frame
	private void Update()
	{

		var n = new OpenSimplexNoise();
		var tex = new Texture2D(textureSize, textureSize);
		var tempMap = new float[textureSize, textureSize];
		var rainMap = new float[textureSize, textureSize];


		foreach (var item in poisson)
		{
			var x = (int)item.x;
			var y = (int)item.y;

			var temp = GetTemp(n, x, y);
			var rain = GetRain(n, x, y);
			rain = Mathf.Min(rain, temp); // rain is never larger than temp

			var biome = GetBiome(rain, temp);

			//biomeDict.Add(item, biome);

			tex.SetPixel(x + textureSize / 2, y + textureSize / 2, biomeColorMap[biome]);
		}

		for (int i = 0; i < textureSize; i++)
		{
			for (int j = 0; j < textureSize; j++)
			{
				var temp = GetTemp(n, i, j);
				var tempColor = tempGradient.Evaluate(temp / 2 + 0.5f);

				tempMap[i, j] = temp;

				var rain = GetRain(n, i, j);
				rain = Mathf.Min(rain, temp); // rain is never larger than temp
				var rainColor = rainGradient.Evaluate(rain / 2 + 0.5f);

				rainMap[i, j] = rain;

				//tex.SetPixel(i, j, tempColor);
			}
		}

		foreach (var point in voronoi.Region(poisson[0]))
		{
			var start = new Vector3(-point.x, -point.y, 0);
			var end = start - Vector3.forward*5;
			Debug.DrawLine(start, end, Color.red);
		}

		foreach (var line in diagram)
		{
			if (line.p0.HasValue && line.p1.HasValue)
			{
				// why inverse?
				var start = new Vector3(-line.p0.Value.x, -line.p0.Value.y, 0);
				var end = new Vector3(-line.p1.Value.x, -line.p1.Value.y, 0);
				Debug.DrawLine(start, end);
			}
		}

		//var triangulation = v.DelaunayTriangulation();

		tex.Apply();

		renderer.material.SetTexture("_MainTex", tex);
	}

	private float GetRain(OpenSimplexNoise n, int i, int j)
	{
		return (float)n.eval(i / rainScale + rainOffset, j / rainScale + rainOffset);
	}

	private float GetTemp(OpenSimplexNoise n, int i, int j)
	{
		return (float)n.eval(i / tempScale + tempOffset, j / tempScale + tempOffset);
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
}