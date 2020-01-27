using System.Collections;
using System.Collections.Generic;
using Assets;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NetworkInterface))]
public class NetworkInterfaceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        NetworkInterface network = target as NetworkInterface;
        if (!network) return;
        DrawDefaultInspector();
        EditorGUI.BeginDisabledGroup( network.UseLocalGenerator);
        
        if (network.Connected)
        {
            if( GUILayout.Button("Disconnect")) network.Disconnect();
        }
        else
        {
            if (GUILayout.Button("Connect")) network.Connect();
        }

        EditorGUI.EndDisabledGroup();
    }
}
