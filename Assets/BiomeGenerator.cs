using Delaunay;
using Delaunay.Geo;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class BiomeGenerator
{
	public const int poissonRetries = 20;
	public int size;
	public float biomeSeparation = 100f;
	public float biomeBlendDist = 50f;
	public float lakeMountainSeparation = 300f;
	public float lakeMountainBlendDist = 300f;
	public float settlementSeparation = 800f;

	public float baseScale = 1024;
	public float baseOffset = 53;
	public float detailScale = 512;
	public float detailOffset = -60;
	public float detailMultiplier = 1;
	public float tempScale = 512;
	public float tempOffset = -258;
	public float rainScale = 512;
	public float rainOffset = 654;

	private OpenSimplexNoise noise;
	private System.Random random;

	public List<SuperRegion> regions;
	public List<BiomeRegion> biomes; 
	public List<Settlement> settlements;
	public Dictionary<Vector2, SuperRegion> regionLookup;
	public Dictionary<Vector2, BiomeRegion> biomeLookup;
	public Dictionary<Vector2, Settlement> settlementLookup;

	private Dictionary<SuperBiome, SuperBiomeDescriptor> superBiomes;
	private Dictionary<Biome, BiomeDescriptor> biomeMap;

	public BiomeGenerator(int size)
	{
		this.size = size;
		noise = new OpenSimplexNoise();
		random = new System.Random();

		superBiomes = new Dictionary<SuperBiome, SuperBiomeDescriptor>
		{
			{ SuperBiome.Lake       , new SuperBiomeDescriptor { Biome = SuperBiome.Lake    , HeightFunc = (u, v) => 0.00f + (float)noise.eval(u,v) * 0.002f } },
			{ SuperBiome.Plains     , new SuperBiomeDescriptor { Biome = SuperBiome.Plains  , HeightFunc = (u, v) => 0.03f + (float)noise.eval(u,v) * 0.005f } },
			{ SuperBiome.Mountain   , new SuperBiomeDescriptor { Biome = SuperBiome.Mountain, HeightFunc = (u, v) => 0.10f + (float)noise.eval(u,v) * 0.020f } },
		};

		biomeMap = new Dictionary<Biome, BiomeDescriptor> {
			{ Biome.Lake                    , new BiomeDescriptor { Biome = Biome.Lake               , SplatIndex = 7, HeightFunc = (u, v) => 0f} },
			{ Biome.Beach                   , new BiomeDescriptor { Biome = Biome.Beach              , SplatIndex = 7, HeightFunc = (u, v) => 0f} }, //not used
			{ Biome.Mountain                , new BiomeDescriptor { Biome = Biome.Mountain           , SplatIndex = 0, HeightFunc = (u, v) => 0f} },

			{ Biome.Tundra                  , new BiomeDescriptor { Biome = Biome.Tundra             , SplatIndex = 5, HeightFunc = (u, v) => 0f} },
			{ Biome.Taiga                   , new BiomeDescriptor { Biome = Biome.Taiga              , SplatIndex = 6, HeightFunc = (u, v) => 0f} },
			{ Biome.Grassland               , new BiomeDescriptor { Biome = Biome.Grassland          , SplatIndex = 1, HeightFunc = (u, v) => 0f} },
			{ Biome.Desert                  , new BiomeDescriptor { Biome = Biome.Desert             , SplatIndex = 7, HeightFunc = (u, v) => 0f} },
			{ Biome.Woodlands               , new BiomeDescriptor { Biome = Biome.Woodlands          , SplatIndex = 1, HeightFunc = (u, v) => 0f} },
			{ Biome.TemperateForest         , new BiomeDescriptor { Biome = Biome.TemperateForest    , SplatIndex = 3, HeightFunc = (u, v) => 0f} },
			{ Biome.TropicalForest          , new BiomeDescriptor { Biome = Biome.TropicalForest     , SplatIndex = 4, HeightFunc = (u, v) => 0f} },
			{ Biome.TemperateRainForest     , new BiomeDescriptor { Biome = Biome.TemperateRainForest, SplatIndex = 2, HeightFunc = (u, v) => 0f} },
			{ Biome.TropicalRainForest      , new BiomeDescriptor { Biome = Biome.TropicalRainForest , SplatIndex = 4, HeightFunc = (u, v) => 0f} },
		};
	}

	public void Generate()
	{
		var bounds = new Rect(-size, -size, size * 2, size * 2);

		MakeMountainsAndLakes(bounds);

		MakeBiomes(bounds);

		MakeSettlements(bounds);

		MakeRoads();
	}

	private void MakeRoads()
	{
		foreach (var s in settlements)
		{
			foreach (var n in s.Neighbours)
			{
				var destination = settlementLookup[n];

				var match = destination.Roads.FirstOrDefault(x => x.B == s);
				if (match != null)
				{
					s.Roads.Add(match);
					continue;
				}

				var points = new List<Vector2> { s.Centre };

				var current = GetBiomeAt(s.Centre.x, s.Centre.y).Centre;
				var target = GetBiomeAt(n.x, n.y).Centre;
				var tries = 100;

				// walk via biomes
				bool endEarly = false;
				while (tries > 0)
				{
					var neighbours = biomeLookup[current].Neighbours;
					var haveRoad = neighbours.Where(x => biomeLookup[x].Roads.Any(r => r.B.Centre == current));
					if (haveRoad.Any())
					{
						points.Add(haveRoad.First());
						endEarly = true;
						break;
					}

					var nearest = neighbours.MinBy(x => Vector2.Distance(n, x));

					if (nearest == target) break;

					points.Add(nearest);
					current = nearest;
					tries--;
				}

				if (!endEarly)
				{
					points.Add(n);
				}

				var newRoad = new Road
				{
					Points = points,
					A = s,
					B = destination
				};
				s.Roads.Add(newRoad);

				for (int i = 1; i < points.Count - (endEarly ? 0 : 1); i++)
				{
					var point = points[i];
					biomeLookup[point].Roads.Add(newRoad);
				}
			} 
		}
	}

	private void MakeSettlements(Rect bounds)
	{
		var settlementPoissonEnum = Poisson.generate_poisson(random, size, settlementSeparation, poissonRetries);

		var settlementPoisson = new List<Vector2>();

		while (settlementPoissonEnum.MoveNext())
		{
			settlementPoisson.Add(settlementPoissonEnum.Current);
		}

		var settlementVoronoi = new Voronoi(settlementPoisson, null, bounds);

		settlements = settlementPoisson.Select(p => new Settlement
		{
			Centre = p,
			Biome = GetBiomeAt(p.x, p.y),
			Neighbours = settlementVoronoi.NeighborSitesForSite(p).Where(n => Vector2.Distance(p, n) < settlementSeparation * 2).ToList()
		}).ToList();
		settlementLookup = settlements.ToDictionary(x => x.Centre);
	}

	private void MakeBiomes(Rect bounds)
	{
		var poissonEnum = Poisson.generate_poisson(random, size, biomeSeparation, poissonRetries);

		var poisson = new List<Vector2>();

		while (poissonEnum.MoveNext())
		{
			poisson.Add(poissonEnum.Current);
		}

		var voronoi = new Voronoi(poisson, null, bounds);

		biomes = MakeBiomes(poisson, voronoi);
		biomeLookup = biomes.ToDictionary(x => x.Centre);
	}

	private void MakeMountainsAndLakes(Rect bounds)
	{
		var bigPoissonEnum = Poisson.generate_poisson(random, size, lakeMountainSeparation, poissonRetries);

		var bigPoisson = new List<Vector2>();

		while (bigPoissonEnum.MoveNext())
		{
			bigPoisson.Add(bigPoissonEnum.Current);
		}

		var bigVoronoi = new Voronoi(bigPoisson, null, bounds);

		regions = MakeTerrainRegions(bigPoisson, bigVoronoi);
		regionLookup = regions.ToDictionary(x => x.Centre);
	}

	private IEnumerable<Vector2> FilterSettlements(List<Vector2> settlementPoisson)
	{
		foreach (var poisson in settlementPoisson)
		{
			var landType = GetSuperBiomesAt(poisson.x, poisson.y).OrderBy(b => b.Proportion).LastOrDefault().BiomeDescriptor.Biome;

			if (landType == SuperBiome.Plains)
			{
				yield return poisson;
			}
		}
		yield break;
	}

	private List<SuperRegion> MakeTerrainRegions(List<Vector2> poisson, Voronoi voronoi)
	{
		var regions = new List<SuperRegion>();

		foreach (var regionCentre in poisson)
		{
			var x = (int)regionCentre.x;
			var y = (int)regionCentre.y;

			var height = GetNoise(x, y, baseScale, baseOffset);

			var biome =
				height > 0.75f ? SuperBiome.Mountain :
				height < 0.3f ? SuperBiome.Lake :
				SuperBiome.Plains;

			var points = voronoi.Region(regionCentre);

			var bounds = GetBounds(points);

			var v = new Voronoi(points, null, bounds);

			regions.Add(new SuperRegion
			{
				Centre = regionCentre,
				Points = points,
				Bounds = bounds,
				Triangles = v.Triangles(),
				Biome = biome,
				Neighbours = voronoi.NeighborSitesForSite(regionCentre)
			});
		}

		return regions;
	}

	private List<BiomeRegion> MakeBiomes(List<Vector2> poisson, Voronoi voronoi)
	{
		var regions = new List<BiomeRegion>();

		foreach (var regionCentre in poisson)
		{
			var x = (int)regionCentre.x;
			var y = (int)regionCentre.y;

			var superBiome = GetSuperBiomesAt(regionCentre.x, regionCentre.y).OrderBy(b => b.Proportion).LastOrDefault();

			Biome biome;

			if (superBiome != null && superBiome.BiomeDescriptor.Biome == SuperBiome.Lake)
			{
				biome = Biome.Lake;
			}
			else if (superBiome != null && superBiome.BiomeDescriptor.Biome == SuperBiome.Mountain)
			{
				biome = Biome.Mountain;
			}
			else
			{
				var temp = GetNoise(x, y, tempScale, tempOffset);
				var rain = GetNoise(x, y, rainScale, rainOffset);

				biome = GetBiome(rain, temp);
			}

			var points = voronoi.Region(regionCentre);

			var bounds = GetBounds(points);

			var v = new Voronoi(points, null, bounds);

			regions.Add(new BiomeRegion
			{
				Centre = regionCentre,
				Points = points,
				Bounds = bounds,
				Triangles = v.Triangles(),
				Biome = biome,
				Neighbours = voronoi.NeighborSitesForSite(regionCentre)
			});
		}

		return regions;
	}

	private static Rect GetBounds(List<Vector2> points)
	{
		float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

		foreach (var point in points)
		{
			if (point.x < minX) minX = point.x;
			if (point.y < minY) minY = point.y;
			if (point.x > maxX) maxX = point.x;
			if (point.y > maxY) maxY = point.y;
		}

		var bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
		return bounds;
	}

	private float GetNoise(float u, float v, float scale, float offset)
	{
		return (float)noise.eval(u / scale + offset, v / scale + offset) / 2f + 0.5f;
	}

	private Biome GetBiome(float rain, float temp)
	{
		if (temp < 0.125f)
		{
			return Biome.Tundra;
		}
		else if (temp < 0.671875f)
		{
			if (rain < 0.21875f)
			{
				return Biome.Grassland;
			}

			if (rain < 0.421875f)
			{
				return Biome.Woodlands;
			}

			if (temp < 0.296875f)
			{
				return Biome.Taiga;
			}
			else if (rain < 0.703125f)
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
			if (rain < 0.3125f)
			{
				return Biome.Desert;
			}
			else if (rain < 0.34375f)
			{
				return Biome.TropicalForest;
			}
			else
			{
				return Biome.TropicalRainForest;
			}
		}
	}

	public List<SuperBiomeProportion> GetSuperBiomesAt(float u, float v)
	{
		var point = new Vector2(u, v);
		var list = new List<SuperBiomeProportion>();

		foreach (var region in regions)
		{
			if (region.Bounds.Overlaps(new Rect(point.x - lakeMountainBlendDist, point.y - lakeMountainBlendDist, lakeMountainBlendDist * 2, lakeMountainBlendDist * 2)))
			{
				foreach (var triangle in region.Triangles)
				{
					var within = triangle.Contains(point);
					var dist = triangle.DistToClosestEdge(point);
					if (dist <= lakeMountainBlendDist)
					{
						if (within)
						{
							dist = -dist;
						}
						var rawr = dist / lakeMountainBlendDist / 2f + 0.5f;

						float proportion = Mathf.Cos(rawr * Mathf.PI) / 2f + 0.5f;
						list.Add(new SuperBiomeProportion(superBiomes[region.Biome], proportion));
					}
					else if (within)
					{
						// that's all folks
						return new List<SuperBiomeProportion>
						{
							new SuperBiomeProportion(superBiomes[region.Biome], 1)
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

	public List<BiomeProportion> GetBiomesAt(float u, float v)
	{
		var point = new Vector2(u, v);
		var list = new List<BiomeProportion>();

		foreach (var region in biomes)
		{
			if (region.Bounds.Overlaps(new Rect(point.x - biomeBlendDist, point.y - biomeBlendDist, biomeBlendDist * 2, biomeBlendDist * 2)))
			{
				foreach (var triangle in region.Triangles)
				{
					var within = triangle.Contains(point);
					var dist = triangle.DistToClosestEdge(point);
					if (dist <= biomeBlendDist)
					{
						if (within)
						{
							dist = -dist;
						}
						var rawr = dist / biomeBlendDist / 2f + 0.5f;

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

	public BiomeRegion GetBiomeAt(float u, float v)
	{
		var point = new Vector2(u, v);

		foreach (var region in biomes)
		{
			if (region.Bounds.Overlaps(new Rect(point.x - biomeBlendDist, point.y - biomeBlendDist, biomeBlendDist * 2, biomeBlendDist * 2)))
			{
				foreach (var triangle in region.Triangles)
				{
					var within = triangle.Contains(point); if (within)
					{
						return region;
					}
				}
			}
		}

		return null;
	}
}
