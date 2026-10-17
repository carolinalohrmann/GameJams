using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomLevelLayout : MonoBehaviour
{
    public LevelOne level;
    public Transform previousWorld;
    public Sprite platformTop;
    public Sprite platformFill;
    public Sprite[] scenerySprites;
    public Sprite goalSprite;
    public GameObject pickupPrefab;
    public Sprite[] powerUpIcons;
    public GameObject[] meleeEnemies;
    public GameObject rangedEnemy;
    [Range(0, 3)] public int maximumPowerUps = 3;
    public int stageNumber = 1;

    readonly List<Segment> segments = new List<Segment>();
    readonly List<PowerUp.Kind> availablePowerUps = new List<PowerUp.Kind>();
    Transform world;
    int seed;

    public IReadOnlyList<PowerUp.Kind> AvailablePowerUps => availablePowerUps;

    void Awake()
    {
        if (level == null) level = FindAnyObjectByType<LevelOne>();
        if (level == null || platformTop == null || platformFill == null || pickupPrefab == null ||
            powerUpIcons == null || powerUpIcons.Length != Enum.GetValues(typeof(PowerUp.Kind)).Length ||
            meleeEnemies == null || meleeEnemies.Length == 0 || rangedEnemy == null)
        {
            Debug.LogError("Random level configuration is incomplete.", this);
            enabled = false;
            return;
        }

        if (previousWorld != null)
        {
            previousWorld.gameObject.SetActive(false);
            Destroy(previousWorld.gameObject);
        }
        seed = Guid.NewGuid().GetHashCode();
        var random = new System.Random(seed);
        world = new GameObject("Procedural level " + stageNumber).transform;
        level.layout = this;
        level.stageNumber = stageNumber;
        BuildPlatforms(random);
        BuildScenery(random);
        BuildPickups(random);
        BuildEnemies(random);
        BuildGoal();
        Debug.Log("LEVEL_GENERATED stage=" + stageNumber + " seed=" + seed +
                  " platforms=" + segments.Count + " powerUps=" + availablePowerUps.Count);
    }

    void BuildPlatforms(System.Random random)
    {
        float x = level.leftEdge;
        float surface = -4;
        float right = level.rightEdge;
        int index = 0;

        while (x < right)
        {
            float width = Next(random, 5.2f, 7.1f);
            float gap = Next(random, 0.55f, 1.2f);
            surface = Mathf.Clamp(surface + Next(random, -0.35f, 0.35f), -4.35f, -2.65f);
            CreatePlatform("Route " + index, x, surface, width);
            segments.Add(new Segment(x, width, surface));

            if (index > 1 && index < 22 && random.NextDouble() < 0.28)
            {
                float bonusWidth = Next(random, 2.3f, 3.3f);
                float bonusX = x + Next(random, 0.6f, Mathf.Max(0.7f, width - bonusWidth - 0.4f));
                CreatePlatform("Bonus " + index, bonusX, Mathf.Min(surface + 1.35f, -1.1f), bonusWidth);
            }

            x += width + gap;
            index++;
        }
    }

    void CreatePlatform(string name, float left, float surface, float width)
    {
        var platform = new GameObject(name);
        platform.transform.SetParent(world, false);
        platform.transform.position = new Vector3(left + width / 2, surface - 0.25f, 0);
        platform.AddComponent<BoxCollider2D>().size = new Vector2(width, 0.5f);

        AddTile(platform.transform, "Top", platformTop, new Vector3(0, 0.25f, 0),
            new Vector2(width, 0.5f), 1);
        AddTile(platform.transform, "Soil", platformFill, new Vector3(0, -0.25f, 0),
            new Vector2(width, 0.5f), 0);
        AddTile(platform.transform, "Soil depth", platformFill, new Vector3(0, -0.75f, 0),
            new Vector2(width, 0.5f), 0);
    }

    void AddTile(Transform parent, string name, Sprite sprite, Vector3 position, Vector2 size, int sortingOrder)
    {
        var visual = new GameObject(name).AddComponent<SpriteRenderer>();
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = position;
        visual.sprite = sprite;
        visual.drawMode = SpriteDrawMode.Tiled;
        visual.size = size;
        visual.sortingOrder = sortingOrder;
    }

    void BuildScenery(System.Random random)
    {
        if (scenerySprites == null || scenerySprites.Length == 0) return;
        float x = level.leftEdge - 4;
        while (x < level.rightEdge + 5)
        {
            var sprite = scenerySprites[random.Next(scenerySprites.Length)];
            var visual = new GameObject("Scenery").AddComponent<SpriteRenderer>();
            visual.transform.SetParent(world, false);
            visual.transform.position = new Vector3(x, -3.75f, 1);
            visual.transform.localScale = Vector3.one * Next(random, 0.8f, 1.2f);
            visual.sprite = sprite;
            visual.sortingOrder = -5;
            x += Next(random, 5, 9);
        }
    }

    void BuildPickups(System.Random random)
    {
        var kinds = (PowerUp.Kind[])Enum.GetValues(typeof(PowerUp.Kind));
        for (int i = kinds.Length - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            (kinds[i], kinds[other]) = (kinds[other], kinds[i]);
        }

        int count = Mathf.Min(maximumPowerUps, kinds.Length, segments.Count);
        for (int i = 0; i < count; i++)
        {
            var kind = kinds[i];
            availablePowerUps.Add(kind);
            int segmentIndex = Mathf.Clamp((i + 1) * (segments.Count - 1) / (count + 1),
                1, segments.Count - 2);
            Segment segment = segments[segmentIndex];
            float x = segment.left + segment.width * Next(random, 0.3f, 0.7f);
            var pickupObject = Instantiate(pickupPrefab, world);
            pickupObject.name = "PowerUp " + kind;
            pickupObject.transform.position = new Vector3(x, segment.surface + 1, 0);

            var powerUp = pickupObject.GetComponent<PowerUp>();
            powerUp.kind = kind;
            powerUp.pickupId = "Level" + stageNumber + "." + kind;
            powerUp.durationSeconds = PowerUp.DefaultDuration(kind);
            powerUp.icon.sprite = powerUpIcons[(int)kind];
        }
    }

    void BuildEnemies(System.Random random)
    {
        int placed = 0;
        bool hasFlyingEye = false;
        for (int i = 2; i < segments.Count - 1; i++)
        {
            if (i % 2 != 0 || random.NextDouble() > 0.78) continue;
            Segment segment = segments[i];
            float x = segment.left + segment.width * 0.5f;
            if (x > level.endX - 7) continue;

            bool flyingEye = stageNumber == 2 || random.NextDouble() < 0.3;
            var prefab = flyingEye ? rangedEnemy : meleeEnemies[random.Next(meleeEnemies.Length)];
            var enemy = Instantiate(prefab, world);
            enemy.name = flyingEye ? "Flying Eye" : prefab.name;
            enemy.transform.position = new Vector3(x, segment.surface + (flyingEye ? 1.3f : 0.5f), 0);
            var behaviour = enemy.GetComponent<Enemy>();
            behaviour.patrolDistance = Mathf.Min(1.5f, segment.width * 0.25f);
            hasFlyingEye |= flyingEye;
            placed++;
        }

        if (!hasFlyingEye && segments.Count > 5)
        {
            Segment segment = segments[segments.Count / 2];
            var enemy = Instantiate(rangedEnemy, world);
            enemy.name = "Flying Eye";
            enemy.transform.position = new Vector3(segment.left + segment.width / 2, segment.surface + 1.3f, 0);
        }

        if (placed == 0 && segments.Count > 4)
        {
            Segment segment = segments[3];
            var enemy = Instantiate(meleeEnemies[0], world);
            enemy.transform.position = new Vector3(segment.left + segment.width / 2, segment.surface + 0.5f, 0);
        }
    }

    void BuildGoal()
    {
        var goal = new GameObject("Level " + stageNumber + " goal").AddComponent<SpriteRenderer>();
        goal.transform.SetParent(world, false);
        goal.transform.position = new Vector3(level.endX + 1, -3.4f, 0);
        goal.sprite = goalSprite;
        goal.sortingOrder = 2;
        goal.transform.localScale = Vector3.one * 1.5f;
    }

    static float Next(System.Random random, float minimum, float maximum)
    {
        return minimum + (float)random.NextDouble() * (maximum - minimum);
    }

    struct Segment
    {
        public readonly float left;
        public readonly float width;
        public readonly float surface;

        public Segment(float left, float width, float surface)
        {
            this.left = left;
            this.width = width;
            this.surface = surface;
        }
    }
}
