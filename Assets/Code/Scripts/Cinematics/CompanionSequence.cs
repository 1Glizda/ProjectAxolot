using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Cinemachine;
using UnityEngine.Playables;
using UnityEngine.Events;

namespace Cinematics
{
    public enum EasingType
    {
        Linear,
        EaseInOut,
        EaseIn,
        EaseOut
    }

    [Serializable]
    public struct CompanionKeyframe
    {
        [Tooltip("Time in seconds for this keyframe")]
        public float Time;
        
        [Tooltip("Position along the spline (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float SplinePosition;

        [Tooltip("Easing applied when transitioning TO the next keyframe")]
        public EasingType Easing;
        
        [Tooltip("Cinemachine Camera Priority starting at this keyframe")]
        public int CameraPriority;
    }

    public class CompanionSequence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SplineAnimate _splineAnimate;
        [SerializeField] private CinemachineCamera _cinemachineCamera;

        [Header("Sequence")]
        [Tooltip("List of keyframes. They will be automatically sorted by Time.")]
        [SerializeField] private List<CompanionKeyframe> _keyframes = new List<CompanionKeyframe>();
        
        [Header("Playback")]
        [SerializeField] private bool _playOnAwake = true;
        [SerializeField] private bool _loop = false;
        
        [Header("Timeline Integration")]
        [Tooltip("The Timeline to play. Leave empty if none.")]
        [SerializeField] private PlayableDirector _timeline;
        [Tooltip("Time in seconds to trigger the Timeline. Use -1 to disable.")]
        [SerializeField] private float _timelineTriggerTime = -1f;
        
        [Header("Events")]
        public UnityEvent OnStart;
        public UnityEvent OnEnd;

        private float _currentTime = 0f;
        private bool _isPlaying = false;
        private bool _timelinePlayed = false;

        private void Start()
        {
            if (_playOnAwake)
            {
                Play();
            }
            else
            {
                // Ensure SplineAnimate doesn't start playing on its own
                if (_splineAnimate != null)
                {
                    _splineAnimate.Pause();
                }

                // Snap to the first keyframe's state immediately
                if (_keyframes != null && _keyframes.Count > 0)
                {
                    _keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
                    Evaluate(0f);
                }
            }
        }

        public void Play()
        {
            _currentTime = 0f;
            _isPlaying = true;
            _timelinePlayed = false;

            OnStart?.Invoke();

            // Take manual control over the spline animation
            if (_splineAnimate != null)
            {
                _splineAnimate.Pause(); 
            }

            // Ensure keyframes are sorted chronologically
            if (_keyframes != null && _keyframes.Count > 0)
            {
                _keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
            }
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        private void Update()
        {
            if (!_isPlaying || _keyframes == null || _keyframes.Count == 0) return;

            _currentTime += Time.deltaTime;

            // Trigger timeline if configured
            if (!_timelinePlayed && _timeline != null && _timelineTriggerTime >= 0f && _currentTime >= _timelineTriggerTime)
            {
                _timelinePlayed = true;
                _timeline.Play();
            }

            float maxTime = _keyframes[_keyframes.Count - 1].Time;

            if (_currentTime >= maxTime)
            {
                if (_loop)
                {
                    _currentTime %= maxTime;
                    _timelinePlayed = false; // Reset timeline trigger for next loop
                }
                else
                {
                    _currentTime = maxTime;
                    _isPlaying = false; // end of sequence
                    OnEnd?.Invoke();
                }
            }

            Evaluate(_currentTime);
        }

        private void Evaluate(float t)
        {
            // Edge case: single keyframe
            if (_keyframes.Count == 1)
            {
                ApplyKeyframeValues(_keyframes[0].SplinePosition, _keyframes[0].CameraPriority);
                return;
            }

            // Find bracketing keyframes
            CompanionKeyframe prev = _keyframes[0];
            CompanionKeyframe next = _keyframes[_keyframes.Count - 1];

            foreach (var kf in _keyframes)
            {
                if (kf.Time <= t)
                {
                    prev = kf;
                }
            }

            for (int i = 0; i < _keyframes.Count; i++)
            {
                if (_keyframes[i].Time > t)
                {
                    next = _keyframes[i];
                    break;
                }
            }

            // Interpolate spline position smoothly
            float splinePos = prev.SplinePosition;
            if (next.Time > prev.Time)
            {
                float tRatio = (t - prev.Time) / (next.Time - prev.Time);
                
                // Apply easing
                switch (prev.Easing)
                {
                    case EasingType.EaseInOut:
                        tRatio = Mathf.SmoothStep(0f, 1f, tRatio);
                        break;
                    case EasingType.EaseIn:
                        tRatio = tRatio * tRatio;
                        break;
                    case EasingType.EaseOut:
                        tRatio = tRatio * (2f - tRatio);
                        break;
                }

                splinePos = Mathf.Lerp(prev.SplinePosition, next.SplinePosition, tRatio);
            }

            // Apply priority from the most recently passed keyframe (prev)
            // Cameras don't smoothly interpolate priority, they just snap to the new integer value.
            ApplyKeyframeValues(splinePos, prev.CameraPriority);
        }

        private void ApplyKeyframeValues(float splinePosition, int priority)
        {
            if (_splineAnimate != null)
            {
                _splineAnimate.NormalizedTime = splinePosition;
            }

            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Priority = priority;
            }
        }
    }
}
