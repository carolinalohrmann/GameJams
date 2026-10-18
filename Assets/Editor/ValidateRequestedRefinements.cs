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
public static class ValidateRequestedRefinements
{
    static IEnumerator<float> checks;
    static float next;
    static double deadline;
    static string testSave;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static TutorialCanvasUI UI => TutorialCanvasUI.Instance;
    static FinalCampaignStage Stage => UnityEngine.Object.FindAnyObjectByType<FinalCampaignStage>();
    static ValidateRequestedRefinements()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("RequestedRefinementValidation", false)) return;
            SceneManager.sceneLoaded += (scene, mode) => { var p = UnityEngine.Object.FindAnyObjectByType<player>(); if (p != null) p.persistUpgrades = false; };
            checks = Checks(); next = 0; deadline = EditorApplication.timeSinceStartup + 100;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Level6.unity");
        UnityEngine.Object.FindAnyObjectByType<player>().persistUpgrades = false;
        SessionState.SetBool("RequestedRefinementValidation", true); EditorApplication.EnterPlaymode();
    }
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Refinement validation timeout");
            if (Time.unscaledTime < next) return;
            if (checks.MoveNext()) next = Time.unscaledTime + checks.Current;
            else { Debug.Log("REQUESTED_REFINEMENTS_PLAYMODE_OK: illustrated guide, repeat-visit apples, moving/casting Necromancer, one-second player shots, five-heart manual/fall/death penalties with minimum one, tutorial confirmation/cancel/pause, E only for two doors, automatic single-door entry, Hunter meteors and six-damage bullets"); Finish(0); }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code)
    {
        if (testSave != null) { PlayerPrefs.DeleteKey(testSave + "MaxHealth"); PlayerPrefs.DeleteKey(testSave + "AttackDamage"); PlayerPrefs.Save(); }
        SessionState.SetBool("RequestedRefinementValidation", false); Time.timeScale = 1; EditorApplication.Exit(code);
    }
    static player Prepare()
    {
        UI.SetGuide(false); var p = UI.player; p.persistUpgrades = false; p.enabled = false;
        p.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>())
            if (enemy.GetComponent<BossController>() == null) { enemy.enabled = false; enemy.GetComponent<Collider2D>().enabled = false; }
        Stage.enabled = false; return p;
    }
    static void Defeat()
    {
        for (int i = 0; i < 30 && !Stage.Boss.Defeated; i++) Stage.Boss.TakeDamage(999);
    }
    static void Warp(Vector2 point) { UI.player.transform.position = point; UI.player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero; Physics2D.SyncTransforms(); }
    static IEnumerator<float> Checks()
    {
        yield return 0.5f;
        Require(UI.simpleGuideText == null && UI.guideContent.GetComponentsInChildren<Image>().Count(i => i.name == "Icono") == 3, "Restore the guide with power icons and descriptions");
        var originalScene = SceneManager.GetActiveScene().handle;
        int originalMaximum = UI.player.maxHealth;
        UI.SetGuide(false);
        UI.tutorialButton.onClick.Invoke();
        Require(UI.ReturnConfirmationOpen && Time.timeScale == 0 && TutorialCanvasUI.BlocksGameplay, "Return button must pause and request confirmation");
        Require(SceneManager.GetActiveScene().handle == originalScene, "Opening confirmation must not reload or change the scene");
        var cancel = UI.GetComponentsInChildren<Button>().Single(b => b.name == "Cancelar");
        Require(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == cancel.gameObject, "Cancel should receive initial keyboard focus");
        cancel.onClick.Invoke();
        Require(!UI.ReturnConfirmationOpen && !UI.GuideOpen && Time.timeScale == 1 && !TutorialCanvasUI.BlocksGameplay, "Cancel must resume gameplay");
        UI.SetGuide(true); UI.guideTutorialButton.onClick.Invoke();
        cancel.onClick.Invoke();
        Require(UI.GuideOpen && !UI.ReturnConfirmationOpen && Time.timeScale == 0, "Cancelling from the guide must keep the guide paused");
        Require(UI.player.maxHealth == originalMaximum && SceneManager.GetActiveScene().handle == originalScene, "Cancel must preserve health and the current scene");
        var p = Prepare(); var boss = Stage.Boss;
        Warp(new Vector2(boss.transform.position.x - 7, -3.4f));
        p.ApplyPowerUp(PowerUp.Kind.Resistance, "motiontest", 100);
        boss.Activate(); float min = boss.transform.position.x, max = min, castMin = float.MaxValue, castMax = float.MinValue;
        for (int i = 0; i < 10; i++)
        {
            yield return 0.25f;
            float x = boss.transform.position.x; min = Mathf.Min(min, x); max = Mathf.Max(max, x);
            if (boss.GetComponent<Enemy>().animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) { castMin = Mathf.Min(castMin, x); castMax = Mathf.Max(castMax, x); }
            Require(x >= boss.arenaLeft + 0.9f && x <= boss.arenaRight - 0.9f, "Boss escaped arena");
        }
        Require(max - min > 0.6f && castMax - castMin > 0.2f, "Necromancer must move while fighting and casting");
        boss.ResetEncounter(); Stage.ClearAttacks(); Warp(new Vector2(-8, -3.4f));
        var source = p.GetComponent<Collider2D>();
        var normal = UnityEngine.Object.Instantiate(p.projectile, new Vector3(0, 5, 0), Quaternion.identity); normal.Launch(Vector2.right, source, false, 0); normal.UsePlayerEffect(false);
        var rapid = UnityEngine.Object.Instantiate(p.projectile, new Vector3(2, 5, 0), Quaternion.identity); rapid.Launch(Vector2.right, source, false, 0); rapid.UsePlayerEffect(true);
        var rock = UnityEngine.Object.Instantiate(p.projectile, new Vector3(4, 5, 0), Quaternion.identity); rock.Launch(Vector2.right, source, false, 0, 999);
        var hostile = UnityEngine.Object.Instantiate(p.projectile, new Vector3(6, 5, 0), Quaternion.identity); hostile.Launch(Vector2.left, source, true, 0);
        yield return 0.55f; Require(normal != null && rapid != null && rock != null && hostile != null, "Projectiles disappeared too early");
        yield return 0.6f; Require(normal == null && rapid == null && rock == null && hostile != null, "Only player bullets must expire after one second");
        yield return 2.05f; Require(hostile == null, "Enemy projectile lifetime must remain three seconds");
        testSave = "GameJams.Validation.Restart." + Guid.NewGuid().ToString("N") + ".";
        PlayerPrefs.SetInt(testSave + "Pickup.legacy.apple", 1);
        p.saveSlot = testSave; p.persistUpgrades = true;
        int beforeApple = p.maxHealth;
        int beforeDamage = p.AttackDamage;
        Require(!p.HasCollected("legacy.apple") && p.ApplyPowerUp(PowerUp.Kind.Apple, "legacy.apple"), "Legacy pickup saves must not hide or block new apples");
        Require(p.maxHealth == beforeApple + 10 && PlayerPrefs.GetInt(testSave + "MaxHealth") == p.maxHealth, "Apple must permanently save ten extra maximum hearts");
        Require(p.AttackDamage == beforeDamage + 1 && PlayerPrefs.GetInt(testSave + "AttackDamage") == p.AttackDamage, "Apple damage must persist");
        Require(!p.ApplyPowerUp(PowerUp.Kind.Apple, "legacy.apple"), "Same apple must only be collected once per visit");
        p.Respawn(); Require(p.HasCollected("legacy.apple") && !p.ApplyPowerUp(PowerUp.Kind.Apple, "legacy.apple"), "Respawn must not replenish collected apples");
        PlayerPrefs.DeleteKey(testSave + "Pickup.legacy.apple");
        p.saveSlot = testSave; p.persistUpgrades = true; p.maxHealth = 26;
        p.Respawn(); Require(p.maxHealth == 21 && p.Health == 21, "Manual restart must subtract five maximum hearts");
        Warp(new Vector2(-8, -9)); p.enabled = true; yield return 0.08f; p.enabled = false;
        Require(p.maxHealth == 16 && p.Health == 16, "Falling must subtract five maximum hearts");
        p.KillInstantly(); yield return 1.5f;
        Require(!p.Dying && p.maxHealth == 11 && p.Health == 11, "Death restart must subtract five maximum hearts once");
        Require(PlayerPrefs.GetInt(testSave + "MaxHealth") == 11, "Restart penalty must persist");
        p.maxHealth = 3; p.Respawn(); Require(p.maxHealth == 1 && p.Health == 1, "Restart penalty must never reduce maximum health below one");
        p.maxHealth = 5;
        var clone = UnityEngine.Object.Instantiate(p.gameObject).GetComponent<player>();
        Require(clone.maxHealth == 1 && clone.Health == 1, "Saved one-heart maximum must survive a new player instance");
        Require(!clone.HasCollected("legacy.apple"), "New player/scene entry must make apples available again");
        Require(clone.AttackDamage == p.AttackDamage, "Apple damage must survive respawn and scene reload");
        UnityEngine.Object.DestroyImmediate(clone.gameObject); p.maxHealth = 1; p.persistUpgrades = false;
        Defeat(); yield return 1.25f;
        Require(Stage.Portals.Count == 2 && Stage.Portals.All(portal => portal.RequiresInteraction), "Two exits must require E");
        var secret = Stage.Portals.Single(portal => portal.destination == "Level7");
        Warp(new Vector2(secret.transform.position.x, -3.4f)); yield return 0.25f;
        Require(SceneManager.GetActiveScene().name == "Level6" && !Stage.Level.Completed, "Two-door choice must not auto-enter");
        Require(secret.Enter(p), "Explicit two-door selection failed"); yield return 0.5f;
        Prepare(); Defeat(); yield return 1.25f;
        var tower = Stage.Portals.Single(); Require(!tower.RequiresInteraction, "Single exit must not require E");
        Warp(new Vector2(tower.transform.position.x, -3.4f)); yield return 0.5f;
        Require(SceneManager.GetActiveScene().name == "Level8", "Single tower must enter automatically without E");
        p = Prepare(); Stage.enabled = true;
        Warp(new Vector2(12, -3.4f));
        typeof(FinalCampaignStage).GetField("nextMeteor", Private).SetValue(Stage, Time.time - 1);
        yield return 0.08f;
        Require(UnityEngine.Object.FindObjectsByType<CampaignHazard>().Any(h => h.meteor && h.lethal && !h.Armed), "Hunter arena must spawn warned lethal meteorites");
        Stage.enabled = false; Stage.ClearAttacks();
        p.ApplyPowerUp(PowerUp.Kind.Apple, "hunter.damage.test");
        typeof(player).GetField("invincibleUntil", Private).SetValue(p, 0f);
        var burst = (System.Collections.IEnumerator)typeof(BossController).GetMethod("HunterBurst", Private).Invoke(Stage.Boss, new object[] { 0 });
        Require(burst.MoveNext() && burst.MoveNext() && burst.MoveNext(), "Hunter burst must produce its first bullet");
        var hunterShot = UnityEngine.Object.FindObjectsByType<CampaignAttack>().Single().GetComponent<bullet>();
        int beforeHit = p.Health;
        typeof(bullet).GetMethod("OnTriggerEnter2D", Private).Invoke(hunterShot, new object[] { p.GetComponent<Collider2D>() });
        Require(p.Health == beforeHit - 6, "A real Hunter burst bullet must cause exactly six damage");
        Defeat(); yield return 0.1f;
        UI.restartButton.onClick.Invoke();
        Require(UI.ReturnConfirmationOpen && SceneManager.GetActiveScene().name == "Level8", "Final return button must also request confirmation");
        UI.CancelReturnToTutorial();
        UI.tutorialButton.onClick.Invoke();
        UI.GetComponentsInChildren<Button>().Single(b => b.name == "Volver al tutorial" && b.transform.parent.name == "Dialogo").onClick.Invoke();
        yield return 0.5f;
        Require(SceneManager.GetActiveScene().name == "Tutorial" && UI.GuideOpen && !UI.ReturnConfirmationOpen, "Confirm must load the tutorial with its guide");
        UI.SetGuide(false);
        Require(Time.timeScale == 1 && !TutorialCanvasUI.BlocksGameplay, "Leaving confirmation must not leave the new scene paused");
    }
}
