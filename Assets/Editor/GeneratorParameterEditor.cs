using Assets;
using UnityEditor;

[CustomEditor(typeof(GeneratorParameter))]

public class GeneratorParameterEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GeneratorParameter parameter = target as GeneratorParameter;
        if (!parameter) return;
        parameter.ChunkSize = EditorGUILayout.IntField("Chunk Resolution", parameter.ChunkSize);
    }
}
