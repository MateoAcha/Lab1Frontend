using UnityEngine;
using UnityEngine.UI;

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
    private AudioClip _clipGiantAttackStomp;
    private AudioClip _clipMenuButtonClick;
    private AudioClip _clipExplosionSpecialAttack;
    private AudioClip _clipSwordThrow;
    private AudioClip _clipSpearThrow;
    private AudioClip _clipMinionSpawn;
    private AudioClip _clipGravityBomb;
    private AudioClip _clipExitPortal;
    private AudioClip _clipGenericPower;
    private AudioClip _clipFireTrail;

    private float _nextButtonHookAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapAudio()
    {
        EnsureInstance();
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
        _clipMagicBurst      = Resources.Load<AudioClip>("Sounds/humordome-magic-burst-452852");
        _clipRangedEnemyShot = Resources.Load<AudioClip>("Sounds/rescopicsound-elemental-magic-spell-impact-outgoing-228342");
        _clipSwordCut        = Resources.Load<AudioClip>("Sounds/ribhavagrawal-sword-cut-type-1-230552");
        _clipItemPickup  = Resources.Load<AudioClip>("Sounds/yodguard-item-pickup-1-540174");
        _clipPotionDrink = Resources.Load<AudioClip>("Sounds/yodguard-potion-drink-3-540167");
        _clipTheme       = Resources.Load<AudioClip>("Sounds/musicinmedia-8bit-theme-loop-chiptune-symphony-387749");
        _clipGiantAttackStomp       = Resources.Load<AudioClip>("Sounds/giant-attack-stomp");
        _clipMenuButtonClick        = Resources.Load<AudioClip>("Sounds/menu-button-click");
        _clipExplosionSpecialAttack = Resources.Load<AudioClip>("Sounds/explosion-special-attack");
        _clipSwordThrow             = Resources.Load<AudioClip>("Sounds/sword-throw");
        _clipSpearThrow             = Resources.Load<AudioClip>("Sounds/spear-throw");
        _clipMinionSpawn            = Resources.Load<AudioClip>("Sounds/minion-spawn");
        _clipGravityBomb            = Resources.Load<AudioClip>("Sounds/gravity-bomb");
        _clipExitPortal             = Resources.Load<AudioClip>("Sounds/exit-portal");
        _clipGenericPower           = Resources.Load<AudioClip>("Sounds/generic-power");
        _clipFireTrail              = Resources.Load<AudioClip>("Sounds/fire-trail");
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
        EnsureInstance();
        if (Instance != null && Instance._clipTheme != null && !Instance._music.isPlaying)
            Instance._music.Play();
    }

    private static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("GameAudio");
            obj.AddComponent<GameAudio>();
        }
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

    private static GameAudio GetInstance()
    {
        EnsureInstance();
        return Instance;
    }

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

        if (giantAttackStomp != null) Instance._clipGiantAttackStomp = giantAttackStomp;
        if (menuButtonClick != null) Instance._clipMenuButtonClick = menuButtonClick;
        if (explosionSpecialAttack != null) Instance._clipExplosionSpecialAttack = explosionSpecialAttack;
        if (swordThrow != null) Instance._clipSwordThrow = swordThrow;
        if (spearThrow != null) Instance._clipSpearThrow = spearThrow;
        if (minionSpawn != null) Instance._clipMinionSpawn = minionSpawn;
        if (gravityBomb != null) Instance._clipGravityBomb = gravityBomb;
        if (exitPortal != null) Instance._clipExitPortal = exitPortal;
        if (genericPower != null) Instance._clipGenericPower = genericPower;
        if (fireTrail != null) Instance._clipFireTrail = fireTrail;
    }

    public static void PlayMagicBurst(float volumeScale = 0.18f) { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipMagicBurst : null, volumeScale); }
    public static void PlayRangedEnemyShot() { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipRangedEnemyShot : null, 0.35f); }
    public static void PlaySwordCut(float volumeScale = 1f) { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipSwordCut : null, volumeScale); }
    public static void PlayItemPickup()      { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipItemPickup      : null); }
    public static void PlayPotionDrink()     { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipPotionDrink     : null); }
    public static void PlayGiantAttackStomp()       { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipGiantAttackStomp       : null); }
    public static void PlayMenuButtonClick()        { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipMenuButtonClick        : null, 0.8f); }
    public static void PlayExplosionSpecialAttack() { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipExplosionSpecialAttack : null); }
    public static void PlaySwordThrow()             { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipSwordThrow             : null); }
    public static void PlaySpearThrow()             { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipSpearThrow             : null); }
    public static void PlayMinionSpawn()            { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipMinionSpawn            : null, 1.6f); }
    public static void PlayGravityBomb()            { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipGravityBomb            : null, 1.8f); }
    public static void PlayExitPortal()             { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipExitPortal             : null); }
    public static void PlayGenericPower()           { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipGenericPower           : null); }
    public static void PlayFireTrail()              { GameAudio audio = GetInstance(); PlayClip(audio != null ? audio._clipFireTrail              : null); }

    private void Update()
    {
        if (Time.unscaledTime < _nextButtonHookAt)
        {
            return;
        }

        _nextButtonHookAt = Time.unscaledTime + 0.25f;
        Button[] buttons = FindObjectsOfType<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].GetComponent<MenuButtonClickSound>() == null)
            {
                buttons[i].gameObject.AddComponent<MenuButtonClickSound>();
            }
        }
    }
}

public class MenuButtonClickSound : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }

        if (_button != null)
        {
            _button.onClick.RemoveListener(Play);
            _button.onClick.AddListener(Play);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(Play);
        }
    }

    private void Play()
    {
        GameAudio.PlayMenuButtonClick();
    }
}
