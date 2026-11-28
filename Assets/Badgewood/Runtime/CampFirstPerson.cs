using UnityEngine;
using UnityEngine.InputSystem;

namespace Badgewood
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CampFirstPerson : MonoBehaviour
    {
        public Camera view;
        public CampGameFlow game;
        public CliffDiveActivity diving;
        public Vector3 spawnPoint = new Vector3(0f, 34.16f, -92f);
        public float walkSpeed = 4.5f;
        public float runSpeed = 6.5f;
        public float jumpHeight = 1.15f;
        public float gravity = 20f;
        private CharacterController controller;
        private float verticalSpeed;
        private float pitch;
        private float coyoteUntil;
        private float jumpUntil;
        public bool Grounded => controller != null && controller.isGrounded;
        public float Radius => controller.radius;
        public Vector3 BodyCenter => transform.TransformPoint(controller.center);

        private void Awake() { controller = GetComponent<CharacterController>(); }

        public void Teleport(Vector3 position, float yaw = 0f, float lookPitch = 0f)
        {
            if (controller == null) controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            pitch = lookPitch;
            view.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            verticalSpeed = 0f;
            coyoteUntil = jumpUntil = -1f;
            controller.enabled = true;
        }

        private void Update()
        {
            if (game == null || !game.GameplayActive || !Application.isFocused) return;
#if UNITY_EDITOR
            UnityEditor.EditorWindow focused = UnityEditor.EditorWindow.focusedWindow;
            if (focused == null || focused.GetType().Name != "GameView") return;
#endif
            Keyboard keys = Keyboard.current;
            Mouse mouse = Mouse.current;
            bool inputAllowed = game.GameplayInputAllowed;
            if (inputAllowed && mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = mouse.delta.ReadValue() * game.Sensitivity;
                transform.Rotate(0f, delta.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch + delta.y * (game.InvertY ? 1f : -1f), -85f, 85f);
                view.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 move = Vector2.zero;
            bool jump = false, run = false;
            if (keys != null && inputAllowed)
            {
                move = new Vector2((keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1f : 0f),
                    (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1f : 0f) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1f : 0f));
                if (!diving.IsDiving) move = Vector2.ClampMagnitude(move, 1f);
                jump = keys.spaceKey.wasPressedThisFrame;
                run = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed;
                if (keys.rKey.wasPressedThisFrame && diving.IsDiving) { diving.Retry(); return; }
            }
            if (jump && diving.CanLaunch)
            {
                diving.Launch();
                return;
            }
            if (!diving.IsDiving)
            {
                if (Grounded) coyoteUntil = Time.time + 0.1f;
                if (jump) jumpUntil = Time.time + 0.12f;
                if (jumpUntil >= Time.time && coyoteUntil >= Time.time)
                {
                    verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
                    jumpUntil = coyoteUntil = -1f;
                }
            }
            float remaining = Mathf.Min(Time.deltaTime, 0.1f);
            while (remaining > 0f && game.GameplayActive)
            {
                float dt = Mathf.Min(remaining, 1f / 90f);
                remaining -= dt;
                bool inDive = diving.IsDiving;
                Vector3 before = BodyCenter;
                if (!inDive && Grounded && verticalSpeed < 0f) verticalSpeed = -2f;
                verticalSpeed -= (inDive ? diving.DiveGravity : gravity) * dt;
                Vector3 velocity = inDive
                    ? diving.CourseRight * (move.x * diving.SteerSpeed) + diving.CourseForward * (diving.ForwardSpeed + diving.ForwardAdjustment(move.y))
                    : (transform.right * move.x + transform.forward * move.y) * (run ? runSpeed : walkSpeed);
                velocity.y = verticalSpeed;
                CollisionFlags collisions = controller.Move(velocity * dt);
                if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
                if (inDive) diving.CheckTravel(before, BodyCenter, transform.position);
                else if (transform.position.y < 5f)
                {
                    Teleport(spawnPoint);
                    game.Notify("Back at camp. Follow the marked trails to the activities.");
                }
            }
            if (inputAllowed && !diving.IsDiving && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(view.transform.position, view.transform.forward, out RaycastHit hit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                {
                    birdclick bird = hit.collider.GetComponentInParent<birdclick>();
                    if (bird != null) bird.Observe();
                    else
                    {
                        lb_Bird livingBird = hit.collider.GetComponentInParent<lb_Bird>();
                        if (livingBird != null) game.ObserveBird(livingBird);
                    }
                }
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus && game != null) game.Pause();
        }
    }
}
