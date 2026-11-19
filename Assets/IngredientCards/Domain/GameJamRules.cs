using System.Collections.Generic;
using UnityEngine;

namespace CookingCards
{
    /// <summary>Short five-draw bags: three Midori and two Umi, independently weighted by tier.</summary>
    public static class GameJamRules
    {
        public const float FirstTierChance = .90f;
        public const float SecondTierChance = .08f;
        public const float ThirdTierChance = .02f;
        public const float MidoriJadeChance = .60f;
        public static readonly IReadOnlyList<IngredientKind> Ingredients = System.Array.AsReadOnly(new[]
        {
            IngredientKind.Rice, IngredientKind.Nori, IngredientKind.Avocado,
            IngredientKind.Shrimp, IngredientKind.Tuna, IngredientKind.Salmon
        });

        public static bool Allows(IngredientKind kind)
        { foreach (var allowed in Ingredients) if (kind == allowed) return true; return false; }

        public static IngredientKind SampleIngredient(float groupSample, float tierSample)
        {
            int tier = tierSample < FirstTierChance ? 0 : tierSample < FirstTierChance + SecondTierChance ? 1 : 2;
            if (groupSample < MidoriJadeChance)
                return tier == 0 ? IngredientKind.Rice : tier == 1 ? IngredientKind.Nori : IngredientKind.Avocado;
            return tier == 0 ? IngredientKind.Shrimp : tier == 1 ? IngredientKind.Tuna : IngredientKind.Salmon;
        }

        // Bags span hand refills; hand capacity stays seven. No guaranteed Rice.
        // Merge/split outputs do not advance the draw sequence.
        public static List<IngredientKind> CreateDrawBatch()
        {
            var batch = new List<IngredientKind>();
            for (int i = 0; i < 5; i++)
                batch.Add(SampleIngredient(i<3?0:1,Random.value));
            for (int i = batch.Count - 1; i > 0; i--)
            { int j = Random.Range(0, i + 1); var card = batch[i]; batch[i] = batch[j]; batch[j] = card; }
            return batch;
        }
    }
}
