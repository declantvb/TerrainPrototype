using UnityEngine;
using System.Collections;

public class ChunkController : MonoBehaviour
{
	public int X = 0;
	public int Z = 0;
	public Terrain Left = null;
	public Terrain Top = null;
	public Terrain Right = null;
	public Terrain Bottom = null;
	public bool Updating = false;
	
	public void UpdateNeighbours()
	{
		Updating = true;
		
		if (Left == null)
		{
			var o = GameObject.Find("chunk(" + (X - 1) + "," + Z + ")");
			if (o != null)
			{
				Left = o.GetComponent<Terrain>();
				var con = Left.GetComponentInParent<ChunkController>();
				if (!con.Updating)
				{
					con.UpdateNeighbours();
				}
			}
		}
		if (Top == null)
		{
			var o = GameObject.Find("chunk(" + X + "," + (Z + 1) + ")");
			if (o != null)
			{
				Top = o.GetComponent<Terrain>();
				var con = Top.GetComponentInParent<ChunkController>();
				if (!con.Updating)
				{
					con.UpdateNeighbours();
				}
			}
		}
		if (Right == null)
		{
			var o = GameObject.Find("chunk(" + (X + 1) + "," + Z + ")");
			if (o != null)
			{
				Right = o.GetComponent<Terrain>();
				var con = Right.GetComponentInParent<ChunkController>();
				if (!con.Updating)
				{
					con.UpdateNeighbours();
				}
			}
		}
		if (Bottom == null)
		{
			var o = GameObject.Find("chunk(" + X + "," + (Z - 1) + ")");
			if (o != null)
			{
				Bottom = o.GetComponent<Terrain>();
				var con = Bottom.GetComponentInParent<ChunkController>();
				if (!con.Updating)
				{
					con.UpdateNeighbours();
				}
			}
		}
		
		var terrainComponent = gameObject.GetComponent<Terrain>();
		terrainComponent.SetNeighbors(Left, Top, Right, Bottom);
		
		Updating = false;
	}
}
