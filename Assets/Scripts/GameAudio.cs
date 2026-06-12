using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    private AudioSource _music;
    private AudioSource _sfx;
    private bool _menuClickActive;
    private int _clickPlayedFrame = -1;

    private AudioClip _clipMenuTheme;
    private AudioClip _clipMatchTheme0;   // Shattered Reaches
    private AudioClip _clipMatchTheme1;   // Ashen Stonefields
    private AudioClip _clipMatchTheme2;   // Damnation's Maw

    private AudioClip _clipMagicBurst;
    private AudioClip _clipRangedEnemyShot;
    private AudioClip _clipSwordCut;
    private AudioClip _clipSpearThrust;
    private AudioClip _clipItemPickup;
    private AudioClip _clipPotionDrink;
    private AudioClip _clipGiantAttackStomp;
    private AudioClip _clipGiantFootsteps;
    private AudioClip _clipMenuButtonClick;
    private AudioClip _clipExplosionSpecialAttack;
    private AudioClip _clipSwordThrow;
    private AudioClip _clipSpearThrow;
    private AudioClip _clipMinionSpawn;
    private AudioClip _clipGravityBomb;
    private AudioClip _clipExitPortal;
    private AudioClip _clipGenericPower;
    private AudioClip _clipFireTrail;

    private void Update()
    {
        if (_menuClickActive && Input.GetMouseButtonDown(0))
            PlayMenuButtonClick();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadClips();
        SetupSources();
    }

    private void LoadClips()
    {
        _clipMenuTheme      = Resources.Load<AudioClip>("Sounds/giri_is-item-check-video-game-theme-141774");
        _clipMatchTheme0    = Resources.Load<AudioClip>("Sounds/musicinmedia-8bit-theme-loop-chiptune-symphony-387749");
        _clipMatchTheme1    = Resources.Load<AudioClip>("Sounds/melodyayresgriffiths-over-the-mountain-chiptune-8-bit-rpg-japan-80s-c64-sid-138354");
        _clipMatchTheme2    = Resources.Load<AudioClip>("Sounds/u_w2fp0sqa7t-8bit-boi-146470");
        _clipMagicBurst     = Resources.Load<AudioClip>("Sounds/humordome-magic-burst-452852");
        _clipRangedEnemyShot         = Resources.Load<AudioClip>("Sounds/rescopicsound-elemental-magic-spell-impact-outgoing-228342");
        _clipSwordCut                = Resources.Load<AudioClip>("Sounds/ribhavagrawal-sword-cut-type-1-230552");
        _clipSpearThrust             = Resources.Load<AudioClip>("Sounds/yodguard-spear_thrust-1-382402");
        _clipItemPickup              = Resources.Load<AudioClip>("Sounds/yodguard-item-pickup-1-540174");
        _clipPotionDrink             = Resources.Load<AudioClip>("Sounds/yodguard-potion-drink-3-540167");
        _clipGiantAttackStomp        = Resources.Load<AudioClip>("Sounds/giant-attack-stomp");
        _clipGiantFootsteps          = Resources.Load<AudioClip>("Sounds/thestoryrug-sfx-giant-footsteps-206272");
        _clipMenuButtonClick         = Resources.Load<AudioClip>("Sounds/menu-button-click");
        _clipExplosionSpecialAttack  = Resources.Load<AudioClip>("Sounds/explosion-special-attack");
        _clipSwordThrow              = Resources.Load<AudioClip>("Sounds/sword-throw");
        _clipSpearThrow              = Resources.Load<AudioClip>("Sounds/spear-throw");
        _clipMinionSpawn             = Resources.Load<AudioClip>("Sounds/minion-spawn");
        _clipGravityBomb             = Resources.Load<AudioClip>("Sounds/gravity-bomb");
        _clipExitPortal              = Resources.Load<AudioClip>("Sounds/exit-portal");
        _clipGenericPower            = Resources.Load<AudioClip>("Sounds/generic-power");
        _clipFireTrail               = Resources.Load<AudioClip>("Sounds/fire-trail");
    }

    private void SetupSources()
    {
        _music = gameObject.AddComponent<AudioSource>();
        _music.loop = true;
        _music.playOnAwake = false;
        _music.volume = 0.4f;

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.loop = false;
        _sfx.playOnAwake = false;
        _sfx.volume = 0.8f;
    }

    // Optional: lets GameBootstrap override any clip with an inspector-assigned asset.
    public static void ConfigureSoundEffects(
        AudioClip giantAttackStomp,
        AudioClip menuButtonClick,
        AudioClip explosionSpecialAttack,
        AudioClip swordThrow,
        AudioClip spearThrow,
        AudioClip minionSpawn,
        AudioClip gravityBomb,
        AudioClip exitPortal,
        AudioClip genericPower,
        AudioClip fireTrail)
    {
        EnsureInstance();
        if (Instance == null) return;
        if (giantAttackStomp      != null) Instance._clipGiantAttackStomp       = giantAttackStomp;
        if (menuButtonClick       != null) Instance._clipMenuButtonClick        = menuButtonClick;
        if (explosionSpecialAttack != null) Instance._clipExplosionSpecialAttack = explosionSpecialAttack;
        if (swordThrow            != null) Instance._clipSwordThrow             = swordThrow;
        if (spearThrow            != null) Instance._clipSpearThrow             = spearThrow;
        if (minionSpawn           != null) Instance._clipMinionSpawn            = minionSpawn;
        if (gravityBomb           != null) Instance._clipGravityBomb            = gravityBomb;
        if (exitPortal            != null) Instance._clipExitPortal             = exitPortal;
        if (genericPower          != null) Instance._clipGenericPower           = genericPower;
        if (fireTrail             != null) Instance._clipFireTrail              = fireTrail;
    }

    private static void EnsureInstance()
    {
        if (Instance == null)
            new GameObject("GameAudio").AddComponent<GameAudio>();
    }

    private static void SwitchMusic(AudioClip clip)
    {
        if (Instance == null || clip == null) return;
        if (Instance._music.clip == clip && Instance._music.isPlaying) return;
        Instance._music.Stop();
        Instance._music.clip = clip;
        Instance._music.Play();
    }

    public static void EnsureMenuMusic()
    {
        EnsureInstance();
        if (Instance != null) { Instance._music.volume = 0.2f; Instance._menuClickActive = true; }
        SwitchMusic(Instance?._clipMenuTheme);
    }

    public static void EnsureMatchMusic(int mapIndex)
    {
        EnsureInstance();
        if (Instance != null) Instance._menuClickActive = false;
        if (Instance == null) return;
        AudioClip track = mapIndex switch
        {
            0 => Instance._clipMatchTheme0,
            1 => Instance._clipMatchTheme1,
            2 => Instance._clipMatchTheme2,
            _ => Instance._clipMatchTheme0
        };
        SwitchMusic(track);
    }

    public static void EnsureMusic() => EnsureMatchMusic(GameMapSelection.SelectedMapIndex);

    public static void StopMusic()
    {
        if (Instance != null)
            Instance._music.Stop();
    }

    private static void PlayClip(AudioClip clip, float volumeScale = 1f)
    {
        EnsureInstance();
        if (clip == null || Instance == null) return;
        Instance._sfx.PlayOneShot(clip, volumeScale);
    }

    public static void PlayMagicBurst(float volumeScale = 1f)     => PlayClip(Instance?._clipMagicBurst, volumeScale);
    public static void PlayRangedEnemyShot()                      => PlayClip(Instance?._clipRangedEnemyShot, 0.15f);
    public static void PlaySwordCut(float volumeScale = 1f)       => PlayClip(Instance?._clipSwordCut, volumeScale);
    public static void PlaySpearThrust()                          => PlayClip(Instance?._clipSpearThrust);
    public static void PlayItemPickup()                           => PlayClip(Instance?._clipItemPickup);
    public static void PlayPotionDrink()                          => PlayClip(Instance?._clipPotionDrink);
    public static void PlayGiantAttackStomp()                     => PlayClip(Instance?._clipGiantAttackStomp);
    public static void PlayGiantFootsteps()                       => PlayClip(Instance?._clipGiantFootsteps);
    public static void PlayMenuButtonClick()
    {
        if (Instance == null) return;
        if (Time.frameCount == Instance._clickPlayedFrame) return;
        Instance._clickPlayedFrame = Time.frameCount;
        PlayClip(Instance._clipMenuButtonClick);
    }
    public static void PlayExplosionSpecialAttack()               => PlayClip(Instance?._clipExplosionSpecialAttack);
    public static void PlaySwordThrow()                           => PlayClip(Instance?._clipSwordThrow);
    public static void PlaySpearThrow()                           => PlayClip(Instance?._clipSpearThrow);
    public static void PlayMinionSpawn()                          => PlayClip(Instance?._clipMinionSpawn, 1.8f);
    public static void PlayGravityBomb()                          => PlayClip(Instance?._clipGravityBomb, 1.8f);
    public static void PlayExitPortal()                           => PlayClip(Instance?._clipExitPortal);
    public static void PlayGenericPower()                         => PlayClip(Instance?._clipGenericPower);
    public static void PlayFireTrail()                            => PlayClip(Instance?._clipFireTrail);
}
