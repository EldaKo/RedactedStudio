using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;

/// <summary>
/// 탈출 게임에 필요한 오브젝트(아이템 3개 + 출구존)를 씬에 자동 배치한다.
/// 메뉴: Tools/Escape Map/Build (Auto)
/// </summary>
public static class EscapeMapBuilder
{
    private const string FogBoundaryName = "_FogBoundary";
    private const string EscapeRootName = "_Escape";
    private const string MapRootName = "-----------map---------";

    // 출구는 플레이어 시작 위치 부근에 배치
    private static readonly Vector3 ExitPosition = new Vector3(-68f, 7.21f, 14f);
    private static readonly Vector3 ExitSize = new Vector3(4f, 4f, 4f);

    // 아이템 3개 정의 (id, 색상)
    private static readonly (string id, Color color)[] Items =
    {
        ("redKey",   new Color(1f, 0.25f, 0.25f)),
        ("greenKey", new Color(0.25f, 1f, 0.4f)),
        ("blueKey",  new Color(0.3f, 0.55f, 1f)),
    };

    [MenuItem("Tools/Escape Map/Build (Auto)")]
    public static void Build()
    {
        // 1) 기존 _Escape 제거 (재실행 대비)
        var existing = GameObject.Find(EscapeRootName);
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject(EscapeRootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Escape Map");

        // 2) Fog boundary 영역 계산 (벽 4개의 안쪽 면 기준)
        if (!TryComputeBoundaryBox(out Bounds box))
        {
            EditorUtility.DisplayDialog("Escape Map",
                "Fog Boundary가 없습니다. 먼저 Tools/Fog Boundary/Build를 실행하세요.", "확인");
            Object.DestroyImmediate(root);
            return;
        }

        // 3) 아이템 3개를 boundary 내부 랜덤 위치에 배치
        var itemRoot = new GameObject("Items");
        itemRoot.transform.SetParent(root.transform, false);

        for (int i = 0; i < Items.Length; i++)
        {
            Vector3 pos = FindRandomPositionInBox(box, attempts: 30);
            CreateItem(itemRoot.transform, Items[i].id, Items[i].color, pos);
        }

        // ProximityTracker 설치 (이미 있으면 재사용)
        if (Object.FindObjectsByType<EscapeProximityTracker>(FindObjectsSortMode.None).Length == 0)
        {
            root.AddComponent<EscapeProximityTracker>();
        }

        
        // 4) 출구 존 배치
        CreateExitZone(root.transform, ExitPosition, ExitSize);

        // 5) 필수 카운트 동기화
        // (런타임에서 자동 초기화되지만, 명시적으로 표시)
        Debug.Log($"[EscapeMap] 아이템 {Items.Length}개, 출구 존 1개 배치 완료. 출구 위치={ExitPosition}");

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Escape Map/Remove")]
    public static void Remove()
    {
        var existing = GameObject.Find(EscapeRootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[EscapeMap] 제거 완료.");
    }

    // -- helpers --

    /// <summary>FogBoundary의 4개 벽 안쪽으로 정의되는 박스를 계산.</summary>
    /// <summary>FogBoundary의 4개 벽 안쪽으로 정의되는 박스를 계산.</summary>
    private static bool TryComputeBoundaryBox(out Bounds box)
    {
        box = default;
        var fog = FindRootByName(FogBoundaryName);
        if (fog == null) return false;

        // 벽 4개 위치의 X/Z min/max로 사각형을 정의
        var walls = new System.Collections.Generic.List<Transform>();
        foreach (Transform wall in fog.transform)
        {
            if (wall.GetComponent<BoxCollider>() != null) walls.Add(wall);
        }
        if (walls.Count < 4) return false;

        float[] xs = new float[walls.Count];
        float[] zs = new float[walls.Count];
        for (int i = 0; i < walls.Count; i++)
        {
            xs[i] = walls[i].position.x;
            zs[i] = walls[i].position.z;
        }
        System.Array.Sort(xs); System.Array.Sort(zs);

        // 정렬 후 1번째와 끝-1번째가 안쪽 벽 (X 양쪽 / Z 양쪽 중간값)
        // 4개라면 [0]=minX wall, [1],[2]=중간(Z벽들), [3]=maxX wall 같이 섞일 수 있어
        // 안전하게: X 차이가 큰 두 값을 X 경계로, Z 차이가 큰 두 값을 Z 경계로
        float minX = xs[0], maxX = xs[xs.Length - 1];
        float minZ = zs[0], maxZ = zs[zs.Length - 1];

        // 지면 높이는 맵 루트의 bounds.min.y
        float groundY = 0f;
        var mapRoot = FindRootByName(MapRootName);
        if (mapRoot != null)
        {
            var renderers = mapRoot.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                groundY = b.min.y;
            }
        }

        const float pad = 5f;
        var center = new Vector3((minX + maxX) * 0.5f, groundY + 30f, (minZ + maxZ) * 0.5f);
        var size2 = new Vector3((maxX - minX) - pad * 2f, 60f, (maxZ - minZ) - pad * 2f);
        box = new Bounds(center, size2);
        return size2.x > 0 && size2.z > 0;
    }

    /// <summary>활성 씬의 루트 오브젝트 중 이름이 일치하는 것을 찾는다 (대소문자 정확).</summary>
    private static GameObject FindRootByName(string name)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name) return roots[i];
        }
        // fallback: hierarchy 어디든 (자식 검색)
        for (int i = 0; i < roots.Length; i++)
        {
            var t = roots[i].transform.Find(name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    // 박스 내부에서 아이템을 떨어뜨릴 랜덤 위치를 찾는다. 카메라 높이보다 낮게 (Y 상한 적용)
    // 박스 내부에서 아이템을 떨어뜨릴 랜덤 위치를 찾는다. terrain 바로 위에 살짝 뜨게 (Y 상한 적용)
    private static Vector3 FindRandomPositionInBox(Bounds box, int attempts)
    {
        // 카메라 월드 Y ≈ 8.5 (player y=7.21 + cam offset 1.28)
        const float MaxItemY = 6f;
        // terrain 위에 떠온 높이 (아주 조금)
        const float HoverOffset = 0.3f;

        for (int i = 0; i < attempts; i++)
        {
            Vector3 sample = new Vector3(
                Random.Range(box.min.x, box.max.x),
                box.max.y,
                Random.Range(box.min.z, box.max.z));

            // 1) raycast로 지면 직접 찾기 (terrain/건물 바닥 등)
            if (Physics.Raycast(sample + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 300f))
            {
                if (hit.point.y <= MaxItemY)
                {
                    return hit.point + Vector3.up * HoverOffset;
                }
            }

            // 2) NavMesh fallback (raycast가 헛공을 치면)
            if (NavMesh.SamplePosition(sample, out NavMeshHit navHit, 25f, NavMesh.AllAreas))
            {
                if (navHit.position.y <= MaxItemY)
                {
                    return navHit.position + Vector3.up * HoverOffset;
                }
            }
        }

        // 실패 시 fallback: 박스 중앙에서 아래로 raycast
        Vector3 fallbackStart = new Vector3(box.center.x, MaxItemY + 50f, box.center.z);
        if (Physics.Raycast(fallbackStart, Vector3.down, out RaycastHit fb, 300f))
        {
            float y = Mathf.Min(fb.point.y, MaxItemY);
            return new Vector3(fb.point.x, y + HoverOffset, fb.point.z);
        }
        return new Vector3(box.center.x, MaxItemY * 0.5f, box.center.z);
    }

    private static void CreateItem(Transform parent, string id, Color color, Vector3 worldPos)
    {
        var go = new GameObject($"Item_{id}");
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;

        // 트리거용 콜라이더 (큰 범위)
        var col = go.AddComponent<SphereCollider>();
        col.radius = 1.2f;
        col.isTrigger = true;

        // EscapeItem 컴포넌트
        var item = go.AddComponent<EscapeItem>();
        item.itemId = id;

        // 시각용 자식 큐브
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.6f;

        // 시각용은 콜라이더 제거 (트리거는 부모가 담당)
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        // 색상 적용 (URP/Built-in 모두 호환되도록 _BaseColor 우선 시도)
        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // 신규 머티리얼 인스턴스
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            // 약간 반짝이게 emissive
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 1.5f);
            }
            renderer.sharedMaterial = mat;
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Escape Item");
    }

    private static void CreateExitZone(Transform parent, Vector3 position, Vector3 size)
    {
        var go = new GameObject("ExitZone");
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = true;

        go.AddComponent<ExitZone>();

        // 시각용: 반투명 노란 큐브 (디버그/길안내용)
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Marker";
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = size;
        Object.DestroyImmediate(visual.GetComponent<Collider>());

        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader);
            var color = new Color(1f, 0.85f, 0.2f, 0.35f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            // 반투명 설정 (URP Unlit은 surface=Transparent로)
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            mat.renderQueue = 3000;
            renderer.sharedMaterial = mat;
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Exit Zone");
    }
}
