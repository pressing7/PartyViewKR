# 플러그인 수명·조회·자리 매핑

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

게임 상태 읽기는 `Plugin.OnUpdate`에서 한다. `PartySnapshot`은 현재 파티, `ListingSnapshot`/`RefreshTarget`은 모집글이다. 직업 수정과 표시의 기준은 후자다.

기준: 0.3.49.0 소스. 기반 main `a34099802f12d6e5c4859489472489319d146e4f` 이후 AFK 설정·Discord 수정창 닫기 변경을 포함한다. 2026-10-25 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [src/Plugin.cs](#file-src-plugin-cs)
- [src/AfkListingMonitor.cs](#file-src-afklistingmonitor-cs)
- [src/AfkSettings.cs](#file-src-afksettings-cs)
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
- [src/PartyRoster.cs](#file-src-partyroster-cs)
- [src/PartyRosterReader.cs](#file-src-partyrosterreader-cs)
- [src/PartyWindow.cs](#file-src-partywindow-cs)
- [src/Plugin.Afk.cs](#file-src-plugin-afk-cs)
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
| <a id="symbol-src-plugin-cs-399-f29cdf7ede"></a><code>Plugin.OnUpdate</code><br><code>void OnUpdate(IFramework framework)</code> | 메서드 | 기본500ms 또는 명시 요청마다 먼저 우선 닫기 queue를 처리한다. 현재 파티→외부/AFK guard→모집글→초안/갱신→외부 snapshot→AFK 감시 순서로 이어가며 닫기 진행 중 일반 갱신은 진행하지 않는다. | [L399](../../src/Plugin.cs#L399) |
| <a id="symbol-src-plugin-cs-458-85d8cf05c7"></a><code>Plugin.Dispose</code><br><code>void Dispose()</code> | 메서드 | AFK의 자기 조회를 먼저 취소한 뒤 외부 연결·trace·갱신·Addon/Framework/UI 이벤트와 명령/창을 정리한다. 저장 Discord token을 지우지 않는다. | [L458](../../src/Plugin.cs#L458) |

<a id="file-src-afklistingmonitor-cs"></a>
## src/AfkListingMonitor.cs

[소스 파일](../../src/AfkListingMonitor.cs) · 명명된 항목/진입점 15개

AFK 감시의 순수 상태 기계. CID 명단 변화와 선택한 주기로 내 모집글을 조회하고 실패한 필요 상태를 보존한다. host 계약에는 수정/제출 기능이 없다.

직접 관련 테스트: [AfkListingMonitorTests.cs](tests.md#file-tests-afklistingmonitortests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-afklistingmonitor-cs-7-1f2864697a"></a><code>IAfkListingHost.TryReadRoster</code><br><code>bool TryReadRoster(out PartyRoster? roster)</code> | 인터페이스 계약 | 전체 현재 CID 명단을 읽는 host 계약. false는 빈 명단이 아니라 이번 읽기 실패를 뜻한다. | [L7](../../src/AfkListingMonitor.cs#L7) |
| <a id="symbol-src-afklistingmonitor-cs-8-7a48722135"></a><code>IAfkListingHost.QueryBlockReason</code><br><code>string? QueryBlockReason(bool ownQueryInProgress)</code> | 인터페이스 계약 | 현재 조회를 실행할 수 없는 사유를 반환한다. 자기 진행 중 요청은 조회 busy 검사에서 제외하되 수정창/접속 등의 차단은 유지한다. | [L8](../../src/AfkListingMonitor.cs#L8) |
| <a id="symbol-src-afklistingmonitor-cs-13-d514b3dc36"></a><code>IAfkListingHost.OwnListingMatches</code><br><code>bool OwnListingMatches(PartyRoster roster)</code> | 인터페이스 계약 | fresh own 상세의 참가 CID가 현재 관측 명단과 일치하는지 확인하는 계약이다. 현재 직업은 검사하지 않는다. | [L13](../../src/AfkListingMonitor.cs#L13) |
| <a id="symbol-src-afklistingmonitor-cs-14-9193ac90f0"></a><code>IAfkListingHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 인터페이스 계약 | 기존 조회 경로에1회 예약하고 요청 revision을 전진시키는 계약이다. 실제 native dispatch는 다음 읽기에서 일어난다. | [L14](../../src/AfkListingMonitor.cs#L14) |
| <a id="symbol-src-afklistingmonitor-cs-15-068fdadc5b"></a><code>IAfkListingHost.CancelQuery</code><br><code>void CancelQuery(long revision)</code> | 인터페이스 계약 | 지정한 request revision과 같은 자기 요청만 취소하는 계약이다. 이후 수동/외부 요청을 취소하면 안 된다. | [L15](../../src/AfkListingMonitor.cs#L15) |
| <a id="symbol-src-afklistingmonitor-cs-36-3a8e3274e8"></a><code>AfkListingMonitor.InFlight</code><br><code>bool InFlight</code> | 계산 속성 | AFK가 소유한 query revision을 기다리는 중인지 반환한다. 계산식: <code>requestRevision &gt;= 0</code>. | [L36](../../src/AfkListingMonitor.cs#L36) |
| <a id="symbol-src-afklistingmonitor-cs-38-7f5d26a95f"></a><code>AfkListingMonitor.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 활성/필요/진행·명단 신뢰도/인원·변화/시도/성공/실패 횟수와 generation을 출력한다. 멤버 CID 원문은 제외한다. | [L38](../../src/AfkListingMonitor.cs#L38) |
| <a id="symbol-src-afklistingmonitor-cs-45-98462a4b59"></a><code>AfkListingMonitor.Configure</code><br><code>void Configure(AfkSettings settings, long now)</code> | 메서드 | 정규화한 정책을 교체한다. 주기 변경은 현재 시각부터 새 간격을 잡고, 실패 대기 중 재시도 설정 변경은 새 대기 시간으로 예약한다. | [L45](../../src/AfkListingMonitor.cs#L45) |
| <a id="symbol-src-afklistingmonitor-cs-58-dc0568fb29"></a><code>AfkListingMonitor.Watching</code><br><code>string Watching</code> | 계산 속성 | 현재 인원과 선택한 추가 조회 간격 또는 주기 조회 꺼짐 상태를 표시한다. | [L58](../../src/AfkListingMonitor.cs#L58) |
| <a id="symbol-src-afklistingmonitor-cs-61-c5eb63ec0a"></a><code>AfkListingMonitor.SetEnabled</code><br><code>bool SetEnabled(bool enabled, IAfkListingHost host, long now)</code> | 메서드 | 접속한 캐릭터에 AFK를 묶고 첫 조회를 예약한다. 끌 때 자기 query만 취소하고 기준 명단/재시도 상태를 초기화한다. 설정을 영구 저장하거나 자동 재활성화하지 않는다. | [L61](../../src/AfkListingMonitor.cs#L61) |
| <a id="symbol-src-afklistingmonitor-cs-77-4db71e0508"></a><code>AfkListingMonitor.BeforeRead</code><br><code>void BeforeRead(IAfkListingHost host, long now)</code> | 메서드 | native 지연 dispatch 전에 소유권·접속·기존 조회 guard와5/15초 대기를 재검사한다. 차단되면 자기 요청만 취소하고 재시도 필요 상태를 남긴다. | [L77](../../src/AfkListingMonitor.cs#L77) |
| <a id="symbol-src-afklistingmonitor-cs-88-ea82a44621"></a><code>AfkListingMonitor.Update</code><br><code>void Update(IAfkListingHost host, long now)</code> | 메서드 | 명단 변화/읽기 불확실성을 generation에 남긴다. fresh 본인 상세·CID 일치로 성공을 확인하고 조회 중 변화를 보존한다. 주기 조회는 선택 시만 예약하며 재시도 꺼짐은 같은 실패를 반복하지 않는다. 새 명단 변화와 선택 주기는 실패 재시도와 별도 trigger다. | [L88](../../src/AfkListingMonitor.cs#L88) |
| <a id="symbol-src-afklistingmonitor-cs-178-6fe81948b2"></a><code>AfkListingMonitor.CheckSession</code><br><code>bool CheckSession(IAfkListingHost host)</code> | 메서드 | 실제 로그아웃 또는0이 아닌 다른 self CID를 감지하면 AFK를 끈다. 일시적 PlayerState 부재(SelfId0)는 기존 guard에 맡기며 모드를 끄지 않는다. | [L178](../../src/AfkListingMonitor.cs#L178) |
| <a id="symbol-src-afklistingmonitor-cs-192-f2fd657430"></a><code>AfkListingMonitor.CancelOwned</code><br><code>void CancelOwned(IAfkListingHost host)</code> | 메서드 | 기록한 revision만 host에 취소 요청하고 소유 guard를 비운다. 다른 owner의 요청이나 이미 제출한 게임 수정은 건드리지 않는다. | [L192](../../src/AfkListingMonitor.cs#L192) |
| <a id="symbol-src-afklistingmonitor-cs-198-9238f87fed"></a><code>AfkListingMonitor.Retry</code><br><code>void Retry(long now, string message)</code> | 메서드 | 실패 필요 상태와 해당 generation을 보관한다. 선택한 고정 초 간격으로 다음 시각을 잡고 재시도 꺼짐이면 명단 변화 또는 별도 주기 trigger를 기다린다. | [L198](../../src/AfkListingMonitor.cs#L198) |

<a id="file-src-afksettings-cs"></a>
## src/AfkSettings.cs

[소스 파일](../../src/AfkSettings.cs) · 명명된 항목/진입점 4개

영구 저장할 AFK 조회 정책의 불변 값. 주기 조회는 기본 꺼짐, 실패 재시도는 기본 켜짐·5초이며 AFK 실행 여부는 저장하지 않는다.

직접 관련 테스트: [AfkSettingsTests.cs](tests.md#file-tests-afksettingstests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-afksettings-cs-3-8948fa4246"></a><code>AfkSettings</code><br><code>AfkSettings(bool PeriodicEnabled = false, int PeriodMinutes = 1, bool RetryEnabled = true, int RetrySeconds = 5)</code> | 주 생성자/모델 | 선택 주기 조회 여부/분 간격과 실패 재시도 여부/초 간격을 묶는다. | [L3](../../src/AfkSettings.cs#L3) |
| <a id="symbol-src-afksettings-cs-6-26e633b54a"></a><code>AfkSettings.Normalize</code><br><code>AfkSettings Normalize()</code> | 메서드 | 주기1~55분, 실패 재시도1~3600초로 보정한다. UI와 기존/수정된 설정 파일 모두 같은 범위를 사용한다. | [L6](../../src/AfkSettings.cs#L6) |
| <a id="symbol-src-afksettings-cs-9-e61b1cb7ef"></a><code>AfkSettings.PeriodMilliseconds</code><br><code>long PeriodMilliseconds</code> | 계산 속성 | 분 단위 주기를 long 밀리초로 변환한다. 계산식: <code>PeriodMinutes * 60000L</code>. | [L9](../../src/AfkSettings.cs#L9) |
| <a id="symbol-src-afksettings-cs-10-e6864f35e7"></a><code>AfkSettings.RetryMilliseconds</code><br><code>long RetryMilliseconds</code> | 계산 속성 | 초 단위 대기 시간을 long 밀리초로 변환한다. 계산식: <code>RetrySeconds * 1000L</code>. | [L10](../../src/AfkSettings.cs#L10) |

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

<a id="file-src-partyroster-cs"></a>
## src/PartyRoster.cs

[소스 파일](../../src/PartyRoster.cs) · 명명된 항목/진입점 2개

AFK 비교 전용 불변 CID 명단. 직업·이름·레벨·정렬 순서·그룹 내 위치는 비교에 넣지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-partyroster-cs-5-ba02232267"></a><code>PartyRoster</code><br><code>PartyRoster(ulong SelfId, string Fingerprint, int Count)</code> | 주 생성자/모델 | 본인 CID·정렬된 전체 멤버 CID fingerprint·인원수를 보관한다. fingerprint는 관리형 비교용이며 진단/외부 응답에는 출력하지 않는다. | [L5](../../src/PartyRoster.cs#L5) |
| <a id="symbol-src-partyroster-cs-7-dba9c2c7c0"></a><code>PartyRoster.TryCreate</code><br><code>bool TryCreate(ulong selfId, IEnumerable&lt;ulong&gt; members, out PartyRoster? roster)</code> | 메서드 | 최대48명·자기 포함·0이 아닌 고유 CID를 검증하고 정렬해 순서에 영향받지 않는 명단을 만든다. 잘못 읽은 빈/부분 명단을 정상 탈퇴로 취급하지 않게 실패를 반환한다. | [L7](../../src/PartyRoster.cs#L7) |

<a id="file-src-partyrosterreader-cs"></a>
## src/PartyRosterReader.cs

[소스 파일](../../src/PartyRosterReader.cs) · 명명된 항목/진입점 4개

AFK가 켜진 동안만 현재 전체 파티 CID를 읽는다. 기존 PartyReader의 실시간 직업 표시와 별개이며 서버 초월은 최대6개 그룹 전체를 순회한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-partyrosterreader-cs-10-abcca7443a"></a><code>PartyRosterReader.TryRead</code><br><code>bool TryRead(out PartyRoster? roster)</code> | 메서드 | 접속/PlayerState와 native 포인터 가용성을 확인한다. 유효한 서버 초월 파티는 전체 그룹을 읽고, 일반 파티는 Dalamud IPartyList로 읽는다. 예외/부분 읽기는 false로 반환하며 기존 명단은 monitor가 보존한다. 별도 지역 이동 조건은 검사하지 않는다. | [L10](../../src/PartyRosterReader.cs#L10) |
| <a id="symbol-src-partyrosterreader-cs-34-d5513846ca"></a><code>PartyRosterReader.ReadCrossRealm</code><br><code>bool ReadCrossRealm(InfoProxyCrossRealm* proxy, ulong self, out PartyRoster? roster)</code> | 메서드 | GroupCount1~6, 각 GroupMemberCount0~8, local group의 자기 포함 및 고유 CID를 검증한다. 다른 연합 그룹·친선 관전도 포함하며 읽는 도중 group/count가 바뀌면 실패한다. 활성 서버 초월 데이터가 불완전할 때 일반 파티로 축소하지 않는다. | [L34](../../src/PartyRosterReader.cs#L34) |
| <a id="symbol-src-partyrosterreader-cs-62-98b05f8127"></a><code>PartyRosterReader.ReadRegular</code><br><code>bool ReadRegular(ulong self, out PartyRoster? roster)</code> | 메서드 | 일반 파티 indexer의 최대8명과 IsAlliance이면 공개 alliance API의20개 영역에서 유효 CID를 모은다. 빈 영역만 제외하고 null/자기 누락/중복/party ID·인원 변화는 거부한다. 실제 비파티 상태는 본인1명으로 표현한다. | [L62](../../src/PartyRosterReader.cs#L62) |
| <a id="symbol-src-partyrosterreader-cs-101-9ac50b7311"></a><code>PartyRosterReader.Fail</code><br><code>bool Fail(string reason)</code> | 메서드 | CID 원문 없이 명단 읽기 실패 사유를 진단에 남기고 false를 반환한다. | [L101](../../src/PartyRosterReader.cs#L101) |

<a id="file-src-partywindow-cs"></a>
## src/PartyWindow.cs

[소스 파일](../../src/PartyWindow.cs) · 명명된 항목/진입점 29개

인게임 ImGui 화면. 현재 파티/모집글/외부 연결/수정 초안/대기 변경/진단을 표시하고 Plugin에 의도만 전달한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-partywindow-cs-23-68a6556cbf"></a><code>PartyWindow.PartyWindow</code><br><code>PartyWindow(Plugin plugin)</code> | 생성자 | Plugin 참조와 창 크기·제약 등 ImGui window 설정을 초기화한다. | [L23](../../src/PartyWindow.cs#L23) |
| <a id="symbol-src-partywindow-cs-31-84955a83d8"></a><code>PartyWindow.Draw</code><br><code>void Draw()</code> | 메서드 | 전체 창의 조회·갱신·대기 변경 버튼, 파티/모집글/연결 패널과 편집창을 그린다. | [L31](../../src/PartyWindow.cs#L31) |
| <a id="symbol-src-partywindow-cs-57-7c8ea2c278"></a><code>PartyWindow.DrawAfk</code><br><code>void DrawAfk()</code> | 메서드 | AFK 토글과 감시 상태, 별도의 주기 조회 켜기/1~55분 및 실패 재시도 켜기/1~3600초를 그린다. 네 옵션은 저장하고 AFK 자체는 세션에서 직접 켠다. | [L57](../../src/PartyWindow.cs#L57) |
| <a id="symbol-src-partywindow-cs-90-e644261552"></a><code>PartyWindow.DrawBrowser</code><br><code>void DrawBrowser()</code> | 메서드 | 같은 PC 웹 연결의 켜기/끄기, 주소·인증 링크와 복사 UI를 표시한다. | [L90](../../src/PartyWindow.cs#L90) |
| <a id="symbol-src-partywindow-cs-105-1b1767a90a"></a><code>PartyWindow.DrawRelay</code><br><code>void DrawRelay()</code> | 메서드 | 중계 서버 주소·연결 키·외부 링크와 연결 상태를 표시하고 relay 시작/종료를 요청한다. | [L105](../../src/PartyWindow.cs#L105) |
| <a id="symbol-src-partywindow-cs-138-46fd41bc2c"></a><code>PartyWindow.DrawDiscord</code><br><code>void DrawDiscord()</code> | 메서드 | 봇 토큰 입력, 기억/저장 토큰 사용, 초대 URL·이모지 동기화 상태와 연결 해제/저장 삭제 UI를 표시한다. | [L138](../../src/PartyWindow.cs#L138) |
| <a id="symbol-src-partywindow-cs-162-816245c820"></a><code>PartyWindow.DrawCurrentParty</code><br><code>void DrawCurrentParty()</code> | 메서드 | PartySnapshot의 현재 멤버·직업·월드·stale 상태를 표시한다. 모집 등록 직업 화면과 별개다. | [L162](../../src/PartyWindow.cs#L162) |
| <a id="symbol-src-partywindow-cs-193-ae22dc4e02"></a><code>PartyWindow.DrawListing</code><br><code>void DrawListing()</code> | 메서드 | 상세 모집의 소유자·임무·시간·조건·소개말·등록 자리를 표시하고 개별 편집 진입 UI를 연결한다. | [L193](../../src/PartyWindow.cs#L193) |
| <a id="symbol-src-partywindow-cs-272-f28f1ddafd"></a><code>PartyWindow.DrawDescription</code><br><code>void DrawDescription(ListingSnapshot listing)</code> | 메서드 | 조회된 소개말의 표시값과 편집 진입 버튼을 그린다. | [L272](../../src/PartyWindow.cs#L272) |
| <a id="symbol-src-partywindow-cs-294-81813b5f29"></a><code>PartyWindow.DrawDescriptionEditor</code><br><code>void DrawDescriptionEditor()</code> | 메서드 | 소개말 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyDescriptionDraft에 대기 변경을 전달한다. | [L294](../../src/PartyWindow.cs#L294) |
| <a id="symbol-src-partywindow-cs-335-2973a83877"></a><code>PartyWindow.DrawListingSlots</code><br><code>void DrawListingSlots(ListingSnapshot listing)</code> | 메서드 | 등록된 자리만 순회하고 인원수·그룹/멤버 위치·미등록 매핑 진단을 표시한다. | [L335](../../src/PartyWindow.cs#L335) |
| <a id="symbol-src-partywindow-cs-378-5ce5dc03e0"></a><code>PartyWindow.DrawObjective</code><br><code>void DrawObjective(ListingSnapshot listing)</code> | 메서드 | 조회된 모집 목적의 표시값과 편집 진입 버튼을 그린다. | [L378](../../src/PartyWindow.cs#L378) |
| <a id="symbol-src-partywindow-cs-390-bedc1412e7"></a><code>PartyWindow.DrawBeginner</code><br><code>void DrawBeginner(ListingSnapshot listing)</code> | 메서드 | 조회된 초행 및 초보자 환영의 표시값과 편집 진입 버튼을 그린다. | [L390](../../src/PartyWindow.cs#L390) |
| <a id="symbol-src-partywindow-cs-402-1bcad5c21b"></a><code>PartyWindow.DrawBeginnerEditor</code><br><code>void DrawBeginnerEditor()</code> | 메서드 | 초행 및 초보자 환영 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyBeginnerDraft에 대기 변경을 전달한다. | [L402](../../src/PartyWindow.cs#L402) |
| <a id="symbol-src-partywindow-cs-435-af16dd4147"></a><code>PartyWindow.DrawUniqueJobs</code><br><code>void DrawUniqueJobs(ListingSnapshot listing)</code> | 메서드 | 조회된 잡 중복 없음의 표시값과 편집 진입 버튼을 그린다. | [L435](../../src/PartyWindow.cs#L435) |
| <a id="symbol-src-partywindow-cs-447-f0326be7c2"></a><code>PartyWindow.DrawUniqueJobsEditor</code><br><code>void DrawUniqueJobsEditor()</code> | 메서드 | 잡 중복 없음 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyUniqueJobsDraft에 대기 변경을 전달한다. | [L447](../../src/PartyWindow.cs#L447) |
| <a id="symbol-src-partywindow-cs-480-fe04c47781"></a><code>PartyWindow.DrawPrivate</code><br><code>void DrawPrivate(ListingSnapshot listing)</code> | 메서드 | 조회된 비공개/PIN의 표시값과 편집 진입 버튼을 그린다. | [L480](../../src/PartyWindow.cs#L480) |
| <a id="symbol-src-partywindow-cs-493-6ef76c8a2b"></a><code>PartyWindow.DrawPrivateEditor</code><br><code>void DrawPrivateEditor()</code> | 메서드 | 비공개/PIN 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyPrivateDraft에 대기 변경을 전달한다. | [L493](../../src/PartyWindow.cs#L493) |
| <a id="symbol-src-partywindow-cs-537-968ecb30ae"></a><code>PartyWindow.DrawCompletion</code><br><code>void DrawCompletion(ListingSnapshot listing)</code> | 메서드 | 조회된 완료조건의 표시값과 편집 진입 버튼을 그린다. | [L537](../../src/PartyWindow.cs#L537) |
| <a id="symbol-src-partywindow-cs-550-42d7a11a9c"></a><code>PartyWindow.DrawCompletionEditor</code><br><code>void DrawCompletionEditor()</code> | 메서드 | 완료조건 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyCompletionDraft에 대기 변경을 전달한다. | [L550](../../src/PartyWindow.cs#L550) |
| <a id="symbol-src-partywindow-cs-597-df53156f1f"></a><code>PartyWindow.DrawItemLevel</code><br><code>void DrawItemLevel(ListingSnapshot listing)</code> | 메서드 | 조회된 평균 아이템 레벨의 표시값과 편집 진입 버튼을 그린다. | [L597](../../src/PartyWindow.cs#L597) |
| <a id="symbol-src-partywindow-cs-610-28d8783fe4"></a><code>PartyWindow.DrawItemLevelEditor</code><br><code>void DrawItemLevelEditor()</code> | 메서드 | 평균 아이템 레벨 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyItemLevelDraft에 대기 변경을 전달한다. | [L610](../../src/PartyWindow.cs#L610) |
| <a id="symbol-src-partywindow-cs-652-8e1ae28cc2"></a><code>PartyWindow.DrawRule</code><br><code>void DrawRule(ListingSnapshot listing, ListingRuleKind kind)</code> | 메서드 | 조회된 임무 찾기/전리품 규칙의 표시값과 편집 진입 버튼을 그린다. | [L652](../../src/PartyWindow.cs#L652) |
| <a id="symbol-src-partywindow-cs-666-cad6d0ffbb"></a><code>PartyWindow.DrawRuleEditor</code><br><code>void DrawRuleEditor()</code> | 메서드 | 임무 찾기/전리품 규칙 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyRuleDraft에 대기 변경을 전달한다. | [L666](../../src/PartyWindow.cs#L666) |
| <a id="symbol-src-partywindow-cs-710-7d930b871b"></a><code>PartyWindow.DrawObjectiveTrace</code><br><code>void DrawObjectiveTrace()</code> | 메서드 | 수동 이벤트 기록의 시작/종료 및 진단 복사 UI를 그린다. | [L710](../../src/PartyWindow.cs#L710) |
| <a id="symbol-src-partywindow-cs-729-3631a86e66"></a><code>PartyWindow.DrawObjectiveEditor</code><br><code>void DrawObjectiveEditor()</code> | 메서드 | 모집 목적 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyObjectiveDraft에 대기 변경을 전달한다. | [L729](../../src/PartyWindow.cs#L729) |
| <a id="symbol-src-partywindow-cs-771-0b53bf51f4"></a><code>PartyWindow.DrawListingSlotJob</code><br><code>void DrawListingSlotJob(ListingSlot slot, ListingSnapshot listing)</code> | 메서드 | 입장 직업/직업 제한 라벨과 마우스오버 허용 직업 목록을 표시한다. | [L771](../../src/PartyWindow.cs#L771) |
| <a id="symbol-src-partywindow-cs-809-fb7805c015"></a><code>PartyWindow.DrawJobEditor</code><br><code>void DrawJobEditor()</code> | 메서드 | 직업 초안 선택 UI와 적용 가능 여부를 표시하고 대기 변경으로 추가한다. | [L809](../../src/PartyWindow.cs#L809) |
| <a id="symbol-src-partywindow-cs-858-9a21849037"></a><code>PartyWindow.Cell</code><br><code>void Cell(string text)</code> | 메서드 | 표 셀을 이동하고 지정 문자열을 출력하는 공통 그리기 helper다. | [L858](../../src/PartyWindow.cs#L858) |

<a id="file-src-plugin-afk-cs"></a>
## src/Plugin.Afk.cs

[소스 파일](../../src/Plugin.Afk.cs) · 명명된 항목/진입점 18개

인게임 AFK 토글과 순수 monitor를 실제 Reader/기존 조회 guard에 연결한다. 별도의 지역 이동 상태 기계나 자동 갱신/알림 전송 경로를 만들지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-afk-cs-10-905d17be5b"></a><code>Plugin.AfkOptions</code><br><code>AfkSettings AfkOptions</code> | 계산 속성 | 저장 설정을 최초1회 읽어 monitor의 현재 정책을 반환한다. | [L10](../../src/Plugin.Afk.cs#L10) |
| <a id="symbol-src-plugin-afk-cs-11-9998af65e4"></a><code>Plugin.AfkEnabled</code><br><code>bool AfkEnabled</code> | 계산 속성 | 현재 실행의 AFK 활성 여부를 반환한다. 게임 자체의 자리비움 상태와 별개의 PVKR 토글이다. 계산식: <code>afk.Enabled</code>. | [L11](../../src/Plugin.Afk.cs#L11) |
| <a id="symbol-src-plugin-afk-cs-12-edb3c0617b"></a><code>Plugin.AfkStatus</code><br><code>string AfkStatus</code> | 계산 속성 | 자동 조회의 감시/대기/실패/재시도 상태를 인게임 UI에 제공한다. 계산식: <code>afk.Status</code>. | [L12](../../src/Plugin.Afk.cs#L12) |
| <a id="symbol-src-plugin-afk-cs-14-5dd17e23b1"></a><code>Plugin.SetAfkEnabled</code><br><code>void SetAfkEnabled(bool enabled)</code> | 메서드 | host를 지연 생성하고 monitor의 활성 상태를 바꾼 뒤 Framework 읽기를 예약한다. | [L14](../../src/Plugin.Afk.cs#L14) |
| <a id="symbol-src-plugin-afk-cs-22-6e1cdd4995"></a><code>Plugin.LoadAfkSettings</code><br><code>void LoadAfkSettings()</code> | 메서드 | RelayConfiguration의 AFK 정책을 로드한다. 실패 시 기본값과 읽기 오류를 표시하며 AFK 자체를 자동 활성화하지 않는다. | [L22](../../src/Plugin.Afk.cs#L22) |
| <a id="symbol-src-plugin-afk-cs-33-2e175a58b3"></a><code>Plugin.SaveAfkSettings</code><br><code>void SaveAfkSettings(AfkSettings settings)</code> | 메서드 | 같은 설정 객체의 연결 정보를 보존하면서 AFK 정책을 저장하고 성공한 정책만 monitor에 반영한다. 실패 문구를 인게임 UI에 남긴다. | [L33](../../src/Plugin.Afk.cs#L33) |
| <a id="symbol-src-plugin-afk-cs-45-d067668d1c"></a><code>Plugin.AfkHost</code><br><code>AfkHost(Plugin plugin)</code> | 주 생성자/모델 | Plugin의 Reader·Listing·조회 guard를 IAfkListingHost 계약으로 연결하는 게임 스레드 어댑터다. | [L45](../../src/Plugin.Afk.cs#L45) |
| <a id="symbol-src-plugin-afk-cs-47-84f1b0a62b"></a><code>Plugin.AfkHost.LoggedIn</code><br><code>bool LoggedIn</code> | 계산 속성 | 실제 ClientState 로그인 상태를 반환한다. PlayerState가 잠시 준비되지 않은 상태와 구분한다. 계산식: <code>ClientState.IsLoggedIn</code>. | [L47](../../src/Plugin.Afk.cs#L47) |
| <a id="symbol-src-plugin-afk-cs-48-952835f8e9"></a><code>Plugin.AfkHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | 로그인 및 PlayerState 준비 시 현재 CID, 그 외0을 반환한다. 계산식: <code>LoggedIn &amp;&amp; PlayerState.IsLoaded ? PlayerState.ContentId : 0</code>. | [L48](../../src/Plugin.Afk.cs#L48) |
| <a id="symbol-src-plugin-afk-cs-49-c7fd17c9dc"></a><code>Plugin.AfkHost.TryReadRoster</code><br><code>bool TryReadRoster(out PartyRoster? roster)</code> | 메서드 | AFK 전용 전체 파티 CID reader를 호출한다. | [L49](../../src/Plugin.Afk.cs#L49) |
| <a id="symbol-src-plugin-afk-cs-50-d69023bc5b"></a><code>Plugin.AfkHost.QueryBlockReason</code><br><code>string? QueryBlockReason(bool ownQueryInProgress)</code> | 메서드 | 기존 GetQueryUnavailableReason에 적용 대기 변경 및 갱신 cooldown을 더해 자동 조회를 미룬다. | [L50](../../src/Plugin.Afk.cs#L50) |
| <a id="symbol-src-plugin-afk-cs-53-92762d6bd3"></a><code>Plugin.AfkHost.QueryBusy</code><br><code>bool QueryBusy</code> | 계산 속성 | ListingReader의 대기/진행 여부를 반환한다. 계산식: <code>plugin.listingReader.Busy</code>. | [L53](../../src/Plugin.Afk.cs#L53) |
| <a id="symbol-src-plugin-afk-cs-54-f734cee023"></a><code>Plugin.AfkHost.QuerySucceeded</code><br><code>bool QuerySucceeded</code> | 계산 속성 | ListingReader가 새로운 상세 응답을 관측했는지 반환한다. 이것만으로 소유권/명단이 확인된 것은 아니다. 계산식: <code>plugin.listingReader.LastOwnRequestSucceeded</code>. | [L54](../../src/Plugin.Afk.cs#L54) |
| <a id="symbol-src-plugin-afk-cs-55-1bafad19d4"></a><code>Plugin.AfkHost.RequestRevision</code><br><code>long RequestRevision</code> | 계산 속성 | 현재 조회 요청 serial을 제공해 AFK/수동/외부 작업의 취소 소유권을 분리한다. 계산식: <code>plugin.listingReader.RequestRevision</code>. | [L55](../../src/Plugin.Afk.cs#L55) |
| <a id="symbol-src-plugin-afk-cs-56-63ce6348f5"></a><code>Plugin.AfkHost.DetailRevision</code><br><code>long DetailRevision</code> | 계산 속성 | 상세창 갱신 revision을 제공해 이전 결과 재사용을 거부한다. 계산식: <code>plugin.listingReader.DetailRevision</code>. | [L56](../../src/Plugin.Afk.cs#L56) |
| <a id="symbol-src-plugin-afk-cs-57-0c7084aceb"></a><code>Plugin.AfkHost.OwnListingMatches</code><br><code>bool OwnListingMatches(PartyRoster roster)</code> | 메서드 | 현재 Live/Self·Viewer CID·슬롯 인원 일치와 모든 참가 CID의 완전성을 검사하고 정렬한 명단을 비교한다. 등록 직업을 현재 직업으로 덮어쓰지 않는다. | [L57](../../src/Plugin.Afk.cs#L57) |
| <a id="symbol-src-plugin-afk-cs-64-c3df8528d5"></a><code>Plugin.AfkHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 메서드 | Plugin.TryQueryOwnListing 공통 예약 경로를 사용한다. 수정창 열기나 갱신 제출은 하지 않는다. | [L64](../../src/Plugin.Afk.cs#L64) |
| <a id="symbol-src-plugin-afk-cs-65-24b10d46cb"></a><code>Plugin.AfkHost.CancelQuery</code><br><code>void CancelQuery(long revision)</code> | 메서드 | busy이며 현재 reader request revision이 인자와 같을 때만 FailRequest로 취소한다. | [L65](../../src/Plugin.Afk.cs#L65) |

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
