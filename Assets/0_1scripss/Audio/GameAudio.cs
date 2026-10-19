using System.Collections.Generic;
using UnityEngine;

public class GameAudio : MonoBehaviour
{
    const float EffectVolumeMultiplier = 0.75f;
    public enum Sound { Pickup, WeaponPickup, Chicken, RockPickup, RockThrow, AKShot, HunterShot, HunterBreath, NecromancerBreath, Magic, MeteorImpact, PlayerHurt, PlayerDeath, Footsteps, SkeletonDeath, EyeDeath, GoblinDeath, Menu, EarlyMusic, LateMusic, FinalMusic, Melee, NecromancerMusic, PlayerJump, NormalShot }
    public static GameAudio Instance { get; private set; }
    public static readonly string[] FileNames = {
        "powerups", "akymachinegun", "pollo", "agarrar el power up catapulta", "lanzar piedra", "ak47disparo", "disparos cazador", "enemigo", "necromancer", "atakes necromancer", "exposion al caer el merorito", "danio al player", "muerte player", "pasitosplayer", "esqeletomuerte", "eyedead", "goblindeadd", "menu", "sonido de fondo", "musica fondo desl nivel 5", "sonido fondo nivel final usar hasta el segundo 40", "golpes de esesapda de los enemigo a mele", "musica necromancer", "salto player", "disparo player"
    };
    readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
    readonly List<AudioSource> gameplaySources = new List<AudioSource>();
    AudioSource effects, menu, steps, breath, playerGun, hunterGun, magic;
    float playerGunUntil, hunterGunUntil, magicUntil;
    bool paused;
    LevelOne level;
    public AudioSource Music { get; private set; }
    public int CueCount { get; private set; }
    public AudioClip LastEffectClip { get; private set; }
    public AudioClip Clip(Sound sound) => clips.TryGetValue(sound, out var clip) ? clip : null;
    void Awake()
    {
        Instance = this; level = GetComponent<LevelOne>();
        for (int i = 0; i < FileNames.Length; i++) clips[(Sound)i] = Resources.Load<AudioClip>("GameAudio/" + FileNames[i]);
        Music = Source("Musica", 0.3f, true, true);
        effects = Source("Efectos", 0.65f, false);
        steps = Source("Pasos", 0.22f, true);
        breath = Source("Respiracion del jefe", 0.28f, true);
        playerGun = Source("Arma del jugador", 0.4f, true);
        hunterGun = Source("Arma del Cazador", 0.4f, true);
        magic = Source("Magia", 0.4f, false);
        menu = Source("Menu", 0.3f, false); gameplaySources.Remove(menu);
    }
    AudioSource Source(string name, float volume, bool loop, bool music = false)
    {
        var root = new GameObject(name); root.transform.SetParent(transform, false);
        var source = root.AddComponent<AudioSource>(); source.playOnAwake = false;
        source.spatialBlend = 0; source.volume = volume * (music ? 1 : EffectVolumeMultiplier); source.loop = loop;
        gameplaySources.Add(source); return source;
    }
    void Start()
    {
        Music.clip = Clip(level.stageNumber == 8 ? Sound.FinalMusic : level.stageNumber == 6 || level.stageNumber == 7 ? Sound.NecromancerMusic : level.stageNumber == 5 ? Sound.LateMusic : Sound.EarlyMusic);
        if (Music.clip != null) Music.Play();
    }
    void Update()
    {
        bool help = TutorialCanvasUI.BlocksGameplay || level.Completed;
        if (help != paused)
        {
            paused = help;
            foreach (var source in gameplaySources) { if (paused) source.Pause(); else source.UnPause(); }
            if (paused) { menu.clip = Clip(Sound.Menu); if (menu.clip != null) menu.Play(); }
            else menu.Stop();
        }
        if (paused) return;
        if (Time.time > playerGunUntil || level.player.Dying || level.Completed) playerGun.Stop();
        if (Time.time > hunterGunUntil || level.player.Dying || level.Completed) hunterGun.Stop();
        if (Time.time > magicUntil) magic.Stop();
        if (level.player.Dying || level.Completed) breath.Stop();
        bool walking = !level.player.Dying && !level.Completed && level.player.Grounded && Mathf.Abs(level.player.GetComponent<Rigidbody2D>().linearVelocity.x) > 0.2f;
        if (walking) Loop(steps, Clip(Sound.Footsteps)); else steps.Stop();
    }
    static void Loop(AudioSource source, AudioClip clip)
    {
        if (clip == null) return;
        source.loop = true;
        if (source.clip != clip) { source.Stop(); source.clip = clip; }
        if (!source.isPlaying) source.Play();
    }
    public void Cue(Sound sound)
    {
        PlayClip(Clip(sound));
    }
    void PlayClip(AudioClip clip)
    {
        if (clip == null || paused) return;
        LastEffectClip = clip;
        effects.PlayOneShot(clip); CueCount++;
    }
    public void Pickup(PowerUp.Kind kind)
    {
        Cue(kind == PowerUp.Kind.Chicken ? Sound.Chicken : kind == PowerUp.Kind.Shirt ? Sound.WeaponPickup : kind == PowerUp.Kind.Rock ? Sound.RockPickup : Sound.Pickup);
    }
    public void PlayerShot(bool rock, bool minigun, bool ak)
    {
        if (rock) { Cue(Sound.RockThrow); return; }
        if (paused) return;
        if (!minigun && !ak)
        {
            var clip = Clip(Sound.NormalShot); if (clip == null) return;
            playerGun.Stop(); playerGun.loop = false; playerGun.clip = clip; playerGun.Play();
            playerGunUntil = Time.time + clip.length; CueCount++; return;
        }
        Loop(playerGun, Clip(minigun ? Sound.HunterShot : Sound.AKShot));
        playerGunUntil = Time.time + 0.18f; CueCount++;
    }
    public void HunterShot()
    {
        if (paused) return;
        Loop(hunterGun, Clip(Sound.HunterShot)); hunterGunUntil = Time.time + 0.16f; CueCount++;
    }
    public void BossBreathing(bool hunter) { if (!paused) Loop(breath, Clip(hunter ? Sound.HunterBreath : Sound.NecromancerBreath)); }
    public void StopBoss() { breath.Stop(); hunterGun.Stop(); magic.Stop(); }
    public void Magic()
    {
        if (paused || Clip(Sound.Magic) == null) return;
        magic.Stop(); magic.clip = Clip(Sound.Magic); magic.Play(); magicUntil = Time.time + 1.3f; CueCount++;
    }
    public void EnemyDeath(Enemy enemy)
    {
        if (enemy.GetComponent<BossController>() != null) return;
        if (enemy.deathSound != null) { PlayClip(enemy.deathSound); return; }
        string kind = enemy.animator != null && enemy.animator.runtimeAnimatorController != null ? enemy.animator.runtimeAnimatorController.name : enemy.name;
        if (enemy.flying && Clip(Sound.EyeDeath) != null) Cue(Sound.EyeDeath);
        else if ((kind.Contains("Skeleton") || enemy.GetComponent<BossSummon>() != null) && Clip(Sound.SkeletonDeath) != null) Cue(Sound.SkeletonDeath);
        else Cue(Sound.GoblinDeath);
    }
    public void EnemyAttack(Enemy enemy)
    {
        if (enemy.GetComponent<BossController>() != null) return;
        PlayClip(enemy.attackSound != null ? enemy.attackSound : Clip(Sound.Melee));
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
}
