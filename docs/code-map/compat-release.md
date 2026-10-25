# 호환 검사·진단·배포 도구

[전체 지도](../CODE_MAP.md) · [전체 파일 목록](files.md)

호환성은 build MVID의 동일성 대신 필요한 ABI 구조/메서드 계약으로 판정한다. metadata 검사는 DLL을 실행하지 않는다. 배포는 별도 명시 실행이며 docs 작업만으로 release/version을 바꾸지 않는다.

기준: 0.3.48.0 소스. 기반 main `1ef1c969e8dea0d58babf71a80698880b8fcc0dc` 이후 AFK 감시 변경을 포함한다. 2026-10-25 정리. 소스 링크의 행 번호는 이 기준에 해당한다.

표에는 직접 선언한 메서드·생성자·로컬 함수·계산 속성·인터페이스 메서드를 각각 포함한다. 자동 생성 record 메서드/단순 auto-property accessor는 제외한다. 이름 없는 LINQ/타이머/콜백은 이를 소유한 함수 설명에 포함하며, 중요한 DOM 이벤트 handler와 명명된 테스트 시나리오는 별도로 표시한다.

## 파일 탐색

- [shared/NativeContract.cs](#file-shared-nativecontract-cs)
- [src/ListingObjectiveTrace.cs](#file-src-listingobjectivetrace-cs)
- [src/NativeCompatibility.cs](#file-src-nativecompatibility-cs)
- [src/NativeReadGuard.cs](#file-src-nativereadguard-cs)
- [src/ObjectiveTraceBuffer.cs](#file-src-objectivetracebuffer-cs)
- [tools/CompatibilityTool/Program.cs](#file-tools-compatibilitytool-program-cs)
- [tools/release.py](#file-tools-release-py)

<a id="file-shared-nativecontract-cs"></a>
## shared/NativeContract.cs

[소스 파일](../../shared/NativeContract.cs) · 명명된 항목/진입점 24개

실행/Assembly.Load 없이 PE metadata를 읽는 ABI 계약 도구. 플러그인 runtime 검사와 CompatibilityTool이 같은 코드를 공유한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-shared-nativecontract-cs-10-00d8477770"></a><code>NativeContract</code><br><code>NativeContract(int Schema, int ApiLevel, string AssemblyName, string BuildVersion, Guid BuildMvid, Dictionary&lt;string, string[]&gt; Types, SortedDictionary&lt;string, string&gt; Entries)</code> | 주 생성자/모델 | schema/API·assembly identity·요구 타입/메서드·정렬된 ABI entries를 보관한다. | [L10](../../shared/NativeContract.cs#L10) |
| <a id="symbol-shared-nativecontract-cs-14-8585bc42fc"></a><code>NativeContract.Parse</code><br><code>NativeContract Parse(Stream stream)</code> | 메서드 | JSON stream을 NativeContract로 역직렬화하고 비어 있으면 거부한다. | [L14](../../shared/NativeContract.cs#L14) |
| <a id="symbol-shared-nativecontract-cs-19-01105f2448"></a><code>NativeContract.Read</code><br><code>NativeContract Read(string path, Dictionary&lt;string, string[]&gt; types, int apiLevel)</code> | 메서드 | 선택 타입의 크기/pack/layout, field 순서·offset·상수·signature, 요구 메서드 및 ABI attribute를 수집한다. 참조되는 generated fixed arrays도 추적한다. | [L19](../../shared/NativeContract.cs#L19) |
| <a id="symbol-shared-nativecontract-cs-81-8326a5e1f6"></a><code>NativeContract.Differences</code><br><code>string[] Differences(NativeContract actual)</code> | 메서드 | schema·assembly·API와 요구 entries의 값/존재를 비교한다. 추가 항목은 허용하고 build version/MVID는 정보로만 다룬다. | [L81](../../shared/NativeContract.cs#L81) |
| <a id="symbol-shared-nativecontract-cs-95-04ee0e5ee9"></a><code>NativeContract.Attributes</code><br><code>string Attributes(MetadataReader md, MetadataNames names, CustomAttributeHandleCollection handles)</code> | 메서드 | Inherits/InlineArray/VirtualFunction/FixedBuffer 등 ABI 영향 attribute의 raw blob을 정렬한다. signature byte pattern은 비교 대상에서 제외한다. | [L95](../../shared/NativeContract.cs#L95) |
| <a id="symbol-shared-nativecontract-cs-115-4d892798e5"></a><code>MetadataNames</code><br><code>MetadataNames(MetadataReader reader)</code> | 주 생성자/모델 | PE MetadataReader를 받아 ISignatureTypeProvider의 모든 타입 표현을 안정된 문자열로 만든다. | [L115](../../shared/NativeContract.cs#L115) |
| <a id="symbol-shared-nativecontract-cs-117-c7889aed53"></a><code>MetadataNames.Definition</code><br><code>string Definition(TypeDefinitionHandle h)</code> | 메서드 | nested type의 부모 이름과 namespace를 포함한 정규 metadata 이름을 만든다. | [L117](../../shared/NativeContract.cs#L117) |
| <a id="symbol-shared-nativecontract-cs-123-d6194dd422"></a><code>MetadataNames.Reference</code><br><code>string Reference(TypeReferenceHandle h)</code> | 메서드 | TypeReference의 중첩 scope를 따라 정규 타입 이름을 만든다. | [L123](../../shared/NativeContract.cs#L123) |
| <a id="symbol-shared-nativecontract-cs-129-2ee45aba88"></a><code>MetadataNames.Join</code><br><code>string Join(string ns, string name)</code> | 메서드 | 비어 있지 않은 namespace와 타입 이름을 점으로 연결한다. | [L129](../../shared/NativeContract.cs#L129) |
| <a id="symbol-shared-nativecontract-cs-130-eb449c18b7"></a><code>MetadataNames.Entity</code><br><code>string Entity(EntityHandle h)</code> | 메서드 | definition/reference/specification handle을 알맞은 이름 해석기로 보낸다. | [L130](../../shared/NativeContract.cs#L130) |
| <a id="symbol-shared-nativecontract-cs-136-6de0a2d753"></a><code>MetadataNames.GetArrayType</code><br><code>string GetArrayType(string x, ArrayShape s)</code> | 메서드 | 배열 rank·크기·하한을 포함하는 비교용 type signature를 만든다. | [L136](../../shared/NativeContract.cs#L136) |
| <a id="symbol-shared-nativecontract-cs-137-5931eb9979"></a><code>MetadataNames.GetByReferenceType</code><br><code>string GetByReferenceType(string x)</code> | 메서드 | 타입에 &amp;를 붙여 by-reference signature를 만든다. | [L137](../../shared/NativeContract.cs#L137) |
| <a id="symbol-shared-nativecontract-cs-138-4b656dc84c"></a><code>MetadataNames.GetFunctionPointerType</code><br><code>string GetFunctionPointerType(MethodSignature&lt;string&gt; s)</code> | 메서드 | calling header·반환형·인자형을 포함한 함수 포인터 signature를 만든다. | [L138](../../shared/NativeContract.cs#L138) |
| <a id="symbol-shared-nativecontract-cs-139-616d9d37eb"></a><code>MetadataNames.GetGenericInstantiation</code><br><code>string GetGenericInstantiation(string x, ImmutableArray&lt;string&gt; args)</code> | 메서드 | generic 정의와 실제 타입 인자를 결합한다. | [L139](../../shared/NativeContract.cs#L139) |
| <a id="symbol-shared-nativecontract-cs-140-9d0fdafeb3"></a><code>MetadataNames.GetGenericMethodParameter</code><br><code>string GetGenericMethodParameter(object? context, int index)</code> | 메서드 | 메서드 generic 인자를 !!index로 표시한다. | [L140](../../shared/NativeContract.cs#L140) |
| <a id="symbol-shared-nativecontract-cs-141-c91156d50d"></a><code>MetadataNames.GetGenericTypeParameter</code><br><code>string GetGenericTypeParameter(object? context, int index)</code> | 메서드 | 타입 generic 인자를 !index로 표시한다. | [L141](../../shared/NativeContract.cs#L141) |
| <a id="symbol-shared-nativecontract-cs-142-eb9ca08f32"></a><code>MetadataNames.GetModifiedType</code><br><code>string GetModifiedType(string modifier, string x, bool required)</code> | 메서드 | 필수/선택 custom modifier를 타입 signature에 기록한다. | [L142](../../shared/NativeContract.cs#L142) |
| <a id="symbol-shared-nativecontract-cs-143-92505801a3"></a><code>MetadataNames.GetPinnedType</code><br><code>string GetPinnedType(string x)</code> | 메서드 | pinned 타입 표시를 추가한다. | [L143](../../shared/NativeContract.cs#L143) |
| <a id="symbol-shared-nativecontract-cs-144-9f24cc6032"></a><code>MetadataNames.GetPointerType</code><br><code>string GetPointerType(string x)</code> | 메서드 | 비관리 포인터 타입의 * 표시를 추가한다. | [L144](../../shared/NativeContract.cs#L144) |
| <a id="symbol-shared-nativecontract-cs-145-3e7eb4a02b"></a><code>MetadataNames.GetPrimitiveType</code><br><code>string GetPrimitiveType(PrimitiveTypeCode code)</code> | 메서드 | primitive metadata 코드를 안정된 문자열로 변환한다. | [L145](../../shared/NativeContract.cs#L145) |
| <a id="symbol-shared-nativecontract-cs-146-77950205b8"></a><code>MetadataNames.GetSZArrayType</code><br><code>string GetSZArrayType(string x)</code> | 메서드 | 1차원 zero-based vector 배열 signature를 만든다. | [L146](../../shared/NativeContract.cs#L146) |
| <a id="symbol-shared-nativecontract-cs-147-1803b8034c"></a><code>MetadataNames.GetTypeFromDefinition</code><br><code>string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle h, byte kind)</code> | 메서드 | ISignatureTypeProvider의 type definition을 Definition에 위임한다. | [L147](../../shared/NativeContract.cs#L147) |
| <a id="symbol-shared-nativecontract-cs-148-50a58e4207"></a><code>MetadataNames.GetTypeFromReference</code><br><code>string GetTypeFromReference(MetadataReader r, TypeReferenceHandle h, byte kind)</code> | 메서드 | ISignatureTypeProvider의 type reference를 Reference에 위임한다. | [L148](../../shared/NativeContract.cs#L148) |
| <a id="symbol-shared-nativecontract-cs-149-6a98d2ed46"></a><code>MetadataNames.GetTypeFromSpecification</code><br><code>string GetTypeFromSpecification(MetadataReader r, object? context, TypeSpecificationHandle h, byte kind)</code> | 메서드 | 복합 TypeSpecification blob을 같은 provider로 재귀 decode한다. | [L149](../../shared/NativeContract.cs#L149) |

<a id="file-src-listingobjectivetrace-cs"></a>
## src/ListingObjectiveTrace.cs

[소스 파일](../../src/ListingObjectiveTrace.cs) · 명명된 항목/진입점 22개

수동 UI 조작 관찰기. editor/dropdown/checkbox/list ReceiveEvent와 editor FireCallback을 hook하지만 이벤트/콜백을 새로 생성하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-listingobjectivetrace-cs-12-7b49713ec0"></a><code>ListingObjectiveTrace</code><br><code>ListingObjectiveTrace(Plugin plugin)</code> | 주 생성자/모델 | 이 파일의 역할에 필요한 값과 의존성을 묶어 초기화한다. 전달 항목은 왼쪽 생성자 signature가 기준이다. | [L12](../../src/ListingObjectiveTrace.cs#L12) |
| <a id="symbol-src-listingobjectivetrace-cs-26-190cb159c8"></a><code>ListingObjectiveTrace.Busy</code><br><code>bool Busy</code> | 계산 속성 | 실행 중인 조회/편집/추적 단계가 남아 있는지 계산한다. 계산식: <code>startRequested &#124;&#124; buffer.Active &#124;&#124; eventHooks.Count != 0 &#124;&#124; callbackHook != null</code>. | [L26](../../src/ListingObjectiveTrace.cs#L26) |
| <a id="symbol-src-listingobjectivetrace-cs-27-9f0aad465f"></a><code>ListingObjectiveTrace.Active</code><br><code>bool Active</code> | 계산 속성 | 해당 Bridge/trace가 현재 활성 상태인지 반환한다. 계산식: <code>buffer.Active</code>. | [L27](../../src/ListingObjectiveTrace.cs#L27) |
| <a id="symbol-src-listingobjectivetrace-cs-28-3b783f800e"></a><code>ListingObjectiveTrace.Status</code><br><code>string Status</code> | 계산 속성 | 현재 사용자용 연결/작업 진행 문구를 반환한다. | [L28](../../src/ListingObjectiveTrace.cs#L28) |
| <a id="symbol-src-listingobjectivetrace-cs-29-b87a1ecf4c"></a><code>ListingObjectiveTrace.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L29](../../src/ListingObjectiveTrace.cs#L29) |
| <a id="symbol-src-listingobjectivetrace-cs-31-62748e9c44"></a><code>ListingObjectiveTrace.RequestStart</code><br><code>void RequestStart()</code> | 메서드 | 활성/종료 상태를 검사해 다음 Framework에서 trace 시작을 예약한다. | [L31](../../src/ListingObjectiveTrace.cs#L31) |
| <a id="symbol-src-listingobjectivetrace-cs-32-485be7c815"></a><code>ListingObjectiveTrace.RequestStop</code><br><code>void RequestStop()</code> | 메서드 | 사용자 종료를 기록하되 수집한 로그는 유지한다. | [L32](../../src/ListingObjectiveTrace.cs#L32) |
| <a id="symbol-src-listingobjectivetrace-cs-33-8889191969"></a><code>ListingObjectiveTrace.EditorClosed</code><br><code>void EditorClosed()</code> | 메서드 | 수정창 닫힘에 따라 trace를 종료한다. | [L33](../../src/ListingObjectiveTrace.cs#L33) |
| <a id="symbol-src-listingobjectivetrace-cs-37-1336184c80"></a><code>ListingObjectiveTrace.Update</code><br><code>void Update()</code> | 메서드 | 매 Framework tick에서 시작 예약·시간 제한·컨트롤 binding·상태 변화 및 종료 후 hook 해제를 처리한다. detour 안에서는 hook을 Dispose하지 않는다. | [L37](../../src/ListingObjectiveTrace.cs#L37) |
| <a id="symbol-src-listingobjectivetrace-cs-74-40555b01b6"></a><code>ListingObjectiveTrace.Start</code><br><code>void Start()</code> | 메서드 | 다른 편집/갱신이 없고 수동으로 열린 수정창을 검증한 뒤 listener/node 주소를 고정한다. 같은 구현 주소 hook을 중복 설치하지 않는다. | [L74](../../src/ListingObjectiveTrace.cs#L74) |
| <a id="symbol-src-listingobjectivetrace-cs-109-411e809eb0"></a><code>ListingObjectiveTrace.BindNode</code><br><code>void BindNode(AtkComponentNode* node, string name)</code> | 메서드 | 읽을 수 있는 component node를 이름/주소 registry에 등록하고 node ID를 기록한다. | [L109](../../src/ListingObjectiveTrace.cs#L109) |
| <a id="symbol-src-listingobjectivetrace-cs-116-70cb51636f"></a><code>ListingObjectiveTrace.AddEventHook</code><br><code>void AddEventHook(AtkEventListener* listener)</code> | 메서드 | ReceiveEvent 원본을 정확히 1회, 인자 변경 없이 호출하는 detour를 설치한다. 관찰 대상 instance만 기록하고 MouseMove는 제외한다. | [L116](../../src/ListingObjectiveTrace.cs#L116) |
| <a id="symbol-src-listingobjectivetrace-cs-149-82c0db5d14"></a><code>ListingObjectiveTrace.CallbackDetour</code><br><code>bool CallbackDetour(AtkUnitBase* self, uint count, AtkValue* values, bool close)</code> | 메서드 | 대상 editor의 callback 전후 상태/반환값을 관찰하고 원본을 정확히 한 번 호출한다. | [L149](../../src/ListingObjectiveTrace.cs#L149) |
| <a id="symbol-src-listingobjectivetrace-cs-171-39abbbbd77"></a><code>ListingObjectiveTrace.ReadState</code><br><code>string? ReadState()</code> | 메서드 | 본인·Addon 및 세부 component 주소가 시작 때와 같으면 목적 index/list index/stored/open/pending/checked를 읽는다. | [L171](../../src/ListingObjectiveTrace.cs#L171) |
| <a id="symbol-src-listingobjectivetrace-cs-190-3dd0903c17"></a><code>ListingObjectiveTrace.ReadRegisteredEvents</code><br><code>void ReadRegisteredEvents(AtkResNode* node, string name)</code> | 메서드 | 최대32개 순환 없는 event chain을 읽어 type/param/target/listener를 기록한다. | [L190](../../src/ListingObjectiveTrace.cs#L190) |
| <a id="symbol-src-listingobjectivetrace-cs-202-b2fab20d8e"></a><code>ListingObjectiveTrace.EventInfo</code><br><code>string EventInfo(AtkEvent* evt)</code> | 메서드 | event의 target/listener 분류, optional Node ID와 return flags를 문자열로 만든다. Node는 버튼 identity와 같다고 가정하면 안 된다. | [L202](../../src/ListingObjectiveTrace.cs#L202) |
| <a id="symbol-src-listingobjectivetrace-cs-211-f4de88b1e5"></a><code>ListingObjectiveTrace.DataInfo</code><br><code>string DataInfo(AtkEventType type, AtkEventData* data)</code> | 메서드 | list 이벤트이면 selected index/mouse button을 읽고 다른 payload는 존재 여부만 기록한다. | [L211](../../src/ListingObjectiveTrace.cs#L211) |
| <a id="symbol-src-listingobjectivetrace-cs-219-048853f7b7"></a><code>ListingObjectiveTrace.CallbackValues</code><br><code>string CallbackValues(uint count, AtkValue* values)</code> | 메서드 | 최대16개 scalar 인자만 기록하고 string/pointer는 역참조하지 않는다. password command16의 값은 redacted로 치환한다. | [L219](../../src/ListingObjectiveTrace.cs#L219) |
| <a id="symbol-src-listingobjectivetrace-cs-238-cae89df26b"></a><code>ListingObjectiveTrace.Record</code><br><code>void Record(string line)</code> | 메서드 | 현재 단조 시각과 관찰문을 제한된 trace buffer에 넣는다. | [L238](../../src/ListingObjectiveTrace.cs#L238) |
| <a id="symbol-src-listingobjectivetrace-cs-239-db375de617"></a><code>ListingObjectiveTrace.RecordFault</code><br><code>void RecordFault(Exception ex)</code> | 메서드 | 관찰 예외 종류를 기록하고 trace를 멈춘다. 진단 예외가 게임으로 새어 나가지 않게 한다. | [L239](../../src/ListingObjectiveTrace.cs#L239) |
| <a id="symbol-src-listingobjectivetrace-cs-245-2265956356"></a><code>ListingObjectiveTrace.Cleanup</code><br><code>void Cleanup()</code> | 메서드 | 모든 hook을 해제하고 listener/node 주소 registry를 비운다. | [L245](../../src/ListingObjectiveTrace.cs#L245) |
| <a id="symbol-src-listingobjectivetrace-cs-252-75f14044e6"></a><code>ListingObjectiveTrace.Dispose</code><br><code>void Dispose()</code> | 메서드 | 시작 예약을 취소하고 trace 종료·hook 정리를 수행한다. | [L252](../../src/ListingObjectiveTrace.cs#L252) |

<a id="file-src-nativecompatibility-cs"></a>
## src/NativeCompatibility.cs

[소스 파일](../../src/NativeCompatibility.cs) · 명명된 항목/진입점 8개

빌드에 embedded한 ABI 계약과 현재 로드된 native/interop DLL의 PE metadata를 비교한다. 버전/MVID 차이만으로는 차단하지 않는다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-nativecompatibility-cs-9-2f153aea3c"></a><code>NativeCompatibility.Result</code><br><code>Result(bool Compatible, Guid BuildMvid, Guid LoadedMvid, string Diagnostic)</code> | 주 생성자/모델 | 호환 검사 결과 bool과 build/loaded MVID·진단을 Lazy cache에 보관한다. | [L9](../../src/NativeCompatibility.cs#L9) |
| <a id="symbol-src-nativecompatibility-cs-11-c254ab6b3f"></a><code>NativeCompatibility.IsCompatible</code><br><code>bool IsCompatible</code> | 계산 속성 | Lazy로 한 번 실행한 ABI 검사 통과 여부를 반환한다. 계산식: <code>ResultValue.Value.Compatible</code>. | [L11](../../src/NativeCompatibility.cs#L11) |
| <a id="symbol-src-nativecompatibility-cs-12-9ebceabce1"></a><code>NativeCompatibility.BuildMvid</code><br><code>Guid BuildMvid</code> | 계산 속성 | embedded native 계약에 기록된 빌드 MVID를 반환한다. 계산식: <code>ResultValue.Value.BuildMvid</code>. | [L12](../../src/NativeCompatibility.cs#L12) |
| <a id="symbol-src-nativecompatibility-cs-13-a1d573a008"></a><code>NativeCompatibility.LoadedMvid</code><br><code>Guid LoadedMvid</code> | 계산 속성 | 현재 로드된 native assembly의 MVID를 반환한다. 계산식: <code>ResultValue.Value.LoadedMvid</code>. | [L13](../../src/NativeCompatibility.cs#L13) |
| <a id="symbol-src-nativecompatibility-cs-14-b17ec43d70"></a><code>NativeCompatibility.Diagnostic</code><br><code>string Diagnostic</code> | 계산 속성 | 해당 편집의 단계·요청값·UI/저장값 일치·호출 횟수·실패 원인을 진단 문자열로 합성한다. PIN 원문은 제외한다. | [L14](../../src/NativeCompatibility.cs#L14) |
| <a id="symbol-src-nativecompatibility-cs-16-4e756cfe6c"></a><code>NativeCompatibility.Check</code><br><code>Result Check()</code> | 메서드 | 두 embedded contract를 읽고 현재 파일/로드 MVID 및 API·필드/메서드/layout 증거를 비교한다. 읽기 실패는 fail-closed하고 첫 6개 차이를 진단으로 반환한다. | [L16](../../src/NativeCompatibility.cs#L16) |
| <a id="symbol-src-nativecompatibility-cs-52-4c9e450e8c"></a><code>NativeCompatibility.GetInteropAssembly</code><br><code>Assembly GetInteropAssembly()</code> | 메서드 | NoInlining 경계에서 현재 CStringPointer가 속한 InteropGenerator.Runtime assembly를 찾는다. | [L52](../../src/NativeCompatibility.cs#L52) |
| <a id="symbol-src-nativecompatibility-cs-55-4e10acc50b"></a><code>NativeCompatibility.GetNativeAssembly</code><br><code>Assembly GetNativeAssembly()</code> | 메서드 | NoInlining 경계에서 현재 InfoProxyCrossRealm가 속한 FFXIVClientStructs assembly를 찾는다. | [L55](../../src/NativeCompatibility.cs#L55) |

<a id="file-src-nativereadguard-cs"></a>
## src/NativeReadGuard.cs

[소스 파일](../../src/NativeReadGuard.cs) · 명명된 항목/진입점 5개

네이티브 포인터와 SeString 읽기의 범위/페이지 검사. 호환 계약 통과와 포인터 유효성 검사는 별개다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-nativereadguard-cs-21-d6bfc6d718"></a><code>NativeReadGuard.VirtualQuery</code><br><code>nuint VirtualQuery(nuint address, out MemoryInfo info, nuint size)</code> | 인터페이스 계약 | Windows kernel32 메모리 페이지 속성을 조회하는 P/Invoke 선언이다. | [L21](../../src/NativeReadGuard.cs#L21) |
| <a id="symbol-src-nativereadguard-cs-24-c18ccbd23d"></a><code>NativeReadGuard.Readable</code><br><code>bool Readable(void* pointer, nuint length)</code> | 메서드 | 주소·길이·overflow·최대64KiB와 최대16개 페이지를 검사해 committed/readable이며 guard가 아닌지 판정한다. 메모리 layout 자체의 의미까지 보장하지 않는다. | [L24](../../src/NativeReadGuard.cs#L24) |
| <a id="symbol-src-nativereadguard-cs-42-a441cdf21a"></a><code>NativeReadGuard.TextNode</code><br><code>bool TextNode(AtkTextNode* node, out string text)</code> | 메서드 | AtkTextNode와 NodeType.Text를 검사한 뒤 NodeText를 읽는다. | [L42](../../src/NativeReadGuard.cs#L42) |
| <a id="symbol-src-nativereadguard-cs-48-92fdf3266d"></a><code>NativeReadGuard.Text</code><br><code>bool Text(Utf8String* value, out string text)</code> | 메서드 | Utf8String 구조와 BufUsed/BufSize, 최대16KiB 및 실제 문자열 포인터를 확인해 FixedText로 넘긴다. | [L48](../../src/NativeReadGuard.cs#L48) |
| <a id="symbol-src-nativereadguard-cs-59-cc352d5b2f"></a><code>NativeReadGuard.FixedText</code><br><code>bool FixedText(ReadOnlySpan&lt;byte&gt; bytes, out string text)</code> | 메서드 | NUL 이전 byte를 SeString으로 해석해 trim한다. 대체 문자/허용하지 않은 제어문자나 decode 예외는 실패로 반환한다. | [L59](../../src/NativeReadGuard.cs#L59) |

<a id="file-src-objectivetracebuffer-cs"></a>
## src/ObjectiveTraceBuffer.cs

[소스 파일](../../src/ObjectiveTraceBuffer.cs) · 명명된 항목/진입점 9개

lock으로 보호하는 최대1024줄/120초 메모리 기록. 중단은 기록을 보존하고 새 시작은 이전 기록을 교체한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-src-objectivetracebuffer-cs-14-c62d5024cb"></a><code>ObjectiveTraceBuffer.Active</code><br><code>bool Active</code> | 계산 속성 | 해당 Bridge/trace가 현재 활성 상태인지 반환한다. | [L14](../../src/ObjectiveTraceBuffer.cs#L14) |
| <a id="symbol-src-objectivetracebuffer-cs-15-43cfc9e146"></a><code>ObjectiveTraceBuffer.Count</code><br><code>int Count</code> | 계산 속성 | 현재 registry/카탈로그/버퍼의 항목 수를 반환한다. | [L15](../../src/ObjectiveTraceBuffer.cs#L15) |
| <a id="symbol-src-objectivetracebuffer-cs-16-778db4ecef"></a><code>ObjectiveTraceBuffer.Reason</code><br><code>string Reason</code> | 계산 속성 | trace buffer의 현재 기록/종료 사유를 lock 안에서 반환한다. | [L16](../../src/ObjectiveTraceBuffer.cs#L16) |
| <a id="symbol-src-objectivetracebuffer-cs-18-414f860a82"></a><code>ObjectiveTraceBuffer.Start</code><br><code>void Start(long now)</code> | 메서드 | 기존 entries를 비우고 시작시각/recording 상태를 설정한다. | [L18](../../src/ObjectiveTraceBuffer.cs#L18) |
| <a id="symbol-src-objectivetracebuffer-cs-23-7ce22dc9fa"></a><code>ObjectiveTraceBuffer.Add</code><br><code>void Add(long now, string line)</code> | 메서드 | 활성 상태·120초·1024줄 제한을 검사한 뒤 상대 ms와 한 줄을 추가한다. | [L23](../../src/ObjectiveTraceBuffer.cs#L23) |
| <a id="symbol-src-objectivetracebuffer-cs-34-897e37675a"></a><code>ObjectiveTraceBuffer.Tick</code><br><code>void Tick(long now)</code> | 메서드 | 새 이벤트가 없어도 시간 제한이 지나면 종료한다. | [L34](../../src/ObjectiveTraceBuffer.cs#L34) |
| <a id="symbol-src-objectivetracebuffer-cs-39-e787b2f3b6"></a><code>ObjectiveTraceBuffer.Stop</code><br><code>void Stop(string stopReason)</code> | 메서드 | 활성 기록의 종료 사유를 설정한다. | [L39](../../src/ObjectiveTraceBuffer.cs#L39) |
| <a id="symbol-src-objectivetracebuffer-cs-44-41c40668bf"></a><code>ObjectiveTraceBuffer.StopCore</code><br><code>void StopCore(string stopReason)</code> | 메서드 | lock을 이미 보유한 내부 경로에서 active=false와 종료 사유를 기록한다. | [L44](../../src/ObjectiveTraceBuffer.cs#L44) |
| <a id="symbol-src-objectivetracebuffer-cs-46-b30bdbe4e1"></a><code>ObjectiveTraceBuffer.Snapshot</code><br><code>string Snapshot()</code> | 메서드 | 상태/사유/줄 수 header와 전체 로그를 lock 안에서 문자열로 복사한다. | [L46](../../src/ObjectiveTraceBuffer.cs#L46) |

<a id="file-tools-compatibilitytool-program-cs"></a>
## tools/CompatibilityTool/Program.cs

[소스 파일](../../tools/CompatibilityTool/Program.cs) · 명명된 항목/진입점 1개

PE metadata CLI: inspect는 assembly 이름/버전, capture/capture-runtime은 계약 baseline 생성, verify는 현재 DLL과 저장 계약을 비교한다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tools-compatibilitytool-program-cs-1-e327bba016"></a><code>&lt;top-level&gt;</code><br><code>&lt;top-level&gt;</code> | 진입점 | CLI 인자를 검증한다. capture에서 플러그인이 사용하는 타입/메서드 allowlist 누락을 검사하고 JSON을 쓴다. verify 차이가 있으면 exit code1이다. baseline capture는 자동 호환 승인과 다르다. | [L1](../../tools/CompatibilityTool/Program.cs#L1) |

<a id="file-tools-release-py"></a>
## tools/release.py

[소스 파일](../../tools/release.py) · 명명된 항목/진입점 17개

고정 reference 다운로드→ABI 확인→버전/런타임 의존성 검증 ZIP→release 검증 후 feed 업데이트. 명시 실행하는 배포 도구다.

| 심볼 · signature | 종류 | 역할 / 디버깅 시 확인할 점 | 소스 |
|---|---|---|---|
| <a id="symbol-tools-release-py-1-e6050d09a1"></a><code>&lt;module&gt;</code><br><code>&lt;module&gt;</code> | 진입점 | 상수·패키지 allowlist·경로를 정의하고 직접 실행일 때만 main을 호출한다. | [L1](../../tools/release.py#L1) |
| <a id="symbol-tools-release-py-31-46a4444583"></a><code>read_json</code><br><code>read_json(path)</code> | Python 함수 | UTF-8 BOM 허용으로 JSON 파일을 읽는다. | [L31](../../tools/release.py#L31) |
| <a id="symbol-tools-release-py-35-f5bbed54f7"></a><code>write_json</code><br><code>write_json(path, value)</code> | Python 함수 | 한글을 유지한 들여쓰기 JSON과 마지막 줄바꿈을 기록한다. | [L35](../../tools/release.py#L35) |
| <a id="symbol-tools-release-py-39-d854d38d4a"></a><code>version</code><br><code>version(value)</code> | Python 함수 | 정확히 네 숫자 부분의 assembly version을 검증하고 숫자 tuple로 반환한다. | [L39](../../tools/release.py#L39) |
| <a id="symbol-tools-release-py-45-012f66835e"></a><code>tag_version</code><br><code>tag_version(tag)</code> | Python 함수 | 선택적 v 접두어와3/4부분 tag를 검증해 네 부분 version tuple로 정상화한다. | [L45](../../tools/release.py#L45) |
| <a id="symbol-tools-release-py-52-94547e141f"></a><code>run</code><br><code>run(*args, capture=False)</code> | Python 함수 | repo를 cwd로 subprocess를 실행하고 실패 시 예외를 발생시킨다. capture면 stdout을 반환한다. | [L52](../../tools/release.py#L52) |
| <a id="symbol-tools-release-py-58-f650f9a6f3"></a><code>download</code><br><code>download(url)</code> | Python 함수 | HTTPS URL만 허용하고 제한 시간 안에 byte를 다운로드한다. | [L58](../../tools/release.py#L58) |
| <a id="symbol-tools-release-py-65-49250e1f92"></a><code>references</code><br><code>references()</code> | Python 함수 | reference-lock URL의 archive SHA-256을 검사한 후 allowlist의 root DLL만 .build/references에 저장한다. 무제한 zip extract를 하지 않는다. | [L65](../../tools/release.py#L65) |
| <a id="symbol-tools-release-py-79-6cb7b33de7"></a><code>verify</code><br><code>verify(refs)</code> | Python 함수 | CompatibilityTool을 build하고 reference Dalamud API와 manifest/contract API를 맞춘 뒤 native/interop ABI를 비교한다. | [L79](../../tools/release.py#L79) |
| <a id="symbol-tools-release-py-92-c967ff0832"></a><code>verify_runtime_files</code><br><code>verify_runtime_files(build_dir)</code> | Python 함수 | deps.json의 runtime DLL 집합과 패키지 allowlist가 정확히 맞고 필요한 파일이 모두 있는지 확인한다. 별도 플랫폼 runtimeTargets는 명시 검토 전 거부한다. | [L92](../../tools/release.py#L92) |
| <a id="symbol-tools-release-py-104-3541973726"></a><code>package</code><br><code>package(build_dir, tag=None)</code> | Python 함수 | project Version/AssemblyVersion/FileVersion·manifest·선택 tag·실제 DLL identity를 비교한다. 고정 timestamp ZIP과 SHA256SUMS/package metadata를 생성한다. | [L104](../../tools/release.py#L104) |
| <a id="symbol-tools-release-py-134-1b439df736"></a><code>feed_entry</code><br><code>feed_entry(manifest, repo, tag)</code> | Python 함수 | 검증된 버전/tag의 release ZIP 링크와 timestamp를 가진 플러그인 feed 항목을 만든다. | [L134](../../tools/release.py#L134) |
| <a id="symbol-tools-release-py-144-196ab3ef6e"></a><code>merge_feed</code><br><code>merge_feed(entries, entry)</code> | Python 함수 | 다른 플러그인 항목을 보존하며 PartyViewKR만 교체/추가한다. 중복 항목은 거부하고 새 버전으로부터 downgrade하지 않는다. | [L144](../../tools/release.py#L144) |
| <a id="symbol-tools-release-py-159-5d97ea0bf6"></a><code>api</code><br><code>api(repo, path, method='GET', data=None, raw=False)</code> | Python 함수 | api.github.com/uploads.github.com allowlist에만 GH_TOKEN 인증 요청을 보내고 JSON 응답을 읽는다. token을 로그로 출력하지 않는다. | [L159](../../tools/release.py#L159) |
| <a id="symbol-tools-release-py-175-5f9255d769"></a><code>optional_get</code><br><code>optional_get(repo, path)</code> | Python 함수 | GitHub GET의404만 없는 리소스로 처리하고 다른 오류는 전파한다. | [L175](../../tools/release.py#L175) |
| <a id="symbol-tools-release-py-184-a18e4a8399"></a><code>publish</code><br><code>publish(tag)</code> | Python 함수 | 검증한 ZIP·manifest·tag commit을 재확인하고 draft release/upload/digest/public download 순으로 확인한다. 공개 asset 검증 후에만 최신 feed SHA로 업데이트하며 충돌은 제한 재시도한다. | [L184](../../tools/release.py#L184) |
| <a id="symbol-tools-release-py-255-436036510b"></a><code>main</code><br><code>main()</code> | Python 함수 | references/verify/package/publish와 refs/build-dir/tag 인자를 파싱해 해당 명령을 호출한다. | [L255](../../tools/release.py#L255) |
