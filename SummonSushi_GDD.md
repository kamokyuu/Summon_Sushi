# Summon Sushi — Game Design Document / Vertical Slice Specification

> Agent reference. Based on the original 13-page Summon Sushi GDD and subsequent designer decisions through 25 November 2026. This is a revised implementation specification, not a verbatim transcription. Where rules conflict, the latest vertical-slice decisions below take precedence. Do not automatically implement the full campaign scope.

## 1. Game identity
- Title: Summon Sushi
- Genre: Card game, strategy, cooking; single-player 2D first-person PC game.
- Fantasy: Run a magical Japanese sushi restaurant. Drag ingredient cards into a Summon Box, summon sushi, and serve it on moving conveyor plates or to customers.
- Target audience: casual strategy/cozy cooking players aged 13–30.
- Visual direction: cute Japanese-inspired stylized 2D pop art, warm/light colors, satisfying summon/card/knife feedback, cheerful and hectic Japanese-inspired music.
- Language: English. Original business model: premium PC release with free web demo.
- Engine: Unity 6000.3.23f1 (original GDD); artwork authored in Figma; original demo target itch.io.
- References: Balatro, Cake Factory, 2048.

## 2. Design pillars
1. Fast, satisfying actions; do not force idle waiting for ingredients.
2. Meaningful decisions: serve now, merge for better ingredients, save resources, or exchange unusable cards.
3. Learn by doing: introduce recipes and plate colors progressively; minimize memorization using contextual hints and recipe journal.
4. Difficulty comes from decisions and conveyor pressure, not unavoidable resource starvation or mandatory knife use.
5. Multiple recipes per active plate grade; the player chooses which compatible sushi to make.

## 3. Current project and deadline
- Repository: `kamokyuu/Summon_Sushi`; branch `feature/gameplay-rebalance`.
- Last reported working commit: `7c4f440` (Batch B). Verify current HEAD; do not revert newer work.
- Submission/build deadline: 26 November 2026, 04:00 WIB (UTC+7).
- Reported implemented: LevelConfig/ActiveLevel, Level01–03 progression, weighted shuffle bags, manual draw, full-hand exchange, recipe-ID food visuals with named placeholders, contextual skippable tutorials, ingredient deposit notes, base plate prices, two-charge Level03 knife. Zero compilation errors/warnings reported by agent; full-duration balance and player build not yet verified.
- Latest manual feedback: Level02 plate variety, exchange and tutorials feel good; Nori shortage remains; Level03 not yet reached/tested by designer. Do not redesign working tutorial/conveyor/exchange unless needed for a bug.
- Batch C is the next intended scope: resource hotfix, combo, shop/power-ups, achievable final boss, results. Implementation status must be checked against current HEAD.

## 4. Locked vertical-slice gameplay rules
### Cards and draw
- Manual draw, zero gameplay cooldown. Only block duplicate input during existing draw animation.
- Starting hand 5; base maximum hand 7. Extra Hand can increase capacity for the shift.
- Use existing demand-weighted shuffle bags and controlled full-hand exchange. Configure weights per level; opening hands and later draws must use the actual selected LevelConfig.
- Do not draw ingredients irrelevant to active recipes or their accessible merge paths. Ensure enough basic resources, especially Rice and Nori. Avoid flooding early hands with Salmon/Avocado.
- Exchange remains available to recover from unusable full hands; preserve existing implementation.

### Merge and knife — latest designer decision (supersedes P0.2)
- Merge two identical same-tier cards into one next-tier card. Knife reverses a valid merge into two previous-tier cards.
- Midori Jade: Rice → Nori → Avocado. Restore this chain in Level02–03; Level01 has merge disabled. This is intentionally different from the original GDD and the intermediate P0.2 standalone-Rice/Nori design.
- Umi Fortune: Shrimp → Tuna → Salmon.
- Tamago Pearls: Egg (Chicken Egg) → Tobiko → Ikura.
- Cucumber may remain standalone/direct-draw if used by an active recipe; do not add it merely to fill the catalog.
- Level01: merge and knife off. Level02: merge on, knife off. Level03: merge and knife on; 2 initial knife charges, maximum 2 stored; invalid splits never consume charges. Reject splits without sufficient hand capacity safely.
- Nori should also be directly drawable when needed. Merging Rice is an optional recovery/strategy, not a mandatory source of Nori. Knife is optional recovery, not required for routine basic cooking.
- Suggested tier draw starting ratios (not final): L1 100/0/0, L2 80/20/0, L3 75/20/5. These are aggregate guidelines, not a mandate to draw every tier; prioritize actual recipe demand, including standalone ingredients where applicable. Adjust existing weights rather than introducing another draw system.

