# PVKR 외부 접속 서버

개인용 Cloudflare Worker 한 개에 게임 PC 한 대를 연결합니다. 휴대폰에서는 HTTPS 웹사이트만 사용합니다. 게임 PC의 포트를 공개하거나 휴대폰에 VPN을 설치할 필요가 없습니다. 게임과 PVKR은 실행 중이어야 합니다.

`0.3.33.0`에서 **조회·현재 조건 갱신·일괄 수정**을 중계합니다. 웹 푸시 알림은 아직 포함하지 않습니다. 같은 PC 연결과 외부 연결 중 하나를 켤 수 있으며, 외부 연결은 실행할 때마다 직접 켭니다.

## 1. Cloudflare에 배포

Cloudflare 대시보드의 **Workers & Pages**에서 GitHub 저장소를 연결하여 **Worker**를 만듭니다. 정적 Pages 프로젝트가 아닙니다.

| 설정 | 값 |
| --- | --- |
| 저장소 | `pressing7/PartyViewKR` |
| 브랜치 | `main` |
| Worker 이름 | `pvkr-relay` |
| Root directory / 루트 디렉터리 | `relay` |
| Build command / 빌드 명령 | `npm run build` |
| Deploy command / 배포 명령 | `npx wrangler deploy` |

의존성은 `package-lock.json` 기준으로 설치됩니다. Node 버전을 별도로 묻는다면 22 이상을 사용합니다. `wrangler.jsonc`가 `ROOM` Durable Object 바인딩과 SQLite 마이그레이션을 설정합니다. 별도 데이터베이스를 수동으로 만들 필요가 없습니다. 웹 화면은 `../src/Web`에서 가져오므로 저장소 전체가 체크아웃되어야 합니다.

배포 후 표시되는 `https://pvkr-relay.<내 계정>.workers.dev` 주소를 사용합니다. 유료 플랜이나 도메인을 먼저 구매할 필요는 없습니다. 무료 사용량 한도를 넘으면 연결이 제한될 수 있습니다.

**Workers Builds는 GitHub Actions와 별개입니다.** Git 연결 시 이후 커밋으로 Cloudflare 배포가 자동 실행될 수 있습니다. 직접 배포 시점을 정하려면 Worker의 Settings → Build에서 자동 빌드를 끄고 필요할 때 배포합니다. 기존 플러그인 Actions 빌드·릴리스는 사용자가 직접 실행합니다.

## 2. 서버 연결 키 등록

1. Actions에서 플러그인 `0.3.33.0`을 빌드·배포하고 게임에서 업데이트합니다.
2. 게임의 `/pvkr` → **외부 접속 · 휴대폰 브라우저**를 펼칩니다.
3. **새 서버 연결 키 만들기 → 서버 연결 키 복사**를 누릅니다.
4. Cloudflare의 해당 Worker → **Settings → Variables and Secrets**에 다음 값을 추가합니다. Build variables가 아니라 **실행 환경의 Secret**입니다.

   - Type: **Secret**
   - Name: `PVKR_CONNECT_KEY`
   - Value: 방금 복사한 64자리 키

5. 저장/배포한 뒤, PVKR에 Worker의 HTTPS 주소를 입력하고 **외부 연결 켜기**를 누릅니다. 키가 PVKR 입력란에 그대로 있다면 다시 붙여넣을 필요가 없습니다.

서버 연결 키는 이 서버에 게임 PC를 등록하는 비밀 값입니다. 저장소, 이슈, 채팅에 올리지 않습니다. 플러그인은 서버 주소만 저장하며 키는 파일에 저장하지 않습니다. 다음 게임 실행에서도 같은 키를 입력하려면 개인 비밀번호 관리자 등에 보관하세요. 키를 잃었으면 새로 만들고 Cloudflare의 Secret도 바꾸면 됩니다.

## 3. 휴대폰에서 연결

1. PVKR에 **외부 연결됨**이 표시되면 **외부 접속 링크 복사**를 누릅니다.
2. 본인 휴대폰의 Chrome에서 링크를 엽니다. 또는 Worker 웹사이트를 먼저 열고 **연결 코드 복사**로 얻은 값을 붙여넣습니다.
3. **내 모집글 조회** 후 **내 모집글 갱신** 또는 **모집 내용 수정**을 사용합니다.
4. 집 밖 접속 확인은 휴대폰 와이파이를 끄고 모바일 데이터로 검사합니다.

