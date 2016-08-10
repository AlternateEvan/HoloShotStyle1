using UnityEngine;

public class DartTarget : MonoBehaviour, IDartTarget
{
	private new ParticleSystem particleSystem;

	void Start()
	{
		particleSystem = GetComponentInChildren<ParticleSystem>();
	}

	public void StickToTarget(Vector3 position, Vector3 forward)
	{
		particleSystem.time = 0.0f;
		particleSystem.Clear();
		particleSystem.Play();

		particleSystem.transform.position = position;
		particleSystem.transform.rotation = Quaternion.LookRotation(forward);
	}
}
