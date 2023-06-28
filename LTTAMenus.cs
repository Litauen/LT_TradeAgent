using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace LT_TradeAgent
{
    public partial class LTTABehaviour : CampaignBehaviorBase
    {
        private void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town_backstreet", "trade_warehouse_menu", "Visit Trade Warehouse",
            (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Trade;

                Hero ? tradeAgent = GetSettlementsTradeAgent(Settlement.CurrentSettlement);
                if (tradeAgent == null) return false;
                LTTATradeData tradeData = GetTradeAgentTradeData(tradeAgent);
                if (tradeData == null) return false;

                if (tradeData.Balance < 0)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("The warehouse is locked. Guard tells you to see " + tradeAgent.Name.ToString(), null);
                    args.optionLeaveType = GameMenuOption.LeaveType.DonateTroops;
                } else
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Trade;
                }

                return true;
            },
            delegate (MenuCallbackArgs args)
            {
                WarehouseScreenFromMenu();
            }, false, 1, false);
        }




    }
}
