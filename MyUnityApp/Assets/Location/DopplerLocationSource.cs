using System;
using System.Collections.Concurrent;
using UnityEngine;
#if PLATFORM_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace ARSpeedHUD.Location
{
    /// <summary>
    /// Unity의 AndroidJavaObject/AndroidJavaProxy 상호 운용으로 Android 네이티브
    /// LocationManager를 직접 호출해 실제 GPS를 사용한다. 속도 필드가 없는 Unity의
    /// Input.location LocationInfo를 우회한다. DeviceLocationSource.cs를 참고한다.
    ///
    /// Android의 Location.getSpeed()는 두 위치 정보의 차이가 아니라 GNSS 도플러 편이
    /// 측정값에서 계산되므로 DeviceLocationSource.cs의 하버사인 기반 속도보다 훨씬
    /// 정확하다. 네이티브 Kotlin 앱의 DeviceLocationSource.kt가
    /// FusedLocationProviderClient를 통해 얻는 Location.getSpeed()와 같다. Play
    /// 서비스 대신 일반 Android LocationManager와 GPS 공급자를 사용하므로 Unity
    /// Android 빌드에 별도 의존성을 추가할 필요가 없다.
    ///
    /// Android 빌드에서는 DeviceLocationSource.cs보다 이 구현을 우선 사용한다.
    /// DeviceLocationSource는 더 단순한 비 Android용 크로스 플랫폼 대체 구현이다.
    ///
    /// Android 권한 처리: 네이티브 앱의 MainActivity가 시작 시 처리하는 것처럼 장면
    /// 부트스트랩 스크립트 등에서 ACCESS_FINE_LOCATION을 미리 한 번 요청한다. 권한이
    /// 승인되기 전에 StartSampling()을 호출하면 권한만 요청하고 시작하지 않은 채
    /// 반환하며, 자동으로 다시 시도하지 않는다.
    ///
    /// Android 위치 콜백은 Unity 메인 스레드가 아닌 백그라운드 JNI 스레드에서 도착한다.
    /// 따라서 원시 샘플을 큐에 넣고 Update()에서 꺼낸 뒤 SampleReceived를 발생시킨다.
    ///
    /// 에디터/XR Device Simulator 참고: 이 클래스는 실제 Android 기기에서만 실행된다.
    /// 단순히 "#if PLATFORM_ANDROID"가 아니라 "#if PLATFORM_ANDROID &&
    /// !UNITY_EDITOR"로 보호한다. Android가 활성 빌드 대상이면 에디터 재생 모드에서도
    /// PLATFORM_ANDROID가 정의되지만, 에디터 프로세스에는 통신할 실제 JNI/Android
    /// 런타임이 없어 AndroidJavaObject/AndroidJavaClass 호출 시 예외가 발생한다.
    /// 모든 에디터/XR Device Simulator 테스트에는 SimulatedLocationSource를 사용한다.
    /// DopplerLocationSource는 실제 휴대전화에서 빌드하고 실행할 때만 사용한다.
    /// </summary>
    public class DopplerLocationSource : MonoBehaviour, ILocationSource
    {
        public LocationSourceType Type => LocationSourceType.DeviceGps;
        public event Action<RunSample> SampleReceived;

        private const long MinTimeMillis = 1000L;
        private const float MinDistanceMeters = 1f;
        private const string GpsProvider = "gps"; // == android.location.LocationManager.GPS_PROVIDER

        private AndroidJavaObject _locationManager;
        private LocationListenerProxy _listener;
        private readonly ConcurrentQueue<RunSample> _pending = new ConcurrentQueue<RunSample>();
        private bool _isRunning;

        public void StartSampling()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
                return;
            }
 
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                _locationManager = activity.Call<AndroidJavaObject>("getSystemService", "location");
 
                _listener = new LocationListenerProxy(OnRawLocation);
 
                using var looperClass = new AndroidJavaClass("android.os.Looper");
                using var mainLooper = looperClass.CallStatic<AndroidJavaObject>("getMainLooper");
 
                _locationManager.Call(
                    "requestLocationUpdates",
                    GpsProvider,
                    MinTimeMillis,
                    MinDistanceMeters,
                    _listener,
                    mainLooper);
 
                _isRunning = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DopplerLocationSource] StartSampling failed: {e}");
            }
