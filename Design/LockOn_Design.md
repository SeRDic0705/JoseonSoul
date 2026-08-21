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

## 6. 락온 중 Walk 애니메이션 — 4방향 블렌드 (계획 A안 v2, 2026-08-21, CodexBot 검토 반영·구현 전)

### 배경
락온 중 Walk 시 실제 이동방향과 무관하게 항상 전진 애니메이션(`Assets/Animations/Samurai_Katana/Basic/WalkForward.FBX`)만 재생돼 시각적으로 어색함(마스터 2026-08-21 보고). 조사 결과 `Assets/Animations/Samurai_Katana/Lock-On/` 폴더에 락온 전용 4방향 클립(`WalkForward_HS`/`WalkBackward_HS`/`WalkLeft_HS`/`WalkRight_HS`, 전투 자세)이 이미 존재함을 확인.

### 확정 방향 (A안 채택, B안은 기각)
- A안(채택): 평상시(비락온) Walk는 기존 `Basic/WalkForward` 그대로 유지, **락온 중에만** 4방향 HS 블렌드트리로 전환. 자세 일관성 유지가 목적.
- B안(기각): Walk 자체를 통째로 4방향 HS 블렌드트리로 교체(전진도 `WalkForward_HS` 사용) — 평소 걷기 자세가 전투 자세로 바뀌어 시각적 불일치 발생하므로 채택 안 함.

### 설계 (v2 — CodexBot 검토 반영)
- 신규 Animator 상태 `WalkLockedOn` (`ground` 서브머신 내부, 기존 `Walk`와 나란히): 2D Blend Tree(Freeform Directional), 파라미터 `MoveX`/`MoveZ`(Float), 클립 4개(WalkForward_HS/WalkBackward_HS/WalkLeft_HS/WalkRight_HS). Blend 좌표: Forward `(0,1)` / Back `(0,-1)` / Left `(-1,0)` / Right `(1,0)`.
- 신규 bool 파라미터 `LockedOn`.
- **전이 (v1에서 보완):**
  - `Walk`↔`WalkLockedOn`: `LockedOn==true`→`WalkLockedOn`, `LockedOn==false`→`Walk`.
  - **`WalkLockedOn`에 `Walk`가 이미 가진 이탈 전이를 그대로 복제**: `Run==true`→`Run` 직결, `Walk==false`→Exit(bubble up). (서브머신으로 묶는 대안 대신 복제 방식 채택 — 기존 `ground` 구조를 그대로 유지하기 위함). 이걸 빠뜨리면 락온 중 Walk가 멈추거나 Run으로 바뀔 때 `WalkLockedOn`이 멈춰버림(CodexBot 지적).
  - **`Idle`에 직결 전이 추가**: `Walk==true && LockedOn==true`→`WalkLockedOn` (기존 `Walk==true`→`Walk` 조건은 `LockedOn==false`로 분기). Idle에서 락온 중 이동 시작할 때 Basic Walk를 거쳤다가 다시 넘어가는 자세 튐 방지.
  - `LockedOn` 전이는 전부 `HasExitTime=false` + 짧은 `TransitionDuration`으로 즉시 반응하게 함.
- **C# (v1에서 보완):**
  - `LockedOn` bool 갱신 위치는 `PlayerWalkState`가 아니라 **`PlayerGroundState.Update()`**(Idle/Walk/Run/Avoid 공용 상위)에 둔다 — Idle에 머무는 동안에도 최신값이어야 위 "Idle→WalkLockedOn 직결 전이" 조건이 정확히 평가됨(CodexBot 지적: 갱신 위치가 Walk 안에만 있으면 stale).
  - `MoveX`/`MoveZ`는 원시 `MoveInput`이 아니라 **`GetMoveDir()`가 만드는 카메라 기준 월드 이동벡터(Y=0)를 `transform.InverseTransformDirection()`으로 캐릭터 로컬좌표 변환**한 값을 사용 — `.x`→`MoveX`, `.z`→`MoveZ`로 SetFloat(가능하면 damping 적용). `GetMoveDir()`이 현재 `PlayerBaseState`의 private 메서드라 protected로 노출 필요.

