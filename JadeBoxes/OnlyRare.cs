using CustomJadebox.Util;
using HarmonyLib;
using LBoL.Base;
using LBoL.Base.Extensions;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Cards;
using LBoL.Core.Randoms;
using LBoL.Core.Stations;
using LBoL.Core.StatusEffects;
using LBoL.Core.Units;
using LBoL.EntityLib.Exhibits.Shining;
using LBoL.EntityLib.JadeBoxes;
using LBoL.EntityLib.StatusEffects.Others;
using LBoL.Presentation;
using LBoL.Presentation.UI.Panels;
using LBoL.Presentation.UI.Widgets;
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
using UnityEngine.Events;
using static CustomJadebox.BepinexPlugin;

namespace CustomJadebox.JadeBoxes
{
    public class OnlyRare
    {

        public sealed class OnlyRareDef : JadeBoxTemplate
        {


            public override IdContainer GetId()
            {
                return nameof(OnlyRareJadebox);
            }


            public override LocalizationOption LoadLocalization()
            {
                return BepinexPlugin.JadeboxBatchLoc.AddEntity(this);
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                return config;
            }


            [EntityLogic(typeof(OnlyRareDef))]
            public sealed class OnlyRareJadebox : JadeBox
            {

                protected override void OnGain(GameRunController gameRun)
                {
                    //If Cromatic Dominator is active, reroll the deck to get a deck with only rare cards
                    GameMaster.Instance.StartCoroutine(ResetDeck(gameRun));
                }

                private static IEnumerator ResetDeck(GameRunController gameRun)
                {
                    //Need to delay reroll by one frame for the jadebox check to work
                    yield return null;
                    ResetStart50.ResetStart50Deck(gameRun);
                }


                private static bool IsInShopStation(GameRunController gameRun)
                {
                    if (gameRun == null || gameRun.CurrentStation == null)
                    {
                        return false;
                    }
                    var currentStationType = gameRun.CurrentStation.Type;
                    return (currentStationType == StationType.Shop );
                }

                

                public static bool IsOnlyRareJadebox(GameRunController run)
                {
                    if (run == null)
                    {
                        return false;
                    }

                    IReadOnlyList<JadeBox> jadeBox = run.JadeBoxes;

                    if (jadeBox != null && jadeBox.Count > 0)
                    {
                        if (run.JadeBoxes.Any((JadeBox jb) => jb is OnlyRareJadebox))
                        {
                            return true;
                        }
                    }
                    return false;
                }


                //overwrite the RollCards method by intercepting its prameters to use RarityWeightTable.OnlyRare in the CardWeightTable 
                [HarmonyPatch]
                class GameRunController_RollCards_Patch
                {
                    static IEnumerable<MethodBase> TargetMethods()
                    {
                        return AccessTools.GetDeclaredMethods(typeof(GameRunController)).Where(m => m.Name == "RollCards").ToList();
                    }

                    static void Prefix(ref CardWeightTable weightTable, GameRunController __instance)
                    {
                        if (IsOnlyRareJadebox(__instance) )
                        {
                            //Use completely random cards for non tool shop cards to avoid exception where the game can't generate a rare of a specific card type
                            if (IsInShopStation(__instance) && weightTable.CardTypeTable != CardTypeWeightTable.OnlyTool)
                            {
                                weightTable = new CardWeightTable(RarityWeightTable.OnlyRare, weightTable.OwnerTable, CardTypeWeightTable.CanBeLoot);
                            }
                            else
                            {
                                weightTable = new CardWeightTable(RarityWeightTable.OnlyRare, weightTable.OwnerTable, weightTable.CardTypeTable);
                            }
                            
                        }
                    }
                }

                //Search the toggles for Burden of the Mighty and Joy of Medioce to disable each other on activation so that the two become mutually exctusive
                [HarmonyPatch(typeof(StartGamePanel), nameof(StartGamePanel.Awake))]
                class StartGamePanel_Awake_Patch
                {
                    static void Postfix(ref StartGamePanel __instance)
                    {
                        try
                        {
                            JadeBoxToggle onlyRareToggle = null;
                            JadeBoxToggle noRareToggle = null;
                            foreach (var toggle in __instance._jadeBoxToggles)
                            {
                                //Search for the two relevant toggles
                                if (toggle.Value.JadeBox.GetType() == typeof(OnlyRareJadebox))
                                {
                                    Debug.Log("found only rare toggle");
                                    onlyRareToggle = toggle.Value;
                                }
                                if (toggle.Value.JadeBox.GetType() == typeof(NoRareCard))
                                {
                                    Debug.Log("found no rare toggle");
                                    noRareToggle = toggle.Value;
                                }
                            }

                            if (onlyRareToggle != null && noRareToggle != null)
                            {
                                //Add action to the toggles that disable the other toggle when triggered
                                onlyRareToggle.Toggle.onValueChanged.AddListener(new UnityAction<bool>((bool b) => { 
                                    Debug.Log("toggled onlyRareToggle");  
                                    if (noRareToggle != null && noRareToggle.IsOn)
                                    {
                                        Debug.Log("disabling noRareToggle");
                                        noRareToggle.Toggle.SetIsOnWithoutNotify(false);
                                    }
                                }));

                                noRareToggle.Toggle.onValueChanged.AddListener(new UnityAction<bool>((bool b) => {
                                    Debug.Log("toggled noRareToggle");
                                    if (onlyRareToggle != null && onlyRareToggle.IsOn)
                                    {
                                        Debug.Log("disabling onlyRareToggle");
                                        onlyRareToggle.Toggle.SetIsOnWithoutNotify(false);
                                    }
                                }));
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.Log("error when checking  toggles: " + e.Message + e.StackTrace);
                        }
                        

                    }
                }

            }


        }
    }
}
