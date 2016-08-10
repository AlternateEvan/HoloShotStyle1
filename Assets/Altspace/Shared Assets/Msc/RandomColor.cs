using UnityEngine;

public class RandomColor : MonoBehaviour {

	// Use this for initialization
	void Start () {
		// pick a random color. this will break batching so use sparingly
		// (since the material has to be unique if the color is set)
		Color newColor = new Color( Random.value, Random.value, Random.value, 1.0f );
		// apply it on current object's material
		GetComponent<Renderer>().material.color = newColor;    
	}
}
