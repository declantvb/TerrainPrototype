using System.Collections.Generic;
using UnityEngine;

public class Settlement
{
	public BiomeRegion Biome { get; set; }
	public Vector2 Centre { get; set; }
	public List<Vector2> Neighbours { get; set; }
	public List<Road> Roads { get; internal set; }

	public Settlement()
	{
		Roads = new List<Road>();
	}
}