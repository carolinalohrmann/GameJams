using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ValidateFinalCampaign
{
    static IEnumerator<float> checks;
    static float next;
    static double deadline;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static ValidateFinalCampaign()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("FinalCampaignValidation", false)) return;
            SceneManager.sceneLoaded += (scene, mode) => { var p = UnityEngine.Object.FindAnyObjectByType<player>(); if (p != null) p.persistUpgrades = false; };
            checks = Checks(); next = 0; deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        Require(EditorBuildSettings.scenes.Count(s => s.enabled) == 8, "Eight scenes must be included");
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Level5.unity");
        UnityEngine.Object.FindAnyObjectByType<player>().persistUpgrades = false;
        SessionState.SetBool("FinalCampaignValidation", true); EditorApplication.EnterPlaymode();
    }
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Final campaign validation timed out");
            if (Time.unscaledTime < next) return;
            if (checks.MoveNext()) next = Time.unscaledTime + checks.Current;
            else { Debug.Log("FINAL_CAMPAIGN_PLAYMODE_OK: Space-only jump input, real 10s/15s weapon expiry, stronger/faster Necromancers on levels 6/7, left-only skeletons, no minigun on levels 7/8, no AK on level 8, exact normal/secret ending messages, safe layouts and Hunter barrage"); Finish(0); }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code) { SessionState.SetBool("FinalCampaignValidation", false); Time.timeScale = 1; EditorApplication.Exit(code); }
    static TutorialCanvasUI UI => TutorialCanvasUI.Instance;
    static FinalCampaignStage Stage => UnityEngine.Object.FindAnyObjectByType<FinalCampaignStage>();
    static void Prepare()
    {
        UI.SetGuide(false); var p = UI.player; p.persistUpgrades = false; p.enabled = false;
        p.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        p.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>())
            if (enemy.GetComponent<BossController>() == null) { enemy.enabled = false; enemy.GetComponent<Collider2D>().enabled = false; }
    }
    static void Warp(Vector2 point)
    {
        UI.player.transform.position = point; UI.player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
    }
    static string CheckLayout()
    {
        var stage = Stage; var level = stage.Level;
        Require(level.manualCompletion && !level.Completed && !stage.ExitsUnlocked && stage.Portals.Count == 0, "Exits must remain locked before defeating the boss");
        var boss = stage.Boss;
        Require(boss != null && boss.Health == boss.maximumHealth && boss.maximumHealth >= 180, "Missing high-health boss");
        Require(UnityEngine.Object.FindObjectsByType<Enemy>().Where(e => e.GetComponent<BossController>() == null && e.GetComponent<BossSummon>() == null).All(e => e.health == (level.stageNumber >= 7 ? 10 : 6)), "Common enemy health must be six normally and ten in secret levels");
        foreach (var clip in boss.GetComponent<Enemy>().animator.runtimeAnimatorController.animationClips)
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            Require(AnimationUtility.GetObjectReferenceCurve(clip, binding).All(f => f.value != null), "Boss has missing animation sprites");
        var world = GameObject.Find(level.stageNumber == 8 ? "Hunter fixed arena" : "Procedural level " + level.stageNumber);
        Require(world != null, "Missing campaign world");
        int apples = world.GetComponentsInChildren<PowerUp>().Count(p => p.kind == PowerUp.Kind.Apple);
        Require(level.stageNumber == 8 ? apples == 1 : apples <= 1, "Fixed arena keeps its apple; randomized finals offer at most one");
        var platforms = world.GetComponentsInChildren<BoxCollider2D>().Where(c => !c.isTrigger && c.attachedRigidbody == null).ToArray();
        foreach (var actor in world.GetComponentsInChildren<Enemy>().Select(e => e.GetComponent<Collider2D>()).Concat(world.GetComponentsInChildren<PowerUp>().Select(p => p.GetComponent<Collider2D>())).Append(boss.GetComponent<Collider2D>()))
        foreach (var platform in platforms)
            Require(actor.Distance(platform).distance >= -0.001f, actor.name + " intersects " + platform.name);
        if (level.stageNumber == 7)
        {
            var enemies = world.GetComponentsInChildren<Enemy>();
            Require(enemies.Length >= 10 && enemies.All(e => e.health == 10 && e.contactDamage == 2 && e.projectileDamage == 2), "Secret enemies must have more health and damage");
            Require(world.GetComponentsInChildren<GlitchDrift>().Length >= 4 && level.layout.mixedPlatformTops.Length == 4, "Glitch world must mix four art packs");
            Require(boss.maximumHealth == 400, "Second Necromancer must have 400 health");
        }
        if (level.stageNumber == 6) Require(AssetDatabase.GetAssetPath(level.layout.platformTop).Contains("Final/Tiles"), "Level6 must use Final.zip");
        if (level.stageNumber == 8) Require(level.layout == null && platforms.Length == 10 && level.rightEdge - level.leftEdge <= 45, "Hunter level must be short and fixed with platforms");
        if (level.stageNumber == 8)
            foreach (var kind in new[] { PowerUp.Kind.Citrus, PowerUp.Kind.Feather })
                Require(world.GetComponentsInChildren<PowerUp>().Count(p => p.kind == kind) == 1, "Hunter arena must always offer Citrus and Feather");
        Require(world.GetComponentsInChildren<PowerUp>(true).All(p => PowerUp.IsAvailableInStage(p.kind, level.stageNumber)), "Forbidden weapons must not be generated or left in the authored arena");
        if (level.stageNumber >= 7)
            Require(level.layout != null ? !level.layout.AvailablePowerUps.Contains(PowerUp.Kind.Shirt)
                : !stage.fixedPowerUps.Contains(PowerUp.Kind.Shirt) && !stage.fixedPowerUps.Contains(PowerUp.Kind.Chicken), "Guide power list must exclude forbidden weapons");
        return string.Join(";", platforms.OrderBy(p => p.name).Select(p => p.name + p.bounds.ToString()));
    }
    static IEnumerable<float> CheckSummons()
    {
        var boss = Stage.Boss;
        for (int cycle = 0; cycle < 3; cycle++)
        {
            boss.StartCoroutine((IEnumerator)typeof(BossController).GetMethod("Summon", Private).Invoke(boss, null));
            yield return 0.55f;
            var summons = UnityEngine.Object.FindObjectsByType<BossSummon>();
            Require(summons.Length >= 1 && summons.Length <= 3, "Necromancer must summon skeletons with a maximum of three alive");
            Require(summons.All(s => s.transform.position.x < boss.transform.position.x && s.GetComponent<Enemy>().health == 10 && s.GetComponent<Enemy>().contactDamage == (Stage.Level.stageNumber == 7 ? 8 : 3)), "Both Necromancers must summon stronger skeletons only to their left");
            foreach (var summon in summons)
            foreach (var solid in UnityEngine.Object.FindObjectsByType<BoxCollider2D>().Where(c => !c.isTrigger && c.attachedRigidbody == null))
                Require(summon.GetComponent<Collider2D>().Distance(solid).distance >= -0.001f, "Summon spawned inside terrain");
        }
        boss.ResetEncounter();
    }
    static void Traverse()
    {
        var level = Stage.Level; var p = UI.player;
        var routes = GameObject.Find("Procedural level " + level.stageNumber).GetComponentsInChildren<BoxCollider2D>()
            .Where(c => c.name.StartsWith("Route ")).OrderBy(c => c.bounds.min.x).ToArray();
        var body = p.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Dynamic;
        ValidateGameLevels.Traverse(p, routes);
        p.enabled = false; body.bodyType = RigidbodyType2D.Kinematic; Warp(new Vector2(-8, -3));
    }
    static void DefeatBoss()
    {
        var boss = Stage.Boss;
        int guard = 100;
        while (!boss.Defeated && guard-- > 0) boss.GetComponent<Enemy>().Damage(999);
        Require(boss.Defeated && boss.Health == 0, "Boss did not die");
    }
    static void CheckCredits()
    {
        var credits = UI.completionPanel.GetComponentsInChildren<Text>().Single(t => t.name == "Creditos finales");
        foreach (string required in new[] { "Diseño y elección de sprays", "Elección de la música", "Código", "Carolina Lohrmann", "Kevin Illanes", "itch.io", "Miro Haverinen", "tres canciones de Fear & Hunger", "¡Gracias por jugar!" })
            Require(credits.text.Contains(required), "Missing ending credit: " + required);
        Canvas.ForceUpdateCanvases();
        Require(credits.preferredHeight <= credits.rectTransform.rect.height, "Ending credits must fit without clipped lines");
        var button = (RectTransform)UI.restartButton.transform;
        Require(-button.anchoredPosition.y >= -credits.rectTransform.anchoredPosition.y + credits.rectTransform.sizeDelta.y + 16, "Ending button must sit below the credits");
    }
    static void CheckJumpInput()
    {
        var p = UI.player;
        var previousKeyboard = Keyboard.current;
        var previousUpdateMode = InputSystem.settings.updateMode;
        var previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        var previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var buffer = typeof(player).GetField("jumpBuffer", Private);
        try
        {
            foreach (var key in new[] { Key.W, Key.UpArrow, Key.Space })
            {
                buffer.SetValue(p, 0f);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                _ = keyboard.spaceKey.wasPressedThisFrame;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
                keyboard.MakeCurrent();
                typeof(player).GetMethod("Update", Private).Invoke(p, null);
                Require(((float)buffer.GetValue(p) > Time.time) == (key == Key.Space), "Only Space may request a jump; tested " + key + ", pressed=" + keyboard[key].wasPressedThisFrame + ", paused=" + TutorialCanvasUI.BlocksGameplay);
            }
        }
        finally
        {
            buffer.SetValue(p, 0f); InputSystem.RemoveDevice(keyboard);
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            InputSystem.settings.updateMode = previousUpdateMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
        }
    }
    static IEnumerable<float> CheckNecromancerBalance()
    {
        var boss = Stage.Boss; var p = UI.player;
        Stage.enabled = false;
        p.ResetAppleUpgrades(); p.ApplyPowerUp(PowerUp.Kind.Apple, "test.necro.damage");
        typeof(player).GetField("immunityUntil", Private).SetValue(p, 0f);
        typeof(player).GetField("invincibleUntil", Private).SetValue(p, 0f);
        int bonus = Stage.Level.stageNumber == 7 ? 5 : 0;
        p.ApplyPowerUp(PowerUp.Kind.Apple, "test.necro.damage.extra");
        int health = p.Health; boss.Contact(p);
        Require(p.Health == health - (4 + bonus), "Necromancer contact damage must be 4 on level 6 and 9 on level 7");
        Warp(new Vector2(-8, -3.4f));
        boss.StartCoroutine((IEnumerator)typeof(BossController).GetMethod("FanVolley", Private).Invoke(boss, null));
        yield return 0.55f;
        var shots = UnityEngine.Object.FindObjectsByType<CampaignAttack>().Select(a => a.GetComponent<bullet>()).ToArray();
        Require(shots.Length >= 5, "Necromancer volley must start faster than the old 0.6-second windup");
        Require(shots.All(s => Mathf.Abs(s.GetComponent<Rigidbody2D>().linearVelocity.magnitude - 6.5f) < 0.001f), "Necromancer projectiles must travel faster");
        typeof(player).GetField("invincibleUntil", Private).SetValue(p, 0f);
        health = p.Health;
        typeof(bullet).GetMethod("OnTriggerEnter2D", Private).Invoke(shots[0], new object[] { p.GetComponent<Collider2D>() });
        Require(p.Health == health - (3 + bonus), "Necromancer projectile damage must be 3 on level 6 and 8 on level 7");
        boss.StopAllCoroutines(); Stage.ClearAttacks(); yield return 0.05f;
        var ritual = (IEnumerator)typeof(BossController).GetMethod("Ritual", Private).Invoke(boss, new object[] { false });
        Require(ritual.MoveNext(), "Necromancer must cast its ritual");
        var marks = UnityEngine.Object.FindObjectsByType<CampaignHazard>();
        Require(marks.Length == 3 && marks.All(m => !m.lethal && Mathf.Approximately(m.warningSeconds, 0.85f)), "Ritual must use faster warnings");
        yield return 0.95f;
        typeof(player).GetField("invincibleUntil", Private).SetValue(p, 0f);
        health = p.Health; marks[0].Hit(p);
        Require(p.Health == health - (4 + bonus), "Necromancer ritual damage must be 4 on level 6 and 9 on level 7");
        boss.ResetEncounter(); Stage.ClearAttacks(); yield return 0.05f;
    }
    static IEnumerator<float> Checks()
    {
        yield return 0.5f;
        Prepare(); var previous = UI.level;
        CheckJumpInput();
        var weaponPlayer = UI.player;
        weaponPlayer.ApplyPowerUp(PowerUp.Kind.Shirt, "test.ten.second.minigun");
        weaponPlayer.ApplyPowerUp(PowerUp.Kind.Chicken, "test.fifteen.second.ak");
        Require(weaponPlayer.MinigunRemaining > 9.9f && weaponPlayer.MinigunRemaining <= 10 && weaponPlayer.AKRemaining > 14.9f && weaponPlayer.AKRemaining <= 15, "Weapon durations must be ten and fifteen seconds");
        yield return 10.1f; weaponPlayer.UpdateWeaponVisual();
        Require(weaponPlayer.MinigunRemaining == 0 && weaponPlayer.WeaponName == "AK", "Minigun must expire after ten seconds and reveal the remaining AK");
        yield return 5.1f; weaponPlayer.UpdateWeaponVisual();
        Require(!weaponPlayer.HasAK && weaponPlayer.WeaponName == "Pistola", "AK must expire after fifteen seconds");
        Require(previous.nextSceneName == "Level6", "Level5 must continue to Level6");
        Warp(new Vector2(previous.endX, -2.8f)); yield return 0.1f;
        Require(previous.Completed, "Level5 cannot complete");
        UI.restartButton.onClick.Invoke(); yield return 0.6f;
        Require(SceneManager.GetActiveScene().name == "Level6" && UI.GuideOpen, "Level6 must load its own guide");
        string first = CheckLayout(); Prepare(); Traverse();
        var boss = Stage.Boss;
        Warp(new Vector2(Stage.Level.endX, -3.4f)); yield return 0.05f;
        Require(!Stage.Level.Completed, "Walking past the boss must not complete Level6");
        Warp(new Vector2(-8, -3)); Stage.enabled = false;
        boss.GetComponent<Enemy>().Damage(999);
        Require(boss.Health == boss.maximumHealth - 20 && !Stage.ExitsUnlocked, "A single stone must not instantly kill a boss");
        boss.ResetEncounter(); Stage.ClearAttacks();
        foreach (float wait in CheckNecromancerBalance()) yield return wait;
        UI.player.ApplyPowerUp(PowerUp.Kind.Resistance, "test.protection", 100);
        Warp(new Vector2(boss.transform.position.x - 8, boss.transform.position.y + 1));
        boss.StartCoroutine((IEnumerator)typeof(BossController).GetMethod("FanVolley", Private).Invoke(boss, null));
        yield return 1.1f;
        Require(UnityEngine.Object.FindObjectsByType<CampaignAttack>().Length >= 5, "Necromancer must fire a fan of projectiles");
        Stage.ClearAttacks(); boss.ResetEncounter(); Warp(new Vector2(-8, -3.4f));
        foreach (float wait in CheckSummons()) yield return wait;
        int health = UI.player.Health;
        var lethal = CampaignHazard.CreateRitual(Stage, boss.ritualSprite, -8, 0.8f, true);
        lethal.Hit(UI.player);
        Require(!UI.player.Dying && UI.player.Health == health && !lethal.Armed, "Ritual must warn before doing damage");
        yield return 1;
        Require(UI.player.Dying && UI.player.Health == 0, "Lethal ritual must ignore hearts and resistance after warning");
        UI.player.Respawn(); Stage.ClearAttacks();
        Require(boss.Health == boss.maximumHealth && !boss.Active && !Stage.ExitsUnlocked, "Death/respawn must reset an unfinished boss");
        DefeatBoss(); yield return 1.25f;
        Require(Stage.Portals.Count == 2 && Stage.ExitsUnlocked, "First Necromancer must unlock exactly two exits");
        var normal = Stage.Portals.Single(p => string.IsNullOrEmpty(p.destination));
        Require(AssetDatabase.GetAssetPath(normal.GetComponent<SpriteRenderer>().sprite).EndsWith("Torre.png"), "Normal exit must use Torre.png");
        Warp(new Vector2(normal.transform.position.x, -3.4f)); Require(normal.Enter(UI.player), "Normal ending gate cannot be selected");
        yield return 0.05f;
        Require(Stage.Level.Completed && UI.completionTitleText.text == "Final normal" && SceneManager.GetActiveScene().name == "Level6", "Tower must show the normal ending");
        Require(UI.completionDescriptionText.text == "Tener miedo a lo desconocido es normal. Disfruta tu mediocridad.", "Normal ending must display the requested message");
        CheckCredits();
        SceneManager.LoadScene("Level6"); yield return 0.6f;
        string replay = CheckLayout(); Require(first != replay, "Level6 must change on replay"); Prepare(); Stage.enabled = false;
        DefeatBoss(); yield return 1.25f;
        var secret = Stage.Portals.Single(p => p.destination == "Level7");
        Warp(new Vector2(secret.transform.position.x, -3.4f)); Require(secret.Enter(UI.player), "Secret exit cannot be selected");
        yield return 0.6f;
        Require(SceneManager.GetActiveScene().name == "Level7", "Secret portal must load Level7");
        string glitch = CheckLayout(); Prepare(); Stage.enabled = false; Traverse();
        foreach (float wait in CheckNecromancerBalance()) yield return wait;
        foreach (float wait in CheckSummons()) yield return wait;
        UI.player.ApplyPowerUp(PowerUp.Kind.Apple, "test.extrahearts");
        UI.player.ApplyPowerUp(PowerUp.Kind.Resistance, "test.meteorimmunity", 100);
        Warp(new Vector2(-8, -3.4f));
        var meteor = CampaignHazard.CreateMeteor(Stage, -8, -4);
        meteor.Hit(UI.player); Require(!UI.player.Dying && !meteor.Armed, "Meteor telegraph must not deal premature damage");
        yield return 2;
        Require(UI.player.Dying && UI.player.Health == 0, "Meteor must kill even with extra hearts and immunity");
        UI.player.Respawn(); Stage.ClearAttacks();
        DefeatBoss(); yield return 1.25f;
        Require(Stage.Portals.Count == 1 && Stage.Portals[0].destination == "Level8", "Second boss must unlock the tower to the Hunter");
        var tower = Stage.Portals[0]; Warp(new Vector2(tower.transform.position.x, -3.4f)); tower.Enter(UI.player);
        yield return 0.6f;
        Require(SceneManager.GetActiveScene().name == "Level8", "Tower must load Level8");
        string fixedLayout = CheckLayout(); Prepare(); Stage.enabled = false; boss = Stage.Boss;
        Require(boss.hunter && boss.maximumHealth == 666, "Fixed arena must contain the high-health Hunter");
        var arena = GameObject.Find("Hunter fixed arena").transform;
        var approach = new GameObject("Test floor approach").AddComponent<BoxCollider2D>();
        approach.transform.position = new Vector3(-5.3f, -4.25f, 0); approach.size = new Vector2(1, 0.5f);
        var playerBody = UI.player.GetComponent<Rigidbody2D>(); playerBody.bodyType = RigidbodyType2D.Dynamic;
        try
        {
            var ledges = arena.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name.StartsWith("Arena ledge ")).OrderBy(c => c.bounds.min.x).ToArray();
            ValidateGameLevels.Traverse(UI.player, new[] { approach }.Concat(ledges).ToArray());
        }
        finally { UnityEngine.Object.DestroyImmediate(approach.gameObject); playerBody.bodyType = RigidbodyType2D.Kinematic; UI.player.enabled = false; }
        UI.player.ApplyPowerUp(PowerUp.Kind.Resistance, "test.hunterprotection", 100);
        Warp(new Vector2(boss.transform.position.x - 7, boss.transform.position.y + 1.5f));
        boss.StartCoroutine((IEnumerator)typeof(BossController).GetMethod("HunterBurst", Private).Invoke(boss, new object[] { 0 }));
        yield return 1.65f;
        var hunterShots = UnityEngine.Object.FindObjectsByType<CampaignAttack>().Select(s => s.GetComponent<bullet>()).ToArray();
        Require(hunterShots.Length >= 5, "Hunter must fire rapid minigun-like bursts");
        Require(hunterShots.All(s => s.rapidFrames.Contains(s.GetComponent<SpriteRenderer>().sprite)), "Hunter must use animated effect008");
        Stage.ClearAttacks(); boss.ResetEncounter();
        boss.Contact(UI.player);
        Require(UI.player.Dying && UI.player.Health == 0, "Hunter contact must kill instantly even with active resistance");
        UI.player.Respawn(); Stage.ClearAttacks();
        DefeatBoss(); yield return 1.25f;
        Require(Stage.Level.Completed && UI.completionTitleText.text == "Felicidades, eres housestatico" && UI.completionPanel.activeSelf, "Hunter must unlock the exact secret ending message");
        CheckCredits();
        SceneManager.LoadScene("Level7"); yield return 0.6f;
        Require(CheckLayout() != glitch, "Glitch route must change on replay");
        SceneManager.LoadScene("Level8"); yield return 0.6f;
        Require(CheckLayout() == fixedLayout, "Hunter arena must remain fixed on replay");
    }
}
