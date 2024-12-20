﻿using CustomJadebox.Util;
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
using LBoL.EntityLib.Cards.Character.Cirno;
using LBoL.EntityLib.Cards.Character.Koishi;
using LBoL.EntityLib.Cards.Character.Marisa;
using LBoL.EntityLib.Cards.Character.Reimu;
using LBoL.EntityLib.Cards.Character.Sakuya;
using LBoL.EntityLib.Exhibits.Shining;
using LBoL.EntityLib.JadeBoxes;
using LBoL.EntityLib.StatusEffects.Others;
using LBoL.Presentation;
using LBoL.Presentation.Bullet;
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
using System.Numerics;
using System.Reflection;
using System.Text;
using UnityEngine;
using static CustomJadebox.BepinexPlugin;

namespace CustomJadebox.JadeBoxes
{
    public class ChromaShift
    {
        public sealed class ChromaShiftDef : JadeBoxTemplate
        {
            public override IdContainer GetId()
            {
                return nameof(GetChromaShift);
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


            [EntityLogic(typeof(ChromaShiftDef))]
            public sealed class GetChromaShift : JadeBox
            {
                private static Exhibit selectedExhibit;
                private static Exhibit secondaryExhibit;

                protected override void OnGain(GameRunController gameRun)
                {

                    GameMaster.Instance.StartCoroutine(GainExhibit(gameRun));
                    GameMaster.Instance.StartCoroutine(SetMana(gameRun));

                }

                private static IEnumerator GainExhibit(GameRunController gameRun)
                {
                    //list of all currently available starting exhibits
                    var exhibits = new List<Type> { typeof(CirnoG), typeof(CirnoU), typeof(MarisaB),
                        typeof(MarisaR), typeof(ReimuR), typeof(ReimuW), typeof(SakuyaU),typeof(SakuyaW),typeof(KoishiB),typeof(KoishiG) };

                    var rng = gameRun.AdventureRng;

                    
                    var toRemove = new List<Type>();
                    for (int i = 0; i < exhibits.Count; i++)
                    {
                        //remove exhibits with the colors the player has
                        var ex = exhibits[i];
                        var config = Library.CreateExhibit(ex).Config;
                        if (gameRun.BaseMana.HasColor(config.BaseManaColor.Value))
                        {
                            toRemove.Add(ex);
                            continue;
                        }


                        //remove exhibit if the resulting mana base would be identical to that of the gained exhibits owner
                        foreach (var secondaryExhibit in gameRun.ShiningExhibitPool)
                        {
                            var secodaryConfig = Library.CreateExhibit(secondaryExhibit).Config;

                            if (secondaryExhibit.Name != ex.Name && config.Owner == secodaryConfig.Owner)
                            {
                                Debug.Log("secondary exhibit of " + ex.Name + " is " + secondaryExhibit.Name);
                                if (secodaryConfig.BaseManaColor.Value == GetPrimaryExhibit(gameRun).Config.BaseManaColor.Value)
                                {
                                    Debug.Log("removing " + ex.Name + " because of vanilla color combination");

                                    toRemove.Add(ex);
                                }
                            }
                        }

                    }
                    foreach (var ex in toRemove)
                    {
                        exhibits.Remove(ex);
                    }

                    //randomly remove until one remains
                    int removeCount = exhibits.Count - 1;
                    for (int i = 0; i < removeCount; i++)
                    {
                        int remove = rng.NextInt(0, exhibits.Count - 1);
                        exhibits.RemoveAt(remove);
                    }

                    //Give the payer the randomly chosen exhibit
                    foreach (var ex in exhibits)
                    {
                        Debug.Log("giving exhibit: " + ex.Name);
                        selectedExhibit = Library.CreateExhibit(ex);
                        yield return gameRun.GainExhibitRunner(selectedExhibit, true);
                    }
                    gameRun.ExhibitPool.RemoveAll(e => exhibits.Contains(e));

                }

                private static Exhibit GetSecondaryExhibit(GameRunController gameRun)
                {
                    return Library.CreateExhibit((gameRun.PlayerType == PlayerType.TypeA) ? gameRun.Player.Config.ExhibitB : gameRun.Player.Config.ExhibitA);
                }

                private static Exhibit GetPrimaryExhibit(GameRunController gameRun)
                {
                    return Library.CreateExhibit((gameRun.PlayerType == PlayerType.TypeB) ? gameRun.Player.Config.ExhibitB : gameRun.Player.Config.ExhibitA);
                }

                private static IEnumerator SetMana(GameRunController gameRun)
                {
                    yield return null;

                    //remove mana just gained from the new exhibit
                    gameRun.LoseBaseMana(selectedExhibit.BaseMana, false);

                    //grab the starting exhibit the player didn't chose to determine the less abundant mana color
                    secondaryExhibit = GetSecondaryExhibit(gameRun);
                    Debug.Log("players other exhibit: " + secondaryExhibit.Name);

                    //check how much mana to remove and only add mana equal to the removed mana
                    ManaColor colorToRemove = secondaryExhibit.Config.BaseManaColor.Value;
                    int manaCount = gameRun.BaseMana.GetValue(colorToRemove);
                    Debug.Log("about to remove: " + manaCount + " mana of color: " + colorToRemove.ToLongName());
                    for (int i = 0; i < manaCount; i++)
                    {
                        gameRun.LoseBaseMana(secondaryExhibit.BaseMana, false);
                        gameRun.GainBaseMana(selectedExhibit.BaseMana, false);
                    }

                    yield return null;
                    ReplaceCards(gameRun);
                }

                private static void ReplaceCards(GameRunController gameRun)
                {
                    //If Cromatic Dominator is active, reroll the deck with the new mana and skip replacing basics
                    if (ResetStart50.ResetStart50Deck(gameRun))
                    {
                        return;
                    }

                    var toRemove = new List<Card>();
                    ManaColor colorToRemove = secondaryExhibit.Config.BaseManaColor.Value;
                    foreach (var card in gameRun.BaseDeck)
                    {
                        //check how many basic cards need to be removed
                        if (card.Config.Colors.Contains(colorToRemove))
                        {
                            toRemove.Add(card);
                        }
                    }

                    //add different color basic cards equal to the number of removed ones
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        GetBasicForExhibit(gameRun);
                    }

                    foreach (var card in toRemove)
                    {
                        gameRun.RemoveDeckCard(card);
                    }
                }

                private static void GetBasicForExhibit(GameRunController gameRun)
                {
                      

                    //select basic card to replace based on the gained exhibit
                    Debug.Log("about to add card for selected exhibit: " + selectedExhibit.Name);
                    if (selectedExhibit is CirnoG)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<CirnoBlockG>(1, false));
                    }
                    else if (selectedExhibit is CirnoU)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<CirnoBlockU>(1, false));
                    }

                    else if (selectedExhibit is MarisaB)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<MarisaBlockB>(1, false));
                    }
                    else if (selectedExhibit is MarisaR)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<MarisaBlockR>(1, false));
                    }

                    else if (selectedExhibit is ReimuR)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<ReimuBlockR>(1, false));
                    }
                    else if (selectedExhibit is ReimuW)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<ReimuBlockW>(1, false));
                    }

                    else if (selectedExhibit is SakuyaU)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<SakuyaBlockU>(1, false));
                    }
                    else if (selectedExhibit is SakuyaW)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<SakuyaBlockW>(1, false));
                    }
                    else if (selectedExhibit is KoishiG)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<KoishiBlockG>(1, false));
                    }
                    else if (selectedExhibit is KoishiB)
                    {
                        gameRun.AddDeckCards(Library.CreateCards<KoishiBlockB>(1, false));
                    }
                    else
                    {
                        //Use upgareded reimbu basics as fallback option
                        gameRun.AddDeckCards(Library.CreateCards<ReimuBlockR>(1, true));
                        Debug.LogError("cant find card for selected exhibit: " + selectedExhibit.Name);
                    }


                }

                

            }


        }
    }
}
