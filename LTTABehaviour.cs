using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace LT_TradeAgent
{
    public partial class LTTABehaviour : CampaignBehaviorBase
    {

        public static LTTABehaviour? Instance { get; set; }

        public static Dictionary<Hero, LTTATradeData> TradeAgentsData = new();

        public bool BannerKingsActive = false;

        readonly bool _debug = false;

        public LTTABehaviour()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));

            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnNewGameCreated));

            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickEvent);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTickEvent);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTickEvent);
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            TradeAgentsData.Clear();
        }


        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            CleanBrokenTradeAgents();

            AddDialogs(starter);
            AddGameMenus(starter);
        }

        private void HourlyTickEvent()
        {
            //ProcessTradeAgents();
        }

        private void DailyTickEvent()
        {
            ProcessTradeAgents();
        }

        private void WeeklyTickEvent()
        {

        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<Dictionary<Hero, LTTATradeData>>("_tradeData", ref LTTABehaviour.TradeAgentsData);
        }



        [CommandLineFunctionality.CommandLineArgumentFunction("show_agents", "ltta")]
        public static string ConsoleShowAgents(List<string> args)
        {

            if (Instance == null) return $"Command failed. Instance == null";

            string output = "";

            foreach (KeyValuePair<Hero, LTTATradeData> td in TradeAgentsData)
            {
                Hero hero = td.Key;
                LTTATradeData tradeData = td.Value;

                if (hero.CurrentSettlement == null) continue;
                Town town = hero.CurrentSettlement.Town;

                output += hero.Name.ToString() + " (" + town.Name + ") [" + (tradeData.Active?"Active":"Passive") + "] Gold: " + tradeData.Balance.ToString() + " Fee: " + tradeData.FeePercent.ToString() +"%\n";
                              
            }

            return $"Total Trade Agents: " + TradeAgentsData.Count.ToString() + "\n" + output;

        }
    }
}
