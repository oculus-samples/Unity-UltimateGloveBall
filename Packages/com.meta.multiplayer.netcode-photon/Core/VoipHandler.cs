// Copyright (c) Meta Platforms, Inc. and affiliates.

using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Keeps reference to the audio speaker and the voice recorder to handle muting.
    /// </summary>
    public class VoipHandler : MonoBehaviour
    {
        private AudioSource m_speakerAudioSource;

        private VoiceConnection m_voiceRecorder;

        private bool m_isMuted = false;

        public bool IsMuted
        {
            get => m_isMuted;
            set
            {
                m_isMuted = value;
                if (m_speakerAudioSource)
                {
                    m_speakerAudioSource.mute = m_isMuted;
                }

                if (m_voiceRecorder)
                {
                    m_voiceRecorder.PrimaryRecorder.TransmitEnabled = !m_isMuted;
                }
            }
        }

        public void SetRecorder(VoiceConnection recorder)
        {
            m_voiceRecorder = recorder;
            m_voiceRecorder.PrimaryRecorder.TransmitEnabled = !m_isMuted;
        }

        public void SetSpeaker(Speaker speaker)
        {
            m_speakerAudioSource = speaker.GetComponent<AudioSource>();
            Assert.IsNotNull(m_speakerAudioSource, "Voip Speaker should have an AudioSource component.");
            m_speakerAudioSource.mute = m_isMuted;
        }
    }
}