package com.myarspeedhud.app.location

import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlin.math.PI
import kotlin.math.cos
import kotlin.math.sin
import kotlin.random.Random

/**
 * 실제 하드웨어 없이도 러너(러닝 중인 사용자)의 물리적으로 적합한 GPS 데이터 좌표 스트림을 생성합니다.
 * 덕분에 Android 에뮬레이터에서 HUD를 완벽하게 테스트할 수 있습니다.
 * GPX 경로 재생이나 adb emu geo 명령을 호출할 필요도 없고, 실제 스마트 글래스 기기도 필요하지 않습니다.
 * 속도 보정, 페이스 계산, 누적 거리, HUD 렌더링에 이르는 전체 파이프라인을 스스로 처음부터 끝까지 구동합니다.
 * 속도는 짧은 웜업(준비 운동) 단계를 거치며 서서히 증가한 뒤, 약간의 노이즈와 느린 사인파(sinusoidal)를 이용하여 유동적으로 변합니다.
 * 방향(heading) 역시 무작위로 변경되면서 구불구불한 러닝 코스를 달리는 듯한 환경을 시뮬레이션 합니다.
 */
class SimulatedLocationSource(
    private val startLat: Double = 37.5665,
    private val startLon: Double = 126.9780,
    private val tickMillis: Long = 500L,
    private val basePaceMps: Double = 2.6 // ~9.4 km/h easy jog
) : LocationSource {
    override val type = LocationSourceType.SIMULATOR

    override fun samples(): Flow<RunSample> = flow {
        var lat = startLat
        var lon = startLon
        var bearingDeg = Random.nextDouble(0.0, 360.0)
        var elapsedMs = 0L
        val dtSeconds = tickMillis / 1000.0
        val earthRadius = 6371000.0

        while (true) {
            elapsedMs += tickMillis
            val tSec = elapsedMs / 1000.0

            val warmup = (tSec / 30.0).coerceAtMost(1.0)
            val drift = sin(tSec / 45.0) * 0.5
            val noise = Random.nextDouble(-0.15, 0.15)
            val speed = (basePaceMps * warmup + drift + noise).coerceIn(0.0, 5.5)

            bearingDeg = (bearingDeg + Random.nextDouble(-6.0, 6.0) + 360.0) % 360.0

            val distanceMeters = speed * dtSeconds
            val bearingRad = Math.toRadians(bearingDeg)
            lat += (distanceMeters * cos(bearingRad) / earthRadius) * (180.0 / PI)
            lon += (distanceMeters * sin(bearingRad) / (earthRadius * cos(Math.toRadians(lat)))) * (180.0 / PI)

            emit(
                RunSample(
                    timestampMillis = System.currentTimeMillis(),
                    latitude = lat,
                    longitude = lon,
                    speedMps = speed.toFloat(),
                    accuracyMeters = 3.5f
                )
            )
            delay(tickMillis)
        }
    }
}
