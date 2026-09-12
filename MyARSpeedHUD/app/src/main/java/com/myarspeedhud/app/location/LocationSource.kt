package com.myarspeedhud.app.location

import kotlinx.coroutines.flow.Flow

/**
 * GPS 위치 정보의 출처를 추상화한 것입니다. 이러한 구현체 교체 방식을 사용하면,
 * 현재 GPS나 글래스가 없는 안드로이드 에뮬레이터 환경과 향후 XREAL One Pro 글래스를 연결한
 * 실제 스마트폰 환경 모두에서 앱을 동일하게 구동할 수 있습니다.
 */
interface LocationSource {
    val type: LocationSourceType
    fun samples(): Flow<RunSample>
}
