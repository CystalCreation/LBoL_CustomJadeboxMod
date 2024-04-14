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
    public class PurchasePower
    {

        public sealed class PurchasePowerDef : JadeBoxTemplate
        {
            private static int cardReduction = 2;



            public override IdContainer GetId()
            {
                return nameof(GetPurchasePower);
            }
            public override JadeBoxConfig MakeConfig()
            {
                var config = DefaultConfig();
                config.Value1 = cardReduction;
                return config;
            }



            public override LocalizationOption LoadLocalization()
            {
                return new DirectLocalization(new Dictionary<string, object>() {
                { "Name", "Capitalism, Ho!" },
                { "Description", "At the start of the run, gain the |Membership Card| and the |Portal Gun|." + 
                " Card rewards \nhave two less options."}
            });
            }



            [EntityLogic(typeof(PurchasePowerDef))]
            public sealed class GetPurchasePower : JadeBox
            {
                


                

                protected override void OnGain(GameRunController gameRun)
                {
                    GameMaster.Instance.StartCoroutine(GainExhibits(gameRun));
                }

                private IEnumerator GainExhibits(GameRunController gameRun)
                {
                    //give player the Membership Card and the Portal Gun
                    var exhibit = new HashSet<Type> { typeof(PortalGun), typeof(Huiyuanka) };
                    foreach (var ex in exhibit)
                    {
                        Debug.Log("gaining: " + ex.Name + " was in pool: " + (gameRun.ExhibitPool.Contains(ex)));

                        yield return gameRun.GainExhibitRunner(Library.CreateExhibit(ex));
                    }

                    gameRun.ExhibitPool.RemoveAll(e => exhibit.Contains(e));
                }

                private static void Init(GameRunController gameRun)
                {
                    gameRun.AdditionalRewardCardCount -= cardReduction;
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

