using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class PreviewBasicGuide
{
    static int frame;
    static RenderTexture target;
    static PreviewBasicGuide()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("PreviewBasicGuide", false)) return;
            frame = 0; target = new RenderTexture(1280, 720, 24); target.Create();
            Directory.CreateDirectory("Logs/Previews"); EditorApplication.update += Frame;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        UnityEngine.Object.FindAnyObjectByType<player>().persistUpgrades = false;
        SessionState.SetBool("PreviewBasicGuide", true); EditorApplication.EnterPlaymode();
    }
    static void Frame()
    {
        var ui = TutorialCanvasUI.Instance; if (ui == null) return;
        ui.SetGuide(frame < 20); ui.player.persistUpgrades = false; ui.player.enabled = false; ui.level.enabled = false;
        ui.player.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var camera = ui.level.followCamera; camera.aspect = 16f / 9; camera.targetTexture = target;
        camera.transform.position = new Vector3(-2, -0.8f, -10);
        var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        if (++frame != 20 && frame != 40) return;
        RenderTexture.active = target; var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
        File.WriteAllBytes("Logs/Previews/" + (frame == 20 ? "simple-guide.png" : "tutorial-sky.png"), texture.EncodeToPNG());
        Object.DestroyImmediate(texture); RenderTexture.active = null;
        if (frame < 40) return;
        Debug.Log("BASIC_GUIDE_PREVIEW_OK"); SessionState.SetBool("PreviewBasicGuide", false); EditorApplication.Exit(0);
    }
}
