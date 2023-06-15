using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace LT_TradeAgent
{
    [SaveableRootClass(2)]
    public class TradeItemData
    {
        [SaveableField(1)]
        public ItemObject item;

        [SaveableField(2)]
        public int minPrice;

        [SaveableField(3)]
        public int maxPrice;

        [SaveableField(4)]
        public int maxItemAmount;

        [SaveableField(5)]
        public int maxGoldAmount;

        [SaveableField(6)]
        public int spentTotal;     // how much spent gold for these items from the start of the contract

        [SaveableField(7)]
        public int boughTotal;       // how many items bought from the start of the contract
    
        public TradeItemData(ItemObject item) 
        { 
            this.item = item;
            this.minPrice = 0;
            this.maxPrice = -1;          // unlimited
            this.maxItemAmount = -1;     // unlimited
            this.maxGoldAmount = -1;     // unlimited
            this.spentTotal = 0;
            this.boughTotal= 0;
        }

    }

    [SaveableRootClass(1)]
    public class LTTATradeData
    {

        [SaveableField(1)]
        public Hero Hero;

        [SaveableField(2)]
        public bool Active;

        [SaveableField(3)]
        public int Balance;

        [SaveableField(4)]
        public ItemRoster Stash;

        [SaveableField(5)]
        public List<TradeItemData> TradeItemsDataList;

        public LTTATradeData(Hero hero)
        {
            this.Hero = hero;
            this.Active = false;
            this.Balance = 0;
            this.Stash = new ItemRoster();

            this.TradeItemsDataList = new List<TradeItemData>();
        }

        public int GetTotalWaresCountInStash()
        {
            int totalCount = 0;

            for (int i = 0; i < Stash.Count; i++)
            {
                ItemObject item = Stash.GetItemAtIndex(i);
                totalCount += Stash.GetItemNumber(item);
            }

            return totalCount;
        }

        public bool RemoveTradeItem(ItemObject item)
        {
            if (item == null) return false;
            if (this.TradeItemsDataList.Count == 0) return false;
            int i = 0;
            int found = -1;
            foreach (TradeItemData ware in this.TradeItemsDataList)
            {
                if (item == ware.item)
                {
                    found = i; 
                    break;
                }
                i++;
            }
            if (found == -1) return false;

            this.TradeItemsDataList.RemoveAt(found);

            return true;
        }
    }


    public class CustomSaveDefiner : SaveableTypeDefiner
    {
        public CustomSaveDefiner() : base(1885468685) { }

        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(LTTATradeData), 1);
            base.AddClassDefinition(typeof(TradeItemData), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            base.ConstructContainerDefinition(typeof(Dictionary<Hero, LTTATradeData>));
            base.ConstructContainerDefinition(typeof(List<TradeItemData>));
        }
    }

}

