using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SwordScreenOffset))]
public class AnchorPose : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SwordScreenOffset script = (SwordScreenOffset)target;

        GUILayout.Space(15);
        GUILayout.Label("Pose Utilities", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        //Origin
        DrawOriginSection(script);
        GUILayout.Space(10);

        //Pose A
        DrawPoseSection(script, script.poseA, "Pose A");
        GUILayout.Space(10);

        //Pose B
        DrawPoseSection(script, script.poseB, "Pose B");

        if (GUI.changed)
        {
            EditorUtility.SetDirty(script);
        }
    }

    void DrawOriginSection(SwordScreenOffset script)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Origin", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save", GUILayout.Width(60)))
        {
            SavePose(script, script.Origin);
            EditorUtility.SetDirty(script);
        }

        if (GUILayout.Button("Load", GUILayout.Width(60)))
        {
            LoadPose(script, script.Origin);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void DrawPoseSection(SwordScreenOffset script, SwordScreenOffset.AnchorPose pose, string label)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save", GUILayout.Width(60)))
            SavePose(script, pose);

        if (GUILayout.Button("Load", GUILayout.Width(60)))
            LoadPose(script, pose);

        if (GUILayout.Button("Reset", GUILayout.Width(60)))
        {
            ResetPose(script, pose);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }


    void SavePose(SwordScreenOffset script, SwordScreenOffset.AnchorPose pose)
    {
        Vector3 localPos = script.transform.localPosition;

        pose.localPosition = new Vector3(localPos.x, localPos.y, localPos.z);
        pose.localEulerRotation = script.transform.localEulerAngles;

        EditorUtility.SetDirty(script);
    }

    void LoadPose(SwordScreenOffset script, SwordScreenOffset.AnchorPose pose)
    {
        script.transform.localPosition = pose.localPosition;
        script.transform.localEulerAngles = pose.localEulerRotation;
    }

    void ResetPose(SwordScreenOffset script, SwordScreenOffset.AnchorPose pose)
    {
        SwordScreenOffset.AnchorPose originPose = script.Origin;

        pose.localPosition = originPose.localPosition;
        pose.localEulerRotation = originPose.localEulerRotation;
    }
}
