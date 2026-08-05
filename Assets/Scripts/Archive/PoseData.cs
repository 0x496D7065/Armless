using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Pose Data")]
public class PoseData : ScriptableObject
{
    [Header("ID")]
    //public int poseID;
    public string poseName;

    [Header("Pose")]
    public Vector3 localPosition;
    public Vector3 localEulerRotation;

    [Header("Animation")]
    public string attackTrigger;

    [Header("Attack Result")]
    public PoseData chainPose;

    [Header("Can Combo to")]
    public List<PoseData> possibleComboPose;
}
