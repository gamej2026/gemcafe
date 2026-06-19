# 젬카페(GemCafe) — AI 구현용 PRD (Agent Spec)

> 본 문서는 사람용 PRD([PRD.html](PRD.html))를 **AI 코딩 에이전트가 결정론적으로 구현**할 수 있도록 재구성한 명세이다.
> 모든 요구사항은 고유 ID, 입력, 동작, 검증 가능한 수용 기준(AC)을 가진다.
> `[ASSUMPTION]` = 기획서에 없어 추정한 항목. `[TBD]` = 확정 필요(구현 전 질의).

---

## 0. 메타데이터

| key | value |
|---|---|
| project | gemcafe |
| engine | Unity `6000.3.18f1` |
| render | URP 17.5, 2D |
| input | 키보드(이동/상호작용) + 마우스(드래그&드롭) |
| ui | uGUI 2.5 (Canvas 기반) |
| target_platform | PC (Standalone) |
| source | 「젬카페 UI」 기획서 21p |
| state | DRAFT v0.1 |

### 0.1 구현 원칙 (에이전트 지침)
1. 한 번에 하나의 `FR-xxx`를 구현하고 해당 AC를 모두 충족시킨 뒤 다음으로 진행한다.
2. `[TBD]`/`[ASSUMPTION]` 값은 **9장 표의 기본값**을 사용하되, 코드에서 한 곳(설정 SO/상수)으로 분리해 추후 교체 가능하게 한다.
3. 데이터(손님/레시피/재료)는 코드 하드코딩 금지 → `ScriptableObject`로 분리(§7).
4. 씬 전환·UI 상태는 §3 상태머신을 단일 진실원본으로 따른다.
5. 매직넘버 금지. 타이밍/속도/임계값은 인스펙터 노출 필드로.

---

## 1. 용어(Glossary)

| 용어 | 의미 |
|---|---|
| 삼도천 | 시작 배경이 되는 저승 강 |
| 마님 | 카페 주인 NPC (고용주) |
| 돌쇠 | 뱃사공 NPC |
| 인내심(Patience) | 손님별 제한시간 게이지 |
| 사발(Bowl) | 재료를 담는 그릇 (드롭 타깃) |
| 막자(Pestle) | 사발 내용물을 섞는 도구 (완료 트리거) |
| 트레이(Tray) | 재료 선택 패널 (← 슬라이드로 열림) |

---

## 2. 기술 컨텍스트 & 아키텍처

### 2.1 권장 씬 구성
| 씬 | 역할 |
|---|---|
| `Lobby` | 타이틀/메뉴 |
| `Stage1_Riverside` | 횡이동 + 비주얼노벨 인트로 |
| `Cafe` | 카페 진입 대화 + (선택)튜토리얼 + 본게임 루프 |

> `[ASSUMPTION]` 단일 `Cafe` 씬 내에서 패널 전환으로 본게임을 처리(씬 분리 대신 슬라이드 연출). 성능/관리상 단순.

### 2.2 권장 코드 레이아웃
```
Assets/_Game/
  Scripts/
    Core/        # GameManager, SceneRouter, EventBus, SaveSystem
    Player/      # PlayerMover, Interactor
    Dialogue/    # DialogueRunner, DialogueLine, SpeakerView
    Crafting/    # TrayController, DraggableIngredient, BowlReceiver, PestleMixer, RecipeEvaluator
    Customer/    # CustomerSpawner, PatienceTimer, LivesSystem
    UI/          # HUD, PopupManager(설정/레시피/대화다시보기)
  Data/
    Ingredients/ # *.asset (IngredientSO)
    Recipes/     # *.asset (RecipeSO)
    Customers/   # *.asset (CustomerSO)
  Art/ Audio/ Scenes/
```

### 2.3 전역 이벤트(EventBus) — 권장 신호
`OnDialogueStarted/Ended`, `OnCustomerArrived(CustomerSO)`, `OnCraftStarted`,
`OnIngredientAdded(IngredientSO)`, `OnDrinkCompleted(RecipeSO result)`,
`OnPatienceDepleted`, `OnLivesChanged(int)`, `OnDayCompleted(int)`.

---

## 3. 게임 상태머신 (단일 진실원본)

```
LOBBY
  └─(새 게임)→ INTRO_STAGE1
INTRO_STAGE1   (이동 + 대화 + 카메라/따라가기)
  └─(카페 진입)→ CAFE_INTRO
CAFE_INTRO     (마님 대화 → 취업)
  └─→ TUTORIAL? ──(skip)──┐
                          ↓
SERVICE_LOOP  ← 손님 단위 반복, 일자(Day 1..3)로 묶임
  ├─ CUSTOMER_ENTER  (딸랑~ + 페이드인)
  ├─ ORDER_DIALOGUE  (말풍선 주문)
  ├─ CRAFTING        (슬라이드 인 → 재료 D&D)
  │    ├─ POPUP(설정=시간정지 / 레시피·대화다시보기=시간흐름[TBD])
  │    └─ MIX (막자 드롭)
  ├─ FINISH_ANIM     (믹스 애니)
  └─ RESULT          (판정 → 보상/실패) → 다음 손님 or DAY_END
DAY_END (1..3)
  └─(Day3 종료)→ ENDING
FAIL: 인내심 소진 또는 목숨 0 → 실패 처리
```

