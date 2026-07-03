using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// AUDIOVIDO — Lounge Inspector Wirer
/// Menu: AUDIOVIDO → Wire Lounge References
/// Auto-assigns all serialized fields on LoungeManager and LoungeUIController
/// by searching the scene hierarchy by name.
/// Run after both Build Lounge Scene and Build Lounge UI have been executed.
/// </summary>
public static class LoungeWirer
{
    [MenuItem("AUDIOVIDO/Wire Lounge References")]
    public static void WireReferences()
    {
        int wired = 0;

        // ── LoungeUIController ───────────────────────────────────────────────
        LoungeUIController uiCtrl = Object.FindFirstObjectByType<LoungeUIController>();
        if (uiCtrl != null)
        {
            SerializedObject so = new SerializedObject(uiCtrl);

            // Top bar
            SetButton(so, "exitButton",    Find<Button>("Btn_Exit"));
            SetTMP(so,    "titleText",     Find<TMPro.TextMeshProUGUI>("Txt_Title"));
            SetTMP(so,    "userCountText", Find<TMPro.TextMeshProUGUI>("Txt_UserCount"));
            SetTMP(so,    "nxtBalanceText",Find<TMPro.TextMeshProUGUI>("Txt_NxtBalance"));

            // Bottom bar
            SetTMP(so,    "nowPlayingText", Find<TMPro.TextMeshProUGUI>("Txt_NowPlaying"));
            SetButton(so, "queueButton",   Find<Button>("Btn_Queue"));
            SetButton(so, "chatButton",    Find<Button>("Btn_Chat"));
            SetButton(so, "inviteButton",  Find<Button>("Btn_Invite"));
            SetButton(so, "sitHereButton", Find<Button>("Btn_SitHere"));

            // DRIFT bubble (inactive at rest — must use FindGO, not GameObject.Find)
            var driftBubble = FindGO("DriftBubble");
            if (driftBubble != null)
            {
                so.FindProperty("driftBubbleRoot").objectReferenceValue =
                    driftBubble.GetComponent<RectTransform>();
                wired++;
            }
            SetTMP(so, "driftBubbleText", Find<TMPro.TextMeshProUGUI>("Txt_DriftMessage"));

            // Mood panel (inactive at rest)
            var moodPanel = FindGO("MoodPanel");
            if (moodPanel != null)
            {
                so.FindProperty("moodPanelRoot").objectReferenceValue =
                    moodPanel.GetComponent<RectTransform>();
                wired++;
            }
            SetButton(so, "btnMelancholic", Find<Button>("Btn_Melancholic"));
            SetButton(so, "btnEnergetic",   Find<Button>("Btn_Energetic"));
            SetButton(so, "btnNostalgic",   Find<Button>("Btn_Nostalgic"));
            SetButton(so, "btnChill",       Find<Button>("Btn_Chill"));

            // Track card (inactive at rest)
            var trackCard = FindGO("TrackCard");
            if (trackCard != null)
            {
                so.FindProperty("trackCardRoot").objectReferenceValue =
                    trackCard.GetComponent<RectTransform>();
                wired++;
            }
            SetTMP(so, "cardTrackText",  Find<TMPro.TextMeshProUGUI>("Txt_CardTrack"));
            SetTMP(so, "cardArtistText", Find<TMPro.TextMeshProUGUI>("Txt_CardArtist"));

            so.ApplyModifiedProperties();
            Debug.Log("[LoungeWirer] LoungeUIController wired.");
            wired++;
        }
        else Debug.LogWarning("[LoungeWirer] LoungeUIController not found.");

        // ── LoungeManager ────────────────────────────────────────────────────
        LoungeManager mgr = Object.FindFirstObjectByType<LoungeManager>();
        if (mgr != null)
        {
            SerializedObject so = new SerializedObject(mgr);

            // drift
            DriftController drift = Object.FindFirstObjectByType<DriftController>();
            if (drift != null)
            {
                so.FindProperty("drift").objectReferenceValue = drift;
                wired++;
            }

            // ui
            if (uiCtrl != null)
            {
                so.FindProperty("ui").objectReferenceValue = uiCtrl;
                wired++;
            }

            // fadeCanvas (CanvasGroup on FadeCanvas)
            var fadeObj = FindGO("FadeCanvas");
            if (fadeObj != null)
            {
                var cg = fadeObj.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    so.FindProperty("fadeCanvas").objectReferenceValue = cg;
                    wired++;
                }
            }

            // ambientAudio — add AudioSource if missing, then wire it
            AudioSource ambientAudio = mgr.GetComponent<AudioSource>();
            if (ambientAudio == null)
            {
                ambientAudio = mgr.gameObject.AddComponent<AudioSource>();
                ambientAudio.playOnAwake = false;
                ambientAudio.loop       = true;
                ambientAudio.spatialBlend = 0f;
                ambientAudio.volume     = 0f;
                Debug.Log("[LoungeWirer] AudioSource added to LoungeManager.");
            }
            so.FindProperty("ambientAudio").objectReferenceValue = ambientAudio;
            wired++;

            // ambientLights — collect all Point lights
            Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var pointLights = System.Array.FindAll(allLights,
                l => l.type == LightType.Point);
            SerializedProperty lightsProp = so.FindProperty("ambientLights");
            lightsProp.arraySize = pointLights.Length;
            for (int i = 0; i < pointLights.Length; i++)
                lightsProp.GetArrayElementAtIndex(i).objectReferenceValue = pointLights[i];
            wired++;

            so.ApplyModifiedProperties();
            Debug.Log("[LoungeWirer] LoungeManager wired.");
        }
        else Debug.LogWarning("[LoungeWirer] LoungeManager not found.");

