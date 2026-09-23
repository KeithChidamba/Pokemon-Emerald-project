// SoundManager.cs
//
// A layered sound manager modeled on how Pokemon Emerald handles audio:
//
//   - MUSIC layer: one track at a time (town theme, route theme, battle theme).
//     Starting a new music track crossfades out the old one. This is what
//     happens when you walk from Route 101 into Oldale Town, or when a
//     battle starts and the field theme cuts to "Battle! Wild Pokemon".
//   - JINGLE layer: short one-shot stingers that interrupt music briefly
//     without permanently replacing it (level up, badge obtained, evolution).
//     Emerald ducks the music under these rather than stopping it outright;
//     here we support either behaviour per-sound.
//   - UI layer: menu clicks, cursor moves, errors. Always plays instantly,
//     never fades, can overlap itself (fast menu navigation spams it).
//   - SFX layer: overworld/battle sound effects (wall bump, ball throw, hit
//     sounds). Can have many simultaneous voices (e.g. multi-hit moves).
//
// Enums identify sounds. A single serializable dictionary-like list maps
// each enum to a SoundData asset-like entry editable in the Inspector.
// A static Play/PlayMusic/PlayUI API is exposed so any script can call
// SoundManager.Play(SfxId.WallBump) without holding a reference.
//
// Not compiled/tested here (no Unity install in this environment) -- if
// something doesn't build, tell me the error and I'll fix it.
//
// Setup:
//   1. Put this script on an empty GameObject called "SoundManager" in your
//      first-loaded scene (or bootstrap it with DontDestroyOnLoad, already
//      handled below).
//   2. Fill in the MusicSounds / UISounds / SfxSounds lists in the Inspector.
//   3. Call SoundManager.PlayMusic(MusicId.Littleroot), SoundManager.Play(UiId.Select),
//      SoundManager.Play(SfxId.WallBump), etc. from anywhere.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

#region Enums — the "song IDs" of this system, similar to Emerald's MUS_/SE_ constants

/// <summary>Looping background music. Only one plays at a time, crossfaded.</summary>
public enum MusicId
{
    None = 0,
    Littleroot,
    OldaleTown,
    PetalburgCity,
    Route101,
    Route104,
    PokemonCenter,
    PokeMart,
    BattleWild,
    BattleTrainer,
    BattleGymLeader,
    VictoryWild,
    VictoryTrainer,

    // Appended (not inserted) so previously serialized enum values keep their meaning.
    BirchLab,
    May,
    TrainerEyesMeetYoungster,
    TrainerEyesMeetLass,
}

/// <summary>Short stingers that interrupt/duck music but don't replace it long-term.</summary>
public enum JingleId
{
    LevelUp,
    ObtainedBadge,
    ObtainedItem,
    Healed,
    EvolutionComplete,
}

/// <summary>Menu/UI sounds. Always instant, no fading, can overlap.</summary>
public enum UiId
{
    Select,
    WindowOpen,
    Error,
    Save,
    Shop,
    PcOn,
    PcOff,
    BagCursor,

    // Appended for the full Emerald/FRLG sound set.
    ClickMechanical,
    Failure,
    Alert,
    Success,
    UseItem,
    PcLogin,
    PokedexScroll,
    PokedexPageTurn,
    PokeNavOn,
    PokeNavOff,
    BagPocketSwitch,
    BallClick,
    FrlgShop,
    HelpOpen,
    HelpClose,
    HelpError,
}

/// <summary>Overworld/battle sound effects. Many simultaneous voices allowed.</summary>
public enum SfxId
{
    WallBump,
    Door,
    LedgeJump,
    BikeBell,
    BallThrow,
    BallShake,
    BallOpen,
    HitNormal,
    HitSuperEffective,
    HitNotVeryEffective,
    Faint,
    Flee,
    StatusParalysis,
    StatusPoison,
    StatusBurn,

    // Appended for the full Emerald/FRLG sound set.
    ExitStairs,
    SlidingDoor,
    BikeHop,
    Unlock,
    WarpIn,
    WarpOut,
    PoisonDamageOverworld,
    Itemfinder,
    DingDong,
    BallBounce1,
    BallBounce2,
    BallBounce3,
    BallBounce4,
    BallPullInTrade,
    ShinySparkle,
    EggHatch,
    LowHealthBeep,
    ExpGain,
    StatIncrease,
    StatDecrease,
    StatusBurnAlt,
}

