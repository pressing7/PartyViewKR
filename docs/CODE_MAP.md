# PartyViewKR 코드 지도

이 문서는 이후 디버깅과 기능 추가를 위해 **현재 구현의 책임·함수·상태·검증 경계**를 찾는 진입점이다. 먼저 증상에 해당하는 경로를 고르고, 연결된 함수 표에서 원본을 확인한다. 문서와 소스가 다르면 소스를 우선한다.

## 기준과 범위

- 정리일: 2026-10-25. 플러그인 `0.3.49.0`, Dalamud API `15`.
- 소스 기준: `0.3.49.0`. 기반 main [`a34099802f12d6e5c4859489472489319d146e4f`](https://github.com/pressing7/PartyViewKR/commit/a34099802f12d6e5c4859489472489319d146e4f) 이후 AFK 주기·실패 재시도 설정과 Discord 수정창 닫기 변경을 포함한다.
- 문서 자체와 신규 AFK 설정·복구 파일을 포함한 추적 파일 241개 전체를 [파일 지도](code-map/files.md)에 분류했다. 소스 위치는 이 버전의 행 번호다.
- C# Roslyn 구문 분석, JavaScript Acorn 구문 분석, Python AST로 **1,294개 명명된 항목/진입점**을 대조했다: C# 1,172, JS 91, Python 31. 메서드뿐 아니라 생성자·로컬 함수·계산 속성·인터페이스 선언을 포함한 숫자다.
- 자동 생성 record 메서드와 단순 auto-property accessor는 별도 함수로 세지 않는다. 이름 없는 LINQ/콜백은 소유 함수에서 설명하고, 중요한 DOM handler와 테스트 시나리오는 따로 연결했다.
- 현재 C# 등록 테스트는 416개, Node 등록 테스트는 15개, Python `test_*` 메서드는 12개다. 등록 범위와 실행 결과를 구분하며 이번 AFK 변경의 검증 환경은 아래에 기록한다.

현재 구현은 **기존 본인 모집글의 조회·수정·수동 갱신 및 AFK 명단 감시에 따른 자동 조회**다. 새 모집 시작, 콘텐츠/파티 유형 변경 및 자동 알림은 아직 구현하지 않았다. 루트의 `PartyViewKR.dll/json/deps.json`은 이전 수동 배포본이다. 빌드 기준은 `src/PartyViewKR.csproj`와 `src/PartyViewKR.json`, 설치 대상은 Releases ZIP이다.

## 함수별 문서

| 문서 | 찾을 내용 |
|---|---|
| [플러그인 수명·조회·자리 매핑](code-map/core.md) | `Plugin`, `PartyReader`, `ListingReader`, AFK `PartyRosterReader`/`AfkListingMonitor`, ownership/소개말, 슬롯·직업·친선 padding, 인게임 UI |
| [편집 모델·대기 변경·상태 기계](code-map/edits.md) | `Draft`, `Edit`, `Expected`, typed 조건, `ListingChange`, checkbox/dropdown/numeric UI 순서 |
| [갱신 흐름·네이티브 UI](code-map/refresh.md) | 단일/배치 갱신, `ListingRefreshHost` partial 전체, native 이벤트·guard·최종 제출 |
| [Discord](code-map/discord.md) | Gateway/REST·ACK·카드·모달·전체 결과·이모지 동기화·DPAPI 토큰 복원 |
| [공통 브리지·웹·중계](code-map/web-relay.md) | 외부 요청 큐·소유권·HTTP·WebSocket·브라우저 JS·Cloudflare Worker |
| [호환 검사·진단·배포](code-map/compat-release.md) | ABI 계약·포인터 읽기·수동 trace·reference/ZIP/release/feed 도구 |
| [테스트 함수·시나리오](code-map/tests.md) | fake host/SDK/HTTP fixture와 모든 등록 시나리오의 소스 위치 |
| [전체 파일 지도](code-map/files.md) | 함수 없는 설정/프로젝트/manifest/workflow/이미지까지 포함한 파일별 역할 |

## 책임과 스레드 경계

```mermaid
flowchart TD
  G["인게임 PartyWindow"] --> P["Plugin 요청·초안·대기 변경"]
  W["브라우저·중계·Discord"] --> B["ListingWebBridge 관리형 큐"]
  B --> F["Plugin.OnUpdate / WebHost"]
  P --> F
  A["AFK CID 명단 감시"] --> F
  F --> C["PartyRosterReader 전체 그룹"]
  F --> R["ListingReader / PartyReader"]
  F --> S["ListingRefresh / ListingBatchRefresh"]
  S --> H["ListingRefreshHost native UI"]
  H --> R
  R --> V["Snapshot / RefreshTarget"]
  V --> B
```

`Plugin.OnUpdate`가 게임 스레드에서 처리한다. 저장 Discord 연결 복원과 trace를 먼저 진행하고, 기본 500ms 간격 또는 `RequestRead`에 따라 우선 수정창 닫기 → 현재 파티 표시용 읽기 → 외부/AFK 요청 guard → 모집글 읽기 → 초안 준비 → 갱신 상태 기계 → 외부 snapshot 발행 → AFK 감시를 수행한다. 닫기 진행 중에는 일반 초안/갱신 진행을 건너뛴다. 기존 현재 파티·직업 읽기는 PVKR 창을 표시하거나 명시 읽기를 요청했을 때만 진행한다. AFK 전용 전체 CID 읽기는 AFK가 켜진 동안만 진행한다.

네트워크 callback은 네이티브 포인터를 읽지 않는다. `ListingWebBridge`의 lock 안에서 관리형 snapshot을 읽고 작업을 예약한다. 실제 `RequestQuery`/`StartRefresh`는 Framework에서 실행된다. `ListingWebRequestGuard`/`ListingWebRefreshGuard`는 **이 외부 연결이 시작한 revision**만 취소하므로 이후의 수동 요청까지 취소하면 안 된다.

인게임의 `Apply*Draft`도 즉시 게임을 바꾸지 않는다. `PendingChanges`에 넣고 갱신을 실행할 때 반영한다. 같은 필드/물리 자리는 교체하고, 원래 값으로 되돌리면 대기 변경을 제거한다. 다른 모집 원본과 섞이면 큐를 비우고 다시 선택하도록 한다. 배치는 사전 검증만 실패했을 때 대기 목록을 잃지 않도록 실제 수정창 진입 시점에 소비한다.

## 혼동하면 안 되는 데이터

| 데이터 | 실제 의미 | 주의점 |
|---|---|---|
| `PartySnapshot` / `MemberInfo` | 지금 파티에 있는 멤버와 현재 직업 | 입장 때 모집창에 등록된 직업과 다를 수 있다. 서버 초월은 현재 로컬 그룹만 읽는다. |
| `PartyRoster` | AFK가 읽은 현재 전체 파티의 정렬된 고유 CID 집합 | 최대48명·자기 포함을 검증한다. 직업·이름·순서·그룹 내 위치는 비교하지 않는다. |
| `ListingSnapshot` | 마지막으로 읽은 모집 상세 및 표시값 | `IsLive`는 현재 UI와 검증 가능한 읽기라는 뜻이다. 새 서버 응답 증거는 query 성공과 `DetailRevision` 증가를 함께 확인한다. |
| `RefreshTarget` | 편집에 묶인 본인 CID·listing ID·임무·조건·슬롯 원본 | 수정 전 새로 조회한 source가 같아야 한다. `SameRecruitment`만으로 listing ID/참가자까지 검증됐다고 생각하면 안 된다. |
| `LastViewedListing` | 네이티브 상세 모집 정보 | 현재 작성 중인 수정창 draft가 아니다. |
| `StoredRecruitmentInfo` | 게임 수정창의 현재 draft | 수정창에 들어가면 한섭에서 모집이 잠시 중지되어 active listing 정보가 비어도 이 draft는 남는다. |
| `ListingSlot.JobId` | 모집 상세에 등록된 참가 직업 | 현재 캐릭터가 바꾼 직업으로 덮지 않는다. |
| `ListingSlot.SlotFlags` | 그 자리의 허용 직업 raw mask | 점유 상태와 다른 축이다. Discord는 이 제한 이미지에 `HasMember`에 따른 join 변형을 붙인다. |
| `Position` | 등록된 자리만 나열한 1-based 표시 위치 | native array index나 그룹 내 번호가 아니다. |
| `NativeIndex` | 그룹당8칸을 보존한 0-based 물리 위치 | 자리 수정·그룹 구분·대기 변경 key는 이것을 기준으로 한다. |
| `TotalSlots` | 모집에 등록된 자리 수 | `NativeSlotCount`의 물리8/24/48과 다를 수 있다. |
| `SlotFlags=0` | 미등록 자리 | 빈 모집 자리와 다르다. 표시 목록에서 제외하되 물리 위치는 보존한다. |

친선 5+5+8은 기본적으로 물리24칸 중 **0~4 / 8~12 / 16~23**을 표시한다. 따라서 단순히 배열 앞18개를 자르면 안 된다. A/B의 물리5~7/13~15는 padding이다. 추가로 실제 미등록을 설정한 자리는 각 그룹 뒤로 이동하므로 실제 `SlotFlags`로 매핑한다.

`ListingEditorCriteria.Matches`는 검증된 친선 형태에서만 이 padding의 **조회값0 ↔ 수정창값`0xFDFFFFFE`** 차이를 허용한다. 다른 미등록 자리, 멤버 변경, 그룹/임무 변경에 이 예외를 확대하면 안 된다. `ListingBatchSnapshot.TryRead`의 정규화는 관리형 데이터에만 적용한다.

## 조건 값 계약

`ListingRefreshHost.Criteria`는 아래 9개를 `/`로 연결한다. 마지막 슬롯 값은 `MemoryMarshal.AsBytes(ulong[])`의 hex이므로 **little-endian byte 표현**이다. 진단에서 `X16`으로 출력한 정수와 문자열 방향이 다를 수 있다.

| 0-based 필드 | 값 | 관련 모델 |
|---|---|---|
| 0 / 1 | category / duty ID | 임무 identity 검증 |
| 2 | objective: 1/2/4/8 | `ListingObjectives`: UI index0/1/2/3과 변환 |
| 3 | beginner: 0/1 | `ListingBeginners` |
| 4 | completion: 1=미설정, 2=공략완료, 4=미완료 | 값8은 표시만 가능. callback ordinal과 stored bit를 구분한다. |
| 5 | finder flags: bit1/2/4 | 제한 해제 / 최저 아이템 레벨 / 초월 무효 |
| 6 | loot: 0/1/2 | 일반 / 선입찰 금지 / 파티장 분배 |
| 7 | language flags | 이번 편집 범위에서 보존 |
| 8 | 물리 슬롯별 raw `ulong` mask의 hex | `ListingJobEdit`, `ListingEditorCriteria` |

`ExtraCriteria`는 `실효 평균 아이템 레벨 / JoinConditionFlags / IsAlliance / NumberOfParties`의4필드다. 평균 레벨0은 해제, 입력은1~999다. 비공개와 잡 중복 없음은 각각의 bit만 변경해 다른 flag를 보존한다.

PIN은 `ExtraCriteria`나 외부 snapshot에 넣지 않는다. 수정창 password는0~9999이면 비공개,10000이면 공개다. UI 입력은 **ASCII 숫자 정확히4자리**이고 `0000`도 유효하다. 서버 상세 재조회로 확인할 수 있는 것은 비공개 flag이며 PIN 자체는 아니다. PIN은 수정창 UI/저장값 일치로만 확인한다.

직업의 `ClassJob` row ID, 모집 flag의 bit 위치, 직업 선택창 node ID는 세 개의 다른 값이다. 매핑은 각각 `ListingJobRules`와 `ListingRoleButtons`에 있다. “누구나 가능”은 런타임 전투 잡/클래스 전체가 허용된 경우에만 쓴다.

## UI 조작 경로

| 동작 | 검증된 경로 | 구현 |
|---|---|---|
| 내 모집 상세→수정 / 기존 조건 갱신 | `ReceiveEvent(ButtonClick, 0, 빈 event/data)` | `ClickZero`; Create 모드에서는 제출 금지 |
| 소개말 | 입력 `SetText` → 필요 시 command15 문자열 → 최종 command0에도 문자열 | `ListingDescriptionEditHost`, `Submit`, `SubmitBatch` |
| 목적 | dropdown16, 내부 checkbox2/list3; open click2 → item click1 → highlight가 editor20으로 전달 | `ListingObjectiveEditHost` |
| 초보자 환영 | checkbox17, click param21 | `ListingBeginnerEditHost` |
| 잡 중복 없음 | `OnePlayerPerJobCheckbox`, click32 | `ListingUniqueJobsEditHost`; node는 포인터/등록 경로로 확인 |
| 비공개/PIN | checkbox click29 / numeric ValueUpdate29 | `ListingPrivateEditHost`; 숫자 입력 API가 payload 생성 |
| 완료조건 | checkbox click23 / dropdown highlight22 | `ListingCompletionEditHost` |
| 평균 레벨 | checkbox click4 / numeric ValueUpdate0 | `ListingItemLevelEditHost` |
| finder 3종 | node99/100/101, 공통 click25; event.Node로 종류 구분 | `ListingRuleEditHost` |
| 전리품 | dropdown highlight26 | `ListingRuleEditHost`, `ListingLootUi` |
| 일반 자리 | node51+멤버index, param8 | `ListingSlotRoute` |
| 24인 자리 | node51+그룹index×9+멤버index; param은 유일한 등록 click에서 읽음 | `TryRegisteredClickParam` |
| 친선 자리 | 같은 node 공식, param8; 실제 등록 자리만 | `ListingSlotRoute` / 차이 직업 토글 |
| 48인 자리 | 그룹 node44~49/param9~14 → 멤버 node3~10/param4~11 | `UpdateSlotEdit`의 selecting_member 단계 |
| 직업 체크/확인 | Anyone66/param2, 직업별 node/param3, Confirm69/param0 | `ListingRoleButtons`; 같은 종류 토글을 묶고 단계 간 최소100ms |

체크박스는 표시 상태를 `SetChecked`로 준비하고 검증된 등록 이벤트를 보낸다. dropdown은 열기→`DispatchItemEvent`→`SelectItem(..., true)`를 따른다. 소개말의 검증된 예외를 제외하고 callback ordinal을 임의로 조립하거나 `StoredRecruitmentInfo` 필드를 직접 덮어쓰는 방식으로 바꾸지 않는다.

`ReadRegisteredUiEvent`는 event type/param/target/listener의 유일한 연결을 확인한다. `AtkEvent.Node`는 optional payload일 수 있으므로 버튼의 실제 target과 항상 같다고 가정하면 안 된다. 관찰 로그에 같은 click이 중첩돼 보여도 native forwarding일 수 있어 호출을 두 번 재현해서는 안 된다.

## 갱신 상태와 실패의 의미

기본 경로는 `Queued → ReadingOwn → OpeningEditor → Editing* → WaitingResult → Verifying`다. 단순 갱신은 `Editing*` 없이 제출할 수 있다. 여러 변경은 `EditingBatch` 안에서 항목을 순회한다.

| 상태/증거 | 해석 |
|---|---|
| `Stopped`, submit0 | 제출하지 못한 종료. 수정창을 열었다면 게임 모집은 중지 상태일 수 있다. |
| `Unconfirmed`, submit1 | 제출을 시도했지만 서버 결과 또는 시간 갱신의 확신이 부족하다. 자동 재전송 대상이 아니다. |
| `Completed` | fresh own 상세와 요청 결과를 확인하고 listing ID 변화 또는 남은시간 증가의 갱신 증거도 확보했다. |
| `own_listing_seen_timer_unconfirmed` | 본인 모집/내용은 확인했지만 이전·이후가 모두60분처럼 시간 증가를 확인하지 못했다. 내용 편집 실패와 동일하지 않다. |
| 배치 개별 항목 실패 | 해당 항목의 결과에 원인을 남긴다. 정리와 identity 검증이 가능하면 다음 항목 및 최종 갱신으로 진행한다. |
| identity/멤버/창 바인딩 상실 또는 안전한 정리 실패 | 계속 제출하지 않는다. 콘텐츠가 비활성화한 한 필드와 다른 종류의 실패다. |

작업은 호출 **전에** phase/counter를 전진시킨다. `Submit`, checkbox click, 직업 confirm, numeric input이 예외를 던져도 이미 처리됐을 가능성이 있으므로 같은 동작을 반복하면 안 된다. `FireCallback`의 `False` 반환은 그 자체가 실패 판정 근거가 아니다.

배치는 항목마다 `PrepareBatchStep`의 실제 draft에 `Rebase`한다. 성공하지 못한 항목의 화면/저장값이 일부 바뀌었을 수 있어 `FinishBatchStep`에서 자기 자식 창을 닫고 실제 누적 상태를 읽는다. 소개말은 자기 작업이 남긴 UI 입력만 stored comment와 맞춰 복구한다. 임무가 제한 해제를 비활성화한 경우처럼 지원하지 않는 항목은 건너뛰지만, 실제 원본 검증을 없애는 식으로 진행하지 않는다.

현재 주요 대기값은 초기/검증 조회15초, 수정창 준비7초, 체크/선택/숫자 저장 관찰3초, 배치 항목12초·항목 정리4초, 제출 후 창 닫힘10초, 재조회 전 최소1.5초, 제출 후 cooldown5초다. 외부의 queued TTL/발행 freshness와 이 값은 다른 제한이다. 변경 시 상수/테스트를 함께 확인한다.

## 외부 조작·재접속 계약

| 구간 | 경계 |
|---|---|
| Browser API | `apiVersion=2`, session, request UUID, edit revision. `GET /api/state`, query/operation 결과 GET, `POST /api/query`, `POST /api/apply`만 사용한다. |
| Bridge writes | ID를 접수 때 소비한다. session당 최대1024개의 write ID를 유지하고 최근 결과32개를 보관한다. 조회 결과를 잃어도 같은 ID로 다시 실행하지 않는다. |
| 브라우저 불명확한 POST | ID/session/시각만 저장하고 결과를 GET한다. body/PIN 저장과 timeout 후 자동 POST 반복은 하지 않는다. |
| Relay frame | `v=1`, id/method/path/session/requestId/body/expiresAt. browser API 버전과 별개다. |
| Cloudflare | host는 서버 secret `PVKR_CONNECT_KEY`, browser는 plugin이 만든 room/browser key를 사용한다. 서버 secret 이름 자체를 value로 넣으면 형식 검증에서 거부된다. |
| Worker static assets | 최초 페이지 이동을 허용하기 위해 public static GET을 API의 cross-origin 검사 전에 처리한다. API는 인증/Origin 경계를 유지한다. |
| Relay 재접속 | transport만 backoff 재연결한다. 이미 넘긴 게임 명령을 재발송하지 않는다. host 교체 close4001은 이전 연결을 중단한다. |
| Discord | 개인 application owner만 허용한다. `Defer` 성공 뒤에만 Bridge 작업을 넣는다. 응답 timeout 때문에 같은 게임 작업을 재제출하지 않는다. |
| 카드 | Overview/Editor/Details/Results는 독립 카드다. nonce, session, editor revision,15분 수명을 검사한다. 긴 결과는 전체 페이지로 보관한다. |
| 이모지 | embedded PNG80개를 봇 application에 등록한다. 매 연결에서 목록을 읽어 재사용/정확한 legacy rename/누락 업로드만 한다. runtime ID를 고정 저장하지 않는다. |
| 저장 token | `RelayConfiguration`의 DPAPI ciphertext와 자동 연결 설정. 명시 ‘끄기’는 자동 연결을 끄고, 플러그인 Dispose는 저장 token을 삭제하지 않는다. |

`ListingWebView.From`은 CID·raw mask·PIN·내부 진단을 빼고 표시 데이터만 내보낸다. `DraftInvariant`에는 비교용 PIN 숫자가 포함될 수 있으므로 그 문자열을 로그/외부 응답에 추가하지 않는다. 수동 trace도 command16의 PIN과 문자열/pointer payload를 숨긴다.

## 증상에서 시작하는 탐색

| 증상 / diagnostic | 먼저 볼 함수 | 이어서 볼 검증 |
|---|---|---|
| 구조 DLL 버전이 다른데 읽기가 차단됨 | `NativeCompatibility.Check`, `NativeContract.Differences` | MVID만 비교하지 말고 `nativeDifferences`, 파일/로드 MVID, native+interop 두 계약 |
| 서버 초월/연합에서 현재 파티 인원이 일부만 나옴 | `PartyReader.ReadCrossRealm` | 현재 local group만 읽는 경계. 전체 모집은 `ListingSlots.Read` |
| AFK에서 다른 그룹의 가입/탈퇴가 감지되지 않음 | `PartyRosterReader.ReadCrossRealm/ReadRegular` | `afkRosterSource`, 전체 그룹 범위·CID 완전성·`afkRosterReason`. 기존 현재 직업 표시의 local group 읽기와 구분 |
| AFK 재입장 또는 조회 실패 후 자동 조회가 멈춤 | `AfkListingMonitor.Update/Retry`, `AfkHost.OwnListingMatches` | `Pending`, generation, 요청 revision, 설정된 주기/실패 대기, 새 상세/참가 CID 일치. A→B→A는 pending을 보존하며 관측 사이 재입장은 선택한 추가 조회로 보완 |
| 빈 소개말에서 Self 확인 실패 | `ListingDescription.Resolve`, `ListingIdentity.Resolve`, `ListingReader.Read` | UI ‘소개말 없음’과 native empty가 함께 맞는지 |
| `initial_own_ready_timeout` | `ListingRefresh.WaitForOwn`, `ListingReader.HandleRequest`, `TryGetOwn` | 새 revision·조회 성공·visible/ready·리더 CID/이름/comment |
| `stored_criteria_mismatch` / `roles` | `ValidateEditor`, `ListingRoleComparison.Describe`, `ListingEditorCriteria.Matches` | 실제 미등록과 친선 padding 구분, display/native 인덱스 |
| 친선5+5+8의 뒤 관전 자리가 잘림 | `ListingSlots.Read`, `PositionLabel`, `ListingWebView.From` | 전체 물리24칸에서 등록 자리만 추출했는지 |
| 24/48인 또는 직업 선택창에서 정지 | `ListingSlotRoute.TryCreate`, `BeginSlotEdit`, `UpdateSlotEdit` | node/param, registered event target/listener, role revision,48인 멤버 선택 |
| Anyone였던 자리를 수정한 후 제출 안 됨 | `MatchesSelectedFlags`, `ValidateSlotGuard`, `Expected(..., actualFlags)` | known mask 비교 후 게임의 실제 raw flags로 target을 고정했는지 |
| 소개말은 입력돼 보이는데 갱신 후 원래 값 | `UpdateDescriptionEdit`, `Submit`, `SubmitBatch` | command15 draft와 command0 제출 문자열을 모두 확인 |
| 콘텐츠에서 특정 조건을 못 켬 | `ReadRuleControl` 등 각 Read*Controls, `UpdateItems` | disabled 항목 결과와 정리 상태; blanket guard 제거 금지 |
| 일부 수정 실패 뒤 나머지도 멈춤 | `FinishBatchStep`, `RecoverBatchDescription`, `ReadBatchDraft` | 자기 자식 창 정리, 타인/다른 UI 변경, stored와 visible 차이 |
| 바뀐 값은 보이나 갱신 결과 미확정 | `RefreshEvidence.ParseSeconds`, 최종 `Verifying` | 내용 일치와 시간 증가 증거를 구분 |
| 웹은 보이는데 클릭이409/오래된 상태로 거부 | `QueueQuery`, `QueueApply`, `PublishEditor`, `WebHost.BeforeRead` | session/revision/5초 freshness, 게임 수정창, 인게임 대기 초안 |
| 웹 timeout 뒤 중복 갱신 우려 | `app.js.applyChanges`, `acceptOperation`, `UpdateOperation` | pending UUID ledger와 결과 GET; 자동 POST 재시도 없음 |
| Worker `/`가403 | `worker.mjs` 기본 `fetch` | static GET 순서와 API `sameOrigin` 경계를 분리했는지 |
| relay host503 ‘서버 연결 키’ | Worker 기본 `fetch`, `protocol.token` | runtime secret 이름·64자리 대문자 hex 값; 로그에 실제 키를 붙이지 않는다. |
| Discord ‘적시에 응답하지 않음’ | `ListingDiscordClient.Interaction`, `ListingDiscordSession.Process`, `Response.Defer` | Gateway 처리 분리와 ACK 성공이 game work보다 먼저인지 |
| 재시작마다 토큰/이모지를 다시 요구 | `RestoreDiscordConnection`, `PrepareDiscordConnection`, `ListingDiscordEmojiAssets.Sync` | 기억/자동 연결·DPAPI 복원, 기존 application emoji 목록 재사용 |
| Discord 자리 이미지가 현재 직업과 다름 | `ListingDiscordIcons.ConditionKey/ForSlot` | 의도적으로 모집 제한 mask+참가 여부를 표시한다. 실시간 JobId로 대체하지 않는다. |
| 긴 Discord 결과가 안 보임 | `PaginateResult`, `DrawResults`, `RunResults` 테스트 | 표시 예산·Unicode/escape 분할·전체 결과 버튼 및 페이지 nonce |
| `Project and manifest versions disagree` | `tools/release.py.package` | src csproj의 Version/AssemblyVersion/FileVersion과 src manifest 및 build DLL. 루트 옛 manifest 제외 |

진단은 먼저 `refreshStage`, `refreshResult`, `refreshEditCalls`, `refreshSubmitCalls`로 **어디까지 실행했는지** 확인한 뒤 `refreshNativeCheck`와 해당 `*EditCheck`/`editorRole*`로 좁힌다. 이후의 passive snapshot이 오류 시점과 달라질 수 있으므로 보존된 host diagnostic도 함께 본다.

## 검증과 문서 갱신

0.3.49 AFK 설정·닫기 변경에서 관리형 테스트416개(추가24개)와 Python 배포 테스트12개를 통과했다. NuGet 캐시/WindowsDesktop 참조 팩이 없는 환경이므로 기존0.3.47 build의 의존 DLL을 직접 참조하는 별도 오프라인 프로젝트로 테스트 및 전체 C# 소스 컴파일을 수행했다. 소스 컴파일은 `net10.0`+Windows platform attribute에서 경고0/오류0이었으며, 정식 `net10.0-windows` 패키지 빌드/Actions 실행이나 인게임 검증을 대신하지 않는다. 기존 native ABI1088개 항목의 차이는0이었다.

기능 변경 후 해당 모델/host/외부 어댑터와 관련 테스트를 함께 확인한다. 다음은 필요할 때 개발 환경에서 실행하는 명령이다. GitHub Actions 실행은 사용자가 담당한다.

```bash
dotnet run --project tests/RefreshTests.csproj -c Release
python -m unittest discover -s tests -p 'test_*.py' -v
node --test tests/web-connection.test.cjs tests/web-editor.test.cjs
```

relay 변경은 `relay` 디렉터리에서 의존성을 준비한 뒤 `npm run build`, `npm test`를 사용한다. 통합 테스트는 Miniflare와 .NET fixture를 사용한다. `tests/browser-smoke.cjs`는 빌드한 `RefreshTests.dll`, Playwright 및 Chromium이 필요하고 `PVKR_DOTNET`/`PVKR_BROWSER_ARTIFACTS`를 받을 수 있다. 현재 release workflow에는 관리형/Python 검사만 자동 등록돼 있다.

native 변경은 고정 참조를 준비해 `python tools/release.py verify`와 Release build를 확인하고, 실제 한섭에서 UI/저장값/갱신 결과를 별도로 검증한다. baseline은 실제로 사용하는 구조/메서드를 검토한 경우에만 갱신한다. build/manifest/package 버전은 배포 변경 때 함께 맞춘다.

이 코드 지도를 유지할 때는 다음을 함께 바꾼다.

1. 실제 변경 파일의 함수 표: 추가/삭제/이름/인자/책임·부작용·소스 위치.
2. 해당 모델의 값 계약과 UI node/event 경로, guard/timeout/소유권 규칙.
3. 새 regression test의 이름과 fixture 책임. native UI 검증이 필요한 부분을 명시한다.
4. 새 파일은 전체 파일 지도에 등록하고, 버전/기준 commit 및 항목 수를 다시 확인한다.
5. 링크 target·함수 누락/중복·placeholder·문서 외 변경이 없는지 확인한다.

## AFK 감시와 후속 알림의 연결 지점

PVKR 창 상단의 AFK 체크박스로 활성화한다. 시작 시 꺼져 있으며 실제 로그아웃/캐릭터 변경/Dispose에서 종료한다. 일시적인 `PlayerState.IsLoaded=false`는 모드를 끄지 않는다. 이 토글은 게임 자체의 자리비움 상태와 별개다.

`PartyRosterReader`는 현재 직업을 읽지 않는다. 일반 파티는 `IPartyList`의 로컬 명단, 일반 연합은 공개 alliance API의20개 영역을 추가로 확인한다. 서버 초월은 `CrossRealmGroups` 전체1~6개 그룹을 읽으므로 친선 관전·다른 연합 그룹도 포함한다. 본인 누락·0 CID·중복·범위 오류·읽는 중 형태 변경은 불완전한 관측으로 처리한다. 활성 서버 초월 명단이 불완전하면 일반 파티1명으로 축소하지 않는다. 기존 `PartyReader`의 로컬 그룹/현재 직업 표시와는 서로 다른 경로다.

`AfkListingMonitor.Update`는 명단이 바뀌면 `Pending`과 `generation`을 남긴다. A→B→A처럼 원상복귀해도 지우지 않는다. 주기 조회는 기본 꺼짐이며 사용자가 켜면1~55분 간격으로 추가 조회한다. 읽기 실패는 이전 정상 명단을 유지한다. 조회 실패 재시도는 기본 켜짐·5초이며1~3600초 고정 대기로 설정한다. 꺼진 상태의 같은 실패는 반복하지 않고 새 명단 변화 또는 별도로 켠 주기를 기다린다. 네 옵션만 RelayConfiguration에 저장하며 AFK 모드 자체는 저장하지 않는다. 영역 이동 여부를 별도로 판정하지 않고 기존 조회 guard에 접속/이동/수정창 판단을 맡긴다.

성공은 자기 request revision, 새 detail revision, `Live/Self`, 참가 CID 전체 일치로 확인한다. 조회 중 generation이 변했으면 성공 후에도 후속 조회를 남긴다. 요청 최소 간격은2초, 완료 대기는15초다. `BeforeRead`는 기존 request guard를 재사용해 아직 native dispatch 전인 요청도 재검사한다. 다른 수동/외부 요청이 같은 Reader를 사용하므로 **자기 revision만 취소**한다. 적용 대기 변경/편집/갱신 동안에는 자동 조회를 미루고 필요 표시를 유지한다.

AFK는 모집글 조회만 수행하며 `IAfkListingHost`에는 수정/제출 계약이 없다. Discord 메시지 자동 업데이트·알림 전송은 아직 구현하지 않았다. 이후 가득 참/고정 직업 자리 제외 알림은 성공한 fresh 모집 상세의 등록 자리·점유·허용 직업으로 판정해야 한다. 현재 직업으로 등록 직업을 덮어쓰면 안 된다. 감시 사이에 들어왔다 나간 모든 중간 이력은 보장하지 않는다. 관측 사이 재입장 보완이 필요하면 주기적으로 추가 조회를 켠다.


## Discord 수정창 닫기

조회 카드와 진행/busy 화면의 `수정창 닫기`는 `pvkr:close:<session>`을 사용한다. `ListingDiscordSession.Process`에서 owner/app 인증을 먼저 적용하고 일반 `handling` 잠금보다 앞서 독립 경로로 분기한다. `CloseEditor`는 session과 중복 interaction ID를 검사하고 ACK가 성공한 뒤에만 `QueueCloseEditor`를 호출한다.

`ListingWebBridge.Recovery`는 query/apply가 busy여도 닫기를 접수한다. 기존 queued/running 작업은 중단 상태로 바꾸고 source·pendingWrite를 비운다. 닫기 접수 세대 번호가 바뀌므로 닫기 전에 생성된 `DiscordWork`의 ACK가 늦게 돌아와도 `QueueQuery/QueueApply` 내부 lock에서 거부된다. UI 요청에는 native 주소나 임의 창 이름을 받을 수 있는 입력이 없다.

실제 중단은 `Plugin.OnUpdate`의 읽기/제출보다 앞선 `ProcessRecovery`에서 실행한다. query/refresh/trace·로컬 초안·변경 대기열을 중단한 뒤 두 모집 관련 창의 주소/PreFinalize revision을 내부 ticket으로 고정한다. 직업 선택창을 먼저1회 닫고 사라진 뒤 부모 수정창을1회 닫으며,5초 안에 둘 다 사라져야 성공이다. 다른 창으로 교체되거나 읽기 실패/예외/timeout이면 실패를 알리고 close를 반복하지 않는다. 이미 제출한 변경을 취소하거나 새 모집을 만들지 않으며 다른 확인 대화상자를 누르지 않는다.

디버깅 시작점은 [공통 queue](code-map/web-relay.md#file-src-listingwebbridge-recovery-cs), [게임 창 연결](code-map/refresh.md#file-src-plugin-recovery-cs), [별도 interaction 경로](code-map/discord.md#file-src-listingdiscordsession-recovery-cs), [회귀 테스트](code-map/tests.md#file-tests-listingrecoverytests-cs)다. 이 단계의 닫기 버튼은 Discord에 제공하며 브라우저 HTTP 경로를 새로 추가한 것은 아니다.
