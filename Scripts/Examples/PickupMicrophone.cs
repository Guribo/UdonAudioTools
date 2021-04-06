﻿using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.UdonBetterAudio.Scripts.Examples
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(10000)]
    public class PickupMicrophone : UdonSharpBehaviour
    {
        protected const int NoUser = -1;

        public BetterPlayerAudio playerAudio;
        public BetterPlayerAudioOverride betterPlayerAudioOverride;
        
        public int playerId = NoUser;
        [SerializeField] protected SyncedPlayerId syncedPlayerId;
        protected int OldMicUserId = NoUser;

        public override void OnPickup()
        {
            var localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer))
            {
                return;
            }

            TakeOwnership(localPlayer, false);
            playerId = localPlayer.playerId;
            TryRequestSerialization();
        }

        public override void OnDrop()
        {
            playerId = NoUser;
            TryRequestSerialization();
        }

        public override void OnDeserialization()
        {
            UpdateMicUser();
        }

        public override void OnPreSerialization()
        {
            UpdateMicUser();
        }

        private void OnEnable()
        {
            NewUserStartUsingMic(playerId);
        }

        private void OnDisable()
        {
            CleanUpOldUser(playerId);
        }

        private void OnDestroy()
        {
            CleanUpOldUser(playerId);
        }

        /// <summary>
        /// if the current user has changed switch let only the new user be affected by the mic
        /// </summary>
        private void UpdateMicUser()
        {
            if (playerId != OldMicUserId)
            {
                CleanUpOldUser(OldMicUserId);
                NewUserStartUsingMic(playerId);
            }

            OldMicUserId = playerId;
        }

        /// <summary>
        /// take ownership of the microphone if the user doesn't have it yet, or force it
        /// </summary>
        /// <param name="localPlayer"></param>
        /// <param name="force"></param>
        private void TakeOwnership(VRCPlayerApi localPlayer, bool force)
        {
            if (!Utilities.IsValid(localPlayer))
            {
                Debug.LogWarning("PickupMicrophone.TakeOwnership: Invalid local player", this);
                return;
            }
            
            if (force || !Networking.IsOwner(localPlayer, gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }
        }

        /// <summary>
        /// if the mic is still held by the given user let that person no longer be affected by the mic
        /// </summary>
        private void CleanUpOldUser(int oldUser)
        {
            if (!Utilities.IsValid(playerAudio))
            {
                Debug.LogError("PickupMicrophone.CleanUpOldUser: playerAudio is invalid");
                return;
            }

            if (oldUser == NoUser)
            {
                return;
            }

            var currentMicUser = VRCPlayerApi.GetPlayerById(oldUser);
            if (Utilities.IsValid(currentMicUser))
            {
                if (Utilities.IsValid(betterPlayerAudioOverride))
                {
                    betterPlayerAudioOverride.RemoveAffectedPlayer(currentMicUser);
                }

                playerAudio.ClearPlayerOverride(currentMicUser.playerId);
            }
        }

        /// <summary>
        /// let the given user be affected by the mic
        /// </summary>
        private void NewUserStartUsingMic(int newUser)
        {
            if (!Utilities.IsValid(playerAudio))
            {
                Debug.LogError("PickupMicrophone.CleanUpOldUser: playerAudio is invalid");
                return;
            }

            if (newUser == NoUser)
            {
                return;
            }

            var newMicUser = VRCPlayerApi.GetPlayerById(newUser);
            if (!Utilities.IsValid(newMicUser))
            {
                return;
            }

            if (Utilities.IsValid(betterPlayerAudioOverride))
            {
                betterPlayerAudioOverride.AffectPlayer(newMicUser);
            }
            playerAudio.OverridePlayerSettings( betterPlayerAudioOverride);
        }
        
        private bool TryRequestSerialization()
        {
            if (Utilities.IsValid(syncedPlayerId))
            {
                syncedPlayerId.UpdateForAll();
                return true;
            }
            return false;
        }
    }
}