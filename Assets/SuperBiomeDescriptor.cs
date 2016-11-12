using System;
using UnityEngine;

public class SuperBiomeDescriptor
{
	public SuperBiome Biome { get; set; }
	public Func<float,float,float> HeightFunc { get; set; }
}