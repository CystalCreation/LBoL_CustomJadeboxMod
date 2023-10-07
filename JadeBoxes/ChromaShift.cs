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
using LBoL.EntityLib.Cards.Character.Marisa;
using LBoL.EntityLib.Cards.Character.Reimu;
using LBoL.EntityLib.Cards.Character.Sakuya;
using LBoL.EntityLib.Cards.Neutral.NoColor;
using LBoL.EntityLib.Cards.Neutral.Red;
using LBoL.EntityLib.Cards.Other.Enemy;
using LBoL.EntityLib.Cards.Other.Misfortune;
using LBoL.EntityLib.Exhibits;
using LBoL.EntityLib.Exhibits.Common;
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
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Chroma Shift" },
                { "Description", "At the start of the run, gain a random character shining exhibit of a different color than the mana color in your mana base without increasing your base mana. " +
                "All mana in your mana base of your less abundant mana color and all basic block cards become the color of the new exhibit." 
                }
            });
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                return config;
            }


            [EntityLogic(typeof(ChromaShiftDef))]
            public sealed class GetChromaShift : JadeBox
            {
                private Exhibit selectedExhibit;
                private Exhibit secondaryExhibit;

                protected override void OnGain(GameRunController gameRun)
                {

                    GameMaster.Instance.StartCoroutine(GainExhibit(gameRun));
                    GameMaster.Instance.StartCoroutine(SetMana(gameRun));

                }

                private IEnumerator GainExhibit(GameRunController gameRun)
                {
                    //list of all currently available starting exhibits
                    var exhibits = new List<Type> { typeof(CirnoG), typeof(CirnoU), typeof(MarisaB),
                        typeof(MarisaR), typeof(ReimuR), typeof(ReimuW), typeof(SakuyaU),typeof(SakuyaW) };

                    var rng = gameRun.AdventureRng;

                    //remove exhibits with the colors the player has
                    var toRemove = new List<Type>();
                    for (int i = 0; i < exhibits.Count; i++)
                    {
                        var ex = exhibits[i];
                        if (gameRun.BaseMana.HasColor(Library.CreateExhibit(ex).Config.BaseManaColor.Value))
                        {
                            toRemove.Add(ex);
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

                private IEnumerator SetMana(GameRunController gameRun)
                {
                    yield return null;

                    //remove mana just gained from the new exhibit
                    gameRun.LoseBaseMana(selectedExhibit.BaseMana, false);

                    //grab the starting exhibit the player didn't chose to determine the less abundant mana color
                    secondaryExhibit = Library.CreateExhibit((gameRun.PlayerType == PlayerType.TypeA) ? gameRun.Player.Config.ExhibitB : gameRun.Player.Config.ExhibitA);
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

                    ReplaceCards();
                }

                private void ReplaceCards()
                {
                    var toRemove = new List<Card>();
                    ManaColor colorToRemove = secondaryExhibit.Config.BaseManaColor.Value;
                    foreach (var card in GameRun.BaseDeck)
                    {
                        //check how many basic cards need to be removed
                        if (card.Config.Colors.Contains(colorToRemove) && card.IsBasic)
                        {
                            toRemove.Add(card);
                        }
                    }

                    //add different color basic cards equal to the number of removed ones
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        GetBasicForExhibit();
                    }

                    foreach (var card in toRemove)
                    {
                        base.GameRun.RemoveDeckCard(card);
                    }
                }

                private void GetBasicForExhibit()
                {
                    //select basic card to replace based on the gained exhibit
                    Debug.Log("about to add card for selected exhibit: " + selectedExhibit.Name);
                    if (selectedExhibit is CirnoG)
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<CirnoBlockG>(1, false));
                    }
                    else if (selectedExhibit is CirnoU)
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<CirnoBlockU>(1, false));
                    }

                    else if (selectedExhibit is MarisaB)
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<MarisaBlockB>(1, false));
                    }
                    else if (selectedExhibit is MarisaR)
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<MarisaBlockR>(1, false));
                    }

                    else if (selectedExhibit is ReimuR)
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<ReimuBlockR>(1, false));
                    }
                    else if (selectedExhibit is ReimuW)
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<ReimuBlockW>(1, false));
                    }

                    else if (selectedExhibit is SakuyaU)
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<SakuyaBlockU>(1, false));
                    }
                    else if (selectedExhibit is SakuyaW)
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<SakuyaBlockW>(1, false));
                    }

                    else
                    {
                        base.GameRun.AddDeckCards(Library.CreateCards<ReimuBlockR>(1, true));
                        Debug.LogError("cant find card for selected exhibit: " + selectedExhibit.Name);
                    }


                }

            }


        }
    }
}
