using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 맵 주위에 보이지 않는 벽을 자동으로 생성하고,
/// 거리 기반 안개를 어두운 청회색으로 설정한다.
/// 메뉴: Tools/Fog Boundary/Build (Auto) 로 실행.
/// </summary>
public static class FogBoundaryBuilder
{
    private const string MapRootName = "-----------map---------";
    private const string BoundaryRootName = "_FogBoundary";

    // 맵 bounds 바깥으로 얼마나 여유를 둘지 (월드 단위)
    private const float Padding = -20f;
    // 벽 두께
    private const float WallThickness = 5f;
    // 벽 높이 추가량 (맵 위로 얼마나 더 올릴지)
    private const float WallExtraHeight = 200f;

    // 안개 설정
    private static readonly Color FogColor = new Color(0.18f, 0.22f, 0.28f, 1f); // 어두운 청회색
    private const float FogStartDistance = 25f;
    private const float FogEndDistance = 90f;

public static void Build()
    {
        // 중앙 광장 범위: X -52~+32, Z -3~+45
        const float MinX = -52f;
        const float MaxX = 32f;
        const float MinZ = -3f;
        const float MaxZ = 45f;
        const float WallY = 2f;      // 지면 기준
        const float wallHeight = 120f;
        const float thickness = 30f; // 두꺼운 벽 = 안개가 더 안 뚫림

        var existing = GameObject.Find(BoundaryRootName);
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject(BoundaryRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Fog Boundary");

        float xLength = (MaxX - MinX) + thickness * 2f;
        float zLength = (MaxZ - MinZ) + thickness * 2f;
        float centerZ = (MinZ + MaxZ) * 0.5f;
        float centerX = (MinX + MaxX) * 0.5f;

        CreateWall(root.transform, "Wall_-X",
            new Vector3(MinX - thickness * 0.5f, WallY, centerZ),
            new Vector3(thickness, wallHeight, zLength));
        CreateWall(root.transform, "Wall_+X",
            new Vector3(MaxX + thickness * 0.5f, WallY, centerZ),
            new Vector3(thickness, wallHeight, zLength));
        CreateWall(root.transform, "Wall_-Z",
            new Vector3(centerX, WallY, MinZ - thickness * 0.5f),
            new Vector3(xLength, wallHeight, thickness));
        CreateWall(root.transform, "Wall_+Z",
            new Vector3(centerX, WallY, MaxZ + thickness * 0.5f),
            new Vector3(xLength, wallHeight, thickness));

        // 안개 설정 - 엄청 빽빽하게
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.12f, 0.12f, 0.14f, 1f); // 거의 검정
        RenderSettings.fogStartDistance = 20f;  // 20m 부터 시작
        RenderSettings.fogEndDistance   = 38f;  // 38m 에서 완전 불투명

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            Undo.RecordObject(mainCam, "Set Camera Background");
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        Debug.Log("[FogBoundary] 중앙광장 안개 배치 완료");
    }

    [MenuItem("Tools/Fog Boundary/Remove")]
    public static void Remove()
    {
        var existing = GameObject.Find(BoundaryRootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }
        RenderSettings.fog = false;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[FogBoundary] 제거 완료.");
    }

    private static void CreateWall(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.position = center;
        go.transform.localScale = Vector3.one;

        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = false; // 물리 충돌

        // 보이지 않게 하기 위해 MeshRenderer는 추가하지 않음
        // (BoxCollider만으로 물리 차단 가능)

        Undo.RegisterCreatedObjectUndo(go, "Create Boundary Wall");
    }
}
