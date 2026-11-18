using UnityEngine;

namespace CliffDiversBadge
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ScoutDiver : MonoBehaviour
    {
        public DiveChallenge challenge;
        public Transform visual;
        public float walkSpeed = 5f;
        public float airSteerSpeed = 5f;
        public float forwardDiveSpeed = 6f;
        public float gravity = 4.9f;
        public Vector3 launchPosition = new Vector3(0, 34, 1.5f);
        private CharacterController controller;
        private float verticalSpeed;

        private void Awake() { controller = GetComponent<CharacterController>(); }

        public bool AtLaunchPad => Mathf.Abs(transform.position.x) < 2.2f &&
            transform.position.z > -4f && transform.position.z < 0.7f && transform.position.y > 33f;

        public void ResetTo(Vector3 point)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.position = point;
            transform.rotation = Quaternion.identity;
            verticalSpeed = 0f;
            if (visual != null) visual.localRotation = Quaternion.identity;
            controller.enabled = true;
        }

        private void Launch()
        {
            ResetTo(launchPosition);
            challenge.BeginDive();
        }

        private void Update()
        {
            if (challenge == null || !challenge.CanMove) return;
            if (challenge.State == DiveChallenge.Stage.Exploring && AtLaunchPad && ScoutInput.DivePressed)
                Launch();

            Vector2 input = ScoutInput.Move;
            // Stable integration at common frame rates; cap stalls instead of teleporting.
            float remaining = Mathf.Min(Time.deltaTime, 0.1f);
            while (remaining > 0f && challenge.CanMove)
            {
                float dt = Mathf.Min(remaining, 1f / 120f);
                remaining -= dt;
                bool diving = challenge.State == DiveChallenge.Stage.Diving;
                Vector3 before = transform.position;
                if (!diving && controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
                verticalSpeed -= (diving ? gravity : 18f) * dt;
                Vector3 velocity = diving
                    ? new Vector3(input.x * airSteerSpeed, verticalSpeed, forwardDiveSpeed + input.y * 3f)
                    : new Vector3(input.x * walkSpeed, verticalSpeed, input.y * walkSpeed);
                controller.Move(velocity * dt);
                if (!diving && transform.position.y < 33.3f)
                {
                    challenge.BeginDive();
                    diving = true;
                }
                if (diving) challenge.CheckTravel(before, transform.position, controller.center);
            }
            if (visual != null)
            {
                Quaternion target = challenge.State == DiveChallenge.Stage.Diving
                    ? Quaternion.Euler(65f, 0f, -input.x * 18f) : Quaternion.identity;
                visual.localRotation = Quaternion.Slerp(visual.localRotation, target, 12f * Time.deltaTime);
            }
        }
    }
}
