namespace ARSpeedHUD.Location
{
    /// <summary>실제 하드웨어 또는 시뮬레이터에서 받은 단일 원시 GPS 위치 정보.</summary>
    public struct RunSample
    {
        public double TimestampMillis;
        public double Latitude;
        public double Longitude;
        public float SpeedMps;
        public float AccuracyMeters;
    }

    public enum LocationSourceType { DeviceGps, Simulator }

    /// <summary>HUD가 한 프레임을 렌더링하는 데 필요한 모든 정보.</summary>
    public struct HudState
    {
        public double SpeedKmh;
        public double? PaceMinPerKm;
        public double DistanceMeters;
        public long ElapsedSeconds;
        public LocationSourceType Source;
        public float? GpsAccuracy;
        public bool IsTracking;
    }
}
