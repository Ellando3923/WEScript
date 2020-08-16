using System;
using System.Collections.Generic;
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
using WeScript.SDK.Utils;

namespace RocketLeague
{
    class Program
    {
        public static IntPtr processHandle = IntPtr.Zero; //processHandle variable used by OpenProcess (once)
        public static bool gameProcessExists = false; //avoid drawing if the game process is dead, or not existent
        public static bool isWow64Process = false; //we all know the game is 32bit, but anyway...
        public static bool isGameOnTop = false; //we should avoid drawing while the game is not set on top
        public static bool isOverlayOnTop = false; //we might allow drawing visuals, while the user is working with the "menu"
        public static uint PROCESS_ALL_ACCESS = 0x1FFFFF; //hardcoded access right to OpenProcess
        public static Vector2 wndMargins = new Vector2(0, 0); //if the game window is smaller than your desktop resolution, you should avoid drawing outside of it
        public static Vector2 wndSize = new Vector2(0, 0); //get the size of the game window ... to know where to draw
        public static IntPtr GameBase = IntPtr.Zero;
        public static Vector2 GameCenterPos = new Vector2(0, 0);


        public static Menu RootMenu { get; private set; }
        public static Menu VisualsMenu { get; private set; }


        class Components
        {
            public static readonly MenuKeyBind MainAssemblyToggle = new MenuKeyBind("mainassemblytoggle", "Toggle the whole assembly effect by pressing key:", VirtualKeyCode.Delete, KeybindType.Toggle, true);
            public static class VisualsComponent
            {
                public static readonly MenuBool DrawTheVisuals = new MenuBool("drawthevisuals", "Enable all of the Visuals", true);
                public static readonly MenuBool DrawBoostTimer = new MenuBool("drawtext", "Draw Boost Timer", true);
                
            }
        }


        public static void InitializeMenu()
        {
            VisualsMenu = new Menu("visualsmenu", "Visuals Menu")
            {
                Components.VisualsComponent.DrawTheVisuals,
                
                Components.VisualsComponent.DrawBoostTimer,
            };


            RootMenu = new Menu("RocketLeague", "WeScript.app RocketLeague Assembly", true)
            {
                Components.MainAssemblyToggle.SetToolTip("The magical boolean which completely disables/enables the assembly!"),
                VisualsMenu,
                Components.VisualsComponent.DrawBoostTimer,
            };
            RootMenu.Attach();
        }


        static void Main(string[] args)
        {
            Console.WriteLine("WeScript.app RocketLeague Assembly By Poptart && GameHackerPM 1.0 Loaded!");

            InitializeMenu();
            Renderer.OnRenderer += OnRenderer;
            Memory.OnTick += OnTick;
        }

        private static void OnTick(int counter, EventArgs args)
        {
            if (processHandle == IntPtr.Zero) //if we still don't have a handle to the process
            {
                var wndHnd = Memory.FindWindowName("Rocket League (64-bit, DX11, Cooked)"); //try finding the window of the process (check if it's spawned and loaded)
                if (wndHnd != IntPtr.Zero) //if it exists
                {
                    var calcPid = Memory.GetPIDFromHWND(wndHnd); //get the PID of that same process
                    if (calcPid > 0) //if we got the PID
                    {
                        processHandle = Memory.OpenProcess(PROCESS_ALL_ACCESS, calcPid); //get full access to the process so we can use it later
                        if (processHandle != IntPtr.Zero)
                        {
                            //if we got access to the game, check if it's x64 bit, this is needed when reading pointers, since their size is 4 for x86 and 8 for x64
                            isWow64Process = Memory.IsProcess64Bit(processHandle);
                            //here you can scan for signatures and stuff, it happens only once on "attach"
                        }
                    }
                }
            }
            else //else we have a handle, lets check if we should close it, or use it
            {
                var wndHnd = Memory.FindWindowName("Rocket League (64-bit, DX11, Cooked)");
                if (wndHnd != IntPtr.Zero) //window still exists, so handle should be valid? let's keep using it
                {
                    //the lines of code below execute every 33ms outside of the renderer thread, heavy code can be put here if it's not render dependant
                    gameProcessExists = true;
                    wndMargins = Renderer.GetWindowMargins(wndHnd);
                    wndSize = Renderer.GetWindowSize(wndHnd);
                    isGameOnTop = Renderer.IsGameOnTop(wndHnd);
                    GameCenterPos = new Vector2(wndSize.X / 2 + wndMargins.X, wndSize.Y / 2 + wndMargins.Y);
                    isOverlayOnTop = Overlay.IsOnTop();
                    GameBase = Memory.GetModule(processHandle, null, isWow64Process);
                }
                else //else most likely the process is dead, clean up
                {
                    Memory.CloseHandle(processHandle); //close the handle to avoid leaks
                    processHandle = IntPtr.Zero; //set it like this just in case for C# logic
                    gameProcessExists = false;
                }
            }
        }
        
