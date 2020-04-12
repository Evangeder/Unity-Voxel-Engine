#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VoxaNovus;

[CustomEditor(typeof(Chunk))]
public class ChunkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Chunk myScript = (Chunk)target;
        if (GUILayout.Button("Force update"))
        {
            //myScript.Job_UpdateChunk();
        }
    }
}
#endif
