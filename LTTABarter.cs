using Helpers;
using LT.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace LT_TradeAgent
{
    public class LTTABarterBehaviour : CampaignBehaviorBase
    {

        private List<PersuasionTask> _allReservations;

        [SaveableField(1)]
        private List<PersuasionAttempt> _previousPersuasionAttempts;

        private float _maximumScoreCap;
        private readonly float _successValue = 1f;
        private readonly float _criticalSuccessValue = 2f;
        private readonly float _criticalFailValue = 2f;
        private readonly float _failValue = 0f;

        private readonly int _daysToWaitTillFeeRenegotiation = 21;

        private readonly bool _debug = false;

        public LTTABarterBehaviour()
        {
            _previousPersuasionAttempts = new List<PersuasionAttempt>();
            _allReservations = new List<PersuasionTask>();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickEvent);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTickEvent);
        }


        private void DailyTickEvent()
        {
            if (!_debug) RemoveOldAttempts();
        }

        private void HourlyTickEvent()
        {
            if (_debug) RemoveOldAttempts();
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<List<PersuasionAttempt>>("previousPersuasionAttempts", ref this._previousPersuasionAttempts);
        }

        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            this.AddBarterDialogs(campaignGameStarter);
        }

        private void AddBarterDialogs(CampaignGameStarter starter)
        {

            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_fee_persuade", "ltta_barter_intro", "Kindly hear my plea...", null, null, 100, null, null);
            starter.AddPlayerLine("lt_trade_agent", "lt_trade_agent_fee_persuade", "lt_trade_agent_contract", "Oook. If you say so...", null, null, 100, null, null);

            // agrees for persuasion attempt
            starter.AddDialogLine("lt_trade_agent", "ltta_barter_intro", "ltta_start_barter_process", "Ok, make your point", CanBePersuaded, null, 100, null);           
            // you already tried, get lost
            starter.AddDialogLine("lt_trade_agent", "ltta_barter_intro", "lt_trade_agent_intro", "{NO_PERSUASION_RESPONSE}", () => !CanBePersuaded() , null, 100, null);

            starter.AddPlayerLine("lt_trade_agent", "ltta_start_barter_process", "ltta_barter_next_reservation", "As fellow traders in this prosperous realm, I humbly request your reconsideration in lowering your fee. By doing so, we can foster a more equitable and harmonious market, benefiting both our businesses and the customers we serve, while fostering stronger collaborations and a shared vision of success.", 
                    null, new ConversationSentence.OnConsequenceDelegate(this.StartPersuasionOnConsequence), 100, null, null);

            // reservation - I'm not sure ...
            starter.AddDialogLine("lt_trade_agent", "ltta_barter_next_reservation", "ltta_persuasion", "{PERSUASION_TASK_LINE}", new ConversationSentence.OnConditionDelegate(this.ConversationCheckIfReservationsMetOnCondition), null, 100, null);

            // persuasion options
            string text = "{=!}{PERSUADE_ATTEMPT_1}";
            ConversationSentence.OnConditionDelegate conditionDelegate = new( () => { return this.ConversationPersuadeOptionOnCondition(0); } );
            ConversationSentence.OnConsequenceDelegate consequenceDelegate = new(() => { this.ConversationPersuadeOptionOnConsequence(0); });
            ConversationSentence.OnClickableConditionDelegate onClickableConditionDelegate = new(this.PersuasionOption1ClickableOnCondition1);
            ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate = new(() => { return this.SetupPersuasionOption(0); });
            starter.AddPlayerLine("lt_trade_agent", "ltta_persuasion", "ltta_barter_reaction", text, conditionDelegate, consequenceDelegate, 100, onClickableConditionDelegate, persuasionOptionDelegate);

            string text2 = "{=!}{PERSUADE_ATTEMPT_2}";
            ConversationSentence.OnConditionDelegate conditionDelegate2 = new(() => { return this.ConversationPersuadeOptionOnCondition(1); });
            ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = new(() => { this.ConversationPersuadeOptionOnConsequence(1); });
            ConversationSentence.OnClickableConditionDelegate onClickableConditionDelegate2 = new(this.PersuasionOption2ClickableOnCondition2);
            ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate2 = new(() => { return this.SetupPersuasionOption(1); });
            starter.AddPlayerLine("lt_trade_agent", "ltta_persuasion", "ltta_barter_reaction", text2, conditionDelegate2, consequenceDelegate2, 100, onClickableConditionDelegate2, persuasionOptionDelegate2);

            string text3 = "{=!}{PERSUADE_ATTEMPT_3}";
            ConversationSentence.OnConditionDelegate conditionDelegate3 = new(() => { return this.ConversationPersuadeOptionOnCondition(2); });
            ConversationSentence.OnConsequenceDelegate consequenceDelegate3 = new(() => { this.ConversationPersuadeOptionOnConsequence(2); });
            ConversationSentence.OnClickableConditionDelegate onClickableConditionDelegate3 = new(this.PersuasionOption3ClickableOnCondition3);
            ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate3 = new(() => { return this.SetupPersuasionOption(2); });
            starter.AddPlayerLine("lt_trade_agent", "ltta_persuasion", "ltta_barter_reaction", text3, conditionDelegate3, consequenceDelegate3, 100, onClickableConditionDelegate3, persuasionOptionDelegate3);

            string text4 = "{=!}{PERSUADE_ATTEMPT_4}";
            ConversationSentence.OnConditionDelegate conditionDelegate4 = new(() => { return this.ConversationPersuadeOptionOnCondition(3); });
            ConversationSentence.OnConsequenceDelegate consequenceDelegate4 = new(() => { this.ConversationPersuadeOptionOnConsequence(3); });
            ConversationSentence.OnClickableConditionDelegate onClickableConditionDelegate4 = new(this.PersuasionOption4ClickableOnCondition4);
            ConversationSentence.OnPersuasionOptionDelegate persuasionOptionDelegate4 = new(() => { return this.SetupPersuasionOption(3); });
            starter.AddPlayerLine("lt_trade_agent", "ltta_persuasion", "ltta_barter_reaction", text4, conditionDelegate4, consequenceDelegate4, 100, onClickableConditionDelegate4, persuasionOptionDelegate4);

            // reaction to persuade attempt
            starter.AddDialogLine("lt_trade_agent", "ltta_barter_reaction", "ltta_barter_next_reservation", "{PERSUASION_REACTION}",
                new ConversationSentence.OnConditionDelegate(this.ConversationOptionReactionOnCondition),
                new ConversationSentence.OnConsequenceDelegate(this.ConversationOptionReactionOnConsequence), 100, null);

            // exit - nevermind
            starter.AddPlayerLine("lt_trade_agent", "ltta_persuasion", "lt_trade_agent_intro", "Nevermind...", null, ConversationOnEndPersuasionOnConsequence, 100, null);

            // success
            starter.AddDialogLine("lt_trade_agent", "ltta_barter_next_reservation", "lt_trade_agent_intro", "{=BeYbp6M2}Very well. You've convinced me that this is something I can consider.", new ConversationSentence.OnConditionDelegate(this.ConversationSuccessWithBarterOnCondition), new ConversationSentence.OnConsequenceDelegate(this.ConversationSuccessWithBarterOnConsequence), 100, null);

            // fail
            starter.AddDialogLine("lt_trade_agent", "ltta_barter_next_reservation", "lt_trade_agent_intro", "{=!}{FAILED_PERSUASION_LINE}", ConversationPlayerHasFailedOnCondition, new ConversationSentence.OnConsequenceDelegate(this.ConversationOnEndPersuasionOnConsequence), 100, null);


        }



        private bool CanBePersuaded()
        {

            MBTextManager.SetTextVariable("NO_PERSUASION_RESPONSE", new TextObject("NO_PERSUASION_RESPONSE - ERROR. Report to mod developer with the screenshot.", null), false);

            // recently tried to persuade, cooldown not passed yet
            if (this._previousPersuasionAttempts.Any((PersuasionAttempt x) => x.PersuadedHero == Hero.OneToOneConversationHero))
            {
                MBTextManager.SetTextVariable("NO_PERSUASION_RESPONSE", new TextObject("{=03lc5R2t}You have tried to persuade me before. I will not stand your words again.", null), false);
                return false;
            }

            Hero notable = Hero.OneToOneConversationHero;
            if (LTTABehaviour.Instance == null) return false;
            LTTATradeData tradeData = LTTABehaviour.Instance.GetTradeAgentTradeData(notable);
            if (tradeData == null) return false;

            // minimal fee percent is 3
            if (tradeData.FeePercent < 4)
            {
                MBTextManager.SetTextVariable("NO_PERSUASION_RESPONSE", new TextObject("Do you want to bleed me dry?! No nono nonono... Don't ever mention this again.", null), false);
                return false;
            }

            return true;
        }



        // fail on condition - failed to persuade
        public bool ConversationPlayerHasFailedOnCondition()
        {
            if (this.GetCurrentPersuasionTask().Options.All((PersuasionOptionArgs x) => x.IsBlocked) && !ConversationManager.GetPersuasionProgressSatisfied())
            {
                PersuasionTask anyFailedPersuasionTask = this.GetAnyFailedPersuasionTask();
                if (anyFailedPersuasionTask != null)
                {
                    MBTextManager.SetTextVariable("FAILED_PERSUASION_LINE", anyFailedPersuasionTask.FinalFailLine, false);
                }
                return true;
            }
            return false;
        }

        private PersuasionTask GetAnyFailedPersuasionTask()
        {
            foreach (PersuasionTask persuasionTask in this._allReservations)
            {
                if (!this.CanAttemptToPersuade(Hero.OneToOneConversationHero, persuasionTask.ReservationType))
                {
                    return persuasionTask;
                }
            }
            return null;
        }

        private bool CanAttemptToPersuade(Hero targetHero, int reservationType)
        {
            foreach (PersuasionAttempt persuasionAttempt in this._previousPersuasionAttempts)
            {
                if (_debug) 
                { 
                    if (persuasionAttempt.Matches(targetHero, reservationType) && !persuasionAttempt.IsSuccesful() && persuasionAttempt.GameTime.ElapsedHoursUntilNow < 1f) return false; 
                }
                else 
                { 
                    if (persuasionAttempt.Matches(targetHero, reservationType) && !persuasionAttempt.IsSuccesful() && persuasionAttempt.GameTime.ElapsedDaysUntilNow < (float)_daysToWaitTillFeeRenegotiation) return false; 
                }
            }
            return true;
        }



        // success
        public bool ConversationSuccessWithBarterOnCondition()
        {
            return ConversationManager.GetPersuasionProgressSatisfied();
        }

        public void ConversationSuccessWithBarterOnConsequence()
        {
            ConversationOnEndPersuasionOnConsequence();

            // decrease fee           
            if (LTTABehaviour.Instance == null) return;
            Hero notable = Hero.OneToOneConversationHero;
            LTTATradeData tradeData = LTTABehaviour.Instance.GetTradeAgentTradeData(notable);
            if (tradeData == null) return;
            
            tradeData.FeePercent -= 1;
            if (tradeData.FeePercent < 3) tradeData.FeePercent = 3; // just in case
            else
            {
                LTLogger.IMTAGreen("Fee reduced to: " + tradeData.FeePercent.ToString() + "%");
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(notable, Hero.MainHero, -1, true);
            }
        }
        
        private PersuasionTask GetCurrentPersuasionTask()
        {
            foreach (PersuasionTask persuasionTask in this._allReservations)
            {
                if (!persuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked)) return persuasionTask;
            }
            return this._allReservations[this._allReservations.Count - 1];
        }


        private bool ConversationPersuadeOptionOnCondition(int option)
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > option)
            {
                TextObject textObject = new("{PERSUASION_OPTION_LINE} {SUCCESS_CHANCE}", null);
                textObject.SetTextVariable("SUCCESS_CHANCE", PersuasionHelper.ShowSuccess(currentPersuasionTask.Options.ElementAt(option), true));
                textObject.SetTextVariable("PERSUASION_OPTION_LINE", currentPersuasionTask.Options.ElementAt(option).Line);

                string var = "PERSUADE_ATTEMPT_" + (option+1).ToString();
                MBTextManager.SetTextVariable(var, textObject, false);
                return true;
            }
            return false;
        }
        private void ConversationPersuadeOptionOnConsequence(int option)
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > option) currentPersuasionTask.Options[option].BlockTheOption(true);
        }


        // how to join all these into 1 ?
        private bool PersuasionOption1ClickableOnCondition1(out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > 0)  return !currentPersuasionTask.Options.ElementAt(0).IsBlocked;           
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }

        private bool PersuasionOption2ClickableOnCondition2(out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > 1) return !currentPersuasionTask.Options.ElementAt(1).IsBlocked;
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }

        private bool PersuasionOption3ClickableOnCondition3(out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > 2) return !currentPersuasionTask.Options.ElementAt(2).IsBlocked;
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }

        private bool PersuasionOption4ClickableOnCondition4(out TextObject hintText)
        {
            hintText = TextObject.Empty;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask.Options.Count > 3) return !currentPersuasionTask.Options.ElementAt(3).IsBlocked;
            hintText = new TextObject("{=9ACJsI6S}Blocked", null);
            return false;
        }



        private PersuasionOptionArgs SetupPersuasionOption(int option)
        {
            return this.GetCurrentPersuasionTask().Options.ElementAt(option);
        }



        private void ConversationOptionReactionOnConsequence()
        {
            Tuple<PersuasionOptionArgs, PersuasionOptionResult> tuple = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>();

            float difficulty = Campaign.Current.Models.PersuasionModel.GetDifficulty(PersuasionDifficulty.Medium);
            Campaign.Current.Models.PersuasionModel.GetEffectChances(tuple.Item1, out float moveToNextStageChance, out float blockRandomOptionChance, difficulty);
            PersuasionTask persuasionTask = this.FindTaskOfOption(tuple.Item1);
            persuasionTask.ApplyEffects(moveToNextStageChance, blockRandomOptionChance);
            PersuasionAttempt item = new(Hero.OneToOneConversationHero, CampaignTime.Now, tuple.Item1, tuple.Item2, persuasionTask.ReservationType);
            this._previousPersuasionAttempts.Add(item);
        }


        private PersuasionTask FindTaskOfOption(PersuasionOptionArgs optionChosenWithLine)
        {
            foreach (PersuasionTask persuasionTask in this._allReservations)
            {
                using (List<PersuasionOptionArgs>.Enumerator enumerator2 = persuasionTask.Options.GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        if (enumerator2.Current.Line == optionChosenWithLine.Line)
                        {
                            return persuasionTask;
                        }
                    }
                }
            }
            return null;
        }

        private bool ConversationOptionReactionOnCondition()
        {
            PersuasionOptionResult item = ConversationManager.GetPersuasionChosenOptions().Last<Tuple<PersuasionOptionArgs, PersuasionOptionResult>>().Item2;
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (item == PersuasionOptionResult.Failure)
            {
                MBTextManager.SetTextVariable("IMMEDIATE_FAILURE_LINE", (currentPersuasionTask?.ImmediateFailLine) ?? TextObject.Empty, false);
                MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=18xOURG4}Hmm.. No... {IMMEDIATE_FAILURE_LINE}", false);
            }
            else
            {
                if (item == PersuasionOptionResult.CriticalFailure)
                {
                    MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=Lj5Lghww}What? No...", false);
                    TextObject text = (currentPersuasionTask?.ImmediateFailLine) ?? TextObject.Empty;
                    MBTextManager.SetTextVariable("IMMEDIATE_FAILURE_LINE", text, false);
                    MBTextManager.SetTextVariable("PERSUASION_REACTION", "{=18xOURG4}Hmm.. No... {IMMEDIATE_FAILURE_LINE}", false);
                    using (List<PersuasionTask>.Enumerator enumerator = this._allReservations.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            PersuasionTask persuasionTask = enumerator.Current;
                            persuasionTask.BlockAllOptions();
                        }
                        return true;
                    }
                }
                MBTextManager.SetTextVariable("PERSUASION_REACTION", PersuasionHelper.GetDefaultPersuasionOptionReaction(item), false);
            }
            return true;
        }

        public bool ConversationCheckIfReservationsMetOnCondition()
        {
            PersuasionTask currentPersuasionTask = this.GetCurrentPersuasionTask();
            if (currentPersuasionTask == this._allReservations[this._allReservations.Count - 1])
            {
                if (currentPersuasionTask.Options.All((PersuasionOptionArgs x) => x.IsBlocked))
                {
                    return false;
                }
            }
            if (!ConversationManager.GetPersuasionProgressSatisfied())
            {
                MBTextManager.SetTextVariable("PERSUASION_TASK_LINE", currentPersuasionTask.SpokenLine, false);
                return true;
            }
            return false;
        }

        private void StartPersuasionOnConsequence()
        {

            this._allReservations = this.GetPersuasionTasks();
            this._maximumScoreCap = (float)this._allReservations.Count * 1f;
            float num = 0f;
            foreach (PersuasionTask persuasionTask in this._allReservations)
            {
                foreach (PersuasionAttempt persuasionAttempt in this._previousPersuasionAttempts)
                {
                    if (persuasionAttempt.Matches(Hero.OneToOneConversationHero, persuasionTask.ReservationType))
                    {
                        switch (persuasionAttempt.Result)
                        {
                            case PersuasionOptionResult.CriticalFailure:
                                num -= this._criticalFailValue;
                                break;
                            case PersuasionOptionResult.Failure:
                                num -= 0f;
                                break;
                            case PersuasionOptionResult.Success:
                                num += this._successValue;
                                break;
                            case PersuasionOptionResult.CriticalSuccess:
                                num += this._criticalSuccessValue;
                                break;
                        }
                    }
                }
            }

            ConversationManager.StartPersuasion(this._maximumScoreCap, this._successValue, this._failValue, this._criticalSuccessValue, this._criticalFailValue, num, PersuasionDifficulty.Medium);
        }


        PersuasionArgumentStrength GetPersuasionArgumentStrength(int value)
        {
            PersuasionArgumentStrength persuasionArgumentStrength = PersuasionArgumentStrength.Normal;

            if (value < -2) return PersuasionArgumentStrength.ExtremelyHard;
            if (value == -2) return PersuasionArgumentStrength.VeryHard;
            if (value == -1) return PersuasionArgumentStrength.Hard;
            if (value == 1) return PersuasionArgumentStrength.Easy;
            if (value == 2) return PersuasionArgumentStrength.VeryEasy;
            if (value > 2) return PersuasionArgumentStrength.ExtremelyEasy;

            return persuasionArgumentStrength;
        }

        private List<PersuasionTask> GetPersuasionTasks()
        {
            Random rand = new();

            List<PersuasionTask> list = new();

            Hero notable = Hero.OneToOneConversationHero;

            if (LTTABehaviour.Instance == null) return list;
            LTTATradeData tradeData = LTTABehaviour.Instance.GetTradeAgentTradeData(notable);
            if (tradeData == null) return list;

            // Change difficulty based on Fee %
            int argumentStrengthModifier;
            if (tradeData.FeePercent < 6) argumentStrengthModifier = -3;
            else if (tradeData.FeePercent < 8) argumentStrengthModifier = -2;
            else if (tradeData.FeePercent < 10) argumentStrengthModifier = -1;
            else if (tradeData.FeePercent < 12) argumentStrengthModifier = 0;
            else if (tradeData.FeePercent < 14) argumentStrengthModifier = 1;
            else if (tradeData.FeePercent < 18) argumentStrengthModifier = 2;
            else argumentStrengthModifier = 3;

            //LTLogger.IMRed("argumentStrengthModifier: " + argumentStrengthModifier.ToString());

            // Reservation #1 - Relation

            PersuasionTask persuasionTask = new(0)
            {
                ImmediateFailLine = new TextObject("I am not entirely comfortable discussing this with you.", null),
                FinalFailLine = new TextObject("I am simply not comfortable discussing this with you.", null)
            };

            int relation = CharacterRelationManager.GetHeroRelation(Hero.OneToOneConversationHero, Hero.MainHero);
            int persuasionArgumentStrength;


            if (relation <= 0)
            {
                persuasionTask.SpokenLine = new TextObject("I don't even like you. You expect me to discuss something like this with you?", null);
                persuasionArgumentStrength = -2;
            }
            else if (relation <= 20)
            {
                persuasionTask.SpokenLine = new TextObject("I barely know you, and you're asking me to lower my fee?", null);
                persuasionArgumentStrength = -1;
            }
            else
            {
                persuasionTask.SpokenLine = new TextObject("You are my friend, but even so, this is not a pleasant conversation to have.", null);
                persuasionArgumentStrength = 0;
            }

            // Charm, Honor
            if (relation <= 0)
            {
                TextObject textObject = new("I acknowledge that we may have differing opinions or impressions, but I'm dedicated to maintaining a constructive approach. Let's put any personal differences aside and work towards finding a solution that meets both our interests.", null);
                PersuasionOptionArgs option = new(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, GetPersuasionArgumentStrength(-2 + argumentStrengthModifier), rand.Next(100) > 70, textObject, null, false, true, false);
                persuasionTask.AddOptionToTask(option);
            }
            else if (relation <= 20)
            {
                TextObject textObject = new("Though we may be just getting to know each other, I believe in fostering a positive connection, and I look forward to engaging in fruitful discussion as we pursue common goal.", null);
                PersuasionOptionArgs option2 = new(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, GetPersuasionArgumentStrength(-1 + argumentStrengthModifier), rand.Next(100) > 80, textObject, null, false, true, false);
                persuasionTask.AddOptionToTask(option2);
            }
            else
            {
                TextObject textObject2 = new("With our friendship as a foundation, I'm confident we can navigate this smoothly while maintaining our positive relationship.", null);
                PersuasionOptionArgs option3 = new(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, GetPersuasionArgumentStrength(0 + argumentStrengthModifier), rand.Next(100) > 90, textObject2, null, false, true, false);
                persuasionTask.AddOptionToTask(option3);
            }

            // Trade, Generosity
            if (Hero.MainHero.GetTraitLevel(DefaultTraits.Generosity) > 0)
            {
                PersuasionOptionArgs option4 = new(DefaultSkills.Trade, DefaultTraits.Generosity, TraitEffect.Positive, GetPersuasionArgumentStrength(persuasionArgumentStrength + argumentStrengthModifier), rand.Next(100) > 70,
                    new TextObject("Rest assured that my reputation as skilled and generous precedes me, guaranteeing a prosperous and mutually beneficial partnership.", null), null, false, true, false);
                persuasionTask.AddOptionToTask(option4);
            }
            else
            {
                PersuasionOptionArgs option5 = new(DefaultSkills.Trade, DefaultTraits.Generosity, TraitEffect.Positive, GetPersuasionArgumentStrength(persuasionArgumentStrength - 1 + argumentStrengthModifier), rand.Next(100) > 80,
                    new TextObject("My proven track record as a competent and astute trader assures you of a fruitful and advantageous collaboration.", null), null, false, true, false);
                persuasionTask.AddOptionToTask(option5);
            }

            // Charm, Calculating 
            if (relation >= 20f)
            {
                PersuasionOptionArgs option6 = new(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, GetPersuasionArgumentStrength(persuasionArgumentStrength + argumentStrengthModifier), rand.Next(100) > 70,
                    new TextObject("You know me. I'll be careful not to get this get back to the wrong ears.", null), null, false, true, false);
                persuasionTask.AddOptionToTask(option6);
            }
            else
            {
                PersuasionOptionArgs option7 = new(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, GetPersuasionArgumentStrength(persuasionArgumentStrength - 1 + argumentStrengthModifier), rand.Next(100) > 80,
                    new TextObject("You must know of my reputation. You know that it's not in my interest to betray your trust.", null), null, false, true, false);
                persuasionTask.AddOptionToTask(option7);
            }
            list.Add(persuasionTask);




            // Reservation #2 - Market Competition

            PersuasionTask persuasionTask2 = new(1)
            {
                ImmediateFailLine = new TextObject("I do not see it that way.", null),
                FinalFailLine = new TextObject("I do not see it that way. We are done for now.", null)
            };

            int randInt = rand.Next(3);
            if (randInt == 2) persuasionTask2.SpokenLine = new TextObject("In this bustling market, where rivals offer similar services, maintaining my competitive fee is crucial. Lowering it would compromise my profitability and devalue the entire market's offerings.", null);
            else if (randInt == 1) persuasionTask2.SpokenLine = new TextObject("Amidst the clamor of this vibrant market, where competitors vie with similar services, preserving my competitive fee becomes imperative, for a reduction would undermine profitability and diminish the value of offerings across the market.", null);
            else persuasionTask2.SpokenLine = new TextObject("Within this bustling market, teeming with rivals offering akin services, upholding my competitive fee holds utmost significance, as a decrease would erode profitability and dilute the worth of offerings throughout the market.", null);


            // power rating
            if (notable.Power < 100)
            {
                PersuasionOptionArgs option8 = new(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, GetPersuasionArgumentStrength(argumentStrengthModifier), rand.Next(100) > 70,
                    new TextObject("Lowering the fee could attract a larger customer base, increase market share, and potentially enable you to negotiate better terms with suppliers, thus enhancing your power and profitability in the long run.", null), null, false, true, false);
                persuasionTask2.AddOptionToTask(option8);
            } else if (notable.Power < 200)
            {
                PersuasionOptionArgs option8 = new(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, GetPersuasionArgumentStrength(-1 + argumentStrengthModifier), rand.Next(100) > 80,
                    new TextObject("Lowering the fee could be a strategic move to differentiate yourself from competitors, attract new customers, and maintain a competitive edge in the market, ultimately benefiting your profitability and reinforcing your position as a respected player in the industry.", null), null, false, true, false);
                persuasionTask2.AddOptionToTask(option8);
            }
            else
            {
                PersuasionOptionArgs option8 = new(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, GetPersuasionArgumentStrength(-2 + argumentStrengthModifier), rand.Next(100) > 90,
                    new TextObject("Lowering the fee would not only strengthen your market dominance by discouraging potential competitors but also enable you to capture a larger market share, exploit economies of scale, and solidify your position as the industry leader, thereby maximizing your long-term profitability.", null), null, false, true, false);
                persuasionTask2.AddOptionToTask(option8);
            }

            // type of notable: merchant/artisan/gang leader
            if (notable.IsMerchant)
            {
                PersuasionOptionArgs option9 = new(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, GetPersuasionArgumentStrength(argumentStrengthModifier), rand.Next(100) > 70,
                    new TextObject("By adapting pricing strategies and providing additional value, you can strengthen your competitive position, attract loyal customers, and ultimately increase profitability as a powerful merchant.", null), null, false, true, false);
                persuasionTask2.AddOptionToTask(option9);
            } else if (notable.IsArtisan)
            {
                PersuasionOptionArgs option9 = new(DefaultSkills.Charm, DefaultTraits.Honor, TraitEffect.Positive, GetPersuasionArgumentStrength(-1 + argumentStrengthModifier), rand.Next(100) > 80,
                    new TextObject("By emphasizing your artisanal craftsmanship and offering unique, personalized experiences, you can successfully differentiate yourself from traditional merchants and attract a dedicated customer base that values authenticity, enabling you to compete effectively in the market.", null), null, false, true, false);
                persuasionTask2.AddOptionToTask(option9);
            } else if (notable.IsGangLeader)
            {
                PersuasionOptionArgs option9 = new(DefaultSkills.Charm, DefaultTraits.Valor, TraitEffect.Positive, GetPersuasionArgumentStrength(-2 + argumentStrengthModifier), rand.Next(100) > 90,
                    new TextObject("As a formidable figure in the industry, maintaining competitive pricing secures your position of strength, fosters customer loyalty, and solidifies your market dominance, ultimately boosting profitability and reinforcing your influence as a formidable trader.", null), null, false, true, false);
                persuasionTask2.AddOptionToTask(option9);
            }

            // random joke
            PersuasionOptionArgs option11 = new(DefaultSkills.Roguery, DefaultTraits.Valor, TraitEffect.Positive, GetPersuasionArgumentStrength(-3 + argumentStrengthModifier), true,
                new TextObject("Lowering fees may compromise profitability, but imagine the thrill of seeing rival traders' faces as you crash the market!", null), null, false, true, false);
            persuasionTask2.AddOptionToTask(option11);

            list.Add(persuasionTask2);




            // Reservation #3 - Risk and Uncertainty

            PersuasionTask persuasionTask3 = new(2)
            {
                ImmediateFailLine = new TextObject("Nah, I don't agree.", null),
                FinalFailLine = new TextObject("Nah, I don't agree. Let's finish this.", null),
                SpokenLine = new TextObject("My fee accounts for the risks and uncertainties inherent in trading, protecting me against bandit attacks, unpredictable weather, and political instability, ensuring I can sustain my business and mitigate potential losses.", null)
            };

            // culture
            if (Hero.MainHero.Culture == notable.Culture)
            {
                PersuasionOptionArgs option12 = new(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, GetPersuasionArgumentStrength(argumentStrengthModifier), rand.Next(100) > 80,
                        new TextObject("Though we may hail from the same culture, adjusting fees to match the ever-changing market conditions can help us navigate risks and uncertainties, fostering resilience and prosperity within our shared trading realm.", null), null, false, true, false);
                persuasionTask3.AddOptionToTask(option12);
            } else
            {
                PersuasionOptionArgs option12 = new(DefaultSkills.Charm, DefaultTraits.Generosity, TraitEffect.Positive, GetPersuasionArgumentStrength(-1 + argumentStrengthModifier), rand.Next(100) > 80,
                        new TextObject("Though our cultures may be different, embracing the opportunity to learn and adapt to the distinct risks and uncertainties of each other's trading environments can lead to prosperous exchanges and mutual growth in our diverse cultural realms.", null), null, false, true, false);
                persuasionTask3.AddOptionToTask(option12);
            }

            // sex
            if (Hero.MainHero.IsFemale == notable.IsFemale)
            {
                TextObject option13to = new("As one {?HERO.GENDER}woman{?}man{\\?} to another, it's important to consider that embracing fair competition and reasonable fees can create a more balanced market environment, benefiting both our businesses and fostering mutual respect in our trade.", null);
                StringHelpers.SetCharacterProperties("HERO", notable.CharacterObject, option13to, false);
                PersuasionOptionArgs option13 = new(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, GetPersuasionArgumentStrength(-1 + argumentStrengthModifier), rand.Next(100) > 80, option13to, null, false, true, false);
                persuasionTask3.AddOptionToTask(option13);
            } else
            {               
                PersuasionOptionArgs option13 = new(DefaultSkills.Charm, DefaultTraits.Mercy, TraitEffect.Positive, GetPersuasionArgumentStrength(-1 + argumentStrengthModifier), rand.Next(100) > 80,
                    new TextObject("Let us acknowledge that upholding fairness in competition and setting reasonable fees can create a just and balanced trading environment, ensuring equal opportunities for success and fostering harmony in this town.", null), null, false, true, false);
                persuasionTask3.AddOptionToTask(option13);
            }

            // age
            if (Math.Abs(Hero.MainHero.Age - notable.Age) < 10)
            {
                PersuasionOptionArgs option14 = new(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, GetPersuasionArgumentStrength(argumentStrengthModifier), rand.Next(100) > 80, 
                    new TextObject("Given our comparable ages, let us adapt our pricing strategies and embrace the changing market to ensure mutual success.", null), null, false, true, false);
                persuasionTask3.AddOptionToTask(option14);
            } else
            {
                PersuasionOptionArgs option14 = new(DefaultSkills.Charm, DefaultTraits.Calculating, TraitEffect.Positive, GetPersuasionArgumentStrength(-1 + argumentStrengthModifier), rand.Next(100) > 80,
                    new TextObject("Despite our age difference, let us combine our wisdom and energy, adapting pricing strategies to the dynamic market.", null), null, false, true, false);
                persuasionTask3.AddOptionToTask(option14);
            }

            list.Add(persuasionTask3);




            // Reservation #4 - Quality and Expertise

            PersuasionTask persuasionTask4 = new(3)
            {
                ImmediateFailLine = new TextObject("I must refuse.", null),
                FinalFailLine = new TextObject("I must refuse. Let's talk about something else.", null),
                SpokenLine = new TextObject("The expertise and exceptional quality I provide in my services justify the higher fee, as they offer a unique value that sets me apart from competitors, ensuring clients receive unparalleled excellence and specialized expertise that cannot be easily replicated.", null)
            };

            //  threaten to leave
            PersuasionOptionArgs option16 = new(DefaultSkills.Trade, DefaultTraits.Valor, TraitEffect.Positive, GetPersuasionArgumentStrength(argumentStrengthModifier), rand.Next(100) > 80,
                new TextObject("I understand your position, and while your exceptional service quality is acknowledged, not adjusting your fee to be more competitive may jeopardize customer loyalty, potentially impacting our ongoing business partnership.", null), null, false, true, false);
            persuasionTask4.AddOptionToTask(option16);

            // end of patience
            PersuasionOptionArgs option17 = new(DefaultSkills.Athletics, DefaultTraits.Valor, TraitEffect.Positive, GetPersuasionArgumentStrength(-2 + argumentStrengthModifier), true,
                new TextObject("Oh come one! Cut the cr... Well... you are absolutely right... Your service quality is exceptional but I can't operate with such a high fee and ask you to reconsider.", null), null, false, true, false);
            persuasionTask4.AddOptionToTask(option17);

            // threaten to use force
            PersuasionOptionArgs option18 = new(DefaultSkills.Roguery, DefaultTraits.Honor, TraitEffect.Negative, GetPersuasionArgumentStrength(-3 + argumentStrengthModifier), true,
                new TextObject("Listen well, trader! You'd be wise to reconsider, for I have the might to ravage your pitiful business to the ground, should you persist in your stubborn refusal to negotiate fair terms!", null), null, false, true, false);
            persuasionTask4.AddOptionToTask(option18);

            list.Add(persuasionTask4);
            return list;
        }


        private void ConversationOnEndPersuasionOnConsequence()
        {
            this._allReservations = null;
            ConversationManager.EndPersuasion();
        }


        private void RemoveOldAttempts()
        {
            //LTLogger.IMRed("RemoveOldAttempts, _previousPersuasionAttempts.Count: " + _previousPersuasionAttempts.Count.ToString());
            for (int i = _previousPersuasionAttempts.Count - 1; i >= 0; i--)
            {
                if (_debug) 
                { 
                    if (this._previousPersuasionAttempts[i].GameTime.ElapsedHoursUntilNow > 1f) this._previousPersuasionAttempts.RemoveAt(i); 
                }
                else
                {
                    //LTLogger.IMRed("ElapsedDaysUntilNow: " + this._previousPersuasionAttempts[i].GameTime.ElapsedDaysUntilNow.ToString());
                    if (this._previousPersuasionAttempts[i].GameTime.ElapsedDaysUntilNow > (float)_daysToWaitTillFeeRenegotiation) this._previousPersuasionAttempts.RemoveAt(i);
                }
            }
        }

    }
}
