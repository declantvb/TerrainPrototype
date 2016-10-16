using System.Collections.Generic;
using Delaunay.Utils;
using UnityEngine;

namespace Delaunay
{

	public sealed class Triangle : IDisposable
	{
		private List<Site> _sites;
		public List<Site> sites
		{
			get { return this._sites; }
		}

		public Triangle(Site a, Site b, Site c)
		{
			_sites = new List<Site>() { a, b, c };
		}

		public void Dispose()
		{
			_sites.Clear();
			_sites = null;
		}

		public bool Contains(Vector2 p)
		{
			var p0 = _sites[0];
			var p1 = _sites[1];
			var p2 = _sites[2];

			// from http://stackoverflow.com/a/20861130

			var s = p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * p.x + (p0.x - p2.x) * p.y;
			var t = p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * p.x + (p1.x - p0.x) * p.y;

			if ((s < 0) != (t < 0))
				return false;

			var A = -p1.y * p2.x + p0.y * (p2.x - p1.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y;
			if (A < 0.0)
			{
				s = -s;
				t = -t;
				A = -A;
			}
			return s > 0 && t > 0 && (s + t) <= A;
		}

		public float DistToClosestEdge(Vector2 point)
		{
			var p0 = _sites[0].Coord;
			var p1 = _sites[1].Coord;
			var p2 = _sites[2].Coord;

			var s1 = DistToLineSegment(point, p0, p1);
			var s2 = DistToLineSegment(point, p1, p2);
			var s3 = DistToLineSegment(point, p2, p0);

			return Mathf.Min(s1, Mathf.Min(s2, s3));
		}

		private float sign(Vector2 p1, Vector2 p2, Vector2 p3)
		{
			return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}

		//calculates the projection of a point p to line w-v
		private static Vector2 min_projection(Vector2 v, Vector2 w, Vector2 p)
		{
			// Return minimum distance between line segment vw and point p
			float l2 = Vector2.SqrMagnitude(w - v);  // i.e. |w-v|^2 -  avoid a sqrt
													 //if (l2 == 0.0)
													 //	return Vector2.Distance(p, v);   // v == w case
													 // Consider the line extending the segment, parameterized as v + t (w - v).
													 // We find projection of point p onto the line.
													 // It falls where t = [(p-v) . (w-v)] / |w-v|^2
													 // We clamp t from [0,1] to handle points outside the segment vw.
			float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - v, w - v) / l2));
			Vector2 projection = v + t * (w - v);  // Projection falls on the segment
			return projection;
		}

		private float DistToLineSegment(Vector2 point, Vector2 lineA, Vector2 lineB)
		{
			var proj = min_projection(lineA, lineB, point);
			return Vector2.Distance(proj, point);
		}
	}
}