### 스코프
- 이번 작업은 **Walk만**. Run/Idle/Avoid 등 다른 로코모션은 범위 밖(Lock-On 폴더에 `RunXXX_HS` 클립도 이미 있어 추후 동일 패턴으로 확장 가능).
- `_HS_Start`/`_HS_Stop` 전환 전용 클립은 이번 스코프 제외, 루프 클립 4개만 우선 사용.

### 리스크/확인 필요
- `WalkLockedOn`의 4개 클립이 전투 자세(칼 든 자세)라 `Walk`(비전투 자세)와 전환 시 시각적 불일치가 있을 수 있음 — 우선 크로스페이드 블렌드(TransitionDuration)로 완화, 체감상 어색하면 추후 자세 전환 애니메이션(`StandIdle_To_FightIdle`류 패턴) 추가 검토.
- HS 클립 4개의 Loop Time / Loop Pose 임포트 설정 확인 필요(안 맞으면 블렌드 루프가 튐, CodexBot 지적).
- Blend Tree 파라미터는 순수 로컬 이동방향 계산이라 회피/공격 등 다른 상태 전이 로직(Rotate(), ChangeState 계열)과는 무관 — 기존 코드 변경 없음.
- 구현은 Animator Controller 애셋 편집(Unity MCP) + `PlayerGroundState`/`PlayerWalkState` 소폭 코드 수정.

### 검토 이력
- 2026-08-21: CodexBot 1차 검토 — WalkLockedOn 이탈 전이 누락, Idle 직결 전이 필요, LockedOn 갱신 위치, MoveX/MoveZ 소스, Blend 좌표/damping, ExitTime, Loop 설정 지적. 전부 반영해 v2로 갱신, 이견 없음([합의 요청]으로 종료).

### v2 → v3 재설계 (2026-08-21, 마스터 지시 — 라우팅 구조를 `ground`↔`Attack`/`Air` 패턴과 통일)

v2(위)는 `WalkLockedOn`에 `Walk`의 이탈 전이(Idle/Avoid/Exit)를 개별 복제하고, `Idle`/`Avoid`의 기존 `Walk==true` 조건에도 `LockedOn` 분기를 추가하는 방식이었음 — 실제 구현 후 마스터가 "마음에 안 든다"고 반려, 기존 `ground`↔`Attack`/`Air` 서브머신 라우팅 패턴과 다른 별도 방식이라 확장성이 떨어진다는 이유로 **B안(서브머신 중앙화)** 채택.

**구조 변경:**
- `ground` 안에 자식 서브머신 `LockOn` 신규 생성. `WalkLockedOn`(블렌드트리)은 그 안으로 이동(향후 `RunLockedOn` 등 추가 시 같은 자리에 들어감).
- **진입**: `Walk` 상태에만 트랜지션 1개 추가 — `LockedOn==true` → 서브머신 `LockOn` 직결(상태→서브머신, Exit 아님). `Idle`/`Avoid`는 전혀 안 건드림 — 락온 중 Idle/Avoid에서 이동 시작하면 기존처럼 일단 `Walk`로 간 뒤 같은 프레임에 `LockOn`으로 한 번 더 넘어감(Walk를 한 프레임 스치는 정도의 트레이드오프, v1의 "완전히 다른 클립" 문제보다는 훨씬 경미해서 감수).
- **이탈**: `WalkLockedOn` 내부에 조건별 Exit 3개(`Idle==true`/`Avoid==true`/`LockedOn==false`, 전부 bubble). `ground`의 `m_StateMachineTransitions`에 `LockOn` 서브머신 기준 라우팅 3갈래 등록(`Idle==true→Idle`, `Avoid==true→Avoid`, `LockedOn==false→Walk`) — `ground`↔`Attack` 라우팅과 완전히 동일한 구조.

