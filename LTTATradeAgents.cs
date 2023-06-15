using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using LT.Logger;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Party;

namespace LT_TradeAgent
{
    public partial class LTTABehaviour : CampaignBehaviorBase
    {
        private LTTATradeData GetTradeAgentTradeData(Hero hero)
        {
            LTTATradeData tradeData;
            LTTATradeData result;

            if (TradeAgentsData.TryGetValue(hero, out tradeData))
            {
                result = tradeData;
            }
            else
            {
                LTTATradeData tradeData2 = new LTTATradeData(hero);
                LTTABehaviour.TradeAgentsData.Add(hero, tradeData2);
                result = tradeData2;
            }
            return result;

        }


        private Hero? GetSettlementsTradeAgent(Settlement town)
        {
            if (town == null || !town.IsTown) return null;

            foreach (KeyValuePair<Hero, LTTATradeData> td in TradeAgentsData)
            {
                Settlement s = td.Key.CurrentSettlement;
                if (s != null && s == town && td.Value != null && td.Value.Active) return td.Key;
            }

            return null;
        }


        private void BuyItems(ItemObject item, int itemCount, int goldAmount, Settlement fromSettlement, LTTATradeData tradeData)
        {
            if (itemCount == 0 || goldAmount == 0 || item == null) return;
            if (fromSettlement == null) return;
            if (tradeData == null) return;

            fromSettlement.ItemRoster.AddToCounts(item, itemCount * (-1));
            tradeData.Stash.AddToCounts(item, itemCount);

            fromSettlement.SettlementComponent.ChangeGold(goldAmount);
            tradeData.Balance -= goldAmount;
        }



        private void ProcessTradeAgents()
        {

            int totalTA = 0;
            int totalWares = 0;
            int outOfMoney = 0;

            foreach (KeyValuePair<Hero, LTTATradeData> td in TradeAgentsData)
            {
                Hero hero = td.Key;
                LTTATradeData tradeData = td.Value;

                if (tradeData.Active == false) continue;
                if (hero.CurrentSettlement == null) continue;

                Town town = hero.CurrentSettlement.Town;

                //if (town.TradeBoundVillages.Count == 0) continue;

                if (_debug) LTLogger.IMGreen("Processing TA " + hero.Name.ToString() + " in " + town.Name.ToString());

                totalTA++;

                // villages
                foreach(Village village in town.TradeBoundVillages)
                {

                    foreach (TradeItemData ware in tradeData.TradeItemsDataList)
                    {
                        if (ware.item == null) continue;                      

                        // check how many items this village has
                        int itemCount = village.Settlement.ItemRoster.GetItemNumber(ware.item);
                        if (itemCount == 0) continue;
                        float  itemPrice = village.MarketData.GetPrice(ware.item, MobileParty.MainParty, false, village.Settlement.Party);

                        itemPrice += itemPrice / 100f * TAPercent;     //add TA percent

                        int goldAmount = (int)(itemCount * itemPrice);

                        if (goldAmount > tradeData.Balance) 
                        {
                            itemCount = (int)(tradeData.Balance / itemPrice);
                            goldAmount = (int)(itemPrice * itemCount);

                            outOfMoney += 1;
                        }
                       
                        BuyItems(ware.item, itemCount, goldAmount, village.Settlement, tradeData);

                        totalWares += itemCount;

                        if (_debug) LTLogger.IMGrey("  Village: " + village.Name.ToString() + "  " + ware.item.Name.ToString() + ": " + itemCount.ToString() + "  price: " + itemPrice.ToString() + "  total gold: " + goldAmount.ToString());
                    }
                }


                // for town itself
                foreach (TradeItemData ware in tradeData.TradeItemsDataList)
                {
                    if (ware.item == null) continue;
                    
                    // check how many items this village has
                    int itemCount = town.Settlement.ItemRoster.GetItemNumber(ware.item);
                    if (itemCount == 0) continue;
                    int itemPrice = town.MarketData.GetPrice(ware.item, MobileParty.MainParty, false, town.Settlement.Party);
                    int goldAmount = itemCount * itemPrice;

                    if (goldAmount > tradeData.Balance)
                    {
                        itemCount = tradeData.Balance / itemPrice;
                        goldAmount = itemPrice * itemCount;

                        outOfMoney += 1;
                    }

                    BuyItems(ware.item, itemCount, goldAmount, town.Settlement, tradeData);

                    totalWares += itemCount;

                    if (_debug) LTLogger.IMGrey("  Town: " + town.Name.ToString() + "  " + ware.item.Name.ToString() + ": " + itemCount.ToString() + "  price: " + itemPrice.ToString() + "  total gold: " + goldAmount.ToString());
                }

                if (_debug)
                {
                    // print TA stash
                    for (int i = 0; i < tradeData.Stash.Count; i++)
                    {
                        ItemObject item = tradeData.Stash.GetItemAtIndex(i);
                        int itemCount = tradeData.Stash.GetItemNumber(item);
                        LTLogger.IMBlue("  In TA stash: " + item.Name.ToString() + ": " + itemCount.ToString());
                    }
                    LTLogger.IMBlue("  TA balance: " + tradeData.Balance.ToString());
                }

            }

            if (totalTA == 0) return;

            // status report
            string ta = "Trade Agent";
            if (totalTA > 1) ta = "Trade Agents";

            string statusReport = totalTA.ToString() + " " + ta + " bought " + totalWares.ToString() + " wares.";

            string outOfMoneyReport = "";
            if (outOfMoney > 0)
            {
                outOfMoneyReport = outOfMoney.ToString() + " of them is out of money.";
            }

            LTLogger.IMGrey(statusReport + " " + outOfMoneyReport);

        }


    }
}
