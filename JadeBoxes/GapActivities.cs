using HarmonyLib;
using LBoL.Base;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Cards;
using LBoL.Core.GapOptions;
using LBoL.Core.Stations;
using LBoL.Core.StatusEffects;
using LBoL.Core.Units;
using LBoL.EntityLib.Cards.Neutral.Red;
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
    public class GapActivities
    {
        public sealed class GapActivitiesDef : JadeBoxTemplate
        {
            private static int exibitsToGain = 2;
            private static int newUpgradePrice = 100;



            public override IdContainer GetId()
            {
                return nameof(GetGapActivities);
            }
            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = exibitsToGain;
                config.Value2 = newUpgradePrice;
                return config;
            }



            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Crafting at Campfires" },
                { "Description",  "At the start of the run, gain {Value1}"+
                " random exhibits that unlock options at a gap. Upgrading cards at a shop or non-basic cards at a gap costs {Value2}<sprite=\"Point\"\\ name=\"Gold\">."}
            });
            }

            



            [EntityLogic(typeof(GapActivitiesDef))]
            public sealed class GetGapActivities : JadeBox
            {

                protected override void OnGain(GameRunController gameRun)
                {
                    GameMaster.Instance.StartCoroutine(GainExhibits(gameRun));
                }

                private IEnumerator GainExhibits(GameRunController gameRun)
                {
                    //start with all gap option exhibits
                    var exhibits = new List<Type> { typeof(Baota), typeof(Saiqianxiang), typeof(ShanliangDengpao),
                        typeof(ShoushiYubi), typeof(Xunlongchi) };

                    var rng = gameRun.AdventureRng;

                    //randomly remove until the desired number remains 
                    int removeCount = exhibits.Count - exibitsToGain;
                    for (int i = 0; i < removeCount; i++)
                    {
                        int remove = rng.NextInt(0, exhibits.Count - 1);
                        exhibits.RemoveAt(remove);
                    }

                    //give the remaining exhibits to the player
                    foreach (var ex in exhibits)
                    {
                        yield return gameRun.GainExhibitRunner(Library.CreateExhibit(ex),true);
                    }

                    gameRun.ExhibitPool.RemoveAll(e => exhibits.Contains(e));
                }


                private static bool IsGapActivityJadebox()
                {
                    var run = GameMaster.Instance.CurrentGameRun;
                    IReadOnlyList<JadeBox> jadeBox = run.JadeBoxes;

                    if (jadeBox != null && jadeBox.Count > 0)
                    {
                        if (run.JadeBoxes.Any((JadeBox jb) => jb is GetGapActivities))
                        {
                            return true;
                        }
                    }
                    return false;

                }

                //overwrite shop price for card upgrades
                [HarmonyPatch]
                class UpgradeDeckCardPrice_Patch
                {
                    static IEnumerable<MethodBase> TargetMethods()
                    {
                        yield return AccessTools.PropertyGetter(typeof(GameRunController), nameof(GameRunController.UpgradeDeckCardPrice));
                    }

                    static void Postfix(ref int __result)
                    {
                        //check if jadebox is actually enabled
                        if (IsGapActivityJadebox())
                        {
                            __result = newUpgradePrice;
                        }
                       
                    }
                }

                //overwrite gap price for card upgrades
                [HarmonyPatch(typeof(GapStation), nameof(GapStation.OnEnter))]
                class GapStation_OnEnter_Patch
                {
                    static void Postfix(GapStation __instance)
                    {
                        //check if jadebox is actually enabled
                        if (IsGapActivityJadebox())
                        {
                            //Don't change price if upgrade option doesn't exist
                            UpgradeCard upgradeOption = ((UpgradeCard)__instance.GapOptions.Find((GapOption g) => g is UpgradeCard));
                            if(upgradeOption != null)
                            {
                                upgradeOption.Price = newUpgradePrice;
                            }
                            else
                            {
                                Debug.Log("could not find gap upgrade option");    
                            }
                        }

                    }
                }


            }


        }

    }


}

