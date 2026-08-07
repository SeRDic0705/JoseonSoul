# PlayerStateMachine — As-Is 설계

> 이 문서는 2026-07-31 시점 구현된 코드를 관찰해 작성했다(2026-08-06 입력구독 안정화 작업분, 이후 Animation Event 릴레이/버퍼 작업분 반영해 갱신). 실제 코드가 기준이며, 이 문서와 어긋나면 코드를 우선한다. 이후 상태/전이가 추가되면 이 문서를 갱신한다.

---

## 1. 구조

```
IState (interface)
  ├─ Enter() / Exit()
  ├─ HandleInput()
  ├─ Update()
  └─ PhysicsUpdate()

StateMachine (abstract)
  ├─ ChangeState(IState) / HandleInput() / Update() / PhysicsUpdate() 를 currentState로 위임
  └─ CurrentState (get) — 2026-08-06 추가, 동프레임 이중전이 가드용

PlayerStateMachine : StateMachine
  └─ Player 참조 + 상태 인스턴스(Idle/Walk/Run/Avoid/ComboAttack/DodgeAttack) + 공유 런타임 값 보유

PlayerBaseState : IState
  └─ 입력 콜백 등록/해제, 이동·회전, 애니메이션 Bool 헬퍼 공용 로직, CanBeInterruptedByAttack(virtual)
  ├─ PlayerGroundState : PlayerBaseState
  │    ├─ PlayerIdleState
  │    ├─ PlayerWalkState
  │    ├─ PlayerRunState
  │    └─ PlayerAvoidState (CanBeInterruptedByAttack = false)
  └─ PlayerAttackState : PlayerBaseState, IForceEventReceiver
       ├─ PlayerComboAttackState : IComboWindowEventReceiver 추가 구현
       └─ PlayerDodgeAttackState (2026-08-06 신규, IForceEventReceiver만 — 콤보 창 개념 없음)

IForceEventReceiver / IComboWindowEventReceiver (interface, attack-events 브랜치 신규)
  └─ AttackAnimationEventRelay (MonoBehaviour, Animator 있는 GameObject(`Player/SKM_Solider_Fist`)에 부착)
       └─ Animation Event가 호출 → Player.StateMachine.CurrentState를 해당 인터페이스로 캐스팅해 전달, 미구현이면 무시
```

`Player.cs`(MonoBehaviour)가 `Awake()`에서 `PlayerStateMachine`을 생성하고, `Update()`/`FixedUpdate()`에서 각각 `HandleInput()+Update()` / `PhysicsUpdate()`를 위임 호출한다. 초기 상태는 `Start()`에서 `IdleState`로 진입.

## 2. PlayerStateMachine 공유 필드

| 필드 | 용도 |
|---|---|
| `MoveInput` (Vector2) | 최신 이동 입력값 |
| `MoveSpeed` | `Data.GroundData.BaseSpeed` 초기값 |
| `MoveSpeedModifier` | 상태별 배율(Idle=0, Walk=`WalkSpeed`, Run=`RunSpeed`, Attack=0) |
| `RotationDamping` | 카메라 forward 기준 회전 보간 속도 |
| `IsAttacking` | Attack 입력 performed~canceled 사이 true (누르고 있는 동안 유지 — 콤보 체인 판단용, 버퍼 아님) |
| `ComboIndex` | 다음 진입할 콤보 단계 인덱스 (`PlayerAttackData.AttackDatas` 인덱스) |
| `AttackQueued` / `AttackQueuedTime` | 2026-08-06 추가. 공격 입력이 눌린 순간 원샷으로 큐잉되는 진짜 버퍼(유효시간 0.2초). `CanBeInterruptedByAttack=false`인 상태(Avoid)에서 눌린 공격을 상태 종료 시점에 소비하기 위함 — `IsAttacking`과 역할이 다름. `PlayerComboAttackState`의 콤보 창 래치에도 재사용(§4-2) — **읽어서 래치 판단에 쓰는 즉시 그 자리에서 소비(`false`로)하는 규칙을 모든 사용처에서 통일** |
| `JumpForce` | 선언은 있으나 현재 어떤 State도 사용하지 않음(공중 상태 미구현) |
| `MainCameraTransform` | 이동 방향 계산 기준(카메라 forward/right 평면 투영) |

## 3. 상태 전이 그래프 (현재 구현분)

