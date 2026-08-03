# Implementation Backlog

2026-07-31 프로젝트 스캔 기준 현황. 새 항목이 생기면 이 문서를 갱신한다.

---

## 진행 중 (knight 브랜치)

- [ ] `mainscene`에 구현된 캐릭터 이동(FSM)·카메라를 Sword and Shield Pack(Mixamo, `Assets/Sword and Shield Pack/`) 기반 새 씬으로 이식
  - 새 씬 아직 미생성 (현재 씬은 `Assets/Scenes/mainscene.unity` 하나뿐)
  - `SceneRebuildTool`(Tools/Joseon 메뉴)은 2026-08-03 Cinemachine 기반으로 갱신 완료 — `PlayerSO`/`CameraSO`/`Solider_Fist` 프리팹 경로는 여전히 하드코딩이라, 새 애셋팩 기준 캐릭터 프리팹으로 교체 시 이 경로들 갱신 필요
  - Sword and Shield Pack은 애니메이션 클립 위주(idle/walk/run/attack/block/slash/death 등) — 기존 `PlayerAnimationData`/Animator Controller(`SKM_Solider_Fist.controller`)와 파라미터 매핑 재작업 필요

## 구현됨 (As-Is 문서 있음)

- 캐릭터 이동 FSM: Idle/Walk/Run/Avoid/ComboAttack — `Design/PlayerStateMachine_Design.md`
- Cinemachine 3인칭 오빗 카메라(2026-08-03, 레거시 `CameraController` 대체 완료) — `Design/Camera_Design.md`, `Design/Cinemachine_Migration_Plan.md`
- 넉백/중력 임팩트 시스템(ForceReceiver) — `Design/ForceReceiver_Design.md`
- 콤보 공격 데이터 모델(AttackInfo, 콤보 윈도우 타이밍) — `Design/CombatData_Design.md`
- New Input System 기반 입력(Player/UI 액션맵)
- Unity MCP(CoplayDev) 설치 완료, HTTP 브리지 컴파일 검증 가능

## 미구현

- **히트 판정/데미지 적용** — `AttackInfo.Damage`를 소비하는 코드 없음, 히트박스/헐트박스 컴포넌트 자체가 없음 (`Design/CombatData_Design.md` §2)
- **적(Enemy) 시스템** — 타입, AI, 체력, 사망 처리 전무. `com.unity.ai.navigation`(AI Navigation) 패키지가 설치돼 있으나 미사용
- **공중 상태(Jump/Fall)** — `PlayerAirData.JumpForce`, 애니메이션 파라미터는 존재하나 어떤 State도 진입 경로 없음 (`Design/PlayerStateMachine_Design.md` §5)
- **비콤보 단발 공격 상태(`PlayerAttackState`)** — 클래스는 있으나 실제 진입 경로 없음
- **소울라이크 핵심 루프** — 죽음/리스폰, 스태미나, 자원(소울류) 시스템 전혀 없음. 현재는 순수 이동+카메라+콤보 애니메이션 프로토타입 단계

## 네임스페이스/asmdef (별도 결정 — 이번 문서 정비에 포함 안 함)

CodexBot 리뷰(2026-07-31)에 따라 이번 작업에서 도입하지 않음. 현재 전부 글로벌 네임스페이스, asmdef 없음(Assembly-CSharp 단일 어셈블리). 도입 시 직렬화 타입명·리플렉션·에디터 코드 영향 검토, 단계적 전환 계획을 별도 문서(ADR)로 먼저 세운다.
