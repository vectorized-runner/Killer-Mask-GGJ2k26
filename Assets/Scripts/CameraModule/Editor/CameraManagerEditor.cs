using UnityEditor;
using UnityEngine;
using CameraModule;

[CustomEditor(typeof(CameraManager))]
public class CameraManagerEditor : Editor
{
    private CameraPositionType _testType = CameraPositionType.Default;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Camera Test", EditorStyles.boldLabel);
        _testType = (CameraPositionType)EditorGUILayout.EnumPopup("Test Position", _testType);
        if (GUILayout.Button("Move Camera To Test Position"))
        {
            CameraManager manager = (CameraManager)target;
            manager.MoveToPosition(_testType);
        }
    }
}
