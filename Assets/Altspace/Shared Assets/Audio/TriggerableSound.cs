
//this script is used for sound colliders. Script checks every frame to see if characterController has entered collider (via teleport) and triggers a collision if necessary.

using UnityEngine;

public class TriggerableSound : MonoBehaviour
{
	private AudioSource myAudioSource;
	private GameObject playerObject;
	private BoxCollider boxCollider;

	void Awake()
	{
		myAudioSource = gameObject.GetComponent<AudioSource>();
		boxCollider = gameObject.GetComponent<BoxCollider>();
	}

	void Update()
	{
#if ALTSPACE_UNITYCLIENT
		if (playerObject == null)
		{
			if (Main.PlayerManager != null && Main.PlayerManager.Me.Value != null)
			{
				playerObject = Main.PlayerManager.Me.Value.gameObject;
			}
		}
#else
		if (playerObject == null)
		{
			playerObject = GameObject.Find("First Person Controller");
		}
#endif
		if (playerObject != null)
		{
			var shouldPlay = boxCollider.bounds.Contains(playerObject.transform.position);
			var shouldChangeAudioState = shouldPlay != myAudioSource.isPlaying;

			if (shouldChangeAudioState)
			{
				if (myAudioSource.isPlaying)
				{
					myAudioSource.Stop();
				}
				else
				{
					myAudioSource.Play();
				}
			}
		}
	}
}