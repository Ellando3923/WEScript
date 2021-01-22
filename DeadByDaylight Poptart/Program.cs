using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Mathematics;
using SharpDX.XInput;
using WeScriptWrapper;
using WeScript.SDK.UI;
using WeScript.SDK.UI.Components;

namespace DeadByDaylight
{
    class Program
    {
        public static IntPtr processHandle = IntPtr.Zero; //processHandle variable used by OpenProcess (once)
        public static bool gameProcessExists = false; //avoid drawing if the game process is dead, or not existent
        public static bool isWow64Process = false; //we all know the game is 32bit, but anyway...
        public static bool isGameOnTop = false; //we should avoid drawing while the game is not set on top
        public static bool isOverlayOnTop = false; //we might allow drawing visuals, while the user is working with the "menu"
        public static uint PROCESS_ALL_ACCESS = 0x1FFFFF; //hardcoded access right to OpenProcess (even EAC strips some of the access flags)
        public static Vector2 wndMargins = new Vector2(0, 0); //if the game window is smaller than your desktop resolution, you should avoid drawing outside of it
        public static Vector2 wndSize = new Vector2(0, 0); //get the size of the game window ... to know where to draw
        public static IntPtr GameBase = IntPtr.Zero;
        public static IntPtr GameSize = IntPtr.Zero;
        public static DateTime LastSpacePressedDT = DateTime.Now;
        public static IntPtr GWorldPtr = IntPtr.Zero;
        public static IntPtr GNamesPtr = IntPtr.Zero;

        public static Vector3 FMinimalViewInfo_Location = new Vector3(0, 0, 0);
        public static Vector3 FMinimalViewInfo_Rotation = new Vector3(0, 0, 0);
        public static float FMinimalViewInfo_FOV = 0;
        //public static float Score = 0;

        public static uint survivorID = 0;
        public static uint killerID = 0;
        public static uint escapeID = 0;
        public static uint hatchID = 0;
        public static uint generatorID = 0;
        public static uint totemID = 0;
        public static uint HexID = 0;
        public static uint Pallet = 0;
        public static uint Locker = 0;
        public static uint Trap = 0;
        public static uint Chest = 0;
        public static uint Hook = 0;
        public static uint MedKit = 0;
        public static uint ToolBox = 0;
        


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
                public static readonly MenuBool DrawGenerators = new MenuBool("drawgenerators", "Draw Generators positions", true);
                //public static readonly MenuBool DrawLocker = new MenuBool("drawlockers", "Draw Lockers positions", true);
                public static readonly MenuBool DrawPallet = new MenuBool("drawpallets", "Draw Pallets positions", true);
                public static readonly MenuBool DrawChest = new MenuBool("drawchests", "Draw Chests positions", true);
                public static readonly MenuBool DrawTrap = new MenuBool("drawtraps", "Draw Traps positions", true);
                public static readonly MenuBool DrawHook = new MenuBool("drawHooks", "Draw Hooks positions", true);
                public static readonly MenuBool DrawMedKit = new MenuBool("drawMedKits", "Draw MedKit positions", true);
                public static readonly MenuBool DrawToolBox = new MenuBool("drawToolBox", "Draw ToolBox positions", true);
                //public static readonly MenuSlider OffsetGuesser = new MenuSlider("ofsgues", "Guess the offset", 10, 1, 250);
            }

            public static class MiscComponent
            {
                public static readonly MenuBool AutoSkillCheck = new MenuBool("autoskillcheck", "Auto Skill Check (+Bonus)", true);
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
                Components.VisualsComponent.DrawGenerators,
                //Components.VisualsComponent.DrawLocker,
                Components.VisualsComponent.DrawPallet,
                Components.VisualsComponent.DrawChest,
                Components.VisualsComponent.DrawTrap,
                Components.VisualsComponent.DrawHook,
                 Components.VisualsComponent.DrawMedKit,
                 Components.VisualsComponent.DrawToolBox,
                //Components.VisualsComponent.OffsetGuesser,
            };

            MiscMenu = new Menu("miscmenu", "Misc Menu")
            {
                Components.MiscComponent.AutoSkillCheck
            };