**구현 메모(Unity Scripting API, 향후 참고용):**
- `AnimatorStateMachine.AddStateMachineTransition(sourceStateMachine)` **단일 인자 오버로드는 반환된 트랜지션에 `destinationState`를 나중에 대입해도 저장이 안 됨**(직렬화 배열에 반영 안 되는 버그성 동작 확인, 2026-08-21). **`AddStateMachineTransition(sourceStateMachine, destinationState)` 3인자 오버로드를 써야 정상 저장됨.**
- `AnimatorStateMachine.AddStateMachine(name)`으로 자식 서브머신 생성, `AnimatorStateMachine.defaultState`로 기본 진입 상태 지정.

**상태:** 구현 완료(Unity MCP), 컴파일/콘솔 에러 0건. 플레이모드 테스트 대기 중.

### v3 → v4: IdleLockedOn 추가 (2026-08-21, 마스터 지시)

정지 상태도 락온 중엔 전투 대기 자세(`Basic/FightIdle.FBX`)를 쓰도록 확장.

- `LockOn` 서브머신에 `IdleLockedOn` 신규(단일 클립, 블렌드트리 아님)
- `Idle`에도 `Walk`와 동일하게 `LockedOn==true`→`LockOn` 직결 진입 추가
- `LockOn` 내부에서 `IdleLockedOn`↔`WalkLockedOn`은 서로 직결(`Walk==true`/`Idle==true`) — ground의 Idle↔Walk와 동일 패턴. `WalkLockedOn`의 옛 `Idle==true→Exit`(ground의 Idle로 직접 bubble)는 이 직결로 대체하며 제거.
- 둘 다 `Run==true`→Exit 추가(RunLockedOn 아직 없어서 평범한 `Run`으로 이탈), `ground`의 `LockOn` 라우팅에 `Run==true→Run` 항목 신규.
- `Avoid==true`→Exit는 `WalkLockedOn`에만 유지(`Avoid`는 Walk 중에만 트리거되는 게 기존 C# 설계라 `IdleLockedOn`엔 불필요).
- `LockedOn==false` 탈출은 상태에 따라 갈 곳이 갈라져야 해서 `ground` 라우팅을 `Walk==true && LockedOn==false→Walk` / `Walk==false && LockedOn==false→Idle`로 세분화.
- `@Ground==false`(공격/점프 등 ground 자체 이탈)는 `IdleLockedOn`에도 추가, `ground`의 `LockOn` 라우팅에도 "더 위로 계속 bubble"용 Exit 엔트리 추가.
- **버그 발견·수정**: 위 작업 검증 중 `WalkLockedOn`에 애초에 `@Ground==false` 탈출 경로가 아예 없었던 걸 발견(B안 최초 구현 시 누락) — 있었다면 공격/점프 시 멈춰있었을 것. 추가해서 수정.

**상태:** 구현 완료(Unity MCP), 컴파일/콘솔 에러 0건, `WalkLockedOn`/`IdleLockedOn` 전이 세트 대칭 확인 완료. 플레이모드 테스트 대기 중.

### v4 → v5: AvoidLockedOn 추가 + 트랜지션 유실 버그 대응 (2026-08-21, 마스터 지시)

회피(닷지)도 `Action/Dodge_Front/Back/Left/Right.FBX`로 4방향 블렌드트리화.

- `LockOn`에 `AvoidLockedOn` 신규(블렌드트리, Walk/Idle과 동일 좌표)
- `Avoid`에도 `LockedOn==true`→(처음엔 `LockOn` 서브머신 직결로 넣었다가, 아래 이슈 발견 후) **`AvoidLockedOn` 상태로 직결**로 수정 — 서브머신 직결이면 `LockOn`의 default state(`WalkLockedOn`)로 들어가버려서 회피 중 락온 시작하는 극히 드문 케이스에 자세가 잠깐 잘못 나올 수 있었음(Idle/Walk는 왜인지 저장 시 자동으로 특정 state 직결로 resolve됐는데 Avoid만 안 됐음 — 정확한 원인 불명, 수동으로 직결 처리해서 통일)
- `AvoidLockedOn`↔`WalkLockedOn`/`IdleLockedOn` 내부 직결(`Walk==true`/`Idle==true`), `Run==true`/`LockedOn==false`/`@Ground==false`는 bubble
- `ground` 라우팅에 `Avoid==true && LockedOn==false→Avoid` 신규, 기존 Idle행 규칙에 `Avoid==false` 조건 보강(회피 중 락온 해제 시 잘못 Idle로 튀는 것 방지)

**⚠️ 정정(2026-08-21): "트랜지션 유실"은 유니티 버그가 아니었음.**
당시 "이전 호출에서 저장 확인했던 트랜지션이 다음 호출 땐 사라져있다"고 판단해서 원인을 유니티 직렬화 타이밍 문제로 단정하고, 관련 상태들의 트랜지션을 전부 지우고 재구성해버렸음. **실제로는 마스터가 같은 라이브 에디터 세션에서 Animator 창을 열고 직접 수동으로 수정하던 트랜지션들이었음** — 재구성 작업이 그 수동 편집을 통째로 덮어써서 마스터가 직접 되돌려야 했던 사고. 교훈은 [[feedback_unity_shared_editor_concurrent_edits]] 메모리에 기록. **핵심 원칙: 예상과 다른 상태를 발견하면 버그로 단정해 지우고 재구성하지 말고, 먼저 "혹시 방금 에디터에서 수정하셨나요?"라고 확인할 것.**

### v5 → v6: AvoidLockedOn 재작업 (2026-08-21, 마스터가 수동 복구 후 재지시)

마스터가 위 사고로 지워진 자신의 수동 편집분을 에디터 Undo로 복구. 그 결과 라우팅 구조가 v5 계획과는 다르게(더 단순하게) 정리되어 있었음 — **`ground` 레벨 공용 라우팅 테이블 방식 대신, 각 로코모션 상태가 자기 짝(`Idle`↔`IdleLockedOn`, `Walk`↔`WalkLockedOn`)으로 직접 연결되는 방식으로 단순화**(`LockedOn==false` 탈출도 각 LockedOn 상태에서 자기 짝 상태로 직결, `ground`의 라우팅 테이블은 `@Ground==false` bubble 하나만 남음). `Run` 처리도 이 복구본엔 없음(마스터가 의도적으로 뺐을 가능성 있어 임의로 안 건드림).

이 복구된 구조에 맞춰 `AvoidLockedOn`만 최소한으로 다시 추가:
- `AvoidLockedOn` 상태(블렌드트리) 재생성
- `Avoid`의 `LockedOn==true` 전이를 `AvoidLockedOn` 직결로(기존엔 서브머신 직결이었던 것을 Idle/Walk와 같은 패턴으로 통일)
- `WalkLockedOn`의 `Avoid==true` 전이 목적지를 평범한 `Avoid`→`AvoidLockedOn`으로 재조준
- `AvoidLockedOn`↔`WalkLockedOn`/`IdleLockedOn` 내부 직결, `AvoidLockedOn`→`Avoid`(`LockedOn==false`, 직결), `AvoidLockedOn`→Exit(`@Ground==false`)

마스터의 기존 편집분(Idle/Walk 쪽 전이)은 **전혀 건드리지 않고** 위 6개 항목만 추가/재조준.

**상태:** 구현 완료(Unity MCP), 컴파일/콘솔 에러 0건. fresh 재확인 완료. 플레이모드 테스트 대기 중.
(참고: 작업 중 `AssetDatabase.Refresh()`가 무관한 `StandIdle_Break.FBX`를 구버전 임포트 포맷에서 재임포트시켜 메타파일이 같이 변경됨 — 의도한 변경 아니지만 정상적인 임포터 갱신으로 보임.)
