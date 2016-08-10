using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PlayerShieldPulse : MonoBehaviour {

	public float MaxScale;
	public float ExpandTime;
	public float CollapseTime;
	public Color DestroyedShieldColor;
	public Material ShieldMaterial;

	private float _startingScale;
	private Color _startingColor;
	private Renderer _renderer;
	

	void Start ()
	{
		//all xyz vals should be the same
		_startingScale = transform.localScale.x;
		_renderer = gameObject.GetComponent<Renderer>();
		_startingColor = _renderer.material.GetColor("_V_WIRE_Color");

		for(int i = 0; i < _renderer.materials.Length; i++)
		{
			Debug.Log("a mat: " + _renderer.materials[i].name);
		}
		ShieldMaterial.SetColor("_Color", Color.red);
		
		//Debug.Log("Material color: " + _renderer.material.GetColor("_V_WIRE_Color"));
	}
	
	// Update is called once per frame
	void Update ()
	{
		//Debug.Log("Current Color: " + ShieldMaterial.color);
		if(Input.GetMouseButtonDown(0))
		{
			Debug.Log("Clicked");
			DestroyShield();
		}
		if(Input.GetMouseButtonDown(1))
		{
			Debug.Log("Closing");
			RegenerateShield();
		}
	}

	void DestroyShield()
	{
		Color transparent = new Color(1.0f, 0.0f, 0.0f, 0.0f);
		transform.DOScale(MaxScale, ExpandTime).OnComplete(Finished);
		_renderer.material.DOColor(transparent, "_V_WIRE_Color", ExpandTime);
	}

	void RegenerateShield()
	{
		transform.DOScale(_startingScale, ExpandTime).OnComplete(Finished);
		_renderer.material.DOColor(_startingColor, "_V_WIRE_Color", ExpandTime);
	}

	void Finished()
	{
		Debug.Log("Finished");
	}

	
}
