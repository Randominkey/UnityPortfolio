using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace MasterFramework.Core
{
    public enum SfxType
    {
        Click,
        TabSwitch,
        Rotate,
        KeypadInput,
        Undo,
        Redo,
        SuccessStar,
        StageClear,
        Error
    }

    public interface IAudioService
    {
        bool IsMuted { get; }
        IReadOnlyReactiveProperty<bool> IsMutedProperty { get; }

        void PlaySfx(SfxType sfxType);
        void ToggleMute();
        void SetMute(bool mute);
    }

    /// <summary>
    /// Central audio service that procedurally synthesizes crisp UI and puzzle SFX at runtime.
    /// Requires zero external audio assets.
    /// </summary>
    public class AudioService : MonoBehaviour, IAudioService
    {
        private static AudioService _instance;
        public static AudioService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[AudioService]", typeof(AudioSource), typeof(AudioService));
                    DontDestroyOnLoad(go);
                    _instance = go.GetComponent<AudioService>();
                }
                return _instance;
            }
        }

        private AudioSource _audioSource;
        private readonly ReactiveProperty<bool> _isMuted = new ReactiveProperty<bool>(false);
        private readonly Dictionary<SfxType, AudioClip> _clipCache = new Dictionary<SfxType, AudioClip>();

        public bool IsMuted => _isMuted.Value;
        public IReadOnlyReactiveProperty<bool> IsMutedProperty => _isMuted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            GenerateProceduralClips();
        }

        public void PlaySfx(SfxType sfxType)
        {
            if (IsMuted) return;

            if (_clipCache.TryGetValue(sfxType, out var clip))
            {
                _audioSource.PlayOneShot(clip, 0.7f);
            }
        }

        public void ToggleMute()
        {
            SetMute(!IsMuted);
        }

        public void SetMute(bool mute)
        {
            _isMuted.Value = mute;
        }

        private void GenerateProceduralClips()
        {
            const int sampleRate = 44100;

            _clipCache[SfxType.Click] = CreateWaveform("SFX_Click", sampleRate, 0.035f, (t, dur) =>
            {
                float freq = 1200f;
                float env = 1f - (t / dur);
                return Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.5f;
            });

            _clipCache[SfxType.TabSwitch] = CreateWaveform("SFX_TabSwitch", sampleRate, 0.08f, (t, dur) =>
            {
                float freq = Mathf.Lerp(450f, 900f, t / dur);
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                return Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.4f;
            });

            _clipCache[SfxType.Rotate] = CreateWaveform("SFX_Rotate", sampleRate, 0.05f, (t, dur) =>
            {
                float freq = Mathf.Lerp(700f, 350f, t / dur);
                float env = 1f - (t / dur);
                return Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.45f;
            });

            _clipCache[SfxType.KeypadInput] = CreateWaveform("SFX_Keypad", sampleRate, 0.06f, (t, dur) =>
            {
                float freq = 587.33f; // D5
                float env = 1f - (t / dur);
                return Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.4f;
            });

            _clipCache[SfxType.Undo] = CreateWaveform("SFX_Undo", sampleRate, 0.09f, (t, dur) =>
            {
                float freq = Mathf.Lerp(800f, 320f, t / dur);
                float env = 1f - (t / dur);
                return Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.45f;
            });

            _clipCache[SfxType.Redo] = CreateWaveform("SFX_Redo", sampleRate, 0.09f, (t, dur) =>
            {
                float freq = Mathf.Lerp(320f, 800f, t / dur);
                float env = 1f - (t / dur);
                return Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.45f;
            });

            _clipCache[SfxType.SuccessStar] = CreateWaveform("SFX_Star", sampleRate, 0.35f, (t, dur) =>
            {
                float env = 1f - (t / dur);
                float c5 = Mathf.Sin(2 * Mathf.PI * 523.25f * t);
                float e5 = Mathf.Sin(2 * Mathf.PI * 659.25f * t);
                float g5 = Mathf.Sin(2 * Mathf.PI * 783.99f * t);
                return (c5 + e5 + g5) / 3f * env * 0.6f;
            });

            _clipCache[SfxType.StageClear] = CreateWaveform("SFX_Clear", sampleRate, 0.6f, (t, dur) =>
            {
                float env = 1f - (t / dur);
                float c5 = Mathf.Sin(2 * Mathf.PI * 523.25f * t);
                float e5 = Mathf.Sin(2 * Mathf.PI * 659.25f * t);
                float g5 = Mathf.Sin(2 * Mathf.PI * 783.99f * t);
                float c6 = Mathf.Sin(2 * Mathf.PI * 1046.50f * t);
                return (c5 + e5 + g5 + c6) / 4f * env * 0.7f;
            });

            _clipCache[SfxType.Error] = CreateWaveform("SFX_Error", sampleRate, 0.15f, (t, dur) =>
            {
                float freq = 180f;
                float env = Mathf.Sin(Mathf.PI * (t / dur));
                return (Mathf.Sin(2 * Mathf.PI * freq * t) > 0 ? 1f : -1f) * env * 0.3f; // Square wave buzz
            });
        }

        private AudioClip CreateWaveform(string name, int sampleRate, float duration, Func<float, float, float> sampleFunc)
        {
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = sampleFunc(t, duration);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
