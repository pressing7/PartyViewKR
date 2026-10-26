# Discord 연결·카드·폼·이모지·토큰·채팅

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

개인 봇 owner만 조작할 수 있다. interaction ACK가 성공한 뒤 공통 Bridge에 게임 작업을 넣는다. 카드/모달/이모지는 각자의 책임이며 토큰은 Windows CurrentUser DPAPI ciphertext로만 저장한다.

기준: 0.3.52.0 소스. 기반 main `6e6e482b4e56a7c409c7e58fdb0ed5b2d1d24bce` 이후 상시 채팅·인게임 수신/멘션 토글·홈 서버 보완·점유 자리 직업 자동 정규화 검증 변경을 포함한다. 2026-10-26 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [src/DiscordChatFormatting.cs](#file-src-discordchatformatting-cs)
- [src/DiscordChatSettings.cs](#file-src-discordchatsettings-cs)
- [src/ListingDiscordChat.cs](#file-src-listingdiscordchat-cs)
- [src/ListingDiscordClient.Chat.cs](#file-src-listingdiscordclient-chat-cs)
- [src/ListingDiscordClient.Notifications.cs](#file-src-listingdiscordclient-notifications-cs)
- [src/ListingDiscordClient.cs](#file-src-listingdiscordclient-cs)
- [src/ListingDiscordEmojiAssets.cs](#file-src-listingdiscordemojiassets-cs)
- [src/ListingDiscordIcons.cs](#file-src-listingdiscordicons-cs)
- [src/ListingDiscordNotifications.cs](#file-src-listingdiscordnotifications-cs)
- [src/ListingDiscordPanel.Forms.cs](#file-src-listingdiscordpanel-forms-cs)
- [src/ListingDiscordPanel.Render.cs](#file-src-listingdiscordpanel-render-cs)
- [src/ListingDiscordPanel.Results.cs](#file-src-listingdiscordpanel-results-cs)
- [src/ListingDiscordPanel.cs](#file-src-listingdiscordpanel-cs)
- [src/ListingDiscordSession.Chat.cs](#file-src-listingdiscordsession-chat-cs)
- [src/ListingDiscordSession.Notifications.cs](#file-src-listingdiscordsession-notifications-cs)
- [src/ListingDiscordSession.Recovery.cs](#file-src-listingdiscordsession-recovery-cs)
- [src/ListingDiscordSession.cs](#file-src-listingdiscordsession-cs)
- [src/Plugin.Discord.cs](#file-src-plugin-discord-cs)
- [src/RelayConfiguration.cs](#file-src-relayconfiguration-cs)

<a id="file-src-discordchatformatting-cs"></a>
## src/DiscordChatFormatting.cs

[소스 파일](../../src/DiscordChatFormatting.cs) · 명명된 항목/진입점 2개

채팅을 모바일에서도 표시되는 색상 임베드로 만든다. 일반 봇 메시지이며 조회 카드의 Components V2/만료 수명과 별개다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-discordchatformatting-cs-8-c7ba6996f2"></a><code>DiscordChatFormatting.Color</code><br><code>uint Color(GameChatKind kind)</code> | 메서드 | 파티0x4D9CFF·귓속말0xFF77B7·연합0xFFA34D 색을 반환한다. 알 수 없는 종류는 거부한다. | [L8](../../src/DiscordChatFormatting.cs#L8) |
| <a id="symbol-src-discordchatformatting-cs-15-6c9c2b4f03"></a><code>DiscordChatFormatting.Render</code><br><code>Embed[] Render(GameChatMessage message)</code> | 메서드 | 닉네임@서버 : 본문을 Markdown escape하고 @를 비활성화한다. Unicode scalar/escape 단위를 자르지 않고 description4000자 이내로 나눠 모든 내용과 원래 시각을 보존한다. 각 part는 새 일반 메시지용 embed다. | [L15](../../src/DiscordChatFormatting.cs#L15) |

<a id="file-src-discordchatsettings-cs"></a>
## src/DiscordChatSettings.cs

[소스 파일](../../src/DiscordChatSettings.cs) · 명명된 항목/진입점 8개

채팅 종류별 서버/채널/everyone 선호 설정과 관리형 메시지·명령 모델. 설정에는 채팅 본문을 저장하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-discordchatsettings-cs-18-a25559e736"></a><code>DiscordChatSettings.ForApplication</code><br><code>DiscordChatSettings ForApplication(ulong app)</code> | 메서드 | 현재 앱 ID와 일치하는 유효 경로만 복사·정렬해 복원한다. 잘못된 종류/0 서버·채널을 버리고 같은 종류의 마지막 경로를 사용한다. | [L18](../../src/DiscordChatSettings.cs#L18) |
| <a id="symbol-src-discordchatsettings-cs-25-4b111d8c8a"></a><code>DiscordChatSettings.WithOptions</code><br><code>DiscordChatSettings WithOptions(GameChatKind kind, bool? enabled, bool? mention)</code> | 메서드 | 채널/앱 정보를 보존하며 해당 종류의 Enabled와 MentionEveryone만 nullable 선택에 따라 바꾼 새 배열을 만든다. 미설정 종류의 채널을 새로 만들지는 않는다. | [L25](../../src/DiscordChatSettings.cs#L25) |
| <a id="symbol-src-discordchatsettings-cs-32-0b255ec942"></a><code>GameChatMessage</code><br><code>GameChatMessage(GameChatKind Kind, string Name, string World, string Text, DateTimeOffset At)</code> | 주 생성자/모델 | 공개 채팅 이벤트에서 복사한 종류·발신자·서버·본문·시각이다. 네이티브 포인터나 SeString 참조를 큐에 넘기지 않는다. | [L32](../../src/DiscordChatSettings.cs#L32) |
| <a id="symbol-src-discordchatsettings-cs-33-d368033355"></a><code>DiscordChatCommand</code><br><code>DiscordChatCommand(string Action, string Kind = "", ulong ChannelId = 0, bool? MentionEveryone = null)</code> | 주 생성자/모델 | 검증된 slash 하위 명령·종류·채널과 nullable 멘션 선택이다. 멘션 null은 기존 선택 보존을 뜻한다. | [L33](../../src/DiscordChatSettings.cs#L33) |
| <a id="symbol-src-discordchatsettings-cs-38-a15ede856d"></a><code>GameChat.FromType</code><br><code>GameChatKind? FromType(ushort type)</code> | 메서드 | API15 파티14·서버 초월 파티32를 Party, 받은 귓속말13을 Tell, 연합15를 Alliance로 묶는다. 보낸 귓속말12 등 다른 종류는 제외한다. | [L38](../../src/DiscordChatSettings.cs#L38) |
| <a id="symbol-src-discordchatsettings-cs-41-1a9d99e8f7"></a><code>GameChat.Key</code><br><code>string Key(GameChatKind kind)</code> | 메서드 | 채팅 종류를 저장용 party/tell/alliance 키로 변환한다. | [L41](../../src/DiscordChatSettings.cs#L41) |
| <a id="symbol-src-discordchatsettings-cs-44-d8d2dfed58"></a><code>GameChat.TryKind</code><br><code>bool TryKind(string key, out GameChatKind kind)</code> | 메서드 | 저장·명령 입력 키를 세 가지 알려진 채팅 종류로만 변환한다. | [L44](../../src/DiscordChatSettings.cs#L44) |
| <a id="symbol-src-discordchatsettings-cs-49-f90502621c"></a><code>GameChat.Label</code><br><code>string Label(GameChatKind kind)</code> | 메서드 | 채팅 종류의 한국어 표시명을 반환한다. | [L49](../../src/DiscordChatSettings.cs#L49) |

<a id="file-src-listingdiscordchat-cs"></a>
## src/ListingDiscordChat.cs

[소스 파일](../../src/ListingDiscordChat.cs) · 명명된 항목/진입점 20개

종류별 저장 경로·수신/멘션 선택과 단일 전송 큐를 소유한다. 인게임/슬래시 명령 변경을 같은 semaphore로 직렬화하며, 설정 변경은 과거 채팅을 새 목적지로 보내지 않는다.

직접 관련 테스트: [ListingDiscordChatTests.cs](tests.md#file-tests-listingdiscordchattests-cs).

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordchat-cs-6-534036582b"></a><code>IDiscordChatTransport.CheckChannel</code><br><code>Task&lt;string?&gt; CheckChannel(DiscordChatRoute route, CancellationToken cancellation)</code> | 인터페이스 계약 | 선택 경로의 서버/채널·권한을 검증하는 계약이다. 메시지는 전송하지 않는다. | [L6](../../src/ListingDiscordChat.cs#L6) |
| <a id="symbol-src-listingdiscordchat-cs-7-f59aae5f59"></a><code>IDiscordChatTransport.Send</code><br><code>Task Send(DiscordChatRoute route, GameChatMessage message, CancellationToken cancellation)</code> | 인터페이스 계약 | 검증된 경로로 한 게임 채팅을 보내는 계약이다. 길면 여러 메시지가 될 수 있다. | [L7](../../src/ListingDiscordChat.cs#L7) |
| <a id="symbol-src-listingdiscordchat-cs-21-3e75aa3b50"></a><code>ListingDiscordChat.Pending</code><br><code>Pending(ulong Self, DiscordChatRoute Route, GameChatMessage Message)</code> | 주 생성자/모델 | 받을 당시 본인 ID·경로 snapshot·관리형 채팅을 고정한다. 나중의 설정 변경으로 목적지가 바뀌지 않는다. | [L21](../../src/ListingDiscordChat.cs#L21) |
| <a id="symbol-src-listingdiscordchat-cs-30-7682a51d7c"></a><code>ListingDiscordChat.Status</code><br><code>string Status</code> | 계산 속성 | lock 안에서 전송·실패·제외·대기 통계와 마지막 상태를 반환한다. | [L30](../../src/ListingDiscordChat.cs#L30) |
| <a id="symbol-src-listingdiscordchat-cs-31-65cd5dfd8c"></a><code>ListingDiscordChat.Editable</code><br><code>bool Editable</code> | 계산 속성 | controller가 종료/취소되지 않아 인게임 설정을 받을 수 있는지 lock 안에서 반환한다. | [L31](../../src/ListingDiscordChat.cs#L31) |
| <a id="symbol-src-listingdiscordchat-cs-32-940502faf6"></a><code>ListingDiscordChat.Settings</code><br><code>DiscordChatSettings Settings</code> | 계산 속성 | 현재 설정을 배열 복사한 snapshot으로 반환한다. UI가 controller의 route 배열을 변경하지 못하게 한다. | [L32](../../src/ListingDiscordChat.cs#L32) |
| <a id="symbol-src-listingdiscordchat-cs-33-12c72f7f43"></a><code>ListingDiscordChat.PendingCount</code><br><code>int PendingCount</code> | 계산 속성 | 대기 큐와 진행 중 전송을 합친 수를 반환한다. 테스트의 완료 대기에 사용한다. | [L33](../../src/ListingDiscordChat.cs#L33) |
| <a id="symbol-src-listingdiscordchat-cs-35-a6f69d2210"></a><code>ListingDiscordChat.ListingDiscordChat</code><br><code>ListingDiscordChat(ulong applicationId, IDiscordChatTransport transport, DiscordChatSettings? saved, Func&lt;DiscordChatSettings, CancellationToken, Task&gt; save, CancellationToken cancellation)</code> | 생성자 | 현재 앱의 경로를 복원하고 transport·저장 callback·취소 수명과 단일 Pump를 준비한다. | [L35](../../src/ListingDiscordChat.cs#L35) |
| <a id="symbol-src-listingdiscordchat-cs-44-1c29f19ba5"></a><code>ListingDiscordChat.Observe</code><br><code>void Observe(bool loggedIn, ulong selfId)</code> | 메서드 | 로그인/CID를 관찰한다. 로그아웃이나 캐릭터 변경은 대기를 버리고 진행 전송을 취소한다. AFK는 전달받지 않는다. | [L44](../../src/ListingDiscordChat.cs#L44) |
| <a id="symbol-src-listingdiscordchat-cs-54-7c1531b7c7"></a><code>ListingDiscordChat.Wants</code><br><code>bool Wants(GameChatKind kind)</code> | 메서드 | 로그인 상태에서 해당 종류의 활성 경로가 있는지 확인해 불필요한 payload 읽기를 막는다. | [L54](../../src/ListingDiscordChat.cs#L54) |
| <a id="symbol-src-listingdiscordchat-cs-59-8e0366c09a"></a><code>ListingDiscordChat.Receive</code><br><code>bool Receive(ulong selfId, GameChatMessage message)</code> | 메서드 | 본인/종류/경로·본문을 검사하고 route snapshot과 함께 enqueue한다. 과대 입력은 실패로,200건 초과는 가장 오래된 대기 메시지 제외로 기록한다. 같은 내용의 실제 반복 채팅은 합치지 않는다. | [L59](../../src/ListingDiscordChat.cs#L59) |
| <a id="symbol-src-listingdiscordchat-cs-76-8b8f46aa64"></a><code>ListingDiscordChat.Configure</code><br><code>Task&lt;string&gt; Configure(DiscordChatCommand command, ulong guildId, CancellationToken cancellation)</code> | 메서드 | 서버 슬래시의 상태/설정/멘션/해제를 처리한다. 권한 확인 후 공통 Apply로 저장한다. 설정 명령은 수신을 켜며 멘션만 바꾸면 기존 Enabled를 보존한다. | [L76](../../src/ListingDiscordChat.cs#L76) |
| <a id="symbol-src-listingdiscordchat-cs-116-cfa08e7e1d"></a><code>ListingDiscordChat.ConfigureLocal</code><br><code>Task&lt;string&gt; ConfigureLocal(GameChatKind kind, bool? enabled, bool? mention, CancellationToken cancellation)</code> | 메서드 | 슬래시와 같은 editing semaphore를 얻은 뒤 최신 route의 수신/멘션만 바꾼다. 채널 권한이 없어도 끄기가 가능하며 미설정 채널은 만들지 않는다.15초 취소 예산을 적용한다. | [L116](../../src/ListingDiscordChat.cs#L116) |
| <a id="symbol-src-listingdiscordchat-cs-137-58ec6f5dd9"></a><code>ListingDiscordChat.Apply</code><br><code>Task Apply(DiscordChatSettings updated, string kind, bool changed, CancellationToken ct)</code> | 메서드 | 취소 확인→Framework 저장→활성 설정 반영을 수행한다. 변경된 종류의 대기만 버리고 진행 전송을 취소한다. 다른 종류의 메시지·경로는 유지한다. | [L137](../../src/ListingDiscordChat.cs#L137) |
| <a id="symbol-src-listingdiscordchat-cs-150-bdfc4ecebf"></a><code>ListingDiscordChat.RouteSummary</code><br><code>string RouteSummary(GameChatKind kind, DiscordChatRoute route)</code> | 메서드 | 종류/채널·수신 켜짐 여부·멘션 선택을 Discord 설정 응답 문자열로 만든다. | [L150](../../src/ListingDiscordChat.cs#L150) |
| <a id="symbol-src-listingdiscordchat-cs-152-643fa75ed3"></a><code>ListingDiscordChat.StatusLocked</code><br><code>string StatusLocked()</code> | 메서드 | 로그인/수신 대기 상태·활성 종류 수·전송/실패/제외/대기와 마지막 오류를 조합한다. | [L152](../../src/ListingDiscordChat.cs#L152) |
| <a id="symbol-src-listingdiscordchat-cs-154-c0c61a2db9"></a><code>ListingDiscordChat.SummaryLocked</code><br><code>string SummaryLocked()</code> | 메서드 | 세 채팅 종류 각각의 채널/멘션과 전송 상태를 상태 명령 응답으로 만든다. | [L154](../../src/ListingDiscordChat.cs#L154) |
| <a id="symbol-src-listingdiscordchat-cs-158-215363b4e3"></a><code>ListingDiscordChat.Current</code><br><code>bool Current(Pending pending)</code> | 메서드 | 대기 항목의 본인 CID와 저장 경로 snapshot이 현재와 같고 그 종류가 여전히 Enabled인지 재검사한다. | [L158](../../src/ListingDiscordChat.cs#L158) |
| <a id="symbol-src-listingdiscordchat-cs-160-d994ccb625"></a><code>ListingDiscordChat.Pump</code><br><code>Task Pump()</code> | 메서드 | 한 worker가 수신 순서대로 처리한다. 일시적 Gateway 단절은 대기하고,30초 예산 내 채널·경로·캐릭터를 재검증해 전송한다. 불확실한 POST를 반복하지 않으며 실패 후 다음 항목은 계속 처리한다. | [L160](../../src/ListingDiscordChat.cs#L160) |
| <a id="symbol-src-listingdiscordchat-cs-200-02943f4e7f"></a><code>ListingDiscordChat.Dispose</code><br><code>void Dispose()</code> | 메서드 | 새 수신을 막고 대기 큐·진행 전송·worker 대기를 취소한다. 이미 전송된 Discord 메시지는 삭제하지 않는다. | [L200](../../src/ListingDiscordChat.cs#L200) |

<a id="file-src-listingdiscordclient-chat-cs"></a>
## src/ListingDiscordClient.Chat.cs

[소스 파일](../../src/ListingDiscordClient.Chat.cs) · 명명된 항목/진입점 17개

Discord.Net slash 등록/엄격한 입력 파싱과 채팅 서버 채널 전송 어댑터. 기존 pvkr 카드 경로와 독립적으로 pvkr-chat 명령을 등록한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordclient-chat-cs-9-fb038ba733"></a><code>ListingDiscordClient.ChatStatus</code><br><code>string ChatStatus</code> | 계산 속성 | 채팅 controller 상태 또는 연결 준비 문구를 반환한다. 계산식: <code>chat?.Status ?? "채팅 연결 준비 중입니다."</code>. | [L9](../../src/ListingDiscordClient.Chat.cs#L9) |
| <a id="symbol-src-listingdiscordclient-chat-cs-10-d6437094bf"></a><code>ListingDiscordClient.ChatEditable</code><br><code>bool ChatEditable</code> | 계산 속성 | 인게임 토글을 받을 controller가 준비/활성 상태인지 반환한다. 계산식: <code>chat?.Editable == true</code>. | [L10](../../src/ListingDiscordClient.Chat.cs#L10) |
| <a id="symbol-src-listingdiscordclient-chat-cs-11-7da14032c1"></a><code>ListingDiscordClient.ChatSettings</code><br><code>DiscordChatSettings? ChatSettings</code> | 계산 속성 | 현재 controller의 복사된 설정을 반환한다. 아직 만들지 않았다면 null이다. 계산식: <code>chat?.Settings</code>. | [L11](../../src/ListingDiscordClient.Chat.cs#L11) |
| <a id="symbol-src-listingdiscordclient-chat-cs-12-27fac7ae33"></a><code>ListingDiscordClient.ConfigureChat</code><br><code>Task&lt;string&gt; ConfigureChat(GameChatKind kind, bool? enabled, bool? mention)</code> | 메서드 | 인게임 수신/멘션 선택을 controller의 비동기 직렬화 경로로 전달한다. 준비 전에는 안내만 반환한다. | [L12](../../src/ListingDiscordClient.Chat.cs#L12) |
| <a id="symbol-src-listingdiscordclient-chat-cs-14-eb555fd3ef"></a><code>ListingDiscordClient.ObserveChat</code><br><code>void ObserveChat(bool enabled, ulong self)</code> | 메서드 | 로그인·캐릭터 관측을 현재 채팅 controller에 전달한다. AFK와 독립적이다. | [L14](../../src/ListingDiscordClient.Chat.cs#L14) |
| <a id="symbol-src-listingdiscordclient-chat-cs-15-e46d48226e"></a><code>ListingDiscordClient.WantsChat</code><br><code>bool WantsChat(GameChatKind kind)</code> | 메서드 | 해당 채팅 종류의 현재 수신 필요 여부를 반환한다. | [L15](../../src/ListingDiscordClient.Chat.cs#L15) |
| <a id="symbol-src-listingdiscordclient-chat-cs-16-2e201f6327"></a><code>ListingDiscordClient.ReceiveChat</code><br><code>bool ReceiveChat(ulong self, GameChatMessage message)</code> | 메서드 | 게임에서 복사한 관리형 메시지를 controller 큐에 전달한다. | [L16](../../src/ListingDiscordClient.Chat.cs#L16) |
| <a id="symbol-src-listingdiscordclient-chat-cs-18-86bad51929"></a><code>ListingDiscordClient.ChatCommand</code><br><code>SlashCommandProperties ChatCommand()</code> | 메서드 | 서버 전용 pvkr-chat에 설정·멘션·해제·상태 하위 명령과 한국어 선택지·텍스트 채널 picker를 선언한다. | [L18](../../src/ListingDiscordClient.Chat.cs#L18) |
| <a id="symbol-src-listingdiscordclient-chat-cs-20-66b6f1f878"></a><code>ListingDiscordClient.ChatCommand.Kind</code><br><code>SlashCommandOptionBuilder Kind()</code> | 로컬 함수 | 파티/귓속말/연합파티 필수 선택 옵션을 만든다. | [L20](../../src/ListingDiscordClient.Chat.cs#L20) |
| <a id="symbol-src-listingdiscordclient-chat-cs-23-b3bbd664ac"></a><code>ListingDiscordClient.ChatCommand.Mention</code><br><code>SlashCommandOptionBuilder Mention(string name, bool required)</code> | 로컬 함수 | 끄기/everyone 옵션을 필수 또는 선택 형태로 만든다. 설정에서 생략 시 이전 선택을 유지한다. | [L23](../../src/ListingDiscordClient.Chat.cs#L23) |
| <a id="symbol-src-listingdiscordclient-chat-cs-26-9b66434d05"></a><code>ListingDiscordClient.ChatCommand.Action</code><br><code>SlashCommandOptionBuilder Action(string name, string description)</code> | 로컬 함수 | 설명과 이름을 가진 slash 하위 명령 builder를 만든다. | [L26](../../src/ListingDiscordClient.Chat.cs#L26) |
| <a id="symbol-src-listingdiscordclient-chat-cs-39-8b9fead3e5"></a><code>ListingDiscordClient.ReadChatCommand</code><br><code>DiscordChatCommand? ReadChatCommand(SocketSlashCommand command)</code> | 메서드 | SDK가 파싱한 단일 하위 명령의 허용 이름·중복·타입·종류·채널/멘션 선택을 엄격히 검사해 managed 명령으로 바꾼다. | [L39](../../src/ListingDiscordClient.Chat.cs#L39) |
| <a id="symbol-src-listingdiscordclient-chat-cs-69-086c864103"></a><code>ListingDiscordClient.ChatTransport</code><br><code>ChatTransport(ListingDiscordClient client)</code> | 주 생성자/모델 | 현재 개인 봇 socket의 채널·권한과 bot REST 전송을 controller 계약에 연결한다. | [L69](../../src/ListingDiscordClient.Chat.cs#L69) |
| <a id="symbol-src-listingdiscordclient-chat-cs-71-b0f121c39a"></a><code>ListingDiscordClient.ChatTransport.Connected</code><br><code>bool Connected</code> | 계산 속성 | Gateway/ready/종료를 포함한 클라이언트 연결 상태를 반환한다. 계산식: <code>client.Connected</code>. | [L71](../../src/ListingDiscordClient.Chat.cs#L71) |
| <a id="symbol-src-listingdiscordclient-chat-cs-72-6a0106a5fd"></a><code>ListingDiscordClient.ChatTransport.Channel</code><br><code>SocketTextChannel? Channel(DiscordChatRoute route)</code> | 메서드 | 선택한 서버 안의 동일 ID 일반 텍스트 채널을 캐시에서 찾는다. 다른 서버·DM으로 fallback하지 않는다. | [L72](../../src/ListingDiscordClient.Chat.cs#L72) |
| <a id="symbol-src-listingdiscordclient-chat-cs-73-f39e5b7da3"></a><code>ListingDiscordClient.ChatTransport.CheckChannel</code><br><code>Task&lt;string?&gt; CheckChannel(DiscordChatRoute route, CancellationToken cancellation)</code> | 메서드 | 연결·일반 텍스트 채널·ViewChannel/SendMessages/EmbedLinks를 확인한다. 멘션 설정이 켜진 경우에만 MentionEveryone을 요구한다. | [L73](../../src/ListingDiscordClient.Chat.cs#L73) |
| <a id="symbol-src-listingdiscordclient-chat-cs-84-1be315bd05"></a><code>ListingDiscordClient.ChatTransport.Send</code><br><code>Task Send(DiscordChatRoute route, GameChatMessage message, CancellationToken cancellation)</code> | 메서드 | 권한을 각 part마다 재검사하고 일반 embed 메시지를 보낸다. 첫 part만 선택적 everyone을 허용하고 나머지는 명시적 빈 allowed_mentions를 사용한다. rate limit만 SDK 재시도하며 멘션 응답을 확인한다. | [L84](../../src/ListingDiscordClient.Chat.cs#L84) |

<a id="file-src-listingdiscordclient-notifications-cs"></a>
## src/ListingDiscordClient.Notifications.cs

[소스 파일](../../src/ListingDiscordClient.Notifications.cs) · 명명된 항목/진입점 8개

모집 완료 알림의 Discord.Net 전송 어댑터. 서버 텍스트 채널·bot 권한을 검사하고 everyone만 허용한 새 메시지를 보내며 서버 mention 응답을 확인한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordclient-notifications-cs-9-997b92d8fd"></a><code>ListingDiscordClient.NotificationStatus</code><br><code>string NotificationStatus</code> | 계산 속성 | 알림 controller의 상태 또는 연결 준비 문구를 반환한다. 계산식: <code>notifications?.Status ?? "디스코드 알림 연결 준비 중입니다."</code>. | [L9](../../src/ListingDiscordClient.Notifications.cs#L9) |
| <a id="symbol-src-listingdiscordclient-notifications-cs-10-e00893c852"></a><code>ListingDiscordClient.ObserveCompletion</code><br><code>void ObserveCompletion(bool enabled, ulong self, long revision, bool complete)</code> | 메서드 | 연결된 클라이언트일 때만 AFK의 현재 완료 관측을 managed 알림 controller에 전달한다. | [L10](../../src/ListingDiscordClient.Notifications.cs#L10) |
| <a id="symbol-src-listingdiscordclient-notifications-cs-13-f765c3abe6"></a><code>ListingDiscordClient.CompletionMentions</code><br><code>AllowedMentions CompletionMentions()</code> | 메서드 | 고정 완료 메시지의 everyone mention type만 허용한다. 일반 카드의 AllowedMentions.None 정책은 유지한다. | [L13](../../src/ListingDiscordClient.Notifications.cs#L13) |
| <a id="symbol-src-listingdiscordclient-notifications-cs-14-8959912a35"></a><code>ListingDiscordClient.NotificationTransport</code><br><code>NotificationTransport(ListingDiscordClient client)</code> | 주 생성자/모델 | 현재 개인 봇 socket의 채널/권한과 새 메시지 전송을 controller 계약에 연결한다. | [L14](../../src/ListingDiscordClient.Notifications.cs#L14) |
| <a id="symbol-src-listingdiscordclient-notifications-cs-16-241f554369"></a><code>ListingDiscordClient.NotificationTransport.Connected</code><br><code>bool Connected</code> | 계산 속성 | 클라이언트의 Gateway/ready/종료 상태를 함께 확인한다. 계산식: <code>client.Connected</code>. | [L16](../../src/ListingDiscordClient.Notifications.cs#L16) |
| <a id="symbol-src-listingdiscordclient-notifications-cs-17-7c09aa1962"></a><code>ListingDiscordClient.NotificationTransport.Channel</code><br><code>SocketTextChannel? Channel(DiscordNotificationTarget target)</code> | 메서드 | 선택된 서버 안의 동일 ID 텍스트 채널만 socket 캐시에서 찾는다. 다른 채널로 fallback하지 않는다. | [L17](../../src/ListingDiscordClient.Notifications.cs#L17) |
| <a id="symbol-src-listingdiscordclient-notifications-cs-20-e01dffd7d6"></a><code>ListingDiscordClient.NotificationTransport.CheckChannel</code><br><code>Task&lt;string?&gt; CheckChannel(DiscordNotificationTarget target, CancellationToken cancellation)</code> | 메서드 | 연결·서버 텍스트 채널·ViewChannel/SendMessages/MentionEveryone을 검사한다. 오류는 사용자용 고정 문구로 반환한다. | [L20](../../src/ListingDiscordClient.Notifications.cs#L20) |
| <a id="symbol-src-listingdiscordclient-notifications-cs-32-d165ddc03e"></a><code>ListingDiscordClient.NotificationTransport.SendCompletion</code><br><code>Task SendCompletion(DiscordNotificationTarget target, CancellationToken cancellation)</code> | 메서드 | 권한과 취소를 재검사하고 @everyone 파티모집완료를 봇 토큰으로 POST한다. 자동 HTTP 재시도를 끄고 응답 MentionedEveryone이 참인지 확인한다. | [L32](../../src/ListingDiscordClient.Notifications.cs#L32) |

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
| <a id="symbol-src-listingdiscordclient-cs-26-ed702c5d24"></a><code>ListingDiscordClient.ListingDiscordClient</code><br><code>ListingDiscordClient(ListingWebBridge bridge, string token, DiscordSocketConfig? config = null, DiscordNotificationTarget? notificationTarget = null, Func&lt;DiscordNotificationTarget, CancellationToken, Task&gt;? saveNotificationTarget = null, DiscordChatSettings? chatSettings = null, Func&lt;DiscordChatSettings, CancellationToken, Task&gt;? saveChatSettings = null)</code> | 생성자 | Bridge·token·socket config를 받고 Gateway 이벤트를 연결한다. 비동기 lifetime을 시작하며 game thread를 기다리게 하지 않는다. | [L26](../../src/ListingDiscordClient.cs#L26) |
| <a id="symbol-src-listingdiscordclient-cs-42-00fca0fe36"></a><code>ListingDiscordClient.Run</code><br><code>Task Run(string token, DiscordNotificationTarget? notificationTarget, Func&lt;DiscordNotificationTarget, CancellationToken, Task&gt;? saveNotificationTarget, DiscordChatSettings? chatSettings, Func&lt;DiscordChatSettings, CancellationToken, Task&gt;? saveChatSettings)</code> | 메서드 | 봇 로그인/소유자 확인 뒤 알림·채팅 controller와 세션을 만들고 Gateway READY를 기다린다. pvkr와 pvkr-chat을 등록한 뒤 연결을 활성화하고 이모지를 준비한다. 종료 시 chat worker 완료 후 socket을 해제한다. | [L42](../../src/ListingDiscordClient.cs#L42) |
| <a id="symbol-src-listingdiscordclient-cs-104-e8c2b1d038"></a><code>ListingDiscordClient.Options</code><br><code>RequestOptions Options()</code> | 메서드 | 일반 SDK 요청은 rate-limit 재시도/timeout 정책을, Response 내 요청은 최초 ACK 2초·재시도 없음과 후속 10초 정책을 구성한다. | [L104](../../src/ListingDiscordClient.cs#L104) |
| <a id="symbol-src-listingdiscordclient-cs-105-79e1a56b40"></a><code>ListingDiscordClient.PrepareEmojis</code><br><code>Task PrepareEmojis(ListingDiscordIcons icons)</code> | 메서드 | 5분 취소 예산 안에서 application emoji 목록을 읽고 필요한 이미지 재사용·이름 변경·업로드를 수행한다. 일부 실패해도 기호 fallback으로 연결을 유지한다. | [L105](../../src/ListingDiscordClient.cs#L105) |
| <a id="symbol-src-listingdiscordclient-cs-109-ed44f3775c"></a><code>ListingDiscordClient.PrepareEmojis.EmojiOptions</code><br><code>RequestOptions EmojiOptions(CancellationToken ct)</code> | 로컬 함수 | 이모지 API 호출의 취소 token/timeout/재시도 옵션을 구성한다. | [L109](../../src/ListingDiscordClient.cs#L109) |
| <a id="symbol-src-listingdiscordclient-cs-111-dc144f8ba9"></a><code>ListingDiscordClient.PrepareEmojis.Progress</code><br><code>void Progress(int done, int total)</code> | 로컬 함수 | 이모지 처리 완료 수와 재사용/추가/이름 변경 진행 상태를 표시한다. | [L111](../../src/ListingDiscordClient.cs#L111) |
| <a id="symbol-src-listingdiscordclient-cs-140-53b6de0fba"></a><code>ListingDiscordClient.ConnectedMessage</code><br><code>string ConnectedMessage</code> | 계산 속성 | 연결 완료 문구와 현재 이모지 상태를 합성한다. 계산식: <code>ConnectedStatus + (emojiStatus.Length &gt; 0 ? "\n" + emojiStatus : "")</code>. | [L140](../../src/ListingDiscordClient.cs#L140) |
| <a id="symbol-src-listingdiscordclient-cs-141-637e530535"></a><code>ListingDiscordClient.SetEmojiStatus</code><br><code>void SetEmojiStatus(string text)</code> | 메서드 | 이모지 상태를 thread-safe하게 갱신해 연결 UI에 반영한다. | [L141](../../src/ListingDiscordClient.cs#L141) |
| <a id="symbol-src-listingdiscordclient-cs-146-82e0dd5f8a"></a><code>ListingDiscordClient.FailureDetails</code><br><code>string FailureDetails(string stage, Exception error)</code> | 메서드 | 실패 단계와 HTTP/Gateway/예외 종류만 반환한다. 원시 SDK 오류/토큰은 상태 문자열에 붙이지 않는다. | [L146](../../src/ListingDiscordClient.cs#L146) |
| <a id="symbol-src-listingdiscordclient-cs-156-4143db87a9"></a><code>ListingDiscordClient.Ready</code><br><code>Task Ready()</code> | 메서드 | READY 대기 completion을 완료하고 연결 상태를 갱신한다. | [L156](../../src/ListingDiscordClient.cs#L156) |
| <a id="symbol-src-listingdiscordclient-cs-162-8832701095"></a><code>ListingDiscordClient.Disconnected</code><br><code>Task Disconnected(Exception exception)</code> | 메서드 | 일시 Gateway 단절 상태를 기록한다. 수동 작업을 재전송하지 않는다. | [L162](../../src/ListingDiscordClient.cs#L162) |
| <a id="symbol-src-listingdiscordclient-cs-167-d9c5e5bd7e"></a><code>ListingDiscordClient.Reconnected</code><br><code>Task Reconnected()</code> | 메서드 | 재연결/RESUME 후 READY가 다시 오지 않는 경우에도 연결 표시를 복구한다. | [L167](../../src/ListingDiscordClient.cs#L167) |
| <a id="symbol-src-listingdiscordclient-cs-174-b42fa879de"></a><code>ListingDiscordClient.Interaction</code><br><code>Task Interaction(SocketInteraction interaction)</code> | 메서드 | pvkr/pvkr-chat slash·기존 버튼/모달을 관리형 입력으로 파싱한다. 서버/채널 ID를 SDK interaction에서 가져오고 기존 세션의 owner/app 검사로 보낸다. | [L174](../../src/ListingDiscordClient.cs#L174) |
| <a id="symbol-src-listingdiscordclient-cs-197-99ee80986a"></a><code>ListingDiscordClient.ReadModal</code><br><code>DiscordInput ReadModal(SocketModal modal)</code> | 메서드 | Discord modal의 checkbox group/checkbox/radio/text 입력만 allowlist로 읽고 중복·지원하지 않는 구조는 거부한다. | [L197](../../src/ListingDiscordClient.cs#L197) |
| <a id="symbol-src-listingdiscordclient-cs-220-e43f85fb35"></a><code>ListingDiscordClient.Dispose</code><br><code>void Dispose()</code> | 메서드 | 알림·채팅 전송과 Bridge·연결 수명을 취소하고 네트워크 정리는 비동기 lifetime에 맡긴다. Framework를 기다리게 하지 않는다. | [L220](../../src/ListingDiscordClient.cs#L220) |
| <a id="symbol-src-listingdiscordclient-cs-227-ea23d40323"></a><code>ListingDiscordClient.Response</code><br><code>Response(SocketInteraction interaction, CancellationToken cancellation)</code> | 주 생성자/모델 | 해당 SDK interaction에 묶인 IDiscordResponse 어댑터를 만든다. | [L227](../../src/ListingDiscordClient.cs#L227) |
| <a id="symbol-src-listingdiscordclient-cs-229-fdf95e3733"></a><code>ListingDiscordClient.Response.Options</code><br><code>RequestOptions Options(bool ack = false)</code> | 메서드 | 일반 SDK 요청은 rate-limit 재시도/timeout 정책을, Response 내 요청은 최초 ACK 2초·재시도 없음과 후속 10초 정책을 구성한다. | [L229](../../src/ListingDiscordClient.cs#L229) |
| <a id="symbol-src-listingdiscordclient-cs-233-002b8a27aa"></a><code>ListingDiscordClient.Response.Defer</code><br><code>Task Defer()</code> | 메서드 | 게임 작업 전 interaction을 ACK한다. slash는 ephemeral deferred response를 사용한다. | [L233](../../src/ListingDiscordClient.cs#L233) |
| <a id="symbol-src-listingdiscordclient-cs-234-9418c53bbb"></a><code>ListingDiscordClient.Response.Modal</code><br><code>Task Modal(Modal modal)</code> | 메서드 | 즉시 modal을 최초 응답으로 보낸다. 이미 defer한 interaction에 modal을 다시 열지 않는다. | [L234](../../src/ListingDiscordClient.cs#L234) |
| <a id="symbol-src-listingdiscordclient-cs-235-79b8036046"></a><code>ListingDiscordClient.Response.Screen</code><br><code>Task Screen(DiscordScreen screen, bool replace, bool deferred)</code> | 메서드 | 새 카드/기존 카드 업데이트/후속 응답을 맞는 SDK 경로로 전달한다. Components V2를 쓰고 기존 content/embeds를 비우며 AllowedMentions.None을 적용한다. | [L235](../../src/ListingDiscordClient.cs#L235) |
| <a id="symbol-src-listingdiscordclient-cs-237-7ac507c2e7"></a><code>ListingDiscordClient.Response.Screen.Set</code><br><code>void Set(MessageProperties p)</code> | 로컬 함수 | SDK MessageProperties에 카드 components·flags·mention 정책을 설정한다. | [L237](../../src/ListingDiscordClient.cs#L237) |

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

<a id="file-src-listingdiscordnotifications-cs"></a>
## src/ListingDiscordNotifications.cs

[소스 파일](../../src/ListingDiscordNotifications.cs) · 명명된 항목/진입점 11개

AFK 완료 세대별 everyone 알림 controller. 명시적으로 선택한 목적지만 저장하며 비동기 전송 중 취소/권한 실패/불확실한 POST의 중복 멘션을 막는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordnotifications-cs-3-7f3245a21e"></a><code>DiscordNotificationTarget</code><br><code>DiscordNotificationTarget(ulong ApplicationId, ulong GuildId, ulong ChannelId)</code> | 주 생성자/모델 | 봇 애플리케이션·서버·텍스트 채널 ID를 묶는다. 게임 내용이나 interaction token을 포함하지 않는다. | [L3](../../src/ListingDiscordNotifications.cs#L3) |
| <a id="symbol-src-listingdiscordnotifications-cs-5-921b6fa0b1"></a><code>DiscordNotificationTarget.ForApplication</code><br><code>bool ForApplication(ulong app)</code> | 메서드 | 저장한 목적지가 현재 봇과 같고 서버·채널 ID가 비어 있지 않은지 검사한다. | [L5](../../src/ListingDiscordNotifications.cs#L5) |
| <a id="symbol-src-listingdiscordnotifications-cs-11-af85b19d5e"></a><code>IDiscordNotificationTransport.CheckChannel</code><br><code>Task&lt;string?&gt; CheckChannel(DiscordNotificationTarget target, CancellationToken cancellation)</code> | 인터페이스 계약 | 목적지 접근 및 everyone 권한 확인 계약이다. 메시지를 보내지 않는다. | [L11](../../src/ListingDiscordNotifications.cs#L11) |
| <a id="symbol-src-listingdiscordnotifications-cs-12-a6599d6d02"></a><code>IDiscordNotificationTransport.SendCompletion</code><br><code>Task SendCompletion(DiscordNotificationTarget target, CancellationToken cancellation)</code> | 인터페이스 계약 | 검증한 목적지에 고정 완료 메시지를1회 보내는 계약이다. | [L12](../../src/ListingDiscordNotifications.cs#L12) |
| <a id="symbol-src-listingdiscordnotifications-cs-34-a8d991e3f8"></a><code>ListingDiscordNotifications.Status</code><br><code>string Status</code> | 계산 속성 | 마지막 설정/전송 상태를 lock으로 읽는다. | [L34](../../src/ListingDiscordNotifications.cs#L34) |
| <a id="symbol-src-listingdiscordnotifications-cs-37-d12747bd73"></a><code>ListingDiscordNotifications.ListingDiscordNotifications</code><br><code>ListingDiscordNotifications(ulong app, IDiscordNotificationTransport transport, DiscordNotificationTarget? saved, Func&lt;DiscordNotificationTarget, CancellationToken, Task&gt; save, CancellationToken cancellation)</code> | 생성자 | 현재 앱과 일치하는 저장 목적지만 복원하고 저장 callback·transport·취소 수명을 준비한다. | [L37](../../src/ListingDiscordNotifications.cs#L37) |
| <a id="symbol-src-listingdiscordnotifications-cs-47-21d2190816"></a><code>ListingDiscordNotifications.SelectChannel</code><br><code>Task&lt;string&gt; SelectChannel(ulong guildId, ulong channelId, CancellationToken cancellation)</code> | 메서드 | DM을 거부하고 채널 확인→Framework 저장→활성 목적지 변경을 직렬 처리한다. 실패 시 이전 목적지를 유지한다. 자체적으로 테스트/알림 메시지를 보내지 않는다. | [L47](../../src/ListingDiscordNotifications.cs#L47) |
| <a id="symbol-src-listingdiscordnotifications-cs-79-c8a3174919"></a><code>ListingDiscordNotifications.Observe</code><br><code>void Observe(bool afkEnabled, ulong selfId, long completionRevision, bool isFull)</code> | 메서드 | AFK 활성·본인 CID·완료 세대·현재 가득 참을 관찰한다. 기존 작업을 취소하거나 연결/목적지가 준비되면1개 비동기 전송을 예약한다. 같은 완료 세대/목적지의 이미 시도한 POST는 반복하지 않는다. | [L79](../../src/ListingDiscordNotifications.cs#L79) |
| <a id="symbol-src-listingdiscordnotifications-cs-97-23ac209d67"></a><code>ListingDiscordNotifications.Deliver</code><br><code>Task Deliver(DiscordNotificationTarget selected, ulong observedSelf, long observedRevision, CancellationTokenSource budget)</code> | 메서드 | 취소 예산 안에서 채널을 다시 확인하고 최신 관측/목적지를 재검증한 뒤 POST 전에 완료 세대를 소비한다. 전송 실패/불확실성은 표시하되 자동 재전송하지 않는다. POST 전 취소는 후속 정상 명단에서 다시 시도할 수 있다. | [L97](../../src/ListingDiscordNotifications.cs#L97) |
| <a id="symbol-src-listingdiscordnotifications-cs-132-91178ade7c"></a><code>ListingDiscordNotifications.SetResult</code><br><code>void SetResult(string message, DiscordNotificationTarget selected, ulong observedSelf, long observedRevision)</code> | 메서드 | 아직 같은 AFK 완료/본인/목적지인 경우에만 비동기 결과를 상태로 반영한다. | [L132](../../src/ListingDiscordNotifications.cs#L132) |
| <a id="symbol-src-listingdiscordnotifications-cs-138-23eda7ccf3"></a><code>ListingDiscordNotifications.Dispose</code><br><code>void Dispose()</code> | 메서드 | 관측과 진행 중 전송을 종료하고 연결 취소를 전파한다. 이미 보낸 메시지를 지우지 않는다. | [L138](../../src/ListingDiscordNotifications.cs#L138) |

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
| <a id="symbol-src-listingdiscordpanel-render-cs-67-19801498ec"></a><code>ListingDiscordPanel.DrawActiveSettings</code><br><code>void DrawActiveSettings(ListingDiscordCard card, WebListing listing)</code> | 메서드 | 켜진 설정만 지정된 두 줄 태그로 표시한다. 비공개/초보자는 임무 앞 아이콘으로 표현한다. | [L67](../../src/ListingDiscordPanel.Render.cs#L67) |
| <a id="symbol-src-listingdiscordpanel-render-cs-69-988a86a7d7"></a><code>ListingDiscordPanel.DrawActiveSettings.Tags</code><br><code>string Tags(params string[] keys)</code> | 로컬 함수 | 활성화된 설정 label들을 대괄호 태그 문자열로 묶는다. | [L69](../../src/ListingDiscordPanel.Render.cs#L69) |
| <a id="symbol-src-listingdiscordpanel-render-cs-83-cccd4262f0"></a><code>ListingDiscordPanel.Remaining</code><br><code>string Remaining(WebListing listing)</code> | 메서드 | ReadAt와 읽은 잔여초를 고정 종료시각으로 바꿔 Discord 상대시간 timestamp를 만든다. 카드 다시 그리기만으로 시간이 리셋되지 않는다. | [L83](../../src/ListingDiscordPanel.Render.cs#L83) |
| <a id="symbol-src-listingdiscordpanel-render-cs-92-ceeaf46d8f"></a><code>ListingDiscordPanel.DrawJobDetails</code><br><code>void DrawJobDetails(ListingDiscordCard card, Panel p)</code> | 메서드 | 선택 그룹 슬롯의 허용 직업 텍스트와 참가 여부를 읽기 전용 상세 카드로 표시한다. | [L92](../../src/ListingDiscordPanel.Render.cs#L92) |
| <a id="symbol-src-listingdiscordpanel-render-cs-106-6018b8f4c5"></a><code>ListingDiscordPanel.Button</code><br><code>ButtonBuilder Button(Panel p, string action, string label, ButtonStyle style = ButtonStyle.Secondary, bool disabled = false)</code> | 메서드 | 동작 ID·label·style·비활성 상태를 가진 버튼을 구성한다. | [L106](../../src/ListingDiscordPanel.Render.cs#L106) |
| <a id="symbol-src-listingdiscordpanel-render-cs-110-de22ed546c"></a><code>ListingDiscordPanel.CloseButton</code><br><code>ButtonBuilder CloseButton(string session, bool disabled = false)</code> | 메서드 | session에 연결한 수정창 닫기 버튼을 만든다. overview nonce/편집 revision 만료와 무관하게 현재 session에서 복구할 수 있다. | [L110](../../src/ListingDiscordPanel.Render.cs#L110) |
| <a id="symbol-src-listingdiscordpanel-render-cs-112-636f28c70f"></a><code>ListingDiscordPanel.RecoveryMessage</code><br><code>DiscordScreen RecoveryMessage(string message, string session)</code> | 메서드 | 조회·갱신 진행/다른 요청 busy 화면에 닫기 버튼을 유지하는 V2 카드를 만든다. | [L112](../../src/ListingDiscordPanel.Render.cs#L112) |
| <a id="symbol-src-listingdiscordpanel-render-cs-117-67c88b0438"></a><code>ListingDiscordPanel.GroupMenu</code><br><code>void GroupMenu(ListingDiscordCard card, Panel p)</code> | 메서드 | 24/48인/친선 그룹 선택 메뉴를 만든다. 조회 overview의 전체 그룹 표시는 이 선택에 제한되지 않는다. | [L117](../../src/ListingDiscordPanel.Render.cs#L117) |
| <a id="symbol-src-listingdiscordpanel-render-cs-124-ef0b3946c6"></a><code>ListingDiscordPanel.Setting</code><br><code>string Setting(Panel p, string key)</code> | 메서드 | 필드별 원래 값/대기 값을 편집 목록 한 항목으로 표시한다. | [L124](../../src/ListingDiscordPanel.Render.cs#L124) |
| <a id="symbol-src-listingdiscordpanel-render-cs-126-ed4a061efa"></a><code>ListingDiscordPanel.Pending</code><br><code>void Pending(ListingDiscordCard card, Panel p)</code> | 메서드 | 해당 필드가 원본과 다른 대기 변경인지 표시한다. | [L126](../../src/ListingDiscordPanel.Render.cs#L126) |
| <a id="symbol-src-listingdiscordpanel-render-cs-133-f7e24014b7"></a><code>ListingDiscordPanel.DrawSettings</code><br><code>void DrawSettings(ListingDiscordCard card, Panel p)</code> | 메서드 | 목적·모집 참가 조건·임무 찾기·모집 범위·모집 역할 순으로 설정 편집 카드를 그린다. | [L133](../../src/ListingDiscordPanel.Render.cs#L133) |
| <a id="symbol-src-listingdiscordpanel-render-cs-157-6db130c4dd"></a><code>ListingDiscordPanel.DrawSettings.SettingsSection</code><br><code>void SettingsSection(string title, params string[] keys)</code> | 로컬 함수 | 설정 섹션 제목/본문/수정 버튼을 공통 형식으로 추가한다. | [L157](../../src/ListingDiscordPanel.Render.cs#L157) |
| <a id="symbol-src-listingdiscordpanel-render-cs-164-f430959cca"></a><code>ListingDiscordPanel.DrawJobs</code><br><code>void DrawJobs(ListingDiscordCard card, Panel p)</code> | 메서드 | 자리와 직업 분류별 선택 메뉴 및 방어·회복·공격 preset을 그린다. | [L164](../../src/ListingDiscordPanel.Render.cs#L164) |
| <a id="symbol-src-listingdiscordpanel-render-cs-193-6cbde95d2e"></a><code>ListingDiscordCard</code><br><code>ListingDiscordCard(Color color)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L193](../../src/ListingDiscordPanel.Render.cs#L193) |
| <a id="symbol-src-listingdiscordpanel-render-cs-198-71af14a0af"></a><code>ListingDiscordCard.Display</code><br><code>TextDisplayBuilder Display(string value)</code> | 메서드 | 한 카드에 누적된 표시 길이를 확인한다. | [L198](../../src/ListingDiscordPanel.Render.cs#L198) |
| <a id="symbol-src-listingdiscordpanel-render-cs-203-9d901ae7b9"></a><code>ListingDiscordCard.Text</code><br><code>void Text(string value)</code> | 메서드 | 남은 카드 표시 예산 안에서 TextDisplay component를 추가한다. | [L203](../../src/ListingDiscordPanel.Render.cs#L203) |
| <a id="symbol-src-listingdiscordpanel-render-cs-204-bd22b566ed"></a><code>ListingDiscordCard.Section</code><br><code>void Section(string value, ButtonBuilder button)</code> | 메서드 | 텍스트와 보조 버튼 accessory를 가진 Section component를 추가한다. | [L204](../../src/ListingDiscordPanel.Render.cs#L204) |
| <a id="symbol-src-listingdiscordpanel-render-cs-205-3f848d6c6e"></a><code>ListingDiscordCard.Divider</code><br><code>void Divider()</code> | 메서드 | 시각 구분용 separator를 넣는다. | [L205](../../src/ListingDiscordPanel.Render.cs#L205) |
| <a id="symbol-src-listingdiscordpanel-render-cs-206-64f8bfb128"></a><code>ListingDiscordCard.Row</code><br><code>void Row(params IMessageComponentBuilder[] controls)</code> | 메서드 | 버튼/select 등을 ActionRow에 넣는다. | [L206](../../src/ListingDiscordPanel.Render.cs#L206) |
| <a id="symbol-src-listingdiscordpanel-render-cs-207-86fcd90b0c"></a><code>ListingDiscordCard.Build</code><br><code>DiscordScreen Build()</code> | 메서드 | 모아둔 component를 Container로 완성한다. | [L207](../../src/ListingDiscordPanel.Render.cs#L207) |
| <a id="symbol-src-listingdiscordpanel-render-cs-208-96e9b9d5b2"></a><code>ListingDiscordCard.Message</code><br><code>DiscordScreen Message(string value)</code> | 메서드 | 간단한 텍스트 응답을 Components V2 메시지로 만든다. | [L208](../../src/ListingDiscordPanel.Render.cs#L208) |
| <a id="symbol-src-listingdiscordpanel-render-cs-209-b82d53e47b"></a><code>ListingDiscordCard.Walk</code><br><code>IEnumerable&lt;IMessageComponent&gt; Walk(IEnumerable&lt;IMessageComponent&gt; components)</code> | 메서드 | 중첩 Container/Section/Row component를 재귀 순회한다. 컨트롤 존재 여부와 테스트 검사에 사용한다. | [L209](../../src/ListingDiscordPanel.Render.cs#L209) |

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
| <a id="symbol-src-listingdiscordpanel-cs-18-e45534f430"></a><code>DiscordWork</code><br><code>DiscordWork(bool Query, string Id, string Session, byte[]? Body, long RecoveryRevision = 0)</code> | 주 생성자/모델 | 조회 또는 apply 작업의 요청 ID·session·body 등 실행 대상을 묶는다. | [L18](../../src/ListingDiscordPanel.cs#L18) |
| <a id="symbol-src-listingdiscordpanel-cs-19-92b8a3e0d5"></a><code>DiscordScreen</code><br><code>DiscordScreen(string Text, MessageComponent Components)</code> | 주 생성자/모델 | 텍스트와 Components V2 컴포넌트를 함께 보관한다. HasControls는 버튼/select 존재를 판정하며, 메시지 교체 여부는 별도 DiscordDecision에 있다. | [L19](../../src/ListingDiscordPanel.cs#L19) |
| <a id="symbol-src-listingdiscordpanel-cs-21-8a5ebe0b27"></a><code>DiscordScreen.HasControls</code><br><code>bool HasControls</code> | 계산 속성 | 중첩 카드 component에 사용자 조작 컨트롤이 있는지 검사한다. 계산식: <code>ListingDiscordCard.Walk(Components.Components).Any(c =&gt; c is ButtonComponent or SelectMenuComponent)</code>. | [L21](../../src/ListingDiscordPanel.cs#L21) |
| <a id="symbol-src-listingdiscordpanel-cs-23-d3fde9e21e"></a><code>DiscordDecision</code><br><code>DiscordDecision(DiscordScreen? Screen = null, Modal? Modal = null, DiscordWork? Work = null, bool Replace = true)</code> | 주 생성자/모델 | 입력을 처리한 결과로 화면/modal/지연 게임 작업 중 무엇을 수행할지 전달한다. | [L23](../../src/ListingDiscordPanel.cs#L23) |
| <a id="symbol-src-listingdiscordpanel-cs-28-250bbc9c46"></a><code>ListingDiscordPanel</code><br><code>ListingDiscordPanel(ListingWebBridge bridge, ulong applicationId, ulong ownerId, ListingDiscordIcons? iconCatalog = null, Func&lt;string&gt;? notificationStatus = null)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L28](../../src/ListingDiscordPanel.cs#L28) |
| <a id="symbol-src-listingdiscordpanel-cs-35-50d62dd6b3"></a><code>ListingDiscordPanel.Panel</code><br><code>Panel(WebView view, PanelKind kind = PanelKind.Overview)</code> | 주 생성자/모델 | 조회/수정/상세/결과 카드의 nonce·만료·원본·초안·선택 그룹/자리·결과 페이지를 분리해 보관한다. | [L35](../../src/ListingDiscordPanel.cs#L35) |
| <a id="symbol-src-listingdiscordpanel-cs-39-6f40e17835"></a><code>ListingDiscordPanel.Panel.Editing</code><br><code>bool Editing</code> | 계산 속성 | 현재 편집 원본/초안 상태가 존재하는지 검사한다. 계산식: <code>Kind == PanelKind.Editor</code>. | [L39](../../src/ListingDiscordPanel.cs#L39) |
| <a id="symbol-src-listingdiscordpanel-cs-60-51f8dfa8c3"></a><code>ListingDiscordPanel.Authorized</code><br><code>bool Authorized(DiscordInput input)</code> | 메서드 | interaction user ID와 application ID가 이 개인 봇의 허용 owner/application과 같은지 확인한다. | [L60](../../src/ListingDiscordPanel.cs#L60) |
| <a id="symbol-src-listingdiscordpanel-cs-61-ea12422607"></a><code>ListingDiscordPanel.RequestId</code><br><code>string RequestId(ulong id)</code> | 메서드 | Discord interaction ID를 SHA-256 기반 deterministic GUID로 변환해 재처리 시 동일한 Bridge 요청 ID를 사용한다. | [L61](../../src/ListingDiscordPanel.cs#L61) |
| <a id="symbol-src-listingdiscordpanel-cs-62-35c7c1447f"></a><code>ListingDiscordPanel.Error</code><br><code>DiscordDecision Error(string message)</code> | 메서드 | 오류 메시지 전용 화면을 Replace=false 결정으로 반환해 기존 카드를 덮어쓰지 않는다. 현재 패널의 재렌더링은 하지 않는다. | [L62](../../src/ListingDiscordPanel.cs#L62) |
| <a id="symbol-src-listingdiscordpanel-cs-63-8dc6231ae7"></a><code>ListingDiscordPanel.Current</code><br><code>bool Current(Panel p, WebView view, long now)</code> | 메서드 | 카드의 만료 시각·session·editor revision을 현재 View와 비교한다. custom ID의 nonce 검사는 Handle에서 별도로 수행한다. | [L63](../../src/ListingDiscordPanel.cs#L63) |
| <a id="symbol-src-listingdiscordpanel-cs-66-7b5fe33162"></a><code>ListingDiscordPanel.Handle</code><br><code>DiscordDecision Handle(DiscordInput input, long now)</code> | 메서드 | 조회·갱신·새 수정 카드·직업 상세·결과 페이지·설정 modal·그룹/자리/직업 선택을 라우팅한다. 적용 시 변경을 strict JSON body로 만들고 현재 초안을 소비한다. | [L66](../../src/ListingDiscordPanel.cs#L66) |
| <a id="symbol-src-listingdiscordpanel-cs-167-0a862535e0"></a><code>ListingDiscordPanel.Start</code><br><code>WebReply Start(DiscordWork work, long now)</code> | 메서드 | DiscordWork를 공통 query/apply queue에 전달하며 캡처한 recovery revision을 같은 lock에서 검사하도록 한다. | [L167](../../src/ListingDiscordPanel.cs#L167) |
| <a id="symbol-src-listingdiscordpanel-cs-168-fda0bbca1d"></a><code>ListingDiscordPanel.Progress</code><br><code>WebReply Progress(DiscordWork work)</code> | 메서드 | 앞서 발행한 요청 ID의 query/operation 결과를 읽는다. 명령을 재발송하지 않는다. | [L168](../../src/ListingDiscordPanel.cs#L168) |
| <a id="symbol-src-listingdiscordpanel-cs-169-736d84af24"></a><code>ListingDiscordPanel.Complete</code><br><code>DiscordDecision Complete(long now, string notice = "")</code> | 메서드 | 작업 결과를 panel과 조회 카드에 반영하고 전체 결과 텍스트를 별도 페이지로 보관한다. | [L169](../../src/ListingDiscordPanel.cs#L169) |
| <a id="symbol-src-listingdiscordpanel-cs-186-eab7df6e4d"></a><code>ListingDiscordPanel.Clear</code><br><code>void Clear()</code> | 메서드 | 편집 원본·nonce·선택·PIN 포함 초안·최근 결과 상태를 지운다. | [L186](../../src/ListingDiscordPanel.cs#L186) |
| <a id="symbol-src-listingdiscordpanel-cs-187-cedcc2165c"></a><code>ListingDiscordPanel.One</code><br><code>bool One(DiscordInput input, out string value)</code> | 메서드 | 단일 select 값이 정확히 하나인지 검사해 읽는다. | [L187](../../src/ListingDiscordPanel.cs#L187) |
| <a id="symbol-src-listingdiscordpanel-cs-188-032d328e14"></a><code>ListingDiscordPanel.Original</code><br><code>JsonNode? Original(Panel p, string key)</code> | 메서드 | 선택 필드의 원래 값을 editor snapshot에서 가져온다. | [L188](../../src/ListingDiscordPanel.cs#L188) |
| <a id="symbol-src-listingdiscordpanel-cs-189-4f7f9301f9"></a><code>ListingDiscordPanel.Value</code><br><code>JsonNode? Value(Panel p, string key)</code> | 메서드 | 대기 편집이 있으면 그 값을, 없으면 원래 값을 반환한다. | [L189](../../src/ListingDiscordPanel.cs#L189) |
| <a id="symbol-src-listingdiscordpanel-cs-190-666bffbb38"></a><code>ListingDiscordPanel.Enabled</code><br><code>bool Enabled(Panel p, string key)</code> | 메서드 | 설정 값을 bool 표시 상태로 해석한다. | [L190](../../src/ListingDiscordPanel.cs#L190) |
| <a id="symbol-src-listingdiscordpanel-cs-191-697063be67"></a><code>ListingDiscordPanel.FlagChange</code><br><code>JsonObject FlagChange(string key, bool enabled)</code> | 메서드 | 토글을 field/value JSON으로 변환한다. ConditionFlags는 bool, 그 외 flag는 정수0/1로 인코딩한다. 원래 값과 비교해 대기 변경을 제거하는 책임은 Set에 있다. | [L191](../../src/ListingDiscordPanel.cs#L191) |
| <a id="symbol-src-listingdiscordpanel-cs-192-066aef39d0"></a><code>ListingDiscordPanel.Set</code><br><code>void Set(Panel p, string key, JsonObject change)</code> | 메서드 | 원래 값과 같으면 대기 변경을 제거하고 다르면 해당 field key만 교체한다. | [L192](../../src/ListingDiscordPanel.cs#L192) |
| <a id="symbol-src-listingdiscordpanel-cs-199-21b11cbb81"></a><code>ListingDiscordPanel.GroupSlots</code><br><code>WebEditSlot[] GroupSlots(Panel p)</code> | 메서드 | 현재 선택한 물리 그룹의 등록된 slot만 반환한다. | [L199](../../src/ListingDiscordPanel.cs#L199) |
| <a id="symbol-src-listingdiscordpanel-cs-206-98105606ea"></a><code>ListingDiscordPanel.SelectedJobs</code><br><code>HashSet&lt;uint&gt; SelectedJobs(Panel p)</code> | 메서드 | 대기 직업 변경 또는 원래 슬롯에서 현재 선택된 직업 ID 집합을 얻는다. | [L206](../../src/ListingDiscordPanel.cs#L206) |
| <a id="symbol-src-listingdiscordpanel-cs-210-b35fb733d0"></a><code>ListingDiscordPanel.JobGroup</code><br><code>int JobGroup(uint id)</code> | 메서드 | 잡과 클래스를 방어·회복·근거리·원거리 물리·원거리 마법의 5개 선택 그룹에 대응시킨다. | [L210](../../src/ListingDiscordPanel.cs#L210) |
| <a id="symbol-src-listingdiscordpanel-cs-218-47ac99285e"></a><code>ListingDiscordPanel.Preset</code><br><code>bool Preset(string name, uint id)</code> | 메서드 | 방어/회복/공격 등의 일괄 선택을 잡과 클래스 모두에 적용한다. | [L218](../../src/ListingDiscordPanel.cs#L218) |
| <a id="symbol-src-listingdiscordpanel-cs-222-e7cbe3c620"></a><code>ListingDiscordPanel.Id</code><br><code>string Id(Panel p, string action)</code> | 메서드 | panel nonce/동작을 포함한 component custom ID를 구성한다. | [L222](../../src/ListingDiscordPanel.cs#L222) |
| <a id="symbol-src-listingdiscordpanel-cs-223-b27fa8b837"></a><code>ListingDiscordPanel.Label</code><br><code>string Label(string key)</code> | 메서드 | 필드 ID를 한국어 편집 항목명으로 바꾼다. | [L223](../../src/ListingDiscordPanel.cs#L223) |
| <a id="symbol-src-listingdiscordpanel-cs-224-910ebfb015"></a><code>ListingDiscordPanel.DisplayValue</code><br><code>string DisplayValue(Panel p, string key, bool original = false)</code> | 메서드 | 필드별 숫자/토글/규칙을 사용자용 값으로 표시한다. 비공개 PIN은 드러내지 않는다. | [L224](../../src/ListingDiscordPanel.cs#L224) |
| <a id="symbol-src-listingdiscordpanel-cs-233-14357bbec3"></a><code>ListingDiscordPanel.Options</code><br><code>(string Value, string Label)[] Options(string key)</code> | 메서드 | 허용된 label/value 목록을 Discord select option으로 구성한다. | [L233](../../src/ListingDiscordPanel.cs#L233) |
| <a id="symbol-src-listingdiscordpanel-cs-239-ac72ede9d9"></a><code>ListingDiscordPanel.Escape</code><br><code>string Escape(string value)</code> | 메서드 | Discord Markdown에 영향을 주는 문자를 escape한다. | [L239](../../src/ListingDiscordPanel.cs#L239) |
| <a id="symbol-src-listingdiscordpanel-cs-245-0422c57935"></a><code>ListingDiscordPanel.Limit</code><br><code>string Limit(string text, int max = 1900)</code> | 메서드 | 최대 길이에 맞춰 문자열을 줄이되 Unicode surrogate/rune 경계를 보존한다. 긴 작업 결과 본문은 별도 PaginateResult 경로를 사용한다. | [L245](../../src/ListingDiscordPanel.cs#L245) |

<a id="file-src-listingdiscordsession-chat-cs"></a>
## src/ListingDiscordSession.Chat.cs

[소스 파일](../../src/ListingDiscordSession.Chat.cs) · 명명된 항목/진입점 1개

기존 owner/app 인증 뒤 실행되는 채팅 설정 interaction. 게임 조회/수정/카드 상태를 바꾸지 않고 설정 명령 결과만 본인에게 응답한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordsession-chat-cs-5-d86aa115c1"></a><code>ListingDiscordSession.ConfigureChat</code><br><code>Task ConfigureChat(DiscordInput input, IDiscordResponse response)</code> | 메서드 | 중복 ID·명령 파싱·controller 가용성을 검사하고 Defer 성공 후 설정/상태를 처리한다. 저장 응답을 잃어도 게임 작업이나 메시지를 재실행하지 않으며 상태 명령으로 확인하도록 안내한다. | [L5](../../src/ListingDiscordSession.Chat.cs#L5) |

<a id="file-src-listingdiscordsession-notifications-cs"></a>
## src/ListingDiscordSession.Notifications.cs

[소스 파일](../../src/ListingDiscordSession.Notifications.cs) · 명명된 항목/진입점 1개

알림 채널 선택 interaction. Process의 owner/app 인증 뒤 session/중복/ACK를 확인하며 서버·채널 ID는 SDK가 읽은 현재 interaction에서만 가져온다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingdiscordsession-notifications-cs-5-a80baad496"></a><code>ListingDiscordSession.SelectNotificationChannel</code><br><code>Task SelectNotificationChannel(DiscordInput input, IDiscordResponse response)</code> | 메서드 | 현 세션과 중복 interaction을 검사하고 ACK가 성공한 뒤 채널 검사/저장을 실행한다. 일반 query/apply handling과 독립이며 알림 전송 또는 native 게임 조작은 하지 않는다. | [L5](../../src/ListingDiscordSession.Notifications.cs#L5) |

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
| <a id="symbol-src-listingdiscordsession-cs-16-558ce13e67"></a><code>ListingDiscordSession</code><br><code>ListingDiscordSession(ListingWebBridge bridge, ulong appId, ulong ownerId, CancellationToken cancellation, Func&lt;long&gt;? clock = null, ListingDiscordIcons? iconCatalog = null, ListingDiscordNotifications? notifications = null, ListingDiscordChat? chat = null)</code> | 주 생성자/모델 | 해당 편집의 원본/요청/expected, 불변식, 컨트롤 주소와 UI 진행 상태를 시작 시점에 고정한다. 이후 guard와 1회 실행 counter의 기준이다. | [L16](../../src/ListingDiscordSession.cs#L16) |
| <a id="symbol-src-listingdiscordsession-cs-24-945cc0b4ed"></a><code>ListingDiscordSession.Now</code><br><code>long Now</code> | 계산 속성 | 단조 시계 Environment.TickCount64를 반환해 UI session 만료/작업 대기를 계산한다. 계산식: <code>clock?.Invoke() ?? Environment.TickCount64</code>. | [L24](../../src/ListingDiscordSession.cs#L24) |
| <a id="symbol-src-listingdiscordsession-cs-25-bd81d663ff"></a><code>ListingDiscordSession.Message</code><br><code>DiscordScreen Message(string text)</code> | 메서드 | 단순 메시지를 DiscordScreen 형태로 만든다. | [L25](../../src/ListingDiscordSession.cs#L25) |
| <a id="symbol-src-listingdiscordsession-cs-27-f98d431edd"></a><code>ListingDiscordSession.Process</code><br><code>Task Process(DiscordInput input, IDiscordResponse response)</code> | 메서드 | owner/app를 먼저 검사한다. pvkr-chat 설정, 수정창 닫기와 알림 목적지 경로는 독립 처리하고 나머지 기존 카드 조작은 세션/중복/ACK·handling 경계를 따른다. | [L27](../../src/ListingDiscordSession.cs#L27) |
| <a id="symbol-src-listingdiscordsession-cs-90-c7c006b1ac"></a><code>ListingDiscordSession.ClearIfIdle</code><br><code>void ClearIfIdle()</code> | 메서드 | 진행 중 interaction이 없을 때 panel 상태를 비우고, 진행 중이면 안전한 정리를 예약한다. | [L90](../../src/ListingDiscordSession.cs#L90) |
| <a id="symbol-src-listingdiscordsession-cs-95-c2a4384b56"></a><code>ListingDiscordSession.SafeScreen</code><br><code>Task SafeScreen(IDiscordResponse response, DiscordScreen screen, bool deferred = false)</code> | 메서드 | 응답 갱신 실패가 상위 처리 흐름을 깨지 않도록 best-effort로 화면을 보낸다. | [L95](../../src/ListingDiscordSession.cs#L95) |
| <a id="symbol-src-listingdiscordsession-cs-100-f36374a5d9"></a><code>ListingDiscordSession.Pending</code><br><code>bool Pending(WebReply reply)</code> | 메서드 | query·apply·recovery 결과가 queued/running인지 판정한다. | [L100](../../src/ListingDiscordSession.cs#L100) |
| <a id="symbol-src-listingdiscordsession-cs-101-e7e114d874"></a><code>ListingDiscordSession.ResultSummary</code><br><code>string ResultSummary(WebOperation operation)</code> | 메서드 | 조회/갱신 응답의 최종 요약을 카드에 사용할 문자열로 변환한다. | [L101](../../src/ListingDiscordSession.cs#L101) |
| <a id="symbol-src-listingdiscordsession-cs-103-2a8bb48849"></a><code>ListingDiscordSession.Describe</code><br><code>string Describe(WebReply reply)</code> | 메서드 | 작업 상태와 항목별 적용/실패 결과를 사용자에게 설명한다. 긴 결과는 생략하지 않고 별도 결과 카드 경로로 넘긴다. | [L103](../../src/ListingDiscordSession.cs#L103) |

<a id="file-src-plugin-discord-cs"></a>
## src/Plugin.Discord.cs

[소스 파일](../../src/Plugin.Discord.cs) · 명명된 항목/진입점 16개

사용자 설정과 Discord client 수명 연결. 재시작 복원은 한 번, 명시 끄기는 자동 연결도 끄며 Dispose는 저장 토큰을 지우지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-plugin-discord-cs-11-c18d138fb6"></a><code>Plugin.DiscordEnabled</code><br><code>bool DiscordEnabled</code> | 계산 속성 | Discord client가 실행 중인지 반환한다. 계산식: <code>discordClient?.Running == true</code>. | [L11](../../src/Plugin.Discord.cs#L11) |
| <a id="symbol-src-plugin-discord-cs-12-a8c0c2f707"></a><code>Plugin.DiscordStatus</code><br><code>string DiscordStatus</code> | 계산 속성 | 봇 연결/이모지 진행/오류 상태를 반환한다. 계산식: <code>discordClient?.Status ?? discordStatus</code>. | [L12](../../src/Plugin.Discord.cs#L12) |
| <a id="symbol-src-plugin-discord-cs-13-5a040469aa"></a><code>Plugin.DiscordNotificationStatus</code><br><code>string DiscordNotificationStatus</code> | 계산 속성 | 연결 클라이언트의 알림 채널/전송 상태를 인게임 UI에 전달한다. 계산식: <code>discordClient?.NotificationStatus ?? ""</code>. | [L13](../../src/Plugin.Discord.cs#L13) |
| <a id="symbol-src-plugin-discord-cs-14-080bbe9feb"></a><code>Plugin.DiscordInviteUrl</code><br><code>string DiscordInviteUrl</code> | 계산 속성 | 현재 봇 설치 링크를 반환한다. 계산식: <code>discordClient?.InviteUrl ?? ""</code>. | [L14](../../src/Plugin.Discord.cs#L14) |
| <a id="symbol-src-plugin-discord-cs-15-a0432bf1a1"></a><code>Plugin.HasSavedDiscordToken</code><br><code>bool HasSavedDiscordToken</code> | 계산 속성 | 저장 ciphertext가 존재하는지 반환한다. 복호화 가능 여부를 미리 보장하지 않는다. 계산식: <code>(PluginInterface.GetPluginConfig() as RelayConfiguration)?.HasSavedDiscordToken == true</code>. | [L15](../../src/Plugin.Discord.cs#L15) |
| <a id="symbol-src-plugin-discord-cs-16-276350ac16"></a><code>Plugin.RememberDiscordToken</code><br><code>bool RememberDiscordToken</code> | 계산 속성 | 토큰 기억 설정을 읽거나 변경한다. 계산식: <code>(PluginInterface.GetPluginConfig() as RelayConfiguration)?.RememberDiscordToken ?? true</code>. | [L16](../../src/Plugin.Discord.cs#L16) |
| <a id="symbol-src-plugin-discord-cs-18-2f833b12e1"></a><code>Plugin.RestoreDiscordConnection</code><br><code>void RestoreDiscordConnection()</code> | 메서드 | 최초 update에서 기억/자동 연결 옵션과 저장 ciphertext가 있으면 복호화 후 StartDiscord를 요청한다. 실패를 매 프레임 재시도하지 않는다. | [L18](../../src/Plugin.Discord.cs#L18) |
| <a id="symbol-src-plugin-discord-cs-30-3ee88f8393"></a><code>Plugin.StartDiscord</code><br><code>bool StartDiscord(string token, bool remember)</code> | 메서드 | 동일 토큰 중복 연결을 피하고 웹 연결을 정리한 뒤 토큰/자동 연결 선호·완료 알림 목적지·채팅 설정과 저장 callback으로 개인 봇을 시작한다. 단계별 안전한 오류를 표시한다. | [L30](../../src/Plugin.Discord.cs#L30) |
| <a id="symbol-src-plugin-discord-cs-62-021bb3930f"></a><code>Plugin.SaveDiscordNotificationTarget</code><br><code>Task SaveDiscordNotificationTarget(DiscordNotificationTarget target, CancellationToken cancellation)</code> | 메서드 | Framework 스레드로 이동해 취소/종료를 재검사한 뒤 봇·서버·채널 ID만 설정에 저장한다. 기존 토큰/AFK 옵션은 보존한다. | [L62](../../src/Plugin.Discord.cs#L62) |
| <a id="symbol-src-plugin-discord-cs-70-e06ab183e1"></a><code>Plugin.UpdateDiscordCompletion</code><br><code>void UpdateDiscordCompletion()</code> | 메서드 | AFK의 정원 도달 세대와 현재 완료·본인 CID만 Discord에 전달한다. 모집글 재조회 성공을 전송 조건으로 요구하지 않는다. | [L70](../../src/Plugin.Discord.cs#L70) |
| <a id="symbol-src-plugin-discord-cs-73-8f90c90417"></a><code>Plugin.ProtectDiscordToken</code><br><code>string ProtectDiscordToken(string token)</code> | 메서드 | Windows CurrentUser DPAPI로 토큰을 보호해 저장 가능한 문자열로 만든다. | [L73](../../src/Plugin.Discord.cs#L73) |
| <a id="symbol-src-plugin-discord-cs-79-983f7f2a7c"></a><code>Plugin.UnprotectDiscordToken</code><br><code>string UnprotectDiscordToken(string encrypted)</code> | 메서드 | 저장 ciphertext를 같은 Windows 사용자 범위로 복호화한다. 다른 계정/손상 데이터는 실패 처리한다. | [L79](../../src/Plugin.Discord.cs#L79) |
| <a id="symbol-src-plugin-discord-cs-85-18e9ffb8d6"></a><code>Plugin.DisableDiscordAutoConnect</code><br><code>bool DisableDiscordAutoConnect()</code> | 메서드 | 명시 다른 연결 방식 선택 시 DiscordAutoConnect=false를 저장한다. 저장 실패를 호출자에게 전달한다. | [L85](../../src/Plugin.Discord.cs#L85) |
| <a id="symbol-src-plugin-discord-cs-98-058e2b7474"></a><code>Plugin.TurnOffDiscord</code><br><code>void TurnOffDiscord()</code> | 메서드 | 자동 재연결 설정을 끄고 현재 Discord 연결을 종료한다. | [L98](../../src/Plugin.Discord.cs#L98) |
| <a id="symbol-src-plugin-discord-cs-103-0b0a424e92"></a><code>Plugin.ForgetDiscordToken</code><br><code>void ForgetDiscordToken()</code> | 메서드 | 설정의 암호화 토큰과 자동 연결 플래그를 제거한다. 현재 살아 있는 연결과 저장 삭제의 의미를 구분한다. | [L103](../../src/Plugin.Discord.cs#L103) |
| <a id="symbol-src-plugin-discord-cs-112-6774f0e135"></a><code>Plugin.StopDiscord</code><br><code>void StopDiscord()</code> | 메서드 | 현재 client를 교체/종료하고 그 연결이 소유한 조회·갱신만 취소한다. 수명 종료 중 토큰 기억 설정은 보존한다. | [L112](../../src/Plugin.Discord.cs#L112) |

<a id="file-src-relayconfiguration-cs"></a>
## src/RelayConfiguration.cs

[소스 파일](../../src/RelayConfiguration.cs) · 명명된 항목/진입점 8개

기존 설정 type identity를 유지하는 Version=1 설정 모델. 서버 주소, DPAPI ciphertext, 토큰 기억/자동 연결만 저장한다. PIN/모집글/relay 연결 키는 저장하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-relayconfiguration-cs-18-3afec7bff0"></a><code>RelayConfiguration.ReadNotificationTarget</code><br><code>DiscordNotificationTarget ReadNotificationTarget()</code> | 메서드 | 저장된 애플리케이션·서버·채널 ID를 관리형 목적지 값으로 읽는다. 실제 사용 전 앱 일치를 확인한다. | [L18](../../src/RelayConfiguration.cs#L18) |
| <a id="symbol-src-relayconfiguration-cs-20-a67fecb153"></a><code>RelayConfiguration.WriteNotificationTarget</code><br><code>void WriteNotificationTarget(DiscordNotificationTarget target)</code> | 메서드 | 세 알림 목적지 ID만 복사한다. 저장 실행은 게임 스레드의 Plugin이 담당한다. | [L20](../../src/RelayConfiguration.cs#L20) |
| <a id="symbol-src-relayconfiguration-cs-29-9e1ab69d59"></a><code>RelayConfiguration.ReadAfkSettings</code><br><code>AfkSettings ReadAfkSettings()</code> | 메서드 | 저장된 네 AFK 옵션을 정규화한 불변 정책으로 읽는다. 이전 JSON의 누락 값은 기본값을 사용한다. | [L29](../../src/RelayConfiguration.cs#L29) |
| <a id="symbol-src-relayconfiguration-cs-31-55eae34091"></a><code>RelayConfiguration.WriteAfkSettings</code><br><code>void WriteAfkSettings(AfkSettings settings)</code> | 메서드 | 정규화한 주기·재시도 옵션만 설정 객체에 복사한다. 토큰·연결 설정은 보존하고 저장 자체는 Plugin이 수행한다. | [L31](../../src/RelayConfiguration.cs#L31) |
| <a id="symbol-src-relayconfiguration-cs-37-ab90a7aeb2"></a><code>RelayConfiguration.HasSavedDiscordToken</code><br><code>bool HasSavedDiscordToken</code> | 계산 속성 | 저장 ciphertext가 존재하는지 반환한다. 복호화 가능 여부를 미리 보장하지 않는다. 계산식: <code>!string.IsNullOrEmpty(EncryptedDiscordToken)</code>. | [L37](../../src/RelayConfiguration.cs#L37) |
| <a id="symbol-src-relayconfiguration-cs-38-38e2226e1c"></a><code>RelayConfiguration.ShouldAutoConnectDiscord</code><br><code>bool ShouldAutoConnectDiscord</code> | 계산 속성 | DiscordAutoConnect와 저장 ciphertext 존재 여부를 합쳐 시작 시 연결 복원 대상인지 판정한다. PrepareDiscordConnection이 기억 옵션과 자동 연결을 함께 저장한다. 계산식: <code>DiscordAutoConnect &amp;&amp; HasSavedDiscordToken</code>. | [L38](../../src/RelayConfiguration.cs#L38) |
| <a id="symbol-src-relayconfiguration-cs-40-3f3dc84a58"></a><code>RelayConfiguration.PrepareDiscordConnection</code><br><code>bool PrepareDiscordConnection(string input, bool remember, Func&lt;string, string&gt; protect, Func&lt;string, string&gt; unprotect, out string token)</code> | 메서드 | 명시 token 또는 saved token 경로를 검증하고 protect/unprotect를 통해 새 설정을 준비한다. 암호화 실패 전에 기존 저장 설정을 변경하지 않는다. | [L40](../../src/RelayConfiguration.cs#L40) |
| <a id="symbol-src-relayconfiguration-cs-56-ee10a6666f"></a><code>RelayConfiguration.ForgetDiscordToken</code><br><code>void ForgetDiscordToken()</code> | 메서드 | EncryptedDiscordToken을 비우고 자동 연결을 끈다. | [L56](../../src/RelayConfiguration.cs#L56) |
