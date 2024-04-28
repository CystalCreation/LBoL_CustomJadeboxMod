using HarmonyLib;
using LBoL.ConfigData;
using LBoL.Core.Stations;
using LBoL.Core;
using LBoL.EntityLib.Exhibits.Mythic;
using LBoLEntitySideloader.Attributes;
using LBoLEntitySideloader.Resource;
using LBoLEntitySideloader;
using System;
using System.Collections.Generic;
using System.Text;
using LBoLEntitySideloader.Entities;
using UnityEngine;
using System.Linq;
using LBoL.EntityLib.Exhibits.Common;
using LBoL.Presentation;
using System.Collections;
using LBoL.Base;
using LBoL.EntityLib.Exhibits.Shining;

namespace CustomJadebox.JadeBoxes
{
    public class AllShinies
    {
        public sealed class AllShiniesDef : JadeBoxTemplate
        {

            public override IdContainer GetId()
            {
                return nameof(GetAllShinies);
            }
            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Mana = ManaGroup.Single(ManaColor.Colorless);
                return config;
            }


            public override LocalizationOption LoadLocalization()
            {
                return BepinexPlugin.JadeboxBatchLoc.AddEntity(this);
            }


            [EntityLogic(typeof(AllShiniesDef))]
            public sealed class GetAllShinies : JadeBox
            {
                private static List<string> bossesDone = new List<string>();

                protected override void OnGain(GameRunController run)
                {
                    GameMaster.Instance.StartCoroutine(RemoveFromPool(run));
                    bossesDone = new List<string>();
                }
                protected override void OnAdded()
                {
                    //GameMaster.Instance.StartCoroutine(RemoveFromPool(base.GameRun));
                    bossesDone = new List<string>();
                }

                private IEnumerator RemoveFromPool(GameRunController gameRun)
                {
                    if(gameRun == null)
                    {
                        yield break ;
                    }

                    //remove shining exhibits that gain mana mid battle instead of adding to base mana
                    var exhibit = new HashSet<Type> { typeof(YizangnuoWuzhi), typeof(QipaiYouhua),
                        typeof(HuiyeBaoxiang), typeof(LouguanJian)};
                    foreach (var item in exhibit)
                    {
                        Debug.Log("removing from pool: " + item.Name + " was in pool: " + (gameRun.ShiningExhibitPool.Contains(item)));
                    }
                    gameRun.ShiningExhibitPool.RemoveAll(e => exhibit.Contains(e));
                    yield return null;
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


                //Overwrite TriggerGain to gain all shining exhibits from bosses
                [HarmonyPatch(typeof(Exhibit), "TriggerGain")]
                class Exhibit_Patch
                {
                    static void Postfix(ref IEnumerator __result)
                    {
                        var extendedRez = new CoroutineExtender(__result);

                        extendedRez.postItems.Add(GainOtherShinies());

                        __result = extendedRez.GetEnumerator();
                    }

                    static IEnumerator GainOtherShinies()
                    {
                        if ((GameMaster.Instance != null) && (GameMaster.Instance.CurrentGameRun != null) && AllShiniesJadebox(GameMaster.Instance.CurrentGameRun))
                        {
                            Debug.Log("start gaining shinies");
                            var run = GameMaster.Instance.CurrentGameRun;
                            if(run.CurrentStation != null && run.CurrentStation.Type == StationType.Boss)
                            {
                                string bossName = ((BossStation)run.CurrentStation).BossId;

                                //check if we already picked an exhibit from this boss to avoid retriggering method when gaining the other exhibits
                                if (!bossesDone.Contains(bossName))
                                {
                                    bossesDone.Add(bossName);
                                    var exhibitList = ((BossStation)run.CurrentStation).BossRewards;
                                    foreach (var exhibit in exhibitList)
                                    {
                                        if (!run.Player.Exhibits.Contains(exhibit))
                                        {
                                            Debug.Log("gaining exhibit " + exhibit);
                                            yield return run.GainExhibitRunner(exhibit);
                                        }
                                        else
                                        {
                                            run.GainBaseMana(ManaGroup.Colorlesses(1));
                                        }
                                        yield return null;

                                        //remove mana just gained from the new exhibit
                                        run.LoseBaseMana(exhibit.BaseMana, false);
                                    }

                                }
                                else
                                {
                                    Debug.Log("already gained exhibit from boss: " + bossName);
                                }
                                

                            }

                            
                        }
                        yield break;
                    }
                }
            }

            }
        }
    }

