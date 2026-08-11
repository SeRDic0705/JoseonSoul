# 공중 상태 (Jump/Fall) — 설계안 (초안, 구현 전)

> `Design/PlayerStateMachine_Design.md` §5 "미구현/열린 이슈"의 공중 상태 항목을 실제로 구현하기 위한 계획. 아직 코드 작업 전, Discord 승인 대기 중.

## 1. 배경 / 현재 상태

- `PlayerAnimationData`에 `@Air`(레이어 bool 추정), `Jump`, `Fall` 파라미터 해시가 이미 선언돼 있으나 어떤 State도 참조하지 않음.
- `PlayerAirData.JumpForce`(Range 0~25, 기본 5) 데이터도 선언만 있고 미사용.
- `ForceReceiver.Jump(float)`도 구현은 돼 있으나 호출부가 없음. `ForceReceiver.Update()`의 중력 로직(`verticalVelocity`, `Controller.isGrounded` 기반 리셋)은 이미 점프/낙하를 지원할 수 있는 형태로 짜여 있어 **추가 변경 불필요**.
- 입력 액션(`InputActions.inputactions` Player 맵)에 Jump 액션이 없음 — Move/Look/Attack/Avoid&Run만 존재. `Avoid&Run`은 `LeftShift`+게임패드 `leftStickPress`, `Attack`은 마우스 좌클릭+게임패드 `buttonWest`를 씀 → Space bar 미사용 상태라 Jump에 배정 가능.

## 2. 목표 범위 (이번 작업)

- 지상 상태(Idle/Walk/Run)에서 점프 입력 시 `PlayerJumpState` 진입, 정점 이후 자동으로 `PlayerFallState`로 전환.
- 발판이 없어져 걷다가 떨어지는 경우(점프 없이) 자연스럽게 `PlayerFallState` 진입.
- 착지 시 현재 이동 입력/AvoidRun 홀드 상태를 보고 Idle/Walk/Run 중 복귀.
- **범위 밖(다음 과제로 미룸):** 공중 공격, 공중 회피, 이중 점프, 코요테 타임/입력 버퍼링 — 기존 프로젝트가 "필요해지면 그때 추가" 기조라 최소 구현만 우선.

## 3. 구조 설계

```
PlayerBaseState
  ├─ PlayerGroundState (기존)
  │    ├─ Idle / Walk / Run / Avoid
  └─ PlayerAirState (신규, PlayerGroundState와 형제 — 공용 로직만 보유)
       ├─ PlayerJumpState (신규)
       └─ PlayerFallState (신규)
```

- `PlayerAirState`: Enter에서 `@Air` bool true, Exit에서 false (Ground의 `@Ground` bool 패턴과 동일). `PhysicsUpdate`에서 착지 감지(`Controller.isGrounded && ForceReceiver.Movement.y <= 0`) 시 착지 후 상태 결정 로직 호출.
- `PlayerJumpState : PlayerAirState`: Enter 시 `ForceReceiver.Jump(Data.AirData.JumpForce)` 1회 호출 + `Jump` bool true. verticalVelocity가 0 이하로 꺾이면(정점 통과) `PlayerFallState`로 전이.
- `PlayerFallState : PlayerAirState`: Enter 시 `Fall` bool true. 점프 없이 지상 상태에서 바로 진입하는 경로도 있음(아래 §4).
- **착지 후 복귀 상태 결정 로직 중복 제거:** 현재 `PlayerAvoidState.WaitForAvoidEnd()`에 있는 "AvoidRun 홀드+MoveInput 보고 Run/Walk/Idle 결정" 로직을 `PlayerGroundState`(또는 `PlayerBaseState`)의 `protected` 헬퍼로 뽑아서 Avoid 종료 시점과 착지 시점 둘 다 재사용. (기존 로직 동작은 그대로, 중복만 제거)

## 4. 전이 규칙

| 출발 | 조건 | 도착 |
|---|---|---|
| Idle/Walk/Run | Jump 입력, `CanJump` (신규 virtual, 기본 true) | Jump |
| Jump | `ForceReceiver` 수직속도 ≤ 0 (정점 통과) | Fall |
| Idle/Walk/Run | `!Controller.isGrounded` (점프 없이 낙하 시작 — 벼랑 등) | Fall |
| Jump/Fall | `Controller.isGrounded` (착지) | Idle/Walk/Run (§3 공용 헬퍼로 결정) |

