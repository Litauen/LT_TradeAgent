using LT.Logger;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace LT_TradeAgent
{
    public partial class LTTABehaviour : CampaignBehaviorBase
    {
        public void WarehouseScreenFromMenu()
        {
            Hero? tradeAgent = GetSettlementsTradeAgent(Settlement.CurrentSettlement);
            if (tradeAgent == null) return;
            LTTATradeData tradeData = GetTradeAgentTradeData(tradeAgent);
            if (tradeData == null) return;

            InventoryManager.OpenScreenAsReceiveItems(tradeData.Stash, new TextObject("Trade Warehouse"), new InventoryManager.DoneLogicExtrasDelegate(() => { OnInventoryScreenDone(tradeData); }));
        }

        public void WarehouseScreenFromDialog()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;
            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            InventoryManager.OpenScreenAsReceiveItems(tradeData.Stash, new TextObject("Trade Warehouse"), new InventoryManager.DoneLogicExtrasDelegate(() => { OnInventoryScreenDone(tradeData); }));
        }

        private void OnInventoryScreenDone(LTTATradeData tradeData)
        {
            //LTLogger.IMTAGreen("Items in warehouse: " + tradeData.Stash.Count());
            // currently does nothing
        }


        private void ChargeForWaresInRentedWarehouses()
        {

            foreach (KeyValuePair<Hero, LTTATradeData> td in TradeAgentsData)
            {
                Hero hero = td.Key;
                LTTATradeData tradeData = td.Value;

                if (hero.CurrentSettlement == null) continue;
                Town town = hero.CurrentSettlement.Town;

                bool showLog = false;
                if (tradeData.SendsTradeInfo || _debug) showLog = true;

                if (tradeData == null) return;

                int totalWares = tradeData.GetTotalWaresCountInStash();

                int warehouseFee = (int)(totalWares / 100f);

                tradeData.Balance -= warehouseFee;

                if (showLog) LTLogger.IMTAGreen("Trade Agent " + hero.Name.ToString() + " (" + town.Name.ToString() + ") charged " + warehouseFee.ToString() + "{GOLD_ICON} for " + totalWares.ToString() + " wares in the warehouse");
            }
        }

    }
}
