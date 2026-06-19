using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class SwordPose : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform sword;
    [SerializeField] private CinemachineInputAxisController inputAxisController;

    [Header("Movement")]
    public float sensitivity = 0.3f;
    public float maxOffset = 0.5f;

    [Header("Smoothing")]
    public float smoothSpeed = 10f;
    public float rotationSmoothSpeed = 10f;

    [Header("Input buffer")]
    public float attackBufferTime = 0.2f;

    public bool IsInAttack { get; private set; }

    [Header("Rotation Blending")]
    public List<PoseData> NeutralPoses = new();
    public List<PoseData> DefensePoses = new();

    private Vector3 targetLocalPos;
    private Vector3 currentLocalPos;
    private PoseState currentState;
    private bool isInChainWindow = false;
    private PoseData lastAttackPose;

    private bool bufferedAttack;
    private float bufferedAttackTimer;


    [System.Serializable]
    public enum PoseState
    {
        Neutral,
        Attack,
        Defense
    }

    void Start()
    {
        currentLocalPos = transform.localPosition;
        targetLocalPos = currentLocalPos;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsInAttack)
            {
                OnAttackStarted();
            }
            else
            {
                bufferedAttack = true;
                bufferedAttackTimer = attackBufferTime;
            }
        }
        if (bufferedAttack)
        {
            Debug.Log("Buffered Attack entered");
            bufferedAttackTimer -= Time.deltaTime;
            OnAttackStarted();
            if (bufferedAttackTimer <= 0)
                bufferedAttack = false;
        }
    }

    void LateUpdate()
    {
        if (!IsInAttack || isInChainWindow)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            targetLocalPos += new Vector3(mouseX, mouseY, 0f) * sensitivity;

            // Clamp movement area
            targetLocalPos.x = Mathf.Clamp(targetLocalPos.x, -maxOffset, maxOffset);
            targetLocalPos.y = Mathf.Clamp(targetLocalPos.y, -maxOffset, maxOffset);

            currentLocalPos = Vector3.Lerp(
            currentLocalPos,
            targetLocalPos,
            Time.deltaTime * smoothSpeed);

            transform.localPosition = currentLocalPos;
            UpdateRotation();
        }
    }

    void UpdateRotation()
    {
        List<PoseData> activePoses = GetActivePoseSet();
        if (activePoses == null || activePoses.Count == 0)
            return;

        Vector2 current2D = new Vector2(currentLocalPos.x, currentLocalPos.y);

        float[] weights = new float[activePoses.Count];
        float totalWeight = 0f;

        for (int i = 0; i < activePoses.Count; i++)
        {
            float distance = Vector2.Distance(current2D, activePoses[i].localPosition);
            float weight = 1f / (distance + 0.0001f);// this avoid division by 0 if anchor is miraculously exactly on the pose point

            weights[i] = weight;
            totalWeight += weight;
        }

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] /= totalWeight;
        }

        Quaternion blended = Quaternion.Euler(activePoses[0].localEulerRotation);

        float accumulatedWeight = weights[0];

        for (int i = 1; i < activePoses.Count; i++)
        {
            Quaternion nextRot = Quaternion.Euler(activePoses[i].localEulerRotation);
            float t = weights[i] / (accumulatedWeight + weights[i]);
            blended = Quaternion.Slerp(blended, nextRot, t);
            accumulatedWeight += weights[i];
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, blended, Time.deltaTime * rotationSmoothSpeed);
    }

    List<PoseData> GetActivePoseSet()
    {
        return currentState switch
        {
            PoseState.Defense => DefensePoses,
            _ => NeutralPoses,
        };
    }

    PoseData GetClosestPose(List<PoseData> poses)
    {
        Vector2 current2D = (Vector2)currentLocalPos;

        float closest = float.MaxValue;
        PoseData bestPose = null;

        foreach (var pose in poses)
        {
            float dist = Vector2.Distance(current2D, pose.localPosition);
            if (dist < closest)
            {
                closest = dist;
                bestPose = pose;
            }
        }

        return bestPose;
    }

    public void OnAttackStarted()
    {
        if (IsInAttack && !isInChainWindow)
            return;
        var poses = NeutralPoses;
        if (poses == null || poses.Count == 0)
            return;

        PoseData selectedPose = GetClosestPose(poses);
        if (selectedPose == null)
            return;

        if (isInChainWindow)
        {
            if (!lastAttackPose.possibleComboPose.Contains(selectedPose))
                return;
        }
        isInChainWindow = false;
        bufferedAttack = false;

        foreach (var axis in inputAxisController.Controllers)// Camera sensibility reduced during attack
        {
            Debug.Log(axis.Name);
            if (axis.Name == "Look X (Pan)")
                axis.Input.Gain = 0.3f;
            else if (axis.Name == "Look Y (Tilt)")
                axis.Input.Gain = -0.3f;
        }
        lastAttackPose = selectedPose;

        transform.localPosition = selectedPose.localPosition;

        IsInAttack = true;
        animator.SetTrigger(selectedPose.attackTrigger);
        Debug.Log("attack started");
    }

    public void ChainWindowEntered()
    {
        isInChainWindow = true;
        if (lastAttackPose.chainPose != null)
        {
            targetLocalPos = lastAttackPose.chainPose.localPosition;
        }
    }

    public void OnAttackFinished()
    {
        isInChainWindow = false;

        foreach (var axis in inputAxisController.Controllers) //Camera sensibility back to normal
        {
            if (axis.Name == "Look X (Pan)")
                axis.Input.Gain = 1f;
            else if (axis.Name == "Look Y (Tilt)")
                axis.Input.Gain = -1f;
        }
        IsInAttack = false;
        Debug.Log("attack finished");
    }
}
