using UnityEngine;
using MasterFramework.Core;

namespace MasterFramework.Services
{
    public class MockSoundService : ISoundService
    {
        public void PlaySFX(string clipName)
        {
            Debug.Log($"[MockSound] Playing SFX clip: '{clipName}'");
        }

        public void PlayBGM(string clipName)
        {
            Debug.Log($"[MockSound] Playing Background Music (BGM): '{clipName}'");
        }

        public void StopBGM()
        {
            Debug.Log("[MockSound] Stopped Background Music.");
        }

        public void SetVolume(string channel, float volume)
        {
            Debug.Log($"[MockSound] Set volume of channel '{channel}' to {volume:F2}");
        }
    }
}
