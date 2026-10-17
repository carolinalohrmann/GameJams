using UnityEngine;
public class bullet : MonoBehaviour
{
    bool hostile, spent;
    Collider2D owner;
    public void Launch(Vector2 direction, Collider2D source, bool enemyShot, float speed)
    {
        hostile = enemyShot; owner = source;
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), source);
        GetComponent<Rigidbody2D>().linearVelocity = direction.normalized * speed;
        Destroy(gameObject, 3);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (spent || other == owner || other.isTrigger || other.GetComponent<bullet>() != null) return;
        var p = other.GetComponent<player>(); var enemy = other.GetComponent<Enemy>();
        if ((hostile && enemy != null) || (!hostile && p != null)) return;
        spent = true;
        if (hostile && p != null) p.Damage();
        if (!hostile && enemy != null) enemy.Damage();
        Destroy(gameObject);
    }
}
