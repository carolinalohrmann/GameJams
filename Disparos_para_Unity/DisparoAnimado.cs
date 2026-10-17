using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DisparoAnimado : MonoBehaviour
{
    public Sprite[] cuadros;
    [Min(0.1f)] public float fps = 10f;
    public bool reproducirAlActivar;
    public bool repetir;
    private SpriteRenderer imagen;
    private float tiempo;
    private bool activo;

    private void Awake() { imagen = GetComponent<SpriteRenderer>(); }
    private void OnEnable()
    {
        if (reproducirAlActivar) Reproducir();
        else if (imagen != null) imagen.sprite = null;
    }
    private void OnDisable() { activo = false; }
    public void Reproducir()
    {
        if (imagen == null) imagen = GetComponent<SpriteRenderer>();
        tiempo = 0f;
        activo = cuadros != null && cuadros.Length > 0;
        imagen.sprite = activo ? cuadros[0] : null;
    }
    private void Update()
    {
        if (!activo) return;
        tiempo += Time.deltaTime;
        int indice = Mathf.FloorToInt(tiempo * Mathf.Max(0.1f, fps));
        if (indice >= cuadros.Length)
        {
            if (repetir) indice %= cuadros.Length;
            else { activo = false; imagen.sprite = null; return; }
        }
        imagen.sprite = cuadros[indice];
    }
}
