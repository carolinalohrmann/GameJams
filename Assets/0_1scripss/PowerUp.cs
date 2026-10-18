using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum Kind { Apple, Medkit, Citrus, Feather, Resistance, Shirt, Rock, Chicken }
    public Kind kind;
    public string pickupId;
    public SpriteRenderer icon;
    [Min(0.1f)] public float durationSeconds = 5;
    public static float DefaultDuration(Kind effect) => effect == Kind.Citrus || effect == Kind.Resistance || effect == Kind.Chicken ? 10 : 5;
    string DurationLabel => durationSeconds.ToString("0.#") + " s";
    public string Title => kind == Kind.Apple ? "Apple: +10 vida maxima y +1 de daño permanentes" : kind == Kind.Medkit ? "Botiquin: cura 2 corazones" : kind == Kind.Citrus ? "Citrus: movimiento y disparo +50% por " + DurationLabel : kind == Kind.Feather ? "Pluma: doble salto por " + DurationLabel : kind == Kind.Resistance ? "Resistencia: inmunidad por " + DurationLabel : kind == Kind.Shirt ? "Camisa: minigun ilimitada por " + DurationLabel : kind == Kind.Rock ? "Lanzapiedra: 5 disparos, sin limite de tiempo" : "Chicken: AK por " + DurationLabel;
    bool collected;
    Vector3 origin;
    void Start()
    {
        origin = transform.position;
        var p = FindAnyObjectByType<player>();
        // Apples stay consumed on respawn, but become available again on a new scene visit.
        if (p != null && kind == Kind.Apple && p.HasCollected(pickupId)) gameObject.SetActive(false);
    }
    void Update() { transform.position = origin + Vector3.up * Mathf.Sin(Time.time * 2.5f) * 0.07f; }
    void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponent<player>();
        if (p == null || p.Dying || collected) return;
        collected = true;
        if (p.ApplyPowerUp(kind, pickupId, durationSeconds) && LevelOne.Instance != null) LevelOne.Instance.Notify(Title);
        gameObject.SetActive(false);
    }
}