- `CanJump`은 `CanBeInterruptedByAttack`과 동일한 컨벤션으로 `PlayerBaseState`에 가상 프로퍼티로 선언(기본 true), `PlayerAvoidState`/공격 상태들은 `false`로 오버라이드(회피 중·공격 중 점프 금지). `PlayerAttackState` 계열은 애초에 `PlayerGroundState`가 아니라 점프 입력 체크 경로 자체가 없음 — 자연히 배제됨.
- Jump 입력 콜백은 `Attack`과 동일하게 `PlayerBaseState.AddInputActionsCallback`에 등록(모든 상태 공통), `CanJump` 체크 후 즉시 전이(Attack의 버퍼링 패턴과 달리 점프는 유지 입력 개념이 없으므로 단순 performed 콜백으로 충분).

## 5. 데이터/애니메이터 변경

- **Input Actions:** `Player` 맵에 `Jump`(Button) 액션 신규 추가, 바인딩 `<Keyboard>/space` + `<Gamepad>/buttonSouth`. 프로젝트 세팅(New Input System) 상 InputActions.cs는 Unity 에디터가 재임포트 시 자동 재생성 — 코드에서 직접 건드리지 않음.
- **Animator Controller (`PlayerAnimator.controller`):** 기존 `@Ground`/`@Attack` bool + 서브스테이트머신 패턴을 그대로 따라 `@Air` bool 서브스테이트머신 신설(Jump/Fall 두 상태, 각각 `Jump`/`Fall` bool로 진입). 정확한 기존 Ground 서브스테이트머신 구조는 구현 단계에서 Unity MCP로 직접 인스펙터 확인 후 동일 패턴으로 미러링(현재 텍스트 검색만으론 GUID 참조라 세부 확인 어려움).
- **코드 변경 없이 재사용:** `ForceReceiver`(점프/중력 이미 지원), `PlayerAirData.JumpForce`, `PlayerAnimationData`의 `Air`/`Jump`/`Fall` 해시 — 전부 기존 선언 그대로 사용.

## 6. 열린 질문 / 확인 필요

1. 점프 키를 Space bar로 배정하는 것 확인 필요 (게임패드는 South 버튼 — 일반적인 점프 배정).
2. 착지 판정 프레임 오차(CharacterController.isGrounded가 간헐적으로 1프레임 흔들리는 Unity 특성) — 기존 `ForceReceiver`도 같은 값을 신뢰하고 있어 이번 작업에서 별도 보정(코요테 타임 등)은 넣지 않을 예정, 문제 되면 추후 튜닝.
3. Attack 중 발판이 사라지는 경우(허공에서 공격 애니메이션 계속 재생) 등은 이번 범위 밖 — 나중에 공중 공격 설계 시 다룸.

## 7. 공중 공격 (2026-08-11 마스터 요청 추가)

지상 콤보(`PlayerComboAttackState`)와 완전히 동일한 3단 콤보 구조를 공중에도 별도 체인으로 추가한다.

