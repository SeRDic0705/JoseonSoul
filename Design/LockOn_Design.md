# 시점 고정(Lock-On) — 설계안 v2 (다크소울식 카메라, 구현 전)

> 1차 시도(별도 vcam + Priority 전환)는 마스터 확인 결과 원하는 동작과 달라 폐기, 처음부터 재설계. 아직 코드 작업 전, Discord 승인 대기 중.

## 1. 배경 / 카메라 방식 재확정 (2026-08-11)

**1차 시도(폐기):** 새 vcam(`CM_LockOnCamera`, TargetGroup Follow)을 만들어 락온 시 Priority로 전환하는 방식으로 구현했으나, 마스터 확인 결과 이건 원하는 "다크소울식" 동작이 아니었음.

**확정된 이해:** 다크소울류 락온은 카메라가 다른 카메라로 전환(블렌드/컷)되는 게 아니라, **기존 오빗캠 하나를 계속 쓰면서, 락온 중엔 자유 마우스 입력 대신 "플레이어가 타겟을 보는 방향"이 오빗 각도를 부드럽게 몰아가는 방식**. 즉:
- 카메라 오브젝트/vcam은 `CM_ThirdPersonCamera` 하나 그대로 유지 — 새 vcam·Priority 전환·TargetGroup Follow 전부 제거.
- 락온 중엔 `CinemachineInputAxisController`(마우스/스틱 자유 입력)를 끄고, 코드가 `CinemachineOrbitalFollow`의 Horizontal(그리고 필요시 Vertical) 축 값을 매 프레임 "플레이어가 타겟을 바라보는 방향"에 맞춰 부드럽게(Damping) 보간.
- `LookAt`은 락온 중에만 플레이어+타겟을 함께 담는 `CinemachineTargetGroup`으로 바꿔서(위치는 그대로, 조준만) 화면 프레이밍을 다크소울처럼 유지하는 걸 고려 — 카메라 위치(오빗 반경/거리)는 안 바꿈.
- 해제 시 `LookAt`을 원래 대상(`Head_M`)으로 되돌리고 `InputAxisController` 다시 켬.

## 2. 목표 범위 — §2(1차 시도)와 동일, 변경 없음

버튼으로 가까운 Enemy 태그 대상에 락온 on/off, 고정 중 캐릭터는 이동 무관 타겟 응시(스트레이프), 범위 이탈/수동 해제/타겟 소멸 시 자동 해제. 타겟 전환·리티클 UI·LOS는 범위 밖.

## 3. 구조 설계 v2

