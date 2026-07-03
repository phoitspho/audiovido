using UnityEngine;
using UnityEditor;

/// <summary>
/// AUDIOVIDO — Lounge Scene Builder
/// Menu: AUDIOVIDO → Build Lounge Scene
/// Builds all 3D geometry for Scene_Lounge (SCR-19) using design tokens.
/// Run this while Scene_Lounge is open.
/// </summary>
public static class LoungeSceneBuilder
{
    // ── Design Tokens (spec §1.2) ────────────────────────────────────────────
    static readonly Color BG_PRIMARY   = HexColor("0A0A0F"); // floor / walls
    static readonly Color BG_SECONDARY = HexColor("111118"); // bar counter
    static readonly Color AMBER        = HexColor("FFD700"); // stools / accent
    static readonly Color CYAN         = HexColor("00D4FF"); // neon bar strip
    static readonly Color MID_GREY     = HexColor("1A1A2E"); // tables
    static readonly Color DRIFT_COLOR  = HexColor("3D2B1F"); // DRIFT placeholder

    [MenuItem("AUDIOVIDO/Build Lounge Scene")]
    public static void BuildLoungeScene()
    {
        // Safety check
        if (!UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().name.Contains("Lounge"))
        {
            if (!EditorUtility.DisplayDialog("Wrong Scene?",
                    "Active scene is not Scene_Lounge. Build anyway?", "Yes", "Cancel"))
                return;
        }

        // ── Materials ────────────────────────────────────────────────────────
        Material matWalls    = URP_Lit("Mat_Walls",      BG_PRIMARY,   0f);
        Material matBar      = URP_Lit("Mat_BarCounter", BG_SECONDARY, 0f);
        Material matStool    = URP_Lit("Mat_Stool",      AMBER,        0f);
        Material matNeon     = URP_Emissive("Mat_NeonCyan", CYAN,      2.5f);
        Material matTable    = URP_Lit("Mat_Table",      MID_GREY,     0f);
        Material matDrift    = URP_Lit("Mat_DriftProxy", DRIFT_COLOR,  0f);
        Material matFloor    = URP_Lit("Mat_Floor",      BG_PRIMARY,   0f);
        Material matAmberNeon= URP_Emissive("Mat_NeonAmber", AMBER,    1.8f);

        // ── Root container ───────────────────────────────────────────────────
        GameObject root = new GameObject("Lounge_Geometry");

        // ── Floor ────────────────────────────────────────────────────────────
        // Plane default = 10x10 units; scale 1.2 → 12x12m room
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        floor.GetComponent<Renderer>().sharedMaterial = matFloor;

        // ── Ceiling ──────────────────────────────────────────────────────────
        GameObject ceiling = Box("Ceiling", root.transform,
            new Vector3(0, 4f, 0), new Vector3(12f, 0.15f, 12f), matWalls);

        // ── Walls ────────────────────────────────────────────────────────────
        // Back wall (Z+)
        Box("Wall_Back",  root.transform, new Vector3(0, 2f, 6f),  new Vector3(12f, 4f, 0.2f), matWalls);
        // Front wall (Z-) — partial (doorway feel, no solid wall)
        Box("Wall_Front_L", root.transform, new Vector3(-4f, 2f, -6f), new Vector3(4f, 4f, 0.2f), matWalls);
        Box("Wall_Front_R", root.transform, new Vector3( 4f, 2f, -6f), new Vector3(4f, 4f, 0.2f), matWalls);
        // Left wall (X-)
        Box("Wall_Left",  root.transform, new Vector3(-6f, 2f, 0f), new Vector3(0.2f, 4f, 12f), matWalls);
        // Right wall (X+)
        Box("Wall_Right", root.transform, new Vector3( 6f, 2f, 0f), new Vector3(0.2f, 4f, 12f), matWalls);

        // ── Bar Counter (spans back) ─────────────────────────────────────────
        // Main counter top (elevated platform)
        Box("Bar_CounterTop", root.transform,
            new Vector3(0f, 1.1f, 4.5f), new Vector3(6f, 0.15f, 0.8f), matBar);
        // Counter body
        Box("Bar_CounterBody", root.transform,
            new Vector3(0f, 0.55f, 4.5f), new Vector3(6f, 1.1f, 0.6f), matBar);

        // Neon cyan strip along bar front edge
        Box("Bar_NeonStrip", root.transform,
            new Vector3(0f, 1.04f, 4.1f), new Vector3(6f, 0.04f, 0.04f), matNeon);

        // Amber glow underbar
        Box("Bar_AmberUnder", root.transform,
            new Vector3(0f, 0.08f, 4.2f), new Vector3(5.8f, 0.04f, 0.04f), matAmberNeon);

        // Back shelf / bottle rack outline
        Box("Bar_Shelf_Back", root.transform,
            new Vector3(0f, 2.2f, 5.7f), new Vector3(5f, 0.08f, 0.3f), matBar);
        Box("Bar_Shelf_Mid",  root.transform,
            new Vector3(0f, 1.6f, 5.7f), new Vector3(5f, 0.08f, 0.3f), matBar);

        // ── Bar Stools (3) ───────────────────────────────────────────────────
        float[] stoolX = { -2f, 0f, 2f };
        for (int i = 0; i < 3; i++)
        {
            GameObject stoolParent = new GameObject($"BarStool_{i + 1}");
            stoolParent.transform.SetParent(root.transform);
            stoolParent.transform.localPosition = new Vector3(stoolX[i], 0, 3f);

            // Seat (cylinder)
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seat.name = "Seat";
            seat.transform.SetParent(stoolParent.transform);
            seat.transform.localPosition = new Vector3(0, 0.75f, 0);
            seat.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
            seat.GetComponent<Renderer>().sharedMaterial = matStool;

            // Leg
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "Leg";
            leg.transform.SetParent(stoolParent.transform);
            leg.transform.localPosition = new Vector3(0, 0.35f, 0);
            leg.transform.localScale = new Vector3(0.07f, 0.35f, 0.07f);
            leg.GetComponent<Renderer>().sharedMaterial = matBar;

            // LoungeInteractable on stool root
            var interactable = stoolParent.AddComponent<LoungeInteractable>();
            SerializedObject so = new SerializedObject(interactable);
            so.FindProperty("type").enumValueIndex = 0; // BarStool
            so.FindProperty("interactLabel").stringValue = "Sit here";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Tables (2 background tables) ─────────────────────────────────────
        CreateTable("Table_Left",  root.transform, new Vector3(-3.5f, 0, 0.5f), matTable);
        CreateTable("Table_Right", root.transform, new Vector3( 3.5f, 0, 0.5f), matTable);

        // ── DRIFT Placeholder ────────────────────────────────────────────────
        GameObject driftRoot = new GameObject("DRIFT_Character");
        driftRoot.transform.SetParent(root.transform);
        driftRoot.transform.localPosition = new Vector3(0f, 0f, 5.1f);

        GameObject driftBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        driftBody.name = "Body";
        driftBody.transform.SetParent(driftRoot.transform);
        driftBody.transform.localPosition = new Vector3(0, 1f, 0);
        driftBody.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
        driftBody.GetComponent<Renderer>().sharedMaterial = matDrift;
        // DriftController added here — animator will be null until model is ready
        driftRoot.AddComponent<DriftController>();

        // ── Ambient Point Lights ─────────────────────────────────────────────
        // Warm amber fill over the bar
        CreatePointLight("Light_BarAmber", root.transform,
            new Vector3(0, 3.5f, 4.5f), new Color(1f, 0.65f, 0.1f), 3f, 8f);
        // Subtle cool fill on seating area
        CreatePointLight("Light_SeatingFill", root.transform,
            new Vector3(0, 3.2f, 0f), new Color(0.05f, 0.05f, 0.15f), 1f, 10f);
        // Left and right rim lights
        CreatePointLight("Light_RimLeft",  root.transform,
            new Vector3(-5f, 2f, 2f), new Color(0f, 0.82f, 1f), 0.8f, 6f);
        CreatePointLight("Light_RimRight", root.transform,
            new Vector3( 5f, 2f, 2f), new Color(0f, 0.82f, 1f), 0.8f, 6f);

        // ── Main Camera position ─────────────────────────────────────────────
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 1.7f, -1f);
            mainCam.transform.rotation = Quaternion.Euler(5f, 0f, 0f); // slight downward tilt
            mainCam.backgroundColor = HexColor("0A0A0F");
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            // Add LoungeInputManager
            if (mainCam.GetComponent<LoungeInputManager>() == null)
                mainCam.gameObject.AddComponent<LoungeInputManager>();
        }

