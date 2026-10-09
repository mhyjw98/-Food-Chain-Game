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

## 문서 위치
- 기획: Docs/[.md]
- 진행상황: Docs/[progress]
- 코드 리뷰 기록: Docs/review-progress.md(세션 1~5), Docs/review-progress-2.md(세션 6~) (계획: Docs/review-plan.md, review-plan-2.md / 규칙: Docs/review-rules.md)
- **Unity MCP 연결 후 진행할 작업 목록: Docs/mcp-pending-tasks.md** — MCP가 연결된 세션을 시작하면 이 문서의 "시작 절차"부터 수행 (0번 컴파일/Weaver 확인이 최우선)
- Node.js 방 코드 서버 점검 목록: Docs/node-server-checklist.md — 사용자가 서버 코드를 주면 이 문서의 "서버 코드를 받으면 진행할 순서"대로 리뷰·보완
- 리뷰 후 테스트 목록: Docs/review-tests.md(세션 1~5), 이후분은 Docs/review-tests-2.md — 리뷰 중 코드를 수정하면 테스트 항목을 반드시 여기에 추가 (규칙은 review-rules.md 6번). 테스트는 리뷰가 전부 끝난 뒤 한 번에 진행하므로, 수정 직후 테스트를 요청하며 멈추지 않는다


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