```
PlayerLockOn (MonoBehaviour, Player 형제 컴포넌트)
  ├─ CurrentTarget / LockPoint(프록시 Transform) — 단일 진실원천(PlayerStateMachine.LockedTarget은 이걸
  │    그대로 읽기전용으로 통과시키는 프로퍼티일 뿐 복제 아님)
  ├─ TryLock() — Enemy 태그 스캔(OverlapSphere, QueryTriggerInteraction 명시 지정) →
  │    콜라이더에서 부모 방향으로 올라가며 **"Enemy" 태그를 가진 가장 가까운 조상**을 논리적 루트로 판정
  │    (`attachedRigidbody`도 `transform.root`도 아님 — 전자는 래그돌/다중 Rigidbody에서 깨지고, 후자는
  │    "Enemies" 같은 공통 부모 아래 여러 캐릭터가 있으면 전부 같은 루트로 뭉개짐. 태그 자체를 경계로 삼는
  │    방식이라 마커 컴포넌트 없이도 두 문제 다 해결됨) → 중복 제거 →
  │    카메라 정면 각도(Vector3.Angle) 최소 후보 선택(동률이면 거리로 타이브레이크)
  ├─ Unlock() — 타겟 초기화 + 프록시 비활성화(해제 공통 경로)
  ├─ Update() — Lock 입력 폴링 + LockPoint 갱신 + 거리·비활성 체크로 자동 해제
  └─ OnDisable()/OnDestroy() — 고정 중이었다면 반드시 Unlock() 호출(카메라 LookAt/입력축 상태가
     영구히 락온 상태로 남는 것 방지)

PlayerStateMachine.LockedTarget — 그대로 유지 (Player.LockOn.CurrentTarget을 그대로 통과, 복제 아님)
PlayerBaseState.Rotate() — 그대로 유지, 단 플레이어→타겟 XZ 투영 거리가 거의 0이면 방향 계산 스킵(이전 각도 유지)

CinemachineCameraBridge (기존 컴포넌트 확장)
  ├─ 신규 vcam 없음 — CM_ThirdPersonCamera 하나만 사용, 로직은 Update()에서(Cinemachine의 LateUpdate
  │    평가보다 먼저 끝나도록)
  ├─ 락온 시작: orbitInputAxis의 **직전 enabled 상태를 저장**한 뒤 false로(무조건 true로 복원하지 않음),
  │    vcam.LookAt = 타겟(LockPoint) 직접 지정(TargetGroup 아님)
  ├─ 락온 중: **타겟-플레이어-카메라 일직선** — 오빗 수평 각도를 "플레이어→타겟 반대 방향"에 해당하는
  │    각도로 `Mathf.SmoothDampAngle`류(180/-180 경계 처리)로 매 프레임 보간. 플레이어→타겟 XZ 거리가
  │    거의 0이면 이번 프레임 각도 갱신 스킵. Vertical은 고정 유지.
  ├─ 락온 해제: orbitInputAxis를 저장해둔 상태로 복원, LookAt을 원래 대상(Head_M)으로 복귀
  └─ **구현 전 실측 확인 항목(MCP):**
     1. `CinemachineOrbitalFollow`의 Binding Mode 확인 — 오빗 기준이 월드 고정인지 Follow 대상(플레이어)의
        회전에 종속되는지에 따라 각도 계산식이 달라짐. 플레이어가 회전 중인 상태에서도 축값 매핑이
        성립하는지 검증.
     2. `RotationComposer`가 실제로 `vcam.LookAt`을 Aim 단계에서 소비하는 구성인지 확인.
```

## 4. 확정 (2026-08-11, CodexBot 교차검증 반영)

- **LookAt**: ~~락온 중 타겟(또는 `LockPoint`)을 직접 지정 — TargetGroup 안 씀.~~ **(2026-08-12 변경)** 락온 여부와 무관하게 원래 대상(플레이어, Head_M)을 그대로 유지 — 더 이상 타겟으로 전환하지 않음. 마스터 지시로 변경, 사유: 타겟-플레이어-카메라 정렬(Follow 쪽 오빗 각도 보간)은 그대로 유지하되 화면 조준(Aim/LookAt)은 플레이어 기준을 유지하고 싶어함. 결과적으로 `RotationComposer`의 데드존/구도 계산이 계속 플레이어 움직임 기준으로 동작하고, 타겟 자체의 움직임에는 반응하지 않게 됨.
- **Follow(카메라 위치)**: 타겟-플레이어-카메라가 일직선이 되도록 오빗 수평 각도를 매 프레임 부드럽게 보간(SmoothDampAngle류, 각도 경계 처리 포함).
- 거리/높이(오빗 반경, 수직 각도)는 이번 범위에서 고정 유지 — 필요해지면 나중에 추가.
- **타겟 루트 판정**: `attachedRigidbody`도 `transform.root`도 아닌, **콜라이더에서 부모로 올라가며 "Enemy" 태그를 가진 가장 가까운 조상**을 논리적 루트로 판정(마커 컴포넌트 없이 래그돌 문제와 "공통 부모 아래 여러 캐릭터" 문제 둘 다 해결, CodexBot 재지적 반영). `QueryTriggerInteraction` 명시.
- **동률 후보**: 각도 동일 시 거리로 타이브레이크.
- **벽 너머 타겟(LOS)**: 기존 합의대로 이번 범위 밖 유지.
- **타겟 선택 각도 계산이 카메라 피치 영향받는 것**: 기존 합의(`Vector3.Angle` 3D 전체, `WorldToViewportPoint` 안 씀)대로 유지 — 알려진 단순화로 남겨두고, 실사용에서 문제되면 그때 보정.
- **예외 처리**: 플레이어-타겟 XZ 거리 0에 가까우면 방향 계산 스킵(이전 값 유지). `OnDisable`/`OnDestroy`에서 Unlock() 강제 호출. `orbitInputAxis`는 무조건 true 복원이 아니라 락온 진입 전 enabled 상태 저장 후 복원.

