using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace WallShields
{
    class WallShieldsMod : Mod
    {
        private Vector2 scrollPosition = Vector2.zero;

        public WallShieldsMod(ModContentPack content) : base(content)
        {
            LongEventHandler.ExecuteWhenFinished(GetSettings);
        }

        public void GetSettings()
        {
            GetSettings<WallShieldsSettings>();
        }

        public static IEnumerable<ThingDef> PossibleThingDefs()
        {
            return from d in DefDatabase<ThingDef>.AllDefs
                   where (d.category == ThingCategory.Item && d.scatterableOnMapGen && !d.destroyOnDrop && !d.MadeFromStuff)
                   select d;
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard list = new Listing_Standard()
            {
                ColumnWidth = rect.width
            };

            list.Begin(rect);

            list.Label("Shield - Power Per Cell Covered: " + WallShieldsSettings.shieldPowerPerCell);
            WallShieldsSettings.shieldPowerPerCell = list.Slider(WallShieldsSettings.shieldPowerPerCell, 0, 100).RoundToAsInt(1);

            list.Label("Shield - Cell Exponent Multiplication Value: " + WallShieldsSettings.shieldCellExponent);
            WallShieldsSettings.shieldCellExponent = list.Slider(WallShieldsSettings.shieldCellExponent, 1, 20).RoundToAsInt(1);

            list.Label("Shield total power calculation is ('Cells Covered' ^ 'Cell Exponent') * power per cell");

            list.Label("AA Turret - Protection Range Value: " + WallShieldsSettings.protectionRange);
            WallShieldsSettings.protectionRange = list.Slider(WallShieldsSettings.protectionRange, 1, 100).RoundToAsInt(1);

            list.Label("AA Turret - Reload Speed (in ticks) Value: " + WallShieldsSettings.reloadSpeed);
            WallShieldsSettings.reloadSpeed = list.Slider(WallShieldsSettings.reloadSpeed, 1, 240).RoundToAsInt(1);

            list.Label("AA Turret - Ammo Count Value: " + WallShieldsSettings.ammoCount);
            WallShieldsSettings.ammoCount = list.Slider(WallShieldsSettings.ammoCount, 1, 20).RoundToAsInt(1);

            list.End();
        }

        public override string SettingsCategory()
        {
            return "Wall Defenses";
        }
    }
}
