using UnityEngine;

namespace ARSpeedHUD.Hud
{
    /// <summary>
    /// 월드 또는 머리에 고정된 HUD 요소가 매 프레임 목표 자세로 즉시 이동하지 않고
    /// 부드럽게 따라가도록 한다. 잡음이 있는 머리 추적 데이터에 HUD 요소를 즉시 다시
    /// 고정하면 AR/VR 멀미를 유발하는 전정 감각 불일치가 발생하기 쉽다. 대신 여러
    /// 프레임에 걸쳐 Lerp/Slerp로 목표에 접근한다.
    ///
    /// 머리가 거의 정지해 있어도 문자 같은 세부 요소가 눈에 띄게 "떨리는" 두 가지
    /// 현상을 의도적으로 방지한다.
    /// - 프레임마다 고정 Lerp 계수를 쓰는 방식(기존: 매 프레임
    ///   `Lerp(a, b, 0.15f)`)은 프레임률에 의존한다. 프레임 시간의 흔들림이 위치와
    ///   회전의 흔들림으로 그대로 나타난다. Time.deltaTime 기반 지수 감쇠를 사용하면
    ///   프레임률과 관계없이 일관되게 평활화할 수 있다.
    /// - "정지한" 머리 자세에서 센서 및 입력 잡음으로 생기는 밀리미터 또는 도 미만의
    ///   모든 변화를 따라가면 HUD가 안정 위치 주변에서 눈에 띄게 배회한다. 작은
    ///   데드존을 두어 임계값 아래의 움직임은 평활화하지 않고 무시한다.
    ///
    /// 이 스크립트는 의도적으로 XREAL에 종속되지 않으며
    /// <see cref="headTransform"/>에 할당한 Transform을 따라갈 뿐이다. XREAL SDK
    /// 패키지를 가져온 뒤에는 이 필드를 SDK가 제공하는 머리 자세 Transform에 연결한다.
    /// SDK의 카메라 리그 프리팹을 참고한다. 그전에는 Unity XR Device Simulator의
    /// 가상 카메라에 연결해 에디터에서 평활화 느낌을 테스트할 수 있다. README.md를
    /// 참고한다.
    /// </summary>
    public class GazeAnchorSmoother : MonoBehaviour
    {
        [SerializeField] private Transform headTransform;
        [SerializeField] private Vector3 anchorOffset = new Vector3(0f, -0.15f, 2f);

        [Header("Smoothing (higher = snappier, lower = smoother)")]
        [SerializeField, Range(0.5f, 20f)] private float positionSmoothRate = 10f;
        [SerializeField, Range(0.5f, 20f)] private float rotationSmoothRate = 10f;

        [Header("Dead zone (ignore movement below this -- kills sensor-noise jitter)")]
        [SerializeField] private float positionDeadZoneMeters = 0.002f;
        [SerializeField] private float rotationDeadZoneDegrees = 0.15f;

        private void LateUpdate()
        {
            if (headTransform == null) return;

            Vector3 targetPosition = headTransform.TransformPoint(anchorOffset);
            if (Vector3.Distance(transform.position, targetPosition) > positionDeadZoneMeters)
            {
                float positionT = 1f - Mathf.Exp(-positionSmoothRate * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, targetPosition, positionT);
            }

            // UI Canvas에서 읽을 수 있는 앞면은 로컬 -Z 방향이다. 따라서 +Z 방향을
            // 바라보는 기본 Unity 카메라에 기본 회전 Canvas가 올바르게 보인다.
            // HUD 앞면이 사용자를 향하게 하려면 HUD의 정방향(+Z)은 사용자 반대쪽을
            // 가리켜야 한다. 에디터에서 반대 방향으로 설정하면 HUD를 뒷면을 통해
            // 보게 되어 읽을 수는 있지만 좌우가 뒤집히는 것을 확인했다.
            Vector3 lookDirection = transform.position - headTransform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                if (Quaternion.Angle(transform.rotation, targetRotation) > rotationDeadZoneDegrees)
                {
                    float rotationT = 1f - Mathf.Exp(-rotationSmoothRate * Time.deltaTime);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationT);
                }
            }
        }
    }
}
