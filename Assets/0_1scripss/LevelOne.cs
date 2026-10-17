using UnityEngine;

public class LevelOne : MonoBehaviour
{
    public static LevelOne Instance { get; private set; }
    public player player;
    public Camera followCamera;
    public bool Completed { get; private set; }
    public float endX = 116;
    public float leftEdge = -11, rightEdge = 124;
    string notice;
    float noticeUntil;
    public string CurrentNotice => Time.time < noticeUntil ? notice : null;
    public void Notify(string text) { notice = text; noticeUntil = Time.time + 4; }
    void Awake() { Instance = this; }
    void Update()
    {
        if (player == null || player.Dying) return;
        if (player.transform.position.x >= endX && player.transform.position.y > -4) Completed = true;
    }
    void LateUpdate()
    {
        if (followCamera == null || player == null) return;
        float halfWidth = Mathf.Min(30, followCamera.orthographicSize * followCamera.aspect);
        var desired = new Vector3(Mathf.Clamp(player.transform.position.x + 2, leftEdge + halfWidth, rightEdge - halfWidth), -0.8f, -10);
        followCamera.transform.position = Vector3.Lerp(followCamera.transform.position, desired, 1 - Mathf.Exp(-6 * Time.deltaTime));
    }
}
