import importlib.util
import json
from pathlib import Path
import unittest
import tempfile
import hashlib
import zipfile
import os
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('release', Path(__file__).resolve().parents[1] / 'tools/release.py')
r = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r)


class ReleaseTests(unittest.TestCase):
    def test_repository_api_url_has_no_trailing_slash(self):
        with patch.dict(os.environ, {'GH_TOKEN': 'test-token'}), patch.object(r.urllib.request, 'urlopen') as opened:
            opened.return_value.__enter__.return_value.read.return_value = b'{"default_branch":"main"}'
            r.api('pressing7/PartyViewKR', '')
            self.assertEqual(opened.call_args.args[0].full_url, 'https://api.github.com/repos/pressing7/PartyViewKR')

    def test_actual_tag_is_used_with_or_without_v(self):
        for tag in ['0.3.2', 'v0.3.2', '0.3.2.0', 'v0.3.2.0']:
            entry = r.feed_entry({'AssemblyVersion': '0.3.2.0'}, 'pressing7/PartyViewKR', tag)
            self.assertIn('/' + tag + '/PartyViewKR.zip', entry['DownloadLinkUpdate'])
            self.assertEqual(entry['DownloadLinkInstall'], entry['DownloadLinkUpdate'])

    def test_mismatched_or_malformed_tag_is_rejected(self):
        for tag in ['v0.3.1', '../0.3.2', '0.3.2-rc', '0.3.2.0.0']:
            with self.assertRaises(ValueError):
                r.feed_entry({'AssemblyVersion': '0.3.2.0'}, 'pressing7/PartyViewKR', tag)

    def test_numeric_version_order_prevents_downgrade(self):
        old = [{'InternalName': 'PartyViewKR', 'AssemblyVersion': '0.10.0.0'}]
        self.assertIsNone(r.merge_feed(old, {'InternalName': 'PartyViewKR', 'AssemblyVersion': '0.3.2.0'}))

    def test_other_plugins_are_preserved(self):
        other = {'InternalName': 'Other', 'AssemblyVersion': '1.0.0.0'}
        old = [other, {'InternalName': 'PartyViewKR', 'AssemblyVersion': '0.3.1.0'}]
        new = {'InternalName': 'PartyViewKR', 'AssemblyVersion': '0.3.2.0'}
        self.assertEqual(r.merge_feed(old, new), [other, new])
        self.assertEqual(old[1]['AssemblyVersion'], '0.3.1.0')

    def test_duplicate_feed_entries_are_rejected(self):
        entry = {'InternalName': 'PartyViewKR', 'AssemblyVersion': '0.3.2.0'}
        with self.assertRaises(ValueError):
            r.merge_feed([entry, entry], entry)

    def test_package_version_mismatch_fails_before_writing_zip(self):
        with self.assertRaises(ValueError):
            r.package(Path('nonexistent'), 'v99.0.0')

    def test_package_contains_the_complete_runtime_dependency_set(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp)
            for name in r.FILES:
                (path / name).write_text('fixture')
            runtime = {name: {} for name in r.FILES if name.endswith('.dll')}
            deps = {'runtimeTarget': {'name': 'net10'}, 'targets': {'net10': {'fixture': {'runtime': runtime}}}}
            r.write_json(path / 'PartyViewKR.deps.json', deps)
            r.verify_runtime_files(path)
            (path / 'Discord.Net.Core.dll').unlink()
            with self.assertRaisesRegex(ValueError, 'missing'):
                r.verify_runtime_files(path)

    def test_new_or_native_runtime_dependencies_require_package_review(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp)
            for name in r.FILES:
                (path / name).write_text('fixture')
            runtime = {name: {} for name in r.FILES if name.endswith('.dll')}
            runtime['Unexpected.dll'] = {}
            deps = {'runtimeTarget': {'name': 'net10'}, 'targets': {'net10': {'fixture': {'runtime': runtime}}}}
            r.write_json(path / 'PartyViewKR.deps.json', deps)
            with self.assertRaisesRegex(ValueError, 'allowlist'):
                r.verify_runtime_files(path)
            del runtime['Unexpected.dll']
            deps['targets']['net10']['fixture']['runtimeTargets'] = {'runtimes/win-x64/native/extra.dll': {}}
            r.write_json(path / 'PartyViewKR.deps.json', deps)
            with self.assertRaisesRegex(ValueError, 'Platform-specific'):
                r.verify_runtime_files(path)

    def test_wrong_reference_hash_never_extracts_dlls(self):
        with patch.object(r, 'download', return_value=b'wrong'), patch.object(r, 'zipfile') as zip_mock:
            with self.assertRaises(ValueError):
                r.references()
            zip_mock.ZipFile.assert_not_called()

    def test_no_non_https_downloads(self):
        with self.assertRaises(ValueError):
            r.download('http://example.test/archive.zip')

    def test_failed_public_download_never_updates_feed(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp)
            manifest = {'InternalName': 'PartyViewKR', 'AssemblyVersion': '0.3.2.0'}
            with zipfile.ZipFile(out / 'PartyViewKR.zip', 'w') as z:
                for name in r.FILES:
                    z.writestr(name, json.dumps(manifest) if name.endswith('KR.json') else 'test')
            digest = hashlib.sha256((out / 'PartyViewKR.zip').read_bytes()).hexdigest()
            r.write_json(out / 'package.json', {'version': '0.3.2.0', 'manifest': manifest, 'sha256': digest})
            calls = []
            def fake_api(repo, path, method='GET', data=None, raw=False):
                calls.append((path, method))
                if path == '': return {'default_branch': 'main'}
                if path.startswith('git/ref/'): return {'object': {'type': 'commit', 'sha': 'abc'}}
                if path.startswith('releases/tags/'):
                    return {'id': 1, 'draft': False, 'assets': [{'name': 'PartyViewKR.zip', 'digest': 'sha256:' + digest}]}
                raise AssertionError('Unexpected API call: ' + path)
            with patch.object(r, 'OUT', out), patch.object(r, 'api', side_effect=fake_api), \
                 patch.object(r, 'download', return_value=b'corrupted'), \
                 patch.dict(os.environ, {'GITHUB_REPOSITORY': 'pressing7/PartyViewKR', 'GITHUB_SHA': 'abc'}):
                with self.assertRaisesRegex(ValueError, 'feed left unchanged'):
                    r.publish('0.3.2')
            self.assertFalse(any('contents/' in path for path, method in calls))


if __name__ == '__main__':
    unittest.main()
