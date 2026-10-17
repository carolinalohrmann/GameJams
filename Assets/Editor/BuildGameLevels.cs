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
    const string LevelOnePath = "Assets/0_0Scenes/Level1.unity";
    const string LevelTwoPath = "Assets/0_0Scenes/Level2.unity";
    const string MultiRoot = "Assets/Art/Multi Platformer Tileset Free/GrassLand/";
    const string MonstersRoot = "Assets/Art/Monsters Creatures Fantasy/";

    [MenuItem("GameJams/Construir los dos niveles")]
    public static void BuildAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        BuildTutorial.Build();
        CreateEnemyPrefabs();
        ConfigureLevelOne();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LevelTwoPath) != null)
            AssetDatabase.DeleteAsset(LevelTwoPath);
        if (!AssetDatabase.CopyAsset(LevelOnePath, LevelTwoPath))
            throw new InvalidOperationException("No se pudo crear la escena del nivel 2.");
        ConfigureLevelTwo();
        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("GAME_LEVELS_BUILD_OK: two procedural scenes, randomized safe jumps, three power-ups per run");
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

    static GameObject BuildFantasyEnemy(string folder, string prefabName, bool ranged, bullet projectile,
        string[] sourceAnimations, string[] stateNames)
    {
        string animationFolder = "Assets/Animations/Monsters/" + prefabName;
        EnsureFolder(animationFolder);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(animationFolder + "/" + prefabName + ".controller");

        Sprite[] idleFrames = null;
        for (int i = 0; i < sourceAnimations.Length; i++)
        {
            string source = MonstersRoot + folder + "/" + sourceAnimations[i] + ".png";
            var frames = HorizontalFrames(source, 150, 150, 150, new Vector2(0.5f, 0));
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
        visualObject.transform.localPosition = new Vector3(0, ranged ? 0 : -0.5f, 0);
        var visual = visualObject.AddComponent<SpriteRenderer>();
        visual.sprite = idle;
        visual.sortingOrder = 3;
        var animator = visualObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        var enemy = root.AddComponent<Enemy>();
        enemy.ranged = ranged;
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
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture.width % frameWidth != 0 || texture.height != frameHeight)
            throw new InvalidOperationException("Unexpected sprite sheet dimensions: " + path);

        int count = texture.width / frameWidth;
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
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture.width % tileSize != 0 || texture.height % tileSize != 0)
            throw new InvalidOperationException("Unexpected tile atlas dimensions: " + path);

        int columns = texture.width / tileSize;
        int rows = texture.height / tileSize;
        var rects = Enumerable.Range(0, columns * rows)
            .Select(i =>
            {
                int column = i % columns;
                int row = i / columns;
                return new SpriteRect
                {
                    name = "tile_" + i.ToString("D2"),
                    rect = new Rect(column * tileSize, texture.height - tileSize * (row + 1), tileSize, tileSize),
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

    static Sprite SpriteAt(string path, float pixelsPerUnit, Vector2 pivot)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new InvalidOperationException("Missing sprite asset: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spritePivot = pivot;
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
        var layout = level.gameObject.GetComponent<RandomLevelLayout>() ??
            level.gameObject.AddComponent<RandomLevelLayout>();
        layout.level = level;
        layout.previousWorld = GameObject.Find("Tutorial - Jungle").transform;
        layout.stageNumber = 1;
        layout.maximumPowerUps = 3;
        layout.platformTop = LoadSlice("Assets/Art/Jungle/Jungle platformer assets/Solid blocks/Grass/Grass.png", "frame_000");
        layout.platformFill = LoadSlice("Assets/Art/Jungle/Jungle platformer assets/Solid blocks/Grass/Grass.png", "frame_002");
        layout.scenerySprites = LoadAllSlices("Assets/Art/Jungle/Jungle platformer assets/Decorations/Decorations.png");
        layout.goalSprite = SpriteAt("Assets/Art/Buildings/Granero.png", 256, new Vector2(0.5f, 94f / 1024));
        layout.pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/0_2prefafs/PowerUpCapsule.prefab");
        layout.powerUpIcons = LoadPowerUpIcons();
        layout.meleeEnemies = meleePrefabs;
        layout.rangedEnemy = rangedPrefab;
        level.layout = layout;
        level.stageNumber = 1;
        level.nextSceneName = "Level2";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void ConfigureLevelTwo()
    {
        var scene = EditorSceneManager.OpenScene(LevelTwoPath, OpenSceneMode.Single);
        var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
        var layout = level.gameObject.GetComponent<RandomLevelLayout>();
        layout.level = level;
        layout.previousWorld = GameObject.Find("Tutorial - Jungle").transform;
        layout.stageNumber = 2;
        layout.maximumPowerUps = 3;

        var tiles = GridFrames(MultiRoot + "Terrain/Grassland_Terrain_47Tiles.png", 16, 32);
        layout.platformTop = tiles[1];
        layout.platformFill = tiles[13];
        layout.scenerySprites = new[]
        {
            SpriteAt(MultiRoot + "Props/GrassLand_Tree.png", 32, Vector2.one * 0.5f),
            SpriteAt(MultiRoot + "Props/GrassLand_Bush.png", 32, Vector2.one * 0.5f)
        };
        layout.goalSprite = SpriteAt(MultiRoot + "Props/GrassLand_Entry_3.png", 32, Vector2.one * 0.5f);
        layout.powerUpIcons = LoadPowerUpIcons();
        layout.meleeEnemies = meleePrefabs;
        layout.rangedEnemy = rangedPrefab;
        level.layout = layout;
        level.stageNumber = 2;
        level.nextSceneName = "Level1";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static Sprite[] LoadPowerUpIcons()
    {
        string[] paths =
        {
            "apple.png", "botikin.png", "citrus.png", "pluma.png",
            "powerup resistencias.png", "camisa.png", "lanzapiedra.png", "Chicken.png"
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
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(LevelOnePath, true),
            new EditorBuildSettingsScene(LevelTwoPath, true)
        };
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
}
