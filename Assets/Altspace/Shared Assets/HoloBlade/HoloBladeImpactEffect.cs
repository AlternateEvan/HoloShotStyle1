using UnityEngine;

public class HoloBladeImpactEffect {
	private ParticleSystem sparkEffect, stationarySparkEffect, smokeEffect;
	// cached transforms
	private Transform sparkTransform, stationarySparkTransform, smokeTransform;

	public HoloBladeImpactEffect(GameObject sparkPrefab, GameObject stationarySparkPrefab,
		GameObject smokePrefab, Transform assignedParent)
	{
		GameObject spark = GameObject.Instantiate(sparkPrefab) as GameObject;
		GameObject stationarySpark = GameObject.Instantiate(stationarySparkPrefab) as GameObject;
		GameObject smoke = GameObject.Instantiate(smokePrefab) as GameObject;
		spark.transform.parent = assignedParent;
		stationarySpark.transform.parent = assignedParent;
		smoke.transform.parent = assignedParent;

		spark.transform.rotation = Quaternion.identity;
		stationarySpark.transform.rotation = Quaternion.identity;
		smoke.transform.rotation = Quaternion.AngleAxis(-90.0f, new Vector3(1.0f, 0.0f, 0.0f));

		sparkEffect = spark.GetComponent<ParticleSystem>();
		stationarySparkEffect = stationarySpark.GetComponent<ParticleSystem>();
		smokeEffect = smoke.GetComponent<ParticleSystem>();
		sparkTransform = sparkEffect.transform;
		stationarySparkTransform = stationarySparkEffect.transform;
		smokeTransform = smokeEffect.transform;
	}

	public void StartPlayingAtPoint(Vector3 point)
	{
		PlayParticleSystemAtPoint(sparkTransform, sparkEffect, point);
		PlayParticleSystemAtPoint(stationarySparkTransform, stationarySparkEffect, point);
		PlayParticleSystemAtPoint(smokeTransform, smokeEffect, point);
	}

	public void MoveToPoint(Vector3 point)
	{
		sparkTransform.position = point;
		stationarySparkTransform.position = point;
		smokeTransform.position = point;
	}

	public void StopPlaying()
	{
		sparkEffect.Stop();
		stationarySparkEffect.Stop();
		smokeEffect.Stop();
	}

	private void PlayParticleSystemAtPoint(Transform trans, ParticleSystem particleSystem, Vector3 point)
	{
		trans.position = point;
		particleSystem.time = 0.0f;
		particleSystem.Clear();
		particleSystem.Play();
	}
}
