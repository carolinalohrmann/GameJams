using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PolishTutorial
{
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/Level1.unity");
        var p = Object.FindAnyObjectByType<player>();
        var box = p.GetComponent<BoxCollider2D>();
        if (box != null) Object.DestroyImmediate(box);
        var capsule = p.GetComponent<CapsuleCollider2D>();
        if (capsule == null) capsule = p.gameObject.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical; capsule.size = new Vector2(0.65f, 1.2f);
        capsule.sharedMaterial = null;
        var rest = GameObject.Find("Descanso"); if (rest != null) Object.DestroyImmediate(rest);
        string[] platforms = { "Primer salto", "Salto con pluma", "Practica doble salto" };
        for (int n = 0; n < platforms.Length; n++)
        {
            var platform = GameObject.Find(platforms[n]);
            var old = platform.GetComponentsInChildren<SpriteRenderer>();
            var sprite = old[0].sprite; var material = old[0].sharedMaterial;
            foreach (var sr in old) Object.DestroyImmediate(sr.gameObject);
            float width = platform.GetComponent<BoxCollider2D>().size.x;
            int count = n == 1 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Jungle tramo " + (i + 1)); go.transform.SetParent(platform.transform, false);
                go.transform.localPosition = new Vector3(-width / 2 + width / count * (i + 0.5f), 0, 0);
                go.transform.localScale = new Vector3(width / count / sprite.bounds.size.x, 0.5f / sprite.bounds.size.y, 1);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = material;
            }
        }
        const string path = "Assets/0_2prefafs/PowerUpCapsule.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        var background = prefab.transform.Find("Capsula");
        if (background != null) Object.DestroyImmediate(background.gameObject);
        prefab.GetComponent<PowerUp>().durationSeconds = 5;
        PrefabUtility.SaveAsPrefabAsset(prefab, path); PrefabUtility.UnloadPrefabContents(prefab);
        foreach (var power in Object.FindObjectsByType<PowerUp>())
        {
            power.durationSeconds = PowerUp.DefaultDuration(power.kind);
            PrefabUtility.RecordPrefabInstancePropertyModifications(power);
            foreach (var sr in power.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.name == "Capsula") { sr.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(sr); }
        }
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("TUTORIAL_POLISH_OK: capsule player, 1/2 stretched sections, no pickup backgrounds, no Descanso, explicit timers");
    }
}
