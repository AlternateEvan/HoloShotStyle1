using UnityEngine;
using System.Collections;

public class CameraMover1 : MonoBehaviour {

	public float Speed;
	public float RotationSpeed;
	public GameObject camera;
	private Rigidbody _rigidbody;

	void Start ()
	{

		_rigidbody = gameObject.GetComponent<Rigidbody>();
	}
	
	void Update ()
	{
		if(Input.GetKeyDown(KeyCode.E))
		{
			transform.Rotate(0.0f, 15.0f, 0.0f);
		}
		if(Input.GetKeyDown(KeyCode.Q))
		{
			transform.Rotate(0.0f, -15.0f, 0.0f);
		}
		
	}
	
	void Move()
	{
		float strafe = Input.GetAxis("Horizontal") * Speed;
		float forwardback = Input.GetAxis("Vertical") * Speed;

		Vector3 finalMovement = new Vector3(strafe, 0.0f, forwardback);
		_rigidbody.velocity = camera.transform.forward * forwardback;
	}

	void Rotate()
	{
		float horizontalRotation = Input.GetAxis("Mouse Y") * RotationSpeed;
		float verticalRotation = Input.GetAxis("Mouse X") * RotationSpeed;

		transform.Rotate(-horizontalRotation, verticalRotation, 0.0f);
		

	}

	void FixedUpdate()
	{
		Move();
		Rotate();
	}
}
