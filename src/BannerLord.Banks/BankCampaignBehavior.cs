using System;
using System.Collections.Generic;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace BannerLord.Banks
{
    public class BankCampaignBehavior : CampaignBehaviorBase
    {
        private const string BANK_INFO_TEXT_VARIABLE = "BANK_INFO";
        private const string BANK_INFO_FLAVOR_TEXT_VARIABLE = "BANK_INFO_FLAVOR";
        private const string BANK_INFO_APY_TEXT_VARIABLE = "BANK_INFO_APY";
        private const string BANK_INFO_BALANCE_TEXT_VARIABLE = "BANK_INFO_BALANCE";
        private const string BANK_INFO_WITHDRAW_TEXT_VARIABLE = "BANK_INFO_WITHDRAW";

        private const string PORTFOLIO_DATA_KEY = "BannerLord.Banks.HeroPortfolios";
        private Dictionary<string, HeroPortfolio> _heroPortfolios;

        public float BASE_APY = MCMUISettings.Instance.TradeXP;
        public int DAYS_IN_A_YEAR = MCMUISettings.Instance.DaysPerYear;
        public int PROSPEROUS_TOWN = MCMUISettings.Instance.ProsperousTown;
        public float WITHDRAW_FEE = MCMUISettings.Instance.WithdrawFee;
        public float TRADE_SKILL_PROFIT_MULTIPLIER = MCMUISettings.Instance.TradeXP;

        public void UpdateVariables()
        {
            BASE_APY = MCMUISettings.Instance.TradeXP;
            DAYS_IN_A_YEAR = MCMUISettings.Instance.DaysPerYear;
            PROSPEROUS_TOWN = MCMUISettings.Instance.ProsperousTown;
            WITHDRAW_FEE = MCMUISettings.Instance.WithdrawFee;
            TRADE_SKILL_PROFIT_MULTIPLIER = MCMUISettings.Instance.TradeXP;
        }

        public BankCampaignBehavior()
        {
            _heroPortfolios = new Dictionary<string, HeroPortfolio>();
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
            var json = string.Empty;

            if (dataStore.IsLoading)
            {
                dataStore.SyncData(PORTFOLIO_DATA_KEY, ref json);
                _heroPortfolios = JsonConvert.DeserializeObject<Dictionary<string, HeroPortfolio>>(json);
            }

            if (dataStore.IsSaving)
            {
                json = JsonConvert.SerializeObject(_heroPortfolios);
                dataStore.SyncData(PORTFOLIO_DATA_KEY, ref json);
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

        private void HandleFlavorText()
        {
            var heroId = PartyBase.MainParty.LeaderHero.Id.ToString();
            var settlementId = Settlement.CurrentSettlement.Id.ToString();

            var hasPortfolio = _heroPortfolios.ContainsKey(heroId);
            var hasPortfolioInSettlement = hasPortfolio && _heroPortfolios[heroId].PortFoliosBySettlementId.ContainsKey(settlementId);

            if (!hasPortfolioInSettlement)
            {
                MBTextManager.SetTextVariable(
                    BANK_INFO_FLAVOR_TEXT_VARIABLE,
                    new TextObject("After waiting an hour you are finally led to the moneylenders office.")
                );
            }
            else
            {
                var portfolioDenars = _heroPortfolios[heroId].PortFoliosBySettlementId[settlementId];

                if (portfolioDenars < 10000)
                {
                    MBTextManager.SetTextVariable(
                        BANK_INFO_FLAVOR_TEXT_VARIABLE,
                        new TextObject("After waiting a few minutes you are led the moneylenders office.")
                    );
                }
                else if (portfolioDenars < 100000)
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
            var heroId = PartyBase.MainParty.LeaderHero.Id.ToString();
            var settlementId = Settlement.CurrentSettlement.Id.ToString();

            var hasPortfolio = _heroPortfolios.ContainsKey(heroId);
            var hasPortfolioInSettlement = hasPortfolio && _heroPortfolios[heroId].PortFoliosBySettlementId.ContainsKey(settlementId);

            if (!hasPortfolioInSettlement)
            {
                MBTextManager.SetTextVariable(
                    BANK_INFO_BALANCE_TEXT_VARIABLE,
                    new TextObject("You do not have an account at this bank")
                );
            }
            else
            {
                var portfolioDenars = _heroPortfolios[heroId].PortFoliosBySettlementId[settlementId];

                MBTextManager.SetTextVariable(
                    BANK_INFO_BALANCE_TEXT_VARIABLE,
                    new TextObject($"Account Balance:\n{portfolioDenars:N0} denars")
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
            var heroId = PartyBase.MainParty.LeaderHero.Id.ToString();
            var settlementId = Settlement.CurrentSettlement.Id.ToString();

            var hasPortfolio = _heroPortfolios.ContainsKey(heroId);
            var hasPortfolioInSettlement = hasPortfolio && _heroPortfolios[heroId].PortFoliosBySettlementId.ContainsKey(settlementId);

            // just in case the UI didn't detect it
            if (PartyBase.MainParty.LeaderHero.Gold >= denars)
            {
                if (!hasPortfolio)
                {
                    _heroPortfolios.Add(heroId, new HeroPortfolio());
                }

                if (!hasPortfolioInSettlement)
                {
                    _heroPortfolios[heroId].PortFoliosBySettlementId.Add(settlementId, 0);
                }

                PartyBase.MainParty.LeaderHero.ChangeHeroGold(-denars);
                _heroPortfolios[heroId].PortFoliosBySettlementId[settlementId] += denars;
            }

            GameMenu.SwitchToMenu("town_bank_deposit");
        }

        private void HandleDailyTickClanEvent(Clan clan)
        {
            UpdateVariables();

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

            var heroId = clan.Leader.Id.ToString();
            var hasPortfolio = _heroPortfolios.ContainsKey(heroId);

            if (!hasPortfolio)
            {
                return;
            }

            foreach (var townPortfolio in _heroPortfolios[heroId].PortFoliosBySettlementId)
            {
                if (townPortfolio.Value > 0)
                {
                    var settlement = Settlement.FindFirst(s => s.Id.ToString().Equals(townPortfolio.Key, StringComparison.OrdinalIgnoreCase));

                    if (settlement != null)
                    {
                        var dpy = CalculateDailyInterest(settlement);
                        var interest = (int)Math.Round(dpy * townPortfolio.Value);
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
            var hasEnoughMoney = GetAccountBalance(Settlement.CurrentSettlement) >= 1000;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars in account"));
        }

        private bool HandleTownBankWithdrawDenarMenuOptions_10000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            var hasEnoughMoney = GetAccountBalance(Settlement.CurrentSettlement) >= 10000;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars in account"));
        }

        private bool HandleTownBankWithdrawDenarMenuOptions_100000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            var hasEnoughMoney = GetAccountBalance(Settlement.CurrentSettlement) >= 100000;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars in account"));
        }

        private bool HandleTownBankWithdrawDenarMenuOptions_1000000(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            var hasEnoughMoney = GetAccountBalance(Settlement.CurrentSettlement) >= 1000000;
            return MenuHelper.SetOptionProperties(args, hasEnoughMoney, !hasEnoughMoney, new TextObject("Insufficient denars in account"));
        }

        private float GetAccountBalance(Settlement settlement)
        {
            var heroId = PartyBase.MainParty.LeaderHero.Id.ToString();
            var settlementId = settlement.Id.ToString();

            var hasPortfolio = _heroPortfolios.ContainsKey(heroId);
            var hasPortfolioInSettlement = hasPortfolio && _heroPortfolios[heroId].PortFoliosBySettlementId.ContainsKey(settlementId);

            if (!hasPortfolioInSettlement)
            {
                return 0;
            }

            return _heroPortfolios[heroId].PortFoliosBySettlementId[settlementId];
        }

        private void WithdrawDenars(int denars)
        {
            var heroId = PartyBase.MainParty.LeaderHero.Id.ToString();
            var settlementId = Settlement.CurrentSettlement.Id.ToString();

            var hasPortfolio = _heroPortfolios.ContainsKey(heroId);
            var hasPortfolioInSettlement = hasPortfolio && _heroPortfolios[heroId].PortFoliosBySettlementId.ContainsKey(settlementId);

            if (!hasPortfolioInSettlement)
            {
                return;
            }

            if (_heroPortfolios[heroId].PortFoliosBySettlementId[settlementId] >= denars)
            {
                var fee = (int)Math.Round(denars * WITHDRAW_FEE);
                PartyBase.MainParty.LeaderHero.ChangeHeroGold(denars - fee);
                _heroPortfolios[heroId].PortFoliosBySettlementId[settlementId] -= denars;
            }

            GameMenu.SwitchToMenu("town_bank_withdraw");
        }
    }

    public class HeroPortfolio
    {
        public Dictionary<string, float> PortFoliosBySettlementId { get; set; }

        public HeroPortfolio()
        {
            PortFoliosBySettlementId = new Dictionary<string, float>();
        }
    }
}