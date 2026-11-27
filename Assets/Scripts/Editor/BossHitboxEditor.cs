using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Boss1AttackHitboxes))]
public class BossHitboxEditor : Editor
{
    private static readonly string[] Modes = { "휘두르기", "펀치", "내려찍기", "돌진", "도약 몸통", "충격파", "후방 미사일", "조준 미사일 폭발", "미사일 난사", "조준 미사일 탄체" };
    private static readonly string[] Reach = { "sweepReachAndTopH", "punchReachAndTopH", "slamReachAndTopH" };
    private static readonly string[] Bottom = { "sweepBottomH", "punchSlideClearanceH", "slamBottomH" };
    private static readonly string[] BodyReach = { "sweepBodyReachH", "punchBodyReachH", "slamBodyReachH" };
    private int mode;
    private bool editing;
    private Boss1AttackHitboxes Boxes => (Boss1AttackHitboxes)target;

    private Component Settings()
    {
        switch (mode)
        {
            case 3: return Boxes.GetComponent<Boss1BoostRush>();
            case 4: case 5: return Boxes.GetComponent<Boss1LeapSlam>();
            case 6: return Boxes.GetComponent<Boss1RearMissiles>();
            case 7: case 9: return Boxes.GetComponent<Boss1AimedMissile>();
            case 8: return Boxes.GetComponent<Boss1MissileBarrage>();
            default: return Boxes;
        }
    }

    private string RadiusField => mode == 6 ? "radiusH" :
        mode == 9 ? "projectileRadiusH" : "explosionRadiusH";

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        editing = GUILayout.Toggle(editing, "Scene에서 히트박스 편집", "Button");
        mode = EditorGUILayout.Popup("편집할 판정", mode, Modes);
        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
        var component = Settings();
        if (component != null)
        {
            using var settings = new SerializedObject(component);
            void Field(string name, string label) => EditorGUILayout.PropertyField(settings.FindProperty(name), new GUIContent(label));
            if (mode <= 2)
            {
                Field(Reach[mode], "전방 거리 / 상단 높이 (H)");
                Field(BodyReach[mode], "몸 쪽으로 확장 (H)");
                Field(Bottom[mode], mode == 1 ? "슬라이드 위 여유 (H)" : "하단 높이 (H)");
            }
            else if (mode == 3) { Field("armReach", "팔 거리 (H)"); Field("bodyReachH", "몸 쪽으로 확장 (H)"); Field("crouchHeight", "웅크린 높이 (H)"); }
            else if (mode == 4)
            {
                EditorGUILayout.HelpBox("수직 하강 시작부터 착지 직후까지 보스 BoxCollider2D의 크기와 위치를 따릅니다.", MessageType.Info);
                Field("landingRadiusH", "착지 경고 반경 (H)");
            }
            else if (mode == 5) { Field("waveWidthH", "폭 (H)"); Field("waveHeightH", "높이 (H)"); Field("waveRangeH", "이동 거리 (H)"); }
            else Field(RadiusField, "반경 (H)");
            if (settings.ApplyModifiedProperties()) SceneView.RepaintAll();
        }
        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    private static float Drag(Vector3 point, Vector3 axis)
    {
        float scale = Handles.matrix.MultiplyVector(axis).magnitude;
        float cap = HandleUtility.GetHandleSize(Handles.matrix.MultiplyPoint3x4(point)) * 0.075f / Mathf.Max(scale, 0.001f);
        Vector3 moved = Handles.Slider(point, axis, cap, Handles.DotHandleCap, 0f);
        return Vector3.Dot(moved - point, axis);
    }

