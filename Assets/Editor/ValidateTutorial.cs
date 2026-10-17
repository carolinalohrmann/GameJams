using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ValidateTutorial
{
    static IEnumerator<float> checks;
    static float next;
    static double deadline;
    static player p;
    static bool pushing;
    static string slot;
    static ValidateTutorial()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("TutorialValidation", false)) return;
            slot = SessionState.GetString("TutorialValidationSlot", "");
            checks = Checks(); next = 0; deadline = EditorApplication.timeSinceStartup + 80;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Level1.unity");
        var testPlayer = UnityEngine.Object.FindAnyObjectByType<player>();
        testPlayer.maxHealth = 5;
        testPlayer.saveSlot = "GameJams.Test." + Guid.NewGuid() + ".";
        testPlayer.persistUpgrades = true;
        var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
        if (ui != null) ui.showGuideOnStart = false;
        SessionState.SetString("TutorialValidationSlot", testPlayer.saveSlot);
        SessionState.SetBool("TutorialValidation", true);
        EditorApplication.EnterPlaymode();
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Cleanup()
    {
        foreach (string key in new[] { "MaxHealth", "DoubleJump", "AK", "Pickup.testApple", "Pickup.testFeather", "Pickup.testChicken" }) PlayerPrefs.DeleteKey(slot + key);
        PlayerPrefs.Save();
        SessionState.SetBool("TutorialValidation", false);
    }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Tutorial validation timeout");
            if (pushing && p != null) p.GetComponent<Rigidbody2D>().linearVelocity = Vector2.right * 5;
            if (Time.time < next) return;
            if (checks.MoveNext()) next = Time.time + checks.Current;
            else
            {
                Debug.Log("TUTORIAL_PLAYMODE_OK: generated route, three unique powerups, animated Flying Eye and melee enemies, weapons, health, death and completion");
                Cleanup(); EditorApplication.Exit(0);
            }
        }
        catch (Exception exception) { Debug.LogException(exception); Cleanup(); EditorApplication.Exit(1); }
    }
    static IEnumerator<float> Checks()
    {
        yield return 1;
        p = UnityEngine.Object.FindAnyObjectByType<player>();
        Require(p != null && p.Health == 5, "Initial player health");
        Require(p.animator != null && p.animator.runtimeAnimatorController.animationClips.Length == 4, "Horse must have all 4 animation clips");
        Require(p.shootPoint.name == "shootp", "Original shooting point missing");
        var pickups = UnityEngine.Object.FindObjectsByType<PowerUp>();
        var layout = UnityEngine.Object.FindAnyObjectByType<RandomLevelLayout>();
        Require(layout != null && layout.AvailablePowerUps.Count == 3 &&
            pickups.Length <= 3 && pickups.Select(x => x.kind).Distinct().Count() == pickups.Length,
            "A run must offer at most three distinct powerups");
        Require(GameObject.Find("Level 1 goal") != null, "Level 1 goal missing");
        Require(!System.IO.File.Exists("Assets/0_2prefafs/CharacterPhysics.physicsMaterial2D"), "Removed physics material returned");
        var enemies = UnityEngine.Object.FindObjectsByType<Enemy>();
        var enemy = enemies.FirstOrDefault(x => !x.ranged);
        Require(enemy != null && enemy.animator != null, "Animated melee enemies missing");
        var rangedTarget = enemies.FirstOrDefault(x => x.ranged);
        Require(rangedTarget != null && rangedTarget.animator != null &&
            rangedTarget.animator.runtimeAnimatorController.name == "FlyingEye", "Flying Eye ranged enemy missing");
        rangedTarget.enabled = false;
        rangedTarget.transform.position = new Vector3(p.transform.position.x + 2, -3.5f, 0);
        Physics2D.SyncTransforms();
        Require(p.TryShoot(), "Normal pistol should fire from the corrected muzzle");
        yield return 0.4f;
        Require(p.TryShoot(), "Normal pistol delay should allow a second shot");
        yield return 0.25f;
        Require(rangedTarget == null, "Corrected muzzle height must still hit the shorter ranged enemy");
        var body = p.GetComponent<Rigidbody2D>();
        float enemyX = enemy.transform.position.x;
        p.enabled = false; p.transform.position = new Vector3(enemyX - 0.8f, -3.4f, 0); Physics2D.SyncTransforms(); pushing = true;
        yield return 1.6f;
        pushing = false;
        Require(Mathf.Abs(enemy.transform.position.x - enemyX) < 0.01f, "Player can push enemy");
        Require(p.Health < 5, "Contact does not hurt player");
        int before = p.Health;
        p.ApplyPowerUp(PowerUp.Kind.Medkit, "testMedkit");
        Require(p.Health == Mathf.Min(5, before + 2), "Medkit must heal exactly two, capped");
        p.ApplyPowerUp(PowerUp.Kind.Medkit, "testMedkitFull");
        Require(p.Health == 5 && p.maxHealth == 5, "Medkit increases maximum health");
        p.enabled = true; p.Respawn();
        yield return 0.5f;
        p.ApplyPowerUp(PowerUp.Kind.Apple, "testApple");
        Require(p.maxHealth == 15 && p.Health == 15, "Apple must add 10 permanent health");
        Require(!p.ApplyPowerUp(PowerUp.Kind.Apple, "testApple") && p.maxHealth == 15, "Permanent apple collected twice");
        p.ApplyPowerUp(PowerUp.Kind.Feather, "testFeather");
        Require(p.DoubleJump && p.FeatherRemaining > 4.9f && !PlayerPrefs.HasKey(slot + "DoubleJump") && PlayerPrefs.GetInt(slot + "MaxHealth") == 15, "Health must save while Feather stays temporary");
        p.ApplyPowerUp(PowerUp.Kind.Citrus, "testCitrus");
        float buffStart = Time.time;
        Require(Mathf.Abs(p.MoveSpeed - p.speed * 1.5f) < 0.001f && Mathf.Abs(p.FireDelay - p.normalFireDelay / 1.5f) < 0.001f, "Citrus must increase both speeds by 50 percent");
        p.ApplyPowerUp(PowerUp.Kind.Resistance, "testResistance");
        p.ApplyPowerUp(PowerUp.Kind.Shirt, "testShirt");
        Require(p.weaponVisual.enabled && p.weaponVisual.sprite == p.minigunSprite && p.FireDelay < 0.05f, "Minigun overlay or rate incorrect");
        p.SetFacingDirection(-1);
        Require(p.weaponVisual.flipX && p.weaponVisual.transform.localPosition.x < 0 && p.shootPoint.localPosition.x < 0, "Weapon and muzzle must face left together");
        Require(Mathf.Abs(p.weaponVisual.bounds.size.x - 0.65f) < 0.01f && Mathf.Abs(p.weaponVisual.transform.localPosition.y - 0.23f) < 0.001f, "Minigun must fit the pistol position");
        p.SetFacingDirection(1);
        Require(!p.weaponVisual.flipX && p.shootPoint.localPosition.x > 0, "Weapon must face right together with muzzle");
        p.RequestJump();
        yield return 0.2f;
        Require(p.transform.position.y > -3.3f, "First jump failed");
        p.RequestJump();
        yield return 0.08f;
        Require(body.linearVelocity.y > 7.5f, "Double jump failed; velocity=" + body.linearVelocity.y);
        p.RequestJump();
        yield return 0.08f;
        Require(body.linearVelocity.y < 8.5f, "Third jump should not be possible");
        for (int i = 0; i < 15; i++)
        {
            Require(p.TryShoot(), "Minigun unexpectedly limited or too slow");
            Require(!p.TryShoot(), "Fire delay not enforced");
            yield return 0.04f;
        }
        yield return Mathf.Max(0.1f, buffStart + 5.2f - Time.time);
        Require(p.MinigunRemaining == 0 && p.WeaponName == "Pistola" && !p.weaponVisual.enabled, "Minigun must disappear after 5 seconds");
        int protectedHealth = p.Health; p.Damage(); Require(p.Health == protectedHealth, "Resistance does not prevent damage");
        p.ApplyPowerUp(PowerUp.Kind.Rock, "testRock");
        Require(p.RockAmmo == 5 && p.weaponVisual.sprite == p.rockSprite, "Rock overlay/ammo missing");
        var target = GameObject.Find("Skeleton - piedra 1").GetComponent<Enemy>();
        target.enabled = false;
        target.transform.position = p.shootPoint.position + Vector3.right * 1.2f;
        Physics2D.SyncTransforms();
        for (int i = 0; i < 5; i++)
        {
            float delay = p.FireDelay;
            Require(p.TryShoot() && p.RockAmmo == 4 - i, "Rock shot count incorrect");
            if (i == 0)
            {
                var rockShot = UnityEngine.Object.FindObjectsByType<bullet>().First(s => s.GetComponent<SpriteRenderer>().sprite == p.rockSprite);
                Require(Mathf.Abs(rockShot.GetComponent<SpriteRenderer>().bounds.size.x - 0.85f) < 0.01f, "Thrown rocks should be larger");
            }
            yield return delay + 0.05f;
            if (i == 0) Require(target == null || (target.health <= 0 && !target.GetComponent<Collider2D>().enabled), "One rock should kill a normal enemy");
        }
        Require(p.WeaponName == "Pistola", "Rock weapon must end after exactly 5 shots");
        PlayerPrefs.SetInt(slot + "Pickup.testChicken", 1); // Simulate an older permanently collected Chicken.
        Require(p.ApplyPowerUp(PowerUp.Kind.Chicken, "testChicken"), "Old Chicken saves must not block the new timed pickup");
        float akStart = Time.time;
        Require(p.HasAK && p.AKRemaining > 9.9f && p.weaponVisual.sprite == p.akSprite, "10-second AK missing");
        Require(Mathf.Abs(p.weaponVisual.bounds.size.x - 0.7f) < 0.01f, "AK should fit the character");
        yield return Mathf.Max(0.1f, buffStart + 10.2f - Time.time);
        Require(p.CitrusRemaining == 0 && Mathf.Abs(p.MoveSpeed - p.speed) < 0.001f && Mathf.Abs(p.FireDelay - 0.12f) < 0.001f, "Citrus did not expire after 10 seconds");
        Require(p.ImmunityRemaining == 0, "Resistance did not expire after 10 seconds");
        int health = p.Health; p.Damage(); Require(p.Health == health - 1, "Damage must resume after immunity");
        p.ApplyPowerUp(PowerUp.Kind.Shirt, "testShirtAfterAK");
        yield return 5.2f;
        Require(p.WeaponName == "AK" && p.weaponVisual.sprite == p.akSprite, "Minigun should return to AK only during its remaining 10 seconds");
        PlayerPrefs.SetInt(slot + "AK", 1); // Old saves must no longer grant a permanent rifle.
        PlayerPrefs.SetInt(slot + "DoubleJump", 1); // Legacy Feather saves must not grant permanent double jump.
        // Reload an inactive copy to exercise Awake's saved-upgrade loading.
        var copy = UnityEngine.Object.Instantiate(p.gameObject); copy.SetActive(false);
        var loaded = copy.GetComponent<player>();
        Require(loaded.maxHealth == 15 && !loaded.DoubleJump && !loaded.HasAK, "Only permanent health should load from old saves");
        UnityEngine.Object.Destroy(copy);
        yield return Mathf.Max(0.1f, akStart + 10.1f - Time.time);
        Require(!p.HasAK && p.AKRemaining == 0 && p.WeaponName == "Pistola" && !p.weaponVisual.enabled, "AK should disappear after exactly 10 seconds");
        Require(Mathf.Abs(p.FireDelay - p.normalFireDelay) < 0.001f, "Normal fire rate should return after AK expires");
        p.Respawn();
        Require(!p.DoubleJump && !p.HasAK && p.maxHealth == 15 && Mathf.Abs(p.transform.position.x + 8) < 0.01f, "Respawn should return to start with permanent health and expired temporary powers");
        for (int i = 0; i < 15; i++) { p.Damage(); yield return 1.25f; }
        // First hit can be covered by spawn protection; finish reducing the remaining health.
        while (!p.Dying) { p.Damage(); yield return 0.05f; }
        Require(p.animator.GetCurrentAnimatorStateInfo(0).IsName("Death") || p.Dying, "Death animation state not reached");
        yield return 1.5f;
        Require(!p.Dying && p.Health == 15 && !p.HasAK && !p.DoubleJump, "Death did not return player with permanent health only");
        p.transform.position = new Vector3(117, -3.4f, 0);
        yield return 0.1f;
        Require(LevelOne.Instance.Completed, "Barn destination does not complete tutorial");
    }
}
