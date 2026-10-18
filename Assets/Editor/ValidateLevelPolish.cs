using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ValidateLevelPolish
{
    static IEnumerator<float> checks;
    static float next;
    static double deadline;
    static ValidateLevelPolish()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PolishValidation", false)) return;
            checks = Checks(); next = 0; deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        if (UnityEngine.Object.FindAnyObjectByType<RandomLevelLayout>() != null) { ValidateGameLevels.Run(); return; }
        var p = UnityEngine.Object.FindAnyObjectByType<player>(); p.persistUpgrades = false;
        var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
        if (ui != null) ui.showGuideOnStart = false;
        SessionState.SetBool("PolishValidation", true); EditorApplication.EnterPlaymode();
    }
    static void Require(bool ok, string text) { if (!ok) throw new Exception(text); }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Polish validation timed out");
            if (Time.time < next) return;
            if (checks.MoveNext()) next = Time.time + checks.Current;
            else { Debug.Log("LEVEL_POLISH_PLAYMODE_OK: 12 platform corners, 8 enemy side jumps, contact damage without pushing, larger icons, vegetation outside patrols, preserved powerup timers"); SessionState.SetBool("PolishValidation", false); EditorApplication.Exit(0); }
        }
        catch (Exception e) { Debug.LogException(e); SessionState.SetBool("PolishValidation", false); EditorApplication.Exit(1); }
    }
    static IEnumerator<float> Checks()
    {
        yield return 1;
        var p = UnityEngine.Object.FindAnyObjectByType<player>();
        Require(p.GetComponent<CapsuleCollider2D>() != null && p.GetComponent<BoxCollider2D>() == null, "Player must use a rounded capsule");
        Require(p.GetComponent<Collider2D>().sharedMaterial == null, "No custom physics material should be added");
        Require(GameObject.Find("Descanso") == null, "Optional Descanso platform was not removed");
        string[] names = { "Primer salto", "Salto con pluma", "Practica doble salto" };
        var platforms = names.Select(n => GameObject.Find(n)).ToArray();
        for (int i = 0; i < platforms.Length; i++)
        {
            var visuals = platforms[i].GetComponentsInChildren<SpriteRenderer>();
            Require(visuals.Length == (i == 1 ? 2 : 1), "Each platform should have one or two stretched sprites");
            float width = visuals.Sum(v => v.bounds.size.x);
            Require(Mathf.Abs(width - platforms[i].GetComponent<BoxCollider2D>().size.x) < 0.01f, "Stretched sprites must span the whole collider");
        }
        var powers = UnityEngine.Object.FindObjectsByType<PowerUp>();
        var enemies = UnityEngine.Object.FindObjectsByType<Enemy>();
        foreach (var bush in UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Where(s => s.name.StartsWith("Vegetacion ")))
            foreach (var enemy in enemies)
                Require(Mathf.Abs(bush.bounds.center.x - (float)typeof(Enemy).GetField("home", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(enemy)) >= enemy.patrolDistance + enemy.GetComponent<Collider2D>().bounds.extents.x + bush.bounds.extents.x + 0.69f, "Vegetation overlaps enemy patrol: " + enemy.name);
        Require(powers.Length == 8, "Powerup count changed");
        foreach (var power in powers)
        {
            Require(!power.GetComponentsInChildren<SpriteRenderer>().Any(sr => sr.enabled && sr.name == "Capsula"), "White powerup background remains");
            Require(power.durationSeconds == PowerUp.DefaultDuration(power.kind), "Explicit durations/default 5s not configured");
            Require(Mathf.Abs(power.icon.transform.localScale.x - 0.75f) < 0.001f && power.icon.sortingOrder > p.characterVisual.sortingOrder, "Powerup must be moderately larger and visible above characters");
            Require(power.GetComponent<CapsuleCollider2D>().size == new Vector2(0.85f, 0.95f), "Pickup trigger must fit the larger icon");
            power.gameObject.SetActive(false);
        }
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>()) { enemy.enabled = false; enemy.GetComponent<Collider2D>().enabled = false; }
        var rb = p.GetComponent<Rigidbody2D>();
        var fixedStep = typeof(player).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        var movement = typeof(player).GetField("movement", BindingFlags.Instance | BindingFlags.NonPublic);
        var originalMode = Physics2D.simulationMode;
        p.enabled = false;
        Physics2D.simulationMode = SimulationMode2D.Script;
        try
        {
            foreach (var platform in platforms)
            foreach (int direction in new[] { 1, -1 })
            foreach (float jumpVelocity in new[] { 0f, 10f })
            {
                var bounds = platform.GetComponent<BoxCollider2D>().bounds;
                float side = direction > 0 ? bounds.min.x : bounds.max.x;
                float startY = bounds.max.y - 0.1f;
                rb.position = new Vector2(side - direction * 0.32f, startY);
                rb.linearVelocity = Vector2.up * jumpVelocity;
                movement.SetValue(p, (float)direction);
                Physics2D.SyncTransforms();
                for (int step = 0; step < 60; step++) { fixedStep.Invoke(p, null); Physics2D.Simulate(0.02f); }
                float foot = rb.position.y - 0.6f;
                bool landedAbove = p.Grounded && foot >= bounds.max.y - 0.12f;
                bool fellAway = rb.position.y < startY - 0.2f;
                Require(landedAbove || fellAway, "Character stuck at " + platform.name + ", direction=" + direction + ", jump=" + jumpVelocity + ", position=" + rb.position);
            }
            foreach (bool ranged in new[] { false, true })
            {
                var enemy = enemies.First(e => e.ranged == ranged);
                var enemyCollider = enemy.GetComponent<Collider2D>();
                Vector3 originalPosition = enemy.transform.position;
                enemy.transform.position = new Vector3(8, 0, 0);
                enemyCollider.enabled = true;
                Physics2D.SyncTransforms();
                foreach (int direction in new[] { 1, -1 })
                foreach (float jumpVelocity in new[] { 0f, 10f })
                {
                    var bounds = enemyCollider.bounds;
                    float side = direction > 0 ? bounds.min.x : bounds.max.x;
                    float startY = bounds.max.y - 0.1f;
                    rb.position = new Vector2(side - direction * 0.32f, startY);
                    rb.linearVelocity = Vector2.up * jumpVelocity;
                    movement.SetValue(p, (float)direction);
                    Physics2D.SyncTransforms();
                    for (int step = 0; step < 60; step++) { fixedStep.Invoke(p, null); Physics2D.Simulate(0.02f); }
                    Require(rb.position.y < startY - 0.2f || rb.position.y - 0.6f >= bounds.max.y - 0.12f, "Player stuck against " + enemy.name + ", direction=" + direction + ", jump=" + jumpVelocity);
                    Require(Mathf.Abs(enemy.transform.position.x - 8) < 0.001f, "Player must not push enemies");
                }
                // The lateral fix must still allow physical contact and damage.
                typeof(player).GetField("invincibleUntil", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(p, 0f);
                int health = p.Health;
                rb.position = new Vector2(enemyCollider.bounds.min.x - 0.32f, 0);
                rb.linearVelocity = Vector2.zero; movement.SetValue(p, 1f); Physics2D.SyncTransforms();
                for (int step = 0; step < 8; step++) { fixedStep.Invoke(p, null); Physics2D.Simulate(0.02f); }
                Require(p.Health == health - 1, "Side contact must still hurt the player");
                enemyCollider.enabled = false; enemy.transform.position = originalPosition;
            }
        }
        finally { Physics2D.simulationMode = originalMode; }
        movement.SetValue(p, 0f); p.enabled = true; p.Respawn();
        p.ApplyPowerUp(PowerUp.Kind.Shirt, "defaultDuration");
        Require(p.MinigunRemaining > 4.9f && p.MinigunRemaining <= 5, "Unspecified timed powerup must default to 5 seconds");
        yield return 5.2f;
        Require(p.MinigunRemaining == 0 && p.WeaponName == "Pistola", "Default 5-second effect did not expire");
        p.ApplyPowerUp(PowerUp.Kind.Rock, "defaultRocks");
        Require(p.RockAmmo == 5, "Rock effect needs 5 shots");
        yield return 5.2f;
        Require(p.RockAmmo == 5 && p.WeaponName.StartsWith("Piedras"), "Rocks must not expire with time");
        p.ApplyPowerUp(PowerUp.Kind.Citrus, "explicitCitrus");
        p.ApplyPowerUp(PowerUp.Kind.Resistance, "explicitResistance");
        p.ApplyPowerUp(PowerUp.Kind.Chicken, "explicitAK");
        Require(p.CitrusRemaining > 9.9f && p.ImmunityRemaining > 9.9f && p.AKRemaining > 9.9f, "Explicit 10-second durations must be preserved");
    }
}
