using UnityEngine;

public class Enemy : MonoBehaviour
{
    public bool ranged;
    public bullet projectile;
    public float patrolDistance = 2, moveSpeed = 1.2f;
    public int health = 2;
    public int contactDamage = 1, projectileDamage = 1;
    BossController boss;
    public Animator animator;
    public SpriteRenderer visual;
    public AudioClip attackSound, deathSound;
    public bool flying;
    public bool spriteFacesLeft;
    Rigidbody2D body;
    Collider2D hitbox, targetHitbox;
    player target;
    float home, homeY, nextAttack, attackUntil, hitUntil;
    int direction = -1;
    bool dead;
    bool healthAdjusted;
    string state;
    readonly RaycastHit2D[] obstacles = new RaycastHit2D[16];
    float nextMeleeAttack;
    float attackDuration = 0.5f;
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        boss = GetComponent<BossController>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        body.gravityScale = 0;
    }
    void Start()
    {
        var level = LevelOne.Instance;
        if (!healthAdjusted && boss == null && GetComponent<BossSummon>() == null && level != null)
        {
            health = level.stageNumber >= 7 ? 10 : 6;
            healthAdjusted = true;
        }
        home = transform.position.x;
        homeY = transform.position.y;
        target = FindAnyObjectByType<player>();
        if (target != null) targetHitbox = target.GetComponent<Collider2D>();
        nextAttack = Time.time + 1.5f;
        if (animator != null)
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name.Contains("Attack")) attackDuration = Mathf.Max(0.25f, clip.length);
    }
    void Animate(string animation)
    {
        if (animator == null || state == animation) return;
        animator.Play(animation, 0, 0);
        state = animation;
    }
    bool CanSeePlayer()
    {
        Vector2 delta = targetHitbox.bounds.center - hitbox.bounds.center;
        var filter = new ContactFilter2D(); filter.NoFilter(); filter.useTriggers = false;
        int count = Physics2D.Raycast(hitbox.bounds.center, delta.normalized, filter, obstacles, delta.magnitude);
        for (int i = 0; i < count; i++)
            if (obstacles[i].collider != hitbox) return obstacles[i].collider == targetHitbox;
        return false;
    }
    bool MoveSafely(Vector2 displacement)
    {
        float distance = displacement.magnitude;
        if (distance < 0.0001f) return false;
        var filter = new ContactFilter2D(); filter.NoFilter(); filter.useTriggers = false;
        int count = hitbox.Cast(displacement.normalized, filter, obstacles, distance + 0.02f);
        float allowed = distance;
        for (int i = 0; i < count; i++)
        {
            var hit = obstacles[i];
            if (Vector2.Dot(hit.normal, displacement.normalized) > -0.05f) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - 0.015f));
        }
        body.MovePosition(body.position + displacement.normalized * allowed);
        return allowed > 0.0001f;
    }
    void ContactAttack()
    {
        if (Time.time >= nextMeleeAttack)
        {
            target.Damage(contactDamage);
            GameAudio.Instance?.EnemyAttack(this);
            attackUntil = Time.time + attackDuration;
            nextMeleeAttack = attackUntil + 0.25f;
            state = null;
        }
        Animate(!ranged && Time.time < attackUntil ? "Attack" : "Idle");
    }
    void FixedUpdate()
    {
        if (boss != null) return;
        body.linearVelocity = Vector2.zero;
        if (dead || target == null || target.Dying || (LevelOne.Instance != null && LevelOne.Instance.Completed)) return;
        float delta = target.transform.position.x - transform.position.x;
        Face(delta);
        if (Time.time < hitUntil) { Animate("Hit"); return; }
        if (hitbox.IsTouching(targetHitbox)) { ContactAttack(); return; }
        if (!ranged && hitbox.Distance(targetHitbox).distance <= 0.12f && CanSeePlayer()) { ContactAttack(); return; }
        bool nearby = Mathf.Abs(delta) < (ranged ? 8 : 4) && Mathf.Abs(target.transform.position.y - transform.position.y) < 1.8f;
        bool moving = false;
        if (!ranged)
        {
            if (nearby) direction = delta > 0 ? 1 : -1;
            else if (transform.position.x < home - patrolDistance || transform.position.x > home + patrolDistance) direction = transform.position.x < home ? 1 : -1;
            var ground = Physics2D.Raycast(new Vector2(hitbox.bounds.center.x + direction * (hitbox.bounds.extents.x + 0.2f), hitbox.bounds.min.y + 0.05f), Vector2.down, 0.7f);
            bool inBounds = Mathf.Abs(transform.position.x + direction * 0.1f - home) <= patrolDistance + 0.1f;
            bool blocked = nearby && Mathf.Abs(delta) <= hitbox.bounds.extents.x + targetHitbox.bounds.extents.x + 0.06f;
            if (ground.collider != null && inBounds && !blocked)
            {
                moving = MoveSafely(Vector2.right * (direction * moveSpeed * Time.fixedDeltaTime));
                if (!moving) direction *= -1;
            }
            if (ground.collider == null || !inBounds) direction *= -1;
            Face(direction);
        }
        else
        {
            if (flying) MoveSafely(new Vector2(home, homeY + Mathf.Sin(Time.time * 2) * 0.1f) - body.position);
            if (nearby && Time.time >= nextAttack && CanSeePlayer())
            {
                Vector2 aim = ((Vector2)targetHitbox.bounds.center - body.position).normalized;
                Instantiate(projectile, body.position + aim * 0.72f, Quaternion.identity).Launch(aim, hitbox, true, 4, projectileDamage);
                GameAudio.Instance?.EnemyAttack(this);
                nextAttack = Time.time + 2.2f;
                attackUntil = Time.time + 0.6f;
            }
        }
        Animate(Time.time < attackUntil ? "Attack" : moving ? "Walk" : "Idle");
    }
    void OnCollisionEnter2D(Collision2D collision) { HurtPlayer(collision); }
    void Face(float horizontalDirection)
    {
        if (visual != null) visual.flipX = (horizontalDirection < 0) != spriteFacesLeft;
    }
    void OnCollisionStay2D(Collision2D collision) { HurtPlayer(collision); }
    void HurtPlayer(Collision2D collision)
    {
        if (dead) return;
        var p = collision.gameObject.GetComponent<player>();
        if (boss != null) { if (p != null) boss.Contact(p); return; }
        if (p != null && Time.time >= hitUntil) ContactAttack();
    }
    public void Damage(int damage = 1)
    {
        if (boss != null) { boss.TakeDamage(damage); return; }
        if (dead) return;
        health -= damage;
        if (health <= 0)
        {
            dead = true;
            GameAudio.Instance?.EnemyDeath(this);
            hitbox.enabled = false;
            body.linearVelocity = Vector2.zero;
            Animate("Death");
            Destroy(gameObject, animator == null ? 0 : 0.65f);
        }
        else { hitUntil = Time.time + 0.2f; Animate("Hit"); }
    }
}
