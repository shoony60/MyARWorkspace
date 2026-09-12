package com.myarspeedhud.app

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import android.view.WindowManager
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.core.content.ContextCompat
import androidx.lifecycle.viewmodel.compose.viewModel
import com.myarspeedhud.app.display.ExternalDisplayMonitor
import com.myarspeedhud.app.location.DeviceLocationSource
import com.myarspeedhud.app.location.LocationForegroundService
import com.myarspeedhud.app.location.LocationSourceType
import com.myarspeedhud.app.location.RunSessionViewModel
import com.myarspeedhud.app.location.SimulatedLocationSource
import com.myarspeedhud.app.ui.HudScreen
import com.myarspeedhud.app.ui.theme.MyARSpeedHUDTheme

class MainActivity : ComponentActivity() {

    private val requestStartupPermissionsLauncher =
        registerForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { /* GPS 모드 전환으로 처리됨 */ }

    private val requestBackgroundLocationLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { /* 포그라운드 서비스는 이와 상관없이 시작되지만, 이 권한이 없으면 화면이 꺼질 때 GPS가 작동을 멈추게 됩니다(또는 유지되지 않습니다) */ }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // 포그라운드 서비스(8단계)는 화면 제한 시간이 지나도 GPS 액세스를 유지하지만,
        // 안경으로 출력되는 외부 미러링은 휴대전화 화면이 잠기는 순간 이와 상관없이 꺼집니다.
        // DisplayPort Alt Mode 미러링은 휴대전화 화면 자체의 전원 상태와 연동되어 있어, 앱이 이를 별도로 분리할 수 없기 때문입니다.
        // 따라서 안경의 화면이 계속 켜져 있게 하려면 휴대전화 화면도 여전히 켜져 있어야 합니다.
        // 다만, 배터리 소모와 눈부심을 줄이기 위해 밝기를 최소로 낮춘 상태로 실행됩니다.
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        window.attributes = window.attributes.apply { screenBrightness = 0.01f }

        val startupPermissions = buildList {
            add(Manifest.permission.ACCESS_FINE_LOCATION)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                add(Manifest.permission.POST_NOTIFICATIONS)
            }
        }
        val missingStartupPermissions = startupPermissions.filter {
            ContextCompat.checkSelfPermission(this, it) != PackageManager.PERMISSION_GRANTED
        }
        if (missingStartupPermissions.isNotEmpty()) {
            requestStartupPermissionsLauncher.launch(missingStartupPermissions.toTypedArray())
        }

        setContent {
            MyARSpeedHUDTheme {
                val viewModel: RunSessionViewModel = viewModel()
                val state by viewModel.hudState.collectAsState()
                val simulatedSource = remember { SimulatedLocationSource() }
                val deviceSource = remember { DeviceLocationSource(applicationContext) }
                val externalDisplayMonitor = remember { ExternalDisplayMonitor(applicationContext) }
                val externalDisplayConnected by externalDisplayMonitor.isExternalDisplayConnectedFlow()
                    .collectAsState(initial = false)

                HudScreen(
                    state = state,
                    externalDisplayConnected = externalDisplayConnected,
                    onToggleSource = {
                        val nextType = if (state.source == LocationSourceType.SIMULATOR) {
                            LocationSourceType.DEVICE_GPS
                        } else {
                            LocationSourceType.SIMULATOR
                        }
                        if (state.isTracking) {
                            val nextSource =
                                if (nextType == LocationSourceType.SIMULATOR) simulatedSource else deviceSource
                            viewModel.start(nextSource)
                        } else {
                            viewModel.setPreferredSource(nextType)
                        }
                    },
                    onToggleTracking = {
                        if (state.isTracking) {
                            viewModel.stop()
                            stopService(Intent(this, LocationForegroundService::class.java))
                        } else {
                            val usingDeviceGps = state.source == LocationSourceType.DEVICE_GPS
                            if (usingDeviceGps) {
                                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q &&
                                    ContextCompat.checkSelfPermission(
                                        this,
                                        Manifest.permission.ACCESS_BACKGROUND_LOCATION
                                    )
                                    != PackageManager.PERMISSION_GRANTED
                                ) {
                                    requestBackgroundLocationLauncher.launch(Manifest.permission.ACCESS_BACKGROUND_LOCATION)
                                }
                                ContextCompat.startForegroundService(
                                    this,
                                    Intent(this, LocationForegroundService::class.java)
                                )
                            }
                            val source =
                                if (state.source == LocationSourceType.SIMULATOR) simulatedSource else deviceSource
                            viewModel.start(source)
                        }
                    }
                )
            }
        }
    }
}
