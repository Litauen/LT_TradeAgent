using LT.Logger;
using LT_Education;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;


namespace LT_TradeAgent
{
    public class SubModule : MBSubModuleBase
    {

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            try
            {
                if (gameStarterObject is not CampaignGameStarter) return;
                if (game.GameType is not Campaign) return;

                ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new LTTABehaviour());

            }
            catch (Exception ex)
            {
                LTLogger.IMRed("LT_TradeAgent: An Error occurred, when trying to load the mod into your current game.");
                LTLogger.LogError(ex);
            }
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            LTLogger.IMGrey(LHelpers.GetModName() + " Loaded");
        }

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);

            try
            {
                string[] modulesNames = Utilities.GetModulesNames();
                for (int i = 0; i < modulesNames.Length; i++)
                {
                    //LTLogger.IMRed(modulesNames[i]);
                    if (modulesNames[i] == "BannerKings")
                    {
                        if (LTTABehaviour.Instance != null)
                        {
                            LTTABehaviour.Instance.BannerKingsActive = true;
                            //#if DEBUG
                            //                            //LTLogger.IMGreen("BannerKings detected");
                            //#endif
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LTLogger.LogError(ex);
            }

        }
    }
}