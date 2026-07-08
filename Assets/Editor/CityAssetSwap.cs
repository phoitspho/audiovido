using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AUDIOVIDO — City Asset Swap
/// Menu: AUDIOVIDO → Assets → Swap District Towers (Skyscraper)
///
/// Replaces each district's primitive "Tower" cube with a real imported building
/// model (CC0 Kenney skyscraper .glb), auto-fitted to the cube's height and seated
/// on the ground. The primitive's renderer is hidden but its collider is kept, so
/// tap-to-focus still works. Idempotent — re-running replaces the previous model.
/// </summary>
public static class CityAssetSwap
{
    const string ModelPath = "Assets/Models/Kenney_Tower6.glb";
    const string CityScene = "Assets/Scenes/Scene_City.unity";

    [MenuItem("AUDIOVIDO/Assets/Swap District Towers (Skyscraper)")]
    public static void SwapDistrictTowers()
    {
        if (SceneManager.GetActiveScene().path != CityScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(CityScene, OpenSceneMode.Single);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (prefab == null)
        {
            Debug.LogError($"[AssetSwap] Model not imported yet: {ModelPath} (let glTFast finish importing, then re-run).");
            return;
        }

        int count = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.name != "Tower") continue; // district main towers only (skyline is "SkyTower_i")
            SwapOne(t, prefab);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log($"[AssetSwap] Swapped {count} district tower(s) with {System.IO.Path.GetFileName(ModelPath)}.");
    }

    static void SwapOne(Transform tower, GameObject prefab)
    {
        Transform parent = tower.parent;
        float height = Mathf.Max(0.01f, tower.lossyScale.y);
        Vector3 basePos = tower.position - Vector3.up * (height * 0.5f);

        var existing = parent.Find("TowerModel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (inst == null) inst = (GameObject)Object.Instantiate(prefab);
        inst.name = "TowerModel";
        inst.transform.SetParent(parent, true);
        inst.transform.position = tower.position;
        inst.transform.rotation = tower.rotation;
        inst.transform.localScale = Vector3.one;

        // Fit model height to the primitive's height.
        if (TryBounds(inst, out Bounds b0) && b0.size.y > 0.001f)
            inst.transform.localScale = Vector3.one * (height / b0.size.y);

        // Seat base on ground and center over the tower footprint.
        if (TryBounds(inst, out Bounds b1))
        {
            inst.transform.position += new Vector3(
                tower.position.x - b1.center.x,
                basePos.y        - b1.min.y,
                tower.position.z - b1.center.z);
        }

        // Hide primitive building parts; keep collider(s) for tap-to-focus.
        HideRenderer(tower);
        HideSibling(parent, "TowerTop");
        HideSibling(parent, "Antenna");
        HideSibling(parent, "SideBlock");
    }

    static void HideRenderer(Transform t)
    {
        var r = t.GetComponent<Renderer>();
        if (r != null) r.enabled = false;
    }

    static void HideSibling(Transform parent, string name)
    {
        var c = parent.Find(name);
        if (c != null) HideRenderer(c);
    }

    static bool TryBounds(GameObject go, out Bounds b)
    {
        b = new Bounds(go.transform.position, Vector3.zero);
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return true;
    }
}
