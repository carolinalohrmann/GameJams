using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FinalCampaignStage : MonoBehaviour
{
    public const string NormalEndingMessage = "Tener miedo a lo desconocido es normal. Disfruta tu mediocridad.";
    public BossController bossPrefab;
    public Sprite towerSprite, portalSprite, meteorSprite;
    public Sprite[] powerUpIcons;
    public PowerUp.Kind[] fixedPowerUps = { PowerUp.Kind.Medkit, PowerUp.Kind.Shirt, PowerUp.Kind.Chicken, PowerUp.Kind.Apple, PowerUp.Kind.Citrus, PowerUp.Kind.Feather };
    public float arenaLeft, arenaRight, floorY = -4;
    public float towerFootOffset;
    public BossController Boss { get; private set; }
    public bool ExitsUnlocked { get; private set; }
    public LevelOne Level { get; private set; }
    public string Objective => ExitsUnlocked ? (Level.stageNumber == 6 ? "Acércate a una de las dos puertas y pulsa E para elegir y entrar." : "Acércate a la Torre para continuar.")
        : Level.stageNumber == 6 ? "Llega al Necromancer y derrótalo para abrir las dos puertas."
        : Level.stageNumber == 7 ? "Esquiva los meteoritos y derrota al segundo Necromancer."
        : "Esquiva las ráfagas y derrota al Cazador.";
    public readonly List<CampaignPortal> Portals = new List<CampaignPortal>();
    float nextMeteor;
    bool cleanedDeath;
    void Awake()
    {
        Level = GetComponent<LevelOne>(); Level.manualCompletion = true;
        fixedPowerUps = System.Array.FindAll(fixedPowerUps, kind => PowerUp.IsAvailableInStage(kind, Level.stageNumber));
    }
    void Start()
    {
        Boss = Instantiate(bossPrefab, new Vector3(arenaRight - 5, floorY + bossPrefab.GetComponent<BoxCollider2D>().size.y / 2 + 0.03f, 0), Quaternion.identity);
        Boss.ConfigureStage(this); Boss.arenaLeft = arenaLeft; Boss.arenaRight = arenaRight - 2;
        Level.player.Respawned += ResetEncounter;
        nextMeteor = Time.time + 4;
    }
    void Update()
    {
        if (Level.player == null || Level.Completed || TutorialCanvasUI.BlocksGameplay) return;
        if (Level.player.Dying)
        {
            if (!cleanedDeath) { ClearAttacks(); cleanedDeath = true; }
            return;
        }
        cleanedDeath = false;
        if (Boss != null && !Boss.Defeated && Level.player.transform.position.x >= arenaLeft - 1) Boss.Activate();
        if ((Level.stageNumber == 7 || Level.stageNumber == 8) && Time.time >= nextMeteor && !ExitsUnlocked && Level.player.transform.position.x > Level.leftEdge + 8)
        {
            float x = Mathf.Clamp(Level.player.transform.position.x + Random.Range(-2.2f, 2.2f), Level.leftEdge + 2, Level.rightEdge - 2);
            float ground = floorY;
            foreach (var hit in Physics2D.RaycastAll(new Vector2(x, 8), Vector2.down, 20))
                if (!hit.collider.isTrigger && hit.collider.attachedRigidbody == null) { ground = hit.point.y; break; }
            CampaignHazard.CreateMeteor(this, x, ground);
            nextMeteor = Time.time + Random.Range(2.1f, 3.6f);
        }
    }
    public void BossDefeated()
    {
        if (ExitsUnlocked) return;
        ExitsUnlocked = true;
        ClearAttacks();
        if (Level.stageNumber == 8)
        {
            var ui = TutorialCanvasUI.Instance;
            ui.completionTitleText.text = "Felicidades, eres housestatico";
            ui.completionDescriptionText.text = "Derrotaste al Cazador y completaste la ruta secreta.";
            ui.MostrarCreditosFinales();
            Level.nextSceneName = "Tutorial";
            ui.restartButton.GetComponentInChildren<Text>().text = "Volver al tutorial";
            Level.Complete(); return;
        }
        if (Level.stageNumber == 6)
        {
            SpawnNecromancerApple();
            AddPortal(arenaRight - 10, towerSprite, "Torre · final normal", "");
            AddPortal(arenaRight - 3.5f, portalSprite, "Ruta secreta", "Level7");
            Level.Notify("Necromancer derrotado. Elige una puerta y pulsa E.");
        }
        else
        {
            AddPortal(arenaRight - 4, towerSprite, "Torre · el Cazador", "Level8");
            Level.Notify("La torre se ha abierto. Acércate para entrar.");
        }
    }
    void SpawnNecromancerApple()
    {
        var root = Instantiate(Level.layout.pickupPrefab, new Vector3(arenaRight - 13, floorY + 0.7f, 0), Quaternion.identity, transform);
        root.name = "Apple - recompensa del Necromancer";
        var pickup = root.GetComponent<PowerUp>();
        pickup.kind = PowerUp.Kind.Apple;
        pickup.pickupId = "Level6.NecromancerReward";
        pickup.durationSeconds = PowerUp.DefaultDuration(PowerUp.Kind.Apple);
        pickup.icon.sprite = powerUpIcons[(int)PowerUp.Kind.Apple];
    }
    void AddPortal(float x, Sprite sprite, string title, string destination)
    {
        var portal = new GameObject(title).AddComponent<CampaignPortal>();
        portal.transform.position = new Vector3(x, floorY, 0);
        portal.stage = this; portal.destination = destination; portal.title = title;
        var visual = portal.gameObject.AddComponent<SpriteRenderer>(); visual.sprite = sprite; visual.sortingOrder = sprite == towerSprite ? 1 : 4;
        // Sprites are imported with their pivot at the visible base.
        float width = destination == "Level7" ? 1.5f : 3;
        visual.transform.localScale = Vector3.one * Mathf.Min(width / sprite.bounds.size.x, 2.5f / sprite.bounds.size.y);
        if (sprite == towerSprite) portal.transform.position += Vector3.down * (towerFootOffset * portal.transform.localScale.y + 0.06f);
        if (sprite == towerSprite)
            foreach (var hit in Physics2D.RaycastAll(new Vector2(x, floorY + 0.5f), Vector2.down, 2))
                if (!hit.collider.isTrigger && hit.collider.attachedRigidbody == null)
                {
                    foreach (var soil in hit.collider.GetComponentsInChildren<SpriteRenderer>()) soil.sortingOrder = Mathf.Max(2, soil.sortingOrder);
                    break;
                }
        var trigger = new GameObject("Entrada").AddComponent<BoxCollider2D>();
        trigger.transform.SetParent(portal.transform, false);
        trigger.size = new Vector2(1 / portal.transform.localScale.x, 1.8f / portal.transform.localScale.y);
        trigger.offset = new Vector2(0, (floorY + 0.9f - portal.transform.position.y) / portal.transform.localScale.y); trigger.isTrigger = true;
        Portals.Add(portal);
    }
    public void NormalEnding()
    {
        var ui = TutorialCanvasUI.Instance;
        ui.completionTitleText.text = "Final normal";
        ui.completionDescriptionText.text = NormalEndingMessage;
        ui.MostrarCreditosFinales();
        Level.nextSceneName = "Tutorial";
        ui.restartButton.GetComponentInChildren<Text>().text = "Volver al tutorial";
        Level.Complete();
    }
    public void ClearAttacks()
    {
        foreach (var hazard in FindObjectsByType<CampaignHazard>()) Destroy(hazard.gameObject);
        foreach (var shot in FindObjectsByType<CampaignAttack>()) Destroy(shot.gameObject);
        foreach (var summon in FindObjectsByType<BossSummon>()) Destroy(summon.gameObject);
    }
    void ResetEncounter()
    {
        ClearAttacks(); nextMeteor = Time.time + 4;
        if (Boss != null && !Boss.Defeated) Boss.ResetEncounter();
    }
    void OnDestroy() { if (Level != null && Level.player != null) Level.player.Respawned -= ResetEncounter; }
    public void ConfigureUI(TutorialCanvasUI ui)
    {
        if (Level == null) Level = GetComponent<LevelOne>();
        int number = Level.stageNumber;
        ui.stageTitleText.text = number == 6 ? "NIVEL 6 · El Necromancer" : number == 7 ? "RUTA SECRETA · Mundo roto" : "EL CAZADOR";
        ui.stageObjectiveText.text = number == 6 ? "Derrota al Necromancer. Acércate a una de las dos puertas y pulsa E para elegir."
            : number == 7 ? "Enemigos reforzados y meteoritos letales. Derrota al segundo Necromancer."
            : "Esquiva ráfagas y meteoritos. Derrota al Cazador.";
        ui.completionTitleText.text = number == 8 ? "Felicidades, eres housestatico" : "Final normal";
        ui.completionDescriptionText.text = "Has completado esta ruta.";
        ((RectTransform)ui.completionPanel.transform).sizeDelta = new Vector2(620, 250);
        ui.completionTitleText.rectTransform.sizeDelta = new Vector2(572, 64);
        ui.completionTitleText.resizeTextForBestFit = true; ui.completionTitleText.resizeTextMinSize = 18; ui.completionTitleText.resizeTextMaxSize = 26;
        ui.completionDescriptionText.rectTransform.anchoredPosition = new Vector2(24, -96);
        ui.completionDescriptionText.rectTransform.sizeDelta = new Vector2(572, 52);
        var button = (RectTransform)ui.restartButton.transform; button.anchoredPosition = new Vector2(190, -178);
        ui.restartButton.GetComponentInChildren<Text>().text = "Volver al tutorial";
        var hint = ui.guideContent.Find("Doble salto").GetComponent<Text>();
        hint.text = number == 7 ? "Las marcas del suelo avisan: meteoritos y ritual rojo matan de un golpe."
            : number == 6 ? "Aléjate de la marca del ritual letal. E: entrar en la puerta elegida."
            : "Usa las plataformas. Los meteoritos matan de un golpe.";
    }
}
