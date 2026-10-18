using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AdjustPresentation
{
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Art/Buildings/Granero.png");
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        // Bottom of the visible wood, ignoring the transparent glow and border.
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 94f / 1024);
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
        var barn = GameObject.Find("Granero - meta");
        var position = barn.transform.position; position.y = -4.02f; barn.transform.position = position;
        var p = Object.FindAnyObjectByType<player>();
        p.weaponVisual.transform.localPosition = new Vector3(0.27f, 0.20f, 0);
        p.weaponVisual.transform.localScale = Vector3.one;
        p.shootPoint.localPosition = new Vector3(0.65f, 0.23f, 0);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("PRESENTATION_APPLY_OK");
    }
}
