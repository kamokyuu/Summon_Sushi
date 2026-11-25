# P0.2.1 — Runtime configuration and beginner entry hotfix

Branch: `feature/gameplay-rebalance`.

## Root cause

P0.2's Level01 asset was correct, but normal navigation did not select it. `MainMenuController.ShowPanel(0)` calls `PlayLevelOne()`, which called `PreparationSession.BeginVerticalSlice()` and loaded MainScene. That method only reset session data. `ActiveLevel.Config` then silently loaded LegacyDefault when no explicit selection existed. Manual scene Play had the same fallback.

LegacyDefault intentionally has Avocado/Salmon in its normal distribution and Ebi Nigiri with 2 Rice + 1 Shrimp. Its migrated Rice share is 31.25%, not Level01's 45%. Thus isolated Level01 tests passed while the actual game used a different balance profile. There was no hidden recipe or draw override defeating an already selected Level01; the missing selection was the fault.

The map is a distinct path: its existing button is wired to `EnterTutorial`, which calls `PreparationSession.Begin()` and launches the scripted legacy tutorial. It has no normal numbered-level node. Also, LevelMap is absent from enabled build scenes; the shipped scene list currently contains only MainMenu and MainScene. No map node or build setting was changed.

## Changes

- `Assets/MainMenu/MainMenuController.cs`: normal Play explicitly calls `BeginBeginnerLevel()` before loading gameplay.
- `Assets/IngredientCards/Domain/PreparationSession.cs`: beginner entry resets the session and selects the existing Level01 asset; missing Level01 fails explicitly. `BeginVerticalSlice()` now explicitly selects LegacyDefault for regression use. Normal Replay retains its current configuration instead of reverting to legacy. Tutorial entry remains unchanged.
- `Assets/IngredientCards/ActiveLevel.cs`: unselected normal gameplay defaults to Level01; only a scripted tutorial falls back to LegacyDefault. Explicit regression selection remains supported. Added selection-source tracking and runtime diagnostics on gameplay scene loads.
- `Assets/IngredientCards/LevelSelection.cs`: an empty optional scene hook no longer erases a menu selection; explicit non-null overrides still work and are identified in diagnostics.
- `Assets/IngredientCards/Editor/LevelConfigSetup.cs`: added **Tools > Cooking Cards > Log Active Level Configuration**.
- This report plus historical-report supersession notes.

No LevelConfig asset data, ingredient rules, scenes, prefabs, UI, artwork, deposited note implementation, build settings, or knife charges were changed. No timed/automatic draw mechanism was introduced. The existing P0.1 draw/deal behavior was left intact.

## Runtime diagnostics

Unity Console receives `[Summon Sushi Level]` when a gameplay scene loads. It includes:

- Active asset and selection source (beginner entry, normal default, replay, explicit override, or scripted tutorial fallback).
- Tutorial mode; active draw mode; initial hand and capacity.
- Merge/knife flags and initial charges.
- Active ingredients, normalized draw-weight percentages, plate weights.
- Active recipe IDs, grades and ingredient quantities.

Use the Editor menu above to print the snapshot again. `ActiveLevel.Diagnostics()` returns the same text. Scripted tutorial snapshots identify the finite deck and label normal-play weights as such; tutorial deck supply is not misrepresented as random weighted drawing. No new HUD was added.

## Actual entry-flow tests

Tests ran against existing scenes and their actual Unity button callbacks, not just direct config sampling. No changes were saved to those scenes.

1. Opened MainMenu, entered Play, raycasted the visible PLAY button and dispatched its pointer-click event. MainScene loaded with source `Normal beginner entry`, Level01, five allowed starting cards, merge/knife disabled and zero knife charges.
2. Runtime diagnostics reported Common=1; Rice 45%, Shrimp 30%, Eggs 25%; only those three active ingredients; Ebi = 1 Rice + 1 Shrimp, Tamago = 1 Rice + 1 Eggs.
3. Clicked the live Draw button 30 times, freeing hand capacity as needed. Observed 13 Rice, 13 Shrimp, 4 Eggs and zero other ingredients. This small stochastic sample is not a replacement for the exact 45/30/25 configuration/distribution test in P0.2.
4. Verified every plate on that beginner conveyor was Common. Journal listed the two Common recipes. Both recipes were deposited, appeared correctly in ingredient notes, summoned onto Common plates and cleared the consumed stock notes.
5. Clicked the actual results REPLAY button. The new gameplay instance retained Level01 with source `Replay` and a five-card opening.
6. A normal runtime load attempt of LevelMap exposed its existing missing-build-scene registration. Tested the map via `EditorSceneManager.LoadSceneInPlayMode` without modifying build settings, moved the map character to the existing tutorial node, and clicked its existing Enter button. It loaded MainScene in LegacyDefault/scripted tutorial as intended.
7. Opened MainScene directly in the Editor and pressed Play without adding a selector or changing Inspector values. Verified Level01 with source `Normal beginner default` and a five-card opening.
8. Ran the P0.2 ingredient/configuration regression suite after the entry changes; checked compilation and whitespace.

## Remaining limitations

- Common-only conveyor plates are intentional for Level01, not a spawn-distribution bug.
- The map's Tutorial node still uses legacy formulas/decks, including its 2-Rice tutorial Ebi recipe. It is not normal Level01. Normal PLAY now bypasses that ambiguity with explicit beginner selection.
- LevelMap is not included in the current build scene list. This existing navigation/build issue is documented rather than changed outside scope.
- Weighted draws remain stochastic. Even with Rice at 45%, a short opening hand can contain no Rice; no guaranteed-Rice rule or knife-based scarcity workaround was added.
- Existing P0.2 artwork placeholders and grade-based food presentation remain unchanged.
- No player/WebGL build or full tutorial progression walkthrough was performed.
