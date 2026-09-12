using ARSpeedHUD.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if PLATFORM_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace ARSpeedHUD.Location
{
    /// <summary>
    /// DevBootstrap에 대응하는 실기기 구현이다. 실행 시 DopplerLocationSource를 사용하는
    /// 세션을 자동으로 시작하며, 필요하면 먼저 ACCESS_FINE_LOCATION 권한을 요청한다.
    /// 네이티브 앱 MainActivity.kt의 onToggleTracking과 같은 방식이다. 탭 입력 대신
    /// 자동 시작하는 것은 의도된 동작이다. 이 월드 공간 Canvas 버튼에는 아직 실기기
    /// 상호작용 경로(시선+클릭 또는 컨트롤러 바인딩)가 연결되지 않아, 자동 시작하지
    /// 않으면 글래스를 착용한 상태에서 접근할 수 없다. 나중에 UI 상호작용을 연결하면
    /// 이 버튼은 수동 중지/시작 전환 기능으로도 사용할 수 있다. 사용 전에 장면에서
    /// DevBootstrap을 제거하고 SimulatedLocationSource 대신 DopplerLocationSource에
    /// 연결한다.
    /// </summary>
    public class StartSessionButton : MonoBehaviour
    {
        [SerializeField] private RunSessionController session;
        [SerializeField] private DopplerLocationSource dopplerSource;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text buttonLabel;

        private bool _isTracking;

        private void OnEnable() => button.onClick.AddListener(OnClick);
        private void OnDisable() => button.onClick.RemoveListener(OnClick);

        private void Start()
        {
            Debug.Log("[StartSessionButton] Start() called");
#if PLATFORM_ANDROID && !UNITY_EDITOR
            // 글래스를 착용하면 휴대전화를 보거나 만지지 않으므로 화면이 꺼지고, 시간
            // 제한이 지나면 앱이 "백그라운드"로 강등된다. "앱 사용 중" 위치 권한만
            // 있으면 이때 Android가 GPS 접근을 완전히 취소한다. dumpsys location에서
            // 약 1~2분 안에 모든 GPS 등록이 "unpermitted"로 차단되는 것을 확인했다.
            // 위치 유형 포그라운드 서비스(LocationForegroundServiceBridge)는 앱을 이
            // 백그라운드 차단에서 제외하므로 GPS 자체는 유지된다. 글래스 디스플레이
            // 출력은 별개의 문제다. 실기기에서 휴대전화 화면이 잠기는 순간 글래스도
            // 검게 꺼지는 것을 확인했으며, 네이티브 앱의 DisplayPort 미러링과 같다.
            // 따라서 글래스를 켜 두려면 휴대전화 화면도 켜야 한다. 러닝 중 배터리
            // 소모와 눈부심을 줄이기 위해 ScreenKeepAliveBridge로 밝기를 최저로 낮춘다.
            LocationForegroundServiceBridge.EnsurePermissionsThenStart();
            ScreenKeepAliveBridge.KeepAliveDimmed();
            RequestPermissionThenStart();
#else
            Debug.Log("[StartSessionButton] Skipping auto-start: not an Android device build");
#endif
        }

        private void OnClick()
        {
            if (_isTracking)
            {
                session.StopSession();
                _isTracking = false;
                if (buttonLabel != null) buttonLabel.text = "START";
#if PLATFORM_ANDROID && !UNITY_EDITOR
                LocationForegroundServiceBridge.Stop();
#endif
                return;
            }

            RequestPermissionThenStart();
        }

        private void RequestPermissionThenStart()
        {
            if (_isTracking) return;

#if PLATFORM_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.Log("[StartSessionButton] Requesting ACCESS_FINE_LOCATION permission");
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ =>
                {
                    Debug.Log("[StartSessionButton] Permission granted callback fired");
                    StartTracking();
                };
                callbacks.PermissionDenied += _ => Debug.Log("[StartSessionButton] Permission denied");
                Permission.RequestUserPermission(Permission.FineLocation, callbacks);
                return;
            }
            Debug.Log("[StartSessionButton] Permission already granted");
#endif
            StartTracking();
        }

        private void StartTracking()
        {
            Debug.Log($"[StartSessionButton] StartTracking() -- session={session != null}, dopplerSource={dopplerSource != null}");
            session.StartSession(dopplerSource);
            _isTracking = true;
            if (buttonLabel != null) buttonLabel.text = "STOP";
        }
    }
}
