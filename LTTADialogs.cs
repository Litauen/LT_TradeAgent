using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using LT.Logger;
using System.Linq;
using System;

namespace LT_TradeAgent
{
    public partial class LTTABehaviour : CampaignBehaviorBase
    {
        private void AddDialogs(CampaignGameStarter starter)
        {

            // hiring new Trade Agent
            starter.AddPlayerLine("lt_trade_agent_new", "hero_main_options", "lt_trade_agent_new_intro", "I want to hire you to trade for me when I am not around.", IsHeroPotentialTradeAgent, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent_new", "lt_trade_agent_new_intro", "lt_trade_agent_new_intro2", "{HIRE_RESPONSE}[ib:confident3][if:convo_excited]", null, null, 100, null);

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

            starter.AddDialogLine("lt_trade_agent_new", "lt_trade_agent_new_agreement", "lt_trade_agent_options", "Ok. Here are the keys to the rented warehouse. You will find it in the backstreets, not far from the tavern. Anything else?", null, null, 100, null);

            // another Trade Agent already active in the Town or wrong type notable
            starter.AddPlayerLine("lt_trade_agent_other", "hero_main_options", "lt_trade_agent_other", "Would you trade for me when I am not around?", IsNotableNotSuitable, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent_other", "lt_trade_agent_other", "hero_main_options", "{REFUSE_RESPONSE}[ib:confident3][if:convo_mocking_revenge]", null, null, 100, null);

            // Trade Agent hired
            starter.AddPlayerLine("lt_trade_agent", "hero_main_options", "lt_trade_agent_intro", "About our trade agreement...", IsHeroTownsTradeAgent, TalkWithTAConsequence, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_intro", "lt_trade_agent_options", "{TA_WARES} {ACTIVE_STATUS_INFO} [ib:normal2][if:convo_calm_friendly]", FormatTextVariables, null, 100, null);

            // options
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_gold", "Let's talk about gold{GOLD_ICON}", null, null, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_amounts", "About the wares...", null, null, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_storage", "Let's inspect the warehouse", null, null, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_contract", "I want to talk about our contract...", null, FormatTradeSettlementsTextVariables, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_options", "lt_trade_agent_new_nvm", "That will be all.", null, null, 100, null, null);

            // contract
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_contract", "lt_trade_agent_contract2", "As agreed I will trade for you for a small fee of {COMMISSION_PERC}%. {STATUS_REPORT_INFO} {ACTIVE_STATUS_INFO}[ib:nervous2][if:convo_undecided_closed]", null, null, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_contract2", "lt_trade_agent_fee", "That fee of yours is kind of high... What about lowering it a bit?", null, null, 100, null, null);

            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_contract2", "lt_trade_agent_fee2", "I want you to send me detailed status reports about your trades.", () => { return !IsStatusReportEnabled(); }, () => ChangeStatusReport(), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_contract2", "lt_trade_agent_fee2", "I don't want to be bothered with detailed status reports anymore.", IsStatusReportEnabled, () => ChangeStatusReport(), 100, null, null);

            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_contract2", "lt_trade_agent_fee2", "Let's resume the trading.", () => { return !IsTAActive(); }, () => ChangeTAActiveStatus(), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_contract2", "lt_trade_agent_fee2", "Let's temporarily pause the trading.", IsTAActive, () => ChangeTAActiveStatus(), 100, null, null);


            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_contract2", "lt_trade_agent_stop", "I want to end our agreement...", null, null, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_contract2", "lt_trade_agent_intro", "That will be all.", null, null, 100, null, null);

            // fee
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_fee", "lt_trade_agent_fee2", "Lower my fee, you say? Ah, I see you have a keen eye for fine and exquisite service. Alas, my fee is as sturdy as a castle wall, fortified with quality and exclusivity. Perhaps, you'd prefer a journey to the mystical realm of \"Wishful Thinking\" for more affordable deals? [ib:nervous2][if: convo_insulted]", null, null, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_fee2", "lt_trade_agent_intro", "Ok then...", null, null, 100, null, null);

            // stop the contract
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_stop", "lt_trade_agent_stop2", "Pitty, that was a good contract. Let me transfer remaining balance and all the wares to you now...[ib:nervous][if:convo_shocked]", null, null, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_stop2", "lt_trade_agent_stop3", "Ok, let's end this", null, EndContract, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_stop2", "lt_trade_agent_stop4", "Actually, I changed my mind. Keep up the good work.", null, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_stop3", "hero_main_options", "As you wish.", null, null, 100, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_stop4", "lt_trade_agent_options", "That's what I though.[ib:normal2][if:convo_calm_friendly]", null, null, 100, null);

            // storage
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_storage", "lt_trade_agent_storage2", "{WAREHOUSE_RESPONSE} [ib:hip]", null, FormatTextVariableStorage, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_storage2", "lt_trade_agent_storage3", "I want to take all the wares from the warehouse", BalanceNotNegative, TransferWares, 100, (out TextObject explanation) =>
            {
                if (!BalanceNotNegative())
                {
                    explanation = new TextObject("Negative balance. Cover your debt before accessing the warehouse.");
                    return false;
                }
                else
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
            }
            );
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_storage2", "lt_trade_agent_intro", "I will visit the warehouse myself", null, WarehouseScreenFromDialog, 100, (out TextObject explanation) =>
            {
                if (!BalanceNotNegative())
                {
                    explanation = new TextObject("Negative balance. Cover your debt before accessing the warehouse.");
                    return false;
                }
                else
                {
                    explanation = new TextObject("");
                    return true;
                }
            }

            );
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_storage2", "lt_trade_agent_intro", "Ok, another thing...", null, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_storage3", "lt_trade_agent_intro", "Here you go...", null, null, 100, null);


            // balance
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_gold", "lt_trade_agent_gold2", "Let me check my records... Right, here it is... Current balance is {BALANCE}{GOLD_ICON} [ib:closed]", null, FormatBalance, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold3", "I want to increase the balance by 1000{GOLD_ICON}", null, () => ChangeBalance(1000), 100, (out TextObject explanation) =>
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
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold3", "I want to increase the balance by 10000{GOLD_ICON}", null, () => ChangeBalance(10000), 100, (out TextObject explanation) =>
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
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold3", "I want to increase the balance by 50000{GOLD_ICON}", null, () => ChangeBalance(50000), 100, (out TextObject explanation) =>
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
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold3", "I want to increase the balance by 100000{GOLD_ICON}", null, () => ChangeBalance(100000), 100, (out TextObject explanation) =>
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

            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold_back", "Let me take some of my gold back...", () => { return GetTradeAgentGold(CharacterObject.OneToOneConversationCharacter.HeroObject) > 0; }, FormatBalance, 100, null, null);

            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_gold3", "lt_trade_agent_gold2", "Ok... with the increase of {BALANCE_INCREASE}{GOLD_ICON} the total balance will be {BALANCE}{GOLD_ICON}", null, null, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold2", "lt_trade_agent_gold4", "Another thing...", null, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_gold4", "lt_trade_agent_options", "I am listening.", null, null, 100, null);

            // gold back
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_gold_back", "lt_trade_agent_gold_back2", "What's yours is yours. Current balance is {BALANCE}{GOLD_ICON}", null, null, 100, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold_back2", "lt_trade_agent_gold_back", "I want to take 1000{GOLD_ICON}", null, () => ChangeBalance(-1000), 100, (out TextObject explanation) =>
            {
                if (GetTradeAgentGold(CharacterObject.OneToOneConversationCharacter.HeroObject) < 1000)
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
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold_back2", "lt_trade_agent_gold_back", "I want to take 10000{GOLD_ICON}", null, () => ChangeBalance(-10000), 100, (out TextObject explanation) =>
            {
                if (GetTradeAgentGold(CharacterObject.OneToOneConversationCharacter.HeroObject) < 10000)
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
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold_back2", "lt_trade_agent_gold_back", "I want to take 100000{GOLD_ICON}", null, () => ChangeBalance(-100000), 100, (out TextObject explanation) =>
            {
                if (GetTradeAgentGold(CharacterObject.OneToOneConversationCharacter.HeroObject) < 100000)
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
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold_back2", "lt_trade_agent_gold", "I will take all the gold {BALANCE}{GOLD_ICON}", null, () => ChangeBalance(GetTradeAgentGold(CharacterObject.OneToOneConversationCharacter.HeroObject) * (-1)), 100, (out TextObject explanation) =>
            {
                if (GetTradeAgentGold(CharacterObject.OneToOneConversationCharacter.HeroObject) < 1)
                {
                    explanation = new TextObject("Nothing to take...");
                    return false;
                }
                else
                {
                    explanation = new TextObject("{BALANCE}{GOLD_ICON}");
                    return true;
                }
            }
            );
            //starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold_back2", "lt_trade_agent_gold", "I will take all the gold {BALANCE}{GOLD_ICON}", null, () => IncreaseBalance(GetTradeAgentGold(CharacterObject.OneToOneConversationCharacter.HeroObject) * (-1)), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_gold_back2", "lt_trade_agent_gold", "Nevermind", null, null, 100, null, null);



            // TA menu
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_amounts", "lt_trade_agent_amounts2", "{TA_WARES_WITH_AMOUNTS} [ib:confident]", FormatTextVariables, null, 100, null);

            // ware management
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts3", "I want to add new wares to the list", null, () => SelectTradeWares(true), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts3", "Let me remove some wares from the list", null, () => SelectTradeWares(false), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts3", "I want to change buy prices", null, () => ChangePrices(true), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts3", "I want to change sell prices", null, () => ChangePrices(false), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts3", "Do not buy more than...", null, () => ChangeAmounts(true), 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts3", "When selling leave at least...", null, () => ChangeAmounts(false), 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_amounts3", "lt_trade_agent_amounts", "Noted.", FormatTextVariables, null, 100, null);

            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_amounts2", "lt_trade_agent_amounts4", "Great, continue as it is.", null, null, 100, null, null);
            starter.AddDialogLine("lt_trade_agent", "lt_trade_agent_amounts4", "lt_trade_agent_options", "Understood.", null, null, 100, null);


        }


        private bool IsNotableNotSuitable()
        {
            if (!IsHeroTownNotable()) return false;

            Settlement town = Hero.MainHero.CurrentSettlement;
            if (town == null || town.IsTown == false || town.Notables == null) return false;

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;
            Hero notable = co.HeroObject;

            // wrong type of notable
            if (!(notable.IsArtisan || notable.IsMerchant || notable.IsGangLeader))
            {
                MBTextManager.SetTextVariable("REFUSE_RESPONSE", "Do I look like I know anything about trading?", false);
                return true;
            }

            Hero? tradeAgent = GetSettlementsTradeAgent(town);
            if (tradeAgent == notable) return false;  // TA is himself or not found in town

            // TA already present
            if (tradeAgent != null)
            {
                MBTextManager.SetTextVariable("REFUSE_RESPONSE", "As far as I know you already have agreement with " + tradeAgent.Name.ToString(), false);
                return true;
            }

            // TA limit check 
            if (!CanHaveMoreTA())
            {
                if (Clan.PlayerClan.Tier < 6) MBTextManager.SetTextVariable("REFUSE_RESPONSE", "I don't think you will be able to manage one more Trade Agent. Let's talk again after you will gain more renown.", false);
                else MBTextManager.SetTextVariable("REFUSE_RESPONSE", "Most esteemed noble, I regretfully cannot accommodate your request at this time, for it grieves me to inform you that your noble self is preoccupied with weighty matters, and I humbly apologize for any inconvenience caused by my inability to fulfill your desires. (Trade Agent limit reached)", false);
                return true;
            }


            // check relations
            int relation = CharacterRelationManager.GetHeroRelation(Hero.MainHero, notable);
            int necessaryRelation = GetNecessaryRelationForHire(notable);
            //LTLogger.IMRed(relation + " " + necessaryRelation);
            if (relation < necessaryRelation)
            {
                if (relation < -50) MBTextManager.SetTextVariable("REFUSE_RESPONSE", "Get lost or I'll gut you.", false);
                else if (relation < -20) MBTextManager.SetTextVariable("REFUSE_RESPONSE", "I don't like you. Piss off.", false);
                else if (relation < 0) MBTextManager.SetTextVariable("REFUSE_RESPONSE", "No, I don't think so.", false);
                else MBTextManager.SetTextVariable("REFUSE_RESPONSE", "I don't know you well enough.", false);

                return true;
            }

            return false;
        }



        private bool IsHeroPotentialTradeAgent()
        {

            if (!IsHeroTownNotable()) return false;

            Settlement town = Hero.MainHero.CurrentSettlement;
            if (town == null || town.IsTown == false || town.Notables == null) return false;

            // does this town already has Trade Agent?
            if (GetSettlementsTradeAgent(town) != null) return false;

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;
            Hero notable = co.HeroObject;

            if (!(notable.IsArtisan || notable.IsMerchant || notable.IsGangLeader)) return false;

            //LTLogger.IMTAGreen("IsHeroPotentialTradeAgent");
            //FormatTradeSettlementsTextVariables();

            // relation check
            int relation = CharacterRelationManager.GetHeroRelation(Hero.MainHero, notable);
            int necessaryRelation = GetNecessaryRelationForHire(notable);
            if (relation < necessaryRelation) return false;


            // TA limit check 
            if (!CanHaveMoreTA()) return false;


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
                locations = locations.Substring(0, lastIndex) + " and" + locations.Substring(lastIndex + 1);
            }

            int feePercent = GetFeePercent(notable);

            if (notable.IsArtisan || notable.IsMerchant)
            {
                MBTextManager.SetTextVariable("HIRE_RESPONSE", "I think ... I can do that. For an additional " + feePercent.ToString() + "% fee of course. I have connections in " + locations + town.Name.ToString(), false);
            } else
            {
                MBTextManager.SetTextVariable("HIRE_RESPONSE", "DO I LOOK LIKE ... actually... I can hir... Yes! Sure! We can have a deal. My fee is .... mmm ... " + feePercent.ToString() + "%. I can organize trade in " + locations + town.Name.ToString(), false);
            }

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
                locations = locations.Substring(0, lastIndex) + " and" + locations.Substring(lastIndex + 1);
            }

            MBTextManager.SetTextVariable("TRADE_SETTLEMENTS", locations, false);
            MBTextManager.SetTextVariable("TOWN_NAME", Hero.MainHero.CurrentSettlement.Town.Name.ToString(), false);
            MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);

            //LTLogger.IMTAGreen("FormatTradeSettlementsTextVariables");

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;
            LTTATradeData tradeData = GetTradeAgentTradeData(notable);
            MBTextManager.SetTextVariable("COMMISSION_PERC", tradeData.FeePercent.ToString(), false);
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

            //LTLogger.IMTAGreen("IsHeroTownsTradeAgent");

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


        private bool FormatTextVariables()
        {

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return true;

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

                        taWaresWithAmounts += ware.item.Name.ToString() + " (" + itemCount.ToString() + " " + GetNicePriceString(ware.minPrice, false) + "/" + GetNiceAmountString(ware.minItemAmount, false) + " " + GetNicePriceString(ware.maxPrice, true) + "/" + GetNiceAmountString(ware.maxItemAmount, true) + "), ";
                        i++;
                    }
                }
                taWares = taWares.Substring(0, taWares.Length - 2);
                taWaresWithAmounts = taWaresWithAmounts.Substring(0, taWaresWithAmounts.Length - 2);

                MBTextManager.SetTextVariable("TA_WARES", "As agreed I will trade for you " + taWares + ". My fee is " + tradeData.FeePercent + "%.", false);
                MBTextManager.SetTextVariable("TA_WARES_WITH_AMOUNTS", "Wares in the list (in the warehouse, min sell price/min leave amount, max buy price/max buy amount): " + taWaresWithAmounts, false);

            }
            else
            {
                MBTextManager.SetTextVariable("TA_WARES", "Yes, about that. We need to agree on what should I trade.", false);
                MBTextManager.SetTextVariable("TA_WARES_WITH_AMOUNTS", "Yes, about that. We need to agree on what should I trade.", false);
            }

            MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);

            if (tradeData.SendsTradeInfo) MBTextManager.SetTextVariable("STATUS_REPORT_INFO", "I will send you detailed reports about my trades.", false);
            else MBTextManager.SetTextVariable("STATUS_REPORT_INFO", "I will not bother you with detailed reports about my trades.", false);

            if (tradeData.Active) MBTextManager.SetTextVariable("ACTIVE_STATUS_INFO", "", false);
            else MBTextManager.SetTextVariable("ACTIVE_STATUS_INFO", "Currently I will not do any trades as you asked.", false);




            return true;
        }

        private void FormatTextVariableStorage()
        {

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;

            Hero notable = co.HeroObject;

            string taWaresWithAmounts = "";

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            if (!BalanceNotNegative())
            {
                MBTextManager.SetTextVariable("WAREHOUSE_RESPONSE", "There is a small issue we need to resolve before you will be allowed to the warehouse. There is a debt of " + (tradeData.Balance * (-1)).ToString() + "{GOLD_ICON}...", false);
                return;
            }

            if (tradeData.Stash.Count > 0)
            {
                for (int i = 0; i < tradeData.Stash.Count; i++)
                {
                    ItemObject item = tradeData.Stash.GetItemAtIndex(i);
                    int itemCount = tradeData.Stash.GetItemNumber(item);
                    taWaresWithAmounts += item.Name.ToString() + " (" + itemCount.ToString() + "), ";
                }
                taWaresWithAmounts = taWaresWithAmounts.Substring(0, taWaresWithAmounts.Length - 2);

                MBTextManager.SetTextVariable("WAREHOUSE_RESPONSE", "I have such wares in the warehouse currently: " + taWaresWithAmounts, false);
            }

            else
            {
                MBTextManager.SetTextVariable("WAREHOUSE_RESPONSE", "I don't have any wares currently.", false);
            }

            //MBTextManager.SetTextVariable("BALANCE", tradeData.Balance.ToString(), false);
        }

        private void SetNewTradeAgent()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTLogger.IMTAGreen(notable.Name.ToString() + " hired as new Trade Agent in " + notable.CurrentSettlement.Name.ToString());

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);
            tradeData.Active = true;

            tradeData.Balance = 10000;
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -10000, false);

            tradeData.FeePercent = GetFeePercent(notable);

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, 2, false);

            tradeData.LastTradeExperienceGainFromInteraction = CampaignTime.Now;

            SelectTradeWares(true);

        }

        private void EndContract()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData? tradeData = GetTradeAgentTradeData(notable);
            tradeData.Active = false;

            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, tradeData.Balance, false);
            tradeData.Balance = 0;

            TransferWares();

            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, -10, true);

            // clean ware data
            tradeData.TradeItemsDataList.Clear();
            TradeAgentsData.Remove(notable);

            //if (!TradeAgentsData.ContainsKey(notable))
            //{
            //    LTLogger.IMRed("Entry notable was successfully removed.");
            //}

            LTLogger.IMTARed("Trade Agent contract with " + notable.Name.ToString() + " in " + notable.CurrentSettlement.Name.ToString() + " terminated.");
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

                LTLogger.IMGrey(item.Name.ToString() + " (" + itemCount.ToString() + ") received");
            }

            SoundEvent.PlaySound2D("event:/ui/multiplayer/shop_purchase_proceed");

            tradeData.Stash.Clear();

            FormatTextVariables();
        }

        private void ChangeBalance(int amount)
        {
            if (amount == 0) return;

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            tradeData.Balance += amount;

            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount * (-1), false);

            SoundEvent.PlaySound2D("event:/ui/multiplayer/purchase_success");

            MBTextManager.SetTextVariable("BALANCE", tradeData.Balance.ToString(), false);
            MBTextManager.SetTextVariable("BALANCE_INCREASE", amount.ToString(), false);

            if (amount == -100000)
            {
                // -4 / -100k
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, -4, false);
            } else if (amount > 0)
            {
                // +1 / 50k
                int relationChange = amount / 50000;
                if (relationChange > 0) ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, relationChange, false);
            } else if (tradeData.Balance == 0) ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, -4, false);    // -4 for leaving balance empty
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


        private List<InquiryElement> FormatTradeWaresInquiryList(bool toBuy = true)
        {
            List<InquiryElement> list = new();

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return list;
            Hero notable = co.HeroObject;
            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            var orderedItems = Items.All
                .Where(item => item.Type == ItemObject.ItemTypeEnum.Goods && !item.StringId.Contains("book"))
                .OrderBy(item => item.Name.ToString(), StringComparer.Ordinal);

            foreach (ItemObject item in orderedItems)
            //foreach (ItemObject item in Items.All.Where(item => item.Type == ItemObject.ItemTypeEnum.Goods && !item.StringId.Contains("book")).OrderBy(item => item.Name))
            //foreach (ItemObject item in Items.AllTradeGoods)
            {
                bool contains = false;
                foreach (TradeItemData ware in tradeData.TradeItemsDataList)
                {
                    if (ware.item == null) continue;
                    if (ware.item == item)
                    {
                        contains = true;
                        break;
                    }
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
            if (toBuy) titleText = new TextObject("Select trade wares you want your Trade Agent to trade").ToString();
            else titleText = new TextObject("Select trade wares you don't want your Trade Agent to trade anymore").ToString();

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
                                        LTLogger.IMGrey(item.Name.ToString() + " added to the list");
                                    } else
                                    {
                                        tradeData.RemoveTradeItem(item);
                                        LTLogger.IMGrey(item.Name.ToString() + " removed from the list");
                                    }
                                }
                            }
                        }

                    }, (List<InquiryElement> list) => { }, "");
            MBInformationManager.ShowMultiSelectionInquiry(data);

        }



        private List<InquiryElement> FormatTradeWaresInquiryListForPriceChange(bool toBuy = true, bool showPrice = true)
        {
            List<InquiryElement> list = new();

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return list;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            string showPriceString = "";

            foreach (TradeItemData ware in tradeData.TradeItemsDataList)
            {
                if (ware.item == null) continue;

                if (showPrice)
                {
                    if (toBuy) showPriceString = " [" + GetNicePriceString(ware.maxPrice) + "]"; else showPriceString = " [" + GetNicePriceString(ware.minPrice) + "]";
                }

                list.Add(new InquiryElement(ware.item, ware.item.Name.ToString() + showPriceString, new ImageIdentifier(ware.item)));
            }

            return list;
        }



        private void ChangePrices(bool toBuy = true)
        {

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            List<InquiryElement> list = FormatTradeWaresInquiryListForPriceChange(toBuy, true);

            string titleText = "";
            if (toBuy) titleText = new TextObject("Select the ware to change max buy price").ToString();
            else titleText = new TextObject("Select the ware to change min sell price").ToString();

            MultiSelectionInquiryData data = new(titleText, "", list, true, 1,
                new TextObject("Select").ToString(), new TextObject("Leave").ToString(),
                    (List<InquiryElement> list) => {

                        // what we will do with selected item?
                        foreach (InquiryElement inquiryElement in list)
                        {
                            if (inquiryElement != null && inquiryElement.Identifier != null)
                            {
                                ItemObject? item = inquiryElement.Identifier as ItemObject;
                                if (item != null)
                                {
                                    if (toBuy)
                                    {
                                        string title = "Enter new max buy price for " + item.Name;
                                        string text = "The Trade Agent will not buy this ware if price will be higher than max buy price.\n\nCurrent max buy price: " + GetNicePriceString(tradeData.GetWarePrice(item)) + "          Average price: " + item.Value.ToString() + "\n\nEnter -1 for unlimited (∞) - to always buy no matter the price.\nEnter 0 - to never buy.";
                                        string inputText = "";

                                        InformationManager.ShowTextInquiry(new TextInquiryData(title, text, true, true, "Confirm", "Cancel",
                                            delegate (string newPrice)
                                            {
                                                int input;
                                                if (int.TryParse(newPrice, out input))
                                                {
                                                    tradeData.ChangeWarePrice(item, input);
                                                    LTLogger.IMGrey(item.Name.ToString() + " max buy price changed to " + GetNicePriceString(input));
                                                }

                                            }
                                            , null, false, null, "", inputText), false, false);
                                    }
                                    else
                                    {
                                        string title = "Enter new min sell price for " + item.Name;
                                        string text = "The Trade Agent will not sell this ware if price will be lower than min sell price.\n\nCurrent min sell price: " + GetNicePriceString(tradeData.GetWarePrice(item, false)) + "          Average price: " + item.Value.ToString() + "\n\nEnter 0 - to do not sell.\nEnter 1 - to always sell no matter the price.";
                                        string inputText = "";

                                        InformationManager.ShowTextInquiry(new TextInquiryData(title, text, true, true, "Confirm", "Cancel",
                                            delegate (string newPrice)
                                            {
                                                int input;
                                                if (int.TryParse(newPrice, out input))
                                                {
                                                    if (input == -1) input = 0;
                                                    tradeData.ChangeWarePrice(item, input, false);
                                                    LTLogger.IMGrey(item.Name.ToString() + " min sell price changed to " + GetNicePriceString(input));
                                                }

                                            }
                                            , null, false, null, "", inputText), false, false);
                                    }
                                }
                            }
                        }

                    }, (List<InquiryElement> list) => { }, "");
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        public string GetNicePriceString(int price, bool buyPrice = true)
        {
            string priceString = price.ToString();
            if (buyPrice && price == -1) priceString = "∞";
            if (!buyPrice && price == 0) priceString = "-";
            return priceString;
        }


        public bool IsStatusReportEnabled()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            return tradeData.SendsTradeInfo;
        }

        public void ChangeStatusReport()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            tradeData.SendsTradeInfo = !tradeData.SendsTradeInfo;
        }


        public bool IsTAActive()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            return tradeData.Active;
        }

        public void ChangeTAActiveStatus()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            tradeData.Active = !tradeData.Active;
        }

        public void TalkWithTAConsequence()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;
            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            if (tradeData.LastTradeExperienceGainFromInteraction.ElapsedDaysUntilNow > 1)
            {
                int skillValue = Hero.MainHero.GetSkillValue(DefaultSkills.Trade);
                if (skillValue < 25) Hero.MainHero.HeroDeveloper.ChangeSkillLevel(DefaultSkills.Trade, 1, true);
                else Hero.MainHero.HeroDeveloper?.AddSkillXp(DefaultSkills.Trade, 500, true, true);
                tradeData.LastTradeExperienceGainFromInteraction = CampaignTime.Now;
            }

            if (tradeData.LastRelationGainFromInteraction.ElapsedDaysUntilNow > 3)
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, 1, false);
                tradeData.LastRelationGainFromInteraction = CampaignTime.Now;
            }
        }

        public bool BalanceNotNegative()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return false;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            return tradeData.Balance >= 0;
        }




        private void ChangeAmounts(bool toBuy = true)
        {

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            List<InquiryElement> list = FormatTradeWaresInquiryListForAmountChange(toBuy, true);

            string titleText = "";
            if (toBuy) titleText = new TextObject("Select the quantity limit for the maximum number of items to purchase").ToString();
            else titleText = new TextObject("Select the item to set a minimum quantity to keep when selling").ToString();

            MultiSelectionInquiryData data = new(titleText, "", list, true, 1,
                new TextObject("Select").ToString(), new TextObject("Leave").ToString(),
                    (List<InquiryElement> list) => {

                        // what we will do with selected item?
                        foreach (InquiryElement inquiryElement in list)
                        {
                            if (inquiryElement != null && inquiryElement.Identifier != null)
                            {
                                ItemObject? item = inquiryElement.Identifier as ItemObject;
                                if (item != null)
                                {
                                    if (toBuy)
                                    {
                                        string title = "Enter new maximum number of items to purchase for " + item.Name;
                                        string text = "The Trade Agent will buy this ware until the set maximum limit and then will stop.\n\nCurrent max buy amount: " + GetNiceAmountString(tradeData.GetWareAmount(item)) + "\n\nEnter -1 for unlimited (∞) - to buy without the limit.\nEnter 0 - to not buy.";
                                        string inputText = "";

                                        InformationManager.ShowTextInquiry(new TextInquiryData(title, text, true, true, "Confirm", "Cancel",
                                            delegate (string newPrice)
                                            {
                                                int input;
                                                if (int.TryParse(newPrice, out input))
                                                {
                                                    tradeData.ChangeWareAmount(item, input);
                                                    LTLogger.IMGrey(item.Name.ToString() + " max buy amount changed to " + GetNiceAmountString(input));
                                                }

                                            }
                                            , null, false, null, "", inputText), false, false);
                                    }
                                    else
                                    {
                                        string title = "Enter new mininum number of items to keep when selling for " + item.Name;
                                        string text = "The Trade Agent will leave this amount of items in the warehouse when selling.\n\nCurrent min sell amount: " + GetNiceAmountString(tradeData.GetWareAmount(item, false)) + "\n\nEnter 0 - to sell everything.";
                                        string inputText = "";

                                        InformationManager.ShowTextInquiry(new TextInquiryData(title, text, true, true, "Confirm", "Cancel",
                                            delegate (string newPrice)
                                            {
                                                int input;
                                                if (int.TryParse(newPrice, out input))
                                                {
                                                    if (input == -1) input = 0;
                                                    tradeData.ChangeWareAmount(item, input, false);
                                                    LTLogger.IMGrey(item.Name.ToString() + " min amount to keep when selling changed to " + GetNiceAmountString(input));
                                                }

                                            }
                                            , null, false, null, "", inputText), false, false);
                                    }
                                }
                            }
                        }

                    }, (List<InquiryElement> list) => { }, "");
            MBInformationManager.ShowMultiSelectionInquiry(data);
        }

        public string GetNiceAmountString(int amount, bool buy = true)
        {
            string amountString = amount.ToString();
            if (buy && amount == -1) amountString = "∞";
            //if (!buy && amount == 0) amountString = "-";
            return amountString;
        }

        private List<InquiryElement> FormatTradeWaresInquiryListForAmountChange(bool toBuy = true, bool showAmount = true)
        {
            List<InquiryElement> list = new();

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.HeroObject == null) return list;
            Hero notable = co.HeroObject;

            LTTATradeData tradeData = GetTradeAgentTradeData(notable);

            string showAmountString = "";

            foreach (TradeItemData ware in tradeData.TradeItemsDataList)
            {
                if (ware.item == null) continue;

                if (showAmount)
                {
                    if (toBuy) showAmountString = " [" + GetNiceAmountString(ware.maxItemAmount) + "]"; else showAmountString = " [" + GetNiceAmountString(ware.minItemAmount) + "]";
                }

                list.Add(new InquiryElement(ware.item, ware.item.Name.ToString() + showAmountString, new ImageIdentifier(ware.item)));
            }

            return list;
        }

    }


}