        // ── Dim the Directional Light (ambient) ──────────────────────────────
        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.intensity = 0.15f;
            dirLight.color = new Color(0.1f, 0.08f, 0.2f); // purple-dark ambient
        }

        // ── LoungeManager root object ────────────────────────────────────────
        GameObject loungeManagerObj = new GameObject("LoungeManager");
        loungeManagerObj.AddComponent<LoungeManager>();

        // Ambient audio source (clip assigned later via Inspector)
        AudioSource ambientAudio = loungeManagerObj.AddComponent<AudioSource>();
        ambientAudio.playOnAwake = false;
        ambientAudio.loop = true;
        ambientAudio.spatialBlend = 0f; // 2D — fills the whole space
        ambientAudio.volume = 0f;       // fades in with scene

        // Wire ambientAudio → LoungeManager.ambientAudio via SerializedObject
        var mgr = loungeManagerObj.GetComponent<LoungeManager>();
        var mgrSO = new UnityEditor.SerializedObject(mgr);
        mgrSO.FindProperty("ambientAudio").objectReferenceValue = ambientAudio;
        mgrSO.ApplyModifiedPropertiesWithoutUndo();

        // Register undo for the entire build
        Undo.RegisterCreatedObjectUndo(root, "Build Lounge Scene");
        Undo.RegisterCreatedObjectUndo(loungeManagerObj, "Build Lounge Scene");

        // Save materials
        SaveMaterial(matWalls);  SaveMaterial(matBar);   SaveMaterial(matStool);
        SaveMaterial(matNeon);   SaveMaterial(matTable);  SaveMaterial(matDrift);
        SaveMaterial(matFloor);  SaveMaterial(matAmberNeon);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[LoungeBuilder] Scene_Lounge geometry built. 0 errors.");
        EditorUtility.DisplayDialog("Done", "Scene_Lounge geometry built!\n\nNext: assign LoungeManager references in Inspector.", "OK");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void CreateTable(string name, Transform parent, Vector3 pos, Material mat)
    {
        GameObject tableRoot = new GameObject(name);
        tableRoot.transform.SetParent(parent);
        tableRoot.transform.localPosition = pos;

        // Tabletop
        var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        top.name = "Top";
        top.transform.SetParent(tableRoot.transform);
        top.transform.localPosition = new Vector3(0, 0.75f, 0);
        top.transform.localScale = new Vector3(0.9f, 0.04f, 0.9f);
        top.GetComponent<Renderer>().sharedMaterial = mat;

        // Pedestal leg
        var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leg.name = "Leg";
        leg.transform.SetParent(tableRoot.transform);
        leg.transform.localPosition = new Vector3(0, 0.35f, 0);
        leg.transform.localScale = new Vector3(0.1f, 0.35f, 0.1f);
        leg.GetComponent<Renderer>().sharedMaterial = mat;

        // Table is interactable (sit/chat)
        var interactable = tableRoot.AddComponent<LoungeInteractable>();
        SerializedObject so = new SerializedObject(interactable);
        so.FindProperty("type").enumValueIndex = 1; // Table
        so.FindProperty("interactLabel").stringValue = "Join table";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreatePointLight(string name, Transform parent, Vector3 pos,
                                  Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    static Material URP_Lit(string name, Color color, float smoothness)
    {
        // Use the URP Lit shader
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard"); // fallback
        var mat = new Material(lit) { name = name };
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);
        return mat;
    }

    static Material URP_Emissive(string name, Color color, float emissiveIntensity)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        var mat = new Material(lit) { name = name };
        mat.color = Color.black;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * emissiveIntensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    static void SaveMaterial(Material mat)
    {
        string path = $"Assets/Materials/Lounge/{mat.name}.mat";
        System.IO.Directory.CreateDirectory("Assets/Materials/Lounge");
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            AssetDatabase.CreateAsset(mat, path);
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}
