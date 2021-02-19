using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace WallShields
{
    class WallShieldsSettings : ModSettings
    {
        internal static int shieldPowerPerCell = 1;
        internal static int shieldCellExponent = 2;
        internal static int protectionRange = 55;
        internal static int reloadSpeed = 120;
        internal static int ammoCount = 2;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref shieldPowerPerCell, "shieldPowerPerCell");
            Scribe_Values.Look(ref shieldCellExponent, "shieldCellExponent");

            Scribe_Values.Look(ref protectionRange, "protectionRange");
            Scribe_Values.Look(ref reloadSpeed, "reloadSpeed");
            Scribe_Values.Look(ref ammoCount, "ammoCount");

        }
    }
}
