package com.myarspeedhud.app.location

import android.annotation.SuppressLint
import android.content.Context
import android.os.Looper
import com.google.android.gms.location.LocationCallback
import com.google.android.gms.location.LocationRequest
import com.google.android.gms.location.LocationResult
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import kotlinx.coroutines.channels.awaitClose
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.callbackFlow

/**
 * Google Play 서비스의 FusedLocationProviderClient를 통한 실제 GPS 데이터입니다.
 * Location.getSpeed()는 두 위치의 좌표 차이를 계산하는 방식이 아니라, GNSS 도플러 효과(Doppler-shift) 측정을 통해 도출됩니다.
 * 따라서 단순히 좌표의 변화량으로 속도를 계산하는 것보다 의미 있게 더 정확합니다.
 */
class DeviceLocationSource(private val context: Context) : LocationSource {
    override val type = LocationSourceType.DEVICE_GPS

    @SuppressLint("MissingPermission")
    override fun samples(): Flow<RunSample> = callbackFlow {
        val client = LocationServices.getFusedLocationProviderClient(context)
        val request = LocationRequest.Builder(Priority.PRIORITY_HIGH_ACCURACY, 1000L)
            .setMinUpdateIntervalMillis(500L)
            .build()

        val callback = object : LocationCallback() {
            override fun onLocationResult(result: LocationResult) {
                val loc = result.lastLocation ?: return
                trySend(
                    RunSample(
                        timestampMillis = loc.time,
                        latitude = loc.latitude,
                        longitude = loc.longitude,
                        speedMps = if (loc.hasSpeed()) loc.speed else 0f,
                        accuracyMeters = if (loc.hasAccuracy()) loc.accuracy else -1f
                    )
                )
            }
        }

        client.requestLocationUpdates(request, callback, Looper.getMainLooper())
        awaitClose { client.removeLocationUpdates(callback) }
    }
}
