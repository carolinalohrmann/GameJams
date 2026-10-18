using UnityEngine;

public class bullet : MonoBehaviour
{
    public Sprite[] normalFrames, rapidFrames;
    Sprite[] frames;
    SpriteRenderer visual;
    float started;
    public void UsePlayerEffect(bool rapid)
    {
        frames = rapid ? rapidFrames : normalFrames;
        if (frames == null || frames.Length == 0) return;
        visual = GetComponent<SpriteRenderer>();
        visual.color = Color.white;
        visual.sprite = frames[0];
        started = Time.time;
        transform.localScale = Vector3.one * (rapid ? 1.4f : 2);
        GetComponent<BoxCollider2D>().size = new Vector2(0.12f, 0.08f);
    }
    void Update()
    {
        if (frames != null && frames.Length > 0)
            visual.sprite = frames[Mathf.FloorToInt((Time.time - started) * 10) % frames.Length];
    }
    bool hostile, spent;
    Collider2D owner;
    int damage = 1;
    bool lethal;
    public void SetLethal() { lethal = true; }
    public void Launch(Vector2 direction, Collider2D source, bool enemyShot, float speed, int hitDamage = 1)
    {
        hostile = enemyShot;
        damage = hitDamage;
        owner = source;
        if (hostile) GetComponent<SpriteRenderer>().color = new Color(0.48f, 0.27f, 0.12f);
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), source);
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
        Destroy(gameObject, hostile ? 3 : 1);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (spent || other == owner || other.isTrigger || other.GetComponent<bullet>() != null) return;
        var p = other.GetComponent<player>();
        var enemy = other.GetComponent<Enemy>();
        if ((hostile && enemy != null) || (!hostile && p != null)) return;
        spent = true;
        if (hostile && p != null) { if (lethal) p.KillInstantly(); else p.Damage(damage); }
        if (!hostile && enemy != null) enemy.Damage(damage);
        Destroy(gameObject);
    }
}
