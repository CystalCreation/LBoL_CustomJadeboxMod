using HarmonyLib;
using LBoL.Base;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Cards;
using LBoL.Core.Stations;
using LBoL.Core.StatusEffects;
using LBoL.Core.Units;
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
using LBoLEntitySideloader.ExtraFunc;
using LBoLEntitySideloader.Resource;
using LBoLEntitySideloader.TemplateGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


using static CustomJadebox.BepinexPlugin;
using static CustomJadebox.JadeBoxes.AllShinies.AllShiniesDef;
using static System.Collections.Specialized.BitVector32;


namespace CustomJadebox.JadeBoxes
{
    public class TreasureHuner
    {

        public sealed class TreasureHunterDef : JadeBoxTemplate
        {


            public override IdContainer GetId()
            {
                return nameof(GetGetTreasureHuner);
            }
            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                return config;
            }


            public override LocalizationOption LoadLocalization()
            {
                return BepinexPlugin.JadeboxBatchLoc.AddEntity(this);
            }


            [EntityLogic(typeof(TreasureHunterDef))]
            public sealed class GetGetTreasureHuner : JadeBox
            {

                //initialize the list with valid default values in case someting goes wrong
                public static List<Exhibit> treasureRewardList = new List<Exhibit>()
                        {
                            Library.CreateExhibit<FoyushiBo>(),
                            Library.CreateExhibit<HuoshuPiyi>(),
                            Library.CreateExhibit<LongjingYu>()
                        };





                private static void SetRewardList(GameRunController run)
                {
                    var rng = run.AdventureRng;

                    //initialize list with all treasures
                    var rewardList = new List<Exhibit>()
                        {
                            Library.CreateExhibit<FoyushiBo>(),
                            Library.CreateExhibit<HuoshuPiyi>(),
                            Library.CreateExhibit<LongjingYu>(),
                            Library.CreateExhibit<PenglaiYuzhi>(),
                            Library.CreateExhibit<YanZianbei>()
                        };
                    

                    //remove all treasures the player already has
                    foreach (var item in run.Player.Exhibits)
                    {
                        Debug.Log("player has exibit: " + item.Name);
                        if (rewardList.Find(i => i.Name == item.Name) != null)
                        {
                            Debug.Log("removing exibit from reward list: " + item.Name);
                            rewardList.Remove(rewardList.Find(i => i.Name == item.Name));
                        }
                    }
                    Debug.Log("after remove existing SetRewardList rewardList count: " + rewardList.Count);

                    //remove random treasures if the list has more than 3
                    if (rewardList.Count > 3)
                    {
                        for (int i = 0; rewardList.Count > 3 || i > 3; i++)
                        {
                            int remove = rng.NextInt(0, rewardList.Count - 1);
                            Debug.Log("randomly removing at index: " + remove);
                            rewardList.RemoveAt(remove);
                        }
                    }

                    //add random exhibits if there are less than 3 treasures left to get
                    else if (rewardList.Count < 3)
                    {
                        for (int i = 0; rewardList.Count < 3 || i > 3; i++)
                        {
                            if (AllShiniesJadebox(run))
                            {
                                //If Oh, Shiny is enabled, allow random regular exhibits to be generated
                                rewardList.Add(run.CurrentStage.GetEliteEnemyExhibit());                                
                            }
                            else
                            {
                                if (rewardList.Count == 0)
                                {
                                    //Add regular exhibits if no more valid treasures are availabe
                                    rewardList.Add(run.CurrentStage.GetEliteEnemyExhibit());
                                }
                                //Otherwhise add dublicate treasures
                                var randomExibit = rewardList[rng.NextInt(0, rewardList.Count - 1)];
                                Debug.Log("randomly adding exibit: " + randomExibit.Name);
                                rewardList.Add(randomExibit);
                            }


                        }

                    }

                    treasureRewardList = rewardList;
                }



                //overwrite GenerateBossRewards method to only provide treasures
                [HarmonyPatch(typeof(BossStation), nameof(BossStation.GenerateBossRewards))]
                class BossStation_GenerateBossRewards_Patch
                {
                    static void Postfix(BossStation __instance)
                    {

                        IReadOnlyList<JadeBox> jadeBox = __instance.GameRun._jadeBoxes;
                        if (jadeBox != null && jadeBox.Count > 0)
                        {
                            foreach (var jb in jadeBox)
                            {
                                if (jb is GetGetTreasureHuner)
                                {
                                    SetRewardList(__instance.GameRun);
                                    Debug.Log("BossStation_GenerateBossRewards_Patch rewardList count: " + treasureRewardList.Count);

                                    __instance.BossRewards = treasureRewardList.ToArray();
                                    return;
                                }
                            }
                        }


                    }
                }

                public static bool AllShiniesJadebox(GameRunController run)
                {
                    if (run == null)
                    {
                        return false;
                    }

                    IReadOnlyList<JadeBox> jadeBox = run.JadeBoxes;

                    if (jadeBox != null && jadeBox.Count > 0)
                    {
                        if (run.JadeBoxes.Any((JadeBox jb) => jb is GetAllShinies))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                public static bool TresureHunterJadebox(GameRunController run)
                {
                    if (run == null)
                    {
                        return false;
                    }

                    IReadOnlyList<JadeBox> jadeBox = run.JadeBoxes;

                    if (jadeBox != null && jadeBox.Count > 0)
                    {
                        if (run.JadeBoxes.Any((JadeBox jb) => jb is GetGetTreasureHuner))
                        {
                            return true;
                        }
                    }
                    return false;
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


                //Overwrite TriggerGain to remove mana gain from treasures
                [HarmonyPatch(typeof(Exhibit), "TriggerGain")]
                class Exhibit_Patch
                {
                    static void Postfix(ref IEnumerator __result)
                    {
                        var extendedRez = new CoroutineExtender(__result);

                        extendedRez.postItems.Add(removeMana());

                        __result = extendedRez.GetEnumerator();

                    }

                    static IEnumerator removeMana()
                    {
                        if ((GameMaster.Instance != null) && (GameMaster.Instance.CurrentGameRun != null) 
                            && TresureHunterJadebox(GameMaster.Instance.CurrentGameRun)
                            && !AllShiniesJadebox(GameMaster.Instance.CurrentGameRun))
                        {
                            var run = GameMaster.Instance.CurrentGameRun;
                            if (run.CurrentStation != null && run.CurrentStation.Type == StationType.Boss)
                            {
                                Debug.Log("removing rainbow mana from treasure gain");
                                run.LoseBaseMana(ManaGroup.Philosophies(1), false);
                            }

                        }
                        yield break;
                    }
                }




            }


        }
    }
}
