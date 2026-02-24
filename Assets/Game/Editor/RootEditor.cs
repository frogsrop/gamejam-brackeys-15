using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Root))]
public class RootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
