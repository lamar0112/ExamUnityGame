#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bygger spillbare gråboks-scener (primitiver + komponenter + UI-kobling).
/// Kjør fra Unity: Exam → Greybox → …
/// </summary>
public static class ExamGreyboxSetup
{
    const string ScenesDir = "Assets/_Project/Scenes";
    const string RobotKylePrefabPath = "Assets/UnityTechnologies/SpaceRobotKyle/Prefabs/RobotKyle.prefab";
    const string SkyboxMaterialPath = "Assets/AllSkyFree/Cartoon Base BlueSky/Day_BlueSky_Nothing.mat";
    const string RobotSfxDir = "Assets/UnityTechnologies/SpaceRobotKyle/Sfx/";
    const string BgmTrack1Path = "Assets/Peaceful Piano - Free Loop Sample Pack/Track 1 (Loop).wav";
    const string BgmTrack2Path = "Assets/Peaceful Piano - Free Loop Sample Pack/Track 2 (Loop).wav";
    const string KenneyNatureFbx = "Assets/ThirdParty/Kenney/NatureKit/Models/FBX format/";
    const string TerminalOutbreakPolyRoot =
        "Assets/_Project/ImportedFromTerminalOutbreak/SimplePoly City - Low Poly Assets/Prefab/";

    [MenuItem("Exam/Greybox/0 — Ensure scenes exist (kjør hvis mangler)")]
    public static void EnsureScenes()
    {
        ExamProjectSetup.CreateScenesAndBuildSettings();
    }

    [MenuItem("Exam/Greybox/1 — MainMenu (meny + GameManager + AudioManager)")]
    public static void BuildMainMenu()
    {
        if (!SceneFileExists("MainMenu")) return;
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.OpenScene($"{ScenesDir}/MainMenu.unity");
        RemoveExamGreyboxRoot();
        DestroyRootObjectIfExists("Main Camera");
        DestroyRootObjectIfExists("Directional Light");

        var root = new GameObject("ExamGreybox");

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.85f;
        lightGO.transform.SetParent(root.transform);
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(root.transform);
        camGO.transform.position = new Vector3(0f, 2f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.15f);
        camGO.AddComponent<AudioListener>();
        ApplySceneSkybox(cam);

        var gm = new GameObject("GameManager");
        gm.transform.SetParent(null);
        gm.AddComponent<GameManager>();
        var am = new GameObject("AudioManager");
        am.transform.SetParent(null);
        am.AddComponent<AudioManager>();
        WireDefaultAudioClips(am.GetComponent<AudioManager>());

        var canvas = new GameObject("MainMenu_Canvas");
        canvas.transform.SetParent(root.transform);
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<GraphicRaycaster>();
        canvas.AddComponent<TmpDefaultFontOnAwake>();
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.transform.SetParent(null);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var bg = new GameObject("Background");
        bg.transform.SetParent(canvas.transform, false);
        bg.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.14f);
        StretchFull(bg);

        MakeTMPText(canvas, "TitleText", "EXAM PLATFORMER",
            new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(800, 100), 52);
        MakeTMPText(canvas, "SubText", "Robot Kyle + AllSky — erstatt primitiver med Kenney der du vil",
            new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(900, 40), 18);

