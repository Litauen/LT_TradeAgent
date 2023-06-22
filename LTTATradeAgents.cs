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
                LTTATradeData tradeData2 = new(hero);
                LTTABehaviour.TradeAgentsData.Add(hero, tradeData2);
                result = tradeData2;
            }

            if (result.FeePercent == 0) result.FeePercent = 10;

            //LTLogger.IMRed("GetTradeAgentTradeData");

            return result;

        }

        private int GetTradeAgentGold(Hero hero)
        {           
            LTTATradeData tradeData = GetTradeAgentTradeData(hero);
            return tradeData.Balance;
        }

        private Hero? GetSettlementsTradeAgent(Settlement town)
        {
            if (town == null || !town.IsTown) return null;

            //LTLogger.IMTAGreen("GetSettlementsTradeAgent");

            foreach (KeyValuePair<Hero, LTTATradeData> td in TradeAgentsData)
            {
                Settlement s = td.Key.CurrentSettlement;
                if (s != null && s == town && td.Value != null)
                {
                    //LTLogger.IMTAGreen("GetSettlementsTradeAgent -> " + td.Key.Name.ToString());
                    return td.Key;
                }
            }

            //LTLogger.IMTAGreen("GetSettlementsTradeAgent -> null");
            return null;
        }

        float GetItemPriceFromSettlement(Settlement settlement, ItemObject item, bool isSelling = false, int feePercent = 0)
        {
            float itemPrice = -1;   // error

            if (settlement.IsVillage)
            {
                if (settlement.Village == null || settlement.Village.MarketData == null || settlement.Village.Settlement == null) return -1;
                itemPrice = settlement.Village.MarketData.GetPrice(item, MobileParty.MainParty, isSelling, settlement.Village.Settlement.Party);
            }

            if (settlement.IsTown)
            {
                if (settlement.Town == null || settlement.Town.MarketData == null || settlement.Town.Settlement == null) return -1;
                itemPrice = settlement.Town.MarketData.GetPrice(item, MobileParty.MainParty, isSelling, settlement.Town.Settlement.Party);
            }

            if (feePercent > 0)
            {
                itemPrice += itemPrice / 100f * feePercent;     //add TA percent
            }

            return itemPrice;
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

            // TODO: Town tax calculations
        }


        private void SellItems(ItemObject item, int itemCount, int goldAmount, Settlement toSettlement, LTTATradeData tradeData)
        {
            if (itemCount == 0 || goldAmount == 0 || item == null) return;
            if (toSettlement == null) return;
            if (tradeData == null) return;

            toSettlement.ItemRoster.AddToCounts(item, itemCount);
            tradeData.Stash.AddToCounts(item, itemCount * (-1));

            toSettlement.SettlementComponent.ChangeGold(goldAmount * (-1));

            int goldWithoutTax = goldAmount / (100 + tradeData.FeePercent) * 100;          
            tradeData.Balance += goldWithoutTax;

            // TODO: Town tax calculations
        }


        private int SellTAItems()
        {
            int totalWares = 0;

            foreach (KeyValuePair<Hero, LTTATradeData> td in TradeAgentsData)
            {
                Hero hero = td.Key;
                LTTATradeData tradeData = td.Value;

                if (hero.CurrentSettlement == null) continue;
                Town town = hero.CurrentSettlement.Town;

                bool showLog = false;
                if (tradeData.SendsTradeInfo || _debug) showLog = true;

                if (tradeData.Active == false)
                {
                    if (showLog) LTLogger.IMTAGreen("Trade Agent " + hero.Name.ToString() + " (" + town.Name.ToString() + ") is not selling currently as you asked.");
                    continue;
                }

                List<Settlement> settlements = new() { town.Settlement };
                foreach (Village village in town.TradeBoundVillages) settlements.Add(village.Settlement);

                if (showLog)
                {
                    if (tradeData.TradeItemsDataList.Count > 0) LTLogger.IMTAGreen("Trade Agent " + hero.Name.ToString() + " (" + town.Name.ToString() + ") tries to sell:");
                        else LTLogger.IMTAGreen("Trade Agent " + hero.Name.ToString() + " (" + town.Name.ToString() + ") does not have wares to sell.");
                }

                // select wares to sell
                foreach (TradeItemData ware in tradeData.TradeItemsDataList)
                {
                    if (ware.item == null) continue;

                    // check sell status
                    if (ware.minPrice == 0)
                    {
                        if (showLog) LTLogger.IMGrey(ware.item.Name.ToString() + " marked as not to sell, skipping");
                        continue;
                    }

                    // select batch size
                    int batchSize = 1;
                    if (ware.item.Value < 20) batchSize = 50;
                    else if (ware.item.Value < 40) batchSize = 10;


                    // repeat until no settlement wants to buy or everything is sold
                    for (; ; )
                    {

                        // check amount
                        int itemCount = tradeData.Stash.GetItemNumber(ware.item);
                        if (itemCount == 0)
                        {
                            if (showLog) LTLogger.IMGrey("  " + ware.item.Name.ToString() + " 0 in storage, skipping");
                            //continue;
                            break;
                        }
                        // control batchSize based on available item count
                        if (batchSize > itemCount) batchSize = itemCount;


                        if (showLog) LTLogger.IMGrey(itemCount.ToString() + " x " + ware.item.Name.ToString() + ", will try to sell x" + batchSize.ToString() + " if price > " + ware.minPrice.ToString());



                        // find the settlement where we can sell our batch with the highest price
                        Settlement? bestSettlement = null;
                        float bestSellPrice = 0;

                        foreach (Settlement settlement in settlements)
                        {

                            float sellPrice = GetItemPriceFromSettlement(settlement, ware.item, true, tradeData.FeePercent);

                            // check settlement gold
                            int sGold = settlement.SettlementComponent.Gold;
                            if (sGold < sellPrice)
                            {
                                if (showLog) LTLogger.IMGrey("  " + settlement.Name.ToString() + " does not have enough gold.");
                                continue;
                            }

                            float batchPrice = batchSize * sellPrice;

                            // adjust batch size based on settlement's gold and batch price
                            if (sGold < batchPrice)
                            {
                                batchSize = (int)((float)sGold / sellPrice);
                                batchPrice = batchSize * sellPrice;
                            }

                            if (showLog)
                            {
                                string additionalInfo = " x" + batchSize.ToString() + " would cost " + batchPrice.ToString();
                                if (sellPrice < (float)ware.minPrice) additionalInfo = "Too cheap to sell (<" + ware.minPrice.ToString() + ")";
                                LTLogger.IMGrey("  " + settlement.Name.ToString() + " buys " + ware.item.Name.ToString() + " for " + sellPrice.ToString() + ". " + additionalInfo);
                            }

                            if (sellPrice >= (float)ware.minPrice && sellPrice > bestSellPrice)
                            {
                                bestSellPrice = sellPrice;
                                bestSettlement = settlement;
                            }
                        }
                        if (bestSettlement != null)
                        {

                            totalWares += batchSize;

                            int batchPrice = (int)(batchSize * bestSellPrice);
                            if (showLog) LTLogger.IMGrey("  " + batchSize.ToString() + " x " + ware.item.Name.ToString() + " sold to " + bestSettlement.Name.ToString() + " for " + batchPrice.ToString() + " (tax not deducted). Price/item: " + bestSellPrice.ToString());
                            SellItems(ware.item, batchSize, batchPrice, bestSettlement, tradeData);
                        }
                        else
                        {
                            if (showLog) LTLogger.IMGrey("  No settlement wants to buy " + ware.item.Name.ToString() + " with price > " + ware.minPrice);
                            break;
                        }

                    }


                }

            }

            return totalWares;
        }


        private int BuyTAItems()
        {
            int totalWares = 0;

            // Buying
            foreach (KeyValuePair<Hero, LTTATradeData> td in TradeAgentsData)
            {
                Hero hero = td.Key;
                LTTATradeData tradeData = td.Value;

                if (hero.CurrentSettlement == null) continue;
                Town town = hero.CurrentSettlement.Town;

                bool showLog = false;
                if (tradeData.SendsTradeInfo || _debug) showLog = true;

                if (tradeData.Active == false)
                {
                    if (showLog) LTLogger.IMTAGreen("Trade Agent " + hero.Name.ToString() + " (" + town.Name.ToString() + ") is not buying currently as you asked.");
                    continue;
                }

                List<Settlement> settlements = new() { town.Settlement };
                foreach (Village village in town.TradeBoundVillages) settlements.Add(village.Settlement);

                if (showLog)
                {
                    if (tradeData.TradeItemsDataList.Count > 0) LTLogger.IMTAGreen("Trade Agent " + hero.Name.ToString() + " (" + town.Name.ToString() + ") tries to buy:");
                     else LTLogger.IMTAGreen("Trade Agent " + hero.Name.ToString() + " (" + town.Name.ToString() + ") does not have wares to buy.");
                }

                foreach (Settlement settlement in settlements)
                {
                    foreach (TradeItemData ware in tradeData.TradeItemsDataList)
                    {
                        if (ware.item == null) continue;

                        // check how many items this settlement has
                        int itemCount = settlement.ItemRoster.GetItemNumber(ware.item);
                        if (itemCount == 0)
                        {
                            if (showLog) LTLogger.IMGrey("  " + settlement.Name.ToString() + " does not have " + ware.item.Name + " to sell.");
                            continue;
                        }

                        float itemPrice = GetItemPriceFromSettlement(settlement, ware.item, false, tradeData.FeePercent);

                        if (ware.maxPrice != -1 && itemPrice > ware.maxPrice)
                        {
                            if (showLog) LTLogger.IMGrey("  " + settlement.Name.ToString() + " sells " + ware.item.Name + " for " + itemPrice.ToString() + ". Too expensive to buy (>" + ware.maxPrice.ToString() + ")");
                            continue;
                        }

                        int goldAmount = (int)(itemCount * itemPrice);

                        if (goldAmount > tradeData.Balance)
                        {
                            itemCount = (int)(tradeData.Balance / itemPrice);
                            goldAmount = (int)(itemPrice * itemCount);
                        }

                        if (itemCount == 0)
                        {
                            if (showLog) LTLogger.IMGrey("  Not enough gold to buy " + ware.item.Name.ToString() + " in " + settlement.Name.ToString() + " for " + itemPrice.ToString() + ".  Gold left: " + tradeData.Balance.ToString());
                            continue;
                        }

                        BuyItems(ware.item, itemCount, goldAmount, settlement, tradeData);

                        totalWares += itemCount;

                        if (showLog) LTLogger.IMGrey("  Bought " + itemCount.ToString() + " x " + ware.item.Name.ToString() + " in " + settlement.Name.ToString() + " for " + goldAmount.ToString() + "  price/item: " + itemPrice.ToString());
                    }
                }

                if (showLog)
                {
                    // print TA stash
                    string wareData = "";
                    for (int i = 0; i < tradeData.Stash.Count; i++)
                    {
                        ItemObject item = tradeData.Stash.GetItemAtIndex(i);
                        int itemCount = tradeData.Stash.GetItemNumber(item);
                        wareData += " " + item.Name.ToString() + " [" + itemCount.ToString() + "]";
                    }
                    LTLogger.IMTAGreen(hero.Name.ToString() + "'s (" + town.Name.ToString() + ") Balance: " + tradeData.Balance.ToString() + "  Wares in storage: " + wareData);
                }

            }

            return totalWares;
        }




        private void ProcessTradeAgents()
        {

            int totalAgents = TradeAgentsData.Count;
            if (totalAgents == 0) return;

            int totalSold = SellTAItems();
            int totalBought = BuyTAItems();

            // status report
            string ta = "Trade Agent";  if (totalAgents > 1) ta = "Trade Agents";
            string statusReport = totalAgents.ToString() + " " + ta + " sold " + totalSold.ToString() + " and bought " + totalBought.ToString() + " wares.";
            LTLogger.IMTAGreen(statusReport);

        }


        // fee percent notable charges for his services
        int GetFeePercent(Hero hero)
        {
            int feePercent = 20;

            if (hero.IsMerchant) feePercent = 8;          // merchant just doing his work
            else if (hero.IsArtisan) feePercent = 14;     // artisan is busy with his artisan stuff
            else if (hero.IsGangLeader) feePercent = 20;  // gang leader hires somebody else to trade for you, he's just a middle-man

            feePercent += (int)(hero.Power / 100);

            return feePercent;
        }

        // what relation is necessary to hire some notable
        int GetNecessaryRelationForHire(Hero notable)
        {
            int relation = -5; //if (notable.IsGangLeader)  // does deals with everybody he doesn't hate

            if (notable.IsArtisan) relation = 10;           // he is busy doing his artisan stuff
            else if (notable.IsMerchant) relation = 20;     // he is busy by his own trade

            relation += (int)(notable.Power / 100 * 3);     // more powerfull - needs more relation, powerfull cares less about you

            return relation;
        }

        // how many Trade Agents player can hire
        int GetTALimit()
        {
            return Clan.PlayerClan.Tier * 3 + 1;
        }

        bool CanHaveMoreTA()
        {
            if (TradeAgentsData.Count < GetTALimit()) return true;
            return false;
        }
    }
}
