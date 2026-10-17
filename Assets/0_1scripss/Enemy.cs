using UnityEngine;

public class Enemy : MonoBehaviour
{
    public bool ranged;
    public bullet projectile;
    public float patrolDistance = 2, moveSpeed = 1.2f;
    public int health = 2;
    public Animator animator;
    public SpriteRenderer visual;
    public bool flying;
    Rigidbody2D body;
    Collider2D hitbox, targetHitbox;
    player target;
    float home, homeY, nextAttack, attackUntil, hitUntil;
    int direction = -1;
    bool dead;
    string state;
    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        body.gravityScale = 0;
    }
    void Start()
    {
        home = transform.position.x;
        homeY = transform.position.y;
        target = FindAnyObjectByType<player>();
        if (target != null) targetHitbox = target.GetComponent<Collider2D>();
        nextAttack = Time.time + 1.5f;
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
        var hit = Physics2D.Raycast((Vector2)hitbox.bounds.center + delta.normalized * 0.72f, delta.normalized, delta.magnitude);
        return hit.collider != null && hit.collider == targetHitbox;
    }
    void FixedUpdate()
    {
        body.linearVelocity = Vector2.zero;
        if (dead || target == null || target.Dying || (LevelOne.Instance != null && LevelOne.Instance.Completed)) return;
        float delta = target.transform.position.x - transform.position.x;
        if (visual != null) visual.flipX = delta < 0;
        if (Time.time < hitUntil) { Animate("Hit"); return; }
        if (hitbox.IsTouching(targetHitbox)) { Animate(ranged ? "Idle" : "Attack"); return; }
        bool nearby = Mathf.Abs(delta) < (ranged ? 8 : 4) && Mathf.Abs(target.transform.position.y - transform.position.y) < 1.8f;
        bool moving = false;
        if (!ranged)
        {
            if (nearby) direction = delta > 0 ? 1 : -1;
            else if (transform.position.x < home - patrolDistance || transform.position.x > home + patrolDistance) direction = transform.position.x < home ? 1 : -1;
            var ground = Physics2D.Raycast((Vector2)transform.position + new Vector2(direction * 0.65f, -0.48f), Vector2.down, 0.7f);
            bool inBounds = Mathf.Abs(transform.position.x + direction * 0.1f - home) <= patrolDistance + 0.1f;
            bool blocked = nearby && Mathf.Abs(delta) <= hitbox.bounds.extents.x + targetHitbox.bounds.extents.x + 0.06f;
            if (ground.collider != null && inBounds && !blocked)
            {
                body.MovePosition(body.position + Vector2.right * (direction * moveSpeed * Time.fixedDeltaTime));
                moving = true;
            }
            if (ground.collider == null || !inBounds) direction *= -1;
            if (visual != null) visual.flipX = direction < 0;
        }
        else
        {
            if (flying) body.MovePosition(new Vector2(home, homeY + Mathf.Sin(Time.time * 2) * 0.1f));
            if (nearby && Time.time >= nextAttack && CanSeePlayer())
            {
                Vector2 aim = ((Vector2)targetHitbox.bounds.center - body.position).normalized;
                Instantiate(projectile, body.position + aim * 0.72f, Quaternion.identity).Launch(aim, hitbox, true, 4);
                nextAttack = Time.time + 2.2f;
                attackUntil = Time.time + 0.6f;
            }
        }
        Animate(Time.time < attackUntil ? "Attack" : moving ? "Walk" : "Idle");
    }
    void OnCollisionEnter2D(Collision2D collision) { HurtPlayer(collision); }
    void OnCollisionStay2D(Collision2D collision) { HurtPlayer(collision); }
    void HurtPlayer(Collision2D collision)
    {
        if (dead) return;
        var p = collision.gameObject.GetComponent<player>();
        if (p != null) { p.Damage(); attackUntil = Time.time + 0.5f; }
    }
    public void Damage(int damage = 1)
    {
        if (dead) return;
        health -= damage;
        if (health <= 0)
        {
            dead = true;
            hitbox.enabled = false;
            body.linearVelocity = Vector2.zero;
            Animate("Death");
            Destroy(gameObject, animator == null ? 0 : 0.65f);
        }
        else { hitUntil = Time.time + 0.2f; Animate("Hit"); }
    }
}