    private void OnSceneGUI()
    {
        if (!editing || Settings() == null) return;
        var owner = Boxes.GetComponent<Boss1ProximityAttack>();
        var body = Boxes.GetComponent<BoxCollider2D>();
        if (owner == null || body == null || owner.Height <= 0f) return;
        using var settings = new SerializedObject(Settings());
        SerializedProperty Prop(string name) => settings.FindProperty(name);
        float h = owner.Height;
        Vector3 forward = Vector3.right * owner.Facing;
        Vector3 up = Vector3.up;
        var matrix = Boxes.transform.localToWorldMatrix;
        if (mode >= 5)
            matrix = Matrix4x4.TRS(Boxes.transform.position, Quaternion.identity, Vector3.one * Mathf.Abs(Boxes.transform.lossyScale.y));
        using var drawing = new Handles.DrawingScope(Color.white, matrix);
        EditorGUI.BeginChangeCheck();
        if (mode <= 2)
        {
            Boxes.GetArea(mode + 1, out var center, out var size);
            Vector3 mid = forward * center.x + up * center.y;
            Handles.DrawWireCube(mid, size);
            var reach = Prop(Reach[mode]);
            var bodyReach = Prop(BodyReach[mode]);
            var bottom = Prop(Bottom[mode]);
            Vector2 value = reach.vector2Value;
            float dx = Drag(mid + forward * size.x * 0.5f, forward);
            float dr = Drag(mid - forward * size.x * 0.5f, -forward);
            float dy = Drag(mid + up * size.y * 0.5f, up);
            float db = Drag(mid - up * size.y * 0.5f, up);
            if (dx != 0f) value.x = Mathf.Max(0f, value.x + dx / h);
            if (dr != 0f) bodyReach.floatValue = Mathf.Max(0f, bodyReach.floatValue + dr / h);
            if (dy != 0f) value.y = Mathf.Max(center.y - size.y * 0.5f + 0.25f * h, center.y + size.y * 0.5f + dy) / h;
            if (db != 0f) bottom.floatValue = Mathf.Max(0f, bottom.floatValue + db / h);
            reach.vector2Value = value;
        }
        else if (mode == 3)
        {
            var arm = Prop("armReach");
            var bodyReach = Prop("bodyReachH");
            var crouch = Prop("crouchHeight");
            float height = Mathf.Min(body.size.y, crouch.floatValue * h);
            float width = (arm.floatValue + Mathf.Max(0f, bodyReach.floatValue)) * h;
            Vector3 foot = (Vector3)body.offset - up * body.size.y * 0.5f;
            Vector3 front = foot + forward * (body.size.x * 0.5f + arm.floatValue * h) + up * height * 0.5f;
            Vector3 mid = front - forward * width * 0.5f;
            Handles.DrawWireCube(mid, new Vector3(width, height));
            arm.floatValue = Mathf.Max(0f, arm.floatValue + Drag(front, forward) / h);
            bodyReach.floatValue = Mathf.Max(0f, bodyReach.floatValue + Drag(front - forward * width, -forward) / h);
            crouch.floatValue = Mathf.Clamp(crouch.floatValue + Drag(mid + up * height * 0.5f, up) / h, 0.2f, 1.2f);
        }
        else if (mode == 4)
        {
            Handles.DrawWireCube(body.offset, body.size);
        }
        else if (mode == 5)
        {
            var range = Prop("waveRangeH");
            var width = Prop("waveWidthH");
            var height = Prop("waveHeightH");
            Vector3 end = forward * range.floatValue * h;
            Vector3 mid = end + up * height.floatValue * h * 0.5f;
            Handles.DrawLine(Vector3.zero, end);
            Handles.DrawWireCube(mid, new Vector3(width.floatValue, height.floatValue) * h);
            range.floatValue = Mathf.Max(0.01f, range.floatValue + Drag(end, forward) / h);
            width.floatValue = Mathf.Max(0.01f, width.floatValue + 2f * Drag(mid + forward * width.floatValue * h * 0.5f, forward) / h);
            height.floatValue = Mathf.Max(0.01f, height.floatValue + Drag(mid + up * height.floatValue * h * 0.5f, up) / h);
        }
        else
        {
            var radius = Prop(RadiusField);
            Vector3 preview = (mode == 6 ? -forward : forward) * 2f * h;
            Handles.DrawWireDisc(preview, Vector3.forward, radius.floatValue * h);
            radius.floatValue = Mathf.Max(0.01f, radius.floatValue + Drag(preview + up * radius.floatValue * h, up) / h);
        }
        if (EditorGUI.EndChangeCheck() && settings.ApplyModifiedProperties()) Repaint();
    }
}
