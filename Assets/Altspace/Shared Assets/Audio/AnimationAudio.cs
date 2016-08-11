using UnityEngine;
using System.Collections;

public class AnimationAudio : MonoBehaviour
{
	public AudioClip openSound;
	public AudioClip closeSound;

	private AudioSource audioSource;
	
	void Start()
	{
		audioSource = GetComponentInChildren<AudioSource>();
	}

	public void PlayDoorOpen()
	{
		audioSource.clip = openSound;
		audioSource.timeSamples = 0;
		audioSource.Play();
	}

	public void PlayDoorClose()
	{
		audioSource.clip = closeSound;
		audioSource.timeSamples = 0;
		audioSource.Play();
	}
}