using UnityEngine;
namespace CookingCards
{
    [CreateAssetMenu(menuName="Summon Sushi/Plate Grade")]
    public sealed class PlateDefinition:ScriptableObject
    {
        [SerializeField] private RecipeGrade grade;
        [SerializeField] private Color color=Color.white;
        public RecipeGrade Grade=>grade;
        public Color Color=>color;
        public bool Accepts(SushiRecipe recipe)=>recipe!=null && recipe.Service==RecipeService.Conveyor && recipe.Grade==grade;
        public void Configure(RecipeGrade value,Color tint){grade=value;color=tint;}
    }
}
