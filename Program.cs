using System;
using System.Collections.Generic;
using SharpDX;
using WeScriptWrapper;
using WeScript.SDK.UI;
using WeScript.SDK.UI.Components;


namespace Pacify
{
    public class Program
    {
        public static float M_PI_F = (180.0f / Convert.ToSingle(System.Math.PI));
        public static IntPtr processHandle = IntPtr.Zero; //processHandle variable used by OpenProcess (once)
        public static bool gameProcessExists = false; //avoid drawing if the game process is dead, or not existent
        public static bool isWow64Process = true; //we all know the game is 32bit, but anyway...
        public static bool isGameOnTop = false; //we should avoid drawing while the game is not set on top
        public static bool isOverlayOnTop = false; //we might allow drawing visuals, while the user is working with the "menu"
        public static uint PROCESS_ALL_ACCESS = 0x1FFFFF; //hardcoded access right to OpenProcess (even EAC strips some of the access flags)
        public static Vector2 wndMargins = new Vector2(0, 0); //if the game window is smaller than your desktop resolution, you should avoid drawing outside of it
        public static Vector2 wndSize = new Vector2(0, 0); //get the size of the game window ... to know where to draw
        public static IntPtr GameBase = IntPtr.Zero;
        public static IntPtr GameSize = IntPtr.Zero;
        public static DateTime LastSpacePressedDT = DateTime.Now;
        public static IntPtr GWorldPtr = IntPtr.Zero;
        public static IntPtr GWorldPtr2 = IntPtr.Zero;
        public static IntPtr GNamesPtr = IntPtr.Zero;
        public static Vector2 GameCenterPos = new Vector2(0, 0);
        public static Vector2 GameCenterPos2 = new Vector2(0, 0);
        public static Vector3 FMinimalViewInfo_Location = new Vector3(0, 0, 0);
        public static Vector3 FMinimalViewInfo_Rotation = new Vector3(0, 0, 0);
        public static float FMinimalViewInfo_FOV = 0;
        public static Dictionary<UInt32, string> CachedID = new Dictionary<UInt32, string>();
        public static IntPtr FindWindow = IntPtr.Zero;

        
        public static uint survivorID = 0;
        public static uint killerID = 0;
        public static uint woodID = 0;
        public static uint keyEntranceID = 0;
        public static uint keyMainID = 0;
        public static uint keyAtticID = 0;
        public static uint keyBasementID = 0;
        public static uint keyBedroomID = 0;
        public static uint happydollID = 0;
        public static uint runningdollID = 0;
        public static uint matchesID = 0;
        public static uint PacifyDollID = 0;
        public static Vector2 AimTarg2D = new Vector2(0, 0); //for aimbot
        public static Vector3 AimTarg3D = new Vector3(0, 0, 0);



        public static Menu RootMenu { get; private set; }
        public static Menu VisualsMenu { get; private set; }
        public static Menu MiscMenu { get; private set; }

        

        class Components
        {

