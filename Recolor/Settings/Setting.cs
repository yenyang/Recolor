using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;

namespace Recolor.Settings
{
    [FileLocation(nameof(Recolor))]
    public class Setting : ModSetting
    {
        public Setting(IMod mod) : base(mod)
        {
        }

        public override void SetDefaults()
        {
        }
    }
}
