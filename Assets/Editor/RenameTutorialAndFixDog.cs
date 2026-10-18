using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RenameTutorialAndFixDog
{
    public static void Apply()
    {
        const string oldPath = "Assets/0_0Scenes/Level1.unity";
        const string tutorialPath = "Assets/0_0Scenes/Tutorial.unity";
        bool hasForest = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/0_0Scenes/Level3.unity") != null;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(tutorialPath) == null)
        {
            string error = AssetDatabase.MoveAsset(oldPath, tutorialPath);
            if (!string.IsNullOrEmpty(error)) throw new System.InvalidOperationException(error);
        }
        const string dogPath = "Assets/0_2prefafs/EnemyZombieDog.prefab";
        var dog = PrefabUtility.LoadPrefabContents(dogPath);
        dog.GetComponent<Enemy>().spriteFacesLeft = true;
        PrefabUtility.SaveAsPrefabAsset(dog, dogPath);
        PrefabUtility.UnloadPrefabContents(dog);
        foreach (string path in new[] { tutorialPath, "Assets/0_0Scenes/Level2.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            var level = Object.FindAnyObjectByType<LevelOne>();
            var ui = Object.FindAnyObjectByType<TutorialCanvasUI>();
            if (level.stageNumber == 1)
            {
                ui.stageTitleText.text = "Tutorial";
                ui.completionTitleText.text = "Tutorial completado";
            }
            else level.nextSceneName = hasForest ? "Level3" : "Tutorial";
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(tutorialPath, true),
            new EditorBuildSettingsScene("Assets/0_0Scenes/Level2.unity", true)
        };
        if (hasForest) scenes.Add(new EditorBuildSettingsScene("Assets/0_0Scenes/Level3.unity", true));
        foreach (string name in new[] { "Level4", "Level5" })
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/0_0Scenes/" + name + ".unity") != null)
                scenes.Add(new EditorBuildSettingsScene("Assets/0_0Scenes/" + name + ".unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets(); Debug.Log("TUTORIAL_RENAMED_DOG_FIXED_OK");
    }
}
