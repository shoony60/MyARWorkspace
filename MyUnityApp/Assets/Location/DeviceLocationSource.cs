using System;
using System.Collections;
using UnityEngine;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

namespace ARSpeedHUD.Location
{
    /// <summary>
    /// Unity의 크로스 플랫폼 Input.location을 통해 실제 GPS를 사용한다. 네이티브 앱의
    /// DeviceLocationSource.kt에서 사용하는 Android FusedLocationProviderClient와 달리
    /// Unity의 LocationInfo에는 속도 필드가 없다. 자세한 내용은
    /// docs.unity3d.com/ScriptReference/LocationInfo.html을 참고한다. 따라서 여기서는
    /// RunSessionController가 거리를 누적할 때와 같은 방식으로 연속 위치 정보의
    /// 하버사인 거리와 시간 차를 이용해 속도를 계산한다. 이 방식은 실제 도플러 기반
    /// GPS 속도보다 정확도가 눈에 띄게 낮다. 특히 약 1초 주기로 러닝 속도를 측정하면
    /// 위치 오차가 샘플 사이의 실제 이동 거리와 비슷해 속도 값이 크게 흔들린다.
    ///
    /// Android에서는 DopplerLocationSource.cs를 우선 사용한다. 이 구현은
    /// AndroidJavaObject/AndroidJavaProxy로 Android 네이티브 LocationManager를 직접
    /// 호출해 네이티브 Kotlin 앱과 같은 도플러 기반 Location.getSpeed()를 얻으므로
    /// 위 제한을 완전히 우회한다. 이 클래스는 단순한 비 Android 전용 대체 구현으로만
    /// 유지한다.
    /// </summary>
    public class DeviceLocationSource : MonoBehaviour, ILocationSource
    {
        public LocationSourceType Type => LocationSourceType.DeviceGps;
        public event Action<RunSample> SampleReceived;

        private Coroutine _routine;
        private LocationInfo? _previous;

        public void StartSampling()
        {
            StopSampling();
            _routine = StartCoroutine(Sample());
        }

        public void StopSampling()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _previous = null;
            Input.location.Stop();
        }

        private IEnumerator Sample()
        {
#if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
            }
#endif
            if (!Input.location.isEnabledByUser)
            {
                yield break;
            }

            Input.location.Start(desiredAccuracyInMeters: 5f, updateDistanceInMeters: 1f);

            int maxWaitSeconds = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWaitSeconds > 0)
            {
                yield return new WaitForSeconds(1f);
                maxWaitSeconds--;
            }

            if (Input.location.status != LocationServiceStatus.Running)
            {
                yield break;
            }

            while (true)
            {
                var current = Input.location.lastData;
                float speedMps = 0f;

                if (_previous.HasValue && current.timestamp > _previous.Value.timestamp)
                {
                    double dtSeconds = current.timestamp - _previous.Value.timestamp;
                    double distanceMeters = HaversineMeters(
                        _previous.Value.latitude, _previous.Value.longitude,
                        current.latitude, current.longitude);
                    speedMps = dtSeconds > 0 ? (float)(distanceMeters / dtSeconds) : 0f;
                }

                SampleReceived?.Invoke(new RunSample
                {
                    TimestampMillis = (long)(current.timestamp * 1000),
                    Latitude = current.latitude,
                    Longitude = current.longitude,
                    SpeedMps = speedMps,
                    AccuracyMeters = current.horizontalAccuracy
                });

                _previous = current;
                yield return new WaitForSeconds(1f);
            }
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
