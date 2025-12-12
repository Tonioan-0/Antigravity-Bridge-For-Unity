using UnityEngine;

/// <summary>
/// Simple first-person player controller with WASD movement and mouse look.
/// Requires CharacterController component.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float gravity = 20f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float lookUpLimit = 80f;
    public float lookDownLimit = -80f;

    private CharacterController controller;
    private Camera playerCamera;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Find or create camera
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("PlayerController: No camera found as child!");
            return;
        }

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (playerCamera == null) return;

        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        // Mouse X = Rotate player (Y axis)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0, mouseX, 0);

        // Mouse Y = Rotate camera (X axis) with limits
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, lookDownLimit, lookUpLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    void HandleMovement()
    {
        if (controller.isGrounded)
        {
            // Get input
            float moveX = Input.GetAxis("Horizontal"); // A/D
            float moveZ = Input.GetAxis("Vertical");   // W/S

            // Calculate direction relative to player facing
            moveDirection = transform.TransformDirection(new Vector3(moveX, 0, moveZ));

            // Apply speed (run with Shift)
            float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            moveDirection *= speed;
        }

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        // Move
        controller.Move(moveDirection * Time.deltaTime);
    }

    void OnDisable()
    {
        // Unlock cursor when disabled
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
