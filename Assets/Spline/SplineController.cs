using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum eOrientationMode { NODE = 0, TANGENT }

[AddComponentMenu("Splines/Spline Controller")]
public class SplineController : MonoBehaviour
{
	public GameObject SplineRoot;
	public float Duration = 10;


	SplineInterpolator mSplineInterp;
	Transform[] mTransforms;

	void OnDrawGizmos()
	{
		Transform[] trans = GetTransforms();
		if (trans.Length < 2)
			return;
				
		SplineInterpolator interp = new SplineInterpolator(getNodes(trans));

		Vector3 prevPos = trans[0].position;
		for (int c = 1; c <= 100; c++)
		{
			float currTime = c * Duration / 100;
			Vector3 currPos = interp.GetHermiteAtTime(currTime);
			float mag = (currPos-prevPos).magnitude * 2;
			Gizmos.color = new Color(mag, 0, 0, 1);
			Gizmos.DrawLine(prevPos, currPos);
			prevPos = currPos;
		}
	}

	List<SplineInterpolator.SplineNode> getNodes(Transform[] trans)
	{
		float step = Duration / (trans.Length - 1);

		return trans.Select((t, i) => new SplineInterpolator.SplineNode(t.position, step * i, new Vector2(0, 1))).ToList();
	}


	/// <summary>
	/// Returns children transforms, sorted by name.
	/// </summary>
	Transform[] GetTransforms()
	{
		if (SplineRoot != null)
		{
			List<Component> components = new List<Component>(SplineRoot.GetComponentsInChildren(typeof(Transform)));
			List<Transform> transforms = components.ConvertAll(c => (Transform)c);

			transforms.Remove(SplineRoot.transform);
			transforms.Sort(delegate(Transform a, Transform b)
			{
				return a.name.CompareTo(b.name);
			});

			return transforms.ToArray();
		}

		return null;
	}
}