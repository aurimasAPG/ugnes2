using System.Collections.Generic;

namespace HiddenValley.Core
{
    /// <summary>
    /// Crafting. Recipes are known individually but granted in families, which is how one
    /// NPC hands over a whole branch of capability in a single effect.
    /// </summary>
    public sealed class Crafting
    {
        private readonly GameState _state;
        private readonly QuestEngine _quests;

        public Crafting(GameState state, QuestEngine quests)
        {
            _state = state;
            _quests = quests;
        }

        public List<RecipeDef> KnownRecipes(string station = null)
        {
            var list = new List<RecipeDef>();
            foreach (var recipe in _state.Content.Recipes)
            {
                if (!_state.KnowsRecipe(recipe.Id)) continue;
                if (station != null && recipe.Station != station) continue;
                list.Add(recipe);
            }
            return list;
        }

        public bool HasInputs(RecipeDef recipe)
        {
            if (recipe == null) return false;
            foreach (var kv in recipe.Inputs)
                if (_state.ItemCount(kv.Key) < kv.Value) return false;
            return true;
        }

        public bool CanCraft(string recipeId)
        {
            var recipe = _state.Content.Recipe(recipeId);
            if (recipe == null) return false;
            return _state.KnowsRecipe(recipeId) && HasInputs(recipe);
        }

        public bool Craft(string recipeId)
        {
            if (!CanCraft(recipeId)) return false;

            var recipe = _state.Content.Recipe(recipeId);
            foreach (var kv in recipe.Inputs)
                _state.RemoveItem(kv.Key, kv.Value);

            _state.AddItem(recipe.OutputItem, recipe.OutputCount);
            _quests.Settle();
            return true;
        }
    }
}
