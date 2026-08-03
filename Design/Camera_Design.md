# Camera — As-Is 설계

> 2026-08-03 기준. 커스텀 `CameraController`(2026-07-31 As-Is로 문서화됐던 시스템)는 Cinemachine 마이그레이션 완료로 제거됐다. 마이그레이션 과정·의사결정 근거는 `Design/Cinemachine_Migration_Plan.md` 참조. 이 문서는 현재(Cinemachine 기반) 시스템을 As-Is로 기술한다.

---

## 1. 구조

- `CameraContainer`(루트 오브젝트, Main Camera의 부모) — 과거 레거시 `CameraController`가 있던 자리. 현재는 컴포넌트 없이 Transform만 유지(레거시 흔적, 정리 대상 아님 — 계층 구조 변경은 별도 확인 후).
- Main Camera에 `CinemachineBrain` 부착, `enabled=true` 상시 활성.
- `CM_ThirdPersonCamera`(별도 루트 오브젝트) — 실제 카메라 리그. 구성 컴포넌트:
  - `CinemachineCamera` — Follow/LookAt = Player의 캐릭터 스켈레톤 `Head_M` 본(레거시 때부터 동일 타겟 유지)
  - `CinemachineOrbitalFollow` (OrbitStyle=Sphere) — 위치만 제어
  - `CinemachineRotationComposer` — 회전(화면 구도/데드존) 담당
  - `CinemachineInputAxisController` — Look Orbit X/Y가 `Assets/InputActions/InputActions.inputactions`의 `Player/Look` 액션에 바인딩됨(`CancelDeltaTime=true`)
  - `CinemachineDeoccluder` — 충돌 회피/디오클루전
  - `CinemachineCameraBridge` — `CameraSO` 초기값을 위 컴포넌트들에 주입(`OnEnable`에서 1회)
  - `CinemachineTuningPanel` — 오빗 반경/회전 감도/데드존/댐핑/충돌 관련 값을 한 곳에서 인스펙터로 조정(`OnValidate`로 즉시 반영)

## 2. 데이터 흐름

1. `CameraSO`(ScriptableObject)가 초기값 소스: `cameraOffset`(→ Radius), `deadZoneRadius`(→ RotationComposer DeadZone), `cameraRadius`/`collisionOffset`/`collisionMask`(→ Deoccluder).
2. `CinemachineCameraBridge.Configure()`가 CM 오브젝트 활성화 시(`OnEnable`) 위 값을 실제 컴포넌트 필드에 주입.
3. 이후 실시간 튜닝은 `CinemachineTuningPanel`을 통해 직접 진행(CameraSO에 다시 쓰기는 하지 않음 — 패널 값은 씬 로컬).
4. 입력은 New Input System `Player/Look` 액션 → `CinemachineInputAxisController` → `OrbitalFollow`/`RotationComposer` 순으로 흐름.

## 3. CameraSO 필드 (여전히 초기값 소스로 사용)

| 필드 | 실측값(2026-08-01 확인) | Cinemachine 매핑 |
|---|---|---|
| `cameraOffset` | (0, 2, -2) | `OrbitalFollow.Radius` = magnitude |
| `deadZoneRadius` | 0.01 | `RotationComposer.Composition.DeadZone.Size` |
| `cameraRadius` | 0.3 | `Deoccluder.AvoidObstacles.CameraRadius` |
| `collisionOffset` | 0.1 | `Deoccluder.MinimumDistanceFromTarget` |
| `collisionMask` | 64(레이어 6) | `Deoccluder.CollideAgainst` |
| `RotationSpeed`/`xSensitivity`/`ySensitivity`/`pitchLimits`/`followSpeed`/`cameraAdjustSpeed` | — | Cinemachine으로 대체돼 더 이상 직접 쓰이지 않음(레거시 전용 필드, `CameraSO.cs`에는 남아있으나 참조 안 됨) |

## 4. 미구현/열린 이슈

- 카메라 흔들림(어택 히트/피격 시 셰이크) 없음 — Cinemachine Impulse 등으로 향후 추가 가능.
- `CameraSO`에 레거시 전용 필드(RotationSpeed 등)가 안 쓰이는 채로 남아있음 — 다음에 CameraSO 정리할 때 제거 후보.
- `SceneRebuildTool.RebuildPlayerAndCamera()`가 이제 Cinemachine 리그를 생성하도록 갱신됨(`Design/Implementation_Backlog.md` 참조). 단, `CinemachineInputAxisController`의 Look 액션 재바인딩은 도구 실행 후 수동으로 한 번 더 해줘야 함(자동화 시 감도 단위 문제, CodexBot 지적).