### Summoning and serving
- Drag cards into Summon Box; validate recipe composition using the active recipe catalog. Journal, tutorial and ingredient notes must read the same data. Show actual deposited ingredients.
- Only matching recipe grades may serve a plate. Multiple recipes may share a grade.
- Preserve recipe-ID-based visuals: matching food sprite when available, otherwise rectangular placeholder with the actual recipe name. Never show a different sushi as fallback merely because grade matches.
- Conveyor plates move continuously outside protected tutorial/shop/freeze/pause states. A missed/empty plate breaks and applies existing configured penalty; served plates must not be penalized.
- Keep existing customer purchase and results/revenue flow. Do not double-credit money on serving and customer purchase.

### Plate prices (base)
| Color | Grade | Price |
|---|---|---:|
| Yellow | Common | ¥120 |
| Red | Uncommon | ¥180 |
| Blue | Rare | ¥260 |
| Black | Special | ¥400 |

### Level progression
| Level | Active grades | Menu size | Teaching focus | Target successful duration |
|---|---|---:|---|---|
| 01 | Common only | 2 | Draw, summon, serve | 30–45 sec |
| 02 | Common, Uncommon | 4 | Journal, plate matching, merge | 60–90 sec |
| 03 | Common, Uncommon, Rare | 6 | Merge/knife, resource strategy, final boss | 90–120 sec |

- Durations are playtest targets, not hard timers or verified results. Use achievable completion targets and existing navigation. Special plates are outside the three-level slice unless already deliberately configured.
- Level01 recipes: Ebi Nigiri = 1 Rice + 1 Shrimp; Tamago Nigiri = 1 Rice + 1 Egg. Level01 draws: Rice 45%, Shrimp 30%, Egg 25%; all others excluded.
- Level02/03 exact active recipe IDs and quantities must be read from existing LevelConfig/catalog. The agent reported 12 level recipe entries across the three levels, not necessarily 12 unique dishes. Do not replace these with the original expensive recipes or invent a new catalog. Preserve accessible recipes and adjust only if needed to fix demonstrated resource scarcity.
- Batch E Level03 Rare alternatives: Salmon Avocado Roll (1 Rice + 1 Nori + 1 Salmon + 1 Avocado), Maguro Nigiri (1 Rice + 1 Tuna), and Tamago Avocado Roll (1 Rice + 1 Nori + 1 Egg + 1 Avocado). Maguro moves to Rare in Level03 only; Level01–02 recipes remain unchanged. Reduce Level03 Rice/Nori draw bias and improve topping availability using the existing demand-weighted bags; every active ingredient must have a useful recipe or merge path.
- Normal PLAY, direct MainScene Play and Replay must select Level01/appropriate replay configuration, never silently fall back to LegacyDefault. Existing LevelMap tutorial node was intentionally legacy and excluded from build scenes in P0.2.1; do not expand scope to a map redesign.

### Tutorial
- One contextual instruction at a time, advance on actual successful actions; allow skip.
- Level01: draw → summon → serve. Level02: journal → plate matching → merge (new step). Level03: reinforce merge and teach knife.
- Supply required ingredients and compatible plates during guided steps. Prevent unfair misses/penalties and softlocks. Preserve existing contextual tutorial and notes; no polish overhaul before deadline.

## 5. Batch C: combo, shop and boss
### Combo (Batch E locked rules)
- Consecutive successful plate fills: 1x +0%, 2x +10%, 3x +20% maximum. Later successful fills remain at 3x.
- Bonus applies to that plate's base price only, not total revenue. Snapshot bonus when serving if payment occurs later at purchase; credit exactly once.
- A 10-second expiration timer refreshes on every successful plate fill. Expiration or a missed/broken plate resets the combo; direct orders do not interrupt it. Shield-protected misses do not reset combo. Pause the timer during guided tutorial pauses, game pause and shop selection.
- Show floating text such as COMBO 3x and a small countdown bar, without a background panel.

### Power-up shop
- Trigger the shop once per streak upon first reaching 3x. Continued fills at 3x do not reopen it; a reset allows a new streak to earn one opening. Show 3 random applicable options (or fewer if fewer eligible), ¥250 each, 10-second choice window. Pause conveyor while choosing; shop timer must still run. Resume correctly on close/timeout.
- Extra Hand: +1 hand slot until shift ends.
- Knife Refill: restore 1 spent knife use, capped at configured maximum; only offer when knife enabled and charge missing.
- Conveyor Freeze: stop conveyor for 30 seconds, allowing merge/summon and other normal gameplay actions. Keep pause reasons independent.
- Plate Shield: protect next otherwise-missed plate from break, penalty and combo reset; consume once.
- Lucky Draw: player chooses 1 allowed resource type from available packs and receives 3 cards of that type; requires 3 free hand slots. Only offer valid active/allowed resources, respecting progression; no disabled premium resources.
- Use existing UI and simple labeled cards. Never charge for an invalid/unusable purchase; prevent duplicate activation and respect funds. Reset temporary effects between levels/shifts.
- Exact UI purchase flow can reuse existing components; original GDD does not specify whether multiple purchases per opening are allowed. For slice, one successful purchase per opening is a proposed simple default if not already implemented.

