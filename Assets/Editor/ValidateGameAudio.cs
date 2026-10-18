using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ValidateGameAudio
{
    static IEnumerator<float> checks;
    static float next;
    static double deadline;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static ValidateGameAudio()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("GameAudioValidation", false)) return;
            SceneManager.sceneLoaded += (scene, mode) => { var p = UnityEngine.Object.FindAnyObjectByType<player>(); if (p != null) p.persistUpgrades = false; };
            checks = Checks(); next = 0; deadline = EditorApplication.timeSinceStartup + 100;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        UnityEngine.Object.FindAnyObjectByType<player>().persistUpgrades = false;
        SessionState.SetBool("GameAudioValidation", true); EditorApplication.EnterPlaymode();
    }
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Audio validation timed out");
            if (Time.unscaledTime < next) return;
            if (checks.MoveNext()) next = Time.unscaledTime + checks.Current;
            else { Debug.Log("GAME_AUDIO_PLAYMODE_OK: bottom-left buttons, reset to ten hearts/one damage, tutorial apple ten-percent rolls, guaranteed collectible first Necromancer apple, effects 25% quieter with unchanged music, all audio clips and cues"); Finish(0); }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code) { SessionState.SetBool("GameAudioValidation", false); Time.timeScale = 1; EditorApplication.Exit(code); }
    static AudioSource Source(string name) => (AudioSource)typeof(GameAudio).GetField(name, Private).GetValue(GameAudio.Instance);
    static void CheckMusic(GameAudio.Sound sound)
    {
        Require(GameAudio.Instance.Music.clip == GameAudio.Instance.Clip(sound) && GameAudio.Instance.Music.loop, "Wrong music for this level");
        Require(UnityEngine.Object.FindObjectsByType<AudioListener>().Count(l => l.enabled) == 1, "Each scene must have one audio listener");
    }
    static IEnumerator<float> Checks()
    {
        yield return 0.5f;
        var audio = GameAudio.Instance;
        Require(audio != null, "Missing game audio manager");
        foreach (GameAudio.Sound sound in Enum.GetValues(typeof(GameAudio.Sound)))
            Require(audio.Clip(sound) != null && audio.Clip(sound).length > 0, "Missing clip: " + sound);
        Require(audio.Clip(GameAudio.Sound.FinalMusic).length <= 40.001f && audio.Clip(GameAudio.Sound.FinalMusic).length > 39.9f, "Final music must contain exactly its first 40 seconds");
        CheckMusic(GameAudio.Sound.EarlyMusic);
        var ui = TutorialCanvasUI.Instance;
        foreach (var button in new[] { ui.guideButton, ui.tutorialButton })
        {
            var rect = (RectTransform)button.transform;
            Require(rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.zero && rect.pivot == Vector2.zero && rect.anchoredPosition.y == 20 && rect.anchoredPosition.x > 0, "Gameplay buttons must be at the bottom left");
        }
        Require(Mathf.Approximately(audio.Music.volume, 0.3f), "Background music volume must remain unchanged");
        foreach (var expected in new[] { ("effects", 0.4875f), ("steps", 0.165f), ("breath", 0.21f), ("playerGun", 0.3f), ("hunterGun", 0.3f), ("magic", 0.3f), ("menu", 0.225f) })
            Require(Mathf.Approximately(Source(expected.Item1).volume, expected.Item2), "Effect must be 25% quieter: " + expected.Item1);
        Require(!ui.guideButton.GetComponentInChildren<Text>().text.Contains("H"), "Guide button must not display the H shortcut");
        Require(Source("menu").clip == audio.Clip(GameAudio.Sound.Menu), "Help should use the menu sound");
        BuildTutorialCanvas.ResetPlayerStats();
        Require(PlayerPrefs.GetInt(player.SavePrefix + "MaxHealth") == 10 && PlayerPrefs.GetInt(player.SavePrefix + "AttackDamage") == 1, "Reset must save ten hearts and one damage");
        Require(ui.player.maxHealth == 10 && ui.player.Health == 10 && ui.player.AttackDamage == 1, "Reset must also clear current Apple upgrades");
        foreach (var example in new[] { (5, 10), (15, 20), (1, 6) })
        {
            string slot = "GameJams.Validation.BaseHealth." + Guid.NewGuid().ToString("N") + ".";
            PlayerPrefs.SetInt(slot + "MaxHealth", example.Item1); PlayerPrefs.SetInt(slot + "AttackDamage", 3);
            for (int visit = 0; visit < 2; visit++)
            {
                var parent = new GameObject("Base health validation"); parent.SetActive(false);
                var savedPlayer = UnityEngine.Object.Instantiate(ui.player, parent.transform);
                savedPlayer.saveSlot = slot; savedPlayer.persistUpgrades = true;
                parent.SetActive(true);
                Require(savedPlayer.maxHealth == example.Item2 && savedPlayer.Health == example.Item2 && savedPlayer.AttackDamage == 3, "Old saves must gain five base hearts once, keeping Apple damage and previous penalties");
                UnityEngine.Object.DestroyImmediate(parent);
            }
            PlayerPrefs.DeleteKey(slot + "MaxHealth"); PlayerPrefs.DeleteKey(slot + "AttackDamage"); PlayerPrefs.DeleteKey(slot + "BaseHealth");
        }
        var rollState = UnityEngine.Random.state;
        var appleObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/0_2prefafs/PowerUpCapsule.prefab"), new Vector3(0, 99, 0), Quaternion.identity);
        var testApple = appleObject.GetComponent<PowerUp>(); testApple.kind = PowerUp.Kind.Apple; testApple.pickupId = "validation.tutorial.probability";
        var startPickup = typeof(PowerUp).GetMethod("Start", Private);
        UnityEngine.Random.InitState(718);
        int appleSpawns = 0;
        for (int i = 0; i < 400; i++)
        {
            var beforeRoll = UnityEngine.Random.state;
            bool shouldSpawn = UnityEngine.Random.value < 0.1f;
            UnityEngine.Random.state = beforeRoll;
            appleObject.SetActive(true); startPickup.Invoke(testApple, null);
            Require(appleObject.activeSelf == shouldSpawn, "Tutorial Apple must use a ten-percent spawn chance");
            if (appleObject.activeSelf) appleSpawns++;
        }
        UnityEngine.Object.DestroyImmediate(appleObject); UnityEngine.Random.state = rollState;
        Require(appleSpawns > 20 && appleSpawns < 60, "Tutorial spawn sample must be near ten percent");
        Debug.Log("TUTORIAL_APPLE_ROLLS_OK: " + appleSpawns + "/400");
        ui.SetGuide(false); yield return 0.1f;
        var p = ui.player; p.enabled = false; p.persistUpgrades = false;
        var playerBody = p.GetComponent<Rigidbody2D>();
        playerBody.position = new Vector2(-8, -3.39f); playerBody.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        int jumpCues = audio.CueCount;
        p.RequestJump(); typeof(player).GetMethod("FixedUpdate", Private).Invoke(p, null);
        Require(audio.CueCount == jumpCues + 1 && audio.LastEffectClip == audio.Clip(GameAudio.Sound.PlayerJump), "An actual jump must play the new jump sound");
        p.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>()) enemy.gameObject.SetActive(false);
        Require(p.TryShoot() && Source("playerGun").clip == audio.Clip(GameAudio.Sound.NormalShot) && !Source("playerGun").loop, "Normal player shots must use their new sound without overlapping copies");
        foreach (string name in new[] { "EnemyMushroom", "EnemyZombieDog", "EnemyFlyingEye", "EnemyFantasySkeleton" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/0_2prefafs/" + name + ".prefab");
            var enemy = UnityEngine.Object.Instantiate(prefab).GetComponent<Enemy>();
            audio.EnemyAttack(enemy);
            Require(audio.LastEffectClip == audio.Clip(GameAudio.Sound.Melee), "Enemies without a custom attack must use the sword sound");
            audio.EnemyDeath(enemy);
            var expected = name == "EnemyFlyingEye" ? GameAudio.Sound.EyeDeath : name == "EnemyFantasySkeleton" ? GameAudio.Sound.SkeletonDeath : GameAudio.Sound.GoblinDeath;
            Require(audio.LastEffectClip == audio.Clip(expected), "Preserve native death sounds, otherwise use Goblin death");
            enemy.attackSound = enemy.deathSound = audio.Clip(GameAudio.Sound.PlayerJump);
            audio.EnemyAttack(enemy); Require(audio.LastEffectClip == enemy.attackSound, "Custom attack sound must take precedence");
            audio.EnemyDeath(enemy); Require(audio.LastEffectClip == enemy.deathSound, "Custom death sound must take precedence");
            UnityEngine.Object.DestroyImmediate(enemy.gameObject);
        }
        int cues = audio.CueCount;
        foreach (PowerUp.Kind kind in Enum.GetValues(typeof(PowerUp.Kind))) p.ApplyPowerUp(kind, "audio." + kind);
        Require(audio.CueCount == cues + 8, "Each pickup must play one sound");
        p.ApplyPowerUp(PowerUp.Kind.Shirt, "audio.minigun");
        Require(p.TryShoot() && Source("playerGun").clip == audio.Clip(GameAudio.Sound.HunterShot), "Minigun must use the Hunter firing clip");
        p.ApplyPowerUp(PowerUp.Kind.Rock, "audio.stones"); cues = audio.CueCount;
        Require(p.TryShoot() && audio.CueCount == cues + 1, "Throwing a stone must play its sound");
        ui.SetGuide(true); yield return 0.05f;
        Require((bool)typeof(GameAudio).GetField("paused", Private).GetValue(audio), "Guide must pause gameplay audio");
        cues = audio.CueCount; audio.Cue(GameAudio.Sound.RockThrow); Require(audio.CueCount == cues, "Gameplay effects must stay silent while paused");
        foreach (int stage in new[] { 5, 6, 7, 8 })
        {
            SceneManager.LoadScene("Level" + stage); yield return 0.5f;
            Require(!TutorialCanvasUI.Instance.GetComponentsInChildren<Button>(true).Any(b => b.name == "Restablecer valores"), "Stat reset must exist only in the tutorial's initial menu");
            CheckMusic(stage == 8 ? GameAudio.Sound.FinalMusic : stage == 5 ? GameAudio.Sound.LateMusic : GameAudio.Sound.NecromancerMusic);
            if (stage == 6)
            {
                var firstNecromancerStage = UnityEngine.Object.FindAnyObjectByType<FinalCampaignStage>();
                Require(!UnityEngine.Object.FindObjectsByType<PowerUp>().Any(pickup => pickup.pickupId == "Level6.NecromancerReward"), "Boss reward must wait until the first Necromancer dies");
                var hero = TutorialCanvasUI.Instance.player; hero.enabled = false; hero.persistUpgrades = false;
                hero.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                TutorialCanvasUI.Instance.SetGuide(false);
                for (int hit = 0; hit < 30 && !firstNecromancerStage.Boss.Defeated; hit++) firstNecromancerStage.Boss.TakeDamage(999);
                yield return 1.25f;
                var reward = UnityEngine.Object.FindObjectsByType<PowerUp>().Single(pickup => pickup.pickupId == "Level6.NecromancerReward");
                Require(reward.kind == PowerUp.Kind.Apple && reward.icon.sprite == firstNecromancerStage.powerUpIcons[(int)PowerUp.Kind.Apple], "Boss must always leave a visible Apple");
                Require(firstNecromancerStage.Portals.Count == 2 && reward.transform.position.x < firstNecromancerStage.Portals.Min(portal => portal.transform.position.x), "Reward must be before both exits");
                Require(!Physics2D.OverlapBoxAll(reward.transform.position, new Vector2(0.8f, 0.9f), 0).Any(c => !c.isTrigger && c.attachedRigidbody == null), "Reward must not spawn inside a platform");
                firstNecromancerStage.BossDefeated();
                Require(UnityEngine.Object.FindObjectsByType<PowerUp>().Count(pickup => pickup.pickupId == reward.pickupId) == 1, "Boss reward must not duplicate");
                int maxBeforeReward = hero.maxHealth, damageBeforeReward = hero.AttackDamage;
                typeof(PowerUp).GetMethod("OnTriggerEnter2D", Private).Invoke(reward, new object[] { hero.GetComponent<Collider2D>() });
                Require(!reward.gameObject.activeSelf && hero.maxHealth == maxBeforeReward + 10 && hero.AttackDamage == damageBeforeReward + 1, "Boss reward must actually grant Apple's health and damage");
            }
        }
        ui = TutorialCanvasUI.Instance; ui.SetGuide(false); yield return 0.1f;
        var campaign = UnityEngine.Object.FindAnyObjectByType<FinalCampaignStage>();
        campaign.Boss.Activate();
        int bossCues = GameAudio.Instance.CueCount;
        GameAudio.Instance.EnemyDeath(campaign.Boss.GetComponent<Enemy>());
        GameAudio.Instance.EnemyAttack(campaign.Boss.GetComponent<Enemy>());
        Require(GameAudio.Instance.CueCount == bossCues, "Bosses must never use the ordinary enemy fallback sounds");
        Require(Source("breath").clip == GameAudio.Instance.Clip(GameAudio.Sound.HunterBreath) && Source("breath").loop, "Hunter must loop the breathing sound");
        typeof(BossController).GetMethod("Shoot", Private).Invoke(campaign.Boss, new object[] { Vector2.left, 10f, 1 });
        Require(Source("hunterGun").clip == GameAudio.Instance.Clip(GameAudio.Sound.HunterShot), "Hunter must use its firing sound");
        campaign.Boss.ResetEncounter();
        Require(!Source("breath").isPlaying && !Source("hunterGun").isPlaying, "Reset must stop Hunter breathing and fire");
    }
}
