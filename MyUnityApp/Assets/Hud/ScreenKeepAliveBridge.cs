using UnityEngine;

namespace ARSpeedHUD.Hud
{
    /// <summary>
    /// 러닝 중 휴대전화 화면을 최저 밝기로 켜 둔다. GPS 자체는 화면이 꺼져도 유지되지만
    /// LocationForegroundServiceBridge를 참고한다. 글래스 미러링 및 렌더링 출력은
    /// 유지되지 않는다. 실기기에서 휴대전화 화면이 잠기는 순간 XREAL 화면도 검게
    /// 꺼지는 것을 확인했으며, 네이티브 앱의 일반 DisplayPort 미러링과 같은 동작이다.
    /// Screen.sleepTimeout만 사용하면 OS의 화면 어두워짐과 잠금은 막지만 밝기는 낮추지
    /// 않는다. 따라서 화면을 켜 둔 동안 배터리 소모와 눈부심을 줄이기 위해 네이티브
    /// WindowManager.LayoutParams.screenBrightness 필드를 직접 설정한다.
    /// </summary>
    public static class ScreenKeepAliveBridge
    {
        private const float MinimumBrightness = 0.01f;

        public static void KeepAliveDimmed()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if PLATFORM_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using var window = activity.Call<AndroidJavaObject>("getWindow");
                    using var attributes = window.Call<AndroidJavaObject>("getAttributes");
                    attributes.Set("screenBrightness", MinimumBrightness);
                    window.Call("setAttributes", attributes);
                }));
                Debug.Log("[ScreenKeepAliveBridge] Screen kept on, brightness dimmed to minimum");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScreenKeepAliveBridge] Failed to dim screen brightness: {e}");
            }
#endif
        }
    }
}
