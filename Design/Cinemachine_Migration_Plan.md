# Cinemachine 마이그레이션 계획

> 2026-08-01 ClaudeBot·CodexBot 개선점 리뷰 합의 사항(항목 1) 기반. 2026-08-01 2차 리뷰(CodexBot)로 구성 요소·A/B 전환 방식·초기화 순서를 수정. 아직 미구현 — 이 문서는 계획이며, 실제 코드 작업 전 마스터 승인이 필요하다.

## 목표

커스텀 `CameraController`(오빗 회전 + 뷰포트 데드존 추적 + SphereCast 충돌 회피)를 Cinemachine 3.x로 교체한다. 현재 구현엔 `cameraAdjustSpeed`가 선언만 있고 실제 충돌 보정이 즉시 스냅되는 버그가 있다(`Design/Camera_Design.md` §4). 목적은 버그 해소 + 유지보수 부담 감소.

## 구성 요소 (CM3, Unity 6000.3 기준)

| 컴포넌트 | 역할 |
|---|---|
| `CinemachineBrain` | Main Camera에 부착. 기존 `Camera` 컴포넌트는 유지. |
| `CinemachineCamera` | 가상 카메라 1개(전환/블렌드 요구사항 없음 — Priority 단일 운용, 블렌드 0/Cut 고정) |
| `CinemachineOrbitalFollow` | **위치만** 제어. Follow Target = 레거시 `CameraController.target`과 동일한 `Head_M` 본(실제 씬 확인 결과, "CameraTarget" 오브젝트 아님 — §단계별 계획 2 참조). Single Rig(단일 반경) 오빗 |
| `CinemachineRotationComposer` (또는 `HardLookAt`) | **회전 담당 — 필수.** OrbitalFollow는 위치만 제어하므로 이게 없으면 카메라가 타겟을 보장하지 않고 바라본다는 보장이 없음(CodexBot 지적). 뷰포트 데드존 개념은 여기(화면 구도 기준)에 매핑 |
| `CinemachineInputAxisController` | New Input System `Look` 액션을 축에 연결(`CameraSO.xSensitivity`/`ySensitivity`/`RotationSpeed` 대응) — 단, 단순 필드 대응 아님(§요구 동작 스펙 참조) |
| `CinemachineDeoccluder` | 충돌 회피/디오클루전 담당(CM3에서 옛 `CinemachineCollider` 대체) |

## 요구 동작 스펙 — 구현 전 확정 필요

기존 커스텀 구현 값을 그대로 옮기는 것이 아니라, 아래 표의 왼쪽(현재 값/동작)을 기준으로 Cinemachine에서 동등한 체감을 낼 설정을 확정해야 한다.

| 항목 | 현재 커스텀 구현 | Cinemachine 대응(제안) | 상태 |
|---|---|---|---|
| 충돌 여유 거리 | `collisionOffset`=0.2 — **코드 확인**: `AdjustCameraCollision`에서 `hit.distance - collisionOffset`으로 사용 = "충돌 표면과의 여유 거리"(타겟 기준 최소거리 아님) | Deoccluder **Camera Radius** 계열(의미가 일치하는 파라미터, `Minimum Distance From Target` 아님) | 매핑 방향 확정, 수치 재조정은 체감 확인 필요 |
| 충돌 반경 | `cameraRadius`=0.3 (SphereCast 반경) | Deoccluder Camera Radius | 이관 |
| 장애물 진입 감쇠 | 없음(즉시 스냅 — 버그) | Deoccluder Damping 신규 설정 | **마스터 확인 필요**(원하는 감쇠 속도감) |
| 장애물 복귀 감쇠 | 없음 | Damping 대칭 적용 | 위와 동일 |
| 콜라이더 제외(캐릭터 자신) | `collisionMask` 레이어마스크 | Deoccluder Collide Against 레이어마스크 | 기존 마스크값 그대로 이관 가능 |
| 좁은 공간 처리 | 없음(뚫림 가능) | Deoccluder Minimum Distance from Target | **마스터 확인 필요** — 허용 최소 거리 기준 없음 |
| 데드존 추적(화면 구도) | 뷰포트(0.5,0.5) 기준 반경 0.2, `followSpeed`=5 | Rotation Composer의 Dead Zone/Damping (OrbitalFollow 아님) | **마스터 확인 필요** — "동일 재현" vs "비슷하면 충분" 기준 |
| 회전 감도 | `RotationSpeed`(0.01) × `xSensitivity`/`ySensitivity`(50/50) × 입력델타 | InputAxisController Gain + Accel/Decel — **단순 필드 대응 아님**(아래 참조) | **비교 기준: 단순 필드값이 아니라 "초당 회전각"으로 맞춤** |

