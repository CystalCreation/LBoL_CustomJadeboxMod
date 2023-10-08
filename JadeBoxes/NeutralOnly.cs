using CustomJadebox.JadeBoxes;
using CustomJadebox.Util;
using HarmonyLib;
using LBoL.Base;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Cards;
using LBoL.Core.JadeBoxes;
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
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Forget Your Name" },
                { "Description", "At the start of the run, replace each mana in the mana base with {Mana}. " +
                "Cards of all colors are added to the card pool. Card rewards have one more option. Only Neutral cards can appear."}
            });
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Mana = ManaGroup.Single(ManaColor.Philosophy);
                return config;
            }


            [EntityLogic(typeof(ForgetYourNameDef))]
            public sealed class ForgetYourName : JadeBox
            {
                private static bool neutralOnlyActive = false;
                private static bool synestasiaActive = false;
                private static bool fullPowerActive = false;


                protected override void OnGain(GameRunController gameRun)
                {
                    SetBaseMana(gameRun);
                }

                private static void SetBaseMana(GameRunController gameRun)
                {
                    //remove all base mana and then add 5 rainbow mana
                    gameRun.BaseMana = ManaGroup.Empty;
                    int rainbowManaCount = 5;

                    CheckJadeboxes(gameRun);
                    if (fullPowerActive)
                    {
                        //if full power is enabled, leave one mana of the original color that the GameRunController can replace with rainbow mana
                        rainbowManaCount = 4;
                        Exhibit primaryExhibit = Library.CreateExhibit((gameRun.PlayerType != PlayerType.TypeA) ? gameRun.Player.Config.ExhibitB : gameRun.Player.Config.ExhibitA);
                        Debug.Log("primary exhibit: " + primaryExhibit.Name);
                        gameRun.GainBaseMana(primaryExhibit.BaseMana, false);

                    }


                    for (int i = 0; i < rainbowManaCount; i++)
                    {
                        gameRun.GainBaseMana(ManaGroup.Single(ManaColor.Philosophy));
                    }

                    Init(gameRun);
                    ResetStart50.ResetStart50Deck(gameRun,!synestasiaActive);
                }

                private static void Init(GameRunController gameRun)
                {
                    gameRun.RewardAndShopCardColorLimitFlag++;
                    gameRun.AdditionalRewardCardCount++;
                }


                protected override void OnAdded()
                {
                    //flags need to be set in OnAdded or else they will be gone after a reload
                    Init(base.GameRun);
                }


                private static void CheckJadeboxes(GameRunController run)
                {
                    neutralOnlyActive = false;
                    synestasiaActive = false;
                    fullPowerActive = false;
                    try
                    {
                        if(run == null)
                        {
                            Debug.Log("game run controller is null");
                            return;
                        }
                        IReadOnlyList<JadeBox> jadeBox = run.JadeBox;

                        if (jadeBox != null && jadeBox.Count > 0)
                        {
                            neutralOnlyActive = run.JadeBox.Any((JadeBox jb) => jb is ForgetYourName);
                            synestasiaActive = run.JadeBox.Any((JadeBox jb) => jb is AllCharacterCards);
                            fullPowerActive = run.JadeBox.Any((JadeBox jb) => jb is TwoColorStart);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error when checking jadeboxes in Forget your name: " + e.Message + e.StackTrace);
                         
                    }
                    
                    
                }


                //overwrite the RollCards method by intercepting its prameters to always use OwnerWeightTable.OnlyNeutral in the CardWeightTable
                [HarmonyPatch]
                class GameRunController_RollCards_Patch
                {
                    static IEnumerable<MethodBase> TargetMethods()
                    {
                        return AccessTools.GetDeclaredMethods(typeof(GameRunController)).Where(m => m.Name == "RollCards").ToList();
                    }

                    static void Prefix(ref CardWeightTable weightTable, GameRunController __instance)
                    {
                        CheckJadeboxes(__instance);
                        //Allow the use of non-neutral cards only if Synestasia is enabled
                        if (neutralOnlyActive && !synestasiaActive)
                        {
                            weightTable = new CardWeightTable(weightTable.RarityTable, OwnerWeightTable.OnlyNeutral, weightTable.CardTypeTable);
                        }
                    }
                }

                //overwrite ExchangeExhibit method to remove rainbow mana because there is no more mana of the original color to remove
                [HarmonyPatch(typeof(Debut), nameof(Debut.ExchangeExhibit))]
                class BanExhibitSwap_Patch
                {
                    static void Prefix(Debut __instance)
                    {
                        CheckJadeboxes(__instance.GameRun);
                        if (neutralOnlyActive)
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
