using System;
using System.Collections;
using UnityEngine;

namespace ARSpeedHUD.Location
{
    /// <summary>
    /// 네이티브 Android 앱의 SimulatedLocationSource.kt를 C#으로 이식한 버전이다.
    /// 실제 하드웨어, GPS, XREAL 글래스 없이도 러너의 물리적으로 그럴듯한 GPS 위치
    /// 정보 스트림을 생성한다. 준비 구간 동안 속도가 증가한 뒤 느린 사인파 변동과
    /// 잡음을 더해 조깅 페이스 주변을 오가며, 진행 방향도 무작위로 변하게 해
    /// 구불구불한 러닝 경로를 흉내 낸다. Unity 에디터 안에서 세션과 HUD 로직을
    /// 완전히 구축하고 조정할 수 있게 해 준다.
    /// </summary>
    public class SimulatedLocationSource : MonoBehaviour, ILocationSource
    {
        public LocationSourceType Type => LocationSourceType.Simulator;
        public event Action<RunSample> SampleReceived;

        [SerializeField] private double startLat = 37.5665;
        [SerializeField] private double startLon = 126.9780;
        [SerializeField] private float tickSeconds = 0.5f;
        [SerializeField] private double basePaceMps = 2.6; // 약 9.4km/h의 가벼운 조깅

        private Coroutine _routine;

        public void StartSampling()
        {
            StopSampling();
            _routine = StartCoroutine(Sample());
        }

        public void StopSampling()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
        }

        private IEnumerator Sample()
        {
            double lat = startLat;
            double lon = startLon;
            double bearingDeg = UnityEngine.Random.Range(0f, 360f);
            double elapsed = 0;
            const double earthRadius = 6371000.0;

            while (true)
            {
                elapsed += tickSeconds;

                double warmup = Math.Min(elapsed / 30.0, 1.0);
                double drift = Math.Sin(elapsed / 45.0) * 0.5;
                double noise = UnityEngine.Random.Range(-0.15f, 0.15f);
                double speed = Math.Min(Math.Max(basePaceMps * warmup + drift + noise, 0.0), 5.5);

                bearingDeg = (bearingDeg + UnityEngine.Random.Range(-6f, 6f) + 360.0) % 360.0;

                double distanceMeters = speed * tickSeconds;
                double bearingRad = bearingDeg * Math.PI / 180.0;
                lat += (distanceMeters * Math.Cos(bearingRad) / earthRadius) * (180.0 / Math.PI);
                lon += (distanceMeters * Math.Sin(bearingRad) / (earthRadius * Math.Cos(lat * Math.PI / 180.0))) * (180.0 / Math.PI);

                SampleReceived?.Invoke(new RunSample
                {
                    TimestampMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Latitude = lat,
                    Longitude = lon,
                    SpeedMps = (float)speed,
                    AccuracyMeters = 3.5f
                });

                yield return new WaitForSeconds(tickSeconds);
            }
        }
    }
}
