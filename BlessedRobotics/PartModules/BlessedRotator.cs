using UnityEngine;

using System;
using System.Collections.Generic;

namespace BlessedRobotics.PartModules
{
    /// <summary>
    /// Add this PartModule to any part that rotates about a fixed axis.
    /// </summary>
    /// <remarks>This is the most common type of robotic part.</remarks>
    public class BlessedRotator : PartModule
    {
        #region KSPFields // These are part settings that can be loaded via .cfg file or from the save file
        #endregion

        #region KSPEvents // These show up in the part right-click menu
        #endregion

        #region KSPActions // These are behaviors that can be assigned to an action group.
        #endregion

        #region Parent property overrides
        #endregion

        #region Class properties
        #endregion

        #region Local class variables
        protected Transform _fixedMesh;
        protected Transform _movableMesh;
        protected bool _isChildConnected = false;
        protected int _antiDebugSpam = 120;
        #endregion

        #region Parent method overrides
        /// <remarks>
        /// This method is called once when the part is loaded into the current scene (e.g. editor, flight, etc.)
        /// </remarks>
        public override void OnAwake()
        {
            base.OnAwake();

            Debug.Log("[Blessed] Rotator.OnAwake called.");
        }

        /// <remarks>
        /// This method is called once when the part is loaded into the current scene, after OnAwake.
        /// </remarks>
        /// <param name="node"></param>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            Debug.Log("[Blessed] Rotator.OnLoad called.");
        }

        /// <summary>
        /// This method is called once when the part is loaded into the current scene, after OnLoad.
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Debug.Log("[Blessed] Rotator.OnStart called.");

