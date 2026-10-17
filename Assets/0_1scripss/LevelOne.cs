using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelOne : MonoBehaviour
{
    public static LevelOne Instance { get; private set; }
    public player player;
    public Camera followCamera;
    public bool Completed { get; private set; }
    public float endX = 51, checkpointX = 25;
    bool checkpointReached;
    float checkpointNoticeUntil;
    void Awake() { Instance = this; }
    void Update()
    {
        if (player == null) return;
        if (!checkpointReached && player.transform.position.x >= checkpointX && player.transform.position.y > -4)
        {
            checkpointReached = true; checkpointNoticeUntil = Time.time + 2.5f;
            player.SetCheckpoint(new Vector3(checkpointX, -2.8f, 0));
        }
        if (player.transform.position.x >= endX && player.transform.position.y > -4) Completed = true;
    }
    void LateUpdate()
    {
        if (followCamera == null || player == null) return;
        float halfWidth = Mathf.Min(30, followCamera.orthographicSize * followCamera.aspect);
        var desired = new Vector3(Mathf.Clamp(player.transform.position.x + 2, -11 + halfWidth, 56 - halfWidth), -0.8f, -10);
        followCamera.transform.position = Vector3.Lerp(followCamera.transform.position, desired, 1 - Mathf.Exp(-6 * Time.deltaTime));
    }
    void OnGUI()
    {
        if (player == null) return;
        float scale = Mathf.Clamp(Screen.width / 1100f, 0.65f, 2);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUI.Label(new Rect(20, 16, 150, 24), "Vida " + player.Health + " / " + player.maxHealth);
        GUI.Label(new Rect(Screen.width / scale - 90, 16, 80, 24), "Nivel 1");
        if (!Completed && Time.time < checkpointNoticeUntil) GUI.Label(new Rect(Screen.width / scale / 2 - 80, 16, 160, 24), "Checkpoint guardado");
        if (Completed)
        {
            float x = Screen.width / scale / 2 - 80, y = Screen.height / scale / 2 - 30;
            GUI.Label(new Rect(x, y, 180, 24), "Nivel completado");
            if (GUI.Button(new Rect(x, y + 32, 160, 30), "Volver a jugar")) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
