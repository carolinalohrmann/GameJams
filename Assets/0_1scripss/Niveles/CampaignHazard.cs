using UnityEngine;

public class CampaignHazard : MonoBehaviour
{
    public FinalCampaignStage stage;
    public bool meteor, lethal;
    public float warningSeconds = 1.5f;
    public bool Armed { get; private set; }
    float age, ground, speed;
    SpriteRenderer visual;
    BoxCollider2D trigger;
    public static CampaignHazard CreateRitual(FinalCampaignStage stage, Sprite sprite, float x, float warning, bool lethal)
    {
        var hazard = Create(stage, sprite, new Vector2(x, stage.floorY + 0.1f));
        hazard.lethal = lethal; hazard.warningSeconds = warning; hazard.ground = stage.floorY;
        hazard.Fit(new Vector2(lethal ? 2.4f : 1.3f, 0.2f));
        return hazard;
    }
    public static CampaignHazard CreateMeteor(FinalCampaignStage stage, float x, float ground)
    {
        var hazard = Create(stage, stage.meteorSprite, new Vector2(x, ground + 0.12f));
        hazard.meteor = hazard.lethal = true; hazard.warningSeconds = 1.2f; hazard.ground = ground;
        hazard.Fit(new Vector2(1, 0.22f));
        return hazard;
    }
    static CampaignHazard Create(FinalCampaignStage stage, Sprite sprite, Vector2 point)
    {
        var root = new GameObject("Aviso de ataque"); root.transform.position = point;
        var hazard = root.AddComponent<CampaignHazard>(); hazard.stage = stage;
        hazard.visual = root.AddComponent<SpriteRenderer>(); hazard.visual.sprite = sprite; hazard.visual.sortingOrder = 8;
        hazard.trigger = root.AddComponent<BoxCollider2D>(); hazard.trigger.isTrigger = true; hazard.trigger.enabled = false;
        return hazard;
    }
    void Fit(Vector2 size)
    {
        var bounds = visual.sprite.bounds;
        transform.localScale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, 1);
        trigger.size = bounds.size * 0.7f; trigger.offset = bounds.center;
    }
    void Update()
    {
        if (TutorialCanvasUI.BlocksGameplay) return;
        if (stage == null || stage.Level.Completed || stage.Level.player.Dying) { Destroy(gameObject); return; }
        age += Time.deltaTime;
        if (!Armed)
        {
            visual.color = new Color(1, 1, 1, 0.45f + Mathf.PingPong(age * 1.5f, 0.55f));
            if (age < warningSeconds) return;
            Armed = true; trigger.enabled = true; visual.color = Color.white;
            gameObject.name = meteor ? "Meteorito letal" : lethal ? "Ritual letal" : "Llamarada";
            if (meteor)
            {
                transform.position = new Vector3(transform.position.x, ground + 9, 0);
                Fit(new Vector2(1, 1.6f)); speed = 11;
            }
            else
            {
                transform.position = new Vector3(transform.position.x, ground + 1.5f, 0);
                Fit(new Vector2(lethal ? 2.4f : 1.3f, 3));
            }
        }
        if (meteor)
        {
            float travel = speed * Time.deltaTime;
            var size = Vector2.Scale(trigger.size, transform.lossyScale);
            foreach (var hit in Physics2D.BoxCastAll(transform.TransformPoint(trigger.offset), size, 0, Vector2.down, travel))
            {
                var p = hit.collider.GetComponent<player>();
                if (p != null) { Hit(p); return; }
                if (!hit.collider.isTrigger && hit.collider.attachedRigidbody == null) { GameAudio.Instance?.Cue(GameAudio.Sound.MeteorImpact); Destroy(gameObject); return; }
            }
            transform.position += Vector3.down * travel;
            if (transform.position.y < ground - 2) Destroy(gameObject);
        }
        else
        {
            var size = Vector2.Scale(trigger.size, transform.lossyScale);
            foreach (var collider in Physics2D.OverlapBoxAll(transform.TransformPoint(trigger.offset), size, 0))
                if (collider.GetComponent<player>() is player p) Hit(p);
            if (age > warningSeconds + 0.65f) Destroy(gameObject);
        }
    }
    public void Hit(player p)
    {
        if (!Armed) return;
        if (lethal) p.KillInstantly(); else p.Damage(stage.Level.stageNumber == 7 ? 9 : 4);
        if (meteor) { GameAudio.Instance?.Cue(GameAudio.Sound.MeteorImpact); Destroy(gameObject); }
    }
}
