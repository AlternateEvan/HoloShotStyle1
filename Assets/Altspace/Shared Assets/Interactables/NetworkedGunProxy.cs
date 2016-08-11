using UnityEngine;

public class NetworkedGunProxy : Proxy
{
	public GameObject[] BulletPrefabs;

	public Transform FirePoint;

	public float FireRate = 333;

	public float HapticDuration = 0.5f;

	public ushort HapticStrength = 1000;

	public Transform AmmoBarTransform;
}
