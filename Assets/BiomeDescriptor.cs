using System;
using UnityEngine;

public class BiomeDescriptor
{
	public Biome Biome { get; set; }
	public Func<float,float,float> HeightFunc { get; set; }
	public int SplatIndex { get; set; }
}