using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    public bool hunter;
    public int maximumHealth = 180;
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
    void Awake()
    {
        enemy = GetComponent<Enemy>(); body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<BoxCollider2D>();
        enemy.health = maximumHealth; home = transform.position;
    }
    void Start() { CreateHUD(); }
    public void Activate()
    {
        if (Active || Defeated) return;
        Active = true;
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
        // Stones stay lethal to normal enemies, but remove a chunk from a boss.
        enemy.health = Mathf.Max(0, enemy.health - Mathf.Min(amount, 20));
        if (enemy.health > 0) return;
        Defeated = true; Active = false; movement = 0;
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
        p.Damage(hunter ? 2 : 3); nextContact = Time.time + 0.8f;
    }
    public void ResetEncounter()
    {
        if (Defeated) return;
        StopAllCoroutines(); attacks = null; Active = false; movement = 0; performingAttack = false; turnUntil = 0; enemy.health = maximumHealth;
        body.position = home; body.linearVelocity = Vector2.zero; Play("Idle");
        foreach (var summon in FindObjectsByType<BossSummon>()) Destroy(summon.gameObject);
    }
    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(1);
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
            yield return new WaitForSeconds(Enraged ? 0.5f : 0.9f);
        }
    }
    IEnumerator FanVolley()
    {
        Play("Attack");
        yield return new WaitForSeconds(0.6f);
        int count = Enraged ? 7 : 5;
        for (int wave = 0; wave < 2; wave++)
        {
            var aim = ((Vector2)stage.Level.player.transform.position - body.position).normalized;
            for (int i = 0; i < count; i++) Shoot(Quaternion.Euler(0, 0, (i - (count - 1) / 2f) * 11) * aim, 5.2f, 2);
            yield return new WaitForSeconds(0.45f);
        }
    }
    IEnumerator Ritual(bool lethal)
    {
        Play(lethal ? "Ultimate" : "Cast");
        float x = Mathf.Clamp(stage.Level.player.transform.position.x, arenaLeft + 1.5f, arenaRight - 1.5f);
        stage.Level.Notify(lethal ? "RITUAL LETAL: ¡sal de la marca!" : "El suelo arde: aléjate de las marcas.");
        if (lethal) CampaignHazard.CreateRitual(stage, ritualSprite, x, 1.6f, true);
        else for (int i = -1; i <= 1; i++) CampaignHazard.CreateRitual(stage, ritualSprite, Mathf.Clamp(x + i * 2.6f, arenaLeft + 1, arenaRight - 1), 1.1f, false);
        yield return new WaitForSeconds(lethal ? 2.25f : 1.9f);
    }
    IEnumerator Summon()
    {
        Play("Cast"); yield return new WaitForSeconds(0.7f);
        int existing = FindObjectsByType<BossSummon>().Length;
        int remaining = Mathf.Min(2, 3 - existing);
        if (summonPrefab != null && remaining > 0)
        {
            var template = summonPrefab.GetComponent<BoxCollider2D>();
            var size = Vector2.Scale(template.size, summonPrefab.transform.localScale);
            var offset = Vector2.Scale(template.offset, summonPrefab.transform.localScale);
            float rightmost = transform.position.x - hitbox.bounds.extents.x - size.x / 2 - 1.1f;
            // Search only to the left; never fall back to the right or overlap an actor/platform.
            for (float x = rightmost; x >= arenaLeft + size.x / 2 + 1 && remaining > 0; x -= 2.2f)
            {
                Vector2 point = new Vector2(x, stage.floorY + size.y / 2 + 0.03f) - offset;
                var filter = new ContactFilter2D(); filter.NoFilter(); filter.useTriggers = false;
                var overlaps = new Collider2D[16];
                int count = Physics2D.OverlapBox(point + offset, size + new Vector2(1.6f, 0), 0, filter, overlaps);
                if (count > 0) continue;
                var root = Instantiate(summonPrefab, point, Quaternion.identity);
                var summoned = root.GetComponent<Enemy>(); summoned.health = 3; summoned.contactDamage = 2;
                summoned.patrolDistance = 0.75f; root.AddComponent<BossSummon>();
                Physics2D.SyncTransforms(); remaining--;
            }
        }
        yield return new WaitForSeconds(0.7f);
    }
    IEnumerator HunterBurst(int cycle)
    {
        Play("Walk");
        float sign = Mathf.Sign(stage.Level.player.transform.position.x - transform.position.x);
        movement = sign * (Enraged ? 4.8f : 3.2f);
        yield return new WaitForSeconds(cycle % 2 == 0 ? 0.75f : 1.1f);
        movement = 0; Play("Attack"); stage.Level.Notify("Ráfaga: ¡salta y cambia de plataforma!");
        yield return new WaitForSeconds(0.4f);
        Vector2 aim = Vector2.left;
        int rounds = Enraged ? 24 : 18;
        for (int i = 0; i < rounds; i++)
        {
            if (i % 4 == 0) aim = ((Vector2)stage.Level.player.transform.position - body.position).normalized;
            Shoot(aim, 10, 1);
            if (i % 4 == 0) Play("Attack");
            yield return new WaitForSeconds(0.065f);
        }
    }
    void Shoot(Vector2 aim, float speed, int damage)
    {
        var shot = Instantiate(enemy.projectile, (Vector2)transform.position + aim * 1.05f, Quaternion.identity);
        shot.Launch(aim, hitbox, true, speed, damage);
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
    void OnDisable() { if (hud != null) hud.gameObject.SetActive(false); }
}
