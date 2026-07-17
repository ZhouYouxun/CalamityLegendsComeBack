using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Systems
{
    /// <summary>
    /// 在本地玩家进入任意世界五秒后显示欢迎消息。
    /// </summary>
    internal sealed class WelcomeMessagePlayer : ModPlayer
    {
        private const int WelcomeMessageDelay = 60 * 5;

        private int welcomeMessageTimer = -1;

        private static LocalizedText WelcomeMessage =>
            Language.GetText("Mods.CalamityLegendsComeBack.TheSpecialText.WelcomeMessage");

        public override void OnEnterWorld()
        {
            welcomeMessageTimer = 0;
        }

        public override void PostUpdate()
        {
            // 聊天提示使用本地客户端的语言，不能在服务端或其他玩家的更新中发送。
            if (Player.whoAmI != Main.myPlayer || welcomeMessageTimer < 0)
                return;

            if (++welcomeMessageTimer < WelcomeMessageDelay)
                return;

            Main.NewText(WelcomeMessage.Value, Color.Coral);
            welcomeMessageTimer = -1;
        }
    }
}
