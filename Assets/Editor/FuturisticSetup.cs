#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FuturisticSetup : EditorWindow
{
    [MenuItem("Tools/Apply Futuristic Materials")]
    static void SetupMaterials()
    {
        // Create Materials folder if needed
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // Floor: very dark metallic blue-black
        Material floorMat = CreateLitMaterial("Floor_Futuristic",
            new Color(0.04f, 0.04f, 0.06f), metallic: 0.85f, smoothness: 0.85f);

        // Walls: dark blue-grey
        Material wallMat = CreateLitMaterial("Wall_Futuristic",
            new Color(0.07f, 0.09f, 0.14f), metallic: 0.3f, smoothness: 0.55f);

        // Ceiling accent strips — glowing cyan
        Material accentMat = CreateLitMaterial("Accent_Futuristic",
            new Color(0.0f, 0.7f, 0.9f), metallic: 0.0f, smoothness: 0.9f,
            emissiveColor: new Color(0.0f, 1.4f, 2.2f));

        // Skylight glass: semi-transparent pale blue
        Material glassMat = CreateLitMaterial("Glass_Futuristic",
            new Color(0.55f, 0.85f, 1.0f, 0.15f), metallic: 0.0f, smoothness: 0.98f,
            transparent: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Assign to scene objects
        AssignMaterial("Floor",        floorMat);
        AssignMaterial("Wall North",   wallMat);
        AssignMaterial("Wall South",   wallMat);
        AssignMaterial("Wall East",    wallMat);
        AssignMaterial("Wall West",    wallMat);
        AssignMaterial("C-N",          accentMat);
        AssignMaterial("C-S",          accentMat);
        AssignMaterial("C-E",          accentMat);
        AssignMaterial("C-W",          accentMat);
        AssignMaterial("Skylight-Glass", glassMat);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FuturisticSetup] Done — futuristic materials applied.");
    }

    static Material CreateLitMaterial(string name, Color baseColor,
        float metallic = 0f, float smoothness = 0.5f,
        Color emissiveColor = default, bool transparent = false)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic",   metallic);
        mat.SetFloat("_Smoothness", smoothness);

        if (emissiveColor != default && emissiveColor != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissiveColor);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (transparent)
        {
            mat.SetFloat("_Surface",   1f);
            mat.SetFloat("_Blend",     0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void AssignMaterial(string goName, Material mat)
    {
        GameObject go = GameObject.Find(goName);
        if (go == null) { Debug.LogWarning($"[FuturisticSetup] Not found: '{goName}'"); return; }
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        rend.sharedMaterial = mat;
        EditorUtility.SetDirty(go);
    }
}
#endif