#else
            Debug.LogWarning("DopplerLocationSource only works in an Android build -- use SimulatedLocationSource in the Editor.");
#endif
        }

        public void StopSampling()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            if (_isRunning && _locationManager != null && _listener != null)
            {
                _locationManager.Call("removeUpdates", _listener);
            }
#endif
            _isRunning = false;
            _locationManager = null;
            _listener = null;
            while (_pending.TryDequeue(out _)) { }
        }

        private void Update()
        {
            while (_pending.TryDequeue(out var sample))
            {
                SampleReceived?.Invoke(sample);
            }
        }

        private void OnDestroy() => StopSampling();

        private void OnRawLocation(AndroidJavaObject location)
        {
            bool hasSpeed = location.Call<bool>("hasSpeed");
            bool hasAccuracy = location.Call<bool>("hasAccuracy");

            _pending.Enqueue(new RunSample
            {
                TimestampMillis = location.Call<long>("getTime"),
                Latitude = location.Call<double>("getLatitude"),
                Longitude = location.Call<double>("getLongitude"),
                SpeedMps = hasSpeed ? location.Call<float>("getSpeed") : 0f,
                AccuracyMeters = hasAccuracy ? location.Call<float>("getAccuracy") : -1f
            });

            location.Dispose();
        }

        /// <summary>
        /// Java android.location.LocationListener를 대신하는 C# 구현이다. AndroidJavaProxy는
        /// 이름으로 Java 콜백을 전달하므로 메서드 이름이 Java 인터페이스와 정확히 같아야 한다.
        ///
        /// Android 12(API 31)부터 LocationListener에는
        /// onLocationChanged(List&lt;Location&gt;)도 선언된다. 일부 OS 버전(Android 16에서
        /// 확인)에서는 일반 onLocationChanged(Location) 대신 이 오버로드를 호출한다.
        /// AndroidJavaProxy는 메서드 이름만으로 전달하므로 하나의 "onLocationChanged"
        /// 메서드가 시스템이 실제로 호출한 값을 받는다. 때로는 Location이며, 때로는
        /// List&lt;Location&gt; 래퍼다. 후자는 원시 java.util.ArrayList로 관찰됐고 여기에
        /// 직접 hasSpeed()를 호출하면 NoSuchMethodError가 발생했다. 클래스 이름으로
        /// 받은 형식을 판별하고 List인 경우 가장 최근 항목을 꺼낸다.
        /// </summary>
        private class LocationListenerProxy : AndroidJavaProxy
        {
            private readonly Action<AndroidJavaObject> _onLocationChanged;

            public LocationListenerProxy(Action<AndroidJavaObject> onLocationChanged)
                : base("android.location.LocationListener")
            {
                _onLocationChanged = onLocationChanged;
            }

            public void onLocationChanged(AndroidJavaObject locationOrList)
            {
                using var javaClass = locationOrList.Call<AndroidJavaObject>("getClass");
                string className = javaClass.Call<string>("getName");
                if (className == "android.location.Location")
                {
                    _onLocationChanged(locationOrList);
                    return;
                }

                int size = locationOrList.Call<int>("size");
                if (size == 0) return;
                using var last = locationOrList.Call<AndroidJavaObject>("get", size - 1);
                _onLocationChanged(last);
            }

            public void onStatusChanged(string provider, int status, AndroidJavaObject extras) { }
            public void onProviderEnabled(string provider) { }
            public void onProviderDisabled(string provider) { }
        }
    }
}
