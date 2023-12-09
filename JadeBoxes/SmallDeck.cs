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
using LBoL.EntityLib.Cards.Character.Marisa;
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

namespace CustomJadebox.JadeBoxes
{
    public class SmallDeck
    {
        public sealed class SmallDeckDef : JadeBoxTemplate
        {
            public static int maxDeckSize = 15;

            public override IdContainer GetId()
            {
                return nameof(SmallDeckJadebox);
            }


            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Growth Inhibition" },
                { "Description", "Whenever you add a card to your Library when you have {Value1}" +
                " or more cards in your Library, remove cards than can be removed from your Library until {Value1} remain. Cards are removed in the order they where added to the Library."}
            });
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = maxDeckSize;
                return config;
            }


            [EntityLogic(typeof(SmallDeckDef))]
            public sealed class SmallDeckJadebox : JadeBox
            {

                protected override void OnAdded()
                {
                    //needs to be added in OnAdded or else it will be gone after a reload
                    HandleGameRunEvent(GameRun.DeckCardsAdded, OnDeckCardAdded);
                }


                public void OnDeckCardAdded(CardsEventArgs args)
                {
                    var toRemove = new List<Card>();
                    foreach (var card in GameRun.BaseDeck)
                    {
                        if (GameRun.BaseDeck.Count - toRemove.Count > maxDeckSize)
                        {
                            if (card.Unremovable)
                            {
                                continue;
                            }
                            toRemove.Add(card);
                        }
                    }
                    foreach (var card in toRemove)
                    {
                        base.GameRun.RemoveDeckCard(card,true);
                    }

                }

            }


        }

    }

}

