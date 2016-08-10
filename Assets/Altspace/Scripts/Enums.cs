using UnityEngine;
using System.Collections;

public enum ContentType {
	WebUrl,
	WebVideo,
	HolographicApp
}

public enum DisplayType
{	
	Public,
	Personal	
}

public enum PlayerColor
{
	Black = 0,
	Grey = 1,
	White = 2,
	Red = 3, 
	Pink = 4,
	Blue = 5,
	Green = 6,
	Orange = 7,
	Yellow = 8,
	Violet = 9,
}

public enum Emoji
{
    Neutral = 0,
    Smile = 1,
    Frown = 2,
    TongueOut = 3,
    Heart = 4,
    Clap = 5,
    HandUp = 6
}

public enum Layer
{
    DefaultLayer		= 0,
    TransparentFXLayer	= 1,
    IgnoreRaycastLayer	= 2,
	WaterLayer = 4,
	UILayer = 5,
	SpotsLayer = 8,
	BuildingsLayer = 9,
	ThirdPersonOnly = 10,
	LeftEyeOnlyLayer = 11,
	RightEyeOnlyLayer = 12,
	Displays = 13,
	NavMesh = 14,
	Avatars = 15,
	CharacterController = 16,
	Hands = 17,
	Balls = 18,
	OceanReflectors = 19,
	HolographicElements = 20,
	HolographicWindows = 21,
	TriggerableSounds = 22,
	HideFromCamera = 23,
	Interactable = 31,
};

public enum TrackingConfidence
{
	None,
	InferredFromUserAction,
	Low,
	High,
}

public enum BodyPart
{
	Eye,
	Head,
	Neck,
	Spine,
	Hips,
	UpperLeg,
	LowerLeg,
	Foot,
	Toes,
	Shoulder,
	UpperArm,
	LowerArm,
	Hand,
	Thumb,
	Index,
	Middle,
	Ring,
	Pinky
}

public enum BodySide
{
	Left,
	Right,
	Center
}