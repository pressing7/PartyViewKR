# 공통 모집 요청 브리지

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

Discord 콜백은 관리형 snapshot과 queue만 다룬다. query는 새 모집 조회 요청이며 ReadView는 마지막 표시 상태를 읽는다. 실제 게임 읽기·수정은 Framework에서만 실행한다. 로컬 HTTP 서버와 Cloudflare 중계는 제거했다.

기준: 0.3.57.0 소스. main `58fba989007a1ca4e9286eae066422e4dc3134a6` 이후 웹 제거와 공통 브리지 이름 정리를 포함한다. 2026-11-06 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [src/ListingBridge.Edit.cs](#file-src-listingbridge-edit-cs)
- [src/ListingBridge.Recovery.cs](#file-src-listingbridge-recovery-cs)
- [src/ListingBridge.cs](#file-src-listingbridge-cs)
- [src/ListingEditContract.cs](#file-src-listingeditcontract-cs)
- [src/ListingRequestGuard.cs](#file-src-listingrequestguard-cs)
- [src/ListingViewProjection.cs](#file-src-listingviewprojection-cs)
- [src/Plugin.Bridge.cs](#file-src-plugin-bridge-cs)

<a id="file-src-listingbridge-edit-cs"></a>
## src/ListingBridge.Edit.cs

[소스 파일](../../src/ListingBridge.Edit.cs) · 명명된 항목/진입점 8개

외부 수정/갱신 큐와 결과 ledger. session당 write ID는 최대 1024개 유지하고 edit revision은 접수할 때 소비한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingbridge-edit-cs-13-cffc2e2029"></a><code>ListingBridge.PendingWrite</code><br><code>PendingWrite(RefreshTarget Source, ListingChange[] Changes, long Tick)</code> | 주 생성자/모델 | 아직 실행하지 않은 외부 쓰기의 원본 source·변경 배열·접수 시각을 메모리에 보관한다. UUID와 진행 상태는 RemoteOperation이 관리한다. | [L13](../../src/ListingBridge.Edit.cs#L13) |
| <a id="symbol-src-listingbridge-edit-cs-14-fa5edadd47"></a><code>ListingBridge.WriteActive</code><br><code>bool WriteActive</code> | 계산 속성 | 현재 외부 write가 queued/running인지 검사한다. 계산식: <code>view.Operation?.State is "queued" or "running"</code>. | [L14](../../src/ListingBridge.Edit.cs#L14) |
| <a id="symbol-src-listingbridge-edit-cs-16-481a473055"></a><code>ListingBridge.ReadOperation</code><br><code>RemoteReply ReadOperation(string id)</code> | 메서드 | 요청 ID의 작업 결과를 읽는다. 보관된 결과와 이미 소비된 ID를 구분한다. | [L16](../../src/ListingBridge.Edit.cs#L16) |
| <a id="symbol-src-listingbridge-edit-cs-27-051cb888cd"></a><code>ListingBridge.QueueApply</code><br><code>RemoteReply QueueApply(string id, string session, ReadOnlyMemory&lt;byte&gt; body, long now, long? recoveryRevision = null)</code> | 메서드 | body 크기·session·요청 ID 재사용·신선도·진행 여부·편집 revision을 검사해 JSON 변경을 decode한다. 성공 시 수정 source를 소비하고 queued 작업을 만든다. | [L27](../../src/ListingBridge.Edit.cs#L27) |
| <a id="symbol-src-listingbridge-edit-cs-53-19aa91f259"></a><code>ListingBridge.UpdateOperation</code><br><code>void UpdateOperation(IListingEditHost? host, long now)</code> | 메서드 | Framework에서 queued 작업을 host.StartRefresh로 한 번 시작한다. 예외 후에도 다시 시작하지 않고 이후 ReadRefresh로 complete/partial/unconfirmed/failed를 관찰한다. | [L53](../../src/ListingBridge.Edit.cs#L53) |
| <a id="symbol-src-listingbridge-edit-cs-87-ea65825dc6"></a><code>ListingBridge.PublishEditor</code><br><code>void PublishEditor(IListingEditHost? host, ListingSnapshot listing)</code> | 메서드 | 유효한 본인 source를 캡처해 외부 편집 모델을 만든다. 같은 원본은 revision을 유지하고 source가 바뀌면 새 revision을 발급한다. | [L87](../../src/ListingBridge.Edit.cs#L87) |
| <a id="symbol-src-listingbridge-edit-cs-109-daa34e503e"></a><code>ListingBridge.SetOperation</code><br><code>void SetOperation(RemoteOperation operation)</code> | 메서드 | 작업 상태와 제한된 결과 이력을 갱신한다. | [L109](../../src/ListingBridge.Edit.cs#L109) |
| <a id="symbol-src-listingbridge-edit-cs-116-dd90356a58"></a><code>ListingBridge.ClearEdits</code><br><code>void ClearEdits()</code> | 메서드 | 캐릭터/session 종료 시 편집 source·활성 write·결과/중복 방지 기록을 정리한다. | [L116](../../src/ListingBridge.Edit.cs#L116) |

<a id="file-src-listingbridge-recovery-cs"></a>
## src/ListingBridge.Recovery.cs

[소스 파일](../../src/ListingBridge.Recovery.cs) · 명명된 항목/진입점 15개

명시적인 디스코드 수정창 닫기의 공통 queue. 기존 query/apply busy와 무관하게 접수하고 게임 스레드에서 먼저 중단한다. 닫기 ticket의 native 주소는 외부에 직렬화하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingbridge-recovery-cs-4-b05e3f2548"></a><code>RecruitmentWindowTicket</code><br><code>RecruitmentWindowTicket(nint Address, long ClosedRevision)</code> | 주 생성자/모델 | 창 주소와 PreFinalize revision을 묶어 같은 위치에 재생성된 다른 창을 닫지 않도록 한다. 관리형 내부 비교에만 사용한다. | [L4](../../src/ListingBridge.Recovery.cs#L4) |
| <a id="symbol-src-listingbridge-recovery-cs-5-3696061ae5"></a><code>RecruitmentWindows</code><br><code>RecruitmentWindows(RecruitmentWindowTicket Editor, RecruitmentWindowTicket Roles)</code> | 주 생성자/모델 | 모집 수정창과 직업 선택 자식창의 ticket을 묶는다. 주소0은 창이 보이지 않음을 뜻한다. | [L5](../../src/ListingBridge.Recovery.cs#L5) |
| <a id="symbol-src-listingbridge-recovery-cs-10-91f424eca7"></a><code>IListingRecoveryHost.StopForRecovery</code><br><code>void StopForRecovery()</code> | 인터페이스 계약 | 진행 중인 query/refresh와 초안을 중단하는 게임 스레드 계약이다. 제출을 되돌리지는 않는다. | [L10](../../src/ListingBridge.Recovery.cs#L10) |
| <a id="symbol-src-listingbridge-recovery-cs-11-e7e4fedbae"></a><code>IListingRecoveryHost.ReadRecoveryWindows</code><br><code>bool ReadRecoveryWindows(out RecruitmentWindows windows)</code> | 인터페이스 계약 | 두 모집 관련 창의 현재 ticket을 읽는 계약이다. 읽기 실패와 창 없음은 구분한다. | [L11](../../src/ListingBridge.Recovery.cs#L11) |
| <a id="symbol-src-listingbridge-recovery-cs-12-63ba0553ed"></a><code>IListingRecoveryHost.CloseRecoveryWindow</code><br><code>bool CloseRecoveryWindow(RecruitmentWindow kind, RecruitmentWindowTicket ticket, out string error)</code> | 인터페이스 계약 | 명시한 종류와 같은 ticket의 창만 닫는 계약이다. 제출/확인 버튼 조작을 포함하지 않는다. | [L12](../../src/ListingBridge.Recovery.cs#L12) |
| <a id="symbol-src-listingbridge-recovery-cs-14-45df110119"></a><code>RemoteRecovery</code><br><code>RemoteRecovery(string Id, string State, string Message)</code> | 주 생성자/모델 | 닫기 요청 UUID·상태·표시 문구만 외부 interaction에 전달한다. | [L14](../../src/ListingBridge.Recovery.cs#L14) |
| <a id="symbol-src-listingbridge-recovery-cs-24-60d3568505"></a><code>ListingBridge.RecoveryRevision</code><br><code>long RecoveryRevision</code> | 계산 속성 | 닫기 접수 세대 번호를 lock 안에서 읽는다. 닫기 전에 만들어진 DiscordWork의 늦은 ACK 실행을 거부한다. | [L24](../../src/ListingBridge.Recovery.cs#L24) |
| <a id="symbol-src-listingbridge-recovery-cs-25-e1e023ad02"></a><code>ListingBridge.RecoveryActive</code><br><code>bool RecoveryActive</code> | 계산 속성 | 닫기 요청이 queued 또는 running인지 lock 안에서 반환한다. | [L25](../../src/ListingBridge.Recovery.cs#L25) |
| <a id="symbol-src-listingbridge-recovery-cs-27-5d18fc31bf"></a><code>ListingBridge.QueueCloseEditor</code><br><code>RemoteReply QueueCloseEditor(string id, string session, long now)</code> | 메서드 | 소유권 인증 이후 session·UUID·로그인·최근 발행 상태를 검사하고 중복을 재사용한다. 진행 중 query/apply를 중단 상태로 바꾸고 초안 source를 비운다. 네이티브 호출은 하지 않는다. | [L27](../../src/ListingBridge.Recovery.cs#L27) |
| <a id="symbol-src-listingbridge-recovery-cs-54-0e5dfc897f"></a><code>ListingBridge.ReadRecovery</code><br><code>RemoteReply ReadRecovery(string id)</code> | 메서드 | UUID에 해당하는 닫기 결과를 lock 안에서 읽는다. 반복 호출은 상태만 조회한다. | [L54](../../src/ListingBridge.Recovery.cs#L54) |
| <a id="symbol-src-listingbridge-recovery-cs-61-e342ce5403"></a><code>ListingBridge.BeforeReadRecovery</code><br><code>void BeforeReadRecovery(IListingRecoveryHost host, long now)</code> | 메서드 | Framework의 일반 query/refresh 진행보다 앞서 닫기 요청을 처리한다. Bridge.Update에서도 호출되므로 각 close는 sent 표시로1회만 보낸다. | [L61](../../src/ListingBridge.Recovery.cs#L61) |
| <a id="symbol-src-listingbridge-recovery-cs-66-8523894d03"></a><code>ListingBridge.ProcessRecovery</code><br><code>void ProcessRecovery(IListingRecoveryHost? host, long now)</code> | 메서드 | 5초 queued 유효기간·같은 캐릭터를 확인하고 먼저 진행을 취소한다. 창 ticket을 캡처해 자식 닫힘→부모 닫힘을 관찰하며5초 안에 둘 다 사라져야 성공한다. 새 창·native guard·예외는 중단하고 불확실한 close를 반복하지 않는다. | [L66](../../src/ListingBridge.Recovery.cs#L66) |
| <a id="symbol-src-listingbridge-recovery-cs-109-d4b96ec1be"></a><code>ListingBridge.FinishRecovery</code><br><code>void FinishRecovery(bool complete, string message)</code> | 메서드 | 닫기 결과를 complete/failed와 문구로 고정한다. 실패를 성공으로 취급하지 않는다. | [L109](../../src/ListingBridge.Recovery.cs#L109) |
| <a id="symbol-src-listingbridge-recovery-cs-110-18a8583dbc"></a><code>ListingBridge.SetRecovery</code><br><code>void SetRecovery(RemoteRecovery value)</code> | 메서드 | 현재 닫기 상태와 세션 내 UUID 결과 ledger를 함께 갱신한다. | [L110](../../src/ListingBridge.Recovery.cs#L110) |
| <a id="symbol-src-listingbridge-recovery-cs-111-b86f8e9b60"></a><code>ListingBridge.ClearRecovery</code><br><code>void ClearRecovery()</code> | 메서드 | 로그아웃/캐릭터 변경/연결 종료 시 닫기 상태와 ticket 및 결과 기록을 지운다. | [L111](../../src/ListingBridge.Recovery.cs#L111) |

<a id="file-src-listingbridge-cs"></a>
## src/ListingBridge.cs

[소스 파일](../../src/ListingBridge.cs) · 명명된 항목/진입점 13개

네트워크와 Framework 사이의 lock 기반 관리형 경계. Discord 콜백은 관리형 snapshot을 읽고 요청을 큐에 넣으며 실제 게임 실행은 Update에서만 한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingbridge-cs-13-159c72e3f8"></a><code>IListingQueryHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 공통 수동 조회 경로를 호출하고 실제 발급된 request revision을 소유권 guard에 기록한다. | [L13](../../src/ListingBridge.cs#L13) |
| <a id="symbol-src-listingbridge-cs-16-e6629b8c6d"></a><code>RemoteReply</code><br><code>RemoteReply(int Status, object Body)</code> | 주 생성자/모델 | 내부 상태 코드와 관리형 결과 객체를 반환한다. 숫자 코드는 기존 Discord 계약과의 호환을 위해 유지하며 HTTP 서버는 없다. | [L16](../../src/ListingBridge.cs#L16) |
| <a id="symbol-src-listingbridge-cs-33-b60c946744"></a><code>ListingBridge.ListingBridge</code><br><code>ListingBridge(string version)</code> | 생성자 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L32](../../src/ListingBridge.cs#L32) |
| <a id="symbol-src-listingbridge-cs-40-b637b8cee3"></a><code>ListingBridge.Empty</code><br><code>RemoteView Empty(string reason)</code> | 메서드 | 버전/API/session을 포함한 초기 비연결 RemoteView를 만든다. | [L38](../../src/ListingBridge.cs#L38) |
| <a id="symbol-src-listingbridge-cs-42-4501e34e05"></a><code>ListingBridge.Active</code><br><code>bool Active</code> | 계산 속성 | 현재 Query가 queued 또는 running인지 판정한다. 외부 쓰기의 진행 상태는 별도 WriteActive로 검사한다. 계산식: <code>view.Query?.State is "queued" or "running"</code>. | [L40](../../src/ListingBridge.cs#L40) |
| <a id="symbol-src-listingbridge-cs-44-71ddd93b61"></a><code>ListingBridge.ReadView</code><br><code>RemoteView ReadView()</code> | 메서드 | 동일한 관리형 RemoteView snapshot을 반환한다. Discord가 공통 표시/편집 모델을 재사용한다. | [L41](../../src/ListingBridge.cs#L41) |
| <a id="symbol-src-listingbridge-cs-45-8d703fd5db"></a><code>ListingBridge.ReadQuery</code><br><code>RemoteReply ReadQuery(string id)</code> | 메서드 | canonical GUID 형식을 검사하고 조회 요청 ID의 보관된 결과를 반환한다. | [L42](../../src/ListingBridge.cs#L42) |
| <a id="symbol-src-listingbridge-cs-51-d2f48ab912"></a><code>ListingBridge.QueueQuery</code><br><code>RemoteReply QueueQuery(string id, string session, long now, long? recoveryRevision = null)</code> | 메서드 | 정지·session·중복 ID·snapshot freshness·진행 작업·2초 조회 제한을 검사해 조회만 예약한다. 게임 메모리는 접근하지 않는다. | [L48](../../src/ListingBridge.cs#L48) |
| <a id="symbol-src-listingbridge-cs-72-a0e0ad04c2"></a><code>ListingBridge.Update</code><br><code>void Update(IListingQueryHost host, long now)</code> | 메서드 | Framework에서 캐릭터/session 변경을 처리하고 queued query를 host.RequestQuery로 실행한다. 새 revision과 본인 검증을 확인해 조회 결과를 완료하고 edit/operation/state snapshot을 발행한다. | [L68](../../src/ListingBridge.cs#L68) |
| <a id="symbol-src-listingbridge-cs-127-c6f15af493"></a><code>ListingBridge.PublicationFailed</code><br><code>void PublicationFailed()</code> | 메서드 | snapshot 발행 오류를 기록하고 stale source·미실행 요청을 무효화한다. 이미 실행한 native write를 자동 재시도하지 않는다. | [L123](../../src/ListingBridge.cs#L123) |
| <a id="symbol-src-listingbridge-cs-142-b9d3145a76"></a><code>ListingBridge.Stop</code><br><code>void Stop()</code> | 메서드 | Bridge를 비활성화하고 pending query/edit source와 session 상태를 정리한다. | [L137](../../src/ListingBridge.cs#L137) |
| <a id="symbol-src-listingbridge-cs-152-9f835c3461"></a><code>ListingBridge.SetQuery</code><br><code>void SetQuery(RemoteQuery query)</code> | 메서드 | 조회 요청 상태를 교체하고 제한된 결과 이력을 관리한다. | [L147](../../src/ListingBridge.cs#L147) |
| <a id="symbol-src-listingbridge-cs-160-c5b477a285"></a><code>ListingBridge.Error</code><br><code>RemoteReply Error(int status, string message)</code> | 메서드 | 내부 상태 코드와 오류 문구를 가진 RemoteReply를 만든다. | [L154](../../src/ListingBridge.cs#L154) |

<a id="file-src-listingeditcontract-cs"></a>
## src/ListingEditContract.cs

[소스 파일](../../src/ListingEditContract.cs) · 명명된 항목/진입점 17개

외부 editor DTO와 strict JSON decoder. 비밀번호는 모델/상태 응답에 포함하지 않는다. 최대 body64KiB, 변경59개(48자리+11필드)다.

직접 관련 테스트: [ListingEditContractTests.cs](tests.md#file-tests-listingeditcontracttests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingeditcontract-cs-6-63df29f01e"></a><code>RemoteJobChoice</code><br><code>RemoteJobChoice(uint Id, string Name)</code> | 주 생성자/모델 | 외부 editor가 선택할 직업 ID·이름을 표현한다. | [L6](../../src/ListingEditContract.cs#L6) |
| <a id="symbol-src-listingeditcontract-cs-7-ac2c21e520"></a><code>RemoteEditSlot</code><br><code>RemoteEditSlot(int NativeIndex, string Position, uint[] Jobs)</code> | 주 생성자/모델 | 외부에서 편집 가능한 등록 빈자리의 물리 index·그룹/표시·선택 직업 목록을 표현한다. | [L7](../../src/ListingEditContract.cs#L7) |
| <a id="symbol-src-listingeditcontract-cs-8-bcfc362469"></a><code>RemoteEditModel</code><br><code>RemoteEditModel(string Revision, Dictionary&lt;string, object?&gt; Values, string[] Supported, RemoteJobChoice[] Jobs, RemoteEditSlot[] Slots)</code> | 주 생성자/모델 | 원본 revision·지원 필드·현재 값·자리/직업 선택지를 전달한다. PIN 원문은 제외한다. | [L8](../../src/ListingEditContract.cs#L8) |
| <a id="symbol-src-listingeditcontract-cs-10-e49ac94cd9"></a><code>RemoteItemResult</code><br><code>RemoteItemResult(string Key, string Title, bool? Applied, string Message)</code> | 주 생성자/모델 | 한 수정 항목의 key/title/요청값/결과 문구와 상태를 외부 표시용으로 묶는다. | [L10](../../src/ListingEditContract.cs#L10) |
| <a id="symbol-src-listingeditcontract-cs-11-8fec41c5ad"></a><code>RemoteOperation</code><br><code>RemoteOperation(string Id, string State, string Stage, string Message, bool EditorStarted, bool Submitted, RemoteItemResult[] Results)</code> | 주 생성자/모델 | 요청 UUID·진행 상태/단계·요약·수정창 진입/제출 여부·항목별 결과를 표현한다. session은 바깥 RemoteView와 요청 검증에서 관리한다. | [L11](../../src/ListingEditContract.cs#L11) |
| <a id="symbol-src-listingeditcontract-cs-13-20c7ce93f8"></a><code>RemoteRefreshProgress</code><br><code>RemoteRefreshProgress(bool Busy, string Stage, string Message, bool EditorStarted, bool Submitted, RemoteItemResult[] Results)</code> | 주 생성자/모델 | 게임 갱신 stage·요약·배치 항목 결과를 관리형 외부 상태로 전달한다. | [L13](../../src/ListingEditContract.cs#L13) |
| <a id="symbol-src-listingeditcontract-cs-16-9c22a15979"></a><code>RemoteRefreshProgress.From</code><br><code>RemoteRefreshProgress From(ListingRefresh refresh)</code> | 메서드 | ListingRefresh의 stage·상태·배치 항목 결과를 RemoteRefreshProgress로 변환한다. | [L16](../../src/ListingEditContract.cs#L16) |
| <a id="symbol-src-listingeditcontract-cs-24-e031fc93d4"></a><code>IListingEditHost.CaptureEditTarget</code><br><code>RefreshTarget? CaptureEditTarget()</code> | 인터페이스 계약 | 인터페이스 계약. 진행 작업이 없고 현재 캐릭터의 live Self 상세가 검증된 경우에만 편집 source를 캡처한다. | [L24](../../src/ListingEditContract.cs#L24) |
| <a id="symbol-src-listingeditcontract-cs-25-80c4cb7a56"></a><code>IListingEditHost.StartRefresh</code><br><code>bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 외부 source와 현재 본인/차단 사유를 검사한다. changes가 비면 단순 갱신, 있으면 batch를 요청하고 생성된 refresh serial을 guard에 기록한다. | [L25](../../src/ListingEditContract.cs#L25) |
| <a id="symbol-src-listingeditcontract-cs-26-2bb7214a65"></a><code>IListingEditHost.ReadRefresh</code><br><code>RemoteRefreshProgress? ReadRefresh()</code> | 인터페이스 계약 | 인터페이스 계약. 자기가 시작한 refresh serial과 현재 serial이 같을 때만 진행 결과를 반환한다. | [L26](../../src/ListingEditContract.cs#L26) |
| <a id="symbol-src-listingeditcontract-cs-32-6ced374a97"></a><code>ListingEditContract.SameSource</code><br><code>bool SameSource(RefreshTarget a, RefreshTarget b)</code> | 메서드 | 대기 변경의 원본 비교에 더해 JobEditingAllowed와 직업 카탈로그 등 편집 가능 근거까지 같은지 검사한다. | [L32](../../src/ListingEditContract.cs#L32) |
| <a id="symbol-src-listingeditcontract-cs-36-1b892d6911"></a><code>ListingEditContract.Model</code><br><code>RemoteEditModel Model(RefreshTarget source, string revision)</code> | 메서드 | RefreshTarget에서 supported field·현재 값·빈 등록 자리·선택 가능한 직업과 새 revision이 포함된 RemoteEditModel을 만든다. | [L36](../../src/ListingEditContract.cs#L36) |
| <a id="symbol-src-listingeditcontract-cs-70-2fc2a7905b"></a><code>ListingEditContract.Decode</code><br><code>bool Decode(ReadOnlyMemory&lt;byte&gt; body, RefreshTarget source, string revision, out ListingChange[] changes, out string error)</code> | 메서드 | 깊이/속성 중복/미지 key/enum/문자열/직업 ID/자리/PIN을 검증해 ListingChange 배열로 변환한다. no-op는 제외하고 raw NativeIndex는 표시 position으로 다시 매핑한다. | [L70](../../src/ListingEditContract.cs#L70) |
| <a id="symbol-src-listingeditcontract-cs-145-d7a3d8f046"></a><code>ListingEditContract.Keys</code><br><code>bool Keys(JsonElement value, params string[] expected)</code> | 메서드 | JSON object의 필드 집합이 기대 allowlist와 정확히 맞는지 검사한다. | [L145](../../src/ListingEditContract.cs#L145) |
| <a id="symbol-src-listingeditcontract-cs-147-74f031978a"></a><code>ListingEditContract.Completion</code><br><code>ListingCompletionEdit Completion(RefreshTarget source, int value)</code> | 메서드 | 외부 완료조건 값 1/2/4를 enabled/index typed edit로 변환한다. | [L147](../../src/ListingEditContract.cs#L147) |
| <a id="symbol-src-listingeditcontract-cs-150-65577d307a"></a><code>ListingEditContract.ItemLevel</code><br><code>ListingItemLevelEdit ItemLevel(RefreshTarget source, int value)</code> | 메서드 | 외부 평균 레벨 0 또는 1~999를 typed edit로 변환한다. | [L150](../../src/ListingEditContract.cs#L150) |
| <a id="symbol-src-listingeditcontract-cs-152-54c616007a"></a><code>ListingEditContract.Rule</code><br><code>ListingRuleEdit Rule(RefreshTarget source, string key, int value)</code> | 메서드 | 외부 finder/loot 값을 유효한 ListingRuleEdit로 변환한다. | [L152](../../src/ListingEditContract.cs#L152) |

<a id="file-src-listingrequestguard-cs"></a>
## src/ListingRequestGuard.cs

[소스 파일](../../src/ListingRequestGuard.cs) · 명명된 항목/진입점 8개

외부 연결이 소유한 조회/갱신 serial만 취소할 수 있게 한다. 다른 수동 요청을 잘못 취소하지 않도록 source identity와 update 간격을 검사한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingrequestguard-cs-11-9a6a7af24d"></a><code>ListingRequestGuard.Started</code><br><code>void Started(ulong selfId, long requestRevision, long now)</code> | 메서드 | 외부가 시작한 본인 CID·요청 revision·시각을 기록해 소유권을 고정한다. | [L11](../../src/ListingRequestGuard.cs#L11) |
| <a id="symbol-src-listingrequestguard-cs-13-106328294e"></a><code>ListingRequestGuard.Owns</code><br><code>bool Owns(long requestRevision, bool busy)</code> | 메서드 | 현재 실행 중인 요청 serial이 이 guard가 시작한 것과 같은지 검사한다. | [L13](../../src/ListingRequestGuard.cs#L13) |
| <a id="symbol-src-listingrequestguard-cs-14-1b4cfbe6d1"></a><code>ListingRequestGuard.BeforeRead</code><br><code>bool BeforeRead(ulong selfId, long requestRevision, bool busy, bool enabled, bool blocked, long now)</code> | 메서드 | 연결 종료·캐릭터 변경·충돌 상태·너무 늦은 Framework 처리 여부를 검사한다. refresh guard의 제한은 update 공백이며 전체 배치 수명 제한과 다르다. | [L14](../../src/ListingRequestGuard.cs#L14) |
| <a id="symbol-src-listingrequestguard-cs-20-fcb0c4309e"></a><code>ListingRequestGuard.Clear</code><br><code>void Clear()</code> | 메서드 | 소유한 요청의 CID/revision/시각을 초기화한다. | [L20](../../src/ListingRequestGuard.cs#L20) |
| <a id="symbol-src-listingrequestguard-cs-29-4098b278f0"></a><code>ListingRefreshGuard.Started</code><br><code>void Started(ulong selfId, long serial, long now)</code> | 메서드 | 외부가 시작한 본인 CID·요청 revision·시각을 기록해 소유권을 고정한다. | [L29](../../src/ListingRequestGuard.cs#L29) |
| <a id="symbol-src-listingrequestguard-cs-30-e06e7664d7"></a><code>ListingRefreshGuard.Owns</code><br><code>bool Owns(long serial, bool busy)</code> | 메서드 | 현재 실행 중인 요청 serial이 이 guard가 시작한 것과 같은지 검사한다. | [L30](../../src/ListingRequestGuard.cs#L30) |
| <a id="symbol-src-listingrequestguard-cs-31-54513b4c5d"></a><code>ListingRefreshGuard.BeforeRead</code><br><code>bool BeforeRead(ulong selfId, long serial, bool busy, bool enabled, bool queued, long now)</code> | 메서드 | 연결 종료·캐릭터 변경·충돌 상태·너무 늦은 Framework 처리 여부를 검사한다. refresh guard의 제한은 update 공백이며 전체 배치 수명 제한과 다르다. | [L31](../../src/ListingRequestGuard.cs#L31) |
| <a id="symbol-src-listingrequestguard-cs-37-5471591a54"></a><code>ListingRefreshGuard.Clear</code><br><code>void Clear()</code> | 메서드 | 소유한 요청의 CID/revision/시각을 초기화한다. | [L37](../../src/ListingRequestGuard.cs#L37) |

<a id="file-src-listingviewprojection-cs"></a>
## src/ListingViewProjection.cs

[소스 파일](../../src/ListingViewProjection.cs) · 명명된 항목/진입점 7개

외부에 노출하는 최소 관리형 표시 모델. 원시 CID/flag/PIN/상세 진단을 제외하고 전체 그룹과 등록 직업·허용 직업 ID를 포함한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingviewprojection-cs-5-5e6db5a6df"></a><code>RemoteSetting</code><br><code>RemoteSetting(string Key, string Label, string Value)</code> | 주 생성자/모델 | 조건 key·한국어 이름·표시값·활성 여부를 보관한다. | [L5](../../src/ListingViewProjection.cs#L5) |
| <a id="symbol-src-listingviewprojection-cs-11-6cda1b6ac9"></a><code>RemoteSlot</code><br><code>RemoteSlot(int NativeIndex, string Position, bool HasMember, string Job, bool Anyone, bool RulesComplete, string[] AllowedJobs)</code> | 주 생성자/모델 | CID/raw mask 없이 자리 표시·등록 직업·참가 여부·허용 직업과 물리 index를 보관한다. | [L11](../../src/ListingViewProjection.cs#L11) |
| <a id="symbol-src-listingviewprojection-cs-18-4e41fc6e6f"></a><code>RemoteGroup</code><br><code>RemoteGroup(string Name, RemoteSlot[] Slots)</code> | 주 생성자/모델 | 일반/연합/친선 그룹 이름과 등록 자리 배열을 묶는다. | [L18](../../src/ListingViewProjection.cs#L18) |
| <a id="symbol-src-listingviewprojection-cs-19-7a599cfe1a"></a><code>RemoteListing</code><br><code>RemoteListing(string Leader, string Duty, string Description, string Remaining, DateTimeOffset? ReadAt, bool Live, int? Filled, int? Total, bool SlotsReadable, bool CountsAgree, RemoteSetting[] Settings, RemoteGroup[] Groups)</code> | 주 생성자/모델 | Discord에 표시할 임무·소개말·인원·시간·ReadAt·조건·그룹 정보다. | [L19](../../src/ListingViewProjection.cs#L19) |
| <a id="symbol-src-listingviewprojection-cs-22-f32749aa96"></a><code>RemoteQuery</code><br><code>RemoteQuery(string Id, string State, string Message)</code> | 주 생성자/모델 | query UUID·단계·결과 문구를 보관한다. | [L22](../../src/ListingViewProjection.cs#L22) |
| <a id="symbol-src-listingviewprojection-cs-23-f41a781cee"></a><code>RemoteView</code><br><code>RemoteView(int ApiVersion, string Version, string Session, bool LoggedIn, bool CanQuery, string Reason, string Status, DateTimeOffset UpdatedAt, RemoteListing? Listing, RemoteQuery? Query)</code> | 주 생성자/모델 | 관리형 표시 모델 v2의 session·발행시각·로그인/조회 가능 사유·모집글·editor·operation 상태를 묶는다. | [L23](../../src/ListingViewProjection.cs#L23) |
| <a id="symbol-src-listingviewprojection-cs-34-a2f13eb83e"></a><code>ListingViewProjection.From</code><br><code>RemoteListing? From(ListingSnapshot listing, bool loggedIn, bool queryBusy)</code> | 메서드 | 로그인한 현재 캐릭터의 모집 snapshot만 임무·소개말·설정·모든 그룹/자리로 변환한다. 친선 팀 이름과 물리 그룹 기준을 유지하고 조회 중/오래된 데이터의 Live를 구분한다. | [L34](../../src/ListingViewProjection.cs#L34) |

<a id="file-src-plugin-bridge-cs"></a>
## src/Plugin.Bridge.cs

[소스 파일](../../src/Plugin.Bridge.cs) · 명명된 항목/진입점 19개

Plugin에서 Discord의 모집 요청 브리지를 Framework에 연결하고 요청 소유권을 관리한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-bridge-cs-14-4ad052e971"></a><code>Plugin.QueryUnavailableReason</code><br><code>string? QueryUnavailableReason</code> | 계산 속성 | 새 수동/외부 조회가 불가능한 이유를 공통 guard로 계산한다. 계산식: <code>GetQueryUnavailableReason(false)</code>. | [L7](../../src/Plugin.Bridge.cs#L7) |
| <a id="symbol-src-plugin-bridge-cs-15-996aade56a"></a><code>Plugin.GetQueryUnavailableReason</code><br><code>string? GetQueryUnavailableReason(bool ownQueryInProgress, bool ownQueuedRefresh = false)</code> | 메서드 | 종료·복구·접속/이동·native·게임창·초안/요청·조회·갱신·trace 충돌을 확인한다. ownQueryInProgress/ownQueuedRefresh 예외는 해당 요청의 자체 busy 검사만 제외하고 다른 guard는 유지한다. | [L8](../../src/Plugin.Bridge.cs#L8) |
| <a id="symbol-src-plugin-bridge-cs-30-4067b3b4c4"></a><code>Plugin.TryQueryOwnListing</code><br><code>bool TryQueryOwnListing(out string error)</code> | 메서드 | 공통 조회 guard 통과 후 ListingReader.RequestOwn과 RequestRead를 예약한다. | [L23](../../src/Plugin.Bridge.cs#L23) |
| <a id="symbol-src-plugin-bridge-cs-67-2b92448afd"></a><code>Plugin.UpdateDiscordBridge</code><br><code>void UpdateDiscordBridge(long now)</code> | 메서드 | 활성 Discord 연결의 Bridge.Update를 게임 스레드에서 호출한다. 발행 실패는 PublicationFailed로 처리한다. | [L30](../../src/Plugin.Bridge.cs#L30) |
| <a id="symbol-src-plugin-bridge-cs-75-d6d19b3cd6"></a><code>Plugin.DiscordHost</code><br><code>DiscordHost(Plugin plugin)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L38](../../src/Plugin.Bridge.cs#L38) |
| <a id="symbol-src-plugin-bridge-cs-80-b195cf1ca5"></a><code>Plugin.DiscordHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | 현재 로그인한 본인의 ContentId를 반환한다. 로그아웃/미준비는0이다. 계산식: <code>ClientState.IsLoggedIn &amp;&amp; PlayerState.IsLoaded ? PlayerState.ContentId : 0</code>. | [L43](../../src/Plugin.Bridge.cs#L43) |
| <a id="symbol-src-plugin-bridge-cs-81-9e3694263a"></a><code>Plugin.DiscordHost.QueryBlockReason</code><br><code>string? QueryBlockReason</code> | 계산 속성 | 현재 플러그인의 공통 조회 차단 사유를 반환한다. 계산식: <code>plugin.QueryUnavailableReason</code>. | [L44](../../src/Plugin.Bridge.cs#L44) |
| <a id="symbol-src-plugin-bridge-cs-82-203d91cc94"></a><code>Plugin.DiscordHost.QueryBusy</code><br><code>bool QueryBusy</code> | 계산 속성 | ListingReader의 조회 진행 상태를 반환한다. 계산식: <code>plugin.listingReader.Busy</code>. | [L45](../../src/Plugin.Bridge.cs#L45) |
| <a id="symbol-src-plugin-bridge-cs-83-eda4e435d3"></a><code>Plugin.DiscordHost.QuerySucceeded</code><br><code>bool QuerySucceeded</code> | 계산 속성 | 마지막 본인 조회가 검증까지 성공했는지 반환한다. 계산식: <code>plugin.listingReader.LastOwnRequestSucceeded</code>. | [L46](../../src/Plugin.Bridge.cs#L46) |
| <a id="symbol-src-plugin-bridge-cs-84-65bb7efb22"></a><code>Plugin.DiscordHost.DetailRevision</code><br><code>long DetailRevision</code> | 계산 속성 | 상세창 PostSetup/PostRefresh revision을 읽는다. 계산식: <code>plugin.listingReader.DetailRevision</code>. | [L47](../../src/Plugin.Bridge.cs#L47) |
| <a id="symbol-src-plugin-bridge-cs-85-111436a52d"></a><code>Plugin.DiscordHost.Listing</code><br><code>ListingSnapshot Listing</code> | 계산 속성 | 현재 관리형 ListingSnapshot을 반환한다. 계산식: <code>plugin.Listing</code>. | [L48](../../src/Plugin.Bridge.cs#L48) |
| <a id="symbol-src-plugin-bridge-cs-86-7dcd44fa22"></a><code>Plugin.DiscordHost.RefreshBlockReason</code><br><code>string? RefreshBlockReason</code> | 계산 속성 | 공통 조회 차단에 인게임 대기 변경/cooldown 등을 더해 외부 갱신 거부 이유를 반환한다. 계산식: <code>plugin.QueryUnavailableReason ??             (plugin.PendingChanges.Items.Count &gt; 0 ? "PVKR에 적용 대기 중인 변경사항을 먼저 처리해 주세요." :             plugin.Refresh.CoolingDown(Environment.TickCount64) ? "잠시 후 다시 갱신해 주세요." : null)</code>. | [L49](../../src/Plugin.Bridge.cs#L49) |
| <a id="symbol-src-plugin-bridge-cs-89-e3e725bb4a"></a><code>Plugin.DiscordHost.CaptureEditTarget</code><br><code>RefreshTarget? CaptureEditTarget()</code> | 메서드 | 진행 작업이 없고 현재 캐릭터의 live Self 상세가 검증된 경우에만 편집 source를 캡처한다. | [L52](../../src/Plugin.Bridge.cs#L52) |
| <a id="symbol-src-plugin-bridge-cs-95-a2d53c315a"></a><code>Plugin.DiscordHost.StartRefresh</code><br><code>bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error)</code> | 메서드 | 외부 source와 현재 본인/차단 사유를 검사한다. changes가 비면 단순 갱신, 있으면 batch를 요청하고 생성된 refresh serial을 guard에 기록한다. | [L58](../../src/Plugin.Bridge.cs#L58) |
| <a id="symbol-src-plugin-bridge-cs-108-d14c41e672"></a><code>Plugin.DiscordHost.ReadRefresh</code><br><code>RemoteRefreshProgress? ReadRefresh()</code> | 메서드 | 자기가 시작한 refresh serial과 현재 serial이 같을 때만 진행 결과를 반환한다. | [L71](../../src/Plugin.Bridge.cs#L71) |
| <a id="symbol-src-plugin-bridge-cs-110-cf52098510"></a><code>Plugin.DiscordHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 메서드 | 공통 수동 조회 경로를 호출하고 실제 발급된 request revision을 소유권 guard에 기록한다. | [L73](../../src/Plugin.Bridge.cs#L73) |
| <a id="symbol-src-plugin-bridge-cs-115-7bcc4b593c"></a><code>Plugin.DiscordHost.BeforeRead</code><br><code>void BeforeRead(long now)</code> | 메서드 | native dispatch 직전에 외부 연결·캐릭터·대기 시간·충돌을 재검사한다. 자신 소유의 query/refresh만 취소한다. | [L78](../../src/Plugin.Bridge.cs#L78) |
| <a id="symbol-src-plugin-bridge-cs-127-bb3d1aaadc"></a><code>Plugin.DiscordHost.CancelOwnedQuery</code><br><code>void CancelOwnedQuery()</code> | 메서드 | 현재 query serial이 이 연결 소유일 때만 FailRequest하고 guard를 비운다. | [L90](../../src/Plugin.Bridge.cs#L90) |
| <a id="symbol-src-plugin-bridge-cs-134-6d1c8a6303"></a><code>Plugin.DiscordHost.CancelOwnedRefresh</code><br><code>void CancelOwnedRefresh()</code> | 메서드 | 현재 refresh serial이 이 연결 소유일 때만 Cancel하고 guard를 비운다. | [L97](../../src/Plugin.Bridge.cs#L97) |
