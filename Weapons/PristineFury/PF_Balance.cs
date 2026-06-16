namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal static class PF_Balance
    {
        // Left-click base damage per mark.
        // The default (Idle / no mark) value must match Item.damage in NewLegendPristineFury.SetDefaults.
        public static int GetMarkBaseDamage(PristineFuryMark mark)
        {
            return mark switch
            {
                PristineFuryMark.EvilT2            => 88,
                PristineFuryMark.SlimeGod          => 95,
                PristineFuryMark.HardMode          => 118,
                PristineFuryMark.Prime             => 120,
                PristineFuryMark.BrimstoneElemental => 175,
                PristineFuryMark.FakeCalamity      => 190,
                PristineFuryMark.Aurora            => 205,
                PristineFuryMark.Plantera          => 210,
                PristineFuryMark.Goliath           => 230,
                PristineFuryMark.Moonlord          => 270,
                PristineFuryMark.Polterghast       => 285,
                PristineFuryMark.Dog               => 330,
                PristineFuryMark.Dragon            => 360,
                PristineFuryMark.ExoTwins          => 390,
                PristineFuryMark.ExoAres           => 430,
                PristineFuryMark.Providence        => 420,
                PristineFuryMark.Ravager           => 440,
                PristineFuryMark.ExoThanatos       => 460,
                _                                  => 77,  // Idle
            };
        }
    }
}