## 4-1. 실측 결과 (2026-08-11, MCP)

- **BindingMode = `WorldSpace`**(`orbitalFollow.TrackerSettings.BindingMode`). 오빗 기준이 Follow 대상(플레이어)의 회전에 전혀 종속되지 않는 순수 월드 고정 방식 — "플레이어 회전 중에도 축값 매핑이 성립하는지" 우려가 애초에 발생하지 않는 구조로 확인됨(플레이어가 어떻게 회전하든 `HorizontalAxis` 각도-월드방향 매핑은 불변).
- **OrbitStyle = `Sphere`**, `HorizontalAxis.Range = -180~180`, `Wrap = true`, `Radius = 2.83`.
- 기준 상태(Horizontal=0, 플레이어 forward=+Z)에서 카메라 위치 오프셋이 정확히 `(0,0,-radius)`(플레이어 뒤 -Z)로 관측됨 → `Quaternion.Euler(Vertical, Horizontal, 0) * Vector3.back` 형태의 표준 월드축 구면좌표 공식으로 확인(Unity 좌우손 좌표계 Y축 회전 공식과 일치).
- 위 공식을 대수적으로 풀면: **목표 Horizontal 각도 = `Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg`**(`toTarget` = 타겟-플레이어 XZ 방향 벡터) — 즉 "플레이어→타겟 방향의 월드 yaw"와 정확히 같음. `Quaternion.LookRotation` 공식과 동일해서 별도 좌표계 변환 불필요.
- 플레이 모드에서 실시간(라이브 프레임) 검증은 시도했으나 **에디터가 포커스 없는 상태(`is_focused:false`)라 플레이모드 진입 후에도 프레임이 갱신되지 않아**(Unity가 백그라운드에서 틱을 안 돌림) 값 변경 후 즉시 재조회해도 트랜스폼이 그대로였음 — 분석적 유도로 대체, 실제 체감 확인은 마스터가 포커스 있는 세션에서 플레이해봐야 함(§5 8단계에서 안내).
- `RotationComposer` 컴포넌트가 `CM_ThirdPersonCamera`에 실제로 부착돼 있고 `LookAt`(`Head_M`)을 Aim 단계에서 소비하는 표준 구성 확인(Body=OrbitalFollow, Aim=RotationComposer).
- **참고:** 이 실측 중 `PlayerStateMachine.LockedTarget → Player.LockOn.CurrentTarget`에서 NRE 발생 확인(씬에 `PlayerLockOn` 아직 배선 전이라 `Player.LockOn`이 null) — §5 5단계(씬 배선) 완료 전까지는 정상. 코드 결함 아님.

## 5. 작업 순서 (승인 후)

1차 시도에서 살아있는 부분(`PlayerLockOn`, `PlayerLockOnData`, `Lock` 입력 액션, `PlayerStateMachine.LockedTarget`, `PlayerBaseState.Rotate()` 분기)은 로직 그대로 재구현. 카메라 쪽만 새로 짠다.

1. (재구현) `PlayerLockOnData`/`PlayerLockOn`/`Lock` 입력 액션/`LockedTarget`/`Rotate()` 분기 — 1차 시도와 동일 내용.
2. `CinemachineCameraBridge`에 락온 카메라 로직 신규 작성(신규 vcam 없이, 오빗 각도 보간 + LookAt 전환).
3. MCP로 `HorizontalAxis` 축 기준 확인 후 각도 계산식 확정.
4. Unity MCP 컴파일 확인 → 플레이모드에서 락온 on/off, 오빗 각도가 부드럽게 타겟 쪽으로 도는지, 해제 후 자유시점 복귀 확인.
5. Discord로 결과 보고 후 커밋/브랜치/PR은 별도 허가.
