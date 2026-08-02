# ForceReceiver — As-Is 설계

> 2026-07-31 시점 `ForceReceiver.cs` 구현을 관찰해 작성.

---

## 1. 역할

`Player`에 부착되는 컴포넌트로, **중력**과 **외부 임팩트(넉백/공격 전진력/점프)**를 하나의 `Movement` 벡터로 합산해 `CharacterController.Move()`에 매 프레임 더해준다. 이동 로직(`PlayerBaseState.Move`)은 이 값을 상태 무관하게 그대로 가져다 쓴다.

## 2. 필드/동작

```
impact           : Vector3   외부에서 가해진 순간 힘의 누적(감쇠 대상)
verticalVelocity : float     수직 속도(중력 + 점프)
dampingVelocity  : Vector3   SmoothDamp 내부 상태

Movement => impact + Vector3.up * verticalVelocity
```

- `Update()`:
  - 접지 중이고 `verticalVelocity<0`이면 `verticalVelocity`를 `gravity.y * deltaTime`로 리셋(바닥에 붙어있을 때 누적 낙하속도 방지).
  - 아니면 매 프레임 `gravity.y * deltaTime`만큼 계속 가속.
  - `impact`는 `Vector3.SmoothDamp(impact, zero, ref dampingVelocity, drag)`로 `drag`(기본 0.3) 시간 상수로 자연 감쇠.
- `AddForce(Vector3)`: `impact`에 즉시 가산 — 감쇠 시작점이 될 뿐 즉시 소모되지 않음(연속 호출 시 누적).
- `Jump(float jumpForce)`: `verticalVelocity`에 가산.
- `Reset()`: `impact`/`verticalVelocity` 0으로 초기화.

## 3. 현재 사용처

| 호출자 | 메서드 | 용도 |
|---|---|---|
| `PlayerComboAttackState.TryApplyForce` | `Reset()` 후 `AddForce(forward * attackInfo.Force)` | 콤보 공격 전진 임펄스(`Design/CombatData_Design.md` 참조) |
| `PlayerBaseState.ForceMove` (protected) | `Movement` 읽어 `Controller.Move` | 콤보 어택 중 이동 입력 무시하고 임팩트만 반영 |

`Jump()`는 선언돼 있으나 어떤 State에서도 호출되지 않음(공중 상태 미구현, `Design/PlayerStateMachine_Design.md` §5 참조).

## 4. 미구현/열린 이슈

- 피격(맞았을 때) 넉백 경로 없음 — 현재 `AddForce` 호출부는 자기 자신의 공격 전진뿐, 적에게 맞았을 때 밀려나는 처리는 미구현(적 자체가 없음).
- `Reset()`을 공격 시작 시마다 호출해 중력에 의한 `verticalVelocity` 누적도 함께 지워버림 — 공중에서 공격을 허용하게 될 경우 낙하 속도가 매번 리셋되는 부작용이 있을 수 있음(현재는 공중 공격 자체가 없어 문제 없음).
