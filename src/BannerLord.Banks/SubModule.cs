using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerLord.Banks
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if(game.GameType is Campaign) 
            {
                var campaignStarter = gameStarter as CampaignGameStarter;
                campaignStarter.AddBehavior(new PlayerTownVisitCampaignBehavior());
                campaignStarter.AddBehavior(new BankCampaignBehavior());
                campaignStarter.AddModel(new EnhancedClanFinanceModel());
            }
        }
    }
}