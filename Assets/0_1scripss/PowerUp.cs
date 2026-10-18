using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum Kind { Apple, Medkit, Citrus, Feather, Resistance, Shirt, Rock, Chicken }
    public Kind kind;
    public string pickupId;
    public SpriteRenderer icon;
    [Min(0)] public float durationSeconds = 5;
    public static float DefaultDuration(Kind effect) => effect == Kind.Citrus || effect == Kind.Feather ? 0 : effect == Kind.Shirt ? 10 : effect == Kind.Chicken ? 15 : effect == Kind.Resistance ? 10 : 5;
    public static bool IsAvailableInStage(Kind effect, int stage) =>
        (stage < 7 || effect != Kind.Shirt) && (stage != 8 || effect != Kind.Chicken);
    string DurationLabel => durationSeconds.ToString("0.#") + " s";
    public string Title => kind == Kind.Apple ? "Apple: +10 vida maxima y +1 de daño permanentes" : kind == Kind.Medkit ? "Botiquin: cura 2 corazones" : kind == Kind.Citrus ? "Citrus: movimiento y disparo +50% hasta salir del nivel" : kind == Kind.Feather ? "Pluma: doble salto hasta salir del nivel" : kind == Kind.Resistance ? "Resistencia: inmunidad por " + DurationLabel : kind == Kind.Shirt ? "Camisa: minigun ilimitada por " + DurationLabel : kind == Kind.Rock ? "Lanzapiedra: 5 disparos, sin limite de tiempo" : "Chicken: AK por " + DurationLabel;
    bool collected;
    Vector3 origin;
    void Start()
    {
        origin = transform.position;
        var level = LevelOne.Instance;
        if (level != null && !IsAvailableInStage(kind, level.stageNumber))
        {
            gameObject.SetActive(false);
            return;
        }
        // Roll once per tutorial visit; respawning does not roll again.
        if (kind == Kind.Apple && level != null && level.stageNumber == 1 && level.layout == null && Random.value >= 0.1f)
        {
            gameObject.SetActive(false);
            return;
        }
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
