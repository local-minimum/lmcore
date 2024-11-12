using LMCore.AbstractClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LMCore.Inventory
{
    [System.Serializable]
    public struct Ingredient
    {
        public string Id;
        public int Amount;

        public Ingredient(string id, int amount = 1)
        {
            Id = id;
            Amount = amount;
        }

        public static IEnumerable<Ingredient> From(IEnumerable<AbsItem> items) => items
            .Where(i => i != null && i.Type.HasFlag(ItemType.Consumable))
            .GroupBy(i => i.Id)
            .Select(g =>  new Ingredient(g.Key, g.Count()));
    }

    [System.Serializable]
    public struct Tool
    {
        public string Id;
        public int Amount;
        public bool Consumes;

        public Tool(string id, int amount = 1, bool consumes = false)
        {
            Id = id;
            Amount = amount;
            Consumes = consumes;
        }

        public static IEnumerable<Tool> From(IEnumerable<AbsItem> items, bool consumes = false) => items
            .Where(i => i != null && i.Type.HasFlag(ItemType.Tool))
            .GroupBy(i => i.Id)
            .Select(g => new Tool(g.Key, g.Count(), consumes));
    }

    [System.Serializable]
    public struct RecipeEffect
    {
        public int Health;

        public bool Instant;
        public float OverTime;

        public override string ToString()
        {
            if (Instant)
            {
                if (Health == 0)
                {
                    return "No effect";
                }
                else if (Health > 0)
                {
                    return $"Heals +{Health}";
                }
                else
                {
                    return $"Damages -{Health}";
                }
            }
            else
            {
                if (Health == 0)
                {
                    return "No effect";
                }
                else if (Health > 0)
                {
                    return $"Heals +{Health} over {OverTime}s";
                }
                else
                {
                    return $"Damages -{Health} over {OverTime}s";
                }
            }
        }
    }

    [System.Serializable]
    public class Recipe
    {
        [SerializeField]
        public string Id;
        [SerializeField]
        public string Name;

        [SerializeField]
        List<Ingredient> ingredients = new List<Ingredient>();
        [SerializeField]
        List<Tool> tools = new List<Tool>();

        [SerializeField]
        List<RecipeEffect> effects = new List<RecipeEffect>();

        public int TotalHealing => effects.Sum(e => e.Health);

        public bool CanMakeWith(List<Ingredient> availableIngredients, List<Tool> availableTools) =>
            ingredients.All(ing => availableIngredients.Any(aIng => aIng.Id == ing.Id && ing.Amount <= aIng.Amount)) &&
            tools.Any(tool => availableTools.Any(aTool => aTool.Id == tool.Id && tool.Amount <= aTool.Amount));
   
        public IEnumerable<Ingredient> Ingredients => ingredients;
        public IEnumerable<Tool> Tools => tools;

        public struct RecipeSummary
        {
            public string Action;
            public string Description;
            public List<string> Requires;
            public List<string> Effects;

            public override string ToString()
            {
               return $"{Action}\n" +
                    $"{(Description != null ? $"{Description}\n" : "")}" +
                    $"{(Requires != null ? $"Ingredients: {string.Join(", ", Requires)}" : "")}" +
                    $"-> {string.Join(", ", Effects)}";
            }
        }

        public RecipeSummary Summary(string character)
        {
            if (ingredients.Count == 1)
            {
                return new RecipeSummary()
                {
                    Action = $"{character} ate {Name}",
                    Requires = tools.Count() == 0 ? null : tools.Select(t => t.Id).ToList(),
                    Effects = effects.Select(e => e.ToString()).ToList(),
                };
            } else
            {
                // TODO: We need better language support
                return new RecipeSummary()
                {
                    Action = $"{character} made and ate {Name}",
                    Description = string.Join(", ", ingredients.Select(i => $"{i.Amount} {i.Id}")),
                    Requires = tools.Count() == 0 ? null : tools.Select(t => t.Id).ToList(),
                    Effects = effects.Select(e => e.ToString()).ToList(),
                };
            }
        }

        public string Humanize(string character) => 
            Summary(character).ToString();
    }

    public class RecipeCollection : Singleton<RecipeCollection, RecipeCollection>
    {
        [SerializeField]
        List<Recipe> recipes = new List<Recipe>();

        public IEnumerable<Recipe> GetRecipesFor(IEnumerable<AbsItem> consumables, IEnumerable<AbsItem> tools) {
            var potentialIngredients = Ingredient.From(consumables).ToList();
            var potentialTools = Tool.From(tools).ToList();

            return recipes
                .Where(r => r.CanMakeWith(potentialIngredients, potentialTools));
        }
    }
}
