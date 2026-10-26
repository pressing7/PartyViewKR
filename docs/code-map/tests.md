# 테스트 함수·시나리오 지도

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

이 문서는 테스트를 실행한 보고서가 아니라 현재 소스에 등록된 검증 범위의 지도다. 관리형 runner의 481개 등록 시나리오, Node 테스트와 Python 테스트를 source 위치에 연결한다. 실제 게임·Windows DPAPI·Discord live 검증은 fake 테스트와 별개다.

기준: 0.3.52.0 소스. 기반 main `6e6e482b4e56a7c409c7e58fdb0ed5b2d1d24bce` 이후 상시 채팅·인게임 수신/멘션 토글·홈 서버 보완·점유 자리 직업 자동 정규화 검증 변경을 포함한다. 2026-10-26 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [relay/test/relay.test.mjs](#file-relay-test-relay-test-mjs)
- [tests/AfkCompletionTests.cs](#file-tests-afkcompletiontests-cs)
- [tests/AfkListingMonitorTests.cs](#file-tests-afklistingmonitortests-cs)
- [tests/AfkSettingsTests.cs](#file-tests-afksettingstests-cs)
- [tests/CompatibilityTests.cs](#file-tests-compatibilitytests-cs)
- [tests/DiscordSettingsTests.cs](#file-tests-discordsettingstests-cs)
- [tests/GameChatCaptureTests.cs](#file-tests-gamechatcapturetests-cs)
- [tests/ListingBatchTests.cs](#file-tests-listingbatchtests-cs)
- [tests/ListingBeginnerEditTests.cs](#file-tests-listingbeginneredittests-cs)
- [tests/ListingCompletionEditTests.cs](#file-tests-listingcompletionedittests-cs)
- [tests/ListingDescriptionEditTests.cs](#file-tests-listingdescriptionedittests-cs)
- [tests/ListingDescriptionRecoveryTests.cs](#file-tests-listingdescriptionrecoverytests-cs)
- [tests/ListingDiscordCardTests.cs](#file-tests-listingdiscordcardtests-cs)
- [tests/ListingDiscordChatSdkTests.cs](#file-tests-listingdiscordchatsdktests-cs)
- [tests/ListingDiscordChatTests.cs](#file-tests-listingdiscordchattests-cs)
- [tests/ListingDiscordConnectionTests.cs](#file-tests-listingdiscordconnectiontests-cs)
- [tests/ListingDiscordEmojiTests.cs](#file-tests-listingdiscordemojitests-cs)
- [tests/ListingDiscordInteractionTests.cs](#file-tests-listingdiscordinteractiontests-cs)
- [tests/ListingDiscordNotificationSdkTests.cs](#file-tests-listingdiscordnotificationsdktests-cs)
- [tests/ListingDiscordNotificationTests.cs](#file-tests-listingdiscordnotificationtests-cs)
- [tests/ListingDiscordResultTests.cs](#file-tests-listingdiscordresulttests-cs)
- [tests/ListingDiscordRosterTests.cs](#file-tests-listingdiscordrostertests-cs)
- [tests/ListingDiscordTests.cs](#file-tests-listingdiscordtests-cs)
- [tests/ListingEditorCriteriaTests.cs](#file-tests-listingeditorcriteriatests-cs)
- [tests/ListingFriendlyMatchTests.cs](#file-tests-listingfriendlymatchtests-cs)
- [tests/ListingItemLevelEditTests.cs](#file-tests-listingitemleveledittests-cs)
- [tests/ListingJobEditTests.cs](#file-tests-listingjobedittests-cs)
- [tests/ListingJobRulesTests.cs](#file-tests-listingjobrulestests-cs)
- [tests/ListingObjectiveEditTests.cs](#file-tests-listingobjectiveedittests-cs)
- [tests/ListingObjectiveUiTests.cs](#file-tests-listingobjectiveuitests-cs)
- [tests/ListingPrivateEditTests.cs](#file-tests-listingprivateedittests-cs)
- [tests/ListingRecoveryTests.cs](#file-tests-listingrecoverytests-cs)
- [tests/ListingRelayTests.cs](#file-tests-listingrelaytests-cs)
- [tests/ListingRoleButtonsTests.cs](#file-tests-listingrolebuttonstests-cs)
- [tests/ListingRoleComparisonTests.cs](#file-tests-listingrolecomparisontests-cs)
- [tests/ListingRoleNormalizationTests.cs](#file-tests-listingrolenormalizationtests-cs)
- [tests/ListingRuleEditTests.cs](#file-tests-listingruleedittests-cs)
- [tests/ListingSlotRouteTests.cs](#file-tests-listingslotroutetests-cs)
- [tests/ListingSlotsTests.cs](#file-tests-listingslotstests-cs)
- [tests/ListingSparseSlotsTests.cs](#file-tests-listingsparseslotstests-cs)
- [tests/ListingUniqueJobsEditTests.cs](#file-tests-listinguniquejobsedittests-cs)
- [tests/ListingWebEditTests.cs](#file-tests-listingwebedittests-cs)
- [tests/ListingWebTests.cs](#file-tests-listingwebtests-cs)
- [tests/Program.cs](#file-tests-program-cs)
- [tests/RelayFixture.cs](#file-tests-relayfixture-cs)
- [tests/WebBrowserFixture.cs](#file-tests-webbrowserfixture-cs)
- [tests/browser-smoke.cjs](#file-tests-browser-smoke-cjs)
- [tests/test_release.py](#file-tests-test-release-py)
- [tests/web-connection.test.cjs](#file-tests-web-connection-test-cjs)
- [tests/web-editor.test.cjs](#file-tests-web-editor-test-cjs)

<a id="file-relay-test-relay-test-mjs"></a>
## relay/test/relay.test.mjs

[소스 파일](../../relay/test/relay.test.mjs) · 명명된 항목/진입점 10개

Miniflare 로컬 Worker와 fake WebSocket 및 실제 .NET relay fixture로 인증·host 교체·frame/timeout·apply를 통합 검증한다. Cloudflare 배포나 Wrangler dev 서버를 띄우는 테스트가 아니다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-relay-test-relay-test-mjs-1-65043f1a90"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | 로컬 Miniflare Worker와 fake/실제 .NET relay fixture의3개 통합 test를 등록한다. 외부 배포 없이 test별 정리 callback으로 런타임·소켓·process를 종료한다. | [L1](../../relay/test/relay.test.mjs#L1) |
| <a id="symbol-relay-test-relay-test-mjs-12-f35203f3ac"></a><code>options</code><br><code>function options(extra = {})</code> | JS 함수/handler | Miniflare에 Worker/protocol 모듈, Durable Object·asset·테스트 secret binding을 제공할 설정을 만든다. | [L12](../../relay/test/relay.test.mjs#L12) |
| <a id="symbol-relay-test-relay-test-mjs-19-13fb3e8b02"></a><code>auth</code><br><code>(key = browser, id = room) =&gt; (</code> | JS 함수/handler | browser bearer와 room 인증 header를 만든다. | [L19](../../relay/test/relay.test.mjs#L19) |
| <a id="symbol-relay-test-relay-test-mjs-20-5e744b2406"></a><code>host</code><br><code>async function host(mf, key = secret, id = room, browserKey = browser)</code> | JS 함수/handler | Miniflare fetch에 host key/room/browser hash 및 Upgrade header를 넣어 fake host WebSocket을 연다. | [L20](../../relay/test/relay.test.mjs#L20) |
| <a id="symbol-relay-test-relay-test-mjs-24-9b436093d8"></a><code>wait</code><br><code>ms =&gt;</code> | JS 함수/handler | socket의 지정 이벤트를 제한 시간 안에서 기다린다. | [L24](../../relay/test/relay.test.mjs#L24) |
| <a id="symbol-relay-test-relay-test-mjs-42-44613048f7"></a><code>ws.on(message)</code><br><code>event =&gt;</code> | JS 함수/handler | fake host가 중계 request frame을 받았을 때 조건에 맞게 응답하거나 오류를 모사한다. | [L42](../../relay/test/relay.test.mjs#L42) |
| <a id="symbol-relay-test-relay-test-mjs-81-f4d09ad010"></a><code>ws.on(message)</code><br><code>event =&gt;</code> | JS 함수/handler | fake host가 중계 request frame을 받았을 때 조건에 맞게 응답하거나 오류를 모사한다. | [L81](../../relay/test/relay.test.mjs#L81) |
| <a id="symbol-relay-test-relay-test-mjs-116-cb0b318269"></a><code>until</code><br><code>async function until(fn, timeout = 15000)</code> | JS 함수/handler | 실제 .NET relay fixture와 Miniflare 연결이 원하는 상태가 될 때까지 제한 재확인한다. | [L116](../../relay/test/relay.test.mjs#L116) |
| <a id="symbol-relay-test-relay-test-mjs-123-ca6c3a8317"></a><code>request</code><br><code>(path, init = {}) =&gt;</code> | JS 함수/handler | 실제 bridge fixture로 relay API를 요청한다. | [L123](../../relay/test/relay.test.mjs#L123) |
| <a id="symbol-relay-test-relay-test-mjs-168-2c127b081c"></a><code>untilOffline</code><br><code>async function untilOffline(request)</code> | JS 함수/handler | host 종료 후 offline 응답이 나타날 때까지 관찰한다. | [L168](../../relay/test/relay.test.mjs#L168) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>external page navigation is public while relay APIs remain protected</code> | [L26](../../relay/test/relay.test.mjs#L26) |
| <code>relay authentication, route boundaries, WebSocket forwarding, timeout and replacement</code> | [L65](../../relay/test/relay.test.mjs#L65) |
| <code>real .NET relay executes existing batch once and retains results across a Worker restart</code> | [L105](../../relay/test/relay.test.mjs#L105) |

<a id="file-tests-afkcompletiontests-cs"></a>
## tests/AfkCompletionTests.cs

[소스 파일](../../tests/AfkCompletionTests.cs) · 명명된 항목/진입점 3개

모집 자동 종료 뒤 현재 명단과 기억한 등록 정원으로 완료 감지·중복 억제를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-afkcompletiontests-cs-5-5478b5f0a3"></a><code>AfkCompletionTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 모집 자동 종료 뒤 현재 명단과 기억한 등록 정원으로 완료 감지·중복 억제 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L5](../../tests/AfkCompletionTests.cs#L5) |
| <a id="symbol-tests-afkcompletiontests-cs-7-e0134d8c1c"></a><code>AfkCompletionTests.Run.Armed</code><br><code>(AfkListingMonitor Monitor, AfkFakeHost Host) Armed(int capacity = 8)</code> | 로컬 함수 | 아직 빈자리가 있는 본인 모집글의 성공 응답으로 정원을 기억한 monitor와 fake host를 준비한다. | [L7](../../tests/AfkCompletionTests.cs#L7) |
| <a id="symbol-tests-afkcompletiontests-cs-14-97c81dadbc"></a><code>AfkCompletionTests.Run.Roster</code><br><code>PartyRoster Roster(int count)</code> | 로컬 함수 | 자기 CID를 포함하는 지정 인원수의 유효 명단을 만든다. 직업/이름은 포함하지 않는다. | [L14](../../tests/AfkCompletionTests.cs#L14) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>AFK full roster notifies even after recruitment automatically ends and cannot be queried</code> | [L15](../../tests/AfkCompletionTests.cs#L15) |
| <code>AFK remembers registered capacity for ordinary sparse alliance and friendly spectator rosters</code> | [L20](../../tests/AfkCompletionTests.cs#L20) |
| <code>AFK failed or stale initial queries cannot arm completion from roster count alone</code> | [L28](../../tests/AfkCompletionTests.cs#L28) |
| <code>AFK full roster cancels only its in flight query before a failed result</code> | [L33](../../tests/AfkCompletionTests.cs#L33) |
| <code>AFK full recruitment suppresses duplicate alerts and periodic queries of the ended listing</code> | [L38](../../tests/AfkCompletionTests.cs#L38) |
| <code>AFK unreadable roster does not rearm completion but a verified vacancy does</code> | [L43](../../tests/AfkCompletionTests.cs#L43) |
| <code>AFK reset or character switch discards prior recruitment capacity</code> | [L50](../../tests/AfkCompletionTests.cs#L50) |
| <code>AFK newer manual own query can replace an ended recruitment capacity</code> | [L57](../../tests/AfkCompletionTests.cs#L57) |
| <code>AFK unverified manual detail cannot replace remembered recruitment capacity</code> | [L65](../../tests/AfkCompletionTests.cs#L65) |
| <code>AFK capacity is not inferred from oversized roster or invalid recruitment size</code> | [L70](../../tests/AfkCompletionTests.cs#L70) |

<a id="file-tests-afklistingmonitortests-cs"></a>
## tests/AfkListingMonitorTests.cs

[소스 파일](../../tests/AfkListingMonitorTests.cs) · 명명된 항목/진입점 9개

AFK CID 명단 정규화, 가입/탈퇴 변경 보존,60초 보완 조회, 실패 backoff, 요청 revision 소유권과 캐릭터 수명를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-afklistingmonitortests-cs-5-bb71cde680"></a><code>AfkListingMonitorTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 26개 AFK 회귀 시나리오를 등록해 정상/실패/재입장/명단 불확실성/소유권/수명 경계를 검사한다. native 읽기는 fake host로 대체한다. | [L5](../../tests/AfkListingMonitorTests.cs#L5) |
| <a id="symbol-tests-afklistingmonitortests-cs-7-11e81c8b34"></a><code>AfkListingMonitorTests.Run.Start</code><br><code>(AfkListingMonitor m, AfkFakeHost h) Start(bool complete = false, AfkSettings? settings = null)</code> | 로컬 함수 | AFK를 켜고 첫 query가 진행 중이거나 성공한 상태의 monitor와 fake host를 만든다. | [L7](../../tests/AfkListingMonitorTests.cs#L7) |
| <a id="symbol-tests-afklistingmonitortests-cs-179-790f994877"></a><code>AfkFakeHost.TryReadRoster</code><br><code>bool TryReadRoster(out PartyRoster? roster)</code> | 메서드 | 현재 fake 명단 또는 읽기 실패를 반환하고 호출 수를 세어 꺼진 상태의 무작업을 검증한다. | [L179](../../tests/AfkListingMonitorTests.cs#L179) |
| <a id="symbol-tests-afklistingmonitortests-cs-180-a8e787b252"></a><code>AfkFakeHost.QueryBlockReason</code><br><code>string? QueryBlockReason(bool ownQueryInProgress)</code> | 메서드 | 수정창/접속 차단과 다른 query busy를 모사한다. | [L180](../../tests/AfkListingMonitorTests.cs#L180) |
| <a id="symbol-tests-afklistingmonitortests-cs-181-0a0a717cb7"></a><code>AfkFakeHost.OwnListingMatches</code><br><code>bool OwnListingMatches(PartyRoster roster)</code> | 메서드 | fake 새 상세의 본인 검증 및 참가 CID 일치를 모사한다. | [L181](../../tests/AfkListingMonitorTests.cs#L181) |
| <a id="symbol-tests-afklistingmonitortests-cs-182-5b7bfaf756"></a><code>AfkFakeHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 메서드 | 시도 횟수·revision·busy를 갱신하고 거부 또는 접수 후 예외를 주입한다. | [L182](../../tests/AfkListingMonitorTests.cs#L182) |
| <a id="symbol-tests-afklistingmonitortests-cs-190-172201edfe"></a><code>AfkFakeHost.CancelQuery</code><br><code>void CancelQuery(long revision)</code> | 메서드 | 같은 revision의 busy query만 취소하여 이후 사용자 요청이 살아남는지 검증한다. | [L190](../../tests/AfkListingMonitorTests.cs#L190) |
| <a id="symbol-tests-afklistingmonitortests-cs-191-7acce34c5c"></a><code>AfkFakeHost.Complete</code><br><code>void Complete()</code> | 메서드 | fake 상세 응답의 revision을 증가시키고 현재 명단이 담긴 본인 결과를 제공한다. | [L191](../../tests/AfkListingMonitorTests.cs#L191) |
| <a id="symbol-tests-afklistingmonitortests-cs-192-e581d4fc7d"></a><code>AfkFakeHost.RosterOf</code><br><code>PartyRoster RosterOf(params ulong[] ids)</code> | 메서드 | 자기 CID10이 포함된 유효한 순수 명단 fixture를 생성한다. | [L192](../../tests/AfkListingMonitorTests.cs#L192) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>AFK disabled does not read a roster or query</code> | [L16](../../tests/AfkListingMonitorTests.cs#L16) |
| <code>AFK enable requires a loaded character</code> | [L21](../../tests/AfkListingMonitorTests.cs#L21) |
| <code>AFK enabling queries once and requires a fresh own listing</code> | [L25](../../tests/AfkListingMonitorTests.cs#L25) |
| <code>AFK identity ignores member order and supports solo regular and full alliances</code> | [L31](../../tests/AfkListingMonitorTests.cs#L31) |
| <code>AFK rejects partial zero duplicate foreign or oversized rosters</code> | [L38](../../tests/AfkListingMonitorTests.cs#L38) |
| <code>AFK stable identities do not cause repeated immediate queries</code> | [L42](../../tests/AfkListingMonitorTests.cs#L42) |
| <code>AFK join triggers a new query</code> | [L48](../../tests/AfkListingMonitorTests.cs#L48) |
| <code>AFK same count replacement triggers a new query</code> | [L53](../../tests/AfkListingMonitorTests.cs#L53) |
| <code>AFK returning to the original roster keeps an observed departure pending</code> | [L57](../../tests/AfkListingMonitorTests.cs#L57) |
| <code>AFK changes during a query require another query even after returning</code> | [L64](../../tests/AfkListingMonitorTests.cs#L64) |
| <code>AFK supplement catches unobserved leave and rejoin with unchanged identities</code> | [L73](../../tests/AfkListingMonitorTests.cs#L73) |
| <code>AFK failed query remains pending without another roster change</code> | [L81](../../tests/AfkListingMonitorTests.cs#L81) |
| <code>AFK repeated start failures use the configured fixed delay</code> | [L88](../../tests/AfkListingMonitorTests.cs#L88) |
| <code>AFK editor or manual work defers pending queries</code> | [L94](../../tests/AfkListingMonitorTests.cs#L94) |
| <code>AFK queued dispatch is cancelled if editor opens before read</code> | [L99](../../tests/AfkListingMonitorTests.cs#L99) |
| <code>AFK unreadable observations preserve membership and resume pending work</code> | [L104](../../tests/AfkListingMonitorTests.cs#L104) |
| <code>AFK incomplete observation during a query prevents false completion</code> | [L109](../../tests/AfkListingMonitorTests.cs#L109) |
| <code>AFK old detail revision cannot finish a request</code> | [L114](../../tests/AfkListingMonitorTests.cs#L114) |
| <code>AFK unverified owner or mismatched listing membership stays pending</code> | [L118](../../tests/AfkListingMonitorTests.cs#L118) |
| <code>AFK no own listing times out and retries instead of clearing pending</code> | [L125](../../tests/AfkListingMonitorTests.cs#L125) |
| <code>AFK temporary player unavailability does not disable the mode</code> | [L130](../../tests/AfkListingMonitorTests.cs#L130) |
| <code>AFK stopping cancels only its own active request</code> | [L137](../../tests/AfkListingMonitorTests.cs#L137) |
| <code>AFK stopping cannot cancel a newer manual query</code> | [L141](../../tests/AfkListingMonitorTests.cs#L141) |
| <code>AFK replaced request cannot consume a newer query result</code> | [L145](../../tests/AfkListingMonitorTests.cs#L145) |
| <code>AFK logout or character switch disables session and cannot resume later</code> | [L149](../../tests/AfkListingMonitorTests.cs#L149) |
| <code>AFK request exception after acceptance waits without duplicate dispatch</code> | [L157](../../tests/AfkListingMonitorTests.cs#L157) |

<a id="file-tests-afksettingstests-cs"></a>
## tests/AfkSettingsTests.cs

[소스 파일](../../tests/AfkSettingsTests.cs) · 명명된 항목/진입점 2개

AFK 주기·재시도 선택/경계·설정 저장를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-afksettingstests-cs-6-02815bde89"></a><code>AfkSettingsTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | AFK 주기·재시도 선택/경계·설정 저장 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/AfkSettingsTests.cs#L6) |
| <a id="symbol-tests-afksettingstests-cs-8-f799efb3e5"></a><code>AfkSettingsTests.Run.Start</code><br><code>(AfkListingMonitor, AfkFakeHost) Start(AfkSettings settings, bool complete = true)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L8](../../tests/AfkSettingsTests.cs#L8) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>AFK periodic queries default off while membership queries remain enabled</code> | [L15](../../tests/AfkSettingsTests.cs#L15) |
| <code>AFK period boundaries are exactly one and fifty five minutes</code> | [L21](../../tests/AfkSettingsTests.cs#L21) |
| <code>AFK invalid persisted intervals clamp to supported bounds</code> | [L28](../../tests/AfkSettingsTests.cs#L28) |
| <code>AFK disabling an overdue blocked periodic query removes only the periodic trigger</code> | [L32](../../tests/AfkSettingsTests.cs#L32) |
| <code>AFK changing the period starts a fresh interval</code> | [L37](../../tests/AfkSettingsTests.cs#L37) |
| <code>AFK disabled retries retain failure without repeated unchanged queries</code> | [L42](../../tests/AfkSettingsTests.cs#L42) |
| <code>AFK periodic timer stays independent when failure retry is disabled</code> | [L49](../../tests/AfkSettingsTests.cs#L49) |
| <code>AFK configured retry seconds replace exponential backoff</code> | [L54](../../tests/AfkSettingsTests.cs#L54) |
| <code>AFK periodic queries are not postponed by a longer enabled retry interval</code> | [L62](../../tests/AfkSettingsTests.cs#L62) |
| <code>AFK toggling retry while waiting reschedules instead of dispatching immediately</code> | [L67](../../tests/AfkSettingsTests.cs#L67) |
| <code>AFK settings persist without changing saved connection data or enabling AFK</code> | [L73](../../tests/AfkSettingsTests.cs#L73) |

<a id="file-tests-compatibilitytests-cs"></a>
## tests/CompatibilityTests.cs

[소스 파일](../../tests/CompatibilityTests.cs) · 명명된 항목/진입점 2개

네이티브/interop ABI 계약 변화, MVID/버전 독립성 및 fail-closed 비교를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-compatibilitytests-cs-7-a6d7102f22"></a><code>CompatibilityTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 네이티브/interop ABI 계약 변화, MVID/버전 독립성 및 fail-closed 비교 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L7](../../tests/CompatibilityTests.cs#L7) |
| <a id="symbol-tests-compatibilitytests-cs-11-ddb991aa6d"></a><code>CompatibilityTests.Run.Clone</code><br><code>NativeContract Clone()</code> | 로컬 함수 | 계약 fixture를 복사해 한 ABI 요소만 바꾸는 비교 테스트에 쓴다. | [L11](../../tests/CompatibilityTests.cs#L11) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>different DLL identity with unchanged required structure passes</code> | [L12](../../tests/CompatibilityTests.cs#L12) |
| <code>required native layout, offsets, constants and virtual slots fail closed</code> | [L14](../../tests/CompatibilityTests.cs#L14) |
| <code>every used UI event number is captured and changing it rejects compatibility</code> | [L23](../../tests/CompatibilityTests.cs#L23) |
| <code>unrelated metadata additions do not disable compatible builds</code> | [L39](../../tests/CompatibilityTests.cs#L39) |
| <code>different API, assembly, schema or empty baseline is rejected</code> | [L43](../../tests/CompatibilityTests.cs#L43) |
| <code>contract serialization round trip preserves checks</code> | [L49](../../tests/CompatibilityTests.cs#L49) |
| <code>metadata reader detects a missing required type without loading code</code> | [L53](../../tests/CompatibilityTests.cs#L53) |
| <code>sequential native fields retain declaration order in the contract</code> | [L58](../../tests/CompatibilityTests.cs#L58) |
| <code>malformed baseline does not silently become compatible</code> | [L63](../../tests/CompatibilityTests.cs#L63) |

<a id="file-tests-discordsettingstests-cs"></a>
## tests/DiscordSettingsTests.cs

[소스 파일](../../tests/DiscordSettingsTests.cs) · 명명된 항목/진입점 5개

저장 토큰 암복호화 어댑터, 재시작 설정 복원, 기억 해제와 오류 시 설정 보존를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-discordsettingstests-cs-12-23b6e750fb"></a><code>DiscordSettingsTests.Protect</code><br><code>string Protect(string value)</code> | 메서드 | 운영 DPAPI 대신 테스트용 결정적 token 보호 결과를 반환한다. | [L12](../../tests/DiscordSettingsTests.cs#L12) |
| <a id="symbol-tests-discordsettingstests-cs-22-db284b7b44"></a><code>DiscordSettingsTests.Unprotect</code><br><code>string Unprotect(string value)</code> | 메서드 | 테스트용 보호 문자열을 해제해 설정 보존/복원 경로를 검사한다. | [L22](../../tests/DiscordSettingsTests.cs#L22) |
| <a id="symbol-tests-discordsettingstests-cs-33-c9b2cea40c"></a><code>DiscordSettingsTests.Reload</code><br><code>RelayConfiguration Reload(RelayConfiguration config)</code> | 메서드 | 설정을 JSON 직렬화/역직렬화해 재시작을 모사한다. | [L33](../../tests/DiscordSettingsTests.cs#L33) |
| <a id="symbol-tests-discordsettingstests-cs-35-0fc83c5aba"></a><code>DiscordSettingsTests.Saved</code><br><code>RelayConfiguration Saved()</code> | 메서드 | 저장된 토큰과 자동 연결 옵션을 가진 설정 fixture를 만든다. | [L35](../../tests/DiscordSettingsTests.cs#L35) |
| <a id="symbol-tests-discordsettingstests-cs-41-c435613930"></a><code>DiscordSettingsTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 저장 토큰 암복호화 어댑터, 재시작 설정 복원, 기억 해제와 오류 시 설정 보존 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L41](../../tests/DiscordSettingsTests.cs#L41) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>discord fresh settings default to remembering but never auto-connect without a token</code> | [L43](../../tests/DiscordSettingsTests.cs#L43) |
| <code>discord encrypted token survives a disk reload and reconnects without re-entry</code> | [L47](../../tests/DiscordSettingsTests.cs#L47) |
| <code>discord 0.3.46 config gains auto-connect without losing the encrypted token</code> | [L63](../../tests/DiscordSettingsTests.cs#L63) |
| <code>discord explicit disconnect survives restart and keeps token for manual reconnect</code> | [L71](../../tests/DiscordSettingsTests.cs#L71) |
| <code>discord remember opt-out persists and clears a previously saved token</code> | [L79](../../tests/DiscordSettingsTests.cs#L79) |
| <code>discord forget removes token and startup preference across restart</code> | [L88](../../tests/DiscordSettingsTests.cs#L88) |
| <code>discord failed decryption preserves settings for recovery</code> | [L94](../../tests/DiscordSettingsTests.cs#L94) |
| <code>discord invalid replacement and encryption failure never overwrite saved settings</code> | [L103](../../tests/DiscordSettingsTests.cs#L103) |

<a id="file-tests-gamechatcapturetests-cs"></a>
## tests/GameChatCaptureTests.cs

[소스 파일](../../tests/GameChatCaptureTests.cs) · 명명된 항목/진입점 14개

Dalamud 공개 채팅 이벤트의 두 delegate 형태·읽기 전용 수신·필터·서버 추출를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-gamechatcapturetests-cs-5-3a53b7fa16"></a><code>GameChatCaptureTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | Dalamud 공개 채팅 이벤트의 두 delegate 형태·읽기 전용 수신·필터·서버 추출 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L5](../../tests/GameChatCaptureTests.cs#L5) |
| <a id="symbol-tests-gamechatcapturetests-cs-56-c64e46c497"></a><code>GameChatCaptureTests.Run.Home</code><br><code>string Home()</code> | 로컬 함수 | 홈 서버 조회 횟수를 기록하는 callback으로 지역 이동 중 유지와 반복 읽기 억제를 검증한다. | [L56](../../tests/GameChatCaptureTests.cs#L56) |
| <a id="symbol-tests-gamechatcapturetests-cs-84-e550ddeea6"></a><code>GameChatCaptureTests.PlayerPayload.PlayerName</code><br><code>string PlayerName</code> | 계산 속성 | fake 발신자의 실제 이름을 반환한다. 계산식: <code>"모험가"</code>. | [L84](../../tests/GameChatCaptureTests.cs#L84) |
| <a id="symbol-tests-gamechatcapturetests-cs-88-938191d4b6"></a><code>GameChatCaptureTests.IconPayload.Icon</code><br><code>string Icon</code> | 계산 속성 | 해석되지 않은 CrossWorld 표시를 가진 fake payload다. 원격 발신자를 자기 서버로 대체하지 않는지 확인한다. 계산식: <code>"CrossWorld"</code>. | [L88](../../tests/GameChatCaptureTests.cs#L88) |
| <a id="symbol-tests-gamechatcapturetests-cs-93-bbe47dd8ba"></a><code>GameChatCaptureTests.LegacySource.Subscribers</code><br><code>int Subscribers</code> | 계산 속성 | 현재 공개 이벤트 구독 수를 반환해 종료 시 해제를 확인한다. 계산식: <code>ChatMessage?.GetInvocationList().Length ?? 0</code>. | [L93](../../tests/GameChatCaptureTests.cs#L93) |
| <a id="symbol-tests-gamechatcapturetests-cs-95-a4c4358954"></a><code>GameChatCaptureTests.LegacySource.Fire</code><br><code>void Fire(ushort type, bool handled = false, bool payload = true, object[]? customPayloads = null, string name = "보낸사람")</code> | 메서드 | 실제 어댑터 delegate에 legacy 또는 현재 메시지 이벤트를 발생시킨다. 실제 Dalamud/게임은 사용하지 않는다. | [L95](../../tests/GameChatCaptureTests.cs#L95) |
| <a id="symbol-tests-gamechatcapturetests-cs-110-270bb6faaa"></a><code>GameChatCaptureTests.IHandleable.PreventOriginal</code><br><code>void PreventOriginal()</code> | 인터페이스 계약 | 호출 여부를 기록한다. 수신 어댑터가 원본 게임 채팅을 숨기지 않는지 확인한다. | [L110](../../tests/GameChatCaptureTests.cs#L110) |
| <a id="symbol-tests-gamechatcapturetests-cs-115-e678e56a53"></a><code>GameChatCaptureTests.CurrentMessage.LogKind</code><br><code>ushort LogKind</code> | 계산 속성 | explicit interface에 있는 fake 채팅 종류를 노출한다. 계산식: <code>14</code>. | [L115](../../tests/GameChatCaptureTests.cs#L115) |
| <a id="symbol-tests-gamechatcapturetests-cs-116-22041fd491"></a><code>GameChatCaptureTests.CurrentMessage.Timestamp</code><br><code>int Timestamp</code> | 계산 속성 | fake 수신 시각을 반환한다. 계산식: <code>1792935309</code>. | [L116](../../tests/GameChatCaptureTests.cs#L116) |
| <a id="symbol-tests-gamechatcapturetests-cs-117-5721d0b530"></a><code>GameChatCaptureTests.CurrentMessage.Sender</code><br><code>SeString Sender</code> | 계산 속성 | 읽기 횟수를 기록하고 fake 발신자 SeString을 반환한다. 관심 없는 종류를 미리 거르는지 확인한다. | [L117](../../tests/GameChatCaptureTests.cs#L117) |
| <a id="symbol-tests-gamechatcapturetests-cs-118-4cac5622f6"></a><code>GameChatCaptureTests.CurrentMessage.Message</code><br><code>SeString Message</code> | 계산 속성 | fake 본문 SeString 또는 읽기 실패를 제공한다. 민감한 예외 내용이 상태에 노출되지 않는지 확인한다. 계산식: <code>new() { TextValue = "내용" }</code>. | [L118](../../tests/GameChatCaptureTests.cs#L118) |
| <a id="symbol-tests-gamechatcapturetests-cs-119-153a018a9d"></a><code>GameChatCaptureTests.CurrentMessage.PreventOriginal</code><br><code>void PreventOriginal()</code> | 메서드 | 호출 여부를 기록한다. 수신 어댑터가 원본 게임 채팅을 숨기지 않는지 확인한다. | [L119](../../tests/GameChatCaptureTests.cs#L119) |
| <a id="symbol-tests-gamechatcapturetests-cs-124-5c3e3831d3"></a><code>GameChatCaptureTests.CurrentSource.Subscribers</code><br><code>int Subscribers</code> | 계산 속성 | 현재 공개 이벤트 구독 수를 반환해 종료 시 해제를 확인한다. 계산식: <code>ChatMessage?.GetInvocationList().Length ?? 0</code>. | [L124](../../tests/GameChatCaptureTests.cs#L124) |
| <a id="symbol-tests-gamechatcapturetests-cs-125-1f13c13ce1"></a><code>GameChatCaptureTests.CurrentSource.Fire</code><br><code>void Fire(IHandleable message)</code> | 메서드 | 실제 어댑터 delegate에 legacy 또는 현재 메시지 이벤트를 발생시킨다. 실제 Dalamud/게임은 사용하지 않는다. | [L125](../../tests/GameChatCaptureTests.cs#L125) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>chat capture accepts party cross world party incoming tells and alliance only</code> | [L7](../../tests/GameChatCaptureTests.cs#L7) |
| <code>chat legacy callback preserves handled flag sender and message references</code> | [L14](../../tests/GameChatCaptureTests.cs#L14) |
| <code>chat object event reads inherited explicit public interface members without consuming chat</code> | [L18](../../tests/GameChatCaptureTests.cs#L18) |
| <code>chat disabled capture does not read payloads and disposal unsubscribes both event shapes</code> | [L24](../../tests/GameChatCaptureTests.cs#L24) |
| <code>chat missing server metadata remains unknown instead of inventing the local server</code> | [L32](../../tests/GameChatCaptureTests.cs#L32) |
| <code>same-world and zero-world senders use home world while explicit remote senders keep their world</code> | [L37](../../tests/GameChatCaptureTests.cs#L37) |
| <code>unresolved cross-world sender markers never fall back to home world</code> | [L47](../../tests/GameChatCaptureTests.cs#L47) |
| <code>chat identity survives zoning without AFK and clears across logout and character changes</code> | [L54](../../tests/GameChatCaptureTests.cs#L54) |
| <code>chat capture failures expose no chat text and do not escape the event callback</code> | [L70](../../tests/GameChatCaptureTests.cs#L70) |

<a id="file-tests-listingbatchtests-cs"></a>
## tests/ListingBatchTests.cs

[소스 파일](../../tests/ListingBatchTests.cs) · 명명된 항목/진입점 10개

다중 변경 순차 실행, unavailable 건너뛰기, 누적 draft, 1회 제출·원본/멤버 보존를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingbatchtests-cs-6-87fe7d8085"></a><code>ListingBatchTests.Target</code><br><code>RefreshTarget Target(int count = 8)</code> | 메서드 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L6](../../tests/ListingBatchTests.cs#L6) |
| <a id="symbol-tests-listingbatchtests-cs-17-a4a2202bd4"></a><code>ListingBatchTests.Description</code><br><code>ListingChange Description(RefreshTarget t, string text = "새 소개말")</code> | 메서드 | 배치 테스트용 소개말 변경 요청을 만든다. | [L17](../../tests/ListingBatchTests.cs#L17) |
| <a id="symbol-tests-listingbatchtests-cs-18-79dfbe0e77"></a><code>ListingBatchTests.Rule</code><br><code>ListingChange Rule(RefreshTarget t, ListingRuleKind kind = ListingRuleKind.Unrestricted, int value = 1)</code> | 메서드 | 배치 테스트용 임무 찾기/전리품 변경 요청을 만든다. | [L18](../../tests/ListingBatchTests.cs#L18) |
| <a id="symbol-tests-listingbatchtests-cs-19-3972602d24"></a><code>ListingBatchTests.Start</code><br><code>(BatchFakeHost host, ListingRefresh flow) Start(ListingChange[] changes)</code> | 메서드 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L19](../../tests/ListingBatchTests.cs#L19) |
| <a id="symbol-tests-listingbatchtests-cs-25-97ee38d44e"></a><code>ListingBatchTests.RunToSubmit</code><br><code>long RunToSubmit(BatchFakeHost h, ListingRefresh f, long tick = 2500)</code> | 메서드 | 다중 변경 순차 실행, unavailable 건너뛰기, 누적 draft, 1회 제출·원본/멤버 보존 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L25](../../tests/ListingBatchTests.cs#L25) |
| <a id="symbol-tests-listingbatchtests-cs-30-0a9b2f54b7"></a><code>ListingBatchTests.Verify</code><br><code>void Verify(BatchFakeHost h, ListingRefresh f, long tick, RefreshTarget? result = null)</code> | 메서드 | fake 새 상세 응답을 완료해 제출 후 결과 검증을 진행한다. | [L30](../../tests/ListingBatchTests.cs#L30) |
| <a id="symbol-tests-listingbatchtests-cs-36-d211232652"></a><code>ListingBatchTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 다중 변경 순차 실행, unavailable 건너뛰기, 누적 draft, 1회 제출·원본/멤버 보존 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L36](../../tests/ListingBatchTests.cs#L36) |
| <a id="symbol-tests-listingbatchtests-cs-251-b7776f1e74"></a><code>BatchFakeHost.PrepareBatchStep</code><br><code>bool PrepareBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 메서드 | 현재 fake 누적 draft 또는 준비 실패를 반환한다. | [L251](../../tests/ListingBatchTests.cs#L251) |
| <a id="symbol-tests-listingbatchtests-cs-257-cb0a0d9adf"></a><code>BatchFakeHost.FinishBatchStep</code><br><code>SlotEditProgress FinishBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 메서드 | 항목 종료 후 정리/실제 draft 반영 또는 복구 실패를 모사한다. | [L257](../../tests/ListingBatchTests.cs#L257) |
| <a id="symbol-tests-listingbatchtests-cs-275-4e842f7c1c"></a><code>BatchFakeHost.SubmitBatch</code><br><code>bool SubmitBatch(RefreshTarget identity, out string error)</code> | 메서드 | 배치의 최종 제출 횟수·조건을 기록하고 결과/예외를 모사한다. | [L275](../../tests/ListingBatchTests.cs#L275) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>batch queue replaces individual keys, keeps multiple jobs, and restores original values</code> | [L38](../../tests/ListingBatchTests.cs#L38) |
| <code>batch queue rejects invalid input and visibly drops stale listing drafts</code> | [L47](../../tests/ListingBatchTests.cs#L47) |
| <code>preflight failure and cancellation preserve every queued input including PIN</code> | [L52](../../tests/ListingBatchTests.cs#L52) |
| <code>retained changes can be retried and are consumed once when the editor attempt starts</code> | [L75](../../tests/ListingBatchTests.cs#L75) |
| <code>blocked batch requests and later replacements cannot clear the pending queue</code> | [L88](../../tests/ListingBatchTests.cs#L88) |
| <code>cancelling after an editor attempt never restores potentially applied changes</code> | [L99](../../tests/ListingBatchTests.cs#L99) |
| <code>an editor dispatch exception consumes the queue and never retries the native attempt</code> | [L106](../../tests/ListingBatchTests.cs#L106) |
| <code>all edit kinds and multiple slots share one cumulative editor and one submit</code> | [L115](../../tests/ListingBatchTests.cs#L115) |
| <code>disabled rule, later item failure and final success continue to one refresh</code> | [L134](../../tests/ListingBatchTests.cs#L134) |
| <code>every item failing still refreshes the unchanged game draft</code> | [L142](../../tests/ListingBatchTests.cs#L142) |
| <code>item exception and timeout advance without retrying the failed operation</code> | [L148](../../tests/ListingBatchTests.cs#L148) |
| <code>failed verification adopts actual intermediate values for later changes</code> | [L157](../../tests/ListingBatchTests.cs#L157) |
| <code>fresh listing determines results even after editor checks passed or failed</code> | [L166](../../tests/ListingBatchTests.cs#L166) |
| <code>failed PIN verification is never inferred from the server private flag</code> | [L174](../../tests/ListingBatchTests.cs#L174) |
| <code>unreadable item recovery does not stop later items or refresh</code> | [L182](../../tests/ListingBatchTests.cs#L182) |
| <code>stale listing, character and closed editor do not operate on another listing</code> | [L187](../../tests/ListingBatchTests.cs#L187) |
| <code>submit exception never resubmits and verification timeout never claims success</code> | [L199](../../tests/ListingBatchTests.cs#L199) |
| <code>batch cooldown and cancellation prevent repeated actions</code> | [L207](../../tests/ListingBatchTests.cs#L207) |
| <code>friendly sparse batch keeps native job routes and unused padding distinct</code> | [L214](../../tests/ListingBatchTests.cs#L214) |
| <code>batch snapshot preserves actual partial changes and never mutates its source</code> | [L236](../../tests/ListingBatchTests.cs#L236) |

<a id="file-tests-listingbeginneredittests-cs"></a>
## tests/ListingBeginnerEditTests.cs

[소스 파일](../../tests/ListingBeginnerEditTests.cs) · 명명된 항목/진입점 3개

초보자 환영 typed edit·checkbox UI·단일 갱신 결과 검증를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingbeginneredittests-cs-6-25b775098f"></a><code>ListingBeginnerEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 초보자 환영 typed edit·checkbox UI·단일 갱신 결과 검증 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingBeginnerEditTests.cs#L6) |
| <a id="symbol-tests-listingbeginneredittests-cs-8-7eadb256a6"></a><code>ListingBeginnerEditTests.Run.Target</code><br><code>RefreshTarget Target(int count = 8, byte beginner = 0, byte objective = 4)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L8](../../tests/ListingBeginnerEditTests.cs#L8) |
| <a id="symbol-tests-listingbeginneredittests-cs-18-7f5fff2063"></a><code>ListingBeginnerEditTests.Run.Start</code><br><code>(FakeHost host, ListingRefresh flow) Start(bool enabled = true, int count = 8, byte objective = 4, int seconds = 1800)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L18](../../tests/ListingBeginnerEditTests.cs#L18) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>beginner draft uses known detail state and preserves purpose, text and slots</code> | [L27](../../tests/ListingBeginnerEditTests.cs#L27) |
| <code>beginner UI on and off each dispatch one click and require both UI and stored state</code> | [L42](../../tests/ListingBeginnerEditTests.cs#L42) |
| <code>beginner UI unknown, initially stale, reversed and UI-only timeout states fail without retry</code> | [L55](../../tests/ListingBeginnerEditTests.cs#L55) |
| <code>beginner on/off refreshes 4/8/24/48 once, preserving every purpose</code> | [L68](../../tests/ListingBeginnerEditTests.cs#L68) |
| <code>unchanged, unknown, malformed or PvP beginner requests dispatch nothing</code> | [L85](../../tests/ListingBeginnerEditTests.cs#L85) |
| <code>beginner cannot be combined with a purpose, description or job edit</code> | [L94](../../tests/ListingBeginnerEditTests.cs#L94) |
| <code>stale beginner settings, purpose, listing, comment or participant stop before opening</code> | [L106](../../tests/ListingBeginnerEditTests.cs#L106) |
| <code>failed, delayed, cancelled, closed or rebound beginner editing never submits</code> | [L119](../../tests/ListingBeginnerEditTests.cs#L119) |
| <code>failed or thrown beginner operations are not repeated</code> | [L134](../../tests/ListingBeginnerEditTests.cs#L134) |
| <code>unchanged flag or changed purpose, comment or alliance slot fail before and after submit</code> | [L145](../../tests/ListingBeginnerEditTests.cs#L145) |
| <code>beginner confirmation is reported independently of unchanged sixty-minute timer</code> | [L170](../../tests/ListingBeginnerEditTests.cs#L170) |
| <code>beginner editing does not run in a new-party screen</code> | [L176](../../tests/ListingBeginnerEditTests.cs#L176) |

<a id="file-tests-listingcompletionedittests-cs"></a>
## tests/ListingCompletionEditTests.cs

[소스 파일](../../tests/ListingCompletionEditTests.cs) · 명명된 항목/진입점 5개

완료조건 1/2/4 mapping, 체크/목록 선택과 disabled/지연·실패 처리를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingcompletionedittests-cs-7-ef0909b433"></a><code>ListingCompletionEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 완료조건 1/2/4 mapping, 체크/목록 선택과 disabled/지연·실패 처리 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L7](../../tests/ListingCompletionEditTests.cs#L7) |
| <a id="symbol-tests-listingcompletionedittests-cs-9-f5ce969d79"></a><code>ListingCompletionEditTests.Run.Target</code><br><code>RefreshTarget Target(byte completion = 1, int count = 8)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L9](../../tests/ListingCompletionEditTests.cs#L9) |
| <a id="symbol-tests-listingcompletionedittests-cs-19-8a580a147c"></a><code>ListingCompletionEditTests.Run.Edit</code><br><code>ListingCompletionEdit Edit(RefreshTarget t, byte value)</code> | 로컬 함수 | 검증할 필드의 typed edit fixture를 만든다. | [L19](../../tests/ListingCompletionEditTests.cs#L19) |
| <a id="symbol-tests-listingcompletionedittests-cs-20-f50f4a251c"></a><code>ListingCompletionEditTests.Run.Closed</code><br><code>CompletionUiState Closed(byte stored, bool? enabled = null, int? index = null, bool canSelect = true)</code> | 로컬 함수 | 닫힌 dropdown/선택 상태 fixture를 만든다. | [L20](../../tests/ListingCompletionEditTests.cs#L20) |
| <a id="symbol-tests-listingcompletionedittests-cs-23-83912f7c75"></a><code>ListingCompletionEditTests.Run.Start</code><br><code>(FakeHost h, ListingRefresh f) Start(byte original = 1, byte wanted = 2, int count = 8, int seconds = 1800)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L23](../../tests/ListingCompletionEditTests.cs#L23) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>completion flags match the KR enum rather than callback ordinals</code> | [L31](../../tests/ListingCompletionEditTests.cs#L31) |
| <code>completion draft preserves the current checkbox and selected requirement</code> | [L40](../../tests/ListingCompletionEditTests.cs#L40) |
| <code>completion checkbox on and off require both UI state and stored condition</code> | [L52](../../tests/ListingCompletionEditTests.cs#L52) |
| <code>completion dropdown uses open then click and native selection for both observed indices</code> | [L66](../../tests/ListingCompletionEditTests.cs#L66) |
| <code>enabling completion selects the requested requirement only when the restored choice differs</code> | [L79](../../tests/ListingCompletionEditTests.cs#L79) |
| <code>unknown completion states, unavailable items and inconsistent initial UI never mutate</code> | [L88](../../tests/ListingCompletionEditTests.cs#L88) |
| <code>completion missing storage, open timeout and changed selection fail without retry</code> | [L97](../../tests/ListingCompletionEditTests.cs#L97) |
| <code>all six completion transitions preserve unrelated settings and submit once on every layout</code> | [L113](../../tests/ListingCompletionEditTests.cs#L113) |
| <code>unknown, unchanged or unsupported completion requests never query</code> | [L130](../../tests/ListingCompletionEditTests.cs#L130) |
| <code>completion cannot be combined with any of the six other edits</code> | [L138](../../tests/ListingCompletionEditTests.cs#L138) |
| <code>stale completion drafts detect changed participants, flags, text and identity</code> | [L151](../../tests/ListingCompletionEditTests.cs#L151) |
| <code>completion failure, timeout, cancellation, character change or closure never submits</code> | [L162](../../tests/ListingCompletionEditTests.cs#L162) |
| <code>failed or thrown completion dispatch never retries</code> | [L177](../../tests/ListingCompletionEditTests.cs#L177) |
| <code>wrong completion, purpose, flags or slot results cannot pass pre/post-submit verification</code> | [L187](../../tests/ListingCompletionEditTests.cs#L187) |
| <code>completion setting confirmation is separate from timer and create mode is blocked</code> | [L209](../../tests/ListingCompletionEditTests.cs#L209) |

<a id="file-tests-listingdescriptionedittests-cs"></a>
## tests/ListingDescriptionEditTests.cs

[소스 파일](../../tests/ListingDescriptionEditTests.cs) · 명명된 항목/진입점 3개

소개말 정규화/UTF-8 제한·변경 검증·submit payload를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdescriptionedittests-cs-6-9e31554307"></a><code>ListingDescriptionEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 소개말 정규화/UTF-8 제한·변경 검증·submit payload 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingDescriptionEditTests.cs#L6) |
| <a id="symbol-tests-listingdescriptionedittests-cs-8-efdfb49ff8"></a><code>ListingDescriptionEditTests.Run.Target</code><br><code>RefreshTarget Target(int count = 8, string text = "기존 소개말")</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L8](../../tests/ListingDescriptionEditTests.cs#L8) |
| <a id="symbol-tests-listingdescriptionedittests-cs-18-2adabce5b6"></a><code>ListingDescriptionEditTests.Run.Start</code><br><code>(FakeHost host, ListingRefresh flow, ListingDescriptionEdit edit) Start(string text, int count = 8)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L18](../../tests/ListingDescriptionEditTests.cs#L18) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>description draft is local and preserves the original until applied</code> | [L26](../../tests/ListingDescriptionEditTests.cs#L26) |
| <code>plain text, Korean, empty and line endings validate without splitting UTF-8</code> | [L33](../../tests/ListingDescriptionEditTests.cs#L33) |
| <code>description edit applies once and verifies fresh results for 4/8/24/48 slots</code> | [L42](../../tests/ListingDescriptionEditTests.cs#L42) |
| <code>adding an introduction to an empty listing uses empty original text</code> | [L57](../../tests/ListingDescriptionEditTests.cs#L57) |
| <code>final submission carries changed, empty and Korean text independently of the draft</code> | [L63](../../tests/ListingDescriptionEditTests.cs#L63) |
| <code>reported three-character old text after a five-byte edit remains unconfirmed with diagnostic detail</code> | [L75](../../tests/ListingDescriptionEditTests.cs#L75) |
| <code>verified text at the same displayed timer is reported separately from timer confirmation</code> | [L86](../../tests/ListingDescriptionEditTests.cs#L86) |
| <code>failed or thrown description submissions never fall back to a button or retry</code> | [L92](../../tests/ListingDescriptionEditTests.cs#L92) |
| <code>changed description or alliance participants block stale drafts before opening</code> | [L101](../../tests/ListingDescriptionEditTests.cs#L101) |
| <code>description failure, timeout, cancellation or editor closure never submits</code> | [L112](../../tests/ListingDescriptionEditTests.cs#L112) |
| <code>a failed or thrown introduction input is never retried</code> | [L124](../../tests/ListingDescriptionEditTests.cs#L124) |
| <code>truncated text and changed job restrictions cannot pass pre/post-submit verification</code> | [L134](../../tests/ListingDescriptionEditTests.cs#L134) |
| <code>invalid or combined edit requests dispatch no game actions</code> | [L153](../../tests/ListingDescriptionEditTests.cs#L153) |

<a id="file-tests-listingdescriptionrecoverytests-cs"></a>
## tests/ListingDescriptionRecoveryTests.cs

[소스 파일](../../tests/ListingDescriptionRecoveryTests.cs) · 명명된 항목/진입점 7개

소개말 화면만 부분 반영된 실패의 복구와 다른 배치 항목 진행를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdescriptionrecoverytests-cs-5-ed9688eb5e"></a><code>ListingDescriptionRecoveryTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 소개말 화면만 부분 반영된 실패의 복구와 다른 배치 항목 진행 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L5](../../tests/ListingDescriptionRecoveryTests.cs#L5) |
| <a id="symbol-tests-listingdescriptionrecoverytests-cs-70-499a50fecf"></a><code>ListingDescriptionRecoveryTests.PartialDescriptionHost.PrepareBatchStep</code><br><code>bool PrepareBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 메서드 | 현재 fake 누적 draft 또는 준비 실패를 반환한다. | [L70](../../tests/ListingDescriptionRecoveryTests.cs#L70) |
| <a id="symbol-tests-listingdescriptionrecoverytests-cs-75-f4c16b5117"></a><code>ListingDescriptionRecoveryTests.PartialDescriptionHost.BeginDescriptionEdit</code><br><code>bool BeginDescriptionEdit(RefreshTarget target, ListingDescriptionEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L75](../../tests/ListingDescriptionRecoveryTests.cs#L75) |
| <a id="symbol-tests-listingdescriptionrecoverytests-cs-77-551a921806"></a><code>ListingDescriptionRecoveryTests.PartialDescriptionHost.UpdateDescriptionEdit</code><br><code>SlotEditProgress UpdateDescriptionEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L77](../../tests/ListingDescriptionRecoveryTests.cs#L77) |
| <a id="symbol-tests-listingdescriptionrecoverytests-cs-79-c0283181ef"></a><code>ListingDescriptionRecoveryTests.PartialDescriptionHost.FinishBatchStep</code><br><code>SlotEditProgress FinishBatchStep(RefreshTarget identity, out RefreshTarget current, out string error)</code> | 메서드 | 항목 종료 후 정리/실제 draft 반영 또는 복구 실패를 모사한다. | [L79](../../tests/ListingDescriptionRecoveryTests.cs#L79) |
| <a id="symbol-tests-listingdescriptionrecoverytests-cs-101-83f168bb9c"></a><code>ListingDescriptionRecoveryTests.PartialDescriptionHost.BeginRuleEdit</code><br><code>bool BeginRuleEdit(RefreshTarget target, ListingRuleEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L101](../../tests/ListingDescriptionRecoveryTests.cs#L101) |
| <a id="symbol-tests-listingdescriptionrecoverytests-cs-106-c26d48fdef"></a><code>ListingDescriptionRecoveryTests.PartialDescriptionHost.SubmitBatch</code><br><code>bool SubmitBatch(RefreshTarget identity, out string error)</code> | 메서드 | 배치의 최종 제출 횟수·조건을 기록하고 결과/예외를 모사한다. | [L106](../../tests/ListingDescriptionRecoveryTests.cs#L106) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>failed description restores the actual stored text, including an empty comment</code> | [L7](../../tests/ListingDescriptionRecoveryTests.cs#L7) |
| <code>already matching input needs no recovery, even when the item reported a failure</code> | [L15](../../tests/ListingDescriptionRecoveryTests.cs#L15) |
| <code>recovery recognizes delayed input updates and the game's observed formatted text</code> | [L20](../../tests/ListingDescriptionRecoveryTests.cs#L20) |
| <code>unresponsive recovery input times out without retrying the native call</code> | [L28](../../tests/ListingDescriptionRecoveryTests.cs#L28) |
| <code>recovery does not overwrite later manual input or chase a changed stored comment</code> | [L35](../../tests/ListingDescriptionRecoveryTests.cs#L35) |
| <code>failed description no longer blocks the next setting or the single final refresh</code> | [L41](../../tests/ListingDescriptionRecoveryTests.cs#L41) |

<a id="file-tests-listingdiscordcardtests-cs"></a>
## tests/ListingDiscordCardTests.cs

[소스 파일](../../tests/ListingDiscordCardTests.cs) · 명명된 항목/진입점 1개

조회/수정/직업 상세/결과 카드를 독립 화면으로 유지하는 routing를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordcardtests-cs-6-c0bb16dede"></a><code>ListingDiscordTests.RunCardFlow</code><br><code>void RunCardFlow(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 조회/수정/직업 상세/결과 카드를 독립 화면으로 유지하는 routing 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingDiscordCardTests.cs#L6) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>discord separate editor preserves drafts while viewing all parties and navigating details</code> | [L8](../../tests/ListingDiscordCardTests.cs#L8) |
| <code>discord grouped modal rejects malformed fields atomically and explicit empty checks turn options off</code> | [L30](../../tests/ListingDiscordCardTests.cs#L30) |
| <code>discord private modal keeps an existing draft PIN without echoing it and removes it when disabled</code> | [L51](../../tests/ListingDiscordCardTests.cs#L51) |

<a id="file-tests-listingdiscordchatsdktests-cs"></a>
## tests/ListingDiscordChatSdkTests.cs

[소스 파일](../../tests/ListingDiscordChatSdkTests.cs) · 명명된 항목/진입점 3개

실제 SDK 채팅 slash 등록/파싱·일반 임베드 POST·색상/멘션 직렬화를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordchatsdktests-cs-7-4a762ae416"></a><code>ListingDiscordConnectionTests.RunChatSdk</code><br><code>void RunChatSdk(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 실제 SDK 채팅 slash 등록/파싱·일반 임베드 POST·색상/멘션 직렬화 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L7](../../tests/ListingDiscordChatSdkTests.cs#L7) |
| <a id="symbol-tests-listingdiscordchatsdktests-cs-9-6428b4da1e"></a><code>ListingDiscordConnectionTests.RunChatSdk.ConnectedChat</code><br><code>ListingDiscordClient ConnectedChat(OfflineTransport transport, DiscordChatSettings? settings = null, Func&lt;DiscordChatSettings, CancellationToken, Task&gt;? save = null, ulong permissions = 150528)</code> | 로컬 함수 | 실제 Discord.Net을 fake REST/Gateway에 연결하고3개 서버 텍스트 채널과 설정 저장 callback을 준비한다. | [L9](../../tests/ListingDiscordChatSdkTests.cs#L9) |
| <a id="symbol-tests-listingdiscordchatsdktests-cs-84-89e40433a8"></a><code>ListingDiscordConnectionTests.OfflineTransport.ChatInteraction</code><br><code>void ChatInteraction(string action, object[] options)</code> | 메서드 | 종류·resolved 텍스트 채널·멘션 옵션이 있는 실제 guild slash INTERACTION_CREATE JSON을 주입한다. | [L84](../../tests/ListingDiscordChatSdkTests.cs#L84) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>Discord SDK registers pvkr chat separately with localized choices and text channel picker</code> | [L18](../../tests/ListingDiscordChatSdkTests.cs#L18) |
| <code>Discord SDK posts persistent colored chat with exact sender and no mentions by default</code> | [L27](../../tests/ListingDiscordChatSdkTests.cs#L27) |
| <code>Discord SDK delivers each chat category to its own channel and only configured everyone mentions</code> | [L40](../../tests/ListingDiscordChatSdkTests.cs#L40) |
| <code>Discord SDK chat permission checks require embeds and everyone only when selected</code> | [L53](../../tests/ListingDiscordChatSdkTests.cs#L53) |
| <code>Discord SDK slash routing channel mention and removal work from guild interactions</code> | [L64](../../tests/ListingDiscordChatSdkTests.cs#L64) |

<a id="file-tests-listingdiscordchattests-cs"></a>
## tests/ListingDiscordChatTests.cs

[소스 파일](../../tests/ListingDiscordChatTests.cs) · 명명된 항목/진입점 13개

로그인 기반 상시 채팅·종류별 수신/멘션 토글·동시 설정 저장·권한·취소·무손실 전송를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordchattests-cs-9-f87d415e56"></a><code>ListingDiscordChatTests.Settings</code><br><code>DiscordChatSettings Settings(params DiscordChatRoute[] routes)</code> | 메서드 | 현재 앱에 세 종류의 서로 다른 채널과 기본 멘션 설정을 담은 fixture를 만든다. | [L9](../../tests/ListingDiscordChatTests.cs#L9) |
| <a id="symbol-tests-listingdiscordchattests-cs-10-962cad16d9"></a><code>ListingDiscordChatTests.Chat</code><br><code>GameChatMessage Chat(GameChatKind kind = GameChatKind.Party, string text = "안녕하세요.")</code> | 메서드 | 종류·본문·발신자/서버·시각이 알려진 managed 채팅 fixture를 만든다. | [L10](../../tests/ListingDiscordChatTests.cs#L10) |
| <a id="symbol-tests-listingdiscordchattests-cs-11-7d73e8d7f4"></a><code>ListingDiscordChatTests.Done</code><br><code>void Done(Task task)</code> | 메서드 | 비동기 설정/전송 작업을5초 안에 완료시키고 예외를 테스트에 전파한다. | [L11](../../tests/ListingDiscordChatTests.cs#L11) |
| <a id="symbol-tests-listingdiscordchattests-cs-12-d55223fc83"></a><code>ListingDiscordChatTests.Wait</code><br><code>void Wait(Func&lt;bool&gt; condition)</code> | 메서드 | 조건이 참이 될 때까지 짧게 양보하며5초 예산을 초과하면 실패한다. | [L12](../../tests/ListingDiscordChatTests.cs#L12) |
| <a id="symbol-tests-listingdiscordchattests-cs-13-5e6966a48a"></a><code>ListingDiscordChatTests.Drain</code><br><code>void Drain(ListingDiscordChat chat)</code> | 메서드 | 진행 중 전송과 큐가 모두 비워질 때까지 기다린다. | [L13](../../tests/ListingDiscordChatTests.cs#L13) |
| <a id="symbol-tests-listingdiscordchattests-cs-14-85f3eec95d"></a><code>ListingDiscordChatTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 로그인 기반 상시 채팅·종류별 수신/멘션 토글·동시 설정 저장·권한·취소·무손실 전송 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L14](../../tests/ListingDiscordChatTests.cs#L14) |
| <a id="symbol-tests-listingdiscordchattests-cs-16-15a0aaf22f"></a><code>ListingDiscordChatTests.Run.Service</code><br><code>ListingDiscordChat Service(Transport t, DiscordChatSettings? settings = null, Func&lt;DiscordChatSettings, CancellationToken, Task&gt;? save = null)</code> | 로컬 함수 | fake transport와 저장 callback으로 채팅 controller를 만든다. | [L16](../../tests/ListingDiscordChatTests.cs#L16) |
| <a id="symbol-tests-listingdiscordchattests-cs-179-b0c2b64c6b"></a><code>ListingDiscordChatTests.Run.Input</code><br><code>DiscordInput Input(ulong id)</code> | 로컬 함수 | owner/app/guild 및 명령이 담긴 DiscordInput fixture를 만든다. | [L179](../../tests/ListingDiscordChatTests.cs#L179) |
| <a id="symbol-tests-listingdiscordchattests-cs-189-a21aff09c3"></a><code>ListingDiscordChatTests.Response.Defer</code><br><code>Task Defer()</code> | 메서드 | 정상 또는 실패 ACK를 모사해 ACK 전 설정 저장을 차단하는지 확인한다. | [L189](../../tests/ListingDiscordChatTests.cs#L189) |
| <a id="symbol-tests-listingdiscordchattests-cs-190-43d32faf4d"></a><code>ListingDiscordChatTests.Response.Modal</code><br><code>Task Modal(Modal modal)</code> | 메서드 | 채팅 명령에서 모달이 열리면 실패시킨다. | [L190](../../tests/ListingDiscordChatTests.cs#L190) |
| <a id="symbol-tests-listingdiscordchattests-cs-191-780426056d"></a><code>ListingDiscordChatTests.Response.Screen</code><br><code>Task Screen(DiscordScreen screen, bool replace, bool deferred)</code> | 메서드 | 채팅 설정/상태 응답을 기록한다. 실제 메시지를 보내지 않는다. | [L191](../../tests/ListingDiscordChatTests.cs#L191) |
| <a id="symbol-tests-listingdiscordchattests-cs-201-6072839323"></a><code>ListingDiscordChatTests.Transport.CheckChannel</code><br><code>Task&lt;string?&gt; CheckChannel(DiscordChatRoute route, CancellationToken cancellation)</code> | 메서드 | 선택 경로의 권한 오류·연결/취소 가능한 지연을 주입한다. | [L201](../../tests/ListingDiscordChatTests.cs#L201) |
| <a id="symbol-tests-listingdiscordchattests-cs-206-4d48149e5b"></a><code>ListingDiscordChatTests.Transport.Send</code><br><code>Task Send(DiscordChatRoute route, GameChatMessage message, CancellationToken cancellation)</code> | 메서드 | 경로/메시지/시도를 기록하고 지연·불확실한 POST 오류를 주입한다. 실제 네트워크는 사용하지 않는다. | [L206](../../tests/ListingDiscordChatTests.cs#L206) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>chat routes survive configuration reload without changing credentials or notification target</code> | [L19](../../tests/ListingDiscordChatTests.cs#L19) |
| <code>chat capture requires login a matching character and an enabled configured category</code> | [L27](../../tests/ListingDiscordChatTests.cs#L27) |
| <code>chat independent category routes preserve receive order and duplicate text messages</code> | [L34](../../tests/ListingDiscordChatTests.cs#L34) |
| <code>chat logout and character switch cancel preflight and clear queued messages</code> | [L42](../../tests/ListingDiscordChatTests.cs#L42) |
| <code>chat configuration never posts messages and omitted mention defaults off then preserves its setting</code> | [L50](../../tests/ListingDiscordChatTests.cs#L50) |
| <code>in-game receive and mention toggles are independent per category and survive reload</code> | [L59](../../tests/ListingDiscordChatTests.cs#L59) |
| <code>turning receive or mentions off cancels queued and preflight messages even without Discord permissions</code> | [L74](../../tests/ListingDiscordChatTests.cs#L74) |
| <code>local controls and slash commands serialize against the latest settings without losing other categories</code> | [L84](../../tests/ListingDiscordChatTests.cs#L84) |
| <code>legacy routes default to enabled and local failures or unconfigured toggles never alter delivery</code> | [L100](../../tests/ListingDiscordChatTests.cs#L100) |
| <code>chat failed persistence and missing channel permissions leave the original route intact</code> | [L111](../../tests/ListingDiscordChatTests.cs#L111) |
| <code>chat changing one route discards its queued messages without redirecting other categories</code> | [L121](../../tests/ListingDiscordChatTests.cs#L121) |
| <code>chat remove command stops only its category and settings can be inspected without AFK</code> | [L128](../../tests/ListingDiscordChatTests.cs#L128) |
| <code>chat rejects DM wrong server mutations invalid categories and missing mention options</code> | [L135](../../tests/ListingDiscordChatTests.cs#L135) |
| <code>chat temporary disconnect queues messages until reconnect with bounded overflow reporting</code> | [L144](../../tests/ListingDiscordChatTests.cs#L144) |
| <code>chat uncertain POST failure is not retried and does not stop later messages</code> | [L151](../../tests/ListingDiscordChatTests.cs#L151) |
| <code>chat settings cancellation during save does not activate a new destination</code> | [L157](../../tests/ListingDiscordChatTests.cs#L157) |
| <code>chat cards use three distinct colors and preserve complete Unicode text across parts</code> | [L167](../../tests/ListingDiscordChatTests.cs#L167) |
| <code>chat command authorization ACK failure and duplicate IDs never mutate routes</code> | [L176](../../tests/ListingDiscordChatTests.cs#L176) |

<a id="file-tests-listingdiscordconnectiontests-cs"></a>
## tests/ListingDiscordConnectionTests.cs

[소스 파일](../../tests/ListingDiscordConnectionTests.cs) · 명명된 항목/진입점 18개

실제 Discord.Net SDK의 fake Gateway/REST 연결 및 비동기 종료/ACK 처리를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordconnectiontests-cs-16-e7e427631b"></a><code>ListingDiscordConnectionTests.Wait</code><br><code>void Wait(Func&lt;bool&gt; condition)</code> | 메서드 | 비동기 상태가 조건을 만족할 때까지 제한 시간 안에서 기다린다. | [L16](../../tests/ListingDiscordConnectionTests.cs#L16) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-20-56392eb16c"></a><code>ListingDiscordConnectionTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 실제 Discord.Net SDK의 fake Gateway/REST 연결 및 비동기 종료/ACK 처리 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L20](../../tests/ListingDiscordConnectionTests.cs#L20) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-167-70b26ae022"></a><code>ListingDiscordConnectionTests.OfflineTransport.Config</code><br><code>DiscordSocketConfig Config()</code> | 메서드 | DiscordSocketConfig에 fake REST/WebSocket transport를 주입한다. | [L167](../../tests/ListingDiscordConnectionTests.cs#L167) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-172-5e5e006224"></a><code>ListingDiscordConnectionTests.OfflineTransport.SetHeader</code><br><code>void SetHeader(string key, string value)</code> | 메서드 | SDK가 요청한 header를 fake transport에 보관한다. | [L172](../../tests/ListingDiscordConnectionTests.cs#L172) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-173-2d93c1b86c"></a><code>ListingDiscordConnectionTests.OfflineTransport.SetCancelToken</code><br><code>void SetCancelToken(CancellationToken cancelToken)</code> | 메서드 | SDK 요청의 취소 token을 fake transport에 연결한다. | [L173](../../tests/ListingDiscordConnectionTests.cs#L173) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-174-2cdf080601"></a><code>ListingDiscordConnectionTests.OfflineTransport.Dispose</code><br><code>void Dispose()</code> | 메서드 | 테스트 fixture의 fake 연결·Bridge·취소 token을 정리한다. | [L174](../../tests/ListingDiscordConnectionTests.cs#L174) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-175-5056c2999e"></a><code>ListingDiscordConnectionTests.OfflineTransport.ConnectAsync</code><br><code>Task ConnectAsync(string host)</code> | 메서드 | 실제 네트워크 대신 fake Gateway 연결/HELLO를 시작한다. | [L175](../../tests/ListingDiscordConnectionTests.cs#L175) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-176-28b7dc9081"></a><code>ListingDiscordConnectionTests.OfflineTransport.DisconnectAsync</code><br><code>Task DisconnectAsync(int closeCode = 1000)</code> | 메서드 | fake Gateway 종료를 통지한다. | [L176](../../tests/ListingDiscordConnectionTests.cs#L176) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-177-ff8f63d4f7"></a><code>ListingDiscordConnectionTests.OfflineTransport.SendAsync</code><br><code>Task SendAsync(byte[] data, int index, int count, bool isText)</code> | 메서드 | SDK의 Gateway 또는 REST 요청을 fake transport에서 검사하고 지정 응답/실패를 모사한다. | [L177](../../tests/ListingDiscordConnectionTests.cs#L177) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-184-79b58a0178"></a><code>ListingDiscordConnectionTests.OfflineTransport.Receive</code><br><code>Task Receive(string json)</code> | 메서드 | SDK에 fake Gateway packet을 전달한다. | [L184](../../tests/ListingDiscordConnectionTests.cs#L184) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-185-06f2c5adfd"></a><code>ListingDiscordConnectionTests.OfflineTransport.Ready</code><br><code>Task Ready(bool notificationGuild = false)</code> | 메서드 | 개인 봇/application owner 정보와 session이 포함된 Gateway READY packet을 fake SDK 연결에 전달한다. | [L185](../../tests/ListingDiscordConnectionTests.cs#L185) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-190-50b387a7ec"></a><code>ListingDiscordConnectionTests.OfflineTransport.Interaction</code><br><code>void Interaction(int type, object data, ulong user = 600)</code> | 메서드 | 선택한 Discord interaction packet을 Gateway로 주입한다. | [L190](../../tests/ListingDiscordConnectionTests.cs#L190) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-203-ce64e0be91"></a><code>ListingDiscordConnectionTests.OfflineTransport.SendAsync</code><br><code>Task&lt;RestResponse&gt; SendAsync(string method, string endpoint, CancellationToken cancelToken, bool headerOnly = false, string? reason = null, IEnumerable&lt;KeyValuePair&lt;string, IEnumerable&lt;string&gt;&gt;&gt;? requestHeaders = null)</code> | 메서드 | SDK의 Gateway 또는 REST 요청을 fake transport에서 검사하고 지정 응답/실패를 모사한다. | [L203](../../tests/ListingDiscordConnectionTests.cs#L203) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-205-c8538285de"></a><code>ListingDiscordConnectionTests.OfflineTransport.SendAsync</code><br><code>Task&lt;RestResponse&gt; SendAsync(string method, string endpoint, string json, CancellationToken cancelToken, bool headerOnly = false, string? reason = null, IEnumerable&lt;KeyValuePair&lt;string, IEnumerable&lt;string&gt;&gt;&gt;? requestHeaders = null)</code> | 메서드 | SDK의 Gateway 또는 REST 요청을 fake transport에서 검사하고 지정 응답/실패를 모사한다. | [L205](../../tests/ListingDiscordConnectionTests.cs#L205) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-207-64cb7e89c6"></a><code>ListingDiscordConnectionTests.OfflineTransport.SendAsync</code><br><code>Task&lt;RestResponse&gt; SendAsync(string method, string endpoint, IReadOnlyDictionary&lt;string, object&gt; multipartParams, CancellationToken cancelToken, bool headerOnly = false, string? reason = null, IEnumerable&lt;KeyValuePair&lt;string, IEnumerable&lt;string&gt;&gt;&gt;? requestHeaders = null)</code> | 메서드 | SDK의 Gateway 또는 REST 요청을 fake transport에서 검사하고 지정 응답/실패를 모사한다. | [L207](../../tests/ListingDiscordConnectionTests.cs#L207) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-210-d7c5a689a0"></a><code>ListingDiscordConnectionTests.OfflineTransport.Reply</code><br><code>Task&lt;RestResponse&gt; Reply(string method, string endpoint, string? json = null)</code> | 메서드 | 특정 REST 경로에 기대한 JSON 응답을 만들어 fake HTTP 응답 stream으로 반환한다. | [L210](../../tests/ListingDiscordConnectionTests.cs#L210) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-298-d144a62e54"></a><code>ListingDiscordConnectionTests.OfflineTransport.MakeMessage</code><br><code>JsonElement MakeMessage(JsonElement data, bool fresh = false)</code> | 메서드 | Discord SDK가 읽을 message/component JSON을 만든다. | [L298](../../tests/ListingDiscordConnectionTests.cs#L298) |
| <a id="symbol-tests-listingdiscordconnectiontests-cs-312-d358add4a8"></a><code>ListingDiscordConnectionTests.OfflineTransport.ValidateCard</code><br><code>void ValidateCard(JsonElement data)</code> | 메서드 | Components V2 flag/구조/표시 크기와 구형 content/embeds 제거를 확인한다. | [L312](../../tests/ListingDiscordConnectionTests.cs#L312) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>discord SDK waits for READY application ID before registering pvkr</code> | [L22](../../tests/ListingDiscordConnectionTests.cs#L22) |
| <code>discord cancellation before READY never registers commands</code> | [L37](../../tests/ListingDiscordConnectionTests.cs#L37) |
| <code>discord command registration failure reports stage and safe numeric codes</code> | [L45](../../tests/ListingDiscordConnectionTests.cs#L45) |
| <code>discord invalid token response identifies bot information stage</code> | [L56](../../tests/ListingDiscordConnectionTests.cs#L56) |
| <code>discord diagnostics never include exception messages or request contents</code> | [L63](../../tests/ListingDiscordConnectionTests.cs#L63) |
| <code>discord SDK lists application emojis and uploads a missing embedded PNG after READY</code> | [L70](../../tests/ListingDiscordConnectionTests.cs#L70) |
| <code>discord emoji permission failures preserve connection and expose only safe codes</code> | [L84](../../tests/ListingDiscordConnectionTests.cs#L84) |
| <code>discord SDK migrates the legacy emoji name using PATCH and preserves its ID</code> | [L98](../../tests/ListingDiscordConnectionTests.cs#L98) |
| <code>discord full restart restores saved token and reuses all server emojis without uploads</code> | [L113](../../tests/ListingDiscordConnectionTests.cs#L113) |

<a id="file-tests-listingdiscordemojitests-cs"></a>
## tests/ListingDiscordEmojiTests.cs

[소스 파일](../../tests/ListingDiscordEmojiTests.cs) · 명명된 항목/진입점 13개

80개 asset·이름·재사용/legacy rename/upload·slot 제한 및 join 이미지 선택를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordemojitests-cs-6-8324f7e630"></a><code>ListingDiscordEmojiTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 80개 asset·이름·재사용/legacy rename/upload·slot 제한 및 join 이미지 선택 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingDiscordEmojiTests.cs#L6) |
| <a id="symbol-tests-listingdiscordemojitests-cs-11-1e6333b8cf"></a><code>ListingDiscordEmojiTests.Run.Icon</code><br><code>string Icon(string key)</code> | 로컬 함수 | 지정 asset key의 표시 이모지를 조회해 검증한다. | [L11](../../tests/ListingDiscordEmojiTests.cs#L11) |
| <a id="symbol-tests-listingdiscordemojitests-cs-12-6112ce05af"></a><code>ListingDiscordEmojiTests.Run.NoRename</code><br><code>Task&lt;Emote&gt; NoRename(ulong id, string name, CancellationToken ct)</code> | 로컬 함수 | 이 시나리오에서 rename이 불리지 않아야 함을 검증하는 대체 함수다. | [L12](../../tests/ListingDiscordEmojiTests.cs#L12) |
| <a id="symbol-tests-listingdiscordemojitests-cs-13-db7ed1cea7"></a><code>ListingDiscordEmojiTests.Run.Slot</code><br><code>WebSlot Slot(bool member, bool anyone, params uint[] jobs)</code> | 로컬 함수 | 표시/점유/허용 직업을 가진 WebSlot fixture를 만든다. | [L13](../../tests/ListingDiscordEmojiTests.cs#L13) |
| <a id="symbol-tests-listingdiscordemojitests-cs-41-e3122fa51a"></a><code>ListingDiscordEmojiTests.Run.Read</code><br><code>WebSlot Read(ulong flags, bool joined)</code> | 로컬 함수 | 테스트용 입력/asset/slot 결과를 읽는다. 해당 source 위치의 fixture 형태를 기준으로 사용한다. | [L41](../../tests/ListingDiscordEmojiTests.cs#L41) |
| <a id="symbol-tests-listingdiscordemojitests-cs-84-789464adb0"></a><code>ListingDiscordEmojiTests.Run.List</code><br><code>Task&lt;IReadOnlyCollection&lt;Emote&gt;&gt; List(CancellationToken ct)</code> | 로컬 함수 | fake application emoji 목록을 돌려주고 목록 조회 횟수를 기록한다. | [L84](../../tests/ListingDiscordEmojiTests.cs#L84) |
| <a id="symbol-tests-listingdiscordemojitests-cs-85-e26ad0a5ed"></a><code>ListingDiscordEmojiTests.Run.Create</code><br><code>Task&lt;Emote&gt; Create(string name, Stream stream, CancellationToken ct)</code> | 로컬 함수 | fake emoji 생성 요청과 이미지/name을 검사하고 생성 결과를 반환한다. | [L85](../../tests/ListingDiscordEmojiTests.cs#L85) |
| <a id="symbol-tests-listingdiscordemojitests-cs-128-3d0467f489"></a><code>ListingDiscordEmojiTests.Run.List</code><br><code>Task&lt;IReadOnlyCollection&lt;Emote&gt;&gt; List(CancellationToken ct)</code> | 로컬 함수 | fake application emoji 목록을 돌려주고 목록 조회 횟수를 기록한다. | [L128](../../tests/ListingDiscordEmojiTests.cs#L128) |
| <a id="symbol-tests-listingdiscordemojitests-cs-129-f79053378e"></a><code>ListingDiscordEmojiTests.Run.Create</code><br><code>Task&lt;Emote&gt; Create(string name, Stream stream, CancellationToken ct)</code> | 로컬 함수 | fake emoji 생성 요청과 이미지/name을 검사하고 생성 결과를 반환한다. | [L129](../../tests/ListingDiscordEmojiTests.cs#L129) |
| <a id="symbol-tests-listingdiscordemojitests-cs-134-e1a2b8cf07"></a><code>ListingDiscordEmojiTests.Run.Rename</code><br><code>Task&lt;Emote&gt; Rename(ulong id, string name, CancellationToken ct)</code> | 로컬 함수 | 기존 emoji ID를 보존한 fake 이름 변경 요청을 검사한다. | [L134](../../tests/ListingDiscordEmojiTests.cs#L134) |
| <a id="symbol-tests-listingdiscordemojitests-cs-149-d93ffcc17a"></a><code>ListingDiscordEmojiTests.Run.List</code><br><code>Task&lt;IReadOnlyCollection&lt;Emote&gt;&gt; List(CancellationToken ct)</code> | 로컬 함수 | fake application emoji 목록을 돌려주고 목록 조회 횟수를 기록한다. | [L149](../../tests/ListingDiscordEmojiTests.cs#L149) |
| <a id="symbol-tests-listingdiscordemojitests-cs-150-9e12febbd6"></a><code>ListingDiscordEmojiTests.Run.Create</code><br><code>Task&lt;Emote&gt; Create(string name, Stream stream, CancellationToken ct)</code> | 로컬 함수 | fake emoji 생성 요청과 이미지/name을 검사하고 생성 결과를 반환한다. | [L150](../../tests/ListingDiscordEmojiTests.cs#L150) |
| <a id="symbol-tests-listingdiscordemojitests-cs-151-6bd2f3bd27"></a><code>ListingDiscordEmojiTests.Run.Rename</code><br><code>Task&lt;Emote&gt; Rename(ulong id, string name, CancellationToken ct)</code> | 로컬 함수 | 기존 emoji ID를 보존한 fake 이름 변경 요청을 검사한다. | [L151](../../tests/ListingDiscordEmojiTests.cs#L151) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>discord emoji manifest embeds all 80 distinct PNGs with readable joined names</code> | [L16](../../tests/ListingDiscordEmojiTests.cs#L16) |
| <code>discord occupied TH stays TH until a new listing changes its flags to paladin</code> | [L37](../../tests/ListingDiscordEmojiTests.cs#L37) |
| <code>discord role combinations include classes and need only one allowed job per role</code> | [L54](../../tests/ListingDiscordEmojiTests.cs#L54) |
| <code>discord single jobs map all supplied classes jobs and occupied variants</code> | [L65](../../tests/ListingDiscordEmojiTests.cs#L65) |
| <code>discord unknown rules and unprepared emoji never invent a participant icon</code> | [L72](../../tests/ListingDiscordEmojiTests.cs#L72) |
| <code>discord emoji setup reuses bot-owned names uploads only missing and reuses on reconnect</code> | [L80](../../tests/ListingDiscordEmojiTests.cs#L80) |
| <code>discord emoji list failures never trigger uploads and separate bots never share IDs</code> | [L96](../../tests/ListingDiscordEmojiTests.cs#L96) |
| <code>discord cancellation keeps completed emoji registrations and prevents more uploads</code> | [L107](../../tests/ListingDiscordEmojiTests.cs#L107) |
| <code>discord all 48 real emoji slots remain within message limits</code> | [L117](../../tests/ListingDiscordEmojiTests.cs#L117) |
| <code>discord emoji migration renames all legacy entries in place and uploads only six role icons</code> | [L124](../../tests/ListingDiscordEmojiTests.cs#L124) |
| <code>discord emoji migration keeps current names and resumes after a partial rename failure</code> | [L145](../../tests/ListingDiscordEmojiTests.cs#L145) |
| <code>discord longest joined role names show all 48 occupied slots without fallback squares</code> | [L166](../../tests/ListingDiscordEmojiTests.cs#L166) |

<a id="file-tests-listingdiscordinteractiontests-cs"></a>
## tests/ListingDiscordInteractionTests.cs

[소스 파일](../../tests/ListingDiscordInteractionTests.cs) · 명명된 항목/진입점 17개

실제 SDK interaction JSON→모달/버튼/select→공통 Bridge의 end-to-end 경로를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordinteractiontests-cs-6-1a4f303c5c"></a><code>ListingDiscordConnectionTests.RunInteractions</code><br><code>void RunInteractions(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 실제 SDK interaction JSON→모달/버튼/select→공통 Bridge의 end-to-end 경로 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingDiscordInteractionTests.cs#L6) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-82-1f1d1881c0"></a><code>ListingDiscordConnectionTests.RunInteractions.Private</code><br><code>void Private(bool enabled, string password)</code> | 로컬 함수 | 비공개/PIN modal 입력 fixture를 만든다. | [L82](../../tests/ListingDiscordInteractionTests.cs#L82) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-125-ce4b74f8ab"></a><code>ListingDiscordConnectionTests.RunInteractions.Selected</code><br><code>string[] Selected()</code> | 로컬 함수 | 선택된 Discord component option 값을 수집한다. | [L125](../../tests/ListingDiscordInteractionTests.cs#L125) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-166-0bd8c0033f"></a><code>ListingDiscordConnectionTests.WalkJson</code><br><code>IEnumerable&lt;JsonElement&gt; WalkJson(JsonElement components)</code> | 메서드 | 중첩 interaction/component JSON을 재귀 순회한다. | [L166](../../tests/ListingDiscordInteractionTests.cs#L166) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-174-84499791d9"></a><code>ListingDiscordConnectionTests.TextOf</code><br><code>string TextOf(JsonElement data)</code> | 메서드 | Discord message JSON의 모든 표시 텍스트를 추출한다. | [L174](../../tests/ListingDiscordInteractionTests.cs#L174) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-186-fc682d029a"></a><code>ListingDiscordConnectionTests.InteractionFixture.Text</code><br><code>string Text</code> | 계산 속성 | 중첩 카드 component의 표시 텍스트를 모아 assertion에 제공한다. 계산식: <code>TextOf(Screen)</code>. | [L186](../../tests/ListingDiscordInteractionTests.cs#L186) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-187-d4ac5de186"></a><code>ListingDiscordConnectionTests.InteractionFixture.InteractionFixture</code><br><code>InteractionFixture()</code> | 생성자 | 이 파일의 검증에 사용할 fake host/transport/interaction 데이터를 초기화한다. 실제 게임 메모리에는 접근하지 않는다. | [L187](../../tests/ListingDiscordInteractionTests.cs#L187) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-196-28452a6a1c"></a><code>ListingDiscordConnectionTests.InteractionFixture.Open</code><br><code>void Open()</code> | 메서드 | fake Gateway를 통해 /pvkr 진입을 요청한다. | [L196](../../tests/ListingDiscordInteractionTests.cs#L196) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-204-a5981d6000"></a><code>ListingDiscordConnectionTests.InteractionFixture.Id</code><br><code>string Id(string action)</code> | 메서드 | 현재 카드에서 테스트할 동작 custom ID를 찾는다. | [L204](../../tests/ListingDiscordInteractionTests.cs#L204) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-206-6983d117ca"></a><code>ListingDiscordConnectionTests.InteractionFixture.Button</code><br><code>void Button(string action)</code> | 메서드 | 현재 카드의 버튼 interaction을 전송한다. | [L206](../../tests/ListingDiscordInteractionTests.cs#L206) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-207-1a6d85e2c5"></a><code>ListingDiscordConnectionTests.InteractionFixture.Navigate</code><br><code>void Navigate(string action)</code> | 메서드 | 테스트할 panel/card로 버튼 탐색을 진행한다. | [L207](../../tests/ListingDiscordInteractionTests.cs#L207) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-208-33759000c5"></a><code>ListingDiscordConnectionTests.InteractionFixture.Modal</code><br><code>string Modal(string form)</code> | 메서드 | modal interaction 응답을 읽는다. | [L208](../../tests/ListingDiscordInteractionTests.cs#L208) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-217-b4779eaff1"></a><code>ListingDiscordConnectionTests.InteractionFixture.Submit</code><br><code>void Submit(string id, object[] fields)</code> | 메서드 | 테스트 fixture에서는 modal 제출 또는 fake 네이티브 갱신 제출을 모사하며 호출 횟수/허용 여부/예외를 기록한다. | [L217](../../tests/ListingDiscordInteractionTests.cs#L217) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-223-3f6d9dfcd9"></a><code>ListingDiscordConnectionTests.InteractionFixture.Select</code><br><code>void Select(string action, params string[] values)</code> | 메서드 | select interaction payload를 전달한다. | [L223](../../tests/ListingDiscordInteractionTests.cs#L223) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-229-c431db9165"></a><code>ListingDiscordConnectionTests.InteractionFixture.ScreenAfter</code><br><code>void ScreenAfter(int before, bool controls = true)</code> | 메서드 | 지정 interaction 이후 만들어진 화면을 기다려 반환한다. | [L229](../../tests/ListingDiscordInteractionTests.cs#L229) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-245-3f07d5518e"></a><code>ListingDiscordConnectionTests.InteractionFixture.PumpUntil</code><br><code>void PumpUntil(Func&lt;bool&gt; completed)</code> | 메서드 | fake Framework/Bridge를 진행하며 원하는 비동기 조건이 성립할 때까지 기다린다. | [L245](../../tests/ListingDiscordInteractionTests.cs#L245) |
| <a id="symbol-tests-listingdiscordinteractiontests-cs-255-e58fc57331"></a><code>ListingDiscordConnectionTests.InteractionFixture.Dispose</code><br><code>void Dispose()</code> | 메서드 | 테스트 fixture의 fake 연결·Bridge·취소 token을 정리한다. | [L255](../../tests/ListingDiscordInteractionTests.cs#L255) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>discord Gateway buttons without values acknowledge settings</code> | [L8](../../tests/ListingDiscordInteractionTests.cs#L8) |
| <code>discord Gateway details use a new read-only message and preserve the overview</code> | [L24](../../tests/ListingDiscordInteractionTests.cs#L24) |
| <code>discord Gateway query and refresh buttons ACK before executing on framework</code> | [L39](../../tests/ListingDiscordInteractionTests.cs#L39) |
| <code>discord Gateway grouped radio and checkbox values survive modal navigation</code> | [L55](../../tests/ListingDiscordInteractionTests.cs#L55) |
| <code>discord Gateway private checkbox accepts false and keeps a blank reopened PIN</code> | [L80](../../tests/ListingDiscordInteractionTests.cs#L80) |
| <code>discord Gateway full results open separately and page through every item without replaying the edit</code> | [L92](../../tests/ListingDiscordInteractionTests.cs#L92) |
| <code>discord Gateway role presets include classes and five menus preserve every selection</code> | [L119](../../tests/ListingDiscordInteractionTests.cs#L119) |
| <code>discord Gateway unauthorized and stale buttons receive an error response</code> | [L149](../../tests/ListingDiscordInteractionTests.cs#L149) |

<a id="file-tests-listingdiscordnotificationsdktests-cs"></a>
## tests/ListingDiscordNotificationSdkTests.cs

[소스 파일](../../tests/ListingDiscordNotificationSdkTests.cs) · 명명된 항목/진입점 2개

실제 SDK의 서버 권한과 everyone POST 직렬화를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordnotificationsdktests-cs-7-0430313662"></a><code>ListingDiscordConnectionTests.RunNotificationSdk</code><br><code>void RunNotificationSdk(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 실제 Discord.Net SDK가 선택 채널로 고정 본문/everyone allowed_mentions를 한 번 전송하는지와 권한 부족 시 미전송을 확인한다. | [L7](../../tests/ListingDiscordNotificationSdkTests.cs#L7) |
| <a id="symbol-tests-listingdiscordnotificationsdktests-cs-55-d11715d992"></a><code>ListingDiscordConnectionTests.OfflineTransport.NotificationGuild</code><br><code>Task NotificationGuild(ulong permissions = 134144, ulong[]? channelIds = null)</code> | 메서드 | READY에 예고된 서버의 역할·텍스트 채널·봇 멤버 GUILD_CREATE를 모의 전송한다. everyone 권한 bit를 바꿔 허용/거부를 검증한다. | [L55](../../tests/ListingDiscordNotificationSdkTests.cs#L55) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>Discord SDK completion POST serializes exact everyone mention once to selected server channel</code> | [L9](../../tests/ListingDiscordNotificationSdkTests.cs#L9) |
| <code>Discord SDK rejects missing everyone channel permission before posting</code> | [L27](../../tests/ListingDiscordNotificationSdkTests.cs#L27) |
| <code>Discord SDK does not report success or repeat POST if the server did not confirm everyone</code> | [L38](../../tests/ListingDiscordNotificationSdkTests.cs#L38) |

<a id="file-tests-listingdiscordnotificationtests-cs"></a>
## tests/ListingDiscordNotificationTests.cs

[소스 파일](../../tests/ListingDiscordNotificationTests.cs) · 명명된 항목/진입점 9개

알림 채널 저장·소유자/ACK·everyone 전송·취소·오류 중복 방지를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordnotificationtests-cs-8-e35a357e3c"></a><code>ListingDiscordNotificationTests.Done</code><br><code>void Done(Task task)</code> | 메서드 | 비동기 알림/설정 작업을5초 예산으로 완료시키고 실패를 테스트에 전파한다. | [L8](../../tests/ListingDiscordNotificationTests.cs#L8) |
| <a id="symbol-tests-listingdiscordnotificationtests-cs-9-8940b0751d"></a><code>ListingDiscordNotificationTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 알림 채널 저장·소유자/ACK·everyone 전송·취소·오류 중복 방지 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L9](../../tests/ListingDiscordNotificationTests.cs#L9) |
| <a id="symbol-tests-listingdiscordnotificationtests-cs-11-37ecf42258"></a><code>ListingDiscordNotificationTests.Run.Service</code><br><code>ListingDiscordNotifications Service(NotificationFakeTransport t, DiscordNotificationTarget? target = null, Func&lt;DiscordNotificationTarget, CancellationToken, Task&gt;? save = null)</code> | 로컬 함수 | fake transport와 저장 callback으로 연결된 알림 controller를 만든다. | [L11](../../tests/ListingDiscordNotificationTests.cs#L11) |
| <a id="symbol-tests-listingdiscordnotificationtests-cs-110-842e05356e"></a><code>ListingDiscordNotificationTests.Run.Input</code><br><code>DiscordInput Input(ulong id)</code> | 로컬 함수 | owner/application 정보가 붙은 DiscordInput fixture를 만든다. | [L110](../../tests/ListingDiscordNotificationTests.cs#L110) |
| <a id="symbol-tests-listingdiscordnotificationtests-cs-123-1142f19fb8"></a><code>ListingDiscordNotificationTests.NotificationResponse.Defer</code><br><code>Task Defer()</code> | 메서드 | 정상 ACK 또는 실패 ACK를 모사한다. | [L123](../../tests/ListingDiscordNotificationTests.cs#L123) |
| <a id="symbol-tests-listingdiscordnotificationtests-cs-124-7c540c0a68"></a><code>ListingDiscordNotificationTests.NotificationResponse.Modal</code><br><code>Task Modal(Modal modal)</code> | 메서드 | 채널 선택에서 모달 호출이 발생하면 테스트를 실패시킨다. | [L124](../../tests/ListingDiscordNotificationTests.cs#L124) |
| <a id="symbol-tests-listingdiscordnotificationtests-cs-125-54e28baf7f"></a><code>ListingDiscordNotificationTests.NotificationResponse.Screen</code><br><code>Task Screen(DiscordScreen screen, bool replace, bool deferred)</code> | 메서드 | 채널 설정의 응답을 소비한다. 외부 메시지를 보내지 않는다. | [L125](../../tests/ListingDiscordNotificationTests.cs#L125) |
| <a id="symbol-tests-listingdiscordnotificationtests-cs-136-98cf3d8a0d"></a><code>ListingDiscordNotificationTests.NotificationFakeTransport.CheckChannel</code><br><code>Task&lt;string?&gt; CheckChannel(DiscordNotificationTarget target, CancellationToken cancellation)</code> | 메서드 | 권한 실패·중단 가능한 지연 확인을 모사하고 확인 시작 신호를 보낸다. | [L136](../../tests/ListingDiscordNotificationTests.cs#L136) |
| <a id="symbol-tests-listingdiscordnotificationtests-cs-142-c8dc955f55"></a><code>ListingDiscordNotificationTests.NotificationFakeTransport.SendCompletion</code><br><code>Task SendCompletion(DiscordNotificationTarget target, CancellationToken cancellation)</code> | 메서드 | 전송 횟수/목적지를 기록하고 결과 불확실 예외를 주입한다. 실제 네트워크는 사용하지 않는다. | [L142](../../tests/ListingDiscordNotificationTests.cs#L142) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>Discord completion is an everyone only mention with the requested fixed text</code> | [L15](../../tests/ListingDiscordNotificationTests.cs#L15) |
| <code>Discord notification target persists with bot identity while unrelated settings survive</code> | [L22](../../tests/ListingDiscordNotificationTests.cs#L22) |
| <code>Discord alerts require AFK completion and an explicitly selected matching bot channel</code> | [L29](../../tests/ListingDiscordNotificationTests.cs#L29) |
| <code>Discord repeated full roster polls send once and a new completion episode sends again</code> | [L38](../../tests/ListingDiscordNotificationTests.cs#L38) |
| <code>Discord preflight is cancelled when AFK stops a vacancy appears or the character changes</code> | [L46](../../tests/ListingDiscordNotificationTests.cs#L46) |
| <code>Discord temporary unreadability before POST can recover without losing the completion alert</code> | [L54](../../tests/ListingDiscordNotificationTests.cs#L54) |
| <code>Discord ambiguous notification POST failures never retry on every roster poll</code> | [L60](../../tests/ListingDiscordNotificationTests.cs#L60) |
| <code>Discord unavailable connection waits for reconnect without consuming an alert</code> | [L66](../../tests/ListingDiscordNotificationTests.cs#L66) |
| <code>Discord missing everyone permission blocks sending and channel selection without silent fallback</code> | [L71](../../tests/ListingDiscordNotificationTests.cs#L71) |
| <code>Discord DM selection and failed persistence preserve the old notification target</code> | [L80](../../tests/ListingDiscordNotificationTests.cs#L80) |
| <code>Discord channel selection alone never posts a message but can deliver a pending full roster</code> | [L89](../../tests/ListingDiscordNotificationTests.cs#L89) |
| <code>Discord cancellation while waiting for settings save never activates the selected channel</code> | [L96](../../tests/ListingDiscordNotificationTests.cs#L96) |
| <code>Discord notification channel controls reject wrong owner app stale session and failed ACK</code> | [L106](../../tests/ListingDiscordNotificationTests.cs#L106) |

<a id="file-tests-listingdiscordresulttests-cs"></a>
## tests/ListingDiscordResultTests.cs

[소스 파일](../../tests/ListingDiscordResultTests.cs) · 명명된 항목/진입점 1개

긴 결과 페이지의 무손실 분할, escape/Unicode 경계·읽기 전용 탐색를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordresulttests-cs-7-f698b24185"></a><code>ListingDiscordTests.RunResults</code><br><code>void RunResults(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 긴 결과 페이지의 무손실 분할, escape/Unicode 경계·읽기 전용 탐색 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L7](../../tests/ListingDiscordResultTests.cs#L7) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>discord complete results preserve every status, long line, emoji and Markdown character across pages</code> | [L9](../../tests/ListingDiscordResultTests.cs#L9) |
| <code>discord results are read-only and do not discard a draft or accept stale page controls</code> | [L38](../../tests/ListingDiscordResultTests.cs#L38) |
| <code>discord latest operation remains fully readable after another query</code> | [L56](../../tests/ListingDiscordResultTests.cs#L56) |
| <code>discord unified editor groups every setting and removes standalone toggle controls</code> | [L77](../../tests/ListingDiscordResultTests.cs#L77) |

<a id="file-tests-listingdiscordrostertests-cs"></a>
## tests/ListingDiscordRosterTests.cs

[소스 파일](../../tests/ListingDiscordRosterTests.cs) · 명명된 항목/진입점 1개

24/48인/친선 전체 그룹 아이콘과 설정/상대시간 표시를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordrostertests-cs-6-ecf1a31140"></a><code>ListingDiscordTests.RunRoster</code><br><code>void RunRoster(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 24/48인/친선 전체 그룹 아이콘과 설정/상대시간 표시 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingDiscordRosterTests.cs#L6) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>discord compact roster uses checked recruitment conditions and safe placeholders when assets are missing</code> | [L8](../../tests/ListingDiscordRosterTests.cs#L8) |
| <code>discord job details preserve draft and full overview and reject forged editing actions</code> | [L25](../../tests/ListingDiscordRosterTests.cs#L25) |
| <code>discord job details preserve friendly and sparse alliance groups and never truncate allowed job lists</code> | [L50](../../tests/ListingDiscordRosterTests.cs#L50) |
| <code>discord relative expiry stays anchored while navigating and updates only from a new snapshot</code> | [L80](../../tests/ListingDiscordRosterTests.cs#L80) |
| <code>discord never invents a countdown or allows detail controls after expiry or logout</code> | [L96](../../tests/ListingDiscordRosterTests.cs#L96) |

<a id="file-tests-listingdiscordtests-cs"></a>
## tests/ListingDiscordTests.cs

[소스 파일](../../tests/ListingDiscordTests.cs) · 명명된 항목/진입점 12개

owner/nonce/session·폼 유효성·직업 분류·중복 요청·ACK 선행를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingdiscordtests-cs-10-5ca07a5164"></a><code>ListingDiscordTests.Input</code><br><code>DiscordInput Input(string custom = "", string[]? values = null, Dictionary&lt;string, string&gt;? fields = null)</code> | 메서드 | owner/application 정보가 붙은 DiscordInput fixture를 만든다. | [L10](../../tests/ListingDiscordTests.cs#L10) |
| <a id="symbol-tests-listingdiscordtests-cs-12-cb79673791"></a><code>ListingDiscordTests.Id</code><br><code>string Id(DiscordScreen screen, string action)</code> | 메서드 | 현재 카드에서 테스트할 동작 custom ID를 찾는다. | [L12](../../tests/ListingDiscordTests.cs#L12) |
| <a id="symbol-tests-listingdiscordtests-cs-15-5bb11a1c12"></a><code>ListingDiscordTests.Menu</code><br><code>SelectMenuComponent Menu(DiscordScreen screen, string action)</code> | 메서드 | 현재 카드에서 select menu를 찾는다. | [L15](../../tests/ListingDiscordTests.cs#L15) |
| <a id="symbol-tests-listingdiscordtests-cs-17-7375a43309"></a><code>ListingDiscordTests.Act</code><br><code>DiscordScreen Act(ListingDiscordPanel panel, DiscordScreen screen, string action, params string[] values)</code> | 메서드 | 버튼/선택 입력을 panel.Handle에 전달하고 결정된 응답을 검사한다. | [L17](../../tests/ListingDiscordTests.cs#L17) |
| <a id="symbol-tests-listingdiscordtests-cs-19-70bd43c868"></a><code>ListingDiscordTests.Field</code><br><code>DiscordScreen Field(ListingDiscordPanel panel, DiscordScreen screen, string field, string value)</code> | 메서드 | modal에서 지정 필드 component를 찾아 반환한다. | [L19](../../tests/ListingDiscordTests.cs#L19) |
| <a id="symbol-tests-listingdiscordtests-cs-31-838b614f51"></a><code>ListingDiscordTests.Settings</code><br><code>DiscordScreen Settings(ListingDiscordPanel panel, DiscordScreen screen)</code> | 메서드 | 설정 modal의 선택값 dictionary를 만든다. | [L31](../../tests/ListingDiscordTests.cs#L31) |
| <a id="symbol-tests-listingdiscordtests-cs-34-110b2729fc"></a><code>ListingDiscordTests.Text</code><br><code>DiscordScreen Text(ListingDiscordPanel panel, DiscordScreen screen, string field, string text)</code> | 메서드 | 중첩 카드 component의 표시 텍스트를 모아 assertion에 제공한다. | [L34](../../tests/ListingDiscordTests.cs#L34) |
| <a id="symbol-tests-listingdiscordtests-cs-44-1262c19723"></a><code>ListingDiscordTests.FormInput</code><br><code>DiscordInput FormInput(Modal modal)</code> | 메서드 | modal 제출을 모사하는 DiscordInput을 만든다. | [L44](../../tests/ListingDiscordTests.cs#L44) |
| <a id="symbol-tests-listingdiscordtests-cs-57-8cb8220d09"></a><code>ListingDiscordTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | owner/nonce/session·폼 유효성·직업 분류·중복 요청·ACK 선행 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L57](../../tests/ListingDiscordTests.cs#L57) |
| <a id="symbol-tests-listingdiscordtests-cs-270-837519e45c"></a><code>ListingDiscordTests.FakeDiscordResponse.Defer</code><br><code>Task Defer()</code> | 메서드 | 최초 Discord ACK의 횟수·순서와 테스트용 지연/실패를 기록한다. | [L270](../../tests/ListingDiscordTests.cs#L270) |
| <a id="symbol-tests-listingdiscordtests-cs-271-72e8bba8ac"></a><code>ListingDiscordTests.FakeDiscordResponse.Modal</code><br><code>Task Modal(Modal modal)</code> | 메서드 | modal interaction 응답을 읽는다. | [L271](../../tests/ListingDiscordTests.cs#L271) |
| <a id="symbol-tests-listingdiscordtests-cs-272-9c828792cb"></a><code>ListingDiscordTests.FakeDiscordResponse.Screen</code><br><code>Task Screen(DiscordScreen screen, bool replace, bool deferred)</code> | 메서드 | 보낸 카드와 update/응답 정보를 테스트 기록에 저장한다. | [L272](../../tests/ListingDiscordTests.cs#L272) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>discord rejects wrong owner or app without reading or queuing a game action</code> | [L63](../../tests/ListingDiscordTests.cs#L63) |
| <code>discord query uses the shared framework queue and shows the result</code> | [L71](../../tests/ListingDiscordTests.cs#L71) |
| <code>discord stale screens, expired drafts and changed character cannot edit</code> | [L79](../../tests/ListingDiscordTests.cs#L79) |
| <code>discord slash command keeps a current draft but recovers from a stale draft</code> | [L90](../../tests/ListingDiscordTests.cs#L90) |
| <code>discord combines all settings and preserves PIN 0000 without displaying it</code> | [L97](../../tests/ListingDiscordTests.cs#L97) |
| <code>discord validates UTF8 byte limit, item-level bounds and four ASCII PIN digits</code> | [L124](../../tests/ListingDiscordTests.cs#L124) |
| <code>discord job selector preserves 4, 24, 48 and friendly native positions</code> | [L136](../../tests/ListingDiscordTests.cs#L136) |
| <code>discord five role menus include every job and class and preserve other selections</code> | [L153](../../tests/ListingDiscordTests.cs#L153) |
| <code>discord rejects occupied, unregistered and forged job IDs</code> | [L191](../../tests/ListingDiscordTests.cs#L191) |
| <code>discord output fits message/component limits with long 48-slot changes</code> | [L197](../../tests/ListingDiscordTests.cs#L197) |
| <code>discord failed ACK and shutdown during ACK never enqueue a mutation</code> | [L204](../../tests/ListingDiscordTests.cs#L204) |
| <code>discord busy and duplicate events never replay queries</code> | [L218](../../tests/ListingDiscordTests.cs#L218) |
| <code>discord acknowledged refresh is executed once on the framework and returns a usable screen</code> | [L230](../../tests/ListingDiscordTests.cs#L230) |
| <code>discord unsupported setting is skipped and reports remaining successful changes</code> | [L250](../../tests/ListingDiscordTests.cs#L250) |

<a id="file-tests-listingeditorcriteriatests-cs"></a>
## tests/ListingEditorCriteriaTests.cs

[소스 파일](../../tests/ListingEditorCriteriaTests.cs) · 명명된 항목/진입점 6개

친선 padding에서만 raw mask 차이를 허용하고 실제 미등록/멤버 변경은 차단를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingeditorcriteriatests-cs-9-c67cf0f781"></a><code>ListingEditorCriteriaTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 친선 padding에서만 raw mask 차이를 허용하고 실제 미등록/멤버 변경은 차단 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L9](../../tests/ListingEditorCriteriaTests.cs#L9) |
| <a id="symbol-tests-listingeditorcriteriatests-cs-14-af06638cb3"></a><code>ListingEditorCriteriaTests.Run.Criteria</code><br><code>string Criteria(ulong[] flags)</code> | 로컬 함수 | 요구 슬롯 mask 배열을 비교용 Criteria 문자열로 만든다. | [L14](../../tests/ListingEditorCriteriaTests.cs#L14) |
| <a id="symbol-tests-listingeditorcriteriatests-cs-15-94f0feedac"></a><code>ListingEditorCriteriaTests.Run.Target</code><br><code>RefreshTarget Target(ulong[] flags, byte join = 0x13)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L15](../../tests/ListingEditorCriteriaTests.cs#L15) |
| <a id="symbol-tests-listingeditorcriteriatests-cs-27-3ba3144129"></a><code>ListingEditorCriteriaTests.Run.CapturedListing</code><br><code>ulong[] CapturedListing()</code> | 로컬 함수 | 사용자가 제공한 친선 상세 snapshot 형태를 재현한다. | [L27](../../tests/ListingEditorCriteriaTests.cs#L27) |
| <a id="symbol-tests-listingeditorcriteriatests-cs-33-e81e0d65b9"></a><code>ListingEditorCriteriaTests.Run.EditorFlags</code><br><code>ulong[] EditorFlags(ulong[] listing)</code> | 로컬 함수 | 수정창에 나타나는 친선 mask/padding fixture를 만든다. | [L33](../../tests/ListingEditorCriteriaTests.cs#L33) |
| <a id="symbol-tests-listingeditorcriteriatests-cs-39-acae80fc71"></a><code>ListingEditorCriteriaTests.Run.EditorCriteria</code><br><code>string EditorCriteria(RefreshTarget target)</code> | 로컬 함수 | 수정창 flags를 비교용 Criteria 문자열로 만든다. | [L39](../../tests/ListingEditorCriteriaTests.cs#L39) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>captured friendly 17-slot listing accepts only the six unused editor defaults</code> | [L46](../../tests/ListingEditorCriteriaTests.cs#L46) |
| <code>every real friendly position and extra unregistered slot remains an exact comparison</code> | [L58](../../tests/ListingEditorCriteriaTests.cs#L58) |
| <code>unused friendly positions reject unobserved masks instead of ignoring arbitrary values</code> | [L69](../../tests/ListingEditorCriteriaTests.cs#L69) |
| <code>ordinary 24 and 48 groups do not inherit the friendly unused-slot exception</code> | [L78](../../tests/ListingEditorCriteriaTests.cs#L78) |
| <code>friendly padding never excuses changed conditions or an unverified shape</code> | [L91](../../tests/ListingEditorCriteriaTests.cs#L91) |
| <code>job edits retain zero listing padding while validating native defaults before submission</code> | [L106](../../tests/ListingEditorCriteriaTests.cs#L106) |
| <code>all shared edits validate the friendly editor without changing their server expectations</code> | [L125](../../tests/ListingEditorCriteriaTests.cs#L125) |

<a id="file-tests-listingfriendlymatchtests-cs"></a>
## tests/ListingFriendlyMatchTests.cs

[소스 파일](../../tests/ListingFriendlyMatchTests.cs) · 명명된 항목/진입점 5개

5+5+8 친선 등록 자리/물리 배열·편집 경로 및 미등록 축소를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingfriendlymatchtests-cs-12-81b3a78505"></a><code>ListingFriendlyMatchTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 5+5+8 친선 등록 자리/물리 배열·편집 경로 및 미등록 축소 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L12](../../tests/ListingFriendlyMatchTests.cs#L12) |
| <a id="symbol-tests-listingfriendlymatchtests-cs-17-9403c5d0e0"></a><code>ListingFriendlyMatchTests.Run.Arrays</code><br><code>(byte[] jobs, ulong[] members, ulong[] flags) Arrays()</code> | 로컬 함수 | 그룹별 raw job/member/mask 배열을 만든다. | [L17](../../tests/ListingFriendlyMatchTests.cs#L17) |
| <a id="symbol-tests-listingfriendlymatchtests-cs-24-7aa73aa379"></a><code>ListingFriendlyMatchTests.Run.Read</code><br><code>ListingSlots Read(byte[] jobs, ulong[] members, ulong[] flags, int filled = 1)</code> | 로컬 함수 | 테스트용 입력/asset/slot 결과를 읽는다. 해당 source 위치의 fixture 형태를 기준으로 사용한다. | [L24](../../tests/ListingFriendlyMatchTests.cs#L24) |
| <a id="symbol-tests-listingfriendlymatchtests-cs-26-1ebf1ab39e"></a><code>ListingFriendlyMatchTests.Run.Target</code><br><code>RefreshTarget Target()</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L26](../../tests/ListingFriendlyMatchTests.cs#L26) |
| <a id="symbol-tests-listingfriendlymatchtests-cs-34-e8893d3cf5"></a><code>ListingFriendlyMatchTests.Run.ReplaceRaw</code><br><code>RefreshTarget ReplaceRaw(RefreshTarget target, int index, ulong raw)</code> | 로컬 함수 | 지정 물리 자리 mask를 바꾼 source fixture를 만든다. | [L34](../../tests/ListingFriendlyMatchTests.cs#L34) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>friendly detail shows 5 plus 5 plus 8 and retains the last spectator's registered job</code> | [L42](../../tests/ListingFriendlyMatchTests.cs#L42) |
| <code>friendly arrays require all 24 native slots, including the final six spectators</code> | [L56](../../tests/ListingFriendlyMatchTests.cs#L56) |
| <code>every friendly visible row routes to the traced group button and param 8</code> | [L71](../../tests/ListingFriendlyMatchTests.cs#L71) |
| <code>friendly edits reject a packed mapping and allow the shared settings path</code> | [L83](../../tests/ListingFriendlyMatchTests.cs#L83) |
| <code>friendly member guards compare native indices across both teams and all spectators</code> | [L94](../../tests/ListingFriendlyMatchTests.cs#L94) |
| <code>friendly expected edits change exactly the intended native slot including spectator 8</code> | [L105](../../tests/ListingFriendlyMatchTests.cs#L105) |
| <code>friendly result verification rejects packed-index edits, padding changes and spectator arrivals</code> | [L120](../../tests/ListingFriendlyMatchTests.cs#L120) |
| <code>friendly refresh simulation submits and verifies each group boundary exactly once</code> | [L130](../../tests/ListingFriendlyMatchTests.cs#L130) |
| <code>friendly job changes do not reset anyone or toggle already excluded jobs</code> | [L142](../../tests/ListingFriendlyMatchTests.cs#L142) |
| <code>friendly changes add replacement jobs before removing the last current job</code> | [L151](../../tests/ListingFriendlyMatchTests.cs#L151) |

<a id="file-tests-listingitemleveledittests-cs"></a>
## tests/ListingItemLevelEditTests.cs

[소스 파일](../../tests/ListingItemLevelEditTests.cs) · 명명된 항목/진입점 4개

평균 레벨0/1~999·토글/숫자 UI 순서·지연/유효 범위를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingitemleveledittests-cs-6-632e977cc6"></a><code>ListingItemLevelEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 평균 레벨0/1~999·토글/숫자 UI 순서·지연/유효 범위 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingItemLevelEditTests.cs#L6) |
| <a id="symbol-tests-listingitemleveledittests-cs-8-983d3492a4"></a><code>ListingItemLevelEditTests.Run.Target</code><br><code>RefreshTarget Target(int level = 700, int count = 8)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L8](../../tests/ListingItemLevelEditTests.cs#L8) |
| <a id="symbol-tests-listingitemleveledittests-cs-19-331c5e621c"></a><code>ListingItemLevelEditTests.Run.State</code><br><code>ItemLevelUiState State(bool enabled, int stored = 1, int? numeric = null, bool? ready = null)</code> | 로컬 함수 | 테스트할 numeric/checkbox UI state를 생성한다. | [L19](../../tests/ListingItemLevelEditTests.cs#L19) |
| <a id="symbol-tests-listingitemleveledittests-cs-21-13a3d9c9d3"></a><code>ListingItemLevelEditTests.Run.Start</code><br><code>(FakeHost h, ListingRefresh f) Start(int original = 700, int requested = 555, int count = 8, int seconds = 1800)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L21](../../tests/ListingItemLevelEditTests.cs#L21) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>item level accepts exactly the 1 to 999 range without clamping invalid input</code> | [L29](../../tests/ListingItemLevelEditTests.cs#L29) |
| <code>item level draft uses the listing requirement and preserves other conditions</code> | [L45](../../tests/ListingItemLevelEditTests.cs#L45) |
| <code>enabling item level waits for checkbox storage then enters the requested number once</code> | [L62](../../tests/ListingItemLevelEditTests.cs#L62) |
| <code>disabling item level preserves a cached number and requires the stored enabled flag</code> | [L71](../../tests/ListingItemLevelEditTests.cs#L71) |
| <code>enabling an already matching cached level does not send redundant numeric input</code> | [L81](../../tests/ListingItemLevelEditTests.cs#L81) |
| <code>number-only item level edits keep the checkbox on and cover both boundaries</code> | [L90](../../tests/ListingItemLevelEditTests.cs#L90) |
| <code>unknown item level state, stale controls and game range restrictions dispatch nothing</code> | [L101](../../tests/ListingItemLevelEditTests.cs#L101) |
| <code>missing input or stored changes time out without retry and UI mismatches fail</code> | [L113](../../tests/ListingItemLevelEditTests.cs#L113) |
| <code>item level on off and number-only edits preserve 4 8 24 and 48 slot listings</code> | [L127](../../tests/ListingItemLevelEditTests.cs#L127) |
| <code>unchanged unknown or unsupported item level edits do not query</code> | [L146](../../tests/ListingItemLevelEditTests.cs#L146) |
| <code>item level cannot be combined with any other edit</code> | [L158](../../tests/ListingItemLevelEditTests.cs#L158) |
| <code>stale item level drafts detect changed requirements participants and other conditions</code> | [L170](../../tests/ListingItemLevelEditTests.cs#L170) |
| <code>item level failures timeouts cancellation and editor or character changes never submit or retry</code> | [L181](../../tests/ListingItemLevelEditTests.cs#L181) |
| <code>wrong item level and unrelated changes fail both pre-submit and fresh result checks</code> | [L204](../../tests/ListingItemLevelEditTests.cs#L204) |
| <code>item level result confirmation is separate from timer evidence and refuses create mode</code> | [L227](../../tests/ListingItemLevelEditTests.cs#L227) |

<a id="file-tests-listingjobedittests-cs"></a>
## tests/ListingJobEditTests.cs

[소스 파일](../../tests/ListingJobEditTests.cs) · 명명된 항목/진입점 6개

직업 변경 원본/빈자리·known mask·raw mask 보존 및 최종 결과를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingjobedittests-cs-6-8903d365cd"></a><code>ListingJobEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 직업 변경 원본/빈자리·known mask·raw mask 보존 및 최종 결과 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingJobEditTests.cs#L6) |
| <a id="symbol-tests-listingjobedittests-cs-10-27bf05ade2"></a><code>ListingJobEditTests.Run.Target</code><br><code>RefreshTarget Target()</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L10](../../tests/ListingJobEditTests.cs#L10) |
| <a id="symbol-tests-listingjobedittests-cs-20-a0274066af"></a><code>ListingJobEditTests.Run.Edit</code><br><code>ListingJobEdit Edit(RefreshTarget t)</code> | 로컬 함수 | 검증할 필드의 typed edit fixture를 만든다. | [L20](../../tests/ListingJobEditTests.cs#L20) |
| <a id="symbol-tests-listingjobedittests-cs-21-983b317f5f"></a><code>ListingJobEditTests.Run.ReplaceSlot</code><br><code>RefreshTarget ReplaceSlot(RefreshTarget t, int index, ListingSlot slot)</code> | 로컬 함수 | 지정 등록 자리 정보를 교체한 snapshot을 만든다. | [L21](../../tests/ListingJobEditTests.cs#L21) |
| <a id="symbol-tests-listingjobedittests-cs-23-618ab42a8d"></a><code>ListingJobEditTests.Run.WithFlags</code><br><code>RefreshTarget WithFlags(RefreshTarget target, int index, ulong raw)</code> | 로컬 함수 | 지정 mask를 포함한 edit/source fixture를 만든다. | [L23](../../tests/ListingJobEditTests.cs#L23) |
| <a id="symbol-tests-listingjobedittests-cs-32-ee859d3c9b"></a><code>ListingJobEditTests.Run.Start</code><br><code>(FakeHost host, ListingRefresh flow, ListingJobEdit edit) Start(RefreshTarget? original = null, ulong? selected = null)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L32](../../tests/ListingJobEditTests.cs#L32) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>only the edited slot adopts the game's raw value; other slots and settings stay exact</code> | [L40](../../tests/ListingJobEditTests.cs#L40) |
| <code>zero, unchanged, unknown, occupied and unsupported slot edits never start</code> | [L53](../../tests/ListingJobEditTests.cs#L53) |
| <code>fresh query detects changed members, jobs, listing identity and settings before editor</code> | [L68](../../tests/ListingJobEditTests.cs#L68) |
| <code>job edit submits once and confirms the selected flags through fresh detail</code> | [L81](../../tests/ListingJobEditTests.cs#L81) |
| <code>role failures, timeouts, cancellation and editor closure never submit</code> | [L91](../../tests/ListingJobEditTests.cs#L91) |
| <code>an unexpected edit result cannot be submitted</code> | [L103](../../tests/ListingJobEditTests.cs#L103) |
| <code>opening a role editor that fails or throws is never retried</code> | [L107](../../tests/ListingJobEditTests.cs#L107) |
| <code>post-edit old flags or new occupants cannot be reported as success</code> | [L117](../../tests/ListingJobEditTests.cs#L117) |
| <code>anyone to restricted accepts the raw flags produced by the game</code> | [L127](../../tests/ListingJobEditTests.cs#L127) |
| <code>restricted to anyone accepts normalization between editor and fresh listing</code> | [L140](../../tests/ListingJobEditTests.cs#L140) |
| <code>normalization never hides wrong jobs, another slot's bits, members or other settings</code> | [L152](../../tests/ListingJobEditTests.cs#L152) |

<a id="file-tests-listingjobrulestests-cs"></a>
## tests/ListingJobRulesTests.cs

[소스 파일](../../tests/ListingJobRulesTests.cs) · 명명된 항목/진입점 2개

직업 카탈로그 완전성·bit 매핑·순서·누구나 가능 판정를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingjobrulestests-cs-5-e2549edfea"></a><code>ListingJobRulesTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 직업 카탈로그 완전성·bit 매핑·순서·누구나 가능 판정 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L5](../../tests/ListingJobRulesTests.cs#L5) |
| <a id="symbol-tests-listingjobrulestests-cs-7-03149a2137"></a><code>ListingJobRulesTests.Run.Jobs</code><br><code>RecruitmentJob[] Jobs(bool include43 = false)</code> | 로컬 함수 | 직업 규칙 테스트의 ClassJob row 집합을 생성한다. | [L7](../../tests/ListingJobRulesTests.cs#L7) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>recruitment flags map to job rows rather than bit indices</code> | [L11](../../tests/ListingJobRulesTests.cs#L11) |
| <code>anyone requires every runtime battle class and job including limited jobs</code> | [L23](../../tests/ListingJobRulesTests.cs#L23) |
| <code>all jobs follow the runtime catalog instead of the SDK job count</code> | [L31](../../tests/ListingJobRulesTests.cs#L31) |
| <code>tooltip lists every selected job without role summaries</code> | [L38](../../tests/ListingJobRulesTests.cs#L38) |
| <code>tooltips and editor choices share the complete requested job order</code> | [L48](../../tests/ListingJobRulesTests.cs#L48) |
| <code>empty or unmapped runtime catalogs cannot claim anyone</code> | [L54](../../tests/ListingJobRulesTests.cs#L54) |
| <code>empty slot restrictions are copied without replacing occupied registered jobs</code> | [L60](../../tests/ListingJobRulesTests.cs#L60) |

<a id="file-tests-listingobjectiveedittests-cs"></a>
## tests/ListingObjectiveEditTests.cs

[소스 파일](../../tests/ListingObjectiveEditTests.cs) · 명명된 항목/진입점 3개

목적 index/bit·원본 조건 보존·결과 검증를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingobjectiveedittests-cs-6-a66c0b280c"></a><code>ListingObjectiveEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 목적 index/bit·원본 조건 보존·결과 검증 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingObjectiveEditTests.cs#L6) |
| <a id="symbol-tests-listingobjectiveedittests-cs-9-a3ad7386f5"></a><code>ListingObjectiveEditTests.Run.Target</code><br><code>RefreshTarget Target(int count = 8, byte objective = 1)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L9](../../tests/ListingObjectiveEditTests.cs#L9) |
| <a id="symbol-tests-listingobjectiveedittests-cs-19-c463574a1b"></a><code>ListingObjectiveEditTests.Run.Start</code><br><code>(FakeHost host, ListingRefresh flow, ListingObjectiveEdit edit) Start(int selected = 2, int count = 8, byte original = 1)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L19](../../tests/ListingObjectiveEditTests.cs#L19) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>objective menu indices match KR labels and native flags, including None=1</code> | [L28](../../tests/ListingObjectiveEditTests.cs#L28) |
| <code>objective draft prefills current selection and does not mutate other settings</code> | [L39](../../tests/ListingObjectiveEditTests.cs#L39) |
| <code>manual trace keeps nested before/after order and stops without losing evidence</code> | [L49](../../tests/ListingObjectiveEditTests.cs#L49) |
| <code>manual trace times out even when the user sends no more events</code> | [L57](../../tests/ListingObjectiveEditTests.cs#L57) |
| <code>manual trace bounds a flood without evicting initial routing evidence</code> | [L65](../../tests/ListingObjectiveEditTests.cs#L65) |
| <code>the reported Practice value after a Loot request still blocks submission</code> | [L73](../../tests/ListingObjectiveEditTests.cs#L73) |
| <code>every purpose transition including clearing sends once and verifies 4/8/24/48 listings</code> | [L80](../../tests/ListingObjectiveEditTests.cs#L80) |
| <code>invalid, unchanged, unknown, PvP or combined objective requests dispatch no actions</code> | [L97](../../tests/ListingObjectiveEditTests.cs#L97) |
| <code>stale objective, description, participants or flags block before opening the game editor</code> | [L112](../../tests/ListingObjectiveEditTests.cs#L112) |
| <code>failed, timed out, cancelled, closed or rebound objective editing never submits</code> | [L124](../../tests/ListingObjectiveEditTests.cs#L124) |
| <code>failed or thrown purpose operations are not retried</code> | [L138](../../tests/ListingObjectiveEditTests.cs#L138) |
| <code>old objective, changed beginner flag, description or alliance slot cannot pass verification</code> | [L148](../../tests/ListingObjectiveEditTests.cs#L148) |
| <code>verified purpose and unchanged timer are reported separately</code> | [L170](../../tests/ListingObjectiveEditTests.cs#L170) |
| <code>objective edit does not run from a new-party screen</code> | [L176](../../tests/ListingObjectiveEditTests.cs#L176) |

<a id="file-tests-listingobjectiveuitests-cs"></a>
## tests/ListingObjectiveUiTests.cs

[소스 파일](../../tests/ListingObjectiveUiTests.cs) · 명명된 항목/진입점 2개

dropdown open→click→select 순서와1회 동작·timeout를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingobjectiveuitests-cs-5-09661ddc39"></a><code>ListingObjectiveUiTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | dropdown open→click→select 순서와1회 동작·timeout 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L5](../../tests/ListingObjectiveUiTests.cs#L5) |
| <a id="symbol-tests-listingobjectiveuitests-cs-7-f64d3106b0"></a><code>ListingObjectiveUiTests.Run.Closed</code><br><code>ObjectiveUiState Closed(int index, byte stored)</code> | 로컬 함수 | 닫힌 dropdown/선택 상태 fixture를 만든다. | [L7](../../tests/ListingObjectiveUiTests.cs#L7) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>manual KR trace transitions require opening, item click, selection, then settled UI and draft</code> | [L9](../../tests/ListingObjectiveUiTests.cs#L9) |
| <code>dropdown that never opens times out without clicking or retrying</code> | [L31](../../tests/ListingObjectiveUiTests.cs#L31) |
| <code>UI-only, draft-only and unsettled selection never qualify for submission</code> | [L39](../../tests/ListingObjectiveUiTests.cs#L39) |
| <code>wrong purpose from reported failure cannot pass the UI sequence</code> | [L51](../../tests/ListingObjectiveUiTests.cs#L51) |
| <code>unexpected click behavior stops before sending a highlight or fallback callback</code> | [L58](../../tests/ListingObjectiveUiTests.cs#L58) |
| <code>stale, open, unknown or inconsistent starting controls send no events</code> | [L67](../../tests/ListingObjectiveUiTests.cs#L67) |
| <code>user changing purpose while opening or after verification stops the sequence</code> | [L75](../../tests/ListingObjectiveUiTests.cs#L75) |

<a id="file-tests-listingprivateedittests-cs"></a>
## tests/ListingPrivateEditTests.cs

[소스 파일](../../tests/ListingPrivateEditTests.cs) · 명명된 항목/진입점 3개

공개 sentinel10000·0000 포함 PIN·체크/입력 순서·비밀값 비노출를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingprivateedittests-cs-7-c9e08849f8"></a><code>ListingPrivateEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 공개 sentinel10000·0000 포함 PIN·체크/입력 순서·비밀값 비노출 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L7](../../tests/ListingPrivateEditTests.cs#L7) |
| <a id="symbol-tests-listingprivateedittests-cs-9-48ed43052a"></a><code>ListingPrivateEditTests.Run.Target</code><br><code>RefreshTarget Target(bool enabled = false, int count = 8)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L9](../../tests/ListingPrivateEditTests.cs#L9) |
| <a id="symbol-tests-listingprivateedittests-cs-21-4d4695cf10"></a><code>ListingPrivateEditTests.Run.Start</code><br><code>(FakeHost h, ListingRefresh f) Start(bool original = false, bool enabled = true, int count = 8, int seconds = 1800)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L21](../../tests/ListingPrivateEditTests.cs#L21) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>private PIN requires exactly four ASCII digits and keeps leading zeroes</code> | [L29](../../tests/ListingPrivateEditTests.cs#L29) |
| <code>explicit 0000 is distinct from blank input and still requires numeric dispatch and verification</code> | [L43](../../tests/ListingPrivateEditTests.cs#L43) |
| <code>private flag matches native metadata and preserves all unrelated join bits</code> | [L59](../../tests/ListingPrivateEditTests.cs#L59) |
| <code>private drafts never invent a password and off needs no password</code> | [L74](../../tests/ListingPrivateEditTests.cs#L74) |
| <code>private enable waits for checkbox storage then sends one numeric change</code> | [L83](../../tests/ListingPrivateEditTests.cs#L83) |
| <code>private disable emits no numeric change and waits for public sentinel</code> | [L93](../../tests/ListingPrivateEditTests.cs#L93) |
| <code>password-only updates never toggle off and on or retry unchanged input</code> | [L100](../../tests/ListingPrivateEditTests.cs#L100) |
| <code>unknown state, wrong UI, missing storage and unavailable input fail without retry</code> | [L109](../../tests/ListingPrivateEditTests.cs#L109) |
| <code>private on, off and password-only edits submit once on all supported layouts</code> | [L125](../../tests/ListingPrivateEditTests.cs#L125) |
| <code>stale or unsupported private drafts never open the editor</code> | [L142](../../tests/ListingPrivateEditTests.cs#L142) |
| <code>private edit excludes every other edit type</code> | [L153](../../tests/ListingPrivateEditTests.cs#L153) |
| <code>private failure, closure, timeout, cancellation or character change never submits</code> | [L164](../../tests/ListingPrivateEditTests.cs#L164) |
| <code>private dispatch failure or exception is never retried</code> | [L179](../../tests/ListingPrivateEditTests.cs#L179) |
| <code>wrong private flag, other conditions or slot changes cannot pass verification</code> | [L190](../../tests/ListingPrivateEditTests.cs#L190) |
| <code>private confirmation keeps timer evidence separate and refuses create mode</code> | [L214](../../tests/ListingPrivateEditTests.cs#L214) |

<a id="file-tests-listingrecoverytests-cs"></a>
## tests/ListingRecoveryTests.cs

[소스 파일](../../tests/ListingRecoveryTests.cs) · 명명된 항목/진입점 23개

수정창 닫기 우선 처리·ACK·세션·창 수명·중단 소유권를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingrecoverytests-cs-7-0bbe0cc547"></a><code>ListingRecoveryTests.Ready</code><br><code>(ListingWebBridge b, RecoveryFakeHost h, string id, string session) Ready()</code> | 메서드 | 공통 bridge의 정상 자기 모집 snapshot과 복구용 fake host·UUID·session을 만든다. | [L7](../../tests/ListingRecoveryTests.cs#L7) |
| <a id="symbol-tests-listingrecoverytests-cs-12-f6d24880e0"></a><code>ListingRecoveryTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 수정창 닫기 우선 처리·ACK·세션·창 수명·중단 소유권 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L12](../../tests/ListingRecoveryTests.cs#L12) |
| <a id="symbol-tests-listingrecoverytests-cs-112-4cd46be0b3"></a><code>ListingRecoveryTests.RecoveryResponse.Defer</code><br><code>Task Defer()</code> | 메서드 | 최초 Discord ACK의 횟수·순서와 테스트용 지연/실패를 기록한다. | [L112](../../tests/ListingRecoveryTests.cs#L112) |
| <a id="symbol-tests-listingrecoverytests-cs-113-4931b289e3"></a><code>ListingRecoveryTests.RecoveryResponse.Modal</code><br><code>Task Modal(Modal modal)</code> | 메서드 | modal interaction 응답을 읽는다. | [L113](../../tests/ListingRecoveryTests.cs#L113) |
| <a id="symbol-tests-listingrecoverytests-cs-114-5e6a8491e4"></a><code>ListingRecoveryTests.RecoveryResponse.Screen</code><br><code>Task Screen(DiscordScreen screen, bool replace, bool deferred)</code> | 메서드 | 보낸 카드와 update/응답 정보를 테스트 기록에 저장한다. | [L114](../../tests/ListingRecoveryTests.cs#L114) |
| <a id="symbol-tests-listingrecoverytests-cs-119-18b7c03e95"></a><code>RecoveryFakeHost</code><br><code>RecoveryFakeHost(WebEditFakeHost game)</code> | 주 생성자/모델 | 이 파일의 검증에 사용할 fake host/transport/interaction 데이터를 초기화한다. 실제 게임 메모리에는 접근하지 않는다. | [L119](../../tests/ListingRecoveryTests.cs#L119) |
| <a id="symbol-tests-listingrecoverytests-cs-121-ad82973b61"></a><code>RecoveryFakeHost.Game</code><br><code>WebEditFakeHost Game</code> | 계산 속성 | 복구 취소 전후 제출 횟수와 갱신 상태를 검사할 내부 fake game host를 반환한다. 계산식: <code>game</code>. | [L121](../../tests/ListingRecoveryTests.cs#L121) |
| <a id="symbol-tests-listingrecoverytests-cs-125-2b4303d352"></a><code>RecoveryFakeHost.Show</code><br><code>void Show(bool role = false)</code> | 메서드 | 복구 테스트 대상인 부모와 선택적 자식창 ticket을 표시 상태로 만든다. | [L125](../../tests/ListingRecoveryTests.cs#L125) |
| <a id="symbol-tests-listingrecoverytests-cs-126-db90ddddf1"></a><code>RecoveryFakeHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | 현재 로그인한 본인의 ContentId를 반환한다. 로그아웃/미준비는0이다. 계산식: <code>game.SelfId</code>. | [L126](../../tests/ListingRecoveryTests.cs#L126) |
| <a id="symbol-tests-listingrecoverytests-cs-127-69c70640b8"></a><code>RecoveryFakeHost.RecoveryBlockReason</code><br><code>string? RecoveryBlockReason</code> | 계산 속성 | 테스트에서 주입한 native 호환/이동 차단 사유를 반환한다. 계산식: <code>Block</code>. | [L127](../../tests/ListingRecoveryTests.cs#L127) |
| <a id="symbol-tests-listingrecoverytests-cs-128-f633070c07"></a><code>RecoveryFakeHost.StopForRecovery</code><br><code>void StopForRecovery()</code> | 메서드 | fake query/refresh를 취소하고 중단 횟수를 기록한다. | [L128](../../tests/ListingRecoveryTests.cs#L128) |
| <a id="symbol-tests-listingrecoverytests-cs-129-d71a1b4efe"></a><code>RecoveryFakeHost.ReadRecoveryWindows</code><br><code>bool ReadRecoveryWindows(out RecruitmentWindows windows)</code> | 메서드 | fake 창 상태를 반환해 순서·수명·타임아웃을 검증한다. | [L129](../../tests/ListingRecoveryTests.cs#L129) |
| <a id="symbol-tests-listingrecoverytests-cs-130-e216c3bc19"></a><code>RecoveryFakeHost.CloseRecoveryWindow</code><br><code>bool CloseRecoveryWindow(RecruitmentWindow kind, RecruitmentWindowTicket ticket, out string error)</code> | 메서드 | 닫기 종류를 기록하고 즉시 닫힘/변화 없음/접수 후 예외를 주입한다. | [L130](../../tests/ListingRecoveryTests.cs#L130) |
| <a id="symbol-tests-listingrecoverytests-cs-140-3fc1288f86"></a><code>RecoveryFakeHost.QueryBlockReason</code><br><code>string? QueryBlockReason</code> | 계산 속성 | fake 수정창 표시 및 기존 query busy를 일반 조회 차단으로 모사한다. 계산식: <code>Windows.Editor.Address != 0 ? "editor open" : game.QueryBlockReason</code>. | [L140](../../tests/ListingRecoveryTests.cs#L140) |
| <a id="symbol-tests-listingrecoverytests-cs-141-12914927ce"></a><code>RecoveryFakeHost.QueryBusy</code><br><code>bool QueryBusy</code> | 계산 속성 | ListingReader의 조회 진행 상태를 반환한다. 계산식: <code>game.QueryBusy</code>. | [L141](../../tests/ListingRecoveryTests.cs#L141) |
| <a id="symbol-tests-listingrecoverytests-cs-142-998f13285a"></a><code>RecoveryFakeHost.QuerySucceeded</code><br><code>bool QuerySucceeded</code> | 계산 속성 | 마지막 본인 조회가 검증까지 성공했는지 반환한다. 계산식: <code>game.QuerySucceeded</code>. | [L142](../../tests/ListingRecoveryTests.cs#L142) |
| <a id="symbol-tests-listingrecoverytests-cs-143-59a3bde2c6"></a><code>RecoveryFakeHost.DetailRevision</code><br><code>long DetailRevision</code> | 계산 속성 | 상세창 PostSetup/PostRefresh revision을 읽는다. 계산식: <code>game.DetailRevision</code>. | [L143](../../tests/ListingRecoveryTests.cs#L143) |
| <a id="symbol-tests-listingrecoverytests-cs-144-d8eedfaf76"></a><code>RecoveryFakeHost.Listing</code><br><code>ListingSnapshot Listing</code> | 계산 속성 | 현재 관리형 ListingSnapshot을 반환한다. 계산식: <code>game.Listing</code>. | [L144](../../tests/ListingRecoveryTests.cs#L144) |
| <a id="symbol-tests-listingrecoverytests-cs-145-5f69357cb8"></a><code>RecoveryFakeHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 메서드 | Bridge가 실행하는 조회를 fake host/guard에 기록한다. | [L145](../../tests/ListingRecoveryTests.cs#L145) |
| <a id="symbol-tests-listingrecoverytests-cs-146-86d87aba69"></a><code>RecoveryFakeHost.RefreshBlockReason</code><br><code>string? RefreshBlockReason</code> | 계산 속성 | 공통 조회 차단에 인게임 대기 변경/cooldown 등을 더해 외부 갱신 거부 이유를 반환한다. 계산식: <code>QueryBlockReason ?? game.RefreshBlockReason</code>. | [L146](../../tests/ListingRecoveryTests.cs#L146) |
| <a id="symbol-tests-listingrecoverytests-cs-147-7d5bf5049a"></a><code>RecoveryFakeHost.CaptureEditTarget</code><br><code>RefreshTarget? CaptureEditTarget()</code> | 메서드 | fake host가 보유한 source를 외부 편집 target으로 반환한다. | [L147](../../tests/ListingRecoveryTests.cs#L147) |
| <a id="symbol-tests-listingrecoverytests-cs-148-59f591a9ef"></a><code>RecoveryFakeHost.StartRefresh</code><br><code>bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error)</code> | 메서드 | Bridge가 전달한 source/변경을 fake 갱신 흐름에 예약하고 호출 횟수를 기록한다. | [L148](../../tests/ListingRecoveryTests.cs#L148) |
| <a id="symbol-tests-listingrecoverytests-cs-149-6040fcab17"></a><code>RecoveryFakeHost.ReadRefresh</code><br><code>WebRefreshProgress? ReadRefresh()</code> | 메서드 | fake 갱신 상태/결과를 외부 DTO로 반환한다. | [L149](../../tests/ListingRecoveryTests.cs#L149) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>recovery closes the role child before the editor and confirms disappearance</code> | [L14](../../tests/ListingRecoveryTests.cs#L14) |
| <code>recovery supersedes a queued refresh before it can dispatch</code> | [L22](../../tests/ListingRecoveryTests.cs#L22) |
| <code>recovery cancels an active refresh before the next submit step</code> | [L28](../../tests/ListingRecoveryTests.cs#L28) |
| <code>recovery does not need a readable listing or enabled refresh button</code> | [L36](../../tests/ListingRecoveryTests.cs#L36) |
| <code>recovery request IDs are idempotent and concurrent closes are rejected</code> | [L42](../../tests/ListingRecoveryTests.cs#L42) |
| <code>recovery rejects stale sessions publications and expired queued requests</code> | [L49](../../tests/ListingRecoveryTests.cs#L49) |
| <code>recovery character switches never close another character window</code> | [L55](../../tests/ListingRecoveryTests.cs#L55) |
| <code>recovery respects native guards while still stopping the refresh</code> | [L59](../../tests/ListingRecoveryTests.cs#L59) |
| <code>recovery refuses replacement editors and never retries uncertain close calls</code> | [L64](../../tests/ListingRecoveryTests.cs#L64) |
| <code>recovery reports timeout instead of success if the window stays visible</code> | [L73](../../tests/ListingRecoveryTests.cs#L73) |
| <code>recovery makes an old acknowledged Discord request unusable after completion</code> | [L79](../../tests/ListingRecoveryTests.cs#L79) |
| <code>discord recovery works during a blocked normal ACK and requires its own successful ACK</code> | [L85](../../tests/ListingRecoveryTests.cs#L85) |
| <code>discord recovery rejects another owner app and stale card</code> | [L100](../../tests/ListingRecoveryTests.cs#L100) |

<a id="file-tests-listingrelaytests-cs"></a>
## tests/ListingRelayTests.cs

[소스 파일](../../tests/ListingRelayTests.cs) · 명명된 항목/진입점 3개

중계 URL/key/frame allowlist·session/만료·JSON/크기·Bridge 응답를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingrelaytests-cs-7-90b5776ce3"></a><code>ListingRelayTests.Frame</code><br><code>byte[] Frame(string path, string method = "GET", string body = "", string requestId = "", string session = "", long expiresAt = 6000)</code> | 메서드 | 허용/거부 경계를 검사할 relay JSON frame을 만든다. | [L7](../../tests/ListingRelayTests.cs#L7) |
| <a id="symbol-tests-listingrelaytests-cs-9-377a683268"></a><code>ListingRelayTests.Reply</code><br><code>JsonElement Reply(ListingWebBridge bridge, byte[] frame, long tick = 1100, long utc = 1000)</code> | 메서드 | relay protocol의 응답 JSON을 검사하기 쉬운 구조로 읽는다. | [L9](../../tests/ListingRelayTests.cs#L9) |
| <a id="symbol-tests-listingrelaytests-cs-13-b68b0ff73b"></a><code>ListingRelayTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 중계 URL/key/frame allowlist·session/만료·JSON/크기·Bridge 응답 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L13](../../tests/ListingRelayTests.cs#L13) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>relay accepts only an HTTPS origin in production</code> | [L15](../../tests/ListingRelayTests.cs#L15) |
| <code>relay read protocol exposes only managed state and whitelisted paths</code> | [L21](../../tests/ListingRelayTests.cs#L21) |
| <code>relay expires delayed commands before queueing and rejects clock skew</code> | [L27](../../tests/ListingRelayTests.cs#L27) |
| <code>relay duplicate delivery reuses the same operation and never calls game off-thread</code> | [L33](../../tests/ListingRelayTests.cs#L33) |
| <code>relay rejects malformed envelopes, oversized bodies and unrecognized edits without echoing PIN</code> | [L43](../../tests/ListingRelayTests.cs#L43) |
| <code>relay old sessions cannot mutate another character or a stopped bridge</code> | [L53](../../tests/ListingRelayTests.cs#L53) |

<a id="file-tests-listingrolebuttonstests-cs"></a>
## tests/ListingRoleButtonsTests.cs

[소스 파일](../../tests/ListingRoleButtonsTests.cs) · 명명된 항목/진입점 1개

KR 직업 node mapping·Anyone 계획·친선 차이 토글·batch pacing를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingrolebuttonstests-cs-5-0082321848"></a><code>ListingRoleButtonsTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | KR 직업 node mapping·Anyone 계획·친선 차이 토글·batch pacing 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L5](../../tests/ListingRoleButtonsTests.cs#L5) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>node events produce every single-job choice, mixed classes and anyone</code> | [L17](../../tests/ListingRoleButtonsTests.cs#L17) |
| <code>anyone selection sends only reset and confirmation</code> | [L45](../../tests/ListingRoleButtonsTests.cs#L45) |
| <code>exclude jobs in one batch, keeping reset and confirm separated and unrepeated</code> | [L49](../../tests/ListingRoleButtonsTests.cs#L49) |
| <code>a 500 ms update cycle handles 31 exclusions in one cycle instead of 31</code> | [L59](../../tests/ListingRoleButtonsTests.cs#L59) |
| <code>empty, unknown or unreadable selections produce no button plan</code> | [L65](../../tests/ListingRoleButtonsTests.cs#L65) |

<a id="file-tests-listingrolecomparisontests-cs"></a>
## tests/ListingRoleComparisonTests.cs

[소스 파일](../../tests/ListingRoleComparisonTests.cs) · 명명된 항목/진입점 3개

raw flags 차이/자리 위치·추가/제거 직업 진단를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingrolecomparisontests-cs-6-ddcfbc880e"></a><code>ListingRoleComparisonTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | raw flags 차이/자리 위치·추가/제거 직업 진단 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingRoleComparisonTests.cs#L6) |
| <a id="symbol-tests-listingrolecomparisontests-cs-11-0392cb7d82"></a><code>ListingRoleComparisonTests.Run.Criteria</code><br><code>string Criteria(ulong[] flags)</code> | 로컬 함수 | 요구 슬롯 mask 배열을 비교용 Criteria 문자열로 만든다. | [L11](../../tests/ListingRoleComparisonTests.cs#L11) |
| <a id="symbol-tests-listingrolecomparisontests-cs-12-bdf9fe0530"></a><code>ListingRoleComparisonTests.Run.Friendly</code><br><code>(RefreshTarget target, ulong[] flags) Friendly()</code> | 로컬 함수 | 친선경기 그룹/자리 구조를 가진 비교 fixture를 만든다. | [L12](../../tests/ListingRoleComparisonTests.cs#L12) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>role mismatch diagnostics distinguish raw-only bits, changed jobs and unregistered tails</code> | [L23](../../tests/ListingRoleComparisonTests.cs#L23) |
| <code>role diagnostics preserve sources and retain their capture without identifiers or comment text</code> | [L38](../../tests/ListingRoleComparisonTests.cs#L38) |
| <code>role diagnostics reject truncated or malformed criteria and do not guess unknown jobs</code> | [L48](../../tests/ListingRoleComparisonTests.cs#L48) |

<a id="file-tests-listingrolenormalizationtests-cs"></a>
## tests/ListingRoleNormalizationTests.cs

[소스 파일](../../tests/ListingRoleNormalizationTests.cs) · 명명된 항목/진입점 4개

점유 자리의 입장 직업 자동 정규화와 일반·연합·친선의 원본/수정창/결과 검증를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingrolenormalizationtests-cs-6-1e30d3af49"></a><code>ListingRoleNormalizationTests.ReplaceFlags</code><br><code>RefreshTarget ReplaceFlags(RefreshTarget source, params (int Index, ulong Flags)[] changes)</code> | 메서드 | fixture의 지정 물리 슬롯 mask와 Criteria/Slots를 함께 바꾼다. 실제 게임 배열은 사용하지 않는다. | [L6](../../tests/ListingRoleNormalizationTests.cs#L6) |
| <a id="symbol-tests-listingrolenormalizationtests-cs-16-d37a08ecb6"></a><code>ListingRoleNormalizationTests.Target</code><br><code>RefreshTarget Target(int count, bool friendly = false)</code> | 메서드 | 두 참가자와 알려진 입장 직업을 갖는 일반·연합·친선 모집 fixture를 만든다. | [L16](../../tests/ListingRoleNormalizationTests.cs#L16) |
| <a id="symbol-tests-listingrolenormalizationtests-cs-29-0cc7b29747"></a><code>ListingRoleNormalizationTests.Narrow</code><br><code>RefreshTarget Narrow(RefreshTarget t)</code> | 메서드 | 두 점유 자리의 허용 mask를 각각 입장 직업 한 bit로 좁혀 게임 수정창의 자동 변경을 재현한다. | [L29](../../tests/ListingRoleNormalizationTests.cs#L29) |
| <a id="symbol-tests-listingrolenormalizationtests-cs-31-3c38033f7d"></a><code>ListingRoleNormalizationTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 점유 자리의 입장 직업 자동 정규화와 일반·연합·친선의 원본/수정창/결과 검증 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L31](../../tests/ListingRoleNormalizationTests.cs#L31) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>occupied entry job normalization works for normal alliance and friendly listings without changing displayed source</code> | [L33](../../tests/ListingRoleNormalizationTests.cs#L33) |
| <code>occupied normalization never ignores vacancy changes wrong entry jobs or unverified job masks</code> | [L43](../../tests/ListingRoleNormalizationTests.cs#L43) |
| <code>occupied normalization retains exact participant and registered job identity checks</code> | [L57](../../tests/ListingRoleNormalizationTests.cs#L57) |
| <code>normalization compares all other settings and rejects contradictory role snapshots</code> | [L71](../../tests/ListingRoleNormalizationTests.cs#L71) |
| <code>friendly hidden padding and occupied entry-job normalization compose while real unregistered slots stay exact</code> | [L83](../../tests/ListingRoleNormalizationTests.cs#L83) |
| <code>every individual edit keeps its preflight and result checks after occupied masks normalize</code> | [L91](../../tests/ListingRoleNormalizationTests.cs#L91) |
| <code>bound refresh and batch preflight accept normalized occupants and submit once</code> | [L110](../../tests/ListingRoleNormalizationTests.cs#L110) |

<a id="file-tests-listingruleedittests-cs"></a>
## tests/ListingRuleEditTests.cs

[소스 파일](../../tests/ListingRuleEditTests.cs) · 명명된 항목/진입점 4개

finder bit/loot mapping·컨트롤 상태 기계·다른 조건 보존를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingruleedittests-cs-6-b98938e0e5"></a><code>ListingRuleEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | finder bit/loot mapping·컨트롤 상태 기계·다른 조건 보존 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingRuleEditTests.cs#L6) |
| <a id="symbol-tests-listingruleedittests-cs-8-723696f2f2"></a><code>ListingRuleEditTests.Run.Target</code><br><code>RefreshTarget Target(int flags = 0, int loot = 0, int count = 8)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L8](../../tests/ListingRuleEditTests.cs#L8) |
| <a id="symbol-tests-listingruleedittests-cs-19-da37790363"></a><code>ListingRuleEditTests.Run.Start</code><br><code>(FakeHost h, ListingRefresh f) Start(ListingRuleKind kind, int from = 0, int to = 1, int count = 8, int seconds = 1800)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L19](../../tests/ListingRuleEditTests.cs#L19) |
| <a id="symbol-tests-listingruleedittests-cs-28-bf32d811d2"></a><code>ListingRuleEditTests.Run.Loot</code><br><code>LootUiState Loot(int value, bool open = false)</code> | 로컬 함수 | 전리품 dropdown state fixture를 만든다. | [L28](../../tests/ListingRuleEditTests.cs#L28) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>rule toggles change only their own bit across all flag combinations</code> | [L30](../../tests/ListingRuleEditTests.cs#L30) |
| <code>loot choices use exact manual indices and preserve every other condition</code> | [L46](../../tests/ListingRuleEditTests.cs#L46) |
| <code>invalid unknown unsupported and unchanged rules dispatch nothing</code> | [L60](../../tests/ListingRuleEditTests.cs#L60) |
| <code>rule edits cannot combine with any other pending edit</code> | [L75](../../tests/ListingRuleEditTests.cs#L75) |
| <code>all rule edits preserve 4 8 24 and 48 slot listings and verify the fresh result</code> | [L89](../../tests/ListingRuleEditTests.cs#L89) |
| <code>stale rule drafts reject changed listings members and unrelated rules before opening</code> | [L107](../../tests/ListingRuleEditTests.cs#L107) |
| <code>rule failures timeout cancellation and window changes never submit or retry</code> | [L118](../../tests/ListingRuleEditTests.cs#L118) |
| <code>wrong rule or side effects fail both draft and server readback verification</code> | [L142](../../tests/ListingRuleEditTests.cs#L142) |
| <code>rule confirmation is separate from the timer and create mode never edits</code> | [L171](../../tests/ListingRuleEditTests.cs#L171) |
| <code>loot native sequence opens clicks and selects every different pair exactly once</code> | [L182](../../tests/ListingRuleEditTests.cs#L182) |
| <code>loot unknown initial state and changed selection fail without guessing or retry</code> | [L197](../../tests/ListingRuleEditTests.cs#L197) |
| <code>loot timeouts and UI-only changes never confirm or repeat native actions</code> | [L209](../../tests/ListingRuleEditTests.cs#L209) |
| <code>duty checkbox edits wait for stored flags and do not resend a click</code> | [L217](../../tests/ListingRuleEditTests.cs#L217) |

<a id="file-tests-listingslotroutetests-cs"></a>
## tests/ListingSlotRouteTests.cs

[소스 파일](../../tests/ListingSlotRouteTests.cs) · 명명된 항목/진입점 2개

일반/24/48/친선 버튼 node/param과 수정창 형태를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingslotroutetests-cs-6-b84096ffd5"></a><code>ListingSlotRouteTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 일반/24/48/친선 버튼 node/param과 수정창 형태 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L6](../../tests/ListingSlotRouteTests.cs#L6) |
| <a id="symbol-tests-listingslotroutetests-cs-9-aa68aa1fac"></a><code>ListingSlotRouteTests.Run.Target</code><br><code>RefreshTarget Target(int count)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L9](../../tests/ListingSlotRouteTests.cs#L9) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>normal 4/8-person slot routes preserve original node IDs and param 8</code> | [L20](../../tests/ListingSlotRouteTests.cs#L20) |
| <code>24-person group boundaries use A/B/C slot buttons and registered parameters</code> | [L33](../../tests/ListingSlotRouteTests.cs#L33) |
| <code>48-person groups and members use independently observed node/param pairs</code> | [L43](../../tests/ListingSlotRouteTests.cs#L43) |
| <code>inconsistent shapes, party counts, bounds and PvP eligibility block edits</code> | [L57](../../tests/ListingSlotRouteTests.cs#L57) |
| <code>alliance edit retains global position and only submits its verified slot once</code> | [L69](../../tests/ListingSlotRouteTests.cs#L69) |
| <code>changing the same member number in the wrong alliance group cannot be submitted</code> | [L85](../../tests/ListingSlotRouteTests.cs#L85) |

<a id="file-tests-listingslotstests-cs"></a>
## tests/ListingSlotsTests.cs

[소스 파일](../../tests/ListingSlotsTests.cs) · 명명된 항목/진입점 2개

참가 직업/허용 직업·소유 물리 인덱스·인원수 일치를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingslotstests-cs-5-ace13b0518"></a><code>ListingSlotsTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 참가 직업/허용 직업·소유 물리 인덱스·인원수 일치 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L5](../../tests/ListingSlotsTests.cs#L5) |
| <a id="symbol-tests-listingslotstests-cs-7-2ecd5b5943"></a><code>ListingSlotsTests.Run.JobName</code><br><code>string JobName(uint id)</code> | 로컬 함수 | 직업 ID를 테스트용 이름으로 변환한다. | [L7](../../tests/ListingSlotsTests.cs#L7) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>recruitment jobs preserve positions and survive later source changes</code> | [L8](../../tests/ListingSlotsTests.cs#L8) |
| <code>recruitment capacity follows the listing through 48 slots</code> | [L20](../../tests/ListingSlotsTests.cs#L20) |
| <code>invalid or incomplete recruitment arrays are not displayed</code> | [L35](../../tests/ListingSlotsTests.cs#L35) |
| <code>unmatched detail never supplies jobs to the current listing</code> | [L43](../../tests/ListingSlotsTests.cs#L43) |
| <code>missing and unknown recruitment jobs retain their source values</code> | [L49](../../tests/ListingSlotsTests.cs#L49) |
| <code>inconsistent member count remains visible as incomplete data</code> | [L58](../../tests/ListingSlotsTests.cs#L58) |

<a id="file-tests-listingsparseslotstests-cs"></a>
## tests/ListingSparseSlotsTests.cs

[소스 파일](../../tests/ListingSparseSlotsTests.cs) · 명명된 항목/진입점 4개

24/48인 미등록 제외·display/native 위치 보존·편집 결과를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingsparseslotstests-cs-7-8ff0ae1b07"></a><code>ListingSparseSlotsTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 24/48인 미등록 제외·display/native 위치 보존·편집 결과 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L7](../../tests/ListingSparseSlotsTests.cs#L7) |
| <a id="symbol-tests-listingsparseslotstests-cs-15-d912150d9f"></a><code>ListingSparseSlotsTests.Run.Target</code><br><code>RefreshTarget Target(int[] registeredPerGroup, bool friendly = false)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L15](../../tests/ListingSparseSlotsTests.cs#L15) |
| <a id="symbol-tests-listingsparseslotstests-cs-32-7f3b540bd4"></a><code>ListingSparseSlotsTests.Run.RawFlags</code><br><code>ulong[] RawFlags(RefreshTarget target)</code> | 로컬 함수 | slot snapshot의 raw mask 배열을 복원한다. | [L32](../../tests/ListingSparseSlotsTests.cs#L32) |
| <a id="symbol-tests-listingsparseslotstests-cs-34-659144b2b0"></a><code>ListingSparseSlotsTests.Run.Complete</code><br><code>void Complete(FakeHost host, ListingRefresh flow)</code> | 로컬 함수 | 희소 슬롯 편집의 완료/검증 단계를 fake host로 진행한다. | [L34](../../tests/ListingSparseSlotsTests.cs#L34) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>24 and 48 player layouts with a group tail removed retain their final group</code> | [L45](../../tests/ListingSparseSlotsTests.cs#L45) |
| <code>B party removal uses the fetched B1 through B7 and keeps C1 at native index 16</code> | [L57](../../tests/ListingSparseSlotsTests.cs#L57) |
| <code>multiple group tails and an empty first group preserve native group numbers</code> | [L63](../../tests/ListingSparseSlotsTests.cs#L63) |
| <code>friendly 5 plus 5 plus 8 reads every spectator regardless of the alliance boolean</code> | [L74](../../tests/ListingSparseSlotsTests.cs#L74) |
| <code>an ordinary alliance with 18 registered positions does not become a friendly match</code> | [L83](../../tests/ListingSparseSlotsTests.cs#L83) |
| <code>friendly additional unregistered team and spectator tails preserve their original indices</code> | [L89](../../tests/ListingSparseSlotsTests.cs#L89) |
| <code>normal party tails still support edits without showing unused native positions</code> | [L97](../../tests/ListingSparseSlotsTests.cs#L97) |
| <code>member verification includes unregistered tails and the final registered member</code> | [L107](../../tests/ListingSparseSlotsTests.cs#L107) |
| <code>mismatched slot counts and overlapping or packed mappings remain uneditable</code> | [L118](../../tests/ListingSparseSlotsTests.cs#L118) |
| <code>job edits after group tails change only the intended native position and submit once</code> | [L126](../../tests/ListingSparseSlotsTests.cs#L126) |
| <code>shared setting edits work with group tails while preserving all native slot flags</code> | [L140](../../tests/ListingSparseSlotsTests.cs#L140) |
| <code>a newly registered tail invalidates an older edit snapshot</code> | [L167](../../tests/ListingSparseSlotsTests.cs#L167) |

<a id="file-tests-listinguniquejobsedittests-cs"></a>
## tests/ListingUniqueJobsEditTests.cs

[소스 파일](../../tests/ListingUniqueJobsEditTests.cs) · 명명된 항목/진입점 3개

잡 중복 없음 flag·원본/결과·checkbox 지연를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listinguniquejobsedittests-cs-7-2b512e930b"></a><code>ListingUniqueJobsEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | 잡 중복 없음 flag·원본/결과·checkbox 지연 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L7](../../tests/ListingUniqueJobsEditTests.cs#L7) |
| <a id="symbol-tests-listinguniquejobsedittests-cs-9-5ecc02994b"></a><code>ListingUniqueJobsEditTests.Run.Target</code><br><code>RefreshTarget Target(int count = 8, byte joinFlags = 1, byte beginner = 1, byte objective = 4)</code> | 로컬 함수 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L9](../../tests/ListingUniqueJobsEditTests.cs#L9) |
| <a id="symbol-tests-listinguniquejobsedittests-cs-21-0329526f40"></a><code>ListingUniqueJobsEditTests.Run.Start</code><br><code>(FakeHost host, ListingRefresh flow) Start(bool enabled = true, int count = 8, byte beginner = 1, byte objective = 4, int seconds = 1800)</code> | 로컬 함수 | fake host와 갱신 흐름을 만들고 테스트 대상 편집 단계까지 진행한다. | [L21](../../tests/ListingUniqueJobsEditTests.cs#L21) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>unique jobs uses the captured native flag and preserves every other join bit</code> | [L30](../../tests/ListingUniqueJobsEditTests.cs#L30) |
| <code>unique jobs draft prefills the detail bit independently of beginner and purpose</code> | [L46](../../tests/ListingUniqueJobsEditTests.cs#L46) |
| <code>unique jobs on/off submits once for every layout, purpose and beginner state</code> | [L56](../../tests/ListingUniqueJobsEditTests.cs#L56) |
| <code>unknown join data, unchanged state or unsupported unique jobs requests never query</code> | [L74](../../tests/ListingUniqueJobsEditTests.cs#L74) |
| <code>unique jobs cannot be combined with any other edit</code> | [L84](../../tests/ListingUniqueJobsEditTests.cs#L84) |
| <code>stale unique job draft detects flags, text, purpose, identity and participants</code> | [L96](../../tests/ListingUniqueJobsEditTests.cs#L96) |
| <code>unique jobs failure, timeout, cancel, character change and editor closure never submit</code> | [L109](../../tests/ListingUniqueJobsEditTests.cs#L109) |
| <code>failed or thrown unique job clicks are not retried</code> | [L124](../../tests/ListingUniqueJobsEditTests.cs#L124) |
| <code>old unique job bit or changed conditions and alliance slots fail pre/post-submit verification</code> | [L135](../../tests/ListingUniqueJobsEditTests.cs#L135) |
| <code>unique jobs confirmation does not require the displayed sixty-minute timer to increase</code> | [L163](../../tests/ListingUniqueJobsEditTests.cs#L163) |
| <code>unique jobs does not act on a new-party editor</code> | [L169](../../tests/ListingUniqueJobsEditTests.cs#L169) |

<a id="file-tests-listingwebedittests-cs"></a>
## tests/ListingWebEditTests.cs

[소스 파일](../../tests/ListingWebEditTests.cs) · 명명된 항목/진입점 21개

apply decoder·revision/ID ledger·타임아웃·실제 HTTP의 엄격한 body/auth 경계를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingwebedittests-cs-10-b7998d3bf0"></a><code>ListingWebEditTests.Body</code><br><code>byte[] Body(string revision, params object[] changes)</code> | 메서드 | 외부 apply API의 JSON body fixture를 만든다. | [L10](../../tests/ListingWebEditTests.cs#L10) |
| <a id="symbol-tests-listingwebedittests-cs-11-7672aeed63"></a><code>ListingWebEditTests.Ready</code><br><code>(ListingWebBridge Bridge, WebEditFakeHost Host, WebView View) Ready(long now = 1000)</code> | 메서드 | fake source를 Bridge에 발행해 query/edit가 가능한 초기 상태를 만든다. | [L11](../../tests/ListingWebEditTests.cs#L11) |
| <a id="symbol-tests-listingwebedittests-cs-16-95d136fe0e"></a><code>ListingWebEditTests.Step</code><br><code>void Step(ListingWebBridge b, WebEditFakeHost h, long tick)</code> | 메서드 | fake 시간 및 host/Bridge update를 한 단계 진행한다. | [L16](../../tests/ListingWebEditTests.cs#L16) |
| <a id="symbol-tests-listingwebedittests-cs-17-f86f9f7f6f"></a><code>ListingWebEditTests.Finish</code><br><code>WebOperation Finish(ListingWebBridge b, WebEditFakeHost h, long tick = 1500)</code> | 메서드 | fake 조회 또는 편집 작업을 끝내고 다음 snapshot을 발행한다. | [L17](../../tests/ListingWebEditTests.cs#L17) |
| <a id="symbol-tests-listingwebedittests-cs-25-ff49361744"></a><code>ListingWebEditTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | apply decoder·revision/ID ledger·타임아웃·실제 HTTP의 엄격한 body/auth 경계 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L25](../../tests/ListingWebEditTests.cs#L25) |
| <a id="symbol-tests-listingwebedittests-cs-185-edae60dc80"></a><code>ListingWebEditTests.Http</code><br><code>Task Http(Action&lt;bool, string&gt; check)</code> | 메서드 | loopback 서버에 raw HTTP fixture를 보내 응답을 읽는다. | [L185](../../tests/ListingWebEditTests.cs#L185) |
| <a id="symbol-tests-listingwebedittests-cs-229-3821a910ce"></a><code>WebEditFakeHost.WebEditFakeHost</code><br><code>WebEditFakeHost()</code> | 생성자 | 이 파일의 검증에 사용할 fake host/transport/interaction 데이터를 초기화한다. 실제 게임 메모리에는 접근하지 않는다. | [L229](../../tests/ListingWebEditTests.cs#L229) |
| <a id="symbol-tests-listingwebedittests-cs-230-0ee7f705a1"></a><code>WebEditFakeHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | 현재 로그인한 본인의 ContentId를 반환한다. 로그아웃/미준비는0이다. | [L230](../../tests/ListingWebEditTests.cs#L230) |
| <a id="symbol-tests-listingwebedittests-cs-231-a2912b449f"></a><code>WebEditFakeHost.QueryBlockReason</code><br><code>string? QueryBlockReason</code> | 계산 속성 | 현재 플러그인의 공통 조회 차단 사유를 반환한다. | [L231](../../tests/ListingWebEditTests.cs#L231) |
| <a id="symbol-tests-listingwebedittests-cs-232-8ad3a8acc0"></a><code>WebEditFakeHost.RefreshBlockReason</code><br><code>string? RefreshBlockReason</code> | 계산 속성 | 공통 조회 차단에 인게임 대기 변경/cooldown 등을 더해 외부 갱신 거부 이유를 반환한다. 계산식: <code>QueryBlockReason ?? (Flow.CoolingDown(Tick) ? "잠시 후 갱신" : null)</code>. | [L232](../../tests/ListingWebEditTests.cs#L232) |
| <a id="symbol-tests-listingwebedittests-cs-233-adcfbcd193"></a><code>WebEditFakeHost.QueryBusy</code><br><code>bool QueryBusy</code> | 계산 속성 | ListingReader의 조회 진행 상태를 반환한다. 계산식: <code>Native.QueryBusy</code>. | [L233](../../tests/ListingWebEditTests.cs#L233) |
| <a id="symbol-tests-listingwebedittests-cs-234-e7a0020825"></a><code>WebEditFakeHost.QuerySucceeded</code><br><code>bool QuerySucceeded</code> | 계산 속성 | 마지막 본인 조회가 검증까지 성공했는지 반환한다. 계산식: <code>Native.QuerySucceeded</code>. | [L234](../../tests/ListingWebEditTests.cs#L234) |
| <a id="symbol-tests-listingwebedittests-cs-235-ba26324274"></a><code>WebEditFakeHost.DetailRevision</code><br><code>long DetailRevision</code> | 계산 속성 | 상세창 PostSetup/PostRefresh revision을 읽는다. 계산식: <code>Native.DetailRevision</code>. | [L235](../../tests/ListingWebEditTests.cs#L235) |
| <a id="symbol-tests-listingwebedittests-cs-236-ec8ca7880e"></a><code>WebEditFakeHost.Listing</code><br><code>ListingSnapshot Listing</code> | 계산 속성 | 현재 관리형 ListingSnapshot을 반환한다. | [L236](../../tests/ListingWebEditTests.cs#L236) |
| <a id="symbol-tests-listingwebedittests-cs-244-fb0fcdf8c9"></a><code>WebEditFakeHost.CaptureEditTarget</code><br><code>RefreshTarget? CaptureEditTarget()</code> | 메서드 | fake host가 보유한 source를 외부 편집 target으로 반환한다. | [L244](../../tests/ListingWebEditTests.cs#L244) |
| <a id="symbol-tests-listingwebedittests-cs-245-5741c9ef72"></a><code>WebEditFakeHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 메서드 | Bridge가 실행하는 조회를 fake host/guard에 기록한다. | [L245](../../tests/ListingWebEditTests.cs#L245) |
| <a id="symbol-tests-listingwebedittests-cs-246-95cfd6732a"></a><code>WebEditFakeHost.StartRefresh</code><br><code>bool StartRefresh(RefreshTarget source, ListingChange[] changes, out string error)</code> | 메서드 | Bridge가 전달한 source/변경을 fake 갱신 흐름에 예약하고 호출 횟수를 기록한다. | [L246](../../tests/ListingWebEditTests.cs#L246) |
| <a id="symbol-tests-listingwebedittests-cs-252-9dbccb9bbf"></a><code>WebEditFakeHost.ReadRefresh</code><br><code>WebRefreshProgress? ReadRefresh()</code> | 메서드 | fake 갱신 상태/결과를 외부 DTO로 반환한다. | [L252](../../tests/ListingWebEditTests.cs#L252) |
| <a id="symbol-tests-listingwebedittests-cs-253-fd205d47f4"></a><code>WebEditFakeHost.Advance</code><br><code>void Advance(long now)</code> | 메서드 | fake 갱신 상태 기계를 지정 tick만큼 진행한다. | [L253](../../tests/ListingWebEditTests.cs#L253) |
| <a id="symbol-tests-listingwebedittests-cs-262-7d45286618"></a><code>WebEditFakeHost.Guard</code><br><code>void Guard()</code> | 메서드 | 외부 요청 ownership/시간 경계를 fake host에서 검사한다. | [L262](../../tests/ListingWebEditTests.cs#L262) |
| <a id="symbol-tests-listingwebedittests-cs-263-a4059b6829"></a><code>WebEditFakeHost.Target</code><br><code>RefreshTarget Target(int count = 8, bool friendly = false)</code> | 메서드 | 이 테스트가 필요한 조건/슬롯/멤버를 갖춘 RefreshTarget fixture를 만든다. | [L263](../../tests/ListingWebEditTests.cs#L263) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>web edit source revision ignores time but changes with members or conditions</code> | [L27](../../tests/ListingWebEditTests.cs#L27) |
| <code>web edit models expose ordered job IDs only for empty registered slots including friendly spectators</code> | [L35](../../tests/ListingWebEditTests.cs#L35) |
| <code>web decoder accepts all existing settings, leading-zero PIN and native slot IDs</code> | [L43](../../tests/ListingWebEditTests.cs#L43) |
| <code>web decoder rejects unknown, duplicate, malformed or oversized changes atomically</code> | [L55](../../tests/ListingWebEditTests.cs#L55) |
| <code>web friendly unregistered slots cannot be requested, unchanged fields become plain refresh</code> | [L76](../../tests/ListingWebEditTests.cs#L76) |
| <code>web writes stay on framework thread, execute once and never consume local pending changes</code> | [L82](../../tests/ListingWebEditTests.cs#L82) |
| <code>web batch skips an unsupported rule, applies other edits and reports partial result</code> | [L94](../../tests/ListingWebEditTests.cs#L94) |
| <code>web plain refresh has one submit and is bound to the displayed source</code> | [L103](../../tests/ListingWebEditTests.cs#L103) |
| <code>web batch rechecks source after its fresh query before editing</code> | [L112](../../tests/ListingWebEditTests.cs#L112) |
| <code>web 48-person job batch keeps every native slot and still submits just once</code> | [L117](../../tests/ListingWebEditTests.cs#L117) |
| <code>web simple refresh remains available when only job editing is unsupported</code> | [L125](../../tests/ListingWebEditTests.cs#L125) |
| <code>web unavailable preflight, stale publication and expired request never start</code> | [L129](../../tests/ListingWebEditTests.cs#L129) |
| <code>web character switch and shutdown discard queued bodies and credentials</code> | [L140](../../tests/ListingWebEditTests.cs#L140) |
| <code>web PIN never appears in snapshots, progress, HTTP replies or completed results</code> | [L149](../../tests/ListingWebEditTests.cs#L149) |
| <code>web terminal unconfirmed status remains honest even when fields match</code> | [L159](../../tests/ListingWebEditTests.cs#L159) |
| <code>web history eviction does not make an old write executable again</code> | [L164](../../tests/ListingWebEditTests.cs#L164) |
| <code>web refresh guard permits long batches, stops stalls, and never cancels newer local work</code> | [L174](../../tests/ListingWebEditTests.cs#L174) |
| <code>web apply authenticates and handles a fragmented bounded JSON body with one execution</code> | [L182](../../tests/ListingWebEditTests.cs#L182) |

<a id="file-tests-listingwebtests-cs"></a>
## tests/ListingWebTests.cs

[소스 파일](../../tests/ListingWebTests.cs) · 명명된 항목/진입점 11개

snapshot 최소 노출·query queue·Host/Origin/Bearer·HTTP 종료와 소유권를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-listingwebtests-cs-11-5cae6cb33e"></a><code>ListingWebTests.View</code><br><code>WebView View(ListingWebBridge bridge)</code> | 메서드 | Bridge의 JSON 응답을 WebView로 읽어 테스트 assertion에 제공한다. | [L11](../../tests/ListingWebTests.cs#L11) |
| <a id="symbol-tests-listingwebtests-cs-12-41971193a0"></a><code>ListingWebTests.Sample</code><br><code>ListingSnapshot Sample(int count = 8, bool friendly = false)</code> | 메서드 | WebView 변환용 모집글 snapshot fixture를 만든다. | [L12](../../tests/ListingWebTests.cs#L12) |
| <a id="symbol-tests-listingwebtests-cs-31-a4b51e1941"></a><code>ListingWebTests.Ready</code><br><code>(ListingWebBridge Bridge, WebFakeHost Host, string Session) Ready(long now = 1000)</code> | 메서드 | fake source를 Bridge에 발행해 query/edit가 가능한 초기 상태를 만든다. | [L31](../../tests/ListingWebTests.cs#L31) |
| <a id="symbol-tests-listingwebtests-cs-36-0af8c63888"></a><code>ListingWebTests.Run</code><br><code>void Run(Action&lt;string, Action&gt; test, Action&lt;bool, string&gt; check)</code> | 메서드 | snapshot 최소 노출·query queue·Host/Origin/Bearer·HTTP 종료와 소유권 관련 시나리오를 실행한다. 개별 등록 조건은 아래 test 목록을 참조한다. | [L36](../../tests/ListingWebTests.cs#L36) |
| <a id="symbol-tests-listingwebtests-cs-186-87294e036e"></a><code>ListingWebTests.HttpChecks</code><br><code>Task HttpChecks(Action&lt;bool, string&gt; check)</code> | 메서드 | 실제 loopback TCP 서버에 인증/Origin/HTTP 형식 경계 요청을 보내 검사한다. | [L186](../../tests/ListingWebTests.cs#L186) |
| <a id="symbol-tests-listingwebtests-cs-216-32de8a353f"></a><code>ListingWebTests.StopChecks</code><br><code>Task StopChecks(Action&lt;bool, string&gt; check)</code> | 메서드 | listener/연결/진행 요청 종료가 기다림 없이 정리되는지 검사한다. | [L216](../../tests/ListingWebTests.cs#L216) |
| <a id="symbol-tests-listingwebtests-cs-234-e7076a37c6"></a><code>WebFakeHost.SelfId</code><br><code>ulong SelfId</code> | 계산 속성 | 현재 로그인한 본인의 ContentId를 반환한다. 로그아웃/미준비는0이다. | [L234](../../tests/ListingWebTests.cs#L234) |
| <a id="symbol-tests-listingwebtests-cs-236-5f06f4758c"></a><code>WebFakeHost.QueryBlockReason</code><br><code>string? QueryBlockReason</code> | 계산 속성 | 현재 플러그인의 공통 조회 차단 사유를 반환한다. | [L236](../../tests/ListingWebTests.cs#L236) |
| <a id="symbol-tests-listingwebtests-cs-243-9ee1b9252e"></a><code>WebFakeHost.Guard</code><br><code>void Guard()</code> | 메서드 | 외부 요청 ownership/시간 경계를 fake host에서 검사한다. | [L243](../../tests/ListingWebTests.cs#L243) |
| <a id="symbol-tests-listingwebtests-cs-244-82300f6649"></a><code>WebFakeHost.RequestQuery</code><br><code>bool RequestQuery(out string error)</code> | 메서드 | Bridge가 실행하는 조회를 fake host/guard에 기록한다. | [L244](../../tests/ListingWebTests.cs#L244) |
| <a id="symbol-tests-listingwebtests-cs-250-2c2e5714c5"></a><code>WebFakeHost.Finish</code><br><code>void Finish()</code> | 메서드 | fake 조회 또는 편집 작업을 끝내고 다음 snapshot을 발행한다. | [L250](../../tests/ListingWebTests.cs#L250) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>web DTO shows registered jobs and preserves native slots for 4, 8, 24 and 48</code> | [L38](../../tests/ListingWebTests.cs#L38) |
| <code>web DTO friendly match has 5, 5, 8 rows and all spectators</code> | [L46](../../tests/ListingWebTests.cs#L46) |
| <code>web DTO excludes sparse unregistered tails while keeping group numbers</code> | [L53](../../tests/ListingWebTests.cs#L53) |
| <code>web DTO lists allowed jobs in existing order and requires complete anyone rules</code> | [L60](../../tests/ListingWebTests.cs#L60) |
| <code>web DTO never serializes content IDs, diagnostic notes or draft credentials</code> | [L68](../../tests/ListingWebTests.cs#L68) |
| <code>web hides foreign and unverified listings and labels cached results</code> | [L75](../../tests/ListingWebTests.cs#L75) |
| <code>web marks missing and inconsistent slot data without guessing positions</code> | [L81](../../tests/ListingWebTests.cs#L81) |
| <code>web HTTP thread can only queue/read managed data and never call game host</code> | [L88](../../tests/ListingWebTests.cs#L88) |
| <code>web completion requires a fresh own live detail response</code> | [L94](../../tests/ListingWebTests.cs#L94) |
| <code>web duplicate ID is idempotent and second concurrent request is rejected</code> | [L103](../../tests/ListingWebTests.cs#L103) |
| <code>web request shape, old session, stale publication and rapid repeat are rejected</code> | [L110](../../tests/ListingWebTests.cs#L110) |
| <code>web rechecks game editing and other work immediately before executing</code> | [L117](../../tests/ListingWebTests.cs#L117) |
| <code>web queued requests expire and never replay after a game stall</code> | [L121](../../tests/ListingWebTests.cs#L121) |
| <code>web logout or character change clears requests and cannot expose old cached data</code> | [L125](../../tests/ListingWebTests.cs#L125) |
| <code>web stop discards pending work and publication failures remove data</code> | [L133](../../tests/ListingWebTests.cs#L133) |
| <code>web query errors and timeout are reported without retry</code> | [L138](../../tests/ListingWebTests.cs#L138) |
| <code>web request history is bounded</code> | [L146](../../tests/ListingWebTests.cs#L146) |
| <code>web native dispatch is cancelled when character, editor or enabled state changes in the next frame</code> | [L154](../../tests/ListingWebTests.cs#L154) |
| <code>web cancellation ownership never cancels a later local query or refresh</code> | [L160](../../tests/ListingWebTests.cs#L160) |
| <code>web request flag expires before first native dispatch and bounds already started queries</code> | [L166](../../tests/ListingWebTests.cs#L166) |
| <code>web HTTP parser rejects ambiguous framing, duplicate headers and control bytes</code> | [L173](../../tests/ListingWebTests.cs#L173) |
| <code>web real listener authenticates queries, rejects other origins and serves embedded assets</code> | [L182](../../tests/ListingWebTests.cs#L182) |
| <code>web listener stop interrupts unfinished connections and restart rotates its key</code> | [L183](../../tests/ListingWebTests.cs#L183) |

<a id="file-tests-program-cs"></a>
## tests/Program.cs

[소스 파일](../../tests/Program.cs) · 명명된 항목/진입점 29개

전체 관리형 test runner 및 기본 갱신/소유자/빈 소개말/trace buffer 회귀를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-program-cs-1-072369dd2c"></a><code>&lt;top-level&gt;</code><br><code>&lt;top-level&gt;</code> | 진입점 | 전체 회귀 test를 등록·실행하고 --web-fixture/--relay-fixture이면 해당 통합 fixture 모드로 분기한다. | [L1](../../tests/Program.cs#L1) |
| <a id="symbol-tests-program-cs-7-255f99a9bb"></a><code>Check</code><br><code>void Check(bool value, string label)</code> | 로컬 함수 | 조건이 false이면 label을 가진 예외를 발생시키는 assertion helper다. | [L7](../../tests/Program.cs#L7) |
| <a id="symbol-tests-program-cs-8-423ce5a35d"></a><code>Test</code><br><code>void Test(string name, Action body)</code> | 로컬 함수 | 등록된 test body를 실행하고 성공 횟수와 PASS 이름을 출력한다. | [L8](../../tests/Program.cs#L8) |
| <a id="symbol-tests-program-cs-9-38aec8dfb2"></a><code>StartToEditor</code><br><code>(FakeHost h, ListingRefresh flow) StartToEditor()</code> | 로컬 함수 | 기본 갱신 요청과 조회 완료를 시뮬레이션해 OpeningEditor 상태를 만든다. | [L9](../../tests/Program.cs#L9) |
| <a id="symbol-tests-program-cs-16-24c4d6a476"></a><code>StartToVerify</code><br><code>(FakeHost h, ListingRefresh flow) StartToVerify()</code> | 로컬 함수 | 기본 편집/제출을 진행해 제출 후 Verifying 상태를 만든다. | [L16](../../tests/Program.cs#L16) |
| <a id="symbol-tests-program-cs-320-01b5a7e78e"></a><code>FakeHost.QueryOwn</code><br><code>void QueryOwn()</code> | 메서드 | 실제 네이티브 조회 대신 요청 횟수와 query busy 상태를 기록한다. | [L320](../../tests/Program.cs#L320) |
| <a id="symbol-tests-program-cs-321-c3ed2d654e"></a><code>FakeHost.FinishQuery</code><br><code>void FinishQuery()</code> | 메서드 | 새 상세 revision/성공 여부를 지정하여 fake 조회 응답을 완료한다. | [L321](../../tests/Program.cs#L321) |
| <a id="symbol-tests-program-cs-322-44aab4db98"></a><code>FakeHost.CancelQuery</code><br><code>void CancelQuery()</code> | 메서드 | fake 조회의 취소 횟수와 busy 상태를 갱신한다. | [L322](../../tests/Program.cs#L322) |
| <a id="symbol-tests-program-cs-323-d72e28d458"></a><code>FakeHost.TryGetOwn</code><br><code>bool TryGetOwn(out RefreshTarget target, out string error)</code> | 메서드 | 설정된 source와 OwnVerified/오류 상태로 본인 상세 읽기를 모사한다. | [L323](../../tests/Program.cs#L323) |
| <a id="symbol-tests-program-cs-324-43dbec5006"></a><code>FakeHost.OpenEditor</code><br><code>bool OpenEditor(RefreshTarget target, out string error)</code> | 메서드 | fake 수정창 표시/수명 revision을 갱신하고 열기 횟수·실패/예외를 모사한다. | [L324](../../tests/Program.cs#L324) |
| <a id="symbol-tests-program-cs-330-303a4a9502"></a><code>FakeHost.Submit</code><br><code>bool Submit(RefreshTarget target, string? description, out string error)</code> | 메서드 | 테스트 fixture에서는 modal 제출 또는 fake 네이티브 갱신 제출을 모사하며 호출 횟수/허용 여부/예외를 기록한다. | [L330](../../tests/Program.cs#L330) |
| <a id="symbol-tests-program-cs-338-372f37be84"></a><code>FakeHost.BeginSlotEdit</code><br><code>bool BeginSlotEdit(RefreshTarget target, ListingJobEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L338](../../tests/Program.cs#L338) |
| <a id="symbol-tests-program-cs-345-d6325ca732"></a><code>FakeHost.UpdateSlotEdit</code><br><code>SlotEditProgress UpdateSlotEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L345](../../tests/Program.cs#L345) |
| <a id="symbol-tests-program-cs-350-e10260552b"></a><code>FakeHost.BeginDescriptionEdit</code><br><code>bool BeginDescriptionEdit(RefreshTarget target, ListingDescriptionEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L350](../../tests/Program.cs#L350) |
| <a id="symbol-tests-program-cs-357-48c2eee796"></a><code>FakeHost.UpdateDescriptionEdit</code><br><code>SlotEditProgress UpdateDescriptionEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L357](../../tests/Program.cs#L357) |
| <a id="symbol-tests-program-cs-362-b323597c87"></a><code>FakeHost.BeginObjectiveEdit</code><br><code>bool BeginObjectiveEdit(RefreshTarget target, ListingObjectiveEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L362](../../tests/Program.cs#L362) |
| <a id="symbol-tests-program-cs-371-b0cb0f1784"></a><code>FakeHost.UpdateObjectiveEdit</code><br><code>SlotEditProgress UpdateObjectiveEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L371](../../tests/Program.cs#L371) |
| <a id="symbol-tests-program-cs-376-b6b114ea41"></a><code>FakeHost.BeginBeginnerEdit</code><br><code>bool BeginBeginnerEdit(RefreshTarget target, ListingBeginnerEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L376](../../tests/Program.cs#L376) |
| <a id="symbol-tests-program-cs-384-e711defd0d"></a><code>FakeHost.UpdateBeginnerEdit</code><br><code>SlotEditProgress UpdateBeginnerEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L384](../../tests/Program.cs#L384) |
| <a id="symbol-tests-program-cs-389-ea41827c7c"></a><code>FakeHost.BeginUniqueJobsEdit</code><br><code>bool BeginUniqueJobsEdit(RefreshTarget target, ListingUniqueJobsEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L389](../../tests/Program.cs#L389) |
| <a id="symbol-tests-program-cs-397-eea1ae4345"></a><code>FakeHost.UpdateUniqueJobsEdit</code><br><code>SlotEditProgress UpdateUniqueJobsEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L397](../../tests/Program.cs#L397) |
| <a id="symbol-tests-program-cs-402-83d773c1dc"></a><code>FakeHost.BeginPrivateEdit</code><br><code>bool BeginPrivateEdit(RefreshTarget target, ListingPrivateEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L402](../../tests/Program.cs#L402) |
| <a id="symbol-tests-program-cs-410-d254b2d3db"></a><code>FakeHost.UpdatePrivateEdit</code><br><code>SlotEditProgress UpdatePrivateEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L410](../../tests/Program.cs#L410) |
| <a id="symbol-tests-program-cs-415-ea4c2b437f"></a><code>FakeHost.BeginCompletionEdit</code><br><code>bool BeginCompletionEdit(RefreshTarget target, ListingCompletionEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L415](../../tests/Program.cs#L415) |
| <a id="symbol-tests-program-cs-423-330c9ae01c"></a><code>FakeHost.UpdateCompletionEdit</code><br><code>SlotEditProgress UpdateCompletionEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L423](../../tests/Program.cs#L423) |
| <a id="symbol-tests-program-cs-428-fa64075fe1"></a><code>FakeHost.BeginItemLevelEdit</code><br><code>bool BeginItemLevelEdit(RefreshTarget target, ListingItemLevelEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L428](../../tests/Program.cs#L428) |
| <a id="symbol-tests-program-cs-436-11e4e341d5"></a><code>FakeHost.UpdateItemLevelEdit</code><br><code>SlotEditProgress UpdateItemLevelEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L436](../../tests/Program.cs#L436) |
| <a id="symbol-tests-program-cs-441-e2eb5ebe53"></a><code>FakeHost.BeginRuleEdit</code><br><code>bool BeginRuleEdit(RefreshTarget target, ListingRuleEdit edit, out string error)</code> | 메서드 | 실제 UI 대신 지정 edit를 fake host에 기록하고 시작 허용/예외 및 예상 draft를 모사한다. | [L441](../../tests/Program.cs#L441) |
| <a id="symbol-tests-program-cs-448-d0f5374228"></a><code>FakeHost.UpdateRuleEdit</code><br><code>SlotEditProgress UpdateRuleEdit(out RefreshTarget expected, out string error)</code> | 메서드 | fake edit의 Waiting/Complete/Failed와 expected target을 반환해 재시도·timeout·결과 검증을 검사한다. | [L448](../../tests/Program.cs#L448) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>detail event before visible own data waits instead of stopping</code> | [L23](../../tests/Program.cs#L23) |
| <code>idle never acts</code> | [L31](../../tests/Program.cs#L31) |
| <code>cancel while waiting for own data never continues later</code> | [L35](../../tests/Program.cs#L35) |
| <code>new-party editor never submits</code> | [L41](../../tests/Program.cs#L41) |
| <code>editor label can finish loading before one refresh</code> | [L47](../../tests/Program.cs#L47) |
| <code>editor pause does not require an active own listing</code> | [L53](../../tests/Program.cs#L53) |
| <code>stopping during edit keeps the window and explains how to resume</code> | [L58](../../tests/Program.cs#L58) |
| <code>post-refresh owner data may become ready after its event</code> | [L63](../../tests/Program.cs#L63) |
| <code>refresh action label is distinct from create and unknown actions</code> | [L69](../../tests/Program.cs#L69) |
| <code>one click, one submit, fresh timer confirms</code> | [L76](../../tests/Program.cs#L76) |
| <code>old response without a new revision cannot open editor</code> | [L85](../../tests/Program.cs#L85) |
| <code>other or unverified owner cannot open editor</code> | [L90](../../tests/Program.cs#L90) |
| <code>preexisting editor is never submitted</code> | [L97](../../tests/Program.cs#L97) |
| <code>character switch aborts</code> | [L101](../../tests/Program.cs#L101) |
| <code>cancel prevents subsequent registration</code> | [L105](../../tests/Program.cs#L105) |
| <code>closing and reopening editor aborts</code> | [L109](../../tests/Program.cs#L109) |
| <code>editor must have a fresh lifecycle event</code> | [L113](../../tests/Program.cs#L113) |
| <code>zone transition blocks remaining actions</code> | [L117](../../tests/Program.cs#L117) |
| <code>validation failure never retries</code> | [L121](../../tests/Program.cs#L121) |
| <code>throwing native call never retries</code> | [L125](../../tests/Program.cs#L125) |
| <code>server timeout is unconfirmed without retry</code> | [L131](../../tests/Program.cs#L131) |
| <code>unchanged timer is not success</code> | [L135](../../tests/Program.cs#L135) |
| <code>new listing identity confirms after fresh response</code> | [L139](../../tests/Program.cs#L139) |
| <code>post-submit query failure is not success</code> | [L143](../../tests/Program.cs#L143) |
| <code>changed recruitment settings are not success</code> | [L147](../../tests/Program.cs#L147) |
| <code>post-submit cooldown prevents immediate rerun</code> | [L152](../../tests/Program.cs#L152) |
| <code>countdown parsing and unknown formats</code> | [L157](../../tests/Program.cs#L157) |
| <code>ownership remains conservative</code> | [L165](../../tests/Program.cs#L165) |
| <code>reported Korean placeholder in both UI sources resolves to an empty comment</code> | [L171](../../tests/Program.cs#L171) |
| <code>literal Korean placeholder text remains a nonempty authored comment</code> | [L177](../../tests/Program.cs#L177) |
| <code>Korean empty placeholder recognition requires exact agreeing UI text</code> | [L182](../../tests/Program.cs#L182) |
| <code>an empty addon buffer still confirms an empty native comment</code> | [L188](../../tests/Program.cs#L188) |
| <code>empty comment does not grant ownership to another character</code> | [L193](../../tests/Program.cs#L193) |
| <code>unreadable native text cannot be mistaken for an empty comment</code> | [L198](../../tests/Program.cs#L198) |
| <code>an empty native comment alone cannot override visible content</code> | [L202](../../tests/Program.cs#L202) |
| <code>nonempty comments must still agree and are never normalized to empty</code> | [L206](../../tests/Program.cs#L206) |
| <code>normal empty node and existing buffer fallback remain valid</code> | [L212](../../tests/Program.cs#L212) |
| <code>empty listing refreshes once without copying the placeholder into the editor</code> | [L218](../../tests/Program.cs#L218) |
| <code>adding text during an empty-comment refresh is detected</code> | [L232](../../tests/Program.cs#L232) |

<a id="file-tests-relayfixture-cs"></a>
## tests/RelayFixture.cs

[소스 파일](../../tests/RelayFixture.cs) · 명명된 항목/진입점 1개

로컬 Worker와 붙일 살아 있는 fake host/Bridge 중계 통합 fixture를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-relayfixture-cs-9-77dd9e3958"></a><code>RelayFixture.Run</code><br><code>Task Run(string[] args)</code> | 메서드 | Miniflare 주소와 테스트 key로 실제 .NET relay client·fake game host를 연결한다. stdin의 상태 변경 명령을 처리하면서 50ms 간격으로 host/Bridge를 진행하고 quit에서 정리한다. | [L9](../../tests/RelayFixture.cs#L9) |

<a id="file-tests-webbrowserfixture-cs"></a>
## tests/WebBrowserFixture.cs

[소스 파일](../../tests/WebBrowserFixture.cs) · 명명된 항목/진입점 1개

실제 loopback server에 fake source를 제공하는 브라우저 smoke fixture를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-webbrowserfixture-cs-8-38274e55df"></a><code>WebBrowserFixture.Run</code><br><code>Task Run()</code> | 메서드 | 브라우저 통합 검사에 사용할 fake game host·Bridge·loopback server를 실행한다. stdin으로 시나리오를 바꾸며 Framework update를 모사하고 종료 시 연결을 정리한다. | [L8](../../tests/WebBrowserFixture.cs#L8) |

<a id="file-tests-browser-smoke-cjs"></a>
## tests/browser-smoke.cjs

[소스 파일](../../tests/browser-smoke.cjs) · 명명된 항목/진입점 2개

Playwright와 WebBrowserFixture의 실제 loopback UI로 조회·slot 표시·편집/결과를 검사한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-browser-smoke-cjs-1-463dee39c7"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | fixture process와 Chromium을 시작하고 DOM 기반 smoke 시나리오를 실행한 뒤 browser/process를 정리한다. | [L1](../../tests/browser-smoke.cjs#L1) |
| <a id="symbol-tests-browser-smoke-cjs-12-1cbcf497d2"></a><code>send</code><br><code>command =&gt;</code> | JS 함수/handler | fixture stdin에 테스트 제어 명령을 보내고 화면 상태 전환을 유도한다. | [L12](../../tests/browser-smoke.cjs#L12) |

<a id="file-tests-test-release-py"></a>
## tests/test_release.py

[소스 파일](../../tests/test_release.py) · 명명된 항목/진입점 14개

unittest/mock으로 배포 URL·버전·의존성 allowlist·해시·공개 다운로드 실패 시 feed 보존을 검사한다. 실제 릴리스를 만들지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-test-release-py-1-8d10909ef5"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | 전체 회귀 test를 등록·실행하고 --web-fixture/--relay-fixture이면 해당 통합 fixture 모드로 분기한다. | [L1](../../tests/test_release.py#L1) |
| <a id="symbol-tests-test-release-py-17-5f81f48351"></a><code>ReleaseTests.test_repository_api_url_has_no_trailing_slash</code><br><code>test_repository_api_url_has_no_trailing_slash(self)</code> | Python 함수 | 배포 회귀 검사: repository api url has no trailing slash. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L17](../../tests/test_release.py#L17) |
| <a id="symbol-tests-test-release-py-23-c469d4374d"></a><code>ReleaseTests.test_actual_tag_is_used_with_or_without_v</code><br><code>test_actual_tag_is_used_with_or_without_v(self)</code> | Python 함수 | 배포 회귀 검사: actual tag is used with or without v. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L23](../../tests/test_release.py#L23) |
| <a id="symbol-tests-test-release-py-29-7e00c8bfc4"></a><code>ReleaseTests.test_mismatched_or_malformed_tag_is_rejected</code><br><code>test_mismatched_or_malformed_tag_is_rejected(self)</code> | Python 함수 | 배포 회귀 검사: mismatched or malformed tag is rejected. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L29](../../tests/test_release.py#L29) |
| <a id="symbol-tests-test-release-py-34-219f1b1d1b"></a><code>ReleaseTests.test_numeric_version_order_prevents_downgrade</code><br><code>test_numeric_version_order_prevents_downgrade(self)</code> | Python 함수 | 배포 회귀 검사: numeric version order prevents downgrade. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L34](../../tests/test_release.py#L34) |
| <a id="symbol-tests-test-release-py-38-b2cbe38e74"></a><code>ReleaseTests.test_other_plugins_are_preserved</code><br><code>test_other_plugins_are_preserved(self)</code> | Python 함수 | 배포 회귀 검사: other plugins are preserved. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L38](../../tests/test_release.py#L38) |
| <a id="symbol-tests-test-release-py-45-f62c7a95d8"></a><code>ReleaseTests.test_duplicate_feed_entries_are_rejected</code><br><code>test_duplicate_feed_entries_are_rejected(self)</code> | Python 함수 | 배포 회귀 검사: duplicate feed entries are rejected. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L45](../../tests/test_release.py#L45) |
| <a id="symbol-tests-test-release-py-50-ed76cd3ee6"></a><code>ReleaseTests.test_package_version_mismatch_fails_before_writing_zip</code><br><code>test_package_version_mismatch_fails_before_writing_zip(self)</code> | Python 함수 | 배포 회귀 검사: package version mismatch fails before writing zip. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L50](../../tests/test_release.py#L50) |
| <a id="symbol-tests-test-release-py-54-02968f93df"></a><code>ReleaseTests.test_package_contains_the_complete_runtime_dependency_set</code><br><code>test_package_contains_the_complete_runtime_dependency_set(self)</code> | Python 함수 | 배포 회귀 검사: package contains the complete runtime dependency set. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L54](../../tests/test_release.py#L54) |
| <a id="symbol-tests-test-release-py-67-300ccab7d0"></a><code>ReleaseTests.test_new_or_native_runtime_dependencies_require_package_review</code><br><code>test_new_or_native_runtime_dependencies_require_package_review(self)</code> | Python 함수 | 배포 회귀 검사: new or native runtime dependencies require package review. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L67](../../tests/test_release.py#L67) |
| <a id="symbol-tests-test-release-py-84-36b4b15c50"></a><code>ReleaseTests.test_wrong_reference_hash_never_extracts_dlls</code><br><code>test_wrong_reference_hash_never_extracts_dlls(self)</code> | Python 함수 | 배포 회귀 검사: wrong reference hash never extracts dlls. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L84](../../tests/test_release.py#L84) |
| <a id="symbol-tests-test-release-py-90-0ae8ee6237"></a><code>ReleaseTests.test_no_non_https_downloads</code><br><code>test_no_non_https_downloads(self)</code> | Python 함수 | 배포 회귀 검사: no non https downloads. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L90](../../tests/test_release.py#L90) |
| <a id="symbol-tests-test-release-py-94-f95dcfc0a4"></a><code>ReleaseTests.test_failed_public_download_never_updates_feed</code><br><code>test_failed_public_download_never_updates_feed(self)</code> | Python 함수 | 배포 회귀 검사: failed public download never updates feed. 외부 API/파일 부작용은 fixture와 mock으로 통제한다. | [L94](../../tests/test_release.py#L94) |
| <a id="symbol-tests-test-release-py-104-0009de0c0b"></a><code>ReleaseTests.test_failed_public_download_never_updates_feed.fake_api</code><br><code>fake_api(repo, path, method='GET', data=None, raw=False)</code> | Python 함수 | publish 경로가 기대한 GitHub GET만 호출하는지 기록하고 feed write나 다른 API 호출은 assertion으로 차단한다. | [L104](../../tests/test_release.py#L104) |

<a id="file-tests-web-connection-test-cjs"></a>
## tests/web-connection.test.cjs

[소스 파일](../../tests/web-connection.test.cjs) · 명명된 항목/진입점 6개

연결 링크/직접 코드·cross-origin 거부·hash 제거·sessionStorage 실패와 미결 요청의 확정 실패 분류를 검사한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-web-connection-test-cjs-1-b23b6d3375"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | Node assertion으로 connection helper의 연결값 파싱/복원과 거절 분류 시나리오를 실행한다. | [L1](../../tests/web-connection.test.cjs#L1) |
| <a id="symbol-tests-web-connection-test-cjs-13-ae8ac13d15"></a><code>getItem</code><br><code>k =&gt;</code> | JS 함수/handler | 테스트용 sessionStorage map에서 값을 반환한다. | [L13](../../tests/web-connection.test.cjs#L13) |
| <a id="symbol-tests-web-connection-test-cjs-13-1c039e763e"></a><code>removeItem</code><br><code>k =&gt;</code> | JS 함수/handler | 테스트용 sessionStorage key를 제거한다. | [L13](../../tests/web-connection.test.cjs#L13) |
| <a id="symbol-tests-web-connection-test-cjs-13-9b1636d742"></a><code>setItem</code><br><code>(k, v) =&gt;</code> | JS 함수/handler | 테스트용 sessionStorage map에 값을 저장한다. | [L13](../../tests/web-connection.test.cjs#L13) |
| <a id="symbol-tests-web-connection-test-cjs-15-ec45ac64b6"></a><code>replaceState</code><br><code>(_, __, path) =&gt;</code> | JS 함수/handler | 테스트용 history.replaceState 호출을 기록하거나 실패를 모사하여 hash 제거 경로를 검증한다. | [L15](../../tests/web-connection.test.cjs#L15) |
| <a id="symbol-tests-web-connection-test-cjs-19-0238255129"></a><code>replaceState</code><br><code>()</code> | JS 함수/handler | 테스트용 history.replaceState 호출을 기록하거나 실패를 모사하여 hash 제거 경로를 검증한다. | [L19](../../tests/web-connection.test.cjs#L19) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>connection accepts same-origin links and opaque room codes, not foreign URLs</code> | [L5](../../tests/web-connection.test.cjs#L5) |
| <code>fresh connection link clears previous room and pending operation</code> | [L11](../../tests/web-connection.test.cjs#L11) |
| <code>valid fragment works even when browser storage is blocked</code> | [L18](../../tests/web-connection.test.cjs#L18) |
| <code>relay/network uncertainty retains the original operation ID</code> | [L21](../../tests/web-connection.test.cjs#L21) |

<a id="file-tests-web-editor-test-cjs"></a>
## tests/web-editor.test.cjs

[소스 파일](../../tests/web-editor.test.cjs) · 명명된 항목/진입점 2개

브라우저와 같은 collect/draft 순수 로직의 no-op·필드 검증·PIN·직업 선택·아이템 레벨 경계를 검사한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tests-web-editor-test-cjs-1-8a56c9f4c9"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | Node assertion으로 입력 수집과 validation 결과를 검사한다. | [L1](../../tests/web-editor.test.cjs#L1) |
| <a id="symbol-tests-web-editor-test-cjs-5-bd13cc949c"></a><code>model</code><br><code>() =&gt; (</code> | JS 함수/handler | 전체 지원 필드와 슬롯/직업을 포함한 editor model fixture를 만든다. | [L5](../../tests/web-editor.test.cjs#L5) |

### 등록된 회귀 시나리오

| 소스의 시나리오 이름 | 위치 |
|---|---|
| <code>a fresh draft sends nothing and does not mutate its source</code> | [L10](../../tests/web-editor.test.cjs#L10) |
| <code>batch contains only changed fields with physical friendly-match positions</code> | [L15](../../tests/web-editor.test.cjs#L15) |
| <code>job ordering alone is not a change; no jobs is invalid</code> | [L22](../../tests/web-editor.test.cjs#L22) |
| <code>0000 and leading zeros are retained and PIN is only sent when explicitly changed</code> | [L27](../../tests/web-editor.test.cjs#L27) |
| <code>enabling private requires four ASCII digits</code> | [L36](../../tests/web-editor.test.cjs#L36) |
| <code>item-level accepts 1 and 999, rejects empty/noninteger/out-of-range, off sends zero</code> | [L42](../../tests/web-editor.test.cjs#L42) |
| <code>Korean description counts UTF-8 bytes and empty description remains an explicit change</code> | [L50](../../tests/web-editor.test.cjs#L50) |
| <code>unsupported fields are omitted, boolean rules and enum values keep correct types</code> | [L57](../../tests/web-editor.test.cjs#L57) |
