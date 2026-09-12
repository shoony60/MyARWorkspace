using UnityEngine;

namespace ARSpeedHUD.Hud
{
    /// <summary>
    /// 두 HUD가 동일하게 보이도록 네이티브 앱의 ui/theme/Color.kt와 같은 색상표를
    /// 사용한다. 웨이브가이드 디스플레이에서 검정 배경은 투명하게 렌더링되므로 밝은
    /// 문자만 보이도록 설계했다. 같은 설계 근거는 해당 Kotlin 파일의 주석을 참고한다.
    /// </summary>

    public static class HudColors
    {
        public static readonly Color Background = new Color32(0x00, 0x00, 0x00, 0xFF);
        public static readonly Color Green = new Color32(0x39, 0xFF, 0x6A, 0xFF);
        public static readonly Color Amber = new Color32(0xFF, 0xC2, 0x4B, 0xFF);
        public static readonly Color Red = new Color32(0xFF, 0x52, 0x52, 0xFF);
        public static readonly Color Dim = new Color32(0x7A, 0x8A, 0x7E, 0xFF);
    }
}
