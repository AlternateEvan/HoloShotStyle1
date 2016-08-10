using UnityEngine;
using System.Collections;

public class RandomizeSoundLoop : MonoBehaviour {

	public float MinLoopDelay = 0.0f;
	public float MaxLoopDelay = 2.0f;

	// Use this for initialization
	void Start() {
		GetComponent<AudioSource>().Pause();
		GetComponent<AudioSource>().loop = false;

		StartCoroutine (PlaySound ());
	}

	IEnumerator PlaySound () {
		AudioSource myAudioSource = GetComponent<AudioSource>();
		while (true) {
			float waitTime = Random.Range (MinLoopDelay, MaxLoopDelay);
			yield return new WaitForSeconds (waitTime);
			myAudioSource.Play ();
			yield return new WaitForSeconds (myAudioSource.clip.length);
		}
	}
}
