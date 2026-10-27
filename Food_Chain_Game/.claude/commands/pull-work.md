---
description: 다른 PC에서 올린 작업 내용을 GitHub 저장소에서 가져와 현재 브랜치에 반영
argument-hint: "[브랜치 이름 — 생략하면 현재 브랜치]"
---

다른 PC에서 푸시한 작업 내용을 가져와서 로컬 브랜치에 반영해줘.

## 대상

- 작업 저장소: `https://github.com/mhyjw98/-Food-Chain-Game.git`
- 브랜치: `$ARGUMENTS` (비어 있으면 현재 체크아웃된 브랜치)
- 이 폴더의 `origin`은 다른 저장소(`mhyjw98158/Food-Chain-Game`)일 수 있으니, `origin`이 아니라 위 URL에서 직접 가져올 것. remote 설정은 바꾸지 마.

## 절차

1. `git status -sb`로 현재 브랜치와 커밋되지 않은 변경사항을 확인한다.
2. 작업 저장소에서 대상 브랜치를 추적 ref로 가져온다.
   `git fetch https://github.com/mhyjw98/-Food-Chain-Game.git <브랜치>:refs/remotes/mhyjw98/<브랜치>`
3. 로컬과 비교한다.
   - `git rev-list --left-right --count <브랜치>...mhyjw98/<브랜치>`
   - `git log --oneline <브랜치>..mhyjw98/<브랜치>`
   - `git diff --stat <브랜치>...mhyjw98/<브랜치>`
4. 결과에 따라 처리한다.
   - 새 커밋이 없으면: 이미 최신이라고 알리고 끝낸다.
   - 로컬이 뒤처지기만 했으면: `git merge --ff-only mhyjw98/<브랜치>`로 반영한다.
   - 로컬과 원격이 갈라졌으면(양쪽에 서로 다른 커밋): 병합하지 말고 양쪽 커밋 목록을 보여준 뒤 merge / rebase 중 어떻게 할지 물어본다.
   - 커밋되지 않은 로컬 변경이 가져올 파일과 겹쳐서 fast-forward가 거부되면: 겹치는 파일을 알려주고 어떻게 할지 물어본다. 임의로 stash, reset, checkout으로 로컬 변경을 버리거나 치우지 않는다.

## 보고

- 가져온 커밋 목록과 변경된 파일 요약
- 커밋되지 않은 로컬 변경사항이 그대로 남아 있는지
- `.cs` 파일이나 `Packages/`, `ProjectSettings/`가 바뀌었다면 Unity에서 재컴파일·패키지 재해석이 필요하다는 점
