# JoseonSoul — 프로젝트 가이드 (CLAUDE.md)

이 문서는 클로드가 JoseonSoul 프로젝트에서 작업할 때 따르는 규칙·구조·워크플로우를 정의한다. BeatHero(`D:\Unity\BeatHero\CLAUDE.md`)와 동일한 운영 방식(작업 전 승인, Discord 보고)을 따르되 이 프로젝트 고유의 스택·구조를 반영한다.

**설계 문서는 `Design/` 폴더에 있다.** 핵심 기존 시스템은 As-Is로, 신규/변경 기능은 구현 전 설계 문서를 필수로 작성한다(2026-07-31 CodexBot 리뷰 반영 — 기존 코드 전체를 소급 문서화하지 않는다).

---

## 1. 프로젝트 개요

- **장르:** 소울라이크 3D 액션. 처음엔 "조선시대 소울라이크"로 시작했으나, 2026-07-31부로 시대 설정에 얽매이지 않고 범용 소울라이크 액션으로 방향을 넓혔다(Mixamo "Sword and Shield Pack" 도입 계기).
- **현재 단계:** 프로토타입 — 3인칭 이동(Idle/Walk/Run/Avoid), 오빗 카메라, 콤보 공격 애니메이션까지 구현. 히트 판정/데미지/적/사망 처리 등 소울라이크 핵심 루프는 아직 없음(`Design/Implementation_Backlog.md` 참조).
- **진행 중인 작업 (`knight` 브랜치):** 기존 `mainscene`의 캐릭터 이동·상태머신을 Sword and Shield Pack 애셋 기반의 새 씬으로 이식.

## 2. 기술 스택 / 환경

| 항목 | 값 |
|---|---|
| Unity | **6000.3.13f1** (Unity 6.3) |
| 렌더 | URP 17.3 |
| 입력 | **New Input System** 1.19 (`Assets/InputActions/InputActions.inputactions` — Player 맵: Move/Look/Attack/Avoid&Run, UI 맵) — 레거시 `UnityEngine.Input` 사용 금지 |
| 인스펙터/직렬화 | Odin 없음. `[field: SerializeField]` 오토프로퍼티 패턴 + `[Range]`로 인스펙터 튜닝값 노출 |
| 캐릭터 이동 | `CharacterController` + 커스텀 `ForceReceiver`(중력·임팩트 합산), Rigidbody는 kinematic(충돌 트리거용) |
| 검증 도구 | **Unity MCP** (CoplayDev, `com.coplaydev.unity-mcp`) — 컴파일 확인·콘솔 로그·에셋/씬 조작. HTTP 브리지 `http://127.0.0.1:8080/mcp` |
| 패키지 | AI Navigation(미사용), FBX Exporter, Timeline, Visual Scripting — 설치는 돼 있으나 현재 코드에서 미사용 |
| 프로젝트 경로 | `D:\Unity\JoseonSoul` |
| 원격 | `https://github.com/SeRDic0705/JoseonSoul.git` |

## 3. 설계 문서

| 파일 | 내용 |
|---|---|
| `Design/PlayerStateMachine_Design.md` | IState/StateMachine 기반 FSM, 상태 전이 그래프, 콤보 진행 로직 |
| `Design/CombatData_Design.md` | AttackInfo 콤보 데이터 모델. 히트 판정/데미지 적용은 **미구현** — 설계 전 확정 필요 |
| `Design/Camera_Design.md` | 오빗 카메라(데드존 추적 + 스피어캐스트 충돌 보정) |
| `Design/ForceReceiver_Design.md` | 중력·넉백·전진 임팩트를 합산하는 이동 보정 시스템 |
| `Design/Git_Convention.md` | 브랜치(main/develop/토픽 브랜치)·커밋·PR 워크플로우 |
| `Design/Implementation_Backlog.md` | 구현됨/진행중/미구현 현황 |

**새 설계 문서를 작성하지 않고 임의로 구조를 바꾸지 않는다.** 특히 히트 판정·적 AI 등 미구현 시스템에 손대기 전에는 해당 설계를 `Design/`에 먼저 정리하고 Discord로 확인받는다.

## 4. 폴더 구조 & 네임스페이스

```
Assets/
├─ Scripts/
│  ├─ StateMachines/            # 범용 FSM 베이스 (IState, StateMachine)
│  └─ Character/Player/
│     ├─ (Player.cs, PlayerInput.cs, CameraController.cs, ForceReceiver.cs, PlayerAnimationData.cs)
│     └─ PlayerStateMachines/   # PlayerStateMachine + 개별 State 클래스
├─ ScriptableObjects/
│  ├─ Scripts/                  # SO 클래스 정의 (PlayerSO, CameraSO, PlayerGroundData 등)
│  └─ Datas/                    # 실제 .asset 인스턴스
├─ Editor/                      # 에디터 전용 툴 (SceneRebuildTool 등)
├─ InputActions/                # New Input System 액션 에셋 + 생성된 C# 클래스
├─ Prefabs/, Scenes/, Materials/, Textures/, Animations/, Meshs/
├─ KoreanTraditionalMartialArts/    # 기존 조선무술 애니메이션(교전)
└─ Sword and Shield Pack/           # Mixamo 범용 검+방패 애니메이션(신규, knight 브랜치)
```

