using UnityEngine;
using System.Collections.Generic;

// based on a script from here
// https://drive.google.com/file/d/0B6kRCXNkPp32SFJkbEtFMUhGaTg/view?usp=sharing
// from this video
// https://www.youtube.com/watch?v=PXyj36Dw97I&feature=youtu.be

// a trail consists of sections (quads), with two divisions (lines) surrounding a section
public class HoloBladeTrail : MonoBehaviour
{
	[SerializeField]
	private float TrailHeight = 2.0f;
	[SerializeField]
	private float DivisionDuration = 2.0f;
	[SerializeField]
	private float DivisionMinDistance = 0.1f;
	[SerializeField]
	private int NumMaxDivisions = 20;
	[SerializeField]
	private Color StartColor = Color.white;
	[SerializeField]
	private Color EndColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
	[SerializeField]
	private float BottomAlpha = 1.0f;
	[SerializeField]
	private float TopAlpha = 0.0f;
	[SerializeField]
	private bool CameraAligned = false;
	[SerializeField]
	private bool OverrideEnabledForGearVR = false;

	// if disable, no new trail sections will be created but old ones will be cleaned up!
	public bool EnableTrail { get; set; }

	private class TrailDivision
	{
		public TrailDivision()
		{
			// nothing to see here, move along
		}

		public Vector3 Position;
		public Vector3 UpDirection;
		public float ExpirationTime;
	}

	// pre-allocated mesh information per division. some of the information is pre-computed too!
	private class MeshInfo
	{
		public MeshInfo(int numDivisions, Color startColor, Color endColor, float bottomAlpha, float topAlpha)
		{
			NumDivisions = numDivisions;
			int numVertices = NumDivisions * 2;
			Vertices = new Vector3[numVertices];
			Colors = new Color[numVertices];
			UVs = new Vector2[numVertices];
			// a trail's section (think of it as a quad) is contained within two divisions
			// this means we have one less section compared to number of divisions
			// we have two triangles per section, or six triangle vertices per section
			TriangleIndices = new int[(NumDivisions - 1) * 6];

			// compute UVs and colors given number of divisions. we can't precompute
			// the vertices unfortunately, as they need to be transformed in realtime
			for (int divisionIndex = 0; divisionIndex < NumDivisions; divisionIndex++)
			{
				float uCoord = 1.0f - (float)divisionIndex / (NumDivisions - 1);
				int vertIndex0 = divisionIndex * 2, vertIndex1 = divisionIndex * 2 + 1;
				UVs[vertIndex0] = new Vector2(uCoord, 0);
				UVs[vertIndex1] = new Vector2(uCoord, 1);

				// interpolate colors based on position along trail, as dicted by uCoord
				// (which goes from 0.0-1.0 anyway)
				var interpolatedColor = Color.Lerp(startColor, endColor, uCoord);
				Colors[vertIndex0] = new Color(interpolatedColor.r, interpolatedColor.g, interpolatedColor.b, interpolatedColor.a * bottomAlpha);
				// top section is always transparent. that way we have a nice fade
				Colors[vertIndex1] = new Color(interpolatedColor.r, interpolatedColor.g, interpolatedColor.b, interpolatedColor.a * topAlpha);
			}

			int numSections = TriangleIndices.Length / 6;
			// two triangles per section (a section is between two divisions)
			for (int i = 0; i < numSections; i++)
			{
				int baseTriangleVertIndex = i * 6;
				int baseVertIndex = i * 2;
				TriangleIndices[baseTriangleVertIndex] = baseVertIndex;
				TriangleIndices[baseTriangleVertIndex + 1] = baseVertIndex + 1;
				TriangleIndices[baseTriangleVertIndex + 2] = baseVertIndex + 2;

				TriangleIndices[baseTriangleVertIndex + 3] = baseVertIndex + 2;
				TriangleIndices[baseTriangleVertIndex + 4] = baseVertIndex + 1;
				TriangleIndices[baseTriangleVertIndex + 5] = baseVertIndex + 3;
			}
		}

		// accessors for convenience
		public int NumDivisions { get; private set; }
		public Vector3[] Vertices { get; private set; }
		public Color[] Colors { get; private set; }
		public Vector2[] UVs { get; private set; }
		public int[] TriangleIndices { get; private set; }
	}

	private List<TrailDivision> trailDivisions;
	// pool of unused TrailDivisions. just to prevent allocations during updates
	private Stack<TrailDivision> unusedDivisions;

	private MeshFilter meshFilter;
	private Vector3 prevPosition;
	// instead of allocating verts each time, just create them ahead of time so that they are ready to go
	// given a division count
	private Dictionary<int, MeshInfo> divisionCountToMeshInfo;

	void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();

		trailDivisions = new List<TrailDivision>(NumMaxDivisions);
		// pool of unused divisions
		unusedDivisions = new Stack<TrailDivision>(NumMaxDivisions);
		for (int divisionIndex = 0; divisionIndex < NumMaxDivisions; divisionIndex++)
		{
			unusedDivisions.Push(new TrailDivision());
		}