        var mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvas.transform, false);
        var pr = mainPanel.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(340, 300);
        pr.anchoredPosition = new Vector2(0, -40);

        MakeButton(mainPanel, "StartBtn", "START", new Vector2(0, 90));
        MakeButton(mainPanel, "ControlsBtn", "CONTROLS", new Vector2(0, 20));
        MakeButton(mainPanel, "QuitBtn", "QUIT", new Vector2(0, -50));

        var ctrlPanel = new GameObject("ControlsPanel");
        ctrlPanel.transform.SetParent(canvas.transform, false);
        ctrlPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
        StretchFull(ctrlPanel);
        MakeTMPText(ctrlPanel, "CtrlTitle", "CONTROLS",
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(400, 60), 32);
        MakeTMPText(ctrlPanel, "CtrlList",
            "WASD — Move\nMouse — Look\nSpace — Jump\nLeft Shift — Sprint\nEscape — Pause",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480, 240), 20);
        MakeButton(ctrlPanel, "BackBtn", "BACK", new Vector2(0, -160));

        var menuScript = canvas.AddComponent<MainMenu>();
        var soMenu = new SerializedObject(menuScript);
        soMenu.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        soMenu.FindProperty("controlsPanel").objectReferenceValue = ctrlPanel;
        soMenu.ApplyModifiedProperties();

        // GameObject.Find finner ikke objekter under inactive parents — koble før panel skjules.
        WireMainMenuButtons(menuScript);
        ctrlPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Exam Greybox] MainMenu lagret. Test: Play fra MainMenu.");
    }

    [MenuItem("Exam/Greybox/2 — Level01 (hopp-bane)")]
    public static void BuildLevel01()
    {
        if (!SceneFileExists("Level01")) { Debug.LogError("[Exam Greybox] Mangler Level01.unity — kjør Exam/Setup eller Greybox/0."); return; }
        BuildLevelPlatformer($"{ScenesDir}/Level01.unity", "Level02", -2f, 38f);
    }

    [MenuItem("Exam/Greybox/3 — Level02 (bred bane)")]
    public static void BuildLevel02()
    {
        if (!SceneFileExists("Level02")) { Debug.LogError("[Exam Greybox] Mangler Level02.unity."); return; }
        BuildLevelWide($"{ScenesDir}/Level02.unity", "Level03", -3f, 45f);
    }

    [MenuItem("Exam/Greybox/4 — Level03 (steg opp)")]
    public static void BuildLevel03()
    {
        if (!SceneFileExists("Level03")) { Debug.LogError("[Exam Greybox] Mangler Level03.unity."); return; }
        BuildLevelSteps($"{ScenesDir}/Level03.unity", "MainMenu", -2f, 32f);
    }

    [MenuItem("Exam/Greybox/ALL — MainMenu + Level01 + Level02 + Level03")]
    public static void BuildAll()
    {
        EnsureScenes();
        BuildMainMenu();
        BuildLevel01();
        BuildLevel02();
        BuildLevel03();
        Debug.Log("[Exam Greybox] Ferdig: alle fire scener med Robot Kyle, AllSky og standard lyd. Bytt ut geometri med Kenney etter behov.");
    }

    // -------------------------------------------------------------------------

    static bool SceneFileExists(string name) =>
        File.Exists($"{ScenesDir}/{name}.unity");

    static void RemoveExamGreyboxRoot()
    {
        var old = GameObject.Find("ExamGreybox");
        if (old != null)
            Object.DestroyImmediate(old);
    }

    static void DestroyRootObjectIfExists(string objectName)
    {
        var go = GameObject.Find(objectName);
        if (go != null && go.transform.parent == null)
            Object.DestroyImmediate(go);
    }

    static void BuildLevelPlatformer(string scenePath, string nextScene, float startZ, float portalZ)
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.OpenScene(scenePath);
        RemoveExamGreyboxRoot();

        var root = new GameObject("ExamGreybox");
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(10f, 1f, 10f);

        bool level01Kenney = scenePath.Contains("Level01");
        if (level01Kenney)
            StyleUnityGroundForLevel01(ground);

        var platformDefs = new[]
        {
            (new Vector3(0f, 1.5f, 8f), new Vector3(4f, 0.5f, 4f)),
            (new Vector3(-3f, 3f, 16f), new Vector3(3f, 0.5f, 3f)),
            (new Vector3(4f, 4.5f, 22f), new Vector3(3f, 0.5f, 3f)),
        };
        if (level01Kenney)
            AddPlatformsKenneyOrFallback(root, platformDefs);
        else
            AddPlatforms(root, platformDefs);

        if (level01Kenney)
            ScatterTerminalOutbreakSceneryLevel01(root);

        var player = CreatePlayer(root, new Vector3(0f, 1f, startZ));
        SetupCameraAndSystems(root, player, nextScene, portalZ, level01Kenney);
        BuildLevelUI(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (level01Kenney)
            Debug.Log(
                $"[Exam Greybox] {scenePath} lagret — Kenney NatureKit + TerminalOutbreak/SimplePoly-dekor + URP-tint.");
        else
            Debug.Log($"[Exam Greybox] {scenePath} lagret.");
    }

    static void BuildLevelWide(string scenePath, string nextScene, float startZ, float portalZ)
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.OpenScene(scenePath);
        RemoveExamGreyboxRoot();

        var root = new GameObject("ExamGreybox");
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(14f, 1f, 14f);

        for (int i = 0; i < 8; i++)
        {
            var border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = $"Border_{i}";
            border.transform.SetParent(root.transform);
            float x = (i % 2 == 0) ? -22f : 22f;
            float z = -15f + i * 4f;
            if (i >= 4)
            {
                x = -18f + (i - 4) * 12f;
                z = (i % 2 == 0) ? -20f : 20f;
            }
            border.transform.position = new Vector3(x, 0.5f, z);
            border.transform.localScale = new Vector3(2f, 1f, 2f);
        }

        AddPlatforms(root, new[]
        {
            (new Vector3(0f, 0.6f, 10f), new Vector3(8f, 0.4f, 3f)),
            (new Vector3(0f, 0.6f, 22f), new Vector3(8f, 0.4f, 3f)),
        });

        var player = CreatePlayer(root, new Vector3(0f, 1f, startZ));
        SetupCameraAndSystems(root, player, nextScene, portalZ);
        BuildLevelUI(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Exam Greybox] {scenePath} lagret.");
    }

    static void BuildLevelSteps(string scenePath, string nextScene, float startZ, float portalZ)
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.OpenScene(scenePath);
        RemoveExamGreyboxRoot();

        var root = new GameObject("ExamGreybox");
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(8f, 1f, 8f);

        for (int i = 0; i < 6; i++)
        {
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"Step_{i}";
            step.transform.SetParent(root.transform);
            step.transform.position = new Vector3(0f, 0.5f + i * 1.2f, 5f + i * 3f);
            step.transform.localScale = new Vector3(5f - i * 0.4f, 0.5f, 2.5f);
        }

        var player = CreatePlayer(root, new Vector3(0f, 1f, startZ));
        SetupCameraAndSystems(root, player, nextScene, portalZ + 10f);
        BuildLevelUI(root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Exam Greybox] {scenePath} lagret.");
    }

    static void AddPlatforms(GameObject root, (Vector3 pos, Vector3 scale)[] defs)
    {
        for (int i = 0; i < defs.Length; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = $"Platform_{i}";
            p.transform.SetParent(root.transform);
            p.transform.position = defs[i].pos;
            p.transform.localScale = defs[i].scale;
        }
    }

    static GameObject CreatePlayer(GameObject root, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RobotKylePrefabPath);
        if (prefab != null)
        {
            var player = PrefabUtility.InstantiatePrefab(prefab, root.transform) as GameObject;
            if (player == null)
                player = Object.Instantiate(prefab, root.transform);
            player.name = "Player";
            player.tag = "Player";
            var spawn = new Vector3(position.x, 0.08f, position.z);
            player.transform.localPosition = spawn;
            player.transform.localRotation = Quaternion.identity;

            var strip = new List<MonoBehaviour>();
            foreach (var mb in player.GetComponents<MonoBehaviour>())
                strip.Add(mb);
            foreach (var mb in strip)
                Object.DestroyImmediate(mb);

            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerHealth>();
            var respawn = player.AddComponent<PlayerRespawn>();
            var soR = new SerializedObject(respawn);
            soR.FindProperty("defaultSpawnPoint").vector3Value = spawn + Vector3.up * 0.15f;
            soR.ApplyModifiedProperties();
            return player;
        }

        Debug.LogWarning("[Exam Greybox] Fant ikke Robot Kyle — bruker kapsel. Sjekk at prefab ligger i " + RobotKylePrefabPath);
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Player";
        capsule.tag = "Player";
        capsule.transform.SetParent(root.transform);
        capsule.transform.position = position;
        Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());

        var cc = capsule.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.45f;
        cc.center = new Vector3(0f, 0f, 0f);

        capsule.AddComponent<PlayerController>();
        capsule.AddComponent<PlayerHealth>();
        var respawnC = capsule.AddComponent<PlayerRespawn>();
        var soRc = new SerializedObject(respawnC);
        soRc.FindProperty("defaultSpawnPoint").vector3Value = position + Vector3.up * 0.5f;
        soRc.ApplyModifiedProperties();

        return capsule;
    }

    static void ApplySceneSkybox(Camera cam)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        if (mat != null)
        {
            RenderSettings.skybox = mat;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();
        }
        if (cam != null)
            cam.clearFlags = CameraClearFlags.Skybox;
    }

    static void WireDefaultAudioClips(AudioManager audio)
    {
        if (audio == null) return;
        var so = new SerializedObject(audio);
        void SetClip(string prop, string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            SerializedProperty p = so.FindProperty(prop);
            if (p != null)
                p.objectReferenceValue = clip;
        }
        SetClip("jumpClip", RobotSfxDir + "Player_Footstep_02.wav");
        SetClip("collectOrbClip", RobotSfxDir + "Player_Footstep_05.wav");
        SetClip("damageClip", RobotSfxDir + "Player_Land.wav");
        SetClip("powerupClip", RobotSfxDir + "Player_Footstep_08.wav");
        SetClip("checkpointClip", RobotSfxDir + "Player_Footstep_04.wav");
        SetClip("levelCompleteClip", BgmTrack2Path);
        SetClip("enemyDeathClip", RobotSfxDir + "Player_Land.wav");
        SetClip("menuClickClip", RobotSfxDir + "Player_Footstep_01.wav");
        SetClip("portalClip", RobotSfxDir + "Player_Footstep_09.wav");
        SetClip("backgroundMusic", BgmTrack1Path);
        so.ApplyModifiedProperties();
    }

    static void StyleUnityGroundForLevel01(GameObject ground)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return;
        var mat = new Material(lit);
        mat.SetColor("_BaseColor", new Color(0.26f, 0.4f, 0.22f));
        var mr = ground.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = mat;
    }

    static Bounds WorldRenderBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            b.Encapsulate(rends[i].bounds);
        return b;
    }

    static void FitKenneyModelToWorldBox(GameObject inst, Vector3 worldCenter, Vector3 boxSize)
    {
        inst.transform.rotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        var b = WorldRenderBounds(inst);
        var sz = b.size;
        if (sz.x < 0.01f) sz.x = 1f;
        if (sz.y < 0.01f) sz.y = 1f;
        if (sz.z < 0.01f) sz.z = 1f;
        inst.transform.localScale = new Vector3(boxSize.x / sz.x, boxSize.y / sz.y, boxSize.z / sz.z);
        var b2 = WorldRenderBounds(inst);
        inst.transform.position += worldCenter - b2.center;
    }

    static void AddStaticMeshColliders(GameObject inst)
    {
        foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var go = mf.gameObject;
            if (go.GetComponent<MeshCollider>() != null) continue;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }
    }

    static void AddPlatformsKenneyOrFallback(GameObject root, (Vector3 pos, Vector3 scale)[] defs)
    {
        string[] models =
        {
            KenneyNatureFbx + "platform_stone.fbx",
            KenneyNatureFbx + "cliff_blockQuarter_stone.fbx",
            KenneyNatureFbx + "platform_stone.fbx"
        };
        for (int i = 0; i < defs.Length; i++)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(models[i % models.Length]);
            if (asset == null)
            {
                AddPlatforms(root, new[] { defs[i] });
                continue;
            }
            var inst = (GameObject)Object.Instantiate(asset, root.transform);
            inst.name = $"Platform_{i}";
            FitKenneyModelToWorldBox(inst, defs[i].pos, defs[i].scale);
            AddStaticMeshColliders(inst);
        }
    }

    static bool TrySpawnCollectibleKenney(Transform root, Vector3 pos, int index)
    {
        string[] flowers = { "flower_redA.fbx", "flower_purpleA.fbx", "flower_yellowA.fbx" };
        var path = KenneyNatureFbx + flowers[index % flowers.Length];
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return false;

        var holder = new GameObject($"Collectible_{index}");
        holder.transform.SetParent(root);
        holder.transform.position = pos;
        var sc = holder.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.48f;
        holder.AddComponent<Collectible>();

        var vis = (GameObject)Object.Instantiate(prefab, holder.transform);
        vis.name = "Visual";
        vis.transform.localPosition = Vector3.zero;
        vis.transform.localRotation = Quaternion.identity;
        vis.transform.localScale = Vector3.one * 1.6f;
        return true;
    }

    /// <summary>
    /// Dekorasjon fra TerminalOutbreak-prosjektet (SimplePoly City), kopiert til ImportedFromTerminalOutbreak/.
    /// </summary>
    static void ScatterTerminalOutbreakSceneryLevel01(GameObject root)
    {
        var defs = new (string rel, Vector3 pos, Vector3 euler, Vector3 scale)[]
        {
            (TerminalOutbreakPolyRoot + "Natures/Natures_Rock_small.prefab", new Vector3(-12f, 0f, 4f),
                new Vector3(0f, 30f, 0f), Vector3.one * 1.15f),
            (TerminalOutbreakPolyRoot + "Natures/Natures_Rock_Big.prefab", new Vector3(14f, 0f, 7f),
                new Vector3(0f, -40f, 0f), Vector3.one),
            (TerminalOutbreakPolyRoot + "Props/Props_Bench_1.prefab", new Vector3(-8f, 0f, 1f),
                new Vector3(0f, 22f, 0f), Vector3.one),
            (TerminalOutbreakPolyRoot + "Natures/Natures_Bush_01.prefab", new Vector3(10f, 0f, -5f),
                Vector3.zero, Vector3.one * 0.9f),
            (TerminalOutbreakPolyRoot + "Natures/Natures_Fir Tree.prefab", new Vector3(-20f, 0f, -10f),
                Vector3.zero, Vector3.one * 0.75f),
            (TerminalOutbreakPolyRoot + "Natures/Natures_Fir Tree.prefab", new Vector3(20f, 0f, 14f),
                new Vector3(0f, -35f, 0f), Vector3.one * 0.75f),
        };
        foreach (var d in defs)
            TryInstantiateTerminalOutbreakProp(d.rel, root.transform, d.pos, Quaternion.Euler(d.euler), d.scale);
    }

    static void TryInstantiateTerminalOutbreakProp(string assetPath, Transform parent, Vector3 pos,
        Quaternion rot, Vector3 scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return;
        var go = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (go == null)
            go = Object.Instantiate(prefab, parent);
        go.name = "TO_" + prefab.name;
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.transform.localScale = scale;
        SnapPrefabBottomToWorldY(go, 0f);
        AddStaticMeshColliders(go);
    }

    static void SnapPrefabBottomToWorldY(GameObject go, float worldY)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            b.Encapsulate(rends[i].bounds);
        float dy = worldY - b.min.y;
        if (Mathf.Abs(dy) > 0.001f)
            go.transform.position += new Vector3(0f, dy, 0f);
    }

    static void StylePortalForLevel01(GameObject portal)
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) return;
        var mat = new Material(lit);
        mat.SetColor("_BaseColor", new Color(0.42f, 0.18f, 0.62f));
        mat.SetFloat("_Metallic", 0.15f);
        mat.SetFloat("_Smoothness", 0.55f);
        var mr = portal.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sharedMaterial = mat;
    }

    static void SetupCameraAndSystems(GameObject root, GameObject player, string nextScene, float portalZ,
        bool level01KenneyArt = false)
    {
        var oldCam = GameObject.Find("Main Camera");
        if (oldCam != null)
            Object.DestroyImmediate(oldCam);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(root.transform);
        camGO.transform.position = player.transform.position + new Vector3(0f, 6f, -12f);
        var cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        ApplySceneSkybox(cam);
        var follow = camGO.AddComponent<CameraFollow>();
        var soF = new SerializedObject(follow);
        soF.FindProperty("target").objectReferenceValue = player.transform;
        soF.ApplyModifiedProperties();

        var oldLight = GameObject.Find("Directional Light");
        if (oldLight != null)
            Object.DestroyImmediate(oldLight);

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = level01KenneyArt ? 1f : 0.9f;
        light.color = level01KenneyArt ? new Color(1f, 0.94f, 0.86f) : Color.white;
        lightGO.transform.SetParent(root.transform);
        lightGO.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

        var timer = new GameObject("LevelTimer");
        timer.transform.SetParent(root.transform);
        timer.AddComponent<LevelTimer>();

        var portal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        portal.name = "FinishPortal";
        portal.transform.SetParent(root.transform);
        portal.transform.position = new Vector3(0f, 1.5f, portalZ);
        portal.transform.localScale = new Vector3(4f, 3f, 1.5f);
        portal.GetComponent<Collider>().isTrigger = true;
        var fp = portal.AddComponent<FinishPortal>();
        var soP = new SerializedObject(fp);
        soP.FindProperty("nextScene").stringValue = nextScene;
        soP.ApplyModifiedProperties();
        if (level01KenneyArt)
            StylePortalForLevel01(portal);

        for (int i = 0; i < 3; i++)
        {
            var p = new Vector3(-2f + i * 2f, 1.2f, 6f + i * 5f);
            if (level01KenneyArt && TrySpawnCollectibleKenney(root.transform, p, i))
                continue;

            var coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coin.name = $"Collectible_{i}";
            coin.transform.SetParent(root.transform);
            coin.transform.position = p;
            Object.DestroyImmediate(coin.GetComponent<Collider>());
            var sc = coin.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            coin.AddComponent<Collectible>();
        }
    }

    static void BuildLevelUI(GameObject root)
    {
        var canvas = new GameObject("GameCanvas");
        canvas.transform.SetParent(root.transform);
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<GraphicRaycaster>();
        canvas.AddComponent<TmpDefaultFontOnAwake>();

        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.transform.SetParent(null);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var scoreGo = MakeTMPText(canvas, "ScoreText", "Score: 0",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(320, 50), 28);
        var orbsGo = MakeTMPText(canvas, "OrbsText", "Orbs: 0",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -85f), new Vector2(320, 50), 28);
        var hpGo = MakeTMPText(canvas, "HealthText", "HP: 3/3",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -130f), new Vector2(320, 50), 28);
        var timeGo = MakeTMPText(canvas, "TimerText", "00:00",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-120f, -40f), new Vector2(200, 50), 28);

        var hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(canvas.transform, false);
        var hud = hudGo.AddComponent<HUD>();
        var soHud = new SerializedObject(hud);
        soHud.FindProperty("scoreText").objectReferenceValue = scoreGo.GetComponent<TextMeshProUGUI>();
        soHud.FindProperty("orbsText").objectReferenceValue = orbsGo.GetComponent<TextMeshProUGUI>();
        soHud.FindProperty("healthText").objectReferenceValue = hpGo.GetComponent<TextMeshProUGUI>();
        soHud.FindProperty("timerText").objectReferenceValue = timeGo.GetComponent<TextMeshProUGUI>();
        soHud.ApplyModifiedProperties();

        var pausePanel = new GameObject("PausePanel");
        pausePanel.transform.SetParent(canvas.transform, false);
        pausePanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        StretchFull(pausePanel);
        MakeTMPText(pausePanel, "PauseTitle", "PAUSED",
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(400, 60), 36);
        MakeButton(pausePanel, "PauseResumeBtn", "RESUME", new Vector2(0, 80));
        MakeButton(pausePanel, "PauseRestartBtn", "RESTART", new Vector2(0, 10));
        MakeButton(pausePanel, "PauseMenuBtn", "MAIN MENU", new Vector2(0, -60));
        MakeButton(pausePanel, "PauseQuitBtn", "QUIT", new Vector2(0, -130));

        var pmGo = new GameObject("PauseMenu");
        pmGo.transform.SetParent(canvas.transform, false);
        var pm = pmGo.AddComponent<PauseMenu>();
        var soPm = new SerializedObject(pm);
        soPm.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        soPm.ApplyModifiedProperties();

        WirePauseButtons(pm);
        pausePanel.SetActive(false);

        var lcPanel = new GameObject("LevelCompletePanel");
        lcPanel.transform.SetParent(canvas.transform, false);
        lcPanel.AddComponent<Image>().color = new Color(0.02f, 0.05f, 0.12f, 0.92f);
        StretchFull(lcPanel);
        var lcScore = MakeTMPText(lcPanel, "LcScore", "Score: 0",
            new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(500, 50), 28);
        var lcOrbs = MakeTMPText(lcPanel, "LcOrbs", "Orbs: 0",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 50), 28);
        var lcTime = MakeTMPText(lcPanel, "LcTime", "Time: 00:00",
            new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(500, 50), 28);
        MakeButton(lcPanel, "LcContinueBtn", "CONTINUE", new Vector2(0, -40));
        MakeButton(lcPanel, "LcRestartBtn", "RESTART", new Vector2(0, -110));
        MakeButton(lcPanel, "LcMenuBtn", "MAIN MENU", new Vector2(0, -180));

        var lcGo = new GameObject("LevelCompleteUI");
        lcGo.transform.SetParent(canvas.transform, false);
        var lc = lcGo.AddComponent<LevelCompleteUI>();
        var soLc = new SerializedObject(lc);
        soLc.FindProperty("panel").objectReferenceValue = lcPanel;
        soLc.FindProperty("scoreText").objectReferenceValue = lcScore.GetComponent<TextMeshProUGUI>();
        soLc.FindProperty("orbsText").objectReferenceValue = lcOrbs.GetComponent<TextMeshProUGUI>();
        soLc.FindProperty("timeText").objectReferenceValue = lcTime.GetComponent<TextMeshProUGUI>();
        soLc.ApplyModifiedProperties();

        WireLevelCompleteButtons(lc);
        lcPanel.SetActive(false);
    }

    static void WireMainMenuButtons(MainMenu mm)
    {
        WireByName("StartBtn", mm.OnStartGame);
        WireByName("ControlsBtn", mm.OnShowControls);
        WireByName("QuitBtn", mm.OnQuit);
        WireByName("BackBtn", mm.OnHideControls);
    }

    static void WirePauseButtons(PauseMenu pm)
    {
        WireByName("PauseResumeBtn", pm.OnResume);
        WireByName("PauseRestartBtn", pm.OnRestartLevel);
        WireByName("PauseMenuBtn", pm.OnMainMenu);
        WireByName("PauseQuitBtn", pm.OnQuit);
    }

    static void WireLevelCompleteButtons(LevelCompleteUI lc)
    {
        WireByName("LcContinueBtn", lc.OnContinue);
        WireByName("LcRestartBtn", lc.OnRestartLevel);
        WireByName("LcMenuBtn", lc.OnMainMenu);
    }

    static void WireByName(string objectName, UnityAction method)
    {
        var go = GameObject.Find(objectName);
        if (go == null)
        {
            Debug.LogWarning($"[Exam Greybox] Fant ikke knapp {objectName}");
            return;
        }
        var btn = go.GetComponent<Button>();
        if (btn == null) return;
        Undo.RecordObject(btn, "Wire button");
        btn.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(btn.onClick, method);
        EditorUtility.SetDirty(btn);
    }

    static void StretchFull(GameObject go)
    {
        // Image/TMP/Button legger allerede til RectTransform — ikke AddComponent på nytt.
        var r = go.GetComponent<RectTransform>();
        if (r == null)
            r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static GameObject MakeTMPText(GameObject parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Image>().color = new Color(0.14f, 0.48f, 0.88f);
        go.AddComponent<Button>();
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(280, 54);
        r.anchoredPosition = pos;

        var txt = new GameObject("Label");
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        var tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        return go;
    }
}
#endif
