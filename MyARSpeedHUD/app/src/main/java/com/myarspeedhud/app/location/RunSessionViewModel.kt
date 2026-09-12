package com.myarspeedhud.app.location

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.sin
import kotlin.math.sqrt

/**
 * 원시 [LocationSource] 스트림을 매끄럽고 화면에 표시할 수 있는 [HudState]로 변환합니다.
 * 원시 순간 GPS 속도는 노이즈가 심하므로(위성 기하학적 구조, 다중 경로) 화면에 표시되기 전에 지수 이동 평균을 거칩니다.
 * 그렇지 않으면 HUD 숫자가 매초마다 산만하게 흔들리게 됩니다.
 */
class RunSessionViewModel : ViewModel() {
    private val _hudState = MutableStateFlow(HudState())
    val hudState: StateFlow<HudState> = _hudState.asStateFlow()

    private var trackingJob: Job? = null
    private var smoothedSpeedMps: Double? = null
    private var totalDistanceMeters = 0.0
    private var startElapsedMs = 0L
    private var lastSample: RunSample? = null
    private var currentSourceType = LocationSourceType.SIMULATOR

    private val smoothingAlpha = 0.25
    private val minPaceSpeedMps = 0.3 // 이 아래로는 페이스(속도)가 의미 없음 (사실상 정지 상태)
    private val staleSampleThresholdMs = 3000L // 오랫동안 새로운 위치 수정이 없음 -> 속도를 0으로 강제함
    private var lastSampleReceivedAtMs = 0L

    /** 추적 세션을 시작하지 않고 선택된 소스만 업데이트합니다. */
    fun setPreferredSource(sourceType: LocationSourceType) {
        currentSourceType = sourceType
        _hudState.value = _hudState.value.copy(source = sourceType)
    }

    fun start(source: LocationSource) {
        stop()
        smoothedSpeedMps = null
        totalDistanceMeters = 0.0
        lastSample = null
        currentSourceType = source.type
        startElapsedMs = System.currentTimeMillis()
        lastSampleReceivedAtMs = System.currentTimeMillis()
        _hudState.value = HudState(source = source.type, isTracking = true)

        trackingJob = viewModelScope.launch {
            launch {
                source.samples().collect { sample -> onSample(sample) }
            }
            launch {
                // 경과 시간과 속도 정체 감시 타이머 (stale-speed watchdog) 모두
                // GPS 수정과 독립적인 고정된 시계에서 실행되어야 합니다.
                // 정지 상태에서는 GPS가 작동하지 않을 수 있으며, 경과 시간은
                // 다른 요소와 상관없이 일반 스톱워치처럼 계속 흘러가야 합니다.
                while (isActive) {
                    delay(1000L)
                    val now = System.currentTimeMillis()

                    val newElapsedSeconds = (now - startElapsedMs) / 1000
                    if (newElapsedSeconds != _hudState.value.elapsedSeconds) {
                        _hudState.value = _hudState.value.copy(elapsedSeconds = newElapsedSeconds)
                    }

                    val staleFor = now - lastSampleReceivedAtMs
                    if (staleFor >= staleSampleThresholdMs && smoothedSpeedMps != null && smoothedSpeedMps != 0.0) {
                        smoothedSpeedMps = 0.0
                        _hudState.value = _hudState.value.copy(speedKmh = 0.0, paceMinPerKm = null)
                    }
                }
            }
        }
    }

    fun stop() {
        trackingJob?.cancel()
        trackingJob = null
        if (_hudState.value.isTracking) {
            _hudState.value = _hudState.value.copy(isTracking = false)
        }
    }

    private fun onSample(sample: RunSample) {
        lastSampleReceivedAtMs = System.currentTimeMillis()
        lastSample?.let { previous ->
            totalDistanceMeters += haversineMeters(
                previous.latitude, previous.longitude, sample.latitude, sample.longitude
            )
        }
        lastSample = sample

        val previousSmoothed = smoothedSpeedMps
        val newSmoothed = if (previousSmoothed == null) {
            sample.speedMps.toDouble()
        } else {
            smoothingAlpha * sample.speedMps + (1 - smoothingAlpha) * previousSmoothed
        }
        smoothedSpeedMps = newSmoothed

        val speedKmh = newSmoothed * 3.6
        val pace = if (newSmoothed > minPaceSpeedMps) (1000.0 / newSmoothed) / 60.0 else null
        val elapsedSeconds = (System.currentTimeMillis() - startElapsedMs) / 1000

        _hudState.value = HudState(
            speedKmh = speedKmh,
            paceMinPerKm = pace,
            distanceMeters = totalDistanceMeters,
            elapsedSeconds = elapsedSeconds,
            source = currentSourceType,
            gpsAccuracy = sample.accuracyMeters.takeIf { it >= 0f },
            isTracking = true
        )
    }

    override fun onCleared() {
        stop()
    }
}

private fun haversineMeters(lat1: Double, lon1: Double, lat2: Double, lon2: Double): Double {
    val r = 6371000.0
    val dLat = Math.toRadians(lat2 - lat1)
    val dLon = Math.toRadians(lon2 - lon1)
    val a = sin(dLat / 2) * sin(dLat / 2) +
            cos(Math.toRadians(lat1)) * cos(Math.toRadians(lat2)) * sin(dLon / 2) * sin(dLon / 2)
    val c = 2 * atan2(sqrt(a), sqrt(1 - a))
    return r * c
}
