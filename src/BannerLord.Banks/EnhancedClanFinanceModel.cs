using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace BannerLord.Banks
{
    public class EnhancedClanFinanceModel : DefaultClanFinanceModel
    {
        public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            var baseNumber = base.CalculateClanIncome(clan, includeDescriptions, applyWithdrawals);
            AddBankIncome(clan, ref baseNumber);

            return baseNumber;
        }

        public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            var baseNumber = base.CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals);
            AddBankIncome(clan, ref baseNumber);
            return baseNumber;
        }

        private void AddBankIncome(Clan clan, ref ExplainedNumber goldChange)
        {
            var bankBehavior = Campaign.Current.GetCampaignBehavior<BankCampaignBehavior>();

            bankBehavior.CalculateClanBankInterest(clan, ref goldChange);
        }
    }
}