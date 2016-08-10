using UnityEngine;
using System.Collections;

public class RotateSimpleBehavior : MonoBehaviour {

	public float SpeedX;
	public float SpeedY;
	public float SpeedZ;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		Rotate();
	}

	void Rotate()
	{
		transform.Rotate(SpeedX * Time.deltaTime, SpeedY * Time.deltaTime, SpeedZ * Time.deltaTime);
	}
}