```
Idle ──(이동입력, AvoidRun 안누름)──> Walk
Idle ──(이동입력, AvoidRun 누르고 있음)──> Run
Idle ──(IsAttacking)──> ComboAttack (via PlayerGroundState.Update → OnAttack)

Walk ──(AvoidRun.started)──> Avoid
Walk ──(이동입력 사라짐, 유예 0.2s 후)──> Idle
Walk ──(IsAttacking)──> ComboAttack

Run ──(AvoidRun 뗌 + 이동입력 있음)──> Walk
Run ──(AvoidRun 뗌 + 이동입력 없음)──> Idle
Run ──(IsAttacking)──> ComboAttack

Avoid ──(회피 중엔 공격으로 인터럽트 안 됨, CanBeInterruptedByAttack=false)
Avoid ──(0.3s 코루틴 종료 시점, AttackQueued 유효)──> DodgeAttack (버퍼 소비)
Avoid ──(코루틴 종료, 버퍼 없음, AvoidRun 누르고 있고 이동입력 있음)──> Run
Avoid ──(코루틴 종료, 버퍼 없음, 이동입력만 있음)──> Walk
Avoid ──(코루틴 종료, 버퍼 없음, 둘다 없음)──> Idle

ComboAttack ──(애니메이션 normalizedTime>=1, 콤보 성사)──> ComboAttack (ComboIndex 갱신, 재진입)
ComboAttack ──(normalizedTime>=1, 콤보 미성사)──> Idle

DodgeAttack ──(애니메이션 normalizedTime>=1, IsAttacking 유지 중)──> ComboAttack (ComboIndex = DodgeAttackInfo.ComboStateIndex)
DodgeAttack ──(normalizedTime>=1, IsAttacking 없음)──> Idle
```

- `PlayerGroundState`는 이동 입력이 끊긴 뒤 `moveInputGracePeriod`(0.2s) 동안 재입력을 기다렸다가 없으면 Idle로 전이(입력 튐 방지).
- `PlayerAvoidState`는 상태 진입과 동시에 코루틴(`WaitForSeconds(0.3f)`)을 걸어 회피 애니메이션 종료 시점에 다음 상태를 결정한다. 이 0.3초는 `PlayerGroundData.avoid2runTransitionTime`(현재 값 0.5)과 다른 하드코딩 값이라 불일치— 백로그 참조.
- `PlayerAttackState`(기본)와 `PlayerComboAttackState`(콤보 로직 실장) 두 클래스가 있지만, 현재 `PlayerGroundState.OnAttack()`이 항상 `ComboAttackState`로만 전이시켜 순수 `PlayerAttackState`는 진입 경로가 없음(콤보 아닌 단발 공격 상태로 쓸 목적이었던 것으로 추정, 미확정).

### 3-1. 동프레임 이중전이 가드 (컨벤션, 2026-08-06)

`base.Update()` 호출 중 상태 전이가 일어나도, 호출한 쪽(예: `PlayerIdleState.Update()`)은 그 사실을 모른 채 자기 코드를 계속 실행해 같은 프레임에 다시 전이를 덮어쓸 수 있다(실제 버그로 발견됨). **`base.Update()`가 내부에서 `ChangeState`를 호출할 수 있는 오버라이드에서는 직후에 반드시 아래 가드를 넣는다:**

```csharp
public override void Update()
{
    base.Update();
    if (stateMachine.CurrentState != this) return;   // 이미 다른 상태로 전이됨 — 여기서 멈춤
    ...
}
```

현재 `PlayerIdleState`, `PlayerRunState`에 적용돼 있다. 새 상태를 추가할 때 `Update()`에서 `base.Update()` 이후 추가 로직이 있다면 이 가드를 함께 넣을 것.

### 3-2. 공격 인터럽트 허용 여부 선언 (컨벤션, 2026-08-06)

상태별로 공격 입력이 즉시 전이를 일으켜도 되는지를 `protected virtual bool CanBeInterruptedByAttack => true;`(`PlayerBaseState`, 기본값 true)로 선언한다. `PlayerGroundState.Update()`는 `if (IsAttacking && CanBeInterruptedByAttack) OnAttack();`로 이 값을 확인한다. 회피처럼 인터럽트되면 안 되는 상태는 `false`로 오버라이드하고, 대신 `AttackQueued` 버퍼로 입력을 보존했다가 상태 종료 시점에 소비한다(예: `PlayerAvoidState.WaitForAvoidEnd()`). 향후 피격/처형 등 인터럽트 불가 상태를 추가할 때도 이 프로퍼티만 선언하면 된다.

## 4. 콤보 진행 로직 (`PlayerComboAttackState`)

