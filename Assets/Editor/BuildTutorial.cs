using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class BuildTutorial
{
    const string Jungle = "Assets/Art/Jungle/Jungle platformer assets/";
    static Transform world;
    static Material material;
    static Sprite grass, dirt, tree, shrub;
    static bullet projectile;
    static AnimatorController skeletonController;
    static Sprite skeletonIdle, placeholder;

    [MenuItem("GameJams/Construir tutorial con assets")]
    public static void Build()
    {
        // Start from our existing editable greybox; SampleScene remains untouched.
        BuildLevelOne.Build();
        var scene = EditorSceneManager.GetActiveScene();
        var p = UnityEngine.Object.FindAnyObjectByType<player>();
        material = p.GetComponent<SpriteRenderer>().sharedMaterial;
        placeholder = p.GetComponent<SpriteRenderer>().sprite;
        projectile = p.projectile;
        var oldWorld = GameObject.Find("Nivel 1 - plataformas y decoracion");
        UnityEngine.Object.DestroyImmediate(oldWorld);
        world = new GameObject("Tutorial - Jungle").transform;
        Directory.CreateDirectory("Assets/Animations/Horse");
        Directory.CreateDirectory("Assets/Animations/Skeleton");
        AssetDatabase.Refresh();
        var horse = HorseAnimations();
        SkeletonAnimations();

        // Keep the physical rectangle and use an independently sized visual child.
        p.transform.localScale = Vector3.one;
        UnityEngine.Object.DestroyImmediate(p.GetComponent<BoxCollider2D>());
        var playerCollider = p.gameObject.AddComponent<CapsuleCollider2D>();
        playerCollider.direction = CapsuleDirection2D.Vertical;
        playerCollider.size = new Vector2(0.65f, 1.2f);
        p.GetComponent<SpriteRenderer>().enabled = false;
        var visual = Visual("Caballo", p.transform, horse.Item2, new Vector3(0, -0.6f, 0), Vector3.one, 3);
        p.characterVisual = visual;
        p.animator = visual.gameObject.AddComponent<Animator>();
        p.animator.runtimeAnimatorController = horse.Item1;
        p.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        p.shootPoint.localPosition = new Vector3(0.65f, 0.23f, 0);
        p.weaponVisual = Visual("Arma superpuesta", p.transform, null, new Vector3(0.27f, 0.20f, 0), Vector3.one, 5);
        p.minigunSprite = Single("Assets/Art/Weapons/mingun.png", 1536, Vector2.one * 0.5f);
        p.akSprite = Single("Assets/Art/Weapons/ak47.png", 1536, Vector2.one * 0.5f);
        p.rockSprite = Single("Assets/Art/Weapons/piedra.png", 1536, Vector2.one * 0.5f);
        p.weaponVisual.enabled = false;

        var tiles = Slice(Jungle + "Solid blocks/Grass/Grass.png", 16, 16, 4, 32, Vector2.one * 0.5f);
        grass = tiles[0]; dirt = tiles[2];
        // Rectangles below are sprite slices from the Jungle decorations atlas.
        var decor = Regions(Jungle + "Decorations/Decorations.png", new[] {
            new Rect(0, 64, 64, 64), new Rect(16, 160, 32, 32)
        }, 16, new Vector2(0.5f, 0));
        tree = decor[0]; shrub = decor[1];
        Ground("Inicio", -11, 17);
        Ground("Salto corto", 18.5f, 31);
        Ground("Camino tutorial", 32.5f, 124);
        Platform("Primer salto", -1, -3.2f, 2.5f, 1);
        Platform("Salto con pluma", 25, -2.5f, 3, 2);
        Platform("Practica doble salto", 29, -0.7f, 3, 1);
        Limit("Limite izquierdo", -11.5f);
        Limit("Limite derecho", 124.5f);
        for (int i = 0; i < 29; i++)
        {
            var t = Visual("Jungle fondo " + i, world, tree, new Vector3(-14 + i * 5, -4.1f, 0), Vector3.one * (i % 2 == 0 ? 1.5f : 1.25f), -12);
            var parallax = t.gameObject.AddComponent<JungleParallax>();
            parallax.factor = 0.22f; parallax.span = 145;
            Visual("Vegetacion " + i, world, shrub, new Vector3(-10 + i * 4.8f, -4.03f, 0), Vector3.one * 0.55f, -4);
        }
        var barn = Single("Assets/Art/Buildings/Granero.png", 256, new Vector2(0.5f, 94f / 1024));
        var barnVisual = Visual("Granero - meta", world, barn, new Vector3(118, -4.02f, 0), Vector3.one, 1);
        AdjustPresentation.AlignBarn(barnVisual);
        // Single shared capsule prefab, icon and effect configured per instance.
        var pickupPrefab = CreatePickupPrefab();
        Pickup(pickupPrefab, PowerUp.Kind.Apple, -6, "apple.png");
        Pickup(pickupPrefab, PowerUp.Kind.Medkit, 3, "botikin.png");
        Pickup(pickupPrefab, PowerUp.Kind.Citrus, 9, "citrus.png");
        Pickup(pickupPrefab, PowerUp.Kind.Feather, 22, "pluma.png");
        Pickup(pickupPrefab, PowerUp.Kind.Resistance, 36, "powerup resistencias.png");
        Pickup(pickupPrefab, PowerUp.Kind.Shirt, 50, "camisa.png");
        Pickup(pickupPrefab, PowerUp.Kind.Rock, 78, "lanzpiedra.png");
        Pickup(pickupPrefab, PowerUp.Kind.Chicken, 101, "Chicken.png");
        EnemyAt("Skeleton - botiquin", 0.5f, false, 1);
        EnemyAt("Skeleton - practica", 14, false, 1.5f);
        EnemyAt("Tirador provisional - resistencia", 40, true);
        EnemyAt("Skeleton - resistencia", 44, false);
        EnemyAt("Skeleton - minigun 1", 54, false);
        EnemyAt("Tirador provisional - minigun", 58, true);
        EnemyAt("Skeleton - minigun 2", 63, false);
        for (int i = 0; i < 5; i++) EnemyAt("Skeleton - piedra " + (i + 1), 82 + i * 4, false, 0);
        EnemyAt("Tirador provisional - AK", 106, true);
        EnemyAt("Skeleton - AK", 111, false);
        ImproveReadability.SpaceVegetation();
        var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
        level.endX = 116; level.leftEdge = -11; level.rightEdge = 124;
        Camera.main.backgroundColor = Color.gray;
        BuildTutorialCanvas.Create(level);
        EditorSceneManager.SaveScene(scene, "Assets/0_0Scenes/Tutorial.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("TUTORIAL_BUILD_OK: horse animations, Jungle, Skeleton, provisional ranged, barn, 8 powerups");
    }
    static SpriteRenderer Visual(string name, Transform parent, Sprite sprite, Vector3 position, Vector3 scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = material; sr.sortingOrder = order;
        return sr;
    }
    static TextureImporter Import(string path, float ppu, Vector2 pivot, bool multiple)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) throw new Exception("Missing art: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.spritePivot = pivot;
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
        textureSettings.spritePivot = pivot;
        importer.SetTextureSettings(textureSettings);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
        return importer;
    }
    static Sprite Single(string path, float ppu, Vector2 pivot)
    {
        Import(path, ppu, pivot, false);
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite[] Regions(string path, Rect[] regions, float ppu, Vector2 pivot)
    {
        var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (regions.Any(r => r.xMin < 0 || r.yMin < 0 || r.xMax > source.width || r.yMax > source.height))
            throw new Exception("Sprite region outside texture: " + path);
        var importer = Import(path, ppu, pivot, true);
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var rects = regions.Select((r, i) => new SpriteRect { name = "frame_" + i.ToString("D3"), rect = r, pivot = pivot, alignment = SpriteAlignment.Custom, spriteID = GUID.Generate() }).ToArray();
        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }
    static Sprite[] Slice(string path, int width, int height, int count, float ppu, Vector2 pivot)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var rects = Enumerable.Range(0, count).Select(i => new Rect(i % (texture.width / width) * width, texture.height - height * (1 + i / (texture.width / width)), width, height)).ToArray();
        return Regions(path, rects, ppu, pivot);
    }
    static AnimatorController Controller(string folder, string name)
    {
        string path = "Assets/Animations/" + folder + "/" + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) AssetDatabase.DeleteAsset(path);
        return AnimatorController.CreateAnimatorControllerAtPath(path);
    }
    static void Clip(AnimatorController controller, string folder, string state, Sprite[] frames, float[] durations, bool loop)
    {
        var clip = new AnimationClip { frameRate = 30, name = state };
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        float time = 0;
        for (int i = 0; i < frames.Length; i++) { keys[i] = new ObjectReferenceKeyframe { time = time, value = frames[i] }; time += durations.Length == 1 ? durations[0] : durations[i]; }
        keys[frames.Length] = new ObjectReferenceKeyframe { time = time, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
        string path = "Assets/Animations/" + folder + "/" + state + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); UnityEngine.Object.DestroyImmediate(clip); clip = existing; }
        else AssetDatabase.CreateAsset(clip, path);
        var node = controller.layers[0].stateMachine.AddState(state); node.motion = clip;
        if (state == "Idle") controller.layers[0].stateMachine.defaultState = node;
    }
    static Tuple<AnimatorController, Sprite> HorseAnimations()
    {
        var controller = Controller("Horse", "Horse"); Sprite idle = null;
        string[] folders = { "Idle", "Caminar", "Saltar", "Muerte" }; string[] states = { "Idle", "Walk", "Jump", "Death" };
        for (int n = 0; n < folders.Length; n++)
        {
            var frames = Directory.GetFiles("Assets/Art/Characters/Horse/" + folders[n], "*.png").OrderBy(f => f).Select(f => Single(f.Replace('\\', '/'), 155, new Vector2(0.5f, 0.078125f))).ToArray();
            if (n == 0) idle = frames[0];
            float[] durations = n == 0 ? new[] { 0.18f } : n == 1 ? new[] { 0.12f } : n == 2 ? new[] { 0.14f, 0.14f, 0.14f, 0.65f } : new[] { 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.65f };
            Clip(controller, "Horse", states[n], frames, durations, n < 2);
        }
        return Tuple.Create(controller, idle);
    }
    static void SkeletonAnimations()
    {
        skeletonController = Controller("Skeleton", "Skeleton");
        string[] files = { "Idle", "Walk", "Attack", "Hit", "Dead", "React" }; int[] widths = { 24, 22, 43, 30, 33, 22 }; int[] heights = { 32, 33, 37, 32, 32, 32 }; int[] counts = { 11, 13, 18, 8, 15, 4 };
        for (int i = 0; i < files.Length; i++)
        {
            var frames = Slice("Assets/Art/Characters/Skeleton/Sprite Sheets/Skeleton " + files[i] + ".png", widths[i], heights[i], counts[i], 25, new Vector2(0.5f, 0));
            if (i == 0) skeletonIdle = frames[0];
            Clip(skeletonController, "Skeleton", files[i] == "Dead" ? "Death" : files[i], frames, new[] { files[i] == "Dead" ? 0.04f : 0.07f }, i < 2);
        }
    }
    static void Ground(string name, float left, float right)
    {
        var go = new GameObject("Suelo " + name); go.transform.SetParent(world);
        go.transform.position = new Vector3((left + right) / 2, -4.5f, 0);
        var collider = go.AddComponent<BoxCollider2D>(); collider.size = new Vector2(right - left, 1);
        for (float x = left + 0.25f; x < right; x += 0.5f)
        {
            Visual("Cesped", go.transform, grass, new Vector3(x - go.transform.position.x, 0.25f, 0), Vector3.one, 0);
            Visual("Tierra", go.transform, dirt, new Vector3(x - go.transform.position.x, -0.25f, 0), Vector3.one, 0);
        }
    }
    static void Platform(string name, float x, float y, float width, int sections)
    {
        var go = new GameObject(name); go.transform.SetParent(world); go.transform.position = new Vector3(x, y, 0);
        go.AddComponent<BoxCollider2D>().size = new Vector2(width, 0.5f);
        float sectionWidth = width / sections;
        for (int i = 0; i < sections; i++)
            Visual("Jungle tramo " + (i + 1), go.transform, grass,
                new Vector3(-width / 2 + sectionWidth * (i + 0.5f), 0, 0),
                new Vector3(sectionWidth / grass.bounds.size.x, 0.5f / grass.bounds.size.y, 1), 0);
    }
    static void Limit(string name, float x)
    {
        var go = new GameObject(name); go.transform.SetParent(world); go.transform.position = new Vector3(x, 0, 0); go.AddComponent<BoxCollider2D>().size = new Vector2(1, 14);
    }
    static GameObject CreatePickupPrefab()
    {
        var go = new GameObject("PowerUpCapsule");
        var effect = go.AddComponent<PowerUp>();
        effect.icon = Visual("Icono", go.transform, null, Vector3.zero, Vector3.one * 0.75f, 6);
        var collider = go.AddComponent<CapsuleCollider2D>(); collider.size = new Vector2(0.85f, 0.95f); collider.isTrigger = true;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/0_2prefafs/PowerUpCapsule.prefab");
        UnityEngine.Object.DestroyImmediate(go); return prefab;
    }
    static void Pickup(GameObject prefab, PowerUp.Kind kind, float x, string icon)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name = "PowerUp " + kind; go.transform.SetParent(world); go.transform.position = new Vector3(x, -3.1f, 0);
        var effect = go.GetComponent<PowerUp>(); effect.kind = kind; effect.pickupId = "Level1." + kind;
        effect.durationSeconds = PowerUp.DefaultDuration(kind);
        var path = "Assets/Art/Pickups/" + icon;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        effect.icon.sprite = Single(path, Mathf.Max(tex.width, tex.height), Vector2.one * 0.5f);
        EditorUtility.SetDirty(effect);
    }
    static void EnemyAt(string name, float x, bool ranged, float patrol = 2)
    {
        var go = new GameObject(name); go.transform.SetParent(world); go.transform.position = new Vector3(x, -3.5f, 0);
        var collider = go.AddComponent<BoxCollider2D>(); collider.size = ranged ? new Vector2(0.8f, 0.85f) : Vector2.one;
        var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true; body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var enemy = go.AddComponent<Enemy>(); enemy.ranged = ranged; enemy.flying = false; enemy.patrolDistance = patrol; enemy.moveSpeed = 1; enemy.projectile = projectile;
        enemy.visual = Visual(ranged ? "Cuadrado provisional" : "Skeleton", go.transform, ranged ? placeholder : skeletonIdle, new Vector3(0, ranged ? 0 : -0.5f, 0), Vector3.one, 3);
        if (!ranged)
        {
            enemy.animator = enemy.visual.gameObject.AddComponent<Animator>();
            enemy.animator.runtimeAnimatorController = skeletonController;
        }
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/0_2prefafs/" + (ranged ? "EnemyRanged" : "EnemyMelee") + ".prefab");
    }
}