        public static List<long> BoostsObjects = new List<long>();
        private static Dictionary<long, DateTime> BoostsTimers = new Dictionary<long, DateTime>();
        private static void OnRenderer(int fps, EventArgs args)
        {
            if (!gameProcessExists) return; //process is dead, don't bother drawing
            if ((!isGameOnTop) && (!isOverlayOnTop)) return; //if game and overlay are not on top, don't draw



            var GameEngine = Memory.ReadPointer(processHandle, (IntPtr)GameBase.ToInt64() + 0x023BBEE8, isWow64Process);
            var LocalPlayersArray = Memory.ReadPointer(processHandle, (IntPtr)GameEngine.ToInt64() + 0x760, isWow64Process);

            var LocalPlayer = Memory.ReadPointer(processHandle, (IntPtr)LocalPlayersArray.ToInt64(), isWow64Process); //So here we have Base + the offset
            var PlayerController = Memory.ReadPointer(processHandle, (IntPtr)LocalPlayer.ToInt64() + 0x0078, isWow64Process);
            var WorldInfo = Memory.ReadPointer(processHandle, (IntPtr)PlayerController.ToInt64() + 0x0130, isWow64Process);
            

            var PlayerCamera = Memory.ReadPointer(processHandle, (IntPtr)(PlayerController.ToInt64() + 0x0480), isWow64Process);

            

            var Location = Memory.ReadVector3(processHandle, (IntPtr)PlayerCamera.ToInt64() + 0x0090);
            var LastCamFov = Memory.ReadFloat(processHandle, (IntPtr)PlayerCamera.ToInt64() + 0x0278);
            var Pitch = Memory.ReadInt32(processHandle, (IntPtr)PlayerCamera.ToInt64() + 0x009C);
            var Yaw = Memory.ReadInt32(processHandle, (IntPtr)PlayerCamera.ToInt64() + 0x009C + 0x04);
            var Roll = Memory.ReadInt32(processHandle, (IntPtr)PlayerCamera.ToInt64() + 0x009C + 0x08);

            ////////////////////////////////Putting Boost things here!!///////////////////////////////////////////////////


            var GameShare = Memory.ReadPointer(processHandle, (IntPtr)(WorldInfo.ToInt64() + 0x0AF0), isWow64Process);

            var BoostA = Memory.ReadPointer(processHandle, (IntPtr)(GameShare.ToInt64() + 0x0078), isWow64Process); // This is the Pill Array 6 pointers == 6 Pills

            var Boost1 = Memory.ReadPointer(processHandle, (IntPtr)(BoostA.ToInt64() + 0x0000), isWow64Process);
            var Pill1 = Memory.ReadVector3(processHandle, (IntPtr)Boost1.ToInt64() + 0x0090);

            var Boost2 = Memory.ReadPointer(processHandle, (IntPtr)(BoostA.ToInt64() + 0x0008), isWow64Process);
            var Pill2 = Memory.ReadVector3(processHandle, (IntPtr)Boost2.ToInt64() + 0x0090);

            var Boost3 = Memory.ReadPointer(processHandle, (IntPtr)(BoostA.ToInt64() + 0x0010), isWow64Process);
            var Pill3 = Memory.ReadVector3(processHandle, (IntPtr)Boost3.ToInt64() + 0x0090);

            var Boost4 = Memory.ReadPointer(processHandle, (IntPtr)(BoostA.ToInt64() + 0x0018), isWow64Process);
            var Pill4 = Memory.ReadVector3(processHandle, (IntPtr)Boost4.ToInt64() + 0x0090);

            var Boost5 = Memory.ReadPointer(processHandle, (IntPtr)(BoostA.ToInt64() + 0x0020), isWow64Process);
            var Pill5 = Memory.ReadVector3(processHandle, (IntPtr)Boost5.ToInt64() + 0x0090);

            var Boost6 = Memory.ReadPointer(processHandle, (IntPtr)(BoostA.ToInt64() + 0x0028), isWow64Process);
            var Pill6 = Memory.ReadVector3(processHandle, (IntPtr)Boost6.ToInt64() + 0x0090);

            

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////






            ///////////////////////////////////////////////// LOCATION ROTATION FOV ////////////////////////////////////////////////////////////////



            var rotator = new FRotator
            {
                Pitch = Pitch,
                Yaw = Yaw,
                Roll = Roll,
            };
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



            /////////////////////////////////////////////// WORLD TO SCREEN ///////////////////////////////////////////////////////////////////////////////////////////



            var Pills = new[] { Pill1, Pill2, Pill3, Pill4, Pill5, Pill6 };
            var BoostsArray = Memory.ReadPointer(processHandle, (IntPtr)(GameShare.ToInt64() + 0x0078), isWow64Process);
            var BoostsArrayCnt = Memory.ReadInt32(processHandle, (IntPtr)(GameShare.ToInt64() + 0x0080)); //Always 6 Should be.
            for (int r = 0; r < BoostsArrayCnt; r++)
            {
                
                var currentBoost = Memory.ReadPointer(processHandle, (IntPtr)BoostsArray.ToInt64() + (r * 0x8), isWow64Process);

                var boostPos = Memory.ReadVector3(processHandle, (IntPtr)currentBoost.ToInt64() + 0x0090);
               

                string nameOrTime = "Pill" + (r + 1);

                if (!BoostsObjects.Contains(currentBoost.ToInt64()))
                    BoostsObjects.Add(currentBoost.ToInt64());


                
            }

            foreach (long objectPtr in BoostsObjects)
            {
                var isPicked = Memory.ReadBool(processHandle, (IntPtr)objectPtr + 0x02B8);

                if (isPicked)
                {
                    if (!BoostsTimers.ContainsKey(objectPtr))
                        BoostsTimers.Add(objectPtr, DateTime.Now.AddSeconds(10));
                }
            }

            foreach (var boostTimer in BoostsTimers.ToDictionary(x => x.Key, y => y.Value))
            {
                var boostPos = Memory.ReadVector3(processHandle, (IntPtr)boostTimer.Key + 0x0090);
                var timeLeft = (boostTimer.Value - DateTime.Now).TotalMilliseconds / 1000;
                var timeLeftStr = timeLeft.ToString("0.0");
                if (timeLeft <= 0)
                {
                    BoostsTimers.Remove(boostTimer.Key);
                    BoostsObjects.Remove(boostTimer.Key);
                    continue;
                }
                



                Vector2 PillVecOnScreen = new Vector2(0, 0);
                if (Renderer.WorldToScreenUE3(boostPos, out PillVecOnScreen, Location, rotator.Pitch, rotator.Yaw, rotator.Roll, LastCamFov, wndMargins, wndSize))
                    if (Components.VisualsComponent.DrawBoostTimer.Enabled)
                    {
                    Renderer.DrawText(timeLeftStr, PillVecOnScreen, Color.DarkOrange, 35, TextAlignment.centered, true);
                }

            }

            

        }




    }
    public class FRotator
    {
        public int Pitch;
        public int Yaw;
        public int Roll;
    }
}
