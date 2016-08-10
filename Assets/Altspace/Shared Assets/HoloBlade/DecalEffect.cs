using UnityEngine;
using System.Collections;

public class DecalEffect : MonoBehaviour
{
	// inspector properties
	[SerializeField] private Color tintColor = Color.white;
	// make this one public so that it's accessible from other scripts
	public float DecalDuration = 3.0f;

	public Vector3 FacingDirection {
		get { return transform.forward;  }
	}

	public float CreationTime { get; set; }
	public Transform MyTransform { get; set; }

	// only modify material of decal on desktop dynamically
	private Material decalMaterial;
	private string tintColorName = "_Color";

	void Awake()
	{
		// on android, refer to sharedmaterial. otherwise we need a material instance to make each decal fade
		decalMaterial = (Application.platform  == RuntimePlatform.Android) ?
			GetComponent<MeshRenderer>().sharedMaterial : GetComponent<MeshRenderer>().material;
		MyTransform = this.transform;
		gameObject.SetActive(false);
	}

	// in case the duration needs to be modified, use optional argument here
	public void EnableTransientDecal(Vector3 position, Vector3 normal, float modifiedDuration = -1.0f)
	{
		StopAllCoroutines(); // just in case, stop previous coroutines on this behaviour
		gameObject.SetActive(true);
		decalMaterial.SetColor(tintColorName, tintColor);
		MyTransform.position = position;
		MyTransform.forward = normal;
		CreationTime = Time.time;
		float realLifetime = modifiedDuration > 0.0f ? modifiedDuration : DecalDuration;
		if (Application.platform == RuntimePlatform.Android)
			StartCoroutine(DisableAfterDurationSimple(realLifetime));
		else
			StartCoroutine(DisableAfterDuration(realLifetime));
	}

	IEnumerator DisableAfterDurationSimple(float duration)
	{
		yield return new WaitForSeconds(duration);
		gameObject.SetActive(false);
	}

	// this one does a fade. meant for desktop
	// (since the materials are unique, fade independently)
	IEnumerator DisableAfterDuration(float duration)
	{
		float startTime = Time.time;
		float endTime = Time.time + duration;
		while (Time.time < endTime)
		{
			float t = (Time.time - startTime)/duration;
			decalMaterial.SetColor(tintColorName, new Color(tintColor.r, tintColor.g, tintColor.b, tintColor.a-tintColor.a*t));
			yield return null;
		}
		gameObject.SetActive(false);
	}
}
