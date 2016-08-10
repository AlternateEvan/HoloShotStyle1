using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace NewtonVR
{
    public class NVRPlayer : MonoBehaviour
    {
        // altspace: register active instance explicitly instead of automatically on awake, since we arrange things to have more than
        // one NVRPlayer in the scene (e.g. one disabled one for oculus, one enabled one for steam)

        /// <summary>
        /// The NVRPlayer managing the player's currently active hands.
        /// </summary>
        public static NVRPlayer Instance { get; set; }

        public bool PhysicalHands = false;

        public NVRHead Head;
        public NVRHand LeftHand;
        public NVRHand RightHand;

        [HideInInspector]
        public NVRHand[] Hands;

        private Dictionary<Collider, NVRHand> ColliderToHandMapping;

        private void Awake()
        {
            NVRInteractables.Initialize();

            if (Head == null)
            {
                Head = this.GetComponentInChildren<NVRHead>();
            }

            // altspace: we don't consider this an error because in some cases we prefer to register hands after instantiation
            /*
            if (LeftHand == null || RightHand == null)
            {
                Debug.LogError("[FATAL ERROR] Please set the left and right hand to a nvrhands.");
            }
            */

            if (Hands == null || Hands.Length == 0)
            {
                Hands = new NVRHand[] { LeftHand, RightHand };
            }

            ColliderToHandMapping = new Dictionary<Collider, NVRHand>();
        }

        public void RegisterHand(NVRHand hand)
        {
            Collider[] colliders = hand.GetComponentsInChildren<Collider>();

            for (int index = 0; index < colliders.Length; index++)
            {
                if (ColliderToHandMapping.ContainsKey(colliders[index]) == false)
                {
                    ColliderToHandMapping.Add(colliders[index], hand);
                }
            }
        }

        public NVRHand GetHand(Collider collider)
        {
            return ColliderToHandMapping[collider];
        }

        public static void DeregisterInteractable(NVRInteractable interactable)
        {
            for (int index = 0; index < Instance.Hands.Length; index++)
            {
				//TODO ALTSPACE: do a null check
	            if (Instance.Hands[index] != null)
	            {
		            Instance.Hands[index].DeregisterInteractable(interactable);
	            }
			}
        }
    }
}