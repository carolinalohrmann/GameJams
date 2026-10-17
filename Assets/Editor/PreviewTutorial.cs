using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PreviewTutorial
{
    static int index, frame;
    static readonly float[] views = { -2, 26, 54, 118 };
    static Camera camera;
    static RenderTexture target;
    public static void Render()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Level1.unity");
        Directory.CreateDirectory("Logs/Previews");
        camera = Camera.main; camera.aspect = 16f / 9;
        target = new RenderTexture(1280, 720, 24); target.Create();
        index = frame = 0;
        EditorApplication.update += Frame;
    }
    static void Frame()
    {
        camera.transform.position = new Vector3(views[index], -0.8f, -10);
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        // Allow sprite textures and GPU buffers to settle before reading the view.
        if (++frame < 12) return;
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
        File.WriteAllBytes("Logs/Previews/tutorial-" + views[index] + ".png", image.EncodeToPNG());
        RenderTexture.active = null; Object.DestroyImmediate(image);
        frame = 0;
        if (++index < views.Length) return;
        EditorApplication.update -= Frame;
        target.Release(); Object.DestroyImmediate(target);
        Debug.Log("TUTORIAL_PREVIEWS_OK"); EditorApplication.Exit(0);
    }
}