규칙:
- `CRAFTING` 중 **설정 팝업**만 인내심 타이머 일시정지. 그 외 팝업/상태는 흐름.
- 인내심 0 → 현재 손님 `FAIL`(목숨 −1 `[ASSUMPTION]`).

---

## 4. 기능 요구사항 (FR)

> 표기: ID · 설명 · 우선순위(MUST/SHOULD/COULD) · 수용 기준(AC, 검증 가능).

### 4.1 로비
**FR-001 · 로비 메뉴 · MUST**
- 입력: 마우스 클릭 / 키보드 탐색.
- 동작: `게임 이름` 타이틀 + 버튼 4개(새 게임 / 이어하기 / 설정 / 게임 종료).
- AC:
  - [ ] 새 게임 → `INTRO_STAGE1` 진입.
  - [ ] 이어하기 → 저장 데이터 있으면 로드, 없으면 비활성(disabled).
  - [ ] 설정 → 설정 팝업 오픈.
  - [ ] 게임 종료 → `Application.Quit()` (에디터에선 playmode 종료).

**FR-002 · 저장/이어하기 · SHOULD**
- AC: [ ] 진행도(일자, 뱃삯, 목숨) 저장/로드. 저장 단위 `[TBD]`(기본: 일자 시작 시점).

### 4.2 이동 & 상호작용 (Stage1)
**FR-003 · 횡 이동 · MUST**
- 입력: `←`/`→` 또는 `A`/`D`.
- AC: [ ] 좌우 이동만 가능(상하 X). [ ] 이동 방향에 맞춰 스프라이트 facing 반전.

**FR-004 · 근접 상호작용 피드백 · MUST**
- 동작: 플레이어가 NPC 상호작용 반경 내 진입 시 NPC 테두리 발광(아웃라인) + 화면 상단 키 안내(`F`) 표시.
- AC: [ ] 반경 진입 시 아웃라인 ON + 안내 표시. [ ] 이탈 시 OFF.

**FR-005 · F 키 대화 시작 · MUST**
- AC: [ ] 상호작용 가능 상태에서 `F` → 해당 NPC 대화 시작. [ ] 대화 중 이동 입력 잠금.

**FR-006 · 원거리 호출 말풍선 · SHOULD**
- AC: [ ] 멀리서 부르는 연출은 대화창 없이 말풍선 UI만 표시.

### 4.3 대화(비주얼노벨)
**FR-007 · 대화 시스템 · MUST**
- 레이아웃: 좌=주인공, 우=대화 상대 스탠딩 + 하단 대화창 + 「다음」 버튼.
- 동작: 비화자 스탠딩 디밍 + 배경 딤. 「다음」/클릭으로 라인 진행.
- 데이터: `DialogueLine { speaker, text, portrait, effect }`.
- AC:
  - [ ] 현재 화자 강조, 비화자 어둡게.
  - [ ] 「다음」으로 다음 라인, 마지막 라인 후 대화 종료 이벤트 발생.
  - [ ] 텍스트 타이핑 연출 `[ASSUMPTION]`(스킵 가능).

**FR-008 · 끼어들기 말풍선 · SHOULD**
- AC: [ ] 진행 중 끼어드는 대사("야!")를 별도 말풍선 + 뒤 배경 딤으로 표시 후 대화 흐름 복귀.

**FR-009 · 카메라 이동 / 따라가기 · SHOULD**
- AC: [ ] 지정 트리거에서 카메라가 우측(카페)로 보간 이동. [ ] 주인공이 NPC를 따라 이동하는 스크립트 애니 재생.

### 4.4 카페 진입 / 튜토리얼
**FR-010 · 튜토리얼 · COULD**
- AC: [ ] (포함 시) 카페 진입 직후·본게임 직전 1회 노출. [ ] 트레이→사발 드래그 + 막자 믹스 안내. [ ] 스킵/재호출 가능 `[TBD]`.

### 4.5 본게임 — 손님 & 제조
**FR-011 · 손님 등장 · MUST**
- AC: [ ] 입장 SFX "딸랑~" 재생. [ ] 손님 이미지 알파 0→1 페이드 인.

**FR-012 · 주문 대화(말풍선) · MUST**
- AC: [ ] 손님 주문을 말풍선으로 표시. [ ] 주문 종료 시 목표 레시피 컨텍스트 설정.

