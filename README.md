# PartyViewKR

한섭 파티원·모집글 조회 및 **내 모집글 수동 갱신** 플러그인입니다. `/pvkr`로 엽니다.
모집 내용을 수정하는 기능은 아직 포함하지 않습니다.

## 설치와 업데이트

Dalamud 설정 → 실험적 기능 → 사용자 지정 플러그인 저장소에 다음 주소를 추가합니다.

```
https://raw.githubusercontent.com/pressing7/PartyViewKR/main/pluginmaster.json
```

플러그인 설치기에서 **Party View KR**을 설치하거나 업데이트합니다.
배포 파일은 [Releases](https://github.com/pressing7/PartyViewKR/releases)의 `PartyViewKR.zip`입니다.
저장소 루트에 남아 있는 이전 수동 업로드 DLL/JSON은 최신 배포 파일이 아닙니다.

## 0.3.3의 빈 모집 문구 처리

모집 내용을 비웠을 때 화면의 안내 문구와 실제 빈 내용을 비교해서 본인 확인이 막히던 처리를 수정했습니다.
모집 정보와 상세 창의 별도 문자열 버퍼가 모두 빈 값으로 읽히면, 조회·수정 창 확인·갱신 후 비교에 빈 문자열을 사용합니다.
내용을 읽지 못한 경우나 일반 모집 문구가 서로 다른 경우, 작성자 ID·이름이 다른 경우는 계속 차단합니다.
진단에 `uiDescriptionBufferReadable`, `uiDescriptionBufferChars`, `emptyDescriptionPlaceholder`를 추가했습니다.

## 0.3.2의 호환성 검사

기존에는 `FFXIVClientStructs.dll`의 빌드 식별자(MVID)가 다르면 네이티브 기능을 모두 중지했습니다.
이제 최초 조회 시 로드된 DLL의 메타데이터를 한 번 읽어, 실제 사용하는 구조체의 크기·필드 위치·형식,
인라인 배열 길이, 호출 함수 시그니처, 가상 함수 슬롯 및 필요한 열거형 값을 비교합니다.
문자열 포인터를 담는 `InteropGenerator.Runtime.CStringPointer`도 확인합니다.

- 필요한 구조가 같으면 MVID/세부 버전이 달라도 조회와 수동 갱신을 허용합니다.
- API가 바뀌거나 필요한 구조가 다르거나 검사를 완료하지 못하면 네이티브 기능을 중지합니다.
- 런타임에는 인터넷 접속, DLL 다운로드/교체, 자동 재등록을 하지 않습니다.
- 진단의 `nativeCheck=structure_match`가 검사 통과입니다. MVID는 참고 정보로 남깁니다.

이 검사는 플러그인과 로드된 라이브러리 사이의 구조 일치를 확인합니다.
게임 자체의 메모리 배치나 함수 동작까지 증명하는 검사는 아닙니다. 큰 게임 패치 후에는 인게임 확인이 필요합니다.
기존 메모리 접근 검사, 본인 모집글 확인, 갱신 버튼 문구 확인 및 중복 실행 방지는 유지합니다.

## 자동 빌드·배포

`Build and release` 워크플로가 다음을 수행합니다.

1. `compatibility/reference-lock.json`에 고정한 공개 참조 ZIP을 받아 SHA-256을 검사합니다.
2. Dalamud API 및 한섭에서 확인한 구조 기준을 검사합니다. 기준 JSON을 자동으로 다시 만들지는 않습니다.
3. 회귀 테스트와 빌드를 실행하고 DLL·매니페스트·태그 버전을 비교합니다.
4. `main`에 새 버전이 들어오거나 버전 태그가 올라오면 릴리스 초안을 만들고 `PartyViewKR.zip`을 업로드합니다.
5. 업로드 해시와 공개 다운로드를 확인한 후 `pluginmaster.json`만 갱신합니다.

PR에서는 검사와 설치 ZIP 생성까지만 실행합니다. 같은 버전의 기존 태그/파일은 덮어쓰지 않습니다.
새 버전을 배포할 때 아래 세 값을 함께 올린 뒤 `main`에 반영합니다.

- `src/PartyViewKR.csproj`: `Version`, `AssemblyVersion`, `FileVersion`
- `src/PartyViewKR.json`: `AssemblyVersion`
- 예: `Version=0.3.3`, 나머지 버전은 `0.3.3.0`

태그는 `0.3.3`과 `v0.3.3` 모두 지원하며 실제 태그 이름으로 다운로드 URL을 만듭니다.
Actions → Build and release → Run workflow에서 `publish`를 선택하면 실패한 배포를 재시도할 수도 있습니다.
새 버전 태그를 고의로 이동하거나 공개된 ZIP을 교체하지 않습니다.
기본 브랜치 보호 규칙이 Actions의 JSON 갱신을 막는 경우, 해당 규칙에 맞는 반영 절차가 필요합니다.

## 로컬 빌드

.NET SDK 10 및 Python 3이 필요합니다. 저장소 루트에서 실행합니다.

```sh
python tools/release.py references
python tools/release.py verify
dotnet run --project tests/RefreshTests.csproj -c Release
python -m unittest discover -s tests -p 'test_*.py' -v
dotnet build src/PartyViewKR.csproj -c Release -p:DalamudLibPath="<저장소 절대경로>/.build/references"
python tools/release.py package
```

결과는 `release-output/PartyViewKR.zip`입니다. Dalamud 참조 DLL은 설치 ZIP에 포함하지 않습니다.
직접 받은 한섭 DLL 폴더를 쓰려면 `verify --refs <폴더>`와 `-p:DalamudLibPath=<폴더>`를 사용합니다.

## 다음 Dalamud 패치 대응

단순 빌드 변경이며 필요한 구조가 같으면 플러그인을 다시 배포하지 않아도 됩니다.
호환 검사에서 중지되면 `nativeDifferences`와 새 DLL을 비교하고, 실제 코드/게임 동작을 검증한 뒤 기준을 갱신합니다.
`CompatibilityTool capture`는 검토를 위한 개발 명령이며, CI가 자동 호출하면 안 됩니다.

빌드 참조를 바꿀 때는 고정 URL과 SHA-256을 함께 갱신하고 기존 기준으로 `verify`를 먼저 실행합니다.
검사 실패를 해결하려고 현재 실행 DLL을 새 기준으로 무조건 승인하지 마세요.
