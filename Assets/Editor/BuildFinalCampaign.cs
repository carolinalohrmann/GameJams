using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class BuildFinalCampaign
{
    const string Scenes = "Assets/0_0Scenes/", Art = "Assets/Art/", Prefabs = "Assets/0_2prefafs/";
    static Sprite top, fill, tower, meteor, orb;
    static Sprite[] sky;
    static BossController necromancer, hunter;
    public static void RefreshHunterPickups(FinalCampaignStage stage)
    {
        stage.fixedPowerUps = new[] { PowerUp.Kind.Medkit, PowerUp.Kind.Shirt, PowerUp.Kind.Chicken, PowerUp.Kind.Apple, PowerUp.Kind.Citrus, PowerUp.Kind.Feather };
        float[] spots = { -8.8f, 5, -5, -6.5f, -7.6f, -2.5f };
        var template = UnityEngine.Object.FindAnyObjectByType<PowerUp>();
        for (int i = 0; i < stage.fixedPowerUps.Length; i++)
        {
            var kind = stage.fixedPowerUps[i];
            var pickup = UnityEngine.Object.FindObjectsByType<PowerUp>().FirstOrDefault(p => p.kind == kind);
            if (pickup == null) pickup = UnityEngine.Object.Instantiate(template, template.transform.parent);
            pickup.name = "PowerUp " + kind; pickup.kind = kind; pickup.pickupId = "Level8." + kind;
            pickup.durationSeconds = PowerUp.DefaultDuration(kind); pickup.icon.sprite = stage.powerUpIcons[(int)kind];
            pickup.transform.position = new Vector3(spots[i], kind == PowerUp.Kind.Feather ? -1.7f : -3, 0);
            foreach (var changed in new UnityEngine.Object[] { pickup, pickup.icon, pickup.transform }) PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
        }
    }
    public static void ApplyBalanceAndPreview()
    {
        foreach (string name in new[] { "NecromancerBoss", "HunterBoss" })
        {
            string path = Prefabs + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            int health = name == "HunterBoss" ? 666 : 301;
            root.GetComponent<BossController>().maximumHealth = health;
            root.GetComponent<Enemy>().health = health;
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
        }
        var scene = EditorSceneManager.OpenScene(Scenes + "Level8.unity");
        RefreshHunterPickups(UnityEngine.Object.FindAnyObjectByType<FinalCampaignStage>());
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        BuildTutorialCanvas.ApplyAll(); AssetDatabase.SaveAssets();
        Debug.Log("COMBAT_BALANCE_BUILD_OK");
        PreviewTutorialCanvas.Render();
    }
    [MenuItem("GameJams/Crear niveles finales 6, 7 y 8")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        PrepareArt(); BuildBosses();
        BuildStage(6); BuildStage(7); BuildStage(8);
        ConnectCampaign(); ApplyRequestedRefinements.Apply(); AssetDatabase.SaveAssets();
        Debug.Log("FINAL_CAMPAIGN_BUILD_OK: Level5 -> Level6, normal ending or Level7 -> Level8, native animated bosses, warned lethal attacks and fixed Hunter arena");
    }
    public static void BuildAndExit()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static SpriteRect Region(string name, Rect rect, Vector2 pivot)
    {
        return new SpriteRect { name = name, rect = rect, pivot = pivot, alignment = SpriteAlignment.Custom, spriteID = GUID.Generate() };
    }
    static Sprite[] Import(string path, IEnumerable<SpriteRect> regions, float ppu)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 4096; importer.SaveAndReimport();
        typeof(BuildGameLevels).GetMethod("SetSpriteRects", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { path, regions.ToArray() });
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }
    static Sprite Whole(string path, float ppu, bool bottom)
    {
        var texture = new Texture2D(2, 2); texture.LoadImage(System.IO.File.ReadAllBytes(path));
        int left = texture.width, right = 0, low = texture.height, high = 0;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
            if (texture.GetPixel(x, y).a > 0.1f) { left = Mathf.Min(left, x); right = Mathf.Max(right, x + 1); low = Mathf.Min(low, y); high = Mathf.Max(high, y + 1); }
        UnityEngine.Object.DestroyImmediate(texture);
        return Import(path, new[] { Region("Sprite", new Rect(left, low, right - left, high - low), new Vector2(0.5f, bottom ? 0 : 0.5f)) }, ppu).Single();
    }
    static void PrepareArt()
    {
        var tiles = Import(Art + "Final/Tiles.png", new[] {
            Region("Top", new Rect(144, 336, 48, 24), Vector2.one * 0.5f),
            Region("Stone", new Rect(36, 296, 24, 16), Vector2.one * 0.5f)
        }, 32);
        top = tiles.Single(s => s.name == "Top"); fill = tiles.Single(s => s.name == "Stone");
        tower = Whole(Art + "FinalProps/Torre.png", 256, true);
        meteor = Whole(Art + "FinalProps/meteorito.png", 512, false);
        sky = new[] { Whole(Art + "Final/Background_0.png", 32, true), Whole(Art + "Final/Background_1.png", 32, true),
            Whole(Art + "Final/Grass_background_1.png", 32, true), Whole(Art + "Final/Grass_background_2.png", 32, true) };
        Whole(Art + "Final/Salt.png", 64, true); Whole(Art + "Final/brush.png", 64, true);
    }
    static void BuildBosses()
    {
        const string necroPath = Art + "Bosses/Necromancer/Necromancer_creativekind-Sheet.png";
        string[] names = { "Idle", "Walk", "Attack", "Cast", "Ultimate", "Hit", "Death" };
        int[] counts = { 8, 8, 13, 13, 17, 5, 9 };
        var rects = new List<SpriteRect>();
        for (int row = 0; row < 7; row++)
        for (int col = 0; col < counts[row]; col++)
            rects.Add(Region(names[row] + "_" + col.ToString("D2"), new Rect(col * 160, (6 - row) * 128, 160, 128), new Vector2(0.5f, 13f / 128)));
        rects.Add(Region("Orb", new Rect(92, 814, 14, 22), Vector2.one * 0.5f));
        var frames = Import(necroPath, rects, 32); orb = frames.Single(s => s.name == "Orb");
        necromancer = BossPrefab("Necromancer", names, names.Select(n => frames.Where(s => s.name.StartsWith(n + "_")).ToArray()).ToArray(), false);
        names = new[] { "Idle", "Walk", "Attack", "Death" };
        string[] sources = { "Idle", "Caminar", "Disparar", "Muerte" }; counts = new[] { 4, 6, 4, 6 };
        var clips = new Sprite[4][];
        for (int i = 0; i < 4; i++)
        {
            int columns = i == 1 || i == 3 ? 3 : 2;
            int rows = Mathf.CeilToInt(counts[i] / (float)columns);
            var regions = Enumerable.Range(0, counts[i]).Select(n => Region("Frame_" + n.ToString("D2"),
                new Rect(n % columns * 256, (rows - 1 - n / columns) * 256, 256, 256), new Vector2(0.5f, 20f / 256))).ToList();
            var imported = Import(Art + "Bosses/Hunter/" + sources[i] + "_spritesheet.png", regions, 100);
            clips[i] = imported.Where(s => s.name.StartsWith("Frame_")).ToArray();
        }
        hunter = BossPrefab("Hunter", names, clips, true);
    }
    static BossController BossPrefab(string name, string[] states, Sprite[][] frames, bool isHunter)
    {
        string folder = "Assets/Animations/Bosses/" + name;
        System.IO.Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        string controllerPath = folder + "/" + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var entry in machine.states) machine.RemoveState(entry.state);
        for (int i = 0; i < states.Length; i++)
        {
            var clip = new AnimationClip { name = states[i], frameRate = 10 };
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
                frames[i].Select((s, n) => new ObjectReferenceKeyframe { time = n / 10f, value = s }).ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = i < 2; AnimationUtility.SetAnimationClipSettings(clip, settings);
            string clipPath = folder + "/" + states[i] + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (existing != null) { EditorUtility.CopySerialized(clip, existing); UnityEngine.Object.DestroyImmediate(clip); clip = existing; }
            else AssetDatabase.CreateAsset(clip, clipPath);
            var state = machine.AddState(states[i]); state.motion = clip;
            if (i == 0) machine.defaultState = state;
        }
        var root = new GameObject(name);
        var box = root.AddComponent<BoxCollider2D>(); box.size = new Vector2(isHunter ? 1.05f : 1, isHunter ? 2.05f : 2.12f);
        var body = root.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true;
        body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(root.transform, false);
        visual.sprite = frames[0][0]; visual.transform.localPosition = new Vector3(0, -box.size.y / 2, 0);
        visual.transform.localScale = Vector3.one * (isHunter ? 1 : 1.3f); visual.sortingOrder = 4;
        var animator = visual.gameObject.AddComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var enemy = root.AddComponent<Enemy>(); enemy.visual = visual; enemy.animator = animator;
        enemy.projectile = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "bullets.prefab").GetComponent<bullet>();
        enemy.health = isHunter ? 666 : 301; enemy.ranged = true; enemy.spriteFacesLeft = true;
        var boss = root.AddComponent<BossController>(); boss.hunter = isHunter; boss.maximumHealth = enemy.health;
        boss.projectileSprite = isHunter ? enemy.projectile.rapidFrames[0] : orb; boss.ritualSprite = orb;
        boss.summonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EnemyFantasySkeleton.prefab");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + name + "Boss.prefab"); UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<BossController>();
    }
    static void BuildStage(int number)
    {
        var original = EditorSceneManager.OpenScene(Scenes + "Level5.unity");
        string path = Scenes + "Level" + number + ".unity";
        EditorSceneManager.SaveScene(original, path, true);
        var scene = EditorSceneManager.OpenScene(path);
        var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>(); var layout = level.layout;
        level.stageNumber = number; level.manualCompletion = true; level.nextSceneName = "Tutorial";
        level.leftEdge = -11; level.rightEdge = number == 8 ? 30 : number == 7 ? 145 : 115; level.endX = level.rightEdge - 5;
        level.player.transform.position = new Vector3(-8, -3, 0);
        var stage = level.gameObject.AddComponent<FinalCampaignStage>(); stage.arenaLeft = level.rightEdge - (number == 8 ? 22 : 18);
        stage.arenaRight = level.rightEdge - 1; stage.bossPrefab = number == 8 ? hunter : necromancer;
        stage.towerSprite = tower; stage.meteorSprite = meteor; stage.portalSprite = layout.goalSprite; stage.powerUpIcons = layout.powerUpIcons;
        stage.towerFootOffset = AdjustPresentation.VisibleBottom(tower);
        stage.fixedPowerUps = Array.FindAll(stage.fixedPowerUps, kind => PowerUp.IsAvailableInStage(kind, number));
        if (number == 8) BuildFixedArena(level, layout, stage);
        else
        {
            layout.stageNumber = number; layout.bossArenaWidth = 22; layout.platformTop = top; layout.platformFill = fill;
            layout.minimumPlatformWidth = 5.4f; layout.maximumPlatformWidth = 7.3f;
            layout.minimumGap = 0.75f; layout.maximumGap = 1.25f; layout.bonusPlatformChance = 0.65f;
            layout.backgroundLayers = sky; layout.backgroundHeights = new[] { 15f, 15f, 6f, 6f }; layout.backgroundBaseY = -5.4f;
            layout.scenerySprites = new[] { AssetDatabase.LoadAllAssetsAtPath(Art + "Final/Salt.png").OfType<Sprite>().Single() };
            if (number == 7) ConfigureGlitch(layout);
        }
        var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>(); stage.ConfigureUI(ui);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
    static void ConfigureGlitch(RandomLevelLayout layout)
    {
        // Read the already imported native art without changing the shared atlases.
        var forest = AssetDatabase.LoadAllAssetsAtPath(Art + "TinyForest/Tileset(16x16)/Tileset.png").OfType<Sprite>().ToArray();
        var grassPath = Art + "Multi Platformer Tileset Free/GrassLand/Terrain/Grassland_Terrain_47Tiles.png";
        var multi = AssetDatabase.LoadAllAssetsAtPath(grassPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        var jungle = AssetDatabase.LoadAllAssetsAtPath(Art + "Jungle/Jungle platformer assets/Solid blocks/Grass/Grass.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
        layout.mixedPlatformTops = new[] { top, forest.Single(s => s.name == "Top"), multi[1], jungle[0] };
        layout.mixedPlatformFills = new[] { fill, forest.Single(s => s.name == "Soil"), multi[13], jungle[2] };
        var sources = new List<Sprite> { sky[0] };
        foreach (string levelName in new[] { "Tutorial", "Level4", "Level3" })
        {
            var additive = EditorSceneManager.OpenScene(Scenes + levelName + ".unity", OpenSceneMode.Additive);
            var roots = additive.GetRootGameObjects();
            var sourceLayout = roots.SelectMany(r => r.GetComponentsInChildren<RandomLevelLayout>()).FirstOrDefault();
            if (sourceLayout != null) sources.Add(levelName == "Level4" ? sourceLayout.backgroundLayers[1] : sourceLayout.backgroundLayers.Last());
            else sources.Add(roots.SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>()).First(r => r.GetComponent<JungleParallax>() != null).sprite);
            EditorSceneManager.CloseScene(additive, true);
        }
        layout.mixedBackgrounds = sources.ToArray(); layout.scenerySprites = Array.Empty<Sprite>();
    }
    static void BuildFixedArena(LevelOne level, RandomLevelLayout layout, FinalCampaignStage stage)
    {
        var icons = layout.powerUpIcons; var pickupPrefab = layout.pickupPrefab;
        UnityEngine.Object.DestroyImmediate(layout.previousWorld.gameObject); UnityEngine.Object.DestroyImmediate(layout);
        level.layout = null;
        var world = new GameObject("Hunter fixed arena").transform;
        Platform(world, "Floor", -11, -4, 41, false);
        ApplyRequestedRefinements.ArrangeArena(world);
        foreach (float x in new[] { -11.5f, 30.5f })
        { var wall = new GameObject("Arena boundary").AddComponent<BoxCollider2D>(); wall.transform.SetParent(world, false); wall.transform.position = new Vector3(x, 1, 0); wall.size = new Vector2(1, 20); }
        for (int i = 0; i < 3; i++)
        {
            var bg = new GameObject("Final background " + i).AddComponent<SpriteRenderer>(); bg.transform.SetParent(world, false);
            bg.sprite = sky[0]; bg.sortingOrder = -30; bg.transform.position = new Vector3(-10 + i * 22, -6, 0);
            bg.transform.localScale = Vector3.one * (16 / bg.sprite.bounds.size.y);
        }
        float[] spots = { -8.8f, 5, -5, -6.5f, -7.6f, -2.5f };
        for (int i = 0; i < stage.fixedPowerUps.Length; i++)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab, world);
            var kind = stage.fixedPowerUps[i];
            float x = kind == PowerUp.Kind.Medkit ? -8.8f : kind == PowerUp.Kind.Apple ? -6.5f
                : kind == PowerUp.Kind.Citrus ? -7.6f : kind == PowerUp.Kind.Feather ? -2.5f : spots[i];
            root.transform.position = new Vector3(x, kind == PowerUp.Kind.Feather ? -1.7f : -3, 0);
            var power = root.GetComponent<PowerUp>(); power.kind = stage.fixedPowerUps[i]; power.pickupId = "Level8." + power.kind;
            power.durationSeconds = PowerUp.DefaultDuration(power.kind); power.icon.sprite = icons[(int)power.kind];
            PrefabUtility.RecordPrefabInstancePropertyModifications(power); PrefabUtility.RecordPrefabInstancePropertyModifications(power.icon);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
        }
    }
    static void Platform(Transform parent, string name, float left, float surface, float width, bool raised)
    {
        var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.position = new Vector3(left + width / 2, surface - 0.25f, 0);
        var box = root.AddComponent<BoxCollider2D>(); box.size = new Vector2(width, raised ? 0.5f : 1.5f); box.offset = raised ? Vector2.zero : new Vector2(0, -0.5f);
        var visual = new GameObject("Top").AddComponent<SpriteRenderer>(); visual.transform.SetParent(root.transform, false); visual.sprite = top;
        visual.transform.localScale = new Vector3(width / top.bounds.size.x, 0.5f / top.bounds.size.y, 1); visual.sortingOrder = 1;
        if (!raised)
        { var dirt = new GameObject("Stone").AddComponent<SpriteRenderer>(); dirt.transform.SetParent(root.transform, false); dirt.sprite = fill; dirt.transform.localPosition = new Vector3(0, -0.75f, 0); dirt.transform.localScale = new Vector3(width / fill.bounds.size.x, 1 / fill.bounds.size.y, 1); }
    }
    public static void ConnectCampaign()
    {
        var scene = EditorSceneManager.OpenScene(Scenes + "Level5.unity");
        UnityEngine.Object.FindAnyObjectByType<LevelOne>().nextSceneName = "Level6";
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = new[] { "Tutorial", "Level2", "Level3", "Level4", "Level5", "Level6", "Level7", "Level8" }
            .Select(n => new EditorBuildSettingsScene(Scenes + n + ".unity", true)).ToArray();
    }
}
