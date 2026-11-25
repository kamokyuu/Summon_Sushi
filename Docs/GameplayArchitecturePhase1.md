# Gameplay architecture — Phase 1

Historical P0.1 report. Ingredient rules, draw distributions and Level01 recipes are superseded by [P0.2](IngredientRebalancingP02.md).

Default/normal gameplay entry selection is superseded by [P0.2.1](RuntimeConfigurationHotfixP021.md): Level01 is now automatic for normal play; LegacyDefault remains for tutorials and explicit regressions.

Branch: `feature/gameplay-rebalance`.

## Audit and decisions

- Normal play previously depended on `GameJamRules` (six ingredients, weighted five-draw bags), `VerticalSliceRecipes` (five jam recipes), a seven-card hand, and spatial conveyor slots. The 38-recipe `SushiRecipeCatalog` contains conflicting formulas and grades for some identical IDs. Neither catalog was overwritten.
- Tutorials have a finite, ordered deck, merge/knife/Glutton phases, carry-over supplies, and three distinct cooking formulas. Their order and formulas remain legacy behavior. Their recipes are now explicit `tutorialRecipes` in the legacy asset; tutorial cooking and journal read that same data.
- The level map currently starts tutorial phases, not numbered normal levels. Existing map/menu/scene navigation was intentionally preserved.
- Plates previously spawn by distance travelled, not seconds. Legacy compatibility retains that scheduler (including screen-scale effects), scene deal timing, and immediate manual draws. Non-legacy configs use timed plates and a manual draw cooldown. Gameplay motion and payouts were not rebalanced.
- Existing merge chains, prefabs, artwork, pricing, objectives, UI positions and layouts remain intact. In non-legacy levels, merge/split outputs must belong to the active ingredient list.

## Configuration and entry points

Assets: `Assets/Resources/Levels/LegacyDefault.asset` and `Level01.asset`.

`LevelConfig` exposes editable recipe compositions/grades, active ingredients, weighted plate grades, initial hand size, capacity, draw interval/mode, plate interval, knife charges, and merge/knife/combo/power-up/boss flags. `LevelRecipe` converts serialized authoring data to the existing immutable `SushiRecipe` type. Duplicate IDs, invalid limits, unavailable recipe ingredients/plate grades, and incompatible legacy bags are rejected by validation.

`ActiveLevel` is the single runtime selection boundary. No selection loads `LegacyDefault` from Resources. Select before loading gameplay (mid-round switching is not supported):

```csharp
PreparationSession.BeginVerticalSlice();
ActiveLevel.Select(Resources.Load<LevelConfig>("Levels/Level01"));
UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
```

Alternatively, use the optional `LevelSelection` component on a new scene entry object and assign the asset in Inspector; its early execution selects before hand/knife Awake. No component was added to existing scenes. Tutorial `PreparationSession.Begin()` explicitly returns to legacy configuration. Replay retains the selected normal level. Editor Play startup resets selection to legacy.

Level 01: five initial cards, seven-card capacity, uniform Rice/Shrimp/Eggs/Nori draws, 0.25-second deal/cooldown, Common-only plates every five seconds, no knife/charges, merge enabled. Recipes: jam Ebi Nigiri (2 Rice + 1 Shrimp) and Common Tamago Nigiri (2 Rice + 1 Eggs + 1 Nori). The Ebi recipe deliberately retains the jam Common grade rather than the full catalog's conflicting Uncommon definition.

## Changed code

Added `LevelConfig.cs`, `ActiveLevel.cs`, `LevelSelection.cs`, and `Editor/LevelConfigSetup.cs`, plus the two configuration assets and Unity metadata.

Integrated `IngredientCardHand`, `ConveyorController`, `IngredientStove`, `RecipeJournal`, `CardKnifeTool`, `KnifeDragHandle`, `CustomerSystem`, `Domain/ActiveRecipeRules`, `Domain/RecipeRules`, and `Domain/PreparationSession`.

No scene, prefab, texture, sound, or existing UI asset was edited. Unity/Plastic SCM may independently update `.plastic` bookkeeping; these files are not part of the gameplay implementation.

## Validation

- Editor menu: **Tools > Cooking Cards > Validate Level Configurations**. Runs existing `GameJamRulesValidation` regression coverage, legacy recipe/default checks, 1,000 Level 01 plate/draw samples, journal recipe count, exact recipe deposit/cooking/consumption checks, inactive ingredient rejection, feature defaults, and all three legacy tutorial formulas.
- Separate Editor evaluation verified tutorial entry chooses legacy, the journal lists three tutorial recipes, and `RecipeRules` cooks each unchanged formula.
- Play-mode smoke tests passed: legacy seven-card opening; Level 01 five-card opening; active-only ingredients; immediate duplicate draw blocked by cooldown; draws allowed afterward; seven-card capacity; zero knife charges/disabled knife; timed Common-only plate creation. Tests used temporary runtime clones and restored legacy selection afterward; no test scene changes were saved.
- Compilation and final console status checked through the running Unity Editor. `git diff --check` checked whitespace.

## Remaining limitations

- Combo and power-up flags are authoring reservations: no corresponding gameplay implementation exists. No expansion mechanics were added. Boss flag only gates existing legacy boss customers.
- Level 01 is opt-in, not a new map node. Progressive unlocks, level objectives, reward progression, and editor preview tooling are outside this phase.
- Food artwork on conveyor plates is currently selected by grade, not recipe ID. Two Common recipes therefore share the existing Common-grade presentation. No art substitution was introduced.
- Legacy tutorial supply ordering, instructions, patience timers, and three-phase progression remain scripted. They are not fully authored as independent level assets yet.
- Full browser/WebGL builds and an end-to-end manual tutorial walkthrough were not run in this phase.

Tooling guidance consulted: Unity `using-unity` index, `unity-cli.md`, and `common-workflows.md`.
