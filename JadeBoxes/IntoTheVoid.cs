using LBoL.ConfigData;
using LBoL.Core.Battle.BattleActions;
using LBoL.Core.Battle;
using LBoL.Core.StatusEffects;
using LBoL.Core.Units;
using LBoL.Core;
using LBoL.EntityLib.Exhibits.Common;
using LBoL.Presentation;
using LBoLEntitySideloader.Attributes;
using LBoLEntitySideloader.Entities;
using LBoLEntitySideloader.Resource;
using LBoLEntitySideloader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LBoL.EntityLib.StatusEffects.Neutral.Black;
using LBoL.Core.Cards;
using System.Linq;
using LBoL.Base;
using LBoL.Base.Extensions;
using LBoL.Presentation.UI.Panels;
using UnityEngine;

namespace CustomJadebox.JadeBoxes
{
    public class IntoTheVoid
    {

        public sealed class IntoTheVoidDef : JadeBoxTemplate
        {

            public override IdContainer GetId()
            {
                return nameof(GetIntoTheVoid);
            }
            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                return config;
            }


            public override LocalizationOption LoadLocalization()
            {
                return BepinexPlugin.JadeboxBatchLoc.AddEntity(this);
            }



            [EntityLogic(typeof(IntoTheVoidDef))]
            public sealed class GetIntoTheVoid : JadeBox
            {
                protected override void OnEnterBattle()
                {
                    ReactBattleEvent(Battle.Player.TurnStarted, OnBattleStarted);
                }


                private IEnumerable<BattleAction> OnBattleStarted(GameEventArgs args)
                {
                    if (Battle.Player.TurnCounter == 1)
                    {
                        //Add exile and cost reduction to all cards in hand and draw pile
                        List<Card> list = base.Battle.DrawZone.ToList<Card>();

                        list.AddRange(base.Battle.HandZone);

                        if (list.Count > 0)
                        {
                            SetupCards(list);
                        }

                        //Set up event handlers to apply cost reduction and exile to cards generated mid battle
                        base.HandleBattleEvent<CardsEventArgs>(base.Battle.CardsAddedToDiscard, new GameEventHandler<CardsEventArgs>(this.OnAddCard));
                        base.HandleBattleEvent<CardsEventArgs>(base.Battle.CardsAddedToHand, new GameEventHandler<CardsEventArgs>(this.OnAddCard));
                        base.HandleBattleEvent<CardsEventArgs>(base.Battle.CardsAddedToExile, new GameEventHandler<CardsEventArgs>(this.OnAddCard));
                        base.HandleBattleEvent<CardsAddingToDrawZoneEventArgs>(base.Battle.CardsAddedToDrawZone, new GameEventHandler<CardsAddingToDrawZoneEventArgs>(this.OnAddCardToDraw));
                    }
                    yield return null;
                }

               

                private void SetupCards(List<Card> list)
                {
                    foreach (Card card in list)
                    {
                        if(!card.IsForbidden && !card.IsBasic )
                        {
                            if (card.Cost.Amount > 0)
                            {
                                card.DecreaseBaseCost(ManaGroup.FromComponents(card.Cost.EnumerateComponents().SampleManyOrAll(1, base.GameRun.BattleRng)));
                            }                            

                            card.IsExile = true;
                            card.NotifyActivating();
                        }
                    }
                }
               

                private void OnAddCardToDraw(CardsAddingToDrawZoneEventArgs args)
                {
                    SetupCards(args.Cards.ToList());
                }

                public void OnAddCard(CardsEventArgs args)
                {
                    SetupCards(args.Cards.ToList());
                }         

            }

    }



}
}
