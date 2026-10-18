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
        AlignBarn(barn.GetComponent<SpriteRenderer>());
        var p = Object.FindAnyObjectByType<player>();
        p.weaponVisual.transform.localPosition = new Vector3(0.27f, 0.20f, 0);
        p.weaponVisual.transform.localScale = Vector3.one;
        p.shootPoint.localPosition = new Vector3(0.65f, 0.23f, 0);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("PRESENTATION_APPLY_OK");
    }
    public static float VisibleBottom(Sprite sprite)
    {
        var texture = new Texture2D(2, 2);
        texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        var rect = sprite.rect;
        var columnBases = new System.Collections.Generic.List<int>();
        // Perspective art has low corners and hanging moss. Align the broad base,
        // rather than the single lowest pixel that left most of the building floating.
        for (int x = (int)(rect.xMin + rect.width * 0.1f); x < rect.xMax - rect.width * 0.1f; x++)
        for (int y = (int)rect.yMin; y < rect.yMax; y++)
            if (texture.GetPixel(x, y).a >= 0.9f) { columnBases.Add(y); break; }
        columnBases.Sort();
        int bottom = columnBases.Count == 0 ? (int)rect.yMin : columnBases[(columnBases.Count - 1) * 4 / 5];
        Object.DestroyImmediate(texture);
        return (bottom - rect.yMin - sprite.pivot.y) / sprite.pixelsPerUnit;
    }
    public static void AlignBarn(SpriteRenderer barn)
    {
        Physics2D.SyncTransforms();
        float ground = -4;
        foreach (var hit in Physics2D.RaycastAll(new Vector2(barn.transform.position.x, 8), Vector2.down, 20))
            if (!hit.collider.isTrigger && hit.collider.attachedRigidbody == null)
            {
                ground = hit.point.y;
                foreach (var soil in hit.collider.GetComponentsInChildren<SpriteRenderer>()) soil.sortingOrder = Mathf.Max(2, soil.sortingOrder);
                break;
            }
        barn.sortingOrder = 1;
        var point = barn.transform.position;
        point.y = ground - VisibleBottom(barn.sprite) * barn.transform.lossyScale.y - 0.06f;
        barn.transform.position = point;
    }
    public static void ApplyBuildings()
    {
        foreach (string name in new[] { "Tutorial", "Level6", "Level7", "Level8" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/" + name + ".unity");
            Physics2D.SyncTransforms();
            var barn = GameObject.Find("Granero - meta");
            if (barn != null) AlignBarn(barn.GetComponent<SpriteRenderer>());
            var stage = Object.FindAnyObjectByType<FinalCampaignStage>();
            if (stage != null) stage.towerFootOffset = VisibleBottom(stage.towerSprite);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("BUILDING_BASE_ALIGNMENT_OK");
    }
}
