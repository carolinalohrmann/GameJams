using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ApplyCombatBalance
{
    public static void Run()
    {
        try
        {
            for (int number = 1; number <= 8; number++)
            {
                string name = number == 1 ? "Tutorial" : "Level" + number;
                var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/" + name + ".unity");
                foreach (var pickup in UnityEngine.Object.FindObjectsByType<PowerUp>(FindObjectsInactive.Include))
                {
                    if (!PowerUp.IsAvailableInStage(pickup.kind, number))
                    {
                        UnityEngine.Object.DestroyImmediate(pickup.gameObject);
                        continue;
                    }
                    pickup.durationSeconds = PowerUp.DefaultDuration(pickup.kind);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
                    EditorUtility.SetDirty(pickup);
                }
                var stage = UnityEngine.Object.FindAnyObjectByType<FinalCampaignStage>();
                if (stage != null)
                {
                    stage.fixedPowerUps = Array.FindAll(stage.fixedPowerUps, kind => PowerUp.IsAvailableInStage(kind, number));
                    EditorUtility.SetDirty(stage);
                }
                var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
                foreach (var label in ui.GetComponentsInChildren<Text>(true))
                {
                    label.text = label.text.Replace("Espacio/W/arriba", "Espacio")
                        .Replace("Espacio, W o arriba", "Espacio")
                        .Replace("Camisa · 20 segundos", "Camisa · 10 segundos")
                        .Replace("Chicken / AK · 30 segundos", "Chicken / AK · 15 segundos");
                    EditorUtility.SetDirty(label);
                }
                if (number == 8)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        var row = ui.guideContent.Find("Poder " + i);
                        if (row == null) continue;
                        bool visible = i < stage.fixedPowerUps.Length;
                        row.gameObject.SetActive(visible);
                        if (!visible) continue;
                        var kind = stage.fixedPowerUps[i];
                        row.Find("Icono").GetComponent<Image>().sprite = stage.powerUpIcons[(int)kind];
                        row.Find("Nombre").GetComponent<Text>().text = kind == PowerUp.Kind.Medkit ? "Botiquín · instantáneo"
                            : kind == PowerUp.Kind.Apple ? "Apple · permanente"
                            : kind == PowerUp.Kind.Citrus ? "Citrus · hasta salir del nivel" : "Pluma · hasta salir del nivel";
                        row.Find("Explicacion").GetComponent<Text>().text = kind == PowerUp.Kind.Medkit ? "Cura 2 corazones."
                            : kind == PowerUp.Kind.Apple ? "+10 de vida y +1 de daño. Se guarda."
                            : kind == PowerUp.Kind.Citrus ? "+50% movimiento y disparo." : "Permite un segundo salto.";
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("COMBAT_BALANCE_SCENES_OK: eight scenes updated, 10s minigun/15s AK, Space-only help, no minigun on levels 7/8 and no AK on level 8");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
