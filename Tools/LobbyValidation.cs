// Unity CLI: script run --file Tools/LobbyValidation.cs --entry LobbyValidation.Run
// 편집 모드에서 저장된 연결만 확인하며 Play 모드를 시작하지 않는다.
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class LobbyValidation
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        Debug.Log("PASS: " + message);
    }

    public static string Run()
    {
        Check(!EditorApplication.isPlaying, "Validation stays in Edit mode");
        Check(SceneManager.GetActiveScene().path == "Assets/Scenes/Lobby.unity", "Lobby scene is open");
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        var dummy = Object.FindFirstObjectByType<TrainingDummy>();
        Check(player != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player) == "Assets/Prefabs/Player.prefab",
            "Lobby uses the existing player prefab");
        Check(dummy != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(dummy) == "Assets/Prefabs/TrainingDummy.prefab",
            "Lobby uses the existing training dummy prefab");
        var zones = Object.FindObjectsByType<LobbyInteraction>(FindObjectsSortMode.None).OrderBy(x => x.transform.position.x).ToArray();
        Check(zones.Length == 3 && zones[0].name == "연주 스킬 교체 구역" && zones[1].name == "의뢰 게시판 구역" && zones[2].name == "이펙터 교체 구역",
            "Skills, board and effector follow the reference order");
        Check(dummy.transform.position.x < player.transform.position.x && player.transform.position.x < zones[0].transform.position.x,
            "Training area is left of the initial player spawn");
        for (int i = 0; i < zones.Length; i++)
        {
            var settings = new SerializedObject(zones[i]);
            Check(zones[i].GetComponent<BoxCollider2D>().isTrigger &&
                !string.IsNullOrEmpty(settings.FindProperty("interactionName").stringValue), "Clickable zone has a label and does not block movement");
            Check(settings.FindProperty("sceneToLoad").stringValue == (i == 1 ? "Boss01_Arena" : ""),
                "Only the board loads the boss arena; NPCs only log");
            Check(settings.FindProperty("allowUpArrow").boolValue == (i == 1),
                "Only the board accepts nearby Up Arrow interaction");
        }
        Check(EditorBuildSettings.scenes[0].path == "Assets/Scenes/Lobby.unity" &&
            EditorBuildSettings.scenes.Any(x => x.enabled && x.path == "Assets/Scenes/Boss01_Arena.unity"),
            "Build starts in lobby and includes boss arena");
        var follow = Object.FindFirstObjectByType<CityCameraFollow>();
        var cameraSettings = new SerializedObject(follow);
        Check(cameraSettings.FindProperty("target").objectReferenceValue == player.transform &&
            cameraSettings.FindProperty("viewCamera").objectReferenceValue == Camera.main &&
            Mathf.Approximately(Camera.main.orthographicSize, 2.25f), "Camera follows the player with the reference zoom");
        Check(Camera.main.transform.parent == follow.transform, "Camera shake remains separate from camera tracking");
        var ground = GameObject.Find("LobbyGround").GetComponent<BoxCollider2D>();
        Check(Mathf.Abs(player.GetComponent<BoxCollider2D>().bounds.min.y - ground.bounds.max.y) < 0.03f &&
            Mathf.Abs(dummy.GetComponent<BoxCollider2D>().bounds.min.y - ground.bounds.max.y) < 0.03f,
            "Player and dummy stand at floor height");
        Check(GameObject.Find("LeftBoundary") != null && GameObject.Find("RightBoundary") != null, "Both lobby boundaries exist");
        Check(!SceneManager.GetActiveScene().isDirty, "Lobby is saved");
        return "PASS: saved lobby layout, prefab references, click settings, camera, floor and build scenes (Edit mode only)";
    }
}