- **네임스페이스/asmdef 없음** — 전부 글로벌 네임스페이스, 단일 `Assembly-CSharp` 어셈블리. **이번 문서 작업으로 새로 도입하지 않는다**(2026-07-31 CodexBot 리뷰 — 직렬화 타입명/리플렉션 영향 때문에 별도 마이그레이션+ADR로 분리 결정). 앞으로 도입하게 되면 이 섹션과 `Design/Implementation_Backlog.md`를 갱신한다.
- 생성한 ScriptableObject 에셋은 `Assets/ScriptableObjects/Datas/`에 저장.

## 5. 코딩 컨벤션

- C# 표준 + Unity 관례. `[field: SerializeField] public X { get; private set; }` 패턴으로 인스펙터 노출 + 외부 읽기전용을 함께 만족(관측된 프로젝트 관례).
- 네이밍: 클래스/메서드 `PascalCase`, 지역변수/파라미터/private 필드 `camelCase`(BeatHero와 달리 **언더스코어 접두사 쓰지 않음** — 관측된 그대로 유지).
- 인스펙터 튜닝 수치는 `[Range(min,max)]`로 감싸 기획 조정 여지를 남긴다.
- 한 파일 = 한 주요 타입. 파일명 = 타입명.
- 주석은 한국어, 식별자는 영어. 주변 코드의 주석 밀도에 맞춘다(현재 카메라/스테이트 클래스는 로직 라인마다 짧은 한국어 주석이 붙어있는 편).

## 6. 아키텍처 원칙

1. **ScriptableObject = 읽기 전용 데이터.** `PlayerSO`/`CameraSO`와 그 하위 데이터 클래스(`PlayerGroundData` 등)는 런타임에 수정하지 않는다. 가변 상태(MoveInput, ComboIndex, IsAttacking 등)는 `PlayerStateMachine`이 보유.
2. **FSM은 `IState`/`StateMachine` 범용 베이스 위에 짓는다.** 캐릭터별 상태머신(`PlayerStateMachine`)이 상태 인스턴스와 공유 런타임 값을 들고, 개별 State는 `PlayerBaseState` 계층에서 파생.
3. **이동력은 `ForceReceiver`로 합산.** 중력·넉백·공격 전진력 등 순간적 힘은 상태 코드가 직접 `CharacterController.Move()`를 건드리지 않고 `ForceReceiver.AddForce`/`Jump`를 통해 반영한다.
4. **카메라는 `CameraSO` 데이터로 완전히 파라미터화.** 하드코딩된 상수 없이 오프셋/감도/데드존/충돌 값을 SO에서 읽는다.
5. **에디터 자동화 도구(`SceneRebuildTool` 등)는 하드코딩 경로 의존을 명시적으로 남긴다** — 애셋 경로를 바꾸면 해당 툴도 함께 갱신.

## 7. 작업 워크플로우 (중요)

BeatHero와 동일:

1. 구현 전 관련 `Design/*.md`를 읽는다. 미구현 시스템(히트 판정, 적 AI 등)에 손대려면 먼저 설계 문서를 쓰고 Discord로 확인받는다.
2. 컨벤션(§4~6)에 맞춰 코드 작성.
3. **Unity MCP로 컴파일 확인.** 에러 시 콘솔 로그 읽고 스스로 수정 후 재확인. 컴파일 통과 전엔 "완료" 보고하지 않는다.
4. 가능하면 플레이모드/에셋 생성으로 동작까지 검증.
5. 작업 단위가 끝나면 **Discord 메시지로** 진행 상황 보고(터미널 출력만으로는 보고로 인정하지 않음). 채널: `1532731100282228866`.
6. 막히거나 선택이 필요하면 터미널 프롬프트가 아니라 **Discord로 질문**한다.

> Unity MCP 연결이 끊겨 있으면(HTTP 브리지 `127.0.0.1:8080` 무응답) 먼저 사용자에게 Unity 에디터 상태 확인을 요청한다. 검증 불가 상태에서 대량 코드 작성 금지.

## 8. 하지 말 것 (Constraints)

- 레거시 `UnityEngine.Input` 사용 금지 — New Input System 사용.
- 런타임에 ScriptableObject/데이터 클래스 필드 수정 금지.
- 설계 문서와 다른 임의 설계 변경 금지 — 바꾸려면 문서부터 갱신 후 Discord 확인.
- 컴파일 검증 없이 "완료" 보고 금지.
- 작업 완료 보고를 터미널 출력으로만 끝내지 말 것 — 반드시 Discord 메시지로 전송.
- `Design/`·`CLAUDE.md`를 사용자 확인 없이 대규모로 갈아엎지 말 것(점진적 갱신은 OK).
- namespace/asmdef를 이 문서 정비 김에 슬쩍 도입하지 말 것 — 별도 마이그레이션 작업으로 분리(§4 참조).
- 한글이 들어가는 생성 스크립트(.ps1 등)는 UTF-8 BOM 필수(cp949 깨짐 방지).
