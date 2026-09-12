package com.myarspeedhud.app.display

import android.annotation.SuppressLint
import android.content.Context
import android.hardware.display.DisplayManager
import android.view.Display
import kotlinx.coroutines.channels.awaitClose
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.callbackFlow

/**
 * 외부 디스플레이(USB-C DisplayPort Alt Mode로 연결된 스마트폰)가 현재 연결되어 있는지 감지합니다.
 * XREAL 글래스는 일반 안드로이드 앱에 다른 외부 모니터와 완전히 동일하게 작동합니다.
 * 안드로이드는 글래스 전용 API 필요 없이 표준 DisplayManager를 통해 화면을 전송합니다.
 */
class ExternalDisplayMonitor(private val context: Context) {

    @SuppressLint("ServiceCast")
    fun isExternalDisplayConnectedFlow(): Flow<Boolean> = callbackFlow {
        val displayManager = context.getSystemService(Context.DISPLAY_SERVICE) as DisplayManager

        fun currentlyConnected(): Boolean =
            displayManager.displays.any { it.displayId != Display.DEFAULT_DISPLAY }

        trySend(currentlyConnected())

        val listener = object : DisplayManager.DisplayListener {
            override fun onDisplayAdded(displayId: Int) {
                trySend(currentlyConnected())
            }

            override fun onDisplayRemoved(displayId: Int) {
                trySend(currentlyConnected())
            }

            override fun onDisplayChanged(displayId: Int) {
                trySend(currentlyConnected())
            }
        }

        displayManager.registerDisplayListener(listener, null)
        awaitClose { displayManager.unregisterDisplayListener(listener) }
    }
}
