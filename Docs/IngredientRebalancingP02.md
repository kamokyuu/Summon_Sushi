# P0.2 — Recipe and ingredient rebalancing

Normal entry/default selection is superseded by the [P0.2.1 runtime configuration hotfix](RuntimeConfigurationHotfixP021.md). Level01 now loads automatically for normal PLAY and direct gameplay scene starts.

Branch: `feature/gameplay-rebalance`. Continues the accepted, uncommitted P0.1 working tree; no prior work was reset.

## Implementation

- Removed Rice → Nori and Nori → Avocado from `IngredientRules.TryMerge`. Reverse knife splitting is derived from the merge table, so Rice, Nori, Cucumber, and Avocado cannot split either. Rice/Nori are independent basic ingredients.
- Preserved Shrimp → Tuna → Salmon and Eggs → Tobiko → Ikura, including reverse knife operations. Other existing chains were not changed.
- Added `WeightedActiveIngredients = 2`, appending to the existing draw-mode enum without renumbering modes 0/1. Ingredient enum IDs and prefab/artwork references were not changed. Egg remains the existing `Eggs` ingredient (ID 2).
- Added Inspector-editable `ingredientWeights` entries (`Ingredient`, `Weight`) to LevelConfig. Weights are relative, normalized at sampling time. Validation rejects inactive/duplicate ingredients, negative/non-finite weights, and zero totals. Runtime sampling also rejects invalid/inactive entries instead of drawing them.
- ActiveLevel draws from those weights. Cucumber and Avocado can be sampled directly whenever a level enables them and gives them positive weight. They are intentionally absent from Level01, whose recipes do not use them. Existing configured merge/knife feature gates remain in force.
- Kept the current deposited ingredient notes implementation. The journal, recipe validation and Summon Box continue to share the active LevelConfig recipe data.
- No timed/automatic draw feature was added. P0.1's existing draw/deal interval behavior was not changed; P0.3 timing work remains separate.

## Level01

| Setting | Value |
|---|---|
| Plates | Common only |
| Ebi Nigiri | 1 Rice + 1 Shrimp |
| Tamago Nigiri | 1 Rice + 1 Eggs |
| Active ingredients | Rice, Shrimp, Eggs |
| Draw weights | 45 / 30 / 25 |
| Merge / knife | Disabled / disabled |

Initial hand, capacity, plate timing, artwork and UI settings remain as accepted in P0.1.

## Legacy migration

`LevelConfigSetup.CreateAssets()` applies a one-time migration to the two named assets when `ingredientBalanceVersion < 2`. It updates existing assets in place, preserving their GUIDs and all references; it does not recreate them. Version 2 prevents subsequent domain reloads from resetting designer weight edits. Custom level assets are not migrated automatically.

- Level01 receives the requested recipes, ingredient set, weights, Common plates and disabled merge/knife.
- LegacyDefault retains its five normal recipes, three tutorial formulas, plate distribution, scene tuning, hand capacity and knife settings. Its normal draw mode changes from the historical quota bag to configurable weighted sampling, because the removed Garden chain cannot be relied on for production.
- LegacyDefault weights are the summed demand of its existing five normal recipe definitions: Rice 5, Nori 2, Avocado 2, Shrimp 1, Tuna 1, Salmon 5. Thus Rice's expected normal draw share drops from 54% to 31.25%. Nori and Avocado are directly drawable. This is an explicit initial migration, not a claim of optimized plate/customer economics; designers can tune each weight.
- Scripted tutorials keep their finite supply/deck and seafood progression. No map nodes or customers were added.
- The old weighted-bag mode and helper remain available for asset compatibility, but neither shipped configuration uses it after migration. Its pure historical-distribution tests still run. The live legacy regression now expects Nori merging/splitting to be rejected.
- The larger SushiRecipeCatalog remains a reference catalog with different historical formulas; Level01 deliberately overrides its recipe IDs through LevelConfig. No unrelated catalog entries were rebalanced.

## Changed files relative to accepted P0.1

- `Assets/IngredientCards/Domain/IngredientRules.cs`
- `Assets/IngredientCards/LevelConfig.cs`
- `Assets/IngredientCards/ActiveLevel.cs`
- `Assets/IngredientCards/Editor/LevelConfigSetup.cs`
- `Assets/IngredientCards/Editor/GameJamRulesValidation.cs`
- New `Assets/IngredientCards/Editor/IngredientRebalanceValidation.cs` and its Unity metadata.
- `Assets/Resources/Levels/Level01.asset`
- `Assets/Resources/Levels/LegacyDefault.asset`
- This report, and a supersession note in the P0.1 report.

No scene, prefab, artwork, audio, or ingredient ID file was modified. Existing P0.1 modifications remain uncommitted on this branch. Plastic SCM bookkeeping may update independently in the Editor.

## Tests run

Editor menu: **Tools → Cooking Cards → Validate P0.2 Ingredient Balance**.

- P0.1 LevelConfig regression and historical jam draw/recipe tests passed.
- Basic-ingredient merge/split exclusions and unchanged serialized IDs passed.
- Seafood and egg chain merges plus reverse splits passed.
- 10,000 evenly spaced probability samples gave exactly 4,500 Rice, 3,000 Shrimp, 2,500 Eggs for Level01.
- Invalid weights (negative, NaN, infinity, inactive ingredient, zero total) were rejected.
- Temporary Cucumber/Avocado configuration validated direct sampling without changing shipped recipe sets.
- Common-only beginner recipe validation, active draws, journal count, exact stock consumption and legacy tutorial recipes passed.
- Play-mode tests passed for the legacy seven-card opening, active-only weighted draws, disabled Level01 merge/knife, and both beginner recipes actually deposited and summoned onto Common plates. Ingredient notes displayed deposits and returned to `BOX STOCK` after consumption.
- Live hand tests passed for both seafood and egg merge/knife chains, with Rice/Nori operations rejected.
- Final compilation/console and `git diff --check` checked. No player build or full manual tutorial walkthrough was performed.

## Artwork audit and limitations

Inspected actual prefab Image references without changing imports or assets:

- Rice, Nori, Avocado, Shrimp, Tuna, Salmon use existing final `Assets/Asset/new card/` artwork.
- Eggs uses existing legacy `Assets/Asset/Card/Eggs_card.png`; a matching final-set Egg card image is missing.
- Cucumber, Tobiko and Ikura prefabs have no assigned sprite artwork (placeholders). Existing Mushroom, Shiitake and Matsutake prefabs also have no assigned sprites.
- Chicken, Unagi and Beef have enum/catalog entries but no corresponding card prefab in the audited folder. They are not active in either shipped configuration.
- Conveyor food presentation is still grade-based. Both Common beginner dishes use the existing Common-grade presentation. Recipe identity/cooking is correct; per-recipe art mapping was not added.
- Weights control expected long-run distribution, not a guarantee within every five-card hand. No guaranteed-Rice draws or automatic recipe-demand solver was added.
- Level01 remains opt-in through the P0.1 selection API/component. UI layout, notes styling, scene entry points, and map progression are unchanged.
