using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Localization;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using System.Linq;

namespace BannerLord.Banks
{
    public class BankCampaignBehavior : CampaignBehaviorBase
    {
        private const string BANK_INFO_TEXT_VARIABLE = "BANK_INFO";
        private const string BANK_INFO_FLAVOR_TEXT_VARIABLE = "BANK_INFO_FLAVOR";
        private const string BANK_INFO_APY_TEXT_VARIABLE = "BANK_INFO_APY";
        private const string BANK_INFO_BALANCE_TEXT_VARIABLE = "BANK_INFO_BALANCE";
        private const string BANK_INFO_WITHDRAW_TEXT_VARIABLE = "BANK_INFO_WITHDRAW";

        private const float BASE_APY = 0.6f;
        private const int DAYS_IN_A_YEAR = 120;
        private const int PROSPEROUS_TOWN = 6000;
        private const float WITHDRAW_FEE = 0.1f;
        private const float TRADE_SKILL_PROFIT_MULTIPLIER = 0.15f;

        private const string CLAN_PORTFOLIO_DATA_KEY = "BannerLord.Banks.ClanPortfolios";

        private List<Portfolio> _portfolios;

        public BankCampaignBehavior()
        {
            _portfolios = new List<Portfolio>();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(
                this,
                new Action<CampaignGameStarter>(this.HandleSessionLaunchedEvent)
            );

            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(
                this,
                new Action<Clan>(this.HandleDailyTickClanEvent)
            );
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsLoading)
            {
                try
                {
                    var clanJson = string.Empty;

                    dataStore.SyncData(CLAN_PORTFOLIO_DATA_KEY, ref clanJson);
                    _portfolios = JsonConvert.DeserializeObject<List<Portfolio>>(clanJson);
                }
                catch (System.Exception) { }
            }

            if (dataStore.IsSaving)
            {
                var clanJson = JsonConvert.SerializeObject(_portfolios);
                dataStore.SyncData(CLAN_PORTFOLIO_DATA_KEY, ref clanJson);
            }
        }

        protected void HandleSessionLaunchedEvent(CampaignGameStarter starter)
        {
            AddTownBankMenu(starter);
            AddTownBankDepositMenu(starter);
            AddTownBankWithdrawMenu(starter);
        }

        protected void AddTownBankMenu(CampaignGameStarter starter)
        {
            // add town bank menu
            starter.AddGameMenu(
                "town_bank",
                $"{{{BANK_INFO_TEXT_VARIABLE}}}",
                new OnInitDelegate(this.HandleTownBankGameMenuInit),
                GameOverlays.MenuOverlayType.SettlementWithCharacters
            );

            // add deposit to town bank menu
            starter.AddGameMenuOption(
                "town_bank",
                "town_bank_deposit",
                "Make a deposit",
                new GameMenuOption.OnConditionDelegate(BankCampaignBehavior.HandleTownBankDepositMenuOptions),
                (args => GameMenu.SwitchToMenu("town_bank_deposit"))
            );

            // add withdaw to town bank menu
            starter.AddGameMenuOption(
                "town_bank",
                "town_bank_withdraw",
                "Make a withdrawl",
                new GameMenuOption.OnConditionDelegate(BankCampaignBehavior.HandleTownBankWithdrawMenuOptions),
                (args => GameMenu.SwitchToMenu("town_bank_withdraw"))
            );

            // add back to town bank menu
            starter.AddGameMenuOption(
                "town_bank",
                "town_bank_back",
                "Back to town center",
                new GameMenuOption.OnConditionDelegate(BankCampaignBehavior.HandleBackMenuOptions),
                (args => GameMenu.SwitchToMenu("town")),
                true
            );
        }

        private void HandleTownBankGameMenuInit(MenuCallbackArgs args)
        {
            HandleFlavorText();
            HandleYieldText();
            HandleAccountBalanceText();

            MBTextManager.SetTextVariable(BANK_INFO_TEXT_VARIABLE, $"{{{BANK_INFO_FLAVOR_TEXT_VARIABLE}}}\n \n{{{BANK_INFO_APY_TEXT_VARIABLE}}}\n \n{{{BANK_INFO_BALANCE_TEXT_VARIABLE}}}", false);
        }


