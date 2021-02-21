using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace WallShields
{
    class WallMountedShieldEmitterComp : ThingComp
    {

        private List<IntVec3> shieldEdgeCells;

        private IntVec3 topCenter;
        private IntVec3 bottomCenter;
        private IntVec3 leftCenter;
        private IntVec3 rightCenter;

        private IntVec3 topLeft;
        private IntVec3 topRight;
        private IntVec3 bottomLeft;
        private IntVec3 bottomRight;

        private int fieldWidth = 8;
        private int fieldHeight = 8;
        private Mesh cubeMesh;
        private Mesh topLeftMesh;
        private Mesh topRightMesh;
        private Mesh bottomLeftMesh;
        private Mesh bottomRightMesh;
        private Color shieldColor = new Color(0, 0.5f, 0.5f, 0.35f);

        private readonly float shieldSize = 0.5f; // Actually The Size of the shields individual cell;

        private readonly int wattPerCell = WallShieldsSettings.shieldPowerPerCell;

        public static readonly SoundDef HitSoundDef = SoundDef.Named("WallShield_Hit");


        public override string CompInspectStringExtra()
        {
            if(shieldEdgeCells == null)
            {
                return "Shield Inactive";
            }
            return "Shield Width: " + fieldWidth + 
                "\n" + 
                "Shield Height: " + fieldHeight +
                "\n" +
                "Power when active: " + PowerUsage + " W";
        }

        public override void PostDraw()
        {
            GenerateMeshs();
            DrawShieldField();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            RefreshShieldCells(); 
        }

        private void GenerateMeshs()
        {
            cubeMesh = GraphicsUtil.CreateCuboidMesh();
            topLeftMesh = GraphicsUtil.CreateTopLeftRoundedCuboidMesh();
            topRightMesh = GraphicsUtil.CreateTopRightRoundedCuboidMesh();
            bottomLeftMesh = GraphicsUtil.CreateBottomLeftRoundedCuboidMesh();
            bottomRightMesh = GraphicsUtil.CreateBottomRightRoundedCuboidMesh();
        }

        public override void CompTick()
        {
            if (!IsActive())
            {
                SetPowerLevel(0);
                return;
            }
            ShieldThings();
            SetPowerLevel();
        }

        private void DrawShieldField()
        {
            if (!IsActive())
            {
                return;
            }

            //Draw Top
            DrawShieldAtPointWithCubeMesh(topCenter, fieldWidth - shieldSize, shieldSize, shieldColor, ShaderDatabase.MetaOverlay);

            //Draw Bottom
            DrawShieldAtPointWithCubeMesh(bottomCenter, fieldWidth - shieldSize, shieldSize, shieldColor, ShaderDatabase.MetaOverlay);

            //Draw Left
            DrawShieldAtPointWithCubeMesh(leftCenter, shieldSize, fieldHeight - shieldSize, shieldColor, ShaderDatabase.MetaOverlay);

            //Draw Right
            DrawShieldAtPointWithCubeMesh(rightCenter, shieldSize, fieldHeight - shieldSize, shieldColor, ShaderDatabase.MetaOverlay);

            //DrawTopLeft
            DrawOneCellShieldAtPointWithMesh(topLeft, topLeftMesh);

            //DrawTopRight
            DrawOneCellShieldAtPointWithMesh(topRight, topRightMesh);

            //DrawBottomLeft
            DrawOneCellShieldAtPointWithMesh(bottomLeft, bottomLeftMesh);

            //DrawBottomRight
            DrawOneCellShieldAtPointWithMesh(bottomRight, bottomRightMesh);
        }

        private void DrawOneCellShieldAtPointWithMesh(IntVec3 point, Mesh thisMesh)
        {
            Vector3 centre = point.ToVector3() + (new Vector3(0.5f, 0f, 0.5f));
            Vector3 scale = new Vector3(shieldSize, 1f, shieldSize);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(centre, Quaternion.identity, scale);
                   
            Material currentMatrialColour = SolidColorMaterials.NewSolidColorMaterial(shieldColor, ShaderDatabase.MetaOverlay);
            Graphics.DrawMesh(thisMesh, matrix, currentMatrialColour, 0);
        }

        private void DrawShieldAtPointWithCubeMesh(IntVec3 point, float width, float height, Color color, Shader shader)
        {
            Vector3 centre = point.ToVector3() + (new Vector3(0.5f, 0f, 0.5f));
            Vector3 scale = new Vector3(width, 1f, height);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(centre, Quaternion.identity, scale);
            Material currentMatrialColour = SolidColorMaterials.NewSolidColorMaterial(color, shader);
            Graphics.DrawMesh(cubeMesh, matrix, currentMatrialColour, 0);
        }

        private bool PowerOn
        {
            get
            {
                CompPowerTrader comp = this.parent.GetComp<CompPowerTrader>();
                return comp != null && comp.PowerOn;
            }
        }

        private float PowerUsage
        {
            get
            {
                return -(float)Math.Pow(shieldEdgeCells.Count(), WallShieldsSettings.shieldCellExponent) * wattPerCell;
            }
        }


        private void SetPowerLevel()
        {
            SetPowerLevel(PowerUsage);
        }

        private void SetPowerLevel(float powerLevel)
        {
            CompPowerTrader comp = this.parent.GetComp<CompPowerTrader>();
            comp.PowerOutput = powerLevel;
        }

        private bool IsActive()
        {
            bool isActive = this.parent.Spawned && this.PowerOn && IsThereAThreat();
            return isActive;
        }

        private bool IsThereAThreat()
        {
            if (GenHostility.AnyHostileActiveThreatToPlayer(parent.Map, true))
            {
                return true;
            }
            return false;
        }

        private void ShieldThings()
        {
            foreach (IntVec3 cell in shieldEdgeCells)
            {
                BlockProjectiles(cell);
            }
        }

        private void RefreshShieldCells()
        {
            RefreshShieldEdgeCells();
            RefreshShieldEdgeDrawCells();
        }

        private void RefreshShieldEdgeDrawCells()
        {
            IntVec3 center = this.parent.Position;

            topCenter = new IntVec3(0, 0, fieldHeight) + center;
            bottomCenter = new IntVec3(0, 0, -fieldHeight) + center;
            leftCenter =  new IntVec3(-fieldWidth, 0, 0) + center;
            rightCenter = new IntVec3(fieldWidth, 0, 0) + center;


            bottomLeft = new IntVec3(-fieldWidth, 0, -fieldHeight) + center;
            topLeft = new IntVec3(-fieldWidth, 0, fieldHeight) + center;
            bottomRight = new IntVec3(fieldWidth, 0, -fieldHeight) + center;
            topRight = new IntVec3(fieldWidth, 0, fieldHeight) + center;
        }

        private void RefreshShieldEdgeCells()
        {
            IntVec3 center = this.parent.Position;
            int edgeWidth = fieldWidth + 1;
            int edgeHeight = fieldHeight + 1;

            List<IntVec3> result = new List<IntVec3>();

            //Get Cells Along Top
            for (int i = -edgeWidth; i <= edgeWidth; i++)
            {
                IntVec3 cell = new IntVec3(i, 0, edgeHeight) + center;
                result.Add(cell);
            }
            //Get Cells Along Bottom
            for (int i = -edgeWidth; i <= edgeWidth; i++)
            {
                IntVec3 cell = new IntVec3(i, 0, -edgeHeight) + center;
                result.Add(cell);
            }
            //Get Cells Along Left
            for (int i = -edgeHeight + 1; i <= edgeHeight - 1; i++)
            {
                IntVec3 cell = new IntVec3(-edgeWidth, 0, i) + center;
                result.Add(cell);
            }
            //Get Cells Along Right
            for (int i = -edgeHeight + 1; i <= edgeHeight - 1; i++)
            {
                IntVec3 cell = new IntVec3(edgeWidth, 0, i) + center;
                result.Add(cell);
            }

            shieldEdgeCells = result;
        }

        private void BlockProjectiles(IntVec3 cell)
        {
            //Ignore cells outside the map
            if (!cell.InBounds(this.parent.Map))
            {
                return;
            }
            List<Thing> things = this.parent.Map.thingGrid.ThingsListAt(cell);
            for (int i = 0, l = things.Count(); i < l; i++)
            {
                Thing thing = things[i];
                if (thing != null && thing is Projectile)
                {
                    Projectile projectile = (Projectile)thing;
                    if (!projectile.Destroyed)
                    {
                        bool wantToIntercept = !WasProjectileFiredByAlly(projectile);

                        if (wantToIntercept)
                        {
                            if (!projectile.Destroyed)
                            {
                                projectile.Destroy();
                            }

                            MoteMaker.ThrowMicroSparks(projectile.ExactPosition, this.parent.Map);
                            MoteMaker.ThrowSmoke(projectile.ExactPosition, this.parent.Map, 1.5f);
                            MoteMaker.ThrowLightningGlow(projectile.ExactPosition, this.parent.Map, 1.5f);
                            HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(this.parent.Position, this.parent.Map, false));

                        }
                    }
                }
            }
        }

        private bool WasProjectileFiredByAlly(Projectile projectile)
        {
            Thing launcher = GetInstanceField(typeof(Projectile), projectile, "launcher") as Thing;

            if (launcher != null)
            {
                if (launcher.Faction != null)
                {
                    if (launcher.Faction.IsPlayer)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            {
                Command_Action act = new Command_Action();
                act.action = () => this.IncreaseFieldWidth();
                act.defaultLabel = "Increase Field Width";
                act.activateSound = SoundDef.Named("Click");
                act.icon = ContentFinder<Texture2D>.Get("UI/IncField", true);
                yield return act;
            }
            {
                Command_Action act = new Command_Action();
                act.action = () => this.DecreaseFieldWidth();
                act.defaultLabel = "Decrease Field Width";
                act.activateSound = SoundDef.Named("Click");
                act.icon = ContentFinder<Texture2D>.Get("UI/DecField", true);
                yield return act;
            }
            {
                Command_Action act = new Command_Action();
                act.action = () => this.IncreaseFieldHeight();
                act.defaultLabel = "Increase Field Height";
                act.activateSound = SoundDef.Named("Click");
                act.icon = ContentFinder<Texture2D>.Get("UI/IncFieldDay", true);
                yield return act;
            }
            {
                Command_Action act = new Command_Action();
                act.action = () => this.DecreaseFieldHeight();
                act.defaultLabel = "Decrease Field Height";
                act.activateSound = SoundDef.Named("Click");
                act.icon = ContentFinder<Texture2D>.Get("UI/DecFieldDay", true);
                yield return act;
            }

        }

        private void IncreaseFieldWidth()
        {
            fieldWidth++;
            RefreshShieldCells();
        }

        private void DecreaseFieldWidth()
        {
            fieldWidth--;
            RefreshShieldCells();
        }

        private void IncreaseFieldHeight()
        {
            fieldHeight++;
            RefreshShieldCells();
        }

        private void DecreaseFieldHeight()
        {
            fieldHeight--;
            RefreshShieldCells();
        }

        public static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref fieldWidth, "fieldWidth");
            Scribe_Values.Look(ref fieldHeight, "fieldHeight");
        }
    }
}
