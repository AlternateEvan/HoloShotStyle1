using UnityEngine;

public class RandomizeNextState : StateMachineBehaviour {
	int transitionToIntHash = Animator.StringToHash("TransitionInt");

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
	{
		animator.SetInteger(transitionToIntHash, Random.Range(0, 6));
	}
}
