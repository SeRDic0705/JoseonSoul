# Git 컨벤션

JoseonSoul의 깃 운영 규칙. BeatHero(`D:\Unity\BeatHero\Design\Git_Convention.md`)의 공통 규칙(커밋 컨벤션·검증 게이트·PR 형식)을 재사용하되, 브랜치 전략은 이 프로젝트의 실제 히스토리에 맞춰 조정했다(2026-07-31, CodexBot 리뷰 반영).

---

## 1. 브랜치 전략

현재 원격 브랜치: `main`, `develop`, `Camera`, `attack`, `level`, `Movement`, `knight`.

| 브랜치 | 역할 |
|---|---|
| `main` | 안정 릴리스. 직접 커밋 금지. |
| `develop` | 통합 브랜치. 토픽 브랜치가 병합되는 대상. |
| `<토픽이름>` (예: `knight`, `Camera`, `attack`, `level`, `Movement`) | 기능/영역 단위 작업 브랜치. **`feature/` 접두사 없이 토픽명만 사용**하는 것이 기존 관례 — BeatHero의 `feature/phase<N>-...` 네이밍을 그대로 가져오지 않는다. |

- **`knight` 브랜치의 용도(2026-07-31 마스터 확인):** 조선시대 한정 설정을 풀고 범용 소울라이크 3D 액션으로 방향을 잡으면서, Mixamo "Sword and Shield Pack"을 임포트해 이 애셋 기반의 새 씬으로 `mainscene`의 캐릭터 이동·상태머신을 이식하는 작업 브랜치. **영구 브랜치가 아니라 이 포팅 작업이 끝나면 develop에 병합 후 정리되는 토픽 브랜치**로 취급한다.
- 새 토픽 작업 시작 시 develop 최신화 후 분기: `git switch develop && git pull && git switch -c <토픽이름>`.
- main에는 직접 작업하지 않는다.
- 토픽 브랜치가 지나치게 커지면(예: 포팅 작업처럼 여러 영역을 건드리는 경우) 논리 단위로 PR을 나눌 수 있다 — 이 경우 Discord로 먼저 알린다.

---

## 2. 커밋 컨벤션

**Conventional Commits + 한국어 설명** (기존 히스토리 스타일 유지).

```
<type>: <한국어 요약>

(필요 시 본문 — 무엇을/왜)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
```

**type 종류:**
| type | 용도 |
|---|---|
| `feat` | 새 기능 |
| `fix` | 버그 수정 |
| `refactor` | 동작 변화 없는 구조 개선 |
| `build` | 빌드/패키지 설정 변경 |
| `import` | 애셋·패키지 임포트 (기존 히스토리에서 `build`와 혼용돼 있었음 — 앞으로는 애셋/패키지 임포트는 `import`로 통일 권장) |
| `docs` | 문서(설계·CLAUDE 등) |
| `test` | 테스트 |
| `chore` | 잡무(설정·정리) |
| `style` | 포맷팅 |
| `perf` | 성능 |

**커밋 단위:**
- 논리적으로 독립된 변경 단위로 잘게 분리 (예: FSM 이식 / 카메라 재배선 / 애니메이터 교체를 각각 커밋).
- 각 커밋은 가능하면 컴파일이 통과하는 상태로 만든다.
- 요약은 한 줄 50자 내외 권장, 명령형/현재형.

---

## 3. 검증 게이트 (필수)

- **커밋 전: Unity MCP로 컴파일 통과 확인** (`http://127.0.0.1:8080/mcp`, 인스턴스 `JoseonSoul@...`).
- 컴파일이 깨진 상태로 커밋하지 않는다.

---

## 4. 푸시 / PR 워크플로우

- **푸시 권한:** 사용자가 허가함. 단, 푸시·PR 생성 직전엔 **Discord로 알리고 진행**한다.
- 완료 조건: 컴파일 검증 통과 → 커밋 정리 → Discord 확인 → `git push` → PR 생성(develop 대상).
- **PR 본문 형식:**
  - 무엇을/왜
  - 관련 백로그 항목 (`Design/Implementation_Backlog.md`)
  - 검증 결과 (컴파일 통과 여부, 플레이모드 테스트 여부)
  - 끝에:
    ```
    🤖 Generated with [Claude Code](https://claude.com/claude-code)
    ```
- PR 병합은 사용자가 검토 후 수행. 자동 병합하지 않는다.

---

## 5. 추적 대상

- `Design/`, `CLAUDE.md`는 추적·커밋한다 (설계 변경도 `docs:` 커밋).
- Unity 표준 `.gitignore` 적용 중(Library/·Temp/·UserSettings/ 등 제외).
- `.asset`/`.meta`/`.prefab` 등 Unity 직렬화 파일은 커밋.