            // Let's see if we can find our fixed mesh and movable mesh
            try
            {
                _fixedMesh = KSPUtil.FindInPartModel(transform, "Fixed");
                _movableMesh = KSPUtil.FindInPartModel(transform, "Movable");

                if (_fixedMesh != null && _movableMesh != null)
                {
                    Debug.Log("[Blessed] Rotator.OnStart: Found fixed mesh!");
                    Debug.Log("[Blessed] Rotator.OnStart: Found movable mesh!");

                    // Give our FixedMesh its own Rigidbody
                    Rigidbody fixedMeshRigidbody = _fixedMesh.gameObject.AddComponent<Rigidbody>();

                    // Give our MovableMesh its own Rigidbody
                    _movableMesh.gameObject.AddComponent<Rigidbody>();

                    // Setup a Joint for our MovableMesh
                    HingeJoint joint = _movableMesh.gameObject.AddComponent<HingeJoint>();

                    // Mate the joint to the FixedMesh
                    joint.connectedBody = fixedMeshRigidbody;

                    // Configure other Joint options
                    joint.anchor = Vector3.zero;
                    joint.axis = new Vector3(0, 0, 1);
                    joint.autoConfigureConnectedAnchor = true;
                    joint.useMotor = true;
                    JointMotor motor = joint.motor;
                    motor.targetVelocity = 20f;
                    motor.force = 0.1f;
                    joint.motor = motor;
                }
                else
                    throw new Exception("Part must contain child GameObjects named Fixed and Movable.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Blessed] Rotator.OnStart encountered an error: " + ex.Message);
            }
        }

        /// <summary>
        /// This method is called once when the part is loaded into the current scene, after OnStart.
        /// </summary>
        /// <param name="state"></param>
        public override void OnStartFinished(StartState state)
        {
            base.OnStartFinished(state);

            try
            {
                // We need to anchor our FixedMesh to the root part's Rigidbody with a Joint
                Rigidbody rootRigidbody = part.GetComponent<Rigidbody>();

                if (_fixedMesh != null && rootRigidbody != null)
                {
                    Debug.Log("[Blessed] Rotator.OnStartFinished: Found root Rigidbody!");

                    // Create a joint to "lock" our FixedMesh in place
                    HingeJoint joint = _fixedMesh.gameObject.AddComponent<HingeJoint>();

                    // Mate the joint to the root part's Rigidbody
                    joint.connectedBody = part.GetComponent<Rigidbody>();

                    // Configure other Joint options
                    joint.anchor = Vector3.zero;
                    joint.axis = new Vector3(0, 0, 1);
                    joint.autoConfigureConnectedAnchor = true;
                    joint.useSpring = true;
                    JointSpring spring = joint.spring;
                    spring.spring = 1000;
                    spring.damper = 1000;
                    spring.targetPosition = 0;
                    joint.spring = spring;
                }
                else
                    throw new Exception("FixedMesh must exist and root part must have a Rigidbody.");

            }
            catch (Exception ex)
            {
                Debug.LogError("[Blessed] Rotator.OnStartFinished encountered an error: " + ex.Message);
            }
        }

        /// <summary>
        /// This method gets called when MonoBehaviour.Update would normally be called.
        /// </summary>
        /// <remarks>
        /// See <see cref="MonoBehaviour"/> for more information.
        /// </remarks>
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!_isChildConnected && part.children.Count > 0 && _antiDebugSpam > 0)
            {
                Debug.Log("[Blessed] Rotator.OnUpdate: Looking for child attach nodes.");
                _antiDebugSpam--;

                // See if child has a ConfigurableJoint yet
                ConfigurableJoint joint;
                foreach (var child in part.children)
                {
                    joint = child.attachJoint.Joint;

                    if (joint != null)
                    {
                        // Reconfigure child's attach node ConfigurableJoint to be locked to our MovableMesh
                        this.ReattachChild(joint);

                        // Reset antiDebugSpam, just because
                        _antiDebugSpam = 120;
                    }
                }
            }
        }

        /// <remarks>
        /// This method gets called periodically during flight. Use it to intercept/modify the values being saved.
        /// </remarks>
        /// <param name="node"></param>
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            Debug.Log("[Blessed] Rotator.OnSave called.");
        }

        // Not sure when this gets called, maybe when you switch vessels?
        public override void OnActive()
        {
            base.OnActive();

            Debug.Log("[Blessed] Rotator.OnActive called.");
        }

        // Not sure when this gets called, maybe when you switch vessels?
        public override void OnInactive()
        {
            base.OnInactive();

            Debug.Log("[Blessed] Rotator.OnInactive called.");
        }
        #endregion

        #region Class methods
        /// <summary>
        /// This method is called by <see cref="OnUpdate"/> to attach a child part's attach node (<see cref="ConfigurableJoint"/>) to our <see cref="_movableMesh"/>.
        /// </summary>
        /// <param name="joint"></param>
        protected void ReattachChild(ConfigurableJoint joint)
        {
            try
            {
                // Get current position of joint
                Debug.Log("[Blessed] Rotator.ReattachChild: Joint is currently anchored to: " + joint.connectedBody.name + ".");
                Debug.Log("[Blessed] Rotator.ReattachChild: Current joint anchor position is: " + joint.anchor.ToString() + ".");
                Debug.Log("[Blessed] Rotator.ReattachChild: Current joint connected anchor position is: " + joint.connectedAnchor.ToString() + ".");
                joint.connectedBody = _movableMesh.GetComponent<Rigidbody>();
                joint.anchor = new Vector3(0, 0.2f, 0);
                joint.autoConfigureConnectedAnchor = true;
                Debug.Log("[Blessed] Rotator.ReattachChild: New joint anchor position is: " + joint.anchor.ToString() + ".");
                Debug.Log("[Blessed] Rotator.ReattachChild: New joint connected anchor position is: " + joint.connectedAnchor.ToString() + ".");

                _isChildConnected = true;
            }
            catch (Exception ex)
            {
                Debug.Log("[Blessed] Rotator.ReattachChild encountered an error: " + ex.Message);
            }
        }
        #endregion
    }
}
