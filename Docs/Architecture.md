# Summon Sushi architecture

## First migration

RecipeRules contains cooking rules without scene or UI dependencies. IngredientStove owns the scene interaction and calls those rules. Existing component names, serialized fields, asset paths and recipe priority are preserved.

## Ownership

| Area | Current owner | Next boundary |
| --- | --- | --- |
| Card interactions | IngredientCard | Keep presentation and pointer input here |
| Hand, draw, merge | IngredientCardHand | Extract draw and merge rules separately |
| Cooking | IngredientStove + RecipeRules | Move recipe definitions into configurable data |
| Customers and shift | CustomerSystem | Extract shift state from spawning and HUD |
| Customer presentation | NpcCustomer | Keep movement and order display here |
| Dish interaction | CookedDish | Keep serving input here |

## Scene direction

Use MainMenu and one reusable Gameplay scene. Levels should supply configuration to Gameplay rather than duplicate scenes. Recipe journal, settings and credits can be panels or reusable prefabs. HUD belongs to the gameplay Canvas and reads state; it should not decide cooking or win rules.

## Migration order

1. Extract pure cooking rules (implemented).
2. Extract merge/draw rules after documenting existing behavior.
3. Introduce shift state and events for coins, completion and HUD updates.
4. Introduce recipe and level data assets with current values as defaults.
5. Move assets through AssetDatabase.MoveAsset, retaining GUIDs and updating hardcoded Editor paths together.

Do not rename serialized fields or MonoBehaviour classes during a folder cleanup. Editor setup scripts currently hardcode Assets/IngredientCards/Prefabs and Assets/Asset paths. Moving those directories without updating tools will break setup workflows.

## Validation

Check existing Editor validation commands for customer, merge and stove behavior. Test MainScene draw, merge, deposit, recipe output, serving, completion and restart in Play mode. CLI currently discovers this project's Editor but reports it unreachable; live validation has not yet been completed.
