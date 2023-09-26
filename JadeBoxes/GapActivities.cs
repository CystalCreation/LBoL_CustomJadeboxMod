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
using static CustomJadebox.NeutralOnly.ForgetYourNameDef;

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
                return config;
            }



            public override LocalizationOption LoadLocalization()
            {
                //TODO colors for numbers?
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Crafting at Campfires" },
                { "Description",  "At the start of the run, gain " +exibitsToGain+
                " random exhibits that unlock options at a gap. Upgrading cards at a shop or non-basic cards at a gap costs " + newUpgradePrice + "."}
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
                    var exhibits = new List<Type> { typeof(Baota), typeof(Saiqianxiang), typeof(ShanliangDengpao),
                        typeof(ShoushiYubi), typeof(Xunlongchi) };

                    var rng = gameRun.AdventureRng;

                    int removeCount = exhibits.Count - exibitsToGain;
                    for (int i = 0; i < removeCount; i++)
                    {
                        int remove = rng.NextInt(0, exhibits.Count - 1);
                        exhibits.RemoveAt(remove);
                    }

                    foreach (var ex in exhibits)
                    {
                        yield return gameRun.GainExhibitRunner(Library.CreateExhibit(ex),true);
                    }

                    gameRun.ExhibitPool.RemoveAll(e => exhibits.Contains(e));
                }


                private static bool IsGapActivityJadebox()
                {
                    var run = GameMaster.Instance.CurrentGameRun;
                    IReadOnlyList<JadeBox> jadeBox = run.JadeBox;

                    if (jadeBox != null && jadeBox.Count > 0)
                    {
                        if (run.JadeBox.Any((JadeBox jb) => jb is GetGapActivities))
                        {
                            return true;
                        }
                    }
                    return false;

                }

                [HarmonyPatch]
                class UpgradeDeckCardPrice_Patch
                {
                    static IEnumerable<MethodBase> TargetMethods()
                    {
                        yield return AccessTools.PropertyGetter(typeof(GameRunController), nameof(GameRunController.UpgradeDeckCardPrice));
                    }

                    static void Postfix(ref int __result)
                    {
                        if (IsGapActivityJadebox())
                        {
                            __result = newUpgradePrice;
                        }
                       
                    }
                }


                [HarmonyPatch(typeof(GapStation), nameof(GapStation.OnEnter))]
                class GapStation_OnEnter_Patch
                {
                    static void Postfix(GapStation __instance)
                    {
                        if (IsGapActivityJadebox())
                        {
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

