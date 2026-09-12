using UnityEngine;
#if PLATFORM_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace ARSpeedHUD.Location
{
    /// <summary>
    /// 네이티브 LocationForegroundService를 시작하고 중지해 휴대전화 화면이 꺼진 뒤에도
    /// GPS 업데이트를 유지한다. 구현은 Assets/Plugins/Android/src/main/java/.../
    /// LocationForegroundService.java를 참고한다. ACCESS_FINE_LOCATION과 별도로
    /// ACCESS_BACKGROUND_LOCATION 권한이 필요하다. Android 11 이상에서는 Google이
    /// 직접 권한 요청을 제한하므로 일반적으로 인라인 대화상자 대신 설정 화면으로
    /// 이동한다. 또한 Android 13 이상에서는 위치 유형 포그라운드 서비스가 실행 중
    /// 표시해야 하는 지속적인 "추적" 알림을 위해 POST_NOTIFICATIONS 권한이 필요하다.
    /// </summary>
    public static class LocationForegroundServiceBridge
    {
        private const string ServiceClassName = "com.arspeedhud.xr.location.LocationForegroundService";
        private const string BackgroundLocationPermission = "android.permission.ACCESS_BACKGROUND_LOCATION";
        private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";

        public static void EnsurePermissionsThenStart()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(BackgroundLocationPermission))
            {
                Debug.Log("[LocationForegroundServiceBridge] Requesting ACCESS_BACKGROUND_LOCATION");
                Permission.RequestUserPermission(BackgroundLocationPermission);
            }
            if (!Permission.HasUserAuthorizedPermission(PostNotificationsPermission))
            {
                Debug.Log("[LocationForegroundServiceBridge] Requesting POST_NOTIFICATIONS");
                Permission.RequestUserPermission(PostNotificationsPermission);
            }
            Start();
#endif
        }

        public static void Start()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            try
            {
                using var activity = CurrentActivity();
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setClassName", activity, ServiceClassName);

                if (AndroidSdkInt() >= 26)
                {
                    activity.Call<AndroidJavaObject>("startForegroundService", intent);
                }
                else
                {
                    activity.Call<AndroidJavaObject>("startService", intent);
                }
                Debug.Log("[LocationForegroundServiceBridge] LocationForegroundService started");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocationForegroundServiceBridge] Failed to start service: {e}");
            }
#endif
        }

        public static void Stop()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            try
            {
                using var activity = CurrentActivity();
                using var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setClassName", activity, ServiceClassName);
                activity.Call<bool>("stopService", intent);
                Debug.Log("[LocationForegroundServiceBridge] LocationForegroundService stopped");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LocationForegroundServiceBridge] Failed to stop service: {e}");
            }
#endif
        }

#if PLATFORM_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject CurrentActivity()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        private static int AndroidSdkInt()
        {
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            return version.GetStatic<int>("SDK_INT");
        }
#endif
    }
}
