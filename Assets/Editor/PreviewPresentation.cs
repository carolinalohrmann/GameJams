using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PreviewPresentation
{
    static int index, frame;
    static Camera camera;
    static RenderTexture target;
    static GameObject template, actor;
    static readonly string[] names = { "minigun-derecha", "minigun-izquierda", "ak-derecha", "ak-izquierda", "piedras", "granero" };
    public static void Render()
    {
        AdjustPresentation.Apply();
        Directory.CreateDirectory("Logs/Presentation");
        camera = Camera.main; camera.aspect = 16f / 9;
        template = Object.FindAnyObjectByType<player>().gameObject; template.SetActive(false);
        target = new RenderTexture(1280, 720, 24); target.Create();
        index = frame = 0; Setup(); EditorApplication.update += Frame;
    }
    static void Setup()
    {
        if (actor != null) Object.DestroyImmediate(actor);
        actor = Object.Instantiate(template); actor.SetActive(true);
        var p = actor.GetComponent<player>(); p.animator.enabled = false;
        actor.transform.position = new Vector3(index == 5 ? 114 : -8, -3.385f, 0);
        if (index < 2) p.ApplyPowerUp(PowerUp.Kind.Shirt, "preview");
        else if (index < 4) p.ApplyPowerUp(PowerUp.Kind.Chicken, "preview");
        else if (index == 4)
        {
            p.ApplyPowerUp(PowerUp.Kind.Rock, "preview");
            var stone = new GameObject("Muestra piedra lanzada"); stone.transform.SetParent(actor.transform, false);
            stone.transform.localPosition = new Vector3(1.5f, 0.25f, 0);
            stone.transform.localScale = Vector3.one * (0.85f / p.rockSprite.bounds.size.x);
            var sr = stone.AddComponent<SpriteRenderer>(); sr.sprite = p.rockSprite; sr.sharedMaterial = p.weaponVisual.sharedMaterial; sr.sortingOrder = 5;
        }
        p.SetFacingDirection(index == 1 || index == 3 ? -1 : 1);
        camera.orthographicSize = index == 5 ? 4 : 1.7f;
        camera.transform.position = new Vector3(index == 5 ? 117 : -7.7f, index == 5 ? -0.8f : -2.8f, -10);
    }
    static void Frame()
    {
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        if (++frame < 12) return;
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
        File.WriteAllBytes("Logs/Presentation/" + names[index] + ".png", image.EncodeToPNG());
        RenderTexture.active = null; Object.DestroyImmediate(image);
        frame = 0;
        if (++index < names.Length) { Setup(); return; }
        EditorApplication.update -= Frame; target.Release(); Object.DestroyImmediate(target);
        Debug.Log("PRESENTATION_PREVIEWS_OK"); EditorApplication.Exit(0);
    }
}
