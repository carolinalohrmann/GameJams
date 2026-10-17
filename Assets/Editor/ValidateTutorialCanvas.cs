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
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Level1.unity");
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
        Require(ui != null && ui.GetComponent<Canvas>() != null && ui.GetComponent<GraphicRaycaster>() != null, "Real Canvas UI missing");
        Require(ui.GuideOpen && Time.timeScale == 0 && TutorialCanvasUI.BlocksGameplay, "Tutorial guide should initially pause gameplay");
        var input = UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>();
        Require(EventSystem.current != null && input != null && input.actionsAsset != null, "Canvas buttons need an active input module");
        Require(EventSystem.current.currentSelectedGameObject == ui.continueButton.gameObject, "Enter should select Continue rather than reopen the guide");
        var text = string.Join("\n", ui.guideContent.GetComponentsInChildren<Text>().Select(t => t.text));
        foreach (string term in new[] { "Mover", "Saltar", "Disparar", "Pluma", "segundo salto", "Apple", "Botiquín", "Citrus", "Resistencia", "Camisa", "Lanzapiedra", "Sin límite de tiempo", "Chicken" }) Require(text.Contains(term), "Missing tutorial explanation: " + term);
        Require(text.Contains("Clic izquierdo") && text.Contains("Pluma · 5 segundos") && !text.Contains("checkpoint") && !text.Contains("J /"), "Guide must explain the current controls and timed Feather");
        var icons = ui.guideContent.GetComponentsInChildren<Image>().Where(i => i.name == "Icono").ToArray();
        Require(icons.Length == 8 && icons.All(i => i.sprite != null), "Eight powerup illustrations required");
        ui.continueButton.onClick.Invoke();
        yield return 0.2f;
        Require(!ui.GuideOpen && Time.timeScale == 1 && !TutorialCanvasUI.BlocksGameplay, "Continue button should resume gameplay");
        Require(EventSystem.current.currentSelectedGameObject == null, "Closing guide should release UI focus");
        var p = ui.player;
        Vector3 start = p.transform.position;
        p.ApplyPowerUp(PowerUp.Kind.Feather, "temporaryFeather");
        Require(p.DoubleJump && p.FeatherRemaining > 4.9f, "Feather must activate for 5 seconds");
        p.ApplyPowerUp(PowerUp.Kind.Rock, "noTimeout");
        yield return 6;
        Require(!p.DoubleJump && p.FeatherRemaining == 0, "Feather must expire after 5 seconds");
        p.transform.position = new Vector3(60, -2.8f, 0);
        yield return 0.1f;
        p.Respawn();
        Require(Mathf.Abs(p.transform.position.x - start.x) < 0.01f, "Crossing the old checkpoint must still respawn at the beginning");
        Require(p.RockAmmo == 5 && p.WeaponName.StartsWith("Piedras"), "Rocks must remain available after waiting more than 5 seconds");
        for (int i = 0; i < 5; i++)
        {
            Require(p.TryShoot() && p.RockAmmo == 4 - i, "Stone shot count must decrease exactly once");
            yield return 0.95f;
        }
        Require(p.WeaponName == "Pistola" && p.RockAmmo == 0, "Fifth stone should restore the normal weapon");
        ui.guideButton.onClick.Invoke();
        float pausedTime = Time.time;
        yield return 0.25f;
        Require(ui.GuideOpen && Time.timeScale == 0 && Time.time == pausedTime && ui.continueText.text.Contains("Continuar"), "Guide button should reopen and pause");
        ui.continueButton.onClick.Invoke();
        yield return 0.1f;
        Require(!ui.GuideOpen && Time.timeScale == 1, "Guide close should restore time");
    }
}
