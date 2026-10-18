using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ValidateTutorialCanvas
{
    static IEnumerator<float> checks;
    static float next;
    static double deadline;
    static ValidateTutorialCanvas()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CanvasValidation", false)) return;
            checks = Checks(); next = 0; deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update += Tick;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        UnityEngine.Object.FindAnyObjectByType<player>().persistUpgrades = false;
        SessionState.SetBool("CanvasValidation", true); EditorApplication.EnterPlaymode();
    }
    static void Require(bool ok, string text) { if (!ok) throw new Exception(text); }
    static void Tick()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < deadline, "Canvas validation timeout");
            if (Time.unscaledTime < next) return;
            if (checks.MoveNext()) next = Time.unscaledTime + checks.Current;
            else { Debug.Log("TUTORIAL_CANVAS_PLAYMODE_OK: Canvas controls, mouse-only shooting labels, Feather expires at 5s, no checkpoints, rocks end only after 5 shots"); SessionState.SetBool("CanvasValidation", false); Time.timeScale = 1; EditorApplication.Exit(0); }
        }
        catch (Exception e) { Debug.LogException(e); SessionState.SetBool("CanvasValidation", false); Time.timeScale = 1; EditorApplication.Exit(1); }
    }
    static IEnumerator<float> Checks()
    {
        yield return 0.5f;
        var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
        Require(UnityEngine.Object.FindObjectsByType<Enemy>().All(e => e.health == 6), "Every tutorial enemy must receive four extra health");
        Require(ui != null && ui.GetComponent<Canvas>() != null && ui.GetComponent<GraphicRaycaster>() != null, "Real Canvas UI missing");
        Require(ui.GuideOpen && Time.timeScale == 0 && TutorialCanvasUI.BlocksGameplay, "Tutorial guide should initially pause gameplay");
        var input = UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();
        Require(EventSystem.current != null && input != null && input.actionsAsset != null, "Canvas buttons need an active input module");
        Require(EventSystem.current.currentSelectedGameObject == ui.continueButton.gameObject, "Enter should select Continue rather than reopen the guide");
        var reset = ui.GetComponentsInChildren<Button>().Single(b => b.name == "Restablecer valores");
        string originalSlot = ui.player.saveSlot;
        string resetSlot = "GameJams.Validation.MenuReset." + Guid.NewGuid().ToString("N") + ".";
        ui.player.saveSlot = resetSlot; ui.player.persistUpgrades = true;
        ui.player.ApplyPowerUp(PowerUp.Kind.Apple, "test.menu.reset");
        Require(ui.player.maxHealth > 10 && ui.player.AttackDamage > 1, "Reset test must start with upgraded stats");
        reset.onClick.Invoke();
        Require(ui.player.Health == 10 && ui.player.maxHealth == 10 && ui.player.AttackDamage == 1, "Initial menu button must reset current health, maximum health and damage");
        Require(PlayerPrefs.GetInt(resetSlot + "MaxHealth") == 10 && PlayerPrefs.GetInt(resetSlot + "AttackDamage") == 1 && PlayerPrefs.GetInt(resetSlot + "BaseHealth") == 10, "Menu reset must persist initial values");
        ui.player.persistUpgrades = false; ui.player.saveSlot = originalSlot;
        PlayerPrefs.DeleteKey(resetSlot + "MaxHealth"); PlayerPrefs.DeleteKey(resetSlot + "AttackDamage"); PlayerPrefs.DeleteKey(resetSlot + "BaseHealth"); PlayerPrefs.Save();
        var text = string.Join("\n", ui.guideContent.GetComponentsInChildren<Text>().Select(t => t.text));
        foreach (string term in new[] { "Mover", "Saltar", "Disparar" })
            Require(text.ToLowerInvariant().Contains(term.ToLowerInvariant()), "Missing control explanation: " + term);
        Require(text.Contains("Clic izquierdo") && text.Contains("resta 5 de vida") &&
            !text.Contains("checkpoint") && !text.Contains("J /"), "Guide must explain current controls and generated powerups");
        var icons = ui.guideContent.GetComponentsInChildren<Image>()
            .Where(i => i.name == "Icono" && i.isActiveAndEnabled).ToArray();
        Require(ui.simpleGuideText != null ? icons.Length == 0 : icons.Length == (ui.level.stageNumber == 1 ? 8 : 3) && icons.All(i => i.sprite != null), "Guide must show plain explanations or the legacy icons");
        ui.continueButton.onClick.Invoke();
        yield return 0.2f;
        Require(!ui.GuideOpen && Time.timeScale == 1 && !TutorialCanvasUI.BlocksGameplay, "Continue button should resume gameplay");
        Require(!reset.gameObject.activeSelf, "Reset button must disappear after starting");
        Require(EventSystem.current.currentSelectedGameObject == null, "Closing guide should release UI focus");
        var p = ui.player;
        float baseSpeed = p.MoveSpeed;
        p.ApplyPowerUp(PowerUp.Kind.Citrus, "levelCitrus");
        p.ApplyPowerUp(PowerUp.Kind.Citrus, "levelCitrusAgain");
        Require(Mathf.Abs(p.MoveSpeed - baseSpeed * 1.5f) < 0.001f, "Level Citrus must not stack speed multipliers");
        Vector3 start = p.transform.position;
        p.ApplyPowerUp(PowerUp.Kind.Feather, "temporaryFeather");
        Require(p.DoubleJump && p.FeatherRemaining > 4.9f, "Feather must activate for 5 seconds");
        p.ApplyPowerUp(PowerUp.Kind.Rock, "noTimeout");
        yield return 6;
        Require(p.DoubleJump && float.IsPositiveInfinity(p.FeatherRemaining), "Feather must stay active for the entire level");
        p.transform.position = new Vector3(60, -2.8f, 0);
        yield return 0.1f;
        p.Respawn();
        Require(p.DoubleJump && float.IsPositiveInfinity(p.CitrusRemaining), "Level powers must survive respawn");
        Require(Mathf.Abs(p.transform.position.x - start.x) < 0.01f, "Crossing the old checkpoint must still respawn at the beginning");
        Require(p.RockAmmo == 5 && p.WeaponName.StartsWith("Piedras"), "Rocks must remain available after waiting more than 5 seconds");
        for (int i = 0; i < 5; i++)
        {
            Require(p.TryShoot() && p.RockAmmo == 4 - i, "Stone shot count must decrease exactly once");
            yield return 0.95f;
        }
        Require(p.WeaponName == "Pistola" && p.RockAmmo == 0, "Fifth stone should restore the normal weapon");
        ui.guideButton.onClick.Invoke();
        Require(!reset.gameObject.activeSelf, "In-game guide must not offer the initial menu reset");
        float pausedTime = Time.time;
        yield return 0.25f;
        Require(ui.GuideOpen && Time.timeScale == 0 && Time.time == pausedTime && ui.continueText.text.Contains("Continuar"), "Guide button should reopen and pause");
        ui.continueButton.onClick.Invoke();
        yield return 0.1f;
        Require(!ui.GuideOpen && Time.timeScale == 1, "Guide close should restore time");
        Require(ui.tutorialButton != null && ui.guideTutorialButton != null, "Return-to-tutorial buttons must exist in gameplay and help");
        ui.guideTutorialButton.onClick.Invoke();
        Require(ui.ReturnConfirmationOpen, "Return button must ask for confirmation");
        ui.ConfirmReturnToTutorial();
        yield return 0.5f;
        ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
        Require(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Tutorial" && ui.GuideOpen, "Return button must load the tutorial and its help");
        Require(!ui.player.DoubleJump && ui.player.CitrusRemaining == 0, "Citrus and Feather must clear when leaving the level");
        ui.continueButton.onClick.Invoke();
        Require(Time.timeScale == 1 && !ui.GuideOpen, "Returning to the tutorial must not leave gameplay stuck paused");
        for (int stage = 2; stage <= 8; stage++)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level" + stage); yield return 0.4f;
            ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
            Require(!ui.GetComponentsInChildren<Button>(true).Any(b => b.name == "Restablecer valores"), "Stat reset must not exist in level " + stage);
        }
    }
}
