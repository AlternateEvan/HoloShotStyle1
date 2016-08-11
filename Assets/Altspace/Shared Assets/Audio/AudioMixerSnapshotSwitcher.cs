using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerSnapshotSwitcher : MonoBehaviour 
{
	public AudioMixer Mixer;
	public AudioMixerSnapshot SnapshotInside;
	public AudioMixerSnapshot SnapshotOutside;
	public float TransitionTime = 0.5f;

	private GameObject playerObject = null;
	private string[] possiblePlayerControllerNames = { "First Person Controller", "OVRPlayerController", "FPSController",
		"FirstPersonController", "AltVRPlayerController" };

	void OnTriggerEnter( Collider other ) 
	{
		HandleTrigger( other, SnapshotInside );
	}
	
	void OnTriggerExit( Collider other ) 
	{
		HandleTrigger( other, SnapshotOutside );
	}
	
	private void HandleTrigger( Collider other, AudioMixerSnapshot snapshot ) 
	{
		EnsurePlayerGameObject();
		if (playerObject == other.gameObject && snapshot != null)
		{
			snapshot.TransitionTo (TransitionTime);
		}
	}

	// this script can be used in the client or art projects for testing,
	// so it has to search for a player game object that might exist in both.
	// it can't assume that it will be used in the client all of the time, which
	// is why it doesn't use the client's classes to search for the player
	private void EnsurePlayerGameObject() 
	{
		if (playerObject == null)
		{
			foreach (var gameObjectName in possiblePlayerControllerNames)
			{
				playerObject = GameObject.Find(gameObjectName);
				if (playerObject != null) break;
			}
		}
	}
}

