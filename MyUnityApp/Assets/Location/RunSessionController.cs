using System;
using UnityEngine;

namespace ARSpeedHUD.Location
{
    /// <summary>
    /// RunSessionViewModel.kt의 C# 이식 버전이다. 원시 GPS 속도를 지수 이동 평균으로
    /// 평활화하고 하버사인 방식으로 거리를 누적하며 페이스를 계산한다. 순간 GPS 원시
    /// 속도에는 잡음이 있으므로 이 평활화 과정이 없으면 샘플마다 HUD 숫자가 흔들린다.
    /// </summary>
    public class RunSessionController : MonoBehaviour
    {
        public event Action<HudState> StateChanged;

        private const double SmoothingAlpha = 0.25;
        private const double MinPaceSpeedMps = 0.3; // 이보다 느리면 사실상 정지 상태이므로 페이스가 의미 없다.
        private const double StaleSampleThresholdSeconds = 3.0; // 이 시간 동안 새 위치 정보가 없으면 속도를 0으로 강제한다.
        private double _lastSampleReceivedAtSeconds;

        private ILocationSource _source;
        private double? _smoothedSpeedMps;
        private double _totalDistanceMeters;
        private double _startTimeSeconds;
        private RunSample? _lastSample;
        private LocationSourceType _currentSourceType = LocationSourceType.Simulator;
        private HudState _state;

        public HudState State => _state;

        /// <summary>추적 세션을 시작하지 않고 선택된 위치 정보 소스만 변경한다.</summary>
        public void SetPreferredSource(LocationSourceType type)
        {
            _currentSourceType = type;
            _state.Source = type;
            StateChanged?.Invoke(_state);
        }

        public void StartSession(ILocationSource source)
        {
            StopSession();
            _source = source;
            _smoothedSpeedMps = null;
            _totalDistanceMeters = 0;
            _lastSample = null;
            _currentSourceType = source.Type;
            _startTimeSeconds = Time.realtimeSinceStartupAsDouble;
            _lastSampleReceivedAtSeconds = _startTimeSeconds;

            _state = new HudState { Source = source.Type, IsTracking = true };
            StateChanged?.Invoke(_state);

            source.SampleReceived += OnSample;
            source.StartSampling();
        }

        private void Update()
        {
            if (!_state.IsTracking) return;

            // 이전에는 경과 시간을 OnSample() 안에서만 다시 계산해 GPS 위치 정보 사이의
            // 공백 동안 일반 스톱워치처럼 흐르지 않고 멈춘 것처럼 보였다. 새 샘플 도착
            // 여부와 관계없이 여기서 매 프레임 갱신한다.
            long elapsedSeconds = (long)(Time.realtimeSinceStartupAsDouble - _startTimeSeconds);
            if (elapsedSeconds != _state.ElapsedSeconds)
            {
                _state.ElapsedSeconds = elapsedSeconds;
                StateChanged?.Invoke(_state);
            }

            // GPS 공급자는 정지 상태에서 절전이나 위치 상실로 조용해질 수 있으며,
            // 속도를 다시 낮출 새 샘플이 오지 않을 수도 있다. 이 감시가 없으면 러너가
            // 멈춘 뒤에도 마지막 0이 아닌 속도가 화면에 계속 남는다.
            if (_smoothedSpeedMps.HasValue && _smoothedSpeedMps.Value != 0)
            {
                double staleFor = Time.realtimeSinceStartupAsDouble - _lastSampleReceivedAtSeconds;
                if (staleFor >= StaleSampleThresholdSeconds)
                {
                    _smoothedSpeedMps = 0;
                    _state.SpeedKmh = 0;
                    _state.PaceMinPerKm = null;
                    StateChanged?.Invoke(_state);
                }
            }
        }

        public void StopSession()
        {
            if (_source != null)
            {
                _source.SampleReceived -= OnSample;
                _source.StopSampling();
                _source = null;
            }
            if (_state.IsTracking)
            {
                _state.IsTracking = false;
                StateChanged?.Invoke(_state);
            }
        }

        private void OnSample(RunSample sample)
        {
            _lastSampleReceivedAtSeconds = Time.realtimeSinceStartupAsDouble;
            if (_lastSample.HasValue)
            {
                _totalDistanceMeters += HaversineMeters(
                    _lastSample.Value.Latitude, _lastSample.Value.Longitude,
                    sample.Latitude, sample.Longitude);
            }
            _lastSample = sample;

            _smoothedSpeedMps = _smoothedSpeedMps.HasValue
                ? SmoothingAlpha * sample.SpeedMps + (1 - SmoothingAlpha) * _smoothedSpeedMps.Value
                : sample.SpeedMps;

            double speedKmh = _smoothedSpeedMps.Value * 3.6;
            double? pace = _smoothedSpeedMps.Value > MinPaceSpeedMps
                ? (1000.0 / _smoothedSpeedMps.Value) / 60.0
                : (double?)null;
            long elapsedSeconds = (long)(Time.realtimeSinceStartupAsDouble - _startTimeSeconds);

            _state = new HudState
            {
                SpeedKmh = speedKmh,
                PaceMinPerKm = pace,
                DistanceMeters = _totalDistanceMeters,
                ElapsedSeconds = elapsedSeconds,
                Source = _currentSourceType,
                GpsAccuracy = sample.AccuracyMeters >= 0f ? sample.AccuracyMeters : (float?)null,
                IsTracking = true
            };
            StateChanged?.Invoke(_state);
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double r = 6371000.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return r * c;
        }
    }
}
