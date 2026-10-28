# 전체 추적 파일 지도

[전체 지도](../CODE_MAP.md)

현재 문서와 신규 채팅/역할 검증 파일을 포함한 git tracked 파일 전체를 분류했다. 생성물(bin/obj/node_modules/relay/assets), 사용자 설정/토큰, 로컬 분석 도구는 문서 범위에서 제외한다. 이 문서 묶음 자체는 아래 안내와 연결되어 있다.

| 경로 | 역할 / 연결 문서 |
|---|---|
| [.github/workflows/release.yml](../../.github/workflows/release.yml) | workflow_dispatch 전용. 고정 참조/ABI→관리형·Python 테스트→build/package→artifact 순서. publish=true인 main 수동 실행만 release/feed 게시한다. Node/Miniflare/Playwright는 이 workflow에 자동 포함돼 있지 않다. |
| [.gitignore](../../.gitignore) | build/reference/node_modules/relay generated assets·로컬 secret·release output 등 재생성물/로컬 파일 제외 규칙. |
| [PartyViewKR.deps.json](../../PartyViewKR.deps.json) | 루트 이전 수동 배포 DLL에 동반된 의존성 manifest. 현재 패키지는 src build output의 deps를 사용한다. |
| [PartyViewKR.dll](../../PartyViewKR.dll) | 루트에 남은 이전 수동 업로드 DLL. 최신 build/release의 입력 또는 설치 대상이 아니다. |
| [PartyViewKR.json](../../PartyViewKR.json) | 루트에 남은0.3.1.0 수동 manifest. 현재 기준 src/PartyViewKR.json과 혼동하지 않는다. |
| [README.md](../../README.md) | 설치/업데이트·버전별 작업 설명·수동 빌드 안내. 현재 아키텍처 추적은 CODE_MAP에서 시작한다. |
| [THIRD-PARTY-NOTICES.txt](../../THIRD-PARTY-NOTICES.txt) | 배포에 포함되는 의존성의 라이선스/출처 고지. 패키지 allowlist에도 포함된다. |
| [compatibility/interop-contract.json](../../compatibility/interop-contract.json) | InteropGenerator.Runtime의 CStringPointer ABI baseline. |
| [compatibility/native-contract.json](../../compatibility/native-contract.json) | 검토된 FFXIVClientStructs ABI baseline. 빌드와 runtime 검사에서 shared NativeContract와 함께 사용한다. 임의 재캡처로 불일치를 숨기지 않는다. |
| [compatibility/reference-lock.json](../../compatibility/reference-lock.json) | 참조 DLL archive URL·SHA-256·기록 버전. release.references가 무결성을 검증한다. |
| [docs/discord.md](../../docs/discord.md) | 개인 Discord 봇 토큰/owner·설치/연결/카드 조작 안내. |
| [pluginmaster.json](../../pluginmaster.json) | Dalamud 사용자 저장소 feed. 검증된 공개 release ZIP을 가리킨다. publish 도구가 마지막 단계에서 갱신한다. |
| [relay/README.md](../../relay/README.md) | Cloudflare 설정/secret·빌드 root·주소 연결·로컬 테스트/운영 설명. |
| [relay/build.mjs](../../relay/build.mjs) | 플러그인 embedded Web UI와 중계 UI의 단일 소스를 유지하는 빌드 단계. [함수 목록](web-relay.md#file-relay-build-mjs) |
| [relay/package-lock.json](../../relay/package-lock.json) | relay npm 의존성 lock; CI npm ci가 사용한다. |
| [relay/package.json](../../relay/package.json) | relay npm scripts와 Wrangler/Miniflare 개발 의존성. package version0.3.33은 plugin AssemblyVersion과 별개다. |
| [relay/src/protocol.mjs](../../relay/src/protocol.mjs) | Worker 공통 HTTP/인증/route/body helper. 호스트와 브라우저의 허용 경계를 정한다. [함수 목록](web-relay.md#file-relay-src-protocol-mjs) |
| [relay/src/worker.mjs](../../relay/src/worker.mjs) | Cloudflare Worker 라우터와 PvkrRoom Durable Object. 연결/대기 응답은 메모리에 두며 게임 명령은 영구 저장/재전송하지 않는다. [함수 목록](web-relay.md#file-relay-src-worker-mjs) |
| [relay/test/relay.test.mjs](../../relay/test/relay.test.mjs) | Miniflare 로컬 Worker와 fake WebSocket 및 실제 .NET relay fixture로 인증·host 교체·frame/timeout·apply를 통합 검증한다. Cloudflare 배포나 Wrangler dev 서버를 띄우는 테스트가 아니다. [함수 목록](tests.md#file-relay-test-relay-test-mjs) |
| [relay/wrangler.jsonc](../../relay/wrangler.jsonc) | Worker entry·ASSETS·ROOM(PvkrRoom) binding·SQLite migration v1·compatibility date·run_worker_first를 선언한다. PVKR_CONNECT_KEY 실제 값은 여기 저장하지 않는다. |
| [shared/NativeContract.cs](../../shared/NativeContract.cs) | 실행/Assembly.Load 없이 PE metadata를 읽는 ABI 계약 도구. 플러그인 runtime 검사와 CompatibilityTool이 같은 코드를 공유한다. [함수 목록](compat-release.md#file-shared-nativecontract-cs) |
| [src/AfkListingMonitor.cs](../../src/AfkListingMonitor.cs) | AFK 감시의 순수 상태 기계. CID 명단 변화와 선택한 주기로 내 모집글을 조회하고 실패한 필요 상태를 보존한다. host 계약에는 수정/제출 기능이 없다. [함수 목록](core.md#file-src-afklistingmonitor-cs) |
| [src/AfkSettings.cs](../../src/AfkSettings.cs) | 영구 저장할 AFK 조회 정책의 불변 값. 주기 조회는 기본 꺼짐, 실패 재시도는 기본 켜짐·5초이며 AFK 실행 여부는 저장하지 않는다. [함수 목록](core.md#file-src-afksettings-cs) |
| [src/DiscordChatFormatting.cs](../../src/DiscordChatFormatting.cs) | 채팅을 모바일에서도 표시되는 색상 임베드로 만든다. 일반 봇 메시지이며 조회 카드의 Components V2/만료 수명과 별개다. [함수 목록](discord.md#file-src-discordchatformatting-cs) |
| [src/DiscordChatSettings.cs](../../src/DiscordChatSettings.cs) | 채팅 종류별 서버/채널/everyone 선호 설정과 관리형 메시지·명령 모델. 설정에는 채팅 본문을 저장하지 않는다. [함수 목록](discord.md#file-src-discordchatsettings-cs) |
| [src/DiscordIcons/HD.png](../../src/DiscordIcons/HD.png) | embedded 슬롯 이미지 `HD`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/README.md](../../src/DiscordIcons/README.md) | 80개 사용자 제공320×320 PNG의 출처·파일 의미·join naming·legacy rename 규칙. |
| [src/DiscordIcons/TD.png](../../src/DiscordIcons/TD.png) | embedded 슬롯 이미지 `TD`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/TH.png](../../src/DiscordIcons/TH.png) | embedded 슬롯 이미지 `TH`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/THD.png](../../src/DiscordIcons/THD.png) | embedded 슬롯 이미지 `THD`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/acn.png](../../src/DiscordIcons/acn.png) | embedded 슬롯 이미지 `acn`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/any.png](../../src/DiscordIcons/any.png) | embedded 슬롯 이미지 `any`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/arc.png](../../src/DiscordIcons/arc.png) | embedded 슬롯 이미지 `arc`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/ast.png](../../src/DiscordIcons/ast.png) | embedded 슬롯 이미지 `ast`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/blm.png](../../src/DiscordIcons/blm.png) | embedded 슬롯 이미지 `blm`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/blu.png](../../src/DiscordIcons/blu.png) | embedded 슬롯 이미지 `blu`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/brd.png](../../src/DiscordIcons/brd.png) | embedded 슬롯 이미지 `brd`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/bst.png](../../src/DiscordIcons/bst.png) | embedded 슬롯 이미지 `bst`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_HD.png](../../src/DiscordIcons/checked_HD.png) | embedded 슬롯 이미지 `HD`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_TD.png](../../src/DiscordIcons/checked_TD.png) | embedded 슬롯 이미지 `TD`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_TH.png](../../src/DiscordIcons/checked_TH.png) | embedded 슬롯 이미지 `TH`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_THD.png](../../src/DiscordIcons/checked_THD.png) | embedded 슬롯 이미지 `THD`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_acn.png](../../src/DiscordIcons/checked_acn.png) | embedded 슬롯 이미지 `acn`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_any.png](../../src/DiscordIcons/checked_any.png) | embedded 슬롯 이미지 `any`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_arc.png](../../src/DiscordIcons/checked_arc.png) | embedded 슬롯 이미지 `arc`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_ast.png](../../src/DiscordIcons/checked_ast.png) | embedded 슬롯 이미지 `ast`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_blm.png](../../src/DiscordIcons/checked_blm.png) | embedded 슬롯 이미지 `blm`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_blu.png](../../src/DiscordIcons/checked_blu.png) | embedded 슬롯 이미지 `blu`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_brd.png](../../src/DiscordIcons/checked_brd.png) | embedded 슬롯 이미지 `brd`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_bst.png](../../src/DiscordIcons/checked_bst.png) | embedded 슬롯 이미지 `bst`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_cnj.png](../../src/DiscordIcons/checked_cnj.png) | embedded 슬롯 이미지 `cnj`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_dealer.png](../../src/DiscordIcons/checked_dealer.png) | embedded 슬롯 이미지 `dealer`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_dnc.png](../../src/DiscordIcons/checked_dnc.png) | embedded 슬롯 이미지 `dnc`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_drg.png](../../src/DiscordIcons/checked_drg.png) | embedded 슬롯 이미지 `drg`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_drk.png](../../src/DiscordIcons/checked_drk.png) | embedded 슬롯 이미지 `drk`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_gla.png](../../src/DiscordIcons/checked_gla.png) | embedded 슬롯 이미지 `gla`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_gnb.png](../../src/DiscordIcons/checked_gnb.png) | embedded 슬롯 이미지 `gnb`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_healer.png](../../src/DiscordIcons/checked_healer.png) | embedded 슬롯 이미지 `healer`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_lnc.png](../../src/DiscordIcons/checked_lnc.png) | embedded 슬롯 이미지 `lnc`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_mch.png](../../src/DiscordIcons/checked_mch.png) | embedded 슬롯 이미지 `mch`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_mnk.png](../../src/DiscordIcons/checked_mnk.png) | embedded 슬롯 이미지 `mnk`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_mrd.png](../../src/DiscordIcons/checked_mrd.png) | embedded 슬롯 이미지 `mrd`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_nin.png](../../src/DiscordIcons/checked_nin.png) | embedded 슬롯 이미지 `nin`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_pgl.png](../../src/DiscordIcons/checked_pgl.png) | embedded 슬롯 이미지 `pgl`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_pic.png](../../src/DiscordIcons/checked_pic.png) | embedded 슬롯 이미지 `pic`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_pld.png](../../src/DiscordIcons/checked_pld.png) | embedded 슬롯 이미지 `pld`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_rdm.png](../../src/DiscordIcons/checked_rdm.png) | embedded 슬롯 이미지 `rdm`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_rog.png](../../src/DiscordIcons/checked_rog.png) | embedded 슬롯 이미지 `rog`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_rpr.png](../../src/DiscordIcons/checked_rpr.png) | embedded 슬롯 이미지 `rpr`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_sam.png](../../src/DiscordIcons/checked_sam.png) | embedded 슬롯 이미지 `sam`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_sch.png](../../src/DiscordIcons/checked_sch.png) | embedded 슬롯 이미지 `sch`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_sge.png](../../src/DiscordIcons/checked_sge.png) | embedded 슬롯 이미지 `sge`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_smn.png](../../src/DiscordIcons/checked_smn.png) | embedded 슬롯 이미지 `smn`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_tanker.png](../../src/DiscordIcons/checked_tanker.png) | embedded 슬롯 이미지 `tanker`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_thm.png](../../src/DiscordIcons/checked_thm.png) | embedded 슬롯 이미지 `thm`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_vip.png](../../src/DiscordIcons/checked_vip.png) | embedded 슬롯 이미지 `vip`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_war.png](../../src/DiscordIcons/checked_war.png) | embedded 슬롯 이미지 `war`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/checked_whm.png](../../src/DiscordIcons/checked_whm.png) | embedded 슬롯 이미지 `whm`의 참가 완료(checked→join_) 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/cnj.png](../../src/DiscordIcons/cnj.png) | embedded 슬롯 이미지 `cnj`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/dealer.png](../../src/DiscordIcons/dealer.png) | embedded 슬롯 이미지 `dealer`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/dnc.png](../../src/DiscordIcons/dnc.png) | embedded 슬롯 이미지 `dnc`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/drg.png](../../src/DiscordIcons/drg.png) | embedded 슬롯 이미지 `drg`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/drk.png](../../src/DiscordIcons/drk.png) | embedded 슬롯 이미지 `drk`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/gla.png](../../src/DiscordIcons/gla.png) | embedded 슬롯 이미지 `gla`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/gnb.png](../../src/DiscordIcons/gnb.png) | embedded 슬롯 이미지 `gnb`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/healer.png](../../src/DiscordIcons/healer.png) | embedded 슬롯 이미지 `healer`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/lnc.png](../../src/DiscordIcons/lnc.png) | embedded 슬롯 이미지 `lnc`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/mch.png](../../src/DiscordIcons/mch.png) | embedded 슬롯 이미지 `mch`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/mnk.png](../../src/DiscordIcons/mnk.png) | embedded 슬롯 이미지 `mnk`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/mrd.png](../../src/DiscordIcons/mrd.png) | embedded 슬롯 이미지 `mrd`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/nin.png](../../src/DiscordIcons/nin.png) | embedded 슬롯 이미지 `nin`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/pgl.png](../../src/DiscordIcons/pgl.png) | embedded 슬롯 이미지 `pgl`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/pic.png](../../src/DiscordIcons/pic.png) | embedded 슬롯 이미지 `pic`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/pld.png](../../src/DiscordIcons/pld.png) | embedded 슬롯 이미지 `pld`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/rdm.png](../../src/DiscordIcons/rdm.png) | embedded 슬롯 이미지 `rdm`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/rog.png](../../src/DiscordIcons/rog.png) | embedded 슬롯 이미지 `rog`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/rpr.png](../../src/DiscordIcons/rpr.png) | embedded 슬롯 이미지 `rpr`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/sam.png](../../src/DiscordIcons/sam.png) | embedded 슬롯 이미지 `sam`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/sch.png](../../src/DiscordIcons/sch.png) | embedded 슬롯 이미지 `sch`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/sge.png](../../src/DiscordIcons/sge.png) | embedded 슬롯 이미지 `sge`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/smn.png](../../src/DiscordIcons/smn.png) | embedded 슬롯 이미지 `smn`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/tanker.png](../../src/DiscordIcons/tanker.png) | embedded 슬롯 이미지 `tanker`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/thm.png](../../src/DiscordIcons/thm.png) | embedded 슬롯 이미지 `thm`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/vip.png](../../src/DiscordIcons/vip.png) | embedded 슬롯 이미지 `vip`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/war.png](../../src/DiscordIcons/war.png) | embedded 슬롯 이미지 `war`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/DiscordIcons/whm.png](../../src/DiscordIcons/whm.png) | embedded 슬롯 이미지 `whm`의 빈자리/일반 변형. 실제 이모지 이름/ID 재사용은 [EmojiAssets](discord.md#file-src-listingdiscordemojiassets-cs)가 담당한다. |
| [src/GameChatCapture.cs](../../src/GameChatCapture.cs) | 런타임 IChatGui의 공개 ChatMessage 이벤트를 읽는 어댑터. 한섭/공개판의 legacy5인자와 현재 message1인자 delegate를 모두 지원하며 새 native ABI 계약을 추가하지 않는다. [함수 목록](core.md#file-src-gamechatcapture-cs) |
| [src/GameChatSending.cs](../../src/GameChatSending.cs) | Discord→게임 전송의 순수 모델·입력 검증·단일 대기열. Framework만 native host를 호출하고 답장은 작성 시 상대를 고정한다. [함수 목록](discord.md#file-src-gamechatsending-cs) |
| [src/GameChatSession.cs](../../src/GameChatSession.cs) | AFK와 별개로 채팅 수신 캐릭터와 홈 서버 이름을 보관한다. 게임 스레드에서만 갱신하며 지역 이동의 일시적인 PlayerState 공백을 보존한다. [함수 목록](core.md#file-src-gamechatsession-cs) |
| [src/ListingBatchEditHost.cs](../../src/ListingBatchEditHost.cs) | 배치 항목 사이의 실제 draft 읽기, 자식 창 정리, 소개말 부분 반영 복구 및 최종 1회 제출을 담당한다. [함수 목록](refresh.md#file-src-listingbatchedithost-cs) |
| [src/ListingBatchRefresh.cs](../../src/ListingBatchRefresh.cs) | 여러 편집을 한 수정창에 순서대로 적용하고 가능한 항목만 1회 제출한다. 항목 실패와 전체 제출 확신을 별도로 보관한다. [함수 목록](refresh.md#file-src-listingbatchrefresh-cs) |
| [src/ListingBatchSnapshot.cs](../../src/ListingBatchSnapshot.cs) | 수정창의 누적 실제 조건을 검증 가능한 RefreshTarget으로 재구성한다. [함수 목록](edits.md#file-src-listingbatchsnapshot-cs) |
| [src/ListingBeginnerEdit.cs](../../src/ListingBeginnerEdit.cs) | 초행 및 초보자 환영의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listingbeginneredit-cs) |
| [src/ListingBeginnerEditHost.cs](../../src/ListingBeginnerEditHost.cs) | 초행 및 초보자 환영의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다. [함수 목록](refresh.md#file-src-listingbeginneredithost-cs) |
| [src/ListingChange.cs](../../src/ListingChange.cs) | 개별 typed edit를 공통 배치 항목으로 연결하고 같은 원본의 변경을 필드/자리별로 중복 없이 보관한다. [함수 목록](edits.md#file-src-listingchange-cs) |
| [src/ListingCheckboxUi.cs](../../src/ListingCheckboxUi.cs) | 단일 체크박스 순수 상태 기계. Next가 반환하는 지시를 native Host가 실행한다. 상태를 먼저 소비해 반복 클릭을 방지한다. [함수 목록](edits.md#file-src-listingcheckboxui-cs) |
| [src/ListingCompletionEdit.cs](../../src/ListingCompletionEdit.cs) | 완료조건의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listingcompletionedit-cs) |
| [src/ListingCompletionEditHost.cs](../../src/ListingCompletionEditHost.cs) | 완료조건의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다. [함수 목록](refresh.md#file-src-listingcompletionedithost-cs) |
| [src/ListingDescription.cs](../../src/ListingDescription.cs) | 상세창 TextNode·문자열 buffer·네이티브 comment를 결합한다. 빈 소개말의 게임 대체문구를 실제 모집글로 착각하지 않는다. [함수 목록](core.md#file-src-listingdescription-cs) |
| [src/ListingDescriptionEdit.cs](../../src/ListingDescriptionEdit.cs) | 소개말의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listingdescriptionedit-cs) |
| [src/ListingDescriptionEditHost.cs](../../src/ListingDescriptionEditHost.cs) | 소개말의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다. [함수 목록](refresh.md#file-src-listingdescriptionedithost-cs) |
| [src/ListingDescriptionRecovery.cs](../../src/ListingDescriptionRecovery.cs) | 배치 소개말 실패 후 화면 입력과 실제 저장 comment를 맞추는 순수 상태 기계. 이미 보낸 변경 callback/제출을 재시도하지 않는다. [함수 목록](edits.md#file-src-listingdescriptionrecovery-cs) |
| [src/ListingDiscordChat.cs](../../src/ListingDiscordChat.cs) | 종류별 저장 경로·수신/멘션 선택과 단일 전송 큐를 소유한다. 인게임/슬래시 명령 변경을 같은 semaphore로 직렬화하며, 설정 변경은 과거 채팅을 새 목적지로 보내지 않는다. [함수 목록](discord.md#file-src-listingdiscordchat-cs) |
| [src/ListingDiscordClient.Chat.cs](../../src/ListingDiscordClient.Chat.cs) | Discord.Net slash 등록/엄격한 입력 파싱과 채팅 서버 채널 전송 어댑터. 기존 pvkr 카드 경로와 독립적으로 pvkr-chat 명령을 등록한다. [함수 목록](discord.md#file-src-listingdiscordclient-chat-cs) |
| [src/ListingDiscordClient.ChatSend.cs](../../src/ListingDiscordClient.ChatSend.cs) | pvkr-send 슬래시 명령의 등록 정의와 허용된4종 하위 명령 해석. [함수 목록](discord.md#file-src-listingdiscordclient-chatsend-cs) |
| [src/ListingDiscordClient.Notifications.cs](../../src/ListingDiscordClient.Notifications.cs) | 모집 완료 알림의 Discord.Net 전송 어댑터. 서버 텍스트 채널·bot 권한을 검사하고 everyone만 허용한 새 메시지를 보내며 서버 mention 응답을 확인한다. [함수 목록](discord.md#file-src-listingdiscordclient-notifications-cs) |
| [src/ListingDiscordClient.cs](../../src/ListingDiscordClient.cs) | Discord.Net Gateway/REST 연결과 application owner 검증, /pvkr 등록, 이모지 동기화 및 SDK interaction 어댑터. 네트워크 작업은 게임 메모리를 직접 읽지 않는다. [함수 목록](discord.md#file-src-listingdiscordclient-cs) |
| [src/ListingDiscordEmojiAssets.cs](../../src/ListingDiscordEmojiAssets.cs) | embedded PNG 80개의 asset key·읽기 쉬운 이름·legacy 이름 매핑 및 application emoji 동기화. 서버에 수동 업로드할 필요가 없다. [함수 목록](discord.md#file-src-listingdiscordemojiassets-cs) |
| [src/ListingDiscordIcons.cs](../../src/ListingDiscordIcons.cs) | 봇 연결별 application emoji ID registry와 슬롯 이미지 선택. 참가 직업으로 제한 mask를 덮지 않고 원래 제한 이미지에 join 상태를 결합한다. [함수 목록](discord.md#file-src-listingdiscordicons-cs) |
| [src/ListingDiscordNotifications.cs](../../src/ListingDiscordNotifications.cs) | AFK 완료 세대별 everyone 알림 controller. 명시적으로 선택한 목적지만 저장하며 비동기 전송 중 취소/권한 실패/불확실한 POST의 중복 멘션을 막는다. [함수 목록](discord.md#file-src-listingdiscordnotifications-cs) |
| [src/ListingDiscordPanel.Forms.cs](../../src/ListingDiscordPanel.Forms.cs) | 소개말·비공개와 묶음 설정의 modal 구성/파싱. 외부 input은 반드시 현재 editor와 allowlisted field에 묶는다. [함수 목록](discord.md#file-src-listingdiscordpanel-forms-cs) |
| [src/ListingDiscordPanel.Render.cs](../../src/ListingDiscordPanel.Render.cs) | 모바일 조회/수정/상세 카드와 Components V2 공통 builder. 조회에는 전체 그룹 아이콘을 한 번에 표시한다. [함수 목록](discord.md#file-src-listingdiscordpanel-render-cs) |
| [src/ListingDiscordPanel.Results.cs](../../src/ListingDiscordPanel.Results.cs) | 긴 최근 작업 결과를 생략하지 않고 독립된 읽기 전용 페이지 카드로 표시한다. [함수 목록](discord.md#file-src-listingdiscordpanel-results-cs) |
| [src/ListingDiscordPanel.cs](../../src/ListingDiscordPanel.cs) | SDK와 게임 메모리에서 분리된 카드/session 상태 모델. Overview·Editor·Details·Results에 nonce와 원본 revision을 묶어 오래된 버튼/폼을 거부한다. [함수 목록](discord.md#file-src-listingdiscordpanel-cs) |
| [src/ListingDiscordSession.Chat.cs](../../src/ListingDiscordSession.Chat.cs) | 기존 owner/app 인증 뒤 실행되는 채팅 설정 interaction. 게임 조회/수정/카드 상태를 바꾸지 않고 설정 명령 결과만 본인에게 응답한다. [함수 목록](discord.md#file-src-listingdiscordsession-chat-cs) |
| [src/ListingDiscordSession.ChatSend.cs](../../src/ListingDiscordSession.ChatSend.cs) | 소유자 인증을 통과한 채팅 입력창과 제출 처리. PF 편집과 별도 직렬화하며 ACK 전에는 전송을 예약하지 않는다. [함수 목록](discord.md#file-src-listingdiscordsession-chatsend-cs) |
| [src/ListingDiscordSession.Notifications.cs](../../src/ListingDiscordSession.Notifications.cs) | 알림 채널 선택 interaction. Process의 owner/app 인증 뒤 session/중복/ACK를 확인하며 서버·채널 ID는 SDK가 읽은 현재 interaction에서만 가져온다. [함수 목록](discord.md#file-src-listingdiscordsession-notifications-cs) |
| [src/ListingDiscordSession.Recovery.cs](../../src/ListingDiscordSession.Recovery.cs) | 일반 interaction의 handling 잠금과 별도로 닫기 interaction을 처리한다. 개인 봇 owner/app 인증은 Process에서 먼저 수행하고 session·ACK를 추가 검증한다. [함수 목록](discord.md#file-src-listingdiscordsession-recovery-cs) |
| [src/ListingDiscordSession.cs](../../src/ListingDiscordSession.cs) | 한 사용자 interaction의 인증·ACK·게임 요청·결과 폴링을 직렬화한다. ACK 실패 뒤 게임 작업을 실행하지 않는 것이 핵심이다. [함수 목록](discord.md#file-src-listingdiscordsession-cs) |
| [src/ListingEditorCriteria.cs](../../src/ListingEditorCriteria.cs) | 조회 시 0으로 보이는 친선경기 A/B 6~8번 padding과 수정창의 PvP 기본 마스크 차이만 한정적으로 허용한다. [함수 목록](core.md#file-src-listingeditorcriteria-cs) |
| [src/ListingIdentity.cs](../../src/ListingIdentity.cs) | 모집 작성자 분류와 문자열 정규화. CID·이름·소개말 증거가 맞지 않으면 Self로 추정하지 않는다. [함수 목록](core.md#file-src-listingidentity-cs) |
| [src/ListingItemLevelEdit.cs](../../src/ListingItemLevelEdit.cs) | 평균 아이템 레벨의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listingitemleveledit-cs) |
| [src/ListingItemLevelEditHost.cs](../../src/ListingItemLevelEditHost.cs) | 평균 아이템 레벨의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다. [함수 목록](refresh.md#file-src-listingitemleveledithost-cs) |
| [src/ListingJobEdit.cs](../../src/ListingJobEdit.cs) | 허용 직업의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listingjobedit-cs) |
| [src/ListingJobRules.cs](../../src/ListingJobRules.cs) | ClassJob row ID와 모집 비트 위치의 명시적 대응, 사용자 지정 직업 순서, 허용 직업 설명을 제공한다. [함수 목록](core.md#file-src-listingjobrules-cs) |
| [src/ListingLootUi.cs](../../src/ListingLootUi.cs) | 전리품 dropdown 순수 상태 기계. Next가 반환하는 지시를 native Host가 실행한다. 상태를 먼저 소비해 반복 클릭을 방지한다. [함수 목록](edits.md#file-src-listinglootui-cs) |
| [src/ListingObjectiveEdit.cs](../../src/ListingObjectiveEdit.cs) | 모집 목적의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listingobjectiveedit-cs) |
| [src/ListingObjectiveEditHost.cs](../../src/ListingObjectiveEditHost.cs) | 모집 목적의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다. [함수 목록](refresh.md#file-src-listingobjectiveedithost-cs) |
| [src/ListingObjectiveTrace.cs](../../src/ListingObjectiveTrace.cs) | 수동 UI 조작 관찰기. editor/dropdown/checkbox/list ReceiveEvent와 editor FireCallback을 hook하지만 이벤트/콜백을 새로 생성하지 않는다. [함수 목록](compat-release.md#file-src-listingobjectivetrace-cs) |
| [src/ListingObjectiveUi.cs](../../src/ListingObjectiveUi.cs) | 목적 dropdown 순수 상태 기계. Next가 반환하는 지시를 native Host가 실행한다. 상태를 먼저 소비해 반복 클릭을 방지한다. [함수 목록](edits.md#file-src-listingobjectiveui-cs) |
| [src/ListingPrivateEdit.cs](../../src/ListingPrivateEdit.cs) | 비공개/PIN의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listingprivateedit-cs) |
| [src/ListingPrivateEditHost.cs](../../src/ListingPrivateEditHost.cs) | 비공개/PIN의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다. [함수 목록](refresh.md#file-src-listingprivateedithost-cs) |
| [src/ListingReader.cs](../../src/ListingReader.cs) | 본인 모집글 조회 예약과 상세창 읽기. 상세창 수명 revision, UI 문자열, LastViewedListing의 소유자·직업·조건을 교차 확인한다. [함수 목록](core.md#file-src-listingreader-cs) |
| [src/ListingRefresh.cs](../../src/ListingRefresh.cs) | 단일 갱신 상태 기계와 배치 façade, IListingRefreshHost 계약, 갱신 증거 판정. 상태는 네이티브 호출 전에 진행시켜 재발송을 방지한다. [함수 목록](refresh.md#file-src-listingrefresh-cs) |
| [src/ListingRefreshHost.cs](../../src/ListingRefreshHost.cs) | 게임 스레드에서 상세창/수정창과 AgentLookingForGroup을 다루는 네이티브 경계. 여러 partial 파일이 동일 클래스 상태를 공유한다. [함수 목록](refresh.md#file-src-listingrefreshhost-cs) |
| [src/ListingRelayClient.cs](../../src/ListingRelayClient.cs) | 게임 PC에서 Cloudflare로 나가는 WSS client. browser key/room을 발급하고 연결 실패 시 transport만 재연결한다. [함수 목록](web-relay.md#file-src-listingrelayclient-cs) |
| [src/ListingRelayProtocol.cs](../../src/ListingRelayProtocol.cs) | 중계 URL/key 검증 및 Worker→plugin WebSocket frame의 허용된 API 매핑. frame v1과 browser API v2는 서로 다른 버전이다. [함수 목록](web-relay.md#file-src-listingrelayprotocol-cs) |
| [src/ListingRoleButtons.cs](../../src/ListingRoleButtons.cs) | 검증된 KR 직업 node ID 표와 클릭 계획. 신규 직업이 생기면 catalog/asset/contract와 함께 검토해야 한다. [함수 목록](edits.md#file-src-listingrolebuttons-cs) |
| [src/ListingRoleComparison.cs](../../src/ListingRoleComparison.cs) | 직업 mask 불일치의 상세 진단 생성. 비교 허용 여부와 별개로 raw 차이를 남긴다. [함수 목록](core.md#file-src-listingrolecomparison-cs) |
| [src/ListingRoleNormalization.cs](../../src/ListingRoleNormalization.cs) | 모집글에 등록된 참가자의 입장 직업으로 허용 mask가 좁아지는 게임 자동 변경만 판별한다. 현재 파티 직업은 읽지 않으며 원본 슬롯/아이콘 데이터를 덮어쓰지 않는다. [함수 목록](core.md#file-src-listingrolenormalization-cs) |
| [src/ListingRuleEdit.cs](../../src/ListingRuleEdit.cs) | 임무 찾기/전리품 규칙의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listingruleedit-cs) |
| [src/ListingRuleEditHost.cs](../../src/ListingRuleEditHost.cs) | 임무 찾기/전리품 규칙의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다. [함수 목록](refresh.md#file-src-listingruleedithost-cs) |
| [src/ListingSlotEditHost.cs](../../src/ListingSlotEditHost.cs) | 직업 편집의 실제 버튼 실행과 guard. 일반/24인/48인/친선 경로를 분리하며 확인 후 실제 raw mask를 고정한다. [함수 목록](refresh.md#file-src-listingslotedithost-cs) |
| [src/ListingSlotRoute.cs](../../src/ListingSlotRoute.cs) | 1~8인·24인·48인·친선경기의 자리 클릭 경로와 수정창 형태를 판정한다. [함수 목록](core.md#file-src-listingslotroute-cs) |
| [src/ListingSlots.cs](../../src/ListingSlots.cs) | 등록된 자리만 표시하는 압축 목록과 8/24/48개 물리 슬롯을 함께 관리한다. 표시 Position과 NativeIndex를 절대 혼용하지 않는다. [함수 목록](core.md#file-src-listingslots-cs) |
| [src/ListingSnapshot.cs](../../src/ListingSnapshot.cs) | 마지막 모집글의 관리형 표시 모델. 본인 CID, 소유자, Live/ReadAt, 조건·직업 슬롯·진단을 보관한다. [함수 목록](core.md#file-src-listingsnapshot-cs) |
| [src/ListingUniqueJobsEdit.cs](../../src/ListingUniqueJobsEdit.cs) | 잡 중복 없음의 관리형 초안·요청·원본/결과 비교 모델. Supports는 snapshot을 편집 모델로 해석할 수 있다는 뜻이며 게임 컨트롤이 실제로 enabled인지는 Host가 확인한다. [함수 목록](edits.md#file-src-listinguniquejobsedit-cs) |
| [src/ListingUniqueJobsEditHost.cs](../../src/ListingUniqueJobsEditHost.cs) | 잡 중복 없음의 실제 게임 UI 조작. 시작 시 컴포넌트 주소·수명·불변식을 고정하고 수정 중 재검증하며 UI와 저장값이 모두 일치해야 완료한다. [함수 목록](refresh.md#file-src-listinguniquejobsedithost-cs) |
| [src/ListingWebBridge.Edit.cs](../../src/ListingWebBridge.Edit.cs) | 외부 수정/갱신 큐와 결과 ledger. session당 write ID는 최대 1024개 유지하고 edit revision은 접수할 때 소비한다. [함수 목록](web-relay.md#file-src-listingwebbridge-edit-cs) |
| [src/ListingWebBridge.Recovery.cs](../../src/ListingWebBridge.Recovery.cs) | 명시적인 디스코드 수정창 닫기의 공통 queue. 기존 query/apply busy와 무관하게 접수하고 게임 스레드에서 먼저 중단한다. 닫기 ticket의 native 주소는 외부에 직렬화하지 않는다. [함수 목록](web-relay.md#file-src-listingwebbridge-recovery-cs) |
| [src/ListingWebBridge.cs](../../src/ListingWebBridge.cs) | 네트워크와 Framework 사이의 lock 기반 관리형 경계. 네트워크는 JSON snapshot을 읽고 요청을 큐에 넣으며 실제 게임 실행은 Update에서만 한다. [함수 목록](web-relay.md#file-src-listingwebbridge-cs) |
| [src/ListingWebEdit.cs](../../src/ListingWebEdit.cs) | 외부 editor DTO와 strict JSON decoder. 비밀번호는 모델/상태 응답에 포함하지 않는다. 최대 body64KiB, 변경59개(48자리+11필드)다. [함수 목록](web-relay.md#file-src-listingwebedit-cs) |
| [src/ListingWebRequestGuard.cs](../../src/ListingWebRequestGuard.cs) | 외부 연결이 소유한 조회/갱신 serial만 취소할 수 있게 한다. 다른 수동 요청을 잘못 취소하지 않도록 source identity와 update 간격을 검사한다. [함수 목록](web-relay.md#file-src-listingwebrequestguard-cs) |
| [src/ListingWebServer.cs](../../src/ListingWebServer.cs) | 127.0.0.1 임의 포트의 제한된 HTTP 서버. 같은 PC 웹 UI용이며 inbound 공용 서버가 아니다. static resource와 인증된 API를 제공한다. [함수 목록](web-relay.md#file-src-listingwebserver-cs) |
| [src/ListingWebView.cs](../../src/ListingWebView.cs) | 외부에 노출하는 최소 관리형 표시 모델. 원시 CID/flag/PIN/상세 진단을 제외하고 전체 그룹과 등록 직업·허용 직업 ID를 포함한다. [함수 목록](web-relay.md#file-src-listingwebview-cs) |
| [src/NativeCompatibility.cs](../../src/NativeCompatibility.cs) | 빌드에 embedded한 ABI 계약과 현재 로드된 native/interop DLL의 PE metadata를 비교한다. 버전/MVID 차이만으로는 차단하지 않는다. [함수 목록](compat-release.md#file-src-nativecompatibility-cs) |
| [src/NativeReadGuard.cs](../../src/NativeReadGuard.cs) | 네이티브 포인터와 SeString 읽기의 범위/페이지 검사. 호환 계약 통과와 포인터 유효성 검사는 별개다. [함수 목록](compat-release.md#file-src-nativereadguard-cs) |
| [src/ObjectiveTraceBuffer.cs](../../src/ObjectiveTraceBuffer.cs) | lock으로 보호하는 최대1024줄/120초 메모리 기록. 중단은 기록을 보존하고 새 시작은 이전 기록을 교체한다. [함수 목록](compat-release.md#file-src-objectivetracebuffer-cs) |
| [src/PartyReader.cs](../../src/PartyReader.cs) | 현재 파티의 관찰용 스냅샷. 일반 파티 목록, 서버 초월의 로컬 그룹, 본인 정보 순으로 읽는다. 모집 등록 직업/빈자리 규칙의 기준으로 사용하면 안 된다. [함수 목록](core.md#file-src-partyreader-cs) |
| [src/PartyRoster.cs](../../src/PartyRoster.cs) | AFK 비교 전용 불변 CID 명단. 직업·이름·레벨·정렬 순서·그룹 내 위치는 비교에 넣지 않는다. [함수 목록](core.md#file-src-partyroster-cs) |
| [src/PartyRosterReader.cs](../../src/PartyRosterReader.cs) | AFK가 켜진 동안만 현재 전체 파티 CID를 읽는다. 기존 PartyReader의 실시간 직업 표시와 별개이며 서버 초월은 최대6개 그룹 전체를 순회한다. [함수 목록](core.md#file-src-partyrosterreader-cs) |
| [src/PartyViewKR.csproj](../../src/PartyViewKR.csproj) | net10.0-windows/x64/unsafe, 버전0.3.55/0.3.55.0, Discord.Net3.20.2·DPAPI 의존성 및 Web/PNG/contracts embedded resource, native references 지정. |
| [src/PartyViewKR.json](../../src/PartyViewKR.json) | 현재 플러그인 manifest/API15/AssemblyVersion0.3.55.0. package 검증의 기준이며 csproj와 반드시 맞아야 한다. |
| [src/PartyWindow.cs](../../src/PartyWindow.cs) | 인게임 ImGui 화면. 현재 파티/모집글/외부 연결/수정 초안/대기 변경/진단을 표시하고 Plugin에 의도만 전달한다. [함수 목록](core.md#file-src-partywindow-cs) |
| [src/Plugin.Afk.cs](../../src/Plugin.Afk.cs) | 인게임 AFK 토글과 순수 monitor를 실제 Reader/기존 조회 guard에 연결한다. 별도의 지역 이동 상태 기계나 자동 갱신/알림 전송 경로를 만들지 않는다. [함수 목록](core.md#file-src-plugin-afk-cs) |
| [src/Plugin.Chat.cs](../../src/Plugin.Chat.cs) | 공개 채팅 이벤트와 Discord 큐를 로그인/CID 기반으로 연결한다. 인게임의 종류별 수신/멘션 토글과 홈 서버 보완을 제공하며 AFK 상태에 의존하지 않는다. [함수 목록](core.md#file-src-plugin-chat-cs) |
| [src/Plugin.ChatSend.cs](../../src/Plugin.ChatSend.cs) | 게임 전송 native 경계. 호환/접속/지역 이동과 실제 최근 귓속말 이력을 검사하고 UIModule 채팅 입력을 호출한다. [함수 목록](core.md#file-src-plugin-chatsend-cs) |
| [src/Plugin.Discord.cs](../../src/Plugin.Discord.cs) | 사용자 설정과 Discord client 수명 연결. 재시작 복원은 한 번, 명시 끄기는 자동 연결도 끄며 Dispose는 저장 토큰을 지우지 않는다. [함수 목록](discord.md#file-src-plugin-discord-cs) |
| [src/Plugin.Recovery.cs](../../src/Plugin.Recovery.cs) | 디스코드 닫기 queue를 실제 게임 창에 연결한다. 반복 중단/제출 기능이 아니라 사용자가 명시적으로 요청한 모집 수정창 닫기만 수행한다. [함수 목록](refresh.md#file-src-plugin-recovery-cs) |
| [src/Plugin.Relay.cs](../../src/Plugin.Relay.cs) | PVKR 설정/UI와 중계 client 생성·종료를 연결한다. 서버 주소만 영구 저장하고 연결 키는 런타임에 사용한다. [함수 목록](web-relay.md#file-src-plugin-relay-cs) |
| [src/Plugin.Web.cs](../../src/Plugin.Web.cs) | Plugin에서 loopback/relay/Discord 중 하나의 Bridge를 Framework에 연결하는 façade와 작업 소유권 어댑터. [함수 목록](web-relay.md#file-src-plugin-web-cs) |
| [src/Plugin.cs](../../src/Plugin.cs) | Dalamud 진입점. 서비스 주입, UI·명령·Addon 수명 이벤트 등록, 500ms 읽기/갱신 진행, 관리형 편집 초안과 대기 변경을 소유한다. 게임 상태 접근의 중심은 OnUpdate다. [함수 목록](core.md#file-src-plugin-cs) |
| [src/RelayConfiguration.cs](../../src/RelayConfiguration.cs) | 기존 설정 type identity를 유지하는 Version=1 설정 모델. 서버 주소, DPAPI ciphertext, 토큰 기억/자동 연결만 저장한다. PIN/모집글/relay 연결 키는 저장하지 않는다. [함수 목록](discord.md#file-src-relayconfiguration-cs) |
| [src/RuntimeRows.cs](../../src/RuntimeRows.cs) | 한섭/공개판 Lumina 어셈블리 차이를 피하도록 런타임 RowRef와 ExcelSheet 공개 API를 reflection으로 읽는다. 이름 캐시와 모집 직업 카탈로그를 제공한다. [함수 목록](core.md#file-src-runtimerows-cs) |
| [src/Web/app.js](../../src/Web/app.js) | 웹 화면 상태/폴링·조회·수정/갱신 요청. GET state 폴링은 서버 모집글 조회를 매번 발생시키지 않는다. [함수 목록](web-relay.md#file-src-web-app-js) |
| [src/Web/connection.js](../../src/Web/connection.js) | 연결 링크/room.key 파싱, sessionStorage와 URL fragment 정리, 불명확한 요청 결과 분류. 브라우저와 Node 테스트가 같은 순수 로직을 사용한다. [함수 목록](web-relay.md#file-src-web-connection-js) |
| [src/Web/editor.js](../../src/Web/editor.js) | 외부 편집 초안 검증과 DOM editor. 입력 자체는 게임에 반영하지 않고 collect한 변경을 submit callback으로 넘긴다. [함수 목록](web-relay.md#file-src-web-editor-js) |
| [src/Web/index.html](../../src/Web/index.html) | 웹 조회/연결/editor/결과 영역과 DOM ID·외부 script 로드 순서. app/editor가 의존하는 ID 계약. |
| [src/Web/style.css](../../src/Web/style.css) | 모바일/desktop의 조회·그룹·자리·편집·상태/오류 표시 스타일. |
| [src/packages.lock.json](../../src/packages.lock.json) | 플러그인의 NuGet 전이 의존성 버전/해시 lock. |
| [tests/AfkCompletionTests.cs](../../tests/AfkCompletionTests.cs) | 모집 자동 종료 뒤 현재 명단과 기억한 등록 정원으로 완료 감지·중복 억제를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-afkcompletiontests-cs) |
| [tests/AfkListingMonitorTests.cs](../../tests/AfkListingMonitorTests.cs) | AFK CID 명단 정규화, 가입/탈퇴 변경 보존,60초 보완 조회, 실패 backoff, 요청 revision 소유권과 캐릭터 수명를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-afklistingmonitortests-cs) |
| [tests/AfkSettingsTests.cs](../../tests/AfkSettingsTests.cs) | AFK 주기·재시도 선택/경계·설정 저장를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-afksettingstests-cs) |
| [tests/CompatibilityTests.cs](../../tests/CompatibilityTests.cs) | 네이티브/interop ABI 계약 변화, MVID/버전 독립성 및 fail-closed 비교를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-compatibilitytests-cs) |
| [tests/DalamudConfigurationStub.cs](../../tests/DalamudConfigurationStub.cs) | 테스트 전용 IPluginConfiguration 최소 interface stub. RelayConfiguration을 실제 Dalamud DLL 없이 빌드하도록 Version 속성 계약만 제공한다. 실행 함수는 없다. |
| [tests/DiscordSettingsTests.cs](../../tests/DiscordSettingsTests.cs) | 저장 토큰 암복호화 어댑터, 재시작 설정 복원, 기억 해제와 오류 시 설정 보존를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-discordsettingstests-cs) |
| [tests/GameChatCaptureTests.cs](../../tests/GameChatCaptureTests.cs) | Dalamud 공개 채팅 이벤트의 두 delegate 형태·읽기 전용 수신·필터·서버 추출를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-gamechatcapturetests-cs) |
| [tests/GameChatSendingTests.cs](../../tests/GameChatSendingTests.cs) | 전송 채널·UTF8·대상 고정·세대/취소·ACK·재실행 방지를 fake host로 검사한다. [함수 목록](tests.md#file-tests-gamechatsendingtests-cs) |
| [tests/ListingBatchTests.cs](../../tests/ListingBatchTests.cs) | 다중 변경 순차 실행, unavailable 건너뛰기, 누적 draft, 1회 제출·원본/멤버 보존를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingbatchtests-cs) |
| [tests/ListingBeginnerEditTests.cs](../../tests/ListingBeginnerEditTests.cs) | 초보자 환영 typed edit·checkbox UI·단일 갱신 결과 검증를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingbeginneredittests-cs) |
| [tests/ListingCompletionEditTests.cs](../../tests/ListingCompletionEditTests.cs) | 완료조건 1/2/4 mapping, 체크/목록 선택과 disabled/지연·실패 처리를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingcompletionedittests-cs) |
| [tests/ListingDescriptionEditTests.cs](../../tests/ListingDescriptionEditTests.cs) | 소개말 정규화/UTF-8 제한·변경 검증·submit payload를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdescriptionedittests-cs) |
| [tests/ListingDescriptionRecoveryTests.cs](../../tests/ListingDescriptionRecoveryTests.cs) | 소개말 화면만 부분 반영된 실패의 복구와 다른 배치 항목 진행를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdescriptionrecoverytests-cs) |
| [tests/ListingDiscordCardTests.cs](../../tests/ListingDiscordCardTests.cs) | 조회/수정/직업 상세/결과 카드를 독립 화면으로 유지하는 routing를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordcardtests-cs) |
| [tests/ListingDiscordChatSdkTests.cs](../../tests/ListingDiscordChatSdkTests.cs) | 실제 SDK 채팅 slash 등록/파싱·일반 임베드 POST·색상/멘션 직렬화를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordchatsdktests-cs) |
| [tests/ListingDiscordChatSendSdkTests.cs](../../tests/ListingDiscordChatSendSdkTests.cs) | 실제 Discord.Net의4종 슬래시→Label/TextInput 모달→제출→ephemeral ACK→Framework 전송 경로를 fake Gateway/REST로 검증한다. [함수 목록](tests.md#file-tests-listingdiscordchatsendsdktests-cs) |
| [tests/ListingDiscordChatTests.cs](../../tests/ListingDiscordChatTests.cs) | 로그인 기반 상시 채팅·종류별 수신/멘션 토글·동시 설정 저장·권한·취소·무손실 전송를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordchattests-cs) |
| [tests/ListingDiscordConnectionTests.cs](../../tests/ListingDiscordConnectionTests.cs) | 실제 Discord.Net SDK의 fake Gateway/REST 연결 및 비동기 종료/ACK 처리를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordconnectiontests-cs) |
| [tests/ListingDiscordEmojiTests.cs](../../tests/ListingDiscordEmojiTests.cs) | 80개 asset·이름·재사용/legacy rename/upload·slot 제한 및 join 이미지 선택를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordemojitests-cs) |
| [tests/ListingDiscordInteractionTests.cs](../../tests/ListingDiscordInteractionTests.cs) | 실제 SDK interaction JSON→모달/버튼/select→공통 Bridge의 end-to-end 경로를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordinteractiontests-cs) |
| [tests/ListingDiscordNotificationSdkTests.cs](../../tests/ListingDiscordNotificationSdkTests.cs) | 실제 SDK의 서버 권한과 everyone POST 직렬화를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordnotificationsdktests-cs) |
| [tests/ListingDiscordNotificationTests.cs](../../tests/ListingDiscordNotificationTests.cs) | 알림 채널 저장·소유자/ACK·everyone 전송·취소·오류 중복 방지를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordnotificationtests-cs) |
| [tests/ListingDiscordResultTests.cs](../../tests/ListingDiscordResultTests.cs) | 긴 결과 페이지의 무손실 분할, escape/Unicode 경계·읽기 전용 탐색를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordresulttests-cs) |
| [tests/ListingDiscordRosterTests.cs](../../tests/ListingDiscordRosterTests.cs) | 24/48인/친선 전체 그룹 아이콘과 설정/상대시간 표시를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordrostertests-cs) |
| [tests/ListingDiscordTests.cs](../../tests/ListingDiscordTests.cs) | owner/nonce/session·폼 유효성·직업 분류·중복 요청·ACK 선행를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingdiscordtests-cs) |
| [tests/ListingEditorCriteriaTests.cs](../../tests/ListingEditorCriteriaTests.cs) | 친선 padding에서만 raw mask 차이를 허용하고 실제 미등록/멤버 변경은 차단를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingeditorcriteriatests-cs) |
| [tests/ListingFriendlyMatchTests.cs](../../tests/ListingFriendlyMatchTests.cs) | 5+5+8 친선 등록 자리/물리 배열·편집 경로 및 미등록 축소를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingfriendlymatchtests-cs) |
| [tests/ListingItemLevelEditTests.cs](../../tests/ListingItemLevelEditTests.cs) | 평균 레벨0/1~999·토글/숫자 UI 순서·지연/유효 범위를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingitemleveledittests-cs) |
| [tests/ListingJobEditTests.cs](../../tests/ListingJobEditTests.cs) | 직업 변경 원본/빈자리·known mask·raw mask 보존 및 최종 결과를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingjobedittests-cs) |
| [tests/ListingJobRulesTests.cs](../../tests/ListingJobRulesTests.cs) | 직업 카탈로그 완전성·bit 매핑·순서·누구나 가능 판정를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingjobrulestests-cs) |
| [tests/ListingObjectiveEditTests.cs](../../tests/ListingObjectiveEditTests.cs) | 목적 index/bit·원본 조건 보존·결과 검증를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingobjectiveedittests-cs) |
| [tests/ListingObjectiveUiTests.cs](../../tests/ListingObjectiveUiTests.cs) | dropdown open→click→select 순서와1회 동작·timeout를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingobjectiveuitests-cs) |
| [tests/ListingPrivateEditTests.cs](../../tests/ListingPrivateEditTests.cs) | 공개 sentinel10000·0000 포함 PIN·체크/입력 순서·비밀값 비노출를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingprivateedittests-cs) |
| [tests/ListingRecoveryTests.cs](../../tests/ListingRecoveryTests.cs) | 수정창 닫기 우선 처리·ACK·세션·창 수명·중단 소유권를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingrecoverytests-cs) |
| [tests/ListingRelayTests.cs](../../tests/ListingRelayTests.cs) | 중계 URL/key/frame allowlist·session/만료·JSON/크기·Bridge 응답를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingrelaytests-cs) |
| [tests/ListingRoleButtonsTests.cs](../../tests/ListingRoleButtonsTests.cs) | KR 직업 node mapping·Anyone 계획·친선 차이 토글·batch pacing를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingrolebuttonstests-cs) |
| [tests/ListingRoleComparisonTests.cs](../../tests/ListingRoleComparisonTests.cs) | raw flags 차이/자리 위치·추가/제거 직업 진단를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingrolecomparisontests-cs) |
| [tests/ListingRoleNormalizationTests.cs](../../tests/ListingRoleNormalizationTests.cs) | 점유 자리의 입장 직업 자동 정규화와 일반·연합·친선의 원본/수정창/결과 검증를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingrolenormalizationtests-cs) |
| [tests/ListingRuleEditTests.cs](../../tests/ListingRuleEditTests.cs) | finder bit/loot mapping·컨트롤 상태 기계·다른 조건 보존를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingruleedittests-cs) |
| [tests/ListingSlotRouteTests.cs](../../tests/ListingSlotRouteTests.cs) | 일반/24/48/친선 버튼 node/param과 수정창 형태를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingslotroutetests-cs) |
| [tests/ListingSlotsTests.cs](../../tests/ListingSlotsTests.cs) | 참가 직업/허용 직업·소유 물리 인덱스·인원수 일치를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingslotstests-cs) |
| [tests/ListingSparseSlotsTests.cs](../../tests/ListingSparseSlotsTests.cs) | 24/48인 미등록 제외·display/native 위치 보존·편집 결과를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingsparseslotstests-cs) |
| [tests/ListingUniqueJobsEditTests.cs](../../tests/ListingUniqueJobsEditTests.cs) | 잡 중복 없음 flag·원본/결과·checkbox 지연를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listinguniquejobsedittests-cs) |
| [tests/ListingWebEditTests.cs](../../tests/ListingWebEditTests.cs) | apply decoder·revision/ID ledger·타임아웃·실제 HTTP의 엄격한 body/auth 경계를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingwebedittests-cs) |
| [tests/ListingWebTests.cs](../../tests/ListingWebTests.cs) | snapshot 최소 노출·query queue·Host/Origin/Bearer·HTTP 종료와 소유권를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-listingwebtests-cs) |
| [tests/NativePointerContractTests.cs](../../tests/NativePointerContractTests.cs) | 포인터 전용 UIModule의 실제 PE 구조 변경·호출 필드/함수 보존·직접 접근 구조 검사를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-nativepointercontracttests-cs) |
| [tests/Program.cs](../../tests/Program.cs) | 전체 관리형 test runner 및 기본 갱신/소유자/빈 소개말/trace buffer 회귀를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-program-cs) |
| [tests/RefreshTests.csproj](../../tests/RefreshTests.csproj) | 게임 없이 실행할 net10.0 console test runner. 순수 모델/Bridge/Discord SDK 코드를 링크하고 PNG/Web 리소스를 포함한다. Dalamud/native UI host는 fake 경계로 대체한다. |
| [tests/RelayFixture.cs](../../tests/RelayFixture.cs) | 로컬 Worker와 붙일 살아 있는 fake host/Bridge 중계 통합 fixture를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-relayfixture-cs) |
| [tests/WebBrowserFixture.cs](../../tests/WebBrowserFixture.cs) | 실제 loopback server에 fake source를 제공하는 브라우저 smoke fixture를 검사한다. 아래 시나리오 이름은 소스의 실제 test 등록문에서 추출했다. [함수 목록](tests.md#file-tests-webbrowserfixture-cs) |
| [tests/browser-smoke.cjs](../../tests/browser-smoke.cjs) | Playwright와 WebBrowserFixture의 실제 loopback UI로 조회·slot 표시·편집/결과를 검사한다. [함수 목록](tests.md#file-tests-browser-smoke-cjs) |
| [tests/packages.lock.json](../../tests/packages.lock.json) | 관리형 회귀 runner의 NuGet lock. |
| [tests/test_release.py](../../tests/test_release.py) | unittest/mock으로 배포 URL·버전·의존성 allowlist·해시·공개 다운로드 실패 시 feed 보존을 검사한다. 실제 릴리스를 만들지 않는다. [함수 목록](tests.md#file-tests-test-release-py) |
| [tests/web-connection.test.cjs](../../tests/web-connection.test.cjs) | 연결 링크/직접 코드·cross-origin 거부·hash 제거·sessionStorage 실패와 미결 요청의 확정 실패 분류를 검사한다. [함수 목록](tests.md#file-tests-web-connection-test-cjs) |
| [tests/web-editor.test.cjs](../../tests/web-editor.test.cjs) | 브라우저와 같은 collect/draft 순수 로직의 no-op·필드 검증·PIN·직업 선택·아이템 레벨 경계를 검사한다. [함수 목록](tests.md#file-tests-web-editor-test-cjs) |
| [tools/CompatibilityTool/CompatibilityTool.csproj](../../tools/CompatibilityTool/CompatibilityTool.csproj) | net10.0 CLI 프로젝트. shared/NativeContract.cs를 링크해 runtime과 같은 ABI 비교 로직을 쓴다. |
| [tools/CompatibilityTool/Program.cs](../../tools/CompatibilityTool/Program.cs) | PE metadata CLI: inspect는 assembly 이름/버전, capture/capture-runtime은 계약 baseline 생성, verify는 현재 DLL과 저장 계약을 비교한다. [함수 목록](compat-release.md#file-tools-compatibilitytool-program-cs) |
| [tools/release.py](../../tools/release.py) | 고정 reference 다운로드→ABI 확인→버전/런타임 의존성 검증 ZIP→release 검증 후 feed 업데이트. 명시 실행하는 배포 도구다. [함수 목록](compat-release.md#file-tools-release-py) |

## 문서 파일

| 경로 | 역할 |
|---|---|
| [docs/CODE_MAP.md](../CODE_MAP.md) | 진입점·호출 흐름·상태/값 계약·증상별 탐색·검증/갱신 지침 |
| [docs/code-map/core.md](core.md) | 플러그인 수명·조회·자리 매핑: 해당 파일의 함수/속성/생성자별 책임과 소스 위치 |
| [docs/code-map/edits.md](edits.md) | 편집 모델·대기 변경·순수 UI 상태 기계: 해당 파일의 함수/속성/생성자별 책임과 소스 위치 |
| [docs/code-map/refresh.md](refresh.md) | 갱신 흐름·네이티브 UI 호스트: 해당 파일의 함수/속성/생성자별 책임과 소스 위치 |
| [docs/code-map/discord.md](discord.md) | Discord 연결·카드·폼·이모지·토큰·채팅: 해당 파일의 함수/속성/생성자별 책임과 소스 위치 |
| [docs/code-map/web-relay.md](web-relay.md) | 공통 외부 브리지·웹·Cloudflare 중계: 해당 파일의 함수/속성/생성자별 책임과 소스 위치 |
| [docs/code-map/compat-release.md](compat-release.md) | 호환 검사·진단·배포 도구: 해당 파일의 함수/속성/생성자별 책임과 소스 위치 |
| [docs/code-map/tests.md](tests.md) | 테스트 함수·시나리오 지도: 해당 파일의 함수/속성/생성자별 책임과 소스 위치 |
| [docs/code-map/files.md](files.md) | 추적된 전체 코드·설정·자산·배포 파일의 책임 |
