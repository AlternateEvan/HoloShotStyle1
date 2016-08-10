using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NewtonVR;

public class MovingPlatform : MonoBehaviour
{
	public Transform Platform;
	private Vector3 lastPlatformPosition;
	private CharacterController characterController;
	private Dictionary<NVRInteractableItem, Transform> interactables = new Dictionary<NVRInteractableItem, Transform>();

	void OnTriggerEnter(Collider collider)
	{
		var charController = collider.GetComponent<CharacterController>();
		var interactable = collider.GetComponentInParent<NVRInteractableItem>();
		if (charController != null)
		{
			characterController = charController;
		}
		else if (interactable != null && !interactable.Rigidbody.isKinematic)
		{
			interactables.Add(interactable, interactable.transform.parent);
			interactable.transform.parent = Platform;
		}
	}

	void OnTriggerExit(Collider collider)
	{
		var interactable = collider.GetComponentInParent<NVRInteractableItem>();

		if (collider.GetComponent<CharacterController>() != null)
		{
			characterController = null;
		}
		else if (interactable != null && interactables.ContainsKey(interactable))
		{
			interactable.transform.parent = interactables[interactable];
			interactables.Remove(interactable);
		}
	}

	void Start()
	{
		lastPlatformPosition = Platform.position;
	}

	void Update()
	{
		var delta = Platform.position - lastPlatformPosition;
		if (characterController != null)
		{
			characterController.Move(delta);
		}

		lastPlatformPosition = Platform.position;
	}
}
