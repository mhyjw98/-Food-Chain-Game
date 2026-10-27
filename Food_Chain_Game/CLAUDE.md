# CLAUDE.md

## 프로젝트 개요
Mirror 기반 멀티플레이 소셜 서바이벌 게임 (Goose Goose Duck / Among Us 류 탑다운 조작감).
"먹이사슬" 컨셉: 포식자(Predator)는 피식자(Prey)를 사냥해 생존, 피식자는 미션으로 먹이를
구하며 생존. 최소 7인 이상 동시 플레이 목표.

- 역할: 포식자 / 피식자
- 승리 조건: [포식자 승리 조건] / [피식자 승리 조건]
- 라운드 구조: [로비 → 게임 → 결과 → 로비 흐름 기술]

## 기술 스택
- Unity [6000.3.22f1] / URP
- Mirror [96.0.1]
- Claude Code + Unity MCP로 개발

## 핵심 시스템
- **시야 시스템**: 2D Light + POV 마스크 기반
- **상호작용 시스템**
- **미션 시스템**
- **네트워크 동기화**

## 네트워크 아키텍처 규칙 (중요)
- 사냥 판정, 시야 판정, 미션 완료 판정은 **반드시 서버(Command)에서 검증** — 클라이언트는
  입력만 보내고 결과를 신뢰하지 않음
- 클라이언트에는 "보여줘도 되는 정보"만 동기화 (예: 포식자 정체를 피식자 클라이언트에
  안 보내는 등 역할 은닉이 필요한 데이터 처리 방식 명시)
- [기타 이미 정해둔 안티치트/동기화 원칙이 있다면 추가]

## 폴더 구조
[Assets 하위 주요 폴더와 역할 — Scripts/Player, Scripts/Network, Scripts/Setting 등]

## 문서 위치와 규칙 (Docs/)
세션 시작 시 `Docs/progress/progress.md`(진행 상태) → `Docs/progress/plan.md`(남은 작업 계획) → `Docs/progress/handoff.md`(작업 방법·주의점) 순으로 읽는다.

| 폴더 | 넣는 것 | 파일 |
|---|---|---|
| `review/` | 코드 리뷰 관련 | `review-rules.md`(규칙), `review-plan.md`(남은 리팩토링), `review-progress.md`·`-2.md`(리뷰·변경 기록), `review-tests.md`·`-2.md`(플레이 테스트 목록) |
| `prompt/` | 사용자가 준 작업 프롬프트 원문 | 예: `minimap.md` |
| `log/` | 변경 이력(changelog) | `changelog.md` |
| `progress/` | 그 외 전부 | `progress.md`(유일한 진행상황 문서), `plan.md`(테스트 제외 남은 작업 계획), `handoff.md`, `important.md`(출시 전 과제), `node-server-checklist.md` |

- 코드를 수정하면: 변경 내역은 `review/review-progress-2.md`에 추가, 확인 항목은 `review/review-tests-2.md`에 추가(규칙은 `review-rules.md` 6번), `progress/progress.md`의 상태를 갱신하고, 끝낸 일은 `plan.md`에서 지운다(새로 생긴 할 일은 `plan.md`의 맞는 단계에 추가). 수정 직후 테스트를 요청하며 멈추지 않는다
- 진행상황은 `progress.md` 한 곳에만 쓴다. 다른 문서에 진행 현황 표나 상태 요약을 따로 만들지 않는다
- 끝난 계획·체크리스트 항목은 지운다(테스트는 통과하면 삭제). 단, 기록 문서(`review-progress*.md`, `log/`)는 지우지 않는다
- 새 문서는 위 분류에 맞는 폴더에 만들고, `Docs/` 바로 아래에는 파일을 두지 않는다
- 서버를 고칠 때는 `progress/node-server-checklist.md`를 보고 짝이 되는 Unity 쪽도 함께 고친다


## 아직 미정인 것
- CLI 도구 추가 여부 검토 중
- [기타 미정 사항]

## 빌드/테스트 & 검증
- 멀티플레이 테스트는 Multiplayer Play Mode(또는 ParrelSync)로 인스턴스 [2~3]개 실행
- 컴파일 검증: C# 코드 수정 후 Unity MCP를 통해 콘솔에 C# 컴파일 에러나 Warning이 없는지 확인할 것.
- 테스트: Unity Test Framework가 작성된 경우 관련 단일 TestRunner 항목만 실행하여 통과 여부 확인.
- 코드 스타일: C# 공식 컨벤션(PascalCase / camelCase) 및 Unity Best Practice 준수.

## 워크플로우
- **코드 수정 후:** 반드시 MCP를 통해 유니티 에디터의 컴파일 성공 여부(타입/구문 에러)를 확인할 것.
- **최소 범위 테스트:** 전체 프로젝트를 다시 굽지 말고, 수정된 스크립트와 연관된 씬(Scene) 또는 컴포넌트만 테스트.
- **구현 후 확인:** Null Reference Exception 방지를 위해 `GetComponent` 및 serialized 필드의 null 체크 로직 포함 여부 확인.

## 개발 방식 관련 요청
- Unity MCP를 통해서만 GameObject/프리팹을 생성·수정해줘. .scene/.prefab YAML 파일은 직접 편집하지 마.
  대량 변경이 필요한 경우에만 Editor/TemporaryGeneratedScripts에 임시 에디터 스크립트를 만들고, 작업 후 삭제해줘.
- 컴파일 에러가 없는 상태로만 작업을 마무리해줘.
- 각 시스템을 기능 단위로 나눠서 순서대로 구현해줘. 한 번에 다 만들지 말고, 하나씩 완료 후 확인받고 다음으로 넘어가는 방식으로 진행해줘.
- 수치(쿨타임, 무적시간, 이동속도 등)는 하드코딩하지 말고 인스펙터에서 조절 가능한 [SerializeField] 변수로 빼줘.
