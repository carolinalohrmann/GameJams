using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildLevelOne
{
    static Sprite square;
    static Material material;
    static Transform world;

    [MenuItem("GameJams/Crear nivel 1")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene("Assets/0_0Scenes/SampleScene.unity");
        var original = GameObject.Find("Player");
        square = original.GetComponent<SpriteRenderer>().sprite;
        material = original.GetComponent<SpriteRenderer>().sharedMaterial;
        if (square == null) throw new Exception("No se encuentra el sprite cuadrado del Player.");
        UnityEngine.Object.DestroyImmediate(GameObject.Find("floor"));
        UnityEngine.Object.DestroyImmediate(GameObject.Find("powerrups"));
        world = new GameObject("Nivel 1 - plataformas y decoracion").transform;
        var bulletObject = PrefabUtility.LoadPrefabContents("Assets/0_2prefafs/bullets.prefab");
        bulletObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        bulletObject.transform.localScale = new Vector3(0.24f, 0.12f, 1);
        var renderer = bulletObject.GetComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 5;
        var bc = bulletObject.GetComponent<BoxCollider2D>() ?? bulletObject.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        var rb = bulletObject.GetComponent<Rigidbody2D>() ?? bulletObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (bulletObject.GetComponent<bullet>() == null) bulletObject.AddComponent<bullet>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(bulletObject, "Assets/0_2prefafs/bullets.prefab");
        PrefabUtility.UnloadPrefabContents(bulletObject);
        var projectile = prefab.GetComponent<bullet>();
        original.transform.position = new Vector3(-8, -2.8f, 0);
        original.transform.localScale = new Vector3(0.65f, 1.2f, 1);
        original.GetComponent<SpriteRenderer>().color = Color.white;
        original.GetComponent<SpriteRenderer>().sortingOrder = 3;
        var body = original.GetComponent<Rigidbody2D>();
        body.gravityScale = 2.5f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var p = original.AddComponent<player>();
        p.shootPoint = original.transform.Find("shootp");
        p.shootPoint.localPosition = new Vector3(0.8f, 0.05f, 0);
        p.projectile = projectile;

        // Broad safe starting area, then two gaps of only 1.5 units.
        Block("Suelo inicio", -2, -4.5f, 18, 1, true);
        Block("Suelo zona melee", 14, -4.5f, 11, 1, true);
        Block("Suelo intermedio", 29, -4.5f, 16, 1, true);
        Block("Suelo meta", 47.25f, -4.5f, 17.5f, 1, true);
        Block("Escalon tutorial", -2, -3.7f, 2.5f, 0.6f, true);
        Block("Plataforma tutorial", 2, -2.7f, 3, 0.5f, true);
        Block("Plataforma puente", 7.75f, -2.4f, 3, 0.5f, true);
        Block("Plataforma descanso", 16, -2.5f, 3.5f, 0.5f, true);
        Block("Plataforma intermedia", 20.25f, -2.3f, 3, 0.5f, true);
        Block("Plataforma final A", 32, -2.8f, 3, 0.5f, true);
        Block("Plataforma final B", 37.75f, -2.3f, 3.5f, 0.5f, true);
        Block("Plataforma final C", 43, -2.7f, 3, 0.5f, true);
        Block("Limite izquierdo", -11.5f, 1, 1, 12, true).GetComponent<SpriteRenderer>().enabled = false;
        Block("Limite derecho", 56.5f, 1, 1, 12, true).GetComponent<SpriteRenderer>().enabled = false;

        EnemyAt("Enemigo melee 1", 12, false, projectile);
        EnemyAt("Enemigo tirador 1", 28, true, projectile);
        EnemyAt("Enemigo melee 2", 34, false, projectile);
        EnemyAt("Enemigo tirador 2", 47, true, projectile);
        Flag("Meta", 51);

        var camera = Camera.main;
        camera.orthographicSize = 5.2f;
        camera.backgroundColor = Color.gray;
        camera.transform.position = new Vector3(-2, -0.8f, -10);
        var level = new GameObject("Nivel 1 - reglas y HUD").AddComponent<LevelOne>();
        level.player = p;
        level.followCamera = camera;
        EditorSceneManager.SaveScene(scene, "Assets/0_0Scenes/Level1.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/0_0Scenes/Level1.unity", true) };
        AssetDatabase.SaveAssets();
        Debug.Log("LEVEL_ONE_BUILD_OK");
    }
    static GameObject Block(string name, float x, float y, float width, float height, bool solid)
    {
        var go = new GameObject(name);
        go.transform.SetParent(world);
        go.transform.position = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(width, height, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.sharedMaterial = material;
        sr.color = Color.white;
        if (solid) go.AddComponent<BoxCollider2D>();
        return go;
    }
    static void EnemyAt(string name, float x, bool ranged, bullet projectile)
    {
        var go = Block(name, x, -3.45f, 1, 1, true);
        go.GetComponent<SpriteRenderer>().color = Color.white;
        go.GetComponent<SpriteRenderer>().sortingOrder = 3;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var enemy = go.AddComponent<Enemy>();
        enemy.ranged = ranged;
        enemy.projectile = projectile;
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/0_2prefafs/" + (ranged ? "EnemyRanged" : "EnemyMelee") + ".prefab");
    }
    static void Flag(string name, float x)
    {
        var pole = Block(name + " poste", x, -2.6f, 0.12f, 2.8f, false);
        pole.GetComponent<SpriteRenderer>().color = Color.white;
        var flag = Block(name + " bandera", x + 0.55f, -1.5f, 1, 0.6f, false);
        flag.GetComponent<SpriteRenderer>().color = Color.white;
        flag.GetComponent<SpriteRenderer>().sortingOrder = 2;
    }
}
