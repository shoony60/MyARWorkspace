using UnityEngine;
using UnityEngine.XR;

namespace ARSpeedHUD.Hud
{
    /// <summary>
    /// XREAL 런타임이 값을 제공하지 않는 Input System 액션 바인딩(Tracked Pose Driver의
    /// "XRI Head" 액션)을 거치지 않고, 기존 InputTracking API를 통해 XRNode.Head를
    /// 직접 읽는 머리 자세 드라이버다. 실제 렌더링 카메라를 구동하므로 의도적으로
    /// 평활화하지 않는다. 카메라 자체를 평활화하거나 지연시키면 광학 투과 렌즈로 직접
    /// 보는 현실 세계에 비해 렌더링된 HUD가 떠다니는 것처럼 보인다. 이는 움직임-광자
    /// 지연 때문이다. 흔들림 필터링은 여기서 하지 않고 이 카메라를 따라가는 HUD
    /// 패널에서 처리한다. GazeAnchorSmoother의 데드존과 평활화를 참고한다.
    /// </summary>
    public class LegacyXRHeadPoseDriver : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text debugText;

        private void Update()
        {
            Vector3 pos = InputTracking.GetLocalPosition(XRNode.Head);
            Quaternion rot = InputTracking.GetLocalRotation(XRNode.Head);

            transform.localPosition = pos;
            transform.localRotation = rot;

            if (debugText != null)
            {
                debugText.text = $"HEAD {pos:F2} {rot.eulerAngles:F0}";
            }
        }
    }
}
