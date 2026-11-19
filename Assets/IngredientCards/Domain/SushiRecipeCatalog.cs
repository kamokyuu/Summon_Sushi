using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using K = CookingCards.IngredientKind;

namespace CookingCards
{
    public enum RecipeGrade { Common, Uncommon, Rare, Special }
    public enum RecipeFamily { Nigiri, Maki, Gunkan, Temaki, SideDish }
    public enum RecipeService { Conveyor, DirectOrder }

    public readonly struct IngredientAmount
    {
        public IngredientKind Kind { get; }
        public int Count { get; }
        public IngredientAmount(IngredientKind kind,int count)
        { if(count<=0)throw new ArgumentOutOfRangeException(nameof(count));Kind=kind;Count=count; }
    }

    /// <summary>Immutable recipe data. Grade for direct orders is a cost class, not a conveyor plate.</summary>
    public sealed class SushiRecipe
    {
        public string Id { get; }
        public string DisplayName { get; }
        public RecipeGrade Grade { get; }
        public RecipeFamily Family { get; }
        public RecipeService Service { get; }
        public IReadOnlyList<IngredientAmount> Ingredients { get; }
        public int TotalCards { get; }
        public SushiRecipe(string id,string name,RecipeGrade grade,RecipeFamily family,params IngredientAmount[] ingredients)
        {
            Id=id;DisplayName=name;Grade=grade;Family=family;
            Service=family==RecipeFamily.Temaki || family==RecipeFamily.SideDish?RecipeService.DirectOrder:RecipeService.Conveyor;
            var copy=(IngredientAmount[])ingredients.Clone();
            var kinds=new HashSet<IngredientKind>();
            foreach(var amount in copy){if(!kinds.Add(amount.Kind))throw new ArgumentException("Duplicate ingredient in "+id);TotalCards+=amount.Count;}
            Ingredients=Array.AsReadOnly(copy);
        }
        public int Required(IngredientKind kind)
        { foreach(var amount in Ingredients)if(amount.Kind==kind)return amount.Count;return 0; }
        public bool MatchesExactly(IReadOnlyList<int> inventory)
        {
            if(inventory==null || inventory.Count!=IngredientRules.Count)return false;
            for(int i=0;i<inventory.Count;i++)if(inventory[i]!=Required((IngredientKind)i))return false;
            return true;
        }
    }