            public static readonly MenuKeyBind MainAssemblyToggle = new MenuKeyBind("mainassemblytoggle", "Toggle the whole assembly effect by pressing key:", VirtualKeyCode.Delete, KeybindType.Toggle, true);
            public static class VisualsComponent
            {
                public static readonly MenuBool DrawTheVisuals = new MenuBool("drawthevisuals", "Enable all of the Visuals", true);
                public static readonly MenuColor SurvColor = new MenuColor("srvcolor", "Survivors Color", new SharpDX.Color(0, 255, 0, 60));
                public static readonly MenuBool DrawSurvivorBox = new MenuBool("srvbox", "Draw Survivors Box", true);
                public static readonly MenuColor KillerColor = new MenuColor("kilcolor", "Killers Color", new SharpDX.Color(255, 0, 0, 100));
                public static readonly MenuBool DrawKillerBox = new MenuBool("drawbox", "Draw Box ESP", true);
                public static readonly MenuSlider DrawBoxThic = new MenuSlider("boxthickness", "Draw Box Thickness", 0, 0, 10);
                public static readonly MenuBool DrawBoxBorder = new MenuBool("drawboxborder", "Draw Border around Box and Text?", true);
                public static readonly MenuBool DrawMiscInfo = new MenuBool("drawmiscinfos", "Draw hatch and escape positions", true);
                public static readonly MenuColor MiscColor = new MenuColor("misccolor", "Draw Text Color", new SharpDX.Color(255, 255, 255, 100));
                public static readonly MenuBool Drawwood = new MenuBool("drawwood", "Draw Wood positions", true);
                //public static readonly MenuBool DrawLocker = new MenuBool("drawlockers", "Draw Lockers positions", true);
                public static readonly MenuBool Drawkeys = new MenuBool("drawkeys", "Draw Key positions", true);
                public static readonly MenuBool DrawMatches = new MenuBool("drawmatches", "Draw Match positions", true);
                public static readonly MenuBool DrawDoll = new MenuBool("drawdolls", "Draw doll positions", true);
                public static readonly MenuBool DrawRunningDoll = new MenuBool("drawrunningdoll", "Draw EvilDoll positions", true);                
            }
   
        }

        public static void InitializeMenu()
        {
            VisualsMenu = new Menu("visualsmenu", "Visuals Menu")
            {
                Components.VisualsComponent.DrawTheVisuals,
                Components.VisualsComponent.SurvColor,
                Components.VisualsComponent.DrawSurvivorBox,
                Components.VisualsComponent.KillerColor,
                Components.VisualsComponent.DrawKillerBox,
                Components.VisualsComponent.DrawBoxThic.SetToolTip("Setting thickness to 0 will let the assembly auto-adjust itself depending on model distance"),
                Components.VisualsComponent.DrawBoxBorder.SetToolTip("Drawing borders may take extra performance (FPS) on low-end computers"),
                Components.VisualsComponent.DrawMiscInfo,
                Components.VisualsComponent.MiscColor,
                Components.VisualsComponent.DrawDoll,
                Components.VisualsComponent.Drawkeys,
                Components.VisualsComponent.DrawMatches,
                Components.VisualsComponent.DrawRunningDoll,
                Components.VisualsComponent.Drawwood,
                
                //Components.VisualsComponent.DrawLocker,
                
                 
                //Components.VisualsComponent.OffsetGuesser,
            };


            RootMenu = new Menu("Pacify", "WeScript.app Pacify BY: Poptart", true)
            {
                Components.MainAssemblyToggle.SetToolTip("The magical boolean which completely disables/enables the assembly!"),
                VisualsMenu,
                MiscMenu,
               
            };
            RootMenu.Attach();
        }

        private static string GetNameFromFName(uint key)
        {

            var chunkOffset = (uint)((int)(key) >> 16); // Block - Sidenote: The casting may not be necessary, arithmetic/logical shifting nonsense.
            var nameOffset = (ushort)key;
            // The first chunk/shard starts at 0x10, so even if chunkOffset is zero, we will start there.
            var namePoolChunk = Memory.ReadPointer(processHandle, (IntPtr)((UInt64)GNamesPtr + ((chunkOffset + 2) * 8)), true);
            var entryOffset = (UInt64)namePoolChunk + (ulong)(2 * nameOffset);
            var nameEntry = Memory.ReadUInt16(processHandle, (IntPtr)entryOffset);

            // If this is negative (and the namePoolChunk & entryOffset's are valid then read the string as Unicode
            // I haven't come across this use case yet.
            // e.g. Read<UnicodeString>(entryOffset + 2, -nameLength * 2)
            var nameLength = nameEntry >> 6;

            string result = Memory.ReadString(processHandle, (IntPtr)entryOffset + 2, false, nameLength);
            return result;
        }

        private static double GetDistance2D(Vector2 pos1, Vector2 pos2)
        {
            Vector2 vector = new Vector2(pos1.X - pos2.X, pos1.Y - pos2.Y);
            return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        }