        private Portfolio GetPortfolio(Clan clan, Settlement settlement)
        {
            var clanId = clan.Id.ToString();
            var settlementId = settlement.Id.ToString();

            return _portfolios
                .Where(p => p.ClanId.Equals(clanId, StringComparison.OrdinalIgnoreCase))
                .Where(p => p.SettlementId.Equals(settlementId, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        private void HandleFlavorText()
        {
            var portfolio = GetPortfolio(PartyBase.MainParty.LeaderHero.Clan, Settlement.CurrentSettlement);

            if (portfolio is null)
            {
                MBTextManager.SetTextVariable(
                    BANK_INFO_FLAVOR_TEXT_VARIABLE,
                    new TextObject("After waiting an hour you are finally led to the moneylenders office.")
                );
            }
            else if (portfolio.Denars < 10000)
            {
                MBTextManager.SetTextVariable(
                    BANK_INFO_FLAVOR_TEXT_VARIABLE,
                    new TextObject("After waiting a few minutes you are led the moneylenders office.")
                );
            }
            else if (portfolio.Denars < 100000)
            {
                MBTextManager.SetTextVariable(
                    BANK_INFO_FLAVOR_TEXT_VARIABLE,
                    new TextObject($"You are immediately ushered into the office.")
                );
            }
            else
            {
                MBTextManager.SetTextVariable(
                    BANK_INFO_FLAVOR_TEXT_VARIABLE,
                    new TextObject($"The moneylender greets you personally.")
                );
            }
        }

        private void HandleYieldText()
        {
            var dpy = CalculateDailyInterest(Settlement.CurrentSettlement);
            MBTextManager.SetTextVariable(
                BANK_INFO_APY_TEXT_VARIABLE,
                new TextObject($"Current Daily Yield:\n100 denar for every {Math.Round(100 / dpy):N0} invested")
            );
        }

        private float CalculateDailyInterest(Settlement settlement)
        {
            var baseInterestRate = BASE_APY / DAYS_IN_A_YEAR;
            var prosperityAdjustment = settlement.Prosperity / PROSPEROUS_TOWN;
            var adjustedInterestRate = baseInterestRate * prosperityAdjustment;

            return adjustedInterestRate;
        }

        private void HandleAccountBalanceText()
        {
            var portfolio = GetPortfolio(PartyBase.MainParty.LeaderHero.Clan, Settlement.CurrentSettlement);

            if (portfolio is null)
            {
                MBTextManager.SetTextVariable(
                    BANK_INFO_BALANCE_TEXT_VARIABLE,
                    new TextObject("You do not have an account at this bank")
                );
            }
            else
            {
                MBTextManager.SetTextVariable(
                    BANK_INFO_BALANCE_TEXT_VARIABLE,
                    new TextObject($"Account Balance:\n{portfolio.Denars:N0} denars")
                );
            }
        }

        private static bool HandleTownBankDepositMenuOptions(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        private static bool HandleTownBankWithdrawMenuOptions(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        private static bool HandleBackMenuOptions(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        protected void AddTownBankDepositMenu(CampaignGameStarter starter)
        {
            // add bank deposit menu
            starter.AddGameMenu(
                "town_bank_deposit",
                $"{{{BANK_INFO_BALANCE_TEXT_VARIABLE}}}",
                new OnInitDelegate(this.HandleTownBankDepositGameMenuInit),
                GameOverlays.MenuOverlayType.SettlementWithCharacters
            );

            // add 1000 denar deposit option
            starter.AddGameMenuOption(
                "town_bank_deposit",
                "town_bank_deposit_1000_denar",
                $"Deposit {1000:N0} denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankDepositDenarMenuOptions_1000),
                (args => this.DepositDenars(1000)),
                isRepeatable: true
            );

            // add 10,000 denar deposit option
            starter.AddGameMenuOption(
                "town_bank_deposit",
                "town_bank_deposit_10000_denar",
                $"Deposit {10000:N0} denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankDepositDenarMenuOptions_10000),
                (args => this.DepositDenars(10000)),
                isRepeatable: true
            );

            // add 100,000 denar deposit option
            starter.AddGameMenuOption(
                "town_bank_deposit",
                "town_bank_deposit_100000_denar",
                $"Deposit {100000:N0} denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankDepositDenarMenuOptions_100000),
                (args => this.DepositDenars(100000)),
                isRepeatable: true
            );

            // add 1,000,000 denar deposit option
            starter.AddGameMenuOption(
                "town_bank_deposit",
                "town_bank_deposit_1000000_denar",
                $"Deposit {1000000:N0} denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankDepositDenarMenuOptions_1000000),
                (args => this.DepositDenars(1000000)),
                isRepeatable: true
            );

            // add back to bank menu to deposit menu
            starter.AddGameMenuOption(
                "town_bank_deposit",
                "town_bank_deposit_back",
                "Back to bank",
                new GameMenuOption.OnConditionDelegate(BankCampaignBehavior.HandleBackMenuOptions),
                (args => GameMenu.SwitchToMenu("town_bank")),
                true
            );
        }

        private void HandleTownBankDepositGameMenuInit(MenuCallbackArgs args)
        {
            HandleAccountBalanceText();
        }

        private bool HandleTownBankDepositDenarMenuOptions_1000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;

            var hasEnoughMoney = PartyBase.MainParty.LeaderHero.Gold >= 1000;

            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars"));
        }

        private bool HandleTownBankDepositDenarMenuOptions_10000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;

            var hasEnoughMoney = PartyBase.MainParty.LeaderHero.Gold >= 10000;

            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars"));
        }

        private bool HandleTownBankDepositDenarMenuOptions_100000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;

            var hasEnoughMoney = PartyBase.MainParty.LeaderHero.Gold >= 100000;

            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars"));
        }

        private bool HandleTownBankDepositDenarMenuOptions_1000000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;

            var hasEnoughMoney = PartyBase.MainParty.LeaderHero.Gold >= 1000000;

            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars"));
        }

        private void DepositDenars(int denars)
        {
            var portfolio = GetPortfolio(PartyBase.MainParty.LeaderHero.Clan, Settlement.CurrentSettlement);

            // just in case the UI didn't detect it
            if (PartyBase.MainParty.LeaderHero.Gold >= denars)
            {
                if (portfolio is null)
                {
                    portfolio = new Portfolio
                    {
                        ClanId = PartyBase.MainParty.LeaderHero.Clan.Id.ToString(),
                        SettlementId = Settlement.CurrentSettlement.Id.ToString(),
                        Denars = 0
                    };
                    _portfolios.Add(portfolio);

                }

                PartyBase.MainParty.LeaderHero.ChangeHeroGold(-denars);
                portfolio.Denars += denars;
            }

            GameMenu.SwitchToMenu("town_bank_deposit");
        }

        private void HandleDailyTickClanEvent(Clan clan)
        {
            var dailyInterest = new ExplainedNumber();

            CalculateClanBankInterest(clan, ref dailyInterest);

            SkillLevelingManager.OnTradeProfitMade(clan.Leader, (int)Math.Round(dailyInterest.ResultNumber * TRADE_SKILL_PROFIT_MULTIPLIER));
        }

        public void CalculateClanBankInterest(Clan clan, ref ExplainedNumber goldChange)
        {
            if (clan.Leader is null)
            {
                return;
            }

            var clanPortfolios = _portfolios
                .Where(p => p.ClanId.Equals(clan.Leader.Clan.Id.ToString(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (clanPortfolios.Count == 0)
            {
                return;
            }

            foreach (var portfolio in clanPortfolios)
            {
                if (portfolio.Denars > 0)
                {
                    var settlement = Settlement.FindFirst(s => s.Id.ToString().Equals(portfolio.SettlementId, StringComparison.OrdinalIgnoreCase));

                    if (settlement != null)
                    {
                        var dpy = CalculateDailyInterest(settlement);
                        var interest = (int)Math.Round(dpy * portfolio.Denars);
                        goldChange.Add(interest, new TextObject($"Bank Interest from {settlement.Name}"));
                    }
                }
            }
        }

        protected void AddTownBankWithdrawMenu(CampaignGameStarter starter)
        {
            // add bank withdraw menu
            starter.AddGameMenu(
                "town_bank_withdraw",
                $"{{{BANK_INFO_WITHDRAW_TEXT_VARIABLE}}}\n \n{{{BANK_INFO_BALANCE_TEXT_VARIABLE}}}",
                new OnInitDelegate(this.HandleTownBankWithdrawGameMenuInit),
                GameOverlays.MenuOverlayType.SettlementWithCharacters
            );

            // add 1000 denar withdraw option
            starter.AddGameMenuOption(
                "town_bank_withdraw",
                "town_bank_withdraw_1000_denar",
                $"Withdraw {1000:N0} denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankWithdrawDenarMenuOptions_1000),
                (args => this.WithdrawDenars(1000)),
                isRepeatable: true
            );

            // add 10,000 denar withdraw option
            starter.AddGameMenuOption(
                "town_bank_withdraw",
                "town_bank_withdraw_10000_denar",
                $"Withdraw {10000:N0} denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankWithdrawDenarMenuOptions_10000),
                (args => this.WithdrawDenars(10000)),
                isRepeatable: true
            );

            // add 100,000 denar withdraw option
            starter.AddGameMenuOption(
                "town_bank_withdraw",
                "town_bank_withdraw_100000_denar",
                $"Withdraw {100000:N0} denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankWithdrawDenarMenuOptions_100000),
                (args => this.WithdrawDenars(100000)),
                isRepeatable: true
            );

            // add 1,000,000 denar withdraw option
            starter.AddGameMenuOption(
                "town_bank_withdraw",
                "town_bank_withdraw_1000000_denar",
                $"Withdraw {100000:N0} denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankWithdrawDenarMenuOptions_1000000),
                (args => this.WithdrawDenars(1000000)),
                isRepeatable: true
            );

            // add 1,000,000 denar withdraw option
            starter.AddGameMenuOption(
                "town_bank_withdraw",
                "town_bank_withdraw_all_denar",
                $"Withdraw all denar",
                new GameMenuOption.OnConditionDelegate(this.HandleTownBankWithdrawDenarMenuOptions_All),
                (args => this.WithdrawDenars(Int32.MaxValue)),
                isRepeatable: true
            );


            // add back to bank menu to deposit menu
            starter.AddGameMenuOption(
                "town_bank_withdraw",
                "town_bank_withdraw_back",
                "Back to bank",
                new GameMenuOption.OnConditionDelegate(BankCampaignBehavior.HandleBackMenuOptions),
                (args => GameMenu.SwitchToMenu("town_bank")),
                true
            );
        }

        private void HandleTownBankWithdrawGameMenuInit(MenuCallbackArgs args)
        {
            HandleBankInfoWithdrawText();
            HandleAccountBalanceText();
        }

        private void HandleBankInfoWithdrawText()
        {
            MBTextManager.SetTextVariable(
                BANK_INFO_WITHDRAW_TEXT_VARIABLE,
                new TextObject($"The withdrawl fee is currently {WITHDRAW_FEE * 100:F0}%")
            );
        }

        private bool HandleTownBankWithdrawDenarMenuOptions_1000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            var hasEnoughMoney = GetAccountBalance() >= 1000;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars in account"));
        }

        private bool HandleTownBankWithdrawDenarMenuOptions_10000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            var hasEnoughMoney = GetAccountBalance() >= 10000;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars in account"));
        }

        private bool HandleTownBankWithdrawDenarMenuOptions_100000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            var hasEnoughMoney = GetAccountBalance() >= 100000;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars in account"));
        }

        private bool HandleTownBankWithdrawDenarMenuOptions_1000000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            var hasEnoughMoney = GetAccountBalance() >= 1000000;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars in account"));
        }

        private bool HandleTownBankWithdrawDenarMenuOptions_All(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            var hasEnoughMoney = GetAccountBalance() > 0;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("There are no denars to withdraw"));
        }

        private float GetAccountBalance()
        {
            var portfolio = GetPortfolio(PartyBase.MainParty.LeaderHero.Clan, Settlement.CurrentSettlement);

            if (portfolio is null)
            {
                return 0;
            }

            return portfolio.Denars;
        }

        private void WithdrawDenars(int denars)
        {
            var portfolio = GetPortfolio(PartyBase.MainParty.LeaderHero.Clan, Settlement.CurrentSettlement);

            if (portfolio is null)
            {
                return;
            }

            denars = Math.Min((int)portfolio.Denars, denars);

            if (portfolio.Denars >= denars)
            {
                var fee = (int)Math.Round(denars * WITHDRAW_FEE);
                PartyBase.MainParty.LeaderHero.ChangeHeroGold(denars - fee);
                portfolio.Denars -= denars;
            }

            GameMenu.SwitchToMenu("town_bank_withdraw");
        }
    }

    public class Portfolio
    {
        public string ClanId { get; set; }
        public string SettlementId { get; set; }
        public float Denars { get; set; }
    }
}