**FR-013 · 목숨 HUD · MUST**
- AC: [ ] 좌상단 목숨 3개 표시. [ ] `OnLivesChanged`에 따라 즉시 갱신.

**FR-014 · 제조 화면 전환 · SHOULD**
- AC: [ ] 제조 진입 시 우→좌 슬라이드 전환 애니.

**FR-015 · 제조 레이아웃 · MUST**
- 구성: 좌패널(목숨/인내심 게이지/대화 상대/좌하단 3버튼) + 우영역(테이블 탑뷰 → 트레이 + 사발).
- AC: [ ] 트레이가 ← 방향 슬라이드로 열림. [ ] 사발이 테이블 위에 노출.

**FR-016 · 재료 드래그&드롭 · MUST**
- 입력: 마우스 down→drag→up.
- 동작: 트레이 재료를 드래그하면 포인터에 재료가 부착되어 따라옴. 드롭 시 **포인터가 사발 영역(Rect/Collider) 내부**면 투입 인식.
- AC:
  - [ ] 드래그 중 재료 고스트가 마우스를 따라감.
  - [ ] 사발 영역 안 드롭 → `OnIngredientAdded` 발생 + 사발 내용물에 추가.
  - [ ] 사발 밖 드롭 → 원위치 복귀(투입 취소).
  - [ ] 동일 재료 복수 투입 허용 여부 `[TBD]`(기본: 허용).

**FR-017 · 좌하단 버튼 팝업 · MUST**
- 버튼: 대화 다시 보기 / 레시피 참고 / 설정.
- AC: [ ] 각 버튼 → 대응 팝업 오픈 + 뒤 배경 딤. [ ] 팝업 중복 오픈 방지. [ ] 닫기 가능.

**FR-018 · 인내심 게이지 · MUST**
- 규칙(§5 상세): 손님별 초기값 상이, 시간에 따라 감소, **설정 팝업에서만 정지**.
- AC:
  - [ ] 손님 입장 시 `CustomerSO.patience`로 초기화.
  - [ ] 매 프레임 감소(게이지 시각 반영).
  - [ ] 설정 팝업 오픈 동안 감소 정지, 닫으면 재개.
  - [ ] 대화 다시 보기/레시피 팝업 중 감소 여부 = `[TBD]`(기본: 감소함).

**FR-019 · 인내심 소진 실패 · MUST**
- AC: [ ] 게이지 0 도달 → 현재 손님 실패 처리 + 목숨 −1 `[ASSUMPTION]` + 다음 흐름.

**FR-020 · 막자 믹스(완료 트리거) · MUST**
- AC: [ ] 사발 옆 막자를 사발에 드롭 → 제조 완료 확정. [ ] 완료 후 추가 투입 잠금.

**FR-021 · 완료 애니메이션 · SHOULD**
- AC: [ ] 막자로 사발 내용물 믹스 애니 재생 후 결과 단계로 전환.

**FR-022 · 레시피 판정 · SHOULD**
- 동작: 사발 내용물 vs `CustomerSO.targetRecipe.ingredients` 비교.
- AC: [ ] 일치/불일치 판정. [ ] 판정 방식(순서·수량 영향, 부분점수) = `[TBD]`(기본: 멀티셋 일치, 순서 무시).

**FR-023 · 일자 진행 & 뱃삯 · SHOULD**
- AC: [ ] Day 1→3 진행. [ ] 성공 시 뱃삯 가산. [ ] Day3 종료 → 엔딩. 수치 `[TBD]`.

---

## 5. 시스템 상세 규칙

### 5.1 인내심 타이머 의사코드
```csharp
// PatienceTimer
float current; // = customer.patience (초)
void Update() {
  if (state != Crafting && state != OrderDialogue) return;
  if (PopupManager.IsOpen(PopupType.Settings)) return;       // 설정만 정지
  // [TBD] 레시피/대화다시보기 팝업도 정지시킬지 → 기본 false(계속 감소)
  current -= Time.deltaTime;
  EventBus.PatienceChanged(current / customer.patience);     // 0..1
  if (current <= 0f) { EventBus.PatienceDepleted(); }
}
```

### 5.2 드롭 판정 의사코드
```csharp
void OnDrop(PointerEventData e) {
  if (RectTransformUtility.RectangleContainsScreenPoint(bowlRect, e.position, cam))
      bowl.Add(draggedIngredient);   // 투입
  else
      draggedIngredient.ReturnToTray();
}
```

### 5.3 레시피 판정(기본 규칙 `[TBD]`)
- 입력 멀티셋 == 목표 멀티셋 → 성공(순서 무시, 수량 일치).
- 부분 일치/감점은 후속 정의.

### 5.4 목숨 규칙 `[ASSUMPTION]`
- 초기 3. 실패(인내심 소진/오답 `[TBD]`) 시 −1. 0 → 게임오버.

