using LBoL.Base;
using LBoL.ConfigData;
using LBoL.Core;
using LBoL.Core.Battle;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Cards;
using LBoL.Core.StatusEffects;
using LBoL.Core.Units;
using LBoL.EntityLib.Cards.Neutral.Red;
using LBoL.EntityLib.Cards.Other.Enemy;
using LBoL.EntityLib.Cards.Other.Misfortune;
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
    public class AllUpgraded
    {
        public sealed class AllUpgradedDef : JadeBoxTemplate
        {
            private static int firepowerGain = 2;
            private static int regretsGain = 1;

            public override IdContainer GetId()
            {
                return nameof(AllUpgradedJadebox);
            }


            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Adored by All" },
                { "Description", "All cards that get added to your library are upgraded. " +
                "Non-shining exhibits that upgrade cards are removed from the pool. At the start of each combat, all enemies gain {Value1}"
                +" Firepower and {Value2} Lingering Regrets."}
            });
            }

            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = firepowerGain;
                config.Value2 = regretsGain;
                return config;
            }


            [EntityLogic(typeof(AllUpgradedDef))]
            public sealed class AllUpgradedJadebox : JadeBox
            {
                protected override void OnEnterBattle()
                {
                    ReactBattleEvent(Battle.Player.TurnStarted, OnBattleStarted);
                }


                private IEnumerable<BattleAction> OnBattleStarted(GameEventArgs args)
                {
                    if (Battle.Player.TurnCounter == 1)
                    {
                        NotifyActivating();
                        
                        //apply buffs to all enemies on the first turn
                        foreach (var enemy in Battle.AllAliveEnemies)
                        {
                            yield return new ApplyStatusEffectAction<LBoL.EntityLib.StatusEffects.Enemy.LoveGirlDamageReduce>(enemy, regretsGain, null, null, null, 0, false);
                            yield return new ApplyStatusEffectAction<Firepower>(enemy, firepowerGain);
                        }
                    }
                }

                protected override void OnGain(GameRunController gameRun)
                {
                    UpgradeAllCards();
                    GameMaster.Instance.StartCoroutine(RemoveFromPool(gameRun));
                }

                protected override void OnAdded()
                {
                    //card upgrade event needs to be added in OnAdded or else it will be gone after a reload
                    HandleGameRunEvent(GameRun.DeckCardsAdded, OnDeckCardAdded);
                }

                public void OnDeckCardAdded(CardsEventArgs args)
                {
                    try
                    {
                        UpgradeAllCards();

                    }
                    catch (Exception e)
                    {
                        Debug.LogError(" exception in OnDeckCardAdded: " + e.Message + e.StackTrace);
                    }
                }

                private void UpgradeAllCards()
                {
                    try
                    {
                        //always upgrae all card in the deck in case several card are added at once
                        Debug.Log("cards in deck: " + GameRun.BaseDeck.Count);
                        foreach (var card in GameRun.BaseDeck)
                        {
                            //check if card is not upgraded
                            if (card.CanUpgrade && card.CanUpgradeAndPositive && !card.IsUpgraded)
                            {
                                card.Upgrade();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(" exception in UpgradeAllCards: " + e.Message + e.StackTrace);
                    }
                }

                private IEnumerator RemoveFromPool(GameRunController gameRun)
                {
                    //remove exhibits that become useless with universal access to upgraded cards
                    var exhibit = new HashSet<Type> { typeof(Chaidao), typeof(Fengrenji),
                        typeof(Jiaobu), typeof(Shoubiao), typeof(Shouyinji), typeof(Zixingche)};

                    gameRun.ExhibitPool.RemoveAll(e => exhibit.Contains(e));
                    yield return null;
                }

            }


        }
    }


}

