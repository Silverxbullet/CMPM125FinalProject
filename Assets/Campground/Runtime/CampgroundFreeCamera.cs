using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Campground
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CampgroundFreeCamera : MonoBehaviour
    {
        [Serializable]
        public struct Viewpoint
        {
            public string title;
            public Vector3 position;
            public Vector3 eulerAngles;
        }

        [SerializeField, Min(0.1f)] private float moveSpeed = 6f;
        [SerializeField, Min(1f)] private float fastMultiplier = 3f;
        [SerializeField, Range(0.01f, 1f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private bool avoidSolidObjects = true;
        [SerializeField, Min(0.05f)] private float clearance = 0.18f;
        [SerializeField] private bool showControls = true;
        [SerializeField] private Viewpoint[] viewpoints = Array.Empty<Viewpoint>();

        private float yaw;
        private float pitch;
        private bool looking;
        private CursorLockMode previousLock;
        private bool previousVisibility;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private string currentView = "Entrance";

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void OnEnable()
        {
            Camera viewCamera = GetComponent<Camera>();
            viewCamera.orthographic = false;
            viewCamera.nearClipPlane = 0.03f;
            viewCamera.farClipPlane = 600f;
            viewCamera.fieldOfView = 60f;
            viewCamera.useOcclusionCulling = false;
            viewCamera.ResetProjectionMatrix();
            ReadRotation();
        }

        private void Update()
        {
            if (!Application.isFocused)
            {
                EndLook();
                return;
            }
#if UNITY_EDITOR
            // Scene view navigation must not also drive the Play Mode camera.
            UnityEditor.EditorWindow focused = UnityEditor.EditorWindow.focusedWindow;
            if (focused == null || focused.GetType().Name != "GameView")
            {
                EndLook();
                return;
            }
#endif

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f1Key.wasPressedThisFrame) showControls = !showControls;
                if (keyboard.rKey.wasPressedThisFrame) ResetView();
                if (keyboard.digit1Key.wasPressedThisFrame) GoToViewpoint(0);
                if (keyboard.digit2Key.wasPressedThisFrame) GoToViewpoint(1);
                if (keyboard.digit3Key.wasPressedThisFrame) GoToViewpoint(2);
                if (keyboard.digit4Key.wasPressedThisFrame) GoToViewpoint(3);
                if (keyboard.digit5Key.wasPressedThisFrame) GoToViewpoint(4);
            }

            if (mouse != null)
            {
                bool escaping = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
                if (escaping || !mouse.rightButton.isPressed) EndLook();
                else if (mouse.rightButton.wasPressedThisFrame) BeginLook();

                if (looking)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    yaw += delta.x * mouseSensitivity;
                    pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -85f, 85f);
                    transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.001f)
                    moveSpeed = Mathf.Clamp(moveSpeed * (scroll > 0f ? 1.25f : 0.8f), 0.5f, 40f);
            }

            if (keyboard == null) return;
            Vector3 input = Vector3.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.z += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.z -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.eKey.isPressed) input.y += 1f;
            if (keyboard.qKey.isPressed) input.y -= 1f;

            float multiplier = 1f;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                multiplier = fastMultiplier;
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                multiplier *= 0.2f;

            Vector3 direction = transform.right * input.x + transform.forward * input.z + Vector3.up * input.y;
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            Move(direction * (moveSpeed * multiplier * Time.unscaledDeltaTime));
        }

        private void Move(Vector3 displacement)
        {
            if (!avoidSolidObjects)
            {
                transform.position += displacement;
                return;
            }

            // Slide along colliders so the viewpoint cannot enter the ground or cabin walls.
            Vector3 position = transform.position;
            for (int step = 0; step < 3 && displacement.sqrMagnitude > 0.000001f; step++)
            {
                float distance = displacement.magnitude;
                Vector3 direction = displacement / distance;
                if (!Physics.SphereCast(position, clearance, direction, out RaycastHit hit,
                    distance + 0.02f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    position += displacement;
                    break;
                }

                float travel = Mathf.Clamp(hit.distance - 0.02f, 0f, distance);
                position += direction * travel;
                displacement = Vector3.ProjectOnPlane(direction * (distance - travel), hit.normal);
            }
            transform.position = position;
        }

        private void BeginLook()
        {
            if (looking) return;
            previousLock = Cursor.lockState;
            previousVisibility = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            looking = true;
        }

        private void EndLook()
        {
            if (!looking) return;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisibility;
            looking = false;
        }

        public void GoToViewpoint(int index)
        {
            if (viewpoints == null || index < 0 || index >= viewpoints.Length) return;
            Viewpoint point = viewpoints[index];
            transform.SetPositionAndRotation(point.position, Quaternion.Euler(point.eulerAngles));
            currentView = point.title;
            ReadRotation();
        }

        public void ResetView()
        {
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            currentView = "Entrance";
            ReadRotation();
        }

        private void ReadRotation()
        {
            yaw = transform.eulerAngles.y;
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, transform.eulerAngles.x), -85f, 85f);
        }

        private void OnDisable() => EndLook();
        private void OnApplicationFocus(bool focused) { if (!focused) EndLook(); }

        private void OnGUI()
        {
            if (!showControls) return;
            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box);
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
                labelStyle.normal.textColor = Color.white;
            }
            float width = Mathf.Min(435f, Screen.width - 24f);
            if (width < 180f) return;
            GUI.Box(new Rect(12f, 12f, width, 142f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(24f, 20f, width - 24f, 130f),
                "CAMP TOUR  |  " + currentView + "\n" +
                "WASD / arrows: fly   |   Q / E: down / up\n" +
                "Hold right mouse: look   |   Esc: release cursor\n" +
                "Shift: faster   |   Ctrl: slower   |   Wheel: speed\n" +
                "1 Entrance  2 Commons  3 Campfire  4 Cliff  5 Overview\n" +
                "R: reset   |   F1: hide controls   |   " + moveSpeed.ToString("0.0") + " m/s", labelStyle);
        }
    }
}
