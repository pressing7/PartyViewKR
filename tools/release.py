"""Reproducible references, version-checked packages and release-first feed updates."""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'release-output'
REFS = ROOT / '.build' / 'references'
DOTNET = os.environ.get('DOTNET', 'dotnet')
TOOL = ROOT / 'tools/CompatibilityTool/bin/Release/net10.0/CompatibilityTool.dll'
FILES = ['PartyViewKR.dll', 'PartyViewKR.deps.json', 'PartyViewKR.json']
REFERENCE_FILES = ['Dalamud.dll', 'FFXIVClientStructs.dll', 'Dalamud.Bindings.ImGui.dll',
                   'InteropGenerator.Runtime.dll', 'Dalamud.Common.dll']


def read_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write_json(path, value):
    Path(path).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def version(value):
    if not re.fullmatch(r'\d+\.\d+\.\d+\.\d+', value):
        raise ValueError('Expected four-component assembly version: ' + value)
    return tuple(map(int, value.split('.')))


def tag_version(tag):
    if not re.fullmatch(r'v?\d+\.\d+\.\d+(?:\.\d+)?', tag):
        raise ValueError('Invalid version tag: ' + tag)
    value = tag.removeprefix('v')
    return version(value + '.0' if value.count('.') == 2 else value)


def run(*args, capture=False):
    result = subprocess.run(list(map(str, args)), cwd=ROOT, check=True, text=True,
                            stdout=subprocess.PIPE if capture else None)
    return result.stdout


def download(url):
    if urllib.parse.urlsplit(url).scheme != 'https':
        raise ValueError('Download URL must be HTTPS')
    with urllib.request.urlopen(url, timeout=120) as response:
        return response.read()


def references():
    lock = read_json(ROOT / 'compatibility/reference-lock.json')
    data = download(lock['url'])
    if hashlib.sha256(data).hexdigest() != lock['sha256']:
        raise ValueError('Reference archive SHA-256 mismatch')
    REFS.mkdir(parents=True, exist_ok=True)
    archive = ROOT / '.build/references.zip'
    archive.write_bytes(data)
    with zipfile.ZipFile(archive) as z:
        for name in REFERENCE_FILES:  # Only known root-level files; no archive extraction paths.
            (REFS / name).write_bytes(z.read(name))
    print('Pinned reference archive verified.')


def verify(refs):
    run(DOTNET, 'build', 'tools/CompatibilityTool', '-c', 'Release', '--nologo')
    identity = json.loads(run(DOTNET, TOOL, 'inspect', refs / 'Dalamud.dll', capture=True))
    api = read_json(ROOT / 'src/PartyViewKR.json')['DalamudApiLevel']
    if identity['name'] != 'Dalamud' or version(identity['version'])[0] != api:
        raise ValueError('Reference Dalamud API does not match plugin manifest')
    for contract, assembly in [('native', 'FFXIVClientStructs'), ('interop', 'InteropGenerator.Runtime')]:
        baseline = ROOT / f'compatibility/{contract}-contract.json'
        if read_json(baseline)['ApiLevel'] != api:
            raise ValueError('Contract API does not match plugin manifest')
        run(DOTNET, TOOL, 'verify', baseline, refs / (assembly + '.dll'))


