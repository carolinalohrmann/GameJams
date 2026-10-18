using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildGameLevels
{
    const string LevelOnePath = "Assets/0_0Scenes/Tutorial.unity";
    const string LevelTwoPath = "Assets/0_0Scenes/Level2.unity";
    const string LevelThreePath = "Assets/0_0Scenes/Level3.unity";
    const string ForestRoot = "Assets/Art/TinyForest/";
    const string MultiRoot = "Assets/Art/Multi Platformer Tileset Free/GrassLand/";
    const string MonstersRoot = "Assets/Art/Monsters Creatures Fantasy/";

    [MenuItem("GameJams/Construir tutorial y niveles")]
    public static void BuildAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        BuildTutorial.Build();
        CreateEnemyPrefabs();
        ConfigureLevelOne();

        // Preserve the scene GUID when rebuilding an existing second level.
        var firstScene = EditorSceneManager.OpenScene(LevelOnePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(firstScene, LevelTwoPath, true))
            throw new InvalidOperationException("No se pudo crear la escena del nivel 2.");
        ConfigureLevelTwo();
        BuildForestLevel();
        ConfigureBuildSettings();
        ExpandCampaign.Build();
        AssetDatabase.SaveAssets();
        Debug.Log("GAME_LEVELS_BUILD_OK: fixed tutorial, meadow, Tiny Forest");
    }

    public static void BuildAndExit()
    {
        try
        {
            BuildAll();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static void CreateEnemyPrefabs()
    {
        foreach (string prefabName in new[] { "Goblin", "Mushroom", "ZombieDog", "FlyingEye" })
        {
            string folder = "Assets/Animations/Monsters/" + prefabName;
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
        }

        var skeleton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/0_2prefafs/EnemyMelee.prefab");
        if (skeleton == null) throw new InvalidOperationException("Falta el prefab de esqueleto existente.");
        var projectile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/0_2prefafs/bullets.prefab")
            .GetComponent<bullet>();

        var goblin = BuildFantasyEnemy("Goblin", "Goblin", false, projectile,
            new[] { "Idle", "Run", "Attack", "Take Hit", "Death" },
            new[] { "Idle", "Walk", "Attack", "Hit", "Death" });
        var mushroom = BuildFantasyEnemy("Mushroom", "Mushroom", false, projectile,
            new[] { "Idle", "Run", "Attack", "Take Hit", "Death" },
            new[] { "Idle", "Walk", "Attack", "Hit", "Death" });
        var zombieDog = BuildZombieDog(projectile);
        var flyingEye = BuildFantasyEnemy("Flying eye", "FlyingEye", true, projectile,
            new[] { "Flight", "Flight", "Attack", "Take Hit", "Death" },
            new[] { "Idle", "Walk", "Attack", "Hit", "Death" });

        meleePrefabs = new[] { skeleton, goblin, mushroom, zombieDog };
        rangedPrefab = flyingEye;
    }

    static GameObject[] meleePrefabs;
    static GameObject rangedPrefab;

    internal static GameObject BuildFantasyEnemy(string folder, string prefabName, bool ranged, bullet projectile,
        string[] sourceAnimations, string[] stateNames)
    {
        string animationFolder = "Assets/Animations/Monsters/" + prefabName;
        EnsureFolder(animationFolder);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(animationFolder + "/" + prefabName + ".controller");

        Sprite[] idleFrames = null;
        var frameCache = new System.Collections.Generic.Dictionary<string, Sprite[]>();
        Vector2 pivot = VisiblePivot(MonstersRoot + folder + "/" + sourceAnimations[0] + ".png", 150, 150, ranged);
        for (int i = 0; i < sourceAnimations.Length; i++)
        {
            string source = MonstersRoot + folder + "/" + sourceAnimations[i] + ".png";
            if (!frameCache.TryGetValue(source, out var frames))
                frameCache[source] = frames = HorizontalFrames(source, 150, 150, 32, pivot);
            if (i == 0) idleFrames = frames;
            AddAnimationState(controller, animationFolder, stateNames[i], frames, i == 0 || i == 1);
        }

        return SaveEnemyPrefab(prefabName, ranged, ranged, projectile,
            idleFrames[0], controller,
            new Vector2(0.9f, 0.9f), 1);
    }

    static GameObject BuildZombieDog(bullet projectile)
    {
        const string folder = "Assets/Animations/Monsters/ZombieDog";
        EnsureFolder(folder);
        string sheet = "Assets/Art/Enemy Sprites/09_perro_zombie/09_perro_zombie_spritesheet.png";
        var frames = HorizontalFrames(sheet, 229, 151, 180, new Vector2(0.5f, 0));
        var controller = AnimatorController.CreateAnimatorControllerAtPath(folder + "/ZombieDog.controller");
        AddAnimationState(controller, folder, "Idle", new[] { frames[0] }, true);
        AddAnimationState(controller, folder, "Walk", frames, true);
        AddAnimationState(controller, folder, "Attack", frames, true);
        AddAnimationState(controller, folder, "Hit", new[] { frames[frames.Length - 1] }, false);
        AddAnimationState(controller, folder, "Death", frames.Reverse().ToArray(), false);
        return SaveEnemyPrefab("ZombieDog", false, false, projectile, frames[0], controller,
            new Vector2(1.05f, 0.8f), 1);
    }

    static GameObject SaveEnemyPrefab(string name, bool ranged, bool flying, bullet projectile,
        Sprite idle, AnimatorController controller, Vector2 colliderSize, int health)
    {
        var root = new GameObject(name);
        var collider = root.AddComponent<BoxCollider2D>();
        collider.size = colliderSize;
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        var visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(root.transform, false);
        visualObject.transform.localPosition = new Vector3(0, flying ? 0 : -colliderSize.y / 2, 0);
        var visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sprite = idle;
        visual.sortingOrder = 3;
        var animator = visualObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var enemy = root.AddComponent<Enemy>();
        enemy.ranged = ranged;
        enemy.spriteFacesLeft = name == "ZombieDog";
        enemy.flying = flying;
        enemy.projectile = projectile;
        enemy.animator = animator;
        enemy.visual = visual;
        enemy.health = health;
        enemy.patrolDistance = 1.5f;
        enemy.moveSpeed = 1;

        string path = "Assets/0_2prefafs/Enemy" + name + ".prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    static Sprite[] HorizontalFrames(string path, int frameWidth, int frameHeight, float pixelsPerUnit, Vector2 pivot)
    {
        ImportSprite(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width % frameWidth != 0 || height != frameHeight)
            throw new InvalidOperationException("Unexpected sprite sheet dimensions: " + path);

        int count = width / frameWidth;
        var rects = Enumerable.Range(0, count)
            .Select(i => new SpriteRect
            {
                name = "frame_" + i.ToString("D2"),
                rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight),
                pivot = pivot,
                alignment = SpriteAlignment.Custom,
                spriteID = GUID.Generate()
            }).ToArray();
        SetSpriteRects(path, rects);
        SetSpriteImportOptions(path, pixelsPerUnit);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
    }

    static Sprite[] GridFrames(string path, int tileSize, float pixelsPerUnit)
    {
        ImportSprite(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width % tileSize != 0 || height % tileSize != 0)
            throw new InvalidOperationException("Unexpected tile atlas dimensions: " + path);

        int columns = width / tileSize;
        int rows = height / tileSize;
        var rects = Enumerable.Range(0, columns * rows)
            .Select(i =>
            {
                int column = i % columns;
                int row = i / columns;
                return new SpriteRect
                {
                    name = "tile_" + i.ToString("D2"),
                    rect = new Rect(column * tileSize, height - tileSize * (row + 1), tileSize, tileSize),
                    pivot = Vector2.one * 0.5f,
                    alignment = SpriteAlignment.Center,
                    spriteID = GUID.Generate()
                };
            }).ToArray();
        SetSpriteRects(path, rects);
        SetSpriteImportOptions(path, pixelsPerUnit);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
    }

    static void SetSpriteRects(string path, SpriteRect[] rects)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        // Shared atlases must retain sprite IDs when rebuilding a single level.
        var existing = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
        foreach (var rect in rects)
            if (existing.TryGetValue(rect.name, out var spriteID)) rect.spriteID = spriteID;
        provider.SetSpriteRects(rects);
        var nameData = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameData.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }

    static void ImportSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new InvalidOperationException("Missing sprite asset: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    static void SetSpriteImportOptions(string path, float pixelsPerUnit)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    internal static Sprite SpriteAt(string path, float pixelsPerUnit, Vector2 pivot)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new InvalidOperationException("Missing sprite asset: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = pivot;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Could not load sprite: " + path);
        return sprite;
    }

    static void AddAnimationState(AnimatorController controller, string folder, string name, Sprite[] frames, bool loop)
    {
        if (frames.Length == 0) throw new InvalidOperationException("No imported frames for " + folder + "/" + name);
        var clip = new AnimationClip { frameRate = 12, name = name };
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / 12f, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe
        {
            time = frames.Length / 12f,
            value = frames[frames.Length - 1]
        };
        AnimationUtility.SetObjectReferenceCurve(clip,
            new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, folder + "/" + name.Replace(' ', '_') + ".anim");
        var state = controller.layers[0].stateMachine.AddState(name);
        state.motion = clip;
        if (name == "Idle") controller.layers[0].stateMachine.defaultState = state;
    }

    static void ConfigureLevelOne()
    {
        var scene = EditorSceneManager.OpenScene(LevelOnePath, OpenSceneMode.Single);
        var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
        // Level1 is the authored tutorial. Never replace it at runtime.
        var layout = level.GetComponent<RandomLevelLayout>();
        if (layout != null) UnityEngine.Object.DestroyImmediate(layout);
        level.layout = null;
        level.stageNumber = 1;
        level.nextSceneName = "Level2";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void ConfigureLevelTwo()
    {
        var scene = EditorSceneManager.OpenScene(LevelTwoPath, OpenSceneMode.Single);
        var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
        var layout = level.gameObject.GetComponent<RandomLevelLayout>() ?? level.gameObject.AddComponent<RandomLevelLayout>();
        layout.level = level;
        layout.previousWorld = GameObject.Find("Tutorial - Jungle").transform;
        layout.stageNumber = 2;
        layout.maximumPowerUps = 3;
        layout.pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/0_2prefafs/PowerUpCapsule.prefab");

        var tiles = GridFrames(MultiRoot + "Terrain/Grassland_Terrain_47Tiles.png", 16, 32);
        layout.platformTop = tiles[1];
        layout.platformFill = tiles[13];
        layout.scenerySprites = new[]
        {
            SpriteAt(MultiRoot + "Props/GrassLand_Tree.png", 32, Vector2.one * 0.5f),
            SpriteAt(MultiRoot + "Props/GrassLand_Bush.png", 32, Vector2.one * 0.5f)
        };
        layout.goalSprite = SpriteAt(MultiRoot + "Props/GrassLand_Entry_3.png", 32, new Vector2(0.5f, 0));
        layout.powerUpIcons = LoadPowerUpIcons();
        layout.meleeEnemies = meleePrefabs;
        layout.rangedEnemy = rangedPrefab;
        level.layout = layout;
        level.stageNumber = 2;
        level.nextSceneName = "Level3";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static Sprite[] LoadPowerUpIcons()
    {
        string[] paths =
        {
            "apple.png", "botikin.png", "citrus.png", "pluma.png",
            "powerup resistencias.png", "camisa.png", "lanzpiedra.png", "Chicken.png"
        };
        return paths.Select(name =>
        {
            string path = "Assets/Art/Pickups/" + name;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) sprite = SpriteAt(path, 256, Vector2.one * 0.5f);
            return sprite;
        }).ToArray();
    }

    static Sprite LoadSlice(string path, string name)
    {
        var sprite = LoadAllSlices(path).FirstOrDefault(item => item.name == name);
        if (sprite == null) throw new InvalidOperationException("Missing sprite slice " + name + ": " + path);
        return sprite;
    }

    static Sprite[] LoadAllSlices(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();
    }

    static void ConfigureBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(LevelOnePath, true),
            new EditorBuildSettingsScene(LevelTwoPath, true),
            new EditorBuildSettingsScene(LevelThreePath, true)
        };
        foreach (string path in new[] { "Assets/0_0Scenes/Level4.unity", "Assets/0_0Scenes/Level5.unity", "Assets/0_0Scenes/Level6.unity", "Assets/0_0Scenes/Level7.unity", "Assets/0_0Scenes/Level8.unity" })
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [MenuItem("GameJams/Crear nivel Tiny Forest")]
    public static void BuildForestLevel()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var source = EditorSceneManager.OpenScene(LevelTwoPath);
        if (!EditorSceneManager.SaveScene(source, LevelThreePath, true))
            throw new InvalidOperationException("No se pudo crear Level3.unity");
        var scene = EditorSceneManager.OpenScene(LevelThreePath);
        var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
        var layout = level.layout;
        if (layout == null) throw new InvalidOperationException("Falta el generador del nivel 2");
        string terrainPath = ForestRoot + "Tileset(16x16)/Tileset.png";
        ImportSprite(terrainPath);
        // Stretch the middle of the terrain, leaving the bright side caps out.
        SetSpriteRects(terrainPath, new[]
        {
            new SpriteRect { name = "Top", rect = new Rect(3, 80, 26, 16), pivot = Vector2.one * 0.5f,
                alignment = SpriteAlignment.Center, spriteID = GUID.Generate() },
            new SpriteRect { name = "Soil", rect = new Rect(3, 64, 26, 16), pivot = Vector2.one * 0.5f,
                alignment = SpriteAlignment.Center, spriteID = GUID.Generate() }
        });
        SetSpriteImportOptions(terrainPath, 32);
        var terrainSprites = AssetDatabase.LoadAllAssetsAtPath(terrainPath).OfType<Sprite>().ToArray();
        layout.platformTop = terrainSprites.Single(s => s.name == "Top");
        layout.platformFill = terrainSprites.Single(s => s.name == "Soil");
        layout.scenerySprites = Array.Empty<Sprite>();
        layout.backgroundLayers = new[]
        {
            SpriteAt(ForestRoot + "Background/Layer_03.png", 32, new Vector2(0.5f, 0)),
            BackgroundRegion(ForestRoot + "Background/Layer_02.png", new Rect(18, 32, 387, 185)),
            BackgroundRegion(ForestRoot + "Background/Layer_01.png", new Rect(0, 170, 439, 365))
        };
        layout.stageNumber = 3;
        layout.minimumGap = 0.8f; layout.maximumGap = 1.35f;
        level.stageNumber = 3;
        level.nextSceneName = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/0_0Scenes/Level4.unity") != null ? "Level4" : "Tutorial";
        var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
        ui.stageTitleText.text = "NIVEL 3";
        ui.stageObjectiveText.text = "Atraviesa el bosque y llega a la salida.";
        ui.completionTitleText.text = "Nivel 3 completado";
        ui.completionDescriptionText.text = level.nextSceneName == "Tutorial" ? "¡Completaste todos los niveles!" : "Continua a la siguiente etapa.";
        ui.restartButton.GetComponentInChildren<UnityEngine.UI.Text>().text = level.nextSceneName == "Tutorial" ? "Jugar de nuevo" : "Siguiente nivel";
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        scene = EditorSceneManager.OpenScene(LevelTwoPath);
        level = UnityEngine.Object.FindAnyObjectByType<LevelOne>(); level.nextSceneName = "Level3";
        ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
        ui.completionDescriptionText.text = "El bosque es la siguiente etapa.";
        ui.restartButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Siguiente nivel";
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        ConfigureBuildSettings(); AssetDatabase.SaveAssets();
        Debug.Log("FOREST_LEVEL_BUILD_OK: Tutorial preserved; Level2 -> Level3 -> Tutorial; three original forest background layers");
    }

    public static void BuildForestAndExit()
    {
        try { BuildForestLevel(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    static Sprite BackgroundRegion(string path, Rect region)
    {
        ImportSprite(path);
        SetSpriteRects(path, new[] { new SpriteRect { name = "Background", rect = region,
            pivot = new Vector2(0.5f, 0), alignment = SpriteAlignment.Custom, spriteID = GUID.Generate() } });
        SetSpriteImportOptions(path, 32);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static Vector2 VisiblePivot(string path, int frameWidth, int frameHeight, bool centered)
    {
        // These sheets contain large transparent borders around the character.
        var texture = new Texture2D(2, 2);
        if (!texture.LoadImage(System.IO.File.ReadAllBytes(path)))
            throw new InvalidOperationException("Could not decode sprite sheet: " + path);
        int bottom = frameHeight, top = 0;
        for (int y = 0; y < frameHeight; y++)
        for (int x = 0; x < frameWidth; x++)
            if (texture.GetPixel(x, y).a > 0.1f) { bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y + 1); }
        UnityEngine.Object.DestroyImmediate(texture);
        if (bottom >= top) throw new InvalidOperationException("Empty first sprite frame: " + path);
        return new Vector2(0.5f, (centered ? (bottom + top) * 0.5f : bottom) / frameHeight);
    }
}
