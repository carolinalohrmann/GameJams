using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    public bool hunter;
    public int maximumHealth = 301;
    public Sprite projectileSprite, ritualSprite;
    public GameObject summonPrefab;
    public FinalCampaignStage stage;
    public float arenaLeft, arenaRight;
    public bool Active { get; private set; }
    public bool Defeated { get; private set; }
    public int Health => enemy.health;
    Enemy enemy;
    Rigidbody2D body;
    BoxCollider2D hitbox;
    Vector3 home;
    Coroutine attacks;
    float movement, nextContact;
    bool performingAttack;
    int strafeDirection = -1;
    float turnUntil;
    RectTransform hud;
    Image healthFill;
    Text healthLabel;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    public bool Enraged => Health <= maximumHealth / 2;
    bool SegundoNecromancer => !hunter && stage != null && stage.Level.stageNumber == 7;
    float PausaAtaque => SegundoNecromancer ? 0.9f : 1;
    int AttackBonus => SegundoNecromancer ? 5 : 0;
    public void ConfigureStage(FinalCampaignStage owner)
    {
        stage = owner;
        if (!hunter && stage.Level.stageNumber == 7) maximumHealth = 400;
        if (SegundoNecromancer && enemy.animator != null) enemy.animator.speed = 1.4f;
        enemy.health = maximumHealth;
    }
    void Awake()
    {
        enemy = GetComponent<Enemy>(); body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<BoxCollider2D>();
        enemy.health = maximumHealth; home = transform.position;
        if (!hunter && enemy.animator != null) enemy.animator.speed = 1.25f;
    }
    void Start() { CreateHUD(); }
    public void Activate()
    {
        if (Active || Defeated) return;
        Active = true;
        GameAudio.Instance?.BossBreathing(hunter);
        stage.Level.Notify(hunter ? "El Cazador prepara su ráfaga." : "El Necromancer despierta. Cuidado con el ritual letal.");
        attacks = StartCoroutine(AttackLoop());
    }
    void Update()
    {
        if (stage == null) return;
        if (hud != null)
        {
            hud.gameObject.SetActive(Active && !Defeated && !TutorialCanvasUI.BlocksGameplay);
            healthFill.fillAmount = Mathf.Clamp01(Health / (float)maximumHealth);
            healthFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400 * Mathf.Clamp01(Health / (float)maximumHealth));
            healthLabel.text = (hunter ? "CAZADOR" : "NECROMANCER") + "  " + Mathf.Max(0, Health) + " / " + maximumHealth;
        }
        var p = stage.Level.player;
        if (p.Dying && Active) { if (attacks != null) StopCoroutine(attacks); attacks = null; Active = false; movement = 0; Play("Idle"); }
        if (!Defeated && enemy.visual != null) enemy.visual.flipX = (p.transform.position.x < transform.position.x) != enemy.spriteFacesLeft;
    }
    void FixedUpdate()
    {
        if (!Active || Defeated || stage == null || stage.Level.player.Dying) return;
        if (!hunter)
        {
            float delta = stage.Level.player.transform.position.x - body.position.x;
            if (Time.time >= turnUntil)
            {
                strafeDirection = Mathf.Abs(delta) > 5 ? (delta > 0 ? 1 : -1)
                    : Mathf.Abs(delta) < 3.2f ? (delta > 0 ? -1 : 1)
                    : Mathf.Sin(Time.time * 1.3f) > 0 ? 1 : -1;
            }
            movement = strafeDirection * (Enraged ? 2.1f : 1.4f);
            if (SegundoNecromancer) movement *= 1.2f;
            if (!performingAttack && !enemy.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk")) Play("Walk");
        }
        if (movement == 0) return;
        float distance = movement * Time.fixedDeltaTime;
        float x = Mathf.Clamp(body.position.x + distance, arenaLeft + 1, arenaRight - 1);
        distance = x - body.position.x;
        if (Mathf.Abs(distance) < 0.0001f) { strafeDirection *= -1; turnUntil = Time.time + 1; return; }
        var filter = new ContactFilter2D(); filter.NoFilter(); filter.useTriggers = false;
        int count = hitbox.Cast(Vector2.right * Mathf.Sign(distance), filter, hits, Mathf.Abs(distance) + 0.02f);
        float allowed = Mathf.Abs(distance);
        for (int i = 0; i < count; i++)
            if (hits[i].normal.x * Mathf.Sign(distance) < -0.5f) allowed = Mathf.Min(allowed, Mathf.Max(0, hits[i].distance - 0.015f));
        body.MovePosition(body.position + Vector2.right * (Mathf.Sign(distance) * allowed));
        if (!hunter && allowed < Mathf.Abs(distance) * 0.1f) { strafeDirection *= -1; turnUntil = Time.time + 1; }
    }
    public void TakeDamage(int amount)
    {
        if (Defeated || stage == null || stage.Level.player.Dying) return;
        Activate();
        enemy.health = Mathf.Max(0, enemy.health - Mathf.Min(amount, 20));
        if (enemy.health > 0) return;
        Defeated = true; Active = false; movement = 0;
        GameAudio.Instance?.StopBoss();
        if (attacks != null) StopCoroutine(attacks);
        hitbox.enabled = false; body.linearVelocity = Vector2.zero;
        Play("Death"); stage.ClearAttacks();
        StartCoroutine(FinishDeath());
    }
    IEnumerator FinishDeath()
    {
        yield return new WaitForSeconds(1.15f);
        stage.BossDefeated();
        if (hud != null) Destroy(hud.gameObject);
    }
    public void Contact(player p)
    {
        if (Defeated || Time.time < nextContact) return;
        if (hunter) p.KillInstantly();
        else p.Damage(4 + AttackBonus);
        nextContact = Time.time + (hunter ? 0.8f : 0.65f);
    }
    public void ResetEncounter()
    {
        if (Defeated) return;
        GameAudio.Instance?.StopBoss();
        StopAllCoroutines(); attacks = null; Active = false; movement = 0; performingAttack = false; turnUntil = 0; enemy.health = maximumHealth;
        body.position = home; body.linearVelocity = Vector2.zero; Play("Idle");
        foreach (var summon in FindObjectsByType<BossSummon>()) Destroy(summon.gameObject);
    }
    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(hunter ? 0.9f : 0.75f * PausaAtaque);
        int cycle = 0;
        while (!Defeated)
        {
            performingAttack = true;
            if (hunter) yield return HunterBurst(cycle++);
            else
            {
                switch (cycle++ % 4)
                {
                    case 0: yield return FanVolley(); break;
                    case 1: yield return Ritual(false); break;
                    case 2: yield return Summon(); break;
                    default: yield return Ritual(true); break;
                }
            }
            performingAttack = false; movement = 0; Play("Idle");
            yield return new WaitForSeconds(hunter ? (Enraged ? 0.45f : 0.8f) : (Enraged ? 0.35f : 0.65f) * PausaAtaque);
        }
    }
    IEnumerator FanVolley()
    {
        GameAudio.Instance?.Magic();
        Play("Attack");
        yield return new WaitForSeconds(0.45f * PausaAtaque);
        int count = Enraged ? 7 : 5;
        for (int wave = 0; wave < 2; wave++)
        {
            var aim = ((Vector2)stage.Level.player.transform.position - body.position).normalized;
            for (int i = 0; i < count; i++) Shoot(Quaternion.Euler(0, 0, (i - (count - 1) / 2f) * 11) * aim, 6.5f, 3);
            yield return new WaitForSeconds(0.3375f * PausaAtaque);
        }
    }
    IEnumerator Ritual(bool lethal)
    {
        GameAudio.Instance?.Magic();
        Play(lethal ? "Ultimate" : "Cast");
        float x = Mathf.Clamp(stage.Level.player.transform.position.x, arenaLeft + 1.5f, arenaRight - 1.5f);
        stage.Level.Notify(lethal ? "RITUAL LETAL: ¡sal de la marca!" : "El suelo arde: aléjate de las marcas.");
        if (lethal) CampaignHazard.CreateRitual(stage, ritualSprite, x, 1.2f * PausaAtaque, true);
        else for (int i = -1; i <= 1; i++) CampaignHazard.CreateRitual(stage, ritualSprite, Mathf.Clamp(x + i * 2.6f, arenaLeft + 1, arenaRight - 1), 0.85f * PausaAtaque, false);
        yield return new WaitForSeconds((lethal ? 1.9f : 1.55f) * PausaAtaque);
    }
    IEnumerator Summon()
    {
        GameAudio.Instance?.Magic();
        Play("Cast"); yield return new WaitForSeconds(0.5f * PausaAtaque);
        int existing = FindObjectsByType<BossSummon>().Length;
        int remaining = Mathf.Min(2, 3 - existing);
        if (summonPrefab != null && remaining > 0)
        {
            var template = summonPrefab.GetComponent<BoxCollider2D>();
            var size = Vector2.Scale(template.size, summonPrefab.transform.localScale);
            var offset = Vector2.Scale(template.offset, summonPrefab.transform.localScale);
            float rightmost = transform.position.x - hitbox.bounds.extents.x - size.x / 2 - 1.1f;
            for (float x = rightmost; x >= arenaLeft + size.x / 2 + 1 && remaining > 0; x -= 2.2f)
            {
                Vector2 point = new Vector2(x, stage.floorY + size.y / 2 + 0.03f) - offset;
                var filter = new ContactFilter2D(); filter.NoFilter(); filter.useTriggers = false;
                var overlaps = new Collider2D[16];
                int count = Physics2D.OverlapBox(point + offset, size + new Vector2(1.6f, 0), 0, filter, overlaps);
                if (count > 0) continue;
                var root = Instantiate(summonPrefab, point, Quaternion.identity);
                var summoned = root.GetComponent<Enemy>(); summoned.health = 10; summoned.contactDamage = 3 + AttackBonus;
                summoned.patrolDistance = 0.75f; root.AddComponent<BossSummon>();
                Physics2D.SyncTransforms(); remaining--;
            }
        }
        yield return new WaitForSeconds(0.5f * PausaAtaque);
    }
    IEnumerator HunterBurst(int cycle)
    {
        Play("Walk");
        float sign = Mathf.Sign(stage.Level.player.transform.position.x - transform.position.x);
        movement = sign * (Enraged ? 5.4f : 3.6f);
        yield return new WaitForSeconds(cycle % 2 == 0 ? 0.75f : 1.1f);
        movement = 0; Play("Attack"); stage.Level.Notify("Ráfaga: ¡salta y cambia de plataforma!");
        yield return new WaitForSeconds(0.35f);
        Vector2 aim = Vector2.left;
        int rounds = Enraged ? 24 : 18;
        for (int i = 0; i < rounds; i++)
        {
            if (i % 4 == 0) aim = ((Vector2)stage.Level.player.transform.position - body.position).normalized;
            Shoot(aim, 10, 6);
            if (i % 4 == 0) Play("Attack");
            yield return new WaitForSeconds(0.06f);
        }
    }
    void Shoot(Vector2 aim, float speed, int damage)
    {
        if (hunter) GameAudio.Instance?.HunterShot();
        var shot = Instantiate(enemy.projectile, (Vector2)transform.position + aim * 1.05f, Quaternion.identity);
        shot.Launch(aim, hitbox, true, speed, damage + AttackBonus);
        shot.gameObject.AddComponent<CampaignAttack>();
        if (hunter) shot.UsePlayerEffect(true);
        else
        {
            var visual = shot.GetComponent<SpriteRenderer>(); visual.sprite = projectileSprite; visual.color = Color.white;
            shot.transform.localScale = Vector3.one * (0.4f / projectileSprite.bounds.size.x);
            shot.GetComponent<BoxCollider2D>().size = projectileSprite.bounds.size * 0.7f;
        }
        shot.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
    }
    void Play(string animation) { if (enemy.animator != null) enemy.animator.Play(animation, 0, 0); }
    void CreateHUD()
    {
        var ui = TutorialCanvasUI.Instance;
        if (ui == null) return;
        hud = new GameObject("Vida del jefe", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        hud.SetParent(ui.transform, false); hud.anchorMin = hud.anchorMax = hud.pivot = new Vector2(0.5f, 1);
        hud.anchoredPosition = new Vector2(0, -90); hud.sizeDelta = new Vector2(420, 40);
        hud.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.06f, 0.9f); hud.GetComponent<Image>().raycastTarget = false;
        healthLabel = new GameObject("Nombre y vida", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        healthLabel.transform.SetParent(hud, false); healthLabel.rectTransform.sizeDelta = new Vector2(410, 28);
        healthLabel.rectTransform.anchoredPosition = new Vector2(0, 5); healthLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthLabel.fontSize = 19; healthLabel.alignment = TextAnchor.MiddleCenter; healthLabel.raycastTarget = false;
        healthFill = new GameObject("Barra", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        healthFill.transform.SetParent(hud, false); healthFill.rectTransform.sizeDelta = new Vector2(400, 5);
        healthFill.rectTransform.pivot = new Vector2(0, 0.5f);
        healthFill.rectTransform.anchoredPosition = new Vector2(-200, -14); healthFill.raycastTarget = false;
    }
    void OnDestroy() { if (hud != null) Destroy(hud.gameObject); }
    void OnDisable() { GameAudio.Instance?.StopBoss(); if (hud != null) hud.gameObject.SetActive(false); }
}
