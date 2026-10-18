using UnityEngine;

public class GlitchDrift : MonoBehaviour
{
    Vector3 anchor;
    float next, restore;
    void Start() { anchor = transform.localPosition; next = Time.time + Random.Range(1.2f, 3); }
    void Update()
    {
        if (Time.time >= next)
        {
            transform.localPosition = anchor + new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(-0.04f, 0.04f));
            restore = Time.time + 0.12f; next = Time.time + Random.Range(1.2f, 3);
        }
        else if (Time.time >= restore) transform.localPosition = anchor;
    }
}
