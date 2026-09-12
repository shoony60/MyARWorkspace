package com.myarspeedhud.app.location

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Intent
import android.content.pm.ServiceInfo
import android.os.Build
import android.os.IBinder

/**
 * 러닝이 추적되는 동안 이 앱이 안드로이드의 "백그라운드 앱" 버킷에 들어가지 않도록 유지하는 포그라운드 서비스입니다.
 * 이를 통해 ACCESS_BACKGROUND_LOCATION 권한이 계속 유효하게 유지되며, 휴대폰 화면이 꺼진 후에도 GPS 업데이트가 끊기지 않고 계속 흐르게 됩니다.
 * DEVICE_GPS를 사용할 때 RunSessionViewModel.start()/stop()과 함께 MainActivity에서 시작/중지됩니다.
 */
class LocationForegroundService : Service() {

    override fun onCreate() {
        super.onCreate()
        createNotificationChannel()
        val notification = buildNotification()
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            startForeground(
                NOTIFICATION_ID,
                notification,
                ServiceInfo.FOREGROUND_SERVICE_TYPE_LOCATION
            )
        } else {
            startForeground(NOTIFICATION_ID, notification)
        }
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int = START_STICKY

    override fun onBind(intent: Intent?): IBinder? = null

    private fun buildNotification(): Notification {
        val builder = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            Notification.Builder(this, CHANNEL_ID)
        } else {
            @Suppress("DEPRECATION")
            Notification.Builder(this)
        }
        return builder
            .setContentTitle("AR Speed HUD")
            .setContentText("GPS 속도 추적 중")
            .setSmallIcon(android.R.drawable.ic_menu_mylocation)
            .setOngoing(true)
            .build()
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel =
                NotificationChannel(CHANNEL_ID, "위치 추적", NotificationManager.IMPORTANCE_LOW)
            val manager = getSystemService(NotificationManager::class.java)
            manager?.createNotificationChannel(channel)
        }
    }

    companion object {
        private const val CHANNEL_ID = "ar_speed_hud_location"
        private const val NOTIFICATION_ID = 1001
    }
}
