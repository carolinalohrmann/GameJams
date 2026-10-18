using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ValidateGameLevels
{
    static IEnumerator<float> checks;
    static float next;
    static double deadline;
    static ValidateGameLevels()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("GameLevelsValidation", false)) return;
            SceneManager.sceneLoaded += PrepareScene;
            PrepareScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            checks = Checks(); next = 0; deadline = EditorApplication.timeSinceStartup + 150;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        Require(EditorBuildSettings.scenes.Count(s => s.enabled) >= 5, "All campaign scenes must be registered for scene loading");
        foreach (string path in new[] { "Assets/0_0Scenes/Tutorial.unity", "Assets/0_0Scenes/Level2.unity", "Assets/0_0Scenes/Level3.unity", "Assets/0_0Scenes/Level4.unity", "Assets/0_0Scenes/Level5.unity" })
        {
            EditorSceneManager.OpenScene(path);
            var layout = UnityEngine.Object.FindAnyObjectByType<RandomLevelLayout>();
            if (path.EndsWith("Tutorial.unity"))
            {
                Require(layout == null && UnityEngine.Object.FindObjectsByType<PowerUp>().Length == 8, "Original tutorial must stay authored with all eight powers");
                var tutorialEnemies = UnityEngine.Object.FindObjectsByType<Enemy>();
                Require(tutorialEnemies.Length == 14 && tutorialEnemies.All(e => e.animator != null), "Tutorial enemies must all be animated");
                Require(tutorialEnemies.Where(e => e.ranged).All(e => AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(e)).EndsWith("EnemyFlyingEye.prefab")), "Tutorial ranged enemies must use Flying Eye");
                var meleePaths = tutorialEnemies.Where(e => !e.ranged).Select(e => AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(e))).Distinct().ToArray();
                Require(meleePaths.Length == 3 && meleePaths.Any(p => p.Contains("Goblin")) && meleePaths.Any(p => p.Contains("Mushroom")) && meleePaths.Any(p => p.Contains("FantasySkeleton")), "Tutorial must use all three fantasy melee types");
                continue;
            }
            Require(layout != null && layout.rangedEnemy != null && layout.meleeEnemies.All(p => p != null), "Generated scene has missing prefab references: " + path);
            if (path.EndsWith("Level3.unity" ) || path.EndsWith("Level5.unity"))
            {
                Require(layout.backgroundLayers.Length == 3 && layout.backgroundLayers.All(s => s != null), "Forest needs all three background layers");
                Require(AssetDatabase.GetAssetPath(layout.platformTop).Contains("TinyForest"), "Forest must use its own platform art");
            }
            if (path.EndsWith("Level4.unity"))
                Require(layout.backgroundLayers.Length == 5 && AssetDatabase.GetAssetPath(layout.platformTop).Contains("Multi Platformer"), "Level4 must use Multi Platformer terrain and backgrounds");
            foreach (var prefab in layout.meleeEnemies.Concat(new[] { layout.rangedEnemy }))
            {
                var controller = prefab.GetComponentInChildren<Animator>().runtimeAnimatorController;
                Require(controller != null, "Missing enemy animator: " + prefab.name);
                foreach (var clip in controller.animationClips)
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    Require(AnimationUtility.GetObjectReferenceCurve(clip, binding).All(k => k.value != null), "Missing animation frame: " + prefab.name + "/" + clip.name);
            }
        }
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        UnityEngine.Object.FindAnyObjectByType<player>().persistUpgrades = false;
        SessionState.SetBool("GameLevelsValidation", true); EditorApplication.EnterPlaymode();
    }
    static void PrepareScene(Scene scene, LoadSceneMode mode)
    {
        var p = UnityEngine.Object.FindAnyObjectByType<player>();
        if (p != null) p.persistUpgrades = false;
    }
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void ValidateSpriteReferences()
    {
        const string path = "Assets/Art/TinyForest/Tileset(16x16)/Tileset.png";
        var before = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .ToDictionary(s => s.name, s => GlobalObjectId.GetGlobalObjectIdSlow(s).ToString());
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(path));
        provider.InitSpriteEditorDataProvider();
        var proposedRects = provider.GetSpriteRects();
        foreach (var rect in proposedRects) rect.spriteID = GUID.Generate();
        typeof(BuildGameLevels).GetMethod("SetSpriteRects", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { path, proposedRects });
        var after = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        Require(after.Length == before.Count && after.All(s => before[s.name] == GlobalObjectId.GetGlobalObjectIdSlow(s).ToString()), "Rebuilding shared terrain changed existing sprite references");
        foreach (int stage in new[] { 3, 5 })
        {
            EditorSceneManager.OpenScene("Assets/0_0Scenes/Level" + stage + ".unity");
            var layout = UnityEngine.Object.FindAnyObjectByType<RandomLevelLayout>();
            Require(layout.platformTop != null && layout.platformFill != null, "Shared terrain references broke in Level" + stage);
        }
        Debug.Log("SHARED_SPRITE_REFERENCES_OK: rebuilding Tiny terrain preserves Level3 and Level5 references");
    }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Two-level validation timed out");
            if (Time.unscaledTime < next) return;
            if (checks.MoveNext()) next = Time.unscaledTime + checks.Current;
            else { Debug.Log("GAME_LEVELS_PLAYMODE_OK: 10 runs, fixed tutorial with varied fantasy enemies and 8 powers, 4 randomized stages verified different on replay, Multi Level4, Tiny Level5, reachable jumps, dog facing, guide and all transitions"); Finish(0); }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code) { SessionState.SetBool("GameLevelsValidation", false); Time.timeScale = 1; EditorApplication.Exit(code); }
    static IEnumerator<float> Checks()
    {
        var fingerprints = new Dictionary<int, string>();
        for (int run = 0; run < 10; run++)
        {
            yield return 0.5f;
            var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
            var layout = level.layout;
            var p = level.player;
            var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
            int stage = run % 5 + 1;
            Require(level.stageNumber == stage && SceneManager.GetActiveScene().name == (stage == 1 ? "Tutorial" : "Level" + stage), "Wrong stage after scene transition");
            Require(ui.GuideOpen && Time.timeScale == 0, "Every stage must open its guide with gameplay paused");
            bool tutorial = stage == 1;
            if (tutorial) Require(ui.stageTitleText.text == "Tutorial", "Tutorial title must not be numbered");
            int apples = UnityEngine.Object.FindObjectsByType<PowerUp>().Count(power => power.kind == PowerUp.Kind.Apple);
            int expectedPowers = tutorial ? 7 + apples : 3;
            if (!tutorial) Require(layout.AvailablePowerUps.Count == 3 && layout.AvailablePowerUps.Distinct().Count() == 3, "Generator must choose three distinct powers");
            else Require(layout == null && UnityEngine.Object.FindAnyObjectByType<RandomLevelLayout>() == null, "Tutorial must not be replaced by the generator");
            Require(UnityEngine.Object.FindObjectsByType<PowerUp>().Length == expectedPowers, "Wrong number of pickups for this stage");
            Require(apples <= 1, "Tutorial and generated levels may offer at most one apple");
            Require(ui.simpleGuideText != null ? ui.simpleGuideText.text.Contains("PODERES") && ui.simpleGuideText.text.Contains("R:")
                : ui.guideContent.GetComponentsInChildren<Image>().Count(i => i.name == "Icono") == (tutorial ? 8 : expectedPowers), "Tutorial explains every power, including the optional apple");
            var world = GameObject.Find(tutorial ? "Tutorial - Jungle" : "Procedural level " + stage);
            Require(world != null && (tutorial || GameObject.Find("Tutorial - Jungle") == null), "Wrong world for this stage");
            if (stage == 3 || stage == 4 || stage == 5)
            {
                var backgrounds = world.GetComponentsInChildren<SpriteRenderer>().Where(s => s.name.StartsWith("Forest background ")).ToArray();
                Require(backgrounds.Length >= 9 && backgrounds.All(s => s.sprite != null && s.GetComponent<JungleParallax>() != null && s.sortingOrder < -10), "Missing parallax forest background");
                Require(ui.stageObjectiveText.text.Contains(stage == 4 ? "prado" : "bosque"), "Stage objective text is incorrect");
            }
            var routes = world.GetComponentsInChildren<BoxCollider2D>().Where(c => tutorial ? c.name.StartsWith("Suelo ") : c.name.StartsWith("Route ")).OrderBy(c => c.bounds.min.x).ToArray();
            Require(routes.Length >= (tutorial ? 3 : 13), "Route is incomplete");
            string fingerprint = string.Join(";", routes.Select(c => c.bounds.ToString()));
            if (fingerprints.TryGetValue(stage, out var previous))
            {
                Require(tutorial ? previous == fingerprint : previous != fingerprint, "Tutorial must stay fixed while other stages change on replay");
                Debug.Log("LAYOUT_REPLAY_VERIFIED stage=" + stage + " changed=" + (!tutorial) + " platforms=" + routes.Length);
            }
            fingerprints[stage] = fingerprint;
            var goal = GameObject.Find(tutorial ? "Granero - meta" : "Level " + stage + " goal").GetComponent<SpriteRenderer>();
            var exitFloor = routes.SingleOrDefault(c => c.bounds.min.x < level.endX && c.bounds.max.x > level.endX);
            float foot = VisibleFoot(goal);
            Require(exitFloor != null && (tutorial ? foot <= exitFloor.bounds.max.y + 0.02f && foot >= exitFloor.bounds.max.y - 0.6f
                : Mathf.Abs(foot - exitFloor.bounds.max.y) < 0.07f), "Exit must rest on a real platform; perspective corners may sit inside the soil");
            foreach (var route in routes)
                if (!tutorial) Require(Mathf.Abs(route.transform.Find("Top").GetComponent<SpriteRenderer>().bounds.max.y - route.bounds.max.y) < 0.001f, "Visible ground must align with its collider");
            var enemies = world.GetComponentsInChildren<Enemy>();
            ValidateCombatPolish.CheckWorld(world, tutorial);
            Require(enemies.Length >= (tutorial ? 14 : 10), "Too few enemies for this stage: " + enemies.Length);
            if (!tutorial) Require(enemies.Any(e => e.flying) && enemies.Any(e => !e.ranged) && enemies.All(e => e.animator != null), "Meadow needs animated melee and flying enemies");
            foreach (var scenery in world.GetComponentsInChildren<SpriteRenderer>().Where(s => s.name == "Scenery"))
                foreach (var enemy in enemies)
                    Require(Mathf.Abs(scenery.bounds.center.x - enemy.transform.position.x) >= scenery.bounds.extents.x + enemy.patrolDistance + enemy.GetComponent<BoxCollider2D>().size.x / 2 + 0.69f, "Decoration overlaps an enemy patrol");
            ui.continueButton.onClick.Invoke();
            yield return 0.4f;
            Require(p.Grounded && p.transform.position.y > -5, "Player must start safely on the first platform");
            if (!tutorial)
            {
                var dog = enemies.First(e => e.spriteFacesLeft);
                foreach (int side in new[] { -1, 1 })
                {
                    p.transform.position = dog.transform.position + Vector3.right * (side * 2.2f);
                    p.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                    Physics2D.SyncTransforms();
                    yield return 0.08f;
                    Require(dog.visual.flipX == (side > 0), "Zombie dog must face the player on both sides");
                }
                p.Respawn();
            }
            foreach (var enemy in enemies) { enemy.enabled = false; enemy.GetComponent<Collider2D>().enabled = false; }
            foreach (var power in world.GetComponentsInChildren<PowerUp>()) power.gameObject.SetActive(false);
            foreach (var shot in UnityEngine.Object.FindObjectsByType<bullet>()) UnityEngine.Object.DestroyImmediate(shot.gameObject);
            Traverse(p, routes.Take(Array.IndexOf(routes, exitFloor) + 1).ToArray());
            p.transform.position = new Vector3(level.endX, exitFloor.bounds.max.y + 0.6f, 0);
            yield return 0.15f;
            Require(level.Completed && ui.completionPanel.activeSelf, "Reaching the exit must complete the stage");
            if (run < 9)
            {
                if (stage == 5 && level.nextSceneName == "Level6") SceneManager.LoadScene("Tutorial");
                else ui.restartButton.onClick.Invoke();
            }
        }
    }
    internal static void Traverse(player p, BoxCollider2D[] routes)
    {
        var fixedStep = typeof(player).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        var movement = typeof(player).GetField("movement", BindingFlags.Instance | BindingFlags.NonPublic);
        var body = p.GetComponent<Rigidbody2D>();
        var originalMode = Physics2D.simulationMode;
        p.enabled = false;
        Physics2D.simulationMode = SimulationMode2D.Script;
        try
        {
            for (int i = 0; i < routes.Length - 1; i++)
            {
                var from = routes[i].bounds; var to = routes[i + 1].bounds;
                body.position = new Vector2(from.max.x + 0.08f, from.max.y + 0.605f);
                body.linearVelocity = Vector2.zero; movement.SetValue(p, 0f); Physics2D.SyncTransforms();
                for (int step = 0; step < 3; step++) { fixedStep.Invoke(p, null); Physics2D.Simulate(0.02f); }
                p.RequestJump();
                bool landed = false;
                for (int step = 0; step < 100; step++)
                {
                    movement.SetValue(p, body.position.x < to.min.x + 0.5f ? 1f : 0f);
                    fixedStep.Invoke(p, null); Physics2D.Simulate(0.02f);
                    if (step > 8 && p.Grounded && body.position.x > to.min.x && body.position.y - 0.6f >= to.max.y - 0.1f) { landed = true; break; }
                }
                Require(landed, "Cannot jump from " + routes[i].name + " to " + routes[i + 1].name + "; player=" + body.position);
            }
        }
        finally { Physics2D.simulationMode = originalMode; movement.SetValue(p, 0f); p.enabled = true; }
    }
    static float VisibleFoot(SpriteRenderer renderer)
    {
        var sprite = renderer.sprite;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        var rect = sprite.rect;
        int bottom = (int)rect.yMax;
        for (int y = (int)rect.yMin; y < rect.yMax && bottom == (int)rect.yMax; y++)
        for (int x = (int)rect.xMin; x < rect.xMax; x++)
            if (texture.GetPixel(x, y).a > 0.1f) { bottom = y; break; }
        UnityEngine.Object.DestroyImmediate(texture);
        return renderer.transform.TransformPoint(new Vector3(0, (bottom - rect.yMin - sprite.pivot.y) / sprite.pixelsPerUnit, 0)).y;
    }
}
