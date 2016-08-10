using System;
using UnityEngine;
#if ALTSPACE_UNITYCLIENT
using System.Collections.Generic;
using Mono.CSharp;
#endif

/// <summary>
/// An AnimationDispatcher sends an RPC to all clients in response to a particular event (its "cause").
/// Upon receiving an RPC, it sets the value of its targetted AnimatorController parameter.
/// </summary>
[RequireComponent(typeof(DeterministicReference))]
public class AnimationDispatcher
#if ALTSPACE_UNITYCLIENT
	: PlayerTriggerVolume, ICursorTarget
#else
	: MonoBehaviour
#endif
{
#if ALTSPACE_UNITYCLIENT
	public static readonly IDictionary<Tuple<string, int>, AnimationDispatcher> AnimationDispatchers = new Dictionary<Tuple<string, int>, AnimationDispatcher>();

	public CursorTargetType CursorTargetType { get { return CursorTargetType.DynamicEnvironment; } }
	public bool ShouldPersistCursor { get { return true; } }
	public bool IsEnabledCursorTarget { get { return true; } }
#endif
	#region The cause and effect of this dispatcher.
	private enum Cause
	{
		TriggerEnter,
		TriggerExit,
		Click,
		ClickToggle
	}
	[SerializeField] private Cause cause;
	[SerializeField] private Animator animator;
	[SerializeField] private string targetParameterName;
	[SerializeField] private AnimatorControllerParameterType targetParameterType;
	// At most one* of these three fields will be used when the animation is fired. 
	// We have all three fields so that a custom editor script is not required for 
	// setting up AnimationDispatchers via the inspector. We determine which 
	// to use based on the targetParameterType.
	// (* AnimatorControllerParameterType.Trigger does not use a field.)
	[SerializeField] private bool targetParameterBoolValue;
	[SerializeField] private int targetParameterIntValue;
	[SerializeField] private float targetParameterFloatValue;
#endregion

#if ALTSPACE_UNITYCLIENT
	private SphericalCursorModule cursorModule { get { return Main.CockpitManager.Cockpit.SphericalCursorModule; } }
	private IList<AnimationDispatcher> siblings;
	private Tuple<string, int> key;

	public void Awake()
	{
		siblings = GetComponents<AnimationDispatcher>();
		key = new Tuple<string, int>(GetComponent<DeterministicReference>().Id, siblings.IndexOf(this));
		AnimationDispatchers[key] = this;
	}

	public void Start()
	{
		cursorModule.Subscribe(this, CursorEventType.PrimaryDown, this, (_) =>
		{
			// Only one AnimationDispatcher per GameObject will receive events from the SphericalCursorModule, 
			// so the one that receives the event communicates to its siblings.
			foreach (var sibling in siblings)
			{
				sibling.OnPrimaryDown();
			}
		});
	}

	public void OnDestroy()
	{
		cursorModule.Unsubscribe(this);
	}
#endif

	public void FireAnimation()
	{
		switch (targetParameterType)
		{
			case AnimatorControllerParameterType.Bool:
				animator.SetBool(targetParameterName, targetParameterBoolValue);
				break;
			case AnimatorControllerParameterType.Int:
				animator.SetInteger(targetParameterName, targetParameterIntValue);
				break;
			case AnimatorControllerParameterType.Float:
				animator.SetFloat(targetParameterName, targetParameterFloatValue);
				break;
			case AnimatorControllerParameterType.Trigger:
				animator.SetTrigger(targetParameterName);
				break;
			default:
				throw new Exception(string.Format("targetParameterType {0} not supported. Check that it is set correctly in the Environment Project.", targetParameterType));
		}
	}

	private void DispatchAnimation()
	{
#if ALTSPACE_UNITYCLIENT
		Main.PlayerManager.Me.Value.GetComponent<PhotonView>().RPC("FireAnimation", PhotonTargets.AllBufferedViaServer, key.Item1, key.Item2);
#else
		FireAnimation();
#endif
	}

#if ALTSPACE_UNITYCLIENT
#region UnityClient cause detection
	public void OnPrimaryDown()
	{
		switch (cause)
		{
			case Cause.Click:
				DispatchAnimation();
				break;
			case Cause.ClickToggle:
				targetParameterBoolValue = !targetParameterBoolValue;
				DispatchAnimation();
				break;
		}
	}

	protected override void OnPlayerTriggerEnter(PlayerController player)
	{
		if (player.IsMe && cause == Cause.TriggerEnter)
		{
			DispatchAnimation();
		}
	}

	protected override void OnPlayerTriggerExit(PlayerController player)
	{
		if (player.IsMe && cause == Cause.TriggerExit)
		{
			DispatchAnimation();
		}
	}
#endregion

#else
#region Environment Projects cause detection
	void OnMouseDown()
	{
		if (cause == Cause.Click)
		{
			DispatchAnimation();
		}
		else if (cause == Cause.ClickToggle)
		{
			targetParameterBoolValue = !targetParameterBoolValue;
			DispatchAnimation();
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (cause == Cause.TriggerEnter)
		{
			DispatchAnimation();
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (cause == Cause.TriggerExit)
		{
			DispatchAnimation();
		}
	}
#endregion
#endif
}

