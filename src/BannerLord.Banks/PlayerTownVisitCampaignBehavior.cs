using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerLord.Banks
{
    public class PlayerTownVisitCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                (object)this,
                new Action<CampaignGameStarter>(this.AddGameMenus)
            );
        }

        public override void SyncData(IDataStore dataStore)
        {
            // no-op
        }

        protected void AddGameMenus(CampaignGameStarter starter)
        {
            // add bank to town menu
            starter.AddGameMenuOption(
                "town",
                "town_bank",
                "Visit the bank",
                new GameMenuOption.OnConditionDelegate(PlayerTownVisitCampaignBehavior.HandleTownBankMenuOptions),
                (args => GameMenu.SwitchToMenu("town_bank")),
                index: 9
            );
        }

        private static bool HandleTownBankMenuOptions(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

            bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(
                Settlement.CurrentSettlement,
                SettlementAccessModel.SettlementAction.Trade, // don't want to figure out the logic here, if you can trade you can bank
                out var disableOption,
                out var disabledText
            );

            return MenuHelper.SetOptionProperties(args, canPlayerDo, disableOption, disabledText);
        }
    }
}