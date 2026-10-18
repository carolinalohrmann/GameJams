using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

public static class PolishCombatAndLayout
{
    [MenuItem("GameJams/Actualizar colisiones, corazones y disparos")]
    public static void Apply()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        FitEnemyBodies();
        var normal = ImportFrames("Assets/Art/Shots/Efecto_007.png");
        var rapid = ImportFrames("Assets/Art/Shots/Efecto_008.png");
        const string heartPath = "Assets/Art/Health/Hearts.png";
        Import(heartPath, new[] { new SpriteRect { name = "Heart", rect = new Rect(16, 35, 15, 13),
            pivot = Vector2.one * 0.5f, alignment = SpriteAlignment.Center, spriteID = GUID.Generate() } });
        var heart = AssetDatabase.LoadAllAssetsAtPath(heartPath).OfType<Sprite>().Single();
        const string projectilePath = "Assets/0_2prefafs/bullets.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(projectilePath);
        prefab.GetComponent<bullet>().normalFrames = normal;
        prefab.GetComponent<bullet>().rapidFrames = rapid;
        PrefabUtility.SaveAsPrefabAsset(prefab, projectilePath);
        PrefabUtility.UnloadPrefabContents(prefab);
        for (int stage = 1; stage <= 5; stage++)
        {
            var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/" + (stage == 1 ? "Tutorial" : "Level" + stage) + ".unity");
            var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
            ConfigureHearts(ui, heart);
            var layout = UnityEngine.Object.FindAnyObjectByType<RandomLevelLayout>();
            if (layout != null) { layout.bonusPlatformChance = stage >= 4 ? 0.8f : 0.65f; EditorUtility.SetDirty(layout); }
            else
            {
                Physics2D.SyncTransforms();
                var floors = UnityEngine.Object.FindObjectsByType<BoxCollider2D>().Where(c => !c.isTrigger && c.attachedRigidbody == null).ToArray();
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>().Where(e => !e.ranged))
                {
                    var box = enemy.GetComponent<Collider2D>();
                    var support = floors.Where(c => c.bounds.min.x <= box.bounds.center.x && c.bounds.max.x >= box.bounds.center.x && c.bounds.max.y <= box.bounds.center.y)
                        .OrderByDescending(c => c.bounds.max.y).FirstOrDefault();
                    if (support == null) continue;
                    enemy.transform.position += Vector3.up * (support.bounds.max.y + 0.025f - box.bounds.min.y);
                    Physics2D.SyncTransforms();
                    foreach (var obstacle in floors)
                    {
                        if (obstacle == support || box.Distance(obstacle).distance >= -0.001f) continue;
                        float x = box.bounds.center.x >= obstacle.bounds.center.x
                            ? obstacle.bounds.max.x + box.bounds.extents.x + 0.04f
                            : obstacle.bounds.min.x - box.bounds.extents.x - 0.04f;
                        enemy.transform.position += Vector3.right * (x - box.bounds.center.x);
                        Physics2D.SyncTransforms();
                    }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("COMBAT_POLISH_BUILD_OK");
    }
    static void FitEnemyBodies()
    {
        foreach (string name in new[] { "FantasySkeleton", "Goblin", "Mushroom", "ZombieDog", "FlyingEye", "Melee" })
        {
            string path = "Assets/0_2prefafs/Enemy" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var enemy = root.GetComponent<Enemy>(); var visual = enemy.visual;
            var sprite = visual.sprite;
            var texture = new Texture2D(2, 2);
            texture.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            var rect = sprite.rect; int min = (int)rect.yMax, max = (int)rect.yMin;
            for (int y = (int)rect.yMin; y < rect.yMax; y++)
            for (int x = (int)rect.xMin; x < rect.xMax; x++)
                if (texture.GetPixel(x, y).a > 0.1f) { min = Mathf.Min(min, y); max = Mathf.Max(max, y + 1); }
            UnityEngine.Object.DestroyImmediate(texture);
            var box = root.GetComponent<BoxCollider2D>();
            float height = Mathf.Max(box.size.y, (max - min) / sprite.pixelsPerUnit * visual.transform.localScale.y + 0.03f);
            visual.transform.localPosition -= Vector3.up * ((height - box.size.y) / 2);
            box.size = new Vector2(box.size.x, height);
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
        }
    }
    public static void ConfigureHearts(TutorialCanvasUI ui, Sprite heart = null)
    {
        if (heart == null) heart = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Health/Hearts.png").OfType<Sprite>().FirstOrDefault();
        if (heart == null) return;
        var component = ui.GetComponent<HealthHeartsUI>() ?? ui.gameObject.AddComponent<HealthHeartsUI>();
        component.player = ui.player; component.heart = heart;
        var header = ui.healthText.transform.parent;
        var container = header.Find("Corazones") as RectTransform;
        if (container == null)
        {
            container = new GameObject("Corazones", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(header, false);
        }
        container.anchorMin = new Vector2(0, 1); container.anchorMax = new Vector2(1, 1); container.pivot = new Vector2(0, 1);
        container.anchoredPosition = new Vector2(84, -10); container.sizeDelta = new Vector2(-270, 30);
        component.container = container;
        ui.healthText.text = "VIDA";
    }
    static Sprite[] ImportFrames(string path)
    {
        var rects = Enumerable.Range(0, 20).Select(i => new SpriteRect { name = "Frame_" + i.ToString("D2"),
            rect = new Rect(i % 5 * 48, (3 - i / 5) * 48, 48, 48), pivot = Vector2.one * 0.5f,
            alignment = SpriteAlignment.Center, spriteID = GUID.Generate() }).ToArray();
        Import(path, rects);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }
    static void Import(string path, SpriteRect[] rects)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 48;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true; importer.SaveAndReimport();
        typeof(BuildGameLevels).GetMethod("SetSpriteRects", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { path, rects });
    }
    public static void BuildAndExit()
    {
        try { Apply(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
