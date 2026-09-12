package com.myarspeedhud.app.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable

private val HudColorScheme = darkColorScheme(
    background = HudBackground,
    onBackground = HudGreen,
    primary = HudGreen,
    surface = HudBackground,
    onSurface = HudGreen
)

@Composable
fun MyARSpeedHUDTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = HudColorScheme,
        typography = HudTypography,
        content = content
    )
}
