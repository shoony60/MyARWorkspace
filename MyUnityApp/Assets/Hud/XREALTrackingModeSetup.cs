using UnityEngine;
#if PLATFORM_ANDROID && !UNITY_EDITOR
using Unity.XR.XREAL;
#endif

namespace ARSpeedHUD.Hud
{
    /// <summary>
    /// 시작 시 글래스를 위치 SLAM 없이 회전만 추적하는 3DoF 모드로 전환한다. 이 HUD는
    /// 머리에 고정되어 있어 공간 규모의 6DoF 추적이 아니라 머리 회전만 필요하다.
    /// 6DoF의 SLAM에는 환경의 시각적 특징점이 충분해야 하지만 야외 러닝 환경은
    /// 모션 블러, 단조로운 잔디와 포장도로, 밝은 햇빛 때문에 추적이 불안정해져
    /// "Feature point not enough" 대화상자가 나타난다. 3DoF는 SLAM에 의존하지 않으므로
    /// 이 경고를 완전히 피한다.
    /// </summary>
    public class XREALTrackingModeSetup : MonoBehaviour
    {
        private void Start()
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            _ = XREALPlugin.SwitchTrackingTypeAsync(TrackingType.MODE_3DOF, OnTrackingTypeChanged);
#endif
        }

#if PLATFORM_ANDROID && !UNITY_EDITOR
        private void OnTrackingTypeChanged(bool result, TrackingType targetTrackingType)
        {
            Debug.Log($"[XREAL] SwitchTrackingType -> {targetTrackingType}, success={result}");
        }
#endif
    }
}
