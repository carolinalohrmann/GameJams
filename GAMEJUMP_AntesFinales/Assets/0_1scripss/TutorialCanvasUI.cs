using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialCanvasUI : MonoBehaviour
{
    public static TutorialCanvasUI Instance { get; private set; }
    public static bool BlocksGameplay => Instance != null && Instance.GuideOpen;
    public static bool PointerOverUI => UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    public player player;
    public LevelOne level;
    public bool showGuideOnStart = true;
    public GameObject guidePanel, completionPanel;
    public RectTransform guideContent;
    public Text healthText, statusText, contextText, continueText;
    public Text stageTitleText, stageObjectiveText, completionTitleText, completionDescriptionText;
    public Button guideButton, continueButton, restartButton;
    public bool GuideOpen => guidePanel != null && guidePanel.activeSelf;
    bool paused;
    float previousTimeScale;
    bool hasStarted;
    void Awake() { Instance = this; }
    void Start()
    {
        guideButton.onClick.AddListener(() => SetGuide(!GuideOpen));
        continueButton.onClick.AddListener(() => SetGuide(false));
        restartButton.onClick.AddListener(LoadNextStage);
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
        if (k != null)
        {
            if (k.hKey.wasPressedThisFrame) SetGuide(!GuideOpen);
            else if (GuideOpen && (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) SetGuide(false);
        }
        Refresh();
    }
    void Refresh()
    {
        if (player == null || level == null) return;
        var canvasSize = ((RectTransform)transform).rect.size;
        guideContent.localScale = Vector3.one * Mathf.Min(1, (canvasSize.x - 32) / 960, (canvasSize.y - 32) / 600);
        healthText.text = GetComponent<HealthHeartsUI>() != null ? "VIDA" : "VIDA  " + player.Health + " / " + player.maxHealth;
        string status = player.WeaponName;
        if (player.MinigunRemaining > 0) status += "  " + Mathf.CeilToInt(player.MinigunRemaining) + " s";
        if (player.WeaponName == "AK") status += "  " + Mathf.CeilToInt(player.AKRemaining) + " s";
        if (player.CitrusRemaining > 0) status += "   |   Citrus " + Mathf.CeilToInt(player.CitrusRemaining) + " s";
        if (player.ImmunityRemaining > 0) status += "   |   Inmune " + Mathf.CeilToInt(player.ImmunityRemaining) + " s";
        if (player.DoubleJump) status += "   |   Doble salto " + Mathf.CeilToInt(player.FeatherRemaining) + " s";
        statusText.text = status;
        string context = level.CurrentNotice;
        if (context == null && !GuideOpen && !level.Completed)
        {
            float nearest = 3;
            foreach (var power in FindObjectsByType<PowerUp>())
            {
                float distance = Vector2.Distance(player.transform.position, power.transform.position);
                if (distance < nearest) { nearest = distance; context = power.Title; }
            }
        }
        contextText.text = context ?? (level.stageNumber == 1
            ? "Llega al granero. Recoge los poderes para practicar."
            : "Llega a la salida. Recoge los poderes de esta partida.");
        completionPanel.SetActive(level.Completed && !GuideOpen);
    }
    void ConfigureStageText()
    {
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
        if (level.stageNumber == 1 || level.layout == null || guideContent == null) return;
        ConfigureGuide(level.layout.AvailablePowerUps, level.layout.powerUpIcons);
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
            cards[i].anchoredPosition = new Vector2(i % 2 == 0 ? 28 : 488, -(194 + i / 2 * 76));
            var icon = cards[i].Find("Icono").GetComponent<Image>();
            icon.sprite = icons[(int)kind];
            cards[i].Find("Nombre").GetComponent<Text>().text = PowerUpName(kind);
            cards[i].Find("Explicacion").GetComponent<Text>().text = PowerUpDescription(kind);
        }

        var jumpHint = guideContent.Find("Doble salto");
        if (jumpHint != null)
            jumpHint.GetComponent<Text>().text = "Usa los poderes de esta partida.  R: volver al inicio.";
    }

    void LoadNextStage()
    {
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
            case PowerUp.Kind.Citrus: return "Citrus · 10 segundos";
            case PowerUp.Kind.Feather: return "Pluma · 5 segundos";
            case PowerUp.Kind.Resistance: return "Resistencia · 10 segundos";
            case PowerUp.Kind.Shirt: return "Camisa · 5 segundos";
            case PowerUp.Kind.Rock: return "Lanzapiedra · 5 disparos";
            default: return "Chicken / AK · 10 segundos";
        }
    }

    static string PowerUpDescription(PowerUp.Kind kind)
    {
        switch (kind)
        {
            case PowerUp.Kind.Apple: return "+10 de vida máxima y actual.";
            case PowerUp.Kind.Medkit: return "Cura 2 corazones.";
            case PowerUp.Kind.Citrus: return "+50% movimiento y disparo.";
            case PowerUp.Kind.Feather: return "Permite un segundo salto.";
            case PowerUp.Kind.Resistance: return "Inmunidad temporal.";
            case PowerUp.Kind.Shirt: return "Minigun rápida e ilimitada.";
            case PowerUp.Kind.Rock: return "Cinco disparos letales.";
            default: return "Disparo rápido por tiempo limitado.";
        }
    }

    public void SetGuide(bool open)
    {
        if (open)
        {
            if (!paused) { previousTimeScale = Time.timeScale; Time.timeScale = 0; paused = true; }
            continueText.text = hasStarted ? "Continuar  [Enter]" : "Empezar  [Enter]";
        }
        else
        {
            if (paused) { Time.timeScale = previousTimeScale; paused = false; }
            hasStarted = true;
        }
        guidePanel.SetActive(open);
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null) events.SetSelectedGameObject(open ? continueButton.gameObject : null);
    }
    void OnDestroy()
    {
        if (paused) Time.timeScale = previousTimeScale;
        if (Instance == this) Instance = null;
    }
}