- **데이터:** `PlayerAttackData`에 `AirAttackDatas: List<AttackInfo>`(3단) 신규 필드 추가. `AttackInfo` 구조체(ComboStateIndex/ComboTransitionTime/ForceTransitionTime/Force/Damage)는 그대로 재사용 — 지상용과 스키마 동일, 값만 별도 세트.
- **상태:** `PlayerAirAttackState`(`PlayerAttackState`와 형제, `PlayerBaseState` 직속 + `IForceEventReceiver`) → `PlayerAirComboAttackState`(`IComboWindowEventReceiver` 추가). 지상 Attack/ComboAttack과 완전히 대칭 구조. `PlayerAirState`를 상속하지 않음 — 착지 자동판정 로직(`ChangeToLocomotionState`)이 공격 도중 끼어들면 안 되기 때문(지상 공격이 `PlayerGroundState`가 아니라 `PlayerBaseState` 바로 아래 있는 것과 같은 이유).
- **진입:** Jump/Fall 상태에서 공격 입력이 들어오면(지상과 동일하게 `IsAttacking && CanBeInterruptedByAttack` 체크) 바로 `AirComboAttackState`로 전이. 이 체크 자체는 `PlayerBaseState`로 끌어올려서 지상/공중 양쪽이 공유(현재 `PlayerGroundState`에만 있던 걸 공용화).
- **콤보 진행/버퍼링/이벤트:** `PlayerComboAttackState`의 콤보창(Open/Close)·`AttackQueued` 버퍼·Animation Event/폴백 배타 실행 로직을 그대로 재사용(복붙 아니라 거의 동일 로직이라 공용화 여지 있음 — 리팩터는 구현 시점에 판단). `ComboIndex` 필드도 지상과 공유(지상 콤보 중엔 공중에, 공중 콤보 중엔 지상에 있을 일이 없어서 안전).
- **중력 정지:** `ForceReceiver`에 `SuspendGravity()`/`ResumeGravity()` 신규 추가. `PlayerAirAttackState.Enter()`에서 `SuspendGravity()`(수직속도 0으로 고정) + `MoveSpeedModifier=0`(지상 공격과 동일하게 이동 봉쇄), `Exit()`에서 `ResumeGravity()`. 콤보가 이어져서 `AirComboAttackState`를 재진입해도 Exit→Enter가 같은 프레임 내 동기 호출이라 체감상 계속 떠있는 상태 유지.
- **종료:** 애니메이션 종료 시점에 콤보 미확정(입력 없음) 또는 3타(`ComboStateIndex==-1`)까지 끝나면 `PlayerFallState`로 전이(중력 재개, 그 지점부터 다시 낙하 시작 → 이후 착지 판정은 기존 Fall 로직 그대로).

## 8. CodexBot 교차검증 반영 (2026-08-11 확정)

`Design/AirState_Design.md` 초안(§1~7)을 CodexBot과 Discord에서 교차검증. 아래는 그 결과 확정된 사항 — §1~7과 배치되는 부분은 이 섹션이 우선.

**8-1. 전체 전이표(누락분 포함, 확정)**

| 출발 | 조건 | 도착 |
|---|---|---|
| Idle/Walk/Run | `CanJump` && 점프 입력 && `!IsAttacking`(공격 우선, 아래 참고) | Jump |
| Idle/Walk/Run | `!isGrounded`(벼랑 이탈) | Fall |
| Idle/Walk/Run/Avoid | `IsAttacking && CanBeInterruptedByAttack` | GroundComboAttack |
| Jump | `Movement.y <= 0`(정점 통과) | Fall |
| Jump/Fall | `IsAttacking && CanBeInterruptedByAttack`(공중에서도 공격 허용) | AirComboAttack |
| Jump/Fall | `isGrounded && Movement.y <= 0`(착지) | Idle/Walk/Run(`ChangeToLocomotionState`) |
| GroundComboAttack | 유효 콤보 체인 확정 | GroundComboAttack(다음 타, 재진입) |
| GroundComboAttack 종료(미확정) | `isGrounded ? Idle/Walk/Run : Fall` | (분기) |
| GroundDodgeAttack 종료 | `IsAttacking` 유지 시 → GroundComboAttack(체인 진입, 1회성. 콤보 재진입 아님) | |
| GroundDodgeAttack 종료 | `IsAttacking` 없음 → `isGrounded ? Idle/Walk/Run : Fall` | (분기) |
| AirComboAttack | 유효 콤보 체인 확정 | AirComboAttack(다음 타, 재진입) |
| AirComboAttack 종료(미확정) | `isGrounded ? Idle/Walk/Run : Fall` | (분기) |
| 모든 상태 | (미구현) 피격/사망 등 강제 상태 | 최우선 전이 — 향후 추가 시 아래 표 맨 위에 배치 |

**전이 우선순위(같은 프레임에 여러 조건이 겹칠 때) — `Jump`/`Fall`(공중 로코모션)에만 적용, `GroundComboAttack`/`AirComboAttack`은 8-2에 따라 애니메이션 종료 시점까지 캔슬 없음:** 강제 전이(피격/사망, 미구현) > 착지/벼랑 이탈 > Jump 정점 통과 > 공격/점프 입력.

