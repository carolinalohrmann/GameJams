using UnityEngine;

public class JungleParallax : MonoBehaviour
{
    public float factor = 0.25f, span = 140;
    Vector3 origin;
    Vector3 cameraOrigin;
    Camera view;
    void Start() { origin = transform.position; view = Camera.main; if (view != null) cameraOrigin = view.transform.position; }
    void LateUpdate()
    {
        if (view == null) return;
        var position = origin + Vector3.right * ((view.transform.position.x - cameraOrigin.x) * factor);
        if (position.x < view.transform.position.x - span / 2) position.x += span;
        if (position.x > view.transform.position.x + span / 2) position.x -= span;
        transform.position = position;
    }
}
