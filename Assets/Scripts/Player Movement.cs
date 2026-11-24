using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private float cameraPitch;
    private float verticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard == null || mouse == null)
            return;

        Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(0f, look.x, 0f);

        cameraPitch = Mathf.Clamp(cameraPitch - look.y, -85f, 85f);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        float horizontal = 0f;
        float forward = 0f;

        if (keyboard.aKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;
        if (keyboard.sKey.isPressed) forward -= 1f;
        if (keyboard.wKey.isPressed) forward += 1f;

        Vector3 move = transform.right * horizontal + transform.forward * forward;
        move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;

        if (controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalSpeed += gravity * Time.deltaTime;
        move.y = verticalSpeed;

        CollisionFlags collisions = controller.Move(move * Time.deltaTime);

        if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            verticalSpeed = 0f;
    }
}