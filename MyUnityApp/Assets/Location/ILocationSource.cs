using System;

namespace ARSpeedHUD.Location
{
    /// <summary>
    /// "GPS 위치 정보가 어디에서 오는가"를 추상화한 네이티브 Android 앱의
    /// LocationSource.kt에 대응하는 C# 구현이다. 구현체를 교체하면 하드웨어와 GPS가
    /// 없는 에디터에서는 SimulatedLocationSource로 개발하고, 나중에 실제
    /// 휴대전화에서는 DeviceLocationSource로 전환할 수 있다.
    /// </summary>
    public interface ILocationSource
    {
        LocationSourceType Type { get; }
        event Action<RunSample> SampleReceived;
        void StartSampling();
        void StopSampling();
    }
}