        private static Vector3 ReadBonePos(IntPtr playerPtr, int boneIDX)
        {
            //#define OFFSET_MeshToWorld 0x1E0
            // OFFSET_MeshArray 0x4A0
            Vector3 targetVec = new Vector3(0, 0, 0);
            var ActorPawn = Memory.ReadPointer(processHandle, playerPtr + 0x128, isWow64Process);
            var mesh = Memory.ReadPointer(processHandle, ActorPawn + 0x0290, isWow64Process);
            //Console.WriteLine(ActorPawn);
            var BoneMatrixPtr = Memory.ReadPointer(processHandle, (mesh + 0x4A0), isWow64Process); //m_dwBoneMatrix
            if (BoneMatrixPtr != IntPtr.Zero)
            {
                targetVec.X = Memory.ReadFloat(processHandle, (BoneMatrixPtr + 0x30 * boneIDX + 0x0C));
                targetVec.Y = Memory.ReadFloat(processHandle, (BoneMatrixPtr + 0x30 * boneIDX + 0x1C));
                targetVec.Z = Memory.ReadFloat(processHandle, (BoneMatrixPtr + 0x30 * boneIDX + 0x2C));
            }
            return targetVec;
        }

        public static Vector3 ClampAngle(Vector3 angle)
        {
            while (angle.Y > 180) angle.Y -= 360;
            while (angle.Y < -180) angle.Y += 360;

            if (angle.X > 89.0f) angle.X = 89.0f;
            if (angle.X < -89.0f) angle.X = -89.0f;

            angle.Z = 0f;

            return angle;
        }

        public static Vector3 NormalizeAngle(Vector3 angle)
        {
            while (angle.X < -180.0f) angle.X += 360.0f;
            while (angle.X > 180.0f) angle.X -= 360.0f;

            while (angle.Y < -180.0f) angle.Y += 360.0f;
            while (angle.Y > 180.0f) angle.Y -= 360.0f;

            while (angle.Z < -180.0f) angle.Z += 360.0f;
            while (angle.Z > 180.0f) angle.Z -= 360.0f;

            return angle;
        }

        public static Vector3 CalcAngle(Vector3 playerPosition, Vector3 enemyPosition, Vector3 aimPunch, Vector3 vecView, float yawRecoilReductionFactory, float pitchRecoilReductionFactor)
        {
            Vector3 delta = new Vector3(playerPosition.X - enemyPosition.X, playerPosition.Y - enemyPosition.Y, (playerPosition.Z + vecView.Z) - enemyPosition.Z);

            Vector3 tmp = Vector3.Zero;
            tmp.X = Convert.ToSingle(System.Math.Atan(delta.Z / System.Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y))) * 57.295779513082f - aimPunch.X * yawRecoilReductionFactory;
            tmp.Y = Convert.ToSingle(System.Math.Atan(delta.Y / delta.X)) * M_PI_F - aimPunch.Y * pitchRecoilReductionFactor;
            tmp.Z = 0;

            if (delta.X >= 0.0) tmp.Y += 180f;

            tmp = NormalizeAngle(tmp);
            tmp = ClampAngle(tmp);

            return tmp;
        }

