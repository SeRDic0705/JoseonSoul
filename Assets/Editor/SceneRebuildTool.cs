using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SceneRebuildTool
{
    private const string PlayerSOPath = "Assets/ScriptableObjects/Datas/PlayerSO.asset";
    private const string CameraSOPath = "Assets/ScriptableObjects/Datas/CameraSO.asset";
    private const string CharacterPrefabPath = "Assets/Prefabs/Solider_Fist.prefab";

    [MenuItem("Tools/Joseon/Rebuild Player And Camera", false)]
    public static void RebuildPlayerAndCamera()
    {
        var playerSO = AssetDatabase.LoadAssetAtPath<PlayerSO>(PlayerSOPath);
        var cameraSO = AssetDatabase.LoadAssetAtPath<CameraSO>(CameraSOPath);
        var characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);

        if (playerSO == null || cameraSO == null || characterPrefab == null)
        {
            Debug.LogError("[SceneRebuildTool] 필요한 에셋을 찾을 수 없습니다. PlayerSO/CameraSO/Solider_Fist 경로를 확인하세요.");
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[SceneRebuildTool] 씬에 MainCamera 태그가 붙은 카메라가 없습니다. mainscene.unity를 열고 다시 실행하세요.");
            return;
        }

        if (GameObject.Find("Player") != null)
        {
            Debug.LogWarning("[SceneRebuildTool] 이미 'Player' 오브젝트가 있어 중단합니다. 기존 Player를 지우고 다시 실행하세요.");
            return;
        }

        if (mainCamera.GetComponent<CameraController>() != null)
        {
            Debug.LogWarning("[SceneRebuildTool] Main Camera에 이미 CameraController가 있어 중단합니다.");
            return;
        }

        // --- Player root ---
        GameObject playerGO = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(playerGO, "Rebuild Player");

        var controller = Undo.AddComponent<CharacterController>(playerGO);
        controller.radius = 0.3f;
        controller.height = 1.7f;
        controller.center = new Vector3(0f, 0.85f, 0f);

        var rb = Undo.AddComponent<Rigidbody>(playerGO);
        rb.isKinematic = true;
        rb.useGravity = false;

        Undo.AddComponent<PlayerInput>(playerGO);

        var forceReceiver = Undo.AddComponent<ForceReceiver>(playerGO);
        var forceReceiverSO = new SerializedObject(forceReceiver);
        forceReceiverSO.FindProperty("controller").objectReferenceValue = controller;
        forceReceiverSO.ApplyModifiedProperties();

        // Visual model as child (keeps Animator, which Player.cs finds via GetComponentInChildren)
        GameObject characterInstance = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
        Undo.RegisterCreatedObjectUndo(characterInstance, "Rebuild Player");
        characterInstance.transform.SetParent(playerGO.transform, false);
        characterInstance.transform.localPosition = Vector3.zero;
        characterInstance.transform.localRotation = Quaternion.identity;

        // CameraTarget above the head
        GameObject cameraTargetGO = new GameObject("CameraTarget");
        Undo.RegisterCreatedObjectUndo(cameraTargetGO, "Rebuild Player");
        cameraTargetGO.transform.SetParent(playerGO.transform, false);
        cameraTargetGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        var player = Undo.AddComponent<Player>(playerGO);
        var playerEditor = new SerializedObject(player);
        playerEditor.FindProperty("<Data>k__BackingField").objectReferenceValue = playerSO;
        playerEditor.ApplyModifiedProperties();

        // --- Camera ---
        var cameraController = Undo.AddComponent<CameraController>(mainCamera.gameObject);
        var cameraEditor = new SerializedObject(cameraController);
        cameraEditor.FindProperty("<Data>k__BackingField").objectReferenceValue = cameraSO;
        cameraEditor.FindProperty("target").objectReferenceValue = cameraTargetGO.transform;
        cameraEditor.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = playerGO;

        Debug.Log("[SceneRebuildTool] Player/Camera 세팅 완료. CharacterController 크기(radius/height/center)를 캐릭터 모델에 맞게 조정한 뒤 Ctrl+S로 씬을 저장하세요.");
    }

    [MenuItem("Tools/Joseon/Rebuild Player And Camera", true)]
    private static bool ValidateRebuildPlayerAndCamera()
    {
        return !EditorApplication.isPlaying;
    }
}
