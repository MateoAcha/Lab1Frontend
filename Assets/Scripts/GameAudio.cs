using UnityEngine;
using UnityEngine.UI;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    private AudioSource _music;
    private AudioSource _sfx;

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
    private AudioClip _clipTheme;

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
        _clipRangedEnemyShot = Resources.Load<AudioClip>("Sounds/rescopicsound-elemental-magic-spell-impact-outgoing-228342");
        _clipSwordCut        = Resources.Load<AudioClip>("Sounds/ribhavagrawal-sword-cut-type-1-230552");
        _clipItemPickup  = Resources.Load<AudioClip>("Sounds/yodguard-item-pickup-1-540174");
        _clipPotionDrink = Resources.Load<AudioClip>("Sounds/yodguard-potion-drink-3-540167");
        _clipTheme       = Resources.Load<AudioClip>("Sounds/musicinmedia-8bit-theme-loop-chiptune-symphony-387749");
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

    public static void EnsureMusic()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("GameAudio");
            obj.AddComponent<GameAudio>();
        }
        if (Instance != null && Instance._clipTheme != null && !Instance._music.isPlaying)
            Instance._music.Play();
    }

    public static void EnsureMenuMusic()
    {
        EnsureInstance();
        SwitchMusic(Instance?._clipMenuTheme);
    }

    public static void EnsureMatchMusic(int mapIndex)
    {
        EnsureInstance();
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

    public static void PlayMagicBurst()      => PlayClip(Instance != null ? Instance._clipMagicBurst      : null);
    public static void PlayRangedEnemyShot() => PlayClip(Instance != null ? Instance._clipRangedEnemyShot : null, 0.35f);
    public static void PlaySwordCut()        => PlayClip(Instance != null ? Instance._clipSwordCut        : null);
    public static void PlayItemPickup()      => PlayClip(Instance != null ? Instance._clipItemPickup      : null);
    public static void PlayPotionDrink()     => PlayClip(Instance != null ? Instance._clipPotionDrink     : null);
}
