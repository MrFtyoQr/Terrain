using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float gravity = -9.81f;

    [Header("Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float maxLookAngle = 80f;

    [Header("References")]
    [SerializeField] Transform cameraTransform;

    CharacterController controller;
    Vector3 velocity;
    float xRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
    }

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        float speed = isSprinting ? sprintSpeed : walkSpeed;
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
