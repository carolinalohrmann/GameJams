using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CampaignPortal : MonoBehaviour
{
    public FinalCampaignStage stage;
    public string destination, title;
    public bool RequiresInteraction => stage.Level.stageNumber == 6;
    void Update()
    {
        var p = stage.Level.player;
        if (!CanEnter(p) || TutorialCanvasUI.BlocksGameplay) return;
        if (!RequiresInteraction) { Enter(p); return; }
        stage.Level.Notify("E · " + title);
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Enter(p);
    }
    public bool CanEnter(player p)
    {
        return stage.ExitsUnlocked && !stage.Level.Completed && !p.Dying && Mathf.Abs(p.transform.position.x - transform.position.x) < 1.6f && Mathf.Abs(p.transform.position.y - transform.position.y) < 2;
    }
    public bool Enter(player p)
    {
        if (!CanEnter(p)) return false;
        if (string.IsNullOrEmpty(destination)) stage.NormalEnding();
        else SceneManager.LoadScene(destination);
        return true;
    }
}
