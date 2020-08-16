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

namespace RocketLeagueTest
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
                public static readonly MenuBool DrawBox = new MenuBool("drawbox", "Draw Box ESP", true);
                public static readonly MenuSlider DrawBoxThic = new MenuSlider("boxthickness", "Draw Box Thickness", 0, 0, 10);
                public static readonly MenuBool DrawBoxBorder = new MenuBool("drawboxborder", "Draw Border around Box and Text?", true);
                public static readonly MenuBool DrawBoxHP = new MenuBool("drawboxhp", "Draw Health", true);
            }
        }


        public static void InitializeMenu()
        {
            VisualsMenu = new Menu("visualsmenu", "Visuals Menu")
            {
                Components.VisualsComponent.DrawTheVisuals,
                Components.VisualsComponent.DrawBoxThic.SetToolTip("Setting thickness to 0 will let the assembly auto-adjust itself depending on model distance"),
                Components.VisualsComponent.DrawBoxBorder.SetToolTip("Drawing borders may take extra performance (FPS) on low-end computers"),
                Components.VisualsComponent.DrawBox,
            };


            RootMenu = new Menu("RocketLeague", "WeScript.app RocketLeague Assembly", true)
            {
                Components.MainAssemblyToggle.SetToolTip("The magical boolean which completely disables/enables the assembly!"),
                VisualsMenu,
                Components.VisualsComponent.DrawBox,
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
        //Let's do a model for these functions..

        //private static float VectorDotProduct(Vector3 A, Vector3 B)
        //{
        //    float Retn;
        //    Retn = A.X * B.X + A.Y * B.Y + A.Z * B.Z;
        //    return Retn;
        //}
        //private static Vector2 WorldToScreenUE3(FRotator rotation, float FOV, Vector3 playerPos, Vector3 targetVec)
        //{

        //    Vector2 output = new Vector2();

        //    Vector3 vAxisY = new Vector3();
        //    Vector3 vAxisZ = new Vector3();
        //    Vector3 vAxisX = new Vector3();
        //    Vector3 Delta = new Vector3();
        //    Vector3 Transformed = new Vector3();

        //    FRotator rotationCopy = new FRotator
        //    {
        //        Pitch = rotation.Pitch,
        //        Yaw = rotation.Yaw,
        //        Roll = rotation.Roll
        //    };

        //    GetAxes(rotationCopy, ref vAxisX, ref vAxisY, ref vAxisZ);

        //    Delta = targetVec - playerPos;
        //    Transformed.X = VectorDotProduct(Delta, vAxisY);
        //    Transformed.Y = VectorDotProduct(Delta, vAxisZ);
        //    Transformed.Z = VectorDotProduct(Delta, vAxisX);

        //    if (Transformed.Z < 1.00f)
        //        Transformed.Z = 1.00f;

        //    //float FOVAngle = PlayerCamera.LastCamFOV; //we have this already NP

        //    output.X = (float)((wndSize.X / 2.0f) + Transformed.X * ((wndSize.X / 2.0f) / Math.Tan(FOV * Math.PI / 360.0f)) / Transformed.Z);
        //    output.Y = (float)((wndSize.Y / 2.0f) + -Transformed.Y * ((wndSize.X / 2.0f) / Math.Tan(FOV * Math.PI / 360.0f)) / Transformed.Z);

        //    return output;
        //}
        //private static void GetAxes(FRotator r, ref Vector3 x, ref Vector3 y, ref Vector3 z)
        //{
        //    Vector3 rVec = RotatorToVector(r);
        //    rVec.Normalize();
        //    x = rVec;
        //    r.Yaw += 16384;
        //    FRotator r2 = r;
        //    r2.Pitch = 0;
        //    rVec = RotatorToVector(r2);
        //    rVec.Normalize();
        //    y = rVec;
        //    y.Z = 0.0f;
        //    r.Yaw -= 16384;
        //    r.Pitch += 16384;
        //    rVec = RotatorToVector(r);
        //    rVec.Normalize();
        //    z = rVec;
        //}
        //public static Vector3 RotatorToVector(FRotator R)
        //{
        //    float UROTTORAD = 0.00009587379924285f;//(int)Math.PI * 32768;
        //    Vector3 Vec = new Vector3();
        //    float fYaw = R.Yaw * UROTTORAD;
        //    float fPitch = R.Pitch * UROTTORAD;
        //    float CosPitch = (float)Math.Cos(fPitch);
        //    Vec.X = (float)Math.Cos(fYaw) * CosPitch;
        //    Vec.Y = (float)Math.Sin(fYaw) * CosPitch;
        //    Vec.Z = (float)Math.Sin(fPitch);
        //    return Vec;
        //}
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
            //var LocalPlayer = Memory.ReadPointer(processHandle, (IntPtr)(GameBase.ToInt64() + 0x02306658), isWow64Process);
            //RocketLeague.exe+230E658
            //So we got localplayer.. we wanna reach camera (to use the functions that we Boost from c++ after converting them)
            //So now what we need?

            var PlayerCamera = Memory.ReadPointer(processHandle, (IntPtr)(PlayerController.ToInt64() + 0x0480), isWow64Process);

            //So we reached player camera .. let's get Location / LastCamFOV / Rotation

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

            //var Boost = Memory.ReadInt32(processHandle, (IntPtr)GameShare.ToInt64() + 0x0080);

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
                //var curPill = Pills[r];
                //var PillVec1 = WorldToScreenUE3(rotator, LastCamFov, Location, curPill);
                //Renderer.DrawText("Pill", PillVec1, Color.DeepSkyBlue, 35, TextAlignment.centered, true);

                //Console.WriteLine(r);

                // This is the Pill Array 6 pointers == 6 Pills
                var currentBoost = Memory.ReadPointer(processHandle, (IntPtr)BoostsArray.ToInt64() + (r * 0x8), isWow64Process);

                var boostPos = Memory.ReadVector3(processHandle, (IntPtr)currentBoost.ToInt64() + 0x0090);
                //var PillVecOnScreen = WorldToScreenUE3(rotator, LastCamFov, Location, boostPos);

                string nameOrTime = "Pill" + (r + 1);

                if (!BoostsObjects.Contains(currentBoost.ToInt64()))
                    BoostsObjects.Add(currentBoost.ToInt64());


                //Renderer.DrawText(nameOrTime, PillVecOnScreen, Color.DeepSkyBlue, 35, TextAlignment.centered, true);

                //var Boost1 = Memory.ReadPointer(processHandle, (IntPtr)(BoostA.ToInt64() + 0x0000), isWow64Process);
                //var Pill1 = Memory.ReadVector3(processHandle, (IntPtr)Boost1.ToInt64() + 0x0090);
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
                //var PillVecOnScreen = WorldToScreenUE3(rotator, LastCamFov, Location, boostPos);
                Vector2 PillVecOnScreen = new Vector2(0, 0);
                if (Renderer.WorldToScreenUE3(boostPos, out PillVecOnScreen, Location, rotator.Pitch, rotator.Yaw, rotator.Roll, LastCamFov, wndMargins, wndSize))
                {
                    Renderer.DrawText(timeLeftStr, PillVecOnScreen, Color.DeepSkyBlue, 35, TextAlignment.centered, true);
                }

            }

            //var foundVec = WorldToScreenUE3(rotator, LastCamFov, Location, new Vector3(x: -1771, y: -821, z: 63)); //Just dummy Vector so see on world..

            //var foundVec2 = WorldToScreenUE3(rotator, LastCamFov, Location, new Vector3(x: -1500, y: -821, z: 63));
            //if (foundVec.X > 0 && foundVec.Y > 0)
            //    if (foundVec2.X > 0 && foundVec2.Y > 0)
            //        Renderer.DrawText("ONWORLDSO 111 ", foundVec, Color.DeepSkyBlue, 50, TextAlignment.centered, true);
            //Renderer.DrawText("ONWORL 2222", foundVec2, Color.DeepSkyBlue, 50, TextAlignment.centered, true);



            //var BoostVec1 = WorldToScreenUE3(rotator, LastCamFov, Location, Pill1);
            //{
            //    if (BoostVec1.X > 0 && BoostVec1.Y > 0)
            //        Renderer.DrawText("1", BoostVec1, Color.DeepSkyBlue, 50, TextAlignment.centered, true);      //does not stay in same spot. moves to dif boost target????????????????????
            //}


            //var BoostVec2 = WorldToScreenUE3(rotator, LastCamFov, Location, Pill2);
            //{
            //    if (BoostVec2.X > 0 && BoostVec2.Y > 0)
            //        Renderer.DrawText("2", BoostVec2, Color.DeepSkyBlue, 50, TextAlignment.centered, true);     //does not stay in same spot. moves to dif boost target????????????????????
            //}

            //var BoostVec3 = WorldToScreenUE3(rotator, LastCamFov, Location, Pill3);
            //{
            //    if (BoostVec3.X > 0 && BoostVec3.Y > 0)
            //        Renderer.DrawText("3", BoostVec3, Color.DeepSkyBlue, 50, TextAlignment.centered, true);    //does not stay in same spot. moves to dif boost target????????????????????
            //}

            //var BoostVec4 = WorldToScreenUE3(rotator, LastCamFov, Location, Pill4);
            //{
            //    if (BoostVec4.X > 0 && BoostVec4.Y > 0)
            //        Renderer.DrawText("4", BoostVec4, Color.DeepSkyBlue, 50, TextAlignment.centered, true);   //does not stay in same spot. moves to dif boost target????????????????????
            //}

            //var BoostVec5 = WorldToScreenUE3(rotator, LastCamFov, Location, Pill5);
            //{
            //    if (BoostVec5.X > 0 && BoostVec5.Y > 0)
            //        Renderer.DrawText("5", BoostVec5, Color.DeepSkyBlue, 50, TextAlignment.centered, true); // does not stay in same spot. moves between 2 dif locations upon removal
            //}

            //var BoostVec6 = WorldToScreenUE3(rotator, LastCamFov, Location, Pill6);
            //{
            //    if (BoostVec6.X > 0 && BoostVec6.Y > 0)
            //        Renderer.DrawText("6", BoostVec6, Color.DeepSkyBlue, 50, TextAlignment.centered, true);   //Stays in same place as it should.... Let's see why xD
            //}

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        }




    }
    public class FRotator
    {
        public int Pitch;
        public int Yaw;
        public int Roll;
    }
}