- `Enter()`: `stateMachine.ComboIndex`로 `AttackInfo` 조회, Animator의 `Combo` int 파라미터 세팅, `alreadyApplyCombo`/이벤트 가드 플래그(§4-2) 전부 초기화.
- `Update()`: 애니메이션 정규화 시간(`GetNormalizedTime(.., "Attack")`) 기준으로
  - `attackInfo.ForceTransitionTime` 도달 시(이벤트 미수신이면 폴백으로) 1회 전진력 적용(`OnApplyForce` → `ForceReceiver.AddForce`)
  - `attackInfo.ComboTransitionTime` 도달 시(이벤트 미수신이면 폴백으로) 콤보 창 오픈(`OnOpenComboWindow`), 오픈~클로즈 구간엔 입력(`IsAttacking` 또는 신선한 `AttackQueued`)을 계속 감시해 `comboRequested` 래치
  - `normalizedTime>=1`(애니메이션 종료)에 콤보 창 클로즈(`OnCloseComboWindow`) 후, `comboRequested` 확정 여부로 다음 콤보 상태 재진입 또는 Idle 복귀.
- `Exit()`: 콤보 확정 안 된 채 종료되면 `ComboIndex`를 0으로 리셋. 이벤트 가드 플래그는 **여기서 리셋하지 않음**(§4-2 참조).

### 4-2. Animation Event 릴레이/버퍼 (2026-08-06 신규, `attack-events` 브랜치)

**문제의식:** 전진력/콤보 창 타이밍이 `ForceTransitionTime`/`ComboTransitionTime` 같은 정규화 시간 숫자로 코드에 박혀 있어, 클립이 바뀔 때마다(예: `knight` 브랜치의 Sword and Shield Pack 교체) 숫자를 재튜닝해야 했다. 이를 클립 자체에 심는 Animation Event 마커로 옮기되, **아직 클립에 마커가 없으므로 기존 숫자 기반 폴백을 안전장치로 그대로 유지**한다(이번 작업은 순수 구조 변경 — 플레이 감각 변화 없음).

**구조:**
- `IForceEventReceiver`(`OnApplyForce`) / `IComboWindowEventReceiver`(`OnOpenComboWindow`/`OnCloseComboWindow`) 인터페이스. `PlayerAttackState`(베이스)가 `IForceEventReceiver`를 구현해 Force 로직을 공유하고, `PlayerComboAttackState`만 추가로 `IComboWindowEventReceiver`를 구현한다 — `PlayerDodgeAttackState`는 콤보 창 개념이 없는 단발 공격이라 Force만.
- `AttackAnimationEventRelay`(MonoBehaviour, `Player/SKM_Solider_Fist`의 Animator에 부착)가 Animation Event 콜백을 받아 `player.StateMachine.CurrentState`가 해당 인터페이스를 구현하면 전달, 아니면 무시(상태가 이미 바뀌었거나 공격 상태가 아닌 경우 안전).

**이벤트/폴백 배타 실행:**
- `forceHandled`(베이스)/`openHandled`/`closeHandled`(콤보) 3개 플래그로 각자 독립 관리. 이벤트가 먼저 오면 플래그가 세팅되고, 이후 같은 프레임이든 나중이든 폴백(정규화 시간 임계값 체크)이 자동 스킵된다.
- 폴백은 새 데이터 필드 없이 기존 `ForceTransitionTime`(Force)/`ComboTransitionTime`(Open) 임계값을 재사용, Close 폴백은 애니메이션 종료 시점(`normalizedTime>=1`).
- **폴백 임계값 = 최종 보장 시점(deadline), Animation Event = 그보다 이른 전환을 위한 가속 신호.** 임계값 이후 도착한 이벤트는 무시된다 — 의도적으로 전환을 늦추려면 이벤트 위치뿐 아니라 `ForceTransitionTime`/`ComboTransitionTime` 기본값도 함께 늦춰야 한다. 같은 프레임 내 이벤트·폴백 실행 순서에는 의존하지 않는다.
- `OnOpenComboWindow()`는 `openHandled || closeHandled`일 때 무시 — Close 폴백 이후 늦게 도착한 Open 이벤트가 창을 되살리는 것을 방지.
- 이벤트 가드 플래그는 **`Enter()`에서만 초기화, `Exit()`에서는 리셋하지 않는다** — 크로스페이드 블렌드 중 이전 클립의 늦은 이벤트가 다음 상태 진입 이후 도착해도, 이미 세팅된 가드가 막아준다.

