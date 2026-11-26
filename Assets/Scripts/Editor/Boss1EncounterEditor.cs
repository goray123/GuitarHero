using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Boss1Encounter))]
public sealed class Boss1EncounterEditor : Editor
{
    private static readonly string[] References = {
        "player", "boss", "triggerPoint", "landingPoint", "leftGate", "rightGate", "cameraFollow"
    };
    private bool showReferences;
    private bool showMap;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "player", "boss", "triggerPoint",
            "landingPoint", "leftGate", "rightGate", "cameraFollow");

        showMap = EditorGUILayout.Foldout(showMap, "맵 위치 · 카메라 · 셔터", true);
        if (showMap)
        {
            DrawSettings(Reference<Transform>("triggerPoint"), "등장 트리거", "m_LocalPosition");
            DrawSettings(Reference<Transform>("landingPoint"), "보스 착지 지점", "m_LocalPosition");
            DrawSettings(Reference<CityCameraFollow>("cameraFollow"), "맵 카메라", "leftEdge",
                "rightEdge", "height", "lookAhead", "smoothTime");
            DrawShutter("leftGate", "왼쪽 셔터");
            DrawShutter("rightGate", "오른쪽 셔터");
        }

        showReferences = EditorGUILayout.Foldout(showReferences, "씬 오브젝트 연결", true);
        if (showReferences)
            foreach (string field in References)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
        serializedObject.ApplyModifiedProperties();
    }

    private T Reference<T>(string field) where T : Object =>
        serializedObject.FindProperty(field).objectReferenceValue as T;

    private void DrawShutter(string field, string label)
    {
        var gate = Reference<BoxCollider2D>(field);
        if (gate != null) DrawSettings(gate.GetComponent<ArenaShutter>(), label, "openDistance", "travelSpeed");
    }

    // Edit the scene objects directly so Undo and prefab overrides retain their normal behavior.
    private static void DrawSettings(Object owner, string label, params string[] fields)
    {
        if (owner == null) return;
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        using (var linked = new SerializedObject(owner))
        {
            foreach (string field in fields)
                EditorGUILayout.PropertyField(linked.FindProperty(field));
            linked.ApplyModifiedProperties();
        }
    }
}
