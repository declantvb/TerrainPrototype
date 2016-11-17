using System.Collections.Generic;
using UnityEngine;

public class Road
{
	public Settlement A { get; internal set; }
	public Settlement B { get; internal set; }
	public List<Vector2> Points { get; internal set; }
}