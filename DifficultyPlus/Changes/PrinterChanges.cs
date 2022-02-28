using BepInEx;
using R2API;
using static R2API.DirectorAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API.Utils;

namespace DifficultyPlus
{
    internal partial class DifficultyPlusPlugin : BaseUnityPlugin
    {
        public static GameObject whitePrinter = Resources.Load<GameObject>("prefabs/networkedobjects/chest/Duplicator");
        public static GameObject greenPrinter = Resources.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorLarge");
        public static GameObject redPrinter = Resources.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorMilitary");
        public static GameObject scrapper = Resources.Load<GameObject>("prefabs/networkedobjects/chest/Scrapper");

        private void EquipBarrelOccurrenceHook(List<DirectorCardHolder> cardList, StageInfo currentStage)
        {
            DirectorCardHolder equipBarrel = cardList.Find((card) => card.Card.spawnCard.name.ToLower() == DirectorAPI.Helpers.InteractableNames.EquipmentBarrel.ToLower());
            if (equipBarrel != null)
            {
                if (IsStageOne(currentStage.stage))
                {
                    equipBarrel.Card.selectionWeight = 6;//2
                }
                else if (!currentStage.CheckStage(Stage.Custom, ""))
                {
                    equipBarrel.Card.selectionWeight = 2;//2
                }
            }

            DirectorCardHolder equipShop = cardList.Find((card) => card.Card.spawnCard.name.ToLower() == "iscTripleShopEquipment".ToLower());
            if(equipShop != null)
            {
                if (IsStageThree(currentStage.stage))
                {
                    equipShop.Card.selectionWeight = 10;//2
                }
                else if (!currentStage.CheckStage(Stage.Custom, ""))
                {
                    equipShop.Card.selectionWeight = 2;//2
                }
            }
        }
        private void ScrapperOccurrenceHook(List<DirectorAPI.DirectorCardHolder> cardList, DirectorAPI.StageInfo currentStage)
        {
            if (OnScrapperStage(currentStage.stage))
            {
                DirectorCardHolder scrapper = cardList.Find((card) => IsScrapper(card));
                if (scrapper != null)
                {
                    scrapper.Card.selectionWeight = 25;//12
                }
            }
            else if (!currentStage.CheckStage(Stage.Custom, ""))
            {
                cardList.RemoveAll((card) => IsScrapper(card));
            }
        }
        private void PrinterOccurrenceHook(List<DirectorAPI.DirectorCardHolder> cardList, DirectorAPI.StageInfo currentStage)
        {
            if (OnPrinterStage(currentStage.stage))
            {
                List<DirectorCardHolder> printers = cardList.FindAll((card) => IsPrinter(card));

                foreach (DirectorAPI.DirectorCardHolder dc in cardList)
                {
                    GameObject cardPrefab = dc.Card.spawnCard.prefab;
                    if (dc.Card.selectionWeight != 0)
                    {
                        if (cardPrefab == greenPrinter)
                        {
                            dc.Card.selectionWeight = 10; //6
                        }
                        if (cardPrefab == redPrinter)
                        {
                            if (currentStage.stage == Stage.SkyMeadow)
                            {
                                dc.Card.selectionWeight = 12; //1
                            }
                            else
                            {
                                dc.Card.selectionWeight = 3; //1
                            }
                        }
                    }
                }
            }
            else if (!currentStage.CheckStage(Stage.Custom, ""))
            {
                cardList.RemoveAll((card) => IsPrinter(card));
            }
        }

        #region bools
        private bool IsPrinter(DirectorCardHolder card)
        {
            string cardName = card.Card.spawnCard.name.ToLower();
            return cardName == DirectorAPI.Helpers.InteractableNames.PrinterCommon.ToLower()
                || cardName == DirectorAPI.Helpers.InteractableNames.PrinterUncommon.ToLower()
                || cardName == DirectorAPI.Helpers.InteractableNames.PrinterLegendary.ToLower();
        }
        private bool IsScrapper(DirectorCardHolder card)
        {
            string cardName = card.Card.spawnCard.name.ToLower();
            return cardName == "iscScrapper".ToLower();
        }
        private bool OnPrinterStage(Stage stage)
        {
            return !OnScrapperStage(stage);
        }
        private bool OnScrapperStage(Stage stage)
        {
            return IsStageOne(stage)
                || IsStageThree(stage);
        }

        private bool IsStageOne(Stage stage)
        {
            return stage == Stage.TitanicPlains
                || stage == Stage.DistantRoost;
        }

        private bool IsStageThree(Stage stage)
        {
            return stage == Stage.RallypointDelta
                || stage == Stage.ScorchedAcres;
        }
        #endregion
    }
}