    /// <summary>Approved 38-menu design catalog. Does not auto-cook or change legacy tutorial recipes.</summary>
    public static class SushiRecipeCatalog
    {
        private static IngredientAmount A(K kind,int count)=>new IngredientAmount(kind,count);
        private static SushiRecipe R(string id,string name,RecipeGrade grade,RecipeFamily family,params IngredientAmount[] amounts)=>new SushiRecipe(id,name,grade,family,amounts);
        public static IReadOnlyList<SushiRecipe> All { get; }=Array.AsReadOnly(new[]
        {
            R("tamago_nigiri","Tamago Nigiri",RecipeGrade.Common,RecipeFamily.Nigiri,A(K.Rice,2),A(K.Eggs,1),A(K.Nori,1)),
            R("ebi_nigiri","Ebi Nigiri",RecipeGrade.Uncommon,RecipeFamily.Nigiri,A(K.Rice,1),A(K.Shrimp,2)),
            R("avocado_nigiri","Avocado Nigiri",RecipeGrade.Uncommon,RecipeFamily.Nigiri,A(K.Rice,1),A(K.Avocado,2)),
            R("kinoko_nigiri","Kinoko Nigiri",RecipeGrade.Uncommon,RecipeFamily.Nigiri,A(K.Rice,1),A(K.Mushroom,2)),
            R("maguro_nigiri","Maguro Nigiri",RecipeGrade.Rare,RecipeFamily.Nigiri,A(K.Rice,1),A(K.Tuna,3)),
            R("matsutake_nigiri","Matsutake Nigiri",RecipeGrade.Rare,RecipeFamily.Nigiri,A(K.Rice,1),A(K.Matsutake,3)),
            R("unagi_nigiri","Unagi Nigiri",RecipeGrade.Rare,RecipeFamily.Nigiri,A(K.Rice,1),A(K.Unagi,3),A(K.Nori,1)),
            R("salmon_nigiri","Salmon Nigiri",RecipeGrade.Special,RecipeFamily.Nigiri,A(K.Rice,1),A(K.Salmon,4)),

            R("kappa_maki","Kappa Maki",RecipeGrade.Common,RecipeFamily.Maki,A(K.Rice,2),A(K.Cucumber,1),A(K.Nori,1)),
            R("tamago_maki","Tamago Maki",RecipeGrade.Uncommon,RecipeFamily.Maki,A(K.Rice,1),A(K.Eggs,2),A(K.Nori,1)),
            R("avocado_maki","Avocado Maki",RecipeGrade.Uncommon,RecipeFamily.Maki,A(K.Rice,1),A(K.Avocado,2),A(K.Nori,1)),
            R("shiitake_maki","Shiitake Maki",RecipeGrade.Uncommon,RecipeFamily.Maki,A(K.Rice,1),A(K.Shiitake,2),A(K.Nori,1)),
            R("ebi_maki","Ebi Maki",RecipeGrade.Uncommon,RecipeFamily.Maki,A(K.Rice,1),A(K.Shrimp,2),A(K.Nori,1)),
            R("cucumber_avocado_roll","Cucumber Avocado Roll",RecipeGrade.Uncommon,RecipeFamily.Maki,A(K.Rice,1),A(K.Cucumber,1),A(K.Avocado,1),A(K.Nori,1)),
            R("chicken_roll","Chicken Roll",RecipeGrade.Uncommon,RecipeFamily.Maki,A(K.Rice,1),A(K.Chicken,1),A(K.Cucumber,1),A(K.Nori,1)),
            R("tekka_maki","Tekka Maki",RecipeGrade.Rare,RecipeFamily.Maki,A(K.Rice,1),A(K.Tuna,3),A(K.Nori,1)),
            R("salmon_maki","Salmon Maki",RecipeGrade.Rare,RecipeFamily.Maki,A(K.Rice,1),A(K.Salmon,3),A(K.Nori,1)),
            R("tuna_cucumber_roll","Tuna Cucumber Roll",RecipeGrade.Rare,RecipeFamily.Maki,A(K.Rice,1),A(K.Tuna,2),A(K.Cucumber,1),A(K.Nori,1)),
            R("tuna_avocado_roll","Tuna Avocado Roll",RecipeGrade.Rare,RecipeFamily.Maki,A(K.Rice,1),A(K.Tuna,2),A(K.Avocado,1),A(K.Nori,1)),
            R("shrimp_avocado_roll","Shrimp Avocado Roll",RecipeGrade.Rare,RecipeFamily.Maki,A(K.Rice,1),A(K.Shrimp,2),A(K.Avocado,1),A(K.Nori,1)),
            R("beef_roll","Beef Roll",RecipeGrade.Rare,RecipeFamily.Maki,A(K.Rice,1),A(K.Beef,2),A(K.Cucumber,1),A(K.Nori,1)),
            R("unagi_avocado_roll","Unagi Avocado Roll",RecipeGrade.Special,RecipeFamily.Maki,A(K.Rice,1),A(K.Unagi,3),A(K.Avocado,1),A(K.Nori,1)),
            R("salmon_avocado_roll","Salmon Avocado Roll",RecipeGrade.Special,RecipeFamily.Maki,A(K.Rice,1),A(K.Salmon,3),A(K.Avocado,1),A(K.Nori,1)),
            R("alaska_roll","Alaska Roll",RecipeGrade.Special,RecipeFamily.Maki,A(K.Rice,1),A(K.Salmon,2),A(K.Avocado,1),A(K.Cucumber,1),A(K.Nori,1)),

            R("tobiko_gunkan","Tobiko Gunkan",RecipeGrade.Rare,RecipeFamily.Gunkan,A(K.Rice,1),A(K.Nori,2),A(K.Tobiko,3)),
            R("ikura_gunkan","Ikura Gunkan",RecipeGrade.Special,RecipeFamily.Gunkan,A(K.Rice,1),A(K.Nori,2),A(K.Ikura,4)),

            R("ebi_temaki","Ebi Temaki",RecipeGrade.Rare,RecipeFamily.Temaki,A(K.Rice,1),A(K.Shrimp,3),A(K.Nori,1)),
            R("maguro_temaki","Maguro Temaki",RecipeGrade.Special,RecipeFamily.Temaki,A(K.Rice,1),A(K.Tuna,4),A(K.Nori,1)),
            R("salmon_temaki","Salmon Temaki",RecipeGrade.Special,RecipeFamily.Temaki,A(K.Rice,1),A(K.Salmon,4),A(K.Nori,1)),
            R("shiitake_temaki","Shiitake Temaki",RecipeGrade.Rare,RecipeFamily.Temaki,A(K.Rice,1),A(K.Shiitake,3),A(K.Nori,1)),

            R("salmon_sashimi","Salmon Sashimi",RecipeGrade.Special,RecipeFamily.SideDish,A(K.Salmon,4)),
            R("maguro_sashimi","Maguro Sashimi",RecipeGrade.Rare,RecipeFamily.SideDish,A(K.Tuna,3)),
            R("mixed_sashimi","Mixed Sashimi",RecipeGrade.Special,RecipeFamily.SideDish,A(K.Salmon,2),A(K.Tuna,2)),
            R("tamagoyaki","Tamagoyaki",RecipeGrade.Common,RecipeFamily.SideDish,A(K.Eggs,1)),
            R("chicken_karaage","Chicken Karaage",RecipeGrade.Uncommon,RecipeFamily.SideDish,A(K.Chicken,2)),
            R("ebi_tempura","Ebi Tempura",RecipeGrade.Uncommon,RecipeFamily.SideDish,A(K.Shrimp,2)),
            R("ebi_chawanmushi","Ebi Chawanmushi",RecipeGrade.Rare,RecipeFamily.SideDish,A(K.Eggs,1),A(K.Shrimp,1),A(K.Shiitake,1)),
            R("matsutake_suimono","Matsutake Suimono",RecipeGrade.Rare,RecipeFamily.SideDish,A(K.Matsutake,3))
        });
        public static SushiRecipe Get(string id)
        {foreach(var recipe in All)if(recipe.Id==id)return recipe;throw new KeyNotFoundException("Unknown recipe: "+id);}
    }
}
