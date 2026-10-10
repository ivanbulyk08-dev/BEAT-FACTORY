# Beat Factory — Audio Assets

All sounds are **original**, synthesized from math by `Tools > Beat Factory > Audio > Regenerate Sounds`
(code: `Scripts/Editor/Audio/Dsp.cs` + `SfxLibrary.cs`). No samples from FL Studio or any other product are used,
so everything can be shipped. Key: **A minor** (A C D E G) at 85 BPM — matches the music loop, so sounds never clash.
Peaks −1.4 to −9 dBFS, no clipping, DC-free, 1 ms fade-in / 60 ms fade-out.

To use your own sound: drag the AudioClip into the matching entry's **Variants** in
`Assets/_BeatFactory/Data/Config/AudioConfig` (scene rebuilds keep your clips).

## Mix (AudioConfig)
Categories: **Production** 0.85 (beats, producers, upgrades) · **UI** 0.7 · **SFX** 0.9 (events) · Music 0.32 · Master 1.
Player sliders (Settings): Master, Music, SFX. VIRAL / Prestige / Studio upgrade / Unlock / HIT briefly duck the music.
12 pooled AudioSources (no cut-offs), anti-spam interval per sound, small pitch variation (≤2%), never the same variation twice in a row.

## Files — `Assets/_BeatFactory/Audio/`
| Event (SoundId) | Files | Content |
|---|---|---|
| MAKE BEAT (Beat) | SFX/Beat/SFX_Beat_Kick808_01, _02 · KickPluck_01, _02 · KickClapStab_01 · KickKeys_01 | kick + 808 / pluck / clap+stab / keys, 0.3–0.5 s |
| GOOD/GREAT beat (BeatGood) | SFX/Beat/SFX_Beat_Great_01, _02 | kick + pluck arpeggio + bell, 0.7 s |
| Hire Beginner | SFX/Producers/SFX_Producer_Beginner_01, _02 | soft kick + rim + keys |
| Hire Melody (also Sound Designer, Hitmaker) | SFX/Producers/SFX_Producer_Melody_01, _02 | pluck chord arpeggio + sparkle |
| Hire Drum (also Sample, Superstar) | SFX/Producers/SFX_Producer_Drums_01, _02 | kick–hat–snare/clap–hat–kick groove |
| Producer upgrade (Upgrade) | SFX/Upgrade/SFX_Upgrade_01–03 | pluck → rising tone → small impact + chord, ~0.9 s |
| Genre / artist / platform (Unlock) | SFX/Upgrade/SFX_Unlock_01 | riser → keys chord + arp + bell |
| Boost | SFX/Upgrade/SFX_Boost_01 | swell → plucks + bell |
| Studio upgrade (StudioTransition) | SFX/Transitions/SFX_Transition_Studio_01 | riser → impact + 808 + stab |
| Prestige | SFX/Transitions/SFX_Prestige_01 | long riser → impact + 808 + keys pad + bells |
| HIT | SFX/Impacts/SFX_Hit_01 | snare roll → kick + clap + 808 + stab |
| VIRAL | SFX/Viral/SFX_Viral_01, _02 | riser + swell → impact + kick + 808 + chord + sparkle |
| Cash out / Money | SFX/Money/SFX_Money_01, _02 | FM ding + pluck + soft sub tick |
| Collect offline | SFX/Money/SFX_Money_Collect_01 | money + keys chord + bell |
| UI click | SFX/UI/SFX_UI_Click_01–03 | soft rim / perc / tiny pluck, 0.07–0.14 s |
| Important button (ClickStrong) | SFX/UI/SFX_UI_Strong_01 | light kick + pluck |
| Tab switch | SFX/UI/SFX_UI_Tab_01, _02 | hat + rim tick |
| Music | Music/MUS_LoFi_Loop_01 (22.6 s loop) | lo-fi 85 BPM |

Buttons whose action already plays a sound have no click (MAKE BEAT, hire/upgrade, unlock rows, COLLECT, CASH OUT).
Producer beats are silent (they happen constantly) except GREAT quality; your own beats always sound.

## Optional: prompts for an AI audio generator (replace placeholders with richer sounds)
- **Beat kick+808**: "0.45 second modern trap one-shot: punchy kick with tight transient layered with a short 808 on A1 (55 Hz) with a quick 3-semitone pitch glide down, clean low end, no reverb, no melody beyond the 808 note, peak −3 dBFS, no clipping, mobile-friendly (audible on phone speakers)."
- **Beat kick+pluck**: "0.45 s hip-hop one-shot: soft punchy kick plus a short warm synth pluck on E4, dry with a touch of room reverb, A minor, gentle, no harsh highs, peak −3 dBFS."
- **Beat kick+clap+stab**: "0.35 s trap one-shot: kick, tight clap, short filtered saw chord stab A minor (A3 C4 E4), clean, punchy, peak −3 dBFS."
- **UI click**: "0.07 s very soft wooden rimshot tick for a mobile UI button, warm, no high-frequency harshness, peak −6 dBFS, must not be annoying after 100 repeats."
- **Producer Melody**: "0.8 s ascending pluck arpeggio A3 C4 E4 G4 with a tiny bell sparkle on top, lo-fi hip-hop, light reverb, peak −4 dBFS."
- **Producer Drums**: "0.75 s trap drum fill at 85 BPM feel: kick, closed hat, snare, hat, kick, tight and clean, peak −4 dBFS."
- **Money**: "0.5 s soft digital FM ding (E5 then A5) with a tiny pluck and sub tick — musical reward, not a cartoon coin, light reverb, peak −5 dBFS."
- **Upgrade**: "0.9 s level-up: short pluck, quick rising synth tone, then a small punchy impact with a bright A minor chord stab, peak −4 dBFS."
- **VIRAL**: "2 s hype moment: 0.6 s filtered noise riser into a big impact with kick, A1 808 and a wide A minor synth chord plus sparkling plucks, modern trap, no vocals, peak −2 dBFS."
- **Music loop**: "Seamless 22–30 s lo-fi hip-hop loop, 85 BPM, A minor (Fmaj7–Em7–Dm7–Cmaj7), Rhodes chords, soft bass, swung drums, subtle vinyl crackle, calm and not distracting, loops perfectly."

## Manual Unity settings
None required. Import settings are applied automatically: SFX = Decompress On Load + ADPCM (instant playback),
music = Streaming + Vorbis (low memory), all mono. An AudioListener is on the Main Camera.
Optional: create an AudioMixer if you want effects on whole groups (Unity has no API to generate mixers from code).
