using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace WallShields
{
    class AATurretComp : ThingComp
    {
        protected AATurretTop top;
        int range = 0;
        int tickCount = 0;
        int ammoRemaining = 0;

        public static readonly SoundDef HitSoundDef = SoundDef.Named("AAGun_Fire");

        public CompProperties_AATurretComp AATurretComps
        {
            get
            {
                return (CompProperties_AATurretComp)this.props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (top != null)
            {
                top.DrawTurret();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            range = WallShieldsSettings.protectionRange;
            ammoRemaining = WallShieldsSettings.ammoCount;
            top = new AATurretTop(this);
        }

        public override void CompTick()
        {
            tickCount++;
            if (!IsActive())
            {
                top.TurretTopTick();
                return;
            }

            this.ShootThings();
            if(tickCount >= WallShieldsSettings.reloadSpeed)
            {
                tickCount = 0;
                Reload();
            }
            top.TurretTopTick();
        }

        private void ShootThings()
        {            
            if (ammoRemaining <= 0)
            {
                return;
            }

            List<Thing> targets = GetTargets();

            List<Thing> targetsInRange = targets.Where<Thing>(t => t.Position.InHorDistOf(this.parent.Position, this.range)).ToList();

            List<Thing> targetsToShootAt = targetsInRange.Shuffle().Take(Math.Max(ammoRemaining, targetsInRange.Count())).Where(t => IsThingAThreateningDropPodOrNotADropPod(t)).ToList();

            foreach (Thing thing in targetsToShootAt)
            {
                if (IsThingNotADropPod(thing) || ShouldDestroyDropPod())
                {
                    DestroyThing(thing);
                }
                else
                {
                    DamageDropPod((DropPodIncoming)thing);
                }
            }

            ammoRemaining = Math.Max(ammoRemaining - targetsToShootAt.Count(), 0);
            tickCount = 0;
        }

        private bool IsThingAThreateningDropPodOrNotADropPod(Thing thing)
        {
            return !(thing is DropPodIncoming) || (thing is DropPodIncoming) && IsPodAThreat((DropPodIncoming)thing);
        }
        private bool ShouldDestroyDropPod()
        {
            return Rand.RangeInclusive(0, 100) <= WallShieldsSettings.chanceOfCompletelyDestroyingDropPod;
        }

        private bool IsThingNotADropPod(Thing thing)
        {
            return !(thing is DropPodIncoming);
        }

        private void DestroyThing(Thing thing)
        {
            MakeShrapnelPlaySoundAndAimAtTarget(thing, 3);
            if (!thing.Destroyed)
            {
                thing.Destroy(DestroyMode.Vanish);
            }            
        }

        private void DamageDropPod(DropPodIncoming pod)
        {
            if (IsPodAThreat(pod))
            {
                MakeShrapnelPlaySoundAndAimAtTarget(pod, 1);
                int i = 0;
                foreach (Thing occupant in pod.Contents.innerContainer)
                {
                    InjureOccupant(occupant);
                }
            }
        }

        private static bool IsPodAThreat(DropPodIncoming pod)
        {
            return pod.Contents.innerContainer.Any((Thing t) => t.Faction.HostileTo(Faction.OfPlayer));
        }

        private void MakeShrapnelPlaySoundAndAimAtTarget(Thing target, int shrapnelCount)
        {
            SkyfallerShrapnelUtility.MakeShrapnel(target.Position, target.Map, Rand.RangeInclusive(0, 360), target.def.skyfaller.shrapnelDistanceFactor, shrapnelCount, 0, spawnMotes: true);
            HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(this.parent.Position, this.parent.Map, false));
            top.CurRotation = (target.Position.ToVector3Shifted() - this.parent.DrawPos).AngleFlat();
            top.ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
        }

        private List<Thing> GetTargets()
        {
            List<Thing> result = new List<Thing>();

            result.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming));
            result.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.ShipChunkIncoming));
            result.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.CrashedShipPartIncoming));
            result.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.DefoliatorShipPart));
            result.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.PsychicDronerShipPart));

            return result;
        }

        private void InjureOccupant(Thing t)
        {
            if (t != null && !t.Destroyed && t.Faction.HostileTo(Faction.OfPlayer))
            {
                for (int i = 0; i < Rand.RangeInclusive(1, WallShieldsSettings.maxShotsAtDropPodOccupant); i++)
                {
                    if (!t.Destroyed)
                    {
                        t.TakeDamage(new DamageInfo(DamageDefOf.Bullet, WallShieldsSettings.bulletDamage));
                    }
                }
            }
        }

        private void Reload()
        {
            ammoRemaining = WallShieldsSettings.ammoCount;
        }

        private bool PowerOn
        {
            get
            {
                CompPowerTrader comp = this.parent.GetComp<CompPowerTrader>();
                return comp != null && comp.PowerOn;
            }
        }

        private bool IsActive()
        {
            bool isActive = this.parent.Spawned && this.PowerOn;
            return isActive;
        }
    }
}
