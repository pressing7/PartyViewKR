# 플러그인 수명·조회·자리 매핑

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

게임 상태 읽기는 `Plugin.OnUpdate`에서 한다. `PartySnapshot`은 현재 파티, `ListingSnapshot`/`RefreshTarget`은 모집글이다. 직업 수정과 표시의 기준은 후자다.

기준: 0.3.55.0 소스. 기반 main `8e2082b724d2f93da48bcba8c0a9a1e3742ece42` 이후 채팅 전송의 포인터 전용 타입 호환 검사 수정을 포함한다. 2026-10-28(UTC) 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [src/Plugin.cs](#file-src-plugin-cs)
- [src/AfkListingMonitor.cs](#file-src-afklistingmonitor-cs)
- [src/AfkSettings.cs](#file-src-afksettings-cs)
- [src/GameChatCapture.cs](#file-src-gamechatcapture-cs)
- [src/GameChatSession.cs](#file-src-gamechatsession-cs)
- [src/ListingDescription.cs](#file-src-listingdescription-cs)
- [src/ListingEditorCriteria.cs](#file-src-listingeditorcriteria-cs)
- [src/ListingIdentity.cs](#file-src-listingidentity-cs)
- [src/ListingJobRules.cs](#file-src-listingjobrules-cs)
- [src/ListingReader.cs](#file-src-listingreader-cs)
- [src/ListingRoleComparison.cs](#file-src-listingrolecomparison-cs)
- [src/ListingRoleNormalization.cs](#file-src-listingrolenormalization-cs)
- [src/ListingSlotRoute.cs](#file-src-listingslotroute-cs)
- [src/ListingSlots.cs](#file-src-listingslots-cs)
- [src/ListingSnapshot.cs](#file-src-listingsnapshot-cs)
- [src/PartyReader.cs](#file-src-partyreader-cs)
- [src/PartyRoster.cs](#file-src-partyroster-cs)
- [src/PartyRosterReader.cs](#file-src-partyrosterreader-cs)
- [src/PartyWindow.cs](#file-src-partywindow-cs)
- [src/Plugin.Afk.cs](#file-src-plugin-afk-cs)
- [src/Plugin.Chat.cs](#file-src-plugin-chat-cs)
- [src/Plugin.ChatSend.cs](#file-src-plugin-chatsend-cs)
- [src/RuntimeRows.cs](#file-src-runtimerows-cs)

<a id="file-src-plugin-cs"></a>
## src/Plugin.cs

[소스 파일](../../src/Plugin.cs) · 명명된 항목/진입점 54개

Dalamud 진입점. 서비스 주입, UI·명령·Addon 수명 이벤트 등록, 500ms 읽기/갱신 진행, 관리형 편집 초안과 대기 변경을 소유한다. 게임 상태 접근의 중심은 OnUpdate다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-cs-69-5cb863d724"></a><code>Plugin.CanQueue</code><br><code>bool CanQueue(object edit)</code> | 메서드 | 편집 요청을 ListingChange로 감싸 현재 PendingChanges와 함께 보관할 수 있는지 검사한다. | [L69](../../src/Plugin.cs#L69) |
| <a id="symbol-src-plugin-cs-70-603c5d79ff"></a><code>Plugin.QueueChange</code><br><code>bool QueueChange(object edit)</code> | 메서드 | 관리형 변경 대기열에 요청을 넣고 PendingStatus를 갱신한다. 게임에는 아직 제출하지 않는다. | [L70](../../src/Plugin.cs#L70) |
| <a id="symbol-src-plugin-cs-77-60d791644d"></a><code>Plugin.ListingBusy</code><br><code>bool ListingBusy</code> | 계산 속성 | 현재 본인 모집글 조회가 대기/진행 중인지 반환한다. 계산식: <code>listingReader.Busy</code>. | [L77](../../src/Plugin.cs#L77) |
| <a id="symbol-src-plugin-cs-78-524e27ca6a"></a><code>Plugin.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L78](../../src/Plugin.cs#L78) |
| <a id="symbol-src-plugin-cs-80-953352f6a8"></a><code>Plugin.Plugin</code><br><code>Plugin()</code> | 생성자 | 설정·reader/host/trace/window·AFK와 기존 UI/명령/Addon 수명 이벤트를 초기화하고 공개 채팅 이벤트 수신 어댑터를 연결한다. | [L80](../../src/Plugin.cs#L80) |
| <a id="symbol-src-plugin-cs-103-71b8fea271"></a><code>Plugin.OnCommand</code><br><code>void OnCommand(string command, string arguments)</code> | 메서드 | 두 채팅 명령을 공통 창 열기 동작으로 보낸다. 인수별 명령 분기는 없다. | [L103](../../src/Plugin.cs#L103) |
| <a id="symbol-src-plugin-cs-104-19311b7d08"></a><code>Plugin.Open</code><br><code>void Open()</code> | 메서드 | PVKR 창을 표시하고 앞으로 가져온 뒤 다음 읽기를 요청한다. | [L104](../../src/Plugin.cs#L104) |
| <a id="symbol-src-plugin-cs-105-1fcdcb2624"></a><code>Plugin.RequestRead</code><br><code>void RequestRead()</code> | 메서드 | 다음 Framework.Update가 500ms 간격을 기다리지 않도록 readRequested를 세운다. | [L105](../../src/Plugin.cs#L105) |
| <a id="symbol-src-plugin-cs-106-5dfb0eeeef"></a><code>Plugin.QueryOwnListing</code><br><code>void QueryOwnListing()</code> | 메서드 | 공통 조회 차단 검사 후 ListingReader에 본인 모집글 조회를 예약한다. | [L106](../../src/Plugin.cs#L106) |
| <a id="symbol-src-plugin-cs-107-c43d09f7b6"></a><code>Plugin.RefreshOwnListing</code><br><code>void RefreshOwnListing()</code> | 메서드 | 충돌 중인 초안·조회·추적·갱신과 cooldown을 검사한다. 대기 변경이 있으면 배치, 없으면 단순 갱신을 예약한다. | [L107](../../src/Plugin.cs#L107) |
| <a id="symbol-src-plugin-cs-118-bfd3765596"></a><code>Plugin.CancelRefresh</code><br><code>void CancelRefresh()</code> | 메서드 | 갱신 상태 기계를 중단하고 UI 읽기를 요청한다. 이미 보낸 제출은 되돌리지 않는다. | [L118](../../src/Plugin.cs#L118) |
| <a id="symbol-src-plugin-cs-119-699dbb6fd0"></a><code>Plugin.RequestSlotEdit</code><br><code>void RequestSlotEdit(int position)</code> | 메서드 | 다른 편집과 충돌하지 않을 때 표시 자리 번호를 저장한다. 실제 모집글 검증과 초안 생성은 PrepareJobDraft에서 한다. | [L119](../../src/Plugin.cs#L119) |
| <a id="symbol-src-plugin-cs-120-e59d17027e"></a><code>Plugin.RequestDescriptionEdit</code><br><code>void RequestDescriptionEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 소개말 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L120](../../src/Plugin.cs#L120) |
| <a id="symbol-src-plugin-cs-121-9dfe931440"></a><code>Plugin.RequestObjectiveEdit</code><br><code>void RequestObjectiveEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 모집 목적 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L121](../../src/Plugin.cs#L121) |
| <a id="symbol-src-plugin-cs-122-a7deec2bb9"></a><code>Plugin.CloseDescriptionDraft</code><br><code>void CloseDescriptionDraft()</code> | 메서드 | 소개말의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L122](../../src/Plugin.cs#L122) |
| <a id="symbol-src-plugin-cs-123-a7fd7c5438"></a><code>Plugin.CloseJobDraft</code><br><code>void CloseJobDraft()</code> | 메서드 | 허용 직업의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L123](../../src/Plugin.cs#L123) |
| <a id="symbol-src-plugin-cs-124-f4257321bf"></a><code>Plugin.CloseObjectiveDraft</code><br><code>void CloseObjectiveDraft()</code> | 메서드 | 모집 목적의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L124](../../src/Plugin.cs#L124) |
| <a id="symbol-src-plugin-cs-125-ad3c63977d"></a><code>Plugin.RequestBeginnerEdit</code><br><code>void RequestBeginnerEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 초행 및 초보자 환영 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L125](../../src/Plugin.cs#L125) |
| <a id="symbol-src-plugin-cs-130-f7882886b4"></a><code>Plugin.CloseBeginnerDraft</code><br><code>void CloseBeginnerDraft()</code> | 메서드 | 초행 및 초보자 환영의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L130](../../src/Plugin.cs#L130) |
| <a id="symbol-src-plugin-cs-131-db737fa36f"></a><code>Plugin.ApplyBeginnerDraft</code><br><code>bool ApplyBeginnerDraft()</code> | 메서드 | 초행 및 초보자 환영 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L131](../../src/Plugin.cs#L131) |
| <a id="symbol-src-plugin-cs-140-4ea2159fa9"></a><code>Plugin.RequestUniqueJobsEdit</code><br><code>void RequestUniqueJobsEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 잡 중복 없음 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L140](../../src/Plugin.cs#L140) |
| <a id="symbol-src-plugin-cs-145-00623941e9"></a><code>Plugin.CloseUniqueJobsDraft</code><br><code>void CloseUniqueJobsDraft()</code> | 메서드 | 잡 중복 없음의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L145](../../src/Plugin.cs#L145) |
| <a id="symbol-src-plugin-cs-146-449e8b4ff2"></a><code>Plugin.ApplyUniqueJobsDraft</code><br><code>bool ApplyUniqueJobsDraft()</code> | 메서드 | 잡 중복 없음 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L146](../../src/Plugin.cs#L146) |
| <a id="symbol-src-plugin-cs-155-2bc2d1cf29"></a><code>Plugin.RequestPrivateEdit</code><br><code>void RequestPrivateEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 비공개/PIN 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L155](../../src/Plugin.cs#L155) |
| <a id="symbol-src-plugin-cs-160-05ee92237d"></a><code>Plugin.ClosePrivateDraft</code><br><code>void ClosePrivateDraft()</code> | 메서드 | 비공개/PIN의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L160](../../src/Plugin.cs#L160) |
| <a id="symbol-src-plugin-cs-161-9b7d50fc21"></a><code>Plugin.ApplyPrivateDraft</code><br><code>bool ApplyPrivateDraft()</code> | 메서드 | 비공개/PIN 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L161](../../src/Plugin.cs#L161) |
| <a id="symbol-src-plugin-cs-170-7e25b7125d"></a><code>Plugin.RequestCompletionEdit</code><br><code>void RequestCompletionEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 완료조건 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L170](../../src/Plugin.cs#L170) |
| <a id="symbol-src-plugin-cs-175-d4f2a7f9d3"></a><code>Plugin.CloseCompletionDraft</code><br><code>void CloseCompletionDraft()</code> | 메서드 | 완료조건의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L175](../../src/Plugin.cs#L175) |
| <a id="symbol-src-plugin-cs-176-9e083a70bc"></a><code>Plugin.ApplyCompletionDraft</code><br><code>bool ApplyCompletionDraft()</code> | 메서드 | 완료조건 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L176](../../src/Plugin.cs#L176) |
| <a id="symbol-src-plugin-cs-185-66e0ce69e4"></a><code>Plugin.RequestItemLevelEdit</code><br><code>void RequestItemLevelEdit()</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 평균 아이템 레벨 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L185](../../src/Plugin.cs#L185) |
| <a id="symbol-src-plugin-cs-191-9cdb7ac0a6"></a><code>Plugin.CloseItemLevelDraft</code><br><code>void CloseItemLevelDraft()</code> | 메서드 | 평균 아이템 레벨의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L191](../../src/Plugin.cs#L191) |
| <a id="symbol-src-plugin-cs-192-e3f552c618"></a><code>Plugin.ApplyItemLevelDraft</code><br><code>bool ApplyItemLevelDraft()</code> | 메서드 | 평균 아이템 레벨 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L192](../../src/Plugin.cs#L192) |
| <a id="symbol-src-plugin-cs-202-789d943fc1"></a><code>Plugin.RequestRuleEdit</code><br><code>void RequestRuleEdit(ListingRuleKind kind)</code> | 메서드 | 다른 편집/추적/갱신과 충돌하지 않으면 임무 찾기/전리품 규칙 편집 진입 의도를 저장하고 다음 읽기를 예약한다. | [L202](../../src/Plugin.cs#L202) |
| <a id="symbol-src-plugin-cs-208-90e8d19d28"></a><code>Plugin.CloseRuleDraft</code><br><code>void CloseRuleDraft()</code> | 메서드 | 임무 찾기/전리품 규칙의 PVKR 관리형 초안만 닫는다. 게임 제출/롤백을 수행하는 함수는 아니다. | [L208](../../src/Plugin.cs#L208) |
| <a id="symbol-src-plugin-cs-209-3c004d1d13"></a><code>Plugin.ApplyRuleDraft</code><br><code>bool ApplyRuleDraft()</code> | 메서드 | 임무 찾기/전리품 규칙 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L209](../../src/Plugin.cs#L209) |
| <a id="symbol-src-plugin-cs-219-cdc00ab2a3"></a><code>Plugin.ApplyJobDraft</code><br><code>bool ApplyJobDraft()</code> | 메서드 | 허용 직업 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L219](../../src/Plugin.cs#L219) |
| <a id="symbol-src-plugin-cs-228-ef92aecb3d"></a><code>Plugin.ApplyDescriptionDraft</code><br><code>bool ApplyDescriptionDraft()</code> | 메서드 | 소개말 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L228](../../src/Plugin.cs#L228) |
| <a id="symbol-src-plugin-cs-237-da4decd6f6"></a><code>Plugin.ApplyObjectiveDraft</code><br><code>bool ApplyObjectiveDraft()</code> | 메서드 | 모집 목적 초안을 검증하고 PendingChanges에 추가/교체한 뒤 초안을 닫는다. 게임에서 실제로 적용하는 시점은 갱신 실행이다. | [L237](../../src/Plugin.cs#L237) |
| <a id="symbol-src-plugin-cs-247-86c51df3e3"></a><code>Plugin.OnDetailUpdated</code><br><code>void OnDetailUpdated(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 상세창 PostSetup/PostRefresh를 ListingReader의 DetailRevision 증가로 연결하고 읽기를 예약한다. | [L247](../../src/Plugin.cs#L247) |
| <a id="symbol-src-plugin-cs-248-0f991a150d"></a><code>Plugin.OnEditorUpdated</code><br><code>void OnEditorUpdated(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 모집 수정창 PostSetup/PostRefresh를 host의 EditorRevision 증가로 연결한다. | [L248](../../src/Plugin.cs#L248) |
| <a id="symbol-src-plugin-cs-249-af2c03deb7"></a><code>Plugin.OnEditorClosed</code><br><code>void OnEditorClosed(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 수정창 PreFinalize를 EditorClosedRevision 및 수동 trace 종료에 반영한다. | [L249](../../src/Plugin.cs#L249) |
| <a id="symbol-src-plugin-cs-250-6efb49b6fd"></a><code>Plugin.OnRoleOpened</code><br><code>void OnRoleOpened(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 직업 선택창 PostSetup을 roleOpenedRevision에 반영한다. | [L250](../../src/Plugin.cs#L250) |
| <a id="symbol-src-plugin-cs-251-44acc579e5"></a><code>Plugin.OnRoleClosed</code><br><code>void OnRoleClosed(AddonEvent addonEvent, AddonArgs args)</code> | 메서드 | 직업 선택창 PreFinalize를 roleClosedRevision에 반영한다. | [L251](../../src/Plugin.cs#L251) |
| <a id="symbol-src-plugin-cs-253-c8f20ced24"></a><code>Plugin.PrepareJobDraft</code><br><code>void PrepareJobDraft()</code> | 메서드 | 허용 직업 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L253](../../src/Plugin.cs#L253) |
| <a id="symbol-src-plugin-cs-271-2a68665521"></a><code>Plugin.PrepareDescriptionDraft</code><br><code>void PrepareDescriptionDraft()</code> | 메서드 | 소개말 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L271](../../src/Plugin.cs#L271) |
| <a id="symbol-src-plugin-cs-287-aa52649abd"></a><code>Plugin.PrepareObjectiveDraft</code><br><code>void PrepareObjectiveDraft()</code> | 메서드 | 모집 목적 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L287](../../src/Plugin.cs#L287) |
| <a id="symbol-src-plugin-cs-303-cd57da9846"></a><code>Plugin.PrepareBeginnerDraft</code><br><code>void PrepareBeginnerDraft()</code> | 메서드 | 초행 및 초보자 환영 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L303](../../src/Plugin.cs#L303) |
| <a id="symbol-src-plugin-cs-319-d20a8778c2"></a><code>Plugin.PrepareUniqueJobsDraft</code><br><code>void PrepareUniqueJobsDraft()</code> | 메서드 | 잡 중복 없음 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L319](../../src/Plugin.cs#L319) |
| <a id="symbol-src-plugin-cs-335-1d312722dc"></a><code>Plugin.PreparePrivateDraft</code><br><code>void PreparePrivateDraft()</code> | 메서드 | 비공개/PIN 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L335](../../src/Plugin.cs#L335) |
| <a id="symbol-src-plugin-cs-351-45dc738a65"></a><code>Plugin.PrepareCompletionDraft</code><br><code>void PrepareCompletionDraft()</code> | 메서드 | 완료조건 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L351](../../src/Plugin.cs#L351) |
| <a id="symbol-src-plugin-cs-367-004af35645"></a><code>Plugin.PrepareItemLevelDraft</code><br><code>void PrepareItemLevelDraft()</code> | 메서드 | 평균 아이템 레벨 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L367](../../src/Plugin.cs#L367) |
| <a id="symbol-src-plugin-cs-384-15b79325cf"></a><code>Plugin.PrepareRuleDraft</code><br><code>void PrepareRuleDraft()</code> | 메서드 | 임무 찾기/전리품 규칙 편집 예약을 소비하고 현재 Self 모집/차단 사유/지원 여부를 검사한다. 같은 필드의 대기 변경이 있으면 초안에 복원한다. | [L384](../../src/Plugin.cs#L384) |
| <a id="symbol-src-plugin-cs-401-f2a2c4a75e"></a><code>Plugin.OnUpdate</code><br><code>void OnUpdate(IFramework framework)</code> | 메서드 | 저장된 Discord 연결과 채팅 수신 상태를 갱신하고 매 tick 전송 bridge를 처리한다. 이어 trace·AFK 명단 감시와 기존500ms 조회·초안·갱신·외부 snapshot 흐름을 진행한다. | [L401](../../src/Plugin.cs#L401) |
| <a id="symbol-src-plugin-cs-464-856d4d176a"></a><code>Plugin.Dispose</code><br><code>void Dispose()</code> | 메서드 | 채팅 이벤트를 해제하고 AFK·외부 연결·trace·갱신 및 모든 Addon/Framework/UI 이벤트·명령·창을 정리한다. 저장 토큰 삭제와는 별개다. | [L464](../../src/Plugin.cs#L464) |

<a id="file-src-afklistingmonitor-cs"></a>
## src/AfkListingMonitor.cs

[소스 파일](../../src/AfkListingMonitor.cs) · 명명된 항목/진입점 20개

AFK 감시의 순수 상태 기계. CID 명단 변화와 선택한 주기로 내 모집글을 조회하고 실패한 필요 상태를 보존한다. host 계약에는 수정/제출 기능이 없다.

직접 관련 테스트: [AfkListingMonitorTests.cs](tests.md#file-tests-afklistingmonitortests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-afklistingmonitor-cs-7-1f2864697a"></a><code>IAfkListingHost.TryReadRoster</code><br><code>bool TryReadRoster(out PartyRoster? roster)</code> | 인터페이스 계약 | 전체 현재 CID 명단을 읽는 host 계약. false는 빈 명단이 아니라 이번 읽기 실패를 뜻한다. | [L7](../../src/AfkListingMonitor.cs#L7) |
| <a id="symbol-src-afklistingmonitor-cs-8-7a48722135"></a><code>IAfkListingHost.QueryBlockReason</code><br><code>string? QueryBlockReason(bool ownQueryInProgress)</code> | 인터페이스 계약 | 현재 조회를 실행할 수 없는 사유를 반환한다. 자기 진행 중 요청은 조회 busy 검사에서 제외하되 수정창/접속 등의 차단은 유지한다. | [L8](../../src/AfkListingMonitor.cs#L8) |
| <a id="symbol-src-afklistingmonitor-cs-13-d514b3dc36"></a><code>IAfkListingHost.OwnListingMatches</code><br><code>bool OwnListingMatches(PartyRoster roster)</code> | 인터페이스 계약 | fresh own 상세의 참가 CID가 현재 관측 명단과 일치하는지 확인하는 계약이다. 현재 직업은 검사하지 않는다. | [L13](../../src/AfkListingMonitor.cs#L13) |
| <a id="symbol-src-afklistingmonitor-cs-16-fe1b535612"></a><code>IAfkListingHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 인터페이스 계약 | 기존 조회 경로에1회 예약하고 요청 revision을 전진시키는 계약이다. 실제 native dispatch는 다음 읽기에서 일어난다. | [L16](../../src/AfkListingMonitor.cs#L16) |
| <a id="symbol-src-afklistingmonitor-cs-17-d1369b6b8e"></a><code>IAfkListingHost.CancelQuery</code><br><code>void CancelQuery(long revision)</code> | 인터페이스 계약 | 지정한 request revision과 같은 자기 요청만 취소하는 계약이다. 이후 수동/외부 요청을 취소하면 안 된다. | [L17](../../src/AfkListingMonitor.cs#L17) |
| <a id="symbol-src-afklistingmonitor-cs-38-3f82bd50fd"></a><code>AfkListingMonitor.InFlight</code><br><code>bool InFlight</code> | 계산 속성 | AFK가 소유한 query revision을 기다리는 중인지 반환한다. 계산식: <code>requestRevision &gt;= 0</code>. | [L38](../../src/AfkListingMonitor.cs#L38) |
| <a id="symbol-src-afklistingmonitor-cs-41-5d0527f5f4"></a><code>AfkListingMonitor.RecruitmentComplete</code><br><code>bool RecruitmentComplete</code> | 계산 속성 | AFK 켜짐·현재 명단 읽기 성공·가득 참 latch가 모두 충족돼야 완료 상태를 반환한다. 계산식: <code>Enabled &amp;&amp; readable &amp;&amp; complete</code>. | [L41](../../src/AfkListingMonitor.cs#L41) |
| <a id="symbol-src-afklistingmonitor-cs-42-c37bcfb58b"></a><code>AfkListingMonitor.SingleJobRecruitmentComplete</code><br><code>bool SingleJobRecruitmentComplete</code> | 계산 속성 | AFK/추가 조건이 켜지고 현재 명단이 읽히며 단일 직업 공석 완료가 현재 generation의 성공한 본인 조회로 확인됐는지 반환한다. 다른 명단/읽기 실패의 이전 결과는 노출하지 않는다. 계산식: <code>Enabled &amp;&amp; Settings.SingleJobCompletionEnabled &amp;&amp; readable &amp;&amp;         singleJobComplete &amp;&amp; singleJobConfirmedGeneration == generation</code>. | [L42](../../src/AfkListingMonitor.cs#L42) |
| <a id="symbol-src-afklistingmonitor-cs-49-8e5e71c53e"></a><code>AfkListingMonitor.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 활성/필요/진행·명단 신뢰도/인원·변화/시도/성공/실패 횟수와 generation을 출력한다. 멤버 CID 원문은 제외한다. | [L49](../../src/AfkListingMonitor.cs#L49) |
| <a id="symbol-src-afklistingmonitor-cs-58-da33219caa"></a><code>AfkListingMonitor.Configure</code><br><code>void Configure(AfkSettings settings, long now)</code> | 메서드 | 조회 주기/재시도 변경을 반영한다. 단일 직업 제외 조건 변경은 latch/확인 generation을 폐기하고 새 조회를 요구한다. | [L58](../../src/AfkListingMonitor.cs#L58) |
| <a id="symbol-src-afklistingmonitor-cs-75-4515e000b9"></a><code>AfkListingMonitor.Watching</code><br><code>string Watching</code> | 계산 속성 | 현재 인원과 선택한 추가 조회 간격 또는 주기 조회 꺼짐 상태를 표시한다. | [L75](../../src/AfkListingMonitor.cs#L75) |
| <a id="symbol-src-afklistingmonitor-cs-78-3804d275d3"></a><code>AfkListingMonitor.SetEnabled</code><br><code>bool SetEnabled(bool enabled, IAfkListingHost host, long now)</code> | 메서드 | 접속한 캐릭터에 AFK를 묶고 첫 조회를 예약한다. 끌 때 자기 query만 취소하고 기준 명단/재시도 상태를 초기화한다. 설정을 영구 저장하거나 자동 재활성화하지 않는다. | [L78](../../src/AfkListingMonitor.cs#L78) |
| <a id="symbol-src-afklistingmonitor-cs-96-ec5b7bcce9"></a><code>AfkListingMonitor.BeforeRead</code><br><code>void BeforeRead(IAfkListingHost host, long now)</code> | 메서드 | native 지연 dispatch 전에 소유권·접속·기존 조회 guard와5/15초 대기를 재검사한다. 차단되면 자기 요청만 취소하고 재시도 필요 상태를 남긴다. | [L96](../../src/AfkListingMonitor.cs#L96) |
| <a id="symbol-src-afklistingmonitor-cs-107-a9ffa4bff5"></a><code>AfkListingMonitor.Update</code><br><code>void Update(IAfkListingHost host, long now)</code> | 메서드 | CID 변화·선택 주기·실패 재시도로 조회를 예약한다. 새 본인 응답만 정원/단일 직업 공석 조건을 갱신한다. 전체 정원 도달은 모집 소멸 전 기준으로 즉시 알리고 조회를 멈추지만 단일 직업 공석 완료는 감시/조회 정책을 유지한다. | [L107](../../src/AfkListingMonitor.cs#L107) |
| <a id="symbol-src-afklistingmonitor-cs-218-48af5bf44f"></a><code>AfkListingMonitor.CheckCompletion</code><br><code>bool CheckCompletion(int members)</code> | 메서드 | 기억한 등록 정원과 현재 유효 CID 명단의 인원을 비교한다. 가득 참 진입 시 단조 CompletionRevision을 올리고, 실제 빈자리 관측에서 재무장한다. 정원 초과는 기준을 폐기하며 읽기 실패는 호출하지 않는다. | [L218](../../src/AfkListingMonitor.cs#L218) |
| <a id="symbol-src-afklistingmonitor-cs-229-d4fd60613b"></a><code>AfkListingMonitor.RememberCapacity</code><br><code>void RememberCapacity(IAfkListingHost host)</code> | 메서드 | 확인한 본인 응답의 등록 정원과 request/detail revision을 저장하고 선택된 단일 직업 공석 완료 조건을 같은 snapshot으로 판정한다. | [L229](../../src/AfkListingMonitor.cs#L229) |
| <a id="symbol-src-afklistingmonitor-cs-237-6f48b69a34"></a><code>AfkListingMonitor.RememberSingleJobCompletion</code><br><code>void RememberSingleJobCompletion(ListingSlots? slots)</code> | 메서드 | 현재 CID 명단과 일치하는 새 본인 응답의 CountsAgree/MappingValid/완전한 직업 표·정원을 검사한다. 빈자리가 있고 모든 빈자리의 알려진 허용 직업이 정확히 하나이며 미확인 bit가 없으면 완료 세대를 한 번 증가시킨다. 실패는 latch를 재무장하지 않으며 실제 일반 공석 확인은 재무장한다. | [L237](../../src/AfkListingMonitor.cs#L237) |
| <a id="symbol-src-afklistingmonitor-cs-258-0767a6214f"></a><code>AfkListingMonitor.CheckSession</code><br><code>bool CheckSession(IAfkListingHost host)</code> | 메서드 | 실제 로그아웃 또는0이 아닌 다른 self CID를 감지하면 AFK를 끈다. 일시적 PlayerState 부재(SelfId0)는 기존 guard에 맡기며 모드를 끄지 않는다. | [L258](../../src/AfkListingMonitor.cs#L258) |
| <a id="symbol-src-afklistingmonitor-cs-272-47cffe087b"></a><code>AfkListingMonitor.CancelOwned</code><br><code>void CancelOwned(IAfkListingHost host)</code> | 메서드 | 기록한 revision만 host에 취소 요청하고 소유 guard를 비운다. 다른 owner의 요청이나 이미 제출한 게임 수정은 건드리지 않는다. | [L272](../../src/AfkListingMonitor.cs#L272) |
| <a id="symbol-src-afklistingmonitor-cs-278-4ed56a4833"></a><code>AfkListingMonitor.Retry</code><br><code>void Retry(long now, string message)</code> | 메서드 | 실패 필요 상태와 해당 generation을 보관한다. 선택한 고정 초 간격으로 다음 시각을 잡고 재시도 꺼짐이면 명단 변화 또는 별도 주기 trigger를 기다린다. | [L278](../../src/AfkListingMonitor.cs#L278) |

<a id="file-src-afksettings-cs"></a>
## src/AfkSettings.cs

[소스 파일](../../src/AfkSettings.cs) · 명명된 항목/진입점 4개

영구 저장할 AFK 조회 정책의 불변 값. 주기 조회는 기본 꺼짐, 실패 재시도는 기본 켜짐·5초이며 AFK 실행 여부는 저장하지 않는다.

직접 관련 테스트: [AfkSettingsTests.cs](tests.md#file-tests-afksettingstests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-afksettings-cs-3-e0ab8f5a43"></a><code>AfkSettings</code><br><code>AfkSettings(bool PeriodicEnabled = false, int PeriodMinutes = 1, bool RetryEnabled = true, int RetrySeconds = 5, bool SingleJobCompletionEnabled = false)</code> | 주 생성자/모델 | 선택 주기 조회 여부/분 간격과 실패 재시도 여부/초 간격을 묶는다. | [L3](../../src/AfkSettings.cs#L3) |
| <a id="symbol-src-afksettings-cs-6-26e633b54a"></a><code>AfkSettings.Normalize</code><br><code>AfkSettings Normalize()</code> | 메서드 | 주기1~55분, 실패 재시도1~3600초로 보정한다. UI와 기존/수정된 설정 파일 모두 같은 범위를 사용한다. | [L6](../../src/AfkSettings.cs#L6) |
| <a id="symbol-src-afksettings-cs-9-e61b1cb7ef"></a><code>AfkSettings.PeriodMilliseconds</code><br><code>long PeriodMilliseconds</code> | 계산 속성 | 분 단위 주기를 long 밀리초로 변환한다. 계산식: <code>PeriodMinutes * 60000L</code>. | [L9](../../src/AfkSettings.cs#L9) |
| <a id="symbol-src-afksettings-cs-10-e6864f35e7"></a><code>AfkSettings.RetryMilliseconds</code><br><code>long RetryMilliseconds</code> | 계산 속성 | 초 단위 대기 시간을 long 밀리초로 변환한다. 계산식: <code>RetrySeconds * 1000L</code>. | [L10](../../src/AfkSettings.cs#L10) |

<a id="file-src-gamechatcapture-cs"></a>
## src/GameChatCapture.cs

[소스 파일](../../src/GameChatCapture.cs) · 명명된 항목/진입점 7개

런타임 IChatGui의 공개 ChatMessage 이벤트를 읽는 어댑터. 한섭/공개판의 legacy5인자와 현재 message1인자 delegate를 모두 지원하며 새 native ABI 계약을 추가하지 않는다.

직접 관련 테스트: [GameChatCaptureTests.cs](tests.md#file-tests-gamechatcapturetests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-gamechatcapture-cs-22-c95f8084ab"></a><code>GameChatCapture.GameChatCapture</code><br><code>GameChatCapture(object source, Type contract, Func&lt;GameChatKind, bool&gt; wanted, Action&lt;GameChatMessage&gt; receive, Func&lt;uint, string&gt; worldName, Action&lt;string&gt; error, Func&lt;string&gt;? homeWorld = null)</code> | 생성자 | 실제 공개 이벤트의 delegate signature를 검사하고 읽기 전용 Expression delegate를 만들어 구독한다. 지원하지 않는 signature는 명확히 실패한다. | [L22](../../src/GameChatCapture.cs#L22) |
| <a id="symbol-src-gamechatcapture-cs-45-6bd0e2e134"></a><code>GameChatCapture.Property</code><br><code>object? Property(object value, string name)</code> | 메서드 | 실제 객체의 공개 속성 또는 구현 interface의 속성을 읽는다. explicit interface 및 inherited IChatMessage에도 대응한다. | [L45](../../src/GameChatCapture.cs#L45) |
| <a id="symbol-src-gamechatcapture-cs-52-32e43089f0"></a><code>GameChatCapture.Current</code><br><code>void Current(object message)</code> | 메서드 | 현재1인자 이벤트의 LogKind를 먼저 필터하고 로그인/활성 수신 경로가 있으면 Timestamp·Sender·Message를 읽는다. 예외는 타입 이름만 상태로 전달한다. | [L52](../../src/GameChatCapture.cs#L52) |
| <a id="symbol-src-gamechatcapture-cs-60-2c5cde808b"></a><code>GameChatCapture.Legacy</code><br><code>void Legacy(ushort type, int timestamp, object sender, object message)</code> | 메서드 | legacy type/timestamp/ref SeString 이벤트를 필터해 복사한다. handled flag와 SeString을 수정하지 않는다. | [L60](../../src/GameChatCapture.cs#L60) |
| <a id="symbol-src-gamechatcapture-cs-67-9e1ba4c39b"></a><code>GameChatCapture.Emit</code><br><code>void Emit(GameChatKind kind, int timestamp, object? sender, object? message)</code> | 메서드 | PlayerPayload/홈 서버 보완으로 발신자를 복사하고 이름의 U+E0E0~E0E9 파티 번호 glyph만 제거한다. 본문에 빠진 효과음 macro 표시를 보완한 뒤 관리형 메시지를 전달한다. 원본 이벤트/SeString은 수정하지 않는다. | [L67](../../src/GameChatCapture.cs#L67) |
| <a id="symbol-src-gamechatcapture-cs-94-47542f357a"></a><code>GameChatCapture.IncludeSoundEffects</code><br><code>string IncludeSoundEffects(object message, string text)</code> | 메서드 | 공개 RawPayload.Data 또는 AutoTranslatePayload.Encode(false)의 canonical fixed(200,5,0..15)를 읽는다. TextValue에 없는 &lt;se.1&gt;~&lt;se.16&gt;만 끝에 보완해 효과음 단독 채팅도 전달하며 실제 소리를 재생하지 않는다. 잘못된/다른 macro는 무시한다. | [L94](../../src/GameChatCapture.cs#L94) |
| <a id="symbol-src-gamechatcapture-cs-112-95213a4e55"></a><code>GameChatCapture.Dispose</code><br><code>void Dispose()</code> | 메서드 | 동일 delegate를 공개 이벤트에서 한 번 해제한다. 게임 채팅 출력을 변경하지 않는다. | [L112](../../src/GameChatCapture.cs#L112) |

<a id="file-src-gamechatsession-cs"></a>
## src/GameChatSession.cs

[소스 파일](../../src/GameChatSession.cs) · 명명된 항목/진입점 1개

AFK와 별개로 채팅 수신 캐릭터와 홈 서버 이름을 보관한다. 게임 스레드에서만 갱신하며 지역 이동의 일시적인 PlayerState 공백을 보존한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-gamechatsession-cs-10-f9ad544e6e"></a><code>GameChatSession.Observe</code><br><code>void Observe(bool loggedIn, bool loaded, ulong self, Func&lt;string&gt; readHomeWorld)</code> | 메서드 | 로그아웃은 CID/홈 서버를 지우고, loaded 캐릭터가 바뀌면 이전 서버를 폐기한다. unloaded 중에는 마지막 정상 값을 유지한다. 홈 서버는 알려지지 않았을 때만 callback으로 읽고 값이 생기면 반복 조회하지 않는다. | [L10](../../src/GameChatSession.cs#L10) |

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

[소스 파일](../../src/ListingEditorCriteria.cs) · 명명된 항목/진입점 4개

조회 시 0으로 보이는 친선경기 A/B 6~8번 padding과 수정창의 PvP 기본 마스크 차이만 한정적으로 허용한다.

직접 관련 테스트: [ListingEditorCriteriaTests.cs](tests.md#file-tests-listingeditorcriteriatests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingeditorcriteria-cs-13-b7a66e6ee2"></a><code>ListingEditorCriteria.IsFriendlyPadding</code><br><code>bool IsFriendlyPadding(ListingSlots slots, int index)</code> | 메서드 | 친선경기의 물리 인덱스 5~7, 13~15인지 판정한다. 실제 등록/미등록 자리를 일괄 면제하는 함수가 아니다. | [L13](../../src/ListingEditorCriteria.cs#L13) |
| <a id="symbol-src-listingeditorcriteria-cs-17-1f43f9aadc"></a><code>ListingEditorCriteria.Matches</code><br><code>bool Matches(RefreshTarget target, string actualCriteria, int draftGroups, int draftMainSlots, out int[] acceptedPaddingIndices)</code> | 메서드 | 정확히 같은 조건은 통과한다. 차이가 있으면 검증된 그룹 형태와 모든 비역할 필드를 확인하고, 친선의 여섯 padding 기본값 또는 동일 CID 참가자의 입장 직업 한 bit 축소만 허용한다. 승인한 padding/점유 물리 인덱스를 각각 진단용으로 반환한다. 멤버 배열은 host가 별도로 검증한다. | [L17](../../src/ListingEditorCriteria.cs#L17) |
| <a id="symbol-src-listingeditorcriteria-cs-21-586bc62640"></a><code>ListingEditorCriteria.Matches</code><br><code>bool Matches(RefreshTarget target, string actualCriteria, int draftGroups, int draftMainSlots, out int[] acceptedPaddingIndices, out int[] acceptedOccupiedIndices)</code> | 메서드 | 정확히 같은 조건은 통과한다. 차이가 있으면 검증된 그룹 형태와 모든 비역할 필드를 확인하고, 친선의 여섯 padding 기본값 또는 동일 CID 참가자의 입장 직업 한 bit 축소만 허용한다. 승인한 padding/점유 물리 인덱스를 각각 진단용으로 반환한다. 멤버 배열은 host가 별도로 검증한다. | [L21](../../src/ListingEditorCriteria.cs#L21) |
| <a id="symbol-src-listingeditorcriteria-cs-50-7c29b1befb"></a><code>ListingEditorCriteria.TryReadFlags</code><br><code>bool TryReadFlags(string criteria, int count, out ulong[] flags)</code> | 메서드 | criteria의 마지막 hex 필드를 정해진 슬롯 수의 little-endian ulong 배열로 엄격히 복원한다. | [L50](../../src/ListingEditorCriteria.cs#L50) |

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

<a id="file-src-listingrolenormalization-cs"></a>
## src/ListingRoleNormalization.cs

[소스 파일](../../src/ListingRoleNormalization.cs) · 명명된 항목/진입점 3개

모집글에 등록된 참가자의 입장 직업으로 허용 mask가 좁아지는 게임 자동 변경만 판별한다. 현재 파티 직업은 읽지 않으며 원본 슬롯/아이콘 데이터를 덮어쓰지 않는다.

직접 관련 테스트: [ListingRoleNormalizationTests.cs](tests.md#file-tests-listingrolenormalizationtests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingrolenormalization-cs-7-fae3e0d0e9"></a><code>ListingRoleNormalization.IsOccupied</code><br><code>bool IsOccupied(ListingSlots slots, int nativeIndex, ulong before, ulong after)</code> | 메서드 | 해당 물리 자리에 CID와 등록 JobId가 있고 원본 raw mask가 slot과 일치하는지 확인한다. 완전한 런타임 직업 표에서 입장 직업의 bit를 찾아 원래 허용됐고 결과가 그 한 bit인 경우만 인정한다. | [L7](../../src/ListingRoleNormalization.cs#L7) |
| <a id="symbol-src-listingrolenormalization-cs-16-d5d13955b9"></a><code>ListingRoleNormalization.SameCriteria</code><br><code>bool SameCriteria(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 동일 문자열은 기존과 같이 통과한다. 차이가 있으면 슬롯 형태·CID·등록 직업·점유 상태를 SameSlots로 검사하고 비역할8필드를 정확히 비교한 뒤 점유 자리의 한 방향 축소만 허용한다. 양쪽 Criteria와 Slots의 변경 mask가 일치해야 한다. | [L16](../../src/ListingRoleNormalization.cs#L16) |
| <a id="symbol-src-listingrolenormalization-cs-30-b4a2a82fe9"></a><code>ListingRoleNormalization.TryReadPair</code><br><code>bool TryReadPair(string before, string after, int count, out ulong[] expected, out ulong[] actual)</code> | 메서드 | 9필드 조건 문자열 두 개의 비역할8필드를 비교하고 동일 물리 길이의 little-endian 역할 배열을 해석한다. 잘못된 길이/hex는 거부한다. | [L30](../../src/ListingRoleNormalization.cs#L30) |

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

[소스 파일](../../src/PartyWindow.cs) · 명명된 항목/진입점 30개

인게임 ImGui 화면. 현재 파티/모집글/외부 연결/수정 초안/대기 변경/진단을 표시하고 Plugin에 의도만 전달한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-partywindow-cs-23-68a6556cbf"></a><code>PartyWindow.PartyWindow</code><br><code>PartyWindow(Plugin plugin)</code> | 생성자 | Plugin 참조와 창 크기·제약 등 ImGui window 설정을 초기화한다. | [L23](../../src/PartyWindow.cs#L23) |
| <a id="symbol-src-partywindow-cs-31-84955a83d8"></a><code>PartyWindow.Draw</code><br><code>void Draw()</code> | 메서드 | 전체 창의 조회·갱신·대기 변경 버튼, 파티/모집글/연결 패널과 편집창을 그린다. | [L31](../../src/PartyWindow.cs#L31) |
| <a id="symbol-src-partywindow-cs-57-7c8ea2c278"></a><code>PartyWindow.DrawAfk</code><br><code>void DrawAfk()</code> | 메서드 | AFK 토글·상태와 주기 조회/실패 재시도·단일 직업 빈자리 제외 완료 알림 선택을 표시하고 저장한다. AFK 활성화 자체는 세션 선택이다. | [L57](../../src/PartyWindow.cs#L57) |
| <a id="symbol-src-partywindow-cs-94-7fa94793b3"></a><code>PartyWindow.DrawBrowser</code><br><code>void DrawBrowser()</code> | 메서드 | 같은 PC 웹 연결의 켜기/끄기, 주소·인증 링크와 복사 UI를 표시한다. | [L94](../../src/PartyWindow.cs#L94) |
| <a id="symbol-src-partywindow-cs-109-0e50a5b0bb"></a><code>PartyWindow.DrawRelay</code><br><code>void DrawRelay()</code> | 메서드 | 중계 서버 주소·연결 키·외부 링크와 연결 상태를 표시하고 relay 시작/종료를 요청한다. | [L109](../../src/PartyWindow.cs#L109) |
| <a id="symbol-src-partywindow-cs-142-37c319faf6"></a><code>PartyWindow.DrawDiscord</code><br><code>void DrawDiscord()</code> | 메서드 | 토큰 연결/저장, 초대 링크, 알림·채팅 상태와 종류별 인게임 수신/멘션 토글을 표시한다. | [L142](../../src/PartyWindow.cs#L142) |
| <a id="symbol-src-partywindow-cs-169-3b3e541dad"></a><code>PartyWindow.DrawDiscordChatControls</code><br><code>void DrawDiscordChatControls()</code> | 메서드 | 각 종류의 수신 체크박스와 멘션 끄기/항상/효과음만 선택·저장 채널을 표시한다. 귓속말은 효과음만 선택지를 제공하지 않는다. 준비/저장 상태를 확인하고 Plugin으로 선택을 넘긴다. | [L169](../../src/PartyWindow.cs#L169) |
| <a id="symbol-src-partywindow-cs-205-ab200df0a4"></a><code>PartyWindow.DrawCurrentParty</code><br><code>void DrawCurrentParty()</code> | 메서드 | PartySnapshot의 현재 멤버·직업·월드·stale 상태를 표시한다. 모집 등록 직업 화면과 별개다. | [L205](../../src/PartyWindow.cs#L205) |
| <a id="symbol-src-partywindow-cs-236-230a96c39e"></a><code>PartyWindow.DrawListing</code><br><code>void DrawListing()</code> | 메서드 | 상세 모집의 소유자·임무·시간·조건·소개말·등록 자리를 표시하고 개별 편집 진입 UI를 연결한다. | [L236](../../src/PartyWindow.cs#L236) |
| <a id="symbol-src-partywindow-cs-315-a3d5acdec8"></a><code>PartyWindow.DrawDescription</code><br><code>void DrawDescription(ListingSnapshot listing)</code> | 메서드 | 조회된 소개말의 표시값과 편집 진입 버튼을 그린다. | [L315](../../src/PartyWindow.cs#L315) |
| <a id="symbol-src-partywindow-cs-337-e597606380"></a><code>PartyWindow.DrawDescriptionEditor</code><br><code>void DrawDescriptionEditor()</code> | 메서드 | 소개말 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyDescriptionDraft에 대기 변경을 전달한다. | [L337](../../src/PartyWindow.cs#L337) |
| <a id="symbol-src-partywindow-cs-378-d256c30108"></a><code>PartyWindow.DrawListingSlots</code><br><code>void DrawListingSlots(ListingSnapshot listing)</code> | 메서드 | 등록된 자리만 순회하고 인원수·그룹/멤버 위치·미등록 매핑 진단을 표시한다. | [L378](../../src/PartyWindow.cs#L378) |
| <a id="symbol-src-partywindow-cs-421-fa81a39348"></a><code>PartyWindow.DrawObjective</code><br><code>void DrawObjective(ListingSnapshot listing)</code> | 메서드 | 조회된 모집 목적의 표시값과 편집 진입 버튼을 그린다. | [L421](../../src/PartyWindow.cs#L421) |
| <a id="symbol-src-partywindow-cs-433-f18c308c7e"></a><code>PartyWindow.DrawBeginner</code><br><code>void DrawBeginner(ListingSnapshot listing)</code> | 메서드 | 조회된 초행 및 초보자 환영의 표시값과 편집 진입 버튼을 그린다. | [L433](../../src/PartyWindow.cs#L433) |
| <a id="symbol-src-partywindow-cs-445-bf8d2527a1"></a><code>PartyWindow.DrawBeginnerEditor</code><br><code>void DrawBeginnerEditor()</code> | 메서드 | 초행 및 초보자 환영 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyBeginnerDraft에 대기 변경을 전달한다. | [L445](../../src/PartyWindow.cs#L445) |
| <a id="symbol-src-partywindow-cs-478-7ab9b731dd"></a><code>PartyWindow.DrawUniqueJobs</code><br><code>void DrawUniqueJobs(ListingSnapshot listing)</code> | 메서드 | 조회된 잡 중복 없음의 표시값과 편집 진입 버튼을 그린다. | [L478](../../src/PartyWindow.cs#L478) |
| <a id="symbol-src-partywindow-cs-490-0ecfc6bf79"></a><code>PartyWindow.DrawUniqueJobsEditor</code><br><code>void DrawUniqueJobsEditor()</code> | 메서드 | 잡 중복 없음 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyUniqueJobsDraft에 대기 변경을 전달한다. | [L490](../../src/PartyWindow.cs#L490) |
| <a id="symbol-src-partywindow-cs-523-2938bff9df"></a><code>PartyWindow.DrawPrivate</code><br><code>void DrawPrivate(ListingSnapshot listing)</code> | 메서드 | 조회된 비공개/PIN의 표시값과 편집 진입 버튼을 그린다. | [L523](../../src/PartyWindow.cs#L523) |
| <a id="symbol-src-partywindow-cs-536-0527ffea19"></a><code>PartyWindow.DrawPrivateEditor</code><br><code>void DrawPrivateEditor()</code> | 메서드 | 비공개/PIN 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyPrivateDraft에 대기 변경을 전달한다. | [L536](../../src/PartyWindow.cs#L536) |
| <a id="symbol-src-partywindow-cs-580-3946956427"></a><code>PartyWindow.DrawCompletion</code><br><code>void DrawCompletion(ListingSnapshot listing)</code> | 메서드 | 조회된 완료조건의 표시값과 편집 진입 버튼을 그린다. | [L580](../../src/PartyWindow.cs#L580) |
| <a id="symbol-src-partywindow-cs-593-f9de71fac6"></a><code>PartyWindow.DrawCompletionEditor</code><br><code>void DrawCompletionEditor()</code> | 메서드 | 완료조건 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyCompletionDraft에 대기 변경을 전달한다. | [L593](../../src/PartyWindow.cs#L593) |
| <a id="symbol-src-partywindow-cs-640-ac49fbf99d"></a><code>PartyWindow.DrawItemLevel</code><br><code>void DrawItemLevel(ListingSnapshot listing)</code> | 메서드 | 조회된 평균 아이템 레벨의 표시값과 편집 진입 버튼을 그린다. | [L640](../../src/PartyWindow.cs#L640) |
| <a id="symbol-src-partywindow-cs-653-d636692def"></a><code>PartyWindow.DrawItemLevelEditor</code><br><code>void DrawItemLevelEditor()</code> | 메서드 | 평균 아이템 레벨 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyItemLevelDraft에 대기 변경을 전달한다. | [L653](../../src/PartyWindow.cs#L653) |
| <a id="symbol-src-partywindow-cs-695-c542d5829b"></a><code>PartyWindow.DrawRule</code><br><code>void DrawRule(ListingSnapshot listing, ListingRuleKind kind)</code> | 메서드 | 조회된 임무 찾기/전리품 규칙의 표시값과 편집 진입 버튼을 그린다. | [L695](../../src/PartyWindow.cs#L695) |
| <a id="symbol-src-partywindow-cs-709-d3b4c6a33f"></a><code>PartyWindow.DrawRuleEditor</code><br><code>void DrawRuleEditor()</code> | 메서드 | 임무 찾기/전리품 규칙 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyRuleDraft에 대기 변경을 전달한다. | [L709](../../src/PartyWindow.cs#L709) |
| <a id="symbol-src-partywindow-cs-753-8a92da6ee6"></a><code>PartyWindow.DrawObjectiveTrace</code><br><code>void DrawObjectiveTrace()</code> | 메서드 | 수동 이벤트 기록의 시작/종료 및 진단 복사 UI를 그린다. | [L753](../../src/PartyWindow.cs#L753) |
| <a id="symbol-src-partywindow-cs-772-622748aad6"></a><code>PartyWindow.DrawObjectiveEditor</code><br><code>void DrawObjectiveEditor()</code> | 메서드 | 모집 목적 초안의 입력·유효성·적용 버튼을 그려 Plugin.ApplyObjectiveDraft에 대기 변경을 전달한다. | [L772](../../src/PartyWindow.cs#L772) |
| <a id="symbol-src-partywindow-cs-814-77e346de14"></a><code>PartyWindow.DrawListingSlotJob</code><br><code>void DrawListingSlotJob(ListingSlot slot, ListingSnapshot listing)</code> | 메서드 | 입장 직업/직업 제한 라벨과 마우스오버 허용 직업 목록을 표시한다. | [L814](../../src/PartyWindow.cs#L814) |
| <a id="symbol-src-partywindow-cs-852-e2b9039006"></a><code>PartyWindow.DrawJobEditor</code><br><code>void DrawJobEditor()</code> | 메서드 | 직업 초안 선택 UI와 적용 가능 여부를 표시하고 대기 변경으로 추가한다. | [L852](../../src/PartyWindow.cs#L852) |
| <a id="symbol-src-partywindow-cs-901-b7a948ff14"></a><code>PartyWindow.Cell</code><br><code>void Cell(string text)</code> | 메서드 | 표 셀을 이동하고 지정 문자열을 출력하는 공통 그리기 helper다. | [L901](../../src/PartyWindow.cs#L901) |

<a id="file-src-plugin-afk-cs"></a>
## src/Plugin.Afk.cs

[소스 파일](../../src/Plugin.Afk.cs) · 명명된 항목/진입점 20개

인게임 AFK 토글과 순수 monitor를 실제 Reader/기존 조회 guard에 연결한다. 별도의 지역 이동 상태 기계나 자동 갱신/알림 전송 경로를 만들지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-afk-cs-10-905d17be5b"></a><code>Plugin.AfkOptions</code><br><code>AfkSettings AfkOptions</code> | 계산 속성 | 저장 설정을 최초1회 읽어 monitor의 현재 정책을 반환한다. | [L10](../../src/Plugin.Afk.cs#L10) |
| <a id="symbol-src-plugin-afk-cs-11-9998af65e4"></a><code>Plugin.AfkEnabled</code><br><code>bool AfkEnabled</code> | 계산 속성 | 현재 실행의 AFK 활성 여부를 반환한다. 게임 자체의 자리비움 상태와 별개의 PVKR 토글이다. 계산식: <code>afk.Enabled</code>. | [L11](../../src/Plugin.Afk.cs#L11) |
| <a id="symbol-src-plugin-afk-cs-12-edb3c0617b"></a><code>Plugin.AfkStatus</code><br><code>string AfkStatus</code> | 계산 속성 | 자동 조회의 감시/대기/실패/재시도 상태를 인게임 UI에 제공한다. 계산식: <code>afk.Status</code>. | [L12](../../src/Plugin.Afk.cs#L12) |
| <a id="symbol-src-plugin-afk-cs-14-5dd17e23b1"></a><code>Plugin.SetAfkEnabled</code><br><code>void SetAfkEnabled(bool enabled)</code> | 메서드 | AFK 상태 기계를 켜거나 끄고 읽기를 예약한다. 채팅 전달의 수신/멘션 상태를 변경하지 않는다. | [L14](../../src/Plugin.Afk.cs#L14) |
| <a id="symbol-src-plugin-afk-cs-23-6d5cad3321"></a><code>Plugin.LoadAfkSettings</code><br><code>void LoadAfkSettings()</code> | 메서드 | RelayConfiguration의 AFK 정책을 로드한다. 실패 시 기본값과 읽기 오류를 표시하며 AFK 자체를 자동 활성화하지 않는다. | [L23](../../src/Plugin.Afk.cs#L23) |
| <a id="symbol-src-plugin-afk-cs-34-b677e02741"></a><code>Plugin.SaveAfkSettings</code><br><code>void SaveAfkSettings(AfkSettings settings)</code> | 메서드 | 같은 설정 객체의 연결 정보를 보존하면서 AFK 정책을 저장하고 성공한 정책만 monitor에 반영한다. 실패 문구를 인게임 UI에 남긴다. | [L34](../../src/Plugin.Afk.cs#L34) |
| <a id="symbol-src-plugin-afk-cs-46-c211e5c79d"></a><code>Plugin.AfkHost</code><br><code>AfkHost(Plugin plugin)</code> | 주 생성자/모델 | Plugin의 Reader·Listing·조회 guard를 IAfkListingHost 계약으로 연결하는 게임 스레드 어댑터다. | [L46](../../src/Plugin.Afk.cs#L46) |
| <a id="symbol-src-plugin-afk-cs-48-051eabae8a"></a><code>Plugin.AfkHost.LoggedIn</code><br><code>bool LoggedIn</code> | 계산 속성 | 실제 ClientState 로그인 상태를 반환한다. PlayerState가 잠시 준비되지 않은 상태와 구분한다. 계산식: <code>ClientState.IsLoggedIn</code>. | [L48](../../src/Plugin.Afk.cs#L48) |
| <a id="symbol-src-plugin-afk-cs-49-c775572a43"></a><code>Plugin.AfkHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | 로그인 및 PlayerState 준비 시 현재 CID, 그 외0을 반환한다. 계산식: <code>LoggedIn &amp;&amp; PlayerState.IsLoaded ? PlayerState.ContentId : 0</code>. | [L49](../../src/Plugin.Afk.cs#L49) |
| <a id="symbol-src-plugin-afk-cs-50-f9070bb079"></a><code>Plugin.AfkHost.TryReadRoster</code><br><code>bool TryReadRoster(out PartyRoster? roster)</code> | 메서드 | AFK 전용 전체 파티 CID reader를 호출한다. | [L50](../../src/Plugin.Afk.cs#L50) |
| <a id="symbol-src-plugin-afk-cs-51-583c6c6119"></a><code>Plugin.AfkHost.QueryBlockReason</code><br><code>string? QueryBlockReason(bool ownQueryInProgress)</code> | 메서드 | 기존 GetQueryUnavailableReason에 적용 대기 변경 및 갱신 cooldown을 더해 자동 조회를 미룬다. | [L51](../../src/Plugin.Afk.cs#L51) |
| <a id="symbol-src-plugin-afk-cs-54-66388644a8"></a><code>Plugin.AfkHost.QueryBusy</code><br><code>bool QueryBusy</code> | 계산 속성 | ListingReader의 대기/진행 여부를 반환한다. 계산식: <code>plugin.listingReader.Busy</code>. | [L54](../../src/Plugin.Afk.cs#L54) |
| <a id="symbol-src-plugin-afk-cs-55-4c56896548"></a><code>Plugin.AfkHost.QuerySucceeded</code><br><code>bool QuerySucceeded</code> | 계산 속성 | ListingReader가 새로운 상세 응답을 관측했는지 반환한다. 이것만으로 소유권/명단이 확인된 것은 아니다. 계산식: <code>plugin.listingReader.LastOwnRequestSucceeded</code>. | [L55](../../src/Plugin.Afk.cs#L55) |
| <a id="symbol-src-plugin-afk-cs-56-0e6a07b0e6"></a><code>Plugin.AfkHost.RequestRevision</code><br><code>long RequestRevision</code> | 계산 속성 | 현재 조회 요청 serial을 제공해 AFK/수동/외부 작업의 취소 소유권을 분리한다. 계산식: <code>plugin.listingReader.RequestRevision</code>. | [L56](../../src/Plugin.Afk.cs#L56) |
| <a id="symbol-src-plugin-afk-cs-57-094b473b79"></a><code>Plugin.AfkHost.DetailRevision</code><br><code>long DetailRevision</code> | 계산 속성 | 상세창 갱신 revision을 제공해 이전 결과 재사용을 거부한다. 계산식: <code>plugin.listingReader.DetailRevision</code>. | [L57](../../src/Plugin.Afk.cs#L57) |
| <a id="symbol-src-plugin-afk-cs-58-652e6b3c27"></a><code>Plugin.AfkHost.RegisteredCapacity</code><br><code>int RegisteredCapacity</code> | 계산 속성 | 현재 모집 상세의 CountsAgree와 MappingValid가 모두 참일 때 실제 등록 정원만 반환한다. 미등록과 친선 padding은 제외된다. 계산식: <code>plugin.Listing.Slots is { CountsAgree: true, MappingValid: true } slots             ? slots.TotalSlots : 0</code>. | [L58](../../src/Plugin.Afk.cs#L58) |
| <a id="symbol-src-plugin-afk-cs-60-30d3e52d94"></a><code>Plugin.AfkHost.RegisteredSlots</code><br><code>ListingSlots RegisteredSlots</code> | 계산 속성 | 현재 본인 상세의 관리형 자리 snapshot을 노출한다. monitor는 새 응답 revision과 OwnListingMatches 확인 후에만 사용한다. 계산식: <code>plugin.Listing.Slots</code>. | [L60](../../src/Plugin.Afk.cs#L60) |
| <a id="symbol-src-plugin-afk-cs-61-f8a99e0feb"></a><code>Plugin.AfkHost.OwnListingMatches</code><br><code>bool OwnListingMatches(PartyRoster roster)</code> | 메서드 | 현재 Live/Self·Viewer CID·슬롯 인원 일치와 모든 참가 CID의 완전성을 검사하고 정렬한 명단을 비교한다. 등록 직업을 현재 직업으로 덮어쓰지 않는다. | [L61](../../src/Plugin.Afk.cs#L61) |
| <a id="symbol-src-plugin-afk-cs-68-6a9279f18c"></a><code>Plugin.AfkHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 메서드 | Plugin.TryQueryOwnListing 공통 예약 경로를 사용한다. 수정창 열기나 갱신 제출은 하지 않는다. | [L68](../../src/Plugin.Afk.cs#L68) |
| <a id="symbol-src-plugin-afk-cs-69-b134950752"></a><code>Plugin.AfkHost.CancelQuery</code><br><code>void CancelQuery(long revision)</code> | 메서드 | busy이며 현재 reader request revision이 인자와 같을 때만 FailRequest로 취소한다. | [L69](../../src/Plugin.Afk.cs#L69) |

<a id="file-src-plugin-chat-cs"></a>
## src/Plugin.Chat.cs

[소스 파일](../../src/Plugin.Chat.cs) · 명명된 항목/진입점 10개

공개 채팅 이벤트와 Discord 큐를 로그인/CID 기반으로 연결한다. 인게임의 종류별 수신/멘션 토글과 홈 서버 보완을 제공하며 AFK 상태에 의존하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-chat-cs-13-b4581ffe15"></a><code>Plugin.DiscordChatStatus</code><br><code>string DiscordChatStatus</code> | 계산 속성 | 전송 통계·공개 채팅 읽기 오류·인게임 설정 저장 결과를 표시한다. 계산식: <code>(discordClient?.ChatStatus ?? "") +         (chatCaptureError.Length == 0 ? "" : "\n" + chatCaptureError) +         (chatSettingStatus.Length == 0 ? "" : "\n" + chatSettingStatus)</code>. | [L13](../../src/Plugin.Chat.cs#L13) |
| <a id="symbol-src-plugin-chat-cs-16-90ea493b1d"></a><code>Plugin.CanConfigureDiscordChat</code><br><code>bool CanConfigureDiscordChat</code> | 계산 속성 | 이전 인게임 저장이 끝났고 연결 준비 중이 아닌 경우에만 토글 조작을 허용한다. 계산식: <code>chatSettingTask?.IsCompleted != false &amp;&amp;         (discordClient == null &#124;&#124; !discordClient.Running &#124;&#124; discordClient.ChatEditable)</code>. | [L16](../../src/Plugin.Chat.cs#L16) |
| <a id="symbol-src-plugin-chat-cs-18-320e5c1ef3"></a><code>Plugin.DiscordChatSettings</code><br><code>DiscordChatSettings DiscordChatSettings</code> | 계산 속성 | 실행 중인 controller의 현재 설정 snapshot 또는 저장한 앱에 해당하는 정리된 설정을 UI로 제공한다. | [L18](../../src/Plugin.Chat.cs#L18) |
| <a id="symbol-src-plugin-chat-cs-27-a7ae955c80"></a><code>Plugin.UpdateDiscordChat</code><br><code>void UpdateDiscordChat()</code> | 메서드 | 로그인/PlayerState에서 AFK와 독립적인 GameChatSession을 갱신한다. 홈 서버는 RuntimeRows로 읽고 캐릭터 CID만 Discord controller에 전달한다. | [L27](../../src/Plugin.Chat.cs#L27) |
| <a id="symbol-src-plugin-chat-cs-40-592e801b65"></a><code>Plugin.InitializeChatCapture</code><br><code>void InitializeChatCapture()</code> | 메서드 | 런타임 공개 ChatMessage 이벤트를 연결한다. 읽기 오류는 진단과 함께 답장 대상을 무효화한다. | [L40](../../src/Plugin.Chat.cs#L40) |
| <a id="symbol-src-plugin-chat-cs-52-42d1abd77a"></a><code>Plugin.WantsCapturedChat</code><br><code>bool WantsCapturedChat(GameChatKind kind)</code> | 메서드 | 로그인/홈 서버 상태를 갱신하고 활성 전달 경로 또는 연결 중 봇의 받은 귓속말인지 검사한다. AFK 조건은 없다. | [L52](../../src/Plugin.Chat.cs#L52) |
| <a id="symbol-src-plugin-chat-cs-57-56fff53a3d"></a><code>Plugin.ReceiveCapturedChat</code><br><code>void ReceiveCapturedChat(GameChatMessage message)</code> | 메서드 | 수신 귓속말이면 답장 상대를 갱신한 뒤 기존 채널 전달 큐로 넘긴다. 네트워크 작업은 하지 않는다. | [L57](../../src/Plugin.Chat.cs#L57) |
| <a id="symbol-src-plugin-chat-cs-63-c1157a3522"></a><code>Plugin.SetDiscordChat</code><br><code>void SetDiscordChat(GameChatKind kind, bool? enabled = null, bool? mention = null, bool? sound = null)</code> | 메서드 | 중복 조작/종료를 막고 nullable 수신·멘션 변경을 비동기로 시작한다. UI/game thread를 저장 완료까지 막지 않는다. | [L63](../../src/Plugin.Chat.cs#L63) |
| <a id="symbol-src-plugin-chat-cs-69-3a5d55bf0e"></a><code>Plugin.ChangeDiscordChat</code><br><code>Task ChangeDiscordChat(GameChatKind kind, bool? enabled, bool? mention, bool? sound)</code> | 메서드 | 실행 중에는 controller의 직렬화된 최신 설정 변경을 기다린다. 연결이 꺼져 있으면 기존 앱/채널을 유지한 채 설정만 저장한다. 채널이 미설정이면 명령 설정을 안내하고 오류는 타입만 표시한다. | [L69](../../src/Plugin.Chat.cs#L69) |
| <a id="symbol-src-plugin-chat-cs-92-f63c882018"></a><code>Plugin.SaveDiscordChatSettings</code><br><code>Task SaveDiscordChatSettings(DiscordChatSettings settings, CancellationToken cancellation)</code> | 메서드 | Framework에서 취소/종료를 재검사하고 최신 RelayConfiguration의 DiscordChat만 복사해 저장한다. 기존 토큰·알림 목적지·AFK 설정은 보존한다. | [L92](../../src/Plugin.Chat.cs#L92) |

<a id="file-src-plugin-chatsend-cs"></a>
## src/Plugin.ChatSend.cs

[소스 파일](../../src/Plugin.ChatSend.cs) · 명명된 항목/진입점 6개

게임 전송 native 경계. 호환/접속/지역 이동과 실제 최근 귓속말 이력을 검사하고 UIModule 채팅 입력을 호출한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-chatsend-cs-10-2ddf93c9da"></a><code>Plugin.UpdateChatSending</code><br><code>void UpdateChatSending(long now)</code> | 메서드 | 봇이 실행 중이면 매 Framework tick에 전송 bridge를 진행한다. 모집조회500ms 간격과 분리한다. | [L10](../../src/Plugin.ChatSend.cs#L10) |
| <a id="symbol-src-plugin-chatsend-cs-15-9f4bcd9299"></a><code>Plugin.ChatSendHost</code><br><code>ChatSendHost(Plugin plugin)</code> | 주 생성자/모델 | Plugin의 현재 서비스와 서버 이름 resolver를 전송 host에 연결한다. | [L15](../../src/Plugin.ChatSend.cs#L15) |
| <a id="symbol-src-plugin-chatsend-cs-17-c107148cc3"></a><code>Plugin.ChatSendHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | ClientState 로그인과 PlayerState 로드가 모두 확인될 때 본인 CID를 반환한다. 계산식: <code>ClientState.IsLoggedIn &amp;&amp; PlayerState.IsLoaded ? PlayerState.ContentId : 0</code>. | [L17](../../src/Plugin.ChatSend.cs#L17) |
| <a id="symbol-src-plugin-chatsend-cs-18-eca8832503"></a><code>Plugin.ChatSendHost.BlockReason</code><br><code>string? BlockReason</code> | 계산 속성 | 종료·native ABI 불일치·지역 이동을 전송 차단 사유로 반환한다. | [L18](../../src/Plugin.ChatSend.cs#L18) |
| <a id="symbol-src-plugin-chatsend-cs-22-f92a54d81b"></a><code>Plugin.ChatSendHost.TrySend</code><br><code>bool TrySend(string command, ChatReplyTarget? reply, out string error)</code> | 메서드 | 실행 직전 접속·UIModule·답장 이력을 다시 확인한다. Utf8String sanitize가 원문을 바꾸면 중단하고 ProcessChatBoxEntry로1회 넘긴 뒤 native 문자열을 해제한다. | [L22](../../src/Plugin.ChatSend.cs#L22) |
| <a id="symbol-src-plugin-chatsend-cs-46-f8590e544f"></a><code>Plugin.ChatSendHost.ReplyMatches</code><br><code>bool ReplyMatches(ChatReplyTarget expected)</code> | 메서드 | AcquaintanceModule의 유효 개수와 GetTellHistory(0)를 읽어 CID·서버ID·이름·해석 서버가 고정한 상대와 맞는지 검사한다. 이 검사는 모집 문의의 서버 답장 권한을 보장하지 않는다. | [L46](../../src/Plugin.ChatSend.cs#L46) |

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
