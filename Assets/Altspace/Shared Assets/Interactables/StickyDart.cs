using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class StickyDart : MonoBehaviour, IProjectile
{
	[SerializeField]
	private Transform StickPoint;
	[SerializeField]
	private float MaxStickAngle = 40;
	[SerializeField]
	private Vector3 BulletForce = new Vector3(0, 0, 500);

	private new Rigidbody rigidbody;

	private Queue<Vector3> velocities = new Queue<Vector3>();

	private bool firstCollision = false;

	private GameObject anchor;

	public event EventHandler<NetworkedProjectileRPCEventArgs> FireRPC;

	void FixedUpdate()
	{
		if (velocities.Count >= 4)
		{
			velocities.Dequeue();
		}

		if(rigidbody.velocity.sqrMagnitude > Mathf.Epsilon)
			velocities.Enqueue(rigidbody.velocity);
	}

	void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
	}

	void Start()
	{
		rigidbody.AddRelativeForce(BulletForce);
	}

	void OnCollisionEnter(Collision collision)
	{
		if (!firstCollision)
		{
			firstCollision = true;
			StartCoroutine(Cleanup());
		}

		ContactPoint contact = collision.contacts[0];

		var velocity = rigidbody.velocity;

		if (velocities.Count > 0)
		{
			velocity = velocities.Peek();
		}

		var arr = velocities.ToArray();

		var dot = Vector3.Dot(velocity.normalized, -contact.normal);

		var layer = contact.otherCollider.transform.gameObject.layer;

		if (dot > 0.5f || layer == (int)Layer.Avatars)
		{
			#region sticky logic

			var dartTarget = contact.otherCollider.GetComponent<IDartTarget>();

			if (layer == (int) Layer.Avatars)
			{
				transform.SetParent(contact.otherCollider.transform, false);
			}
			else if (layer == (int) Layer.Interactable || dartTarget != null)
			{
				anchor = new GameObject();
				anchor.name = "anchor";
				anchor.transform.SetParent(contact.otherCollider.transform);
				transform.SetParent(anchor.transform);
				Destroy(rigidbody);
			}

			transform.forward = -contact.normal;
			transform.position = contact.point;

			var backoffPoint = contact.point + (-velocity.normalized*1f);

			RaycastHit raycastHit;
			var ray = new Ray(backoffPoint, velocity.normalized);
			var success = contact.otherCollider.Raycast(ray, out raycastHit, 2f);

			if (success)
			{
				transform.position = raycastHit.point;
			}

			StickPoint.localPosition = -StickPoint.localPosition;
			transform.position = StickPoint.position;// easiest way to do this
			StickPoint.localPosition = -StickPoint.localPosition;

			if (rigidbody != null)
			{
				rigidbody.isKinematic = true;
				rigidbody.velocity = Vector3.zero;
				rigidbody.angularVelocity = Vector3.zero;
			}


			if (dartTarget != null)
			{
				dartTarget.StickToTarget(StickPoint.position, contact.normal);
			}

			Destroy(this);
			#endregion
		}
	}

	private IEnumerator Cleanup()
	{
		yield return new WaitForSeconds(5);
		Destroy(this);
	}

	public void HandleRPC(string type)
	{

	}

	public void Init(bool isMine)
	{
		if (!isMine)
		{
			Destroy(this);
		}
	}
}
