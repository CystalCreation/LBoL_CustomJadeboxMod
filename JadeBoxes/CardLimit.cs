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
using LBoL.EntityLib.StatusEffects.Enemy;
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
    public class CardLimit
    {
        public sealed class CardLimitDef : JadeBoxTemplate
        {
            public static int cardLimit = 5;


            public override IdContainer GetId()
            {
                return nameof(GetCardLimit);
            }
            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = cardLimit;
                config.Mana = ManaGroup.Single(ManaColor.Philosophy);
                return config;
            }



            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Knockout in Five Steps" },
                { "Description", "At the start of your turn, gain {Mana} for each shining exhibit you have." +
                " Only {Value1} cards can be played each turn."}
            });
            }



            [EntityLogic(typeof(CardLimitDef))]
            public sealed class GetCardLimit : JadeBox
            {
                protected override void OnEnterBattle()
                {
                    ReactBattleEvent(Battle.Player.TurnStarted, OnBattleStarted);
                }


                private IEnumerable<BattleAction> OnBattleStarted(GameEventArgs args)
                {

                    //Gain mana for each shining exhibit
                    int shiningCount = 0;
                    PlayerUnit player = base.Battle.Player;
                    foreach (var item in player.Exhibits)
                    {
                        if (item.Config.Rarity == Rarity.Shining)
                        {
                            shiningCount++;
                        }
                    }

                    yield return new GainManaAction(new ManaGroup(){ Philosophy = shiningCount});


                    //Apply FoxCharm at the start of each turn if status is not already applyed
                    bool alreadyCharmed = false;
                    foreach (var item in player.StatusEffects)
                    {
                        if (item.GetType() == typeof(FoxCharm))
                        {
                            alreadyCharmed = true;
                            break;
                        }
                    }

                    if (!alreadyCharmed)
                    {
                        yield return new ApplyStatusEffectAction<FoxCharm>(player, null, null, null, cardLimit, 0f, true);
                    }


                }

            }

        }

    }
}
