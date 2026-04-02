using System;
using System.Collections.Generic;

namespace Xiuxian.Scripts.Services
{
    /// <summary>
    /// Central registry for all activity systems (alchemy, smithing, garden, mining, etc.).
    /// Provides cross-system lookup by SystemId or RecipeId.
    /// </summary>
    public static class ActivityRegistry
    {
        private static readonly Dictionary<string, IActivityDefinition> Activities = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, IRecipeDefinition> RecipeIndex = new(StringComparer.Ordinal);
        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            RegisterBuiltInActivities();
        }

        public static void Register(IActivityDefinition activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            Activities[activity.SystemId] = activity;
            foreach (IRecipeDefinition recipe in activity.GetRecipes())
            {
                RecipeIndex[recipe.RecipeId] = recipe;
            }
        }

        public static IActivityDefinition? GetBySystem(string systemId)
        {
            EnsureInitialized();
            return Activities.TryGetValue(systemId, out IActivityDefinition? def) ? def : null;
        }

        public static IRecipeDefinition? GetRecipe(string recipeId)
        {
            EnsureInitialized();
            return RecipeIndex.TryGetValue(recipeId, out IRecipeDefinition? def) ? def : null;
        }

        public static IReadOnlyDictionary<string, IActivityDefinition> GetAll()
        {
            EnsureInitialized();
            return Activities;
        }

        /// <summary>Reset state — intended for testing only.</summary>
        public static void ResetForTesting()
        {
            Activities.Clear();
            RecipeIndex.Clear();
            _initialized = false;
        }

        private static void RegisterBuiltInActivities()
        {
            RegisterAlchemy();
            RegisterSmithing();
            RegisterGarden();
            RegisterMining();
            RegisterFishing();
            RegisterTalisman();
            RegisterCooking();
            RegisterFormation();
            RegisterBodyCultivation();
        }

        private static void RegisterAlchemy()
        {
            var alchemy = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeAlchemy,
                DisplayName = "炼丹",
                Category = ActivityCategory.Processing,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            foreach (AlchemyRules.RecipeSpec spec in AlchemyRules.GetRecipes())
            {
                alchemy.AddRecipe(new SimpleRecipeDefinition
                {
                    RecipeId = spec.RecipeId,
                    SystemId = PlayerActionState.ModeAlchemy,
                    DisplayName = spec.DisplayName,
                    Inputs = new[] { new MaterialCost(spec.MaterialItemId, spec.MaterialCount) },
                    LingqiCost = spec.LingqiCost,
                    RequiredInputEvents = spec.RequiredInputs,
                    Outputs = new[] { new MaterialOutput(spec.OutputPotionId, spec.OutputCount) },
                    RequiredMasteryLevel = spec.RequiredMasteryLevel,
                });
            }

            Register(alchemy);
        }

