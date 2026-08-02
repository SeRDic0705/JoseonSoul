# CombatData — As-Is 설계

> 2026-07-31 시점 구현된 데이터 모델을 관찰해 작성. 실제 히트 판정/데미지 적용 시스템은 **미구현**이며, 이 문서는 현재 있는 데이터 구조 + 앞으로 채워야 할 설계를 구분해 적는다.

---

## 1. As-Is: 데이터 구조

```
PlayerSO (ScriptableObject)
  └─ AttackData: PlayerAttackData

PlayerAttackData
  └─ AttackDatas: List<AttackInfo>

AttackInfo
  ├─ AttackName        : string   공격명
  ├─ ComboStateIndex   : int     콤보 성사 시 다음에 재생할 AttackInfo 인덱스 (-1 = 콤보 없음)
  ├─ ComboTransitionTime : float(0~1)  정규화 애니메이션 시간 기준, 이 시점 이후 입력이 남아있으면 콤보 확정
  ├─ ForceTransitionTime : float(0~3)  이 시점에 전진력(Force)을 1회 적용
  ├─ Force             : float(-10~10)  전진 임펄스 크기(ForceReceiver.AddForce로 전달)
  └─ Damage            : int      데미지 값 — **현재 어디에서도 읽히지 않음**
```

- `PlayerComboAttackState`가 `stateMachine.ComboIndex`로 `AttackDatas[index]`를 조회해 진행한다. 상세 흐름은 `Design/PlayerStateMachine_Design.md` §4 참조.
- `AttackInfo.Damage`는 필드만 있고 이를 소비하는 코드(히트박스, OnTriggerEnter, 데미지 적용 함수 등)가 프로젝트 어디에도 없다.
- 히트박스/헐트박스 컴포넌트 자체가 존재하지 않는다(Collider 기반 판정 시스템 없음).
- 적(Enemy) 타입, 체력(Health), 사망 처리 시스템 없음 — 맞을 대상이 없는 상태.

## 2. 향후 설계 (미확정 — 구현 전 별도 확정 필요)

아래는 **아직 구현되지 않은 부분에 대한 제안**이며, 실제 작업 전에 Discord로 설계를 먼저 확정한다.

- 히트박스 컴포넌트를 `PlayerComboAttackState`의 `ForceTransitionTime`~콤보 윈도우 구간에 활성화하는 방식(애니메이션 이벤트 or normalizedTime 구간 체크) 후보.
- 데미지 적용 대상(Enemy)의 체력·피격 반응 컴포넌트 설계는 Enemy 시스템 자체가 아직 없어 선행 작업 필요.
- `AttackInfo.Damage`를 실제로 소비하는 시점에 이 문서를 갱신.

## 3. 관련 문서

- `Design/PlayerStateMachine_Design.md` — 콤보 상태 전이·타이밍
- `Design/ForceReceiver_Design.md` — `Force` 필드가 적용되는 넉백/임팩트 시스템
- `Design/Implementation_Backlog.md` — 히트 판정 미구현 항목 트래킹
