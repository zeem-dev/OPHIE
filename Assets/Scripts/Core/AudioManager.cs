// ============================================================
//  OPHIO — AudioManager
//  Singleton. Handles background music, UI SFX, ability SFX.
//  Add to OPHIO_Managers GameObject in scene.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OPHIO.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        // --------------------------------------------------
        //  Music
        // --------------------------------------------------
        [Header("Background Music")]
        public AudioClip musicMainMenu;
        public AudioClip musicArenaLoop;
        public AudioClip musicBossIntro;
        public AudioClip musicBossLoop;
        public AudioClip musicVictory;
        public AudioClip musicDefeat;

        [Range(0f, 1f)] public float musicVolume   = 0.5f;
        public float musicFadeDuration             = 1.5f;

        // --------------------------------------------------
        //  UI SFX
        // --------------------------------------------------
        [Header("UI SFX")]
        public AudioClip sfxButtonClick;
        public AudioClip sfxButtonHover;
        public AudioClip sfxAbilityEquip;
        public AudioClip sfxCharacterSelect;
        public AudioClip sfxCountdown;
        public AudioClip sfxWaveStart;
        public AudioClip sfxWaveClear;
        public AudioClip sfxVictoryStinger;
        public AudioClip sfxDefeatStinger;

        // --------------------------------------------------
        //  Ability SFX
        // --------------------------------------------------
        [Header("Hawk Ability SFX")]
        public AudioClip sfxArcSlash;
        public AudioClip sfxDischarge;
        public AudioClip sfxNeuralSurge;
        public AudioClip sfxNeuralSurgeEnd;
        public AudioClip sfxVoltDash;
        public AudioClip sfxEnergySiphon;
        public AudioClip sfxTotalOverloadCharge;
        public AudioClip sfxTotalOverloadBlast;
        public AudioClip sfxTotalOverloadHum;

        [Header("Combat SFX")]
        public AudioClip sfxHitImpact;
        public AudioClip sfxPlayerHurt;
        public AudioClip sfxEnemyDeath;
        public AudioClip sfxBossRoar;

        [Range(0f, 1f)] public float sfxVolume     = 0.8f;

        // --------------------------------------------------
        //  Audio sources
        // --------------------------------------------------
        private AudioSource _musicSource;
        private AudioSource _musicSourceB;     // for crossfade
        private AudioSource _sfxSource;
        private AudioSource _sfxLoopSource;    // for looping SFX (hums etc.)

        private bool        _isMusicA = true;  // which source is active
        private Coroutine   _fadeCoroutine;

        // --------------------------------------------------
        //  Lifecycle
        // --------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupSources();
        }

        private void SetupSources()
        {
            _musicSource    = AddSource("Music_A",    musicVolume, true,  true);
            _musicSourceB   = AddSource("Music_B",    0f,          true,  true);
            _sfxSource      = AddSource("SFX",        sfxVolume,   false, false);
            _sfxLoopSource  = AddSource("SFX_Loop",   sfxVolume,   true,  false);
        }

        private AudioSource AddSource(string goName, float volume,
                                       bool loop, bool playOnAwake)
        {
            var go  = new GameObject(goName);
            go.transform.SetParent(transform);
            var src            = go.AddComponent<AudioSource>();
            src.volume         = volume;
            src.loop           = loop;
            src.playOnAwake    = playOnAwake;
            src.spatialBlend   = 0f;   // 2D — UI/music always full volume
            return src;
        }

        // ==================================================
        //  PUBLIC API — Music
        // ==================================================

        public void PlayMusic(AudioClip clip, bool fade = true)
        {
            if (clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            if (fade)
            {
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                _musicSource.clip   = clip;
                _musicSource.volume = musicVolume;
                _musicSource.Play();
            }
        }

        public void StopMusic(bool fade = true)
        {
            if (fade)
            {
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeOut(_musicSource, musicFadeDuration));
            }
            else _musicSource.Stop();
        }

        public void SetMusicVolume(float v)
        {
            musicVolume         = Mathf.Clamp01(v);
            _musicSource.volume = musicVolume;
        }

        // --------------------------------------------------
        //  Convenience music play methods
        // --------------------------------------------------
        public void PlayMenuMusic()   => PlayMusic(musicMainMenu);
        public void PlayArenaMusic()  => PlayMusic(musicArenaLoop);
        public void PlayBossMusic()
        {
            StartCoroutine(BossMusicSequence());
        }
        public void PlayVictoryMusic(){ StopMusic(false); PlayMusic(musicVictory, false); }
        public void PlayDefeatMusic() { StopMusic(false); PlayMusic(musicDefeat,  false); }

        private IEnumerator BossMusicSequence()
        {
            // Intro plays once, then loop starts
            if (musicBossIntro != null)
            {
                PlayMusic(musicBossIntro, true);
                yield return new WaitForSeconds(musicBossIntro.length);
            }
            if (musicBossLoop != null)
                PlayMusic(musicBossLoop, false);
        }

        // ==================================================
        //  PUBLIC API — SFX
        // ==================================================

        /// <summary>Play a one-shot SFX at 2D (UI/ability).</summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        /// <summary>Play SFX at a world position (3D spatial).</summary>
        public void PlaySFXAt(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * volumeScale);
        }

        /// <summary>Play a looping SFX (e.g. Neural Surge hum).</summary>
        public void PlayLoopSFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            _sfxLoopSource.clip   = clip;
            _sfxLoopSource.volume = sfxVolume * volumeScale;
            _sfxLoopSource.Play();
        }

        /// <summary>Stop the looping SFX.</summary>
        public void StopLoopSFX() => _sfxLoopSource.Stop();

        public void SetSFXVolume(float v)
        {
            sfxVolume            = Mathf.Clamp01(v);
            _sfxSource.volume    = sfxVolume;
            _sfxLoopSource.volume= sfxVolume;
        }

        // --------------------------------------------------
        //  Ability SFX shortcuts
        // --------------------------------------------------
        public void PlayArcSlash()         => PlaySFX(sfxArcSlash);
        public void PlayDischarge()        => PlaySFX(sfxDischarge);
        public void PlayNeuralSurge()      { PlaySFX(sfxNeuralSurge); PlayLoopSFX(sfxTotalOverloadHum, 0.4f); }
        public void PlayNeuralSurgeEnd()   { StopLoopSFX(); PlaySFX(sfxNeuralSurgeEnd); }
        public void PlayVoltDash()         => PlaySFX(sfxVoltDash);
        public void PlayEnergySiphon()     => PlaySFX(sfxEnergySiphon);
        public void PlayTotalOverload()    { PlaySFX(sfxTotalOverloadCharge); }
        public void PlayTotalOverloadBlast(){ StopLoopSFX(); PlaySFX(sfxTotalOverloadBlast); PlayLoopSFX(sfxTotalOverloadHum, 0.6f); }
        public void PlayHitImpact(Vector3 pos)  => PlaySFXAt(sfxHitImpact,  pos);
        public void PlayPlayerHurt()       => PlaySFX(sfxPlayerHurt);
        public void PlayEnemyDeath(Vector3 pos) => PlaySFXAt(sfxEnemyDeath, pos);
        public void PlayBossRoar(Vector3 pos)   => PlaySFXAt(sfxBossRoar,   pos);

        // --------------------------------------------------
        //  UI SFX shortcuts
        // --------------------------------------------------
        public void PlayButtonClick()      => PlaySFX(sfxButtonClick,      0.9f);
        public void PlayButtonHover()      => PlaySFX(sfxButtonHover,      0.5f);
        public void PlayAbilityEquip()     => PlaySFX(sfxAbilityEquip,     0.8f);
        public void PlayCharacterSelect()  => PlaySFX(sfxCharacterSelect,  1f);
        public void PlayCountdown()        => PlaySFX(sfxCountdown,        1f);
        public void PlayWaveStart()        => PlaySFX(sfxWaveStart,        1f);
        public void PlayWaveClear()        => PlaySFX(sfxWaveClear,        1f);
        public void PlayVictoryStinger()   => PlaySFX(sfxVictoryStinger,   1f);
        public void PlayDefeatStinger()    => PlaySFX(sfxDefeatStinger,    1f);

        // ==================================================
        //  Crossfade coroutine
        // ==================================================
        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            AudioSource fadeOut = _isMusicA ? _musicSource  : _musicSourceB;
            AudioSource fadeIn  = _isMusicA ? _musicSourceB : _musicSource;
            _isMusicA           = !_isMusicA;

            fadeIn.clip   = newClip;
            fadeIn.volume = 0f;
            fadeIn.Play();

            float elapsed = 0f;
            while (elapsed < musicFadeDuration)
            {
                elapsed         += Time.unscaledDeltaTime;
                float t          = elapsed / musicFadeDuration;
                fadeOut.volume   = Mathf.Lerp(musicVolume, 0f, t);
                fadeIn.volume    = Mathf.Lerp(0f, musicVolume, t);
                yield return null;
            }

            fadeOut.Stop();
            fadeOut.volume = musicVolume;
        }

        private IEnumerator FadeOut(AudioSource src, float duration)
        {
            float start   = src.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed    += Time.unscaledDeltaTime;
                src.volume  = Mathf.Lerp(start, 0f, elapsed / duration);
                yield return null;
            }
            src.Stop();
            src.volume = start;
        }
    }
}
