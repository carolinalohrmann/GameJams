using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialCanvasUI : MonoBehaviour
{
    public static TutorialCanvasUI Instance { get; private set; }
    public static bool BlocksGameplay => Instance != null && (Instance.GuideOpen || Instance.ReturnConfirmationOpen);
    public static bool PointerOverUI => UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    public player player;
    public LevelOne level;
    public bool showGuideOnStart = true;
    public GameObject guidePanel, completionPanel;
    public RectTransform guideContent;
    public Text healthText, statusText, continueText;
    public Text stageTitleText, stageObjectiveText, completionTitleText, completionDescriptionText;
    public Text simpleGuideText;
    public Button guideButton, continueButton, restartButton;
    public Button tutorialButton, guideTutorialButton;
    public bool GuideOpen => guidePanel != null && guidePanel.activeSelf;
    public bool ReturnConfirmationOpen => returnConfirmation != null && returnConfirmation.activeSelf;
    GameObject returnConfirmation;
    RectTransform confirmationContent;
    Button cancelReturnButton;
    Button botonRestablecer;
    Text creditosFinales;
    GameObject previousSelection;
    bool paused;
    float previousTimeScale;
    bool hasStarted;
    void Awake() { Instance = this; }
    void Start()
    {
        guideButton.onClick.AddListener(() => SetGuide(!GuideOpen));
        continueButton.onClick.AddListener(() => SetGuide(false));
        restartButton.onClick.AddListener(LoadNextStage);
        if (tutorialButton != null) tutorialButton.onClick.AddListener(ReturnToTutorial);
        if (guideTutorialButton != null) guideTutorialButton.onClick.AddListener(ReturnToTutorial);
        if (level.stageNumber == 1) CrearBotonRestablecer();
        ConfigureStageText();
        ConfigurePowerUpGuide();
        guidePanel.SetActive(false);
        completionPanel.SetActive(false);
        SetGuide(showGuideOnStart);
        Refresh();
    }
    void Update()
    {
        var k = Keyboard.current;
        if (ReturnConfirmationOpen)
        {
            if (k != null && k.escapeKey.wasPressedThisFrame) CancelReturnToTutorial();
            Refresh();
            return;
        }
        if (k != null)
        {
            if (k.hKey.wasPressedThisFrame) SetGuide(!GuideOpen);
            else if (GuideOpen)
            {
                var events = UnityEngine.EventSystems.EventSystem.current;
                bool continuarConEnter = k.enterKey.wasPressedThisFrame &&
                    (events == null || events.currentSelectedGameObject == null || events.currentSelectedGameObject == continueButton.gameObject);
                if (k.escapeKey.wasPressedThisFrame || continuarConEnter) SetGuide(false);
            }
        }
        Refresh();
    }
    void Refresh()
    {
        if (player == null || level == null) return;
        var canvasSize = ((RectTransform)transform).rect.size;
        guideContent.localScale = Vector3.one * Mathf.Min(1, (canvasSize.x - 32) / guideContent.sizeDelta.x, (canvasSize.y - 32) / guideContent.sizeDelta.y);
        if (confirmationContent != null)
            confirmationContent.localScale = Vector3.one * Mathf.Min(1, (canvasSize.x - 32) / 520, (canvasSize.y - 32) / 220);
        if (creditosFinales != null)
            completionPanel.transform.localScale = Vector3.one * Mathf.Min(1, (canvasSize.x - 32) / 740, (canvasSize.y - 32) / 660);
        healthText.text = GetComponent<HealthHeartsUI>() != null ? "VIDA" : "VIDA  " + player.Health + " / " + player.maxHealth;
        string status = player.WeaponName;
        if (player.MinigunRemaining > 0) status += "  " + Mathf.CeilToInt(player.MinigunRemaining) + " s";
        if (player.WeaponName == "AK") status += "  " + Mathf.CeilToInt(player.AKRemaining) + " s";
        if (player.CitrusRemaining > 0) status += "   |   Citrus (este nivel)";
        if (player.ImmunityRemaining > 0) status += "   |   Inmune " + Mathf.CeilToInt(player.ImmunityRemaining) + " s";
        if (player.DoubleJump) status += "   |   Doble salto (este nivel)";
        statusText.text = status;
        completionPanel.SetActive(level.Completed && !GuideOpen && !ReturnConfirmationOpen);
    }
    public void ConfigureStageText()
    {
        var campaign = level.GetComponent<FinalCampaignStage>();
        if (campaign != null) { campaign.ConfigureUI(this); return; }
        if (stageTitleText != null) stageTitleText.text = level.stageNumber == 1 ? "Tutorial" : "NIVEL " + level.stageNumber;
        if (stageObjectiveText != null)
            stageObjectiveText.text = level.stageNumber == 1
                ? "Cruza la jungla y llega al granero."
                : level.stageNumber == 2 || level.stageNumber == 4 ? "Atraviesa el prado y llega a la salida." : "Atraviesa el bosque y llega a la salida.";
        if (completionTitleText != null)
            completionTitleText.text = level.stageNumber == 1 ? "Tutorial completado" : "Nivel " + level.stageNumber + " completado";
        if (completionDescriptionText != null)
            completionDescriptionText.text = level.stageNumber == 1
                ? "¡Tutorial completado! Sigue hacia el prado."
                : level.nextSceneName == "Tutorial" ? "¡Completaste todos los niveles!" : "¡Bien! Continua a la siguiente etapa.";
        var actionLabel = restartButton.GetComponentInChildren<Text>();
        if (actionLabel != null)
            actionLabel.text = level.nextSceneName == "Tutorial" ? "Jugar de nuevo" : "Siguiente nivel";
    }

    void ConfigurePowerUpGuide()
    {
        var instrucciones = guideContent != null ? guideContent.Find("Pausa") : null;
        if (instrucciones != null)
            instrucciones.GetComponent<Text>().text = "Guía: H. Nivel 6: acércate a una de las dos puertas y pulsa E para elegir.";
        var restartHint = guideContent != null ? guideContent.Find("Doble salto") : null;
        if (restartHint != null)
            restartHint.GetComponent<Text>().text = "Con Pluma: doble salto. Morir, caer o R resta 5 de vida máxima (mínimo 1).";
        if (simpleGuideText != null) { ConfigureSimpleGuide(); return; }
        var campaign = level.GetComponent<FinalCampaignStage>();
        if (campaign != null && level.layout == null)
        {
            ConfigureGuide(campaign.fixedPowerUps, campaign.powerUpIcons);
            campaign.ConfigureUI(this);
            return;
        }
        if (level.stageNumber == 1 || level.layout == null || guideContent == null) return;
        ConfigureGuide(level.layout.AvailablePowerUps, level.layout.powerUpIcons);
        if (campaign != null) campaign.ConfigureUI(this);
    }
    void ConfigureSimpleGuide()
    {
        string text = "A/D o flechas: mover\nEspacio: saltar\nClic izquierdo: disparar\nE: elegir y entrar por una de las dos puertas (nivel 6)\nH: abrir/cerrar esta ayuda\nR: reiniciar (-5 de vida máxima, mínimo 1)\n\nPODERES\n";
        var campaign = level.GetComponent<FinalCampaignStage>();
        var kinds = level.layout != null ? level.layout.AvailablePowerUps
            : campaign != null ? (System.Collections.Generic.IReadOnlyList<PowerUp.Kind>)campaign.fixedPowerUps
            : (PowerUp.Kind[])System.Enum.GetValues(typeof(PowerUp.Kind));
        foreach (var kind in kinds) text += PowerUpName(kind) + ": " + PowerUpDescription(kind) + "\n";
        simpleGuideText.text = text;
    }
    void ConfigureGuide(System.Collections.Generic.IReadOnlyList<PowerUp.Kind> kinds, Sprite[] icons)
    {
        var cards = new RectTransform[8];
        for (int i = 0; i < cards.Length; i++)
            cards[i] = guideContent.Find("Poder " + i) as RectTransform;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            bool available = i < kinds.Count;
            cards[i].gameObject.SetActive(available);
            if (!available) continue;

            var kind = kinds[i];
            cards[i].anchoredPosition = new Vector2(24, -(200 + i * 42));
            var icon = cards[i].Find("Icono").GetComponent<Image>();
            icon.sprite = icons[(int)kind];
            cards[i].Find("Nombre").GetComponent<Text>().text = PowerUpName(kind);
            cards[i].Find("Explicacion").GetComponent<Text>().text = PowerUpDescription(kind);
        }

        var jumpHint = guideContent.Find("Doble salto");
        if (jumpHint != null)
            jumpHint.GetComponent<Text>().text = "Morir, caer o pulsar R resta 5 de vida máxima (mínimo 1).";
    }

    public void ReturnToTutorial()
    {
        if (ReturnConfirmationOpen) return;
        var events = UnityEngine.EventSystems.EventSystem.current;
        previousSelection = events != null ? events.currentSelectedGameObject : null;
        if (returnConfirmation == null) CreateReturnConfirmation();
        returnConfirmation.SetActive(true);
        returnConfirmation.transform.SetAsLastSibling();
        UpdatePauseState();
        if (events != null) events.SetSelectedGameObject(cancelReturnButton.gameObject);
    }

    public void CancelReturnToTutorial()
    {
        if (!ReturnConfirmationOpen) return;
        returnConfirmation.SetActive(false);
        UpdatePauseState();
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null) events.SetSelectedGameObject(previousSelection);
    }

    public void ConfirmReturnToTutorial()
    {
        if (!ReturnConfirmationOpen) return;
        returnConfirmation.SetActive(false);
        SetGuide(false);
        SceneManager.LoadScene("Tutorial");
    }

    void CreateReturnConfirmation()
    {
        returnConfirmation = new GameObject("Confirmar regreso al tutorial", typeof(RectTransform), typeof(Image));
        var overlay = returnConfirmation.GetComponent<RectTransform>();
        overlay.SetParent(transform, false);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        returnConfirmation.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        confirmationContent = new GameObject("Dialogo", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        confirmationContent.SetParent(overlay, false);
        confirmationContent.anchorMin = confirmationContent.anchorMax = confirmationContent.pivot = Vector2.one * 0.5f;
        confirmationContent.sizeDelta = new Vector2(520, 220);
        confirmationContent.GetComponent<Image>().color = new Color(0.94f, 0.94f, 0.94f);
        ConfirmationLabel("¿Volver al tutorial?", new Vector2(0, 65), new Vector2(480, 40), 24);
        ConfirmationLabel("Saldrás del nivel actual.", new Vector2(0, 15), new Vector2(480, 40), 18);
        cancelReturnButton = ConfirmationButton("Cancelar", -120, CancelReturnToTutorial);
        var confirmButton = ConfirmationButton("Volver al tutorial", 120, ConfirmReturnToTutorial);
        foreach (var button in new[] { cancelReturnButton, confirmButton })
        {
            var other = button == cancelReturnButton ? confirmButton : cancelReturnButton;
            button.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnLeft = other, selectOnRight = other, selectOnUp = other, selectOnDown = other };
        }
    }

    Text ConfirmationLabel(string value, Vector2 position, Vector2 size, int fontSize)
    {
        var label = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(confirmationContent, false);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = Vector2.one * 0.5f;
        label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = size;
        label.font = continueText.font; label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter; label.color = Color.black; label.raycastTarget = false;
        return label;
    }

    Button ConfirmationButton(string title, float x, UnityEngine.Events.UnityAction action)
    {
        var button = Instantiate(continueButton, confirmationContent);
        button.name = title;
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.anchoredPosition = new Vector2(x, -65); rect.sizeDelta = new Vector2(220, 40);
        button.onClick = new Button.ButtonClickedEvent(); button.onClick.AddListener(action);
        var label = button.GetComponentInChildren<Text>(); label.text = title;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    void LoadNextStage()
    {
        if (level.nextSceneName == "Tutorial") { ReturnToTutorial(); return; }
        if (Application.CanStreamedLevelBeLoaded(level.nextSceneName))
            SceneManager.LoadScene(level.nextSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    static string PowerUpName(PowerUp.Kind kind)
    {
        switch (kind)
        {
            case PowerUp.Kind.Apple: return "Apple · permanente";
            case PowerUp.Kind.Medkit: return "Botiquín · instantáneo";
            case PowerUp.Kind.Citrus: return "Citrus · hasta salir del nivel";
            case PowerUp.Kind.Feather: return "Pluma · hasta salir del nivel";
            case PowerUp.Kind.Resistance: return "Resistencia · 10 segundos";
            case PowerUp.Kind.Shirt: return "Camisa · 10 segundos";
            case PowerUp.Kind.Rock: return "Lanzapiedra · 5 disparos";
            default: return "Chicken / AK · 15 segundos";
        }
    }

    static string PowerUpDescription(PowerUp.Kind kind)
    {
        switch (kind)
        {
            case PowerUp.Kind.Apple: return "+10 de vida y +1 de daño. Se guarda.";
            case PowerUp.Kind.Medkit: return "Cura 2 corazones.";
            case PowerUp.Kind.Citrus: return "+50% movimiento y disparo.";
            case PowerUp.Kind.Feather: return "Permite un segundo salto.";
            case PowerUp.Kind.Resistance: return "Inmunidad temporal.";
            case PowerUp.Kind.Shirt: return "Minigun rápida e ilimitada.";
            case PowerUp.Kind.Rock: return "Cinco disparos letales.";
            default: return "Disparo rápido por tiempo limitado.";
        }
    }

    public void MostrarCreditosFinales()
    {
        ((RectTransform)completionPanel.transform).sizeDelta = new Vector2(740, 660);
        completionTitleText.rectTransform.anchoredPosition = new Vector2(24, -20);
        completionTitleText.rectTransform.sizeDelta = new Vector2(692, 64);
        completionTitleText.alignment = TextAnchor.MiddleCenter;
        completionDescriptionText.rectTransform.anchoredPosition = new Vector2(24, -90);
        completionDescriptionText.rectTransform.sizeDelta = new Vector2(692, 60);
        completionDescriptionText.alignment = TextAnchor.MiddleCenter;
        if (creditosFinales == null)
        {
            creditosFinales = new GameObject("Creditos finales", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            creditosFinales.transform.SetParent(completionPanel.transform, false);
            creditosFinales.rectTransform.anchorMin = creditosFinales.rectTransform.anchorMax = creditosFinales.rectTransform.pivot = new Vector2(0, 1);
            creditosFinales.rectTransform.anchoredPosition = new Vector2(24, -162);
            creditosFinales.rectTransform.sizeDelta = new Vector2(692, 408);
            creditosFinales.font = completionDescriptionText.font;
            creditosFinales.fontSize = 18; creditosFinales.color = Color.white;
            creditosFinales.alignment = TextAnchor.UpperCenter;
            creditosFinales.supportRichText = true; creditosFinales.raycastTarget = false;
            creditosFinales.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditosFinales.verticalOverflow = VerticalWrapMode.Truncate;
        }
        creditosFinales.text = "<b>CRÉDITOS</b>\n\n"
            + "<b>Diseño y elección de sprays</b>\nCarolina Lohrmann\n\n"
            + "<b>Elección de la música</b>\nCarolina Lohrmann y Kevin Illanes\n\n"
            + "<b>Código</b>\nKevin Illanes y Carolina Lohrmann\n\n"
            + "<b>Agradecimientos</b>\nA todos los creadores de assets de itch.io.\n"
            + "A Miro Haverinen, por las tres canciones de Fear & Hunger.\n\n"
            + "<b>¡Gracias por jugar!</b>";
        ((RectTransform)restartButton.transform).anchoredPosition = new Vector2(250, -596);
    }

    public void SetGuide(bool open)
    {
        if (open)
        {
            continueText.text = hasStarted ? "Continuar  [Enter]" : "Empezar  [Enter]";
        }
        else hasStarted = true;
        guidePanel.SetActive(open);
        if (botonRestablecer != null) botonRestablecer.gameObject.SetActive(open && !hasStarted);
        UpdatePauseState();
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null) events.SetSelectedGameObject(open ? continueButton.gameObject : null);
    }
    void CrearBotonRestablecer()
    {
        botonRestablecer = Instantiate(continueButton, guideContent);
        botonRestablecer.name = "Restablecer valores";
        var posicionBoton = (RectTransform)botonRestablecer.transform;
        posicionBoton.anchorMin = posicionBoton.anchorMax = posicionBoton.pivot = new Vector2(0, 1);
        posicionBoton.anchoredPosition = new Vector2(274, -586);
        posicionBoton.sizeDelta = new Vector2(192, 36);
        var textoBoton = botonRestablecer.GetComponentInChildren<Text>();
        textoBoton.text = "Restablecer valores"; textoBoton.fontSize = 15;
        textoBoton.rectTransform.anchorMin = Vector2.zero; textoBoton.rectTransform.anchorMax = Vector2.one;
        textoBoton.rectTransform.offsetMin = textoBoton.rectTransform.offsetMax = Vector2.zero;
        botonRestablecer.onClick = new Button.ButtonClickedEvent();
        botonRestablecer.onClick.AddListener(RestablecerValoresIniciales);
    }
    void RestablecerValoresIniciales()
    {
        if (level.stageNumber != 1 || hasStarted || !GuideOpen || ReturnConfirmationOpen) return;
        player.ResetAppleUpgrades();
        botonRestablecer.GetComponentInChildren<Text>().text = "Valores restablecidos";
        Refresh();
    }
    void UpdatePauseState()
    {
        bool shouldPause = GuideOpen || ReturnConfirmationOpen;
        if (shouldPause && !paused) { previousTimeScale = Time.timeScale; Time.timeScale = 0; paused = true; }
        else if (!shouldPause && paused) { Time.timeScale = previousTimeScale; paused = false; }
    }
    void OnDestroy()
    {
        if (paused) Time.timeScale = previousTimeScale;
        if (Instance == this) Instance = null;
    }
}
