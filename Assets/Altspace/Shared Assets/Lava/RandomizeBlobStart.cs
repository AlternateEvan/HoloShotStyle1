using UnityEngine;
using System.Collections;

public class RandomizeBlobStart : MonoBehaviour {

	// Use this for initialization
	void Start () {
		StartCoroutine(WaitItOut());
	}

	IEnumerator WaitItOut() {
		yield return new WaitForSeconds(Random.Range(0, 20));
		GetComponent<Animator>().enabled = true;
	}
}