**공격·점프 동시 입력(같은 프레임) 규칙:** **공격 우선.** (2026-08-11 수정 — 최초안의 `OnJumpPerformed`에서 `!IsAttacking` 가드 방식은 콜백 처리 순서가 `Jump→Attack`이면 가드 시점에 `IsAttacking`이 아직 false라 방지가 안 되는 구멍이 있어 CodexBot 지적으로 폐기.) **Jump는 입력 콜백을 쓰지 않고, `PlayerBaseState.Update()`에서 매 프레임 `Jump.WasPerformedThisFrame()`으로 폴링**한다(New Input System 1.19 지원 API). 같은 `Update()` 안에서 반드시 **Attack 체크(`IsAttacking && CanBeInterruptedByAttack` → `OnAttack(); return;`)를 먼저 하고, 그 다음에만 Jump 체크(`CanJump && Jump.WasPerformedThisFrame()` → `OnJump()`)**를 하는 고정 순서로 우선순위를 보장한다 — 콜백 발생 순서에 의존하지 않음. Move/Attack 콜백은 Input System 기본 설정(Dynamic Update 전 이벤트 일괄 디스패치)상 이미 `Update()` 실행 전에 처리 완료된다고 가정(기존 `IsAttacking`/`MoveInput` 폴링 로직도 동일 전제). **전제조건 명시(CodexBot 지적, 2026-08-11):** 이 우선순위 보장은 프로젝트 Input System 설정이 기본값 `Process Events In Dynamic Update`인 것을 전제로 한다 — `WasPerformedThisFrame()`의 "프레임" 기준이 설정된 Dynamic/Fixed update mode를 따르기 때문. 이 설정을 바꾸면 재검토 필요.

**8-2. 공격류 상태 종료 분기 통일 (§3-4-7 수정)**
`PlayerComboAttackState`/`PlayerDodgeAttackState`(지상)와 `PlayerAirComboAttackState`(공중) 전부, 콤보 미확정으로 종료될 때 무조건 고정 타겟(Idle/Fall)으로 가지 않고 **`isGrounded` 확인 후 분기**한다. 공중공격 도중 지면에 닿아도 진행 중인 타격은 끝까지 재생하고(캔슬 없음), 그 타격이 끝나는 시점에 이 분기로 판정.

**8-3. 중력 정지 불변조건 (§7 보강)**
- 소유자는 `PlayerAirAttackState`(및 하위 `PlayerAirComboAttackState`) 단일 — 동시에 다른 주체가 `SuspendGravity()`를 호출하지 않는다.
- `StateMachine.ChangeState`는 항상 이전 상태 `Exit()`를 보장 호출 — 정상 경로에서 Resume 누락 없음.
- `SuspendGravity()`는 `verticalVelocity`를 **0으로 고정**(상승/하강 속도 유지 아님, 완전 호버).
- `ResumeGravity()`는 멱등(중복 호출 무해).
- 개발 빌드 한정으로 `SuspendGravity()` 중복 호출 시 assertion(`Debug.Assert`)으로 소유권 위반 조기 탐지 — 구현 시 추가.
- 이 불변조건이 깨지는 경우(정지 주체가 2개 이상으로 늘어남, 예: 그로기 상태 추가)엔 bool → lease/counter로 전환 재검토.

**8-4. `ComboIndex` 리셋 규칙 (§7 보강)**
체인 진입(콤보 확정 후 다음 타로 재진입)이 아닌 **모든 "새로 시작하는" 진입점**(`PlayerGroundState`/`PlayerAirState`의 `OnAttack()`)은 상태 전이 직전에 `stateMachine.ComboIndex = 0`을 명시적으로 설정한다. 이전 상태의 `Exit()` 정리에만 의존하지 않음 — 향후 히트스턴 등 비정상 인터럽트가 생겨도 안전.

