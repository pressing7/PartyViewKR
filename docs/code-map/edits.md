# 편집 모델·대기 변경·순수 UI 상태 기계

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

`Draft`는 UI 입력, `Edit`는 원본에 묶인 요청, `Expected`는 관리형 예상 결과다. 이 장의 코드는 native 호출 없이 값을 검증/변환한다. `Supports`와 실제 게임 컨트롤 활성화 여부를 구분한다.

기준: 0.3.49.0 소스. 기반 main `a34099802f12d6e5c4859489472489319d146e4f` 이후 AFK 설정·Discord 수정창 닫기 변경을 포함한다. 2026-10-25 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [src/ListingBatchSnapshot.cs](#file-src-listingbatchsnapshot-cs)
- [src/ListingBeginnerEdit.cs](#file-src-listingbeginneredit-cs)
- [src/ListingChange.cs](#file-src-listingchange-cs)
- [src/ListingCheckboxUi.cs](#file-src-listingcheckboxui-cs)
- [src/ListingCompletionEdit.cs](#file-src-listingcompletionedit-cs)
- [src/ListingDescriptionEdit.cs](#file-src-listingdescriptionedit-cs)
- [src/ListingDescriptionRecovery.cs](#file-src-listingdescriptionrecovery-cs)
- [src/ListingItemLevelEdit.cs](#file-src-listingitemleveledit-cs)
- [src/ListingJobEdit.cs](#file-src-listingjobedit-cs)
- [src/ListingLootUi.cs](#file-src-listinglootui-cs)
- [src/ListingObjectiveEdit.cs](#file-src-listingobjectiveedit-cs)
- [src/ListingObjectiveUi.cs](#file-src-listingobjectiveui-cs)
- [src/ListingPrivateEdit.cs](#file-src-listingprivateedit-cs)
- [src/ListingRoleButtons.cs](#file-src-listingrolebuttons-cs)
- [src/ListingRuleEdit.cs](#file-src-listingruleedit-cs)
- [src/ListingUniqueJobsEdit.cs](#file-src-listinguniquejobsedit-cs)

<a id="file-src-listingbatchsnapshot-cs"></a>
## src/ListingBatchSnapshot.cs

[소스 파일](../../src/ListingBatchSnapshot.cs) · 명명된 항목/진입점 1개

수정창의 누적 실제 조건을 검증 가능한 RefreshTarget으로 재구성한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingbatchsnapshot-cs-7-c99f0b9ef5"></a><code>ListingBatchSnapshot.TryRead</code><br><code>bool TryRead(RefreshTarget identity, string criteria, string description, string extra, out RefreshTarget current, out string error)</code> | 메서드 | criteria의 raw 슬롯 배열을 decode하고 미등록 슬롯이0인지 확인한다. 검증된 친선 padding0xFDFFFFFE만 관리형0으로 정규화한 뒤 등록 자리 mask·소개말·추가조건을 current로 만든다. 캐릭터/임무/멤버 검증은 호출하는 Host의 책임이다. | [L7](../../src/ListingBatchSnapshot.cs#L7) |

<a id="file-src-listingbeginneredit-cs"></a>
## src/ListingBeginnerEdit.cs

[소스 파일](../../src/ListingBeginnerEdit.cs) · 명명된 항목/진입점 13개

초행 및 초보자 환영의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingBeginnerEditTests.cs](tests.md#file-tests-listingbeginneredittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingbeginneredit-cs-7-e6feba1209"></a><code>ListingBeginners.Decode</code><br><code>bool? Decode(byte value)</code> | 메서드 | 초보자 환영 stored byte를 공통 0/1/null 규칙으로 해석한다. | [L7](../../src/ListingBeginnerEdit.cs#L7) |
| <a id="symbol-src-listingbeginneredit-cs-8-29fcfeb3ae"></a><code>ListingBeginners.Label</code><br><code>string Label(bool? value)</code> | 메서드 | 해당 설정의 저장값을 한국어 표시명으로 변환하고 미지원/알 수 없음도 구분한다. | [L8](../../src/ListingBeginnerEdit.cs#L8) |
| <a id="symbol-src-listingbeginneredit-cs-9-b1ea2a6c91"></a><code>ListingBeginners.Read</code><br><code>bool? Read(string criteria)</code> | 메서드 | Criteria의 네 번째 필드에서 초보자 환영의 bool?을 읽는다. | [L9](../../src/ListingBeginnerEdit.cs#L9) |
| <a id="symbol-src-listingbeginneredit-cs-17-e91d6d5c66"></a><code>ListingBeginnerDraft</code><br><code>ListingBeginnerDraft(RefreshTarget original)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L17](../../src/ListingBeginnerEdit.cs#L17) |
| <a id="symbol-src-listingbeginneredit-cs-21-98bbd0065b"></a><code>ListingBeginnerDraft.Request</code><br><code>ListingBeginnerEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, Enabled)</code>. | [L21](../../src/ListingBeginnerEdit.cs#L21) |
| <a id="symbol-src-listingbeginneredit-cs-24-c20b474dd1"></a><code>ListingBeginnerEdit</code><br><code>ListingBeginnerEdit(RefreshTarget Original, bool Enabled)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L24](../../src/ListingBeginnerEdit.cs#L24) |
| <a id="symbol-src-listingbeginneredit-cs-26-2a9112d824"></a><code>ListingBeginnerEdit.Value</code><br><code>byte Value</code> | 계산 속성 | 사용자 입력을 해당 필드의 유효한 저장값으로 변환한다. 유효하지 않으면 null 또는 모델 규칙에 따른 값을 반환한다. 계산식: <code>Enabled ? (byte)1 : (byte)0</code>. | [L26](../../src/ListingBeginnerEdit.cs#L26) |
| <a id="symbol-src-listingbeginneredit-cs-27-92d649977a"></a><code>ListingBeginnerEdit.Changed</code><br><code>bool Changed</code> | 계산 속성 | 요청한 값이 바인딩한 원본 값과 실제로 다른지 검사한다. 계산식: <code>ListingBeginners.Read(Original.Criteria) is { } original &amp;&amp; original != Enabled</code>. | [L27](../../src/ListingBeginnerEdit.cs#L27) |
| <a id="symbol-src-listingbeginneredit-cs-28-4a89c0473f"></a><code>ListingBeginnerEdit.Supports</code><br><code>bool Supports(RefreshTarget target)</code> | 메서드 | 현재 source의 지원 layout/읽을 수 있는 해당 조건을 검사한다. 실제 게임 control enabled 검사는 별도 Host 단계다. | [L28](../../src/ListingBeginnerEdit.cs#L28) |
| <a id="symbol-src-listingbeginneredit-cs-29-318c775740"></a><code>ListingBeginnerEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. 계산식: <code>Supports(Original) &amp;&amp; Changed</code>. | [L29](../../src/ListingBeginnerEdit.cs#L29) |
| <a id="symbol-src-listingbeginneredit-cs-30-c281cc8391"></a><code>ListingBeginnerEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L30](../../src/ListingBeginnerEdit.cs#L30) |
| <a id="symbol-src-listingbeginneredit-cs-34-c61f49ee2b"></a><code>ListingBeginnerEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget before)</code> | 메서드 | 현재 before snapshot의 해당 필드만 요청값으로 바꾼 새 RefreshTarget을 만든다. 네이티브 메모리를 쓰지 않는다. | [L34](../../src/ListingBeginnerEdit.cs#L34) |
| <a id="symbol-src-listingbeginneredit-cs-42-6d9c38856d"></a><code>ListingBeginnerEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L42](../../src/ListingBeginnerEdit.cs#L42) |

<a id="file-src-listingchange-cs"></a>
## src/ListingChange.cs

[소스 파일](../../src/ListingChange.cs) · 명명된 항목/진입점 21개

개별 typed edit를 공통 배치 항목으로 연결하고 같은 원본의 변경을 필드/자리별로 중복 없이 보관한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingchange-cs-5-5643f97b9f"></a><code>ListingChange</code><br><code>ListingChange(object edit)</code> | 주 생성자/모델 | 지원하는 typed edit를 공통 배치 항목으로 감싼다. key/표시/실행/검증을 실제 edit 타입으로 분기한다. | [L5](../../src/ListingChange.cs#L5) |
| <a id="symbol-src-listingchange-cs-8-1f0bd9a61f"></a><code>ListingChange.Original</code><br><code>RefreshTarget Original</code> | 계산 속성 | 해당 변경이 처음 참조한 모집 snapshot을 반환한다. | [L8](../../src/ListingChange.cs#L8) |
| <a id="symbol-src-listingchange-cs-15-ddee134d1f"></a><code>ListingChange.NativeIndex</code><br><code>int NativeIndex</code> | 계산 속성 | 표시 번호와 구분되는 0-based 물리 슬롯 인덱스를 반환한다. 계산식: <code>Edit is ListingJobEdit e &amp;&amp; e.Position &gt; 0 &amp;&amp; e.Position &lt;= e.Original.Slots.Slots.Length         ? e.Original.Slots.Slots[e.Position - 1].NativeIndex : -1</code>. | [L15](../../src/ListingChange.cs#L15) |
| <a id="symbol-src-listingchange-cs-17-f89303975a"></a><code>ListingChange.Key</code><br><code>string Key</code> | 계산 속성 | 필드 종류 또는 직업 NativeIndex로 대기 변경을 식별할 고유 key를 만든다. | [L17](../../src/ListingChange.cs#L17) |
| <a id="symbol-src-listingchange-cs-21-2cfed0b820"></a><code>ListingChange.Title</code><br><code>string Title</code> | 계산 속성 | 변경 항목의 사용자용 제목을 만든다. | [L21](../../src/ListingChange.cs#L21) |
| <a id="symbol-src-listingchange-cs-28-734b72cbf1"></a><code>ListingChange.ValueLabel</code><br><code>string ValueLabel</code> | 계산 속성 | 변경 요청값의 사용자용 설명을 만든다. PIN은 표시하지 않는다. | [L28](../../src/ListingChange.cs#L28) |
| <a id="symbol-src-listingchange-cs-39-22edede041"></a><code>ListingChange.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. | [L39](../../src/ListingChange.cs#L39) |
| <a id="symbol-src-listingchange-cs-46-010086ea4d"></a><code>ListingChange.IsPrivatePassword</code><br><code>bool IsPrivatePassword</code> | 계산 속성 | 별도 PIN 검증이 필요한 비공개 활성화 요청인지 판정한다. 계산식: <code>Edit is ListingPrivateEdit { Enabled: true }</code>. | [L46](../../src/ListingChange.cs#L46) |
| <a id="symbol-src-listingchange-cs-48-46c1329ace"></a><code>ListingChange.Rebase</code><br><code>ListingChange Rebase(RefreshTarget target)</code> | 메서드 | 요청한 값은 유지하면서 비교 기준 Original을 현재 누적 draft로 교체한다. 직업은 NativeIndex로 새 display position을 다시 찾는다. | [L48](../../src/ListingChange.cs#L48) |
| <a id="symbol-src-listingchange-cs-60-841160c0b0"></a><code>ListingChange.MatchesListing</code><br><code>bool MatchesListing(RefreshTarget target)</code> | 메서드 | 해당 항목의 최종 요청값이 실제 모집글에 반영됐는지 필드별로 판정한다. PIN은 서버 상세로 검증할 수 없다. | [L60](../../src/ListingChange.cs#L60) |
| <a id="symbol-src-listingchange-cs-73-8ae7fe5de5"></a><code>ListingChange.Begin</code><br><code>bool Begin(IListingRefreshHost host, out string error)</code> | 메서드 | Edit의 실제 타입에 맞는 IListingRefreshHost.Begin*Edit를 호출한다. | [L73](../../src/ListingChange.cs#L73) |
| <a id="symbol-src-listingchange-cs-85-4ca64df7bc"></a><code>ListingChange.Update</code><br><code>SlotEditProgress Update(IListingRefreshHost host, out RefreshTarget expected, out string error)</code> | 메서드 | Edit의 실제 타입에 맞는 Update*Edit 결과와 다음 RefreshTarget을 반환한다. | [L85](../../src/ListingChange.cs#L85) |
| <a id="symbol-src-listingchange-cs-97-4f8353503b"></a><code>ListingChange.ToString</code><br><code>string ToString()</code> | 메서드 | 변경 Key만 반환한다. 비밀번호를 지닌 Edit 객체를 문자열화하지 않는다. | [L97](../../src/ListingChange.cs#L97) |
| <a id="symbol-src-listingchange-cs-103-bf124c5d5d"></a><code>ListingPendingChanges.Items</code><br><code>IReadOnlyList&lt;ListingChange&gt; Items</code> | 계산 속성 | 현재 대기 변경을 읽기 전용 목록으로 노출한다. 계산식: <code>items</code>. | [L103](../../src/ListingChange.cs#L103) |
| <a id="symbol-src-listingchange-cs-104-39ba5ac812"></a><code>ListingPendingChanges.Find</code><br><code>ListingChange? Find(string key)</code> | 메서드 | 필드 key 또는 slot key에 해당하는 대기 변경을 찾는다. | [L104](../../src/ListingChange.cs#L104) |
| <a id="symbol-src-listingchange-cs-105-613b0863c8"></a><code>ListingPendingChanges.CanQueue</code><br><code>bool CanQueue(ListingChange change)</code> | 메서드 | 유효한 edit 또는 기존 key를 원본 값으로 돌리는 요청을 허용한다. 동일 source 검사는 Queue에서 수행한다. | [L105](../../src/ListingChange.cs#L105) |
| <a id="symbol-src-listingchange-cs-107-3eb03aed9a"></a><code>ListingPendingChanges.Queue</code><br><code>bool Queue(ListingChange change, out string error)</code> | 메서드 | source가 달라지면 기존 대기열을 비우고 거부한다. 원래 값으로 되돌린 요청은 제거하며 나머지는 key별 추가/교체한다. PIN 변경은 no-op로 추정하지 않는다. | [L107](../../src/ListingChange.cs#L107) |
| <a id="symbol-src-listingchange-cs-120-5061f88d7f"></a><code>ListingPendingChanges.Take</code><br><code>ListingChange[] Take()</code> | 메서드 | 대기열 배열을 반환하면서 비운다. 실제 배치 진입에서는 Consume 경로와 구분해야 한다. | [L120](../../src/ListingChange.cs#L120) |
| <a id="symbol-src-listingchange-cs-122-059b6bd9f4"></a><code>ListingPendingChanges.Consume</code><br><code>void Consume(ListingChange[] submitted)</code> | 메서드 | 실제 제출 대상으로 잡은 항목과 같은 객체만 대기열에서 제거해 뒤늦게 추가한 변경을 보존한다. | [L122](../../src/ListingChange.cs#L122) |
| <a id="symbol-src-listingchange-cs-123-b1b4260de6"></a><code>ListingPendingChanges.Clear</code><br><code>void Clear()</code> | 메서드 | 메모리의 대기 변경을 모두 제거한다. | [L123](../../src/ListingChange.cs#L123) |
| <a id="symbol-src-listingchange-cs-124-7655b9a68a"></a><code>ListingPendingChanges.SameSource</code><br><code>bool SameSource(RefreshTarget a, RefreshTarget b)</code> | 메서드 | 본인·listing ID·내용·조건·슬롯/멤버가 같은 편집 원본인지 검사한다. | [L124](../../src/ListingChange.cs#L124) |

<a id="file-src-listingcheckboxui-cs"></a>
## src/ListingCheckboxUi.cs

[소스 파일](../../src/ListingCheckboxUi.cs) · 명명된 항목/진입점 7개

단일 체크박스 순수 상태 기계. Next가 반환하는 지시를 native Host가 실행한다. 상태를 먼저 소비해 반복 클릭을 방지한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingcheckboxui-cs-5-500a6ebf37"></a><code>ListingCheckboxValues.Decode</code><br><code>bool? Decode(byte value)</code> | 메서드 | stored byte0/1을 false/true로 바꾸고 다른 값은 null로 반환한다. | [L5](../../src/ListingCheckboxUi.cs#L5) |
| <a id="symbol-src-listingcheckboxui-cs-6-3ec05bc17d"></a><code>ListingCheckboxValues.Label</code><br><code>string Label(bool? value)</code> | 메서드 | bool?을 켬/끔/확인 불가로 표시한다. | [L6](../../src/ListingCheckboxUi.cs#L6) |
| <a id="symbol-src-listingcheckboxui-cs-9-c4def33001"></a><code>CheckboxUiState</code><br><code>CheckboxUiState(bool Checked, byte Stored)</code> | 주 생성자/모델 | checkbox 표시 checked와 저장 byte의 관찰값을 묶는다. | [L9](../../src/ListingCheckboxUi.cs#L9) |
| <a id="symbol-src-listingcheckboxui-cs-11-93bcf28d41"></a><code>CheckboxUiState.Matches</code><br><code>bool Matches(bool enabled)</code> | 메서드 | UI 체크/선택/숫자와 내부 저장값이 요청한 실효값에 동시에 일치하는지 검사한다. 해제 상태의 캐시값은 각 모델 규칙에 따른다. | [L11](../../src/ListingCheckboxUi.cs#L11) |
| <a id="symbol-src-listingcheckboxui-cs-18-69dd7d88c6"></a><code>ListingCheckboxUi</code><br><code>ListingCheckboxUi(bool original, bool enabled, string setting)</code> | 주 생성자/모델 | 원래 값과 요청값을 고정한 순수 UI 상태 기계를 만든다. Next가 반환하는 native 동작은 Host가 수행한다. | [L18](../../src/ListingCheckboxUi.cs#L18) |
| <a id="symbol-src-listingcheckboxui-cs-24-aedffed6ed"></a><code>ListingCheckboxUi.Next</code><br><code>CheckboxUiAction Next(CheckboxUiState state, long now)</code> | 메서드 | 초기 원본 일치→1회 토글/열기/선택/입력→저장값 관찰→완료를 진행하는 순수 함수 경계다. 상태 변경을 지시 반환 전에 소비하고 불일치·3초 반영 timeout은 Failed로 고정한다. | [L24](../../src/ListingCheckboxUi.cs#L24) |
| <a id="symbol-src-listingcheckboxui-cs-45-b43eea3d44"></a><code>ListingCheckboxUi.Fail</code><br><code>CheckboxUiAction Fail(string check)</code> | 메서드 | phase를 failed로 고정하고 오류 코드를 기록한 뒤 Failed action을 반환한다. | [L45](../../src/ListingCheckboxUi.cs#L45) |

<a id="file-src-listingcompletionedit-cs"></a>
## src/ListingCompletionEdit.cs

[소스 파일](../../src/ListingCompletionEdit.cs) · 명명된 항목/진입점 25개

완료조건의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingCompletionEditTests.cs](tests.md#file-tests-listingcompletionedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingcompletionedit-cs-11-bc7c779f83"></a><code>ListingCompletions.Supported</code><br><code>bool Supported(byte? value)</code> | 메서드 | 편집 가능한 stored 완료조건 1(미설정), 2(공략완료), 4(미완료)인지 검사한다. 값 8은 읽기만 지원한다. | [L11](../../src/ListingCompletionEdit.cs#L11) |
| <a id="symbol-src-listingcompletionedit-cs-12-e61ea86a8d"></a><code>ListingCompletions.Value</code><br><code>byte? Value(bool enabled, int index)</code> | 메서드 | enabled/index를 1/2/4 stored bit로 변환한다. callback ordinal 1/2/3과 혼동하면 안 된다. | [L12](../../src/ListingCompletionEdit.cs#L12) |
| <a id="symbol-src-listingcompletionedit-cs-13-bd2d4596ed"></a><code>ListingCompletions.Index</code><br><code>int Index(byte? value)</code> | 메서드 | 공략완료 bit2→0, 미완료 bit4→1로 역변환한다. 미설정은 -1이다. | [L13](../../src/ListingCompletionEdit.cs#L13) |
| <a id="symbol-src-listingcompletionedit-cs-14-bd3bc93ae2"></a><code>ListingCompletions.Label</code><br><code>string Label(byte? value)</code> | 메서드 | 해당 설정의 저장값을 한국어 표시명으로 변환하고 미지원/알 수 없음도 구분한다. | [L14](../../src/ListingCompletionEdit.cs#L14) |
| <a id="symbol-src-listingcompletionedit-cs-18-ec6a57c17f"></a><code>ListingCompletions.Read</code><br><code>byte? Read(string criteria)</code> | 메서드 | Criteria 다섯 번째 필드를 읽는다. 1/2/4와 표시용 8만 허용한다. | [L18](../../src/ListingCompletionEdit.cs#L18) |
| <a id="symbol-src-listingcompletionedit-cs-26-b33da67a6b"></a><code>ListingCompletionDraft</code><br><code>ListingCompletionDraft(RefreshTarget original)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L26](../../src/ListingCompletionEdit.cs#L26) |
| <a id="symbol-src-listingcompletionedit-cs-31-cb41cdef10"></a><code>ListingCompletionDraft.Request</code><br><code>ListingCompletionEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, Enabled, SelectedIndex)</code>. | [L31](../../src/ListingCompletionEdit.cs#L31) |
| <a id="symbol-src-listingcompletionedit-cs-34-3dfe4b39bc"></a><code>ListingCompletionEdit</code><br><code>ListingCompletionEdit(RefreshTarget Original, bool Enabled, int SelectedIndex)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L34](../../src/ListingCompletionEdit.cs#L34) |
| <a id="symbol-src-listingcompletionedit-cs-36-b4959dcaf1"></a><code>ListingCompletionEdit.Value</code><br><code>byte? Value</code> | 계산 속성 | enabled/index를 1/2/4 stored bit로 변환한다. callback ordinal 1/2/3과 혼동하면 안 된다. 계산식: <code>ListingCompletions.Value(Enabled, SelectedIndex)</code>. | [L36](../../src/ListingCompletionEdit.cs#L36) |
| <a id="symbol-src-listingcompletionedit-cs-37-3217ece466"></a><code>ListingCompletionEdit.Changed</code><br><code>bool Changed</code> | 계산 속성 | 요청한 값이 바인딩한 원본 값과 실제로 다른지 검사한다. 계산식: <code>Value.HasValue &amp;&amp; ListingCompletions.Read(Original.Criteria) is { } original &amp;&amp; original != Value</code>. | [L37](../../src/ListingCompletionEdit.cs#L37) |
| <a id="symbol-src-listingcompletionedit-cs-38-66a6650530"></a><code>ListingCompletionEdit.Supports</code><br><code>bool Supports(RefreshTarget target)</code> | 메서드 | 현재 source의 지원 layout/읽을 수 있는 해당 조건을 검사한다. 실제 게임 control enabled 검사는 별도 Host 단계다. | [L38](../../src/ListingCompletionEdit.cs#L38) |
| <a id="symbol-src-listingcompletionedit-cs-39-8d0994d81c"></a><code>ListingCompletionEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. 계산식: <code>Supports(Original) &amp;&amp; Changed</code>. | [L39](../../src/ListingCompletionEdit.cs#L39) |
| <a id="symbol-src-listingcompletionedit-cs-40-a0d25a3d44"></a><code>ListingCompletionEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L40](../../src/ListingCompletionEdit.cs#L40) |
| <a id="symbol-src-listingcompletionedit-cs-42-b99329f961"></a><code>ListingCompletionEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget before)</code> | 메서드 | 현재 before snapshot의 해당 필드만 요청값으로 바꾼 새 RefreshTarget을 만든다. 네이티브 메모리를 쓰지 않는다. | [L42](../../src/ListingCompletionEdit.cs#L42) |
| <a id="symbol-src-listingcompletionedit-cs-48-461c04b6ac"></a><code>ListingCompletionEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L48](../../src/ListingCompletionEdit.cs#L48) |
| <a id="symbol-src-listingcompletionedit-cs-52-22d5fd5954"></a><code>CompletionUiState</code><br><code>CompletionUiState(bool Enabled, int Index, int ListIndex, byte Stored, bool Open, bool Pending, bool DropdownChecked, int Count, bool CanSelect)</code> | 주 생성자/모델 | 완료조건 checkbox와 dropdown/list·stored bit·선택 가능 여부를 함께 보관한다. | [L52](../../src/ListingCompletionEdit.cs#L52) |
| <a id="symbol-src-listingcompletionedit-cs-55-a24831dbf8"></a><code>CompletionUiState.Closed</code><br><code>bool Closed</code> | 계산 속성 | dropdown open/pending/checked가 모두 종료돼 안정적으로 닫혔는지 판정한다. 계산식: <code>!Open &amp;&amp; !Pending &amp;&amp; !DropdownChecked</code>. | [L55](../../src/ListingCompletionEdit.cs#L55) |
| <a id="symbol-src-listingcompletionedit-cs-56-71d303192c"></a><code>CompletionUiState.Matches</code><br><code>bool Matches(byte value)</code> | 메서드 | UI 체크/선택/숫자와 내부 저장값이 요청한 실효값에 동시에 일치하는지 검사한다. 해제 상태의 캐시값은 각 모델 규칙에 따른다. | [L56](../../src/ListingCompletionEdit.cs#L56) |
| <a id="symbol-src-listingcompletionedit-cs-58-2ab931f2c2"></a><code>CompletionUiState.KnownSelection</code><br><code>bool KnownSelection</code> | 계산 속성 | 켜짐·dropdown/list index 일치·stored bit 일치·항목 수 2~3인지 확인한다. 계산식: <code>Enabled &amp;&amp; Index is 0 or 1 &amp;&amp; Index == ListIndex &amp;&amp;         Stored == ListingCompletions.Value(true, Index) &amp;&amp; Count is &gt;= 2 and &lt;= 3</code>. | [L58](../../src/ListingCompletionEdit.cs#L58) |
| <a id="symbol-src-listingcompletionedit-cs-64-673d56c072"></a><code>ListingCompletionUi</code><br><code>ListingCompletionUi(byte original, byte requested)</code> | 주 생성자/모델 | 원래 값과 요청값을 고정한 순수 UI 상태 기계를 만든다. Next가 반환하는 native 동작은 Host가 수행한다. | [L64](../../src/ListingCompletionEdit.cs#L64) |
| <a id="symbol-src-listingcompletionedit-cs-71-4d62e69613"></a><code>ListingCompletionUi.Next</code><br><code>CompletionUiAction Next(CompletionUiState state, long now)</code> | 메서드 | 초기 원본 일치→1회 토글/열기/선택/입력→저장값 관찰→완료를 진행하는 순수 함수 경계다. 상태 변경을 지시 반환 전에 소비하고 불일치·3초 반영 timeout은 Failed로 고정한다. | [L71](../../src/ListingCompletionEdit.cs#L71) |
| <a id="symbol-src-listingcompletionedit-cs-116-ced8c411e9"></a><code>ListingCompletionUi.SelectOrComplete</code><br><code>CompletionUiAction SelectOrComplete(CompletionUiState state, long now)</code> | 메서드 | 체크 활성화 뒤 현재 선택이 이미 요청값이면 완료하고 아니면 선택 전 값을 고정한 후 dropdown 열기로 진행한다. | [L116](../../src/ListingCompletionEdit.cs#L116) |
| <a id="symbol-src-listingcompletionedit-cs-123-c92e7e032d"></a><code>ListingCompletionUi.Advance</code><br><code>CompletionUiAction Advance(string phase, string check, CompletionUiAction action, long now)</code> | 메서드 | phase·진단 코드·단조 시각을 먼저 갱신하고 Host가 실행할 다음 action을 반환한다. | [L123](../../src/ListingCompletionEdit.cs#L123) |
| <a id="symbol-src-listingcompletionedit-cs-125-8c0575f6cd"></a><code>ListingCompletionUi.Wait</code><br><code>CompletionUiAction Wait(long now, string error)</code> | 메서드 | 현재 phase의 대기 코드를 기록하며 3초를 넘기면 Fail로 전환한다. | [L125](../../src/ListingCompletionEdit.cs#L125) |
| <a id="symbol-src-listingcompletionedit-cs-127-2647dc423a"></a><code>ListingCompletionUi.Fail</code><br><code>CompletionUiAction Fail(string error)</code> | 메서드 | phase를 failed로 고정하고 오류 코드를 기록한 뒤 Failed action을 반환한다. | [L127](../../src/ListingCompletionEdit.cs#L127) |

<a id="file-src-listingdescriptionedit-cs"></a>
## src/ListingDescriptionEdit.cs

[소스 파일](../../src/ListingDescriptionEdit.cs) · 명명된 항목/진입점 13개

소개말의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingDescriptionEditTests.cs](tests.md#file-tests-listingdescriptionedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdescriptionedit-cs-5-077818a846"></a><code>ListingDescriptionDraft</code><br><code>ListingDescriptionDraft(RefreshTarget original)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L5](../../src/ListingDescriptionEdit.cs#L5) |
| <a id="symbol-src-listingdescriptionedit-cs-9-94617628e0"></a><code>ListingDescriptionDraft.Request</code><br><code>ListingDescriptionEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, Text)</code>. | [L9](../../src/ListingDescriptionEdit.cs#L9) |
| <a id="symbol-src-listingdescriptionedit-cs-12-d506a38795"></a><code>ListingDescriptionEdit</code><br><code>ListingDescriptionEdit(RefreshTarget Original, string Text)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L12](../../src/ListingDescriptionEdit.cs#L12) |
| <a id="symbol-src-listingdescriptionedit-cs-18-bcb26a41b3"></a><code>ListingDescriptionEdit.Normalize</code><br><code>string Normalize(string text)</code> | 메서드 | 소개말 줄바꿈을 LF로 통일하고 앞뒤 공백을 정리한다. | [L18](../../src/ListingDescriptionEdit.cs#L18) |
| <a id="symbol-src-listingdescriptionedit-cs-19-b2c36e4de3"></a><code>ListingDescriptionEdit.Value</code><br><code>string Value</code> | 계산 속성 | 사용자 입력을 해당 필드의 유효한 저장값으로 변환한다. 유효하지 않으면 null 또는 모델 규칙에 따른 값을 반환한다. 계산식: <code>Normalize(Text)</code>. | [L19](../../src/ListingDescriptionEdit.cs#L19) |
| <a id="symbol-src-listingdescriptionedit-cs-20-fd86b1a677"></a><code>ListingDescriptionEdit.Changed</code><br><code>bool Changed</code> | 계산 속성 | 요청한 값이 바인딩한 원본 값과 실제로 다른지 검사한다. 계산식: <code>!ListingIdentity.SameText(Original.Description, Value)</code>. | [L20](../../src/ListingDescriptionEdit.cs#L20) |
| <a id="symbol-src-listingdescriptionedit-cs-21-ebbcc2cb26"></a><code>ListingDescriptionEdit.ByteCount</code><br><code>int ByteCount</code> | 계산 속성 | 정규화한 소개말의 UTF-8 byte 수를 계산한다. | [L21](../../src/ListingDescriptionEdit.cs#L21) |
| <a id="symbol-src-listingdescriptionedit-cs-22-0ba84a33c7"></a><code>ListingDescriptionEdit.Validate</code><br><code>string Validate(string text)</code> | 메서드 | UTF-8 바이트 제한(192), 제어문자 및 유효한 문자열 조건을 검사하고 오류를 반환한다. | [L22](../../src/ListingDescriptionEdit.cs#L22) |
| <a id="symbol-src-listingdescriptionedit-cs-31-155dbd9747"></a><code>ListingDescriptionEdit.ValidationError</code><br><code>string ValidationError</code> | 계산 속성 | 현재 소개말 초안의 유효성 검사 오류를 반환한다. 계산식: <code>Validate(Text)</code>. | [L31](../../src/ListingDescriptionEdit.cs#L31) |
| <a id="symbol-src-listingdescriptionedit-cs-32-045c53f06b"></a><code>ListingDescriptionEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. 계산식: <code>ListingJobEdit.SupportsSettings(Original) &amp;&amp; Changed &amp;&amp; ValidationError.Length == 0</code>. | [L32](../../src/ListingDescriptionEdit.cs#L32) |
| <a id="symbol-src-listingdescriptionedit-cs-34-f160c4918a"></a><code>ListingDescriptionEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L34](../../src/ListingDescriptionEdit.cs#L34) |
| <a id="symbol-src-listingdescriptionedit-cs-38-b1b3c41945"></a><code>ListingDescriptionEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget before)</code> | 메서드 | 현재 before snapshot의 해당 필드만 요청값으로 바꾼 새 RefreshTarget을 만든다. 네이티브 메모리를 쓰지 않는다. | [L38](../../src/ListingDescriptionEdit.cs#L38) |
| <a id="symbol-src-listingdescriptionedit-cs-44-e0f3b5b739"></a><code>ListingDescriptionEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L44](../../src/ListingDescriptionEdit.cs#L44) |

<a id="file-src-listingdescriptionrecovery-cs"></a>
## src/ListingDescriptionRecovery.cs

[소스 파일](../../src/ListingDescriptionRecovery.cs) · 명명된 항목/진입점 2개

배치 소개말 실패 후 화면 입력과 실제 저장 comment를 맞추는 순수 상태 기계. 이미 보낸 변경 callback/제출을 재시도하지 않는다.

직접 관련 테스트: [ListingDescriptionRecoveryTests.cs](tests.md#file-tests-listingdescriptionrecoverytests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdescriptionrecovery-cs-15-316baef6ab"></a><code>ListingDescriptionRecovery.Next</code><br><code>DescriptionRecoveryAction Next(string visible, string stored, string requestedInput, long now, string? observedInput = null)</code> | 메서드 | 화면과 저장값이 같으면 완료한다. 자기 작업이 남긴 입력만 저장값으로 1회 복원하도록 요청하고 외부 입력 변경·저장값 변화·3초 timeout은 실패한다. | [L15](../../src/ListingDescriptionRecovery.cs#L15) |
| <a id="symbol-src-listingdescriptionrecovery-cs-37-b6ae2584f6"></a><code>ListingDescriptionRecovery.Fail</code><br><code>DescriptionRecoveryAction Fail(string check)</code> | 메서드 | 복구 실패 상태와 코드를 고정하고 Failed를 반환한다. | [L37](../../src/ListingDescriptionRecovery.cs#L37) |

<a id="file-src-listingitemleveledit-cs"></a>
## src/ListingItemLevelEdit.cs

[소스 파일](../../src/ListingItemLevelEdit.cs) · 명명된 항목/진입점 23개

평균 아이템 레벨의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingItemLevelEditTests.cs](tests.md#file-tests-listingitemleveledittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingitemleveledit-cs-10-54fab997c6"></a><code>ListingItemLevels.Decode</code><br><code>int? Decode(int value)</code> | 메서드 | 서버 상세의 실효 평균 레벨 0~999를 해석한다. 0은 조건 해제이며 음수/초과는 null이다. | [L10](../../src/ListingItemLevelEdit.cs#L10) |
| <a id="symbol-src-listingitemleveledit-cs-11-dd2d9dad62"></a><code>ListingItemLevels.Read</code><br><code>int? Read(string extraCriteria)</code> | 메서드 | ExtraCriteria 첫 필드의 실효 평균 아이템 레벨을 읽는다. | [L11](../../src/ListingItemLevelEdit.cs#L11) |
| <a id="symbol-src-listingitemleveledit-cs-17-ed2f5ded6a"></a><code>ListingItemLevels.TryLevel</code><br><code>bool TryLevel(string text, out int value)</code> | 메서드 | ASCII 1~3자리 숫자이며 1~999인지 검사한다. | [L17](../../src/ListingItemLevelEdit.cs#L17) |
| <a id="symbol-src-listingitemleveledit-cs-23-4740e4c0db"></a><code>ListingItemLevels.Label</code><br><code>string Label(int? value)</code> | 메서드 | 해당 설정의 저장값을 한국어 표시명으로 변환하고 미지원/알 수 없음도 구분한다. | [L23](../../src/ListingItemLevelEdit.cs#L23) |
| <a id="symbol-src-listingitemleveledit-cs-28-0f201a822c"></a><code>ListingItemLevelDraft</code><br><code>ListingItemLevelDraft(RefreshTarget original)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L28](../../src/ListingItemLevelEdit.cs#L28) |
| <a id="symbol-src-listingitemleveledit-cs-33-93ee24487b"></a><code>ListingItemLevelDraft.Request</code><br><code>ListingItemLevelEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, Enabled, Enabled ? LevelText : "")</code>. | [L33](../../src/ListingItemLevelEdit.cs#L33) |
| <a id="symbol-src-listingitemleveledit-cs-36-f78e211102"></a><code>ListingItemLevelEdit</code><br><code>ListingItemLevelEdit(RefreshTarget Original, bool Enabled, string LevelText)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L36](../../src/ListingItemLevelEdit.cs#L36) |
| <a id="symbol-src-listingitemleveledit-cs-38-7ba1cecaf6"></a><code>ListingItemLevelEdit.Value</code><br><code>int? Value</code> | 계산 속성 | 사용자 입력을 해당 필드의 유효한 저장값으로 변환한다. 유효하지 않으면 null 또는 모델 규칙에 따른 값을 반환한다. 계산식: <code>!Enabled ? 0 : ListingItemLevels.TryLevel(LevelText, out var value) ? value : null</code>. | [L38](../../src/ListingItemLevelEdit.cs#L38) |
| <a id="symbol-src-listingitemleveledit-cs-39-46b350966b"></a><code>ListingItemLevelEdit.Changed</code><br><code>bool Changed</code> | 계산 속성 | 요청한 값이 바인딩한 원본 값과 실제로 다른지 검사한다. 계산식: <code>Value.HasValue &amp;&amp; ListingItemLevels.Read(Original.ExtraCriteria) is { } original &amp;&amp; original != Value</code>. | [L39](../../src/ListingItemLevelEdit.cs#L39) |
| <a id="symbol-src-listingitemleveledit-cs-40-51d8fdd129"></a><code>ListingItemLevelEdit.Supports</code><br><code>bool Supports(RefreshTarget target)</code> | 메서드 | 현재 source의 지원 layout/읽을 수 있는 해당 조건을 검사한다. 실제 게임 control enabled 검사는 별도 Host 단계다. | [L40](../../src/ListingItemLevelEdit.cs#L40) |
| <a id="symbol-src-listingitemleveledit-cs-41-559f1f6c44"></a><code>ListingItemLevelEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. 계산식: <code>Supports(Original) &amp;&amp; Changed</code>. | [L41](../../src/ListingItemLevelEdit.cs#L41) |
| <a id="symbol-src-listingitemleveledit-cs-42-cca4f56785"></a><code>ListingItemLevelEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L42](../../src/ListingItemLevelEdit.cs#L42) |
| <a id="symbol-src-listingitemleveledit-cs-44-d61fb0a6d4"></a><code>ListingItemLevelEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget before)</code> | 메서드 | 현재 before snapshot의 해당 필드만 요청값으로 바꾼 새 RefreshTarget을 만든다. 네이티브 메모리를 쓰지 않는다. | [L44](../../src/ListingItemLevelEdit.cs#L44) |
| <a id="symbol-src-listingitemleveledit-cs-50-ba96d48fdc"></a><code>ListingItemLevelEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L50](../../src/ListingItemLevelEdit.cs#L50) |
| <a id="symbol-src-listingitemleveledit-cs-54-fadd5ca740"></a><code>ItemLevelUiState</code><br><code>ItemLevelUiState(bool Checked, byte EnabledRaw, ushort StoredValue, int NumericValue, bool InputReady, int Minimum, int Maximum)</code> | 주 생성자/모델 | 평균 레벨 checked·enabled byte·stored/input 숫자와 실제 입력 min/max·준비 여부를 보관한다. | [L54](../../src/ListingItemLevelEdit.cs#L54) |
| <a id="symbol-src-listingitemleveledit-cs-57-06990f9a42"></a><code>ItemLevelUiState.StoredEnabled</code><br><code>bool? StoredEnabled</code> | 계산 속성 | native 저장 표현을 bool?로 해석한다. 잘못된 값은 꺼짐으로 간주하지 않는다. 계산식: <code>EnabledRaw switch { 0 =&gt; false, 1 =&gt; true, _ =&gt; null }</code>. | [L57](../../src/ListingItemLevelEdit.cs#L57) |
| <a id="symbol-src-listingitemleveledit-cs-58-839717e811"></a><code>ItemLevelUiState.CanEnter</code><br><code>bool CanEnter(int value)</code> | 메서드 | UI 입력 준비·프로그램의 1~999·게임 numeric input의 min/max를 모두 만족하는지 확인한다. | [L58](../../src/ListingItemLevelEdit.cs#L58) |
| <a id="symbol-src-listingitemleveledit-cs-60-065fdc2dac"></a><code>ItemLevelUiState.Matches</code><br><code>bool Matches(int effective)</code> | 메서드 | UI 체크/선택/숫자와 내부 저장값이 요청한 실효값에 동시에 일치하는지 검사한다. 해제 상태의 캐시값은 각 모델 규칙에 따른다. | [L60](../../src/ListingItemLevelEdit.cs#L60) |
| <a id="symbol-src-listingitemleveledit-cs-67-70e5636139"></a><code>ListingItemLevelUi</code><br><code>ListingItemLevelUi(int original, int requested)</code> | 주 생성자/모델 | 원래 값과 요청값을 고정한 순수 UI 상태 기계를 만든다. Next가 반환하는 native 동작은 Host가 수행한다. | [L67](../../src/ListingItemLevelEdit.cs#L67) |
| <a id="symbol-src-listingitemleveledit-cs-74-d26d5a71d7"></a><code>ListingItemLevelUi.Next</code><br><code>ItemLevelUiAction Next(ItemLevelUiState state, long now)</code> | 메서드 | 초기 원본 일치→1회 토글/열기/선택/입력→저장값 관찰→완료를 진행하는 순수 함수 경계다. 상태 변경을 지시 반환 전에 소비하고 불일치·3초 반영 timeout은 Failed로 고정한다. | [L74](../../src/ListingItemLevelEdit.cs#L74) |
| <a id="symbol-src-listingitemleveledit-cs-118-fe3ef7e8ff"></a><code>ListingItemLevelUi.Advance</code><br><code>ItemLevelUiAction Advance(string phase, string check, ItemLevelUiAction action, long now)</code> | 메서드 | phase·진단 코드·단조 시각을 먼저 갱신하고 Host가 실행할 다음 action을 반환한다. | [L118](../../src/ListingItemLevelEdit.cs#L118) |
| <a id="symbol-src-listingitemleveledit-cs-120-5acd37cda7"></a><code>ListingItemLevelUi.Wait</code><br><code>ItemLevelUiAction Wait(long now, string error)</code> | 메서드 | 현재 phase의 대기 코드를 기록하며 3초를 넘기면 Fail로 전환한다. | [L120](../../src/ListingItemLevelEdit.cs#L120) |
| <a id="symbol-src-listingitemleveledit-cs-122-a6dcb0b416"></a><code>ListingItemLevelUi.Fail</code><br><code>ItemLevelUiAction Fail(string reason)</code> | 메서드 | phase를 failed로 고정하고 오류 코드를 기록한 뒤 Failed action을 반환한다. | [L122](../../src/ListingItemLevelEdit.cs#L122) |

<a id="file-src-listingjobedit-cs"></a>
## src/ListingJobEdit.cs

[소스 파일](../../src/ListingJobEdit.cs) · 명명된 항목/진입점 13개

허용 직업의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingJobEditTests.cs](tests.md#file-tests-listingjobedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingjobedit-cs-8-35f6edb153"></a><code>ListingJobDraft</code><br><code>ListingJobDraft(RefreshTarget original, int position, ListingJobRules rules)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L8](../../src/ListingJobEdit.cs#L8) |
| <a id="symbol-src-listingjobedit-cs-14-43f86db851"></a><code>ListingJobDraft.Changed</code><br><code>bool Changed</code> | 계산 속성 | 요청한 값이 바인딩한 원본 값과 실제로 다른지 검사한다. 계산식: <code>Selected != (Original.Slots.Slots[Position - 1].SlotFlags &amp; Rules.AllFlags)</code>. | [L14](../../src/ListingJobEdit.cs#L14) |
| <a id="symbol-src-listingjobedit-cs-15-4c28360a4f"></a><code>ListingJobDraft.Request</code><br><code>ListingJobEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, Position, Selected, Rules)</code>. | [L15](../../src/ListingJobEdit.cs#L15) |
| <a id="symbol-src-listingjobedit-cs-18-03797470cf"></a><code>ListingJobEdit</code><br><code>ListingJobEdit(RefreshTarget Original, int Position, ulong Selected, ListingJobRules Rules)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L18](../../src/ListingJobEdit.cs#L18) |
| <a id="symbol-src-listingjobedit-cs-20-cdc40d5141"></a><code>ListingJobEdit.Supports</code><br><code>bool Supports(RefreshTarget target)</code> | 메서드 | JobEditingAllowed, snapshot 총 등록 인원 일치와 지원 layout인지 검사한다. 빈자리·선택 nonzero·카탈로그 Complete 검사는 Valid에서 추가한다. | [L20](../../src/ListingJobEdit.cs#L20) |
| <a id="symbol-src-listingjobedit-cs-24-3650764211"></a><code>ListingJobEdit.SupportsSettings</code><br><code>bool SupportsSettings(RefreshTarget target)</code> | 메서드 | 직업 슬롯 자체를 바꾸지 않는 일반 설정의 원본/slot layout 지원 여부를 검사한다. | [L24](../../src/ListingJobEdit.cs#L24) |
| <a id="symbol-src-listingjobedit-cs-26-e11d938a9d"></a><code>ListingJobEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. | [L26](../../src/ListingJobEdit.cs#L26) |
| <a id="symbol-src-listingjobedit-cs-32-775a73bac5"></a><code>ListingJobEdit.MatchesSelectedFlags</code><br><code>bool MatchesSelectedFlags(ulong actual)</code> | 메서드 | 게임이 돌려준 대상 슬롯 mask의 알려진 직업 bit가 선택과 같은지 확인한다. 선택하지 않은 미래/알 수 없는 bit를 함부로 편집하지 않는다. | [L32](../../src/ListingJobEdit.cs#L32) |
| <a id="symbol-src-listingjobedit-cs-35-790b209a45"></a><code>ListingJobEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L35](../../src/ListingJobEdit.cs#L35) |
| <a id="symbol-src-listingjobedit-cs-39-c6604db365"></a><code>ListingJobEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget fresh)</code> | 메서드 | 대상 NativeIndex의 mask만 교체한 Slots/Criteria를 만든다. actualFlags overload는 게임이 실제로 정규화한 raw 값을 보존한다. | [L39](../../src/ListingJobEdit.cs#L39) |
| <a id="symbol-src-listingjobedit-cs-41-ee77b0fb7c"></a><code>ListingJobEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget fresh, ulong actualFlags)</code> | 메서드 | 대상 NativeIndex의 mask만 교체한 Slots/Criteria를 만든다. actualFlags overload는 게임이 실제로 정규화한 raw 값을 보존한다. | [L41](../../src/ListingJobEdit.cs#L41) |
| <a id="symbol-src-listingjobedit-cs-56-ce4b2ed93d"></a><code>ListingJobEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L56](../../src/ListingJobEdit.cs#L56) |
| <a id="symbol-src-listingjobedit-cs-68-919ab365f0"></a><code>ListingJobEdit.SameSlots</code><br><code>bool SameSlots(ListingSlots a, ListingSlots b)</code> | 메서드 | 등록 구조·물리 위치·참가자와 변경하지 않아야 하는 슬롯 mask를 비교한다. | [L68](../../src/ListingJobEdit.cs#L68) |

<a id="file-src-listinglootui-cs"></a>
## src/ListingLootUi.cs

[소스 파일](../../src/ListingLootUi.cs) · 명명된 항목/진입점 8개

전리품 dropdown 순수 상태 기계. Next가 반환하는 지시를 native Host가 실행한다. 상태를 먼저 소비해 반복 클릭을 방지한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listinglootui-cs-3-7b50223692"></a><code>LootUiState</code><br><code>LootUiState(int Index, int ListIndex, byte Stored, bool Open, bool Pending, bool Checked, int Count)</code> | 주 생성자/모델 | 전리품 dropdown/list index·stored enum·열림 상태·항목 수를 보관한다. | [L3](../../src/ListingLootUi.cs#L3) |
| <a id="symbol-src-listinglootui-cs-5-3e362c33ff"></a><code>LootUiState.Matches</code><br><code>bool Matches(int index)</code> | 메서드 | UI 체크/선택/숫자와 내부 저장값이 요청한 실효값에 동시에 일치하는지 검사한다. 해제 상태의 캐시값은 각 모델 규칙에 따른다. | [L5](../../src/ListingLootUi.cs#L5) |
| <a id="symbol-src-listinglootui-cs-6-7722c30b0b"></a><code>LootUiState.SettledClosed</code><br><code>bool SettledClosed</code> | 계산 속성 | dropdown open/pending/checked가 모두 false인지 판정한다. 계산식: <code>!Open &amp;&amp; !Pending &amp;&amp; !Checked</code>. | [L6](../../src/ListingLootUi.cs#L6) |
| <a id="symbol-src-listinglootui-cs-13-ab6d08dd57"></a><code>ListingLootUi</code><br><code>ListingLootUi(int original, int selected)</code> | 주 생성자/모델 | 원래 값과 요청값을 고정한 순수 UI 상태 기계를 만든다. Next가 반환하는 native 동작은 Host가 수행한다. | [L13](../../src/ListingLootUi.cs#L13) |
| <a id="symbol-src-listinglootui-cs-19-f4b2928be1"></a><code>ListingLootUi.Next</code><br><code>LootUiAction Next(LootUiState state, long now)</code> | 메서드 | 초기 원본 일치→1회 토글/열기/선택/입력→저장값 관찰→완료를 진행하는 순수 함수 경계다. 상태 변경을 지시 반환 전에 소비하고 불일치·3초 반영 timeout은 Failed로 고정한다. | [L19](../../src/ListingLootUi.cs#L19) |
| <a id="symbol-src-listinglootui-cs-51-6836f90d00"></a><code>ListingLootUi.Advance</code><br><code>LootUiAction Advance(string phase, string check, LootUiAction action, long now)</code> | 메서드 | phase·진단 코드·단조 시각을 먼저 갱신하고 Host가 실행할 다음 action을 반환한다. | [L51](../../src/ListingLootUi.cs#L51) |
| <a id="symbol-src-listinglootui-cs-53-a7bf9022e0"></a><code>ListingLootUi.Wait</code><br><code>LootUiAction Wait(long now, string error)</code> | 메서드 | 현재 phase의 대기 코드를 기록하며 3초를 넘기면 Fail로 전환한다. | [L53](../../src/ListingLootUi.cs#L53) |
| <a id="symbol-src-listinglootui-cs-55-65c8500f12"></a><code>ListingLootUi.Fail</code><br><code>LootUiAction Fail(string reason)</code> | 메서드 | phase를 failed로 고정하고 오류 코드를 기록한 뒤 Failed action을 반환한다. | [L55](../../src/ListingLootUi.cs#L55) |

<a id="file-src-listingobjectiveedit-cs"></a>
## src/ListingObjectiveEdit.cs

[소스 파일](../../src/ListingObjectiveEdit.cs) · 명명된 항목/진입점 15개

모집 목적의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingObjectiveEditTests.cs](tests.md#file-tests-listingobjectiveedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingobjectiveedit-cs-9-1b2a1692f5"></a><code>ListingObjectives.AutomaticEditingAvailable</code><br><code>bool AutomaticEditingAvailable</code> | 계산 속성 | 목적 자동 편집 기능의 현재 활성 여부를 노출한다. 계산식: <code>true</code>. | [L9](../../src/ListingObjectiveEdit.cs#L9) |
| <a id="symbol-src-listingobjectiveedit-cs-11-240761d56e"></a><code>ListingObjectives.Value</code><br><code>byte? Value(int index)</code> | 메서드 | 목적 UI index 0~3을 stored bit 값 1/2/4/8로 변환한다. 범위 밖은 null이다. | [L11](../../src/ListingObjectiveEdit.cs#L11) |
| <a id="symbol-src-listingobjectiveedit-cs-12-cfb4ea0bed"></a><code>ListingObjectives.Index</code><br><code>int Index(byte? value)</code> | 메서드 | stored 목적 bit 1/2/4/8을 UI index 0~3으로 역변환한다. | [L12](../../src/ListingObjectiveEdit.cs#L12) |
| <a id="symbol-src-listingobjectiveedit-cs-13-958b684cc9"></a><code>ListingObjectives.Label</code><br><code>string Label(byte? value)</code> | 메서드 | 해당 설정의 저장값을 한국어 표시명으로 변환하고 미지원/알 수 없음도 구분한다. | [L13](../../src/ListingObjectiveEdit.cs#L13) |
| <a id="symbol-src-listingobjectiveedit-cs-18-2016594c2c"></a><code>ListingObjectives.Read</code><br><code>byte? Read(string criteria)</code> | 메서드 | Criteria의 세 번째 필드를 유효한 모집 목적 bit로 읽는다. | [L18](../../src/ListingObjectiveEdit.cs#L18) |
| <a id="symbol-src-listingobjectiveedit-cs-26-b05cae305b"></a><code>ListingObjectiveDraft</code><br><code>ListingObjectiveDraft(RefreshTarget original)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L26](../../src/ListingObjectiveEdit.cs#L26) |
| <a id="symbol-src-listingobjectiveedit-cs-30-c8a6b21f60"></a><code>ListingObjectiveDraft.Request</code><br><code>ListingObjectiveEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, SelectedIndex)</code>. | [L30](../../src/ListingObjectiveEdit.cs#L30) |
| <a id="symbol-src-listingobjectiveedit-cs-33-896b5b61a0"></a><code>ListingObjectiveEdit</code><br><code>ListingObjectiveEdit(RefreshTarget Original, int SelectedIndex)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L33](../../src/ListingObjectiveEdit.cs#L33) |
| <a id="symbol-src-listingobjectiveedit-cs-35-577e8cbe46"></a><code>ListingObjectiveEdit.Value</code><br><code>byte? Value</code> | 계산 속성 | 목적 UI index 0~3을 stored bit 값 1/2/4/8로 변환한다. 범위 밖은 null이다. 계산식: <code>ListingObjectives.Value(SelectedIndex)</code>. | [L35](../../src/ListingObjectiveEdit.cs#L35) |
| <a id="symbol-src-listingobjectiveedit-cs-36-8db6f30dda"></a><code>ListingObjectiveEdit.Changed</code><br><code>bool Changed</code> | 계산 속성 | 요청한 값이 바인딩한 원본 값과 실제로 다른지 검사한다. 계산식: <code>Value.HasValue &amp;&amp; ListingObjectives.Read(Original.Criteria) is { } original &amp;&amp; original != Value</code>. | [L36](../../src/ListingObjectiveEdit.cs#L36) |
| <a id="symbol-src-listingobjectiveedit-cs-37-8c26b4a1a5"></a><code>ListingObjectiveEdit.Supports</code><br><code>bool Supports(RefreshTarget target)</code> | 메서드 | 현재 source의 지원 layout/읽을 수 있는 해당 조건을 검사한다. 실제 게임 control enabled 검사는 별도 Host 단계다. | [L37](../../src/ListingObjectiveEdit.cs#L37) |
| <a id="symbol-src-listingobjectiveedit-cs-38-63532fe3f3"></a><code>ListingObjectiveEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. 계산식: <code>Supports(Original) &amp;&amp; Changed</code>. | [L38](../../src/ListingObjectiveEdit.cs#L38) |
| <a id="symbol-src-listingobjectiveedit-cs-40-a18c1de515"></a><code>ListingObjectiveEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L40](../../src/ListingObjectiveEdit.cs#L40) |
| <a id="symbol-src-listingobjectiveedit-cs-44-148ceefb55"></a><code>ListingObjectiveEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget before)</code> | 메서드 | 현재 before snapshot의 해당 필드만 요청값으로 바꾼 새 RefreshTarget을 만든다. 네이티브 메모리를 쓰지 않는다. | [L44](../../src/ListingObjectiveEdit.cs#L44) |
| <a id="symbol-src-listingobjectiveedit-cs-52-27bdb2f34f"></a><code>ListingObjectiveEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L52](../../src/ListingObjectiveEdit.cs#L52) |

<a id="file-src-listingobjectiveui-cs"></a>
## src/ListingObjectiveUi.cs

[소스 파일](../../src/ListingObjectiveUi.cs) · 명명된 항목/진입점 8개

목적 dropdown 순수 상태 기계. Next가 반환하는 지시를 native Host가 실행한다. 상태를 먼저 소비해 반복 클릭을 방지한다.

직접 관련 테스트: [ListingObjectiveUiTests.cs](tests.md#file-tests-listingobjectiveuitests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingobjectiveui-cs-3-5c5931b945"></a><code>ObjectiveUiState</code><br><code>ObjectiveUiState(int Index, int ListIndex, byte Stored, bool Open, bool Pending, bool Checked, int Count)</code> | 주 생성자/모델 | 목적 dropdown/list index·stored bit·열림/pending/checked·항목 수를 보관한다. | [L3](../../src/ListingObjectiveUi.cs#L3) |
| <a id="symbol-src-listingobjectiveui-cs-5-7789f340ad"></a><code>ObjectiveUiState.SettledClosed</code><br><code>bool SettledClosed</code> | 계산 속성 | dropdown open/pending/checked가 모두 false인지 판정한다. 계산식: <code>!Open &amp;&amp; !Pending &amp;&amp; !Checked</code>. | [L5](../../src/ListingObjectiveUi.cs#L5) |
| <a id="symbol-src-listingobjectiveui-cs-6-7215a84881"></a><code>ObjectiveUiState.Matches</code><br><code>bool Matches(int index)</code> | 메서드 | UI 체크/선택/숫자와 내부 저장값이 요청한 실효값에 동시에 일치하는지 검사한다. 해제 상태의 캐시값은 각 모델 규칙에 따른다. | [L6](../../src/ListingObjectiveUi.cs#L6) |
| <a id="symbol-src-listingobjectiveui-cs-15-0160ed2269"></a><code>ListingObjectiveUi</code><br><code>ListingObjectiveUi(int originalIndex, int selectedIndex)</code> | 주 생성자/모델 | 원래 값과 요청값을 고정한 순수 UI 상태 기계를 만든다. Next가 반환하는 native 동작은 Host가 수행한다. | [L15](../../src/ListingObjectiveUi.cs#L15) |
| <a id="symbol-src-listingobjectiveui-cs-21-30490a416b"></a><code>ListingObjectiveUi.Next</code><br><code>ObjectiveUiAction Next(ObjectiveUiState state, long now)</code> | 메서드 | 초기 원본 일치→1회 토글/열기/선택/입력→저장값 관찰→완료를 진행하는 순수 함수 경계다. 상태 변경을 지시 반환 전에 소비하고 불일치·3초 반영 timeout은 Failed로 고정한다. | [L21](../../src/ListingObjectiveUi.cs#L21) |
| <a id="symbol-src-listingobjectiveui-cs-56-814faf0997"></a><code>ListingObjectiveUi.Advance</code><br><code>ObjectiveUiAction Advance(string phase, string check, ObjectiveUiAction action, long now)</code> | 메서드 | phase·진단 코드·단조 시각을 먼저 갱신하고 Host가 실행할 다음 action을 반환한다. | [L56](../../src/ListingObjectiveUi.cs#L56) |
| <a id="symbol-src-listingobjectiveui-cs-58-9943c8c1ba"></a><code>ListingObjectiveUi.WaitOrFail</code><br><code>ObjectiveUiAction WaitOrFail(long now, string error)</code> | 메서드 | 현재 phase의 대기 코드를 기록하며 3초를 넘기면 Fail로 전환한다. | [L58](../../src/ListingObjectiveUi.cs#L58) |
| <a id="symbol-src-listingobjectiveui-cs-60-7e67d0e587"></a><code>ListingObjectiveUi.Fail</code><br><code>ObjectiveUiAction Fail(string error)</code> | 메서드 | phase를 failed로 고정하고 오류 코드를 기록한 뒤 Failed action을 반환한다. | [L60](../../src/ListingObjectiveUi.cs#L60) |

<a id="file-src-listingprivateedit-cs"></a>
## src/ListingPrivateEdit.cs

[소스 파일](../../src/ListingPrivateEdit.cs) · 명명된 항목/진입점 22개

비공개/PIN의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingPrivateEditTests.cs](tests.md#file-tests-listingprivateedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingprivateedit-cs-11-57e20024fb"></a><code>ListingPrivate.FromFlags</code><br><code>bool FromFlags(byte flags)</code> | 메서드 | 참가 조건 flag의 비공개 bit 0x02를 검사한다. | [L11](../../src/ListingPrivateEdit.cs#L11) |
| <a id="symbol-src-listingprivateedit-cs-12-761271820b"></a><code>ListingPrivate.Read</code><br><code>bool? Read(string extraCriteria)</code> | 메서드 | ExtraCriteria의 참가 조건 flag에서 비공개 여부를 읽는다. 상세 데이터에는 PIN이 없다. | [L12](../../src/ListingPrivateEdit.cs#L12) |
| <a id="symbol-src-listingprivateedit-cs-13-82588e3832"></a><code>ListingPrivate.StoredEnabled</code><br><code>bool? StoredEnabled(ushort password)</code> | 메서드 | 수정창 password 0~9999는 비공개, 10000은 공개, 나머지는 알 수 없음으로 구분한다. | [L13](../../src/ListingPrivateEdit.cs#L13) |
| <a id="symbol-src-listingprivateedit-cs-14-f22dac05a5"></a><code>ListingPrivate.TryPassword</code><br><code>bool TryPassword(string text, out int value)</code> | 메서드 | ASCII 숫자 정확히 네 자리를 검사해 0~9999 정수로 변환한다. 0000 및 앞자리 0을 허용한다. | [L14](../../src/ListingPrivateEdit.cs#L14) |
| <a id="symbol-src-listingprivateedit-cs-21-926373cd58"></a><code>ListingPrivate.WithEnabled</code><br><code>string WithEnabled(string extraCriteria, bool enabled)</code> | 메서드 | ExtraCriteria에서 비공개 bit만 변경하고 다른 참가 조건은 보존한다. | [L21](../../src/ListingPrivateEdit.cs#L21) |
| <a id="symbol-src-listingprivateedit-cs-30-da91e81532"></a><code>ListingPrivateDraft</code><br><code>ListingPrivateDraft(RefreshTarget original)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L30](../../src/ListingPrivateEdit.cs#L30) |
| <a id="symbol-src-listingprivateedit-cs-36-f25f9b39c2"></a><code>ListingPrivateDraft.Request</code><br><code>ListingPrivateEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, Enabled, Enabled ? Password : "")</code>. | [L36](../../src/ListingPrivateEdit.cs#L36) |
| <a id="symbol-src-listingprivateedit-cs-39-50885c586a"></a><code>ListingPrivateEdit</code><br><code>ListingPrivateEdit(RefreshTarget Original, bool Enabled, string Password)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L39](../../src/ListingPrivateEdit.cs#L39) |
| <a id="symbol-src-listingprivateedit-cs-41-3e087f9796"></a><code>ListingPrivateEdit.Supports</code><br><code>bool Supports(RefreshTarget target)</code> | 메서드 | 현재 source의 지원 layout/읽을 수 있는 해당 조건을 검사한다. 실제 게임 control enabled 검사는 별도 Host 단계다. | [L41](../../src/ListingPrivateEdit.cs#L41) |
| <a id="symbol-src-listingprivateedit-cs-42-2ff88beca4"></a><code>ListingPrivateEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. 계산식: <code>Supports(Original) &amp;&amp; (Enabled ? ListingPrivate.TryPassword(Password, out _) : ListingPrivate.Read(Original.ExtraCriteria) == true)</code>. | [L42](../../src/ListingPrivateEdit.cs#L42) |
| <a id="symbol-src-listingprivateedit-cs-43-4eea5a0336"></a><code>ListingPrivateEdit.PasswordValue</code><br><code>int PasswordValue</code> | 계산 속성 | 검증한 네 자리 PIN을 native numeric input에 보낼 정수로 변환한다. 명시 입력 유효성 검사가 선행돼야 한다. 계산식: <code>Enabled &amp;&amp; ListingPrivate.TryPassword(Password, out var value) ? value : 0</code>. | [L43](../../src/ListingPrivateEdit.cs#L43) |
| <a id="symbol-src-listingprivateedit-cs-44-0656894e42"></a><code>ListingPrivateEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L44](../../src/ListingPrivateEdit.cs#L44) |
| <a id="symbol-src-listingprivateedit-cs-46-80ae8ad10f"></a><code>ListingPrivateEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget before)</code> | 메서드 | 현재 before snapshot의 해당 필드만 요청값으로 바꾼 새 RefreshTarget을 만든다. 네이티브 메모리를 쓰지 않는다. | [L46](../../src/ListingPrivateEdit.cs#L46) |
| <a id="symbol-src-listingprivateedit-cs-52-69b6d6b4ca"></a><code>ListingPrivateEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L52](../../src/ListingPrivateEdit.cs#L52) |
| <a id="symbol-src-listingprivateedit-cs-54-58d98ee9f4"></a><code>ListingPrivateEdit.ToString</code><br><code>string ToString()</code> | 메서드 | 비공개 상태만 표시하고 PIN은 redacted로 치환한다. | [L54](../../src/ListingPrivateEdit.cs#L54) |
| <a id="symbol-src-listingprivateedit-cs-57-bd6d88b060"></a><code>PrivateUiState</code><br><code>PrivateUiState(bool Checked, ushort StoredPassword, int NumericValue, bool InputReady)</code> | 주 생성자/모델 | 비공개 checkbox·native password·input 숫자·준비 상태를 메모리에만 보관한다. ToString은 PIN을 숨긴다. | [L57](../../src/ListingPrivateEdit.cs#L57) |
| <a id="symbol-src-listingprivateedit-cs-59-3f5e7b17e3"></a><code>PrivateUiState.StoredEnabled</code><br><code>bool? StoredEnabled</code> | 계산 속성 | 수정창 password 0~9999는 비공개, 10000은 공개, 나머지는 알 수 없음으로 구분한다. 계산식: <code>ListingPrivate.StoredEnabled(StoredPassword)</code>. | [L59](../../src/ListingPrivateEdit.cs#L59) |
| <a id="symbol-src-listingprivateedit-cs-60-b35842b8c7"></a><code>PrivateUiState.Matches</code><br><code>bool Matches(bool enabled, int password)</code> | 메서드 | UI 체크/선택/숫자와 내부 저장값이 요청한 실효값에 동시에 일치하는지 검사한다. 해제 상태의 캐시값은 각 모델 규칙에 따른다. | [L60](../../src/ListingPrivateEdit.cs#L60) |
| <a id="symbol-src-listingprivateedit-cs-62-54152f801c"></a><code>PrivateUiState.ToString</code><br><code>string ToString()</code> | 메서드 | 비공개 상태만 표시하고 PIN은 redacted로 치환한다. | [L62](../../src/ListingPrivateEdit.cs#L62) |
| <a id="symbol-src-listingprivateedit-cs-68-4cca379d1e"></a><code>ListingPrivateUi</code><br><code>ListingPrivateUi(bool original, bool enabled, int password)</code> | 주 생성자/모델 | 원래 값과 요청값을 고정한 순수 UI 상태 기계를 만든다. Next가 반환하는 native 동작은 Host가 수행한다. | [L68](../../src/ListingPrivateEdit.cs#L68) |
| <a id="symbol-src-listingprivateedit-cs-74-115921834b"></a><code>ListingPrivateUi.Next</code><br><code>PrivateUiAction Next(PrivateUiState state, long now)</code> | 메서드 | 초기 원본 일치→1회 토글/열기/선택/입력→저장값 관찰→완료를 진행하는 순수 함수 경계다. 상태 변경을 지시 반환 전에 소비하고 불일치·3초 반영 timeout은 Failed로 고정한다. | [L74](../../src/ListingPrivateEdit.cs#L74) |
| <a id="symbol-src-listingprivateedit-cs-117-7b761dbf1d"></a><code>ListingPrivateUi.Fail</code><br><code>PrivateUiAction Fail(string reason)</code> | 메서드 | phase를 failed로 고정하고 오류 코드를 기록한 뒤 Failed action을 반환한다. | [L117](../../src/ListingPrivateEdit.cs#L117) |

<a id="file-src-listingrolebuttons-cs"></a>
## src/ListingRoleButtons.cs

[소스 파일](../../src/ListingRoleButtons.cs) · 명명된 항목/진입점 5개

검증된 KR 직업 node ID 표와 클릭 계획. 신규 직업이 생기면 catalog/asset/contract와 함께 검토해야 한다.

직접 관련 테스트: [ListingRoleButtonsTests.cs](tests.md#file-tests-listingrolebuttonstests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingrolebuttons-cs-5-e8af037d25"></a><code>RoleButtonAction</code><br><code>RoleButtonAction(RoleButtonActionKind Kind, uint NodeId, int Param, uint JobId = 0)</code> | 주 생성자/모델 | 한 번 실행할 Anyone/include/exclude/confirm과 node/param/직업 ID를 묶는다. | [L5](../../src/ListingRoleButtons.cs#L5) |
| <a id="symbol-src-listingrolebuttons-cs-24-6cd61c2354"></a><code>ListingRoleButtons.TryPlan</code><br><code>bool TryPlan(ListingJobRules rules, ulong selected, out RoleButtonSequence sequence, out string error)</code> | 메서드 | Anyone node66으로 시작해 선택하지 않은 직업을 제거하고 확인하는 기본 클릭 목록을 만든다. 카탈로그 불완전/알 수 없는 선택은 거부한다. | [L24](../../src/ListingRoleButtons.cs#L24) |
| <a id="symbol-src-listingrolebuttons-cs-45-747c12d245"></a><code>ListingRoleButtons.TryPlanChanges</code><br><code>bool TryPlanChanges(ListingJobRules rules, ulong current, ulong selected, out RoleButtonSequence sequence, out string error)</code> | 메서드 | 친선경기용으로 기존 mask와 요청 mask가 다른 직업만 include/exclude한다. 비활성 Anyone 또는 금지 직업을 건드리지 않는다. | [L45](../../src/ListingRoleButtons.cs#L45) |
| <a id="symbol-src-listingrolebuttons-cs-69-7c4ee314d9"></a><code>RoleButtonSequence</code><br><code>RoleButtonSequence(RoleButtonAction[] actions)</code> | 주 생성자/모델 | 정해진 버튼 action 목록과 다음 소비 위치/100ms 단계 간격을 초기화한다. | [L69](../../src/ListingRoleButtons.cs#L69) |
| <a id="symbol-src-listingrolebuttons-cs-75-659673764d"></a><code>RoleButtonSequence.TakeBatch</code><br><code>RoleButtonAction[] TakeBatch(long now)</code> | 메서드 | 간격을 확인해 이번 update에서 보낼 최대 묶음의 클릭을 소비한다. 확인은 모든 직업 변경 뒤 한 번만 반환한다. | [L75](../../src/ListingRoleButtons.cs#L75) |

<a id="file-src-listingruleedit-cs"></a>
## src/ListingRuleEdit.cs

[소스 파일](../../src/ListingRuleEdit.cs) · 명명된 항목/진입점 16개

임무 찾기/전리품 규칙의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingRuleEditTests.cs](tests.md#file-tests-listingruleedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingruleedit-cs-10-f1eee067d0"></a><code>ListingRules.Mask</code><br><code>byte Mask(ListingRuleKind kind)</code> | 메서드 | 제한 해제/최저 아이템 레벨/초월 무효를 finder flag bit 1/2/4에 대응시킨다. Loot는 별도 필드여서 mask0이다. | [L10](../../src/ListingRuleEdit.cs#L10) |
| <a id="symbol-src-listingruleedit-cs-14-45a551f26b"></a><code>ListingRules.Id</code><br><code>string Id(ListingRuleKind kind)</code> | 메서드 | 규칙 enum을 외부 API와 변경 key에 사용하는 안정된 문자열 ID로 변환한다. | [L14](../../src/ListingRuleEdit.cs#L14) |
| <a id="symbol-src-listingruleedit-cs-18-29b4e32bd9"></a><code>ListingRules.Title</code><br><code>string Title(ListingRuleKind kind)</code> | 메서드 | 규칙 enum에 해당하는 한국어 UI 제목을 반환한다. | [L18](../../src/ListingRuleEdit.cs#L18) |
| <a id="symbol-src-listingruleedit-cs-23-76a4041767"></a><code>ListingRules.ValidValue</code><br><code>bool ValidValue(ListingRuleKind kind, int value)</code> | 메서드 | 토글은 0/1, 전리품은 0/1/2만 허용한다. | [L23](../../src/ListingRuleEdit.cs#L23) |
| <a id="symbol-src-listingruleedit-cs-25-3bc63f33a3"></a><code>ListingRules.Label</code><br><code>string Label(ListingRuleKind kind, int? value)</code> | 메서드 | 해당 설정의 저장값을 한국어 표시명으로 변환하고 미지원/알 수 없음도 구분한다. | [L25](../../src/ListingRuleEdit.cs#L25) |
| <a id="symbol-src-listingruleedit-cs-27-4c337f8be4"></a><code>ListingRules.Read</code><br><code>int? Read(string criteria, ListingRuleKind kind)</code> | 메서드 | Criteria의 finder field5 또는 loot field6을 읽어 선택 규칙 값을 반환한다. | [L27](../../src/ListingRuleEdit.cs#L27) |
| <a id="symbol-src-listingruleedit-cs-35-bebad00205"></a><code>ListingRules.WithValue</code><br><code>string WithValue(string criteria, ListingRuleKind kind, int value)</code> | 메서드 | 선택한 finder bit 또는 loot 값만 바꾼 Criteria를 반환한다. 알 수 없는 다른 bit도 보존한다. | [L35](../../src/ListingRuleEdit.cs#L35) |
| <a id="symbol-src-listingruleedit-cs-50-ee4bfdcf43"></a><code>ListingRuleDraft</code><br><code>ListingRuleDraft(RefreshTarget original, ListingRuleKind kind)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L50](../../src/ListingRuleEdit.cs#L50) |
| <a id="symbol-src-listingruleedit-cs-55-8289d194c2"></a><code>ListingRuleDraft.Request</code><br><code>ListingRuleEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, Kind, SelectedValue)</code>. | [L55](../../src/ListingRuleEdit.cs#L55) |
| <a id="symbol-src-listingruleedit-cs-58-27242043f2"></a><code>ListingRuleEdit</code><br><code>ListingRuleEdit(RefreshTarget Original, ListingRuleKind Kind, int Value)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L58](../../src/ListingRuleEdit.cs#L58) |
| <a id="symbol-src-listingruleedit-cs-60-706622980f"></a><code>ListingRuleEdit.Changed</code><br><code>bool Changed</code> | 계산 속성 | 요청한 값이 바인딩한 원본 값과 실제로 다른지 검사한다. 계산식: <code>ListingRules.Read(Original.Criteria, Kind) is { } original &amp;&amp; original != Value</code>. | [L60](../../src/ListingRuleEdit.cs#L60) |
| <a id="symbol-src-listingruleedit-cs-61-17a6c0c6d0"></a><code>ListingRuleEdit.Supports</code><br><code>bool Supports(RefreshTarget target, ListingRuleKind kind)</code> | 메서드 | 현재 source의 지원 layout/읽을 수 있는 해당 조건을 검사한다. 실제 게임 control enabled 검사는 별도 Host 단계다. | [L61](../../src/ListingRuleEdit.cs#L61) |
| <a id="symbol-src-listingruleedit-cs-62-f512dd881a"></a><code>ListingRuleEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. 계산식: <code>Supports(Original, Kind) &amp;&amp; ListingRules.ValidValue(Kind, Value) &amp;&amp; Changed</code>. | [L62](../../src/ListingRuleEdit.cs#L62) |
| <a id="symbol-src-listingruleedit-cs-63-04b68e99b6"></a><code>ListingRuleEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L63](../../src/ListingRuleEdit.cs#L63) |
| <a id="symbol-src-listingruleedit-cs-65-57f2d442b1"></a><code>ListingRuleEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget before)</code> | 메서드 | 현재 before snapshot의 해당 필드만 요청값으로 바꾼 새 RefreshTarget을 만든다. 네이티브 메모리를 쓰지 않는다. | [L65](../../src/ListingRuleEdit.cs#L65) |
| <a id="symbol-src-listingruleedit-cs-70-6d33122a5f"></a><code>ListingRuleEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L70](../../src/ListingRuleEdit.cs#L70) |

<a id="file-src-listinguniquejobsedit-cs"></a>
## src/ListingUniqueJobsEdit.cs

[소스 파일](../../src/ListingUniqueJobsEdit.cs) · 명명된 항목/진입점 13개

잡 중복 없음의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다.

직접 관련 테스트: [ListingUniqueJobsEditTests.cs](tests.md#file-tests-listinguniquejobsedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listinguniquejobsedit-cs-10-26a093bc79"></a><code>ListingUniqueJobs.FromFlags</code><br><code>bool FromFlags(byte flags)</code> | 메서드 | JoinConditionFlags에서 잡 중복 없음 bit를 읽는다. | [L10](../../src/ListingUniqueJobsEdit.cs#L10) |
| <a id="symbol-src-listinguniquejobsedit-cs-11-23f53c2987"></a><code>ListingUniqueJobs.Read</code><br><code>bool? Read(string extraCriteria)</code> | 메서드 | ExtraCriteria에서 참가 조건 flag를 읽어 잡 중복 없음 bool?을 반환한다. | [L11](../../src/ListingUniqueJobsEdit.cs#L11) |
| <a id="symbol-src-listinguniquejobsedit-cs-12-835166a17e"></a><code>ListingUniqueJobs.TryFlags</code><br><code>bool TryFlags(string extraCriteria, out byte flags)</code> | 메서드 | 4필드 ExtraCriteria의 두 번째 필드를 byte flag로 엄격하게 파싱한다. | [L12](../../src/ListingUniqueJobsEdit.cs#L12) |
| <a id="symbol-src-listinguniquejobsedit-cs-18-4530a98a9c"></a><code>ListingUniqueJobs.WithEnabled</code><br><code>string WithEnabled(string extraCriteria, bool enabled)</code> | 메서드 | 잡 중복 없음 bit만 켜거나 끄고 다른 참가 조건 bit는 보존한 ExtraCriteria를 반환한다. | [L18](../../src/ListingUniqueJobsEdit.cs#L18) |
| <a id="symbol-src-listinguniquejobsedit-cs-27-88f46af4bd"></a><code>ListingUniqueJobsDraft</code><br><code>ListingUniqueJobsDraft(RefreshTarget original)</code> | 주 생성자/모델 | 검증한 원본 모집 snapshot에서 UI용 수정 초안을 초기화한다. 실제 게임에 반영하는 것은 Request를 대기열에 넣은 뒤 갱신할 때다. | [L27](../../src/ListingUniqueJobsEdit.cs#L27) |
| <a id="symbol-src-listinguniquejobsedit-cs-31-e05ea584e8"></a><code>ListingUniqueJobsDraft.Request</code><br><code>ListingUniqueJobsEdit Request</code> | 계산 속성 | 현재 관리형 초안 값을 원본 snapshot에 묶인 typed edit로 만든다. 계산식: <code>new(Original, Enabled)</code>. | [L31](../../src/ListingUniqueJobsEdit.cs#L31) |
| <a id="symbol-src-listinguniquejobsedit-cs-34-9d52a9f6db"></a><code>ListingUniqueJobsEdit</code><br><code>ListingUniqueJobsEdit(RefreshTarget Original, bool Enabled)</code> | 주 생성자/모델 | 원본 snapshot과 요청값을 묶는 typed 변경 레코드다. 아래 Valid/MatchesOriginal/Expected/MatchesResult가 실행 전후 검증을 담당한다. | [L34](../../src/ListingUniqueJobsEdit.cs#L34) |
| <a id="symbol-src-listinguniquejobsedit-cs-36-e07de536a8"></a><code>ListingUniqueJobsEdit.Changed</code><br><code>bool Changed</code> | 계산 속성 | 요청한 값이 바인딩한 원본 값과 실제로 다른지 검사한다. 계산식: <code>ListingUniqueJobs.Read(Original.ExtraCriteria) is { } original &amp;&amp; original != Enabled</code>. | [L36](../../src/ListingUniqueJobsEdit.cs#L36) |
| <a id="symbol-src-listinguniquejobsedit-cs-37-3c74564e3f"></a><code>ListingUniqueJobsEdit.Supports</code><br><code>bool Supports(RefreshTarget target)</code> | 메서드 | 현재 source의 지원 layout/읽을 수 있는 해당 조건을 검사한다. 실제 게임 control enabled 검사는 별도 Host 단계다. | [L37](../../src/ListingUniqueJobsEdit.cs#L37) |
| <a id="symbol-src-listinguniquejobsedit-cs-38-c237f1ca2d"></a><code>ListingUniqueJobsEdit.Valid</code><br><code>bool Valid</code> | 계산 속성 | source 지원 여부와 요청값·변경 여부를 합쳐 실행 가능한 edit인지 판정한다. 계산식: <code>Supports(Original) &amp;&amp; Changed</code>. | [L38](../../src/ListingUniqueJobsEdit.cs#L38) |
| <a id="symbol-src-listinguniquejobsedit-cs-39-f99b6c3e29"></a><code>ListingUniqueJobsEdit.MatchesOriginal</code><br><code>bool MatchesOriginal(RefreshTarget current)</code> | 메서드 | 유효한 요청이고 재조회한 listing ID·전체 모집 조건·슬롯/멤버가 요청의 원본과 같은지 검사한다. | [L39](../../src/ListingUniqueJobsEdit.cs#L39) |
| <a id="symbol-src-listinguniquejobsedit-cs-43-6a2cbdaa4e"></a><code>ListingUniqueJobsEdit.Expected</code><br><code>RefreshTarget Expected(RefreshTarget before)</code> | 메서드 | 현재 before snapshot의 해당 필드만 요청값으로 바꾼 새 RefreshTarget을 만든다. 네이티브 메모리를 쓰지 않는다. | [L43](../../src/ListingUniqueJobsEdit.cs#L43) |
| <a id="symbol-src-listinguniquejobsedit-cs-49-e9df10f7d1"></a><code>ListingUniqueJobsEdit.MatchesResult</code><br><code>bool MatchesResult(RefreshTarget before, RefreshTarget after)</code> | 메서드 | 변경한 expected와 재조회 결과의 해당 값·다른 모집 조건·슬롯 보존 여부를 비교한다. | [L49](../../src/ListingUniqueJobsEdit.cs#L49) |
