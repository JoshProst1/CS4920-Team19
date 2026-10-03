using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float mouseSensitivity = 120f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private Camera playerCamera;

    private CharacterController controller;
    private Vector3 velocity;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        BackpackInventory inventory = GetComponent<BackpackInventory>();
        if (inventory != null && inventory.IsInventoryOpen)
        {
            velocity = Vector3.zero;
            return;
        }

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState == CursorLockMode.None)
        {
            bool pointerOverUi = EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && !pointerOverUi)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveInput = new Vector3(horizontal, 0f, vertical).normalized;

        if (moveInput != Vector3.zero)
        {
            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.z;
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
            Vector3 move = moveDirection.normalized * currentSpeed;
            controller.Move(move * Time.deltaTime);
        }

        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
