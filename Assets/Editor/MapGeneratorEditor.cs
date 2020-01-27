using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    [CustomEditor(typeof(GeneratorBehaviour))]
    public class MapGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GeneratorBehaviour generator = target as GeneratorBehaviour;
            if (!generator) return;

            //generator.Parameter.ChunkSize = EditorGUILayout.IntField("Chunk Resolution", generator.Parameter.ChunkSize);
            EditorGUI.BeginDisabledGroup(!generator.Network || !generator.Network.UseLocalGenerator && !generator.Network.Connected || generator.PendingChunks );
            if (GUILayout.Button("Generate"))
            {
                generator.GenerateMap();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
