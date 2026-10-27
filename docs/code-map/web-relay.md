# 공통 외부 브리지·웹·Cloudflare 중계

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

네트워크 thread는 관리형 snapshot과 queue만 다룬다. query는 새 모집 조회 요청이고 GET state polling은 마지막 snapshot 읽기다. loopback/Cloudflare/Discord 모두 같은 editor/refresh 의미를 공유한다.

기준: 0.3.53.0 소스. 기반 main `b1ec51fdd867c2a80f214c95df20a9f80ad722ae` 이후 발신자 번호 제거·효과음 멘션·단일 직업 공석 제외 완료 알림을 포함한다. 2026-10-28 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [relay/build.mjs](#file-relay-build-mjs)
- [relay/src/protocol.mjs](#file-relay-src-protocol-mjs)
- [relay/src/worker.mjs](#file-relay-src-worker-mjs)
- [src/ListingRelayClient.cs](#file-src-listingrelayclient-cs)
- [src/ListingRelayProtocol.cs](#file-src-listingrelayprotocol-cs)
- [src/ListingWebBridge.Edit.cs](#file-src-listingwebbridge-edit-cs)
- [src/ListingWebBridge.Recovery.cs](#file-src-listingwebbridge-recovery-cs)
- [src/ListingWebBridge.cs](#file-src-listingwebbridge-cs)
- [src/ListingWebEdit.cs](#file-src-listingwebedit-cs)
- [src/ListingWebRequestGuard.cs](#file-src-listingwebrequestguard-cs)
- [src/ListingWebServer.cs](#file-src-listingwebserver-cs)
- [src/ListingWebView.cs](#file-src-listingwebview-cs)
- [src/Plugin.Relay.cs](#file-src-plugin-relay-cs)
- [src/Plugin.Web.cs](#file-src-plugin-web-cs)
- [src/Web/app.js](#file-src-web-app-js)
- [src/Web/connection.js](#file-src-web-connection-js)
- [src/Web/editor.js](#file-src-web-editor-js)

<a id="file-relay-build-mjs"></a>
## relay/build.mjs

[소스 파일](../../relay/build.mjs) · 명명된 항목/진입점 1개

플러그인 embedded Web UI와 중계 UI의 단일 소스를 유지하는 빌드 단계.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-relay-build-mjs-1-6726445b5d"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | relay/assets를 만들고 src/Web의 index.html/style.css/app.js/connection.js/editor.js 5개를 복사한다. 서버 코드 build 또는 배포 자체는 수행하지 않는다. | [L1](../../relay/build.mjs#L1) |

<a id="file-relay-src-protocol-mjs"></a>
## relay/src/protocol.mjs

[소스 파일](../../relay/src/protocol.mjs) · 명명된 항목/진입점 10개

Worker 공통 HTTP/인증/route/body helper. 호스트와 브라우저의 허용 경계를 정한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-relay-src-protocol-mjs-1-e78f30485a"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | 토큰/room/UUID 규칙, body/frame 제한, 보안 header 및 API helper를 export한다. | [L1](../../relay/src/protocol.mjs#L1) |
| <a id="symbol-relay-src-protocol-mjs-11-09c175ff6a"></a><code>json</code><br><code>function json(status, body)</code> | JS 함수/handler | JSON 응답에 공통 no-store/CSP/security header를 붙인다. | [L11](../../relay/src/protocol.mjs#L11) |
| <a id="symbol-relay-src-protocol-mjs-14-c360160b81"></a><code>error</code><br><code>(status, message) =&gt;</code> | JS 함수/handler | error 문자열을 가진 JSON 응답을 만든다. | [L14](../../relay/src/protocol.mjs#L14) |
| <a id="symbol-relay-src-protocol-mjs-15-a567a1e5f3"></a><code>sameOrigin</code><br><code>function sameOrigin(request)</code> | JS 함수/handler | Origin과 Sec-Fetch-Site를 확인해 인증 API의 cross-origin 요청을 제한한다. | [L15](../../relay/src/protocol.mjs#L15) |
| <a id="symbol-relay-src-protocol-mjs-19-954c49ca7f"></a><code>token</code><br><code>function token(request)</code> | JS 함수/handler | Authorization Bearer header에서 정해진 64hex 토큰만 읽는다. | [L19](../../relay/src/protocol.mjs#L19) |
| <a id="symbol-relay-src-protocol-mjs-20-a6e2056cdd"></a><code>digest</code><br><code>async function digest(value)</code> | JS 함수/handler | 토큰을 SHA-256으로 해시해 대문자 hex로 반환한다. | [L20](../../relay/src/protocol.mjs#L20) |
| <a id="symbol-relay-src-protocol-mjs-24-cd976aa5fb"></a><code>equal</code><br><code>function equal(a, b)</code> | JS 함수/handler | 두 문자열의 길이와 XOR 누적 비교로 해시 일치를 판정한다. | [L24](../../relay/src/protocol.mjs#L24) |
| <a id="symbol-relay-src-protocol-mjs-29-d2ab732e50"></a><code>route</code><br><code>function route(method, path)</code> | JS 함수/handler | GET state/query result/operation result와 POST query/apply만 허용해 method·ID·body 필요 여부를 반환한다. | [L29](../../relay/src/protocol.mjs#L29) |
| <a id="symbol-relay-src-protocol-mjs-35-60bb5c4b10"></a><code>requireEmptyBody</code><br><code>async function requireEmptyBody(request)</code> | JS 함수/handler | query 요청 body stream을 제한 시간 안에 읽어 실제 byte가 하나도 없는지 확인한다. Content-Length만 믿지 않는다. | [L35](../../relay/src/protocol.mjs#L35) |
| <a id="symbol-relay-src-protocol-mjs-51-88e5603c2c"></a><code>readBody</code><br><code>async function readBody(request)</code> | JS 함수/handler | content type/길이와 stream byte limit 64KiB, deadline, fatal UTF-8 decoding을 적용한다. 임시 buffer를 지우고 취소 시 reader를 종료한다. | [L51](../../relay/src/protocol.mjs#L51) |

<a id="file-relay-src-worker-mjs"></a>
## relay/src/worker.mjs

[소스 파일](../../relay/src/worker.mjs) · 명명된 항목/진입점 9개

Cloudflare Worker 라우터와 PvkrRoom Durable Object. 연결/대기 응답은 메모리에 두며 게임 명령은 영구 저장/재전송하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-relay-src-worker-mjs-1-8490b17dd2"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | HTTP 기본 handler와 migration에 지정한 PvkrRoom class를 export한다. | [L1](../../relay/src/worker.mjs#L1) |
| <a id="symbol-relay-src-worker-mjs-6-6839592067"></a><code>fetch</code><br><code>(request, env)</code> | JS 함수/handler | HTTPS/root·querystring 정책을 확인한다. 공개 static asset은 API origin 검사 전에 제공한다. host WSS는 PVKR_CONNECT_KEY secret을 검사하고 허용 API는 단일 owner Durable Object로 넘긴다. | [L6](../../relay/src/worker.mjs#L6) |
| <a id="symbol-relay-src-worker-mjs-39-8e79d7a573"></a><code>PvkrRoom.constructor</code><br><code>(ctx, env)</code> | JS 함수/handler | Durable Object ctx/env와 pending/rate 상태를 초기화하고 WebSocket ping/pong auto response를 설정한다. | [L39](../../relay/src/worker.mjs#L39) |
| <a id="symbol-relay-src-worker-mjs-44-3269c5400f"></a><code>PvkrRoom.host</code><br><code>()</code> | JS 함수/handler | host tag의 열린 socket을 찾아 반환한다. | [L44](../../relay/src/worker.mjs#L44) |
| <a id="symbol-relay-src-worker-mjs-45-69e1bc1424"></a><code>PvkrRoom.failPending</code><br><code>(socket)</code> | JS 함수/handler | 지정 host에 묶인 미결 HTTP 응답을 실패 완료하고 timer/메모리 항목을 정리한다. | [L45](../../relay/src/worker.mjs#L45) |
| <a id="symbol-relay-src-worker-mjs-51-9e4d9250a6"></a><code>PvkrRoom.fetch</code><br><code>(request)</code> | JS 함수/handler | 새 host 연결을 인증된 room/hash와 묶고 이전 host는4001로 교체한다. browser 요청은 room/key hash/host identity/rate/동시8개를 검사해 expiresAt frame을 한 번 보내고2.5초 응답을 기다린다. | [L51](../../relay/src/worker.mjs#L51) |
| <a id="symbol-relay-src-worker-mjs-97-402d3fc9ee"></a><code>PvkrRoom.webSocketMessage</code><br><code>(socket, message)</code> | JS 함수/handler | 크기/JSON v1/status/id 및 pending socket identity를 검증해 해당 HTTP promise를 완료한다. 잘못된 frame은 close와 pending 실패로 처리한다. | [L97](../../relay/src/worker.mjs#L97) |
| <a id="symbol-relay-src-worker-mjs-109-5f38064e6e"></a><code>PvkrRoom.webSocketClose</code><br><code>(socket, code)</code> | JS 함수/handler | 종료된 host의 미결 응답을 실패시키고 허용 close code로 소켓을 정리한다. | [L109](../../relay/src/worker.mjs#L109) |
| <a id="symbol-relay-src-worker-mjs-110-0a226f60fa"></a><code>PvkrRoom.webSocketError</code><br><code>(socket)</code> | JS 함수/handler | 오류 host를1011로 닫고 미결 응답을 실패시킨다. | [L110](../../relay/src/worker.mjs#L110) |

<a id="file-src-listingrelayclient-cs"></a>
## src/ListingRelayClient.cs

[소스 파일](../../src/ListingRelayClient.cs) · 명명된 항목/진입점 10개

게임 PC에서 Cloudflare로 나가는 WSS client. browser key/room을 발급하고 연결 실패 시 transport만 재연결한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingrelayclient-cs-21-9508b172b3"></a><code>ListingRelayClient.Running</code><br><code>bool Running</code> | 계산 속성 | 비동기 lifetime이 종료되지 않았는지 반환한다. 실제 socket 연결 완료와는 다르다. 계산식: <code>Volatile.Read(ref disposed) == 0 &amp;&amp; !worker.IsCompleted</code>. | [L21](../../src/ListingRelayClient.cs#L21) |
| <a id="symbol-src-listingrelayclient-cs-22-42ea4226f5"></a><code>ListingRelayClient.Connected</code><br><code>bool Connected</code> | 계산 속성 | 실제 외부 연결 완료 상태를 thread-safe하게 읽는다. 계산식: <code>Running &amp;&amp; Volatile.Read(ref connected) != 0</code>. | [L22](../../src/ListingRelayClient.cs#L22) |
| <a id="symbol-src-listingrelayclient-cs-23-595b6888f2"></a><code>ListingRelayClient.Status</code><br><code>string Status</code> | 계산 속성 | 현재 사용자용 연결/작업 진행 문구를 반환한다. 계산식: <code>Volatile.Read(ref status)</code>. | [L23](../../src/ListingRelayClient.cs#L23) |
| <a id="symbol-src-listingrelayclient-cs-24-627dd67861"></a><code>ListingRelayClient.ConnectionCode</code><br><code>string ConnectionCode</code> | 계산 속성 | room과 browser key를 연결한 외부 접속 코드를 반환한다. 로그/문서에 실값을 기록하지 않는다. 계산식: <code>room + "." + browserKey</code>. | [L24](../../src/ListingRelayClient.cs#L24) |
| <a id="symbol-src-listingrelayclient-cs-25-2071df03fe"></a><code>ListingRelayClient.ConnectionUrl</code><br><code>string ConnectionUrl</code> | 계산 속성 | 연결 key/room을 fragment에 포함한 웹 접속 링크를 반환한다. 계산식: <code>server.GetLeftPart(UriPartial.Authority) + "/#room=" + room + "&amp;key=" + browserKey</code>. | [L25](../../src/ListingRelayClient.cs#L25) |
| <a id="symbol-src-listingrelayclient-cs-26-ce6970b535"></a><code>ListingRelayClient.Completion</code><br><code>Task Completion</code> | 계산 속성 | 비동기 연결/서버 종료를 기다릴 수 있는 lifetime Task를 반환한다. 계산식: <code>worker</code>. | [L26](../../src/ListingRelayClient.cs#L26) |
| <a id="symbol-src-listingrelayclient-cs-28-073ed6ed52"></a><code>ListingRelayClient.ListingRelayClient</code><br><code>ListingRelayClient(ListingWebBridge bridge, string address, string key, bool localTest = false)</code> | 생성자 | Bridge·정규화한 server origin·host secret을 받고 room 및 browser token을 생성한다. browser 링크에는 room.key fragment를 사용한다. | [L28](../../src/ListingRelayClient.cs#L28) |
| <a id="symbol-src-listingrelayclient-cs-35-acacafeb1c"></a><code>ListingRelayClient.Run</code><br><code>Task Run()</code> | 메서드 | Authorization/room/browser key hash header로 WSS를 연결한다. keepalive와 2~30초 backoff를 적용하되 중계 frame 명령은 재전송하지 않는다. 교체 close4001이면 멈춘다. | [L35](../../src/ListingRelayClient.cs#L35) |
| <a id="symbol-src-listingrelayclient-cs-86-caa477770d"></a><code>ListingRelayClient.Receive</code><br><code>Task&lt;bool&gt; Receive(ClientWebSocket socket)</code> | 메서드 | 분할된 text frame을 512KiB 이내로 모아 protocol.Handle에 전달한다. 응답은 제한 시간 안에 보내고 임시 바이트를 지운다. | [L86](../../src/ListingRelayClient.cs#L86) |
| <a id="symbol-src-listingrelayclient-cs-119-afa0819a9e"></a><code>ListingRelayClient.Dispose</code><br><code>void Dispose()</code> | 메서드 | 중단 token을 취소해 lifetime을 정리한다. 게임 thread에서 네트워크 종료를 기다리지 않는다. | [L119](../../src/ListingRelayClient.cs#L119) |

<a id="file-src-listingrelayprotocol-cs"></a>
## src/ListingRelayProtocol.cs

[소스 파일](../../src/ListingRelayProtocol.cs) · 명명된 항목/진입점 3개

중계 URL/key 검증 및 Worker→plugin WebSocket frame의 허용된 API 매핑. frame v1과 browser API v2는 서로 다른 버전이다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingrelayprotocol-cs-11-e5873c2c7e"></a><code>ListingRelayProtocol.TryServer</code><br><code>bool TryServer(string value, out Uri server, bool localTest = false)</code> | 메서드 | 기본 포트·root path의 HTTPS DNS origin만 정상화해 허용한다. credentials/query/fragment는 거부하고 HTTP loopback은 명시 local test에서만 허용한다. | [L11](../../src/ListingRelayProtocol.cs#L11) |
| <a id="symbol-src-listingrelayprotocol-cs-20-583bdcb1b0"></a><code>ListingRelayProtocol.IsKey</code><br><code>bool IsKey(string value)</code> | 메서드 | 대문자 hex 64자리 연결 키 형식을 검사한다. | [L20](../../src/ListingRelayProtocol.cs#L20) |
| <a id="symbol-src-listingrelayprotocol-cs-21-c2e8390bdd"></a><code>ListingRelayProtocol.Handle</code><br><code>byte[] Handle(ListingWebBridge bridge, ReadOnlyMemory&lt;byte&gt; frame, long utcNow, long tickNow)</code> | 메서드 | 최대 512KiB JSON frame의 v/id/path/method/session/requestId/만료 및 64KiB body를 검증한다. 허용한 API만 Bridge로 전달하고 응답 frame을 만든 뒤 입력 body를 지운다. | [L21](../../src/ListingRelayProtocol.cs#L21) |

<a id="file-src-listingwebbridge-edit-cs"></a>
## src/ListingWebBridge.Edit.cs

[소스 파일](../../src/ListingWebBridge.Edit.cs) · 명명된 항목/진입점 8개

외부 수정/갱신 큐와 결과 ledger. session당 write ID는 최대 1024개 유지하고 edit revision은 접수할 때 소비한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingwebbridge-edit-cs-13-cffc2e2029"></a><code>ListingWebBridge.PendingWrite</code><br><code>PendingWrite(RefreshTarget Source, ListingChange[] Changes, long Tick)</code> | 주 생성자/모델 | 아직 실행하지 않은 외부 쓰기의 원본 source·변경 배열·접수 시각을 메모리에 보관한다. UUID와 진행 상태는 WebOperation이 관리한다. | [L13](../../src/ListingWebBridge.Edit.cs#L13) |
| <a id="symbol-src-listingwebbridge-edit-cs-14-fa5edadd47"></a><code>ListingWebBridge.WriteActive</code><br><code>bool WriteActive</code> | 계산 속성 | 현재 외부 write가 queued/running인지 검사한다. 계산식: <code>view.Operation?.State is "queued" or "running"</code>. | [L14](../../src/ListingWebBridge.Edit.cs#L14) |
| <a id="symbol-src-listingwebbridge-edit-cs-16-481a473055"></a><code>ListingWebBridge.ReadOperation</code><br><code>WebReply ReadOperation(string id)</code> | 메서드 | 요청 ID의 작업 결과를 읽는다. 보관된 결과와 이미 소비된 ID를 구분한다. | [L16](../../src/ListingWebBridge.Edit.cs#L16) |
| <a id="symbol-src-listingwebbridge-edit-cs-27-051cb888cd"></a><code>ListingWebBridge.QueueApply</code><br><code>WebReply QueueApply(string id, string session, ReadOnlyMemory&lt;byte&gt; body, long now, long? recoveryRevision = null)</code> | 메서드 | body 크기·session·요청 ID 재사용·신선도·진행 여부·편집 revision을 검사해 JSON 변경을 decode한다. 성공 시 수정 source를 소비하고 queued 작업을 만든다. | [L27](../../src/ListingWebBridge.Edit.cs#L27) |
| <a id="symbol-src-listingwebbridge-edit-cs-53-19aa91f259"></a><code>ListingWebBridge.UpdateOperation</code><br><code>void UpdateOperation(IListingWebEditHost? host, long now)</code> | 메서드 | Framework에서 queued 작업을 host.StartRefresh로 한 번 시작한다. 예외 후에도 다시 시작하지 않고 이후 ReadRefresh로 complete/partial/unconfirmed/failed를 관찰한다. | [L53](../../src/ListingWebBridge.Edit.cs#L53) |
| <a id="symbol-src-listingwebbridge-edit-cs-87-ea65825dc6"></a><code>ListingWebBridge.PublishEditor</code><br><code>void PublishEditor(IListingWebEditHost? host, ListingSnapshot listing)</code> | 메서드 | 유효한 본인 source를 캡처해 외부 편집 모델을 만든다. 같은 원본은 revision을 유지하고 source가 바뀌면 새 revision을 발급한다. | [L87](../../src/ListingWebBridge.Edit.cs#L87) |
| <a id="symbol-src-listingwebbridge-edit-cs-109-daa34e503e"></a><code>ListingWebBridge.SetOperation</code><br><code>void SetOperation(WebOperation operation)</code> | 메서드 | 작업 상태와 제한된 결과 이력을 갱신한다. | [L109](../../src/ListingWebBridge.Edit.cs#L109) |
| <a id="symbol-src-listingwebbridge-edit-cs-116-dd90356a58"></a><code>ListingWebBridge.ClearEdits</code><br><code>void ClearEdits()</code> | 메서드 | 캐릭터/session 종료 시 편집 source·활성 write·결과/중복 방지 기록을 정리한다. | [L116](../../src/ListingWebBridge.Edit.cs#L116) |

<a id="file-src-listingwebbridge-recovery-cs"></a>
## src/ListingWebBridge.Recovery.cs

[소스 파일](../../src/ListingWebBridge.Recovery.cs) · 명명된 항목/진입점 15개

명시적인 디스코드 수정창 닫기의 공통 queue. 기존 query/apply busy와 무관하게 접수하고 게임 스레드에서 먼저 중단한다. 닫기 ticket의 native 주소는 외부에 직렬화하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingwebbridge-recovery-cs-4-b05e3f2548"></a><code>RecruitmentWindowTicket</code><br><code>RecruitmentWindowTicket(nint Address, long ClosedRevision)</code> | 주 생성자/모델 | 창 주소와 PreFinalize revision을 묶어 같은 위치에 재생성된 다른 창을 닫지 않도록 한다. 관리형 내부 비교에만 사용한다. | [L4](../../src/ListingWebBridge.Recovery.cs#L4) |
| <a id="symbol-src-listingwebbridge-recovery-cs-5-3696061ae5"></a><code>RecruitmentWindows</code><br><code>RecruitmentWindows(RecruitmentWindowTicket Editor, RecruitmentWindowTicket Roles)</code> | 주 생성자/모델 | 모집 수정창과 직업 선택 자식창의 ticket을 묶는다. 주소0은 창이 보이지 않음을 뜻한다. | [L5](../../src/ListingWebBridge.Recovery.cs#L5) |
| <a id="symbol-src-listingwebbridge-recovery-cs-10-91f424eca7"></a><code>IListingRecoveryHost.StopForRecovery</code><br><code>void StopForRecovery()</code> | 인터페이스 계약 | 진행 중인 query/refresh와 초안을 중단하는 게임 스레드 계약이다. 제출을 되돌리지는 않는다. | [L10](../../src/ListingWebBridge.Recovery.cs#L10) |
| <a id="symbol-src-listingwebbridge-recovery-cs-11-e7e4fedbae"></a><code>IListingRecoveryHost.ReadRecoveryWindows</code><br><code>bool ReadRecoveryWindows(out RecruitmentWindows windows)</code> | 인터페이스 계약 | 두 모집 관련 창의 현재 ticket을 읽는 계약이다. 읽기 실패와 창 없음은 구분한다. | [L11](../../src/ListingWebBridge.Recovery.cs#L11) |
| <a id="symbol-src-listingwebbridge-recovery-cs-12-63ba0553ed"></a><code>IListingRecoveryHost.CloseRecoveryWindow</code><br><code>bool CloseRecoveryWindow(RecruitmentWindow kind, RecruitmentWindowTicket ticket, out string error)</code> | 인터페이스 계약 | 명시한 종류와 같은 ticket의 창만 닫는 계약이다. 제출/확인 버튼 조작을 포함하지 않는다. | [L12](../../src/ListingWebBridge.Recovery.cs#L12) |
| <a id="symbol-src-listingwebbridge-recovery-cs-14-45df110119"></a><code>WebRecovery</code><br><code>WebRecovery(string Id, string State, string Message)</code> | 주 생성자/모델 | 닫기 요청 UUID·상태·표시 문구만 외부 interaction에 전달한다. | [L14](../../src/ListingWebBridge.Recovery.cs#L14) |
| <a id="symbol-src-listingwebbridge-recovery-cs-24-60d3568505"></a><code>ListingWebBridge.RecoveryRevision</code><br><code>long RecoveryRevision</code> | 계산 속성 | 닫기 접수 세대 번호를 lock 안에서 읽는다. 닫기 전에 만들어진 DiscordWork의 늦은 ACK 실행을 거부한다. | [L24](../../src/ListingWebBridge.Recovery.cs#L24) |
| <a id="symbol-src-listingwebbridge-recovery-cs-25-e1e023ad02"></a><code>ListingWebBridge.RecoveryActive</code><br><code>bool RecoveryActive</code> | 계산 속성 | 닫기 요청이 queued 또는 running인지 lock 안에서 반환한다. | [L25](../../src/ListingWebBridge.Recovery.cs#L25) |
| <a id="symbol-src-listingwebbridge-recovery-cs-27-5d18fc31bf"></a><code>ListingWebBridge.QueueCloseEditor</code><br><code>WebReply QueueCloseEditor(string id, string session, long now)</code> | 메서드 | 소유권 인증 이후 session·UUID·로그인·최근 발행 상태를 검사하고 중복을 재사용한다. 진행 중 query/apply를 중단 상태로 바꾸고 초안 source를 비운다. 네이티브 호출은 하지 않는다. | [L27](../../src/ListingWebBridge.Recovery.cs#L27) |
| <a id="symbol-src-listingwebbridge-recovery-cs-54-0e5dfc897f"></a><code>ListingWebBridge.ReadRecovery</code><br><code>WebReply ReadRecovery(string id)</code> | 메서드 | UUID에 해당하는 닫기 결과를 lock 안에서 읽는다. 반복 호출은 상태만 조회한다. | [L54](../../src/ListingWebBridge.Recovery.cs#L54) |
| <a id="symbol-src-listingwebbridge-recovery-cs-61-e342ce5403"></a><code>ListingWebBridge.BeforeReadRecovery</code><br><code>void BeforeReadRecovery(IListingRecoveryHost host, long now)</code> | 메서드 | Framework의 일반 query/refresh 진행보다 앞서 닫기 요청을 처리한다. Bridge.Update에서도 호출되므로 각 close는 sent 표시로1회만 보낸다. | [L61](../../src/ListingWebBridge.Recovery.cs#L61) |
| <a id="symbol-src-listingwebbridge-recovery-cs-66-8523894d03"></a><code>ListingWebBridge.ProcessRecovery</code><br><code>void ProcessRecovery(IListingRecoveryHost? host, long now)</code> | 메서드 | 5초 queued 유효기간·같은 캐릭터를 확인하고 먼저 진행을 취소한다. 창 ticket을 캡처해 자식 닫힘→부모 닫힘을 관찰하며5초 안에 둘 다 사라져야 성공한다. 새 창·native guard·예외는 중단하고 불확실한 close를 반복하지 않는다. | [L66](../../src/ListingWebBridge.Recovery.cs#L66) |
| <a id="symbol-src-listingwebbridge-recovery-cs-109-d4b96ec1be"></a><code>ListingWebBridge.FinishRecovery</code><br><code>void FinishRecovery(bool complete, string message)</code> | 메서드 | 닫기 결과를 complete/failed와 문구로 고정한다. 실패를 성공으로 취급하지 않는다. | [L109](../../src/ListingWebBridge.Recovery.cs#L109) |
| <a id="symbol-src-listingwebbridge-recovery-cs-110-18a8583dbc"></a><code>ListingWebBridge.SetRecovery</code><br><code>void SetRecovery(WebRecovery value)</code> | 메서드 | 현재 닫기 상태와 세션 내 UUID 결과 ledger를 함께 갱신한다. | [L110](../../src/ListingWebBridge.Recovery.cs#L110) |
| <a id="symbol-src-listingwebbridge-recovery-cs-111-b86f8e9b60"></a><code>ListingWebBridge.ClearRecovery</code><br><code>void ClearRecovery()</code> | 메서드 | 로그아웃/캐릭터 변경/연결 종료 시 닫기 상태와 ticket 및 결과 기록을 지운다. | [L111](../../src/ListingWebBridge.Recovery.cs#L111) |

<a id="file-src-listingwebbridge-cs"></a>
## src/ListingWebBridge.cs

[소스 파일](../../src/ListingWebBridge.cs) · 명명된 항목/진입점 15개

네트워크와 Framework 사이의 lock 기반 관리형 경계. 네트워크는 JSON snapshot을 읽고 요청을 큐에 넣으며 실제 게임 실행은 Update에서만 한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingwebbridge-cs-13-159c72e3f8"></a><code>IListingWebHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 공통 수동 조회 경로를 호출하고 실제 발급된 request revision을 소유권 guard에 기록한다. | [L13](../../src/ListingWebBridge.cs#L13) |
| <a id="symbol-src-listingwebbridge-cs-16-e6629b8c6d"></a><code>WebReply</code><br><code>WebReply(int Status, object Body)</code> | 주 생성자/모델 | HTTP status와 JSON body를 함께 반환하는 Bridge 응답이다. | [L16](../../src/ListingWebBridge.cs#L16) |
| <a id="symbol-src-listingwebbridge-cs-33-b60c946744"></a><code>ListingWebBridge.ListingWebBridge</code><br><code>ListingWebBridge(string version)</code> | 생성자 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L33](../../src/ListingWebBridge.cs#L33) |
| <a id="symbol-src-listingwebbridge-cs-40-b637b8cee3"></a><code>ListingWebBridge.Empty</code><br><code>WebView Empty(string reason)</code> | 메서드 | 버전/API/session을 포함한 초기 비연결 WebView를 만든다. | [L40](../../src/ListingWebBridge.cs#L40) |
| <a id="symbol-src-listingwebbridge-cs-42-4501e34e05"></a><code>ListingWebBridge.Active</code><br><code>bool Active</code> | 계산 속성 | 현재 Query가 queued 또는 running인지 판정한다. 외부 쓰기의 진행 상태는 별도 WriteActive로 검사한다. 계산식: <code>view.Query?.State is "queued" or "running"</code>. | [L42](../../src/ListingWebBridge.cs#L42) |
| <a id="symbol-src-listingwebbridge-cs-43-7a4c685f06"></a><code>ListingWebBridge.ReadState</code><br><code>byte[] ReadState()</code> | 메서드 | lock 안에서 마지막으로 직렬화된 state JSON을 반환한다. | [L43](../../src/ListingWebBridge.cs#L43) |
| <a id="symbol-src-listingwebbridge-cs-44-71ddd93b61"></a><code>ListingWebBridge.ReadView</code><br><code>WebView ReadView()</code> | 메서드 | 동일한 관리형 WebView snapshot을 반환한다. Discord가 공통 표시/편집 모델을 재사용한다. | [L44](../../src/ListingWebBridge.cs#L44) |
| <a id="symbol-src-listingwebbridge-cs-45-8d703fd5db"></a><code>ListingWebBridge.ReadQuery</code><br><code>WebReply ReadQuery(string id)</code> | 메서드 | canonical GUID 형식을 검사하고 조회 요청 ID의 보관된 결과를 반환한다. | [L45](../../src/ListingWebBridge.cs#L45) |
| <a id="symbol-src-listingwebbridge-cs-51-d2f48ab912"></a><code>ListingWebBridge.QueueQuery</code><br><code>WebReply QueueQuery(string id, string session, long now, long? recoveryRevision = null)</code> | 메서드 | 정지·session·중복 ID·snapshot freshness·진행 작업·2초 조회 제한을 검사해 조회만 예약한다. 게임 메모리는 접근하지 않는다. | [L51](../../src/ListingWebBridge.cs#L51) |
| <a id="symbol-src-listingwebbridge-cs-72-a0e0ad04c2"></a><code>ListingWebBridge.Update</code><br><code>void Update(IListingWebHost host, long now)</code> | 메서드 | Framework에서 캐릭터/session 변경을 처리하고 queued query를 host.RequestQuery로 실행한다. 새 revision과 본인 검증을 확인해 조회 결과를 완료하고 edit/operation/state snapshot을 발행한다. | [L72](../../src/ListingWebBridge.cs#L72) |
| <a id="symbol-src-listingwebbridge-cs-127-c6f15af493"></a><code>ListingWebBridge.PublicationFailed</code><br><code>void PublicationFailed()</code> | 메서드 | snapshot 발행 오류를 기록하고 stale source·미실행 요청을 무효화한다. 이미 실행한 native write를 자동 재시도하지 않는다. | [L127](../../src/ListingWebBridge.cs#L127) |
| <a id="symbol-src-listingwebbridge-cs-142-b9d3145a76"></a><code>ListingWebBridge.Stop</code><br><code>void Stop()</code> | 메서드 | Bridge를 비활성화하고 pending query/edit source와 session 상태를 정리한다. | [L142](../../src/ListingWebBridge.cs#L142) |
| <a id="symbol-src-listingwebbridge-cs-152-9f835c3461"></a><code>ListingWebBridge.SetQuery</code><br><code>void SetQuery(WebQuery query)</code> | 메서드 | 조회 요청 상태를 교체하고 제한된 결과 이력을 관리한다. | [L152](../../src/ListingWebBridge.cs#L152) |
| <a id="symbol-src-listingwebbridge-cs-159-07849747f7"></a><code>ListingWebBridge.Serialize</code><br><code>void Serialize()</code> | 메서드 | 공통 JSON 옵션으로 camelCase 관리형 응답을 직렬화한다. | [L159](../../src/ListingWebBridge.cs#L159) |
| <a id="symbol-src-listingwebbridge-cs-160-c5b477a285"></a><code>ListingWebBridge.Error</code><br><code>WebReply Error(int status, string message)</code> | 메서드 | HTTP status와 error JSON을 가진 WebReply를 만든다. | [L160](../../src/ListingWebBridge.cs#L160) |

<a id="file-src-listingwebedit-cs"></a>
## src/ListingWebEdit.cs

[소스 파일](../../src/ListingWebEdit.cs) · 명명된 항목/진입점 17개

외부 editor DTO와 strict JSON decoder. 비밀번호는 모델/상태 응답에 포함하지 않는다. 최대 body64KiB, 변경59개(48자리+11필드)다.

직접 관련 테스트: [ListingWebEditTests.cs](tests.md#file-tests-listingwebedittests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingwebedit-cs-6-63df29f01e"></a><code>WebJobChoice</code><br><code>WebJobChoice(uint Id, string Name)</code> | 주 생성자/모델 | 외부 editor가 선택할 직업 ID·이름을 표현한다. | [L6](../../src/ListingWebEdit.cs#L6) |
| <a id="symbol-src-listingwebedit-cs-7-ac2c21e520"></a><code>WebEditSlot</code><br><code>WebEditSlot(int NativeIndex, string Position, uint[] Jobs)</code> | 주 생성자/모델 | 외부에서 편집 가능한 등록 빈자리의 물리 index·그룹/표시·선택 직업 목록을 표현한다. | [L7](../../src/ListingWebEdit.cs#L7) |
| <a id="symbol-src-listingwebedit-cs-8-bcfc362469"></a><code>WebEditModel</code><br><code>WebEditModel(string Revision, Dictionary&lt;string, object?&gt; Values, string[] Supported, WebJobChoice[] Jobs, WebEditSlot[] Slots)</code> | 주 생성자/모델 | 원본 revision·지원 필드·현재 값·자리/직업 선택지를 전달한다. PIN 원문은 제외한다. | [L8](../../src/ListingWebEdit.cs#L8) |
| <a id="symbol-src-listingwebedit-cs-10-e49ac94cd9"></a><code>WebItemResult</code><br><code>WebItemResult(string Key, string Title, bool? Applied, string Message)</code> | 주 생성자/모델 | 한 수정 항목의 key/title/요청값/결과 문구와 상태를 외부 표시용으로 묶는다. | [L10](../../src/ListingWebEdit.cs#L10) |
| <a id="symbol-src-listingwebedit-cs-11-8fec41c5ad"></a><code>WebOperation</code><br><code>WebOperation(string Id, string State, string Stage, string Message, bool EditorStarted, bool Submitted, WebItemResult[] Results)</code> | 주 생성자/모델 | 요청 UUID·진행 상태/단계·요약·수정창 진입/제출 여부·항목별 결과를 표현한다. session은 바깥 WebView와 요청 검증에서 관리한다. | [L11](../../src/ListingWebEdit.cs#L11) |
| <a id="symbol-src-listingwebedit-cs-13-20c7ce93f8"></a><code>WebRefreshProgress</code><br><code>WebRefreshProgress(bool Busy, string Stage, string Message, bool EditorStarted, bool Submitted, WebItemResult[] Results)</code> | 주 생성자/모델 | 게임 갱신 stage·요약·배치 항목 결과를 관리형 외부 상태로 전달한다. | [L13](../../src/ListingWebEdit.cs#L13) |
| <a id="symbol-src-listingwebedit-cs-16-9c22a15979"></a><code>WebRefreshProgress.From</code><br><code>WebRefreshProgress From(ListingRefresh refresh)</code> | 메서드 | ListingRefresh의 stage·상태·배치 항목 결과를 WebRefreshProgress로 변환한다. | [L16](../../src/ListingWebEdit.cs#L16) |
| <a id="symbol-src-listingwebedit-cs-24-e031fc93d4"></a><code>IListingWebEditHost.CaptureEditTarget</code><br><code>RefreshTarget? CaptureEditTarget()</code> | 인터페이스 계약 | 인터페이스 계약. 진행 작업이 없고 현재 캐릭터의 live Self 상세가 검증된 경우에만 편집 source를 캡처한다. | [L24](../../src/ListingWebEdit.cs#L24) |
| <a id="symbol-src-listingwebedit-cs-25-80c4cb7a56"></a><code>IListingWebEditHost.StartRefresh</code><br><code>bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error)</code> | 인터페이스 계약 | 인터페이스 계약. 외부 source와 현재 본인/차단 사유를 검사한다. changes가 비면 단순 갱신, 있으면 batch를 요청하고 생성된 refresh serial을 guard에 기록한다. | [L25](../../src/ListingWebEdit.cs#L25) |
| <a id="symbol-src-listingwebedit-cs-26-2bb7214a65"></a><code>IListingWebEditHost.ReadRefresh</code><br><code>WebRefreshProgress? ReadRefresh()</code> | 인터페이스 계약 | 인터페이스 계약. 자기가 시작한 refresh serial과 현재 serial이 같을 때만 진행 결과를 반환한다. | [L26](../../src/ListingWebEdit.cs#L26) |
| <a id="symbol-src-listingwebedit-cs-32-6ced374a97"></a><code>ListingWebEdit.SameSource</code><br><code>bool SameSource(RefreshTarget a, RefreshTarget b)</code> | 메서드 | 대기 변경의 원본 비교에 더해 JobEditingAllowed와 직업 카탈로그 등 편집 가능 근거까지 같은지 검사한다. | [L32](../../src/ListingWebEdit.cs#L32) |
| <a id="symbol-src-listingwebedit-cs-36-1b892d6911"></a><code>ListingWebEdit.Model</code><br><code>WebEditModel Model(RefreshTarget source, string revision)</code> | 메서드 | RefreshTarget에서 supported field·현재 값·빈 등록 자리·선택 가능한 직업과 새 revision이 포함된 WebEditModel을 만든다. | [L36](../../src/ListingWebEdit.cs#L36) |
| <a id="symbol-src-listingwebedit-cs-70-2fc2a7905b"></a><code>ListingWebEdit.Decode</code><br><code>bool Decode(ReadOnlyMemory&lt;byte&gt; body, RefreshTarget source, string revision, out ListingChange[] changes, out string error)</code> | 메서드 | 깊이/속성 중복/미지 key/enum/문자열/직업 ID/자리/PIN을 검증해 ListingChange 배열로 변환한다. no-op는 제외하고 raw NativeIndex는 표시 position으로 다시 매핑한다. | [L70](../../src/ListingWebEdit.cs#L70) |
| <a id="symbol-src-listingwebedit-cs-145-d7a3d8f046"></a><code>ListingWebEdit.Keys</code><br><code>bool Keys(JsonElement value, params string[] expected)</code> | 메서드 | JSON object의 필드 집합이 기대 allowlist와 정확히 맞는지 검사한다. | [L145](../../src/ListingWebEdit.cs#L145) |
| <a id="symbol-src-listingwebedit-cs-147-74f031978a"></a><code>ListingWebEdit.Completion</code><br><code>ListingCompletionEdit Completion(RefreshTarget source, int value)</code> | 메서드 | 외부 완료조건 값 1/2/4를 enabled/index typed edit로 변환한다. | [L147](../../src/ListingWebEdit.cs#L147) |
| <a id="symbol-src-listingwebedit-cs-150-65577d307a"></a><code>ListingWebEdit.ItemLevel</code><br><code>ListingItemLevelEdit ItemLevel(RefreshTarget source, int value)</code> | 메서드 | 외부 평균 레벨 0 또는 1~999를 typed edit로 변환한다. | [L150](../../src/ListingWebEdit.cs#L150) |
| <a id="symbol-src-listingwebedit-cs-152-54c616007a"></a><code>ListingWebEdit.Rule</code><br><code>ListingRuleEdit Rule(RefreshTarget source, string key, int value)</code> | 메서드 | 외부 finder/loot 값을 유효한 ListingRuleEdit로 변환한다. | [L152](../../src/ListingWebEdit.cs#L152) |

<a id="file-src-listingwebrequestguard-cs"></a>
## src/ListingWebRequestGuard.cs

[소스 파일](../../src/ListingWebRequestGuard.cs) · 명명된 항목/진입점 8개

외부 연결이 소유한 조회/갱신 serial만 취소할 수 있게 한다. 다른 수동 요청을 잘못 취소하지 않도록 source identity와 update 간격을 검사한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingwebrequestguard-cs-11-9a6a7af24d"></a><code>ListingWebRequestGuard.Started</code><br><code>void Started(ulong selfId, long requestRevision, long now)</code> | 메서드 | 외부가 시작한 본인 CID·요청 revision·시각을 기록해 소유권을 고정한다. | [L11](../../src/ListingWebRequestGuard.cs#L11) |
| <a id="symbol-src-listingwebrequestguard-cs-13-106328294e"></a><code>ListingWebRequestGuard.Owns</code><br><code>bool Owns(long requestRevision, bool busy)</code> | 메서드 | 현재 실행 중인 요청 serial이 이 guard가 시작한 것과 같은지 검사한다. | [L13](../../src/ListingWebRequestGuard.cs#L13) |
| <a id="symbol-src-listingwebrequestguard-cs-14-1b4cfbe6d1"></a><code>ListingWebRequestGuard.BeforeRead</code><br><code>bool BeforeRead(ulong selfId, long requestRevision, bool busy, bool enabled, bool blocked, long now)</code> | 메서드 | 연결 종료·캐릭터 변경·충돌 상태·너무 늦은 Framework 처리 여부를 검사한다. refresh guard의 제한은 update 공백이며 전체 배치 수명 제한과 다르다. | [L14](../../src/ListingWebRequestGuard.cs#L14) |
| <a id="symbol-src-listingwebrequestguard-cs-20-fcb0c4309e"></a><code>ListingWebRequestGuard.Clear</code><br><code>void Clear()</code> | 메서드 | 소유한 요청의 CID/revision/시각을 초기화한다. | [L20](../../src/ListingWebRequestGuard.cs#L20) |
| <a id="symbol-src-listingwebrequestguard-cs-29-4098b278f0"></a><code>ListingWebRefreshGuard.Started</code><br><code>void Started(ulong selfId, long serial, long now)</code> | 메서드 | 외부가 시작한 본인 CID·요청 revision·시각을 기록해 소유권을 고정한다. | [L29](../../src/ListingWebRequestGuard.cs#L29) |
| <a id="symbol-src-listingwebrequestguard-cs-30-e06e7664d7"></a><code>ListingWebRefreshGuard.Owns</code><br><code>bool Owns(long serial, bool busy)</code> | 메서드 | 현재 실행 중인 요청 serial이 이 guard가 시작한 것과 같은지 검사한다. | [L30](../../src/ListingWebRequestGuard.cs#L30) |
| <a id="symbol-src-listingwebrequestguard-cs-31-54513b4c5d"></a><code>ListingWebRefreshGuard.BeforeRead</code><br><code>bool BeforeRead(ulong selfId, long serial, bool busy, bool enabled, bool queued, long now)</code> | 메서드 | 연결 종료·캐릭터 변경·충돌 상태·너무 늦은 Framework 처리 여부를 검사한다. refresh guard의 제한은 update 공백이며 전체 배치 수명 제한과 다르다. | [L31](../../src/ListingWebRequestGuard.cs#L31) |
| <a id="symbol-src-listingwebrequestguard-cs-37-5471591a54"></a><code>ListingWebRefreshGuard.Clear</code><br><code>void Clear()</code> | 메서드 | 소유한 요청의 CID/revision/시각을 초기화한다. | [L37](../../src/ListingWebRequestGuard.cs#L37) |

<a id="file-src-listingwebserver-cs"></a>
## src/ListingWebServer.cs

[소스 파일](../../src/ListingWebServer.cs) · 명명된 항목/진입점 12개

127.0.0.1 임의 포트의 제한된 HTTP 서버. 같은 PC 웹 UI용이며 inbound 공용 서버가 아니다. static resource와 인증된 API를 제공한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingwebserver-cs-32-da9548b61b"></a><code>ListingWebServer.Running</code><br><code>bool Running</code> | 계산 속성 | 비동기 lifetime이 종료되지 않았는지 반환한다. 실제 socket 연결 완료와는 다르다. 계산식: <code>!stop.IsCancellationRequested &amp;&amp; !worker.IsCompleted</code>. | [L32](../../src/ListingWebServer.cs#L32) |
| <a id="symbol-src-listingwebserver-cs-33-801743599b"></a><code>ListingWebServer.Failure</code><br><code>string Failure</code> | 계산 속성 | loopback 서버 lifetime의 종료 오류를 반환한다. 계산식: <code>Volatile.Read(ref failure)</code>. | [L33](../../src/ListingWebServer.cs#L33) |
| <a id="symbol-src-listingwebserver-cs-34-a247f26fd7"></a><code>ListingWebServer.Completion</code><br><code>Task Completion</code> | 계산 속성 | 비동기 연결/서버 종료를 기다릴 수 있는 lifetime Task를 반환한다. 계산식: <code>worker</code>. | [L34](../../src/ListingWebServer.cs#L34) |
| <a id="symbol-src-listingwebserver-cs-36-fa11d14acd"></a><code>ListingWebServer.ListingWebServer</code><br><code>ListingWebServer(ListingWebBridge bridge)</code> | 생성자 | loopback listener·random bearer token·embedded 5개 웹 resource를 준비하고 accept loop를 시작한다. 인증은 URL fragment로 전달한다. | [L36](../../src/ListingWebServer.cs#L36) |
| <a id="symbol-src-listingwebserver-cs-57-f58511f7c7"></a><code>ListingWebServer.Asset</code><br><code>byte[] Asset(string name)</code> | 메서드 | embedded 웹 파일을 읽어 MIME/body와 함께 static asset 사전에 저장한다. | [L57](../../src/ListingWebServer.cs#L57) |
| <a id="symbol-src-listingwebserver-cs-64-df43413f2c"></a><code>ListingWebServer.AcceptLoop</code><br><code>Task AcceptLoop()</code> | 메서드 | 최대 동시 연결 수 8을 지키며 TCP client를 받아 Handle 작업으로 분리한다. 종료 시 남은 client/handler를 정리한다. | [L64](../../src/ListingWebServer.cs#L64) |
| <a id="symbol-src-listingwebserver-cs-108-e657f47bc2"></a><code>ListingWebServer.Handle</code><br><code>Task Handle(TcpClient client)</code> | 메서드 | 3초 deadline, 8KiB header, 엄격한 Host/Origin/Fetch Metadata 및 bearer 검사를 수행한다. body는 인증 후 읽고 query/apply/read API 또는 static resource를 처리하며 body buffer를 지운다. | [L108](../../src/ListingWebServer.cs#L108) |
| <a id="symbol-src-listingwebserver-cs-168-de63781e8d"></a><code>ListingWebServer.Request</code><br><code>Request(string Method, string Path, Dictionary&lt;string, string&gt; Headers, int BodyLength)</code> | 주 생성자/모델 | loopback에서 엄격하게 파싱한 HTTP method/path/headers/content length를 보관한다. | [L168](../../src/ListingWebServer.cs#L168) |
| <a id="symbol-src-listingwebserver-cs-169-e0f82db335"></a><code>ListingWebServer.TryParse</code><br><code>bool TryParse(ReadOnlySpan&lt;byte&gt; bytes, out Request request)</code> | 메서드 | ASCII HTTP/1.0·1.1 GET/POST만 허용하고 중복/접힌 header, 잘못된 path, chunked/expect/compression을 거부한다. apply만 정해진 JSON Content-Length body를 받는다. | [L169](../../src/ListingWebServer.cs#L169) |
| <a id="symbol-src-listingwebserver-cs-201-5c09a7602e"></a><code>ListingWebServer.Reply</code><br><code>Task Reply(NetworkStream stream, WebReply reply, CancellationToken token)</code> | 메서드 | HTTP 코드와 직렬화된 JSON 응답을 Send로 전달한다. | [L201](../../src/ListingWebServer.cs#L201) |
| <a id="symbol-src-listingwebserver-cs-204-158bc5e31c"></a><code>ListingWebServer.Send</code><br><code>Task Send(NetworkStream stream, int status, string type, byte[] body, CancellationToken token)</code> | 메서드 | CSP/no-store/nosniff 등 header와 byte length를 구성해 응답을 보낸 뒤 연결을 닫는다. | [L204](../../src/ListingWebServer.cs#L204) |
| <a id="symbol-src-listingwebserver-cs-216-f2319d1bb7"></a><code>ListingWebServer.Dispose</code><br><code>void Dispose()</code> | 메서드 | listener/client를 취소·종료하고 Bridge.Stop을 호출한다. 게임 스레드에서 handler 완료를 동기 대기하지 않는다. | [L216](../../src/ListingWebServer.cs#L216) |

<a id="file-src-listingwebview-cs"></a>
## src/ListingWebView.cs

[소스 파일](../../src/ListingWebView.cs) · 명명된 항목/진입점 7개

외부에 노출하는 최소 관리형 표시 모델. 원시 CID/flag/PIN/상세 진단을 제외하고 전체 그룹과 등록 직업·허용 직업 ID를 포함한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingwebview-cs-5-5e6db5a6df"></a><code>WebSetting</code><br><code>WebSetting(string Key, string Label, string Value)</code> | 주 생성자/모델 | 조건 key·한국어 이름·표시값·활성 여부를 보관한다. | [L5](../../src/ListingWebView.cs#L5) |
| <a id="symbol-src-listingwebview-cs-11-6cda1b6ac9"></a><code>WebSlot</code><br><code>WebSlot(int NativeIndex, string Position, bool HasMember, string Job, bool Anyone, bool RulesComplete, string[] AllowedJobs)</code> | 주 생성자/모델 | CID/raw mask 없이 자리 표시·등록 직업·참가 여부·허용 직업과 물리 index를 보관한다. | [L11](../../src/ListingWebView.cs#L11) |
| <a id="symbol-src-listingwebview-cs-18-4e41fc6e6f"></a><code>WebGroup</code><br><code>WebGroup(string Name, WebSlot[] Slots)</code> | 주 생성자/모델 | 일반/연합/친선 그룹 이름과 등록 자리 배열을 묶는다. | [L18](../../src/ListingWebView.cs#L18) |
| <a id="symbol-src-listingwebview-cs-19-7a599cfe1a"></a><code>WebListing</code><br><code>WebListing(string Leader, string Duty, string Description, string Remaining, DateTimeOffset? ReadAt, bool Live, int? Filled, int? Total, bool SlotsReadable, bool CountsAgree, WebSetting[] Settings, WebGroup[] Groups)</code> | 주 생성자/모델 | 웹/Discord 공통 임무·소개말·인원·시간·ReadAt·조건·그룹 정보다. | [L19](../../src/ListingWebView.cs#L19) |
| <a id="symbol-src-listingwebview-cs-22-f32749aa96"></a><code>WebQuery</code><br><code>WebQuery(string Id, string State, string Message)</code> | 주 생성자/모델 | query UUID·단계·결과 문구를 보관한다. | [L22](../../src/ListingWebView.cs#L22) |
| <a id="symbol-src-listingwebview-cs-23-f41a781cee"></a><code>WebView</code><br><code>WebView(int ApiVersion, string Version, string Session, bool LoggedIn, bool CanQuery, string Reason, string Status, DateTimeOffset UpdatedAt, WebListing? Listing, WebQuery? Query)</code> | 주 생성자/모델 | API v2의 session·발행시각·로그인/조회 가능 사유·모집글·editor·operation 상태를 묶는다. | [L23](../../src/ListingWebView.cs#L23) |
| <a id="symbol-src-listingwebview-cs-34-a2f13eb83e"></a><code>ListingWebView.From</code><br><code>WebListing? From(ListingSnapshot listing, bool loggedIn, bool queryBusy)</code> | 메서드 | 로그인한 현재 캐릭터의 모집 snapshot만 임무·소개말·설정·모든 그룹/자리로 변환한다. 친선 팀 이름과 물리 그룹 기준을 유지하고 조회 중/오래된 데이터의 Live를 구분한다. | [L34](../../src/ListingWebView.cs#L34) |

<a id="file-src-plugin-relay-cs"></a>
## src/Plugin.Relay.cs

[소스 파일](../../src/Plugin.Relay.cs) · 명명된 항목/진입점 9개

PVKR 설정/UI와 중계 client 생성·종료를 연결한다. 서버 주소만 영구 저장하고 연결 키는 런타임에 사용한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-relay-cs-9-a2aef50288"></a><code>Plugin.RelayEnabled</code><br><code>bool RelayEnabled</code> | 계산 속성 | 중계 client의 lifetime 실행 여부를 반환한다. 계산식: <code>relayClient?.Running == true</code>. | [L9](../../src/Plugin.Relay.cs#L9) |
| <a id="symbol-src-plugin-relay-cs-10-84c8d4d289"></a><code>Plugin.RelayConnected</code><br><code>bool RelayConnected</code> | 계산 속성 | 중계 WSS 연결 완료 여부를 반환한다. 계산식: <code>relayClient?.Connected == true</code>. | [L10](../../src/Plugin.Relay.cs#L10) |
| <a id="symbol-src-plugin-relay-cs-11-a7b32bb48b"></a><code>Plugin.RelayStatus</code><br><code>string RelayStatus</code> | 계산 속성 | 중계 연결 단계/오류 문구를 반환한다. 계산식: <code>relayClient?.Status ?? relayStatus</code>. | [L11](../../src/Plugin.Relay.cs#L11) |
| <a id="symbol-src-plugin-relay-cs-12-334f781467"></a><code>Plugin.RelayLink</code><br><code>string RelayLink</code> | 계산 속성 | 외부 브라우저 접속 링크를 반환한다. 계산식: <code>RelayEnabled ? relayClient!.ConnectionUrl : ""</code>. | [L12](../../src/Plugin.Relay.cs#L12) |
| <a id="symbol-src-plugin-relay-cs-13-0ee6325368"></a><code>Plugin.RelayCode</code><br><code>string RelayCode</code> | 계산 속성 | 외부 브라우저용 room.key 코드를 반환한다. 계산식: <code>RelayEnabled ? relayClient!.ConnectionCode : ""</code>. | [L13](../../src/Plugin.Relay.cs#L13) |
| <a id="symbol-src-plugin-relay-cs-14-addc2d741e"></a><code>Plugin.RelaySavedAddress</code><br><code>string RelaySavedAddress</code> | 계산 속성 | 영구 저장된 중계 서버 origin을 반환한다. 계산식: <code>(PluginInterface.GetPluginConfig() as RelayConfiguration)?.ServerUrl ?? ""</code>. | [L14](../../src/Plugin.Relay.cs#L14) |
| <a id="symbol-src-plugin-relay-cs-15-cff4ffc373"></a><code>Plugin.GenerateRelayKey</code><br><code>string GenerateRelayKey()</code> | 메서드 | 암호학적 random 32byte를 대문자 hex64 연결 키로 생성한다. | [L15](../../src/Plugin.Relay.cs#L15) |
| <a id="symbol-src-plugin-relay-cs-16-387327f020"></a><code>Plugin.StartRelay</code><br><code>bool StartRelay(string address, string key)</code> | 메서드 | 서버 origin·키를 검증하고 Discord 자동 연결과 기존 transport를 정리한 뒤 Bridge/relay client를 생성한다. 주소 설정을 저장한다. | [L16](../../src/Plugin.Relay.cs#L16) |
| <a id="symbol-src-plugin-relay-cs-36-f62619b9de"></a><code>Plugin.StopRelay</code><br><code>void StopRelay()</code> | 메서드 | relay client를 원자적으로 분리해 종료하고 소유한 조회·갱신만 취소한다. | [L36](../../src/Plugin.Relay.cs#L36) |

<a id="file-src-plugin-web-cs"></a>
## src/Plugin.Web.cs

[소스 파일](../../src/Plugin.Web.cs) · 명명된 항목/진입점 26개

Plugin에서 loopback/relay/Discord 중 하나의 Bridge를 Framework에 연결하는 façade와 작업 소유권 어댑터.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-web-cs-8-2d2402a26d"></a><code>Plugin.WebEnabled</code><br><code>bool WebEnabled</code> | 계산 속성 | 같은 PC loopback server가 실행 중인지 반환한다. 계산식: <code>webServer?.Running == true</code>. | [L8](../../src/Plugin.Web.cs#L8) |
| <a id="symbol-src-plugin-web-cs-9-76671a92da"></a><code>Plugin.AnyWebEnabled</code><br><code>bool AnyWebEnabled</code> | 계산 속성 | loopback/relay/Discord 중 하나라도 활성인지 반환한다. 계산식: <code>WebEnabled &#124;&#124; RelayEnabled &#124;&#124; DiscordEnabled</code>. | [L9](../../src/Plugin.Web.cs#L9) |
| <a id="symbol-src-plugin-web-cs-10-752c2b1f59"></a><code>Plugin.WebAddress</code><br><code>string WebAddress</code> | 계산 속성 | 인증 key를 뺀 loopback base URL을 반환한다. 계산식: <code>webServer?.BaseUrl ?? ""</code>. | [L10](../../src/Plugin.Web.cs#L10) |
| <a id="symbol-src-plugin-web-cs-11-027ca77eb4"></a><code>Plugin.WebLink</code><br><code>string WebLink</code> | 계산 속성 | loopback 활성 시 인증 fragment 접속 링크를 반환한다. 계산식: <code>WebEnabled ? webServer!.ConnectionUrl : ""</code>. | [L11](../../src/Plugin.Web.cs#L11) |
| <a id="symbol-src-plugin-web-cs-12-cf87b77ea1"></a><code>Plugin.WebStatus</code><br><code>string WebStatus</code> | 계산 속성 | loopback 연결/종료 상태를 사용자용 문구로 반환한다. 계산식: <code>webServer is { Running: false } ? "연결이 종료됐습니다. 브라우저 조회를 다시 켜 주세요." : webStatus</code>. | [L12](../../src/Plugin.Web.cs#L12) |
| <a id="symbol-src-plugin-web-cs-14-4ad052e971"></a><code>Plugin.QueryUnavailableReason</code><br><code>string? QueryUnavailableReason</code> | 계산 속성 | 새 수동/외부 조회가 불가능한 이유를 공통 guard로 계산한다. 계산식: <code>GetQueryUnavailableReason(false)</code>. | [L14](../../src/Plugin.Web.cs#L14) |
| <a id="symbol-src-plugin-web-cs-15-1ce390354a"></a><code>Plugin.GetQueryUnavailableReason</code><br><code>string? GetQueryUnavailableReason(bool ownQueryInProgress)</code> | 메서드 | disposed·네이티브 차단·진행 조회/갱신·게임 수정창·열린/예약된 PVKR 초안 때문에 새 조회를 실행할 수 없는 이유를 반환한다. | [L15](../../src/Plugin.Web.cs#L15) |
| <a id="symbol-src-plugin-web-cs-30-4067b3b4c4"></a><code>Plugin.TryQueryOwnListing</code><br><code>bool TryQueryOwnListing(out string error)</code> | 메서드 | 공통 조회 guard 통과 후 ListingReader.RequestOwn과 RequestRead를 예약한다. | [L30](../../src/Plugin.Web.cs#L30) |
| <a id="symbol-src-plugin-web-cs-37-c757a37280"></a><code>Plugin.StartWeb</code><br><code>void StartWeb()</code> | 메서드 | Discord 자동 연결을 끄고 다른 연결을 정리한 뒤 loopback server와 WebHost를 만든다. | [L37](../../src/Plugin.Web.cs#L37) |
| <a id="symbol-src-plugin-web-cs-57-6172a3dffd"></a><code>Plugin.StopWeb</code><br><code>void StopWeb()</code> | 메서드 | relay/Discord/loopback 모두 종료하고 각 연결이 소유한 요청만 취소한다. | [L57](../../src/Plugin.Web.cs#L57) |
| <a id="symbol-src-plugin-web-cs-67-2b92448afd"></a><code>Plugin.UpdateWeb</code><br><code>void UpdateWeb(long now)</code> | 메서드 | 현재 활성 transport의 Bridge.Update를 게임 스레드에서 호출한다. 발행 실패는 PublicationFailed로 처리한다. | [L67](../../src/Plugin.Web.cs#L67) |
| <a id="symbol-src-plugin-web-cs-75-d6d19b3cd6"></a><code>Plugin.WebHost</code><br><code>WebHost(Plugin plugin)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L75](../../src/Plugin.Web.cs#L75) |
| <a id="symbol-src-plugin-web-cs-80-b195cf1ca5"></a><code>Plugin.WebHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | 현재 로그인한 본인의 ContentId를 반환한다. 로그아웃/미준비는0이다. 계산식: <code>ClientState.IsLoggedIn &amp;&amp; PlayerState.IsLoaded ? PlayerState.ContentId : 0</code>. | [L80](../../src/Plugin.Web.cs#L80) |
| <a id="symbol-src-plugin-web-cs-81-9e3694263a"></a><code>Plugin.WebHost.QueryBlockReason</code><br><code>string? QueryBlockReason</code> | 계산 속성 | 현재 플러그인의 공통 조회 차단 사유를 반환한다. 계산식: <code>plugin.QueryUnavailableReason</code>. | [L81](../../src/Plugin.Web.cs#L81) |
| <a id="symbol-src-plugin-web-cs-82-203d91cc94"></a><code>Plugin.WebHost.QueryBusy</code><br><code>bool QueryBusy</code> | 계산 속성 | ListingReader의 조회 진행 상태를 반환한다. 계산식: <code>plugin.listingReader.Busy</code>. | [L82](../../src/Plugin.Web.cs#L82) |
| <a id="symbol-src-plugin-web-cs-83-eda4e435d3"></a><code>Plugin.WebHost.QuerySucceeded</code><br><code>bool QuerySucceeded</code> | 계산 속성 | 마지막 본인 조회가 검증까지 성공했는지 반환한다. 계산식: <code>plugin.listingReader.LastOwnRequestSucceeded</code>. | [L83](../../src/Plugin.Web.cs#L83) |
| <a id="symbol-src-plugin-web-cs-84-65bb7efb22"></a><code>Plugin.WebHost.DetailRevision</code><br><code>long DetailRevision</code> | 계산 속성 | 상세창 PostSetup/PostRefresh revision을 읽는다. 계산식: <code>plugin.listingReader.DetailRevision</code>. | [L84](../../src/Plugin.Web.cs#L84) |
| <a id="symbol-src-plugin-web-cs-85-111436a52d"></a><code>Plugin.WebHost.Listing</code><br><code>ListingSnapshot Listing</code> | 계산 속성 | 현재 관리형 ListingSnapshot을 반환한다. 계산식: <code>plugin.Listing</code>. | [L85](../../src/Plugin.Web.cs#L85) |
| <a id="symbol-src-plugin-web-cs-86-7dcd44fa22"></a><code>Plugin.WebHost.RefreshBlockReason</code><br><code>string? RefreshBlockReason</code> | 계산 속성 | 공통 조회 차단에 인게임 대기 변경/cooldown 등을 더해 외부 갱신 거부 이유를 반환한다. 계산식: <code>plugin.QueryUnavailableReason ??             (plugin.PendingChanges.Items.Count &gt; 0 ? "PVKR에 적용 대기 중인 변경사항을 먼저 처리해 주세요." :             plugin.Refresh.CoolingDown(Environment.TickCount64) ? "잠시 후 다시 갱신해 주세요." : null)</code>. | [L86](../../src/Plugin.Web.cs#L86) |
| <a id="symbol-src-plugin-web-cs-89-e3e725bb4a"></a><code>Plugin.WebHost.CaptureEditTarget</code><br><code>RefreshTarget? CaptureEditTarget()</code> | 메서드 | 진행 작업이 없고 현재 캐릭터의 live Self 상세가 검증된 경우에만 편집 source를 캡처한다. | [L89](../../src/Plugin.Web.cs#L89) |
| <a id="symbol-src-plugin-web-cs-95-a2d53c315a"></a><code>Plugin.WebHost.StartRefresh</code><br><code>bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error)</code> | 메서드 | 외부 source와 현재 본인/차단 사유를 검사한다. changes가 비면 단순 갱신, 있으면 batch를 요청하고 생성된 refresh serial을 guard에 기록한다. | [L95](../../src/Plugin.Web.cs#L95) |
| <a id="symbol-src-plugin-web-cs-108-d14c41e672"></a><code>Plugin.WebHost.ReadRefresh</code><br><code>WebRefreshProgress? ReadRefresh()</code> | 메서드 | 자기가 시작한 refresh serial과 현재 serial이 같을 때만 진행 결과를 반환한다. | [L108](../../src/Plugin.Web.cs#L108) |
| <a id="symbol-src-plugin-web-cs-110-cf52098510"></a><code>Plugin.WebHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 메서드 | 공통 수동 조회 경로를 호출하고 실제 발급된 request revision을 소유권 guard에 기록한다. | [L110](../../src/Plugin.Web.cs#L110) |
| <a id="symbol-src-plugin-web-cs-115-7bcc4b593c"></a><code>Plugin.WebHost.BeforeRead</code><br><code>void BeforeRead(long now)</code> | 메서드 | native dispatch 직전에 외부 연결·캐릭터·대기 시간·충돌을 재검사한다. 자신 소유의 query/refresh만 취소한다. | [L115](../../src/Plugin.Web.cs#L115) |
| <a id="symbol-src-plugin-web-cs-127-bb3d1aaadc"></a><code>Plugin.WebHost.CancelOwnedQuery</code><br><code>void CancelOwnedQuery()</code> | 메서드 | 현재 query serial이 이 연결 소유일 때만 FailRequest하고 guard를 비운다. | [L127](../../src/Plugin.Web.cs#L127) |
| <a id="symbol-src-plugin-web-cs-134-6d1c8a6303"></a><code>Plugin.WebHost.CancelOwnedRefresh</code><br><code>void CancelOwnedRefresh()</code> | 메서드 | 현재 refresh serial이 이 연결 소유일 때만 Cancel하고 guard를 비운다. | [L134](../../src/Plugin.Web.cs#L134) |

<a id="file-src-web-app-js"></a>
## src/Web/app.js

[소스 파일](../../src/Web/app.js) · 명명된 항목/진입점 21개

웹 화면 상태/폴링·조회·수정/갱신 요청. GET state 폴링은 서버 모집글 조회를 매번 발생시키지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-web-app-js-1-1021958e80"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | 연결값과 미결 작업 metadata를 복원하고 editor·버튼 handler 및 상태 폴링을 초기화한다. | [L1](../../src/Web/app.js#L1) |
| <a id="symbol-src-web-app-js-3-0b145d1a61"></a><code>byId</code><br><code>id =&gt;</code> | JS 함수/handler | ID로 DOM element를 찾는 helper다. | [L3](../../src/Web/app.js#L3) |
| <a id="symbol-src-web-app-js-16-9cc5c04412"></a><code>byId("connect-form").on(submit)</code><br><code>event =&gt;</code> | JS 함수/handler | 입력 연결값을 검증해 fragment로 반영하고 페이지를 다시 열어 새 연결 session을 시작한다. | [L16](../../src/Web/app.js#L16) |
| <a id="symbol-src-web-app-js-25-afeb5aee60"></a><code>byId("forget-connection").on(click)</code><br><code>() =&gt;</code> | JS 함수/handler | 저장된 연결 키·room·미결 작업을 지우고 페이지를 다시 연다. | [L25](../../src/Web/app.js#L25) |
| <a id="symbol-src-web-app-js-30-544bd73611"></a><code>text</code><br><code>function text(id, value)</code> | JS 함수/handler | 문자열이 달라졌을 때만 textContent를 변경한다. | [L30](../../src/Web/app.js#L30) |
| <a id="symbol-src-web-app-js-31-b8640e75a0"></a><code>el</code><br><code>function el(tag, className, value)</code> | JS 함수/handler | 지정 tag/class/textContent의 안전한 DOM node를 생성한다. 모집 문자열을 innerHTML로 삽입하지 않는다. | [L31](../../src/Web/app.js#L31) |
| <a id="symbol-src-web-app-js-37-2032c933cf"></a><code>current</code><br><code>() =&gt;</code> | JS 함수/handler | editor에 현재 연결/session/state/진행 작업 정보를 제공한다. | [L37](../../src/Web/app.js#L37) |
| <a id="symbol-src-web-app-js-38-a478414287"></a><code>active</code><br><code>operation =&gt;</code> | JS 함수/handler | operation 상태가 queued 또는 running인지 판정한다. | [L38](../../src/Web/app.js#L38) |
| <a id="symbol-src-web-app-js-39-9a0db96b88"></a><code>savePending</code><br><code>function savePending(value)</code> | JS 함수/handler | 미결 쓰기의 ID·session·시각만 sessionStorage에 저장한다. body/PIN은 저장하지 않는다. | [L39](../../src/Web/app.js#L39) |
| <a id="symbol-src-web-app-js-43-9d2f2f1b37"></a><code>badge</code><br><code>function badge(value, tone)</code> | JS 함수/handler | 연결/동작 상태 badge의 class와 label을 갱신한다. | [L43](../../src/Web/app.js#L43) |
| <a id="symbol-src-web-app-js-44-564fda03e0"></a><code>stale</code><br><code>function stale()</code> | JS 함수/handler | snapshot UpdatedAt 기준 5초 초과를 stale로 판정한다. | [L44](../../src/Web/app.js#L44) |
| <a id="symbol-src-web-app-js-45-544dc0454b"></a><code>time</code><br><code>function time(value)</code> | JS 함수/handler | 서버 시각 문자열을 한국어 locale의 표시 시각으로 변환한다. | [L45](../../src/Web/app.js#L45) |
| <a id="symbol-src-web-app-js-49-ba7110731f"></a><code>renderStatus</code><br><code>function renderStatus()</code> | JS 함수/handler | 인증·온라인·stale·진행 작업·dirty draft 여부에 따라 조회/갱신/편집 버튼을 잠그고 안내를 표시한다. | [L49](../../src/Web/app.js#L49) |
| <a id="symbol-src-web-app-js-82-e1c2eff758"></a><code>render</code><br><code>function render(view)</code> | JS 함수/handler | API v2를 검사하고 session 변경 시 이전 draft/미결 작업을 무효화한다. 임무·조건·전체 그룹/자리·허용 직업을 DOM 차이 갱신하며 열린 상세와 물리 인덱스를 보존한다. | [L82](../../src/Web/app.js#L82) |
| <a id="symbol-src-web-app-js-147-6963d9fbbe"></a><code>api</code><br><code>async function api(path, options = {})</code> | JS 함수/handler | 같은 origin에 bearer/room header, cache/credentials 제한과 5초 timeout을 붙여 fetch한다. 401이면 인증 상태와 편집을 지우고 JSON 오류에 status를 붙인다. | [L147](../../src/Web/app.js#L147) |
| <a id="symbol-src-web-app-js-163-fb7cee9b04"></a><code>acceptOperation</code><br><code>function acceptOperation(operation)</code> | JS 함수/handler | 미결 ID와 응답 operation을 맞추고 수락된 편집 초안을 정리한다. terminal이면 pending metadata를 지운다. | [L163](../../src/Web/app.js#L163) |
| <a id="symbol-src-web-app-js-168-45128e3f58"></a><code>renderOperation</code><br><code>function renderOperation()</code> | JS 함수/handler | 작업 상태와 항목별 결과를 생략하지 않고 표시한다. | [L168](../../src/Web/app.js#L168) |
| <a id="symbol-src-web-app-js-181-7cb77f6cd5"></a><code>applyChanges</code><br><code>async function applyChanges(revision, changes)</code> | JS 함수/handler | UUID와 pending metadata를 POST 전에 저장하고 apply를 1회 전송한다. timeout/통신 단절은 결과를 조회하며 자동 POST 재시도하지 않는다. finally에서 PIN을 지운다. | [L181](../../src/Web/app.js#L181) |
| <a id="symbol-src-web-app-js-195-64ea5b4d33"></a><code>refreshButton.on(click)</code><br><code>() =&gt;</code> | JS 함수/handler | 변경 배열을 비운 apply를 보내 단순 모집 갱신을 요청한다. | [L195](../../src/Web/app.js#L195) |
| <a id="symbol-src-web-app-js-196-b664ea6de1"></a><code>queryButton.on(click)</code><br><code>async () =&gt;</code> | JS 함수/handler | 독립 UUID와 session으로 query 요청을 한 번 보내고 결과를 기다린다. | [L196](../../src/Web/app.js#L196) |
| <a id="symbol-src-web-app-js-207-530ed32f58"></a><code>poll</code><br><code>async function poll()</code> | JS 함수/handler | 활성 탭은1초, background는4초 간격으로 state 및 미결 작업 결과를 GET한다. 서버 모집 조회/갱신은 버튼 요청 때만 큐에 들어간다. | [L207](../../src/Web/app.js#L207) |

<a id="file-src-web-connection-js"></a>
## src/Web/connection.js

[소스 파일](../../src/Web/connection.js) · 명명된 항목/진입점 4개

연결 링크/room.key 파싱, sessionStorage와 URL fragment 정리, 불명확한 요청 결과 분류. 브라우저와 Node 테스트가 같은 순수 로직을 사용한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-web-connection-js-1-f9200c75bf"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | 연결 helper를 CommonJS와 window.PvkrConnection에 내보낸다. | [L1](../../src/Web/connection.js#L1) |
| <a id="symbol-src-web-connection-js-4-3101957189"></a><code>parse</code><br><code>function parse(value, origin)</code> | JS 함수/handler | raw key 또는 같은 origin의 root 접속 링크에서 64hex key와 선택적 32hex room을 검증한다. query/credentials/다른 origin은 거부한다. | [L4](../../src/Web/connection.js#L4) |
| <a id="symbol-src-web-connection-js-19-2953541ecc"></a><code>read</code><br><code>function read(location, history, storage)</code> | JS 함수/handler | URL hash의 새 연결값을 우선 읽고 이전 키/room/미결 작업을 제거한다. 주소에서 fragment를 지운 뒤 sessionStorage에 저장하며 저장 불가 환경도 처리한다. | [L19](../../src/Web/connection.js#L19) |
| <a id="symbol-src-web-connection-js-39-bc04a96c40"></a><code>definiteRejection</code><br><code>status =&gt;</code> | JS 함수/handler | 408을 제외한 4xx를 명백한 거절로 구분한다. 통신 오류/timeout은 자동 재전송 가능한 실패로 분류하지 않는다. | [L39](../../src/Web/connection.js#L39) |

<a id="file-src-web-editor-js"></a>
## src/Web/editor.js

[소스 파일](../../src/Web/editor.js) · 명명된 항목/진입점 26개

외부 편집 초안 검증과 DOM editor. 입력 자체는 게임에 반영하지 않고 collect한 변경을 submit callback으로 넘긴다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-web-editor-js-1-a2e4839783"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | 순수 편집 helper를 Node 테스트에 내보내고 브라우저에 PvkrEditor.create를 등록한다. 편집 열기/새로 읽기 listener도 연결한다. | [L1](../../src/Web/editor.js#L1) |
| <a id="symbol-src-web-editor-js-3-5883fef938"></a><code>normalize</code><br><code>text =&gt;</code> | JS 함수/handler | CRLF/CR을 LF로 통일하고 앞뒤 공백을 정리한다. | [L3](../../src/Web/editor.js#L3) |
| <a id="symbol-src-web-editor-js-4-267fa6d488"></a><code>sameJobs</code><br><code>(a, b) =&gt;</code> | JS 함수/handler | 직업 ID 배열을 정렬/집합 의미로 비교해 순서만 다른 선택을 변경으로 보지 않는다. | [L4](../../src/Web/editor.js#L4) |
| <a id="symbol-src-web-editor-js-5-9fc4ec664b"></a><code>draft</code><br><code>function draft(model)</code> | JS 함수/handler | 서버 editor model에서 편집용 값·자리별 직업 집합을 복사한다. PIN은 빈 문자열로 시작한다. | [L5](../../src/Web/editor.js#L5) |
| <a id="symbol-src-web-editor-js-9-918bc91945"></a><code>collect</code><br><code>function collect(model, input)</code> | JS 함수/handler | 지원 필드만 검사하여 실제 달라진 변경을 수집한다. 소개말 UTF-8192byte, 비공개 네 자리 PIN, 평균 레벨1~999, 비어 있지 않은 직업 선택을 검증한다. | [L9](../../src/Web/editor.js#L9) |
| <a id="symbol-src-web-editor-js-42-138bfe6786"></a><code>create</code><br><code>({ byId, el, current, changed, submit })</code> | JS 함수/handler | DOM helper·현재 연결 상태·changed/submit callback을 받아 editor 상태와 이벤트 handler를 생성한다. | [L42](../../src/Web/editor.js#L42) |
| <a id="symbol-src-web-editor-js-45-ca030ab211"></a><code>result</code><br><code>function result()</code> | JS 함수/handler | 현재 model/draft를 collect하여 변경 목록과 오류를 얻는다. | [L45](../../src/Web/editor.js#L45) |
| <a id="symbol-src-web-editor-js-46-ba1f9d69be"></a><code>clear</code><br><code>function clear()</code> | JS 함수/handler | model/session/draft 및 표시 편집 상태를 비운다. | [L46](../../src/Web/editor.js#L46) |
| <a id="symbol-src-web-editor-js-47-9b4ce57afb"></a><code>clearPassword</code><br><code>function clearPassword()</code> | JS 함수/handler | 입력 PIN을 즉시 지우고 비공개 입력창의 값도 비운다. | [L47](../../src/Web/editor.js#L47) |
| <a id="symbol-src-web-editor-js-48-62933eeb90"></a><code>control</code><br><code>function control(field, label)</code> | JS 함수/handler | label과 input을 연결한 공통 편집 필드 DOM을 만든다. | [L48](../../src/Web/editor.js#L48) |
| <a id="symbol-src-web-editor-js-52-6d4dc5600a"></a><code>unavailable</code><br><code>function unavailable(node, field)</code> | JS 함수/handler | 지원하지 않는 필드의 안내를 출력한다. | [L52](../../src/Web/editor.js#L52) |
| <a id="symbol-src-web-editor-js-56-b24fcb1b6b"></a><code>select</code><br><code>function select(field, title, options)</code> | JS 함수/handler | 허용 option 목록과 값 변경 handler를 가진 select를 구성한다. | [L56](../../src/Web/editor.js#L56) |
| <a id="symbol-src-web-editor-js-60-b51f66e5eb"></a><code>node.on(change)</code><br><code>() =&gt;</code> | JS 함수/handler | select 또는 checkbox의 변경을 해당 관리형 draft 값에 반영한 뒤 편집 상태를 다시 계산한다. 네이티브 작업은 실행하지 않는다. | [L60](../../src/Web/editor.js#L60) |
| <a id="symbol-src-web-editor-js-63-3f01ac8629"></a><code>toggle</code><br><code>function toggle(field, title, numeric = false)</code> | JS 함수/handler | bool 설정용 checkbox와 label을 만든다. | [L63](../../src/Web/editor.js#L63) |
| <a id="symbol-src-web-editor-js-66-8702fe5be0"></a><code>node.on(change)</code><br><code>() =&gt;</code> | JS 함수/handler | select 또는 checkbox의 변경을 해당 관리형 draft 값에 반영한 뒤 편집 상태를 다시 계산한다. 네이티브 작업은 실행하지 않는다. | [L66](../../src/Web/editor.js#L66) |
| <a id="symbol-src-web-editor-js-69-c8fb46b2e5"></a><code>open</code><br><code>function open()</code> | JS 함수/handler | 편집 영역을 열고 최신 유효 model을 초안에 로드한다. | [L69](../../src/Web/editor.js#L69) |
| <a id="symbol-src-web-editor-js-74-d75d230f81"></a><code>load</code><br><code>function load()</code> | JS 함수/handler | 현재 editor source/session을 고정하고 필드별 입력과 모든 그룹/빈자리 직업 선택 UI를 생성한다. | [L74](../../src/Web/editor.js#L74) |
| <a id="symbol-src-web-editor-js-81-06c1cd976f"></a><code>area.on(input)</code><br><code>() =&gt;</code> | JS 함수/handler | 소개말 입력을 draft에 반영하고 바이트 수/오류/적용 가능 여부를 다시 계산한다. | [L81](../../src/Web/editor.js#L81) |
| <a id="symbol-src-web-editor-js-92-96205c6d78"></a><code>itemEnabled.on(change)</code><br><code>() =&gt;</code> | JS 함수/handler | 평균 레벨 조건 사용 여부를 draft에 반영한다. | [L92](../../src/Web/editor.js#L92) |
| <a id="symbol-src-web-editor-js-93-9c98d60c93"></a><code>itemValue.on(input)</code><br><code>() =&gt;</code> | JS 함수/handler | 평균 레벨 숫자 문자열을 draft에 반영한다. | [L93](../../src/Web/editor.js#L93) |
| <a id="symbol-src-web-editor-js-104-145949ce4b"></a><code>pin.on(input)</code><br><code>() =&gt;</code> | JS 함수/handler | PIN을 메모리 초안에만 반영한다. state/sessionStorage에 보관하지 않는다. | [L104](../../src/Web/editor.js#L104) |
| <a id="symbol-src-web-editor-js-106-6ea1bc81b7"></a><code>byId("edit-private").on(change)</code><br><code>() =&gt;</code> | JS 함수/handler | 공개 전환 시 PIN 초안/입력창을 비우고 검증 결과를 갱신한다. | [L106](../../src/Web/editor.js#L106) |
| <a id="symbol-src-web-editor-js-113-ece7a570d1"></a><code>all.on(click)</code><br><code>() =&gt;</code> | JS 함수/handler | 해당 빈자리의 모든 알려진 직업을 선택하고 checkbox 표시와 dirty 상태를 갱신한다. | [L113](../../src/Web/editor.js#L113) |
| <a id="symbol-src-web-editor-js-117-cb6a80c2fe"></a><code>check.on(change)</code><br><code>() =&gt;</code> | JS 함수/handler | 한 자리의 개별 직업 ID를 선택 집합에 추가/제거하고 변경을 계산한다. | [L117](../../src/Web/editor.js#L117) |
| <a id="symbol-src-web-editor-js-128-8decaa56dd"></a><code>update</code><br><code>function update(available, locked, reason)</code> | JS 함수/handler | 작업 중/세션 또는 revision 변경/오프라인이면 입력과 제출을 잠근다. 오류·바이트 수·아이템 레벨/PIN 가용성·선택 직업 수를 표시한다. | [L128](../../src/Web/editor.js#L128) |
| <a id="symbol-src-web-editor-js-150-f11477c75e"></a><code>byId("edit-form").on(submit)</code><br><code>event =&gt;</code> | JS 함수/handler | 브라우저 기본 제출을 막고 현재 source와 collect 결과가 유효할 때 submit callback을 1회 호출한다. | [L150](../../src/Web/editor.js#L150) |