        public static void SigScan()
        {
            //GWorldPtr = Memory.FindSignature(processHandle, GameBase, GameSize, "48 8B 1D ? ? ? ? 48 85 DB 74 3B 41", 0x3); //4.2
            //Console.WriteLine($"GWorld: {GWorldPtr.ToString("X")}");                                                                                                               //GWorldPtr = Memory.ReadPointer(processHandle, GameBase + 0x8462380, isWow64Process);
            GWorldPtr = Memory.ReadPointer(processHandle, GameBase + 0x35ECE18, isWow64Process);                                                                                                                                                                       // Console.WriteLine($"GWorldPtr: {GWorldPtr.ToString("X")}");
            //GWorldPtr = GameBase + 0x35ECE18; 
            GNamesPtr = GameBase + 0x34ED7C0;

            //GNamesPtr = Memory.FindSignature(processHandle, GameBase, GameSize, "48 8D 1D ? ? ? ? EB 16 48 8D 0D", 0x3);// 4.2
            //Console.WriteLine($"GNamesPtr: {GNamesPtr.ToString("X")}");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("WeScript.app Pacify BY: Poptart");
            InitializeMenu();
            
            Renderer.OnRenderer += OnRenderer;
            Memory.OnTick += OnTick;
        }
        public static double dims = 0.01905f;
        private static double GetDistance3D(Vector3 myPos, Vector3 enemyPos)
        {
            Vector3 vector = new Vector3(myPos.X - enemyPos.X, myPos.Y - enemyPos.Y, myPos.Z - enemyPos.Z);
            return Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z) * dims;
        }

        private static void OnTick(int counter, EventArgs args)
        {

            if (FindWindow == IntPtr.Zero) FindWindow = Memory.FindWindowName("Pacify  ");
            if (FindWindow != IntPtr.Zero)
            {
                
                var calcPid = Memory.GetPIDFromHWND(FindWindow);
                
                gameProcessExists = true;
                if (processHandle == IntPtr.Zero) processHandle = Memory.OpenProcess(PROCESS_ALL_ACCESS, calcPid);

                if (processHandle != IntPtr.Zero)
                {
                    if (GameBase == IntPtr.Zero) GameBase = Memory.GetModule(processHandle, null, isWow64Process);
                    if (GameBase != IntPtr.Zero)
                    {
                        
                        //Console.WriteLine(GameBase.ToString("X"));
                        GameSize = Memory.GetModuleSize(processHandle, null, isWow64Process);
                        if (GWorldPtr == IntPtr.Zero && GNamesPtr == IntPtr.Zero) SigScan();
                        if (GWorldPtr != IntPtr.Zero && GNamesPtr != IntPtr.Zero)
                        {

                            wndMargins = Renderer.GetWindowMargins(FindWindow);
                            wndSize = Renderer.GetWindowSize(FindWindow);
                            isGameOnTop = Renderer.IsGameOnTop(FindWindow);
                            GameCenterPos = new Vector2(wndSize.X / 2 + wndMargins.X, wndSize.Y / 2 + wndMargins.Y);
                            GameCenterPos2 = new Vector2(wndSize.X / 2 + wndMargins.X, wndSize.Y / 2 + wndMargins.Y + 750.0f);//even if the game is windowed, calculate perfectly it's "center" for aim or crosshair
                            isOverlayOnTop = Overlay.IsOnTop();
                            //Console.WriteLine("passed");
                        }
                    }
                }

            }
        }

        private static void OnRenderer(int fps, EventArgs args)
        {
            if (!gameProcessExists) return; //process is dead, don't bother drawing
            if ((!isGameOnTop) && (!isOverlayOnTop)) return; //if game and overlay are not on top, don't draw
            if (!Components.MainAssemblyToggle.Enabled) return; //main menu boolean to toggle the cheat on or off


            
            Functions.Ppc();

            //var UWorld = Memory.ReadPointer(processHandle, GWorldPtr, isWow64Process);
            var ULevel = Memory.ReadPointer(processHandle, GWorldPtr + 0x30, isWow64Process);
            if (ULevel != IntPtr.Zero)
            {

                var AActors = Memory.ReadPointer(processHandle, (IntPtr)ULevel.ToInt64() + 0x98, isWow64Process);
                var ActorCnt = Memory.ReadUInt32(processHandle, (IntPtr)ULevel.ToInt64() + 0xA0);
                //Console.WriteLine(ActorCnt);
                if ((AActors != IntPtr.Zero) && (ActorCnt > 0))
                {
                    for (uint i = 0; i <= ActorCnt; i++)
                    {
                        var AActor = Memory.ReadPointer(processHandle, (IntPtr)(AActors.ToInt64() + i * 8),
                            isWow64Process);
                        if (AActor != IntPtr.Zero)
                        {
                            //Console.WriteLine(AActor.ToString("X"));
                            //var Health = Memory.ReadUInt32(processHandle, (IntPtr)AActor.ToInt64() + Offsets.UE.AActor.Health);
                            var AActorID = Memory.ReadUInt32(processHandle, (IntPtr)AActor.ToInt64() + 0x18);
                            if (!CachedID.ContainsKey(AActorID))
                            {
                                var retname = GetNameFromFName(AActorID);
                                CachedID.Add(AActorID, retname);
                                //Console.WriteLine(AActor.ToString("X"));
                            }
                            var USceneComponent = Memory.ReadPointer(processHandle, (IntPtr)AActor.ToInt64() + 0x130, isWow64Process);
                            if (USceneComponent != IntPtr.Zero)
                            {
                                var tempVec2 = Memory.ReadVector3(processHandle,
                                    (IntPtr)USceneComponent.ToInt64() + 0x11C);

                                var retname = CachedID[AActorID];
                                //Console.WriteLine(retname);

                                if ((AActorID > 0)) //&& (AActorID < 700000)
                                {
                                    if ((survivorID == 0) || (killerID == 0) || (woodID == 0))
                                    {
                                        if (retname.StartsWith("Fire")) woodID = AActorID;
                                        if (retname.StartsWith("Key_Attic")) keyAtticID = AActorID;
                                        if (retname.StartsWith("Key_Bedroom")) keyBedroomID = AActorID;
                                        if (retname.StartsWith("Key_Basement")) keyBasementID = AActorID;
                                        if (retname.StartsWith("Key_Entrance")) keyEntranceID = AActorID;
                                        if (retname.StartsWith("Key_Main")) keyMainID = AActorID;
                                        if (retname.Contains("DarknessDoll")) runningdollID = AActorID;
                                        if (retname.StartsWith("Scary_MP")) killerID = AActorID;
                                        if (retname.StartsWith("BP_Matches")) matchesID = AActorID;
                                        if (retname.StartsWith("PacifyDoll")) PacifyDollID = AActorID;
                                        //PacifyDoll_C if (retname.ToLower().Contains("totem")) totemID = AActorID;

                                    }
                                }


                                var tempVec = Memory.ReadVector3(processHandle, (IntPtr)USceneComponent.ToInt64() + 0x11C);
                                int dist = (int)(GetDistance3D(FMinimalViewInfo_Location, tempVec));

                                



                                if (Components.VisualsComponent.DrawTheVisuals.Enabled)
                                {

                                    if (AActorID == survivorID)
                                    {                                        
                                        Vector2 vScreen_h3adSurvivor = new Vector2(0, 0);
                                        Vector2 vScreen_f33tSurvivor = new Vector2(0, 0);
                                        if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z), out vScreen_h3adSurvivor, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                        {
                                            Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z - 130.0f), out vScreen_f33tSurvivor, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize);

                                            if (Components.VisualsComponent.DrawSurvivorBox.Enabled && dist > 1)
                                            {
                                                Renderer.DrawLine(GameCenterPos2, vScreen_h3adSurvivor, Components.VisualsComponent.SurvColor.Color, Components.VisualsComponent.DrawBoxThic.Value);
                                                //Renderer.DrawFPSBox(vScreen_h3adSurvivor, vScreen_f33tSurvivor, Components.VisualsComponent.SurvColor.Color, BoxStance.standing, Components.VisualsComponent.DrawBoxThic.Value, Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                Renderer.DrawText("SURVIVOR [" + dist + "]", vScreen_f33tSurvivor.X, vScreen_f33tSurvivor.Y + 5, Components.VisualsComponent.SurvColor.Color, 12, TextAlignment.centered, false);
                                            }                                           
                                        }
                                    }


                                    
                                    if (AActorID == killerID)
                                    {
                                        //Console.WriteLine("Inside");
                                        Vector2 vScreen_h3adKiller = new Vector2(0, 0);
                                        Vector2 vScreen_f33tKiller = new Vector2(0, 0);
                                        if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 80.0f), out vScreen_h3adKiller, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                        {
                                            Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z - 70.0f), out vScreen_f33tKiller, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize);
                                            if (Components.VisualsComponent.DrawKillerBox.Enabled)
                                            {
                                                Renderer.DrawFPSBox(vScreen_h3adKiller, vScreen_f33tKiller, Components.VisualsComponent.KillerColor.Color, BoxStance.standing, Components.VisualsComponent.DrawBoxThic.Value, Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                //Renderer.DrawLine(GameCenterPos2, vScreen_h3adKiller + 50, Components.VisualsComponent.KillerColor.Color, Components.VisualsComponent.DrawBoxThic.Value);
                                                Renderer.DrawText("GIRL [" + dist + "m]", vScreen_f33tKiller.X, vScreen_f33tKiller.Y + 5, Components.VisualsComponent.KillerColor.Color, 12, TextAlignment.centered, false);

                                            }

                                            
                                        }
                                    }


                                    if (Components.VisualsComponent.Drawwood.Enabled)
                                    {
                                        if (AActorID == woodID)
                                        {
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 50.0f), out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {

                                                Renderer.DrawText("Wood[" + dist + "m]", vScreen_d3d11, Color.Indigo, 12);
                                            }
                                        }
                                    }

                                    if (Components.VisualsComponent.DrawDoll.Enabled)
                                    {
                                        if (AActorID == PacifyDollID)
                                        {
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 50.0f), out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {

                                                Renderer.DrawText("PacifyDoll[" + dist + "m]", vScreen_d3d11, Color.White, 12);
                                            }
                                        }
                                    }

                                    if (Components.VisualsComponent.DrawMatches.Enabled)
                                    {
                                        if (AActorID == matchesID)
                                        {
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 50.0f), out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {

                                                Renderer.DrawText("Matches[" + dist + "m]", vScreen_d3d11, Color.Silver, 12);
                                            }
                                        }
                                    }

                                    if (Components.VisualsComponent.Drawkeys.Enabled)
                                    {
                                        if (AActorID == keyEntranceID)
                                        {
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 50.0f), out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {

                                                Renderer.DrawText("EntranceKey[" + dist + "m]", vScreen_d3d11, Color.LightGreen, 15);
                                            }
                                        }

                                        if (AActorID == keyAtticID)
                                        {
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 50.0f), out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {

                                                Renderer.DrawText("AtticKey[" + dist + "m]", vScreen_d3d11, Color.Firebrick, 15);
                                            }
                                        }

                                        if (AActorID == keyBasementID) //not working???
                                        {
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {
                                                Renderer.DrawText("BasementKey[" + dist + "m]", vScreen_d3d11, Color.HotPink, 12, TextAlignment.centered, false);
                                            }
                                        }

                                        if (AActorID == keyBedroomID)
                                        {
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {
                                                Renderer.DrawText("BedroomKey[" + dist + "m]", vScreen_d3d11, Color.DeepSkyBlue, 12, TextAlignment.centered, false);
                                            }
                                        }

                                        if (AActorID == keyMainID)
                                        {
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {
                                                Renderer.DrawText("MainKey[" + dist + "m]", vScreen_d3d11, Color.DarkOrchid, 12, TextAlignment.centered, false);
                                            }
                                        }
                                    }


                                    if (Components.VisualsComponent.DrawRunningDoll.Enabled)
                                    {
                                        if (AActorID == runningdollID)
                                        {
                                            Vector2 vScreen_d3d11bb = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11bb, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {
                                                Renderer.DrawText("Catch Me[" + dist + "m]", vScreen_d3d11bb, Color.AliceBlue, 12, TextAlignment.centered, false);
                                            }
                                        }
                                    }



                                    
                                }
                            }
                        }
                    }
                }
            }

        }
    }
}