        // ── DriftController ──────────────────────────────────────────────────
        DriftController driftCtrl = Object.FindFirstObjectByType<DriftController>();
        if (driftCtrl != null)
        {
            SerializedObject so = new SerializedObject(driftCtrl);

            // ── Animator component on DRIFT_Character ────────────────────────
            GameObject driftCharGO = GameObject.Find("DRIFT_Character");
            if (driftCharGO != null)
            {
                Animator anim = driftCharGO.GetComponent<Animator>();
                if (anim == null) anim = driftCharGO.AddComponent<Animator>();

                // Load and assign the animator controller asset
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Animations/DRIFT_Controller.controller");
                if (controller != null)
                {
                    anim.runtimeAnimatorController = controller;
                    Debug.Log("[LoungeWirer] DRIFT_Controller.controller assigned to Animator.");
                    wired++;
                }
                else
                {
                    Debug.LogWarning("[LoungeWirer] DRIFT_Controller.controller not found. " +
                                     "Run AUDIOVIDO → Build DRIFT Animator first.");
                }

                // Wire Animator into DriftController's serialized field
                so.FindProperty("animator").objectReferenceValue = anim;
                wired++;
            }

            // playerCamera = Main Camera transform
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                so.FindProperty("playerCamera").objectReferenceValue = mainCam.transform;
                wired++;
            }

            // barPosition — create an empty at DRIFT's feet if not set
            var barPosObj = GameObject.Find("DRIFT_BarPosition");
            if (barPosObj == null)
            {
                barPosObj = new GameObject("DRIFT_BarPosition");
                // Place at DRIFT's current position
                if (driftCharGO != null)
                    barPosObj.transform.position = driftCharGO.transform.position;
                else
                    barPosObj.transform.position = new Vector3(0, 0, 5.1f);
                barPosObj.transform.rotation = Quaternion.Euler(0, 180f, 0); // faces player
            }
            so.FindProperty("barPosition").objectReferenceValue = barPosObj.transform;

            // approachPosition — leaning forward toward player
            var approachPosObj = GameObject.Find("DRIFT_ApproachPosition");
            if (approachPosObj == null)
            {
                approachPosObj = new GameObject("DRIFT_ApproachPosition");
                approachPosObj.transform.position = new Vector3(0, 0, 4.3f); // front of bar
                approachPosObj.transform.rotation = Quaternion.Euler(0, 180f, 0);
            }
            so.FindProperty("approachPosition").objectReferenceValue = approachPosObj.transform;

            so.ApplyModifiedProperties();
            Debug.Log("[LoungeWirer] DriftController wired.");
            wired++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Done",
            $"Wired {wired} references.\n\nScene_Lounge is ready to play!", "OK");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Find a component by GameObject name, including inactive objects.</summary>
    static T Find<T>(string name) where T : Component
    {
        // First try active-only (fast path)
        var go = GameObject.Find(name);
        if (go != null) return go.GetComponent<T>();

        // Search inactive objects too (DriftBubble, MoodPanel, TrackCard are inactive at rest)
        T[] all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all)
            if (c.gameObject.name == name) return c;

        Debug.LogWarning($"[LoungeWirer] '{name}' not found.");
        return null;
    }

    /// <summary>Find a GameObject by name, including inactive objects.</summary>
    static GameObject FindGO(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) return go;

        // Search through all transforms to find inactive objects
        var all = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in all)
            if (t.gameObject.name == name) return t.gameObject;

        return null;
    }

    static void SetButton(SerializedObject so, string field, Button btn)
    {
        if (btn != null) so.FindProperty(field).objectReferenceValue = btn;
    }

    static void SetTMP(SerializedObject so, string field, TMPro.TextMeshProUGUI tmp)
    {
        if (tmp != null) so.FindProperty(field).objectReferenceValue = tmp;
    }
}
