using UnityEngine;
using System.Collections;

public class RocksEnter : MonoBehaviour
{
	[SerializeField]
	private AudioClip rocksClip;

	private AudioSource myAudioSource;
	private bool alreadyPlayed = false;

	// Use this for initialization
	void Start()
	{
		myAudioSource = GetComponent<AudioSource>();
	}

	// Update is called once per frame
	void OnTriggerEnter(Collider other)
	{
		if (alreadyPlayed)
		{
			return;
		}
		myAudioSource.clip = rocksClip;
		myAudioSource.timeSamples = 0;
		myAudioSource.loop = false;
		myAudioSource.Play();
		alreadyPlayed = true;
	}
}