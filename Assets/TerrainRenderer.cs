using Delaunay;
using Delaunay.Geo;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

//public class TerrainRenderer : MonoBehaviour
//{


//	// Use this for initialization
//	private void Start()
//	{
		
//	}

//	// Update is called once per frame
//	private void Update()
//	{
//		if (ShouldDisplay)
//		{
//			foreach (var region in regions)
//			{
//				var sites = region.Triangles.SelectMany(t => t.sites);
//				var biome = biomeColorMap[region.Biome];
//				var color = biome.Color;

//				var m = new Mesh();

//				m.vertices = sites.Select(s => new Vector3(s.x, biome.Height, s.y)).ToArray();
//				m.triangles = sites.Select((s, i) => i).ToArray();
//				m.colors = sites.Select(p => color).ToArray();

//				Graphics.DrawMesh(m, Matrix4x4.identity, mat, 0);
//			} 
//		}
//	}

//	private float GetRain(OpenSimplexNoise n, int i, int j)
//	{
//		return (float)n.eval(i / rainScale + rainOffset, j / rainScale + rainOffset) / 2f + 0.5f;
//	}

//	private float GetTemp(OpenSimplexNoise n, int i, int j)
//	{
//		return (float)n.eval(i / tempScale + tempOffset, j / tempScale + tempOffset) / 2f + 0.5f;
//	}

//	private Biome GetBiome(float rain, float temp)
//	{
//		if (temp < 0.25f)
//		{
//			return Biome.Tundra;
//		}
//		else if (temp < 0.78125f)
//		{
//			if (rain < 0.125f)
//			{
//				return Biome.Grassland;
//			}

//			if (temp < 0.421875f)
//			{
//				return Biome.Taiga;
//			}

//			if (rain < 0.28125f)
//			{
//				return Biome.Woodlands;
//			}
//			else if (rain < 0.453125f)
//			{
//				return Biome.TemperateForest;
//			}
//			else
//			{
//				return Biome.TemperateRainForest;
//			}
//		}
//		else
//		{
//			if (rain < 0.1875f)
//			{
//				return Biome.Desert;
//			}
//			else if (rain < 0.5625f)
//			{
//				return Biome.TropicalForest;
//			}
//			else
//			{
//				return Biome.TropicalRainForest;
//			}
//		}
//	}
//}