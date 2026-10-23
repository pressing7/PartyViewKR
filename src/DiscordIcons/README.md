# Discord roster images

74 PNGs supplied by the user in `imgfin.zip`: 32 jobs/classes and 5 shared
conditions, each with an ordinary and `checked_` occupied variant. Original
320 × 320 RGBA bytes are preserved; all files are below Discord's 256 KiB upload
limit. They are embedded in the plugin DLL and uploaded using Discord.Net's
application emoji API. No game asset extraction or external image host is needed.

`ListingDiscordEmojiAssets.cs` maps filenames to application emoji names.
`vip` is job 41 (Viper), `pic` is job 42 (Pictomancer), and `bst` is job 43.
`TH`, `TD`, `HD`, and `THD` represent the allowed tank/healer/damage roles;
`any` means all runtime battle jobs and classes are allowed. `checked_` means a
participant occupies the slot, never that an editing option is selected.

No standalone multi-job T/H/D role artwork is included. These conditions use
the existing state symbols until artwork is supplied. Keep runtime IDs and
bot tokens out of this directory; each bot discovers its own emoji IDs.