**8-5. `CanJump` 컨벤션 유지 + 검증 항목 추가**
`CanBeInterruptedByAttack`과 동일하게 기본 true(블랙리스트) 유지 — 화이트리스트로 전환하지 않음. 대신:
- `PlayerStateMachine_Design.md` §3-2에 "새 상태 추가 시 `CanBeInterruptedByAttack`/`CanJump` 둘 다 검토했는지" 체크리스트 명문화.
- **향후 피격/사망 상태가 실제로 추가되는 시점에, 두 플래그가 모두 `false`로 오버라이드됐는지 확인하는 EditMode 테스트를 추가한다.** (이번 공중 상태 작업 범위엔 없음 — 피격/사망 시스템 자체가 아직 없어서. 그 작업 착수 시 이 항목을 다시 꺼내올 것)

**8-6. 벼랑 이탈 판정 보정**
`Ground 상태 → Fall` 조건을 `!isGrounded` 단독이 아니라 `!isGrounded && Movement.y <= 0`로 제한(상승 중 오탐 방지용 방어적 조건 추가). 단, 계단/경사에서 `isGrounded`가 한두 프레임 흔들리는 문제 자체의 근본 해결책은 아님 — 이 AND 조건은 별개 이슈. 흔들림은 여전히 오픈 이슈로 남기고, 플레이테스트 후 필요하면 grace time 추가.

**8-7. 지상/공중 콤보 공용 추상화 (§7 확정)**
"구현 시점에 판단"이 아니라 지금 확정: `PlayerComboAttackStateBase`(가칭)로 콤보창/버퍼/이벤트 로직을 묶고, 하위 클래스(`PlayerComboAttackState`/`PlayerAirComboAttackState`)는 공격 데이터 소스(`AttackDatas` vs `AirAttackDatas`)·콤보 종료 타겟(§8-2 분기)·중력 정책(공중만 Suspend/Resume)만 오버라이드.

## 9. 작업 순서 (승인 후, §8 반영)

1. InputActions에 Jump 액션 추가 (Unity 에디터에서 직접 또는 MCP로).
2. `PlayerAirState`/`PlayerJumpState`/`PlayerFallState` 코드 작성(착지 조건 `isGrounded && Movement.y<=0`) + `PlayerStateMachine`에 인스턴스 등록.
3. `PlayerGroundState`에 벼랑 이탈 감지(`!isGrounded && Movement.y<=0`) 추가. `PlayerBaseState.Update()`에 공유 `OnAttack` 트리거 체크(먼저) → `Jump.WasPerformedThisFrame()` 폴링 기반 `CanJump` 체크(다음) 순서로 끌어올려 지상·공중 공용화 + 공격 우선순위 보장(§8-1 참고, 콜백 순서 의존 없음).
4. 착지 후 복귀 로직 공용 헬퍼(`ChangeToLocomotionState`)로 리팩터(Avoid 코드도 이걸 쓰도록 변경).
5. `ForceReceiver`에 `SuspendGravity()`(verticalVelocity=0 고정)/`ResumeGravity()`(멱등) 추가, 개발 빌드에서 중복 Suspend 시 assertion.
6. `PlayerAttackData`에 `AirAttackDatas`(3단) 필드 추가, 우선 지상 수치 복사해서 초기화.
7. `PlayerComboAttackStateBase`(가칭) 추상화 후 `PlayerComboAttackState`/`PlayerAirComboAttackState`가 상속(공격데이터 소스·종료 타겟·중력정책만 오버라이드), `PlayerDodgeAttackState`/`PlayerAttackState`/`PlayerAirAttackState` 종료 분기도 `isGrounded` 기준으로 통일. 콤보 진입점마다 `ComboIndex=0` 명시적 리셋.
8. `PlayerStateMachine_Design.md` §3-2에 `CanBeInterruptedByAttack`/`CanJump` 동시 검토 체크리스트 추가(EditMode 테스트는 피격/사망 시스템 착수 시점으로 보류, 이 문서에 항목만 남김).
9. Animator Controller에 Air 서브스테이트머신(Jump/Fall) + AirAttack 서브스테이트머신(3단) 배선(Unity MCP).
10. Unity MCP로 컴파일 확인 → 플레이모드에서 점프/낙하/착지/벼랑 이탈/공중공격 3단 콤보(지상 도중 시작한 경우 포함) 동작 확인.
11. Discord로 결과 보고 후 커밋/브랜치/PR은 별도 허가.
