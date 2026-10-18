using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ExpandCampaign
{
    const string Scenes = "Assets/0_0Scenes/";
    const string Prefabs = "Assets/0_2prefafs/";
    static GameObject skeleton, goblin, mushroom, flyingEye;

    [MenuItem("GameJams/Actualizar monstruos y crear niveles 4 y 5")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        LoadEnemies();
        UpdateTutorial();
        CopyScene("Level2", "Level4");
        CopyScene("Level3", "Level5");
        for (int stage = 2; stage <= 5; stage++) ConfigureStage(stage);
        ConfigureBuildSettings(); AssetDatabase.SaveAssets();
        PolishCombatAndLayout.Apply();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Scenes + "Level6.unity") != null) BuildFinalCampaign.ConnectCampaign();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Scenes + "Level8.unity") != null) ApplyRequestedRefinements.Apply();
        Debug.Log("CAMPAIGN_BUILD_OK: authored tutorial with Flying Eye/Goblin/Mushroom/FantasySkeleton; four randomized levels; Level4 Multi Platformer; Level5 Tiny Forest");
    }

    public static void BuildAndExit()
    {
        try { Build(); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    static void LoadEnemies()
    {
        skeleton = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EnemyFantasySkeleton.prefab");
        if (skeleton == null)
        {
            var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "bullets.prefab").GetComponent<bullet>();
            skeleton = BuildGameLevels.BuildFantasyEnemy("Skeleton", "FantasySkeleton", false, projectile,
                new[] { "Idle", "Walk", "Attack", "Take Hit", "Death" },
                new[] { "Idle", "Walk", "Attack", "Hit", "Death" });
            var contents = PrefabUtility.LoadPrefabContents(Prefabs + "EnemyFantasySkeleton.prefab");
            contents.GetComponent<Enemy>().health = 2;
            PrefabUtility.SaveAsPrefabAsset(contents, Prefabs + "EnemyFantasySkeleton.prefab");
            PrefabUtility.UnloadPrefabContents(contents);
        }
        goblin = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EnemyGoblin.prefab");
        mushroom = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EnemyMushroom.prefab");
        flyingEye = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EnemyFlyingEye.prefab");
        if (new[] { skeleton, goblin, mushroom, flyingEye }.Any(p => p == null))
            throw new InvalidOperationException("Faltan prefabs de Monsters Creatures Fantasy. Construye primero los niveles base.");
    }

    static void UpdateTutorial()
    {
        var scene = EditorSceneManager.OpenScene(Scenes + "Tutorial.unity");
        var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
        if (level.layout != null) throw new InvalidOperationException("El tutorial debe conservar su recorrido fijo.");
        var oldEnemies = UnityEngine.Object.FindObjectsByType<Enemy>().OrderBy(e => e.transform.position.x).ToArray();
        var floors = UnityEngine.Object.FindObjectsByType<BoxCollider2D>().Where(c => c.name.StartsWith("Suelo ")).ToArray();
        var melee = new[] { skeleton, goblin, mushroom };
        int meleeIndex = 0;
        foreach (var old in oldEnemies)
        {
            var prefab = old.ranged ? flyingEye : melee[meleeIndex++ % melee.Length];
            var replacement = (GameObject)PrefabUtility.InstantiatePrefab(prefab, old.transform.parent);
            var enemy = replacement.GetComponent<Enemy>();
            float x = old.transform.position.x;
            var floor = floors.First(c => c.bounds.min.x <= x && c.bounds.max.x >= x);
            float y = floor.bounds.max.y + (old.ranged ? 1.05f : replacement.GetComponent<BoxCollider2D>().size.y / 2);
            replacement.transform.position = new Vector3(x, y, old.transform.position.z);
            string suffix = old.name.Contains(" - ") ? old.name.Substring(old.name.IndexOf(" - ", StringComparison.Ordinal)) : "";
            replacement.name = (old.ranged ? "Flying Eye" : prefab == skeleton ? "Skeleton" : prefab == goblin ? "Goblin" : "Mushroom") + suffix;
            enemy.patrolDistance = old.patrolDistance;
            enemy.moveSpeed = old.moveSpeed;
            enemy.health = old.health;
            foreach (var obj in new UnityEngine.Object[] { replacement, replacement.transform, enemy })
            {
                EditorUtility.SetDirty(obj); PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            }
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        Physics2D.SyncTransforms(); ImproveReadability.SpaceVegetation();
        level.nextSceneName = "Level2";
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    static void CopyScene(string sourceName, string destinationName)
    {
        var scene = EditorSceneManager.OpenScene(Scenes + sourceName + ".unity");
        if (!EditorSceneManager.SaveScene(scene, Scenes + destinationName + ".unity", true))
            throw new InvalidOperationException("No se pudo crear " + destinationName);
    }

    static void ConfigureStage(int stage)
    {
        var scene = EditorSceneManager.OpenScene(Scenes + "Level" + stage + ".unity");
        var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
        var layout = level.layout;
        bool forest = stage == 3 || stage == 5;
        layout.stageNumber = level.stageNumber = stage;
        level.nextSceneName = stage < 5 ? "Level" + (stage + 1) : "Tutorial";
        layout.meleeEnemies = new[] { skeleton, goblin, mushroom, AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "EnemyZombieDog.prefab") };
        layout.rangedEnemy = flyingEye;
        layout.minimumPlatformWidth = stage == 4 ? 4.8f : stage == 5 ? 4.6f : 5.2f;
        layout.maximumPlatformWidth = stage == 4 ? 6.4f : stage == 5 ? 6.1f : 7.1f;
        layout.minimumGap = stage == 2 ? 0.55f : stage == 3 ? 0.8f : stage == 4 ? 0.9f : 1.05f;
        layout.maximumGap = stage == 2 ? 1.2f : stage == 3 ? 1.35f : stage == 4 ? 1.45f : 1.55f;
        layout.elevationChange = stage >= 4 ? 0.4f : 0.35f;
        layout.bonusPlatformChance = stage == 4 ? 0.5f : stage == 5 ? 0.4f : 0.28f;
        if (stage >= 4)
        {
            level.rightEdge = stage == 4 ? 134 : 144;
            level.endX = level.rightEdge - 8;
        }
        if (stage == 4)
        {
            const string root = "Assets/Art/Multi Platformer Tileset Free/GrassLand/Background/";
            layout.backgroundLayers = Enumerable.Range(1, 5).Select(i =>
                BuildGameLevels.SpriteAt(root + "GrassLand_Background_" + i + ".png", 32, new Vector2(0.5f, 0))).ToArray();
            layout.backgroundHeights = new[] { 12f, 10, 11, 8, 7 };
            layout.backgroundBaseY = -6.5f;
        }
        var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
        ui.stageTitleText.text = "NIVEL " + stage;
        ui.stageObjectiveText.text = forest ? "Atraviesa el bosque y llega a la salida." : "Atraviesa el prado y llega a la salida.";
        ui.completionTitleText.text = "Nivel " + stage + " completado";
        ui.completionDescriptionText.text = stage < 5 ? "Continua a la siguiente etapa." : "¡Completaste todos los niveles!";
        ui.restartButton.GetComponentInChildren<Text>().text = stage < 5 ? "Siguiente nivel" : "Jugar de nuevo";
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    public static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = new[] { "Tutorial", "Level2", "Level3", "Level4", "Level5", "Level6", "Level7", "Level8" }
            .Where(name => AssetDatabase.LoadAssetAtPath<SceneAsset>(Scenes + name + ".unity") != null)
            .Select(name => new EditorBuildSettingsScene(Scenes + name + ".unity", true)).ToArray();
    }
}