            RootMenu = new Menu("dbdexample", "WeScript.app DeadByDaylight Example Assembly", true)
            {
                Components.MainAssemblyToggle.SetToolTip("The magical boolean which completely disables/enables the assembly!"),
                VisualsMenu,
                MiscMenu,
            };
            RootMenu.Attach();
        }


        public static string GetNameFromID(uint ID) //really bad implementation - probably needs fixing, plus it's better to use it as a dumper once at startup and cache ids
        {
            if (processHandle != IntPtr.Zero)
            {
                if (GameBase != IntPtr.Zero)
                {
                   
                    uint BlockIndex = ID >> 16;
                    var Address = Memory.ZwReadPointer(processHandle, (IntPtr)(GNamesPtr.ToInt64() + 0x10 + BlockIndex * 8), isWow64Process);
                    if (Address != IntPtr.Zero)
                    {
                        var Offset = ID & 65535;
                        var NameAddress = (IntPtr)(Address.ToInt64() + Offset * 4);
                        var tempID = Memory.ZwReadDWORD(processHandle, NameAddress);
                        if (tempID == ID)
                        {
                            var charLen = Memory.ZwReadWORD(processHandle, (IntPtr)(NameAddress.ToInt64() + 4));
                            if (charLen > 0)
                            {
                                var name = Memory.ZwReadString(processHandle, (IntPtr)(NameAddress.ToInt64() + 6), false, charLen);
                                if (name.Length > 0) return name;
                            }
                        }
                    }
                }
            }
            return "NULL";
        }

        static void Main(string[] args)
        {
            Console.WriteLine("WeScript.app experimental DBD assembly for patch 4.0.2 with Driver bypass");
            InitializeMenu();
            if (!Memory.InitDriver(DriverName.nsiproxy))
            {
                Console.WriteLine("[ERROR] Failed to initialize driver for some reason...");
            }
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
            if (processHandle == IntPtr.Zero) //if we still don't have a handle to the process
            {
                var wndHnd = Memory.FindWindowName("DeadByDaylight  "); //why the devs added spaces after the name?!
                if (wndHnd != IntPtr.Zero) //if it exists
                {
                    //Console.WriteLine("weheree");
                    var calcPid = Memory.GetPIDFromHWND(wndHnd); //get the PID of that same process
                    if (calcPid > 0) //if we got the PID
                    {
                        processHandle = Memory.ZwOpenProcess(PROCESS_ALL_ACCESS, calcPid); //the driver will get a stripped handle, but doesn't matter, it's still OK
                        if (processHandle != IntPtr.Zero)
                        {
                            //if we got access to the game, check if it's x64 bit, this is needed when reading pointers, since their size is 4 for x86 and 8 for x64
                            isWow64Process = Memory.IsProcess64Bit(processHandle); //we know DBD is 64 bit but anyway...
                        }
                        else
                        {
                            Console.WriteLine("failed to get handle");
                        }
                    }
                }
            }
            else //else we have a handle, lets check if we should close it, or use it
            {
                var wndHnd = Memory.FindWindowName("DeadByDaylight  "); //why the devs added spaces after the name?!
                if (wndHnd != IntPtr.Zero) //window still exists, so handle should be valid? let's keep using it
                {
                    //the lines of code below execute every 33ms outside of the renderer thread, heavy code can be put here if it's not render dependant
                    gameProcessExists = true;
                    wndMargins = Renderer.GetWindowMargins(wndHnd);
                    wndSize = Renderer.GetWindowSize(wndHnd);
                    isGameOnTop = Renderer.IsGameOnTop(wndHnd);
                    isOverlayOnTop = Overlay.IsOnTop();

                    if (GameBase == IntPtr.Zero) //do we have access to Gamebase address?
                    {
                        GameBase = Memory.ZwGetModule(processHandle, null, isWow64Process); //if not, find it
                        //Console.WriteLine($"GameBase: {GameBase.ToString("X")}");
                        Console.WriteLine("Got GAMEBASE of DBD!");
                    }
                    else
                    {
                        if (GameSize == IntPtr.Zero)
                        {
                            GameSize = Memory.ZwGetModuleSize(processHandle, null, isWow64Process);
                            //Console.WriteLine($"GameSize: {GameSize.ToString("X")}");
                        }
                        else
                        {
                            if (GWorldPtr == IntPtr.Zero)
                            {
                                //GWorldPtr = Memory.ZwFindSignature(processHandle, GameBase, GameSize, "48 8B 1D ? ? ? ? 48 85 DB 74 3B 41", 0x3); //4.1 patch
                                GWorldPtr = Memory.ZwFindSignature(processHandle, GameBase, GameSize, "48 89 05 ? ? ? ? 0F 28 D7", 0x3);
                            }
                            if (GNamesPtr == IntPtr.Zero)
                            {
                                //GNamesPtr = Memory.ZwFindSignature(processHandle, GameBase, GameSize, "48 8B 05 ? ? ? ? 48 85 C0 75 5F", 0x3); //4.1 patch
                                GNamesPtr = Memory.ZwFindSignature(processHandle, GameBase, GameSize, "48 8D 1D ? ? ? ? EB 16 48 8D 0D", 0x3);
                                Console.WriteLine($"GNamesPtr: {GNamesPtr.ToString("X")}");
                            }
                        }
                    }

                }
                else //else most likely the process is dead, clean up
                {
                    Memory.CloseHandle(processHandle); //close the handle to avoid leaks
                    processHandle = IntPtr.Zero; //set it like this just in case for C# logic
                    gameProcessExists = false;
                    //clear your offsets, modules
                    GameBase = IntPtr.Zero;
                    GameSize = IntPtr.Zero;

                    GWorldPtr = IntPtr.Zero;
                    GNamesPtr = IntPtr.Zero;

                }
            }
        }

        private static void OnRenderer(int fps, EventArgs args)
        {
            if (!gameProcessExists) return; //process is dead, don't bother drawing
            if ((!isGameOnTop) && (!isOverlayOnTop)) return; //if game and overlay are not on top, don't draw
            if (!Components.MainAssemblyToggle.Enabled) return; //main menu boolean to toggle the cheat on or off

            float GetLuck = 0;
            GetLuck = Memory.ZwReadFloat(processHandle, (IntPtr)GameBase.ToInt64() + 0x2618D70);
            
            float Score = 0;
            var myPos = new Vector3();
            var USkillCheck = IntPtr.Zero;
            var UWorld = Memory.ZwReadPointer(processHandle, GWorldPtr, isWow64Process);
            if (UWorld != IntPtr.Zero)
            {
                var UGameInstance = Memory.ZwReadPointer(processHandle, (IntPtr)UWorld.ToInt64() + 0x198, isWow64Process);
                if (UGameInstance != IntPtr.Zero)
                {
                    var localPlayerArray = Memory.ZwReadPointer(processHandle, (IntPtr)UGameInstance.ToInt64() + 0x40,
                        isWow64Process);
                    if (localPlayerArray != IntPtr.Zero)
                    {
                        var ULocalPlayer = Memory.ZwReadPointer(processHandle, localPlayerArray, isWow64Process);
                        if (ULocalPlayer != IntPtr.Zero)
                        {
                            var ULocalPlayerControler = Memory.ZwReadPointer(processHandle,
                                (IntPtr)ULocalPlayer.ToInt64() + 0x0038, isWow64Process);

                            var Upawn = Memory.ZwReadPointer(processHandle, (IntPtr)ULocalPlayerControler.ToInt64() + 0x02B8, isWow64Process);
                            var UplayerState = Memory.ZwReadPointer(processHandle, (IntPtr)Upawn.ToInt64() + 0x0250, isWow64Process);
                            Score = Memory.ZwReadFloat(processHandle, (IntPtr)UplayerState.ToInt64() + 0x0230);
                            
                            if (ULocalPlayerControler != IntPtr.Zero)
                            {
                                var ULocalPlayerPawn = Memory.ZwReadPointer(processHandle,
                                    (IntPtr)ULocalPlayerControler.ToInt64() + 0x0278, isWow64Process);
                                if (ULocalPlayerPawn != IntPtr.Zero)
                                {
                                    var UInteractionHandler = Memory.ZwReadPointer(processHandle,
                                        (IntPtr)ULocalPlayerPawn.ToInt64() + 0x0888, isWow64Process);

                                    if (UInteractionHandler != IntPtr.Zero)
                                    {
                                        USkillCheck = Memory.ZwReadPointer(processHandle,
                                            (IntPtr)UInteractionHandler.ToInt64() + 0x02B8, isWow64Process);
                                    }

                                    var ULocalRoot = Memory.ZwReadPointer(processHandle,
                                        (IntPtr)ULocalPlayerPawn.ToInt64() + 0x0140, isWow64Process);
                                    if (ULocalRoot != IntPtr.Zero)
                                    {
                                        myPos = Memory.ZwReadVector3(processHandle,(IntPtr)ULocalRoot.ToInt64() + 0x0118);
                                    }
                                }

                                var APlayerCameraManager = Memory.ZwReadPointer(processHandle,(IntPtr)ULocalPlayerControler.ToInt64() + 0x2D0, isWow64Process);
                                if (APlayerCameraManager != IntPtr.Zero)
                                {
                                    FMinimalViewInfo_Location = Memory.ZwReadVector3(processHandle,(IntPtr)APlayerCameraManager.ToInt64() + 0x1A80 + 0x0000);

                                    FMinimalViewInfo_Rotation = Memory.ZwReadVector3(processHandle,(IntPtr)APlayerCameraManager.ToInt64() + 0x1A80 + 0x000C);

                                    FMinimalViewInfo_FOV = Memory.ZwReadFloat(processHandle,(IntPtr)APlayerCameraManager.ToInt64() + 0x1A80 + 0x0018);
                                }
                            }

                            
                        }
                    }

                }

                if (Components.MiscComponent.AutoSkillCheck.Enabled)
                {
                    if (USkillCheck != IntPtr.Zero)
                    {
                        
                        var isDisplayed = Memory.ZwReadBool(processHandle,
                            (IntPtr)USkillCheck.ToInt64() + 0x027C);
                        if (isDisplayed && LastSpacePressedDT.AddMilliseconds(200) <
                            DateTime.Now)
                        {
                            var currentProgress = Memory.ZwReadFloat(processHandle,
                                (IntPtr)USkillCheck.ToInt64() + 0x022C); //0x02A0
                            var startSuccessZone = Memory.ZwReadFloat(processHandle,
                                (IntPtr)USkillCheck.ToInt64() + 0x0264);

                            if (currentProgress > startSuccessZone)
                            {
                                LastSpacePressedDT = DateTime.Now;
                                Input.KeyPress(VirtualKeyCode.Space);
                            }
                        }
                    }
                }

                


                var ULevel = Memory.ZwReadPointer(processHandle, (IntPtr)UWorld.ToInt64() + 0x38, isWow64Process);
                if (ULevel != IntPtr.Zero)
                {
                    var AActors = Memory.ZwReadPointer(processHandle, (IntPtr)ULevel.ToInt64() + 0xA0, isWow64Process);
                    var ActorCnt = Memory.ZwReadUInt32(processHandle, (IntPtr)ULevel.ToInt64() + 0xA8);
                    if ((AActors != IntPtr.Zero) && (ActorCnt > 0))
                    {
                        for (uint i = 0; i <= ActorCnt; i++)
                        {
                            var AActor = Memory.ZwReadPointer(processHandle, (IntPtr)(AActors.ToInt64() + i * 8),
                                isWow64Process);
                            if (AActor != IntPtr.Zero)
                            {
                                var USceneComponent = Memory.ZwReadPointer(processHandle,
                                    (IntPtr)AActor.ToInt64() + 0x140, isWow64Process);
                                if (USceneComponent != IntPtr.Zero)
                                {
                                    var tempVec = Memory.ZwReadVector3(processHandle,
                                        (IntPtr)USceneComponent.ToInt64() + 0x118);
                                    var AActorID = Memory.ZwReadUInt32(processHandle,
                                        (IntPtr)AActor.ToInt64() + 0x18);
                                    var HexID2 = Memory.ZwReadUInt32(processHandle, (IntPtr)(AActor.ToInt64() + 0x02C8));


                                    string retname2 = "";
                                    string retname = "";
                                    retname = GetNameFromID(AActorID);
                                    if ((AActorID > 0)) //&& (AActorID < 700000)
                                    {
                                        if ((survivorID == 0) || (killerID == 0) || (escapeID == 0) ||
                                            (hatchID == 0) || (generatorID == 0) || (totemID == 0 ) || (HexID == 0)) 
                                        {

                                            retname2 = GetNameFromID(HexID2);
                                            
                                            //Console.WriteLine(retname);
                                            if (retname.Contains("BP_CamperInteractable_"))
                                            {
                                                survivorID = AActorID;
                                            }

                                            if (retname.Contains("Trap"))
                                            {
                                                Trap = AActorID;
                                            }

                                            if (retname.ToLower().Contains("kit"))
                                            {
                                                MedKit = AActorID;
                                            }

                                            if (retname.ToLower().Contains("tool"))
                                            {
                                                ToolBox = AActorID;
                                            }

                                            if (retname.Contains("Meat"))
                                            {
                                                Hook = AActorID;
                                            }

                                            if (retname.Contains("SmallMeatLocker")) 
                                            {
                                                Locker = AActorID;
                                            }

                                            if (retname.Contains("Bookshelf"))
                                            {
                                                Pallet = AActorID;
                                            }

                                            if (retname2.Contains("Hex"))
                                            {
                                                HexID = HexID2;
                                                
                                            }

                                            if (retname.ToLower().Contains("totem"))
                                            {
                                                totemID = AActorID;
                                            }

                                            if (retname.Contains("SlasherInteractable_"))
                                            {
                                                killerID = AActorID;
                                            }

                                            if (retname.Contains("BP_Escape01"))
                                            {
                                                escapeID = AActorID;
                                            }

                                            if (retname.Contains("BP_Hatch"))
                                            {
                                                hatchID = AActorID;
                                            }

                                            if (retname.Contains("Chest"))
                                            {
                                                Chest = AActorID;
                                            }

                                            if (retname.StartsWith("Generator"))
                                                generatorID = AActorID;
                                        }

                                       
                                    }
                                    int dist = (int)(GetDistance3D(myPos, tempVec));

                                    //var Upawn = 
                                    //var UPlayerState = Memory.ZwReadPointer(processHandle, (IntPtr)UWorldSettings.ToInt64() + 0x0358, isWow64Process);
                                    //var Score = Memory.ZwReadFloat(processHandle, (IntPtr)UPlayerState.ToInt64() + 0x0230);


                                    Vector2 vScreen_d3d11xx = new Vector2(0, 0);
                                    if (AActorID == survivorID)
                                    {
                                        if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11xx, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                        {
                                            if(dist < 10)
                                            Renderer.DrawText(Score + "", vScreen_d3d11xx, Color.HotPink, 20, TextAlignment.centered, true);
                                            //Renderer.DrawText(GetLuck + "", vScreen_d3d11x, Color.Honeydew, 20, TextAlignment.lefted, false);
                                        }
                                    }

                                    if (Components.VisualsComponent.DrawTheVisuals.Enabled) 
                                    {
                                        
                                        if (AActorID == survivorID)
                                        {
                                            Vector2 vScreen_h3ad = new Vector2(0, 0);
                                            Vector2 vScreen_f33t = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 60.0f),out vScreen_h3ad, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation,FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {
                                                Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z - 130.0f),out vScreen_f33t, FMinimalViewInfo_Location,FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins,wndSize);

                                                if (Components.VisualsComponent.DrawSurvivorBox.Enabled)
                                                {
                                                    //Renderer.DrawFPSBox(vScreen_h3ad, vScreen_f33t,Components.VisualsComponent.SurvColor.Color,BoxStance.standing,Components.VisualsComponent.DrawBoxThic.Value,Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                    Renderer.DrawText("SURVIVOR [" + dist + "m]", vScreen_f33t.X,vScreen_f33t.Y + 5,Components.VisualsComponent.SurvColor.Color, 12,TextAlignment.centered, false);
                                                }
                                            }
                                        }

                                        if (AActorID == killerID)
                                        {
                                            Vector2 vScreen_h3ad = new Vector2(0, 0);
                                            Vector2 vScreen_f33t = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z + 80.0f),out vScreen_h3ad, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation,FMinimalViewInfo_FOV, wndMargins, wndSize))
                                            {
                                                Renderer.WorldToScreenUE4(new Vector3(tempVec.X, tempVec.Y, tempVec.Z - 150.0f),out vScreen_f33t, FMinimalViewInfo_Location,FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins,wndSize);
                                                if (Components.VisualsComponent.DrawKillerBox.Enabled)
                                                {
                                                    Renderer.DrawFPSBox(vScreen_h3ad, vScreen_f33t,Components.VisualsComponent.KillerColor.Color,BoxStance.standing,Components.VisualsComponent.DrawBoxThic.Value,Components.VisualsComponent.DrawBoxBorder.Enabled);
                                                    Renderer.DrawText("KILLER [" + dist + "m]", vScreen_f33t.X,vScreen_f33t.Y + 5,Components.VisualsComponent.KillerColor.Color, 12,TextAlignment.centered, false);
                                                }
                                            }
                                            
                                        }


                                        if (Components.VisualsComponent.DrawMedKit.Enabled)
                                        {
                                            if (AActorID == MedKit) //not working???
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {

                                                    Renderer.DrawText(" MedKit [" + dist + "m]", vScreen_d3d11, Color.HotPink, 12, TextAlignment.centered, false);
                                                }
                                            }
                                        }

                                        if (Components.VisualsComponent.DrawHook.Enabled)
                                        {
                                            if (AActorID == Hook)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    if(dist < 100)
                                                    Renderer.DrawText(" Hook [" + dist + "m]", vScreen_d3d11, Color.DeepSkyBlue, 12, TextAlignment.centered, false);
                                                }
                                            }
                                        }

                                        if (Components.VisualsComponent.DrawToolBox.Enabled)
                                        {
                                            if (AActorID == ToolBox)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {

                                                    Renderer.DrawText(" ToolBox [" + dist + "m]", vScreen_d3d11, Color.DarkOrchid, 12, TextAlignment.centered, false);
                                                }
                                            }
                                        }

                                        //if (Components.VisualsComponent.DrawLocker.Enabled) // not working???
                                        //{
                                        //    if (AActorID == Locker)
                                        //    {
                                        //        Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                        //        if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                        //        {

                                        //            Renderer.DrawText(" Locker [" + dist + "m]", vScreen_d3d11, Color.HotPink, 12, TextAlignment.centered, false);
                                        //        }
                                        //    }
                                        //}

                                        if (Components.VisualsComponent.DrawPallet.Enabled) // not working???
                                        {
                                            if (AActorID == Pallet)
                                            {
                                                
                                                Vector2 vScreen_d3d11bb = new Vector2(0, 0);
                                                
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11bb, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    if(dist < 100)
                                                    Renderer.DrawText(" Pallet [" + dist + "m]", vScreen_d3d11bb, Color.AliceBlue, 12, TextAlignment.centered, false);
                                                    
                                                }
                                            }
                                        }

                                        if (Components.VisualsComponent.DrawChest.Enabled)
                                        {
                                            if (AActorID == Chest)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {

                                                    Renderer.DrawText(" Chest [" + dist + "m]", vScreen_d3d11, Color.Purple, 12, TextAlignment.centered, false);
                                                }
                                            }

                                        }


                                        if (Components.VisualsComponent.DrawTrap.Enabled)
                                        {
                                            if (AActorID == Trap)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {

                                                    Renderer.DrawText(" Trap [" + dist + "m]", vScreen_d3d11, Color.Beige, 12, TextAlignment.centered, false);
                                                }
                                            }

                                        }



                                        if (Components.VisualsComponent.DrawMiscInfo.Enabled)
                                        {
        
                                            if (AActorID == totemID)
                                            {

                                                var IsCleansed = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x02D4);
                                                var HexID = Memory.ZwReadString(processHandle, (IntPtr)(AActor.ToInt64() + 0x02C8), true, 20);

                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11, FMinimalViewInfo_Location, FMinimalViewInfo_Rotation, FMinimalViewInfo_FOV, wndMargins, wndSize))
                                                {
                                                    if(IsCleansed == false)
                                                    Renderer.DrawText(" Totem [" + HexID + dist + "m]", vScreen_d3d11, Color.HotPink, 12, TextAlignment.centered, false);
                                                }
                                            }
                                            
                                            else if (AActorID == hatchID)
                                            {
                                                Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                                if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11,FMinimalViewInfo_Location, FMinimalViewInfo_Rotation,FMinimalViewInfo_FOV,wndMargins, wndSize))
                                                {
                                                    Renderer.DrawText("HATCH [" + dist + "m]", vScreen_d3d11,Components.VisualsComponent.MiscColor.Color, 12,TextAlignment.centered, false);
                                                }
                                            }

                                            
                                        }

                                        if (Components.VisualsComponent.DrawGenerators.Enabled)
                                        {
                                            if (AActorID != generatorID)
                                                continue;
                                            var isRepaired = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x02C9);
                                            var isBlocked = Memory.ZwReadBool(processHandle, (IntPtr)AActor.ToInt64() + 0x038C);

                                            var currentProgressPercent =
                                                Memory.ZwReadFloat(processHandle, (IntPtr)AActor.ToInt64() + 0x02D8) * 100;
                                            Color selectedColor;
                                            if (isBlocked)
                                                selectedColor = Color.Red;
                                            
                                            else
                                                selectedColor = Color.Yellow;
                                            Vector2 vScreen_d3d11 = new Vector2(0, 0);
                                            if (Renderer.WorldToScreenUE4(tempVec, out vScreen_d3d11,FMinimalViewInfo_Location, FMinimalViewInfo_Rotation,FMinimalViewInfo_FOV,wndMargins, wndSize))
                                            {
                                                if(isRepaired == false)
                                                Renderer.DrawText($"Generator [{dist}m] ({currentProgressPercent:##0}%)",vScreen_d3d11,selectedColor, 15, TextAlignment.centered, false);
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
