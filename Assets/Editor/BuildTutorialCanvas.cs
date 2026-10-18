using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class BuildTutorialCanvas
{
    static Font font;
    static readonly Color ink = new Color(0.95f, 0.95f, 0.95f);
    static readonly Color muted = new Color(0.74f, 0.77f, 0.8f);
    [InitializeOnLoadMethod]
    static void ApplyPendingHealthReset()
    {
        string statsRequest = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/ResetPlayerStats.request"));
        if (File.Exists(statsRequest))
        {
            ResetPlayerStats();
            File.Delete(statsRequest);
        }
        string request = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/ResetHearts.request"));
        if (!File.Exists(request)) return;
        ResetHearts();
        File.Delete(request);
    }
    [MenuItem("GameJams/Reiniciar corazones a 10")]
    public static void ResetHearts()
    {
        PlayerPrefs.SetInt(player.SavePrefix + "MaxHealth", player.BaseHealth);
        PlayerPrefs.SetInt(player.SavePrefix + "BaseHealth", player.BaseHealth);
        PlayerPrefs.Save();
        Debug.Log("HEARTS_RESET_OK: 10 corazones para la siguiente partida.");
    }
    [MenuItem("GameJams/Reiniciar vida y daño iniciales")]
    public static void ResetPlayerStats()
    {
        PlayerPrefs.SetInt(player.SavePrefix + "MaxHealth", player.BaseHealth);
        PlayerPrefs.SetInt(player.SavePrefix + "BaseHealth", player.BaseHealth);
        PlayerPrefs.SetInt(player.SavePrefix + "AttackDamage", 1);
        PlayerPrefs.Save();
        if (Application.isPlaying) Object.FindAnyObjectByType<player>()?.ResetAppleUpgrades();
        Debug.Log("PLAYER_STATS_RESET_OK: 10 corazones y 1 de daño para la siguiente partida.");
    }
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/Tutorial.unity");
        Create(Object.FindAnyObjectByType<LevelOne>());
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("TUTORIAL_CANVAS_BUILD_OK");
    }
    [MenuItem("GameJams/Actualizar canvas de habilidades")]
    public static void ApplyAll()
    {
        for (int stage = 1; stage <= 8; stage++)
        {
            string name = stage == 1 ? "Tutorial" : "Level" + stage;
            var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/" + name + ".unity");
            Create(Object.FindAnyObjectByType<LevelOne>());
            foreach (var pickup in Object.FindObjectsByType<PowerUp>())
            {
                pickup.durationSeconds = PowerUp.DefaultDuration(pickup.kind);
                PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
            }
            var barn = GameObject.Find("Granero - meta");
            if (barn != null) AdjustPresentation.AlignBarn(barn.GetComponent<SpriteRenderer>());
            var campaign = Object.FindAnyObjectByType<FinalCampaignStage>();
            if (campaign != null) campaign.towerFootOffset = AdjustPresentation.VisibleBottom(campaign.towerSprite);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("ABILITIES_CANVAS_BUILD_OK");
    }
    public static void Create(LevelOne level)
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var old = GameObject.Find("Tutorial Canvas"); if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("Tutorial Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var ui = root.AddComponent<TutorialCanvasUI>(); ui.player = level.player; ui.level = level;
        var header = StretchPanel(root.transform, "Estado", true, 80, new Color(0.055f, 0.06f, 0.07f, 0.9f));
        ui.healthText = Label(header, "Vida", "VIDA  10 / 10", 18, 10, 175, 30, 21, true);
        ui.statusText = Label(header, "Arma y efectos", "Pistola", 18, 45, 870, 28, 17);
        FillWidth(ui.statusText.rectTransform, 18);
        ui.guideButton = Button(root.transform, "Abrir guia", "Guía", 0, 0, 150, 40);
        var guideRect = (RectTransform)ui.guideButton.transform;
        guideRect.anchorMin = guideRect.anchorMax = guideRect.pivot = Vector2.zero;
        guideRect.anchoredPosition = new Vector2(215, 20);
        ui.tutorialButton = Button(root.transform, "Volver al tutorial", "Volver al tutorial", 0, 0, 180, 40);
        var tutorialRect = (RectTransform)ui.tutorialButton.transform;
        tutorialRect.anchorMin = tutorialRect.anchorMax = tutorialRect.pivot = Vector2.zero;
        tutorialRect.anchoredPosition = new Vector2(20, 20);
        ui.completionPanel = Panel(root.transform, "Nivel completado", 620, 250, new Color(0.08f, 0.09f, 0.105f, 0.98f)).gameObject;
        ui.completionTitleText = Label(ui.completionPanel.transform, "Titulo", "Tutorial completado", 24, 25, 572, 64, 26, true);
        ui.completionDescriptionText = Label(ui.completionPanel.transform, "Detalle", "Has terminado el tutorial.", 24, 96, 572, 52, 17);
        ui.restartButton = Button(ui.completionPanel.transform, "Repetir", "Volver a jugar", 190, 178, 240, 40);
        ui.completionPanel.SetActive(false);
        var shade = new GameObject("Guia del tutorial", typeof(RectTransform), typeof(Image)); shade.transform.SetParent(root.transform, false);
        var shadeRect = shade.GetComponent<RectTransform>(); shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one; shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().color = new Color(0, 0, 0, 0.62f);
        ui.guidePanel = shade;
        var panel = Panel(shade.transform, "Guia", 740, 640, new Color(0.94f, 0.94f, 0.94f, 1));
        ui.guideContent = panel;
        ui.stageTitleText = Label(panel, "Titulo", "Tutorial", 24, 16, 692, 32, 23, true);
        ui.stageObjectiveText = Label(panel, "Objetivo", "Llega al granero. Recoge y prueba cada poder por el camino.", 24, 55, 692, 38, 16);
        ui.stageObjectiveText.color = muted;
        Label(panel, "Controles", "Mover: A/D o flechas    Saltar: Espacio\nDisparar: Clic izquierdo    Reiniciar: R    Ayuda: H", 24, 100, 692, 46, 16);
        Label(panel, "Doble salto", "Con Pluma: doble salto. Morir, caer o R resta 5 de vida máxima (mínimo 1).", 24, 153, 692, 40, 15);
        string[] icons = { "apple.png", "botikin.png", "citrus.png", "pluma.png", "powerup resistencias.png", "camisa.png", "lanzpiedra.png", "Chicken.png" };
        string[] titles = { "Apple · permanente", "Botiquín · instantáneo", "Citrus · hasta salir del nivel", "Pluma · hasta salir del nivel", "Resistencia · 10 segundos", "Camisa · 10 segundos", "Lanzapiedra · 5 disparos", "Chicken / AK · 15 segundos" };
        string[] descriptions = { "+10 de vida y +1 de daño. Se guarda.", "Cura 2 corazones, sin superar el máximo.", "+50% de movimiento y velocidad de disparo.", "Permite un segundo salto en el aire.", "No recibes daño mientras dura el efecto.", "Minigun muy rápida y munición ilimitada.", "Tiros lentos y letales. Sin límite de tiempo.", "Disparo rápido; luego vuelve la pistola." };
        for (int i = 0; i < icons.Length; i++)
        {
            var card = Rect(panel, "Poder " + i, 24, 200 + i * 42, 692, 38);
            var icon = Rect(card, "Icono", 0, 3, 32, 32).gameObject.AddComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Pickups/" + icons[i]); icon.preserveAspect = true; icon.raycastTarget = false;
            Label(card, "Nombre", titles[i], 46, 0, 646, 18, 15, true);
            Label(card, "Explicacion", descriptions[i], 46, 19, 646, 19, 15);
        }
        Label(panel, "Pausa", "Guía: H. Nivel 6: acércate a una de las dos puertas y pulsa E para elegir.", 24, 547, 692, 24, 15);
        ui.guideTutorialButton = Button(panel, "Volver al tutorial", "Volver al tutorial", 24, 586, 240, 36);
        ui.continueButton = Button(panel, "Empezar o continuar", "Empezar  [Enter]", 476, 586, 240, 36);
        ui.continueText = ui.continueButton.GetComponentInChildren<Text>();
        foreach (var label in panel.GetComponentsInChildren<Text>()) label.color = Color.black;
        foreach (var button in panel.GetComponentsInChildren<Button>()) button.GetComponent<Image>().color = new Color(0.78f, 0.78f, 0.78f);
        PolishCombatAndLayout.ConfigureHearts(ui);
        ui.ConfigureStageText();
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
    static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height); return r;
    }
    static RectTransform Panel(Transform parent, string name, float width, float height, Color color)
    {
        var r = Rect(parent, name, 0, 0, width, height); r.anchorMin = r.anchorMax = r.pivot = Vector2.one * 0.5f;
        var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
    }
    static void FillWidth(RectTransform rect, float margin)
    {
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(-margin * 2, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(margin, rect.anchoredPosition.y);
    }
    static RectTransform StretchPanel(Transform parent, string name, bool top, float height, Color color)
    {
        var r = Rect(parent, name, 0, 0, 0, height);
        r.anchorMin = new Vector2(0, top ? 1 : 0); r.anchorMax = new Vector2(1, top ? 1 : 0);
        r.pivot = new Vector2(0, top ? 1 : 0); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(0, height);
        var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
    }
    static Text Label(Transform parent, string name, string value, float x, float y, float width, float height, int size, bool bold = false)
    {
        var text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Text>(); text.font = font; text.text = value;
        text.fontSize = size; text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; text.color = ink;
        text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
    static Button Button(Transform parent, string name, string title, float x, float y, float width, float height)
    {
        var r = Rect(parent, name, x, y, width, height);
        var image = r.gameObject.AddComponent<Image>(); image.color = new Color(0.25f, 0.28f, 0.31f);
        var button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var label = Label(r, "Texto", title, 0, 0, width, height, 17, true); label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
}
