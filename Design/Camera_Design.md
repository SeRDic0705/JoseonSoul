# Camera — As-Is 설계

> 2026-07-31 시점 `CameraController.cs` / `CameraSO.cs` 구현을 관찰해 작성.

---

## 1. 구조

- `CameraController` (MonoBehaviour) — **정정(2026-08-01, 실제 씬 확인)**: `SceneRebuildTool` 코드는 MainCamera에 직접 붙이는 걸 가정하지만, 실제 `mainscene`에서는 별도 루트 오브젝트 `CameraContainer`에 부착돼 있고 그 자식으로 `Main Camera`가 있다. `target` 필드도 Player의 "CameraTarget" 오브젝트가 아니라 캐릭터 스켈레톤의 `Head_M` 본(Transform)을 직접 참조한다. 즉 **`SceneRebuildTool`의 가정과 실제 씬 배선이 어긋나 있음** — 둘 중 하나가 최신화 안 된 상태(`Design/Implementation_Backlog.md`에 기록).
- `CameraSO` (ScriptableObject): 회전 속도·감도·데드존·충돌 관련 수치 데이터.
- `SceneRebuildTool`(Editor)이 `CameraSO` 에셋을 MainCamera에 자동 배선하는 걸 의도하지만 위 이유로 현재 씬 배선과 다름.

## 2. 동작 흐름

1. **입력 (`Update` → `HandleInput`)**: `Look` 액션(마우스/패드) 델타를 읽어 `yaw`(좌우), `pitch`(상하) 누적. `pitch`는 `Data.pitchLimits`(기본 -80~30)로 클램프.
2. **타겟 추적 (`LateUpdate` → `HandleTargetFollowing`)**: 뷰포트 좌표 기준 화면 중앙(0.5,0.5)에서 타겟이 `Data.deadZoneRadius`(기본 0.2) 밖으로 벗어났을 때만 `currentTargetPosition`을 `Data.followSpeed`로 보간 추적(데드존 방식 — 작은 움직임엔 카메라가 안 흔들림).
3. **카메라 배치 (`LateUpdate` → `HandleCamera`)**: `yaw`/`pitch`로 만든 회전 × `Data.cameraOffset`을 `currentTargetPosition`에 더해 목표 위치 계산 → `AdjustCameraCollision`으로 충돌 보정 → 위치 적용 + `LookAt(currentTargetPosition)`.
4. **충돌 보정 (`AdjustCameraCollision`)**: 타겟→목표위치 방향으로 `Physics.SphereCast`(반지름 `Data.cameraRadius`, 마스크 `Data.collisionMask`). 충돌 시 히트 지점에서 `Data.collisionOffset`만큼 안쪽으로 당김.

## 3. CameraSO 필드

| 필드 | 기본값 | 용도 |
|---|---|---|
| `cameraOffset` | (0, 2, -2) | 타겟 기준 카메라 오프셋 |
| `RotationSpeed` | 0.01 | yaw/pitch 누적 배율 |
| `xSensitivity` / `ySensitivity` | 50 / 50 | 축별 감도 |
| `pitchLimits` | (-80, 30) | 상하 회전 제한 |
| `deadZoneRadius` | 0.2 | 뷰포트 기준 데드존 반경 |
| `followSpeed` | 5 | 데드존 밖일 때 타겟 추적 보간 속도 |
| `collisionMask` | (미설정) | 스피어캐스트 충돌 레이어 |
| `cameraRadius` | 0.3 | 스피어캐스트 반지름 |
| `collisionOffset` | 0.2 | 벽에서 띄우는 최소 거리 |
| `cameraAdjustSpeed` | 10 | 선언만 있고 `CameraController`에서 미사용(충돌 보정이 즉시 스냅, 보간 없음) |

**실제 `CameraSO.asset`의 라이브 값(2026-08-01 확인, 위 표의 "기본값"과 다름):** `cameraRadius`=0.3, `collisionOffset`=0.1, `collisionMask`=64(레이어 6), `deadZoneRadius`=0.01, `followSpeed`=10. 기획자가 이미 튜닝해둔 값이므로 코드 기본값이 아니라 이 실측값을 기준으로 삼을 것.

## 4. 미구현/열린 이슈

- `cameraAdjustSpeed`가 정의돼 있지만 실제 충돌 보정(`AdjustCameraCollision`)은 즉시 위치를 반환하며 이 값으로 보간하지 않음 — 카메라가 벽 앞에서 스냅될 수 있음.
- 카메라 흔들림(어택 히트/피격 시 셰이크) 없음.
- `SceneRebuildTool.RebuildPlayerAndCamera()`(Tools/Joseon 메뉴)가 `CameraSO`/`PlayerSO`/`Solider_Fist` 프리팹 경로를 하드코딩해 Player+Camera를 씬에 재생성한다 — Sword and Shield Pack 기반 새 씬으로 이식할 때 이 경로들도 갱신 필요(`Design/Implementation_Backlog.md` 참조).
- **`SceneRebuildTool`과 실제 씬 배선 불일치**(위 §1). 이 툴을 지금 실행하면 실제 씬과 다른 구조(MainCamera 직결, CameraTarget 신규 생성)가 만들어짐 — Cinemachine 마이그레이션 4단계에서 이 툴도 함께 현재 실제 구조(`CameraContainer`+`Head_M`)에 맞게 갱신 필요.
