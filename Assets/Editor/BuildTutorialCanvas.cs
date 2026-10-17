using UnityEditor;
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
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/Level1.unity");
        Create(Object.FindAnyObjectByType<LevelOne>());
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("TUTORIAL_CANVAS_BUILD_OK");
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
        ui.healthText = Label(header, "Vida", "VIDA  5 / 5", 18, 10, 175, 30, 21, true);
        ui.statusText = Label(header, "Arma y efectos", "Pistola", 18, 45, 870, 28, 17);
        FillWidth(ui.statusText.rectTransform, 18);
        ui.guideButton = Button(header, "Abrir guia", "Guía  [H]", 1100, 15, 150, 40);
        var guideRect = (RectTransform)ui.guideButton.transform;
        guideRect.anchorMin = guideRect.anchorMax = guideRect.pivot = Vector2.one;
        guideRect.anchoredPosition = new Vector2(-20, -15);
        var footer = StretchPanel(root.transform, "Ayuda contextual", false, 86, new Color(0.055f, 0.06f, 0.07f, 0.88f));
        ui.contextText = Label(footer, "Efecto cercano", "Llega al granero.", 20, 8, 1240, 26, 17);
        FillWidth(ui.contextText.rectTransform, 20);
        var controls = Label(footer, "Controles permanentes", "A / D o flechas: mover     Espacio: saltar     Clic izquierdo: disparar     R: volver al inicio     H: guía", 20, 39, 1240, 42, 15);
        FillWidth(controls.rectTransform, 20);
        controls.color = muted;
        ui.completionPanel = Panel(root.transform, "Nivel completado", 400, 190, new Color(0.08f, 0.09f, 0.105f, 0.98f)).gameObject;
        ui.completionTitleText = Label(ui.completionPanel.transform, "Titulo", "Nivel 1 completado", 24, 25, 352, 36, 26, true);
        ui.completionDescriptionText = Label(ui.completionPanel.transform, "Detalle", "Has terminado el tutorial.", 24, 70, 352, 26, 17);
        ui.restartButton = Button(ui.completionPanel.transform, "Repetir", "Volver a jugar", 80, 125, 240, 40);
        ui.completionPanel.SetActive(false);
        var shade = new GameObject("Guia del tutorial", typeof(RectTransform), typeof(Image)); shade.transform.SetParent(root.transform, false);
        var shadeRect = shade.GetComponent<RectTransform>(); shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one; shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().color = new Color(0, 0, 0, 0.62f);
        ui.guidePanel = shade;
        var panel = Panel(shade.transform, "Guia", 960, 600, new Color(0.075f, 0.085f, 0.1f, 0.99f));
        ui.guideContent = panel;
        ui.stageTitleText = Label(panel, "Titulo", "NIVEL 1  /  APRENDE A JUGAR", 28, 22, 900, 38, 29, true);
        ui.stageObjectiveText = Label(panel, "Objetivo", "Llega al granero. Recoge y prueba cada poder por el camino.", 28, 68, 900, 28, 18);
        ui.stageObjectiveText.color = muted;
        Label(panel, "Controles", "A / D o ← / →  Mover     Espacio / W / ↑  Saltar     Clic izquierdo  Disparar", 28, 112, 900, 28, 19, true);
        Label(panel, "Doble salto", "Con Pluma: vuelve a pulsar salto en el aire.  R: volver al inicio.", 28, 146, 900, 26, 17);
        string[] icons = { "apple.png", "botikin.png", "citrus.png", "pluma.png", "powerup resistencias.png", "camisa.png", "lanzpiedra.png", "Chicken.png" };
        string[] titles = { "Apple · permanente", "Botiquín · instantáneo", "Citrus · 10 segundos", "Pluma · 5 segundos", "Resistencia · 10 segundos", "Camisa · 5 segundos", "Lanzapiedra · 5 disparos", "Chicken / AK · 10 segundos" };
        string[] descriptions = { "+10 de vida máxima y actual. Se guarda.", "Cura 2 corazones, sin superar el máximo.", "+50% de movimiento y velocidad de disparo.", "Permite un segundo salto en el aire.", "No recibes daño mientras dura el efecto.", "Minigun muy rápida y munición ilimitada.", "Tiros lentos y letales. Sin límite de tiempo.", "Disparo rápido; luego vuelve la pistola." };
        for (int i = 0; i < icons.Length; i++)
        {
            float x = i % 2 == 0 ? 28 : 488, y = 194 + (i / 2) * 76;
            var card = Rect(panel, "Poder " + i, x, y, 444, 66);
            var bg = card.gameObject.AddComponent<Image>(); bg.color = new Color(0.13f, 0.145f, 0.16f); bg.raycastTarget = false;
            var icon = Rect(card, "Icono", 10, 12, 40, 40).gameObject.AddComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Pickups/" + icons[i]); icon.preserveAspect = true; icon.raycastTarget = false;
            Label(card, "Nombre", titles[i], 62, 8, 375, 25, 17, true);
            var description = Label(card, "Explicacion", descriptions[i], 62, 35, 375, 23, 15); description.color = muted;
        }
        Label(panel, "Pausa", "Juego en pausa · H abre y cierra esta guía.", 28, 536, 550, 30, 16);
        ui.continueButton = Button(panel, "Empezar o continuar", "Empezar  [Enter]", 676, 528, 256, 44);
        ui.continueText = ui.continueButton.GetComponentInChildren<Text>();
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
