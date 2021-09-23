using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class TradebotSettings
    {
        private const string TradeBot = nameof(TradeBot);
        public override string ToString() => "Trade Bot Settings";

 
        [Category(TradeBot), Description("The channel(s) the bot will be accepting commands in separated by a comma, no spaces at all.")]
        public string tradebotchannel { get; set; } = string.Empty;

        [Category(TradeBot), Description("The name of your discord trade bot channel")]
        public string channelname { get; set; } = string.Empty;

        [Category(TradeBot), Description("MGDB folder path")]
        public string mgdbpath { get; set; } = string.Empty;


    }
}
