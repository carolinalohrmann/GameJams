using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    public Transform shootPoint;
    public bullet projectile;
    public float speed = 5.5f, jumpSpeed = 10f;
    public int maxHealth = 5;
    public int Health { get; private set; }
    Rigidbody2D body;
    Collider2D hitbox;
    float movement, jumpBuffer, groundedUntil, nextShot, invincibleUntil;
    int facing = 1;
    Vector3 spawn;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    void Awake()
    {
        body = GetComponent<Rigidbody2D>(); hitbox = GetComponent<Collider2D>();
        spawn = transform.position; Health = maxHealth;
    }
    void Update()
    {
        if (transform.position.y < -8) Respawn();
        var k = Keyboard.current;
        if (k == null) return;
        if (LevelOne.Instance != null && LevelOne.Instance.Completed) { movement = 0; return; }
        movement = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
        if (movement != 0) facing = movement > 0 ? 1 : -1;
        if (shootPoint != null) shootPoint.localPosition = new Vector3(facing * 0.8f, 0.05f, 0);
        if (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame) RequestJump();
        if ((k.jKey.isPressed || (Mouse.current != null && Mouse.current.leftButton.isPressed)) && Time.time >= nextShot && projectile != null && shootPoint != null)
        {
            Instantiate(projectile, shootPoint.position, Quaternion.identity).Launch(Vector2.right * facing, hitbox, false, 12);
            nextShot = Time.time + 0.25f;
        }
        if (k.rKey.wasPressedThisFrame) Respawn();
    }
    void FixedUpdate()
    {
        int count = hitbox.Cast(Vector2.down, new ContactFilter2D { useTriggers = false }, groundHits, 0.08f);
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > 0.6f && groundHits[i].collider.GetComponent<Enemy>() == null) groundedUntil = Time.time + 0.1f;
        body.linearVelocity = new Vector2(movement * speed, body.linearVelocity.y);
        if (jumpBuffer > Time.time && groundedUntil > Time.time)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            jumpBuffer = groundedUntil = 0;
        }
    }
    public void RequestJump() { jumpBuffer = Time.time + 0.12f; }
    public void Damage()
    {
        if (Time.time < invincibleUntil || (LevelOne.Instance != null && LevelOne.Instance.Completed)) return;
        Health--; invincibleUntil = Time.time + 1.2f;
        if (Health <= 0) Respawn();
    }
    public void SetCheckpoint(Vector3 position) { spawn = position; Health = maxHealth; }
    public void Respawn()
    {
        Health = maxHealth; transform.position = spawn; body.linearVelocity = Vector2.zero;
        invincibleUntil = Time.time + 1.5f; jumpBuffer = groundedUntil = 0;
    }
}
