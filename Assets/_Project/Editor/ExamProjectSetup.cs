#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Oppretter tomme scener og legger dem i Build Settings. Kjør fra Unity-menyen.
/// </summary>
public static class ExamProjectSetup
{
    const string ScenesDir = "Assets/_Project/Scenes";

    static readonly string[] SceneNames =
    {
        "MainMenu",
        "Level01",
        "Level02",
        "Level03"
    };

    [MenuItem("Exam/Setup — Create Scenes + Build Settings")]
    public static void CreateScenesAndBuildSettings()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project"))
            AssetDatabase.CreateFolder("Assets", "_Project");
        if (!AssetDatabase.IsValidFolder(ScenesDir))
            AssetDatabase.CreateFolder("Assets/_Project", "Scenes");

        var scenes = SceneNames.Select(name =>
        {
            string path = $"{ScenesDir}/{name}.unity";
            if (!File.Exists(path))
            {
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, path);
            }
            return new EditorBuildSettingsScene(path, true);
        }).ToArray();

        EditorBuildSettings.scenes = scenes;
        AssetDatabase.Refresh();
        Debug.Log("[Exam] Scener opprettet/oppdatert og lagt i File > Build Settings. Rekkefølge: MainMenu, Level01–Level03.");
    }

    [MenuItem("Exam/Open MainMenu Scene")]
    public static void OpenMainMenu()
    {
        string path = $"{ScenesDir}/MainMenu.unity";
        if (!File.Exists(path))
        {
            Debug.LogWarning("[Exam] Kjør først Exam/Setup — Create Scenes + Build Settings.");
            return;
        }
        EditorSceneManager.OpenScene(path);
    }

    [MenuItem("Exam/Remove Missing Scripts (åpen scene)")]
    public static void RemoveMissingScriptsInOpenScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int total = 0;
        foreach (var root in scene.GetRootGameObjects())
            total += RemoveMissingRecursive(root);
        if (total > 0)
            EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[Exam] Fjernet missing script på {total} komponent(er). Lagre scenen (Ctrl/Cmd+S).");
    }

    [MenuItem("Exam/Remove Missing Scripts (ALLE Build Settings-scener)")]
    public static void RemoveMissingScriptsInAllBuildScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string openPath = EditorSceneManager.GetActiveScene().path;
        int grandTotal = 0;

        foreach (var bs in EditorBuildSettings.scenes)
        {
            if (!bs.enabled || string.IsNullOrEmpty(bs.path) || !File.Exists(bs.path))
                continue;

            var scene = EditorSceneManager.OpenScene(bs.path);
            int sceneTotal = 0;
            foreach (var root in scene.GetRootGameObjects())
                sceneTotal += RemoveMissingRecursive(root);

            if (sceneTotal > 0)
            {
                EditorSceneManager.SaveScene(scene);
                grandTotal += sceneTotal;
                Debug.Log($"[Exam] {bs.path}: fjernet {sceneTotal} missing script(s).");
            }
        }

        if (!string.IsNullOrEmpty(openPath) && File.Exists(openPath))
            EditorSceneManager.OpenScene(openPath);

        Debug.Log($"[Exam] Ferdig: {grandTotal} missing script-komponent(er) fjernet totalt (lagrede scener).");
    }

    [MenuItem("Exam/Diagnostics — Log GameObjects med Missing Script (alle lastede scener)")]
    public static void LogMissingScriptHolders()
    {
        int found = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            foreach (var root in scene.GetRootGameObjects())
                found += LogMissingRecursive(root, scene.name);
        }
        if (found == 0)
            Debug.Log("[Exam] Ingen Missing Script funnet via GameObjectUtility i lastede scener.");
        else
            Debug.LogWarning($"[Exam] Fant {found} GameObject(er) med missing script — se liste over.");
    }

    /// <summary>Fanger missing script der GameObjectUtility rapporterer 0 (f.eks. enkelte editor/debug-objekter).</summary>
    [MenuItem("Exam/Diagnostics — Log NULL components (missing script) alle lastede scener")]
    public static void LogNullComponentsMissingScripts()
    {
        int nullSlots = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            foreach (var root in scene.GetRootGameObjects())
                nullSlots += LogNullComponentsRecursive(root, scene.name);
        }
        if (nullSlots == 0)
            Debug.Log("[Exam] Ingen null-component slots (missing script) funnet via GetComponents.");
        else
            Debug.LogWarning($"[Exam] Fant {nullSlots} missing script-slot(s) — se stier over. Fjern komponenten eller slett objektet (ofte '[Debug Updater]' fra editor-integrasjon).");
    }

    [MenuItem("Exam/Fix TMP — Default font på alle TextMeshProUGUI (aktiv scene)")]
    public static void AssignDefaultTmpFontInActiveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        int n = 0;
        foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tmp.gameObject.scene != scene) continue;
            if (tmp.font != null) continue;
            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogError("[Exam] TMP_Settings.defaultFontAsset er null. Importer TMP Essentials (Window → TextMeshPro).");
                return;
            }
            Undo.RecordObject(tmp, "TMP default font");
            tmp.font = TMP_Settings.defaultFontAsset;
            EditorUtility.SetDirty(tmp);
            n++;
        }
        if (n > 0)
            EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[Exam] Satte default TMP-font på {n} TextMeshProUGUI i '{scene.name}'. Lagre scenen.");
    }

    static int LogMissingRecursive(GameObject go, string sceneName)
    {
        int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (n > 0)
        {
            Debug.LogWarning($"[Exam] Missing script ({n}) på [{sceneName}] {GetHierarchyPath(go)}");
            n = 1;
        }
        else n = 0;
        foreach (Transform c in go.transform)
            n += LogMissingRecursive(c.gameObject, sceneName);
        return n;
    }

    static int LogNullComponentsRecursive(GameObject go, string sceneName)
    {
        var comps = go.GetComponents<Component>();
        int nullSlots = 0;
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] != null) continue;
            nullSlots++;
            Debug.LogWarning($"[Exam] Missing script (null slot #{i}) på [{sceneName}] {GetHierarchyPath(go)}");
        }
        foreach (Transform c in go.transform)
            nullSlots += LogNullComponentsRecursive(c.gameObject, sceneName);
        return nullSlots;
    }

    static string GetHierarchyPath(GameObject go)
    {
        if (go.transform.parent == null) return go.name;
        return GetHierarchyPath(go.transform.parent.gameObject) + "/" + go.name;
    }

    static int RemoveMissingRecursive(GameObject go)
    {
        int n = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform c in go.transform)
            n += RemoveMissingRecursive(c.gameObject);
        return n;
    }
}
#endif
