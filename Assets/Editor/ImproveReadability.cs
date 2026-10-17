using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ImproveReadability
{
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/Level1.unity");
        const string path = "Assets/0_2prefafs/PowerUpCapsule.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        Resize(prefab.GetComponent<PowerUp>());
        PrefabUtility.SaveAsPrefabAsset(prefab, path);
        PrefabUtility.UnloadPrefabContents(prefab);
        foreach (var power in Object.FindObjectsByType<PowerUp>()) Resize(power);
        SpaceVegetation();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("READABILITY_BUILD_OK: pickup icons 50% larger, matching triggers, vegetation clear of patrols");
    }
    static void Resize(PowerUp power)
    {
        power.icon.transform.localScale = Vector3.one * 0.75f;
        power.icon.sortingOrder = 6;
        var trigger = power.GetComponent<CapsuleCollider2D>();
        trigger.size = new Vector2(0.85f, 0.95f);
        foreach (var component in new Component[] { power.icon.transform, power.icon, trigger })
        {
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }
    public static void SpaceVegetation()
    {
        var enemies = Object.FindObjectsByType<Enemy>();
        var powers = Object.FindObjectsByType<PowerUp>();
        var grounds = Object.FindObjectsByType<BoxCollider2D>().Where(c => c.name.StartsWith("Suelo ")).ToArray();
        var bushes = Object.FindObjectsByType<SpriteRenderer>().Where(s => s.name.StartsWith("Vegetacion ")).OrderBy(s => s.transform.position.x).ToArray();
        var placed = new System.Collections.Generic.List<Bounds>();
        foreach (var bush in bushes)
        {
            float original = bush.transform.position.x;
            bool found = false;
            for (int step = 0; step <= 96; step++)
            {
                float x = original + (step % 2 == 0 ? 1 : -1) * ((step + 1) / 2) * 0.25f;
                var bounds = bush.bounds;
                bounds.center += Vector3.right * (x - bush.transform.position.x);
                bool onGround = grounds.Any(g => bounds.min.x >= g.bounds.min.x + 0.2f && bounds.max.x <= g.bounds.max.x - 0.2f);
                bool nearEnemy = enemies.Any(e => Mathf.Abs(x - e.transform.position.x) < e.patrolDistance + e.GetComponent<Collider2D>().bounds.extents.x + bounds.extents.x + 0.7f);
                bool nearPickup = powers.Any(p => Mathf.Abs(x - p.transform.position.x) < bounds.extents.x + 0.8f);
                bool crowded = placed.Any(b => Mathf.Abs(x - b.center.x) < bounds.extents.x + b.extents.x + 0.9f);
                if (!onGround || nearEnemy || nearPickup || crowded || x > 114) continue;
                var position = bush.transform.position; position.x = x; bush.transform.position = position;
                placed.Add(bounds); found = true; break;
            }
            // Keep open ground where there is no room for decoration and combat.
            if (!found) Object.DestroyImmediate(bush.gameObject);
        }
    }
}
