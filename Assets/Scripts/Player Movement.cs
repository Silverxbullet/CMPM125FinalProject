
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 120f;

    private Vector2 movementInput;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        float moveInput = movementInput.y * moveSpeed * Time.fixedDeltaTime;
        float rotateInput = movementInput.x * rotationSpeed * Time.fixedDeltaTime;

        Vector3 movement = transform.forward * moveInput;

        rb.MovePosition(rb.position + movement);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotateInput, 0f));
    }
}
