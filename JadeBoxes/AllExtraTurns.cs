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
using System.Text;
using UnityEngine;
using static CustomJadebox.BepinexPlugin;

namespace CustomJadebox.JadeBoxes
{
    public class AllExtraTurns
    {
        public sealed class AllExtraTurnsDef : JadeBoxTemplate
        {
            public override IdContainer GetId()
            {
                return nameof(GetAllExtraTurns);
            }


            public override LocalizationOption LoadLocalization()
            {

                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Space Time Hole" },
                { "Description", "At the start of the run, gain the Lunar Fan. The Player and the enemies take two turns in a row."}
            });
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                return config;
            }


            [EntityLogic(typeof(AllExtraTurnsDef))]
            public sealed class GetAllExtraTurns : JadeBox
            {
                protected override void OnEnterBattle()
                {
                    ReactBattleEvent(Battle.Player.TurnStarted, OnBattleStarted);
                }

                private IEnumerable<BattleAction> OnBattleStarted(GameEventArgs args)
                {

                    PlayerUnit player = Battle.Player;

                    if (!player.IsExtraTurn)
                    {
                        yield return new ApplyStatusEffectAction<LBoL.Core.StatusEffects.ExtraTurn>(player, 1);
                        foreach (var enemy in Battle.AllAliveEnemies)
                        {
                            yield return new ApplyStatusEffectAction<LBoL.Core.StatusEffects.ExtraTurn>(enemy, 1);
                        }
                    }
                }

                protected override void OnGain(GameRunController gameRun)
                {
                    GameMaster.Instance.StartCoroutine(GainExhibits(gameRun));
                }

                private IEnumerator GainExhibits(GameRunController gameRun)
                {
                    var exhibit = new HashSet<Type> { typeof(Yueshan) };
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
