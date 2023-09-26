using HarmonyLib;
using LBoL.Base;
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
using LBoL.EntityLib.Exhibits;
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static CustomJadebox.BepinexPlugin;
using static CustomJadebox.JadeBoxes.TreasureHuner.TreasureHunterDef;

namespace CustomJadebox
{
    public class NeutralOnly
    {
        public sealed class ForgetYourNameDef : JadeBoxTemplate
        {
            public override IdContainer GetId()
            {
                return nameof(ForgetYourName);
            }


            public override LocalizationOption LoadLocalization()
            {
                //TODO colors for numbers?
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Forget Your Name" },
                { "Description", "At the start of the run, replace all mana in the mana base with Philosophers mana. " +
                "Cards of all colors are added to the card pool. Card rewards have one more option. Only Neutral cards can appear."}
            });
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                return config;
            }


            [EntityLogic(typeof(ForgetYourNameDef))]
            public sealed class ForgetYourName : JadeBox
            {
                

                protected override void OnGain(GameRunController gameRun)
                {
                    gameRun.BaseMana = ManaGroup.Empty;

                    for (int i = 0; i < 5; i++)
                    {
                        gameRun.GainBaseMana(ManaGroup.Single(ManaColor.Philosophy));
                    }

                }

                private void Init()
                {
                    base.GameRun.RewardAndShopCardColorLimitFlag++;
                    base.GameRun.AdditionalRewardCardCount++;

                }

                protected override void OnAdded()
                {
                    Init();
                }

                private static bool IsNetralOnlyJadebox()
                {
                    var run = GameMaster.Instance.CurrentGameRun;
                    IReadOnlyList<JadeBox> jadeBox = run.JadeBox;


                    if (jadeBox != null && jadeBox.Count > 0)
                    {
                        if (run.JadeBox.Any((JadeBox jb) => jb is ForgetYourName))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                [HarmonyPatch]
                class GameRunController_Patch
                {
                    static IEnumerable<MethodBase> TargetMethods()
                    {
                        return AccessTools.GetDeclaredMethods(typeof(GameRunController)).Where(m => m.Name == "RollCards").ToList();
                    }

                    static void Prefix(ref CardWeightTable weightTable)
                    {
                        if (IsNetralOnlyJadebox())
                        {
                            weightTable = new CardWeightTable(weightTable.RarityTable, OwnerWeightTable.OnlyNeutral, weightTable.CardTypeTable);
                        }
                    }
                }


                [HarmonyPatch(typeof(Debut), nameof(Debut.ExchangeExhibit))]
                class BanExhibitSwap_Patch
                {
                    static void Prefix()
                    {
                        if (IsNetralOnlyJadebox())
                        {
                            var run = GameMaster.Instance.CurrentGameRun;
                            run.LoseBaseMana(ManaGroup.Single(ManaColor.Philosophy));
                        }
                    }
                }


            }


        }

    }
}
