using System.ComponentModel;

namespace SysBot.Pokemon
{
    public class TradebotSettings
    {
        private const string TradeBot = nameof(TradeBot);
        public override string ToString() => "Trade Bot Settings";

 
        [Category(TradeBot), Description("The channel(s) the bot will be accepting commands in separated by a comma, no spaces at all.")]
        public string tradebotchannel { get; set; } = string.Empty;
        
    }
}
