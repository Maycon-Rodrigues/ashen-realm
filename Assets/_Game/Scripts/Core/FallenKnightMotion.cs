using System.Collections.Generic;
using UnityEngine;

namespace AshenRealm
{
    [RequireComponent(typeof(PlayerCombat))]
    public sealed class FallenKnightMotion : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private Transform weapon;

        private PlayerCombat combat;
        private Transform grip;
        private Quaternion weaponInGrip;
        private Vector3 previousPosition;
        private Vector3 modelRest;
        private float stride;
        private float movementWeight;
        private float lastImpact = float.NegativeInfinity;
        private Joint chest;
        private Joint leftThigh;
        private Joint rightThigh;
        private Joint leftShin;
        private Joint rightShin;
        private Joint leftFoot;
        private Joint rightFoot;
        private Joint leftArm;
        private Joint rightArm;
        private Joint leftForearm;
        private Joint rightForearm;
        private Joint cape;
        private Joint capeHem;
        private Joint tabard;

        private void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            modelRest = model.localPosition;
            var bones = new Dictionary<string, Transform>();
            foreach (Transform bone in model.GetComponentsInChildren<Transform>())
                bones.TryAdd(bone.name, bone);

            chest = new Joint(bones["Chest"], transform);
            leftThigh = new Joint(bones["Thigh.L"], transform);
            rightThigh = new Joint(bones["Thigh.R"], transform);
            leftShin = new Joint(bones["Shin.L"], transform);
            rightShin = new Joint(bones["Shin.R"], transform);
            leftFoot = new Joint(bones["Foot.L"], transform);
            rightFoot = new Joint(bones["Foot.R"], transform);
            leftArm = new Joint(bones["Arm.L"], transform);
            rightArm = new Joint(bones["Arm.R"], transform);
            leftForearm = new Joint(bones["Forearm.L"], transform);
            rightForearm = new Joint(bones["Forearm.R"], transform);
            cape = new Joint(bones["CloakUpper"], transform);
            capeHem = new Joint(bones["CloakLower"], transform);
            tabard = new Joint(bones["Tabard"], transform);
            grip = bones["SwordGrip"];
            weaponInGrip = Quaternion.Inverse(grip.rotation) * weapon.rotation;
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
            combat.Attacked += OnImpact;
        }

        private void OnDisable() => combat.Attacked -= OnImpact;

        private void LateUpdate()
        {
            float distance = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up).magnitude;
            previousPosition = transform.position;
            distance = Mathf.Min(distance, 0.34f);
            float targetWeight = Mathf.Clamp01(distance / Mathf.Max(0.001f, Time.deltaTime * 2.7f));
            movementWeight = Mathf.MoveTowards(movementWeight, targetWeight, Time.deltaTime * 8f);
            stride = Mathf.Repeat(stride + distance * 5.5f, Mathf.PI * 2f);
            float step = Mathf.Sin(stride);
            float breath = Mathf.Sin(Time.time * 2.9f);
            float lift = Mathf.Abs(step) * 0.034f * movementWeight + breath * 0.009f;
            model.localPosition = modelRest + Vector3.up * lift;

            PoseLeg(leftThigh, leftShin, leftFoot, step, movementWeight);
            PoseLeg(rightThigh, rightShin, rightFoot, -step, movementWeight);
            cape.Pose(breath * 3.3f + movementWeight * 9f, step * 5f * movementWeight, 0f);
            capeHem.Pose(breath * -2.5f + Mathf.Sin(stride - 0.56f) * 12f * movementWeight, 0f, step * 3f);
            tabard.Pose(breath * 2f - step * 8f * movementWeight, step * 4f * movementWeight, 0f);

            float swing = step * 14f * movementWeight;
            float yaw = 0f;
            float elbow = 0f;
            float shoulder = 0f;
            float windup = combat.WindupProgress;
            float recovery = (Time.time - lastImpact) / 0.29f;
            if (windup >= 0f)
            {
                float release = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, windup));
                float ready = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.65f, windup));
                yaw = Mathf.Lerp(-63f * ready, 65f, release);
                shoulder = Mathf.Lerp(-19f * ready, -28f, release);
                elbow = Mathf.Lerp(-49f * ready, -14f, release);
            }
            else if (recovery < 1f)
            {
                yaw = Mathf.Lerp(65f, 0f, recovery);
                shoulder = Mathf.Lerp(-28f, 0f, recovery);
                elbow = Mathf.Lerp(-14f, 0f, recovery);
            }

            chest.Pose(breath * 1.4f, yaw * 0.34f - step * 5f * movementWeight, step * 2.3f * movementWeight);
            leftArm.Pose(swing * 0.66f, -yaw * 0.13f, -yaw * 0.06f);
            leftForearm.Pose(0f, yaw * 0.16f, 0f);
            rightArm.Pose(swing + shoulder, yaw * 0.82f, -yaw * 0.12f);
            rightForearm.Pose(elbow, yaw * 0.46f, 0f);
            weapon.SetPositionAndRotation(grip.position, grip.rotation * weaponInGrip);
        }

        private static void PoseLeg(Joint thigh, Joint shin, Joint foot, float step, float strength)
        {
            thigh.Pose(step * 25f * strength, 0f, 0f);
            shin.Pose(Mathf.Max(0f, -step) * 34f * strength, 0f, 0f);
            foot.Pose((-step * 14f - Mathf.Max(0f, -step) * 16f) * strength, 0f, 0f);
        }

        private void OnImpact() => lastImpact = Time.time;

        private readonly struct Joint
        {
            private readonly Transform bone;
            private readonly Quaternion rest;
            private readonly Vector3 pitchAxis;
            private readonly Vector3 yawAxis;
            private readonly Vector3 rollAxis;

            public Joint(Transform bone, Transform actor)
            {
                this.bone = bone;
                rest = bone.localRotation;
                pitchAxis = bone.InverseTransformDirection(actor.right);
                yawAxis = bone.InverseTransformDirection(actor.up);
                rollAxis = bone.InverseTransformDirection(actor.forward);
            }

            public void Pose(float pitch, float yaw, float roll)
            {
                bone.localRotation = rest * Quaternion.AngleAxis(yaw, yawAxis) *
                    Quaternion.AngleAxis(pitch, pitchAxis) * Quaternion.AngleAxis(roll, rollAxis);
            }
        }
    }
}
