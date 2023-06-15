using LT.Logger;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace LT_TradeAgent
{
    public partial class LTTABehaviour : CampaignBehaviorBase
    {
        private void AddDialogs(CampaignGameStarter starter)
        {

            // hiring new Trade Agent
            starter.AddPlayerLine("lt_trade_agent_new", "hero_main_options", "lt_trade_agent_new_intro", "I want to hire you to buy me some goods when I am not around.", IsHeroPotentialTradeAgent, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent_new", "lt_trade_agent_new_intro", "lt_trade_agent_new_intro2", "I think ... I can do that. For an additional {COMMISSION_PERC}% fee of course. I have connections in {TRADE_SETTLEMENTS}{TOWN_NAME}. [ib:confident3][if:convo_excited]", null, null, 100, null);

            starter.AddPlayerLine("lt_trade_agent_new", "lt_trade_agent_new_intro2", "lt_trade_agent_new_agreement", "Great. For the start of the contract take 10000{GOLD_ICON}.", null, SetNewTradeAgent, 100, (out TextObject explanation) =>
                {
                    if (Hero.MainHero.Gold < 10000)
                    {
                        explanation = new TextObject("Not enough gold...");
                        return false; 
                    }
                    else
                    {
                        explanation = new TextObject("10000 {GOLD_ICON}");
                        return true;
                    }
                }
            );
            starter.AddPlayerLine("lt_trade_agent_new", "lt_trade_agent_new_intro2", "lt_trade_agent_new_nvm", "I changed my mind.", null, null, 100, null, null);

            starter.AddDialogLine("lt_trade_agent_new", "lt_trade_agent_new_nvm", "hero_main_options", "Ok then.[ib:warrior][if:convo_grave]", null, null, 100, null);

            starter.AddDialogLine("lt_trade_agent_new", "lt_trade_agent_new_agreement", "lt_trade_agent_options", "Ok. I will proceed with our agreement. Anything else?", null, null, 100, null);

            // another Trade Agent already active in the Town
            starter.AddPlayerLine("lt_trade_agent_other", "hero_main_options", "lt_trade_agent_other", "I want to hire you to buy me some goods when I am not around.", IsTradeAgentPresentInTown, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent_other", "lt_trade_agent_other", "hero_main_options", "As far as I know you already have agreement with {TA_NAME}.[ib:confident3][if:convo_mocking_revenge]", null, null, 100, null);


            // Trade Agent hired
            starter.AddPlayerLine("lt_trade_agent", "hero_main_options", "lt_trade_agent_intro", "About our agreement to buy wares...", IsHeroTownsTradeAgent, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_intro", "lt_trade_agent_options", "{TA_WARES} [ib:normal2][if:convo_calm_friendly]", null, FormatTextVariables, 100, null);

            // options
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_gold", "Let's talk about gold", null, null, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_amounts", "About the wares...", null, null, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_storage", "Show me what you bought", null, null, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_stop", "I want to end our agreement to buy wares", null, null, 100, null, null);

            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_new_nvm", "That will be all.", null, null, 100, null, null);

            // stop the contract
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_stop", "lt_trade_agent_stop2", "Pitty, that was a good contract. Let me transfer remaining balance and all the wares to you now...[ib:nervous][if:convo_shocked]", null, null, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_stop2", "lt_trade_agent_stop3", "Ok, let's end this", null, EndContract, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_stop2", "lt_trade_agent_stop4", "Actually, I changed my mind. Keep up the good work.", null, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_stop3", "hero_main_options", "As you wish.", null, null, 100, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_stop4", "lt_trade_agent_options", "That's what I though.[ib:normal2][if:convo_calm_friendly]", null, null, 100, null);

            // storage
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_storage", "lt_trade_agent_storage2", "{TA_WARES_IN_STORAGE} [ib:hip]", null, FormatTextVariableStorage, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_storage2", "lt_trade_agent_storage3", "I want to take all the wares you bought", null, TransferWares, 100, (out TextObject explanation) =>
            {
                if (!TradeAgentHasWaresInStash())
                {
                    explanation = new TextObject("Trade Agent does not have any wares");
                    return false;
                }
                else
                {
                    explanation = new TextObject("Wares will be transferred to your party's inventory");
                    return true;
                }
            }
            );
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_storage2", "lt_trade_agent_intro", "Ok, another thing...", null, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_storage3", "lt_trade_agent_intro", "Here you go...", null, null, 100, null);


            // balance
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_gold", "lt_trade_agent_gold2", "Let me check my records... Right, here it is... Current balance is {BALANCE}{GOLD_ICON} [ib:closed]", null, FormatBalance, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold3", "I want to increase the balance by 1000{GOLD_ICON}", null, () => IncreaseBalance(1000), 100, (out TextObject explanation) =>
                {
                    if (Hero.MainHero.Gold < 1000)
                    {
                        explanation = new TextObject("Not enough gold...");
                        return false;
                    }
                    else
                    {
                        explanation = new TextObject("1000 {GOLD_ICON}");
                        return true;
                    }
                }
                
            );
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold3", "I want to increase the balance by 10000{GOLD_ICON}", null, () => IncreaseBalance(10000), 100, (out TextObject explanation) =>
                {
                    if (Hero.MainHero.Gold < 10000)
                    {
                        explanation = new TextObject("Not enough gold...");
                        return false;
                    }
                    else
                    {
                        explanation = new TextObject("10000 {GOLD_ICON}");
                        return true;
                    }
                }
            );
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold3", "I want to increase the balance by 50000{GOLD_ICON}", null, () => IncreaseBalance(50000), 100, (out TextObject explanation) =>
            {
                if (Hero.MainHero.Gold < 50000)
                {
                    explanation = new TextObject("Not enough gold...");
                    return false;
                }
                else
                {
                    explanation = new TextObject("50000 {GOLD_ICON}");
                    return true;
                }
            }
            );
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold3", "I want to increase the balance by 100000{GOLD_ICON}", null, () => IncreaseBalance(100000), 100, (out TextObject explanation) =>
            {
                if (Hero.MainHero.Gold < 100000)
                {
                    explanation = new TextObject("Not enough gold...");
                    return false;
                }
                else
                {
                    explanation = new TextObject("100000 {GOLD_ICON}");
                    return true;
                }
            }
            );
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_gold3", "lt_trade_agent_gold2", "Ok... with the increase of {BALANCE_INCREASE}{GOLD_ICON} the total balance will be {BALANCE}{GOLD_ICON}", null, null, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold4", "Another thing...", null, null, 100, null, null);         
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_gold4", "lt_trade_agent_options", "I am listening.", null, null, 100, null);


            // TA menu
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_amounts", "lt_trade_agent_amounts2", "{TA_WARES_WITH_AMOUNTS} [ib:confident]", null, FormatTextVariables, 100, null);

            // ware management
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts3", "I want to add new wares to the buy list", null, () => SelectTradeWares(true), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts3", "Let me remove some wares from the buy list", null, () => SelectTradeWares(false), 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_amounts3", "lt_trade_agent_amounts2", "Noted.", null, null, 100, null);

            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts4", "Great, continue as it is.", null, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_amounts4", "lt_trade_agent_options", "Understood.", null, null, 100, null);


        }


        private bool IsTradeAgentPresentInTown()
        {
            if (IsHeroTownNotable() == false) return false;

            Settlement town = Hero.MainHero.CurrentSettlement;
            if (town == null || town.IsTown == false || town.Notables == null) return false;

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;
            Hero notable = co.HeroObject;

            Hero? tradeAgent = GetSettlementsTradeAgent(town);

            if (tradeAgent == null || tradeAgent == notable) return false;

            MBTextManager.SetTextVariable("TA_NAME", tradeAgent.Name.ToString(), false);
            return true;
        }

        private bool IsHeroPotentialTradeAgent()
        {
            if (IsHeroTownNotable() == false) return false;

            // does this town already has Trade Agent?
            if (GetSettlementsTradeAgent(Hero.MainHero.CurrentSettlement) != null) return false;

            FormatTradeSettlementsTextVariables();

            return true;
        }

        private void FormatTradeSettlementsTextVariables()
        {
            string locations = "";
            int villages = 0;
            foreach (Village village in Hero.MainHero.CurrentSettlement.Town.TradeBoundVillages)
            {
                //LTLogger.IM(village.Name.ToString());
                locations = locations + village.Name.ToString() + ", ";
                villages++;
            }

            if (villages > 1)
            {
                // change last ',' to 'and'
                int lastIndex = locations.LastIndexOf(',');
                TextObject and = new TextObject("and");
                locations = locations.Substring(0, lastIndex) + " " + and.ToString() + locations.Substring(lastIndex + 1);
            }

            MBTextManager.SetTextVariable("TRADE_SETTLEMENTS", locations, false);
            MBTextManager.SetTextVariable("TOWN_NAME", Hero.MainHero.CurrentSettlement.Town.Name.ToString(), false);

            MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);

            // TODO, make it dependable on relation/players Trade skill
            MBTextManager.SetTextVariable("COMMISSION_PERC", TAPercent.ToString(), false);
        }

        private bool IsHeroTownNotable()
        {

            Settlement town = Hero.MainHero.CurrentSettlement;
            if (town == null || town.IsTown == false || town.Notables == null) return false;

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;

            Hero notable = co.HeroObject;

            if (town.Notables.Contains(notable)) return true;

            return false;
        }

        private bool IsHeroTownsTradeAgent()
        {
            Settlement town = Hero.MainHero.CurrentSettlement;
            if (town == null || town.IsTown == false || town.Notables == null) return false;

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;

            Hero notable = co.HeroObject;

            if (GetSettlementsTradeAgent(town) != notable) return false;

            FormatTextVariables();

            return true;
        }

        private void FormatBalance()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;

            Hero notable = co.HeroObject;
            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            MBTextManager.SetTextVariable("BALANCE", tradeData.Balance.ToString(), false);
        }


        private void FormatTextVariables()
        {

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;

            Hero notable = co.HeroObject;

            string taWares = "";
            string taWaresWithAmounts = "";
            int i = 0;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            if (tradeData.TradeItemsDataList.Count > 0)
            {
                foreach (TradeItemData ware in tradeData.TradeItemsDataList)
                {
                    if (ware.item != null)
                    {
                        //LTLogger.IMRed(ware.item.Name.ToString());
                        taWares += ware.item.Name.ToString() + ", ";

                        // count items
                        int itemCount = tradeData.Stash.GetItemNumber(ware.item);

                        taWaresWithAmounts += ware.item.Name.ToString() + " (" + itemCount.ToString() + "), ";
                        i++;
                    }
                }
                taWares = taWares.Substring(0, taWares.Length - 2);
                taWaresWithAmounts = taWaresWithAmounts.Substring(0, taWaresWithAmounts.Length - 2);

                MBTextManager.SetTextVariable("TA_WARES", "As agreed I will buy for you " + taWares + ".", false);
                MBTextManager.SetTextVariable("TA_WARES_WITH_AMOUNTS", "Wares in the buy list: " + taWaresWithAmounts, false);

            }
            else
            {
                MBTextManager.SetTextVariable("TA_WARES", "Yes, about that. We need to agree on what should I buy.", false);
                MBTextManager.SetTextVariable("TA_WARES_WITH_AMOUNTS", "Yes, about that. We need to agree on what should I buy.", false);
            }

            //MBTextManager.SetTextVariable("BALANCE", tradeData.Balance.ToString(), false);
        }

        private void FormatTextVariableStorage()
        {

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return ;

            Hero notable = co.HeroObject;

            string taWaresWithAmounts = "";

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            if (tradeData.Stash.Count > 0)
            {
                for (int i = 0; i < tradeData.Stash.Count; i++)
                {
                    ItemObject item = tradeData.Stash.GetItemAtIndex(i);
                    int itemCount = tradeData.Stash.GetItemNumber(item);
                    taWaresWithAmounts += item.Name.ToString() + " (" + itemCount.ToString() + "), ";
                }
                taWaresWithAmounts = taWaresWithAmounts.Substring(0, taWaresWithAmounts.Length - 2);

                MBTextManager.SetTextVariable("TA_WARES_IN_STORAGE", "I have such wares in my storage currently: " + taWaresWithAmounts, false);
            }

            else
            {
                MBTextManager.SetTextVariable("TA_WARES_IN_STORAGE", "I don't have any wares currently.", false);
            }

            //MBTextManager.SetTextVariable("BALANCE", tradeData.Balance.ToString(), false);
        }

        private void SetNewTradeAgent()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTLogger.IMGreen(notable.Name.ToString() + " hired as new Trade Agent in " + notable.CurrentSettlement.Name.ToString());

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);
            tradeData.Active = true;

            tradeData.Balance = 10000;
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -10000, false);

            SelectTradeWares(true);

            //if (BannerKingsActive)
            //{
            //    ItemObject limestone = MBObjectManager.Instance.GetObject<ItemObject>("limestone");
            //    if (limestone != null)
            //    {
            //        //LTLogger.IMGreen("Limestone found!!!");
            //        TradeItemData limestoneData = new(limestone);
            //        tradeData.TradeItemsDataList.Add(limestoneData);
            //    }

            //    ItemObject marble = MBObjectManager.Instance.GetObject<ItemObject>("marble");
            //    if (marble != null)
            //    {
            //        //LTLogger.IMGreen("Marble found!!!");
            //        TradeItemData marbleData = new(marble);
            //        tradeData.TradeItemsDataList.Add(marbleData);
            //    }
            //} else
            //{
            //    // add grain as item TA will buy
            //    ItemObject grain = MBObjectManager.Instance.GetObject<ItemObject>("grain");
            //    TradeItemData grainData = new(grain);
            //    tradeData.TradeItemsDataList.Add(grainData);
            //}

        }

        private void EndContract()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);
            tradeData.Active = false;

            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, tradeData.Balance, false);
            tradeData.Balance = 0;

            TransferWares();

            // clean ware data
            tradeData.TradeItemsDataList.Clear();

            LTLogger.IMBlue("Trade Agent contract with " + notable.Name.ToString() + " in " + notable.CurrentSettlement.Name.ToString() + " terminated.");
        }

        private void TransferWares()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);
            if (tradeData.Stash.Count == 0) return;

            for (int i = 0; i < tradeData.Stash.Count; i++)
            {
                ItemObject item = tradeData.Stash.GetItemAtIndex(i);
                int itemCount = tradeData.Stash.GetItemNumber(item);
                if (itemCount == 0) continue;

                PartyBase.MainParty.ItemRoster.AddToCounts(item, itemCount);

                LTLogger.IMGrey(item.Name.ToString() + " (" + itemCount.ToString()+ ") received");
            }

            SoundEvent.PlaySound2D("event:/ui/multiplayer/shop_purchase_proceed");

            tradeData.Stash.Clear();

            FormatTextVariables();
        }

        private void IncreaseBalance(int amount)
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);
          
            tradeData.Balance += amount;

            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount * (-1), false);

            SoundEvent.PlaySound2D("event:/ui/multiplayer/purchase_success");

            MBTextManager.SetTextVariable("BALANCE", tradeData.Balance.ToString(), false);
            MBTextManager.SetTextVariable("BALANCE_INCREASE", amount.ToString(), false);
        }


        private bool TradeAgentHasWaresInStash()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            if (tradeData.GetTotalWaresCountInStash() > 0) return true;

            return false;
        }


        //private bool TradeAgentHasWaresInBuyList()
        //{
        //    CharacterObject co = CharacterObject.OneToOneConversationCharacter;
        //    if (co == null || co.HeroObject == null) return false;
        //    Hero notable = co.HeroObject;

        //    LTTATradeData tradeData = GetTradeAgentTradeData(notable);

        //    if (tradeData.TradeItemsDataList.Count > 0) return true;

        //    return false;
        //}

        private List<InquiryElement> FormatTradeWaresInquiryList(bool toBuy = true)
        {

            List<InquiryElement> list = new();
            
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return list;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);
           
            foreach (ItemObject item in Items.AllTradeGoods)
            {

                bool contains = false;
                foreach (TradeItemData ware in tradeData.TradeItemsDataList)
                {
                    if (ware.item == null) continue;
                    if (ware.item == item) contains = true;
                }

                if ((toBuy && !contains) || (!toBuy && contains)) list.Add(new InquiryElement(item, item.Name.ToString(), new ImageIdentifier(item)));

            }
            return list;
        }


        private void SelectTradeWares(bool toBuy = true)
        {

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            List<InquiryElement> list = FormatTradeWaresInquiryList(toBuy);

            string titleText = "";
            if (toBuy) titleText = new TextObject("Select trade wares you want to buy").ToString();
                  else titleText = new TextObject("Select trade wares you don't want to buy anymore").ToString();

            MultiSelectionInquiryData data = new(titleText, "", list, true, 1000, 
                new TextObject("Select").ToString(), new TextObject("Leave").ToString(), 
                    (List<InquiryElement> list) => {
                
                    // what we will do with selected wares?
                    foreach (InquiryElement inquiryElement in list)
                    {
                        if (inquiryElement != null && inquiryElement.Identifier != null)
                        {
                            ItemObject? item = inquiryElement.Identifier as ItemObject;
                            if (item != null)
                            {
                                    if (toBuy)
                                    {
                                        TradeItemData tradeItemData = new(item);
                                        tradeData.TradeItemsDataList.Add(tradeItemData);
                                        LTLogger.IM(item.Name.ToString() + " selected to buy");
                                    } else
                                    {
                                        tradeData.RemoveTradeItem(item);
                                        LTLogger.IM(item.Name.ToString() + " removed from the buy list");
                                    }                                  
                            }
                        }
                    }

                }, (List<InquiryElement> list) => { }, "");
                MBInformationManager.ShowMultiSelectionInquiry(data);
        }

    }


}
