using HarmonyLib;
using LBoL.Base;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Cards;
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

namespace CustomJadebox
{
    public class AllVulnerable
    {


        public sealed class WorldOfHurtDef : JadeBoxTemplate
        {
            private static int spiritGain = 4;



            public override IdContainer GetId()
            {
                return nameof(GetWorldOfHurt);
            }
            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = spiritGain;
                return config;
            }



            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "World of Hurt" },
                { "Description", "At the start of the run, gain the |Sewing Machine|. " + "At the start of combat, gain {Value1}" +
                " Spirit. This Spirit gain decreases after every boss fight. At the start of the Player's turn, gain Vulnerable and apply Vulnerable to each enemy."}
            });
            }



            [EntityLogic(typeof(WorldOfHurtDef))]
            public sealed class GetWorldOfHurt : JadeBox
            {
                protected override void OnEnterBattle()
                {
                    ReactBattleEvent(Battle.Player.TurnStarted, OnBattleStarted);
                }


                private IEnumerable<BattleAction> OnBattleStarted(GameEventArgs args)
                {

                    PlayerUnit player = base.Battle.Player;

                    if (Battle.Player.TurnCounter == 1)
                    {
                        NotifyActivating();
                        //give player spirit relitive to the number of bosses fough
                        int level = spiritGain - base.GameRun._stageIndex;
                        if (level >= 1)
                        {
                            yield return new ApplyStatusEffectAction<Spirit>(player, level);

                        }
                    }

                    //apply vurnerable to everyone
                    yield return new ApplyStatusEffectAction<Vulnerable>(player, null, 1);
                    foreach (var enemy in base.Battle.AllAliveEnemies)
                    {
                        yield return new ApplyStatusEffectAction<Vulnerable>(enemy, null, 1);
                    }
                    


                }

                protected override void OnGain(GameRunController gameRun)
                {
                    GameMaster.Instance.StartCoroutine(GainExhibits(gameRun));
                }

                private IEnumerator GainExhibits(GameRunController gameRun)
                {
                    //give player the Sewing Machine
                    var exhibit = new HashSet<Type> { typeof(Fengrenji) };
                    foreach (var et in exhibit)
                    {
                        yield return gameRun.GainExhibitRunner(Library.CreateExhibit(et));
                    }

                    gameRun.ExhibitPool.RemoveAll(e => exhibit.Contains(e));
                }


            }

            
        }

    }


}

