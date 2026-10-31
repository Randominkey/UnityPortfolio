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
        Error,
        CellHover,  // ASMR 미세 공기방울 스침 (호버)
        CellPop     // ASMR 쫀득한 왁뿌볼 뽁뽁이 팝 (클릭)
    }

    public interface IAudioService
    {
        bool IsMuted { get; }
        IReadOnlyReactiveProperty<bool> IsMutedProperty { get; }

        void PlaySfx(SfxType sfxType);
        void PlaySfx(SfxType sfxType, float volumeScale, float pitch);
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
                    if (Application.isPlaying) DontDestroyOnLoad(go);
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
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            GenerateProceduralClips();
        }

        public void PlaySfx(SfxType sfxType)
        {
            PlaySfx(sfxType, 1.0f, 1.0f);
        }

        public void PlaySfx(SfxType sfxType, float volumeScale, float pitch)
        {
            if (IsMuted) return;

            if (_clipCache.TryGetValue(sfxType, out var clip))
            {
                if (Mathf.Approximately(pitch, 1.0f))
                {
                    _audioSource.PlayOneShot(clip, volumeScale * 0.7f);
                }
                else
                {
                    float prevPitch = _audioSource.pitch;
                    _audioSource.pitch = pitch;
                    _audioSource.PlayOneShot(clip, volumeScale * 0.7f);
                    _audioSource.pitch = prevPitch;
                }
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

            // [ASMR] 마우스 호버 시 은은한 공기방울 미세 스침 (초경량 틱/블립)
            _clipCache[SfxType.CellHover] = CreateWaveform("SFX_CellHover", sampleRate, 0.024f, (t, dur) =>
            {
                float p = t / dur;
                float freq = Mathf.Lerp(1300f, 650f, p * p);
                float env = Mathf.Sin(Mathf.PI * p) * (1f - p * 0.3f);
                return Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.15f; // 극히 은은한 볼륨
            });

            // [ASMR] 왁뿌볼/에어캡 뽁뽁이 터지는 쫀득한 청각 자극음 (파열 팝)
            _clipCache[SfxType.CellPop] = CreateWaveform("SFX_CellPop", sampleRate, 0.046f, (t, dur) =>
            {
                float p = t / dur;
                // 빠른 기압 강하 주파수 (720Hz -> 170Hz)
                float freq = Mathf.Lerp(720f, 170f, Mathf.Sqrt(p));
                // 탄성 질감을 위한 2차 배음 살짝 가미
                float tone1 = Mathf.Sin(2 * Mathf.PI * freq * t);
                float tone2 = Mathf.Sin(2 * Mathf.PI * (freq * 1.8f) * t) * 0.25f;
                // 앞쪽 5ms 급속 어택 후 부드러운 감쇠
                float env = p < 0.12f ? (p / 0.12f) : Mathf.Pow(1f - p, 1.8f);
                return (tone1 + tone2) * env * 0.55f;
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
