using UnityEngine;

namespace JigsawPuzzle.Core
{
    /// <summary>
    /// Manages sound effects and win jingles.
    /// Includes procedural audio generation if no audio files are assigned,
    /// so the game is completely playable with audio out-of-the-box!
    /// Attach to: AudioManager GameObject
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips (Optional - Generates procedural audio if null)")]
        [Tooltip("Played when a piece snaps correctly onto the board.")]
        public AudioClip snapClip;

        [Tooltip("Played when all pieces are placed and the puzzle is solved.")]
        public AudioClip winClip;

        [Tooltip("Played when UI buttons are clicked.")]
        public AudioClip clickClip;

        [Header("Audio Settings")]
        [Range(0f, 1f)] public float sfxVolume = 1f;

        private AudioSource _audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;

            // Generate fallback procedural audio if clips are missing
            if (snapClip == null) snapClip = CreateSnapProceduralClip();
            if (winClip == null) winClip = CreateWinProceduralClip();
            if (clickClip == null) clickClip = CreateClickProceduralClip();
        }

        public void PlaySnap()
        {
            if (snapClip != null)
            {
                _audioSource.PlayOneShot(snapClip, sfxVolume);
            }
        }

        public void PlayWin()
        {
            if (winClip != null)
            {
                _audioSource.PlayOneShot(winClip, sfxVolume);
            }
        }

        public void PlayButtonClick()
        {
            if (clickClip != null)
            {
                _audioSource.PlayOneShot(clickClip, sfxVolume * 0.7f);
            }
        }

        #region Procedural Audio Generators (Zero Asset Dependencies)

        private AudioClip CreateSnapProceduralClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 35f);
                float freq = Mathf.Lerp(600f, 180f, t / duration);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                samples[i] = wave * envelope * 0.85f;
            }

            AudioClip clip = AudioClip.Create("ProceduralSnap", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateWinProceduralClip()
        {
            int sampleRate = 44100;
            float duration = 1.2f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Triumphant chord arpeggio: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
            float[] chordFreqs = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = 0.22f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float currentVal = 0f;

                for (int note = 0; note < chordFreqs.Length; note++)
                {
                    float noteStart = note * noteDuration;
                    if (t >= noteStart)
                    {
                        float localT = t - noteStart;
                        float env = Mathf.Exp(-localT * 3.5f);
                        float freq = chordFreqs[note];
                        currentVal += Mathf.Sin(2f * Mathf.PI * freq * localT) * env * 0.25f;
                    }
                }
                samples[i] = Mathf.Clamp(currentVal, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ProceduralWin", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateClickProceduralClip()
        {
            int sampleRate = 44100;
            float duration = 0.05f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 60f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create("ProceduralClick", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion
    }
}
