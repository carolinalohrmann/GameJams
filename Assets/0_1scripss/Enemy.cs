using UnityEngine;
public class Enemy : MonoBehaviour
{
    public bool ranged;
    public bullet projectile;
    public float patrolDistance = 2, moveSpeed = 1.2f;
    public int health = 2;
    Rigidbody2D body;
    Collider2D hitbox;
    player target;
    float home, nextAttack;
    int direction = -1;
    void Awake()
    {
        body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<Collider2D>();
        body.bodyType = RigidbodyType2D.Kinematic; body.useFullKinematicContacts = true; body.gravityScale = 0;
    }
    void Start()
    {
        home = transform.position.x; target = FindAnyObjectByType<player>(); nextAttack = Time.time + 1.5f;
    }
    bool CanSeePlayer()
    {
        Vector2 delta = target.transform.position - transform.position;
        var hit = Physics2D.Raycast((Vector2)transform.position + delta.normalized * 0.65f, delta.normalized, delta.magnitude);
        return hit.collider != null && hit.collider.GetComponent<player>() != null;
    }
    void FixedUpdate()
    {
        body.linearVelocity = Vector2.zero;
        if (target == null || (LevelOne.Instance != null && LevelOne.Instance.Completed)) return;
        var targetHitbox = target.GetComponent<Collider2D>();
        if (hitbox.IsTouching(targetHitbox)) return;
        float delta = target.transform.position.x - transform.position.x;
        bool nearby = Mathf.Abs(delta) < (ranged ? 8 : 4) && Mathf.Abs(target.transform.position.y - transform.position.y) < 1.2f;
        if (!ranged)
        {
            if (nearby) direction = delta > 0 ? 1 : -1;
            else if (transform.position.x < home - patrolDistance || transform.position.x > home + patrolDistance) direction = transform.position.x < home ? 1 : -1;
            var ground = Physics2D.Raycast((Vector2)transform.position + new Vector2(direction * 0.65f, -0.48f), Vector2.down, 0.7f);
            bool inBounds = Mathf.Abs(transform.position.x + direction * 0.1f - home) <= patrolDistance + 0.1f;
            bool blocked = nearby && Mathf.Abs(delta) <= hitbox.bounds.extents.x + targetHitbox.bounds.extents.x + 0.06f;
            if (ground.collider != null && inBounds && !blocked) body.MovePosition(body.position + Vector2.right * (direction * moveSpeed * Time.fixedDeltaTime));
            if (ground.collider == null || !inBounds) direction *= -1;
        }
        else if (nearby && Time.time >= nextAttack && CanSeePlayer())
        {
            int sign = delta > 0 ? 1 : -1;
            Instantiate(projectile, transform.position + new Vector3(sign * 0.7f, 0, 0), Quaternion.identity).Launch(Vector2.right * sign, hitbox, true, 4);
            nextAttack = Time.time + 2.2f;
        }
    }
    void OnCollisionEnter2D(Collision2D collision) { HurtPlayer(collision); }
    void OnCollisionStay2D(Collision2D collision) { HurtPlayer(collision); }
    void HurtPlayer(Collision2D collision) { var p = collision.gameObject.GetComponent<player>(); if (p != null) p.Damage(); }
    public void Damage() { if (--health <= 0) Destroy(gameObject); }
}
