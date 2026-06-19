using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("References")]

    public static PlayerController localPlayer;

    [SerializeField] private CharacterController charController;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> audioStepClips;

    [Header("Camera Reference")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private InputActionReference lookAction;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintMultiplier;
    [SerializeField] private float gravity;
    [SerializeField] private float jumpForce;

    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;


    private readonly float groundDistance = 0.4f;
    private Vector3 velocity;
    private Camera playerCam;
    [SerializeField] private AudioListener listener;

    private PlayerInput input;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;

    private Vector3 lastPosition;

    private const float STEP_COOLDOWN = 0.5f;
    private const float STEP_DISTANCE = 1.5f;

    private void Awake()
    {
        charController = GetComponent<CharacterController>();
        //rb = GetComponent<Rigidbody>();
        playerCam = GetComponentInChildren<Camera>();
        input = GetComponent<PlayerInput>();
        //listener = playerCam.GetComponent<AudioListener>();
        audioSource = GetComponent<AudioSource>();
        lastPosition = this.transform.position;

        moveAction = input.actions["Move"];
        sprintAction = input.actions["Sprint"];
        jumpAction = input.actions["Jump"];

        charController.detectCollisions = false;
        Application.targetFrameRate = 144;
    }

    public void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void Update()
    {
        if (Vector3.Distance(this.transform.position, lastPosition) > STEP_DISTANCE)
        {
            lastPosition = this.transform.position;
            if (IsGrounded())
                PlayStepSound();
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        bool isSprinting = sprintAction.IsPressed();


        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        float speed = isSprinting ? walkSpeed * sprintMultiplier : walkSpeed;

        charController.Move(speed * Time.deltaTime * moveDirection);
        //rb.MovePosition(rb.position + speed * Time.deltaTime * moveDirection);

        if (jumpAction.triggered)
        {
            Debug.Log("Trying to jump");
            Debug.Log($"{charController.isGrounded}");
        }

        if (IsGrounded() && jumpAction.triggered)
        {
            velocity.y = jumpForce;
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime;
        }

        charController.Move(velocity * Time.deltaTime);
        //rb.linearVelocity = velocity;
    }
    private void FixedUpdate()
    {
        //charController.Move(speed * Time.deltaTime * moveDirection);
    }
    private void LateUpdate()
    {
        Vector3 forward = playerCamera.forward;
        forward.y = 0f;
        forward.Normalize();

        if (forward.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(forward);
        playerBody.rotation = targetRotation;
    }


    private void PlayStepSound()
    {
        //if (tempTime > Time.time)
        //return;

        //tempTime = Time.time + STEP_COOLDOWN;
        PlaySoundToPlayer(audioStepClips[UnityEngine.Random.Range(0, audioStepClips.Count)]);
    }

    public void PlaySoundToPlayer(AudioClip clip)
    {
        //audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private bool IsGrounded()
    {
        if (Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask))
            return true;
        else
            return false;
    }
}

