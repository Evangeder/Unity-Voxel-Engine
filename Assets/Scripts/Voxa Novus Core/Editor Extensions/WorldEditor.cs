#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VoxaNovus
{
    [CustomEditor(typeof(World))]
    public class WorldEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            World myScript = (World)target;
            if (GUILayout.Button("Reset physics queue"))
            {
                Debug.Log($"Physics queue cleared, contained {PhysicsQueue.priorityQueue.Count} items");
                PhysicsQueue.priorityQueue.Clear();
            }
        }
    }
}
#endif