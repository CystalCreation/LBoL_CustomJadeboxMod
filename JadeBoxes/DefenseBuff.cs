using LBoL.ConfigData;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Battle;
using LBoL.Core.StatusEffects;
using LBoL.Core.Units;
using LBoL.Core;
using LBoL.EntityLib.Exhibits.Common;
using LBoL.EntityLib.Exhibits.Shining;
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
    public class DefenseBuff
    {

        public sealed class DefenseBuffDef : JadeBoxTemplate
        {
            private static int statusGain = 4;

            public override IdContainer GetId()
            {
                return nameof(GetDefenseBuff);
            }
            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = statusGain;
                return config;
            }



            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Turtle Up" },
                { "Description", "Card rewards have one more option. At the start of combat, gain {Value1} Spirit and loose {Value1} Firepower."}
            });
            }

            



            [EntityLogic(typeof(DefenseBuffDef))]
            public sealed class GetDefenseBuff : JadeBox
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
                       //give player spirit and negative firepower
                       yield return new ApplyStatusEffectAction<Spirit>(player, statusGain);
                       yield return new ApplyStatusEffectAction<FirepowerNegative>(player, statusGain);                        
                    }

                }


                private static void Init(GameRunController gameRun)
                {
                    gameRun.AdditionalRewardCardCount++;
                }


                protected override void OnAdded()
                {
                    //flags need to be set in OnAdded or else they will be gone after a reload
                    Init(base.GameRun);
                }

            }

        }

    }
}

