using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class PreviewGameLevels
{
    static int view, frame;
    static RenderTexture target;
    static PreviewGameLevels()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PreviewGameLevels", false)) return;
            view = frame = 0; target = new RenderTexture(1280, 720, 24); target.Create();
            Directory.CreateDirectory("Logs/Previews"); EditorApplication.update += Frame;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        SessionState.SetBool("PreviewGameLevels", true); EditorApplication.EnterPlaymode();
    }
    public static void RunWithHUD()
    {
        SessionState.SetBool("PreviewGameLevelsHUD", true); Run();
    }
    static void Frame()
    {
        try
        {
            var level = UnityEngine.Object.FindAnyObjectByType<LevelOne>();
            var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
            if (level == null || ui == null) return;
            ui.SetGuide(false); ui.GetComponent<Canvas>().enabled = false; level.enabled = false;
            level.player.enabled = false;
            level.player.persistUpgrades = false;
            foreach (var power in UnityEngine.Object.FindObjectsByType<PowerUp>()) power.GetComponent<Collider2D>().enabled = false;
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>()) enemy.enabled = false;
            var camera = level.followCamera; camera.aspect = 16f / 9;
            if (SessionState.GetBool("PreviewGameLevelsHUD", false))
            {
                var canvas = ui.GetComponent<Canvas>(); canvas.enabled = true;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                camera.targetTexture = target; Canvas.ForceUpdateCanvases();
            }
            bool exit = view % 2 == 1;
            float x = exit ? level.endX : level.stageNumber == 1 ? 54 : 24;
            foreach (var surface in Physics2D.RaycastAll(new Vector2(x - 2, 8), Vector2.down, 20))
                if (!surface.collider.isTrigger && surface.collider.attachedRigidbody == null)
                { level.player.transform.position = new Vector3(x - 2, surface.point.y + 0.6f, 0); break; }
            level.player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            camera.transform.position = new Vector3(x, -0.8f, -10);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            if (++frame < 14) return;
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes("Logs/Previews/level" + level.stageNumber + (exit ? "-exit.png" : "-route.png"), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture); RenderTexture.active = null;
            frame = 0; view++;
            if (view == 2) SceneManager.LoadScene("Level2");
            if (view == 4) SceneManager.LoadScene("Level3");
            if (view == 6) SceneManager.LoadScene("Level4");
            if (view == 8) SceneManager.LoadScene("Level5");
            if (view < 10) return;
            Debug.Log("GAME_LEVELS_PREVIEWS_OK"); SessionState.SetBool("PreviewGameLevels", false); SessionState.SetBool("PreviewGameLevelsHUD", false); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); SessionState.SetBool("PreviewGameLevels", false); EditorApplication.Exit(1); }
    }
}
