using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class DeterministicReference : MonoBehaviour
{
	public const string NONE = "NONE";
	public string Id;

	public static Dictionary<string, GameObject> References = new Dictionary<string, GameObject>();

	void Awake()
	{
		#if UNITY_EDITOR
		if (!Application.isPlaying && Id == null)
		{
			Id = Guid.NewGuid().ToString();
			EditorUtility.SetDirty(this);
		}
		#endif

		if (Id == null)
		{
			Debug.LogWarning("Missing DeterministicReference ID, generating a local ID");
			Id = UnityEngine.Random.value.ToString();
		}

		if (Id != NONE)
		{
			References.Add(Id, gameObject);
		}
	}

	void OnDestroy()
	{
		References.Remove(Id);
	}
}
