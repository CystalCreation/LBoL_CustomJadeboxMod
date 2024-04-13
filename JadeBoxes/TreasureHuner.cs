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
using LBoLEntitySideloader.Resource;
using LBoLEntitySideloader.TemplateGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


using static CustomJadebox.BepinexPlugin;
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
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Treasure Hunter" },
                { "Description", "Shining exhibit rewards after each boss are replaced with kaguya's treasures."}
            });
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

                    //add random regular ehibit if there are less than 3 treasures left to get
                    else if (rewardList.Count < 3)
                    {
                        for (int i = 0; rewardList.Count < 3 || i > 3; i++)
                        {
                            //if (rewardList.Count == 0)
                            //{
                            //    //Add placeholder if no more valid treasures are availabe
                            //    rewardList.Add(Library.CreateExhibit<YanZianbei>());
                            //}
                            //var randomExibit = rewardList[rng.NextInt(0, rewardList.Count - 1)];
                            //Debug.Log("randomly adding exibit: " + randomExibit.Name);
                            //rewardList.Add(randomExibit);


                            rewardList.Add(run.CurrentStage.GetEliteEnemyExhibit());
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




            }


        }
    }
}
