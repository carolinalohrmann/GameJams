using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ValidateCombatPolish
{
    static IEnumerator<float> checks;
    static float next;
    static double deadline;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static ValidateCombatPolish()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CombatPolishValidation", false)) return;
            checks = Checks(); next = 0; deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        UnityEngine.Object.FindAnyObjectByType<player>().persistUpgrades = false;
        SessionState.SetBool("CombatPolishValidation", true); EditorApplication.EnterPlaymode();
    }
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Combat polish test timeout");
            if (Time.unscaledTime < next) return;
            if (checks.MoveNext()) next = Time.unscaledTime + checks.Current;
            else { Debug.Log("COMBAT_POLISH_PLAYMODE_OK: bidirectional solid wall sweeps, repeated melee attack/recovery, hearts damage/heal/max upgrade, animated normal/rapid shots and brown enemy bullets"); Finish(0); }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Finish(int code) { SessionState.SetBool("CombatPolishValidation", false); Time.timeScale = 1; EditorApplication.Exit(code); }
    public static void CheckWorld(GameObject world, bool tutorial)
    {
        Physics2D.SyncTransforms();
        var platforms = world.GetComponentsInChildren<Collider2D>().Where(c => !c.isTrigger && c.attachedRigidbody == null).ToArray();
        foreach (var actor in world.GetComponentsInChildren<Enemy>().Select(e => e.GetComponent<Collider2D>())
            .Concat(world.GetComponentsInChildren<PowerUp>().Select(p => p.GetComponent<Collider2D>())))
            foreach (var platform in platforms)
                Require(actor.Distance(platform).distance >= -0.001f, actor.name + " spawned inside " + platform.name + " depth=" + actor.Distance(platform).distance);
        if (!tutorial)
        {
            var bonuses = platforms.Where(p => p.name.StartsWith("Bonus ")).ToArray();
            Require(bonuses.Length >= 2, "Generated stage needs elevated platforms");
            foreach (var bonus in bonuses)
            {
                Require(bonus.GetComponentsInChildren<SpriteRenderer>().Length == 1, "Floating platforms must not draw uncollidable dirt underneath");
                Require(Mathf.Abs(bonus.bounds.min.y - bonus.GetComponentInChildren<SpriteRenderer>().bounds.min.y) < 0.001f, "Floating visual/collider thickness mismatch");
            }
            Debug.Log("SPAWN_CLEARANCE_OK " + world.name + " raisedPlatforms=" + bonuses.Length + " enemies=" + world.GetComponentsInChildren<Enemy>().Length);
        }
    }
    static IEnumerator<float> Checks()
    {
        yield return 0.5f;
        var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
        ui.SetGuide(false);
        ui.level.enabled = false;
        var p = ui.player; p.enabled = false;
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>()) enemy.gameObject.SetActive(false);
        var body = p.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        p.transform.position = new Vector3(1003, 0, 0);
        var hearts = ui.GetComponent<HealthHeartsUI>();
        Require(hearts != null && hearts.heart != null, "Missing health sprites");
        yield return 0.05f;
        Require(hearts.container.childCount == p.maxHealth, "One heart must represent each health point");
        int health = p.Health;
        p.Damage(); yield return 0.05f;
        Require(p.Health == health - 1, "Contact damage must remove a heart");
        Require(hearts.container.GetComponentsInChildren<Image>().Count(i => i.color.a > 0.9f) == p.Health, "Damage must empty one heart");
        p.ApplyPowerUp(PowerUp.Kind.Medkit, "test.heal"); yield return 0.05f;
        Require(p.Health == p.maxHealth, "Medkit must refill missing hearts");
        int max = p.maxHealth;
        int damage = p.AttackDamage;
        p.ApplyPowerUp(PowerUp.Kind.Apple, "test.apple"); yield return 0.05f;
        Require(p.maxHealth == max + 10 && hearts.container.childCount == p.maxHealth, "Apple must add ten visible hearts");
        Require(p.AttackDamage == damage + 1 && !p.ApplyPowerUp(PowerUp.Kind.Apple, "test.apple") && p.AttackDamage == damage + 1, "Apple must add exactly one permanent damage per pickup");
        var source = p.GetComponent<Collider2D>();
        var normal = UnityEngine.Object.Instantiate(p.projectile, new Vector3(1000, 5, 0), Quaternion.identity);
        normal.Launch(Vector2.right, source, false, 0); normal.UsePlayerEffect(false);
        var rapid = UnityEngine.Object.Instantiate(p.projectile, new Vector3(1001, 5, 0), Quaternion.identity);
        rapid.Launch(Vector2.right, source, false, 0); rapid.UsePlayerEffect(true);
        Require(normal.normalFrames.Length == 20 && rapid.rapidFrames.Length == 20, "Both effects need all twenty frames");
        Require(normal.GetComponent<SpriteRenderer>().sprite == normal.normalFrames[0] && rapid.GetComponent<SpriteRenderer>().sprite == rapid.rapidFrames[0], "Wrong shot effect selection");
        yield return 0.25f;
        Require(normal.GetComponent<SpriteRenderer>().sprite != normal.normalFrames[0] && rapid.GetComponent<SpriteRenderer>().sprite != rapid.rapidFrames[0], "Shot effects must animate");
        var hostile = UnityEngine.Object.Instantiate(p.projectile, new Vector3(1002, 5, 0), Quaternion.identity);
        hostile.Launch(Vector2.left, source, true, 0);
        var brown = hostile.GetComponent<SpriteRenderer>().color;
        Require(brown.r > brown.g && brown.g > brown.b && brown.r < 0.6f, "Enemy bullet must be brown");
        foreach (int weapon in new[] { 0, 1, 2, 3 })
        {
            if (weapon == 1) p.ApplyPowerUp(PowerUp.Kind.Chicken, "test.ak");
            if (weapon == 2) p.ApplyPowerUp(PowerUp.Kind.Shirt, "test.minigun");
            if (weapon == 3) p.ApplyPowerUp(PowerUp.Kind.Rock, "test.rock");
            if (weapon == 1) Require(p.AKRemaining > 14.9f && p.AKRemaining <= 15 && Mathf.Abs(p.FireDelay - 0.13f) < 0.0001f, "AK must last 15 seconds and fire every 0.13 seconds");
            if (weapon == 2) Require(p.MinigunRemaining > 9.9f && p.MinigunRemaining <= 10, "Camisa must last 10 seconds");
            var existing = UnityEngine.Object.FindObjectsByType<bullet>();
            typeof(player).GetField("nextShot", Private).SetValue(p, 0f);
            Require(p.TryShoot(), "Weapon did not fire");
            var shot = UnityEngine.Object.FindObjectsByType<bullet>().Except(existing).Single();
            Require((int)typeof(bullet).GetField("damage", Private).GetValue(shot) == (weapon == 3 ? 999 : p.AttackDamage), "Pistol, AK and minigun must use apple damage; stones stay lethal");
            var sprite = shot.GetComponent<SpriteRenderer>().sprite;
            Require(sprite == (weapon == 3 ? p.rockSprite : weapon == 0 ? shot.normalFrames[0] : shot.rapidFrames[0]), "Player selected the wrong projectile effect");
            if (weapon == 3) Require(p.RockAmmo == 4 && Mathf.Abs(shot.GetComponent<SpriteRenderer>().bounds.size.x - 0.85f) < 0.001f, "Stone size/ammo changed");
        }
        p.transform.position = new Vector3(1100, 0, 0);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/0_2prefafs/EnemyGoblin.prefab");
        var subject = UnityEngine.Object.Instantiate(prefab, new Vector3(1000, 0, 0), Quaternion.identity).GetComponent<Enemy>();
        subject.enabled = false;
        yield return 0.05f;
        typeof(Enemy).GetMethod("Start", Private).Invoke(subject, null);
        var wall = new GameObject("Test solid wall").AddComponent<BoxCollider2D>();
        wall.transform.position = new Vector3(1001.5f, 0, 0); wall.size = new Vector2(0.5f, 5);
        var move = typeof(Enemy).GetMethod("MoveSafely", Private);
        var enemyBody = subject.GetComponent<Rigidbody2D>();
        var simulation = Physics2D.simulationMode; Physics2D.simulationMode = SimulationMode2D.Script;
        try
        {
            foreach (int side in new[] { -1, 1 })
            {
                enemyBody.position = new Vector2(1001.5f + side * 2, 0); Physics2D.SyncTransforms();
                for (int i = 0; i < 100; i++) { move.Invoke(subject, new object[] { Vector2.right * (-side * 0.1f) }); Physics2D.Simulate(0.02f); }
                var box = subject.GetComponent<Collider2D>();
                float separation = Mathf.Abs(enemyBody.position.x - wall.transform.position.x);
                Require(!box.Distance(wall).isOverlapped && separation > 0.68f && separation < 0.8f, "Kinematic enemy crossed a wall or stopped before reaching it");
            }
        }
        finally { Physics2D.simulationMode = simulation; }
        UnityEngine.Object.DestroyImmediate(wall.gameObject);
        enemyBody.position = new Vector2(1000, 0); p.transform.position = new Vector3(1000.72f, 0, 0); Physics2D.SyncTransforms();
        Require((bool)typeof(Enemy).GetMethod("CanSeePlayer", Private).Invoke(subject, null), "Melee must see the nearby player without detecting its own collider");
        var contact = typeof(Enemy).GetMethod("ContactAttack", Private);
        contact.Invoke(subject, null); subject.animator.Update(0.02f);
        Require(subject.animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"), "Melee attack did not start");
        float duration = (float)typeof(Enemy).GetField("attackDuration", Private).GetValue(subject);
        yield return duration + 0.05f;
        contact.Invoke(subject, null); subject.animator.Update(0.02f);
        Require(subject.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Melee must recover instead of freezing on Attack");
        yield return 0.26f;
        contact.Invoke(subject, null); subject.animator.Update(0.02f);
        Require(subject.animator.GetCurrentAnimatorStateInfo(0).IsName("Attack") && subject.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.15f, "Melee attack must restart from the first frame");
        yield return duration + 0.05f;
        p.transform.position = new Vector3(1002.5f, 0, 0);
        var floor = new GameObject("Test recovery floor").AddComponent<BoxCollider2D>();
        floor.transform.position = new Vector3(1000, -0.85f, 0); floor.size = new Vector2(10, 0.5f);
        Physics2D.SyncTransforms();
        yield return 0.08f;
        typeof(Enemy).GetMethod("FixedUpdate", Private).Invoke(subject, null); subject.animator.Update(0.02f);
        Require(subject.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"), "Melee must resume walking when the player leaves attack range; state=" + typeof(Enemy).GetField("state", Private).GetValue(subject) + " position=" + enemyBody.position);
    }
}