**감도 매핑 주의(CodexBot 지적):** InputAxisController는 gain뿐 아니라 accel/decel까지 적용해 기존 `RotationSpeed × sensitivity × deltaTime`과 단위가 다를 수 있다. 마우스 델타는 `CancelDeltaTime=true`, 스틱 입력은 일반적으로 deltaTime 적용이 필요해서 하나의 `Look` 액션에 마우스/패드를 섞으면 동일 설정으로 정확히 재현하기 어렵다. 필요 시 장치별 액션 분리 또는 custom axis reader 검토.

## A/B 전환 방식 (수정 — "비활성 공존"만으로는 부족함)

CodexBot 지적: 같은 MainCamera에 `CinemachineBrain`과 기존 `CameraController`가 동시에 활성화되면 LateUpdate 변환 경쟁이 생길 수 있다. **원자적 토글**로 명시:

- **Legacy 모드:** `CameraController` ON / `CinemachineBrain`+`CinemachineCamera`+`InputAxisController` OFF
- **CM 모드:** `CameraController` OFF / CM 컴포넌트들 ON
- 전환 시 현재 `yaw`/`pitch`와 카메라 위치를 새 시스템에 동기화(전환 직후 시점 튐 방지)
- 블렌드는 0 또는 Cut으로 고정(전환 중 보간 없음)

## 단계별 계획 (각 단계 = 별도 커밋)

1. **[완료 2026-08-01]** Cinemachine 패키지 추가(`manifest.json`, `com.unity.cinemachine` 3.1.7). 컴파일 에러 0건.
2. **[완료 2026-08-01]** CM 리그(`CinemachineCamera`+`OrbitalFollow`+`RotationComposer`+`InputAxisController`+`Deoccluder`)를 `CM_ThirdPersonCamera` 오브젝트로 **비활성 상태로 생성**, `CinemachineCameraBridge.cs` 작성해 부착.
   - **실제 씬 확인 결과 계획 수정:** `Design/Camera_Design.md` §1 정정과 동일 — 레거시 `CameraController`는 `CameraContainer`(MainCamera 아님)에 있고, `target`은 "CameraTarget" 오브젝트가 아니라 캐릭터 스켈레톤의 `Head_M` 본. CM 리그의 `Follow`/`LookAt`도 레거시 `CameraController.target`을 그대로 읽어와 **`Head_M`으로 동일하게 설정**해 A/B 비교 기준을 맞췄다.
   - `CinemachineBrain`은 Main Camera에 추가하고 `enabled=false`로 비활성(레거시가 계속 주도권 유지).
   - 브리지는 `Data`(CameraSO)·`orbitalFollow`·`rotationComposer`·`deoccluder` 참조를 갖고 `OnEnable`에서 `Configure()` 호출 — `orbitalFollow.Radius`(=cameraOffset 크기), `rotationComposer.Composition.DeadZone`, `deoccluder.AvoidObstacles.CameraRadius`/`CollideAgainst`/`MinimumDistanceFromTarget`을 CameraSO 값으로 주입. Damping류는 Cinemachine 기본값 유지(임의로 안 정함 — 마스터가 인스펙터에서 조정).
   - **수동 처리 필요(미완료):** `CinemachineInputAxisController`는 자동 생성 시 Cinemachine 기본 입력 액션(`CinemachineDefaultInputActions.inputactions`)으로 Auto-Populate되어 있음 — 우리 게임의 `InputActions.inputactions`(Look 액션)으로 재바인딩하는 건 에디터에서 수동으로 해야 함(CodexBot 지적대로 감도 단위가 다르므로 자동 이관 안 함).
   - MCP 컴파일 확인 0건 에러/경고, 씬 저장 완료.
