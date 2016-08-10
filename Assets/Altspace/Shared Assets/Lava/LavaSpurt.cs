using UnityEngine;

public class LavaSpurt : MonoBehaviour {

	private ParticleSystem myParticles;

	// Use this for initialization
	void Awake () 
	{
		myParticles = GetComponent<ParticleSystem>();
	}

	void SpewLava()
	{
		myParticles.enableEmission = true;
	}

	void StopLava()
	{
		myParticles.enableEmission = false;
	}
}