### Boss and results
- Original GDD: two waves per shift; Wave 1 conveyor customers dominate with occasional direct orders; Wave 2 culminates in boss. Boss types: AYCE (progressive servings) and Sushi Platter (specified multiple dishes).
- Vertical slice: one achievable final Level03 boss using existing customer/order systems and active recipes; no requirement to implement both boss types, a full new wave scheduler or campaign. Reuse existing boss/customer art or labeled placeholder. Avoid impossible recipe requests.
- End with existing results screen, showing earnings/service outcome and progression/replay as appropriate. Boss completion must be connected to final success and not softlock navigation.

### Batch E HUD and customer presentation
- Use cream panels, dark brown typography, consistent HUD spacing, and a small Recipe Book icon toggle. Decorative UI must not block card drag/drop; UI sprite references remain replaceable in Inspector.
- Replace purchase prose with floating +¥amount text. Animate it from the customer toward the revenue HUD over 0.6 seconds while shrinking; animate displayed revenue with subtle arrival bounce/coin feedback. Actual accounting is immediate, credited once, and independent of presentation.
- Reuse existing visits: Regular buys one dish, Picky buys a preferred active plate grade, Hungry buys multiple dishes. Level01 stays primarily Regular; introduce Picky/Hungry in Level02–03. Show minimal non-blocking preference bubbles. HUD/money feedback takes priority over customer complexity.

## 6. Original full-game GDD scope (reference only; not required for deadline)
- Original resource packs: Basic Rice; Umi Fortune (Shrimp/Tuna/Salmon); Tamago Pearls (Chicken Egg/Tobiko/Ikura); Midori Jade (Nori/Cucumber/Avocado); Yaki Feast (Chicken/Unagi/Beef); Kinoko Essence (Enoki/Shitake/Matsutake). Original merge chains followed these orders. Revised slice uses Rice/Nori/Avocado as specified above instead.
- Original conveyor menu families: Nigiri, Maki/Roll, Gunkan. Direct orders: temaki, sashimi, side dishes. Original quantities were intentionally superseded for slice balancing; do not implement them from the old GDD without a new design decision.
- Original shift win: complete 2 waves, income target and boss. Campaign goal: expand restaurant, finish story/events, final shift and savings target.
- Permanent expansion stages: Kedai Awal ¥0; Kedai Nyaman ¥5,000 (Tamago pack); Restoran Berkembang ¥15,000 (Yaki/Kinoko packs); Restoran Favorit ¥30,000; Restoran Impian ¥50,000; final savings ¥100,000 after spending. Original values provisional.
- Modes: Story/Campaign, Endless Rush unlocked after campaign, Web Demo 1–2 initial shifts (original plan; current vertical slice has three gameplay levels).
- Original controls: mouse click/drag; E recipe journal; Q shop during active window; Esc pause/back. Preserve actual current controls unless a required feature needs a binding.
- Deferred: full campaign, restaurant upgrades, new packs, complete original recipe list, event/story expansion, Endless Rush, additional boss variants, polished final art.

## 7. Agent workflow / definition of done
- Deadline and token budget are constrained. Read only relevant files; use existing LevelConfig, domain rules, controllers, UI and validation tools. No full-project audit, new architecture, unrelated refactor or large documentation reports.
- Preserve serialized IDs, asset references, scenes/prefabs, working tutorials, notes, exchange, progression and recipe-specific visuals. Use named placeholders for missing art.
- Prioritize: resource fix → combo → power-ups → boss → focused checks → build. Keep a playable checkpoint; if a feature is unsafe, report the blocker rather than breaking core gameplay.
- Validate actual normal Play flow; Level01–03 progression; recipe/journal parity; Rice/Nori availability; merge/knife gates; combo reward/reset and shop trigger; power-up safety; boss/results; no compile errors. Full-duration playtest and player build smoke test before ZIP submission. Test the actual requested build target; submission format/platform must be confirmed by designer/course requirements.
- Commit changes on current feature branch and report completed tasks, test results and blockers in under 80 words. Do not claim tests that were not run.

## 8. Source and revision notes
- Original source: `Ananda Setiyowati_GDD G2X 2026.docx.pdf`, pp. 2–13 (identity, packs/merge, recipes, plate/combos/shop, waves, progression, controls, modes, tutorial, tech/art/audio).
- Latest rules reflect designer playtests and agreed changes in this conversation. The original GDD is not a runtime source of truth for obsolete recipe quantities or pack progression.
- Implementation status is based on agent reports and designer feedback, not an independent verification of commit 7c4f440 or later. Level02/03 recipe lists, exact current spawn weights, completion targets and asset inventory are intentionally left to the repository rather than guessed.
