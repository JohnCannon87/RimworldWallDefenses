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

        private List<IntVec3> shieldedCells;

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

        private bool regionMode = false;
        private Area selectedArea = null;
        private String selectedAreaString = null;
        public static readonly SoundDef HitSoundDef = SoundDef.Named("WallShield_Hit");

        private List<Mesh> meshes = new List<Mesh>();
        private static List<Vector3> verts = new List<Vector3>();
        private static List<int> tris = new List<int>();
        private static List<Color> colors = new List<Color>();
        private Material material;
        private int renderQueue = 3650;
        private bool dirtyMesh = true;

        public override string CompInspectStringExtra()
        {
            if (regionMode)
            {
                if (selectedArea == null)
                {
                    return "No Shield Region Selected";
                }
                return "Power when active: " + PowerUsage + " W";
            }
            
            if(shieldedCells == null)
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
            if (respawningAfterLoad && selectedAreaString != null)
            {
                selectedArea = this.parent.Map.areaManager.GetLabeled(selectedAreaString);
            }
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
            Log.Error("Drawing Shield Field, regionMode: " + regionMode + ", selectedArea: " + selectedArea);
            if (regionMode && selectedArea != null)
            {
                if (dirtyMesh)
                {
                    RegenerateMesh();
                }
                if (this.parent.Map == Find.CurrentMap)
                {
                    for (int i = 0; i < meshes.Count; i++)
                    {
                        Graphics.DrawMesh(meshes[i], Vector3.zero, Quaternion.identity, material, 0);
                    }
                }                
            }
            else if(!regionMode)
            {
                DrawRectangle();
            }

        }

        private void DrawRectangle()
        {
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
                return -(float)Math.Pow(shieldedCells.Count(), WallShieldsSettings.shieldCellExponent) * wattPerCell;
            }
        }

        public Color Color => shieldColor;



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
            foreach (IntVec3 cell in shieldedCells)
            {
                BlockProjectiles(cell);
            }
        }

        private void RefreshShieldCells()
        {
            if (regionMode && selectedArea != null)
            {
                dirtyMesh = true;
                shieldedCells = selectedArea.ActiveCells.ToList();
            }
            else
            {
                RefreshShieldEdgeCells();
                RefreshShieldEdgeDrawCells();
            }
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

            shieldedCells = result;
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
                            FleckMaker.ThrowMicroSparks(projectile.ExactPosition, this.parent.Map);
                            FleckMaker.ThrowSmoke(projectile.ExactPosition, this.parent.Map, 1.5f);
                            FleckMaker.ThrowLightningGlow(projectile.ExactPosition, this.parent.Map, 1.5f);
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
            if (regionMode)
            {
                {
                    Command_Action act = new Command_Action();
                    act.action = () => this.SwapMode();
                    act.defaultLabel = "Swap To Rect Mode";
                    act.activateSound = SoundDef.Named("Click");
                    act.icon = ContentFinder<Texture2D>.Get("UI/RectangleMode", true);
                    yield return act;
                }
                {
                    Command_Action act = new Command_Action();
                    act.action = () => this.SelectRegion();
                    act.defaultLabel = "Select Region To Shield";
                    act.activateSound = SoundDef.Named("Click");
                    act.icon = ContentFinder<Texture2D>.Get("UI/SelectRegion", true);
                    yield return act;
                }
            }
            else
            {
                {
                    Command_Action act = new Command_Action();
                    act.action = () => this.SwapMode();
                    act.defaultLabel = "Swap To Region Mode";
                    act.activateSound = SoundDef.Named("Click");
                    act.icon = ContentFinder<Texture2D>.Get("UI/RegionMode", true);
                    yield return act;
                }
                {
                    Command_Action act = new Command_Action();
                    act.action = () => this.DecreaseFieldWidth();
                    act.defaultLabel = "Decrease Field Width";
                    act.activateSound = SoundDef.Named("Click");
                    act.icon = ContentFinder<Texture2D>.Get("UI/DecWidth", true);
                    yield return act;
                }
                {
                    Command_Action act = new Command_Action();
                    act.action = () => this.IncreaseFieldWidth();
                    act.defaultLabel = "Increase Field Width";
                    act.activateSound = SoundDef.Named("Click");
                    act.icon = ContentFinder<Texture2D>.Get("UI/IncWidth", true);
                    yield return act;
                }
                {
                    Command_Action act = new Command_Action();
                    act.action = () => this.DecreaseFieldHeight();
                    act.defaultLabel = "Decrease Field Height";
                    act.activateSound = SoundDef.Named("Click");
                    act.icon = ContentFinder<Texture2D>.Get("UI/DecHeight", true);
                    yield return act;
                }
                {
                    Command_Action act = new Command_Action();
                    act.action = () => this.IncreaseFieldHeight();
                    act.defaultLabel = "Increase Field Height";
                    act.activateSound = SoundDef.Named("Click");
                    act.icon = ContentFinder<Texture2D>.Get("UI/IncHeight", true);
                    yield return act;
                }
            }
        }

        private void SelectRegion()
        {
            AreaUtility.MakeAllowedAreaListFloatMenu(delegate (Area a)
            {
                selectedArea = a;
                selectedAreaString = selectedArea.Label;
                RefreshShieldCells();
            }, addNullAreaOption: false, addManageOption: true, this.parent.Map);            
        }

        private void SwapMode()
        {
            regionMode = !regionMode;
            RefreshShieldCells();
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
            Scribe_Values.Look(ref selectedAreaString, "selectedAreaString");
            Scribe_Values.Look(ref regionMode, "regionMode");            
        }

        public void RegenerateMesh()
        {
            for (int i = 0; i < meshes.Count; i++)
            {
                meshes[i].Clear();
            }
            int num = 0;
            int num2 = 0;
            if (meshes.Count < num + 1)
            {
                Mesh mesh = new Mesh();
                mesh.name = "CellBoolDrawer";
                meshes.Add(mesh);
            }
            Mesh mesh2 = meshes[num];
            CellRect cellRect = new CellRect(0, 0, this.parent.Map.Size.x, this.parent.Map.Size.z);
            float y = AltitudeLayer.MapDataOverlay.AltitudeFor();
            bool careAboutVertexColors = false;
            for (int j = cellRect.minX; j <= cellRect.maxX; j++)
            {
                for (int k = cellRect.minZ; k <= cellRect.maxZ; k++)
                {
                    int arg = CellIndicesUtility.CellToIndex(j, k, this.parent.Map.Size.x);
                    if (!GetCellBool(arg))
                    {
                        continue;
                    }
                    verts.Add(new Vector3(j, y, k));
                    verts.Add(new Vector3(j, y, k + 1));
                    verts.Add(new Vector3(j + 1, y, k + 1));
                    verts.Add(new Vector3(j + 1, y, k));
                    Color color = GetCellExtraColor(arg);
                    colors.Add(color);
                    colors.Add(color);
                    colors.Add(color);
                    colors.Add(color);
                    if (color != Color.white)
                    {
                        careAboutVertexColors = true;
                    }
                    int count = verts.Count;
                    tris.Add(count - 4);
                    tris.Add(count - 3);
                    tris.Add(count - 2);
                    tris.Add(count - 4);
                    tris.Add(count - 2);
                    tris.Add(count - 1);
                    num2++;
                    if (num2 >= 16383)
                    {
                        FinalizeWorkingDataIntoMesh(mesh2);
                        num++;
                        if (meshes.Count < num + 1)
                        {
                            Mesh mesh3 = new Mesh();
                            mesh3.name = "CellBoolDrawer";
                            meshes.Add(mesh3);
                        }
                        mesh2 = meshes[num];
                        num2 = 0;
                    }
                }
            }
            FinalizeWorkingDataIntoMesh(mesh2);
            CreateMaterialIfNeeded(careAboutVertexColors);
            dirtyMesh = false;
        }

        private void FinalizeWorkingDataIntoMesh(Mesh mesh)
        {
            if (verts.Count > 0)
            {
                mesh.SetVertices(verts);
                verts.Clear();
                mesh.SetTriangles(tris, 0);
                tris.Clear();
                mesh.SetColors(colors);
                colors.Clear();
            }
        }

        private void CreateMaterialIfNeeded(bool careAboutVertexColors)
        {
            if (material == null)
            {
                Color color = Color;
                material = SolidColorMaterials.NewSolidColorMaterial(shieldColor, ShaderDatabase.MetaOverlay);
                material.renderQueue = renderQueue;
            }
        }

        public bool GetCellBool(int index)
        {
            return ((ICellBoolGiver)selectedArea).GetCellBool(index);
        }

        public Color GetCellExtraColor(int index)
        {
            return ((ICellBoolGiver)selectedArea).GetCellExtraColor(index);
        }
    }
}
