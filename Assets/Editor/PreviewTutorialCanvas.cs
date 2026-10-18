using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PreviewTutorialCanvas
{
    static RenderTexture target;
    static Camera camera;
    static int frame;
    static string outputName = "tutorial-canvas";
    public static void RenderEndingCredits()
    {
        outputName = "ending-credits";
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Level8.unity");
        var ui = Object.FindAnyObjectByType<TutorialCanvasUI>();
        ui.ConfigureStageText();
        ui.guidePanel.SetActive(false); ui.completionPanel.SetActive(true);
        ui.completionTitleText.text = "Felicidades, eres housestatico";
        ui.completionDescriptionText.text = "Derrotaste al Cazador y completaste la ruta secreta.";
        ui.MostrarCreditosFinales();
        camera = Camera.main; camera.aspect = 16f / 9;
        target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
        var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        frame = 0; EditorApplication.update += Frame;
    }
    public static void RestoreAndRender()
    {
        BuildTutorialCanvas.ApplyAll();
        Render();
    }
    public static void Render()
    {
        outputName = "tutorial-canvas";
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        var ui = Object.FindAnyObjectByType<TutorialCanvasUI>();
        camera = Camera.main; camera.aspect = 16f / 9;
        target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
        var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        ui.guidePanel.SetActive(true); Canvas.ForceUpdateCanvases();
        frame = 0; EditorApplication.update += Frame;
    }
    static void Frame()
    {
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        if (++frame < 16) return;
        RenderTexture.active = target;
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
        Directory.CreateDirectory("Logs/Previews"); File.WriteAllBytes("Logs/Previews/" + outputName + ".png", texture.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null; Object.DestroyImmediate(texture); target.Release(); Object.DestroyImmediate(target);
        EditorApplication.update -= Frame; Debug.Log("TUTORIAL_CANVAS_PREVIEW_OK"); EditorApplication.Exit(0);
    }
}