        private static void RegisterSmithing()
        {
            // Smithing is level-based (not fixed-recipe). We register a single placeholder
            // recipe representing the base enhancement tier so the system is discoverable.
            var smithing = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeSmithing,
                DisplayName = "炼器",
                Category = ActivityCategory.Processing,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            SmithingCost baseCost = SmithingRules.GetCost(0);
            smithing.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "smithing_enhance_base",
                SystemId = PlayerActionState.ModeSmithing,
                DisplayName = "基础强化",
                Inputs = new[]
                {
                    new MaterialCost("lingqi_shard", baseCost.Shards),
                },
                LingqiCost = baseCost.Lingqi,
                RequiredInputEvents = baseCost.RequiredInputs,
                Outputs = System.Array.Empty<MaterialOutput>(),
                RequiredMasteryLevel = 1,
            });

            Register(smithing);
        }

        private static void RegisterGarden()
        {
            var garden = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeGarden,
                DisplayName = "灵田",
                Category = ActivityCategory.Gathering,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            foreach (GardenRules.CropSpec crop in GardenRules.GetCrops())
            {
                garden.AddRecipe(new SimpleRecipeDefinition
                {
                    RecipeId = crop.RecipeId,
                    SystemId = PlayerActionState.ModeGarden,
                    DisplayName = crop.DisplayName,
                    Inputs = System.Array.Empty<MaterialCost>(),
                    LingqiCost = 0.0,
                    RequiredInputEvents = crop.RequiredInputs,
                    Outputs = new[] { new MaterialOutput(crop.OutputItemId, crop.OutputCount) },
                    RequiredMasteryLevel = crop.RequiredMasteryLevel,
                });
            }

            Register(garden);
        }

        private static void RegisterMining()
        {
            var mining = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeMining,
                DisplayName = "矿脉",
                Category = ActivityCategory.Gathering,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            foreach (MiningRules.NodeSpec node in MiningRules.GetNodes())
            {
                mining.AddRecipe(new SimpleRecipeDefinition
                {
                    RecipeId = node.RecipeId,
                    SystemId = PlayerActionState.ModeMining,
                    DisplayName = node.DisplayName,
                    Inputs = System.Array.Empty<MaterialCost>(),
                    LingqiCost = 0.0,
                    RequiredInputEvents = node.RequiredInputs,
                    Outputs = new[] { new MaterialOutput(node.OutputItemId, node.OutputCount) },
                    RequiredMasteryLevel = node.RequiredMasteryLevel,
                });
            }

            Register(mining);
        }

        private static void RegisterFishing()
        {
            var fishing = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeFishing,
                DisplayName = "灵渔",
                Category = ActivityCategory.Gathering,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            foreach (FishingRules.PondSpec pond in FishingRules.GetPonds())
            {
                fishing.AddRecipe(new SimpleRecipeDefinition
                {
                    RecipeId = pond.RecipeId,
                    SystemId = PlayerActionState.ModeFishing,
                    DisplayName = pond.DisplayName,
                    Inputs = System.Array.Empty<MaterialCost>(),
                    LingqiCost = 0.0,
                    RequiredInputEvents = pond.RequiredInputs,
                    Outputs = new[] { new MaterialOutput(pond.OutputItemId, pond.OutputCount) },
                    RequiredMasteryLevel = pond.RequiredMasteryLevel,
                });
            }

            Register(fishing);
        }

        private static void RegisterTalisman()
        {
            var talisman = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeTalisman,
                DisplayName = "符箓",
                Category = ActivityCategory.Processing,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            talisman.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "talisman_fire_charm",
                SystemId = PlayerActionState.ModeTalisman,
                DisplayName = "火符",
                Inputs = new[] { new MaterialCost("broken_talisman", 2), new MaterialCost("spirit_ink", 1) },
                LingqiCost = 0.0,
                RequiredInputEvents = 180,
                Outputs = new[] { new MaterialOutput("talisman_fire_charm", 1) },
                RequiredMasteryLevel = 1,
            });
            talisman.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "talisman_shield_charm",
                SystemId = PlayerActionState.ModeTalisman,
                DisplayName = "盾符",
                Inputs = new[] { new MaterialCost("broken_talisman", 3), new MaterialCost("beast_bone", 1) },
                LingqiCost = 0.0,
                RequiredInputEvents = 220,
                Outputs = new[] { new MaterialOutput("talisman_shield_charm", 1) },
                RequiredMasteryLevel = 2,
            });

            Register(talisman);
        }

        private static void RegisterCooking()
        {
            var cooking = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeCooking,
                DisplayName = "烹饪",
                Category = ActivityCategory.Processing,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            cooking.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "cooking_spirit_porridge",
                SystemId = PlayerActionState.ModeCooking,
                DisplayName = "灵鱼粥",
                Inputs = new[] { new MaterialCost("spirit_fish", 2), new MaterialCost("spirit_herb", 1) },
                LingqiCost = 40.0,
                RequiredInputEvents = 200,
                Outputs = new[] { new MaterialOutput("food_spirit_porridge", 1) },
                RequiredMasteryLevel = 1,
            });
            cooking.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "cooking_fruit_jelly",
                SystemId = PlayerActionState.ModeCooking,
                DisplayName = "灵果蜜饯",
                Inputs = new[] { new MaterialCost("spirit_fruit", 2), new MaterialCost("spirit_flower", 1) },
                LingqiCost = 60.0,
                RequiredInputEvents = 240,
                Outputs = new[] { new MaterialOutput("food_fruit_jelly", 1) },
                RequiredMasteryLevel = 2,
            });
            cooking.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "cooking_dragon_soup",
                SystemId = PlayerActionState.ModeCooking,
                DisplayName = "龙涎鱼汤",
                Inputs = new[] { new MaterialCost("spirit_fish", 1), new MaterialCost("dragon_saliva", 1) },
                LingqiCost = 100.0,
                RequiredInputEvents = 280,
                Outputs = new[] { new MaterialOutput("food_dragon_soup", 1) },
                RequiredMasteryLevel = 4,
            });

            Register(cooking);
        }

        private static void RegisterFormation()
        {
            var formation = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeFormation,
                DisplayName = "阵法",
                Category = ActivityCategory.Processing,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            formation.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "formation_spirit_plate",
                SystemId = PlayerActionState.ModeFormation,
                DisplayName = "聚灵阵盘",
                Inputs = new[] { new MaterialCost("spirit_jade", 3), new MaterialCost("dragon_saliva", 1) },
                LingqiCost = 0.0,
                RequiredInputEvents = 260,
                Outputs = new[] { new MaterialOutput("formation_spirit_plate", 1) },
                RequiredMasteryLevel = 1,
            });
            formation.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "formation_guard_flag",
                SystemId = PlayerActionState.ModeFormation,
                DisplayName = "护体阵旗",
                Inputs = new[] { new MaterialCost("cold_iron_ore", 2), new MaterialCost("spirit_jade", 1) },
                LingqiCost = 0.0,
                RequiredInputEvents = 220,
                Outputs = new[] { new MaterialOutput("formation_guard_flag", 1) },
                RequiredMasteryLevel = 2,
            });
            formation.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "formation_harvest_array",
                SystemId = PlayerActionState.ModeFormation,
                DisplayName = "丰饶阵盘",
                Inputs = new[] { new MaterialCost("spirit_flower", 2), new MaterialCost("spirit_pearl", 1) },
                LingqiCost = 0.0,
                RequiredInputEvents = 240,
                Outputs = new[] { new MaterialOutput("formation_harvest_array", 1) },
                RequiredMasteryLevel = 3,
            });
            formation.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "formation_craft_array",
                SystemId = PlayerActionState.ModeFormation,
                DisplayName = "工巧阵盘",
                Inputs = new[] { new MaterialCost("mithril", 1), new MaterialCost("spirit_ink", 2) },
                LingqiCost = 0.0,
                RequiredInputEvents = 260,
                Outputs = new[] { new MaterialOutput("formation_craft_array", 1) },
                RequiredMasteryLevel = 4,
            });

            Register(formation);
        }



        private static void RegisterBodyCultivation()
        {
            var bodyCultivation = new SimpleActivityDefinition
            {
                SystemId = PlayerActionState.ModeBodyCultivation,
                DisplayName = "体修",
                Category = ActivityCategory.Cultivation,
                SupportsOffline = true,
                OfflineEfficiency = 0.5,
            };

            bodyCultivation.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "body_cultivation_temper",
                SystemId = PlayerActionState.ModeBodyCultivation,
                DisplayName = "淬体",
                Inputs = new[] { new MaterialCost("beast_bone", 2) },
                LingqiCost = 300.0,
                RequiredInputEvents = 260,
                Outputs = System.Array.Empty<MaterialOutput>(),
                RequiredMasteryLevel = 1,
            });
            bodyCultivation.AddRecipe(new SimpleRecipeDefinition
            {
                RecipeId = "body_cultivation_boneforge",
                SystemId = PlayerActionState.ModeBodyCultivation,
                DisplayName = "炼骨",
                Inputs = new[] { new MaterialCost("cold_iron_ore", 2) },
                LingqiCost = 500.0,
                RequiredInputEvents = 320,
                Outputs = System.Array.Empty<MaterialOutput>(),
                RequiredMasteryLevel = 2,
            });

            Register(bodyCultivation);
        }
    }
}
