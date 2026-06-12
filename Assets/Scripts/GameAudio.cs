using UnityEngine;

public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    private AudioSource _music;
    private AudioSource _sfx;

    private AudioClip _clipMagicBurst;
    private AudioClip _clipRangedEnemyShot;
    private AudioClip _clipSwordCut;
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
        _clipMagicBurst      = Resources.Load<AudioClip>("Sounds/humordome-magic-burst-452852");
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
        _music.clip = _clipTheme;

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

    public static void StopMusic()
    {
        if (Instance != null)
            Instance._music.Stop();
    }

    private static void PlayClip(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || Instance == null) return;
        Instance._sfx.PlayOneShot(clip, volumeScale);
    }

    public static void PlayMagicBurst()      => PlayClip(Instance != null ? Instance._clipMagicBurst      : null);
    public static void PlayRangedEnemyShot() => PlayClip(Instance != null ? Instance._clipRangedEnemyShot : null, 0.35f);
    public static void PlaySwordCut()        => PlayClip(Instance != null ? Instance._clipSwordCut        : null);
    public static void PlayItemPickup()      => PlayClip(Instance != null ? Instance._clipItemPickup      : null);
    public static void PlayPotionDrink()     => PlayClip(Instance != null ? Instance._clipPotionDrink     : null);
}
