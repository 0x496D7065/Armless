using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SwordPose))]
public class AnchorMultiPose : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SwordPose script = (SwordPose)target;

        GUILayout.Space(15);
        GUILayout.Label("Pose Utilities", EditorStyles.boldLabel);
        GUILayout.Space(5);

        DrawPoseList(script, script.NeutralPoses, "Neutral Poses");
        GUILayout.Space(10);

        DrawPoseList(script, script.DefensePoses, "Defense Poses");

        if (GUI.changed)
        {
            EditorUtility.SetDirty(script);
        }
    }

    void DrawPoseList(SwordPose script, List<PoseData> poseList, string title)
    {
        GUILayout.Label(title, EditorStyles.boldLabel);

        for (int i = 0; i < poseList.Count; i++)
        {
            PoseData pose = poseList[i];
            if (pose == null)
            {
                EditorGUILayout.HelpBox($"Missing Pose Asset at index {i}", MessageType.Warning);
                continue;
            }

            GUILayout.BeginVertical("box");

            EditorGUILayout.ObjectField("Asset", pose, typeof(PoseData), false);

            EditorGUI.BeginChangeCheck();

            string newName = EditorGUILayout.TextField("Pose Name", pose.poseName);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pose, "Rename Pose");
                pose.poseName = newName;
                EditorUtility.SetDirty(pose);
            }

            EditorGUI.BeginChangeCheck();

            PoseData newChain = (PoseData)EditorGUILayout.ObjectField("Chain", pose.chainPose, typeof(PoseData), false);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(pose, "Change Chain Pose");
                pose.chainPose = newChain;
                EditorUtility.SetDirty(pose);
            }

            if (GUILayout.Button("Rename Asset"))
            {
                string path = AssetDatabase.GetAssetPath(pose);
                AssetDatabase.RenameAsset(path, pose.poseName);
                AssetDatabase.SaveAssets();
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save"))
            {
                SavePose(script, pose);
            }

            if (GUILayout.Button("Load"))
            {
                LoadPose(script, pose);
            }

            if (GUILayout.Button("Remove"))
            {
                RemovePose(script, poseList, pose);
                return;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        if (GUILayout.Button($"Add New Pose To {title}"))
        {
            CreatePoseAsset(script, poseList);
        }
    }


    void SavePose(SwordPose script, PoseData pose)
    {
        Undo.RecordObject(pose, "Save Pose");

        pose.localPosition = script.transform.localPosition;
        pose.localEulerRotation = script.transform.localEulerAngles;

        EditorUtility.SetDirty(pose);
        AssetDatabase.SaveAssets();
    }

    void LoadPose(SwordPose script, PoseData pose)
    {
        Undo.RecordObject(script.transform, "Load Pose");

        script.transform.localPosition = pose.localPosition;
        script.transform.localEulerAngles = pose.localEulerRotation;
    }

    void RemovePose(SwordPose script, List<PoseData> poseList, PoseData pose)
    {
        Undo.RecordObject(script, "Remove Pose");

        poseList.Remove(pose);

        EditorUtility.SetDirty(script);
    }

    void CreatePoseAsset(SwordPose script, List<PoseData> poseList)
    {
        PoseData pose = ScriptableObject.CreateInstance<PoseData>();

        pose.name = "New Pose";

        string path = EditorUtility.SaveFilePanelInProject("Create Pose", "New Pose", "asset", "Choose location");

        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(pose, path);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(script, "Add Pose");

        poseList.Add(pose);

        EditorUtility.SetDirty(script);
    }
}
