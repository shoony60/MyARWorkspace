using System.Globalization;
using ARSpeedHUD.Location;
using TMPro;
using UnityEngine;

namespace ARSpeedHUD.Hud
{
    /// <summary>
    /// HudState를 여러 TextMeshPro 레이블에 렌더링하는 Unity 측 HudScreen.kt
    /// 대응 구현이다. 두 앱이 동일하게 보이도록 페이스, 거리, 경과 시간 서식 로직을
    /// Kotlin 버전과 맞췄다. Text 필드는 구축한 Canvas 레이아웃에 연결한다. XREAL SDK
    /// 카메라 리그가 장면에 있으면 월드 공간 Canvas를, SimulatedLocationSource를
    /// 사용하는 에디터 전용 테스트에는 화면 공간 Canvas를 사용한다.
    /// </summary>
    public class HudPresenter : MonoBehaviour
    {
        [SerializeField] private RunSessionController session;
        [SerializeField] private TMP_Text sourceBadgeText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text paceText;
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text elapsedText;

        private void Awake()
        {
            foreach (var text in new[] { sourceBadgeText, speedText, paceText, distanceText, elapsedText })
            {
                if (text != null) text.color = HudColors.Green;
            }
        }

        private void OnEnable()
        {
            session.StateChanged += Render;
            // Render immediately on enable too, not just on the next state
            // change, so the placeholder TMP text never shows before the
            // first sample arrives.
            Render(session.State);
        }

        private void OnDisable() => session.StateChanged -= Render;

        private void Render(HudState state)
        {
            sourceBadgeText.text = !state.IsTracking ? "STANDBY"
                : state.Source == LocationSourceType.Simulator ? "● SIMULATOR"
                : "● GPS LIVE";

            speedText.text = state.SpeedKmh.ToString("0.0", CultureInfo.InvariantCulture);
            paceText.text = "PACE  " + FormatPace(state.PaceMinPerKm);
            distanceText.text = "DIST  " + FormatDistance(state.DistanceMeters);
            elapsedText.text = "TIME  " + FormatElapsed(state.ElapsedSeconds);
        }

        private static string FormatPace(double? paceMinPerKm)
        {
            if (!paceMinPerKm.HasValue || double.IsInfinity(paceMinPerKm.Value) || double.IsNaN(paceMinPerKm.Value))
                return "--:--";
            int totalSeconds = (int)(paceMinPerKm.Value * 60);
            return $"{totalSeconds / 60}:{totalSeconds % 60:00} /km";
        }

        private static string FormatDistance(double meters) =>
            meters < 1000
                ? meters.ToString("0", CultureInfo.InvariantCulture) + " m"
                : (meters / 1000.0).ToString("0.00", CultureInfo.InvariantCulture) + " km";

        private static string FormatElapsed(long totalSeconds)
        {
            long h = totalSeconds / 3600;
            long m = (totalSeconds % 3600) / 60;
            long s = totalSeconds % 60;
            return h > 0 ? $"{h}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
        }
    }
}