휴대폰 연결 코드는 `연결 식별자.접속 키` 형태의 긴 값입니다. 서버 연결 키와 다르며, 알고 있는 사람은 현재 모집글을 조작할 수 있으므로 본인 기기에서만 사용하세요. 링크의 키는 `#` 뒤에 넣어 서버 URL로 보내지 않고, 읽은 뒤 주소창에서 지웁니다. 브라우저 탭의 세션 저장소에 연결 정보만 보관합니다. 모집 비밀번호와 편집 중인 입력은 저장하지 않습니다.

PVKR에서 외부 연결을 껐다 켜거나 플러그인을 재시작하면 **새 링크/연결 코드**가 필요합니다. 인터넷이 잠깐 끊겼을 때의 자동 재접속은 같은 연결 정보를 유지합니다. 휴대폰의 **이 브라우저 연결 지우기**는 해당 탭의 연결 정보를 지웁니다. 전체 접속을 끊으려면 게임에서 **외부 연결 끄기**를 사용합니다.

## 처리 방식과 제한

- 브라우저의 허용된 조회·수정 요청만 중계합니다. 게임 메모리 주소나 임의 콜백, 임의 URL은 전달할 수 없습니다.
- 실제 작업은 기존 `ListingWebBridge`와 Framework 스레드에서 처리합니다. 참가자·모집글·캐릭터가 바뀌면 기존 검증으로 중지하며, 콘텐츠가 지원하지 않는 설정은 기존 일괄 수정 방식대로 건너뜁니다.
- 중계 서버에 모집글·모집 비밀번호·명령 큐를 영구 저장하지 않습니다. 요청이 도착한 순간 게임 PC가 연결되어 있어야 합니다.
- 이미 접수한 작업은 일시적인 네트워크 단절 중에도 게임에서 계속 진행될 수 있습니다. 서버가 자동 재전송하지 않으며, 재접속 뒤 원래 요청 번호로 결과만 확인합니다.
- 플러그인 연결을 직접 끄면 대기 중인 요청과 해당 웹 연결이 시작한 진행 작업을 중단합니다. 이미 게임에 제출한 갱신은 되돌리지 않습니다.
- 전송 대기 시간이 지난 요청은 실행하지 않습니다. 게임 PC의 시간을 자동 설정으로 맞춰 주세요.
- 같은 서버 연결 키로 다른 PC가 접속하면 이전 PC 연결을 교체합니다. 첫 버전은 **한 서버·한 게임 PC** 용도입니다.
- HTTPS/WSS는 전송 구간을 보호합니다. 자체 중계 서버는 요청 본문을 처리하므로 브라우저와 플러그인 사이의 종단 간 암호화 방식은 아닙니다.

## 개발·검증

```sh
cd relay
npm ci
npm run build
```

로컬 테스트용 `.dev.vars`에 임의의 테스트 키와 `LOCAL_TEST_ONLY="true"`를 넣고 `npm run dev`로 실행할 수 있습니다. `LOCAL_TEST_ONLY`는 운영 서버에 설정하지 않습니다. 일반 플러그인 UI는 HTTP 주소를 허용하지 않습니다.

C# 테스트 프로젝트를 빌드한 뒤 `PVKR_TEST_DOTNET`에 .NET 10 실행 파일 경로를 지정하고 `npm test`를 실행합니다. 이 검사는 실제 workerd와 .NET WebSocket을 연결하고 가짜 게임 호스트로 친선경기 자리 수정, 비밀번호 `0000`, 일부 미반영, 중복 요청 및 Worker 재시작을 확인합니다. 인게임·실제 Cloudflare 계정·휴대폰 통신망 검증은 별도로 필요합니다.

로컬 터미널로 배포하려면 `npx wrangler login`, `npm run deploy`, `npx wrangler secret put PVKR_CONNECT_KEY`를 사용합니다. 실제 배포는 사용자가 선택해서 실행합니다.

공식 문서: [Workers Builds 설정](https://developers.cloudflare.com/workers/ci-cd/builds/configuration/), [Durable Objects WebSocket](https://developers.cloudflare.com/durable-objects/best-practices/websockets/), [무료 플랜 한도](https://developers.cloudflare.com/durable-objects/platform/pricing/).
