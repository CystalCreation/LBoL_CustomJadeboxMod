using Cysharp.Threading.Tasks;
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
using LBoL.EntityLib.Adventures;
using LBoL.EntityLib.Cards.Neutral.Red;
using LBoL.EntityLib.Exhibits.Common;
using LBoL.EntityLib.Exhibits.Mythic;
using LBoL.EntityLib.Exhibits.Shining;
using LBoL.EntityLib.JadeBoxes;
using LBoL.EntityLib.StatusEffects.Others;
using LBoL.Presentation;
using LBoL.Presentation.UI.Panels;
using LBoLEntitySideloader;
using LBoLEntitySideloader.Attributes;
using LBoLEntitySideloader.Entities;
using LBoLEntitySideloader.ReflectionHelpers;
using LBoLEntitySideloader.Resource;
using LBoLEntitySideloader.TemplateGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CustomJadebox.JadeBoxes
{
    public class SellItAll
    {
        public sealed class SellItAllDef : JadeBoxTemplate
        {
            private static int shiningChance = 15;
            private static int shiningPrice = 500;


            public override IdContainer GetId()
            {
                return nameof(SellItAllJadebox);
            }


            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Traveling Lightly" },
                { "Description", "Whenever you gain an Exhibit that can be sold, loose that Exhibit and gain " +
                "<sprite=\"Point\"\\ name=\"Gold\"> equal to its sell value. " +
                "Exhibits in the shop have a {Value1}% chance to be shining exhibits."}
            });
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = shiningChance;
                return config;
            }


            [EntityLogic(typeof(SellItAllDef))]
            public sealed class SellItAllJadebox : JadeBox
            {

                private static bool IsInShop(GameRunController gameRun)
                {
                    if (gameRun == null || gameRun.CurrentStation == null)
                    {
                        return false;
                    }
                    var currentStationType = gameRun.CurrentStation.Type;
                    return (currentStationType == StationType.Shop);
                }


                public static bool IsSellItAllJadebox(GameRunController run)
                {
                    if (run == null)
                    {
                        return false;
                    }

                    IReadOnlyList<JadeBox> jadeBox = run.JadeBox;

                    if (jadeBox != null && jadeBox.Count > 0)
                    {
                        if (run.JadeBox.Any((JadeBox jb) => jb is SellItAllJadebox))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                //Copied from RinnosukeTrade.GetExhibitPrice
                private static int GetExhibitPrice(Exhibit exhibit)
                {
                    int num;
                    switch (exhibit.Config.Rarity)
                    {
                        case Rarity.Common:
                            num = GlobalConfig.ExhibitPrices[0];
                            break;
                        case Rarity.Uncommon:
                            num = GlobalConfig.ExhibitPrices[1];
                            break;
                        case Rarity.Rare:
                            num = GlobalConfig.ExhibitPrices[2];
                            break;
                        default:
                            throw new InvalidOperationException("exhibit rarity out of range.");
                    }
                    float num2 = (float)num;
                    float num3 = GameMaster.Instance.CurrentGameRun.ShopRng.NextFloat(-0.08f, 0f) + 1f;
                    return Mathf.RoundToInt((float)Mathf.RoundToInt(num2 * num3) * 60f / 100f);
                }



                //Class that allows easier postfixes for IEnumerator corutines
                class CoroutineExtender : IEnumerable
                {
                    public IEnumerator target_enumerator;
                    public List<IEnumerator> preItems = new List<IEnumerator>();
                    public List<IEnumerator> postItems = new List<IEnumerator>();
                    public List<IEnumerator> midItems = new List<IEnumerator>();


                    public CoroutineExtender() { }

                    public CoroutineExtender(IEnumerator target_enumerator) { this.target_enumerator = target_enumerator; }

                    public IEnumerator GetEnumerator()
                    {
                        foreach (var e in preItems) yield return e;
                        int i = 0;
                        while (target_enumerator.MoveNext())
                        {
                            yield return target_enumerator.Current;
                            i++;
                        }
                        foreach (var e in postItems) yield return e;
                    }
                }

                //Overwrite TriggerGain to remove the exhibit after aquisition
                [HarmonyPatch(typeof(Exhibit), "TriggerGain")]
                [HarmonyDebug]
                class Exhibit_Patch
                {
                    static void Postfix(ref IEnumerator __result)
                    {
                        var extendedRez = new CoroutineExtender(__result);

                        extendedRez.postItems.Add(SellAllExhibits());

                        __result = extendedRez.GetEnumerator();
                    }

                    static IEnumerator SellAllExhibits()
                    {
                        var run = GameMaster.Instance.CurrentGameRun;
                        if (IsSellItAllJadebox(run) )
                        {
                            List<Exhibit> toRemove = new List<Exhibit>();
                            foreach (var ex in run.Player.Exhibits)
                            {
                                //filter out shining and trade quest exhibits by checking if they are losable
                                if (ex.LosableType == ExhibitLosableType.Losable ) 
                                {
                                    run.GainMoney(GetExhibitPrice(ex), true, new VisualSourceData
                                    {
                                        SourceType = VisualSourceType.Entity,
                                        Source = ex
                                    });

                                    toRemove.Add(ex);
                                }
                            }

                            foreach (var ex in toRemove)
                            {
                                run.LoseExhibit(ex, true, true);
                            }
                        }
                        yield break;
                    }
                }

                //Overwrite GetShopExhibit to return shining exhibits on the right rng roll
                [HarmonyPatch]
                class Stage_GetShopExhibit_Patch
                {
                    static IEnumerable<MethodBase> TargetMethods()
                    {
                        return AccessTools.GetDeclaredMethods(typeof(Stage)).Where(m => m.Name == "GetShopExhibit").ToList();
                    }

                    static void Postfix(ref Exhibit __result)
                    {
                        var run = GameMaster.Instance.CurrentGameRun;
                        if (IsSellItAllJadebox(run) && IsInShop(run) )
                        {
                            int number = run.ShopRng.NextInt(1, 100);
                            Debug.Log("Random number for shop exhibit: " + number);
                            if (number <= shiningChance)
                            {
                                //Filter out exhibits of unreleased characters
                                __result = run.RollShiningExhibit(run.ShinningExhibitRng, run.CurrentStation.Stage.GetSentinelExhibit,(ExhibitConfig config) => 
                                config != null && (config.Owner == null || (config.Owner != null && !config.Owner.Contains("Koishi") && !config.Owner.Contains("Alice"))));
                            }
                        }
                    }
                }


                //Overwrite ShopStation.GetPrice to support getting a price for shining exhibits
                [HarmonyPatch(typeof(ShopStation), nameof(ShopStation.GetPrice), new Type[] { typeof(Exhibit) })]
                class ShopStation_GetPrice_Patch
                {
                    static bool Prefix(ref Exhibit exhibit, ref int __result)
                    {
                        var run = GameMaster.Instance.CurrentGameRun;
                        if (IsSellItAllJadebox(run) && IsInShop(run) && exhibit.Config.Rarity == Rarity.Shining)
                        {
                            float price = (float)shiningPrice;
                            float multiplyer = run.ShopRng.NextFloat(-0.08f, 0f) + 1f;
                            __result = Mathf.RoundToInt(price * multiplyer);
                            //when Prefix returns false, the pached method does not get executed at all and instead returns the value in __result
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                        
                    }
                }

            }


        }

    }
}
