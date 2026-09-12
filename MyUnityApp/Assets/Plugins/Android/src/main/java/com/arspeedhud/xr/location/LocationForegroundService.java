package com.arspeedhud.xr.location;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.Service;
import android.content.Intent;
import android.content.pm.ServiceInfo;
import android.os.Build;
import android.os.IBinder;

/**
 * 러닝 추적 중 앱이 Android의 "백그라운드 앱"으로 분류되지 않도록 유지하는
 * 포그라운드 서비스다. ACCESS_BACKGROUND_LOCATION 권한의 효력을 유지해 휴대전화
 * 화면이 꺼진 뒤에도 GPS 업데이트가 계속 전달되도록 한다. 글래스를 착용하면
 * 휴대전화 화면을 보거나 만지지 않으므로 화면이 자주 꺼진다. 이 서비스가 없으면
 * Android가 화면이 꺼진 뒤 약 1~2분 만에 위치 접근 권한을 취소한다.
 * `adb shell dumpsys location`에서 모든 GPS 등록 상태가 같은 시간 안에
 * background -> unpermitted -> OFF로 전환되는 것을 확인했다.
 *
 * Unity의 LocationForegroundServiceBridge.cs를 통해 시작하고 중지한다.
 */
public class LocationForegroundService extends Service {
    private static final String CHANNEL_ID = "ar_speed_hud_location";
    private static final int NOTIFICATION_ID = 1001;

    @Override
    public void onCreate() {
        super.onCreate();
        createNotificationChannel();
        Notification notification = buildNotification();
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            startForeground(NOTIFICATION_ID, notification, ServiceInfo.FOREGROUND_SERVICE_TYPE_LOCATION);
        } else {
            startForeground(NOTIFICATION_ID, notification);
        }
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        return START_STICKY;
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    private Notification buildNotification() {
        Notification.Builder builder = (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
                ? new Notification.Builder(this, CHANNEL_ID)
                : new Notification.Builder(this);
        builder.setContentTitle("AR Speed HUD")
                .setContentText("GPS 속도 추적 중")
                .setSmallIcon(android.R.drawable.ic_menu_mylocation)
                .setOngoing(true);
        return builder.build();
    }

    private void createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            NotificationChannel channel = new NotificationChannel(
                    CHANNEL_ID, "위치 추적", NotificationManager.IMPORTANCE_LOW);
            NotificationManager manager = getSystemService(NotificationManager.class);
            if (manager != null) {
                manager.createNotificationChannel(channel);
            }
        }
    }
}
