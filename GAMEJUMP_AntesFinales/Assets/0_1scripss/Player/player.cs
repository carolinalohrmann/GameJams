using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    public Transform shootPoint;
    public bullet projectile;
    public float speed = 5.5f, jumpSpeed = 10f, normalFireDelay = 0.35f;
    public int maxHealth = 5;
    public Animator animator;
    public SpriteRenderer characterVisual, weaponVisual;
    public Sprite minigunSprite, rockSprite, akSprite;
    public bool persistUpgrades = true;
    public string saveSlot = SavePrefix;
    public int Health { get; private set; }
    public bool DoubleJump => FeatherRemaining > 0;
    public float FeatherRemaining => Mathf.Max(0, featherUntil - Time.time);
    public bool HasAK => AKRemaining > 0;
    public bool Dying { get; private set; }
    public bool Grounded { get; private set; }
    public int RockAmmo { get; private set; }
    public float CitrusRemaining => Mathf.Max(0, citrusUntil - Time.time);
    public float ImmunityRemaining => Mathf.Max(0, immunityUntil - Time.time);
    public float MinigunRemaining => weapon == Weapon.Minigun ? Mathf.Max(0, minigunUntil - Time.time) : 0;
    public float AKRemaining => Mathf.Max(0, akUntil - Time.time);
    public float MoveSpeed => speed * (CitrusRemaining > 0 ? 1.5f : 1);
    public float FireDelay => (weapon == Weapon.Minigun ? 0.045f : weapon == Weapon.Rocks ? 0.9f : HasAK ? 0.12f : normalFireDelay) / (CitrusRemaining > 0 ? 1.5f : 1);
    public string WeaponName => weapon == Weapon.Minigun ? "Minigun" : weapon == Weapon.Rocks ? "Piedras " + RockAmmo + "/5" : HasAK ? "AK" : "Pistola";
    enum Weapon { Normal, Minigun, Rocks }
    Weapon weapon;
    Rigidbody2D body;
    Collider2D hitbox;
    float movement, jumpBuffer, groundedUntil, nextShot, invincibleUntil, citrusUntil, immunityUntil, minigunUntil, akUntil, featherUntil;
    int facing = 1;
    bool usedAirJump;
    Vector3 spawn;
    string animationState;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
    readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];
    public const string SavePrefix = "GameJams.Tutorial.v1.";

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        spawn = transform.position;
        if (persistUpgrades)
        {
            maxHealth = Mathf.Max(maxHealth, PlayerPrefs.GetInt(saveSlot + "MaxHealth", maxHealth));
        }
        Health = maxHealth;
    }
    void Update()
    {
        if (Dying) return;
        if (TutorialCanvasUI.BlocksGameplay) { movement = 0; return; }
        if (transform.position.y < -8) { Respawn(); return; }
        ExpireTemporaryWeapon();
        var k = Keyboard.current;
        bool completed = LevelOne.Instance != null && LevelOne.Instance.Completed;
        movement = k == null || completed ? 0 : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
        if (movement != 0) facing = movement > 0 ? 1 : -1;
        UpdateWeaponVisual();
        if (!completed && k != null && (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)) RequestJump();
        bool fire = Mouse.current != null && Mouse.current.leftButton.isPressed && !TutorialCanvasUI.PointerOverUI;
        if (!completed && fire) TryShoot();
        if (k != null && k.rKey.wasPressedThisFrame && !completed) Respawn();
        if (characterVisual != null) characterVisual.flipX = facing < 0;
        UpdateWeaponVisual();
        Animate(!Grounded ? "Jump" : Mathf.Abs(movement) > 0 ? "Walk" : "Idle");
    }
    void FixedUpdate()
    {
        if (Dying) { body.linearVelocity = Vector2.zero; return; }
        int count = hitbox.Cast(Vector2.down, new ContactFilter2D { useTriggers = false }, groundHits, 0.08f);
        Grounded = false;
        for (int i = 0; i < count; i++)
            if (groundHits[i].normal.y > 0.6f && groundHits[i].collider.GetComponent<Enemy>() == null && body.linearVelocity.y <= 0.1f) Grounded = true;
        if (Grounded) { groundedUntil = Time.time + 0.1f; usedAirJump = false; }
        float horizontalSpeed = movement * MoveSpeed;
        if (horizontalSpeed != 0)
        {
            // Stop pushing into side contacts so friction cannot hold a jump
            // against platforms or enemies. Enemies still require real contact
            // before stopping, so their contact damage continues to work.
            float sign = Mathf.Sign(horizontalSpeed);
            int walls = hitbox.Cast(Vector2.right * sign, new ContactFilter2D { useTriggers = false }, wallHits,
                Mathf.Abs(horizontalSpeed) * Time.fixedDeltaTime + 0.025f);
            for (int i = 0; i < walls; i++)
                if (wallHits[i].normal.x * sign < -0.6f && Mathf.Abs(wallHits[i].normal.y) < 0.7f)
                {
                    if (wallHits[i].collider.GetComponent<Enemy>() != null && !hitbox.IsTouching(wallHits[i].collider)) continue;
                    horizontalSpeed = 0;
                    break;
                }
        }
        body.linearVelocity = new Vector2(horizontalSpeed, body.linearVelocity.y);
        if (jumpBuffer > Time.time && (groundedUntil > Time.time || (DoubleJump && !usedAirJump)))
        {
            if (groundedUntil <= Time.time) usedAirJump = true;
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
            jumpBuffer = groundedUntil = 0;
            Grounded = false;
            if (animator != null) animator.Play("Jump", 0, 0);
            animationState = "Jump";
        }
    }
    void Animate(string state)
    {
        if (animator == null || animationState == state) return;
        animator.Play(state, 0, 0);
        animationState = state;
    }
    public void UpdateWeaponVisual()
    {
        ExpireTemporaryWeapon();
        if (weaponVisual == null) return;
        weaponVisual.sprite = weapon == Weapon.Minigun ? minigunSprite : weapon == Weapon.Rocks ? rockSprite : HasAK ? akSprite : null;
        weaponVisual.enabled = weaponVisual.sprite != null && !Dying;
        weaponVisual.flipX = facing < 0;
        // The supplied images have different canvas sizes; fit their world width
        // separately and align the barrel with the pistol drawn into the horse.
        bool rock = weapon == Weapon.Rocks;
        float width = rock ? 0.32f : weapon == Weapon.Minigun ? 0.65f : 0.7f;
        var offset = rock ? new Vector2(0.35f, 0.23f) : weapon == Weapon.Minigun ? new Vector2(0.30f, 0.23f) : new Vector2(0.27f, 0.20f);
        weaponVisual.transform.localPosition = new Vector3(facing * offset.x, offset.y, 0);
        if (weaponVisual.sprite != null)
            weaponVisual.transform.localScale = Vector3.one * (width / weaponVisual.sprite.bounds.size.x);
        if (shootPoint != null) shootPoint.localPosition = new Vector3(facing * (rock ? 0.8f : 0.65f), 0.23f, 0);
    }
    public void RequestJump() { if (!Dying) jumpBuffer = Time.time + 0.12f; }
    public void SetFacingDirection(int direction)
    {
        facing = direction < 0 ? -1 : 1;
        if (characterVisual != null) characterVisual.flipX = facing < 0;
        UpdateWeaponVisual();
    }
    public bool TryShoot()
    {
        if (Dying || Time.time < nextShot || projectile == null || shootPoint == null) return false;
        ExpireTemporaryWeapon();
        UpdateWeaponVisual();
        bool rock = weapon == Weapon.Rocks;
        var shot = Instantiate(projectile, shootPoint.position, Quaternion.identity);
        shot.Launch(Vector2.right * facing, hitbox, false, rock ? 8 : 12, rock ? 999 : 1);
        if (rock)
        {
            shot.GetComponent<SpriteRenderer>().sprite = rockSprite;
            // A large projectile with a collider fitted to its visible silhouette.
            shot.transform.localScale = Vector3.one * (0.85f / rockSprite.bounds.size.x);
            var rockCollider = shot.GetComponent<BoxCollider2D>();
            rockCollider.size = new Vector2(0.65f, 0.5f);
            if (--RockAmmo == 0) weapon = Weapon.Normal;
        }
        else shot.UsePlayerEffect(weapon == Weapon.Minigun || HasAK);
        nextShot = Time.time + (rock ? 0.9f / (CitrusRemaining > 0 ? 1.5f : 1) : FireDelay);
        return true;
    }
    public void Damage()
    {
        if (Dying || Time.time < invincibleUntil || ImmunityRemaining > 0 || (LevelOne.Instance != null && LevelOne.Instance.Completed)) return;
        Health--;
        invincibleUntil = Time.time + 1.2f;
        if (Health <= 0) StartCoroutine(Death());
    }
    IEnumerator Death()
    {
        Dying = true;
        movement = 0;
        body.linearVelocity = Vector2.zero;
        Animate("Death");
        if (weaponVisual != null) weaponVisual.enabled = false;
        yield return new WaitForSeconds(1.4f);
        Respawn();
    }
    public bool HasCollected(string id) => persistUpgrades && PlayerPrefs.GetInt(saveSlot + "Pickup." + id, 0) == 1;
    public bool ApplyPowerUp(PowerUp.Kind kind, string id, float durationSeconds = -1)
    {
        float duration = durationSeconds > 0 ? durationSeconds : PowerUp.DefaultDuration(kind);
        bool permanent = kind == PowerUp.Kind.Apple;
        if (permanent && HasCollected(id)) return false;
        switch (kind)
        {
            case PowerUp.Kind.Apple: maxHealth += 10; Health = Mathf.Min(maxHealth, Health + 10); break;
            case PowerUp.Kind.Medkit: Health = Mathf.Min(maxHealth, Health + 2); break;
            case PowerUp.Kind.Citrus: citrusUntil = Time.time + duration; break;
            case PowerUp.Kind.Feather: featherUntil = Time.time + duration; break;
            case PowerUp.Kind.Resistance: immunityUntil = Time.time + duration; break;
            case PowerUp.Kind.Shirt: weapon = Weapon.Minigun; minigunUntil = Time.time + duration; break;
            case PowerUp.Kind.Rock: weapon = Weapon.Rocks; RockAmmo = 5; break;
            case PowerUp.Kind.Chicken: akUntil = Time.time + duration; break;
        }
        if (permanent && persistUpgrades)
        {
            PlayerPrefs.SetInt(saveSlot + "MaxHealth", maxHealth);
            PlayerPrefs.SetInt(saveSlot + "Pickup." + id, 1);
            PlayerPrefs.Save();
        }
        // A new weapon or buff can be tried immediately; subsequent shots respect its delay.
        nextShot = Time.time;
        UpdateWeaponVisual();
        return true;
    }
    void ExpireTemporaryWeapon()
    {
        if (weapon == Weapon.Minigun && Time.time >= minigunUntil) weapon = Weapon.Normal;
    }
    public void Respawn()
    {
        StopAllCoroutines();
        Dying = false;
        Health = maxHealth;
        transform.position = spawn;
        body.linearVelocity = Vector2.zero;
        invincibleUntil = Time.time + 1.5f;
        jumpBuffer = groundedUntil = 0;
        usedAirJump = false;
        animationState = null;
        Animate("Idle");
    }
}
