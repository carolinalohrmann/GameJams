using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ApplyRequestedRefinements
{
    [MenuItem("GameJams/Actualizar guía de habilidades y arena final")]
    public static void Apply()
    {
        for (int stage = 1; stage <= 8; stage++)
        {
            string name = stage == 1 ? "Tutorial" : "Level" + stage;
            var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/" + name + ".unity");
            var ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
            var level = ui.level;
            BuildTutorialCanvas.Create(level);
            ui = UnityEngine.Object.FindAnyObjectByType<TutorialCanvasUI>();
            var campaign = level.GetComponent<FinalCampaignStage>();
            if (campaign != null) campaign.ConfigureUI(ui);
            else
            {
                ui.stageTitleText.text = stage == 1 ? "Tutorial" : "NIVEL " + stage;
                ui.stageObjectiveText.text = stage == 1 ? "Cruza la jungla y llega al granero."
                    : stage == 2 || stage == 4 ? "Atraviesa el prado y llega a la salida." : "Atraviesa el bosque y llega a la salida.";
                ui.completionTitleText.text = stage == 1 ? "Tutorial completado" : "Nivel " + stage + " completado";
                ui.completionDescriptionText.text = "Continua a la siguiente etapa.";
                ui.restartButton.GetComponentInChildren<Text>().text = "Siguiente nivel";
            }
            if (stage == 1) Camera.main.backgroundColor = new Color(0.48f, 0.79f, 0.96f);
            if (stage == 2)
            {
                var layout = ui.level.layout;
                const string folder = "Assets/Art/Multi Platformer Tileset Free/GrassLand/Background/";
                layout.backgroundLayers = Enumerable.Range(1, 3).Select(i => AssetDatabase.LoadAllAssetsAtPath(folder + "GrassLand_Background_" + i + ".png").OfType<Sprite>().Single()).ToArray();
                layout.backgroundHeights = new[] { 12f, 9f, 8f }; layout.backgroundBaseY = -5.4f;
            }
            if (stage == 6)
            {
                var layout = ui.level.layout;
                layout.backgroundLayers = layout.backgroundLayers.Take(2).ToArray();
                layout.backgroundHeights = new[] { 15f, 15f };
                layout.scenerySprites = Array.Empty<Sprite>();
            }
            if (stage == 8)
            {
                ArrangeArena(GameObject.Find("Hunter fixed arena").transform);
                BuildFinalCampaign.RefreshHunterPickups(campaign);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets(); Debug.Log("REQUESTED_REFINEMENTS_BUILD_OK");
    }
    public static void SimplifyGuide(TutorialCanvasUI ui)
    {
        var panel = ui.guideContent;
        foreach (Transform child in panel.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        panel.sizeDelta = new Vector2(640, 600);
        panel.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.95f);
        ui.guidePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);
        ui.stageTitleText = Label(panel, "Titulo", 20, 15, 600, 34, 24);
        ui.stageObjectiveText = Label(panel, "Objetivo", 20, 55, 600, 50, 17);
        ui.simpleGuideText = Label(panel, "Instrucciones", 20, 115, 600, 365, 16);
        var hint = Label(panel, "Doble salto", 20, 480, 600, 48, 15);
        hint.text = "Morir, caer o pulsar R resta 2 de vida máxima. Mínimo: 1.";
        var buttonRect = Rect(panel, "Continuar", 210, 545, 220, 36);
        var image = buttonRect.gameObject.AddComponent<Image>(); image.color = new Color(0.75f, 0.75f, 0.75f);
        ui.continueButton = buttonRect.gameObject.AddComponent<Button>(); ui.continueButton.targetGraphic = image;
        ui.continueText = Label(buttonRect, "Texto", 0, 0, 220, 36, 18); ui.continueText.alignment = TextAnchor.MiddleCenter;
        ui.continueText.text = "Empezar [Enter]";
        // Keep useful editor previews; runtime updates these for the actual stage.
        var campaign = ui.level.GetComponent<FinalCampaignStage>();
        if (campaign != null) campaign.ConfigureUI(ui);
        else
        {
            ui.stageTitleText.text = ui.level.stageNumber == 1 ? "Tutorial" : "Nivel " + ui.level.stageNumber;
            ui.stageObjectiveText.text = ui.level.stageNumber == 1 ? "Llega al granero y prueba los poderes." : "Llega a la salida.";
        }
    }
    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
    }
    static Text Label(Transform parent, string name, float x, float y, float w, float h, int size)
    {
        var label = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = size; label.color = Color.black;
        label.raycastTarget = false; return label;
    }
    public static void ArrangeArena(Transform world)
    {
        var top = world.Find("Floor/Top").GetComponent<SpriteRenderer>().sprite;
        foreach (var collider in world.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "Step" || c.name.StartsWith("Platform ") || c.name.StartsWith("Arena ledge ")).ToArray())
            UnityEngine.Object.DestroyImmediate(collider.gameObject);
        float[] left = { -4, 0.2f, 6, 11.8f, 18, 23, 27.8f };
        float[] surface = { -2.7f, -1.3f, -1.2f, -0.7f, -1.2f, -1.3f, -2.7f };
        for (int i = 0; i < left.Length; i++)
        {
            float width = i == 3 ? 3.5f : i == 6 ? 2.2f : 3;
            var root = new GameObject("Arena ledge " + i); root.transform.SetParent(world, false);
            root.transform.position = new Vector3(left[i] + width / 2, surface[i] - 0.25f, 0);
            root.AddComponent<BoxCollider2D>().size = new Vector2(width, 0.5f);
            var visual = new GameObject("Top").AddComponent<SpriteRenderer>(); visual.transform.SetParent(root.transform, false);
            visual.sprite = top; visual.sortingOrder = 1; visual.transform.localScale = new Vector3(width / top.bounds.size.x, 0.5f / top.bounds.size.y, 1);
        }
    }
    public static void BuildAndExit()
    {
        try { Apply(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
