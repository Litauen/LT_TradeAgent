using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace LT_TradeAgent
{
    public partial class LTTABehaviour : CampaignBehaviorBase
    {

        public static LTTABehaviour? Instance { get; set; }

        public static Dictionary<Hero, LTTATradeData> TradeAgentsData = new();

        public bool BannerKingsActive = false;

        public const int TAPercent = 10;

        readonly bool _debug = false;

        public LTTABehaviour()
        {
            Instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));

            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickEvent);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTickEvent);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTickEvent);
        }


        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddDialogs(starter);
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
    }
}
