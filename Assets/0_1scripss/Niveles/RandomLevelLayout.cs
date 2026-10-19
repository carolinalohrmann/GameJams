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
    public Sprite[] backgroundLayers;
    public float[] backgroundHeights;
    public float backgroundBaseY = -4.35f;
    public Sprite goalSprite;
    public GameObject pickupPrefab;
    public Sprite[] powerUpIcons;
    public GameObject[] meleeEnemies;
    public GameObject rangedEnemy;
    [Range(0, 3)] public int maximumPowerUps = 3;
    public int stageNumber = 1;
    public float bossArenaWidth;
    public Sprite[] mixedPlatformTops, mixedPlatformFills;
    public Sprite[] mixedBackgrounds;
    public float minimumPlatformWidth = 5.2f, maximumPlatformWidth = 7.1f;
    public float minimumGap = 0.55f, maximumGap = 1.2f, elevationChange = 0.35f;
    [Range(0, 1)] public float bonusPlatformChance = 0.65f;

    readonly List<Segment> segments = new List<Segment>();
    readonly List<Segment> ledges = new List<Segment>();
    readonly List<Rect> occupied = new List<Rect>();
    readonly List<PowerUp.Kind> availablePowerUps = new List<PowerUp.Kind>();
    Transform world;
    int seed;

    public IReadOnlyList<PowerUp.Kind> AvailablePowerUps => availablePowerUps;

    void Awake()
    {
        if (stageNumber == 1) { enabled = false; return; }
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
        BuildBackground();
        BuildPickups(random);
        BuildEnemies(random);
        BuildScenery(random);
        if (level.GetComponent<FinalCampaignStage>() == null) BuildGoal();
        BuildBoundary(level.leftEdge - 0.5f);
        BuildBoundary(level.rightEdge + 0.5f);
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
            float width = Next(random, minimumPlatformWidth, maximumPlatformWidth);
            float gap = Next(random, minimumGap, maximumGap);
            bool arena = bossArenaWidth > 0 && x + width >= right - bossArenaWidth;
            if (arena) { width = right - x; gap = 0; }
            surface = Mathf.Clamp(surface + Next(random, -elevationChange, elevationChange), -4.35f, -2.65f);
            if (arena) surface = -4;
            if (mixedPlatformTops != null && mixedPlatformTops.Length > 0)
            {
                int skin = index % mixedPlatformTops.Length;
                platformTop = mixedPlatformTops[skin]; platformFill = mixedPlatformFills[skin];
            }
            CreatePlatform("Route " + index, x, surface, width);
            segments.Add(new Segment(x, width, surface));

            if (!arena && index > 1 && index < 22 && random.NextDouble() < bonusPlatformChance)
            {
                float bonusWidth = Next(random, 2.3f, 3.3f);
                float bonusX = x + Next(random, 0.6f, Mathf.Max(0.7f, width - bonusWidth - 0.4f));
                float bonusY = surface + 1.8f;
                CreatePlatform("Bonus " + index, bonusX, bonusY, bonusWidth, true);
                ledges.Add(new Segment(bonusX, bonusWidth, bonusY));
            }

            x += width + gap;
            index++;
        }
    }

    void CreatePlatform(string name, float left, float surface, float width, bool floating = false)
    {
        var platform = new GameObject(name);
        platform.transform.SetParent(world, false);
        platform.transform.position = new Vector3(left + width / 2, surface - 0.25f, 0);
        var collider = platform.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(width, floating ? 0.5f : 1.5f);
        collider.offset = floating ? Vector2.zero : new Vector2(0, -0.5f);

        AddTile(platform.transform, "Top", platformTop, Vector3.zero,
            new Vector2(width, 0.5f), 1);
        if (floating) return;
        AddTile(platform.transform, "Soil", platformFill, new Vector3(0, -0.5f, 0),
            new Vector2(width, 0.5f), 0);
        AddTile(platform.transform, "Soil depth", platformFill, new Vector3(0, -1, 0),
            new Vector2(width, 0.5f), 0);
    }

    void AddTile(Transform parent, string name, Sprite sprite, Vector3 position, Vector2 size, int sortingOrder)
    {
        var visual = new GameObject(name).AddComponent<SpriteRenderer>();
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = position;
        visual.sprite = sprite;
        visual.color = Color.white;
        visual.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
        visual.sortingOrder = sortingOrder;
    }

    void BuildScenery(System.Random random)
    {
        if (scenerySprites == null || scenerySprites.Length == 0) return;
        foreach (var segment in segments)
        {
            if (random.NextDouble() > 0.7) continue;
            var sprite = scenerySprites[random.Next(scenerySprites.Length)];
            float scale = Next(random, 0.8f, 1.2f);
            float x = segment.left + segment.width * Next(random, 0.2f, 0.8f);
            float halfWidth = sprite.bounds.extents.x * scale;
            bool blocked = false;
            foreach (var enemy in world.GetComponentsInChildren<Enemy>())
                if (Mathf.Abs(x - enemy.transform.position.x) < halfWidth + enemy.patrolDistance + enemy.GetComponent<BoxCollider2D>().size.x / 2 + 0.7f) blocked = true;
            foreach (var pickup in world.GetComponentsInChildren<PowerUp>())
                if (Mathf.Abs(x - pickup.transform.position.x) < halfWidth + 0.8f) blocked = true;
            if (blocked || x - halfWidth < segment.left || x + halfWidth > segment.left + segment.width) continue;
            var visual = new GameObject("Scenery").AddComponent<SpriteRenderer>();
            visual.transform.SetParent(world, false);
            visual.transform.position = new Vector3(x, segment.surface - sprite.bounds.min.y * scale, 1);
            visual.transform.localScale = Vector3.one * scale;
            visual.sprite = sprite;
            visual.sortingOrder = -5;
        }
    }

    void BuildBackground()
    {
        if (mixedBackgrounds != null && mixedBackgrounds.Length > 0)
        {
            int count = Mathf.CeilToInt((level.rightEdge - level.leftEdge) / 20) + 2;
            for (int i = 0; i < count; i++)
            {
                var baseSprite = mixedBackgrounds[0];
                var backing = new GameObject("Glitch sky " + i).AddComponent<SpriteRenderer>();
                backing.transform.SetParent(world, false); backing.sprite = baseSprite; backing.sortingOrder = -35;
                backing.transform.position = new Vector3(level.leftEdge - 10 + i * 20, -6.4f, 0);
                backing.transform.localScale = new Vector3(20.1f / baseSprite.bounds.size.x, 13 / baseSprite.bounds.size.y, 1);
                var sprite = mixedBackgrounds[i % mixedBackgrounds.Length];
                var panel = new GameObject("Glitch backdrop " + i).AddComponent<SpriteRenderer>();
                panel.transform.SetParent(world, false); panel.sprite = sprite; panel.sortingOrder = -25;
                panel.transform.position = new Vector3(level.leftEdge - 10 + i * 20, -5, 0);
                panel.transform.localScale = new Vector3(20.1f / sprite.bounds.size.x, 13 / sprite.bounds.size.y, 1);
                panel.gameObject.AddComponent<GlitchDrift>();
            }
            return;
        }
        if (backgroundLayers == null) return;
        for (int layer = 0; layer < backgroundLayers.Length; layer++)
        {
            var sprite = backgroundLayers[layer];
            if (sprite == null) continue;
            float height = layer == 0 ? 16 : layer == 1 ? 7 : 9.5f;
            if (backgroundHeights != null && layer < backgroundHeights.Length && backgroundHeights[layer] > 0) height = backgroundHeights[layer];
            float scale = height / sprite.bounds.size.y;
            float width = sprite.bounds.size.x * scale;
            int count = Mathf.CeilToInt((level.rightEdge - level.leftEdge + 60) / width) + 2;
            for (int panel = 0; panel < count; panel++)
            {
                var visual = new GameObject("Forest background " + layer + " / " + panel).AddComponent<SpriteRenderer>();
                visual.transform.SetParent(world, false);
                visual.transform.position = new Vector3(level.leftEdge - 30 + panel * width, layer == 0 ? -6.4f : backgroundBaseY, 0);
                visual.transform.localScale = Vector3.one * scale;
                visual.sprite = sprite;
                visual.sortingOrder = -30 + layer;
                var parallax = visual.gameObject.AddComponent<JungleParallax>();
                parallax.factor = Mathf.Lerp(0.85f, 0.25f, layer / (float)Mathf.Max(1, backgroundLayers.Length - 1));
                parallax.span = count * width;
            }
        }
    }

    void BuildPickups(System.Random random)
    {
        var kinds = Array.FindAll((PowerUp.Kind[])Enum.GetValues(typeof(PowerUp.Kind)),
            kind => PowerUp.IsAvailableInStage(kind, stageNumber));
        for (int i = kinds.Length - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            (kinds[i], kinds[other]) = (kinds[other], kinds[i]);
        }

        int appleIndex = Array.IndexOf(kinds, PowerUp.Kind.Apple);
        int appleSlot = random.NextDouble() < 0.1 ? 0 : kinds.Length - 1;
        (kinds[appleSlot], kinds[appleIndex]) = (kinds[appleIndex], kinds[appleSlot]);
        int count = Mathf.Min(maximumPowerUps, kinds.Length, segments.Count);
        for (int i = 0; i < count; i++)
        {
            var kind = kinds[i];
            int segmentIndex = Mathf.Clamp((i + 1) * (segments.Count - 1) / (count + 1), 1, segments.Count - 2);
            Vector2 point = Vector2.zero;
            bool found = false;
            for (int attempt = 0; attempt < segments.Count - 2 && !found; attempt++)
                found = FindSpawn(segments[1 + (segmentIndex - 1 + attempt) % (segments.Count - 2)],
                    0.55f, 1, 0.5f, random, out point);
            if (!found) continue;
            availablePowerUps.Add(kind);
            var pickupObject = Instantiate(pickupPrefab, world);
            pickupObject.name = "PowerUp " + kind;
            pickupObject.transform.position = new Vector3(point.x, point.y, 0);

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
        int meleeIndex = random.Next(meleeEnemies.Length);
        for (int i = 2; i < segments.Count - 1; i++)
        {
            Segment segment = segments[i];
            if (segment.left > level.endX - 7) continue;
            int count = random.NextDouble() < 0.4 ? 2 : 1;
            for (int slot = 0; slot < count; slot++)
            {
                bool flyingEye = placed % 3 == 2;
                var prefab = flyingEye ? rangedEnemy : meleeEnemies[meleeIndex++ % meleeEnemies.Length];
                float patrol = flyingEye ? 0 : 0.75f;
                float bottom = flyingEye ? 0.85f : 0;
                Vector2 point;
                if (!FindSpawn(segment, 0.75f + patrol, 1.6f, bottom, random, out point))
                {
                    if (flyingEye) continue;
                    patrol = 0.25f;
                    if (!FindSpawn(segment, 0.75f + patrol, 1.6f, bottom, random, out point)) continue;
                }
                var enemy = Instantiate(prefab, world);
                enemy.name = flyingEye ? "Flying Eye" : prefab.name;
                enemy.transform.position = new Vector3(point.x,
                    point.y - 0.8f + enemy.GetComponent<BoxCollider2D>().size.y / 2, 0);
                enemy.GetComponent<Enemy>().patrolDistance = patrol;
                if (stageNumber == 7)
                {
                    var stronger = enemy.GetComponent<Enemy>(); stronger.health = 10;
                    stronger.contactDamage = stronger.projectileDamage = 2; stronger.moveSpeed *= 1.2f;
                }
                placed++;
            }
        }
    }

    bool FindSpawn(Segment route, float halfWidth, float height, float bottomOffset,
        System.Random random, out Vector2 point)
    {
        var supports = new List<Segment> { route };
        foreach (var ledge in ledges)
            if (ledge.left >= route.left && ledge.left + ledge.width <= route.left + route.width)
                supports.Add(ledge);
        foreach (var support in supports)
        {
            if (support.width < halfWidth * 2 + 0.2f) continue;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float x = Next(random, support.left + halfWidth + 0.1f,
                    support.left + support.width - halfWidth - 0.1f);
                var area = new Rect(x - halfWidth, support.surface + bottomOffset + 0.02f,
                    halfWidth * 2, height);
                bool blocked = false;
                foreach (var ledge in ledges)
                    if (area.Overlaps(new Rect(ledge.left - 0.08f, ledge.surface - 0.58f, ledge.width + 0.16f, 0.58f))) blocked = true;
                foreach (var other in occupied) if (area.Overlaps(other)) blocked = true;
                if (blocked) continue;
                occupied.Add(area);
                point = new Vector2(x, area.center.y);
                return true;
            }
        }
        point = Vector2.zero;
        return false;
    }

    void BuildGoal()
    {
        Segment landing = segments[segments.Count - 2];
        foreach (var segment in segments)
            if (level.endX >= segment.left + 1 && level.endX <= segment.left + segment.width - 1) { landing = segment; break; }
        level.endX = Mathf.Clamp(level.endX, landing.left + 1, landing.left + landing.width - 1);
        var goal = new GameObject("Level " + stageNumber + " goal").AddComponent<SpriteRenderer>();
        goal.transform.SetParent(world, false);
        goal.transform.position = new Vector3(level.endX, landing.surface + 0.01f, 0);
        goal.sprite = goalSprite;
        goal.sortingOrder = 2;
        goal.transform.localScale = Vector3.one;
    }

    bool HasPickup(Segment segment)
    {
        foreach (var power in world.GetComponentsInChildren<PowerUp>())
            if (power.transform.position.x >= segment.left && power.transform.position.x <= segment.left + segment.width) return true;
        return false;
    }
    bool HasEnemy(Segment segment)
    {
        foreach (var enemy in world.GetComponentsInChildren<Enemy>())
            if (enemy.transform.position.x >= segment.left && enemy.transform.position.x <= segment.left + segment.width) return true;
        return false;
    }
    void BuildBoundary(float x)
    {
        var boundary = new GameObject("Boundary"); boundary.transform.SetParent(world, false);
        boundary.transform.position = new Vector3(x, 0, 0);
        boundary.AddComponent<BoxCollider2D>().size = new Vector2(1, 20);
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
