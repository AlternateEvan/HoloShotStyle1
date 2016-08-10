using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(DeterministicReference))]
public class TeleporterProxy : MonoBehaviour
{
	public GameObject TargetZone;
	public List<Transform> Destinations;
}