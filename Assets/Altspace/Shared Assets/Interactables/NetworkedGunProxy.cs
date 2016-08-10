using UnityEngine;

public class NetworkedGunProxy : Proxy
{
	public GameObject BulletPrefab;

	public Transform FirePoint;

	public int MaxBulletsPerSecond = 3;

	public float HapticDuration = 0.5f;

	public ushort HapticStrength = 1000;

	public Transform AmmoBarTransform;
}
