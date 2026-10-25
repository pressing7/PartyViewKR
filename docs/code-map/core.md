# 플러그인 수명·조회·자리 매핑

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

게임 상태 읽기는 `Plugin.OnUpdate`에서 한다. `PartySnapshot`은 현재 파티, `ListingSnapshot`/`RefreshTarget`은 모집글이다. 직업 수정과 표시의 기준은 후자다.

기준: 소스 `7af86e71affcda547be45cff6214587adb34fea6`, 배포 포함 main `1cecbfbc678d94650d8511c72faea6936baf143f` (0.3.47.0). 2026-10-25 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [src/Plugin.cs](#file-src-plugin-cs)
- [src/ListingDescription.cs](#file-src-listingdescription-cs)
- [src/ListingEditorCriteria.cs](#file-src-listingeditorcriteria-cs)
- [src/ListingIdentity.cs](#file-src-listingidentity-cs)
- [src/ListingJobRules.cs](#file-src-listingjobrules-cs)
- [src/ListingReader.cs](#file-src-listingreader-cs)
- [src/ListingRoleComparison.cs](#file-src-listingrolecomparison-cs)
- [src/ListingSlotRoute.cs](#file-src-listingslotroute-cs)
- [src/ListingSlots.cs](#file-src-listingslots-cs)
- [src/ListingSnapshot.cs](#file-src-listingsnapshot-cs)
- [src/PartyReader.cs](#file-src-partyreader-cs)
- [src/PartyWindow.cs](#file-src-partywindow-cs)
- [src/RuntimeRows.cs](#file-src-runtimerows-cs)

<a id="file-src-plugin-cs"></a>
## src/Plugin.cs

[소스 파일](../../src/Plugin.cs) · 명명된 항목/진입점 54개

Dalamud 진입점. 서비스 주입, UI·명령·Addon 수명 이벤트 등록, 500ms 읽기/갱신 진행, 관리형 편집 초안과 대기 변경을 소유한다. 게임 상태 접근의 중심은 OnUpdate다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-cs-68-5103c08dbd"></a><code>Plugin.CanQueue</code><br><code>bool CanQueue(object edit)</code> | 메서드 | 편집 요청을 ListingChange로 감싸 현재 PendingChanges와 함께 보관할 수 있는지 검사한다. | [L68](../../src/Plugin.cs#L68) |
| <a id="symbol-src-plugin-cs-69-5298666cad"></a><code>Plugin.QueueChange</code><br><code>bool QueueChange(object edit)</code> | 메서드 | 관리형 변경 대기열에 요청을 넣고 PendingStatus를 갱신한다. 게임에는 아직 제출하지 않는다. | [L69](../../src/Plugin.cs#L69) |
| <a id="symbol-src-plugin-cs-76-51c7fb2589"></a><code>Plugin.ListingBusy</code><br><code>bool ListingBusy</code> | 계산 속성 | 현재 본인 모집글 조회가 대기/진행 중인지 반환한다. 계산식: <code>listingReader.Busy</code>. | [L76](../../src/Plugin.cs#L76) |
| <a id="symbol-src-plugin-cs-77-c2c4ebb600"></a><code>Plugin.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L77](../../src/Plugin.cs#L77) |
| <a id="symbol-src-plugin-cs-79-f63bd0015b"></a><code>Plugin.Plugin</code><br><code>Plugin()</code> | 생성자 | 설정과 reader/host/trace/window를 생성하고 /pvkr·/partyviewkr, Draw, Framework.Update 및 세 Addon의 수명 이벤트를 등록한다. | [L79](../../src/Plugin.cs#L79) |
| <a id="symbol-src-plugin-cs-101-cc8c6f4ca9"></a><code>Plugin.OnCommand</code><br><code>void OnCommand(string command, string arguments)</code> | 메서드 | 두 채팅 명령을 공통 창 열기 동작으로 보낸다. 인수별 명령 분기는 없다. | [L101](../../src/Plugin.cs#L101) |
| <a id="symbol-src-plugin-cs-102-d96b5dc8f0"></a><code>Plugin.Open</code><br><code>void Open()</code> | 메서드 | PVKR 창을 표시하고 앞으로 가져온 뒤 다음 읽기를 요청한다. | [L102](../../src/Plugin.cs#L102) |
| <a id="symbol-src-plugin-cs-103-db0e9ccb26"></a><code>Plugin.RequestRead</code><br><code>void RequestRead()</code> | 메서드 | 다음 Framework.Update가 500ms 간격을 기다리지 않도록 readRequested를 세운다. | [L103](../../src/Plugin.cs#L103) |
| <a id="symbol-src-plugin-cs-104-d6c7e906a7"></a><code>Plugin.QueryOwnListing</code><br><code>void QueryOwnListing()</code> | 메서드 | 공통 조회 차단 검사 후 ListingReader에 본인 모집글 조회를 예약한다. | [L104](../../src/Plugin.cs#L104) |
| <a id="symbol-src-plugin-cs-105-f8b5cc7491"></a><code>Plugin.RefreshOwnListing</code><br><code>void RefreshOwnListing()</code> | 메서드 | 충돌 중인 초안·조회·추적·갱신과 cooldown을 검사한다. 대기 변경이 있으면 배치, 없으면 단순 갱신을 예약한다. | [L105](../../src/Plugin.cs#L105) |
| <a id="symbol-src-plugin-cs-116-bd3fdc03f5"></a><code>Plugin.CancelRefresh</code><br><code>void CancelRefresh()</code> | 메서드 | 갱신 상태 기계를 중단하고 UI 읽기를 요청한다. 이미 보낸 제출은 되돌리지 않는다. | [L116](../../src/Plugin.cs#L116) |
| <a id="symbol-src-plugin-cs-117-6e9ac48de7"></a><code>Plugin.RequestSlotEdit</code><br><code>void RequestSlotEdit(int position)</code> | 메서드 | 다른 편집과 충돌하지 않을 때 표시 자리 번호를 저장한다. 실제 모집글 검증과 초안 생성은 PrepareJobDraft에서 한다. | [L117](../../src/Plugin.cs#L117) |
| <a id="symbol-src-plugin-cs-118-14317c7518"></a><code>Plugin.RequestDescriptionEdit</code><br><code>void RequestDescriptionEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 소개말 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L118](../../src/Plugin.cs#L118) |
| <a id="symbol-src-plugin-cs-119-1279a42b30"></a><code>Plugin.RequestObjectiveEdit</code><br><code>void RequestObjectiveEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 모집 목적 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L119](../../src/Plugin.cs#L119) |
| <a id="symbol-src-plugin-cs-120-cfb5476820"></a><code>Plugin.CloseDescriptionDraft</code><br><code>void CloseDescriptionDraft()</code> | 메서드 | 소개말의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L120](../../src/Plugin.cs#L120) |
| <a id="symbol-src-plugin-cs-121-94e36926db"></a><code>Plugin.CloseJobDraft</code><br><code>void CloseJobDraft()</code> | 메서드 | 허용 직업의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L121](../../src/Plugin.cs#L121) |
| <a id="symbol-src-plugin-cs-122-7cf28d1ece"></a><code>Plugin.CloseObjectiveDraft</code><br><code>void CloseObjectiveDraft()</code> | 메서드 | 모집 목적의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L122](../../src/Plugin.cs#L122) |
| <a id="symbol-src-plugin-cs-123-5f86091e18"></a><code>Plugin.RequestBeginnerEdit</code><br><code>void RequestBeginnerEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 초행 및 초보자 환영 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L123](../../src/Plugin.cs#L123) |
| <a id="symbol-src-plugin-cs-128-5a76cddcf0"></a><code>Plugin.CloseBeginnerDraft</code><br><code>void CloseBeginnerDraft()</code> | 메서드 | 초행 및 초보자 환영의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L128](../../src/Plugin.cs#L128) |
| <a id="symbol-src-plugin-cs-129-076bdeb127"></a><code>Plugin.ApplyBeginnerDraft</code><br><code>bool ApplyBeginnerDraft()</code> | 메서드 | 초행 및 초보자 환영 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L129](../../src/Plugin.cs#L129) |
| <a id="symbol-src-plugin-cs-138-da4f95ddf7"></a><code>Plugin.RequestUniqueJobsEdit</code><br><code>void RequestUniqueJobsEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 잡 중복 없음 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L138](../../src/Plugin.cs#L138) |
| <a id="symbol-src-plugin-cs-143-91f14fe5ac"></a><code>Plugin.CloseUniqueJobsDraft</code><br><code>void CloseUniqueJobsDraft()</code> | 메서드 | 잡 중복 없음의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L143](../../src/Plugin.cs#L143) |
| <a id="symbol-src-plugin-cs-144-157cedb6b0"></a><code>Plugin.ApplyUniqueJobsDraft</code><br><code>bool ApplyUniqueJobsDraft()</code> | 메서드 | 잡 중복 없음 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L144](../../src/Plugin.cs#L144) |
| <a id="symbol-src-plugin-cs-153-6d5dab69a0"></a><code>Plugin.RequestPrivateEdit</code><br><code>void RequestPrivateEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 비공개/PIN 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L153](../../src/Plugin.cs#L153) |
| <a id="symbol-src-plugin-cs-158-50b0b3e2e7"></a><code>Plugin.ClosePrivateDraft</code><br><code>void ClosePrivateDraft()</code> | 메서드 | 비공개/PIN의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L158](../../src/Plugin.cs#L158) |
| <a id="symbol-src-plugin-cs-159-ea7fe39355"></a><code>Plugin.ApplyPrivateDraft</code><br><code>bool ApplyPrivateDraft()</code> | 메서드 | 비공개/PIN 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L159](../../src/Plugin.cs#L159) |
| <a id="symbol-src-plugin-cs-168-18a13875da"></a><code>Plugin.RequestCompletionEdit</code><br><code>void RequestCompletionEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 완료조건 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L168](../../src/Plugin.cs#L168) |
| <a id="symbol-src-plugin-cs-173-02f9e01d6f"></a><code>Plugin.CloseCompletionDraft</code><br><code>void CloseCompletionDraft()</code> | 메서드 | 완료조건의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L173](../../src/Plugin.cs#L173) |
| <a id="symbol-src-plugin-cs-174-db1b4f2511"></a><code>Plugin.ApplyCompletionDraft</code><br><code>bool ApplyCompletionDraft()</code> | 메서드 | 완료조건 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L174](../../src/Plugin.cs#L174) |
| <a id="symbol-src-plugin-cs-183-0c98491544"></a><code>Plugin.RequestItemLevelEdit</code><br><code>void RequestItemLevelEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 평균 아이템 레벨 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L183](../../src/Plugin.cs#L183) |
| <a id="symbol-src-plugin-cs-189-aae2d4e90e"></a><code>Plugin.CloseItemLevelDraft</code><br><code>void CloseItemLevelDraft()</code> | 메서드 | 평균 아이템 레벨의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L189](../../src/Plugin.cs#L189) |
| <a id="symbol-src-plugin-cs-190-c59df5fc95"></a><code>Plugin.ApplyItemLevelDraft</code><br><code>bool ApplyItemLevelDraft()</code> | 메서드 | 평균 아이템 레벨 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L190](../../src/Plugin.cs#L190) |
| <a id="symbol-src-plugin-cs-200-91f75f6597"></a><code>Plugin.RequestRuleEdit</code><br><code>void RequestRuleEdit(ListingRuleKind kind)</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 임무 찾기/전리품 규칙 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L200](../../src/Plugin.cs#L200) |
| <a id="symbol-src-plugin-cs-206-e185168758"></a><code>Plugin.CloseRuleDraft</code><br><code>void CloseRuleDraft()</code> | 메서드 | 임무 찾기/전리품 규칙의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L206](../../src/Plugin.cs#L206) |
| <a id="symbol-src-plugin-cs-207-61034d089e"></a><code>Plugin.ApplyRuleDraft</code><br><code>bool ApplyRuleDraft()</code> | 메서드 | 임무 찾기/전리품 규칙 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L207](../../src/Plugin.cs#L207) |
| <a id="symbol-src-plugin-cs-217-532b8a9339"></a><code>Plugin.ApplyJobDraft</code><br><code>bool ApplyJobDraft()</code> | 메서드 | 허용 직업 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L217](../../src/Plugin.cs#L217) |
| <a id="symbol-src-plugin-cs-226-dcd22dd74b"></a><code>Plugin.ApplyDescriptionDraft</code><br><code>bool ApplyDescriptionDraft()</code> | 메서드 | 소개말 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L226](../../src/Plugin.cs#L226) |
| <a id="symbol-src-plugin-cs-235-6fc1bdfc5a"></a><code>Plugin.ApplyObjectiveDraft</code><br><code>bool ApplyObjectiveDraft()</code> | 메서드 | 모집 목적 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L235](../../src/Plugin.cs#L235) |
| <a id="symbol-src-plugin-cs-245-d516d6977f"></a><code>Plugin.OnDetailUpdated</code><br><code>void OnDetailUpdated(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 상세창 PostSetup/PostRefresh를 ListingReader의 DetailRevision 증가로 연결하고 읽기를 예약한다. | [L245](../../src/Plugin.cs#L245) |
| <a id="symbol-src-plugin-cs-246-8f3f6ba01e"></a><code>Plugin.OnEditorUpdated</code><br><code>void OnEditorUpdated(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 모집 수정창 PostSetup/PostRefresh를 host의 EditorRevision 증가로 연결한다. | [L246](../../src/Plugin.cs#L246) |
| <a id="symbol-src-plugin-cs-247-323ca63998"></a><code>Plugin.OnEditorClosed</code><br><code>void OnEditorClosed(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 수정창 PreFinalize를 EditorClosedRevision 및 수동 trace 종료에 반영한다. | [L247](../../src/Plugin.cs#L247) |
| <a id="symbol-src-plugin-cs-248-f001c81e87"></a><code>Plugin.OnRoleOpened</code><br><code>void OnRoleOpened(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 직업 선택창 PostSetup을 roleOpenedRevision에 반영한다. | [L248](../../src/Plugin.cs#L248) |
| <a id="symbol-src-plugin-cs-249-cc1456068b"></a><code>Plugin.OnRoleClosed</code><br><code>void OnRoleClosed(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 직업 선택창 PreFinalize를 roleClosedRevision에 반영한다. | [L249](../../src/Plugin.cs#L249) |
| <a id="symbol-src-plugin-cs-251-624ce41778"></a><code>Plugin.PrepareJobDraft</code><br><code>void PrepareJobDraft()</code> | 메서드 | 허용 직업 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L251](../../src/Plugin.cs#L251) |
| <a id="symbol-src-plugin-cs-269-93ab5c4c2f"></a><code>Plugin.PrepareDescriptionDraft</code><br><code>void PrepareDescriptionDraft()</code> | 메서드 | 소개말 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L269](../../src/Plugin.cs#L269) |
| <a id="symbol-src-plugin-cs-285-ff6ce1af79"></a><code>Plugin.PrepareObjectiveDraft</code><br><code>void PrepareObjectiveDraft()</code> | 메서드 | 모집 목적 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L285](../../src/Plugin.cs#L285) |
| <a id="symbol-src-plugin-cs-301-12596bb922"></a><code>Plugin.PrepareBeginnerDraft</code><br><code>void PrepareBeginnerDraft()</code> | 메서드 | 초행 및 초보자 환영 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L301](../../src/Plugin.cs#L301) |
| <a id="symbol-src-plugin-cs-317-8f26b87f1a"></a><code>Plugin.PrepareUniqueJobsDraft</code><br><code>void PrepareUniqueJobsDraft()</code> | 메서드 | 잡 중복 없음 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L317](../../src/Plugin.cs#L317) |
| <a id="symbol-src-plugin-cs-333-8775cbfffc"></a><code>Plugin.PreparePrivateDraft</code><br><code>void PreparePrivateDraft()</code> | 메서드 | 비공개/PIN 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L333](../../src/Plugin.cs#L333) |
| <a id="symbol-src-plugin-cs-349-d0e0139f61"></a><code>Plugin.PrepareCompletionDraft</code><br><code>void PrepareCompletionDraft()</code> | 메서드 | 완료조건 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L349](../../src/Plugin.cs#L349) |
| <a id="symbol-src-plugin-cs-365-89e7e57ca5"></a><code>Plugin.PrepareItemLevelDraft</code><br><code>void PrepareItemLevelDraft()</code> | 메서드 | 평균 아이템 레벨 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L365](../../src/Plugin.cs#L365) |
| <a id="symbol-src-plugin-cs-382-010be7dd55"></a><code>Plugin.PrepareRuleDraft</code><br><code>void PrepareRuleDraft()</code> | 메서드 | 임무 찾기/전리품 규칙 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L382](../../src/Plugin.cs#L382) |
| <a id="symbol-src-plugin-cs-399-f29cdf7ede"></a><code>Plugin.OnUpdate</code><br><code>void OnUpdate(IFramework framework)</code> | 메서드 | 저장된 Discord 연결 복원과 trace를 먼저 처리한다. 500ms 또는 명시 요청마다 현재 파티→외부 요청 guard→모집글→초안 준비→갱신 상태 기계→외부 snapshot을 순서대로 진행한다. 읽기 예외는 마지막 결과를 stale로 남긴다. | [L399](../../src/Plugin.cs#L399) |
| <a id="symbol-src-plugin-cs-449-5f6ddefdcd"></a><code>Plugin.Dispose</code><br><code>void Dispose()</code> | 메서드 | 외부 연결·trace·갱신을 정리하고 모든 Addon/Framework/UI 이벤트, 명령과 창을 해제한다. 저장 토큰 삭제와는 별개다. | [L449](../../src/Plugin.cs#L449) |

<a id="file-src-listingdescription-cs"></a>
## src/ListingDescription.cs

[소스 파일](../../src/ListingDescription.cs) · 명명된 항목/진입점 2개

상세창 TextNode·문자열 buffer·네이티브 comment를 결합한다. 빈 소개말의 게임 대체문구를 실제 모집글로 착각하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdescription-cs-3-ce2af5df00"></a><code>ListingDescription</code><br><code>ListingDescription(bool Readable, string Text, string Source, bool NativeAgrees, bool EmptyPlaceholder)</code> | 주 생성자/모델 | 선택한 소개말 텍스트·읽기 source·native 일치·빈문구 대체 판정을 묶는다. | [L3](../../src/ListingDescription.cs#L3) |
| <a id="symbol-src-listingdescription-cs-8-5f5a38d786"></a><code>ListingDescription.Resolve</code><br><code>ListingDescription Resolve(bool nodeRead, string nodeText, bool bufferRead, string bufferText, bool nativeRead, string nativeText)</code> | 메서드 | 읽을 수 있는 UI 소스를 선택하고 native comment와 비교한다. native가 비어 있을 때만 검증된 ‘소개말 없음’ 대체문구를 빈 문자열로 해석한다. | [L8](../../src/ListingDescription.cs#L8) |

<a id="file-src-listingeditorcriteria-cs"></a>
## src/ListingEditorCriteria.cs

[소스 파일](../../src/ListingEditorCriteria.cs) · 명명된 항목/진입점 3개

조회 시 0으로 보이는 친선경기 A/B 6~8번 padding과 수정창의 PvP 기본 마스크 차이만 한정적으로 허용한다.

직접 관련 테스트: [ListingEditorCriteriaTests.cs](tests.md#file-tests-listingeditorcriteriatests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingeditorcriteria-cs-13-b7a66e6ee2"></a><code>ListingEditorCriteria.IsFriendlyPadding</code><br><code>bool IsFriendlyPadding(ListingSlots slots, int index)</code> | 메서드 | 친선경기의 물리 인덱스 5~7, 13~15인지 판정한다. 실제 등록/미등록 자리를 일괄 면제하는 함수가 아니다. | [L13](../../src/ListingEditorCriteria.cs#L13) |
| <a id="symbol-src-listingeditorcriteria-cs-17-1f43f9aadc"></a><code>ListingEditorCriteria.Matches</code><br><code>bool Matches(RefreshTarget target, string actualCriteria, int draftGroups, int draftMainSlots, out int[] acceptedPaddingIndices)</code> | 메서드 | 9개 criteria 필드와 슬롯을 비교한다. 검증된 친선 형태에서 padding의 expected=0→actual=0xFDFFFFFE만 허용하고 수락한 인덱스를 반환한다. | [L17](../../src/ListingEditorCriteria.cs#L17) |
| <a id="symbol-src-listingeditorcriteria-cs-45-b2aac9a6d2"></a><code>ListingEditorCriteria.TryReadFlags</code><br><code>bool TryReadFlags(string criteria, int count, out ulong[] flags)</code> | 메서드 | criteria의 마지막 hex 필드를 정해진 슬롯 수의 little-endian ulong 배열로 엄격히 복원한다. | [L45](../../src/ListingEditorCriteria.cs#L45) |

<a id="file-src-listingidentity-cs"></a>
## src/ListingIdentity.cs

[소스 파일](../../src/ListingIdentity.cs) · 명명된 항목/진입점 3개

모집 작성자 분류와 문자열 정규화. CID·이름·소개말 증거가 맞지 않으면 Self로 추정하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingidentity-cs-7-037bedb82f"></a><code>ListingIdentity.Resolve</code><br><code>ListingOwner Resolve(ulong leaderId, string leaderName, bool displayAgrees, ulong selfId, string selfName, IReadOnlyList&lt;(ulong Id, string Name, bool IsLeader)&gt; party)</code> | 메서드 | 검증된 리더 CID/이름을 본인 및 현재 파티의 리더와 비교해 Self/PartyLeader/Other/Unknown으로 분류한다. | [L7](../../src/ListingIdentity.cs#L7) |
| <a id="symbol-src-listingidentity-cs-19-ee1aa5dab9"></a><code>ListingIdentity.SameText</code><br><code>bool SameText(string a, string b)</code> | 메서드 | 두 문자열을 동일한 정규화 규칙으로 비교한다. | [L19](../../src/ListingIdentity.cs#L19) |
| <a id="symbol-src-listingidentity-cs-20-ded254bb05"></a><code>ListingIdentity.Normalize</code><br><code>string Normalize(string text)</code> | 메서드 | 줄바꿈/공백 표현을 정규화해 UI와 네이티브 텍스트 비교에 사용한다. | [L20](../../src/ListingIdentity.cs#L20) |

<a id="file-src-listingjobrules-cs"></a>
## src/ListingJobRules.cs

[소스 파일](../../src/ListingJobRules.cs) · 명명된 항목/진입점 7개

ClassJob row ID와 모집 비트 위치의 명시적 대응, 사용자 지정 직업 순서, 허용 직업 설명을 제공한다.

직접 관련 테스트: [ListingJobRulesTests.cs](tests.md#file-tests-listingjobrulestests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingjobrules-cs-3-f52e48e2aa"></a><code>RecruitmentJob</code><br><code>RecruitmentJob(uint Id, string Name)</code> | 주 생성자/모델 | 런타임 전투 직업 row ID와 한국어 이름을 보관한다. | [L3](../../src/ListingJobRules.cs#L3) |
| <a id="symbol-src-listingjobrules-cs-4-2d36b0b6a0"></a><code>RecruitmentJobOption</code><br><code>RecruitmentJobOption(uint Id, string Name, ulong Flag)</code> | 주 생성자/모델 | 한 직업/클래스의 row ID·표시명·모집 bit를 묶는다. | [L4](../../src/ListingJobRules.cs#L4) |
| <a id="symbol-src-listingjobrules-cs-6-7e14b1110f"></a><code>ListingAllowedJobs</code><br><code>ListingAllowedJobs(bool Anyone, string[] Names, bool Complete)</code> | 주 생성자/모델 | 슬롯 mask에서 해석한 허용 직업 목록과 Anyone/완전성 등의 표시 근거를 묶는다. | [L6](../../src/ListingJobRules.cs#L6) |
| <a id="symbol-src-listingjobrules-cs-30-c4aea758b0"></a><code>ListingJobRules.Count</code><br><code>int Count</code> | 계산 속성 | 현재 registry/카탈로그/버퍼의 항목 수를 반환한다. 계산식: <code>jobs.Length</code>. | [L30](../../src/ListingJobRules.cs#L30) |
| <a id="symbol-src-listingjobrules-cs-33-f0e82392fd"></a><code>ListingJobRules.ListingJobRules</code><br><code>ListingJobRules(RecruitmentJobOption[] jobs, bool complete)</code> | 생성자 | 직업 선택지·전체 known mask·카탈로그 완전성 정보를 보관한다. | [L33](../../src/ListingJobRules.cs#L33) |
| <a id="symbol-src-listingjobrules-cs-41-e0402cc2e2"></a><code>ListingJobRules.Read</code><br><code>ListingJobRules Read(IEnumerable&lt;RecruitmentJob&gt; battleJobs)</code> | 메서드 | 런타임 전투 직업 카탈로그를 정의된 JobFlags bit 및 표시 순서와 연결한다. 알 수 없는 ID·빈 이름·중복 ID 또는 유효 항목이 없으면 Complete=false다. 한섭 런타임에 없는 잡까지 고정 32개로 요구하지 않는다. | [L41](../../src/ListingJobRules.cs#L41) |
| <a id="symbol-src-listingjobrules-cs-59-595ff2a452"></a><code>ListingJobRules.Describe</code><br><code>ListingAllowedJobs Describe(ulong flags)</code> | 메서드 | 슬롯 bitmask를 허용 직업 목록으로 풀어낸다. 완전한 카탈로그의 모든 직업이 허용될 때만 ‘누구나 가능’으로 표시한다. | [L59](../../src/ListingJobRules.cs#L59) |

<a id="file-src-listingreader-cs"></a>
## src/ListingReader.cs

[소스 파일](../../src/ListingReader.cs) · 명명된 항목/진입점 11개

본인 모집글 조회 예약과 상세창 읽기. 상세창 수명 revision, UI 문자열, LastViewedListing의 소유자·직업·조건을 교차 확인한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingreader-cs-22-6e2a53de31"></a><code>ListingReader.Busy</code><br><code>bool Busy</code> | 계산 속성 | 실행 중인 조회/편집/추적 단계가 남아 있는지 계산한다. 계산식: <code>userRequested &#124;&#124; openingMain &#124;&#124; waitingRevision.HasValue</code>. | [L22](../../src/ListingReader.cs#L22) |
| <a id="symbol-src-listingreader-cs-24-959442b55f"></a><code>ListingReader.DetailRevision</code><br><code>long DetailRevision</code> | 계산 속성 | 상세창 PostSetup/PostRefresh revision을 읽는다. 계산식: <code>Interlocked.Read(ref detailRevision)</code>. | [L24](../../src/ListingReader.cs#L24) |
| <a id="symbol-src-listingreader-cs-25-528b98b8c0"></a><code>ListingReader.RequestRevision</code><br><code>long RequestRevision</code> | 계산 속성 | 조회 요청 serial을 읽어 요청 소유권과 신규 요청 여부를 구분한다. 계산식: <code>Interlocked.Read(ref requestRevision)</code>. | [L25](../../src/ListingReader.cs#L25) |
| <a id="symbol-src-listingreader-cs-26-c325385611"></a><code>ListingReader.RequestOwn</code><br><code>void RequestOwn()</code> | 메서드 | 조회 플래그·요청 serial을 갱신한다. 이 함수 자체가 네이티브 조회를 수행하지는 않는다. | [L26](../../src/ListingReader.cs#L26) |
| <a id="symbol-src-listingreader-cs-27-a9ebf18572"></a><code>ListingReader.DetailUpdated</code><br><code>void DetailUpdated()</code> | 메서드 | 상세창 수명/갱신 이벤트를 원자적 revision 증가로 기록한다. | [L27](../../src/ListingReader.cs#L27) |
| <a id="symbol-src-listingreader-cs-29-0c1612b196"></a><code>ListingReader.Read</code><br><code>ListingSnapshot Read(PartySnapshot party, ListingSnapshot previous)</code> | 메서드 | 호환성·접속·이동·창 준비를 확인하고 HandleRequest 후 UI와 네이티브 상세정보를 결합한다. 소유자·소개말·자리 규칙을 검증해 ListingSnapshot을 만들고 닫힌 창은 마지막 결과를 IsLive=false로 유지한다. | [L29](../../src/ListingReader.cs#L29) |
| <a id="symbol-src-listingreader-cs-166-f869fbab4d"></a><code>ListingReader.HandleRequest</code><br><code>void HandleRequest()</code> | 메서드 | 예약된 본인 CID 조회를 게임 스레드에서 1회 발송한다. 필요하면 파티찾기를 연 뒤 새 상세 revision을 기다리고 timeout/실패를 처리한다. | [L166](../../src/ListingReader.cs#L166) |
| <a id="symbol-src-listingreader-cs-199-45a2e7cdd7"></a><code>ListingReader.FinishFailed</code><br><code>void FinishFailed(string message, string reason)</code> | 메서드 | 조회 진행 플래그를 종료하고 실패 상태와 원인 코드를 기록한다. | [L199](../../src/ListingReader.cs#L199) |
| <a id="symbol-src-listingreader-cs-208-71a6deb875"></a><code>ListingReader.FailRequest</code><br><code>void FailRequest()</code> | 메서드 | 사용자 취소나 소유한 외부 요청 중단에 따라 대기 조회를 실패 종료한다. | [L208](../../src/ListingReader.cs#L208) |
| <a id="symbol-src-listingreader-cs-210-1bcd9beb9a"></a><code>ListingReader.RecordFailure</code><br><code>void RecordFailure(string message)</code> | 메서드 | 읽기 예외를 조회 실패 상태로 기록한다. | [L210](../../src/ListingReader.cs#L210) |
| <a id="symbol-src-listingreader-cs-217-51cbbef118"></a><code>ListingReader.Reset</code><br><code>void Reset()</code> | 메서드 | 로그아웃/캐릭터 변경 등에 맞춰 조회 플래그와 사용자 상태를 초기화한다. | [L217](../../src/ListingReader.cs#L217) |

<a id="file-src-listingrolecomparison-cs"></a>
## src/ListingRoleComparison.cs

[소스 파일](../../src/ListingRoleComparison.cs) · 명명된 항목/진입점 1개

직업 mask 불일치의 상세 진단 생성. 비교 허용 여부와 별개로 raw 차이를 남긴다.

직접 관련 테스트: [ListingRoleComparisonTests.cs](tests.md#file-tests-listingrolecomparisontests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingrolecomparison-cs-9-f857e109dc"></a><code>ListingRoleComparison.Describe</code><br><code>string Describe(RefreshTarget target, string draftCriteria, int draftGroups, int draftMainSlots, int requestedPosition)</code> | 메서드 | 원본/수정창 슬롯 플래그, 요청한 표시/물리 위치, 그룹·멤버, xor 및 추가/제거 직업 ID를 기록한다. 친선 padding 여부도 함께 분류한다. | [L9](../../src/ListingRoleComparison.cs#L9) |

<a id="file-src-listingslotroute-cs"></a>
## src/ListingSlotRoute.cs

[소스 파일](../../src/ListingSlotRoute.cs) · 명명된 항목/진입점 7개

1~8인·24인·48인·친선경기의 자리 클릭 경로와 수정창 형태를 판정한다.

직접 관련 테스트: [ListingSlotRouteTests.cs](tests.md#file-tests-listingslotroutetests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingslotroute-cs-8-83782cef6e"></a><code>ListingSlotRoute</code><br><code>ListingSlotRoute(RecruitmentSlotLayout Layout, int TotalSlots, int GroupIndex, int MemberIndex, uint OpenNodeId, int? OpenParam, uint MemberNodeId = 0, int MemberParam = 0)</code> | 주 생성자/모델 | 하나의 자리 layout·그룹/멤버·열기 node/param 및48인 멤버 선택 node/param을 고정한다. | [L8](../../src/ListingSlotRoute.cs#L8) |
| <a id="symbol-src-listingslotroute-cs-12-7d1bef2b24"></a><code>ListingSlotRoute.SelectMember</code><br><code>bool SelectMember</code> | 계산 속성 | 48인처럼 그룹을 연 다음 별도 멤버 선택이 필요한 경로인지 반환한다. 계산식: <code>Layout == RecruitmentSlotLayout.Alliance48</code>. | [L12](../../src/ListingSlotRoute.cs#L12) |
| <a id="symbol-src-listingslotroute-cs-13-799240fad7"></a><code>ListingSlotRoute.NativeIndex</code><br><code>int NativeIndex</code> | 계산 속성 | 표시 번호와 구분되는 0-based 물리 슬롯 인덱스를 반환한다. 계산식: <code>GroupIndex * 8 + MemberIndex</code>. | [L13](../../src/ListingSlotRoute.cs#L13) |
| <a id="symbol-src-listingslotroute-cs-17-d40476545a"></a><code>ListingSlotRoute.SupportsSettings</code><br><code>bool SupportsSettings(ListingSlots slots)</code> | 메서드 | 검증된 슬롯 매핑과 지원 layout인지 확인해 공통 설정 편집 가능 여부를 반환한다. | [L17](../../src/ListingSlotRoute.cs#L17) |
| <a id="symbol-src-listingslotroute-cs-19-4353297cf1"></a><code>ListingSlotRoute.Classify</code><br><code>RecruitmentSlotLayout Classify(ListingSlots slots)</code> | 메서드 | 친선 flag를 먼저 보고 SingleParty/Alliance24/Alliance48/FriendlyMatch/Unknown으로 구분한다. 인원수만 보고 친선경기를 24인으로 취급하지 않는다. | [L19](../../src/ListingSlotRoute.cs#L19) |
| <a id="symbol-src-listingslotroute-cs-33-d8fc7cb644"></a><code>ListingSlotRoute.TryCreate</code><br><code>bool TryCreate(ListingSlots slots, int position, out ListingSlotRoute route)</code> | 메서드 | 표시 position을 해당 Slot.NativeIndex에 매핑한 뒤 그룹/멤버 버튼 node·param을 산출한다. 48인은 그룹 클릭 후 멤버 선택이 추가된다. | [L33](../../src/ListingSlotRoute.cs#L33) |
| <a id="symbol-src-listingslotroute-cs-64-8a43869ad9"></a><code>ListingSlotRoute.MatchesEditorShape</code><br><code>bool MatchesEditorShape(int groups, int mainPartySlots)</code> | 메서드 | snapshot layout과 수정창 NumberOfGroups/NumberOfSlotsInMainParty가 맞는지 검사한다. 등록 인원 축소와 친선 5명 구성을 고려한다. | [L64](../../src/ListingSlotRoute.cs#L64) |

<a id="file-src-listingslots-cs"></a>
## src/ListingSlots.cs

[소스 파일](../../src/ListingSlots.cs) · 명명된 항목/진입점 11개

등록된 자리만 표시하는 압축 목록과 8/24/48개 물리 슬롯을 함께 관리한다. 표시 Position과 NativeIndex를 절대 혼용하지 않는다.

직접 관련 테스트: [ListingSlotsTests.cs](tests.md#file-tests-listingslotstests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingslots-cs-5-656e84356d"></a><code>ListingSlot</code><br><code>ListingSlot(int Position, ulong MemberContentId, byte JobId, string Job, ulong SlotFlags)</code> | 주 생성자/모델 | 하나의 등록 자리 display position·NativeIndex·참가 CID/JobId·raw mask·허용 직업을 묶는다. | [L5](../../src/ListingSlots.cs#L5) |
| <a id="symbol-src-listingslots-cs-10-ce5b2226b5"></a><code>ListingSlot.HasMember</code><br><code>bool HasMember</code> | 계산 속성 | 모집 slot의 CID 또는 등록 JobId가 존재하는지 판정한다. 계산식: <code>MemberContentId != 0 &#124;&#124; JobId != 0</code>. | [L10](../../src/ListingSlots.cs#L10) |
| <a id="symbol-src-listingslots-cs-14-67b78e3a21"></a><code>ListingSlots</code><br><code>ListingSlots(bool Readable, int TotalSlots, int SlotsFilled, int NumberOfParties, bool IsAlliance, ListingSlot[] Slots, string Diagnostic)</code> | 주 생성자/모델 | 전체 등록 자리와 물리 크기·그룹·인원수·카탈로그·미등록 인덱스 mapping을 보관한다. | [L14](../../src/ListingSlots.cs#L14) |
| <a id="symbol-src-listingslots-cs-18-4d06ca4f9e"></a><code>ListingSlots.ObservedMembers</code><br><code>int ObservedMembers</code> | 계산 속성 | 읽어 온 등록 슬롯에서 참가 중인 수를 계산한다. 계산식: <code>Slots.Count(s =&gt; s.HasMember)</code>. | [L18](../../src/ListingSlots.cs#L18) |
| <a id="symbol-src-listingslots-cs-19-c64b76c942"></a><code>ListingSlots.CountsAgree</code><br><code>bool CountsAgree</code> | 계산 속성 | 서버 보고 인원수와 실제 등록/점유 슬롯 수가 일치하는지 검사한다. 계산식: <code>Readable &amp;&amp; Slots.Length == TotalSlots &amp;&amp; ObservedMembers == SlotsFilled</code>. | [L19](../../src/ListingSlots.cs#L19) |
| <a id="symbol-src-listingslots-cs-26-c435f7103b"></a><code>ListingSlots.IsFriendlyMatch</code><br><code>bool IsFriendlyMatch</code> | 계산 속성 | snapshot의 친선경기 join flag를 검사한다. 계산식: <code>NumberOfParties == 3 &amp;&amp; NativeSlotCount == 24 &amp;&amp;         (JoinConditionFlags &amp; 0x10) != 0 &amp;&amp; Slots.All(s =&gt; s.NativeIndex &gt;= 16 &#124;&#124; s.NativeIndex % 8 &lt; 5)</code>. | [L26](../../src/ListingSlots.cs#L26) |
| <a id="symbol-src-listingslots-cs-28-ce22360922"></a><code>ListingSlots.MappingValid</code><br><code>bool MappingValid</code> | 계산 속성 | 물리 크기·등록 수·인덱스 유일성·미등록 매핑이 일관되는지 검사한다. | [L28](../../src/ListingSlots.cs#L28) |
| <a id="symbol-src-listingslots-cs-49-dde702a1e1"></a><code>ListingSlots.PositionLabel</code><br><code>string PositionLabel(ListingSlot slot)</code> | 메서드 | 물리 그룹·멤버 위치를 기준으로 일반/연합/친선경기 자리 표시명을 만든다. | [L49](../../src/ListingSlots.cs#L49) |
| <a id="symbol-src-listingslots-cs-54-73c7a8136f"></a><code>ListingSlots.MatchesMembers</code><br><code>bool MatchesMembers(ReadOnlySpan&lt;ulong&gt; members)</code> | 메서드 | 네이티브 참가자 CID 배열을 모집 snapshot의 물리 인덱스별 참가자와 비교한다. 빈칸·미등록도 구분한다. | [L54](../../src/ListingSlots.cs#L54) |
| <a id="symbol-src-listingslots-cs-64-8d6a83f94d"></a><code>ListingSlots.Unavailable</code><br><code>ListingSlots Unavailable(string reason)</code> | 메서드 | 읽기 불가 상태와 이유를 담은 빈 슬롯 모델을 만든다. | [L64](../../src/ListingSlots.cs#L64) |
| <a id="symbol-src-listingslots-cs-67-c67f36385c"></a><code>ListingSlots.Read</code><br><code>ListingSlots Read(bool detailAgrees, int totalSlots, int slotsFilled, int numberOfParties, bool isAlliance, ReadOnlySpan&lt;byte&gt; jobs, ReadOnlySpan&lt;ulong&gt; memberContentIds, ReadOnlySpan&lt;ulong&gt; slotFlags, Func&lt;uint, string&gt; jobName, ListingJobRules? jobRules = null, byte joinConditionFlags = 0)</code> | 메서드 | 총 등록 인원·물리 배열 길이·그룹·join flags를 검증하고 SlotFlags=0인 미등록 자리를 제외한다. 참가 여부·등록 직업·허용 직업을 물리 인덱스를 보존한 채 매핑한다. | [L67](../../src/ListingSlots.cs#L67) |

<a id="file-src-listingsnapshot-cs"></a>
## src/ListingSnapshot.cs

[소스 파일](../../src/ListingSnapshot.cs) · 명명된 항목/진입점 2개

마지막 모집글의 관리형 표시 모델. 본인 CID, 소유자, Live/ReadAt, 조건·직업 슬롯·진단을 보관한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingsnapshot-cs-3-710c281a04"></a><code>ListingSnapshot</code><br><code>ListingSnapshot(string Status, ListingOwner Owner, string Leader, string Duty, string Description, string Remaining, DateTime? ReadAt, bool IsLive, string Note, string Diagnostic)</code> | 주 생성자/모델 | 조회한 모집글의 UI/서버 데이터와 소유자·Live·ReadAt·슬롯·진단을 보관하는 불변 snapshot이다. | [L3](../../src/ListingSnapshot.cs#L3) |
| <a id="symbol-src-listingsnapshot-cs-14-7dccd0daf1"></a><code>ListingSnapshot.RuleValue</code><br><code>int? RuleValue(ListingRuleKind kind)</code> | 메서드 | 요청한 ListingRuleKind에 해당하는 snapshot의 제한 해제/최저 레벨/초월 무효/전리품 값을 반환한다. | [L14](../../src/ListingSnapshot.cs#L14) |

<a id="file-src-partyreader-cs"></a>
## src/PartyReader.cs

[소스 파일](../../src/PartyReader.cs) · 명명된 항목/진입점 6개

현재 파티의 관찰용 스냅샷. 일반 파티 목록, 서버 초월의 로컬 그룹, 본인 정보 순으로 읽는다. 모집 등록 직업/빈자리 규칙의 기준으로 사용하면 안 된다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-partyreader-cs-7-61caeb3722"></a><code>MemberInfo</code><br><code>MemberInfo(ulong ContentId, string Name, uint WorldId, string World, uint JobId, string Job, int Level, bool IsSelf, bool IsLeader)</code> | 주 생성자/모델 | 현재 파티 멤버의 CID·이름·월드·현재 직업·리더/본인 표시를 보관한다. 모집 등록 슬롯과 구분한다. | [L7](../../src/PartyReader.cs#L7) |
| <a id="symbol-src-partyreader-cs-10-360bbabcff"></a><code>PartySnapshot</code><br><code>PartySnapshot(string Status, string Source, MemberInfo[] Members, DateTime? ReadAt, bool IsStale, string Error, string Diagnostic)</code> | 주 생성자/모델 | 현재 파티 읽기 결과·source·stale·시각·진단을 묶는다. | [L10](../../src/PartyReader.cs#L10) |
| <a id="symbol-src-partyreader-cs-22-2fef0129c9"></a><code>PartyReader.Read</code><br><code>PartySnapshot Read(PartySnapshot previous)</code> | 메서드 | 로그인·이동 상태와 일반/서버 초월 파티 가용성을 검사해 PartySnapshot을 만든다. 이동 중에는 이전 결과를 stale로 유지한다. | [L22](../../src/PartyReader.cs#L22) |
| <a id="symbol-src-partyreader-cs-62-368238c330"></a><code>PartyReader.Create</code><br><code>MemberInfo Create(ulong cid, string name, uint world, uint job, int level, bool leader)</code> | 메서드 | 자신이 포함된 멤버 목록을 확인하고 source·상태·시각·진단이 포함된 PartySnapshot을 생성한다. | [L62](../../src/PartyReader.cs#L62) |
| <a id="symbol-src-partyreader-cs-69-01a76dbaab"></a><code>PartyReader.ReadRegular</code><br><code>void ReadRegular(List&lt;MemberInfo&gt; result)</code> | 메서드 | Dalamud IPartyList의 최대 8명을 읽고 이름·월드·직업·리더 정보를 변환한다. | [L69](../../src/PartyReader.cs#L69) |
| <a id="symbol-src-partyreader-cs-81-4b8a900f39"></a><code>PartyReader.ReadCrossRealm</code><br><code>bool ReadCrossRealm(List&lt;MemberInfo&gt; result, out string diagnostic)</code> | 메서드 | 호환 검사를 통과한 InfoProxyCrossRealm의 LocalPlayerGroupIndex 그룹을 읽는다. 멤버 수·자기 포함·중복을 검사하며 다른 연합 그룹 전체를 순회하지 않는다. | [L81](../../src/PartyReader.cs#L81) |

<a id="file-src-partywindow-cs"></a>
## src/PartyWindow.cs

[소스 파일](../../src/PartyWindow.cs) · 명명된 항목/진입점 28개

인게임 ImGui 화면. 현재 파티/모집글/외부 연결/수정 초안/대기 변경/진단을 표시하고 Plugin에 의도만 전달한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-partywindow-cs-23-68a6556cbf"></a><code>PartyWindow.PartyWindow</code><br><code>PartyWindow(Plugin plugin)</code> | 생성자 | Plugin 참조와 창 크기·제약 등 ImGui window 설정을 초기화한다. | [L23](../../src/PartyWindow.cs#L23) |
| <a id="symbol-src-partywindow-cs-31-84955a83d8"></a><code>PartyWindow.Draw</code><br><code>void Draw()</code> | 메서드 | 전체 창의 조회·갱신·대기 변경 버튼, 파티/모집글/연결 패널과 편집창을 그린다. | [L31](../../src/PartyWindow.cs#L31) |
| <a id="symbol-src-partywindow-cs-56-eef152632d"></a><code>PartyWindow.DrawBrowser</code><br><code>void DrawBrowser()</code> | 메서드 | 같은 PC 웹 연결의 켜기/끄기, 주소·인증 링크와 복사 UI를 표시한다. | [L56](../../src/PartyWindow.cs#L56) |
| <a id="symbol-src-partywindow-cs-71-a6ebbd559d"></a><code>PartyWindow.DrawRelay</code><br><code>void DrawRelay()</code> | 메서드 | 중계 서버 주소·연결 키·외부 링크와 연결 상태를 표시하고 relay 시작/종료를 요청한다. | [L71](../../src/PartyWindow.cs#L71) |
| <a id="symbol-src-partywindow-cs-104-52eff3d4e2"></a><code>PartyWindow.DrawDiscord</code><br><code>void DrawDiscord()</code> | 메서드 | 봇 토큰 입력, 기억/저장 토큰 사용, 초대 URL·이모지 동기화 상태와 연결 해제/저장 삭제 UI를 표시한다. | [L104](../../src/PartyWindow.cs#L104) |
| <a id="symbol-src-partywindow-cs-128-2c881abfba"></a><code>PartyWindow.DrawCurrentParty</code><br><code>void DrawCurrentParty()</code> | 메서드 | PartySnapshot의 현재 멤버·직업·월드·stale 상태를 표시한다. 모집 등록 직업 화면과 별개다. | [L128](../../src/PartyWindow.cs#L128) |
| <a id="symbol-src-partywindow-cs-159-3b35eb3d63"></a><code>PartyWindow.DrawListing</code><br><code>void DrawListing()</code> | 메서드 | 상세 모집의 소유자·임무·시간·조건·소개말·등록 자리를 표시하고 개별 편집 진입 UI를 연결한다. | [L159](../../src/PartyWindow.cs#L159) |
| <a id="symbol-src-partywindow-cs-238-8eb4182779"></a><code>PartyWindow.DrawDescription</code><br><code>void DrawDescription(ListingSnapshot listing)</code> | 메서드 | 조회된 소개말의 표시값과 편집 진입 버튼을 그린다. | [L238](../../src/PartyWindow.cs#L238) |
| <a id="symbol-src-partywindow-cs-260-f9589bba79"></a><code>PartyWindow.DrawDescriptionEditor</code><br><code>void DrawDescriptionEditor()</code> | 메서드 | 소개말 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyDescriptionDraft에 대기 변경을 전달한다. | [L260](../../src/PartyWindow.cs#L260) |
| <a id="symbol-src-partywindow-cs-301-9ae998dfee"></a><code>PartyWindow.DrawListingSlots</code><br><code>void DrawListingSlots(ListingSnapshot listing)</code> | 메서드 | 등록된 자리만 순회하고 인원수·그룹/멤버 위치·미등록 매핑 진단을 표시한다. | [L301](../../src/PartyWindow.cs#L301) |
| <a id="symbol-src-partywindow-cs-344-c7e99327a8"></a><code>PartyWindow.DrawObjective</code><br><code>void DrawObjective(ListingSnapshot listing)</code> | 메서드 | 조회된 모집 목적의 표시값과 편집 진입 버튼을 그린다. | [L344](../../src/PartyWindow.cs#L344) |
| <a id="symbol-src-partywindow-cs-356-973ac42db3"></a><code>PartyWindow.DrawBeginner</code><br><code>void DrawBeginner(ListingSnapshot listing)</code> | 메서드 | 조회된 초행 및 초보자 환영의 표시값과 편집 진입 버튼을 그린다. | [L356](../../src/PartyWindow.cs#L356) |
| <a id="symbol-src-partywindow-cs-368-4ce4667623"></a><code>PartyWindow.DrawBeginnerEditor</code><br><code>void DrawBeginnerEditor()</code> | 메서드 | 초행 및 초보자 환영 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyBeginnerDraft에 대기 변경을 전달한다. | [L368](../../src/PartyWindow.cs#L368) |
| <a id="symbol-src-partywindow-cs-401-3a462851d3"></a><code>PartyWindow.DrawUniqueJobs</code><br><code>void DrawUniqueJobs(ListingSnapshot listing)</code> | 메서드 | 조회된 잡 중복 없음의 표시값과 편집 진입 버튼을 그린다. | [L401](../../src/PartyWindow.cs#L401) |
| <a id="symbol-src-partywindow-cs-413-2b862a63ee"></a><code>PartyWindow.DrawUniqueJobsEditor</code><br><code>void DrawUniqueJobsEditor()</code> | 메서드 | 잡 중복 없음 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyUniqueJobsDraft에 대기 변경을 전달한다. | [L413](../../src/PartyWindow.cs#L413) |
| <a id="symbol-src-partywindow-cs-446-0de56108b8"></a><code>PartyWindow.DrawPrivate</code><br><code>void DrawPrivate(ListingSnapshot listing)</code> | 메서드 | 조회된 비공개/PIN의 표시값과 편집 진입 버튼을 그린다. | [L446](../../src/PartyWindow.cs#L446) |
| <a id="symbol-src-partywindow-cs-459-082f47ca4e"></a><code>PartyWindow.DrawPrivateEditor</code><br><code>void DrawPrivateEditor()</code> | 메서드 | 비공개/PIN 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyPrivateDraft에 대기 변경을 전달한다. | [L459](../../src/PartyWindow.cs#L459) |
| <a id="symbol-src-partywindow-cs-503-fbe09c64fa"></a><code>PartyWindow.DrawCompletion</code><br><code>void DrawCompletion(ListingSnapshot listing)</code> | 메서드 | 조회된 완료조건의 표시값과 편집 진입 버튼을 그린다. | [L503](../../src/PartyWindow.cs#L503) |
| <a id="symbol-src-partywindow-cs-516-3cfc74b13f"></a><code>PartyWindow.DrawCompletionEditor</code><br><code>void DrawCompletionEditor()</code> | 메서드 | 완료조건 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyCompletionDraft에 대기 변경을 전달한다. | [L516](../../src/PartyWindow.cs#L516) |
| <a id="symbol-src-partywindow-cs-563-0af79c73fc"></a><code>PartyWindow.DrawItemLevel</code><br><code>void DrawItemLevel(ListingSnapshot listing)</code> | 메서드 | 조회된 평균 아이템 레벨의 표시값과 편집 진입 버튼을 그린다. | [L563](../../src/PartyWindow.cs#L563) |
| <a id="symbol-src-partywindow-cs-576-7787f40e18"></a><code>PartyWindow.DrawItemLevelEditor</code><br><code>void DrawItemLevelEditor()</code> | 메서드 | 평균 아이템 레벨 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyItemLevelDraft에 대기 변경을 전달한다. | [L576](../../src/PartyWindow.cs#L576) |
| <a id="symbol-src-partywindow-cs-618-9b231782b2"></a><code>PartyWindow.DrawRule</code><br><code>void DrawRule(ListingSnapshot listing, ListingRuleKind kind)</code> | 메서드 | 조회된 임무 찾기/전리품 규칙의 표시값과 편집 진입 버튼을 그린다. | [L618](../../src/PartyWindow.cs#L618) |
| <a id="symbol-src-partywindow-cs-632-c6cb350056"></a><code>PartyWindow.DrawRuleEditor</code><br><code>void DrawRuleEditor()</code> | 메서드 | 임무 찾기/전리품 규칙 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyRuleDraft에 대기 변경을 전달한다. | [L632](../../src/PartyWindow.cs#L632) |
| <a id="symbol-src-partywindow-cs-676-b6a6c15be1"></a><code>PartyWindow.DrawObjectiveTrace</code><br><code>void DrawObjectiveTrace()</code> | 메서드 | 수동 이벤트 기록의 시작/종료 및 진단 복사 UI를 그린다. | [L676](../../src/PartyWindow.cs#L676) |
| <a id="symbol-src-partywindow-cs-695-799c52074b"></a><code>PartyWindow.DrawObjectiveEditor</code><br><code>void DrawObjectiveEditor()</code> | 메서드 | 모집 목적 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyObjectiveDraft에 대기 변경을 전달한다. | [L695](../../src/PartyWindow.cs#L695) |
| <a id="symbol-src-partywindow-cs-737-d3ab4b380a"></a><code>PartyWindow.DrawListingSlotJob</code><br><code>void DrawListingSlotJob(ListingSlot slot, ListingSnapshot listing)</code> | 메서드 | 입장 직업/직업 제한 라벨과 마우스오버 허용 직업 목록을 표시한다. | [L737](../../src/PartyWindow.cs#L737) |
| <a id="symbol-src-partywindow-cs-775-a83eb614b5"></a><code>PartyWindow.DrawJobEditor</code><br><code>void DrawJobEditor()</code> | 메서드 | 직업 초안 선택 UI와 적용 가능 여부를 표시하고 대기 변경으로 추가한다. | [L775](../../src/PartyWindow.cs#L775) |
| <a id="symbol-src-partywindow-cs-824-1f4d1dbfdf"></a><code>PartyWindow.Cell</code><br><code>void Cell(string text)</code> | 메서드 | 표 셀을 이동하고 지정 문자열을 출력하는 공통 그리기 helper다. | [L824](../../src/PartyWindow.cs#L824) |

<a id="file-src-runtimerows-cs"></a>
## src/RuntimeRows.cs

[소스 파일](../../src/RuntimeRows.cs) · 명명된 항목/진입점 7개

한섭/공개판 Lumina 어셈블리 차이를 피하도록 런타임 RowRef와 ExcelSheet 공개 API를 reflection으로 읽는다. 이름 캐시와 모집 직업 카탈로그를 제공한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-runtimerows-cs-24-06b893356e"></a><code>RuntimeRows.BeginRead</code><br><code>void BeginRead()</code> | 메서드 | 이번 읽기의 row lookup 오류 문자열을 초기화한다. | [L24](../../src/RuntimeRows.cs#L24) |
| <a id="symbol-src-runtimerows-cs-26-220acbf20c"></a><code>RuntimeRows.RowId</code><br><code>uint RowId(PropertyInfo property, object owner)</code> | 메서드 | 주어진 RowRef 속성에서 실제 런타임 타입의 RowId를 읽어 uint로 변환한다. | [L26](../../src/RuntimeRows.cs#L26) |
| <a id="symbol-src-runtimerows-cs-32-346716db82"></a><code>RuntimeRows.World</code><br><code>string World(uint id)</code> | 메서드 | 월드 이름을 캐시/ExcelSheet에서 찾는다. | [L32](../../src/RuntimeRows.cs#L32) |
| <a id="symbol-src-runtimerows-cs-33-5369686092"></a><code>RuntimeRows.Job</code><br><code>string Job(uint id)</code> | 메서드 | 직업 ID 0은 확인 중으로 표시하고 나머지는 직업 이름 캐시/시트에서 찾는다. | [L33](../../src/RuntimeRows.cs#L33) |
| <a id="symbol-src-runtimerows-cs-35-73a208ddd0"></a><code>RuntimeRows.RecruitmentJobs</code><br><code>ListingJobRules RecruitmentJobs()</code> | 메서드 | ClassJob Role이 0이 아닌 전체 전투 직업·클래스를 수집한다. 개인 해금 상태로 제한하지 않으며 완전한 카탈로그만 캐시한다. | [L35](../../src/RuntimeRows.cs#L35) |
| <a id="symbol-src-runtimerows-cs-65-3ee74f87c1"></a><code>RuntimeRows.GetSheet</code><br><code>object? GetSheet(PropertyInfo property)</code> | 메서드 | RowRef의 실제 generic 타입과 IDataManager.GetExcelSheet를 찾아 런타임 시트를 얻는다. | [L65](../../src/RuntimeRows.cs#L65) |
| <a id="symbol-src-runtimerows-cs-73-cf39965911"></a><code>RuntimeRows.Lookup</code><br><code>string Lookup(uint id, PropertyInfo property, Dictionary&lt;uint, string&gt; cache, ref object? sheet, string label)</code> | 메서드 | 캐시 미스 시 GetRow/Name을 reflection으로 읽는다. 실패하면 Error를 기록하고 ‘서버/직업 #ID’로 대체한다. | [L73](../../src/RuntimeRows.cs#L73) |
