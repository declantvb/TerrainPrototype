using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
	public int Subdivisions = 8;
	public int ChunkSize = 256;
	public int ChunkHeight = 2048;
	public int Seed = 100;
	public int WorldSize = 5;
	public Vector3 LastUpdatePosition;

	private List<LoadChunk> LoadedChunks;

	[SerializeField]
	private UnityEngine.Object TerrainPrefab;

	private int ScaledChunkSize { get { return ChunkSize; } }
	private int ScaledChunkHeight { get { return ChunkHeight; } }
	private int ScaledHeightmapSize { get { return (int)Mathf.Pow(2, Subdivisions) + 1; } }

	public bool ShouldDisplay = false;

	private OpenSimplexNoise NoiseMaker;
	private BiomeGenerator BiomeGenerator;

	public Texture2D[] groundTextures;
	public Texture2D[] normalTextures;

	private const int texturescalar = 2;
	public Material mat;
	private int alphamapLayers = 8;

	// Use this for initialization
	private void Start()
	{
		//var rand = new System.Random(Seed);
		NoiseMaker = new OpenSimplexNoise(Seed);

		BiomeGenerator = new BiomeGenerator(ChunkSize * WorldSize);
		var sw = new Stopwatch();
		sw.Start();
		BiomeGenerator.Generate();
		print("Made Biomes in " + sw.Elapsed.TotalSeconds + "s");

		LoadedChunks = new List<LoadChunk>();

		LoadChunks(new Vector3(0, 0, 0));

		//show settlements
		foreach (var s in BiomeGenerator.settlements)
		{
			var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			var height = GetTerrainHeight(s.Centre);
			sphere.transform.position = new Vector3(s.Centre.x, height, s.Centre.y);
			sphere.transform.localScale = new Vector3(50, 50, 50);
			sphere.GetComponent<MeshRenderer>().material.color = Color.red;
		}
	}

	void OnDrawGizmos()
	{
		if (BiomeGenerator != null)
		{
			foreach (var s in BiomeGenerator.settlements)
			{
				foreach (var road in s.Roads)
				{
					var nodes = road.Points.Select((x, i) => new SplineInterpolator.SplineNode(new Vector3(x.x, 0, x.y), (float)i / road.Points.Count, new Vector2(0, 1))).ToList();

					SplineInterpolator interp = new SplineInterpolator(nodes);

					Vector3 prevPos = interp.GetHermiteAtTime(0);
					for (int c = 1; c <= 100; c++)
					{
						float currTime = c * 1f / 100;
						Vector3 currPos = interp.GetHermiteAtTime(currTime);

						var height = GetTerrainHeight(new Vector2(currPos.x, currPos.z));

						currPos.y = height + 1;
						float mag = (currPos - prevPos).magnitude * 2;

						var cross = Vector3.Cross(currPos - prevPos, Vector3.up).normalized * 10;
						Gizmos.DrawLine(prevPos - cross, currPos - cross);
						Gizmos.DrawLine(prevPos + cross, currPos + cross);

						prevPos = currPos;
					}
				}
			}
		}
	}

	private float GetTerrainHeight(Vector2 centre)
	{
		foreach (var chunk in LoadedChunks)
		{
			if (chunk.Bounds.Contains(centre))
			{
				return chunk.Chunk.GetComponent<Terrain>().terrainData.GetHeight(ToChunkCoord(centre.x - ChunkSize * chunk.X), ToChunkCoord(centre.y - ChunkSize * chunk.Z));
			}
		}
		return 500; // err?
	}

	private int ToChunkCoord(float pos)
	{
		return (int)Mathf.Round(pos / ChunkSize * ScaledHeightmapSize);
	}

	public void Update()
	{
		if (ShouldDisplay)
		{
			//BiomeGenerator.Display(mat);
		}
	}

	private void FixedUpdate()
	{
		var playerPos = new Vector3(0, 0, 0);
		var posXZ = playerPos;
		posXZ.y = 0;
		var playerChunk = new Vector3(Mathf.RoundToInt(posXZ.x / ChunkSize) - 1, 0, Mathf.RoundToInt(posXZ.z / ChunkSize) - 1);

		// if we have moved enough since last update
		if ((LastUpdatePosition - posXZ).magnitude > ChunkSize)
		{
			LoadedChunks.ForEach(c => c.Loaded = false);
			LoadChunks(playerChunk);

			var toUnload = LoadedChunks.Where(c => !c.Loaded);
			foreach (var chunk in toUnload)
			{
				chunk.Chunk.SetActive(false);
			}

			LastUpdatePosition = posXZ;
		}

		//var chunkActual = LoadedChunks.SingleOrDefault(c => c.X == playerChunk.x && c.Z == playerChunk.z);
		//var terrain = chunkActual.Chunk.GetComponent<Terrain>();
		//var height = terrain.SampleHeight(playerPos);

		//if (playerPos.y < height - 0.1f)
		//{
		//	var newpos = new Vector3(playerPos.x, height, playerPos.z);
		//}
	}

	protected GameObject MakeChunk(int X, int Z)
	{
		var chunkPos = new Vector3(X * ScaledChunkSize, 0, Z * ScaledChunkSize);
		var terrainClone = Instantiate(TerrainPrefab, chunkPos, Quaternion.identity) as GameObject;
		terrainClone.transform.parent = gameObject.transform;
		var con = terrainClone.GetComponent<ChunkController>();
		con.X = X;
		con.Z = Z;
		terrainClone.name = "chunk(" + X + "," + Z + ")";

		var terrainData = new TerrainData();
		terrainData.heightmapResolution = ScaledHeightmapSize;
		terrainData.alphamapResolution = ScaledHeightmapSize;
		terrainData.size = new Vector3(ScaledChunkSize, ScaledChunkHeight, ScaledChunkSize);

		terrainData.baseMapResolution = 33;

		// Splatmap data is stored internally as a 3d array of floats, so declare a new empty array ready for your custom splatmap data:
		float[,,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

		terrainData.SetHeights(0, 0, GenerateHeightmap(Z, X, ScaledHeightmapSize, out splatmapData));

		SplatPrototype[] newProto = new SplatPrototype[alphamapLayers];
		newProto[0] = MakeSplat(groundTextures[0], normalTextures[0]); //dry earth
		newProto[1] = MakeSplat(groundTextures[1], normalTextures[1]); //dry grass
		newProto[2] = MakeSplat(groundTextures[2], normalTextures[2]); //earth
		newProto[3] = MakeSplat(groundTextures[3], normalTextures[3]); //grass
		newProto[4] = MakeSplat(groundTextures[4], normalTextures[4]); //moss
		newProto[5] = MakeSplat(groundTextures[5], normalTextures[5]); //snow
		newProto[6] = MakeSplat(groundTextures[6], normalTextures[6]); //riverbed
		newProto[7] = MakeSplat(groundTextures[7], normalTextures[7]); //desert

		// Set prototype array
		terrainData.splatPrototypes = newProto;
		terrainData.SetAlphamaps(0, 0, splatmapData);

		terrainData.RefreshPrototypes();

		var terrainComponent = terrainClone.GetComponent<Terrain>();

		terrainComponent.heightmapMaximumLOD = 1;
		terrainComponent.terrainData = terrainData;

		var tc = terrainComponent.gameObject.GetComponent<TerrainCollider>();
		tc.terrainData = terrainData;

		return terrainClone;
	}

	private SplatPrototype MakeSplat(Texture2D groundTexture, Texture2D normalTexture)
	{
		return new SplatPrototype
		{
			metallic = 0,
			smoothness = 0,
			specular = Color.white,
			tileOffset = Vector2.zero,
			tileSize = new Vector2(ChunkSize / texturescalar, ChunkSize / texturescalar),
			texture = groundTexture,
			normalMap = normalTexture,
		};
	}

	private void LoadChunks(Vector3 playerChunk)
	{
		for (int i = (int)playerChunk.x - WorldSize; i < (int)playerChunk.x + WorldSize; i++)
		{
			for (int j = (int)playerChunk.z - WorldSize; j < (int)playerChunk.z + WorldSize; j++)
			{
				var ch = LoadedChunks.SingleOrDefault(c => c.X == i && c.Z == j);
				if (ch != null)
				{
					ch.Chunk.SetActive(true);
					ch.Loaded = true;
				}
				else
				{
					var sw = new Stopwatch();
					sw.Start();
					var newChunk = MakeChunk(i, j);
					print("Made Chunk(" + i + "," + j + ") in " + sw.Elapsed.TotalSeconds + "s");

					LoadedChunks.Add(new LoadChunk
					{
						X = i,
						Z = j,
						Chunk = newChunk,
						Loaded = true,
						Bounds = new Rect(i * ChunkSize, j * ChunkSize, ChunkSize, ChunkSize)
					});

					var controller = newChunk.GetComponent<ChunkController>();
					controller.UpdateNeighbours();
				}

				//yield return null;
			}
		}
	}

	private float MakeTerrainNoise(float u, float v)
	{
		return (float)NoiseMaker.eval(u, v) / 2 + 0.5f;
		//return 0;
	}

	private float[,] GenerateHeightmap(int worldu, int worldv, int size, out float[,,] splatmapData)
	{
		var ret = new float[size, size];

		splatmapData = new float[size, size, alphamapLayers];

		for (int chunky = 0; chunky < size; chunky++)
		{
			for (int chunkx = 0; chunkx < size; chunkx++)
			{
				var chunku = chunky / (size - 1f);
				var chunkv = chunkx / (size - 1f);
				var u = (worldu + chunku);
				var v = (worldv + chunkv);

				var baseBiomeDescriptors = BiomeGenerator.GetSuperBiomesAt(u * ChunkSize, v * ChunkSize);

				var biomeDescriptors = BiomeGenerator.GetBiomesAt(u * ChunkSize, v * ChunkSize);

				// Setup an array to record the mix of texture weights at this point
				float[] splatWeights = new float[alphamapLayers];

				var height = 0f;

				foreach (var desc in baseBiomeDescriptors)
				{
					height += desc.BiomeDescriptor.HeightFunc(u, v) * desc.Proportion;
				}

				foreach (var desc in biomeDescriptors)
				{
					height += desc.BiomeDescriptor.HeightFunc(u, v) * desc.Proportion;
					splatWeights[desc.BiomeDescriptor.SplatIndex] += desc.Proportion;
				}

				ret[chunky, chunkx] = height;

				// Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
				float z = splatWeights.Sum();

				// Loop through each terrain texture
				for (int i = 0; i < alphamapLayers; i++)
				{
					// Normalize so that sum of all texture weights = 1
					splatWeights[i] /= z;

					// Assign this point to the splatmap array
					splatmapData[chunky, chunkx, i] = splatWeights[i];
				}
			}
		}
		return ret;
	}

	public float GetHeightAt(Vector3 position)
	{
		var chunk = new Vector3(Mathf.RoundToInt(position.x / ChunkSize) - 1, 0, Mathf.RoundToInt(position.z / ChunkSize) - 1);
		var chunkActual = LoadedChunks.SingleOrDefault(c => c.X == chunk.x && c.Z == chunk.z);
		var terrain = chunkActual.Chunk.GetComponent<Terrain>();
		return terrain.SampleHeight(position);
	}
}

internal class LoadChunk
{
	public int X;
	public int Z;
	public GameObject Chunk;
	public bool Loaded;
	public Rect Bounds;
}