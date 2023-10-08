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

namespace CustomJadebox.Util
{
    public class ResetStart50
    {
        //50 card deck gets generated before the mana gets switched with Chroma Shift or RollCards get overwritten with Forget your Name so the deck has to be cleared and rerolled
        public static bool ResetStart50Deck(GameRunController run, bool neutralOnlyActive = false)
        {
            if (run == null)
            {
                return false;
            }

            IReadOnlyList<JadeBox> jadeBox = run.JadeBox;

            if (jadeBox != null && jadeBox.Count > 0)
            {

                foreach (var item in jadeBox)
                {
                    if (item is Start50)
                    {
                        run.RemoveDeckCards(run.BaseDeck, false);
                        for (int i = 0; i < item.Value1; i++)
                        {
                            OwnerWeightTable ownerTable = OwnerWeightTable.Valid;
                            if (neutralOnlyActive)
                            {
                                ownerTable = OwnerWeightTable.OnlyNeutral;
                            }
                            Card[] cards = run.RollCards(run.CardRng, new CardWeightTable(RarityWeightTable.EnemyCard, ownerTable, CardTypeWeightTable.CanBeLoot), 1, false, null);
                            run.AddDeckCards(cards, false, null);
                        }

                        return true;
                    }
                }
            }
            return false;
        }

    }
}
