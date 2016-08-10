using UnityEngine;

public class InteractableSpawnerProxy : DeterministicReference
{
	/// <summary>
	/// Set this value if you're not adding your own NVRInteractable to the spawner proxy gameobject
	/// </summary>
	public Transform InteractionPoint;
}