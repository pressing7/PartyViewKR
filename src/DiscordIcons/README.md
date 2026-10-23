# Discord roster images

80 user-supplied PNGs: 74 from `imgfin.zip` plus six standalone tank/healer/dealer
images. There are 32 jobs/classes and 8 shared conditions, each with an ordinary
and `checked_` occupied variant. Original
320 × 320 RGBA bytes are preserved; all files are below Discord's 256 KiB upload
limit. They are embedded in the plugin DLL and uploaded using Discord.Net's
application emoji API. No game asset extraction or external image host is needed.

`ListingDiscordEmojiAssets.cs` maps filenames to application emoji names.
`vip` is job 41 (Viper), `pic` is job 42 (Pictomancer), and `bst` is job 43.
`TH`, `TD`, `HD`, and `THD` represent the allowed tank/healer/damage roles;
`tanker`, `healer`, and `dealer` cover multiple jobs in just one role.
`any` means all runtime battle jobs and classes are allowed. The filename prefix `checked_` means a
participant occupies the slot, never that an editing option is selected.

Discord names use full English names, underscores, and `_job`, `_class`, or
`_role` suffixes; occupied variants add `join_`. For example, `pld.png` is
`paladin_job`, `gla.png` is `gladiator_class`, `TH.png` is `tank_healer_role`, and
`checked_TH.png` is `join_tank_healer_role`. The longest name is 28 characters;
all 80 names fit the 2–32 character limit without abbreviations.

Only the exact 74 legacy `pvkr_` names emitted by 0.3.45 are renamed in place.
Current names take priority if both spellings exist. IDs and image bytes are
preserved; unrelated emojis are never modified or deleted. Keep runtime IDs
and bot tokens out of this directory; each bot discovers its own emoji IDs.