---

## 6. UI 요소 인벤토리

| 화면 | 요소 |
|---|---|
| 로비 | 타이틀, 메뉴 4버튼 |
| Stage1 | 플레이어, NPC(돌쇠/마님), 말풍선, NPC 아웃라인, 상단 키 안내, 대화창+다음, 딤 |
| 제조 | 목숨3, 인내심 게이지, 대화 상대, 좌하단 3버튼, 테이블 탑뷰, 트레이, 사발, 막자, 재료 아이템 |
| 팝업 | 설정 / 레시피 참고 / 대화 다시 보기 (+딤) |
| 완료 | 믹스 애니, 결과/보상 표시 `[TBD]` |

---

## 7. 데이터 모델 (ScriptableObject)

```csharp
public enum IngredientCategory { Base, Syrup, Topping } // [TBD] 실제 분류

[CreateAssetMenu(menuName="GemCafe/Ingredient")]
public class IngredientSO : ScriptableObject {
    public string id;
    public string displayName;
    public Sprite icon;
    public IngredientCategory category;
}

[CreateAssetMenu(menuName="GemCafe/Recipe")]
public class RecipeSO : ScriptableObject {
    public string id;
    public string drinkName;
    public IngredientSO[] ingredients; // 멀티셋 비교 기준
    // [TBD] 순서/수량 가중치, 허용 오차
}

[System.Serializable]
public struct DialogueLine {
    public string speakerId;
    [TextArea] public string text;
    public Sprite portrait;
    // public DialogueEffect effect; // 끼어들기/카메라/따라가기 트리거
}

[CreateAssetMenu(menuName="GemCafe/Customer")]
public class CustomerSO : ScriptableObject {
    public string id;
    public Sprite portrait;
    public DialogueLine[] orderDialogue;
    public RecipeSO targetRecipe;
    public float patience;   // 초, 손님별 상이
    [Range(1,3)] public int day;
}
```

### 7.1 예시 데이터 (플레이스홀더 — 값 확정 전)
```yaml
ingredient: { id: "ing_water", displayName: "삼도천 물", category: Base }
recipe:     { id: "rcp_sample", drinkName: "샘플 음료", ingredients: ["ing_water"] }
customer:   { id: "cst_001", targetRecipe: "rcp_sample", patience: 60, day: 1 }
```

---

## 8. 입력 매핑

| 액션 | 키/마우스 | 컨텍스트 |
|---|---|---|
| 이동 | `←`/`→`, `A`/`D` | Stage1 |
| 상호작용 | `F` | NPC 근접 시 |
| 대화 진행 | 클릭 / `Space`(`[ASSUMPTION]`) | 대화 중 |
| 재료/막자 | 마우스 드래그&드롭 | 제조 중 |
| 팝업 닫기 | `Esc`(`[ASSUMPTION]`) / 닫기 버튼 | 팝업 중 |

---

## 9. 미결 항목 (구현 전 확정 필요) & 기본값

| # | 항목 | 기본값(에이전트 사용) | 비고 |
|---|---|---|---|
| Q1 | 레시피/재료/손님 실제 데이터 | 플레이스홀더 1세트 | 기획 확정 후 SO 채움 |
| Q2 | 레시피 판정 방식 | 멀티셋 일치, 순서 무시 | 부분점수 미정 |
| Q3 | 오답 제출 시 처리 | 목숨 −1 + 재시도 불가 | `[TBD]` |
| Q4 | 목숨 0 처리 | 게임오버→로비 | `[TBD]` |
| Q5 | 일자당 손님 수 / 뱃삯 목표 | 손님 3/일, 목표 미설정 | `[TBD]` |
| Q6 | 레시피/대화 팝업 시간 흐름 | 흐름(감소함) | 기획서 "변동 가능" |
| Q7 | 결과/보상 UI | 단순 성공·실패 토스트 | `[TBD]` |
| Q8 | 튜토리얼 포함 여부 | 미포함(스텁만) | COULD |
| Q9 | 저장 단위 | 일자 시작 시점 | `[TBD]` |

---

## 10. 구현 순서(권장 마일스톤)

1. **M1 코어**: 상태머신, EventBus, 씬 라우팅, SO 정의(§7).
2. **M2 대화**: FR-007~009 + DialogueRunner.
3. **M3 Stage1**: FR-003~006 + 카메라/따라가기.
4. **M4 제조 핵심**: FR-015, FR-016, FR-020 (D&D + 막자).
5. **M5 손님/인내심/목숨**: FR-011~013, FR-018, FR-019.
6. **M6 팝업/판정/일자**: FR-017, FR-022, FR-023.
7. **M7 연출/사운드**: FR-014, FR-021, SFX.

각 마일스톤은 해당 FR의 AC 체크박스를 전부 충족해야 완료로 간주한다.
