using UnityEngine;
using System.Collections.Generic;

public class AvatarContentProxy : MonoBehaviour {
	public List<string> CombinedMaterialNames = new List<string>();
	public List<string> HighlightMaterialNames = new List<string>();
	public List<string> PrimaryMaterialNames = new List<string>();
	public Transform HeadBone;
	public Transform NeckBone;
	public List<GameObject> HeadGeometry = new List<GameObject>();
	public Transform LeftArmGeometry;
	public Transform RightArmGeometry;
	public Transform LeftHandGeometry;
	public Transform RightHandGeometry;
	public Transform LeftHandJoint;
	public Transform RightHandJoint;
	public List<GameObject> BoxColliderGOList = new List<GameObject>();
}
