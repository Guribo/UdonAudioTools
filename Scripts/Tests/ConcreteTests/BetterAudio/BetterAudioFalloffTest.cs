﻿using System;
using Guribo.UdonUtils.Scripts.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;

namespace Guribo.UdonBetterAudio.Scripts.Tests.ConcreteTests.BetterAudio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterAudioFalloffTest : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [NonSerialized] public TestController testController;

        public void Initialize()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Test.CleanUp", this);
            CleanUpTest();
        }

        private bool Assert(bool condition, string message)
        {
            if (!condition)
            {
                if (Utilities.IsValid(this))
                {
                    Debug.LogError(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Assertion failed : '" +
                        GetType() + " : " + message + "'", this);
                }
                else
                {
                    Debug.LogError(
                        "[<color=#000000>UdonUtils</color>] [<color=#804500>Testing</color>] Assertion failed :  'UNKNOWN TYPE: " +
                        message + "'");
                }

                return false;
            }

            Debug.Assert(condition, message);
            return true;
        }

        #endregion

        #region EDIT HERE

        public int samples = 100;
        public float stepSize = 1f;
        public float stepInterval = 1f;
        public float startDelay = 1f;

        [Range(0, 180f)]
        public float emitterAngle = 0f;
        [Range(0, 180f)]
        public float listenerAngle = 0f;

        private int _currentStep;
        private VRCPlayerApi _audioListener;
        public BetterAudioSource betterAudioSource;

        private AudioSource _audioSourceProxy;

        private void InitializeTest()
        {
            _audioListener = Networking.LocalPlayer;
            if (!Assert(Utilities.IsValid(_audioListener), "Local player is invalid"))
            {
                testController.TestInitialized(false);
                return;
            }

            if (!Assert(Utilities.IsValid(betterAudioSource), "betterAudioSource is invalid"))
            {
                testController.TestInitialized(false);
                return;
            }

            _audioSourceProxy = betterAudioSource.GetAudioSourceProxy();
            if (!Assert(Utilities.IsValid(_audioSourceProxy), "audioSourceProxy is invalid"))
            {
                testController.TestInitialized(false);
                return;
            }

            _audioSourceProxy.loop = true;

            // ensure both players can't move around
            _audioListener.Immobilize(true);

            _currentStep = 0;
            EmitterTeleportInFrontOfListener(_currentStep);
            betterAudioSource.Play(false);

            testController.TestInitialized(true);
        }

        private void EmitterTeleportInFrontOfListener(int step)
        {
            var forward = (_audioListener.GetRotation() * Quaternion.Euler(0, listenerAngle, 0)) * Vector3.forward;
            var positionOffset = step * stepSize * forward;
            var teleportPosition = _audioListener.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position +
                                   positionOffset;
            betterAudioSource.transform.SetPositionAndRotation(teleportPosition,
                Quaternion.LookRotation(-forward, Vector3.up) * Quaternion.Euler(0, emitterAngle, 0));
        }

        private void RunTest()
        {
            SendCustomEventDelayedSeconds("PerformStep", startDelay, EventTiming.LateUpdate);
        }

        public void PerformStep()
        {
            if (!Assert(Utilities.IsValid(this) || !gameObject.activeInHierarchy, "This component is no longer valid"))
            {
                testController.TestCompleted(false);
            }

            if (!Assert(Utilities.IsValid(betterAudioSource), "Emitting betterAudioSource is invalid"))
            {
                testController.TestCompleted(false);
                return;
            }

            if (!Assert(Utilities.IsValid(_audioListener), "Voice listening player is invalid"))
            {
                testController.TestCompleted(false);
                return;
            }

            Debug.Log(
                $"[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Teleporting local player to sample position {_currentStep}",
                this);
            EmitterTeleportInFrontOfListener(_currentStep);

            _currentStep++;
            if (_currentStep < samples)
            {
                SendCustomEventDelayedSeconds("PerformStep", stepInterval, EventTiming.LateUpdate);
            }
            else
            {
                testController.TestCompleted(true);
            }
        }

        private void CleanUpTest()
        {
            if (!Assert(Utilities.IsValid(this) || !gameObject.activeInHierarchy, "This component is no longer valid"))
            {
                testController.TestCleanedUp(false);
            }

            if (!Assert(Utilities.IsValid(betterAudioSource), "Emitting betterAudioSource is invalid"))
            {
                testController.TestCleanedUp(false);
                return;
            }
            betterAudioSource.Stop();

            if (!Assert(Utilities.IsValid(_audioListener), "Voice listening player is invalid"))
            {
                testController.TestCleanedUp(false);
                return;
            }

            EmitterTeleportInFrontOfListener(0);

            _audioListener.Immobilize(false);
            testController.TestCleanedUp(true);
        }

        #endregion
    }
}