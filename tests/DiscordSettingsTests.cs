using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using PartyViewKR;

static class DiscordSettingsTests
{
    internal const string FakeToken = "NTAw.fake.only-for-offline-tests-not-a-real-token";
    // Exercise encrypted persistence without depending on Windows DPAPI on CI.
    // Production uses CurrentUser DPAPI, not this test-only AES key.
    private static readonly byte[] Key = RandomNumberGenerator.GetBytes(32);
    internal static string Protect(string value)
    {
        var plain = Encoding.UTF8.GetBytes(value);
        var data = new byte[12 + 16 + plain.Length];
        RandomNumberGenerator.Fill(data.AsSpan(0, 12));
        using var aes = new AesGcm(Key, 16);
        try { aes.Encrypt(data.AsSpan(0, 12), plain, data.AsSpan(28), data.AsSpan(12, 16)); }
        finally { CryptographicOperations.ZeroMemory(plain); }
        return Convert.ToBase64String(data);
    }
    internal static string Unprotect(string value)
    {
        var data = Convert.FromBase64String(value);
        var plain = new byte[data.Length - 28];
        using var aes = new AesGcm(Key, 16);
        try {
            aes.Decrypt(data.AsSpan(0, 12), data.AsSpan(28), data.AsSpan(12, 16), plain);
            return Encoding.UTF8.GetString(plain);
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
    private static RelayConfiguration Reload(RelayConfiguration config) =>
        JsonConvert.DeserializeObject<RelayConfiguration>(JsonConvert.SerializeObject(config))!;
    private static RelayConfiguration Saved()
    {
        var config = new RelayConfiguration { ServerUrl = "https://relay.example.test" };
        if (!config.PrepareDiscordConnection(FakeToken, true, Protect, Unprotect, out _)) throw new Exception("fixture token rejected");
        return config;
    }
    internal static void Run(Action<string, Action> test, Action<bool, string> check)
    {
        test("discord fresh settings default to remembering but never auto-connect without a token", () => {
            var config = Reload(new());
            check(config.RememberDiscordToken && !config.ShouldAutoConnectDiscord, "bad initial connection preference");
        });
        test("discord encrypted token survives a disk reload and reconnects without re-entry", () => {
            var path = Path.Combine(Path.GetTempPath(), "pvkr-settings-" + Guid.NewGuid().ToString("N") + ".json");
            try {
                var saved = Saved();
                File.WriteAllText(path, JsonConvert.SerializeObject(saved));
                var json = File.ReadAllText(path);
                check(!json.Contains(FakeToken), "plaintext token persisted");
                var restored = JsonConvert.DeserializeObject<RelayConfiguration>(json)!;
                check(restored.ShouldAutoConnectDiscord && restored.RememberDiscordToken, "restart lost auto-connect");
                check(restored.PrepareDiscordConnection("", true, _ => throw new Exception("saved token re-encrypted"), Unprotect, out var token)
                    && token == FakeToken, "saved token not restored");
                check(restored.EncryptedDiscordToken == saved.EncryptedDiscordToken && restored.ServerUrl == saved.ServerUrl,
                    "restart changed saved ciphertext or relay settings");
            }
            finally { File.Delete(path); }
        });
        test("discord 0.3.46 config gains auto-connect without losing the encrypted token", () => {
            var saved = Saved();
            var json = JsonConvert.SerializeObject(new { Version = 1, saved.ServerUrl, saved.EncryptedDiscordToken });
            var restored = JsonConvert.DeserializeObject<RelayConfiguration>(json)!;
            check(restored.ShouldAutoConnectDiscord && restored.RememberDiscordToken, "legacy token not migrated");
            check(restored.PrepareDiscordConnection("", true, Protect, Unprotect, out var token) && token == FakeToken,
                "legacy ciphertext changed format");
        });
        test("discord explicit disconnect survives restart and keeps token for manual reconnect", () => {
            var saved = Saved(); saved.DiscordAutoConnect = false;
            var restored = Reload(saved);
            check(!restored.ShouldAutoConnectDiscord && restored.HasSavedDiscordToken, "explicit off lost");
            check(restored.PrepareDiscordConnection("", true, Protect, Unprotect, out var token) && token == FakeToken,
                "disconnect erased reusable token");
            check(Reload(restored).ShouldAutoConnectDiscord, "manual reconnect did not restore startup preference");
        });
        test("discord remember opt-out persists and clears a previously saved token", () => {
            var config = Saved();
            check(config.PrepareDiscordConnection("", false, _ => throw new Exception("unexpected encryption"), Unprotect, out var token)
                && token == FakeToken, "current connection did not receive the token");
            var restored = Reload(config);
            check(!restored.RememberDiscordToken && !restored.HasSavedDiscordToken && !restored.ShouldAutoConnectDiscord,
                "remember opt-out was not persisted");
            check(!JsonConvert.SerializeObject(restored).Contains(FakeToken), "opt-out stored plaintext");
        });
        test("discord forget removes token and startup preference across restart", () => {
            var config = Saved(); config.ForgetDiscordToken();
            var restored = Reload(config);
            check(!restored.HasSavedDiscordToken && !restored.ShouldAutoConnectDiscord && !restored.RememberDiscordToken,
                "forgotten token restored");
        });
        test("discord failed decryption preserves settings for recovery", () => {
            var config = Saved(); var before = JsonConvert.SerializeObject(config);
            try {
                config.PrepareDiscordConnection("", true, Protect, _ => throw new CryptographicException("different Windows account"), out _);
                throw new Exception("expected decryption failure");
            }
            catch (CryptographicException) { }
            check(JsonConvert.SerializeObject(config) == before, "decryption failure erased stored credentials");
        });
        test("discord invalid replacement and encryption failure never overwrite saved settings", () => {
            var config = Saved(); var before = JsonConvert.SerializeObject(config);
            check(!config.PrepareDiscordConnection("invalid", true, _ => throw new Exception("should validate first"), Unprotect, out _),
                "invalid token accepted");
            try {
                config.PrepareDiscordConnection(FakeToken + "replacement", true, _ => throw new CryptographicException(), Unprotect, out _);
                throw new Exception("expected encryption failure");
            }
            catch (CryptographicException) { }
            check(JsonConvert.SerializeObject(config) == before, "invalid replacement overwrote saved credentials");
        });
    }
}