3. **[완료 2026-08-01]** 원자적 토글 구현(`CameraModeSwitcher.cs`, `CameraContainer`에 부착) + Deoccluder/RotationComposer는 이미 2단계에서 브리지가 담당.
   - `CameraController`에 `Yaw`/`Pitch` 읽기 전용 프로퍼티 추가(전환 시 축 동기화용).
   - `SwitchToCinemachine()`: 레거시의 현재 yaw/pitch를 `OrbitalFollow.HorizontalAxis/VerticalAxis.Value`로 복사 → 레거시 비활성화 → `CinemachineBrain` 활성화 → CM 리그 활성화(활성화 순간 브리지 `OnEnable`이 `Configure()` 재실행).
   - `SwitchToLegacy()`: 역순.
   - `CinemachineBrain.DefaultBlend`를 `Cut`/`Time=0`으로 고정(전환 중 블렌드 보간 없음).
   - **플레이모드 실측 검증(2026-08-01):** Play 진입 → `SwitchToCinemachine()` 호출 → `legacy.enabled=False`, `brain.enabled=True`, CM 리그 `activeSelf=True`, 카메라가 유효한 위치/회전으로 이동(콘솔 에러/경고 0건) → `SwitchToLegacy()` 호출 → 정상 복귀 확인. **단, 카메라 프레이밍이 레거시와 픽셀 단위로 동일하진 않음** — Sphere 오빗 반경 기반 매핑이라 원래의 고정 오프셋 벡터 방향과 정확히 일치하지 않음(§요구 동작 스펙에서 이미 "완전 재현 아님, 인스펙터 튜닝"으로 합의된 부분).
   - **수동 처리 남음:** `CinemachineInputAxisController`의 Look 입력을 Cinemachine 기본 액션에서 우리 게임 `InputActions.inputactions`의 Look 액션으로 재바인딩하는 건 에디터에서 마스터가 직접 해야 함(자동화 시 감도 단위 불일치 위험, CodexBot 지적).
   - `useCinemachine` 기본값은 `false`로 유지 — CM 모드는 아직 실제 플레이에 쓰이지 않음, 테스트/토글 목적으로만 존재.
4. Legacy 경로가 충분히 검증되면 `CameraController.cs` 제거.
5. `SceneRebuildTool` 갱신은 4단계까지 미루지 않는다 — 단, 이 툴은 이미 실제 씬 구조(`CameraContainer`+`Head_M`)와 어긋나 있었으므로(`Design/Camera_Design.md` §1/§4), 갱신 시 둘 다 바로잡는다. **패키지 커밋과 씬/도구 커밋은 분리** 유지.
6. 각 단계마다 Unity MCP로 컴파일 확인 + 플레이모드에서 기존 동작과 비교 검증.

## 영향 범위

- 신규: `manifest.json`(Cinemachine 패키지), 브리지 스크립트, CM 리그 프리팹/씬 오브젝트
- 변경: `SceneRebuildTool.cs`(카메라 컴포넌트 배선 — 각 단계마다 갱신)
- 제거 예정(4단계 완료 후): `CameraController.cs`
- 유지: `CameraSO.cs`(역할 축소 — 초기화용 값 소스)

## 열린 질문 → 해결 방식 (2026-08-01 마스터 확인)

숫자를 미리 확정하지 않고, 아래 값들을 **인스펙터에서 직접 조절 가능한 필드**로 노출한다. 좁은 공간 최소거리·감쇠 속도감·데드존 근사 정도는 코드로 결정하지 않고 에디터에서 실시간으로 튜닝한다.

- Deoccluder Camera Radius / Minimum Distance From Target → 인스펙터 노출(기존 `cameraRadius`/`collisionOffset` 대체)
- Deoccluder Damping(진입/복귀) → 인스펙터 노출
- Rotation Composer Dead Zone/Damping → 인스펙터 노출

**브랜치:** `main`에서 분기한 `cinemachine` 브랜치에서 구현(2026-08-01).

## 검증

- 각 단계 MCP 컴파일 확인.
- 전환 직후(Legacy↔CM) 카메라 위치/각도 튐 없는지 확인.
- 최종적으로 좁은 공간/기둥 뒤/급격한 카메라 회전 등 기존에 문제 있었던 케이스를 플레이모드로 재현해 회귀 확인.
- 회전 감도는 "초당 회전각" 기준으로 기존 값과 비교.
