package com.myarspeedhud.app.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.myarspeedhud.app.location.HudState
import com.myarspeedhud.app.location.LocationSourceType
import com.myarspeedhud.app.ui.theme.HudDim
import java.util.Locale

/**
 * HUD 레이아웃은 프레임의 바깥쪽 가장자리를 의도적으로 피합니다.
 * XREAL One Pro는 대각선 FOV(화각)가 약 50도이므로, 전체 화면 렌더링의 가장자리 근처에 배치된 콘텐츠는 웨이브가이드에 의해 가장 먼저 잘려 나갑니다.
 * 주요 수치(속도)는 왼쪽 위에, 보조 통계는 왼쪽 아래에 배치되어 있으며, 모두 안전 마진(여백) 내에 위치합니다.
 */
@Composable
fun HudScreen(
    state: HudState,
    onToggleSource: () -> Unit,
    onToggleTracking: () -> Unit,
    externalDisplayConnected: Boolean = false
) {
    // Surface must wrap the layout -- without it MaterialTheme's background
    // color never actually paints the screen.
    Surface(
        modifier = Modifier.fillMaxSize(),
        color = MaterialTheme.colorScheme.background,
        contentColor = MaterialTheme.colorScheme.onBackground
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = 48.dp, vertical = 32.dp)
        ) {
            Column(
                modifier = Modifier
                    .align(Alignment.TopStart)
                    .fillMaxWidth(0.6f)
            ) {
                SourceBadge(state.source, state.isTracking)
                if (externalDisplayConnected) {
                    Text(
                        text = "⧉ MIRRORED TO GLASSES",
                        style = MaterialTheme.typography.bodyLarge,
                        color = HudDim
                    )
                }
                Spacer(Modifier.height(8.dp))
                Text(
                    text = String.format(Locale.US, "%.1f", state.speedKmh),
                    style = MaterialTheme.typography.displayLarge
                )
                Text(text = "km/h", style = MaterialTheme.typography.headlineMedium)
            }

            Column(modifier = Modifier.align(Alignment.BottomStart)) {
                Text(
                    text = "PACE  ${formatPace(state.paceMinPerKm)}",
                    style = MaterialTheme.typography.bodyLarge
                )
                Text(
                    text = "DIST  ${formatDistance(state.distanceMeters)}",
                    style = MaterialTheme.typography.bodyLarge
                )
                Text(
                    text = "TIME  ${formatElapsed(state.elapsedSeconds)}",
                    style = MaterialTheme.typography.bodyLarge
                )
                state.gpsAccuracy?.let {
                    Text(
                        text = "±${String.format(Locale.US, "%.0f", it)}m",
                        style = MaterialTheme.typography.bodyLarge
                    )
                }
            }

            Row(
                modifier = Modifier.align(Alignment.BottomEnd),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                OutlinedButton(onClick = onToggleSource) {
                    Text(if (state.source == LocationSourceType.SIMULATOR) "SIM" else "GPS")
                }
                Button(onClick = onToggleTracking) {
                    Text(if (state.isTracking) "STOP" else "START")
                }
            }
        }
    }
}

@Composable
private fun SourceBadge(source: LocationSourceType, tracking: Boolean) {
    val label = when {
        !tracking -> "STANDBY"
        source == LocationSourceType.SIMULATOR -> "● SIMULATOR"
        else -> "● GPS LIVE"
    }
    Text(text = label, style = MaterialTheme.typography.bodyLarge, fontWeight = FontWeight.Bold)
}

private fun formatPace(paceMinPerKm: Double?): String {
    if (paceMinPerKm == null || paceMinPerKm.isInfinite() || paceMinPerKm.isNaN()) return "--:--"
    val totalSeconds = (paceMinPerKm * 60).toInt()
    val min = totalSeconds / 60
    val sec = totalSeconds % 60
    return String.format(Locale.US, "%d:%02d /km", min, sec)
}

private fun formatDistance(meters: Double): String =
    if (meters < 1000) String.format(Locale.US, "%.0f m", meters)
    else String.format(Locale.US, "%.2f km", meters / 1000.0)

private fun formatElapsed(totalSeconds: Long): String {
    val h = totalSeconds / 3600
    val m = (totalSeconds % 3600) / 60
    val s = totalSeconds % 60
    return if (h > 0) String.format(Locale.US, "%d:%02d:%02d", h, m, s)
    else String.format(Locale.US, "%02d:%02d", m, s)
}
