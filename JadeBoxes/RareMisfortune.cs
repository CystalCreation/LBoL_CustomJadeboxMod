using HarmonyLib;
using LBoL.Base;
using LBoL.Base.Extensions;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Cards;
using LBoL.Core.Randoms;
using LBoL.Core.Stations;
using LBoL.Core.StatusEffects;
using LBoL.Core.Units;
using LBoL.EntityLib.Cards.Character.Marisa;
using LBoL.EntityLib.Cards.Neutral.Red;
using LBoL.EntityLib.Cards.Other.Enemy;
using LBoL.EntityLib.Cards.Other.Misfortune;
using LBoL.EntityLib.Exhibits.Common;
using LBoL.EntityLib.Exhibits.Shining;
using LBoL.EntityLib.JadeBoxes;
using LBoL.EntityLib.StatusEffects.Others;
using LBoL.Presentation;
using LBoL.Presentation.UI.Panels;
using LBoLEntitySideloader;
using LBoLEntitySideloader.Attributes;
using LBoLEntitySideloader.Entities;
using LBoLEntitySideloader.Resource;
using LBoLEntitySideloader.TemplateGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static CustomJadebox.BepinexPlugin;

namespace CustomJadebox.JadeBoxes
{
    public class RareMisfortune
    {
        public sealed class RareMisfortuneDef : JadeBoxTemplate
        {
            private static int newRareChance = 5;


            public override IdContainer GetId()
            {
                return nameof(RareMisfortuneJadebox);
            }


            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Power Bait" },
                { "Description", "Card rewards from non-boss combats have a {Value2}" +
                "% chance to be a Rare card. When you gain a Rare card from a non-boss reward screen, add a random common Misfortune to the library."}
            });
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = newRareChance;
                config.Value2 = (100 / newRareChance);
                return config;
            }


            [EntityLogic(typeof(RareMisfortuneDef))]
            public sealed class RareMisfortuneJadebox : JadeBox
            {

                protected override void OnAdded()
                {
                    //needs to be added in OnAdded or else it will be gone after a reload
                    HandleGameRunEvent(GameRun.DeckCardsAdded, OnDeckCardAdded);
                }

                public void OnDeckCardAdded(CardsEventArgs args)
                {
                    bool lastCardRare = base.GameRun.BaseDeck.Last<Card>().Config.Rarity == Rarity.Rare;
                    
                    if (lastCardRare && IsInCombatStation(base.GameRun))
                    {
                        if (IsHinaTriggered(base.GameRun))
                        {
                            return;
                        }
                        var rng = base.GameRun.CardRng;
                        base.GameRun.AddDeckCard(GetRandomCurseCard(rng), true);
                    }
                    
                }

                //Hina Doll is bugged with the custom misfortune gain so it needs to be triggered manually
                private static bool IsHinaTriggered(GameRunController gameRun)
                {
                    bool cancelMisfortune = false;
                    foreach (var item in gameRun.Player.Exhibits)
                    {
                        //Hina Doll
                        if (item is ChuRenou)
                        {
                            if(item.Counter > 0)
                            {
                                item.Counter--;
                                cancelMisfortune = true;
                            }
                        }
                    }

                    return cancelMisfortune;
                }
                

                private static bool IsInCombatStation(GameRunController gameRun)
                {
                    var currentStationType = gameRun.CurrentStation.Type;
                    return (currentStationType == StationType.Enemy) || (currentStationType == StationType.EliteEnemy);
                }

                private static bool IsInBattle(GameRunController gameRun)
                {
                    return gameRun.Battle != null && gameRun.Battle.AllAliveEnemies.Count() > 0;
                }


                //Code copied from GameRunController.GetRandomCurseCard but adjusted to only roll common misfortunes
                private Card GetRandomCurseCard(RandomGen rng)
                {
                    List<Type> list = new List<Type>();
                    foreach (ValueTuple<Type, CardConfig> valueTuple in Library.EnumerateCardTypes())
                    {
                        Type item = valueTuple.Item1;
                        CardConfig config = valueTuple.Item2;
                        if (config.Type == CardType.Misfortune && (!config.Keywords.HasFlag(Keyword.Unremovable)  && config.Rarity == Rarity.Common))
                        {
                            list.Add(item);
                        }
                    }
                    if (list.Count == 0)
                    {
                        Debug.Log("No curse card in library found");
                        return null;
                    }
                    return TypeFactory<Card>.CreateInstance(list.Sample(rng));
                }

                public static bool IsRareMisfortuneJadebox()
                {
                    var run = GameMaster.Instance.CurrentGameRun;
                    IReadOnlyList<JadeBox> jadeBox = run.JadeBox;

                    if (jadeBox != null && jadeBox.Count > 0)
                    {
                        if (run.JadeBox.Any((JadeBox jb) => jb is RareMisfortuneJadebox))
                        {
                            return true;
                        }
                    }
                    return false;
                }


                //overwrite the RollCards method by intercepting its prameters to use RarityWeightTable.OnlyRare in the CardWeightTable during the correct rng roll
                [HarmonyPatch]
                class GameRunController_RollCards_Patch
                {
                    static IEnumerable<MethodBase> TargetMethods()
                    {
                        return AccessTools.GetDeclaredMethods(typeof(GameRunController)).Where(m => m.Name == "RollCards").ToList();
                    }

                    static void Prefix(ref CardWeightTable weightTable, GameRunController __instance)
                    {
                        if (IsRareMisfortuneJadebox() && IsInCombatStation(__instance) && !IsInBattle(__instance))
                        {
                            int number = __instance.CardRng.NextInt(1, newRareChance);
                            Debug.Log("Random number for rare card: "+ number);
                            if (number == newRareChance)
                            {
                                weightTable = new CardWeightTable(RarityWeightTable.OnlyRare, weightTable.OwnerTable, weightTable.CardTypeTable);
                            }
                        }
                    }
                }

            }


        }

    }
}