/// <summary>Which mixer/behaviour layer a sound belongs to.</summary>
public enum SoundLayer
{
    Music,
    Jingle,
    UI,
    Sfx,
}

#endregion

#region Sound data

/// <summary>
/// Inspector-editable data for one sound. Works for music, jingles, UI or SFX;
/// which fields matter depends on the layer it's used from.
/// </summary>
[Serializable]
public class SoundData
{
    [Tooltip("Clip(s) to play. If more than one is set, one is chosen at random each time (pitch/round-robin variation, like footstep or hit sounds).")]
    public AudioClip[] clips;

    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Pitch is randomized within +/- this amount each play, to stop repeated SFX (wall bumps, hits) from sounding mechanical.")]
    [Range(0f, 0.5f)] public float pitchVariance = 0f;

    [Tooltip("Music/jingle only: fade time in seconds when this track starts and when it's replaced.")]
    public float fadeSeconds = 0.5f;

    [Tooltip("Jingle only: if true, music volume is ducked instead of stopped while this jingle plays.")]
    public bool duckMusicInstead = true;

    [Tooltip("Music only: loop the track. Battle/town themes loop; a one-shot fanfare would not.")]
    public bool loop = true;

    [HideInInspector] public int lastClipIndex = -1;

    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        // Avoid repeating the same clip twice in a row when there's a pool to pick from.
        int index;
        do { index = UnityEngine.Random.Range(0, clips.Length); }
        while (clips.Length > 1 && index == lastClipIndex);
        lastClipIndex = index;
        return clips[index];
    }

    public float GetPitch()
    {
        return 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
    }
}

/// <summary>Serializable enum->SoundData pair, since Unity can't serialize Dictionary directly.</summary>
[Serializable]
public class SoundEntry<TEnum> where TEnum : Enum
{
    public TEnum id;
    public SoundData data;
}

#endregion

