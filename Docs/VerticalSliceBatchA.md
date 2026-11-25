# Vertical Slice Batch A

Implemented on `feature/gameplay-rebalance`, November 25, 2026.

## Runtime balancing

Normal levels use `RecipeDemandShuffleBag`. For each active recipe, its ingredient quantities are multiplied by its Inspector `Demand Weight`; the resulting demand is summed by ingredient. Each bag guarantees at least one token for every demanded ingredient. Remaining slots approximate the normalized demand, then the whole bag is shuffled. The hand consumes a complete bag before refilling it. Opening hands and manual draws share the same supply. A short opening hand can still lack a particular ingredient.

Edit the level assets under `Assets/Resources/Levels`: recipe quantities, recipe demand weights, bag size, manual draw cooldown, exchange cooldown, hand limits, plate weights/interval, duration, revenue target and next-level reference. Independent ingredient weights apply only to the retained `WeightedActiveIngredients` mode. Diagnostics show the effective bag percentages for demand mode.

Manual draws have a three-second cooldown, displayed below the draw button. Opening cards retain their animated 0.25-second deal interval. There are no automatic timed draws.

When the hand is full, select a card and press EXCHANGE. Exactly one selected card is consumed and replaced. The replacement supplies a missing ingredient of the nearest-complete active recipe, considering the remaining hand and deposited stock; otherwise it uses the normal bag. Exchange has a three-second cooldown, grants no extra card or knife charge, and delays the next manual draw by the same interval. This deliberately bypasses bag supply only for controlled deadlock recovery. Inactive ingredients cannot be selected as replacements.

## Levels

| Level | Recipes | Plates | Interval | Merge / knife | Shift / target |
|---|---|---|---|---|---|
| 01 | Existing Ebi Nigiri and Tamago Nigiri, unchanged (1 Rice + 1 Shrimp/Egg) | Common | 8s | Off / off | 180s / 900 yen |
| 02 | Level01 plus Avocado Maki (Rice + Nori + Avocado) and Maguro Nigiri (Rice + Tuna) | Common, Uncommon, 1:1 | 9s | Off / off | 210s / 1200 yen |
| 03 | Level02 plus Salmon Nigiri (Rice + 2 Salmon) and Tuna Avocado Maki (Rice + Tuna + Nori + Avocado) | Common, Uncommon, Rare, 1:1:1 | 10s | On / on, 3 charges | 240s / 1600 yen |

Every level starts with five cards and a seven-card capacity. Level01's 20-card bag contains 10 Rice, 5 Shrimp, 5 Egg: recipe-demand weighting intentionally replaces the previous independent 45/30/25 distribution. Plate weights initially reflect the summed recipe demand weights per grade and remain independently editable in Inspector. Grade weights are not automatically overwritten when balancing existing assets.

The existing shift timer and net-revenue result conditions remain authoritative. A successful result displays NEXT LEVEL and uses its existing button to load the configured next level. Failure and the final level offer REPLAY; replay retains the selected level. Main-menu PLAY starts Level01. No new map nodes were added.

The journal, Summon Box, notes and hand resource rules continue to read ActiveLevel. No parallel recipe catalog was introduced. Combo, power-ups and bosses remain disabled.

## Migration and files

The one-time `VerticalSliceBatchSetup` migration extends Level01, creates Level02/03, and preserves existing Level01 recipe definitions. Subsequent editor reloads do not reset Inspector balancing. `LegacyDefault` retains its separate draw mode, legacy conveyor, manual drawing, recipes, features and tutorial behavior. Ingredient IDs and existing asset references are unchanged.

Core Batch A files: `LevelConfig.cs`, `RecipeDemandBag.cs`, `ActiveLevel.cs`, `IngredientCardHand.cs`, `DayShiftController.cs`, `ResultsNavigation.cs`; Editor setup/validation in `VerticalSliceBatchSetup.cs` and `VerticalSlicePlayValidation.cs`; Level01/02/03 assets; MainScene exchange button and cooldown text. `IngredientRebalanceValidation` explicitly selects its original independent-weight mode when testing invalid weights. The commit also includes the previously accepted P0.1–P0.2.1 source/assets/docs that were still uncommitted in this checkout.

## Validation

- Live Unity compilation succeeded.
- Batch configuration validation: 300 bags across three levels; bag size, active ingredients, recipe availability, plate grades, cooldowns, feature toggles and next-level references.
- Existing ingredient/configuration regression suite passed, including LegacyDefault recipes/tutorial data, serialized IDs, basic ingredient exclusions and both merge chains.
- Play mode entered the existing MainMenu PLAY callback, verified five opening cards, blocked immediate repeated manual draws, and unlocked drawing after 3.1 seconds.
- Full-hand all-Shrimp fixture required a selection, exchanged into missing Rice without increasing hand size, and blocked immediate repeated exchange.
- All twelve configured recipe entries were deposited, checked against visible ingredient notes and journal entries, summoned onto matching-grade plates, purchased, and verified to consume/clear stock.
- Actual results button callbacks advanced Level01 → Level02 → Level03. Sales and shift expiry were accelerated through existing gameplay methods for this navigation test; full-duration balance was not playtested.
- Level03 Shrimp merge to Tuna and Tuna split back to Shrimp passed through hand operations.

Repeat in Editor: Tools → Cooking Cards → Validate Vertical Slice Batch A. For the full runtime check, enter Play mode and invoke `await CookingCards.Editor.VerticalSlicePlayValidation.Run()` through script eval. This test replaces the runtime session and leaves final results visible; exit Play mode afterwards.

## Limitations

Existing food artwork is grade-based rather than unique per recipe. Existing Cucumber/Tobiko/Ikura placeholders remain; these ingredients are unused by the three shipped level recipes. The existing LevelMap remains outside build settings; progression uses the existing MainMenu/MainScene/results flow. No player build, full-duration difficulty assessment, or complete tutorial walkthrough was performed. Exchange intentionally assists resource completion; tune its cooldown and recipe demand weights after player feedback.
