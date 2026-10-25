# Discord 연결·카드·폼·이모지·토큰

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

개인 봇 owner만 조작할 수 있다. interaction ACK가 성공한 뒤 공통 Bridge에 게임 작업을 넣는다. 카드/모달/이모지는 각자의 책임이며 토큰은 Windows CurrentUser DPAPI ciphertext로만 저장한다.

기준: 0.3.49.0 소스. 기반 main `a34099802f12d6e5c4859489472489319d146e4f` 이후 AFK 설정·Discord 수정창 닫기 변경을 포함한다. 2026-10-25 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [src/ListingDiscordClient.cs](#file-src-listingdiscordclient-cs)
- [src/ListingDiscordEmojiAssets.cs](#file-src-listingdiscordemojiassets-cs)
- [src/ListingDiscordIcons.cs](#file-src-listingdiscordicons-cs)
- [src/ListingDiscordPanel.Forms.cs](#file-src-listingdiscordpanel-forms-cs)
- [src/ListingDiscordPanel.Render.cs](#file-src-listingdiscordpanel-render-cs)
- [src/ListingDiscordPanel.Results.cs](#file-src-listingdiscordpanel-results-cs)
- [src/ListingDiscordPanel.cs](#file-src-listingdiscordpanel-cs)
- [src/ListingDiscordSession.Recovery.cs](#file-src-listingdiscordsession-recovery-cs)
- [src/ListingDiscordSession.cs](#file-src-listingdiscordsession-cs)
- [src/Plugin.Discord.cs](#file-src-plugin-discord-cs)
- [src/RelayConfiguration.cs](#file-src-relayconfiguration-cs)

<a id="file-src-listingdiscordclient-cs"></a>
## src/ListingDiscordClient.cs

[소스 파일](../../src/ListingDiscordClient.cs) · 명명된 항목/진입점 26개

Discord.Net Gateway/REST 연결과 application owner 검증, /pvkr 등록, 이모지 동기화 및 SDK interaction 어댑터. 네트워크 작업은 게임 메모리를 직접 읽지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordclient-cs-20-abc9620864"></a><code>ListingDiscordClient.Running</code><br><code>bool Running</code> | 계산 속성 | 비동기 lifetime이 종료되지 않았는지 반환한다. 실제 socket 연결 완료와는 다르다. 계산식: <code>Volatile.Read(ref running) != 0 &amp;&amp; Volatile.Read(ref disposed) == 0</code>. | [L20](../../src/ListingDiscordClient.cs#L20) |
| <a id="symbol-src-listingdiscordclient-cs-21-e980187fbe"></a><code>ListingDiscordClient.Connected</code><br><code>bool Connected</code> | 계산 속성 | 실제 외부 연결 완료 상태를 thread-safe하게 읽는다. 계산식: <code>Running &amp;&amp; Volatile.Read(ref ready) != 0 &amp;&amp; socket.ConnectionState == ConnectionState.Connected</code>. | [L21](../../src/ListingDiscordClient.cs#L21) |
| <a id="symbol-src-listingdiscordclient-cs-22-007a356cf3"></a><code>ListingDiscordClient.Status</code><br><code>string Status</code> | 계산 속성 | 현재 사용자용 연결/작업 진행 문구를 반환한다. 계산식: <code>Volatile.Read(ref status)</code>. | [L22](../../src/ListingDiscordClient.cs#L22) |
| <a id="symbol-src-listingdiscordclient-cs-23-d1d1f2ae3d"></a><code>ListingDiscordClient.InviteUrl</code><br><code>string InviteUrl</code> | 계산 속성 | owner가 해당 봇을 설치할 수 있는 초대 URL을 반환한다. 계산식: <code>Volatile.Read(ref inviteUrl)</code>. | [L23](../../src/ListingDiscordClient.cs#L23) |
| <a id="symbol-src-listingdiscordclient-cs-24-aaf063f57c"></a><code>ListingDiscordClient.Completion</code><br><code>Task Completion</code> | 계산 속성 | 비동기 연결/서버 종료를 기다릴 수 있는 lifetime Task를 반환한다. 계산식: <code>lifetime</code>. | [L24](../../src/ListingDiscordClient.cs#L24) |
| <a id="symbol-src-listingdiscordclient-cs-26-e8843a9428"></a><code>ListingDiscordClient.ListingDiscordClient</code><br><code>ListingDiscordClient(ListingWebBridge bridge, string token, DiscordSocketConfig? config = null)</code> | 생성자 | Bridge·token·socket config를 받고 Gateway 이벤트를 연결한다. 비동기 lifetime을 시작하며 game thread를 기다리게 하지 않는다. | [L26](../../src/ListingDiscordClient.cs#L26) |
| <a id="symbol-src-listingdiscordclient-cs-39-03fa3dfd2a"></a><code>ListingDiscordClient.Run</code><br><code>Task Run(string token)</code> | 메서드 | 로그인→개인 application owner 검사→Gateway READY→/pvkr 등록→이모지 준비를 진행한다. 종료 시 Bridge/session/socket을 정리하고 비밀값 없는 단계별 실패를 표시한다. | [L39](../../src/ListingDiscordClient.cs#L39) |
| <a id="symbol-src-listingdiscordclient-cs-90-abb4efbdfd"></a><code>ListingDiscordClient.Options</code><br><code>RequestOptions Options()</code> | 메서드 | 일반 SDK 요청은 rate-limit 재시도/timeout 정책을, Response 내 요청은 최초 ACK 2초·재시도 없음과 후속 10초 정책을 구성한다. | [L90](../../src/ListingDiscordClient.cs#L90) |
| <a id="symbol-src-listingdiscordclient-cs-91-3b5498bcff"></a><code>ListingDiscordClient.PrepareEmojis</code><br><code>Task PrepareEmojis(ListingDiscordIcons icons)</code> | 메서드 | 5분 취소 예산 안에서 application emoji 목록을 읽고 필요한 이미지 재사용·이름 변경·업로드를 수행한다. 일부 실패해도 기호 fallback으로 연결을 유지한다. | [L91](../../src/ListingDiscordClient.cs#L91) |
| <a id="symbol-src-listingdiscordclient-cs-95-9618b07bb2"></a><code>ListingDiscordClient.PrepareEmojis.EmojiOptions</code><br><code>RequestOptions EmojiOptions(CancellationToken ct)</code> | 로컬 함수 | 이모지 API 호출의 취소 token/timeout/재시도 옵션을 구성한다. | [L95](../../src/ListingDiscordClient.cs#L95) |
| <a id="symbol-src-listingdiscordclient-cs-97-194b9b736d"></a><code>ListingDiscordClient.PrepareEmojis.Progress</code><br><code>void Progress(int done, int total)</code> | 로컬 함수 | 이모지 처리 완료 수와 재사용/추가/이름 변경 진행 상태를 표시한다. | [L97](../../src/ListingDiscordClient.cs#L97) |
| <a id="symbol-src-listingdiscordclient-cs-126-5b20d1deaf"></a><code>ListingDiscordClient.ConnectedMessage</code><br><code>string ConnectedMessage</code> | 계산 속성 | 연결 완료 문구와 현재 이모지 상태를 합성한다. 계산식: <code>ConnectedStatus + (emojiStatus.Length &gt; 0 ? "\n" + emojiStatus : "")</code>. | [L126](../../src/ListingDiscordClient.cs#L126) |
| <a id="symbol-src-listingdiscordclient-cs-127-340a0f5339"></a><code>ListingDiscordClient.SetEmojiStatus</code><br><code>void SetEmojiStatus(string text)</code> | 메서드 | 이모지 상태를 thread-safe하게 갱신해 연결 UI에 반영한다. | [L127](../../src/ListingDiscordClient.cs#L127) |
| <a id="symbol-src-listingdiscordclient-cs-132-24acad2501"></a><code>ListingDiscordClient.FailureDetails</code><br><code>string FailureDetails(string stage, Exception error)</code> | 메서드 | 실패 단계와 HTTP/Gateway/예외 종류만 반환한다. 원시 SDK 오류/토큰은 상태 문자열에 붙이지 않는다. | [L132](../../src/ListingDiscordClient.cs#L132) |
| <a id="symbol-src-listingdiscordclient-cs-142-4f9dc505ef"></a><code>ListingDiscordClient.Ready</code><br><code>Task Ready()</code> | 메서드 | READY 대기 completion을 완료하고 연결 상태를 갱신한다. | [L142](../../src/ListingDiscordClient.cs#L142) |
| <a id="symbol-src-listingdiscordclient-cs-148-8659e42edb"></a><code>ListingDiscordClient.Disconnected</code><br><code>Task Disconnected(Exception exception)</code> | 메서드 | 일시 Gateway 단절 상태를 기록한다. 수동 작업을 재전송하지 않는다. | [L148](../../src/ListingDiscordClient.cs#L148) |
| <a id="symbol-src-listingdiscordclient-cs-153-1fc356f468"></a><code>ListingDiscordClient.Reconnected</code><br><code>Task Reconnected()</code> | 메서드 | 재연결/RESUME 후 READY가 다시 오지 않는 경우에도 연결 표시를 복구한다. | [L153](../../src/ListingDiscordClient.cs#L153) |
| <a id="symbol-src-listingdiscordclient-cs-160-cc4ce78c75"></a><code>ListingDiscordClient.Interaction</code><br><code>Task Interaction(SocketInteraction interaction)</code> | 메서드 | slash/button/select/modal을 관리형 DiscordInput으로 바꾸고 비동기 session 처리를 분리한다. Gateway 수신 이벤트를 긴 폴링으로 막지 않는다. | [L160](../../src/ListingDiscordClient.cs#L160) |
| <a id="symbol-src-listingdiscordclient-cs-178-3ff840fb51"></a><code>ListingDiscordClient.ReadModal</code><br><code>DiscordInput ReadModal(SocketModal modal)</code> | 메서드 | Discord modal의 checkbox group/checkbox/radio/text 입력만 allowlist로 읽고 중복·지원하지 않는 구조는 거부한다. | [L178](../../src/ListingDiscordClient.cs#L178) |
| <a id="symbol-src-listingdiscordclient-cs-201-724fe972bd"></a><code>ListingDiscordClient.Dispose</code><br><code>void Dispose()</code> | 메서드 | 중복 종료를 방지하고 lifetime 취소·Bridge.Stop을 수행한다. 소켓 종료 완료를 게임 스레드에서 동기 대기하지 않는다. | [L201](../../src/ListingDiscordClient.cs#L201) |
| <a id="symbol-src-listingdiscordclient-cs-208-0c2d92bd7f"></a><code>ListingDiscordClient.Response</code><br><code>Response(SocketInteraction interaction, CancellationToken cancellation)</code> | 주 생성자/모델 | 해당 SDK interaction에 묶인 IDiscordResponse 어댑터를 만든다. | [L208](../../src/ListingDiscordClient.cs#L208) |
| <a id="symbol-src-listingdiscordclient-cs-210-bd122b7b60"></a><code>ListingDiscordClient.Response.Options</code><br><code>RequestOptions Options(bool ack = false)</code> | 메서드 | 일반 SDK 요청은 rate-limit 재시도/timeout 정책을, Response 내 요청은 최초 ACK 2초·재시도 없음과 후속 10초 정책을 구성한다. | [L210](../../src/ListingDiscordClient.cs#L210) |
| <a id="symbol-src-listingdiscordclient-cs-214-ae315d2b82"></a><code>ListingDiscordClient.Response.Defer</code><br><code>Task Defer()</code> | 메서드 | 게임 작업 전 interaction을 ACK한다. slash는 ephemeral deferred response를 사용한다. | [L214](../../src/ListingDiscordClient.cs#L214) |
| <a id="symbol-src-listingdiscordclient-cs-215-6582f3e13e"></a><code>ListingDiscordClient.Response.Modal</code><br><code>Task Modal(Modal modal)</code> | 메서드 | 즉시 modal을 최초 응답으로 보낸다. 이미 defer한 interaction에 modal을 다시 열지 않는다. | [L215](../../src/ListingDiscordClient.cs#L215) |
| <a id="symbol-src-listingdiscordclient-cs-216-88e9804aa5"></a><code>ListingDiscordClient.Response.Screen</code><br><code>Task Screen(DiscordScreen screen, bool replace, bool deferred)</code> | 메서드 | 새 카드/기존 카드 업데이트/후속 응답을 맞는 SDK 경로로 전달한다. Components V2를 쓰고 기존 content/embeds를 비우며 AllowedMentions.None을 적용한다. | [L216](../../src/ListingDiscordClient.cs#L216) |
| <a id="symbol-src-listingdiscordclient-cs-218-cbc4ac17a3"></a><code>ListingDiscordClient.Response.Screen.Set</code><br><code>void Set(MessageProperties p)</code> | 로컬 함수 | SDK MessageProperties에 카드 components·flags·mention 정책을 설정한다. | [L218](../../src/ListingDiscordClient.cs#L218) |

<a id="file-src-listingdiscordemojiassets-cs"></a>
## src/ListingDiscordEmojiAssets.cs

[소스 파일](../../src/ListingDiscordEmojiAssets.cs) · 명명된 항목/진입점 5개

embedded PNG 80개의 asset key·읽기 쉬운 이름·legacy 이름 매핑 및 application emoji 동기화. 서버에 수동 업로드할 필요가 없다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordemojiassets-cs-5-fd339d6a0c"></a><code>DiscordEmojiAsset</code><br><code>DiscordEmojiAsset(string Key, string FileName, string Name, string? LegacyName)</code> | 주 생성자/모델 | embedded resource key·파일명·현재/legacy emoji 이름을 연결한다. Open으로 PNG bytes를 얻는다. | [L5](../../src/ListingDiscordEmojiAssets.cs#L5) |
| <a id="symbol-src-listingdiscordemojiassets-cs-7-04a2bd8d36"></a><code>DiscordEmojiAsset.Open</code><br><code>Stream Open()</code> | 메서드 | embedded PNG resource를 읽고 1~256KiB 크기 제한을 확인한 byte 배열을 반환한다. | [L7](../../src/ListingDiscordEmojiAssets.cs#L7) |
| <a id="symbol-src-listingdiscordemojiassets-cs-24-c5de42e7b3"></a><code>ListingDiscordEmojiAssets.NameFor</code><br><code>string NameFor(string key)</code> | 메서드 | asset key를 job/class/role 및 점유 join 접두어가 포함된 Discord 이모지 이름으로 변환한다. | [L24](../../src/ListingDiscordEmojiAssets.cs#L24) |
| <a id="symbol-src-listingdiscordemojiassets-cs-26-aaebfc642d"></a><code>ListingDiscordEmojiAssets.Build</code><br><code>IReadOnlyList&lt;DiscordEmojiAsset&gt; Build()</code> | 메서드 | 32개 잡/클래스와 8개 공통 역할 이미지의 normal/checked 쌍, 파일명·현재 이름·legacy 이름을 구축한다. | [L26](../../src/ListingDiscordEmojiAssets.cs#L26) |
| <a id="symbol-src-listingdiscordemojiassets-cs-55-0df825de25"></a><code>ListingDiscordEmojiAssets.Sync</code><br><code>Task Sync(ListingDiscordIcons icons, Func&lt;CancellationToken, Task&lt;IReadOnlyCollection&lt;Emote&gt;&gt;&gt; list, Func&lt;string, Stream, CancellationToken, Task&lt;Emote&gt;&gt; create, Func&lt;ulong, string, CancellationToken, Task&lt;Emote&gt;&gt; rename, Action&lt;int, int&gt; progress, CancellationToken cancellation)</code> | 메서드 | 먼저 전체 emoji 목록을 읽어 같은 이름은 재사용한다. legacy가 있으면 ID를 유지해 이름을 바꾸고 나머지만 생성한다. 반환 ID/이름을 검증하고 취소/부분 실패를 전달하며 다른 이모지는 삭제하지 않는다. | [L55](../../src/ListingDiscordEmojiAssets.cs#L55) |

<a id="file-src-listingdiscordicons-cs"></a>
## src/ListingDiscordIcons.cs

[소스 파일](../../src/ListingDiscordIcons.cs) · 명명된 항목/진입점 10개

봇 연결별 application emoji ID registry와 슬롯 이미지 선택. 참가 직업으로 제한 mask를 덮지 않고 원래 제한 이미지에 join 상태를 결합한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordicons-cs-7-87bf7b4386"></a><code>ListingDiscordIcons</code><br><code>ListingDiscordIcons(IReadOnlyDictionary&lt;string, ulong&gt;? applicationEmojiIds = null)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L7](../../src/ListingDiscordIcons.cs#L7) |
| <a id="symbol-src-listingdiscordicons-cs-10-8de74a9761"></a><code>ListingDiscordIcons.Count</code><br><code>int Count</code> | 계산 속성 | 현재 registry/카탈로그/버퍼의 항목 수를 반환한다. 계산식: <code>ids.Count</code>. | [L10](../../src/ListingDiscordIcons.cs#L10) |
| <a id="symbol-src-listingdiscordicons-cs-11-4f96a6dad7"></a><code>ListingDiscordIcons.Register</code><br><code>void Register(string key, ulong id)</code> | 메서드 | asset key와 유효한 application emoji ID를 thread-safe registry에 등록한다. | [L11](../../src/ListingDiscordIcons.cs#L11) |
| <a id="symbol-src-listingdiscordicons-cs-12-8fe6db841f"></a><code>ListingDiscordIcons.Private</code><br><code>string Private</code> | 계산 속성 | 비공개 설정용 자물쇠 표시를 반환한다. 계산식: <code>Resolve("private", "🔒")</code>. | [L12](../../src/ListingDiscordIcons.cs#L12) |
| <a id="symbol-src-listingdiscordicons-cs-13-68f9124726"></a><code>ListingDiscordIcons.Beginner</code><br><code>string Beginner</code> | 계산 속성 | 초보자 환영용 새싹 표시를 반환한다. 계산식: <code>Resolve("beginner", "🌱")</code>. | [L13](../../src/ListingDiscordIcons.cs#L13) |
| <a id="symbol-src-listingdiscordicons-cs-15-d767ca817a"></a><code>ListingDiscordIcons.ForSlot</code><br><code>string ForSlot(WebSlot slot)</code> | 메서드 | 허용 직업 조건으로 이미지 key를 정하고 참가한 슬롯이면 checked asset을 사용한다. 이미지가 없으면 점유/제한/누구나 기호로 대체한다. | [L15](../../src/ListingDiscordIcons.cs#L15) |
| <a id="symbol-src-listingdiscordicons-cs-22-dd52e823a5"></a><code>ListingDiscordIcons.ConditionKey</code><br><code>string? ConditionKey(WebSlot slot)</code> | 메서드 | 전체 허용·단일 잡/클래스·T/H/D 조합을 해당 이미지 key로 분류한다. | [L22](../../src/ListingDiscordIcons.cs#L22) |
| <a id="symbol-src-listingdiscordicons-cs-37-d532b0830f"></a><code>ListingDiscordIcons.Role</code><br><code>int Role(uint id)</code> | 메서드 | 잡 및 클래스 ID를 tank/healer/dealer 역할 bit로 변환한다. | [L37](../../src/ListingDiscordIcons.cs#L37) |
| <a id="symbol-src-listingdiscordicons-cs-44-5cc7b949de"></a><code>ListingDiscordIcons.Resolve</code><br><code>string Resolve(string key, string fallback)</code> | 메서드 | 등록된 application emoji를 읽기 쉬운 이름과 ID로 렌더링하거나 전달받은 fallback 기호를 반환한다. | [L44](../../src/ListingDiscordIcons.cs#L44) |
| <a id="symbol-src-listingdiscordicons-cs-49-da4c932a49"></a><code>ListingDiscordIcons.Legend</code><br><code>string Legend(IEnumerable&lt;WebSlot&gt; slots)</code> | 메서드 | 현재 목록에 실제 사용된 fallback 기호만 범례로 표시한다. | [L49](../../src/ListingDiscordIcons.cs#L49) |

<a id="file-src-listingdiscordpanel-forms-cs"></a>
## src/ListingDiscordPanel.Forms.cs

[소스 파일](../../src/ListingDiscordPanel.Forms.cs) · 명명된 항목/진입점 5개

소개말·비공개와 묶음 설정의 modal 구성/파싱. 외부 input은 반드시 현재 editor와 allowlisted field에 묶는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordpanel-forms-cs-8-246bd14a13"></a><code>ListingDiscordPanel.FormFields</code><br><code>string[] FormFields(string form)</code> | 메서드 | modal 종류에 따라 허용되는 필드 ID 목록을 반환한다. | [L8](../../src/ListingDiscordPanel.Forms.cs#L8) |
| <a id="symbol-src-listingdiscordpanel-forms-cs-12-d26ac89940"></a><code>ListingDiscordPanel.FormSupported</code><br><code>bool FormSupported(Panel p, string form)</code> | 메서드 | 현재 snapshot에서 요청 modal에 필요한 필드 편집을 지원하는지 검사한다. | [L12](../../src/ListingDiscordPanel.Forms.cs#L12) |
| <a id="symbol-src-listingdiscordpanel-forms-cs-13-ca4a3e8735"></a><code>ListingDiscordPanel.BuildForm</code><br><code>Modal BuildForm(Panel p, string form)</code> | 메서드 | 소개말 text, 비공개/PIN 또는 목적·완료조건·평균 레벨·임무 규칙 묶음 modal을 현재 초안 값으로 만든다. | [L13](../../src/ListingDiscordPanel.Forms.cs#L13) |
| <a id="symbol-src-listingdiscordpanel-forms-cs-52-bc6fa4ec1c"></a><code>ListingDiscordPanel.ReadForm</code><br><code>bool ReadForm(Panel p, string form, DiscordInput input, out List&lt;JsonObject&gt; changes, out string error)</code> | 메서드 | modal 제출 key·선택 수·enum·UTF-8 길이·아이템 레벨·4자리 PIN을 검증하고 대기 변경만 갱신한다. 잘못된 입력으로 부분 적용하지 않는다. | [L52](../../src/ListingDiscordPanel.Forms.cs#L52) |
| <a id="symbol-src-listingdiscordpanel-forms-cs-97-e430a74410"></a><code>ListingDiscordPanel.Keys</code><br><code>bool Keys&lt;T&gt;(Dictionary&lt;string, T&gt; values, string[] expected)</code> | 메서드 | 실제 modal key 집합이 허용 key와 일치하고 중복/미지 필드가 없는지 검사한다. | [L97](../../src/ListingDiscordPanel.Forms.cs#L97) |

<a id="file-src-listingdiscordpanel-render-cs"></a>
## src/ListingDiscordPanel.Render.cs

[소스 파일](../../src/ListingDiscordPanel.Render.cs) · 명명된 항목/진입점 23개

모바일 조회/수정/상세 카드와 Components V2 공통 builder. 조회에는 전체 그룹 아이콘을 한 번에 표시한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordpanel-render-cs-8-5cb2960b35"></a><code>ListingDiscordPanel.Render</code><br><code>DiscordScreen Render(Panel p, long now, string notice = "")</code> | 메서드 | panel 종류에 따라 조회·편집·직업 상세·결과 UI를 구성하고 갱신된 nonce를 사용한다. 상단 조회/갱신/수정, 임무 앞 🔒/🌱, 활성 설정과 소개말 순서를 관리한다. | [L8](../../src/ListingDiscordPanel.Render.cs#L8) |
| <a id="symbol-src-listingdiscordpanel-render-cs-64-58a7c6252b"></a><code>ListingDiscordPanel.DrawActiveSettings</code><br><code>void DrawActiveSettings(ListingDiscordCard card, WebListing listing)</code> | 메서드 | 켜진 설정만 지정된 두 줄 태그로 표시한다. 비공개/초보자는 임무 앞 아이콘으로 표현한다. | [L64](../../src/ListingDiscordPanel.Render.cs#L64) |
| <a id="symbol-src-listingdiscordpanel-render-cs-66-64066aa6c8"></a><code>ListingDiscordPanel.DrawActiveSettings.Tags</code><br><code>string Tags(params string[] keys)</code> | 로컬 함수 | 활성화된 설정 label들을 대괄호 태그 문자열로 묶는다. | [L66](../../src/ListingDiscordPanel.Render.cs#L66) |
| <a id="symbol-src-listingdiscordpanel-render-cs-80-d356546a55"></a><code>ListingDiscordPanel.Remaining</code><br><code>string Remaining(WebListing listing)</code> | 메서드 | ReadAt와 읽은 잔여초를 고정 종료시각으로 바꿔 Discord 상대시간 timestamp를 만든다. 카드 다시 그리기만으로 시간이 리셋되지 않는다. | [L80](../../src/ListingDiscordPanel.Render.cs#L80) |
| <a id="symbol-src-listingdiscordpanel-render-cs-89-53467d99b8"></a><code>ListingDiscordPanel.DrawJobDetails</code><br><code>void DrawJobDetails(ListingDiscordCard card, Panel p)</code> | 메서드 | 선택 그룹 슬롯의 허용 직업 텍스트와 참가 여부를 읽기 전용 상세 카드로 표시한다. | [L89](../../src/ListingDiscordPanel.Render.cs#L89) |
| <a id="symbol-src-listingdiscordpanel-render-cs-103-df8a41a9f7"></a><code>ListingDiscordPanel.Button</code><br><code>ButtonBuilder Button(Panel p, string action, string label, ButtonStyle style = ButtonStyle.Secondary, bool disabled = false)</code> | 메서드 | 동작 ID·label·style·비활성 상태를 가진 버튼을 구성한다. | [L103](../../src/ListingDiscordPanel.Render.cs#L103) |
| <a id="symbol-src-listingdiscordpanel-render-cs-106-e904dd448f"></a><code>ListingDiscordPanel.CloseButton</code><br><code>ButtonBuilder CloseButton(string session, bool disabled = false)</code> | 메서드 | session에 연결한 수정창 닫기 버튼을 만든다. overview nonce/편집 revision 만료와 무관하게 현재 session에서 복구할 수 있다. | [L106](../../src/ListingDiscordPanel.Render.cs#L106) |
| <a id="symbol-src-listingdiscordpanel-render-cs-108-a924f6519d"></a><code>ListingDiscordPanel.RecoveryMessage</code><br><code>DiscordScreen RecoveryMessage(string message, string session)</code> | 메서드 | 조회·갱신 진행/다른 요청 busy 화면에 닫기 버튼을 유지하는 V2 카드를 만든다. | [L108](../../src/ListingDiscordPanel.Render.cs#L108) |
| <a id="symbol-src-listingdiscordpanel-render-cs-113-cae1616b63"></a><code>ListingDiscordPanel.GroupMenu</code><br><code>void GroupMenu(ListingDiscordCard card, Panel p)</code> | 메서드 | 24/48인/친선 그룹 선택 메뉴를 만든다. 조회 overview의 전체 그룹 표시는 이 선택에 제한되지 않는다. | [L113](../../src/ListingDiscordPanel.Render.cs#L113) |
| <a id="symbol-src-listingdiscordpanel-render-cs-120-78c6805e8e"></a><code>ListingDiscordPanel.Setting</code><br><code>string Setting(Panel p, string key)</code> | 메서드 | 필드별 원래 값/대기 값을 편집 목록 한 항목으로 표시한다. | [L120](../../src/ListingDiscordPanel.Render.cs#L120) |
| <a id="symbol-src-listingdiscordpanel-render-cs-122-5a18060bf9"></a><code>ListingDiscordPanel.Pending</code><br><code>void Pending(ListingDiscordCard card, Panel p)</code> | 메서드 | 해당 필드가 원본과 다른 대기 변경인지 표시한다. | [L122](../../src/ListingDiscordPanel.Render.cs#L122) |
| <a id="symbol-src-listingdiscordpanel-render-cs-129-ac45f81ee5"></a><code>ListingDiscordPanel.DrawSettings</code><br><code>void DrawSettings(ListingDiscordCard card, Panel p)</code> | 메서드 | 목적·모집 참가 조건·임무 찾기·모집 범위·모집 역할 순으로 설정 편집 카드를 그린다. | [L129](../../src/ListingDiscordPanel.Render.cs#L129) |
| <a id="symbol-src-listingdiscordpanel-render-cs-153-160d0514c2"></a><code>ListingDiscordPanel.DrawSettings.SettingsSection</code><br><code>void SettingsSection(string title, params string[] keys)</code> | 로컬 함수 | 설정 섹션 제목/본문/수정 버튼을 공통 형식으로 추가한다. | [L153](../../src/ListingDiscordPanel.Render.cs#L153) |
| <a id="symbol-src-listingdiscordpanel-render-cs-160-37e42a72da"></a><code>ListingDiscordPanel.DrawJobs</code><br><code>void DrawJobs(ListingDiscordCard card, Panel p)</code> | 메서드 | 자리와 직업 분류별 선택 메뉴 및 방어·회복·공격 preset을 그린다. | [L160](../../src/ListingDiscordPanel.Render.cs#L160) |
| <a id="symbol-src-listingdiscordpanel-render-cs-189-c008a6adfd"></a><code>ListingDiscordCard</code><br><code>ListingDiscordCard(Color color)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L189](../../src/ListingDiscordPanel.Render.cs#L189) |
| <a id="symbol-src-listingdiscordpanel-render-cs-194-7c5d704f3f"></a><code>ListingDiscordCard.Display</code><br><code>TextDisplayBuilder Display(string value)</code> | 메서드 | 한 카드에 누적된 표시 길이를 확인한다. | [L194](../../src/ListingDiscordPanel.Render.cs#L194) |
| <a id="symbol-src-listingdiscordpanel-render-cs-199-1ade6dbe6d"></a><code>ListingDiscordCard.Text</code><br><code>void Text(string value)</code> | 메서드 | 남은 카드 표시 예산 안에서 TextDisplay component를 추가한다. | [L199](../../src/ListingDiscordPanel.Render.cs#L199) |
| <a id="symbol-src-listingdiscordpanel-render-cs-200-d095288fcf"></a><code>ListingDiscordCard.Section</code><br><code>void Section(string value, ButtonBuilder button)</code> | 메서드 | 텍스트와 보조 버튼 accessory를 가진 Section component를 추가한다. | [L200](../../src/ListingDiscordPanel.Render.cs#L200) |
| <a id="symbol-src-listingdiscordpanel-render-cs-201-53ad399484"></a><code>ListingDiscordCard.Divider</code><br><code>void Divider()</code> | 메서드 | 시각 구분용 separator를 넣는다. | [L201](../../src/ListingDiscordPanel.Render.cs#L201) |
| <a id="symbol-src-listingdiscordpanel-render-cs-202-a7803d0ad3"></a><code>ListingDiscordCard.Row</code><br><code>void Row(params IMessageComponentBuilder[] controls)</code> | 메서드 | 버튼/select 등을 ActionRow에 넣는다. | [L202](../../src/ListingDiscordPanel.Render.cs#L202) |
| <a id="symbol-src-listingdiscordpanel-render-cs-203-3c477d479d"></a><code>ListingDiscordCard.Build</code><br><code>DiscordScreen Build()</code> | 메서드 | 모아둔 component를 Container로 완성한다. | [L203](../../src/ListingDiscordPanel.Render.cs#L203) |
| <a id="symbol-src-listingdiscordpanel-render-cs-204-17c2e4acfc"></a><code>ListingDiscordCard.Message</code><br><code>DiscordScreen Message(string value)</code> | 메서드 | 간단한 텍스트 응답을 Components V2 메시지로 만든다. | [L204](../../src/ListingDiscordPanel.Render.cs#L204) |
| <a id="symbol-src-listingdiscordpanel-render-cs-205-aef8c2cdfb"></a><code>ListingDiscordCard.Walk</code><br><code>IEnumerable&lt;IMessageComponent&gt; Walk(IEnumerable&lt;IMessageComponent&gt; components)</code> | 메서드 | 중첩 Container/Section/Row component를 재귀 순회한다. 컨트롤 존재 여부와 테스트 검사에 사용한다. | [L205](../../src/ListingDiscordPanel.Render.cs#L205) |

<a id="file-src-listingdiscordpanel-results-cs"></a>
## src/ListingDiscordPanel.Results.cs

[소스 파일](../../src/ListingDiscordPanel.Results.cs) · 명명된 항목/진입점 3개

긴 최근 작업 결과를 생략하지 않고 독립된 읽기 전용 페이지 카드로 표시한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordpanel-results-cs-11-5247f8b51c"></a><code>ListingDiscordPanel.PaginateResult</code><br><code>string[] PaginateResult(string value)</code> | 메서드 | escape된 결과를 페이지 예산 3200에 맞춰 분할한다. 모든 문자를 보존하고 줄바꿈·Unicode rune·escape 조각을 가능한 한 함께 둔다. | [L11](../../src/ListingDiscordPanel.Results.cs#L11) |
| <a id="symbol-src-listingdiscordpanel-results-cs-16-5ec90519e1"></a><code>ListingDiscordPanel.PaginateResult.Flush</code><br><code>void Flush()</code> | 로컬 함수 | 현재 결과 페이지 버퍼를 목록에 추가하고 다음 페이지용 버퍼를 비운다. | [L16](../../src/ListingDiscordPanel.Results.cs#L16) |
| <a id="symbol-src-listingdiscordpanel-results-cs-36-65787a8ea4"></a><code>ListingDiscordPanel.DrawResults</code><br><code>DiscordScreen DrawResults(ListingDiscordCard card, Panel p)</code> | 메서드 | 현재 결과 페이지와 이전/다음 버튼을 그린다. 게임을 변경하는 버튼은 넣지 않는다. | [L36](../../src/ListingDiscordPanel.Results.cs#L36) |

<a id="file-src-listingdiscordpanel-cs"></a>
## src/ListingDiscordPanel.cs

[소스 파일](../../src/ListingDiscordPanel.cs) · 명명된 항목/진입점 33개

SDK와 게임 메모리에서 분리된 카드/session 상태 모델. Overview·Editor·Details·Results에 nonce와 원본 revision을 묶어 오래된 버튼/폼을 거부한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordpanel-cs-9-344fdc2d8a"></a><code>DiscordInput</code><br><code>DiscordInput(ulong Id, ulong ApplicationId, ulong UserId, string Command, string CustomId, string[] Values, Dictionary&lt;string, string&gt; Fields)</code> | 주 생성자/모델 | SDK interaction을 종류·ID·owner/application·custom ID·선택/폼 값의 관리형 입력으로 변환한 모델이다. | [L9](../../src/ListingDiscordPanel.cs#L9) |
| <a id="symbol-src-listingdiscordpanel-cs-15-0f259e7f13"></a><code>DiscordWork</code><br><code>DiscordWork(bool Query, string Id, string Session, byte[]? Body, long RecoveryRevision = 0)</code> | 주 생성자/모델 | 조회 또는 apply 작업의 요청 ID·session·body 등 실행 대상을 묶는다. | [L15](../../src/ListingDiscordPanel.cs#L15) |
| <a id="symbol-src-listingdiscordpanel-cs-16-71f21b6a30"></a><code>DiscordScreen</code><br><code>DiscordScreen(string Text, MessageComponent Components)</code> | 주 생성자/모델 | 텍스트와 Components V2 컴포넌트를 함께 보관한다. HasControls는 버튼/select 존재를 판정하며, 메시지 교체 여부는 별도 DiscordDecision에 있다. | [L16](../../src/ListingDiscordPanel.cs#L16) |
| <a id="symbol-src-listingdiscordpanel-cs-18-f2855686b2"></a><code>DiscordScreen.HasControls</code><br><code>bool HasControls</code> | 계산 속성 | 중첩 카드 component에 사용자 조작 컨트롤이 있는지 검사한다. 계산식: <code>ListingDiscordCard.Walk(Components.Components).Any(c =&gt; c is ButtonComponent or SelectMenuComponent)</code>. | [L18](../../src/ListingDiscordPanel.cs#L18) |
| <a id="symbol-src-listingdiscordpanel-cs-20-f5e78d0fb3"></a><code>DiscordDecision</code><br><code>DiscordDecision(DiscordScreen? Screen = null, Modal? Modal = null, DiscordWork? Work = null, bool Replace = true)</code> | 주 생성자/모델 | 입력을 처리한 결과로 화면/modal/지연 게임 작업 중 무엇을 수행할지 전달한다. | [L20](../../src/ListingDiscordPanel.cs#L20) |
| <a id="symbol-src-listingdiscordpanel-cs-25-920703d2ff"></a><code>ListingDiscordPanel</code><br><code>ListingDiscordPanel(ListingWebBridge bridge, ulong applicationId, ulong ownerId, ListingDiscordIcons? iconCatalog = null)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L25](../../src/ListingDiscordPanel.cs#L25) |
| <a id="symbol-src-listingdiscordpanel-cs-32-43104016ab"></a><code>ListingDiscordPanel.Panel</code><br><code>Panel(WebView view, PanelKind kind = PanelKind.Overview)</code> | 주 생성자/모델 | 조회/수정/상세/결과 카드의 nonce·만료·원본·초안·선택 그룹/자리·결과 페이지를 분리해 보관한다. | [L32](../../src/ListingDiscordPanel.cs#L32) |
| <a id="symbol-src-listingdiscordpanel-cs-36-634d8c1f0b"></a><code>ListingDiscordPanel.Panel.Editing</code><br><code>bool Editing</code> | 계산 속성 | 현재 편집 원본/초안 상태가 존재하는지 검사한다. 계산식: <code>Kind == PanelKind.Editor</code>. | [L36](../../src/ListingDiscordPanel.cs#L36) |
| <a id="symbol-src-listingdiscordpanel-cs-57-4739709459"></a><code>ListingDiscordPanel.Authorized</code><br><code>bool Authorized(DiscordInput input)</code> | 메서드 | interaction user ID와 application ID가 이 개인 봇의 허용 owner/application과 같은지 확인한다. | [L57](../../src/ListingDiscordPanel.cs#L57) |
| <a id="symbol-src-listingdiscordpanel-cs-58-37b4741431"></a><code>ListingDiscordPanel.RequestId</code><br><code>string RequestId(ulong id)</code> | 메서드 | Discord interaction ID를 SHA-256 기반 deterministic GUID로 변환해 재처리 시 동일한 Bridge 요청 ID를 사용한다. | [L58](../../src/ListingDiscordPanel.cs#L58) |
| <a id="symbol-src-listingdiscordpanel-cs-59-9c3b3c0ed7"></a><code>ListingDiscordPanel.Error</code><br><code>DiscordDecision Error(string message)</code> | 메서드 | 오류 메시지 전용 화면을 Replace=false 결정으로 반환해 기존 카드를 덮어쓰지 않는다. 현재 패널의 재렌더링은 하지 않는다. | [L59](../../src/ListingDiscordPanel.cs#L59) |
| <a id="symbol-src-listingdiscordpanel-cs-60-6fabe38738"></a><code>ListingDiscordPanel.Current</code><br><code>bool Current(Panel p, WebView view, long now)</code> | 메서드 | 카드의 만료 시각·session·editor revision을 현재 View와 비교한다. custom ID의 nonce 검사는 Handle에서 별도로 수행한다. | [L60](../../src/ListingDiscordPanel.cs#L60) |
| <a id="symbol-src-listingdiscordpanel-cs-63-d8bb69c1a4"></a><code>ListingDiscordPanel.Handle</code><br><code>DiscordDecision Handle(DiscordInput input, long now)</code> | 메서드 | 조회·갱신·새 수정 카드·직업 상세·결과 페이지·설정 modal·그룹/자리/직업 선택을 라우팅한다. 적용 시 변경을 strict JSON body로 만들고 현재 초안을 소비한다. | [L63](../../src/ListingDiscordPanel.cs#L63) |
| <a id="symbol-src-listingdiscordpanel-cs-164-3a675f067f"></a><code>ListingDiscordPanel.Start</code><br><code>WebReply Start(DiscordWork work, long now)</code> | 메서드 | DiscordWork를 공통 query/apply queue에 전달하며 캡처한 recovery revision을 같은 lock에서 검사하도록 한다. | [L164](../../src/ListingDiscordPanel.cs#L164) |
| <a id="symbol-src-listingdiscordpanel-cs-165-9ce475bf7d"></a><code>ListingDiscordPanel.Progress</code><br><code>WebReply Progress(DiscordWork work)</code> | 메서드 | 앞서 발행한 요청 ID의 query/operation 결과를 읽는다. 명령을 재발송하지 않는다. | [L165](../../src/ListingDiscordPanel.cs#L165) |
| <a id="symbol-src-listingdiscordpanel-cs-166-5a687cdb28"></a><code>ListingDiscordPanel.Complete</code><br><code>DiscordDecision Complete(long now, string notice = "")</code> | 메서드 | 작업 결과를 panel과 조회 카드에 반영하고 전체 결과 텍스트를 별도 페이지로 보관한다. | [L166](../../src/ListingDiscordPanel.cs#L166) |
| <a id="symbol-src-listingdiscordpanel-cs-183-5f01e878db"></a><code>ListingDiscordPanel.Clear</code><br><code>void Clear()</code> | 메서드 | 편집 원본·nonce·선택·PIN 포함 초안·최근 결과 상태를 지운다. | [L183](../../src/ListingDiscordPanel.cs#L183) |
| <a id="symbol-src-listingdiscordpanel-cs-184-3d5893efd3"></a><code>ListingDiscordPanel.One</code><br><code>bool One(DiscordInput input, out string value)</code> | 메서드 | 단일 select 값이 정확히 하나인지 검사해 읽는다. | [L184](../../src/ListingDiscordPanel.cs#L184) |
| <a id="symbol-src-listingdiscordpanel-cs-185-da45c7e72a"></a><code>ListingDiscordPanel.Original</code><br><code>JsonNode? Original(Panel p, string key)</code> | 메서드 | 선택 필드의 원래 값을 editor snapshot에서 가져온다. | [L185](../../src/ListingDiscordPanel.cs#L185) |
| <a id="symbol-src-listingdiscordpanel-cs-186-ae8f72805e"></a><code>ListingDiscordPanel.Value</code><br><code>JsonNode? Value(Panel p, string key)</code> | 메서드 | 대기 편집이 있으면 그 값을, 없으면 원래 값을 반환한다. | [L186](../../src/ListingDiscordPanel.cs#L186) |
| <a id="symbol-src-listingdiscordpanel-cs-187-551ca9a2a5"></a><code>ListingDiscordPanel.Enabled</code><br><code>bool Enabled(Panel p, string key)</code> | 메서드 | 설정 값을 bool 표시 상태로 해석한다. | [L187](../../src/ListingDiscordPanel.cs#L187) |
| <a id="symbol-src-listingdiscordpanel-cs-188-8114314782"></a><code>ListingDiscordPanel.FlagChange</code><br><code>JsonObject FlagChange(string key, bool enabled)</code> | 메서드 | 토글을 field/value JSON으로 변환한다. ConditionFlags는 bool, 그 외 flag는 정수0/1로 인코딩한다. 원래 값과 비교해 대기 변경을 제거하는 책임은 Set에 있다. | [L188](../../src/ListingDiscordPanel.cs#L188) |
| <a id="symbol-src-listingdiscordpanel-cs-189-6aaae98e0d"></a><code>ListingDiscordPanel.Set</code><br><code>void Set(Panel p, string key, JsonObject change)</code> | 메서드 | 원래 값과 같으면 대기 변경을 제거하고 다르면 해당 field key만 교체한다. | [L189](../../src/ListingDiscordPanel.cs#L189) |
| <a id="symbol-src-listingdiscordpanel-cs-196-12e92ddbe2"></a><code>ListingDiscordPanel.GroupSlots</code><br><code>WebEditSlot[] GroupSlots(Panel p)</code> | 메서드 | 현재 선택한 물리 그룹의 등록된 slot만 반환한다. | [L196](../../src/ListingDiscordPanel.cs#L196) |
| <a id="symbol-src-listingdiscordpanel-cs-203-106ef40547"></a><code>ListingDiscordPanel.SelectedJobs</code><br><code>HashSet&lt;uint&gt; SelectedJobs(Panel p)</code> | 메서드 | 대기 직업 변경 또는 원래 슬롯에서 현재 선택된 직업 ID 집합을 얻는다. | [L203](../../src/ListingDiscordPanel.cs#L203) |
| <a id="symbol-src-listingdiscordpanel-cs-207-754bbf3471"></a><code>ListingDiscordPanel.JobGroup</code><br><code>int JobGroup(uint id)</code> | 메서드 | 잡과 클래스를 방어·회복·근거리·원거리 물리·원거리 마법의 5개 선택 그룹에 대응시킨다. | [L207](../../src/ListingDiscordPanel.cs#L207) |
| <a id="symbol-src-listingdiscordpanel-cs-215-60959f1106"></a><code>ListingDiscordPanel.Preset</code><br><code>bool Preset(string name, uint id)</code> | 메서드 | 방어/회복/공격 등의 일괄 선택을 잡과 클래스 모두에 적용한다. | [L215](../../src/ListingDiscordPanel.cs#L215) |
| <a id="symbol-src-listingdiscordpanel-cs-219-5fcd475ae4"></a><code>ListingDiscordPanel.Id</code><br><code>string Id(Panel p, string action)</code> | 메서드 | panel nonce/동작을 포함한 component custom ID를 구성한다. | [L219](../../src/ListingDiscordPanel.cs#L219) |
| <a id="symbol-src-listingdiscordpanel-cs-220-68f259d15d"></a><code>ListingDiscordPanel.Label</code><br><code>string Label(string key)</code> | 메서드 | 필드 ID를 한국어 편집 항목명으로 바꾼다. | [L220](../../src/ListingDiscordPanel.cs#L220) |
| <a id="symbol-src-listingdiscordpanel-cs-221-f073cccaa2"></a><code>ListingDiscordPanel.DisplayValue</code><br><code>string DisplayValue(Panel p, string key, bool original = false)</code> | 메서드 | 필드별 숫자/토글/규칙을 사용자용 값으로 표시한다. 비공개 PIN은 드러내지 않는다. | [L221](../../src/ListingDiscordPanel.cs#L221) |
| <a id="symbol-src-listingdiscordpanel-cs-230-781ed2649d"></a><code>ListingDiscordPanel.Options</code><br><code>(string Value, string Label)[] Options(string key)</code> | 메서드 | 허용된 label/value 목록을 Discord select option으로 구성한다. | [L230](../../src/ListingDiscordPanel.cs#L230) |
| <a id="symbol-src-listingdiscordpanel-cs-236-cec221d941"></a><code>ListingDiscordPanel.Escape</code><br><code>string Escape(string value)</code> | 메서드 | Discord Markdown에 영향을 주는 문자를 escape한다. | [L236](../../src/ListingDiscordPanel.cs#L236) |
| <a id="symbol-src-listingdiscordpanel-cs-242-646e4ed6b4"></a><code>ListingDiscordPanel.Limit</code><br><code>string Limit(string text, int max = 1900)</code> | 메서드 | 최대 길이에 맞춰 문자열을 줄이되 Unicode surrogate/rune 경계를 보존한다. 긴 작업 결과 본문은 별도 PaginateResult 경로를 사용한다. | [L242](../../src/ListingDiscordPanel.cs#L242) |

<a id="file-src-listingdiscordsession-recovery-cs"></a>
## src/ListingDiscordSession.Recovery.cs

[소스 파일](../../src/ListingDiscordSession.Recovery.cs) · 명명된 항목/진입점 2개

일반 interaction의 handling 잠금과 별도로 닫기 interaction을 처리한다. 개인 봇 owner/app 인증은 Process에서 먼저 수행하고 session·ACK를 추가 검증한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordsession-recovery-cs-8-89236b771f"></a><code>ListingDiscordSession.Remember</code><br><code>bool Remember(ulong id)</code> | 메서드 | 일반/닫기 경로가 공유하는 interaction ID 중복 방지 집합을 lock으로 보호하며 최근1024개를 유지한다. | [L8](../../src/ListingDiscordSession.Recovery.cs#L8) |
| <a id="symbol-src-listingdiscordsession-recovery-cs-19-0838803c38"></a><code>ListingDiscordSession.CloseEditor</code><br><code>Task CloseEditor(DiscordInput input, IDiscordResponse response)</code> | 메서드 | 독립적인 닫기 처리 잠금으로 busy 중에도 진입한다. session 검사와 ACK 성공 후에만 공통 queue에 넣고 닫힘 결과를 기다린다. 중복/ACK 실패/취소는 새 게임 조작을 시작하지 않는다. | [L19](../../src/ListingDiscordSession.Recovery.cs#L19) |

<a id="file-src-listingdiscordsession-cs"></a>
## src/ListingDiscordSession.cs

[소스 파일](../../src/ListingDiscordSession.cs) · 명명된 항목/진입점 12개

한 사용자 interaction의 인증·ACK·게임 요청·결과 폴링을 직렬화한다. ACK 실패 뒤 게임 작업을 실행하지 않는 것이 핵심이다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordsession-cs-9-6464736319"></a><code>IDiscordResponse.Defer</code><br><code>Task Defer()</code> | 인터페이스 계약 | 인터페이스 계약. 게임 작업 전 interaction을 ACK한다. slash는 ephemeral deferred response를 사용한다. | [L9](../../src/ListingDiscordSession.cs#L9) |
| <a id="symbol-src-listingdiscordsession-cs-10-bb5124a51d"></a><code>IDiscordResponse.Modal</code><br><code>Task Modal(Modal modal)</code> | 인터페이스 계약 | 인터페이스 계약. 즉시 modal을 최초 응답으로 보낸다. 이미 defer한 interaction에 modal을 다시 열지 않는다. | [L10](../../src/ListingDiscordSession.cs#L10) |
| <a id="symbol-src-listingdiscordsession-cs-11-75433d4962"></a><code>IDiscordResponse.Screen</code><br><code>Task Screen(DiscordScreen screen, bool replace, bool deferred)</code> | 인터페이스 계약 | 인터페이스 계약. 새 카드/기존 카드 업데이트/후속 응답을 맞는 SDK 경로로 전달한다. Components V2를 쓰고 기존 content/embeds를 비우며 AllowedMentions.None을 적용한다. | [L11](../../src/ListingDiscordSession.cs#L11) |
| <a id="symbol-src-listingdiscordsession-cs-16-576bccc2fe"></a><code>ListingDiscordSession</code><br><code>ListingDiscordSession(ListingWebBridge bridge, ulong appId, ulong ownerId, CancellationToken cancellation, Func&lt;long&gt;? clock = null, ListingDiscordIcons? iconCatalog = null)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L16](../../src/ListingDiscordSession.cs#L16) |
| <a id="symbol-src-listingdiscordsession-cs-23-48a3409a6b"></a><code>ListingDiscordSession.Now</code><br><code>long Now</code> | 계산 속성 | 단조 시계 Environment.TickCount64를 반환해 UI session 만료/작업 대기를 계산한다. 계산식: <code>clock?.Invoke() ?? Environment.TickCount64</code>. | [L23](../../src/ListingDiscordSession.cs#L23) |
| <a id="symbol-src-listingdiscordsession-cs-24-6071ec2a62"></a><code>ListingDiscordSession.Message</code><br><code>DiscordScreen Message(string text)</code> | 메서드 | 단순 메시지를 DiscordScreen 형태로 만든다. | [L24](../../src/ListingDiscordSession.cs#L24) |
| <a id="symbol-src-listingdiscordsession-cs-26-a5110dd151"></a><code>ListingDiscordSession.Process</code><br><code>Task Process(DiscordInput input, IDiscordResponse response)</code> | 메서드 | owner/app 확인 후 닫기 요청은 독립 경로로 보낸다. 일반 작업은 단일 handling 및 공통 중복 방지/ACK를 거쳐 queue에 넣고 결과를 기다린다. 진행 화면에 닫기를 유지한다. | [L26](../../src/ListingDiscordSession.cs#L26) |
| <a id="symbol-src-listingdiscordsession-cs-86-74d8262ab3"></a><code>ListingDiscordSession.ClearIfIdle</code><br><code>void ClearIfIdle()</code> | 메서드 | 진행 중 interaction이 없을 때 panel 상태를 비우고, 진행 중이면 안전한 정리를 예약한다. | [L86](../../src/ListingDiscordSession.cs#L86) |
| <a id="symbol-src-listingdiscordsession-cs-91-012c33ae35"></a><code>ListingDiscordSession.SafeScreen</code><br><code>Task SafeScreen(IDiscordResponse response, DiscordScreen screen, bool deferred = false)</code> | 메서드 | 응답 갱신 실패가 상위 처리 흐름을 깨지 않도록 best-effort로 화면을 보낸다. | [L91](../../src/ListingDiscordSession.cs#L91) |
| <a id="symbol-src-listingdiscordsession-cs-96-3b77121bb9"></a><code>ListingDiscordSession.Pending</code><br><code>bool Pending(WebReply reply)</code> | 메서드 | query·apply·recovery 결과가 queued/running인지 판정한다. | [L96](../../src/ListingDiscordSession.cs#L96) |
| <a id="symbol-src-listingdiscordsession-cs-97-a541db3b4d"></a><code>ListingDiscordSession.ResultSummary</code><br><code>string ResultSummary(WebOperation operation)</code> | 메서드 | 조회/갱신 응답의 최종 요약을 카드에 사용할 문자열로 변환한다. | [L97](../../src/ListingDiscordSession.cs#L97) |
| <a id="symbol-src-listingdiscordsession-cs-99-b3229b3705"></a><code>ListingDiscordSession.Describe</code><br><code>string Describe(WebReply reply)</code> | 메서드 | 작업 상태와 항목별 적용/실패 결과를 사용자에게 설명한다. 긴 결과는 생략하지 않고 별도 결과 카드 경로로 넘긴다. | [L99](../../src/ListingDiscordSession.cs#L99) |

<a id="file-src-plugin-discord-cs"></a>
## src/Plugin.Discord.cs

[소스 파일](../../src/Plugin.Discord.cs) · 명명된 항목/진입점 13개

사용자 설정과 Discord client 수명 연결. 재시작 복원은 한 번, 명시 끄기는 자동 연결도 끄며 Dispose는 저장 토큰을 지우지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-discord-cs-11-c18d138fb6"></a><code>Plugin.DiscordEnabled</code><br><code>bool DiscordEnabled</code> | 계산 속성 | Discord client가 실행 중인지 반환한다. 계산식: <code>discordClient?.Running == true</code>. | [L11](../../src/Plugin.Discord.cs#L11) |
| <a id="symbol-src-plugin-discord-cs-12-a8c0c2f707"></a><code>Plugin.DiscordStatus</code><br><code>string DiscordStatus</code> | 계산 속성 | 봇 연결/이모지 진행/오류 상태를 반환한다. 계산식: <code>discordClient?.Status ?? discordStatus</code>. | [L12](../../src/Plugin.Discord.cs#L12) |
| <a id="symbol-src-plugin-discord-cs-13-5a88c06a1c"></a><code>Plugin.DiscordInviteUrl</code><br><code>string DiscordInviteUrl</code> | 계산 속성 | 현재 봇 설치 링크를 반환한다. 계산식: <code>discordClient?.InviteUrl ?? ""</code>. | [L13](../../src/Plugin.Discord.cs#L13) |
| <a id="symbol-src-plugin-discord-cs-14-1a8f5c5c2d"></a><code>Plugin.HasSavedDiscordToken</code><br><code>bool HasSavedDiscordToken</code> | 계산 속성 | 저장 ciphertext가 존재하는지 반환한다. 복호화 가능 여부를 미리 보장하지 않는다. 계산식: <code>(PluginInterface.GetPluginConfig() as RelayConfiguration)?.HasSavedDiscordToken == true</code>. | [L14](../../src/Plugin.Discord.cs#L14) |
| <a id="symbol-src-plugin-discord-cs-15-e855cb3794"></a><code>Plugin.RememberDiscordToken</code><br><code>bool RememberDiscordToken</code> | 계산 속성 | 토큰 기억 설정을 읽거나 변경한다. 계산식: <code>(PluginInterface.GetPluginConfig() as RelayConfiguration)?.RememberDiscordToken ?? true</code>. | [L15](../../src/Plugin.Discord.cs#L15) |
| <a id="symbol-src-plugin-discord-cs-17-d80adaa1f5"></a><code>Plugin.RestoreDiscordConnection</code><br><code>void RestoreDiscordConnection()</code> | 메서드 | 최초 update에서 기억/자동 연결 옵션과 저장 ciphertext가 있으면 복호화 후 StartDiscord를 요청한다. 실패를 매 프레임 재시도하지 않는다. | [L17](../../src/Plugin.Discord.cs#L17) |
| <a id="symbol-src-plugin-discord-cs-29-96e80831cc"></a><code>Plugin.StartDiscord</code><br><code>bool StartDiscord(string token, bool remember)</code> | 메서드 | 입력/저장 토큰을 검증하고 저장 요청을 먼저 준비한다. 다른 외부 transport를 정리한 뒤 새 Bridge/client를 생성한다. | [L29](../../src/Plugin.Discord.cs#L29) |
| <a id="symbol-src-plugin-discord-cs-59-bb4159311b"></a><code>Plugin.ProtectDiscordToken</code><br><code>string ProtectDiscordToken(string token)</code> | 메서드 | Windows CurrentUser DPAPI로 토큰을 보호해 저장 가능한 문자열로 만든다. | [L59](../../src/Plugin.Discord.cs#L59) |
| <a id="symbol-src-plugin-discord-cs-65-bbff6254d1"></a><code>Plugin.UnprotectDiscordToken</code><br><code>string UnprotectDiscordToken(string encrypted)</code> | 메서드 | 저장 ciphertext를 같은 Windows 사용자 범위로 복호화한다. 다른 계정/손상 데이터는 실패 처리한다. | [L65](../../src/Plugin.Discord.cs#L65) |
| <a id="symbol-src-plugin-discord-cs-71-da76f28a59"></a><code>Plugin.DisableDiscordAutoConnect</code><br><code>bool DisableDiscordAutoConnect()</code> | 메서드 | 명시 다른 연결 방식 선택 시 DiscordAutoConnect=false를 저장한다. 저장 실패를 호출자에게 전달한다. | [L71](../../src/Plugin.Discord.cs#L71) |
| <a id="symbol-src-plugin-discord-cs-84-67ea0b0774"></a><code>Plugin.TurnOffDiscord</code><br><code>void TurnOffDiscord()</code> | 메서드 | 자동 재연결 설정을 끄고 현재 Discord 연결을 종료한다. | [L84](../../src/Plugin.Discord.cs#L84) |
| <a id="symbol-src-plugin-discord-cs-89-6caaa19d8b"></a><code>Plugin.ForgetDiscordToken</code><br><code>void ForgetDiscordToken()</code> | 메서드 | 설정의 암호화 토큰과 자동 연결 플래그를 제거한다. 현재 살아 있는 연결과 저장 삭제의 의미를 구분한다. | [L89](../../src/Plugin.Discord.cs#L89) |
| <a id="symbol-src-plugin-discord-cs-98-10efa54052"></a><code>Plugin.StopDiscord</code><br><code>void StopDiscord()</code> | 메서드 | 현재 client를 교체/종료하고 그 연결이 소유한 조회·갱신만 취소한다. 수명 종료 중 토큰 기억 설정은 보존한다. | [L98](../../src/Plugin.Discord.cs#L98) |

<a id="file-src-relayconfiguration-cs"></a>
## src/RelayConfiguration.cs

[소스 파일](../../src/RelayConfiguration.cs) · 명명된 항목/진입점 6개

기존 설정 type identity를 유지하는 Version=1 설정 모델. 서버 주소, DPAPI ciphertext, 토큰 기억/자동 연결만 저장한다. PIN/모집글/relay 연결 키는 저장하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-relayconfiguration-cs-18-bd75014676"></a><code>RelayConfiguration.ReadAfkSettings</code><br><code>AfkSettings ReadAfkSettings()</code> | 메서드 | 저장된 네 AFK 옵션을 정규화한 불변 정책으로 읽는다. 이전 JSON의 누락 값은 기본값을 사용한다. | [L18](../../src/RelayConfiguration.cs#L18) |
| <a id="symbol-src-relayconfiguration-cs-20-d15633c41f"></a><code>RelayConfiguration.WriteAfkSettings</code><br><code>void WriteAfkSettings(AfkSettings settings)</code> | 메서드 | 정규화한 주기·재시도 옵션만 설정 객체에 복사한다. 토큰·연결 설정은 보존하고 저장 자체는 Plugin이 수행한다. | [L20](../../src/RelayConfiguration.cs#L20) |
| <a id="symbol-src-relayconfiguration-cs-26-11ad051cee"></a><code>RelayConfiguration.HasSavedDiscordToken</code><br><code>bool HasSavedDiscordToken</code> | 계산 속성 | 저장 ciphertext가 존재하는지 반환한다. 복호화 가능 여부를 미리 보장하지 않는다. 계산식: <code>!string.IsNullOrEmpty(EncryptedDiscordToken)</code>. | [L26](../../src/RelayConfiguration.cs#L26) |
| <a id="symbol-src-relayconfiguration-cs-27-d1c504e468"></a><code>RelayConfiguration.ShouldAutoConnectDiscord</code><br><code>bool ShouldAutoConnectDiscord</code> | 계산 속성 | DiscordAutoConnect와 저장 ciphertext 존재 여부를 합쳐 시작 시 연결 복원 대상인지 판정한다. PrepareDiscordConnection이 기억 옵션과 자동 연결을 함께 저장한다. 계산식: <code>DiscordAutoConnect &amp;&amp; HasSavedDiscordToken</code>. | [L27](../../src/RelayConfiguration.cs#L27) |
| <a id="symbol-src-relayconfiguration-cs-29-1d8f49a175"></a><code>RelayConfiguration.PrepareDiscordConnection</code><br><code>bool PrepareDiscordConnection(string input, bool remember, Func&lt;string, string&gt; protect, Func&lt;string, string&gt; unprotect, out string token)</code> | 메서드 | 명시 token 또는 saved token 경로를 검증하고 protect/unprotect를 통해 새 설정을 준비한다. 암호화 실패 전에 기존 저장 설정을 변경하지 않는다. | [L29](../../src/RelayConfiguration.cs#L29) |
| <a id="symbol-src-relayconfiguration-cs-45-d6b35a2840"></a><code>RelayConfiguration.ForgetDiscordToken</code><br><code>void ForgetDiscordToken()</code> | 메서드 | EncryptedDiscordToken을 비우고 자동 연결을 끈다. | [L45](../../src/RelayConfiguration.cs#L45) |