**콤보 버퍼(`comboRequested`):**
- 회피공격용으로 만든 `AttackQueued`/`AttackQueuedTime`(§2) 버퍼를 재사용. "신선함"(콤보 창이 열리는 시점 기준 유효시간 이내)이 래치 트리거가 된 바로 그 순간 `AttackQueued=false`로 소비 — `Update()`의 지속 감시 경로와 `OnOpenComboWindow()` 경로 둘 다 동일 규칙.
- 콤보 창이 열려 있는 동안(`comboWindowOpen`)에는 매 프레임 `IsAttacking`(계속 누르고 있는지)도 확인해 `comboRequested`를 래치할 수 있다.
- `OnCloseComboWindow()`에서 `comboRequested && attackInfo.ComboStateIndex != -1`이면 `alreadyApplyCombo = true`로 확정.

**Animation Event 심기 완료(2026-08-07).** `attack_01/02/03.FBX`의 `ModelImporter.clipAnimations[0].events`에 `OnApplyForce`/`OnOpenComboWindow`/`OnCloseComboWindow` 3개씩 추가(`DodgeAttack_PLACEHOLDER`는 attack_01 재사용이라 자동 적용). 이벤트 위치는 기존 폴백 임계값과 동일한 정규화 시간으로 맞췄다(1·2타: Force 0.3/Open 0.5, 3타: Force 0/Open 0.5, 공통 Close 0.95) — **타이밍 감각은 이전과 동일, 실행 경로만 폴백→이벤트로 넘어감.**

> **주의:** `ModelImporterClipAnimation.events`의 `time` 필드는 초 단위가 아니라 **정규화 시간(0~1, `AnimationEvent.time` 자체가 클립 길이 기준 비율)**이다. 처음에 초 단위(예: 0.2초)로 넣었더니 재임포트 후 실제 클립엔 `0.2 × clip.length`(더 짧은 값)로 들어가는 걸 발견해 정정했다 — 다음에 이 클립들 손댈 때 실수하지 않도록 남겨둠.

향후 이벤트 위치를 조정하고 싶으면 위 정규화 시간 값을 바꿔서 같은 방식(ModelImporter 재설정 + SaveAndReimport)으로 재적용하면 된다. 코드/폴백 임계값은 건드릴 필요 없음.

## 4-1. 회피공격 (`PlayerDodgeAttackState`, 2026-08-06 신규)

회피 중 눌린 공격 입력이 `AttackQueued` 버퍼로 보존됐다가 회피 종료 시 소비되면 진입. `PlayerComboAttackState`와 동일 패턴(정규화 시간 `"Attack"` 태그 기준, `DodgeAttackInfo.ForceTransitionTime`에 전진력 1회 적용)이되 별도 데이터(`PlayerAttackData.DodgeAttackInfo`)와 별도 Animator 파라미터(`DodgeAttackParameterHash`)를 쓴다. 종료 시 `IsAttacking`(공격키를 계속 누르고 있는지)이 true면 `ComboAttackState`로 체인(`ComboIndex = DodgeAttackInfo.ComboStateIndex`), 아니면 Idle.

**애니메이션은 현재 플레이스홀더.** `DodgeAttackInfo`는 콤보 1타(`AttackDatas[0]`) 값을 그대로 복사해 초기화했고, Animator Controller(`Assets/Animations/PlayerAnimator.controller`)의 `Attack` 서브스테이트머신에 `DodgeAttack_PLACEHOLDER` 상태를 추가해 `anim_attack_light_01`과 동일한 클립을 재생한다. Attack SM 진입 시 `DodgeAttack` bool이 true면 이 상태로, 아니면 기존처럼 `anim_attack_light_01`(default)로 들어간다. 전용 회피공격 클립이 준비되면 `DodgeAttack_PLACEHOLDER` 상태의 Motion만 교체하면 되고, C# 코드는 손댈 필요 없다.

## 5. 미구현/열린 이슈

- 공중 상태(Jump/Fall) 없음 — `PlayerAnimationData`에 `Jump`/`Fall` 파라미터, `PlayerAirData.JumpForce`가 정의돼 있지만 어떤 State도 사용하지 않음.
- `PlayerAttackState` 자체는 여전히 직접 인스턴스화되지 않음(콤보 없는 단발 공격 상태가 생기면 후보) — 다만 2026-08-06부터 Force 이벤트 공통 로직(`OnApplyForce`)의 베이스로 실제 사용 중.
- 회피 종료 대기시간(0.3s 하드코딩) vs `avoid2runTransitionTime`(0.5, 데이터 정의만 있고 미사용) 불일치.
- 공격 판정(히트박스/데미지 적용)은 이 상태머신 범위 밖 — `Design/CombatData_Design.md` 참조.
- `DodgeAttack_PLACEHOLDER` Animator 상태는 전용 클립 없이 콤보 1타 클립을 재사용 중 — 전용 회피공격 애니메이션(구르며 찌르기 등) 준비되면 교체 필요.
