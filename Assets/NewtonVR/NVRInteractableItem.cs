﻿using System;
using UnityEngine;
using System.Collections;

namespace NewtonVR
{
    public class NVRInteractableItem : NVRInteractable
    {
        [Tooltip("If you have a specific point you'd like the object held at, create a transform there and set it to this variable")]
        public Transform InteractionPoint;

        protected float AttachedRotationMagic = 20f;
        protected float AttachedPositionMagic = 3000f;

        protected Transform PickupTransform;

		//TODO ALTSPACE:
		public event Action<NVRHand> OnBeginInteraction;
		public event Action OnEndInteraction;
		private float lastVelocity = 0.0f;


		protected override void Awake()
        {
            base.Awake();
            this.Rigidbody.maxAngularVelocity = 100f;
        }

        protected Vector3 LastVelocityAddition;
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsAttached == true)
            {
                Vector3 PositionDelta;
                Quaternion RotationDelta;

                float angle;
                Vector3 axis;

                if (InteractionPoint != null)
                {
                    RotationDelta = AttachedHand.transform.rotation * Quaternion.Inverse(InteractionPoint.rotation);
                    PositionDelta = (AttachedHand.transform.position - InteractionPoint.position);
                }
                else
                {
                    RotationDelta = PickupTransform.rotation * Quaternion.Inverse(this.transform.rotation);
                    PositionDelta = (PickupTransform.position - this.transform.position);
                }

                RotationDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;

                if (angle != 0)
                {
                    Vector3 AngularTarget = (Time.fixedDeltaTime * angle * axis) * AttachedRotationMagic;
                    this.Rigidbody.angularVelocity = Vector3.MoveTowards(this.Rigidbody.angularVelocity, AngularTarget, 10f);
                }

                Vector3 VelocityTarget = PositionDelta * AttachedPositionMagic * Time.fixedDeltaTime;

                this.Rigidbody.velocity = Vector3.MoveTowards(this.Rigidbody.velocity, VelocityTarget, 10f);

				//TODO ALTSPACE: experimental velocity based haptics

	            if (AttachedHand.Controller != null)
	            {
		            var delta = Math.Abs(Rigidbody.velocity.sqrMagnitude - lastVelocity);
		            AttachedHand.TriggerHapticPulse((ushort) Math.Round(500.0f*delta));
		            lastVelocity = Rigidbody.velocity.sqrMagnitude;
	            }

            }
		}

        public override void BeginInteraction(NVRHand hand)
        {
			//TODO ALTSPACE:
	        if (OnBeginInteraction != null)
	        {
				OnBeginInteraction(hand);
			}

			base.BeginInteraction(hand);

			Vector3 closestPoint = Vector3.zero;
            float shortestDistance = float.MaxValue;
            for (int index = 0; index < Colliders.Length; index++)
            {
                Vector3 closest = Colliders[index].bounds.ClosestPoint(AttachedHand.transform.position);
                float distance = Vector3.Distance(AttachedHand.transform.position, closest);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPoint = closest;
                }
            }

            PickupTransform = new GameObject(string.Format("[{0}] PickupTransform", this.gameObject.name)).transform;
            PickupTransform.parent = hand.transform;
            PickupTransform.position = this.transform.position;
            PickupTransform.rotation = this.transform.rotation;
        }

        public override void EndInteraction()
        {
            base.EndInteraction();
            //TODO ALTSPACE:
            if (OnEndInteraction != null)
            {
                OnEndInteraction();
            }

	        if (PickupTransform != null)
                Destroy(PickupTransform.gameObject);
        }

    }
}