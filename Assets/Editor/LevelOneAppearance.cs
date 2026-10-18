using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LevelOneAppearance
{
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sr.name.StartsWith("Fondo geometrico ")) Object.DestroyImmediate(sr.gameObject);
            else sr.color = Color.white;
        }
        Camera.main.backgroundColor = Color.gray;
        EditorSceneManager.SaveScene(scene);
        foreach (string name in new[] { "EnemyMelee", "EnemyRanged", "bullets" })
        {
            string path = "Assets/0_2prefafs/" + name + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true)) sr.color = Color.white;
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL_ONE_APPEARANCE_OK: fondo eliminado; sprites blancos");
    }
}
