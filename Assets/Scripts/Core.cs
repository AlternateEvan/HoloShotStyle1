using UnityEngine;
using System.Collections;
using DG.Tweening;

public class Core : MonoBehaviour
{

	public GameObject InnerShield;
	public GameObject OuterShield;
	public float MaxInnerShieldScale;
	public float MaxOuterShieldScale;
	public float ExpandTime;
	public float ContractTime;
	public float PropagationDelay;

	private float _startingInnerShieldScale;
	private float _startingOuterShieldScale;

	void Start ()
	{
		_startingInnerShieldScale = InnerShield.transform.localScale.x;
		_startingOuterShieldScale = OuterShield.transform.localScale.x;

		BeginPulsing();
	}
	
	void Update ()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	
	}

	void BeginPulsing()
	{
		ExpandInner();
		StartCoroutine("StartOuterShieldPulse");
	}

	IEnumerator StartOuterShieldPulse()
	{
		yield return new WaitForSeconds(PropagationDelay);
		ExpandOuter();
	}

	void ExpandInner()
	{
		InnerShield.transform.DOScale(MaxInnerShieldScale, ExpandTime).OnComplete(ContractInner);
	}

	void ExpandOuter()
	{
		OuterShield.transform.DOScale(MaxOuterShieldScale, ExpandTime).OnComplete(ContractOuter);
	}

	void ContractInner()
	{
		InnerShield.transform.DOScale(_startingInnerShieldScale, ContractTime).OnComplete(ExpandInner);
	}

	void ContractOuter()
	{
		OuterShield.transform.DOScale(_startingOuterShieldScale, ContractTime).OnComplete(ExpandOuter);
	}
}
