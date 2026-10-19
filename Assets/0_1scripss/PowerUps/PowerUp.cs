using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum Kind { Apple, Medkit, Citrus, Feather, Resistance, Shirt, Rock, Chicken }
    public Kind kind;
    public string pickupId;
    public SpriteRenderer icon;
    [Min(0)] public float durationSeconds = 5;
    public static float DefaultDuration(Kind effect)
    {
        switch (effect)
        {
            case Kind.Citrus:
            case Kind.Feather:
                return 0;
            case Kind.Shirt:
            case Kind.Resistance:
                return 10;
            case Kind.Chicken:
                return 15;
            default:
                return 5;
        }
    }

    public static bool IsAvailableInStage(Kind effect, int stage)
    {
        if (stage >= 7 && effect == Kind.Shirt) return false;
        if (stage == 8 && effect == Kind.Chicken) return false;
        return true;
    }
    string TextoDuracion => durationSeconds.ToString("0.#") + " s";
    public string Title
    {
        get
        {
            switch (kind)
            {
                case Kind.Apple: return "Apple: +10 vida maxima y +1 de daño permanentes";
                case Kind.Medkit: return "Botiquin: cura 2 corazones";
                case Kind.Citrus: return "Citrus: movimiento y disparo +50% hasta salir del nivel";
                case Kind.Feather: return "Pluma: doble salto hasta salir del nivel";
                case Kind.Resistance: return "Resistencia: inmunidad por " + TextoDuracion;
                case Kind.Shirt: return "Camisa: minigun ilimitada por " + TextoDuracion;
                case Kind.Rock: return "Lanzapiedra: 5 disparos, sin limite de tiempo";
                default: return "Chicken: AK por " + TextoDuracion;
            }
        }
    }
    bool recogido;
    void Start()
    {
        var nivel = LevelOne.Instance;
        if (nivel != null && !IsAvailableInStage(kind, nivel.stageNumber))
        {
            gameObject.SetActive(false);
            return;
        }
        if (kind == Kind.Apple && nivel != null && nivel.layout == null &&
            pickupId != "Level6.NecromancerReward" && Random.value >= 0.1f)
        {
            gameObject.SetActive(false);
            return;
        }
        var jugador = FindAnyObjectByType<player>();
        if (jugador != null && kind == Kind.Apple && jugador.HasCollected(pickupId)) gameObject.SetActive(false);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        var jugador = other.GetComponent<player>();
        if (jugador == null || jugador.Dying || recogido) return;
        recogido = true;
        if (jugador.ApplyPowerUp(kind, pickupId, durationSeconds) && LevelOne.Instance != null) LevelOne.Instance.Notify(Title);
        gameObject.SetActive(false);
    }
}
