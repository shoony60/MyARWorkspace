package com.myarspeedhud.app.location

/** 실제 하드웨어든 시뮬레이터든 상관없이, 가공되지 않은 GPS (위치 측정) 값. */
data class RunSample(
    val timestampMillis: Long,
    val latitude: Double,
    val longitude: Double,
    val speedMps: Float,
    val accuracyMeters: Float
)

enum class LocationSourceType { DEVICE_GPS, SIMULATOR }

/** HUD가 단일 프레임을 렌더링하는 데 필요한 모든 것. */
data class HudState(
    val speedKmh: Double = 0.0,
    val paceMinPerKm: Double? = null,
    val distanceMeters: Double = 0.0,
    val elapsedSeconds: Long = 0,
    val source: LocationSourceType = LocationSourceType.SIMULATOR,
    val gpsAccuracy: Float? = null,
    val isTracking: Boolean = false
)
