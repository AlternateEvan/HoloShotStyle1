using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HoloBladeColliderBehavior : MonoBehaviour
{
	[SerializeField] private GameObject sparkPrefab;
	[SerializeField] private GameObject stationarySparkPrefab;
	[SerializeField] private GameObject smokePrefab;

	[SerializeField] private GameObject burnDecalPrefab;

	// we cycle through our particle systems using indices; go from beginning to end
	// that way an old one has a chance to expire while the new one (corresponding to an advanced index)
	// plays
	private int currentEffectIndex = 0;
	private HoloBladeImpactEffect [] impactEffects;
	private const string outerBeamName = "HoloBlade_BladeOuter";
	private const string hiltName = "HoloBlade_Hilt";

	// each game object that we collide with has a set of particles that are created
	private Dictionary<Collider, HoloBladeImpactEffect> colliderToImpactEffect;
	// pooled burn prefabs
	private int currentDecalIndex = 0;
	private DecalEffect[] burnDecals;
	private DecalEffect previousDecal = null;

	public bool EnableCollisions { set; get; }

	void Awake()
	{
		int numTotalEffects = (Application.platform == RuntimePlatform.Android ? 1 : 2);
		impactEffects = new HoloBladeImpactEffect[numTotalEffects];
		Transform particleEffectsParent = transform.Find("ParticleEffects").transform;
		for (int i = 0; i < numTotalEffects; i++)
		{
			impactEffects[i] = new HoloBladeImpactEffect(sparkPrefab, stationarySparkPrefab,
				smokePrefab, particleEffectsParent);
		}
		colliderToImpactEffect = new Dictionary<Collider, HoloBladeImpactEffect>();

		// burn decals
		int numDecals = (Application.platform == RuntimePlatform.Android ? 15 : 30);
		// make a parent for decals to keep things tidy
		GameObject parent = GameObject.Find("DecalParent");
		if (parent == null)
		{
			parent = new GameObject("DecalParent");
		}
		parent.transform.position = Vector3.zero;
		parent.transform.rotation = Quaternion.identity;
		burnDecals = new DecalEffect[numDecals];
		for (int i = 0; i < numDecals; i++)
		{
			Transform decal = GameObject.Instantiate(burnDecalPrefab).transform;
			decal.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			burnDecals[i] = decal.GetComponent<DecalEffect>();
			decal.parent = parent.transform;
			decal.localRotation = Quaternion.identity;
			decal.localPosition = Vector3.zero;
		}
#if ALTSPACE_UNITYCLIENT
		EnableCollisions = !Main.IsAndroid;
#else
		EnableCollisions = true;
#endif
		StartCoroutine(CleanUpDeadColliders());
	}

	void OnCollisionEnter(Collision collision)
	{
		if (!EnableCollisions) return;
		PlayNewEffectsForCollision(collision);
		CreateBurnMarkForCollision(collision);
	}

	void OnCollisionStay(Collision collision)
	{
		if (!EnableCollisions) return;
		MoveEffectsToPointForCollision(collision);
		CreateBurnMarkForCollision(collision);
	}

	void OnCollisionExit(Collision collision)
	{
		StopPlayingEffectsForCollision(collision.collider);
		previousDecal = null;
	}

	// sometimes a collider might be disabled while colliding with us,
	// which means that OnColliderExit won't be called for it and we won't be able 
	// to clean up that collider's effects. clean up after those colliders
	// it's quite stupid that disabling a collider during a collision doesn't call
	// OnCollisionExit!!
	IEnumerator CleanUpDeadColliders()
	{
		List<Collider> deadColliders = new List<Collider>();
		while (true)
		{
			yield return new WaitForSeconds(0.25f);
			foreach (var collider in colliderToImpactEffect.Keys)
			{
				if (!collider.enabled || !collider.gameObject.activeInHierarchy)
				{
					deadColliders.Add(collider);
				}
			}
			foreach (var collider in deadColliders)
			{
				StopPlayingEffectsForCollision(collider);
			}
			deadColliders.Clear();
		}
	}

	private void PlayNewEffectsForCollision(Collision collision)
	{
		// we can't play any more effects if we have run of out them
		// also, don't deal with duplicates
		if (colliderToImpactEffect.Keys.Count == impactEffects.Length ||
			colliderToImpactEffect.ContainsKey(collision.collider))
		{
			return;
		}
		// make sure that we hit appropriate collider -- not hilt
		// find first collision only
		bool struckValidPoint = false;
		Vector3 contactPoint = Vector3.zero;
		foreach (var contact in collision.contacts)
		{
			if (contact.thisCollider.name == outerBeamName)
			{
				struckValidPoint = true;
				contactPoint = contact.point;
				break;
			}
		}
		if (struckValidPoint)
		{
			colliderToImpactEffect.Add(collision.collider, impactEffects[currentEffectIndex++]);
			if (currentEffectIndex == impactEffects.Length)
				currentEffectIndex = 0;
			// start playing effects
			colliderToImpactEffect[collision.collider].StartPlayingAtPoint(contactPoint);
		}
	}

	private void MoveEffectsToPointForCollision(Collision collision)
	{
		// only care about game objects that created collision originally
		if (!colliderToImpactEffect.ContainsKey(collision.collider))
			return;
		// move to first collision point only
		foreach (var contact in collision.contacts)
		{
			if (contact.thisCollider.name == outerBeamName)
			{
				colliderToImpactEffect[collision.collider].MoveToPoint(contact.point);
				break;
			}
		}
	}

	private void StopPlayingEffectsForCollision(Collider collider)
	{
		if (!colliderToImpactEffect.ContainsKey(collider))
			return;
		colliderToImpactEffect[collider].StopPlaying();
		colliderToImpactEffect.Remove(collider);
	}

	private void CreateBurnMarkForCollision(Collision collision)
	{
		// don't bother creating a burn mark when hitting another saber!
		// also make sure OUR collider is outer beam only, and not hilt or something else
		foreach (var contact in collision.contacts)
		{
			if (contact.otherCollider.name != outerBeamName &&
				contact.otherCollider.name != hiltName &&
				contact.thisCollider.name == outerBeamName)
			{
				FillInMissingDecals(contact.point, contact.normal);
				CreateDecal(contact.point, contact.normal);
				break;
			}
		}
	}

	// if we have previous decal point, fill in blank spaces
	// assuming two points are on same plane
	private void FillInMissingDecals(Vector3 currentContactPoint, Vector3 currentContactNormal)
	{
		// new and old decals must be parallel (use dot product)
		// in case decals cut corners
		if (previousDecal != null && (1.0f - Vector3.Dot(previousDecal.FacingDirection, currentContactNormal)) < Mathf.Epsilon)
		{
			Vector3 previousDecalPosition = previousDecal.MyTransform.position;
			float prevDecalCreationTime = previousDecal.CreationTime;

			Vector3 prevPointToCurrent = currentContactPoint - previousDecalPosition;
			// march along vector and create decals
			float marchLength = prevPointToCurrent.magnitude;
			prevPointToCurrent.Normalize();
			float decalSpacing = 0.025f;
			int numDecals = (int)(marchLength/decalSpacing); // assume that each decal has a finite length
			// don't include 0 -- that's the previous decal point!
			for (int decalIndex = 1; decalIndex < numDecals; decalIndex++)
			{
				Vector3 decalPoint = previousDecalPosition + prevPointToCurrent*decalSpacing*decalIndex;
				float interpVal = (float) decalIndex/numDecals;
				// tricky business: the decals that are created in-between have a modified duration because
				// they should have been created after previous decal before current one. find their interpolated start time!
				// basically, find out how much time has passed since that previous decal start time to decrement duration
				// of the filled-in one.
				float interpCreationTime = prevDecalCreationTime + interpVal*(Time.time - prevDecalCreationTime);
				float durationReduction = (Time.time - interpCreationTime);
				CreateFillerDecal(decalPoint, currentContactNormal, durationReduction, interpCreationTime);
			}
		}
	}

	private void CreateFillerDecal(Vector3 contactPoint, Vector3 contactNormal, float durationReduction, float creationTime)
	{
		float modifiedDuration = burnDecals[currentDecalIndex].DecalDuration - durationReduction;
		burnDecals[currentDecalIndex].EnableTransientDecal(contactPoint, contactNormal, modifiedDuration);
		burnDecals[currentDecalIndex].CreationTime = creationTime;
		IncrementDecalIndex();
	}

	private void CreateDecal(Vector3 contactPoint, Vector3 contactNormal)
	{
		burnDecals[currentDecalIndex].EnableTransientDecal(contactPoint, contactNormal);
		previousDecal = burnDecals[currentDecalIndex];
		IncrementDecalIndex();
	}

	private void IncrementDecalIndex()
	{
		currentDecalIndex++;
		if (currentDecalIndex == burnDecals.Length)
		{
			currentDecalIndex = 0;
		}
	}
}
