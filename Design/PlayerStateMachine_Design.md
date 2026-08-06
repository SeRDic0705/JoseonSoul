# PlayerStateMachine — As-Is 설계

> 이 문서는 2026-07-31 시점 구현된 코드를 관찰해 작성했다(2026-08-06 입력구독 안정화 작업분 반영해 갱신). 실제 코드가 기준이며, 이 문서와 어긋나면 코드를 우선한다. 이후 상태/전이가 추가되면 이 문서를 갱신한다.

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
  └─ PlayerAttackState : PlayerBaseState
       ├─ PlayerComboAttackState
       └─ PlayerDodgeAttackState (2026-08-06 신규)
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
| `AttackQueued` / `AttackQueuedTime` | 2026-08-06 추가. 공격 입력이 눌린 순간 원샷으로 큐잉되는 진짜 버퍼(유효시간 0.2초). `CanBeInterruptedByAttack=false`인 상태(Avoid)에서 눌린 공격을 상태 종료 시점에 소비하기 위함 — `IsAttacking`과 역할이 다름 |
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

- `Enter()`: `stateMachine.ComboIndex`로 `AttackInfo` 조회, Animator의 `Combo` int 파라미터 세팅, `alreadyApplyCombo`/`alreadyAppliedForce` 플래그 초기화.
- `Update()`: 애니메이션 정규화 시간(`GetNormalizedTime(.., "Attack")`) 기준으로
  - `attackInfo.ForceTransitionTime` 도달 시 1회 전진력 적용(`TryApplyForce` → `ForceReceiver.AddForce`)
  - `attackInfo.ComboTransitionTime` 도달 후 입력이 남아있으면(`IsAttacking`) 콤보 확정(`TryComboAttack`)
  - `normalizedTime>=1`(애니메이션 종료)에 콤보 확정 여부로 다음 콤보 상태 재진입 또는 Idle 복귀.
- `Exit()`: 콤보 확정 안 된 채 종료되면 `ComboIndex`를 0으로 리셋.

## 4-1. 회피공격 (`PlayerDodgeAttackState`, 2026-08-06 신규)

회피 중 눌린 공격 입력이 `AttackQueued` 버퍼로 보존됐다가 회피 종료 시 소비되면 진입. `PlayerComboAttackState`와 동일 패턴(정규화 시간 `"Attack"` 태그 기준, `DodgeAttackInfo.ForceTransitionTime`에 전진력 1회 적용)이되 별도 데이터(`PlayerAttackData.DodgeAttackInfo`)와 별도 Animator 파라미터(`DodgeAttackParameterHash`)를 쓴다. 종료 시 `IsAttacking`(공격키를 계속 누르고 있는지)이 true면 `ComboAttackState`로 체인(`ComboIndex = DodgeAttackInfo.ComboStateIndex`), 아니면 Idle.

**애니메이션은 현재 플레이스홀더.** `DodgeAttackInfo`는 콤보 1타(`AttackDatas[0]`) 값을 그대로 복사해 초기화했고, Animator Controller(`Assets/Animations/PlayerAnimator.controller`)의 `Attack` 서브스테이트머신에 `DodgeAttack_PLACEHOLDER` 상태를 추가해 `anim_attack_light_01`과 동일한 클립을 재생한다. Attack SM 진입 시 `DodgeAttack` bool이 true면 이 상태로, 아니면 기존처럼 `anim_attack_light_01`(default)로 들어간다. 전용 회피공격 클립이 준비되면 `DodgeAttack_PLACEHOLDER` 상태의 Motion만 교체하면 되고, C# 코드는 손댈 필요 없다.

## 5. 미구현/열린 이슈

- 공중 상태(Jump/Fall) 없음 — `PlayerAnimationData`에 `Jump`/`Fall` 파라미터, `PlayerAirData.JumpForce`가 정의돼 있지만 어떤 State도 사용하지 않음.
- `PlayerAttackState`(비콤보)의 실제 사용처 없음.
- 회피 종료 대기시간(0.3s 하드코딩) vs `avoid2runTransitionTime`(0.5, 데이터 정의만 있고 미사용) 불일치.
- 공격 판정(히트박스/데미지 적용)은 이 상태머신 범위 밖 — `Design/CombatData_Design.md` 참조.
- `DodgeAttack_PLACEHOLDER` Animator 상태는 전용 클립 없이 콤보 1타 클립을 재사용 중 — 전용 회피공격 애니메이션(구르며 찌르기 등) 준비되면 교체 필요.