def package(build_dir, tag=None):
    manifest = read_json(ROOT / 'src/PartyViewKR.json')
    ver = manifest['AssemblyVersion']
    version(ver)
    props = ET.parse(ROOT / 'src/PartyViewKR.csproj').getroot().find('PropertyGroup')
    if (props.findtext('AssemblyVersion') != ver or props.findtext('FileVersion') != ver
            or tag_version(props.findtext('Version')) != version(ver)):
        raise ValueError('Project and manifest versions disagree')
    if tag and tag_version(tag) != version(ver):
        raise ValueError('Tag and package versions disagree')
    identity = json.loads(run(DOTNET, TOOL, 'inspect', build_dir / 'PartyViewKR.dll', capture=True))
    if identity != {'name': 'PartyViewKR', 'version': ver}:
        raise ValueError('Built DLL identity disagrees with manifest')
    if read_json(build_dir / 'PartyViewKR.json') != manifest:
        raise ValueError('Built manifest differs from source manifest')
    OUT.mkdir(exist_ok=True)
    # Fixed timestamps make identical builds produce identical ZIPs on retries.
    with zipfile.ZipFile(OUT / 'PartyViewKR.zip', 'w', compression=zipfile.ZIP_DEFLATED) as z:
        for name in FILES:
            info = zipfile.ZipInfo(name, (2020, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            z.writestr(info, (build_dir / name).read_bytes())
    digest = hashlib.sha256((OUT / 'PartyViewKR.zip').read_bytes()).hexdigest()
    write_json(OUT / 'package.json', {'version': ver, 'sha256': digest, 'manifest': manifest})
    (OUT / 'SHA256SUMS.txt').write_text(digest + '  PartyViewKR.zip\n', encoding='utf-8')
    print('Packaged PartyViewKR ' + ver + ' SHA256=' + digest)


def feed_entry(manifest, repo, tag):
    if tag_version(tag) != version(manifest['AssemblyVersion']):
        raise ValueError('Feed tag version mismatch')
    entry = dict(manifest, RepoUrl='https://github.com/' + repo, LastUpdate=int(time.time()))
    url = f'https://github.com/{repo}/releases/download/{tag}/PartyViewKR.zip'
    for key in ['DownloadLinkInstall', 'DownloadLinkUpdate', 'DownloadLinkTesting']:
        entry[key] = url
    return entry


def merge_feed(entries, entry):
    matches = [i for i, old in enumerate(entries) if old.get('InternalName') == 'PartyViewKR']
    if len(matches) > 1:
        raise ValueError('Duplicate PartyViewKR feed entries')
    result = list(entries)
    if matches:
        i = matches[0]
        if version(result[i]['AssemblyVersion']) > version(entry['AssemblyVersion']):
            return None  # Never downgrade a newer deployment.
        result[i] = entry
    else:
        result.append(entry)
    return result


def api(repo, path, method='GET', data=None, raw=False):
    url = path if path.startswith('https://') else f'https://api.github.com/repos/{repo}/{path}'.rstrip('/')
    if urllib.parse.urlsplit(url).hostname not in ('api.github.com', 'uploads.github.com'):
        raise ValueError('Unexpected GitHub API host')
    headers = {'Authorization': 'Bearer ' + os.environ['GH_TOKEN'], 'User-Agent': 'PartyViewKR-release',
               'Accept': 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28'}
    if data is not None:
        headers['Content-Type'] = 'application/zip' if raw else 'application/json'
        if not raw:
            data = json.dumps(data).encode()
    request = urllib.request.Request(url, headers=headers, data=data, method=method)
    with urllib.request.urlopen(request, timeout=120) as response:
        body = response.read()
        return json.loads(body) if body else None


def optional_get(repo, path):
    try:
        return api(repo, path)
    except urllib.error.HTTPError as e:
        if e.code != 404:
            raise
        return None


def publish(tag):
    repo = os.environ['GITHUB_REPOSITORY']
    sha = os.environ['GITHUB_SHA']
    meta = read_json(OUT / 'package.json')
    blob = (OUT / 'PartyViewKR.zip').read_bytes()
    if hashlib.sha256(blob).hexdigest() != meta['sha256']:
        raise ValueError('Package changed after validation')
    with zipfile.ZipFile(OUT / 'PartyViewKR.zip') as z:
        if sorted(z.namelist()) != sorted(FILES) or json.loads(z.read('PartyViewKR.json')) != meta['manifest']:
            raise ValueError('Package contents disagree with metadata')
    tag = tag or meta['version'].removesuffix('.0')
    entry = feed_entry(meta['manifest'], repo, tag)
    repository = api(repo, '')
    branch = repository['default_branch']
    tag_path = 'git/ref/tags/' + urllib.parse.quote(tag, safe='')
    current_tag = optional_get(repo, tag_path)
    if current_tag:
        obj = current_tag['object']
        while obj['type'] == 'tag':
            obj = api(repo, 'git/tags/' + obj['sha'])['object']
        if obj['sha'] != sha:
            # Ordinary main commits with unchanged version must not replace existing releases.
            if os.environ.get('GITHUB_REF') == 'refs/heads/' + branch:
                print('Version already tagged at another commit. Bump source versions for a new release.')
                return
            raise ValueError('Existing tag points to a different commit; refusing to move it')
    else:
        api(repo, 'git/refs', 'POST', {'ref': 'refs/tags/' + tag, 'sha': sha})
    release = optional_get(repo, 'releases/tags/' + urllib.parse.quote(tag, safe=''))
    if release is None:
        release = api(repo, 'releases', 'POST', {'tag_name': tag, 'name': 'PartyViewKR ' + meta['version'],
                    'target_commitish': sha, 'draft': True, 'body':
                    '한섭 파티 조회·수동 갱신 플러그인. 빌드 및 구조 호환성 검사 통과.\n'
                    '설치 파일: PartyViewKR.zip\nSHA-256: ' + meta['sha256'] + '\n'
                    '실제 게임 동작은 별도 확인이 필요합니다.'})
    assets = [a for a in release['assets'] if a['name'] == 'PartyViewKR.zip']
    if assets:
        if len(assets) != 1 or assets[0].get('digest') != 'sha256:' + meta['sha256']:
            raise ValueError('Existing release asset differs or lacks a verifiable digest; refusing overwrite')
    else:
        if not release['draft']:
            raise ValueError('Published release has no installation asset; refusing to alter it')
        uploaded = api(repo, release['upload_url'].split('{')[0] + '?name=PartyViewKR.zip', 'POST', blob, raw=True)
        if uploaded.get('digest') != 'sha256:' + meta['sha256']:
            raise ValueError('Uploaded asset digest mismatch')
    if release['draft']:
        release = api(repo, 'releases/' + str(release['id']), 'PATCH', {'draft': False})
    # Confirm the published asset is available before advertising it in the repository feed.
    if hashlib.sha256(download(entry['DownloadLinkInstall'])).hexdigest() != meta['sha256']:
        raise ValueError('Public release download verification failed; feed left unchanged')
    feed_path = 'contents/pluginmaster.json'
    for attempt in range(3):
        current = api(repo, feed_path + '?ref=' + urllib.parse.quote(branch, safe=''))
        entries = json.loads(base64.b64decode(current['content']))
        merged = merge_feed(entries, entry)
        if merged is None:
            print('A newer version is already listed; feed left unchanged.')
            return
        if merged == entries:
            return
        content = (json.dumps(merged, ensure_ascii=False, indent=2) + '\n').encode()
        try:
            api(repo, feed_path, 'PUT', {'message': 'Publish PartyViewKR ' + meta['version'], 'branch': branch,
                                       'sha': current['sha'], 'content': base64.b64encode(content).decode()})
            print('Published release and updated pluginmaster.json: ' + tag)
            return
        except urllib.error.HTTPError as e:
            if e.code not in (409, 422) or attempt == 2:
                raise


def main():
    p = argparse.ArgumentParser()
    p.add_argument('command', choices=['references', 'verify', 'package', 'publish'])
    p.add_argument('--refs', type=Path, default=REFS)
    p.add_argument('--build-dir', type=Path, default=ROOT / 'src/bin/Release/net10.0-windows')
    p.add_argument('--tag')
    a = p.parse_args()
    if a.command == 'references': references()
    elif a.command == 'verify': verify(a.refs.resolve())
    elif a.command == 'package': package(a.build_dir.resolve(), a.tag)
    elif a.command == 'publish': publish(a.tag)


if __name__ == '__main__':
    main()
