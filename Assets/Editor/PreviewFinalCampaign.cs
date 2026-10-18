using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PreviewFinalCampaign
{
    static int view, frame;
    static RenderTexture target;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly string[] Names = { "level6-route", "level6-boss", "level6-exits", "level7-glitch", "level7-meteor", "level7-tower", "level8-hunter", "secret-ending" };
    static PreviewFinalCampaign()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("FinalCampaignPreview", false)) return;
            view = frame = 0; target = new RenderTexture(1280, 720, 24); target.Create();
            Directory.CreateDirectory("Logs/Previews"); EditorApplication.update += Frame;
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/0_0Scenes/Level6.unity");
        UnityEngine.Object.FindAnyObjectByType<player>().persistUpgrades = false;
        SessionState.SetBool("FinalCampaignPreview", true); EditorApplication.EnterPlaymode();
    }
    static void Frame()
    {
        try
        {
            var stage = UnityEngine.Object.FindAnyObjectByType<FinalCampaignStage>();
            var ui = TutorialCanvasUI.Instance;
            if (stage == null || stage.Boss == null || ui == null) return;
            var level = stage.Level; var camera = level.followCamera;
            ui.SetGuide(false); level.enabled = false; stage.enabled = false; level.player.enabled = false;
            level.player.persistUpgrades = false;
            foreach (var power in UnityEngine.Object.FindObjectsByType<PowerUp>()) power.GetComponent<Collider2D>().enabled = false;
            var body = level.player.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.linearVelocity = Vector2.zero;
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>()) { enemy.enabled = false; enemy.GetComponent<Collider2D>().enabled = false; }
            var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            camera.aspect = 16f / 9; camera.targetTexture = target;
            bool route = view == 0 || view == 3;
            float x = route ? 29 : level.stageNumber == 8 ? 21 : level.rightEdge - camera.orthographicSize * camera.aspect;
            float playerX = route ? x - 3 : level.stageNumber == 8 ? 18 : stage.arenaLeft + 3;
            float surface = -4;
            if (route)
            {
                foreach (var hit in Physics2D.RaycastAll(new Vector2(playerX, 8), Vector2.down, 20))
                    if (!hit.collider.isTrigger && hit.collider.attachedRigidbody == null) { surface = hit.point.y; break; }
            }
            level.player.transform.position = new Vector3(playerX, surface + 0.6f, 0);
            camera.transform.position = new Vector3(x, -0.8f, -10);
            if (frame == 0)
            {
                if (view == 1 || view == 4 || view == 6)
                {
                    stage.Boss.Activate(); stage.Boss.StopAllCoroutines();
                    stage.Boss.GetComponent<Enemy>().animator.Play("Attack", 0, 0.3f);
                    typeof(BossController).GetMethod("Shoot", Private).Invoke(stage.Boss, new object[] { Vector2.left, 0f, 1 });
                }
                if (view == 4 || view == 6)
                {
                    var meteor = CampaignHazard.CreateMeteor(stage, playerX + 3, -4);
                    typeof(CampaignHazard).GetField("age", Private).SetValue(meteor, 1.3f);
                }
                if (view == 2 || view == 5 || view == 7)
                { stage.Boss.gameObject.SetActive(false); stage.BossDefeated(); }
            }
            if (view == 4 || view == 6)
                foreach (var hazard in UnityEngine.Object.FindObjectsByType<CampaignHazard>())
                    if (hazard.meteor && hazard.Armed) { hazard.transform.position = new Vector3(playerX + 3, 1.8f, 0); typeof(CampaignHazard).GetField("speed", Private).SetValue(hazard, 0f); }
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            if (++frame < 22) return;
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes("Logs/Previews/" + Names[view] + ".png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture); RenderTexture.active = null;
            frame = 0; view++;
            if (view == 3) SceneManager.LoadScene("Level7");
            if (view == 6) SceneManager.LoadScene("Level8");
            if (view < Names.Length) return;
            Debug.Log("FINAL_CAMPAIGN_PREVIEWS_OK"); SessionState.SetBool("FinalCampaignPreview", false); EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); SessionState.SetBool("FinalCampaignPreview", false); EditorApplication.Exit(1); }
    }
}
