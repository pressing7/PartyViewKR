# 갱신 흐름·네이티브 UI 호스트

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

`ListingRefreshHost`는 여러 partial 파일이 하나의 클래스/세션 상태를 공유한다. mutation counter와 phase는 호출 전에 소비한다. `False` callback 반환만 보고 실패라고 판단하지 않고 UI/저장값 readback을 검사한다.

기준: 0.3.48.0 소스. 기반 main `1ef1c969e8dea0d58babf71a80698880b8fcc0dc` 이후 AFK 감시 변경을 포함한다. 2026-10-25 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [src/ListingBatchEditHost.cs](#file-src-listingbatchedithost-cs)
- [src/ListingBatchRefresh.cs](#file-src-listingbatchrefresh-cs)
- [src/ListingBeginnerEditHost.cs](#file-src-listingbeginneredithost-cs)
- [src/ListingCompletionEditHost.cs](#file-src-listingcompletionedithost-cs)
- [src/ListingDescriptionEditHost.cs](#file-src-listingdescriptionedithost-cs)
- [src/ListingItemLevelEditHost.cs](#file-src-listingitemleveledithost-cs)
- [src/ListingObjectiveEditHost.cs](#file-src-listingobjectiveedithost-cs)
- [src/ListingPrivateEditHost.cs](#file-src-listingprivateedithost-cs)
- [src/ListingRefresh.cs](#file-src-listingrefresh-cs)
- [src/ListingRefreshHost.cs](#file-src-listingrefreshhost-cs)
- [src/ListingRuleEditHost.cs](#file-src-listingruleedithost-cs)
- [src/ListingSlotEditHost.cs](#file-src-listingslotedithost-cs)
- [src/ListingUniqueJobsEditHost.cs](#file-src-listinguniquejobsedithost-cs)

<a id="file-src-listingbatchedithost-cs"></a>
## src/ListingBatchEditHost.cs

[소스 파일](../../src/ListingBatchEditHost.cs) · 명명된 항목/진입점 7개

배치 항목 사이의 실제 draft 읽기, 자식 창 정리, 소개말 부분 반영 복구 및 최종 1회 제출을 담당한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingbatchedithost-cs-11-821fc47915"></a><code>ListingRefreshHost.PrepareBatchStep</code><br><code>bool PrepareBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 메서드 | 본인 수정창이 같은 작업에 묶였는지 확인하고 현재 draft를 읽어 다음 항목의 기준으로 반환한다. 이전 개별 edit session을 초기화한다. | [L11](../../src/ListingBatchEditHost.cs#L11) |
| <a id="symbol-src-listingbatchedithost-cs-31-37f0a5ed79"></a><code>ListingRefreshHost.FinishBatchStep</code><br><code>SlotEditProgress FinishBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 메서드 | 이 항목이 연 role/dropdown만 닫고 실제 draft를 다시 읽는다. 정리 작업도 중복 전송하지 않고 readback을 기다린다. | [L31](../../src/ListingBatchEditHost.cs#L31) |
| <a id="symbol-src-listingbatchedithost-cs-83-25f325d480"></a><code>ListingRefreshHost.RecoverBatchDescription</code><br><code>SlotEditProgress RecoverBatchDescription(AddonLookingForGroupCondition* addon, RefreshTarget current, out string error)</code> | 메서드 | 실패한 소개말이 화면에만 남아 있다면 저장된 실제 comment로 입력 UI만 1회 맞춘다. 실패한 문자열 callback이나 제출을 재실행하지 않는다. | [L83](../../src/ListingBatchEditHost.cs#L83) |
| <a id="symbol-src-listingbatchedithost-cs-114-a81716fd58"></a><code>ListingRefreshHost.BatchDropdown</code><br><code>AtkComponentDropDownList* BatchDropdown(AddonLookingForGroupCondition* addon)</code> | 메서드 | 현재 배치 항목의 목적/완료조건/전리품 dropdown을 찾아 소유한 창 정리에 사용한다. | [L114](../../src/ListingBatchEditHost.cs#L114) |
| <a id="symbol-src-listingbatchedithost-cs-118-5e7808b8a7"></a><code>ListingRefreshHost.SubmitBatch</code><br><code>bool SubmitBatch(RefreshTarget identity, out string error)</code> | 메서드 | 정리 완료·본인·Refresh 버튼·누적 draft를 확인하고 실제 소개말을 포함한 제출을 1회만 보낸다. | [L118](../../src/ListingBatchEditHost.cs#L118) |
| <a id="symbol-src-listingbatchedithost-cs-139-a3bce3b3c5"></a><code>ListingRefreshHost.ReadBatchWindow</code><br><code>bool ReadBatchWindow(RefreshTarget identity, out AddonLookingForGroupCondition* addon, out AgentLookingForGroup* agent, out string error)</code> | 메서드 | 배치가 연 수정창·Agent의 identity/revision·모드·기본 guard를 검사한다. | [L139](../../src/ListingBatchEditHost.cs#L139) |
| <a id="symbol-src-listingbatchedithost-cs-156-b7f6448edf"></a><code>ListingRefreshHost.ReadBatchDraft</code><br><code>bool ReadBatchDraft(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 메서드 | 실제 comment/criteria/추가조건/멤버를 읽어 ListingBatchSnapshot.TryRead로 누적 RefreshTarget을 만든다. | [L156](../../src/ListingBatchEditHost.cs#L156) |

<a id="file-src-listingbatchrefresh-cs"></a>
## src/ListingBatchRefresh.cs

[소스 파일](../../src/ListingBatchRefresh.cs) · 명명된 항목/진입점 20개

여러 편집을 한 수정창에 순서대로 적용하고 가능한 항목만 1회 제출한다. 항목 실패와 전체 제출 확신을 별도로 보관한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingbatchrefresh-cs-5-444546c29b"></a><code>IListingBatchHost.PrepareBatchStep</code><br><code>bool PrepareBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 본인 수정창이 같은 작업에 묶였는지 확인하고 현재 draft를 읽어 다음 항목의 기준으로 반환한다. 이전 개별 edit session을 초기화한다. | [L5](../../src/ListingBatchRefresh.cs#L5) |
| <a id="symbol-src-listingbatchrefresh-cs-6-87aa0341ba"></a><code>IListingBatchHost.FinishBatchStep</code><br><code>SlotEditProgress FinishBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 이 항목이 연 role/dropdown만 닫고 실제 draft를 다시 읽는다. 정리 작업도 중복 전송하지 않고 readback을 기다린다. | [L6](../../src/ListingBatchRefresh.cs#L6) |
| <a id="symbol-src-listingbatchrefresh-cs-7-abddea97d8"></a><code>IListingBatchHost.SubmitBatch</code><br><code>bool SubmitBatch(RefreshTarget identity, out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 정리 완료·본인·Refresh 버튼·누적 draft를 확인하고 실제 소개말을 포함한 제출을 1회만 보낸다. | [L7](../../src/ListingBatchRefresh.cs#L7) |
| <a id="symbol-src-listingbatchrefresh-cs-10-e8af3cf9f0"></a><code>ListingChangeResult</code><br><code>ListingChangeResult(ListingChange change)</code> | 주 생성자/모델 | 한 배치 변경의 요청값·처리 상태·실제 draft/결과 검증·실패 문구를 보관한다. | [L10](../../src/ListingBatchRefresh.cs#L10) |
| <a id="symbol-src-listingbatchrefresh-cs-16-cf3b326836"></a><code>ListingChangeResult.Text</code><br><code>string Text</code> | 계산 속성 | 항목 처리 결과를 사용자에게 보여줄 문구로 만든다. | [L16](../../src/ListingBatchRefresh.cs#L16) |
| <a id="symbol-src-listingbatchrefresh-cs-25-1fece25dd8"></a><code>ListingBatchRefresh</code><br><code>ListingBatchRefresh(IListingBatchHost host, ListingChange[] changes, long now, Action? editorStarting = null)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L25](../../src/ListingBatchRefresh.cs#L25) |
| <a id="symbol-src-listingbatchrefresh-cs-28-f1fdc7aa96"></a><code>ListingBatchRefresh.Busy</code><br><code>bool Busy</code> | 계산 속성 | 실행 중인 조회/편집/추적 단계가 남아 있는지 계산한다. 계산식: <code>Stage is not (RefreshStage.Completed or RefreshStage.Unconfirmed or RefreshStage.Stopped)</code>. | [L28](../../src/ListingBatchRefresh.cs#L28) |
| <a id="symbol-src-listingbatchrefresh-cs-30-a88a6d9bb4"></a><code>ListingBatchRefresh.Results</code><br><code>IReadOnlyList&lt;ListingChangeResult&gt; Results</code> | 계산 속성 | 배치의 항목별 처리 결과를 읽기 전용으로 반환한다. 계산식: <code>results</code>. | [L30](../../src/ListingBatchRefresh.cs#L30) |
| <a id="symbol-src-listingbatchrefresh-cs-32-1cc684df00"></a><code>ListingBatchRefresh.EditorStarted</code><br><code>bool EditorStarted</code> | 계산 속성 | 수정창 진입을 이미 시도했는지 반환한다. 모집이 중지됐을 가능성을 판단하는 근거다. 계산식: <code>editCalls &gt; 0</code>. | [L32](../../src/ListingBatchRefresh.cs#L32) |
| <a id="symbol-src-listingbatchrefresh-cs-33-cc5ba05b55"></a><code>ListingBatchRefresh.Submitted</code><br><code>bool Submitted</code> | 계산 속성 | 최종 제출을 이미 시도했는지 반환한다. 응답 미확정이어도 재전송 금지 판단에 사용한다. 계산식: <code>submitCalls &gt; 0</code>. | [L33](../../src/ListingBatchRefresh.cs#L33) |
| <a id="symbol-src-listingbatchrefresh-cs-34-73551491e9"></a><code>ListingBatchRefresh.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L34](../../src/ListingBatchRefresh.cs#L34) |
| <a id="symbol-src-listingbatchrefresh-cs-45-5921c54d4a"></a><code>ListingBatchRefresh.Update</code><br><code>void Update(long tick)</code> | 메서드 | 배치의 fresh 조회·source 검증·수정창 진입·EditingBatch·제출 후 조회 검증을 진행한다. 창 진입 callback은 최초 시도 직전에 소비한다. | [L45](../../src/ListingBatchRefresh.cs#L45) |
| <a id="symbol-src-listingbatchrefresh-cs-114-6d20b1ef74"></a><code>ListingBatchRefresh.UpdateItems</code><br><code>void UpdateItems(long tick)</code> | 메서드 | 현재 게임 draft에 각 변경을 Rebase해 시작/관찰/정리한다. 변경 불가 항목은 결과에 남기고 다음 항목으로 진행하되 identity/정리 실패는 전체를 멈춘다. | [L114](../../src/ListingBatchRefresh.cs#L114) |
| <a id="symbol-src-listingbatchrefresh-cs-163-1209c323e5"></a><code>ListingBatchRefresh.FinishItem</code><br><code>void FinishItem(long tick, bool verified, string error)</code> | 메서드 | 항목의 편집 단계 성공 여부와 오류를 보관하고 자식 창/부분 반영 정리 단계로 전환한다. | [L163](../../src/ListingBatchRefresh.cs#L163) |
| <a id="symbol-src-listingbatchrefresh-cs-169-d20c6803ac"></a><code>ListingBatchRefresh.Submit</code><br><code>void Submit(long tick)</code> | 메서드 | 누적 실제 draft를 최종 확인하고 SubmitBatch를 1회만 실행한다. 성공한 항목이 없어도 유효한 기존 조건으로 갱신할 수 있다. | [L169](../../src/ListingBatchRefresh.cs#L169) |
| <a id="symbol-src-listingbatchrefresh-cs-177-d7740c4926"></a><code>ListingBatchRefresh.Query</code><br><code>void Query(long tick, bool verify)</code> | 메서드 | 새 상세 revision을 기준으로 초기/검증용 조회를 예약한다. | [L177](../../src/ListingBatchRefresh.cs#L177) |
| <a id="symbol-src-listingbatchrefresh-cs-183-f79bc0143e"></a><code>ListingBatchRefresh.Cancel</code><br><code>void Cancel()</code> | 메서드 | 배치를 중단한다. 제출 전후에 따라 Stopped/Unconfirmed 의미를 유지한다. | [L183](../../src/ListingBatchRefresh.cs#L183) |
| <a id="symbol-src-listingbatchrefresh-cs-184-27d6214f67"></a><code>ListingBatchRefresh.Fail</code><br><code>void Fail(string message)</code> | 메서드 | 외부 예외를 배치 종료 사유로 처리한다. | [L184](../../src/ListingBatchRefresh.cs#L184) |
| <a id="symbol-src-listingbatchrefresh-cs-185-cb7644356b"></a><code>ListingBatchRefresh.Stop</code><br><code>void Stop(string message, string code)</code> | 메서드 | 남은 항목 상태·진단·전체 상태를 마무리한다. 제출 후 불확실한 결과를 성공으로 바꾸지 않는다. | [L185](../../src/ListingBatchRefresh.cs#L185) |
| <a id="symbol-src-listingbatchrefresh-cs-191-0e7dd761bb"></a><code>ListingBatchRefresh.Set</code><br><code>void Set(RefreshStage stage, long tick, string status)</code> | 메서드 | 배치 stage·시각·표시 상태를 일괄 갱신한다. | [L191](../../src/ListingBatchRefresh.cs#L191) |

<a id="file-src-listingbeginneredithost-cs"></a>
## src/ListingBeginnerEditHost.cs

[소스 파일](../../src/ListingBeginnerEditHost.cs) · 명명된 항목/진입점 7개

초행 및 초보자 환영의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다.

직접 관련 테스트: [ListingBeginnerEditTests.cs](tests.md#file-tests-listingbeginneredittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingbeginneredithost-cs-12-9cb376c5f7"></a><code>ListingRefreshHost.BeginnerEditDiagnostic</code><br><code>string BeginnerEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L12](../../src/ListingBeginnerEditHost.cs#L12) |
| <a id="symbol-src-listingbeginneredithost-cs-17-4ce063805d"></a><code>ListingRefreshHost.BeginnerSession</code><br><code>BeginnerSession(RefreshTarget original, ListingBeginnerEdit edit, string invariant, AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentCheckBox* checkbox)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L17](../../src/ListingBeginnerEditHost.cs#L17) |
| <a id="symbol-src-listingbeginneredithost-cs-31-f2c25ff77b"></a><code>ListingRefreshHost.BeginBeginnerEdit</code><br><code>bool BeginBeginnerEdit(RefreshTarget target, ListingBeginnerEdit edit, out string error)</code> | 메서드 | 초행 및 초보자 환영 요청과 원본, 수정창·멤버·컨트롤 경로를 검사하여 세션을 만들고 첫 UI 동작을 시작한다. | [L31](../../src/ListingBeginnerEditHost.cs#L31) |
| <a id="symbol-src-listingbeginneredithost-cs-69-89d7b08bad"></a><code>ListingRefreshHost.UpdateBeginnerEdit</code><br><code>SlotEditProgress UpdateBeginnerEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 초행 및 초보자 환영의 바인딩/불변식과 UI 상태 기계를 진행한다. 완료 시 expected를 editingTarget으로 바꾸고 전체 수정창을 다시 검증한다. | [L69](../../src/ListingBeginnerEditHost.cs#L69) |
| <a id="symbol-src-listingbeginneredithost-cs-92-6c6a180412"></a><code>ListingRefreshHost.ValidateBeginnerGuard</code><br><code>bool ValidateBeginnerGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, BeginnerSession session, bool final, out AtkEvent clickEvent, out string error)</code> | 메서드 | 같은 캐릭터·Addon/Agent/컨트롤 주소·닫힘 revision·Refresh 모드와 변경 제외 필드를 검사한다. final이면 초행 및 초보자 환영 UI/저장값 일치까지 요구한다. | [L92](../../src/ListingBeginnerEditHost.cs#L92) |
| <a id="symbol-src-listingbeginneredithost-cs-118-366ad7ba10"></a><code>ListingRefreshHost.ReadBeginnerControl</code><br><code>bool ReadBeginnerControl(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, out AtkComponentCheckBox* checkbox, out CheckboxUiState state, out AtkEvent clickEvent, out string error)</code> | 메서드 | 초행 및 초보자 환영 컨트롤의 포인터·소유관계·표시·활성·등록 event 경로를 확인하고 관리형 UI state를 읽는다. 사용할 수 없으면 원인 코드를 남긴다. | [L118](../../src/ListingBeginnerEditHost.cs#L118) |
| <a id="symbol-src-listingbeginneredithost-cs-135-7e4a999901"></a><code>ListingRefreshHost.BeginnerInvariant</code><br><code>string BeginnerInvariant(AgentLookingForGroup* agent)</code> | 메서드 | DraftInvariant에서 초행 및 초보자 환영로 변경할 필드만 제외하고 모든 슬롯 raw mask를 추가해 다른 조건의 변경을 탐지한다. | [L135](../../src/ListingBeginnerEditHost.cs#L135) |

<a id="file-src-listingcompletionedithost-cs"></a>
## src/ListingCompletionEditHost.cs

[소스 파일](../../src/ListingCompletionEditHost.cs) · 명명된 항목/진입점 10개

완료조건의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다.

직접 관련 테스트: [ListingCompletionEditTests.cs](tests.md#file-tests-listingcompletionedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingcompletionedithost-cs-12-ac1c334eb2"></a><code>ListingRefreshHost.CompletionEditDiagnostic</code><br><code>string CompletionEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L12](../../src/ListingCompletionEditHost.cs#L12) |
| <a id="symbol-src-listingcompletionedithost-cs-22-28b4fdb2d3"></a><code>ListingRefreshHost.CompletionSession</code><br><code>CompletionSession(RefreshTarget original, ListingCompletionEdit edit, string invariant, AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L22](../../src/ListingCompletionEditHost.cs#L22) |
| <a id="symbol-src-listingcompletionedithost-cs-41-d78b4a665a"></a><code>ListingRefreshHost.BeginCompletionEdit</code><br><code>bool BeginCompletionEdit(RefreshTarget target, ListingCompletionEdit edit, out string error)</code> | 메서드 | 완료조건 요청과 원본, 수정창·멤버·컨트롤 경로를 검사하여 세션을 만들고 첫 UI 동작을 시작한다. | [L41](../../src/ListingCompletionEditHost.cs#L41) |
| <a id="symbol-src-listingcompletionedithost-cs-60-41dd46fc3f"></a><code>ListingRefreshHost.UpdateCompletionEdit</code><br><code>SlotEditProgress UpdateCompletionEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 완료조건의 바인딩/불변식과 UI 상태 기계를 진행한다. 완료 시 expected를 editingTarget으로 바꾸고 전체 수정창을 다시 검증한다. | [L60](../../src/ListingCompletionEditHost.cs#L60) |
| <a id="symbol-src-listingcompletionedithost-cs-92-bb3de7d4a7"></a><code>ListingRefreshHost.DispatchCompletion</code><br><code>bool DispatchCompletion(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, CompletionSession session, CompletionUiAction action, out string error)</code> | 메서드 | 체크 토글, dropdown 열기, DispatchItemEvent, SelectItem을 해당 단계에서 한 번 실행한다. 각 동작 사이 바인딩/저장값을 재확인한다. | [L92](../../src/ListingCompletionEditHost.cs#L92) |
| <a id="symbol-src-listingcompletionedithost-cs-147-5201af9b15"></a><code>ListingRefreshHost.ValidateCompletionGuard</code><br><code>bool ValidateCompletionGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, CompletionSession session, bool final, out string error)</code> | 메서드 | 같은 캐릭터·Addon/Agent/컨트롤 주소·닫힘 revision·Refresh 모드와 변경 제외 필드를 검사한다. final이면 완료조건 UI/저장값 일치까지 요구한다. | [L147](../../src/ListingCompletionEditHost.cs#L147) |
| <a id="symbol-src-listingcompletionedithost-cs-177-82f5c4812a"></a><code>ListingRefreshHost.ReadCompletionControls</code><br><code>bool ReadCompletionControls(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, ListingCompletionEdit edit, out CompletionUiState state, out string error)</code> | 메서드 | 완료조건 컨트롤의 포인터·소유관계·표시·활성·등록 event 경로를 확인하고 관리형 UI state를 읽는다. 사용할 수 없으면 원인 코드를 남긴다. | [L177](../../src/ListingCompletionEditHost.cs#L177) |
| <a id="symbol-src-listingcompletionedithost-cs-201-d092a3b993"></a><code>ListingRefreshHost.OwnsComponent</code><br><code>bool OwnsComponent(AtkComponentBase* component)</code> | 메서드 | 읽을 수 있는 OwnerNode가 해당 컴포넌트를 다시 소유하는지 확인한다. | [L201](../../src/ListingCompletionEditHost.cs#L201) |
| <a id="symbol-src-listingcompletionedithost-cs-204-83297dd1df"></a><code>ListingRefreshHost.ReadCompletionRoutes</code><br><code>bool ReadCompletionRoutes(AddonLookingForGroupCondition* addon, out AtkEvent toggleEvent, out AtkEvent openEvent, out string error)</code> | 메서드 | 완료조건 toggle/23, dropdown open/2, list click/highlight/1, editor highlight/22 경로를 확인한다. | [L204](../../src/ListingCompletionEditHost.cs#L204) |
| <a id="symbol-src-listingcompletionedithost-cs-217-d75d85267d"></a><code>ListingRefreshHost.CompletionInvariant</code><br><code>string CompletionInvariant(AgentLookingForGroup* agent)</code> | 메서드 | DraftInvariant에서 완료조건로 변경할 필드만 제외하고 모든 슬롯 raw mask를 추가해 다른 조건의 변경을 탐지한다. | [L217](../../src/ListingCompletionEditHost.cs#L217) |

<a id="file-src-listingdescriptionedithost-cs"></a>
## src/ListingDescriptionEditHost.cs

[소스 파일](../../src/ListingDescriptionEditHost.cs) · 명명된 항목/진입점 7개

소개말의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다.

직접 관련 테스트: [ListingDescriptionEditTests.cs](tests.md#file-tests-listingdescriptionedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdescriptionedithost-cs-15-73b571acbc"></a><code>ListingRefreshHost.DescriptionEditDiagnostic</code><br><code>string DescriptionEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L15](../../src/ListingDescriptionEditHost.cs#L15) |
| <a id="symbol-src-listingdescriptionedithost-cs-24-c3824dc451"></a><code>ListingRefreshHost.DescriptionSession</code><br><code>DescriptionSession(RefreshTarget original, ListingDescriptionEdit edit, string invariant, nint input)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L24](../../src/ListingDescriptionEditHost.cs#L24) |
| <a id="symbol-src-listingdescriptionedithost-cs-40-5ea73ea394"></a><code>ListingRefreshHost.BeginDescriptionEdit</code><br><code>bool BeginDescriptionEdit(RefreshTarget target, ListingDescriptionEdit edit, out string error)</code> | 메서드 | 원본·입력칸·그룹/멤버·UTF-8/문자 제한을 확인하고 CommentTextInput.SetText를 1회 호출한다. 실제 표시된 입력도 세션에 저장한다. | [L40](../../src/ListingDescriptionEditHost.cs#L40) |
| <a id="symbol-src-listingdescriptionedithost-cs-73-ad18c5c095"></a><code>ListingRefreshHost.UpdateDescriptionEdit</code><br><code>SlotEditProgress UpdateDescriptionEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 보이는 값이 요청과 같은지 확인한 뒤 stored comment가 아직 원래 값이면 command 15로 문자열을 한 번 알린다. UI와 stored 모두 일치한 이후에만 완료한다. | [L73](../../src/ListingDescriptionEditHost.cs#L73) |
| <a id="symbol-src-listingdescriptionedithost-cs-129-6b90d4bc0e"></a><code>ListingRefreshHost.ValidateDescriptionGuard</code><br><code>bool ValidateDescriptionGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, DescriptionSession session, bool final, out string error)</code> | 메서드 | 같은 캐릭터·Addon/Agent/컨트롤 주소·닫힘 revision·Refresh 모드와 변경 제외 필드를 검사한다. final이면 소개말 UI/저장값 일치까지 요구한다. | [L129](../../src/ListingDescriptionEditHost.cs#L129) |
| <a id="symbol-src-listingdescriptionedithost-cs-148-8f1c0b5dce"></a><code>ListingRefreshHost.DescriptionInvariant</code><br><code>string DescriptionInvariant(AgentLookingForGroup* agent)</code> | 메서드 | DraftInvariant에서 소개말로 변경할 필드만 제외하고 모든 슬롯 raw mask를 추가해 다른 조건의 변경을 탐지한다. | [L148](../../src/ListingDescriptionEditHost.cs#L148) |
| <a id="symbol-src-listingdescriptionedithost-cs-151-68c5915b43"></a><code>ListingRefreshHost.FireDescriptionCallback</code><br><code>bool FireDescriptionCallback(AtkUnitBase* addon, int command, string text)</code> | 메서드 | UTF-8 NUL 종료 문자열을 동기 호출 동안 pin하고 AtkValue[3]의 Int command/String payload로 FireCallback을 호출한다. command 15는 초안, command 0은 제출에 사용된다. | [L151](../../src/ListingDescriptionEditHost.cs#L151) |

<a id="file-src-listingitemleveledithost-cs"></a>
## src/ListingItemLevelEditHost.cs

[소스 파일](../../src/ListingItemLevelEditHost.cs) · 명명된 항목/진입점 8개

평균 아이템 레벨의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다.

직접 관련 테스트: [ListingItemLevelEditTests.cs](tests.md#file-tests-listingitemleveledittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingitemleveledithost-cs-12-558b7f952c"></a><code>ListingRefreshHost.ItemLevelEditDiagnostic</code><br><code>string ItemLevelEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L12](../../src/ListingItemLevelEditHost.cs#L12) |
| <a id="symbol-src-listingitemleveledithost-cs-21-aacb883804"></a><code>ListingRefreshHost.ItemLevelSession</code><br><code>ItemLevelSession(RefreshTarget original, ListingItemLevelEdit edit, string invariant, AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentCheckBox* checkbox, AtkComponentNumericInput* input)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L21](../../src/ListingItemLevelEditHost.cs#L21) |
| <a id="symbol-src-listingitemleveledithost-cs-36-a0ad299821"></a><code>ListingRefreshHost.BeginItemLevelEdit</code><br><code>bool BeginItemLevelEdit(RefreshTarget target, ListingItemLevelEdit edit, out string error)</code> | 메서드 | 평균 아이템 레벨 요청과 원본, 수정창·멤버·컨트롤 경로를 검사하여 세션을 만들고 첫 UI 동작을 시작한다. | [L36](../../src/ListingItemLevelEditHost.cs#L36) |
| <a id="symbol-src-listingitemleveledithost-cs-51-54d4d331ff"></a><code>ListingRefreshHost.UpdateItemLevelEdit</code><br><code>SlotEditProgress UpdateItemLevelEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 평균 아이템 레벨의 바인딩/불변식과 UI 상태 기계를 진행한다. 완료 시 expected를 editingTarget으로 바꾸고 전체 수정창을 다시 검증한다. | [L51](../../src/ListingItemLevelEditHost.cs#L51) |
| <a id="symbol-src-listingitemleveledithost-cs-70-0bf5ead20b"></a><code>ListingRefreshHost.AdvanceItemLevelEdit</code><br><code>bool AdvanceItemLevelEdit(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, ItemLevelSession session, out bool complete, out string error)</code> | 메서드 | Toggle 또는 Input을 등록 checkbox ButtonClick/4, NumericInput.InnerSetValue로 한 번 실행한다. ValueUpdate/0 경로와 1~999 및 실제 입력 범위를 따른다. | [L70](../../src/ListingItemLevelEditHost.cs#L70) |
| <a id="symbol-src-listingitemleveledithost-cs-111-dc271dc55d"></a><code>ListingRefreshHost.ValidateItemLevelGuard</code><br><code>bool ValidateItemLevelGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, ItemLevelSession session, bool final, out AtkEvent clickEvent, out string error)</code> | 메서드 | 같은 캐릭터·Addon/Agent/컨트롤 주소·닫힘 revision·Refresh 모드와 변경 제외 필드를 검사한다. final이면 평균 아이템 레벨 UI/저장값 일치까지 요구한다. | [L111](../../src/ListingItemLevelEditHost.cs#L111) |
| <a id="symbol-src-listingitemleveledithost-cs-138-992024d700"></a><code>ListingRefreshHost.ReadItemLevelControls</code><br><code>bool ReadItemLevelControls(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, out AtkComponentCheckBox* checkbox, out AtkComponentNumericInput* input, out ItemLevelUiState state, out AtkEvent clickEvent, out string error)</code> | 메서드 | 평균 아이템 레벨 컨트롤의 포인터·소유관계·표시·활성·등록 event 경로를 확인하고 관리형 UI state를 읽는다. 사용할 수 없으면 원인 코드를 남긴다. | [L138](../../src/ListingItemLevelEditHost.cs#L138) |
| <a id="symbol-src-listingitemleveledithost-cs-163-0a9510ffd4"></a><code>ListingRefreshHost.ItemLevelInvariant</code><br><code>string ItemLevelInvariant(AgentLookingForGroup* agent)</code> | 메서드 | DraftInvariant에서 평균 아이템 레벨로 변경할 필드만 제외하고 모든 슬롯 raw mask를 추가해 다른 조건의 변경을 탐지한다. | [L163](../../src/ListingItemLevelEditHost.cs#L163) |

<a id="file-src-listingobjectiveedithost-cs"></a>
## src/ListingObjectiveEditHost.cs

[소스 파일](../../src/ListingObjectiveEditHost.cs) · 명명된 항목/진입점 10개

모집 목적의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다.

직접 관련 테스트: [ListingObjectiveEditTests.cs](tests.md#file-tests-listingobjectiveedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingobjectiveedithost-cs-12-1ee2a5a066"></a><code>ListingRefreshHost.ObjectiveEditDiagnostic</code><br><code>string ObjectiveEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L12](../../src/ListingObjectiveEditHost.cs#L12) |
| <a id="symbol-src-listingobjectiveedithost-cs-20-de395d7be9"></a><code>ListingRefreshHost.ObjectiveSession</code><br><code>ObjectiveSession(RefreshTarget original, ListingObjectiveEdit edit, string invariant, AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentDropDownList* dropdown)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L20](../../src/ListingObjectiveEditHost.cs#L20) |
| <a id="symbol-src-listingobjectiveedithost-cs-35-3b3fa08059"></a><code>ListingRefreshHost.BeginObjectiveEdit</code><br><code>bool BeginObjectiveEdit(RefreshTarget target, ListingObjectiveEdit edit, out string error)</code> | 메서드 | 모집 목적 요청과 원본, 수정창·멤버·컨트롤 경로를 검사하여 세션을 만들고 첫 UI 동작을 시작한다. | [L35](../../src/ListingObjectiveEditHost.cs#L35) |
| <a id="symbol-src-listingobjectiveedithost-cs-74-0ffec7bf65"></a><code>ListingRefreshHost.UpdateObjectiveEdit</code><br><code>SlotEditProgress UpdateObjectiveEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 모집 목적의 바인딩/불변식과 UI 상태 기계를 진행한다. 완료 시 expected를 editingTarget으로 바꾸고 전체 수정창을 다시 검증한다. | [L74](../../src/ListingObjectiveEditHost.cs#L74) |
| <a id="symbol-src-listingobjectiveedithost-cs-131-8082132a1a"></a><code>ListingRefreshHost.ValidateObjectiveGuard</code><br><code>bool ValidateObjectiveGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, ObjectiveSession session, bool final, out string error)</code> | 메서드 | 같은 캐릭터·Addon/Agent/컨트롤 주소·닫힘 revision·Refresh 모드와 변경 제외 필드를 검사한다. final이면 모집 목적 UI/저장값 일치까지 요구한다. | [L131](../../src/ListingObjectiveEditHost.cs#L131) |
| <a id="symbol-src-listingobjectiveedithost-cs-157-dfe18fc961"></a><code>ListingRefreshHost.ReadObjectiveControl</code><br><code>bool ReadObjectiveControl(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, int selected, out AtkComponentDropDownList* dropdown, out ObjectiveUiState state, out string error)</code> | 메서드 | node 16 dropdown, node 2 열림 checkbox, node 3 list와 4개 항목/선택 가능 상태를 확인한다. | [L157](../../src/ListingObjectiveEditHost.cs#L157) |
| <a id="symbol-src-listingobjectiveedithost-cs-178-054123033e"></a><code>ListingRefreshHost.ComponentOwnsNode</code><br><code>bool ComponentOwnsNode(AtkComponentBase* component, uint id)</code> | 메서드 | 컴포넌트의 OwnerNode가 예상 ID이며 다시 같은 컴포넌트를 가리키는지 검사한다. | [L178](../../src/ListingObjectiveEditHost.cs#L178) |
| <a id="symbol-src-listingobjectiveedithost-cs-182-169a32b740"></a><code>ListingRefreshHost.ReadObjectiveRoutes</code><br><code>bool ReadObjectiveRoutes(AddonLookingForGroupCondition* addon, AtkComponentDropDownList* dropdown, out AtkEvent openEvent, out string error)</code> | 메서드 | open ButtonClick/2, list click/highlight/1, editor highlight/20의 등록 경로를 검증한다. | [L182](../../src/ListingObjectiveEditHost.cs#L182) |
| <a id="symbol-src-listingobjectiveedithost-cs-194-f4e79309cb"></a><code>ListingRefreshHost.ReadRegisteredUiEvent</code><br><code>bool ReadRegisteredUiEvent(AtkResNode* node, AtkEventListener* listener, AtkEventType type, uint param, out AtkEvent value)</code> | 메서드 | 유일한 type/param/target/listener 일치 event를 값으로 복사한다. 32개 제한, 순환·다중 일치·포인터 불량이면 거부한다. | [L194](../../src/ListingObjectiveEditHost.cs#L194) |
| <a id="symbol-src-listingobjectiveedithost-cs-208-2f8e682b46"></a><code>ListingRefreshHost.ObjectiveInvariant</code><br><code>string ObjectiveInvariant(AgentLookingForGroup* agent)</code> | 메서드 | DraftInvariant에서 모집 목적로 변경할 필드만 제외하고 모든 슬롯 raw mask를 추가해 다른 조건의 변경을 탐지한다. | [L208](../../src/ListingObjectiveEditHost.cs#L208) |

<a id="file-src-listingprivateedithost-cs"></a>
## src/ListingPrivateEditHost.cs

[소스 파일](../../src/ListingPrivateEditHost.cs) · 명명된 항목/진입점 10개

비공개/PIN의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다.

직접 관련 테스트: [ListingPrivateEditTests.cs](tests.md#file-tests-listingprivateedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingprivateedithost-cs-13-53959e8733"></a><code>ListingRefreshHost.PrivateEditDiagnostic</code><br><code>string PrivateEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L13](../../src/ListingPrivateEditHost.cs#L13) |
| <a id="symbol-src-listingprivateedithost-cs-22-181989c587"></a><code>ListingRefreshHost.PrivateSession</code><br><code>PrivateSession(RefreshTarget original, ListingPrivateEdit edit, string invariant, AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentCheckBox* checkbox, AtkComponentNumericInput* input)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L22](../../src/ListingPrivateEditHost.cs#L22) |
| <a id="symbol-src-listingprivateedithost-cs-35-fb02666750"></a><code>ListingRefreshHost.PrivateSession.PasswordUiMatches</code><br><code>bool PasswordUiMatches</code> | 계산 속성 | 요청 PIN과 숫자 입력 UI가 일치하는지만 반환하며 숫자 자체는 diagnostic에 내보내지 않는다. 계산식: <code>Edit.Enabled &amp;&amp; State.NumericValue == Edit.PasswordValue</code>. | [L35](../../src/ListingPrivateEditHost.cs#L35) |
| <a id="symbol-src-listingprivateedithost-cs-36-e987903cee"></a><code>ListingRefreshHost.PrivateSession.PasswordStoredMatches</code><br><code>bool PasswordStoredMatches</code> | 계산 속성 | 요청 PIN과 수정창의 stored password가 일치하는지만 반환한다. 서버에 저장된 PIN 확인을 뜻하지 않는다. 계산식: <code>Edit.Enabled &amp;&amp; State.StoredPassword == Edit.PasswordValue</code>. | [L36](../../src/ListingPrivateEditHost.cs#L36) |
| <a id="symbol-src-listingprivateedithost-cs-39-dd5ed3f6ae"></a><code>ListingRefreshHost.BeginPrivateEdit</code><br><code>bool BeginPrivateEdit(RefreshTarget target, ListingPrivateEdit edit, out string error)</code> | 메서드 | 비공개/PIN 요청과 원본, 수정창·멤버·컨트롤 경로를 검사하여 세션을 만들고 첫 UI 동작을 시작한다. | [L39](../../src/ListingPrivateEditHost.cs#L39) |
| <a id="symbol-src-listingprivateedithost-cs-54-6cbbb1c744"></a><code>ListingRefreshHost.UpdatePrivateEdit</code><br><code>SlotEditProgress UpdatePrivateEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 비공개/PIN의 바인딩/불변식과 UI 상태 기계를 진행한다. 완료 시 expected를 editingTarget으로 바꾸고 전체 수정창을 다시 검증한다. | [L54](../../src/ListingPrivateEditHost.cs#L54) |
| <a id="symbol-src-listingprivateedithost-cs-73-5251f6d2f6"></a><code>ListingRefreshHost.AdvancePrivateEdit</code><br><code>bool AdvancePrivateEdit(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, PrivateSession session, out bool complete, out string error)</code> | 메서드 | 순수 상태 기계의 Toggle/Input 지시에 따라 체크 상태와 등록 ButtonClick/29 또는 NumericInput.InnerSetValue(triggerCallback:true)를 한 번 실행한다. 입력 범위/준비를 검사한다. | [L73](../../src/ListingPrivateEditHost.cs#L73) |
| <a id="symbol-src-listingprivateedithost-cs-117-e9d3119e9a"></a><code>ListingRefreshHost.ValidatePrivateGuard</code><br><code>bool ValidatePrivateGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, PrivateSession session, bool final, out AtkEvent clickEvent, out string error)</code> | 메서드 | 같은 캐릭터·Addon/Agent/컨트롤 주소·닫힘 revision·Refresh 모드와 변경 제외 필드를 검사한다. final이면 비공개/PIN UI/저장값 일치까지 요구한다. | [L117](../../src/ListingPrivateEditHost.cs#L117) |
| <a id="symbol-src-listingprivateedithost-cs-144-859d18ae69"></a><code>ListingRefreshHost.ReadPrivateControls</code><br><code>bool ReadPrivateControls(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, out AtkComponentCheckBox* checkbox, out AtkComponentNumericInput* input, out PrivateUiState state, out AtkEvent clickEvent, out string error)</code> | 메서드 | FormPrivatePartyCheckbox/PasswordNumericInput의 binding과 ButtonClick/29·ValueUpdate/29를 확인한다. 숫자 원문 대신 길이/일치/범위만 진단한다. | [L144](../../src/ListingPrivateEditHost.cs#L144) |
| <a id="symbol-src-listingprivateedithost-cs-170-579eedee74"></a><code>ListingRefreshHost.PrivateInvariant</code><br><code>string PrivateInvariant(AgentLookingForGroup* agent)</code> | 메서드 | DraftInvariant에서 비공개/PIN로 변경할 필드만 제외하고 모든 슬롯 raw mask를 추가해 다른 조건의 변경을 탐지한다. | [L170](../../src/ListingPrivateEditHost.cs#L170) |

<a id="file-src-listingrefresh-cs"></a>
## src/ListingRefresh.cs

[소스 파일](../../src/ListingRefresh.cs) · 명명된 항목/진입점 49개

단일 갱신 상태 기계와 배치 façade, IListingRefreshHost 계약, 갱신 증거 판정. 상태는 네이티브 호출 전에 진행시켜 재발송을 방지한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingrefresh-cs-5-b7e3b8795b"></a><code>RefreshTarget</code><br><code>RefreshTarget(ulong SelfId, ulong ListingId, ushort DutyId, string Description, string Criteria, int? RemainingSeconds, int TotalSlots)</code> | 주 생성자/모델 | 검증한 본인 모집의 identity·조건·시간·인원 원본을 묶는다. Slots/ExtraCriteria/JobEditingAllowed와 함께 편집 guard 및 결과 비교의 기준이다. | [L5](../../src/ListingRefresh.cs#L5) |
| <a id="symbol-src-listingrefresh-cs-27-09e8412f29"></a><code>IListingRefreshHost.QueryOwn</code><br><code>void QueryOwn()</code> | 인터페이스 계약 | 인터페이스 계약. ListingReader.RequestOwn에 조회 예약을 전달한다. | [L27](../../src/ListingRefresh.cs#L27) |
| <a id="symbol-src-listingrefresh-cs-28-e4b9a546dd"></a><code>IListingRefreshHost.CancelQuery</code><br><code>void CancelQuery()</code> | 인터페이스 계약 | 인터페이스 계약. ListingReader의 진행 중 조회를 취소한다. 외부 요청 소유권 검사는 상위 WebHost가 담당한다. | [L28](../../src/ListingRefresh.cs#L28) |
| <a id="symbol-src-listingrefresh-cs-29-bf9d4a96b2"></a><code>IListingRefreshHost.TryGetOwn</code><br><code>bool TryGetOwn(out RefreshTarget target, out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 현재 live Self 상세창과 리더 CID/이름/소개말/slot count를 네이티브 값과 검증한다. Criteria·ExtraCriteria·슬롯을 가진 RefreshTarget을 생성한다. | [L29](../../src/ListingRefresh.cs#L29) |
| <a id="symbol-src-listingrefresh-cs-30-65e08a8b27"></a><code>IListingRefreshHost.OpenEditor</code><br><code>bool OpenEditor(RefreshTarget target, out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 이전 편집 세션을 초기화하고 source가 여전히 같은 본인 모집인지 확인한다. 상세창에 ButtonClick param 0을 보내고 열린 수정창의 수명 기준을 저장한다. | [L30](../../src/ListingRefresh.cs#L30) |
| <a id="symbol-src-listingrefresh-cs-31-52cb76fe2b"></a><code>IListingRefreshHost.BeginSlotEdit</code><br><code>bool BeginSlotEdit(RefreshTarget target, ListingJobEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L31](../../src/ListingRefresh.cs#L31) |
| <a id="symbol-src-listingrefresh-cs-32-8035326f7d"></a><code>IListingRefreshHost.UpdateSlotEdit</code><br><code>SlotEditProgress UpdateSlotEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L32](../../src/ListingRefresh.cs#L32) |
| <a id="symbol-src-listingrefresh-cs-33-51be6381f1"></a><code>IListingRefreshHost.BeginDescriptionEdit</code><br><code>bool BeginDescriptionEdit(RefreshTarget target, ListingDescriptionEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L33](../../src/ListingRefresh.cs#L33) |
| <a id="symbol-src-listingrefresh-cs-34-23eb5e7487"></a><code>IListingRefreshHost.UpdateDescriptionEdit</code><br><code>SlotEditProgress UpdateDescriptionEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L34](../../src/ListingRefresh.cs#L34) |
| <a id="symbol-src-listingrefresh-cs-35-2501a6136e"></a><code>IListingRefreshHost.BeginObjectiveEdit</code><br><code>bool BeginObjectiveEdit(RefreshTarget target, ListingObjectiveEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L35](../../src/ListingRefresh.cs#L35) |
| <a id="symbol-src-listingrefresh-cs-36-e3bab4fa3e"></a><code>IListingRefreshHost.UpdateObjectiveEdit</code><br><code>SlotEditProgress UpdateObjectiveEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L36](../../src/ListingRefresh.cs#L36) |
| <a id="symbol-src-listingrefresh-cs-37-edc596ed26"></a><code>IListingRefreshHost.BeginBeginnerEdit</code><br><code>bool BeginBeginnerEdit(RefreshTarget target, ListingBeginnerEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L37](../../src/ListingRefresh.cs#L37) |
| <a id="symbol-src-listingrefresh-cs-38-60bafb8a6d"></a><code>IListingRefreshHost.UpdateBeginnerEdit</code><br><code>SlotEditProgress UpdateBeginnerEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L38](../../src/ListingRefresh.cs#L38) |
| <a id="symbol-src-listingrefresh-cs-39-9cdcf854f5"></a><code>IListingRefreshHost.BeginUniqueJobsEdit</code><br><code>bool BeginUniqueJobsEdit(RefreshTarget target, ListingUniqueJobsEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L39](../../src/ListingRefresh.cs#L39) |
| <a id="symbol-src-listingrefresh-cs-40-854525348a"></a><code>IListingRefreshHost.UpdateUniqueJobsEdit</code><br><code>SlotEditProgress UpdateUniqueJobsEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L40](../../src/ListingRefresh.cs#L40) |
| <a id="symbol-src-listingrefresh-cs-41-579d3fc75d"></a><code>IListingRefreshHost.BeginPrivateEdit</code><br><code>bool BeginPrivateEdit(RefreshTarget target, ListingPrivateEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L41](../../src/ListingRefresh.cs#L41) |
| <a id="symbol-src-listingrefresh-cs-42-1333151134"></a><code>IListingRefreshHost.UpdatePrivateEdit</code><br><code>SlotEditProgress UpdatePrivateEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L42](../../src/ListingRefresh.cs#L42) |
| <a id="symbol-src-listingrefresh-cs-43-a2bf871f26"></a><code>IListingRefreshHost.BeginCompletionEdit</code><br><code>bool BeginCompletionEdit(RefreshTarget target, ListingCompletionEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L43](../../src/ListingRefresh.cs#L43) |
| <a id="symbol-src-listingrefresh-cs-44-3d5195de6b"></a><code>IListingRefreshHost.UpdateCompletionEdit</code><br><code>SlotEditProgress UpdateCompletionEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L44](../../src/ListingRefresh.cs#L44) |
| <a id="symbol-src-listingrefresh-cs-45-4f8bc6e304"></a><code>IListingRefreshHost.BeginItemLevelEdit</code><br><code>bool BeginItemLevelEdit(RefreshTarget target, ListingItemLevelEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L45](../../src/ListingRefresh.cs#L45) |
| <a id="symbol-src-listingrefresh-cs-46-6df5155d63"></a><code>IListingRefreshHost.UpdateItemLevelEdit</code><br><code>SlotEditProgress UpdateItemLevelEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L46](../../src/ListingRefresh.cs#L46) |
| <a id="symbol-src-listingrefresh-cs-47-01b87b70d5"></a><code>IListingRefreshHost.BeginRuleEdit</code><br><code>bool BeginRuleEdit(RefreshTarget target, ListingRuleEdit edit, out string error)</code> | 인터페이스 계약 | 검증된 수정창에서 해당 필드 편집을 시작하는 host 계약이다. 성공/오류를 반환하고 실제 동작은 ListingRefreshHost partial이 구현한다. | [L47](../../src/ListingRefresh.cs#L47) |
| <a id="symbol-src-listingrefresh-cs-48-f3ac4bf558"></a><code>IListingRefreshHost.UpdateRuleEdit</code><br><code>SlotEditProgress UpdateRuleEdit(out RefreshTarget expected, out string error)</code> | 인터페이스 계약 | 진행 중인 필드 편집을 관찰해 Waiting/Complete/Failed와 누적 expected target을 반환하는 host 계약이다. | [L48](../../src/ListingRefresh.cs#L48) |
| <a id="symbol-src-listingrefresh-cs-51-9b92267832"></a><code>IListingRefreshHost.Submit</code><br><code>bool Submit(RefreshTarget target, string? description, out string error)</code> | 인터페이스 계약 | 제출 시각·횟수·WaitingResult 상태를 먼저 기록한 뒤 host.Submit을 1회 호출한다. | [L51](../../src/ListingRefresh.cs#L51) |
| <a id="symbol-src-listingrefresh-cs-58-20944fa3f4"></a><code>ListingRefresh</code><br><code>ListingRefresh(IListingRefreshHost host)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L58](../../src/ListingRefresh.cs#L58) |
| <a id="symbol-src-listingrefresh-cs-62-6822703f2d"></a><code>ListingRefresh.Stage</code><br><code>RefreshStage Stage</code> | 계산 속성 | 배치 활성 여부에 맞는 실제 갱신 stage를 반환한다. | [L62](../../src/ListingRefresh.cs#L62) |
| <a id="symbol-src-listingrefresh-cs-63-4e8ab997b0"></a><code>ListingRefresh.BatchResults</code><br><code>IReadOnlyList&lt;ListingChangeResult&gt; BatchResults</code> | 계산 속성 | 현재 배치의 각 변경 결과를 반환한다. 계산식: <code>batch?.Results ?? []</code>. | [L63](../../src/ListingRefresh.cs#L63) |
| <a id="symbol-src-listingrefresh-cs-64-777bd338b6"></a><code>ListingRefresh.IsBatch</code><br><code>bool IsBatch</code> | 계산 속성 | 현재 실행을 배치 갱신이 담당하는지 확인한다. 계산식: <code>batch != null</code>. | [L64](../../src/ListingRefresh.cs#L64) |
| <a id="symbol-src-listingrefresh-cs-66-8069d36356"></a><code>ListingRefresh.EditorStarted</code><br><code>bool EditorStarted</code> | 계산 속성 | 수정창 진입을 이미 시도했는지 반환한다. 모집이 중지됐을 가능성을 판단하는 근거다. 계산식: <code>batch?.EditorStarted ?? editCalls &gt; 0</code>. | [L66](../../src/ListingRefresh.cs#L66) |
| <a id="symbol-src-listingrefresh-cs-67-a852f337e2"></a><code>ListingRefresh.Submitted</code><br><code>bool Submitted</code> | 계산 속성 | 최종 제출을 이미 시도했는지 반환한다. 응답 미확정이어도 재전송 금지 판단에 사용한다. 계산식: <code>batch?.Submitted ?? submitCalls &gt; 0</code>. | [L67](../../src/ListingRefresh.cs#L67) |
| <a id="symbol-src-listingrefresh-cs-69-dadb9bc6e0"></a><code>ListingRefresh.Busy</code><br><code>bool Busy</code> | 계산 속성 | 실행 중인 조회/편집/추적 단계가 남아 있는지 계산한다. | [L69](../../src/ListingRefresh.cs#L69) |
| <a id="symbol-src-listingrefresh-cs-81-d614fb1cd7"></a><code>ListingRefresh.Status</code><br><code>string Status</code> | 계산 속성 | 현재 사용자용 연결/작업 진행 문구를 반환한다. | [L81](../../src/ListingRefresh.cs#L81) |
| <a id="symbol-src-listingrefresh-cs-82-216c187f4c"></a><code>ListingRefresh.CoolingDown</code><br><code>bool CoolingDown(long now)</code> | 메서드 | 최근 제출 이후 5초 쿨다운 여부를 확인한다. | [L82](../../src/ListingRefresh.cs#L82) |
| <a id="symbol-src-listingrefresh-cs-83-56af1192a7"></a><code>ListingRefresh.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L83](../../src/ListingRefresh.cs#L83) |
| <a id="symbol-src-listingrefresh-cs-105-1a7e8c2c6a"></a><code>ListingRefresh.RequestBatch</code><br><code>void RequestBatch(long now, ListingPendingChanges pending)</code> | 메서드 | 대기 변경 또는 명시 배열로 배치 갱신을 예약한다. 대기열 전달 overload는 수정창 진입 때만 Consume하도록 callback을 연결한다. | [L105](../../src/ListingRefresh.cs#L105) |
| <a id="symbol-src-listingrefresh-cs-111-8a404793d3"></a><code>ListingRefresh.RequestBatch</code><br><code>void RequestBatch(long now, ListingChange[] changes)</code> | 메서드 | 대기 변경 또는 명시 배열로 배치 갱신을 예약한다. 대기열 전달 overload는 수정창 진입 때만 Consume하도록 callback을 연결한다. | [L111](../../src/ListingRefresh.cs#L111) |
| <a id="symbol-src-listingrefresh-cs-113-609fe96453"></a><code>ListingRefresh.RequestBatch</code><br><code>void RequestBatch(long now, ListingChange[] changes, Action? editorStarting)</code> | 메서드 | 대기 변경 또는 명시 배열로 배치 갱신을 예약한다. 대기열 전달 overload는 수정창 진입 때만 Consume하도록 callback을 연결한다. | [L113](../../src/ListingRefresh.cs#L113) |
| <a id="symbol-src-listingrefresh-cs-122-2e61c70c05"></a><code>ListingRefresh.Request</code><br><code>void Request(long now, ListingJobEdit? edit = null, ListingDescriptionEdit? descriptionEdit = null, ListingObjectiveEdit? objectiveEdit = null, ListingBeginnerEdit? beginnerEdit = null, ListingUniqueJobsEdit? uniqueJobsEdit = null, ListingPrivateEdit? privateEdit = null, ListingCompletionEdit? completionEdit = null, ListingItemLevelEdit? itemLevelEdit = null, ListingRuleEdit? ruleEdit = null, RefreshTarget? source = null)</code> | 메서드 | 본인/원본/쿨다운과 단일 편집 요청의 조합을 검증한 뒤 Queued로 만든다. source가 있으면 이후 재조회 결과를 그 snapshot에 묶는다. | [L122](../../src/ListingRefresh.cs#L122) |
| <a id="symbol-src-listingrefresh-cs-171-e9afcd3cc2"></a><code>ListingRefresh.Update</code><br><code>void Update(long now)</code> | 메서드 | 조회→본인 증거→수정창→해당 필드 편집→1회 제출→새 조회 검증을 진행한다. 배치가 활성화돼 있으면 ListingBatchRefresh에 위임한다. | [L171](../../src/ListingRefresh.cs#L171) |
| <a id="symbol-src-listingrefresh-cs-521-5d42426f00"></a><code>ListingRefresh.Cancel</code><br><code>void Cancel()</code> | 메서드 | 실행 중인 자기 조회를 취소하고 종료한다. 제출 이후에는 결과 미확정으로 남기며 자동 재시도하지 않는다. | [L521](../../src/ListingRefresh.cs#L521) |
| <a id="symbol-src-listingrefresh-cs-527-0ebf5d3ee8"></a><code>ListingRefresh.Fail</code><br><code>void Fail(string message)</code> | 메서드 | 예외 등 외부 오류를 현재 단일/배치 작업의 종료 상태로 전달한다. | [L527](../../src/ListingRefresh.cs#L527) |
| <a id="symbol-src-listingrefresh-cs-530-c8a06224c2"></a><code>ListingRefresh.Submit</code><br><code>void Submit(long now)</code> | 메서드 | 제출 시각·횟수·WaitingResult 상태를 먼저 기록한 뒤 host.Submit을 1회 호출한다. | [L530](../../src/ListingRefresh.cs#L530) |
| <a id="symbol-src-listingrefresh-cs-539-3e018cb239"></a><code>ListingRefresh.BeginQuery</code><br><code>void BeginQuery(long now, bool verify)</code> | 메서드 | 검증 전/후 조회 기준 revision과 시각을 저장하고 본인 모집글 조회를 1회 예약한다. | [L539](../../src/ListingRefresh.cs#L539) |
| <a id="symbol-src-listingrefresh-cs-548-dd2653d02b"></a><code>ListingRefresh.WaitForOwn</code><br><code>void WaitForOwn(string reason, bool verify)</code> | 메서드 | 새 상세창은 왔지만 본인 확인이 아직 끝나지 않은 경우 대기 이유와 횟수를 기록한다. | [L548](../../src/ListingRefresh.cs#L548) |
| <a id="symbol-src-listingrefresh-cs-557-ee5b3f202b"></a><code>ListingRefresh.Stop</code><br><code>void Stop(string message, string code)</code> | 메서드 | 원인 코드·문구를 기록하고 전송 전은 Stopped, 전송 후는 Unconfirmed로 정리한다. 열린 수정창의 모집 중지 상태도 안내한다. | [L557](../../src/ListingRefresh.cs#L557) |
| <a id="symbol-src-listingrefresh-cs-566-f66769dd6e"></a><code>ListingRefresh.SetStage</code><br><code>void SetStage(RefreshStage stage, long now, string message)</code> | 메서드 | 단일 갱신의 stage·진입 시각·status를 함께 바꾼다. | [L566](../../src/ListingRefresh.cs#L566) |
| <a id="symbol-src-listingrefresh-cs-572-8fa65ea756"></a><code>RefreshEvidence.SameRecruitment</code><br><code>bool SameRecruitment(RefreshTarget a, RefreshTarget b)</code> | 메서드 | 본인, 임무, 소개말, 인원수, Criteria/ExtraCriteria를 비교한다. ListingId 및 참가자 비교는 호출자가 별도로 수행한다. | [L572](../../src/ListingRefresh.cs#L572) |
| <a id="symbol-src-listingrefresh-cs-576-6165018ac8"></a><code>RefreshEvidence.ClassifyEditorAction</code><br><code>RecruitmentEditorMode ClassifyEditorAction(string text)</code> | 메서드 | 공백 정규화 후 ‘모집 내용 갱신’과 ‘파티 만들기’를 정확히 구분한다. 부분 문자열 일치로 제출을 허용하지 않는다. | [L576](../../src/ListingRefresh.cs#L576) |
| <a id="symbol-src-listingrefresh-cs-585-1fa4f46ef5"></a><code>RefreshEvidence.ParseSeconds</code><br><code>int? ParseSeconds(string text)</code> | 메서드 | mm:ss, hh:mm:ss, 한국어 분/초 남은 시간을 0~3600초로 해석한다. 불명확하거나 범위를 벗어나면 null이다. | [L585](../../src/ListingRefresh.cs#L585) |

<a id="file-src-listingrefreshhost-cs"></a>
## src/ListingRefreshHost.cs

[소스 파일](../../src/ListingRefreshHost.cs) · 명명된 항목/진입점 29개

게임 스레드에서 상세창/수정창과 AgentLookingForGroup을 다루는 네이티브 경계. 여러 partial 파일이 동일 클래스 상태를 공유한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingrefreshhost-cs-9-c067596256"></a><code>ListingRefreshHost</code><br><code>ListingRefreshHost(Plugin plugin, ListingReader reader)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L9](../../src/ListingRefreshHost.cs#L9) |
| <a id="symbol-src-listingrefreshhost-cs-20-6ee0278bf3"></a><code>ListingRefreshHost.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L20](../../src/ListingRefreshHost.cs#L20) |
| <a id="symbol-src-listingrefreshhost-cs-23-7bc9f03395"></a><code>ListingRefreshHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | 현재 로그인한 본인의 ContentId를 반환한다. 로그아웃/미준비는0이다. 계산식: <code>Plugin.PlayerState.IsLoaded ? Plugin.PlayerState.ContentId : 0</code>. | [L23](../../src/ListingRefreshHost.cs#L23) |
| <a id="symbol-src-listingrefreshhost-cs-24-471bce9fd6"></a><code>ListingRefreshHost.QueryBusy</code><br><code>bool QueryBusy</code> | 계산 속성 | ListingReader의 조회 진행 상태를 반환한다. 계산식: <code>reader.Busy</code>. | [L24](../../src/ListingRefreshHost.cs#L24) |
| <a id="symbol-src-listingrefreshhost-cs-25-d4cb6adfc7"></a><code>ListingRefreshHost.QuerySucceeded</code><br><code>bool QuerySucceeded</code> | 계산 속성 | 마지막 본인 조회가 검증까지 성공했는지 반환한다. 계산식: <code>reader.LastOwnRequestSucceeded</code>. | [L25](../../src/ListingRefreshHost.cs#L25) |
| <a id="symbol-src-listingrefreshhost-cs-26-aa2b11a193"></a><code>ListingRefreshHost.DetailRevision</code><br><code>long DetailRevision</code> | 계산 속성 | 상세창 PostSetup/PostRefresh revision을 읽는다. 계산식: <code>reader.DetailRevision</code>. | [L26](../../src/ListingRefreshHost.cs#L26) |
| <a id="symbol-src-listingrefreshhost-cs-27-ae5537a28a"></a><code>ListingRefreshHost.EditorRevision</code><br><code>long EditorRevision</code> | 계산 속성 | 수정창 PostSetup/PostRefresh revision을 읽는다. 계산식: <code>Interlocked.Read(ref editorRevision)</code>. | [L27](../../src/ListingRefreshHost.cs#L27) |
| <a id="symbol-src-listingrefreshhost-cs-28-e0c06d2470"></a><code>ListingRefreshHost.EditorClosedRevision</code><br><code>long EditorClosedRevision</code> | 계산 속성 | 수정창 PreFinalize revision을 읽는다. 계산식: <code>Interlocked.Read(ref editorClosedRevision)</code>. | [L28](../../src/ListingRefreshHost.cs#L28) |
| <a id="symbol-src-listingrefreshhost-cs-29-33d7bbb2e1"></a><code>ListingRefreshHost.EditorVisible</code><br><code>bool EditorVisible</code> | 계산 속성 | 게임의 모집 수정창이 현재 보이는지 검사한다. 계산식: <code>Visible("LookingForGroupCondition")</code>. | [L29](../../src/ListingRefreshHost.cs#L29) |
| <a id="symbol-src-listingrefreshhost-cs-30-d9cf479db8"></a><code>ListingRefreshHost.EditorReady</code><br><code>bool EditorReady</code> | 계산 속성 | 실제 갱신 버튼을 다시 읽어 텍스트·활성·가시성을 확인한다. 계산식: <code>ReadEditorAction()</code>. | [L30](../../src/ListingRefreshHost.cs#L30) |
| <a id="symbol-src-listingrefreshhost-cs-31-b9811ae175"></a><code>ListingRefreshHost.EditorActionMode</code><br><code>RecruitmentEditorMode EditorActionMode</code> | 계산 속성 | 갱신 버튼을 다시 읽고 Refresh/Create/Unknown을 반환한다. | [L31](../../src/ListingRefreshHost.cs#L31) |
| <a id="symbol-src-listingrefreshhost-cs-33-0c43efded8"></a><code>ListingRefreshHost.BlockReason</code><br><code>string? BlockReason</code> | 계산 속성 | 네이티브 작업을 막는 접속·호환성·이동·확인창/추적 조건의 이유를 반환한다. | [L33](../../src/ListingRefreshHost.cs#L33) |
| <a id="symbol-src-listingrefreshhost-cs-46-71c129b1dc"></a><code>ListingRefreshHost.EditorUpdated</code><br><code>void EditorUpdated()</code> | 메서드 | 수정창 수명 이벤트 revision을 원자적으로 증가시킨다. | [L46](../../src/ListingRefreshHost.cs#L46) |
| <a id="symbol-src-listingrefreshhost-cs-47-8e3adf7b3a"></a><code>ListingRefreshHost.EditorClosed</code><br><code>void EditorClosed()</code> | 메서드 | 수정창 닫힘 revision을 증가시켜 재열린 창을 원래 작업으로 오인하지 않게 한다. | [L47](../../src/ListingRefreshHost.cs#L47) |
| <a id="symbol-src-listingrefreshhost-cs-48-f63e84e2df"></a><code>ListingRefreshHost.QueryOwn</code><br><code>void QueryOwn()</code> | 메서드 | ListingReader.RequestOwn에 조회 예약을 전달한다. | [L48](../../src/ListingRefreshHost.cs#L48) |
| <a id="symbol-src-listingrefreshhost-cs-49-09d389c452"></a><code>ListingRefreshHost.CancelQuery</code><br><code>void CancelQuery()</code> | 메서드 | ListingReader의 진행 중 조회를 취소한다. 외부 요청 소유권 검사는 상위 WebHost가 담당한다. | [L49](../../src/ListingRefreshHost.cs#L49) |
| <a id="symbol-src-listingrefreshhost-cs-53-60beae425e"></a><code>ListingRefreshHost.CaptureOwn</code><br><code>RefreshTarget? CaptureOwn()</code> | 메서드 | TryGetOwn으로 편집 snapshot을 캡처하되 최근 갱신 실패 diagnostic을 수동 읽기가 덮어쓰지 않도록 복원한다. | [L53](../../src/ListingRefreshHost.cs#L53) |
| <a id="symbol-src-listingrefreshhost-cs-60-2bad961828"></a><code>ListingRefreshHost.TryGetOwn</code><br><code>bool TryGetOwn(out RefreshTarget target, out string error)</code> | 메서드 | 현재 live Self 상세창과 리더 CID/이름/소개말/slot count를 네이티브 값과 검증한다. Criteria·ExtraCriteria·슬롯을 가진 RefreshTarget을 생성한다. | [L60](../../src/ListingRefreshHost.cs#L60) |
| <a id="symbol-src-listingrefreshhost-cs-102-b5afcd0a93"></a><code>ListingRefreshHost.OpenEditor</code><br><code>bool OpenEditor(RefreshTarget target, out string error)</code> | 메서드 | 이전 편집 세션을 초기화하고 source가 여전히 같은 본인 모집인지 확인한다. 상세창에 ButtonClick param 0을 보내고 열린 수정창의 수명 기준을 저장한다. | [L102](../../src/ListingRefreshHost.cs#L102) |
| <a id="symbol-src-listingrefreshhost-cs-138-b1e32d74b5"></a><code>ListingRefreshHost.Submit</code><br><code>bool Submit(RefreshTarget target, string? description, out string error)</code> | 메서드 | 원본 바인딩·갱신 버튼·완료된 모든 edit session을 최종 검증한다. 소개말 수정이면 command 0에 문자열을 함께 보내고, 아니면 ClickZero로 1회 갱신한다. | [L138](../../src/ListingRefreshHost.cs#L138) |
| <a id="symbol-src-listingrefreshhost-cs-193-ca844bd118"></a><code>ListingRefreshHost.ValidateEditor</code><br><code>bool ValidateEditor(RefreshTarget target, out AddonLookingForGroupCondition* addon, out AgentLookingForGroup* agent, out string error)</code> | 메서드 | 본인·수정창 revision/close revision·Refresh 모드·소개말·그룹/참가자·criteria를 검증한다. 수정창에서 모집이 일시 중지되어 active listing이 비는 상황은 허용한다. | [L193](../../src/ListingRefreshHost.cs#L193) |
| <a id="symbol-src-listingrefreshhost-cs-259-7c2911f591"></a><code>ListingRefreshHost.ReadEditorAction</code><br><code>bool ReadEditorAction()</code> | 메서드 | 실제 RecruitMembersButton의 텍스트·가시성·활성화를 읽고 Refresh/Create/Unknown을 판정한다. | [L259](../../src/ListingRefreshHost.cs#L259) |
| <a id="symbol-src-listingrefreshhost-cs-277-099f9bdfbb"></a><code>ListingRefreshHost.DetailCriteria</code><br><code>string DetailCriteria(ref AgentLookingForGroup.Detailed detail, int nativeSlots)</code> | 메서드 | 서버 상세의 category/duty/objective/beginner/completion/finder/loot/language/roles를 공통 비교 문자열로 만든다. | [L277](../../src/ListingRefreshHost.cs#L277) |
| <a id="symbol-src-listingrefreshhost-cs-282-52c0e75ee7"></a><code>ListingRefreshHost.DraftCriteria</code><br><code>string DraftCriteria(ref AgentLookingForGroup.RecruitmentSub draft, int slots)</code> | 메서드 | StoredRecruitmentInfo의 동일 필드를 상세와 같은 포맷으로 만든다. | [L282](../../src/ListingRefreshHost.cs#L282) |
| <a id="symbol-src-listingrefreshhost-cs-287-fcd671b2da"></a><code>ListingRefreshHost.Criteria</code><br><code>string Criteria(uint category, ushort duty, byte objective, byte beginner, byte completion, byte finder, byte loot, byte language, ReadOnlySpan&lt;ulong&gt; roles)</code> | 메서드 | 8개 스칼라 필드와 raw ulong 슬롯 바이트의 hex를 ‘/’로 연결한다. 슬롯 hex는 메모리 바이트 순서이므로 little-endian 해석이 필요하다. | [L287](../../src/ListingRefreshHost.cs#L287) |
| <a id="symbol-src-listingrefreshhost-cs-292-f3d4065a40"></a><code>ListingRefreshHost.DifferentCriteria</code><br><code>string DifferentCriteria(string expected, string actual)</code> | 메서드 | 9개 필드 중 다른 필드 이름을 반환한다. 포맷 자체가 다르면 format이다. | [L292](../../src/ListingRefreshHost.cs#L292) |
| <a id="symbol-src-listingrefreshhost-cs-301-65755b16ea"></a><code>ListingRefreshHost.ClickZero</code><br><code>bool ClickZero(AtkUnitBase* addon, out string error)</code> | 메서드 | 준비된 Addon에 ButtonClick(25), param 0, 빈 event/data를 1회 보낸다. FireCallback(0)와 다른 경로다. | [L301](../../src/ListingRefreshHost.cs#L301) |
| <a id="symbol-src-listingrefreshhost-cs-316-5953478685"></a><code>ListingRefreshHost.Visible</code><br><code>bool Visible(string name)</code> | 메서드 | 읽을 수 있는 Addon이 표시 상태인지 확인한다. | [L316](../../src/ListingRefreshHost.cs#L316) |
| <a id="symbol-src-listingrefreshhost-cs-322-2e06389825"></a><code>ListingRefreshHost.Ready</code><br><code>bool Ready(string name)</code> | 메서드 | 읽을 수 있는 Addon이 표시 및 준비 완료 상태인지 확인한다. | [L322](../../src/ListingRefreshHost.cs#L322) |

<a id="file-src-listingruleedithost-cs"></a>
## src/ListingRuleEditHost.cs

[소스 파일](../../src/ListingRuleEditHost.cs) · 명명된 항목/진입점 9개

임무 찾기/전리품 규칙의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다.

직접 관련 테스트: [ListingRuleEditTests.cs](tests.md#file-tests-listingruleedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingruleedithost-cs-12-117ac5d8c1"></a><code>ListingRefreshHost.RuleEditDiagnostic</code><br><code>string RuleEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L12](../../src/ListingRuleEditHost.cs#L12) |
| <a id="symbol-src-listingruleedithost-cs-23-ead3df3641"></a><code>ListingRefreshHost.RuleControlState</code><br><code>RuleControlState(nint Control, nint Node, uint NodeId, nint OpenButton, nint OpenNode, nint List, nint ListNode, byte Flags, CheckboxUiState Toggle, LootUiState Loot)</code> | 주 생성자/모델 | 해당 finder/loot control과 node/list/open button 주소 및 읽은 flag/UI state를 고정한다. | [L23](../../src/ListingRuleEditHost.cs#L23) |
| <a id="symbol-src-listingruleedithost-cs-26-e93a3cfbea"></a><code>ListingRefreshHost.RuleControlState.SameControls</code><br><code>bool SameControls(RuleControlState other)</code> | 메서드 | 읽은 checkbox/dropdown/list의 주소와 node 주소/ID가 시작 때와 같은지 비교한다. | [L26](../../src/ListingRuleEditHost.cs#L26) |
| <a id="symbol-src-listingruleedithost-cs-30-233387f7f5"></a><code>ListingRefreshHost.RuleSession</code><br><code>RuleSession(RefreshTarget original, ListingRuleEdit edit, string invariant, AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, RuleControlState state)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L30](../../src/ListingRuleEditHost.cs#L30) |
| <a id="symbol-src-listingruleedithost-cs-47-acd5d68197"></a><code>ListingRefreshHost.BeginRuleEdit</code><br><code>bool BeginRuleEdit(RefreshTarget target, ListingRuleEdit edit, out string error)</code> | 메서드 | 임무 찾기/전리품 규칙 요청과 원본, 수정창·멤버·컨트롤 경로를 검사하여 세션을 만들고 첫 UI 동작을 시작한다. | [L47](../../src/ListingRuleEditHost.cs#L47) |
| <a id="symbol-src-listingruleedithost-cs-99-3bb77add09"></a><code>ListingRefreshHost.UpdateRuleEdit</code><br><code>SlotEditProgress UpdateRuleEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 임무 찾기/전리품 규칙의 바인딩/불변식과 UI 상태 기계를 진행한다. 완료 시 expected를 editingTarget으로 바꾸고 전체 수정창을 다시 검증한다. | [L99](../../src/ListingRuleEditHost.cs#L99) |
| <a id="symbol-src-listingruleedithost-cs-156-2b9771b25e"></a><code>ListingRefreshHost.ValidateRuleGuard</code><br><code>bool ValidateRuleGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, RuleSession session, bool final, out AtkEvent clickEvent, out string error)</code> | 메서드 | 같은 캐릭터·Addon/Agent/컨트롤 주소·닫힘 revision·Refresh 모드와 변경 제외 필드를 검사한다. final이면 임무 찾기/전리품 규칙 UI/저장값 일치까지 요구한다. | [L156](../../src/ListingRuleEditHost.cs#L156) |
| <a id="symbol-src-listingruleedithost-cs-182-1cc22f1ecf"></a><code>ListingRefreshHost.ReadRuleControl</code><br><code>bool ReadRuleControl(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, ListingRuleEdit edit, out RuleControlState state, out AtkEvent clickEvent, out string error)</code> | 메서드 | 제한 해제 node99/최저 레벨100/초월 무효101의 공통 click param25와 loot dropdown highlight26을 검증한다. 콘텐츠별 disabled/hidden이면 원인을 반환한다. | [L182](../../src/ListingRuleEditHost.cs#L182) |
| <a id="symbol-src-listingruleedithost-cs-238-9ba44154ef"></a><code>ListingRefreshHost.RuleInvariant</code><br><code>string RuleInvariant(AgentLookingForGroup* agent, ListingRuleKind kind)</code> | 메서드 | DraftInvariant에서 임무 찾기/전리품 규칙로 변경할 필드만 제외하고 모든 슬롯 raw mask를 추가해 다른 조건의 변경을 탐지한다. | [L238](../../src/ListingRuleEditHost.cs#L238) |

<a id="file-src-listingslotedithost-cs"></a>
## src/ListingSlotEditHost.cs

[소스 파일](../../src/ListingSlotEditHost.cs) · 명명된 항목/진입점 12개

직업 편집의 실제 버튼 실행과 guard. 일반/24인/48인/친선 경로를 분리하며 확인 후 실제 raw mask를 고정한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingslotedithost-cs-18-9b4bf1f28d"></a><code>ListingRefreshHost.SlotEditDiagnostic</code><br><code>string SlotEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L18](../../src/ListingSlotEditHost.cs#L18) |
| <a id="symbol-src-listingslotedithost-cs-29-e200dfb0fe"></a><code>ListingRefreshHost.RoleOpened</code><br><code>void RoleOpened()</code> | 메서드 | 직업 선택창 열림 revision을 증가시킨다. | [L29](../../src/ListingSlotEditHost.cs#L29) |
| <a id="symbol-src-listingslotedithost-cs-30-afa91dc969"></a><code>ListingRefreshHost.RoleClosed</code><br><code>void RoleClosed()</code> | 메서드 | 직업 선택창 닫힘 revision을 증가시킨다. | [L30](../../src/ListingSlotEditHost.cs#L30) |
| <a id="symbol-src-listingslotedithost-cs-32-7ae277d942"></a><code>ListingRefreshHost.SlotEditSession</code><br><code>SlotEditSession(RefreshTarget original, ListingJobEdit edit, ListingSlotRoute route, string invariant, ulong[] allFlags, RoleButtonSequence sequence, long openedRevision, long closedRevision)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L32](../../src/ListingSlotEditHost.cs#L32) |
| <a id="symbol-src-listingslotedithost-cs-50-8489975a66"></a><code>ListingRefreshHost.BeginSlotEdit</code><br><code>bool BeginSlotEdit(RefreshTarget target, ListingJobEdit edit, out string error)</code> | 메서드 | 빈자리 원본·route·버튼 계획·수정창 불변식을 검증한 뒤 자리/그룹 버튼을 한 번 누른다. 친선은 기존 mask의 차이만 토글하며 나머지는 Anyone 후 제거 계획을 사용한다. | [L50](../../src/ListingSlotEditHost.cs#L50) |
| <a id="symbol-src-listingslotedithost-cs-95-4b04827686"></a><code>ListingRefreshHost.UpdateSlotEdit</code><br><code>SlotEditProgress UpdateSlotEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 새 role 창을 기다리고 48인은 멤버를 선택한 뒤 계획된 직업 버튼과 확인을 실행한다. 닫힘 후 요청 known bits 및 나머지 raw flags를 검증해 실제 ConfirmedTarget을 반환한다. | [L95](../../src/ListingSlotEditHost.cs#L95) |
| <a id="symbol-src-listingslotedithost-cs-197-f1a0b1d26b"></a><code>ListingRefreshHost.CheckRoleNodes</code><br><code>bool CheckRoleNodes(AtkUnitBase* role, RoleButtonSequence sequence, out string error, bool requireEnabled = false)</code> | 메서드 | 계획에 포함된 모든 버튼 node가 읽히고 유효한지 사전 검사하여 roleButtonRead에 결과를 남긴다. | [L197](../../src/ListingSlotEditHost.cs#L197) |
| <a id="symbol-src-listingslotedithost-cs-212-fd7a8c730a"></a><code>ListingRefreshHost.ValidateSlotGuard</code><br><code>bool ValidateSlotGuard(AgentLookingForGroup* agent, SlotEditSession session, bool final, out string error)</code> | 메서드 | 수정하지 않는 조건·멤버·모든 다른 자리 mask를 보존한다. 대상 자리의 known bits를 최종 검증한 후 게임이 정규화한 raw mask를 고정한다. | [L212](../../src/ListingSlotEditHost.cs#L212) |
| <a id="symbol-src-listingslotedithost-cs-245-8c402f61e5"></a><code>ListingRefreshHost.DraftInvariant</code><br><code>string DraftInvariant(AgentLookingForGroup* agent, bool includeComment = true, bool includeObjective = true, bool includeBeginner = true, bool includeUniqueJobs = true, bool includePassword = true, bool includeCompletion = true, bool includeItemLevel = true, byte ignoredDutyFlags = 0, bool includeLootRule = true)</code> | 메서드 | StoredRecruitmentInfo의 비교 문자열을 만든다. 해당 편집 필드만 선택적으로 제외하며 참가자/그룹/서버 범위 등은 보존한다. 원시 PIN 포함 가능성이 있어 이 문자열을 외부 로그에 내보내면 안 된다. | [L245](../../src/ListingSlotEditHost.cs#L245) |
| <a id="symbol-src-listingslotedithost-cs-258-de8463fb9d"></a><code>ListingRefreshHost.TryEventNode</code><br><code>bool TryEventNode(AtkUnitBase* addon, uint id, out AtkResNode* node, out AtkComponentBase* component, out string reason, bool requireEnabled = false)</code> | 메서드 | Addon→node→component의 포인터·가시성·소유관계·vtable을 검증한다. 친선처럼 필요할 때 enabled도 검사한다. | [L258](../../src/ListingSlotEditHost.cs#L258) |
| <a id="symbol-src-listingslotedithost-cs-286-695490915d"></a><code>ListingRefreshHost.FireNode</code><br><code>void FireNode(AtkUnitBase* addon, AtkResNode* node, AtkComponentBase* component, AtkEventType type, int param)</code> | 메서드 | 검증된 node/component로 AtkEvent와 AtkEventData를 구성해 ReceiveEvent를 1회 실행한다. | [L286](../../src/ListingSlotEditHost.cs#L286) |
| <a id="symbol-src-listingslotedithost-cs-293-519697ade8"></a><code>ListingRefreshHost.TryRegisteredClickParam</code><br><code>bool TryRegisteredClickParam(AtkUnitBase* addon, AtkResNode* node, out int param, out string reason)</code> | 메서드 | 최대 32개, 중복 없는 event chain에서 해당 node/Addon에 연결된 유일한 ButtonClick param을 찾는다. 24인 경로에서 추측 param 대용으로 사용한다. | [L293](../../src/ListingSlotEditHost.cs#L293) |

<a id="file-src-listinguniquejobsedithost-cs"></a>
## src/ListingUniqueJobsEditHost.cs

[소스 파일](../../src/ListingUniqueJobsEditHost.cs) · 명명된 항목/진입점 7개

잡 중복 없음의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다.

직접 관련 테스트: [ListingUniqueJobsEditTests.cs](tests.md#file-tests-listinguniquejobsedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listinguniquejobsedithost-cs-12-7d37614086"></a><code>ListingRefreshHost.UniqueJobsEditDiagnostic</code><br><code>string UniqueJobsEditDiagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L12](../../src/ListingUniqueJobsEditHost.cs#L12) |
| <a id="symbol-src-listinguniquejobsedithost-cs-17-1eeee7db14"></a><code>ListingRefreshHost.UniqueJobsSession</code><br><code>UniqueJobsSession(RefreshTarget original, ListingUniqueJobsEdit edit, string invariant, AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, AtkComponentCheckBox* checkbox)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L17](../../src/ListingUniqueJobsEditHost.cs#L17) |
| <a id="symbol-src-listinguniquejobsedithost-cs-32-2fed50eb6f"></a><code>ListingRefreshHost.BeginUniqueJobsEdit</code><br><code>bool BeginUniqueJobsEdit(RefreshTarget target, ListingUniqueJobsEdit edit, out string error)</code> | 메서드 | 잡 중복 없음 요청과 원본, 수정창·멤버·컨트롤 경로를 검사하여 세션을 만들고 첫 UI 동작을 시작한다. | [L32](../../src/ListingUniqueJobsEditHost.cs#L32) |
| <a id="symbol-src-listinguniquejobsedithost-cs-70-beae4eb23c"></a><code>ListingRefreshHost.UpdateUniqueJobsEdit</code><br><code>SlotEditProgress UpdateUniqueJobsEdit(out RefreshTarget expected, out string error)</code> | 메서드 | 잡 중복 없음의 바인딩/불변식과 UI 상태 기계를 진행한다. 완료 시 expected를 editingTarget으로 바꾸고 전체 수정창을 다시 검증한다. | [L70](../../src/ListingUniqueJobsEditHost.cs#L70) |
| <a id="symbol-src-listinguniquejobsedithost-cs-93-c457f6dc06"></a><code>ListingRefreshHost.ValidateUniqueJobsGuard</code><br><code>bool ValidateUniqueJobsGuard(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, UniqueJobsSession session, bool final, out AtkEvent clickEvent, out string error)</code> | 메서드 | 같은 캐릭터·Addon/Agent/컨트롤 주소·닫힘 revision·Refresh 모드와 변경 제외 필드를 검사한다. final이면 잡 중복 없음 UI/저장값 일치까지 요구한다. | [L93](../../src/ListingUniqueJobsEditHost.cs#L93) |
| <a id="symbol-src-listinguniquejobsedithost-cs-121-4b6f89bef8"></a><code>ListingRefreshHost.ReadUniqueJobsControl</code><br><code>bool ReadUniqueJobsControl(AddonLookingForGroupCondition* addon, AgentLookingForGroup* agent, out AtkComponentCheckBox* checkbox, out CheckboxUiState state, out AtkEvent clickEvent, out string error)</code> | 메서드 | 잡 중복 없음 컨트롤의 포인터·소유관계·표시·활성·등록 event 경로를 확인하고 관리형 UI state를 읽는다. 사용할 수 없으면 원인 코드를 남긴다. | [L121](../../src/ListingUniqueJobsEditHost.cs#L121) |
| <a id="symbol-src-listinguniquejobsedithost-cs-139-4a421fcabd"></a><code>ListingRefreshHost.UniqueJobsInvariant</code><br><code>string UniqueJobsInvariant(AgentLookingForGroup* agent)</code> | 메서드 | DraftInvariant에서 잡 중복 없음로 변경할 필드만 제외하고 모든 슬롯 raw mask를 추가해 다른 조건의 변경을 탐지한다. | [L139](../../src/ListingUniqueJobsEditHost.cs#L139) |