		divisionCountToMeshInfo = new Dictionary<int, MeshInfo>();
		// create meshes for all possible division counts, starting from 2
		// (two divisions is equal to one section; can't create a section with fewer divs)
		for (int divisionIndex = 2; divisionIndex <= NumMaxDivisions; divisionIndex++)
		{
			MeshInfo newMesh = new MeshInfo(divisionIndex, StartColor, EndColor, BottomAlpha, TopAlpha);
			divisionCountToMeshInfo.Add(divisionIndex, newMesh);
		}

		// in case awake runs after effects toggle was set, consult it
#if ALTSPACE_UNITYCLIENT
		EnableTrail = !Main.IsAndroid || OverrideEnabledForGearVR;
#else
		EnableTrail = true;
#endif
	}

	// make sure trail lags behind blade
	void LateUpdate()
	{
		Vector3 currentPosition = transform.position;

		// remove old divisions -- starting from item that is too old
		// note that indices of oldest division is at index 0
		int indexOfDeadDivision = -1;   // make invalid to start with
		for (int i = 0; i < trailDivisions.Count; i++)
		{
			// break once we find something that is new enough
			if (trailDivisions[i].ExpirationTime > Time.time)
			{
				break;
			}
			else
			{
				indexOfDeadDivision = i;
			}
		}

		bool removedDivisions = false;
		if (indexOfDeadDivision >= 0)
		{
			RemoveTrailDivisions(0, indexOfDeadDivision + 1);
			removedDivisions = true;
		}

		// add a new trail if:
		// -trail is enabled AND
		// -the last section is too far behind leading position, or if
		//  we have moved a bit since last update
		if (EnableTrail &&
			((trailDivisions.Count > 0 &&
			 (trailDivisions[trailDivisions.Count - 1].Position - currentPosition).sqrMagnitude
			 > DivisionMinDistance * DivisionMinDistance)
			||
			(currentPosition - prevPosition).sqrMagnitude > Mathf.Epsilon)
			)
		{
			// if we have run out of divisions, just remove the oldest first and put back into pool
			if (trailDivisions.Count == NumMaxDivisions)
			{
				RemoveTrailDivisions(0, 1);
			}

			var rot = Vector3.up;

#if ALTSPACE_UNITYCLIENT
			if (CameraAligned)
			{
				var dot = Mathf.Abs(Vector3.Dot(Main.HMDManager.MainCamera.transform.forward, transform.forward));
				rot = Vector3.Lerp(Vector3.up, Vector3.left, dot);
			}
#endif

			TrailDivision newDivision = CreateNewTrailDivision(currentPosition,
				transform.TransformDirection(rot), Time.time + DivisionDuration);
			trailDivisions.Add(newDivision);
		}

		// always need to update mesh if we have at least one section (they require
		// transformations during every update!)
		if (removedDivisions || trailDivisions.Count >= 2)
		{
			UpdateMesh();
		}
		prevPosition = currentPosition;
	}

	private void RemoveTrailDivisions(int startIndex, int count)
	{
		int endIndex = startIndex + count;
		for (int i = startIndex; i < endIndex; i++)
		{
			unusedDivisions.Push(trailDivisions[i]);
		}
		trailDivisions.RemoveRange(startIndex, count);
	}

	private TrailDivision CreateNewTrailDivision(Vector3 position, Vector3 upDirection,
		float expiryTime)
	{
		TrailDivision newDivision = unusedDivisions.Pop();
		newDivision.Position = position;
		newDivision.UpDirection = upDirection;
		newDivision.ExpirationTime = expiryTime;
		return newDivision;
	}

	// rebuild mesh in local space. The transform component will
	// automatically transform the object into world space.
	private void UpdateMesh()
	{
		Mesh bladeMesh = meshFilter.mesh;
		bladeMesh.Clear();
		// need at least two sections
		if (trailDivisions.Count >= 2)
		{
			var currentDivision = trailDivisions[0];
			var worldToLocal = transform.worldToLocalMatrix;
			int numDivisions = trailDivisions.Count;
			MeshInfo meshInfo = divisionCountToMeshInfo[numDivisions];

			// we can reuse the precomputed colors, uvs and triangle indices, but
			// we have to recompute the positions of the mesh in object (local) space!
			Vector3[] vertices = meshInfo.Vertices;
			for (int divisionIndex = 0; divisionIndex < numDivisions; divisionIndex++)
			{
				currentDivision = trailDivisions[divisionIndex];
				int vertIndex0 = divisionIndex * 2;
				// vertices
				vertices[vertIndex0] = worldToLocal.MultiplyPoint(currentDivision.Position);
				vertices[vertIndex0 + 1] = worldToLocal.MultiplyPoint(currentDivision.Position +
					currentDivision.UpDirection * TrailHeight);
			}

			bladeMesh.vertices = vertices;
			bladeMesh.colors = meshInfo.Colors;
			bladeMesh.uv = meshInfo.UVs;
			bladeMesh.triangles = meshInfo.TriangleIndices;
		}
	}
}