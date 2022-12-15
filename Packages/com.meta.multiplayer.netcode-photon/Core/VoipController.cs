// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using Unity.Netcode;
using UnityEngine;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
using UnityEngine.Android;
#endif

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Controlls the voice over ip setup for Photon Voice.
    /// Includes permission requirement for microphone.
    /// </summary>
    public class VoipController : MonoBehaviour
    {
        [SerializeField] private Speaker m_voipSpeakerPrefab;
        [SerializeField] private VoiceConnection m_voipRecorderPrefab;
        [SerializeField] private float m_headHeight = 1.0f;

        private VoiceConnection m_localVoipRecorder;

        private void OnEnable()
        {
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            // On Start we ask for Microphone permission
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
        }

        public void StartVoip(Transform parent)
        {
            m_localVoipRecorder = Instantiate(m_voipRecorderPrefab, parent);
            if (parent.gameObject.TryGetComponent<VoipHandler>(out var voipHandler))
            {
                voipHandler.SetRecorder(m_localVoipRecorder);
            }

            // We attach the recorder to the player entity which has the networkObject we want to reference as the id
            var networkObject = parent.gameObject.GetComponentInParent<NetworkObject>();
            _ = m_localVoipRecorder.Client.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                [nameof(NetworkObject.NetworkObjectId)] = (int)networkObject.NetworkObjectId,
            });
            m_localVoipRecorder.SpeakerFactory = (playerId, voiceId, userData) => CreateSpeaker(playerId, m_localVoipRecorder);

            _ = StartCoroutine(JoinPhotonVoiceRoom());
        }

        private Speaker CreateSpeaker(int playerId, VoiceConnection voiceConnection)
        {
            var actor = voiceConnection.Client.LocalPlayer.Get(playerId);
            Debug.Assert(actor != null, $"Could not find voice client for Player #{playerId}");

            _ = actor.CustomProperties.TryGetValue(nameof(NetworkObject.NetworkObjectId), out var networkId);
            Debug.Assert(networkId != null, $"Could not find network object id for Player #{playerId}");

            _ = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue((ulong)(int)networkId, out var player);
            Debug.Assert(player != null, $"Could not find player instance for Player #{playerId} network id #{networkId}");

            var speaker = Instantiate(m_voipSpeakerPrefab, player.transform);
            if (player.TryGetComponent<VoipHandler>(out var voipHandler))
            {
                voipHandler.SetSpeaker(speaker);
            }
            speaker.transform.localPosition = new Vector3(0.0f, m_headHeight, 0.0f);

            return speaker;
        }

        private IEnumerator JoinPhotonVoiceRoom()
        {
            yield return new WaitUntil(() => NetworkSession.PhotonVoiceRoom != "" && m_localVoipRecorder != null);

            // Only join if we can record voice
            if (CanRecordVoice())
            {
                var connectAndJoin = m_localVoipRecorder.GetComponent<ConnectAndJoin>();
                connectAndJoin.RoomName = NetworkSession.PhotonVoiceRoom;
                connectAndJoin.ConnectNow();
            }
        }

        private bool CanRecordVoice()
        {
            // Only record if permission was accepted
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }
    }
}