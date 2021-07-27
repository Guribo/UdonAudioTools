﻿using System;
using Guribo.UdonBetterAudio.Runtime;
using Guribo.UdonUtils.Runtime.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

namespace Guribo.UdonBetterAudio.Tests.Runtime.BetterAudio
{
    [DefaultExecutionOrder(110)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class BetterAudioSoundPlayDelayTest : UdonSharpBehaviour
    {
        #region DO NOT EDIT

        [NonSerialized] public TestController testController;

        public void Initialize()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Run: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!testController)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.CleanUp: invalid test controller",
                    this);
                return;
            }

            Debug.Log("[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test.CleanUp", this);
            CleanUpTest();
        }

        #endregion

        #region EDIT HERE

        [SerializeField] private BetterAudioSource betterAudioSource;
        private float _expectedDelay = 3f;
        private float _expectedPlayPosition = 1f;
        private readonly Vector3 _playPosition = new Vector3(3f * 343f, 0, 0);
        private bool _pendingCheck;
        private float _scheduledCheckTime;
        private float _maxDifference;

        private void InitializeTest()
        {
            if (!betterAudioSource)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Invalid better audio source",
                    this);
                testController.TestInitialized(false);
                return;
            }

            betterAudioSource.playOnEnable = false;
            betterAudioSource.playOffset = 0f;
            betterAudioSource.Stop();
            betterAudioSource.gameObject.SetActive(false);

            var localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                betterAudioSource.transform.position = localPlayer.GetPosition() + _playPosition;
            }
            else
            {
                betterAudioSource.transform.position = _playPosition;
            }


            _pendingCheck = false;

            // whenever the test is ready to be started call _betterAudioTestController.TestInitialized,
            // can be later in update or whenever but MUST be called at some point
            testController.TestInitialized(true);
        }

        private void RunTest()
        {
            var proxyAudioSource = betterAudioSource.GetAudioSourceProxy();
            if (!proxyAudioSource)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Better audio source has no audio source proxy",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (!proxyAudioSource.loop)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Proxy audio source must have loop set to true",
                    this);
                testController.TestInitialized(false);
                return;
            }

            if (proxyAudioSource.clip.length < 4f)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Test requires the audio clip to be at least 4 seconds long",
                    this);
                testController.TestCompleted(false);
                return;
            }

            betterAudioSource.Play(true);

            var actualAudioSource = betterAudioSource.GetActualAudioSource();
            if (!actualAudioSource)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Better audio source has no actual audio source and is thus not playing",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (actualAudioSource.isPlaying)
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Actual audio source is already playing",
                    this);
                testController.TestCompleted(false);
                return;
            }

            if (!betterAudioSource.IsPlaying())
            {
                Debug.LogError(
                    "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Better audio source is not saying it is playing",
                    this);
                testController.TestCompleted(false);
                return;
            }

            _pendingCheck = true;
            _maxDifference = Time.deltaTime;
            
            SendCustomEventDelayedSeconds("CheckIsPlaying", _expectedDelay + _expectedPlayPosition,EventTiming.LateUpdate);

            Debug.Log(
                "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Waiting for delayed checks",
                this);
        }

        private void CleanUpTest()
        {
            if (betterAudioSource)
            {
                betterAudioSource.Stop();
            }

            testController.TestCleanedUp(true);
        }

        public void CheckIsPlaying()
        {
            if (_pendingCheck)
            {
                _pendingCheck = false;

                var actualAudioSource = betterAudioSource.GetActualAudioSource();
                if (!actualAudioSource)
                {
                    Debug.LogError(
                        "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] No actual audio available",
                        this);
                    testController.TestCompleted(false);
                    return;
                }

                if (!actualAudioSource.isPlaying)
                {
                    Debug.LogError(
                        "[<color=#008000>BetterAudio</color>] [<color=#804500>Testing</color>] Actual audio source is not playing",
                        this);
                    testController.TestCompleted(false);
                    return;
                }

                var playedForExpectedDuration = Mathf.Abs(actualAudioSource.time - _expectedPlayPosition) <
                                                _maxDifference + Time.deltaTime;
                if (!playedForExpectedDuration)
                {
                    Debug.LogError(
                        $"Actual audio didn't play for the expected duration of {_expectedPlayPosition}, is {actualAudioSource.time}",
                        this);
                    testController.TestCompleted(false);
                    return;
                }

                testController.TestCompleted(true);
            }
        }

        #endregion
    }
}