/// <summary>
/// Central audio manager. Exposes static Play methods so any gameplay code can
/// trigger sound without holding a reference:
///   SoundManager.PlayMusic(MusicId.BattleWild);
///   SoundManager.Play(UiId.Select);
///   SoundManager.Play(SfxId.WallBump);
///
/// Wild battle: field -> battle -> victory fanfare -> back to the field theme:
///   SoundManager.PushMusic(MusicId.BattleWild);   // caches the field theme + its position, crossfades to battle
///   SoundManager.PlayMusic(MusicId.VictoryWild);  // PlayMusic doesn't touch the cache
///   SoundManager.PopMusicWhenFinished();          // fanfare ends -> field theme crossfades back in
///
/// Trainer battle: field -> "eyes meet" -> battle -> victory -> back to the field theme:
///   SoundManager.PushMusic(MusicId.TrainerEyesMeetYoungster);
///   SoundManager.PlayMusic(MusicId.BattleTrainer);   // PlayMusic doesn't touch the cache
///   SoundManager.PlayMusic(MusicId.VictoryTrainer);
///   SoundManager.PopMusicWhenFinished();             // fanfare ends -> field theme crossfades back in
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Mixer (optional)")]
    [Tooltip("Assign an AudioMixer with Music/Jingle/UI/Sfx groups to route layers separately. Optional -- works without one.")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup jingleMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Sound Data (enum -> data, editable per-entry in Inspector)")]
    [SerializeField] private List<SoundEntry<MusicId>> musicSounds = new List<SoundEntry<MusicId>>();
    [SerializeField] private List<SoundEntry<JingleId>> jingleSounds = new List<SoundEntry<JingleId>>();
    [SerializeField] private List<SoundEntry<UiId>> uiSounds = new List<SoundEntry<UiId>>();
    [SerializeField] private List<SoundEntry<SfxId>> sfxSounds = new List<SoundEntry<SfxId>>();

    [Header("Voice pool sizes")]
    [Tooltip("How many overlapping UI sounds can play at once (fast menu navigation).")]
    [SerializeField] private int uiVoices = 4;
    [Tooltip("How many overlapping SFX voices can play at once (multi-hit moves, several footsteps).")]
    [SerializeField] private int sfxVoices = 8;

    [Tooltip("Music volume multiplier applied while a ducking jingle is playing (0-1). 0.35 = quieter but audible.")]
    [Range(0f, 1f)][SerializeField] private float duckedMusicVolume = 0.35f;

    [Header("Music history (return to previous track)")]
    [Tooltip("How many previous tracks are remembered by PushMusic. Oldest is dropped past this limit.")]
    [SerializeField] private int maxMusicHistory = 4;
    [Tooltip("If true, PopMusic resumes the cached track from where it was interrupted. If false, it restarts from the beginning.")]
    [SerializeField] private bool resumeFromCachedPosition = true;

    // Runtime lookups built from the serialized lists above.
    private Dictionary<MusicId, SoundData> _musicMap;
    private Dictionary<JingleId, SoundData> _jingleMap;
    private Dictionary<UiId, SoundData> _uiMap;
    private Dictionary<SfxId, SoundData> _sfxMap;

    // One dedicated source per music "slot" so we can crossfade between two.
    private AudioSource _musicSourceA;
    private AudioSource _musicSourceB;
    private AudioSource _activeMusicSource; // whichever source is currently carrying (or fading in) the music
    private MusicId _currentMusic = MusicId.None;
    private Coroutine _musicFadeRoutine;
    private Coroutine _duckRoutine;
    private Coroutine _popWhenFinishedRoutine;
    private float _musicBaseVolume = 1f;

    // Cache of previously playing tracks so we can crossfade back to them
    // (field theme -> battle -> back to the field theme).
    private struct MusicSnapshot
    {
        public MusicId Id;
        public float Time; // playback position when it was interrupted

        public MusicSnapshot(MusicId id, float time)
        {
            Id = id;
            Time = time;
        }
    }
    private readonly List<MusicSnapshot> _musicHistory = new List<MusicSnapshot>();

    // Pooled sources for UI and SFX, since those need overlapping playback.
    private AudioSource[] _uiPool;
    private int _uiPoolIndex;
    private AudioSource[] _sfxPool;
    private int _sfxPoolIndex;

    // Jingles reuse a small pool too, since more than one could theoretically overlap.
    private AudioSource[] _jinglePool;
    private int _jinglePoolIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildLookups();
        BuildAudioSources();
    }

    private void BuildLookups()
    {
        _musicMap = ToDictionary(musicSounds);
        _jingleMap = ToDictionary(jingleSounds);
        _uiMap = ToDictionary(uiSounds);
        _sfxMap = ToDictionary(sfxSounds);
    }

    private static Dictionary<TEnum, SoundData> ToDictionary<TEnum>(List<SoundEntry<TEnum>> entries) where TEnum : Enum
    {
        var dict = new Dictionary<TEnum, SoundData>();
        foreach (var entry in entries)
        {
            if (entry == null || entry.data == null) continue;
            if (dict.ContainsKey(entry.id))
            {
                Debug.LogWarning($"[SoundManager] Duplicate entry for {entry.id}, ignoring the extra one.");
                continue;
            }
            dict.Add(entry.id, entry.data);
        }
        return dict;
    }

    private void BuildAudioSources()
    {
        _musicSourceA = CreateSource("MusicSourceA", musicMixerGroup, loop: true);
        _musicSourceB = CreateSource("MusicSourceB", musicMixerGroup, loop: true);
        _activeMusicSource = _musicSourceA;

        _jinglePool = CreatePool("Jingle", 2, jingleMixerGroup, loop: false);
        _uiPool = CreatePool("UI", uiVoices, uiMixerGroup, loop: false);
        _sfxPool = CreatePool("Sfx", sfxVoices, sfxMixerGroup, loop: false);
    }

    private AudioSource CreateSource(string name, AudioMixerGroup group, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.outputAudioMixerGroup = group;
        return src;
    }

    private AudioSource[] CreatePool(string prefix, int count, AudioMixerGroup group, bool loop)
    {
        var pool = new AudioSource[Mathf.Max(1, count)];
        for (int i = 0; i < pool.Length; i++)
            pool[i] = CreateSource($"{prefix}Voice_{i}", group, loop);
        return pool;
    }

    #region Public static API

    /// <summary>Play a looping music track, crossfading out whatever is currently playing.</summary>
    public static void PlayMusic(MusicId id) => Instance?.PlayMusicInternal(id);

    /// <summary>Stop music (crossfades to silence).</summary>
    public static void StopMusic() => Instance?.PlayMusicInternal(MusicId.None);

    /// <summary>
    /// Cache whatever is playing now (track + position), then crossfade to a new track.
    /// Pair with PopMusic() to crossfade back. Typical use: field theme -> battle theme.
    /// </summary>
    public static void PushMusic(MusicId id) => Instance?.PushMusicInternal(id);

    /// <summary>Crossfade back to the most recently cached track. Does nothing if the cache is empty.</summary>
    public static void PopMusic() => Instance?.PopMusicInternal(Instance.resumeFromCachedPosition);

    /// <summary>Same as PopMusic(), but choose whether to resume from the cached position or restart the track.</summary>
    public static void PopMusic(bool resumeFromPosition) => Instance?.PopMusicInternal(resumeFromPosition);

    /// <summary>
    /// Wait for the current (non-looping) track to finish, then PopMusic(). Use after a victory fanfare
    /// so the field music returns exactly when the fanfare ends. If the current track loops, pops immediately.
    /// </summary>
    public static void PopMusicWhenFinished() => Instance?.PopMusicWhenFinishedInternal(Instance.resumeFromCachedPosition);

    /// <summary>
    /// Crossfade back to the OLDEST cached track and clear the whole cache. Use when several tracks were
    /// pushed in a row (field -> trainer-spotted -> battle) and you want to jump straight back to the field theme.
    /// </summary>
    public static void PopAllMusic() => Instance?.PopAllMusicInternal(Instance.resumeFromCachedPosition);

    /// <summary>Forget all cached tracks without changing what's playing (e.g. when loading a new area).</summary>
    public static void ClearMusicHistory() => Instance?._musicHistory.Clear();

    /// <summary>The track that PopMusic() would return to, or MusicId.None if nothing is cached.</summary>
    public static MusicId PreviousMusic
    {
        get
        {
            var i = Instance;
            return (i != null && i._musicHistory.Count > 0) ? i._musicHistory[i._musicHistory.Count - 1].Id : MusicId.None;
        }
    }

    /// <summary>The track currently playing (or fading in).</summary>
    public static MusicId CurrentMusic => Instance != null ? Instance._currentMusic : MusicId.None;

    /// <summary>Play a one-shot jingle, ducking or pausing music per its SoundData settings.</summary>
    public static void Play(JingleId id) => Instance?.PlayJingleInternal(id);

    /// <summary>Play a UI sound. Instant, no fade, can overlap itself.</summary>
    public static void Play(UiId id) => Instance?.PlayUiInternal(id);

    /// <summary>Play a gameplay SFX. Instant, no fade, supports many simultaneous voices.</summary>
    public static void Play(SfxId id) => Instance?.PlaySfxInternal(id);

    /// <summary>Play a SFX at reduced/boosted volume relative to its authored volume, e.g. distance attenuation done manually.</summary>
    public static void Play(SfxId id, float volumeScale) => Instance?.PlaySfxInternal(id, volumeScale);

    #endregion

    #region Music (Layer 1: crossfaded, single voice)

    private void PlayMusicInternal(MusicId id, float startTime = 0f)
    {
        if (id == _currentMusic) return; // already playing, mirrors Emerald not restarting a track that's already looping

        SoundData data = null;
        if (id != MusicId.None && !_musicMap.TryGetValue(id, out data))
        {
            Debug.LogWarning($"[SoundManager] No SoundData mapped for MusicId.{id}");
            return;
        }

        _currentMusic = id;
        _musicBaseVolume = data?.volume ?? 1f;

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(CrossfadeMusicRoutine(data, startTime));
    }

    private IEnumerator CrossfadeMusicRoutine(SoundData data, float startTime)
    {
        AudioSource outgoing = _activeMusicSource;
        AudioSource incoming = (outgoing == _musicSourceA) ? _musicSourceB : _musicSourceA;
        _activeMusicSource = incoming;

        float fadeTime = data?.fadeSeconds ?? 0.5f;
        AudioClip clip = data?.GetClip();

        if (clip != null)
        {
            incoming.clip = clip;
            incoming.loop = data.loop;
            incoming.volume = 0f;
            incoming.pitch = 1f; // music doesn't use the pitch-variance feature

            // Resume position (used when returning to a cached track). Clamp so we never
            // seek past the end of the clip.
            if (startTime > 0f && clip.length > 0.05f)
                incoming.time = Mathf.Clamp(startTime, 0f, clip.length - 0.05f);
            else
                incoming.time = 0f;

            incoming.Play();
        }

        float t = 0f;
        float outgoingStartVolume = outgoing.volume;
        float targetVolume = data?.volume ?? 1f;

        // Guard against fadeTime == 0 causing a divide-by-zero.
        fadeTime = Mathf.Max(fadeTime, 0.0001f);

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float p = t / fadeTime;
            outgoing.volume = Mathf.Lerp(outgoingStartVolume, 0f, p);
            if (clip != null) incoming.volume = Mathf.Lerp(0f, targetVolume, p);
            yield return null;
        }

        outgoing.Stop();
        outgoing.volume = 0f;
        if (clip != null) incoming.volume = targetVolume;

        _musicFadeRoutine = null;
    }

    private AudioSource CurrentMusicSource => _activeMusicSource; // the source carrying (or fading in) the current track

    #endregion

    #region Music history (previous-track cache for crossfading back)

    private void PushMusicInternal(MusicId id)
    {
        if (id == _currentMusic) return; // already playing this track, nothing to cache

        // Validate BEFORE caching, otherwise a missing mapping would leave a bogus
        // entry in the cache while the current track keeps playing.
        if (id != MusicId.None && !_musicMap.ContainsKey(id))
        {
            Debug.LogWarning($"[SoundManager] No SoundData mapped for MusicId.{id}");
            return;
        }

        // Cache what's playing now, including where it was in the track.
        _musicHistory.Add(new MusicSnapshot(_currentMusic, GetCurrentMusicTime()));

        // Keep the cache bounded; drop the oldest entry first.
        int limit = Mathf.Max(1, maxMusicHistory);
        while (_musicHistory.Count > limit)
            _musicHistory.RemoveAt(0);

        PlayMusicInternal(id);
    }

    private void PopMusicInternal(bool resumeFromPosition)
    {
        if (_musicHistory.Count == 0) return;

        int last = _musicHistory.Count - 1;
        MusicSnapshot snapshot = _musicHistory[last];
        _musicHistory.RemoveAt(last);

        RestoreSnapshot(snapshot, resumeFromPosition);
    }

    private void PopAllMusicInternal(bool resumeFromPosition)
    {
        if (_musicHistory.Count == 0) return;

        MusicSnapshot oldest = _musicHistory[0];
        _musicHistory.Clear();

        RestoreSnapshot(oldest, resumeFromPosition);
    }

    private void RestoreSnapshot(MusicSnapshot snapshot, bool resumeFromPosition)
    {
        // A manual pop supersedes any pending "pop when finished".
        if (_popWhenFinishedRoutine != null)
        {
            StopCoroutine(_popWhenFinishedRoutine);
            _popWhenFinishedRoutine = null;
        }

        PlayMusicInternal(snapshot.Id, resumeFromPosition ? snapshot.Time : 0f);
    }

    private float GetCurrentMusicTime()
    {
        AudioSource src = _activeMusicSource;
        if (src == null || _currentMusic == MusicId.None || src.clip == null || !src.isPlaying)
            return 0f;
        return src.time;
    }

    private void PopMusicWhenFinishedInternal(bool resumeFromPosition)
    {
        if (_musicHistory.Count == 0) return;

        if (_popWhenFinishedRoutine != null) StopCoroutine(_popWhenFinishedRoutine);
        _popWhenFinishedRoutine = StartCoroutine(PopWhenFinishedRoutine(resumeFromPosition));
    }

    private IEnumerator PopWhenFinishedRoutine(bool resumeFromPosition)
    {
        MusicId watched = _currentMusic;

        // Wait a frame so a track that was started this frame is actually playing.
        yield return null;

        AudioSource src = _activeMusicSource;
        while (_currentMusic == watched && src.isPlaying && !src.loop)
            yield return null;

        _popWhenFinishedRoutine = null;

        // Something else took over the music while we were waiting; don't override it.
        if (_currentMusic != watched) yield break;

        PopMusicInternal(resumeFromPosition);
    }

    #endregion

    #region Jingles (Layer 2: one-shot, ducks or pauses music)

    private void PlayJingleInternal(JingleId id)
    {
        if (!_jingleMap.TryGetValue(id, out var data))
        {
            Debug.LogWarning($"[SoundManager] No SoundData mapped for JingleId.{id}");
            return;
        }

        var clip = data.GetClip();
        if (clip == null) return;

        var src = NextInPool(_jinglePool, ref _jinglePoolIndex);
        src.clip = clip;
        src.volume = data.volume;
        src.pitch = 1f;
        src.loop = false;
        src.Play();

        if (data.duckMusicInstead)
        {
            if (_duckRoutine != null) StopCoroutine(_duckRoutine);
            _duckRoutine = StartCoroutine(DuckMusicForDuration(clip.length));
        }
        // If duckMusicInstead is false, callers are expected to pair this with
        // PlayMusic/StopMusic themselves (e.g. explicitly pausing for a big fanfare).
    }

    private IEnumerator DuckMusicForDuration(float seconds)
    {
        var src = CurrentMusicSource;
        float original = src.volume;
        float ducked = _musicBaseVolume * duckedMusicVolume;

        float fade = 0.1f;
        yield return LerpVolume(src, original, ducked, fade);
        yield return new WaitForSeconds(Mathf.Max(0f, seconds - fade * 2f));
        yield return LerpVolume(src, src.volume, _musicBaseVolume, fade);

        _duckRoutine = null;
    }

    private IEnumerator LerpVolume(AudioSource src, float from, float to, float duration)
    {
        float t = 0f;
        duration = Mathf.Max(duration, 0.0001f);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            src.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        src.volume = to;
    }

    #endregion

    #region UI (Layer 3: instant, overlapping, no fade)

    private void PlayUiInternal(UiId id)
    {
        if (!_uiMap.TryGetValue(id, out var data))
        {
            Debug.LogWarning($"[SoundManager] No SoundData mapped for UiId.{id}");
            return;
        }

        var clip = data.GetClip();
        if (clip == null) return;

        var src = NextInPool(_uiPool, ref _uiPoolIndex);
        src.pitch = data.GetPitch();
        src.PlayOneShot(clip, data.volume);
    }

    #endregion

    #region SFX (Layer 4: instant, overlapping, many voices)

    private void PlaySfxInternal(SfxId id, float volumeScale = 1f)
    {
        if (!_sfxMap.TryGetValue(id, out var data))
        {
            Debug.LogWarning($"[SoundManager] No SoundData mapped for SfxId.{id}");
            return;
        }

        var clip = data.GetClip();
        if (clip == null) return;

        var src = NextInPool(_sfxPool, ref _sfxPoolIndex);
        src.pitch = data.GetPitch();
        src.PlayOneShot(clip, data.volume * volumeScale);
    }

    #endregion

    #region Pool helper

    /// <summary>
    /// Round-robins through a fixed pool of AudioSources so overlapping calls
    /// (rapid menu clicks, multi-hit attacks) each get their own voice instead
    /// of cutting each other off. If every voice is busy, it steals the oldest one,
    /// same trade-off Emerald's fixed hardware channel count makes.
    /// </summary>
    private static AudioSource NextInPool(AudioSource[] pool, ref int index)
    {
        // Prefer an idle voice if one exists, otherwise round-robin (steal oldest).
        for (int i = 0; i < pool.Length; i++)
        {
            int candidate = (index + i) % pool.Length;
            if (!pool[candidate].isPlaying)
            {
                index = (candidate + 1) % pool.Length;
                return pool[candidate];
            }
        }

        var chosen = pool[index];
        index = (index + 1) % pool.Length;
        return chosen;
    }

    #endregion
}