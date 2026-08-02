# PlayerStateMachine — As-Is 설계

> 이 문서는 2026-07-31 시점 구현된 코드를 관찰해 작성했다. 실제 코드가 기준이며, 이 문서와 어긋나면 코드를 우선한다. 이후 상태/전이가 추가되면 이 문서를 갱신한다.

---

## 1. 구조

```
IState (interface)
  ├─ Enter() / Exit()
  ├─ HandleInput()
  ├─ Update()
  └─ PhysicsUpdate()

StateMachine (abstract)
  └─ ChangeState(IState) / HandleInput() / Update() / PhysicsUpdate() 를 currentState로 위임

PlayerStateMachine : StateMachine
  └─ Player 참조 + 상태 인스턴스(Idle/Walk/Run/Avoid/ComboAttack) + 공유 런타임 값 보유

PlayerBaseState : IState
  └─ 입력 콜백 등록/해제, 이동·회전, 애니메이션 Bool 헬퍼 공용 로직
  ├─ PlayerGroundState : PlayerBaseState
  │    ├─ PlayerIdleState
  │    ├─ PlayerWalkState
  │    ├─ PlayerRunState
  │    └─ PlayerAvoidState
  └─ PlayerAttackState : PlayerBaseState
       └─ PlayerComboAttackState
```

`Player.cs`(MonoBehaviour)가 `Awake()`에서 `PlayerStateMachine`을 생성하고, `Update()`/`FixedUpdate()`에서 각각 `HandleInput()+Update()` / `PhysicsUpdate()`를 위임 호출한다. 초기 상태는 `Start()`에서 `IdleState`로 진입.

## 2. PlayerStateMachine 공유 필드

| 필드 | 용도 |
|---|---|
| `MoveInput` (Vector2) | 최신 이동 입력값 |
| `MoveSpeed` | `Data.GroundData.BaseSpeed` 초기값 |
| `MoveSpeedModifier` | 상태별 배율(Idle=0, Walk=`WalkSpeed`, Run=`RunSpeed`, Attack=0) |
| `RotationDamping` | 카메라 forward 기준 회전 보간 속도 |
| `IsAttacking` | Attack 입력 performed~canceled 사이 true |
| `ComboIndex` | 다음 진입할 콤보 단계 인덱스 (`PlayerAttackData.AttackDatas` 인덱스) |
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

Avoid ──(0.3s 코루틴 종료 시점, AvoidRun 누르고 있고 이동입력 있음)──> Run
Avoid ──(코루틴 종료, 이동입력만 있음)──> Walk
Avoid ──(코루틴 종료, 둘다 없음)──> Idle

ComboAttack ──(애니메이션 normalizedTime>=1, 콤보 성사)──> ComboAttack (ComboIndex 갱신, 재진입)
ComboAttack ──(normalizedTime>=1, 콤보 미성사)──> Idle
```

- `PlayerGroundState`는 이동 입력이 끊긴 뒤 `moveInputGracePeriod`(0.2s) 동안 재입력을 기다렸다가 없으면 Idle로 전이(입력 튐 방지).
- `PlayerAvoidState`는 상태 진입과 동시에 코루틴(`WaitForSeconds(0.3f)`)을 걸어 회피 애니메이션 종료 시점에 다음 상태를 결정한다. 이 0.3초는 `PlayerGroundData.avoid2runTransitionTime`(현재 값 0.5)과 다른 하드코딩 값이라 불일치— 백로그 참조.
- `PlayerAttackState`(기본)와 `PlayerComboAttackState`(콤보 로직 실장) 두 클래스가 있지만, 현재 `PlayerGroundState.OnAttack()`이 항상 `ComboAttackState`로만 전이시켜 순수 `PlayerAttackState`는 진입 경로가 없음(콤보 아닌 단발 공격 상태로 쓸 목적이었던 것으로 추정, 미확정).

## 4. 콤보 진행 로직 (`PlayerComboAttackState`)

- `Enter()`: `stateMachine.ComboIndex`로 `AttackInfo` 조회, Animator의 `Combo` int 파라미터 세팅, `alreadyApplyCombo`/`alreadyAppliedForce` 플래그 초기화.
- `Update()`: 애니메이션 정규화 시간(`GetNormalizedTime(.., "Attack")`) 기준으로
  - `attackInfo.ForceTransitionTime` 도달 시 1회 전진력 적용(`TryApplyForce` → `ForceReceiver.AddForce`)
  - `attackInfo.ComboTransitionTime` 도달 후 입력이 남아있으면(`IsAttacking`) 콤보 확정(`TryComboAttack`)
  - `normalizedTime>=1`(애니메이션 종료)에 콤보 확정 여부로 다음 콤보 상태 재진입 또는 Idle 복귀.
- `Exit()`: 콤보 확정 안 된 채 종료되면 `ComboIndex`를 0으로 리셋.

## 5. 미구현/열린 이슈

- 공중 상태(Jump/Fall) 없음 — `PlayerAnimationData`에 `Jump`/`Fall` 파라미터, `PlayerAirData.JumpForce`가 정의돼 있지만 어떤 State도 사용하지 않음.
- `PlayerAttackState`(비콤보)의 실제 사용처 없음.
- 회피 종료 대기시간(0.3s 하드코딩) vs `avoid2runTransitionTime`(0.5, 데이터 정의만 있고 미사용) 불일치.
- 공격 판정(히트박스/데미지 적용)은 이 상태머신 범위 밖 — `Design/CombatData_Design.md` 참조.
