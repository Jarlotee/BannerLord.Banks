using System;
using System.Collections;
using System.Collections.Generic;
using MCM;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Settings.Base;
using MCM.Abstractions.Settings.Base.Global;

namespace BannerLord.Banks
{
    internal sealed class MCMUISettings : AttributeGlobalSettings<MCMUISettings> // AttributePerSaveSettings<MCMUISettings>
    {
        public override string Id => "BankMod";
        public override string DisplayName => $"Bank Settings";
        public override string FolderName => "BankMod";
        public override string FormatType => "json2";

        [SettingPropertyFloatingInteger("Interest amount", 0f, 5f, "0%", Order = 0, RequireRestart = false, HintText = "Interest per year.\nCalculation: (this value / Days in a year) * (town prosp. / prosp. town setting) = interest per day.")]
        [SettingPropertyGroup("General")]
        public float Interest { get; set; } = 0.6f;

        [SettingPropertyInteger("Days in a year", 1, 240, "0 Days", Order = 1, RequireRestart = false, HintText = "How many days does a year have.\nLower values -> more interest per day.")]
        [SettingPropertyGroup("General")]
        public int DaysPerYear { get; set; } = 120;

        [SettingPropertyInteger("Prosperous Town", 1, 12000, "0 Prosperity", Order = 2, RequireRestart = false, HintText = "How much prosperity is needed to be counted Properous. Affects scaling of interest.\nIf town prosp. above this setting -> more interest per day.")]
        [SettingPropertyGroup("General")]
        public int ProsperousTown { get; set; } = 6000;

        [SettingPropertyFloatingInteger("Withdraw fee", 0f, 2f, "0%", Order = 3, RequireRestart = false, HintText = "Withdraw fee.\nCalculation: Denars to withdraw * this value = fee.")]
        [SettingPropertyGroup("General")]
        public float WithdrawFee { get; set; } = 0.1f;

        [SettingPropertyFloatingInteger("Trade Skill XP amount", 0f, 2f, "0%", Order = 4, RequireRestart = false, HintText = "You gain x% xp to trade skill.")]
        [SettingPropertyGroup("General")]
        public float TradeXP { get; set; } = 0.1f;
    }
}