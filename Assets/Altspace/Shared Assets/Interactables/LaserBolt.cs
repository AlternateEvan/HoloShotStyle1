using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class LaserBolt : MonoBehaviour, IProjectile
{
	[SerializeField]
	private float MaxReflectDistance = 10.0f;
	[SerializeField]
	private float MaxFirstShotDistance = 100.0f;
	[SerializeField]
	private float Speed = 30.0f;
	private float reducedSpeed = 15.0f;
	[SerializeField]
	private int MaxParticles = 3;
	[SerializeField]
	private GameObject ReflectionParticles;
	[SerializeField]
	private AudioClip[] ShootSounds = new AudioClip[3];
	[SerializeField]
	private AudioClip[] RichochetSounds = new AudioClip[3];
	[SerializeField]
	private float InteractableReflectionDelta = 0.3f;
	[SerializeField]
	private int MaxReflections = 3;
	private int currentInteractableReflections = 0;

	private Queue<RaycastHitPointData> raycastHitPoints;

	private Vector3 cachedForward;

	private Transform boltTransform;

	private RaycastHitPointData currentOrigin;
	private RaycastHitPointData currentTarget;

	private bool hasCurrentPath = false;
	private float startTime = 0.0f;
	private bool finishedCurrentPath = false;
	private bool cleanupStarted = false;

	private static Dictionary<GameObject, List<ParticleSystem>> ReflectionParticlesPool;
	private int currentParticle = 0;

	private bool isMine = false;

	private bool firstReflect = true;

	private PlayerController owner;

	private MeshRenderer trailEffectMeshRenderer;

	public event EventHandler<NetworkedProjectileRPCEventArgs> FireRPC;

	private struct RaycastHitPointData
	{
		public Vector3 Position;
		public Vector3 Normal;

		public RaycastHitPointData(Vector3 position, Vector3 normal)
		{
			Position = position;
			Normal = normal;
		}
	}

	public void Init(bool isMine, PlayerController owner)
	{
		this.isMine = isMine;
		this.owner = owner;
	}

	void Awake()
	{
		boltTransform = GetComponentInChildren<Transform>();

		reducedSpeed = Speed / 2;
	}

	void Start()
	{
		raycastHitPoints = new Queue<RaycastHitPointData>();

		raycastHitPoints.Enqueue(new RaycastHitPointData(transform.position, Vector3.zero));

		firstReflect = true;
		getReflectionPath(transform.position, transform.forward, ref raycastHitPoints, MaxReflections);

		if (!isMine)
		{
			var collider = GetComponentInChildren<SphereCollider>();
			collider.enabled = false;

			var rigidbody = GetComponent<Rigidbody>();
			Destroy(rigidbody);
		}

		//#if !UNITY_ANDROID
		if (ReflectionParticlesPool == null)
		{
			ReflectionParticlesPool = new Dictionary<GameObject, List<ParticleSystem>>();
		}

		if (!ReflectionParticlesPool.ContainsKey(ReflectionParticles))
		{
			ReflectionParticlesPool[ReflectionParticles] = new List<ParticleSystem>();

			for (var i = 0; i < MaxParticles; i++)
			{
				var newParticle = Instantiate(ReflectionParticles);
				newParticle.SetActive(true);
				ReflectionParticlesPool[ReflectionParticles].Add(newParticle.GetComponent<ParticleSystem>());
			}
		}
		//#endif

		playRandomClip(ShootSounds);
	}

	void FixedUpdate()
	{
		//This code animates the projectile (laser) across the prior calculated vector paths.
		if (isMine && raycastHitPoints != null)
		{
			if (raycastHitPoints.Count >= 2 && !hasCurrentPath)
			{
				//there are at least two points in the queue and we haven't started animating yet
				currentOrigin = raycastHitPoints.Dequeue();
				currentTarget = raycastHitPoints.Dequeue();
				startTime = Time.fixedTime;

				transform.LookAt(currentTarget.Position);

				hasCurrentPath = true;
			}
			else if (raycastHitPoints.Count >= 1 && hasCurrentPath && finishedCurrentPath)
			{
				//there are still points in the queue and we've finished animating on the current path
				currentOrigin = currentTarget;
				currentTarget = raycastHitPoints.Dequeue();
				startTime = Time.fixedTime;

				transform.LookAt(currentTarget.Position);

				finishedCurrentPath = false;
			}

			if (hasCurrentPath && !finishedCurrentPath)
			{
				//on a path and animating along it
				var distance = Vector3.Distance(currentOrigin.Position, currentTarget.Position);
				var time = distance / Speed; //time it should take for bolt to travel between the two points
				var now = Time.fixedTime;

				var t = Mathf.Min((now - startTime) / time, 1.0f);

				transform.position = Vector3.Lerp(currentOrigin.Position, currentTarget.Position, t);

				if (t >= 0.999f)
				{
					finishedCurrentPath = true;
					playRichochetSound();
					emitParticles(currentTarget);
				}
			}
			else if (!cleanupStarted)
			{
				//Delay destroy because Photon doesn't like destroying an object so soon after its created.
				cleanupStarted = true;
				StartCoroutine(Cleanup());
			}
		}
	}

	private void playRichochetSound()
	{
		playRandomClip(RichochetSounds);
		emitRPC("PlayRichochetSound");
	}

	private IEnumerator Cleanup()
	{
		yield return new WaitForSeconds(1);
		Destroy(gameObject);
	}

	private void emitParticles(RaycastHitPointData data, bool sendRPC = true)
	{
		//#if !UNITY_ANDROID
		var particleSystem = ReflectionParticlesPool[ReflectionParticles][currentParticle];
		particleSystem.transform.position = data.Position;
		particleSystem.transform.LookAt(data.Position + data.Normal);
		particleSystem.time = 0.0f;
		particleSystem.Clear();
		particleSystem.Play();

		currentParticle = (currentParticle + 1) % MaxParticles;

		if (sendRPC)
		{
			emitRPC("EmitParticles");
		}
		//#endif
	}

	private void getReflectionPath(Vector3 origin, Vector3 direction, ref Queue<RaycastHitPointData> raycastHitPoints, int reflectionCount)
	{
		var dist = firstReflect ? MaxFirstShotDistance : MaxReflectDistance;
		firstReflect = false;

		if (reflectionCount > 0)
		{
			reflectionCount--;
			RaycastHit raycastHit;
			var ignoreLayer = ~(1 << (int)Layer.IgnoreRaycastLayer |
								1 << (int)Layer.Interactable |
								1 << (int)Layer.TriggerableSounds |
								1 << (int)Layer.HolographicWindows |
								1 << (int)Layer.Displays |
								1 << (int)Layer.UILayer);
			var gotHit = Physics.Raycast(origin, direction, out raycastHit, dist, ignoreLayer);
			if (gotHit)
			{
				raycastHitPoints.Enqueue(new RaycastHitPointData(raycastHit.point, raycastHit.normal));

				var reflectionDir = Vector3.Reflect(direction, raycastHit.normal);

				if (Vector3.Distance(origin, raycastHit.point) <= MaxReflectDistance)
				{
					getReflectionPath(raycastHit.point, reflectionDir, ref raycastHitPoints, reflectionCount);
				}
			}
			else
			{
				raycastHitPoints.Enqueue(new RaycastHitPointData(origin + direction * dist, direction));
			}
		}
	}

	void OnTriggerEnter(Collider other)
	{
		var reflection = -transform.forward;
		var data = new RaycastHitPointData(transform.position, reflection);

		if (isMine)
		{
			if (other.gameObject.layer == (int)Layer.Interactable)
			{
				Speed = reducedSpeed; //reduce speed of laser for dramatic effect
				raycastHitPoints.Clear();
				hasCurrentPath = false;
				raycastHitPoints.Enqueue(data);
				emitParticles(data);
				playRichochetSound();

				if (currentInteractableReflections < MaxReflections)
				{
					currentInteractableReflections++;
					reflection.x = reflection.x *
								   UnityEngine.Random.Range(1 - InteractableReflectionDelta, 1 + InteractableReflectionDelta);
					reflection.y = reflection.y *
								   UnityEngine.Random.Range(1 - InteractableReflectionDelta, 1 + InteractableReflectionDelta);
					getReflectionPath(transform.position, reflection, ref raycastHitPoints, 1);
				}
			}
			else if (other.gameObject.layer == (int)Layer.Avatars)
			{
				raycastHitPoints.Clear();
				emitParticles(data);
			}
		}
	}

	private int playRandomClip(AudioClip[] clips)
	{
		var selection = UnityEngine.Random.Range(0, clips.Length - 1);
		AudioSource.PlayClipAtPoint(clips[selection], transform.position, 0.5f);
		return selection;
	}

	private void emitRPC(string type)
	{
		var ev = FireRPC;
		if (ev != null)
		{
			var args = new NetworkedProjectileRPCEventArgs(type);
			ev(this, args);
		}
	}

	public void HandleRPC(string type)
	{
		switch (type)
		{
			case "PlayRichochetSound":
				playRandomClip(RichochetSounds);
				break;
			case "EmitParticles":
				emitParticles(new RaycastHitPointData(transform.position, -transform.forward), false);
				break;
		}
	}
}
