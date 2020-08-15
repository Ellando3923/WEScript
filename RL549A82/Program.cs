using SharpDX;
using System;
using WeScript.SDK.UI;
using WeScript.SDK.UI.Components;
using WeScriptWrapper;

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
        public static IntPtr GameSize = IntPtr.Zero;


        public static Menu RootMenu { get; private set; }
        public static Menu VisualsMenu { get; private set; }

        class Components
        {
            public static readonly MenuKeyBind MainAssemblyToggle = new MenuKeyBind("mainassemblytoggle", "Toggle the whole assembly effect by pressing key:", VirtualKeyCode.Delete, KeybindType.Toggle, true);
            public static class VisualsComponent
            {
                public static readonly MenuBool DrawTheVisuals = new MenuBool("drawthevisuals", "Enable all of the Visuals", true);
                public static readonly MenuSlider DrawBoxThic = new MenuSlider("boxthickness", "Draw Box Thickness", 0, 0, 10);
                public static readonly MenuBool DrawBoxBorder = new MenuBool("drawboxborder", "Draw Border around Box and Text?", true);
                //public static readonly MenuSlider OffsetGuesser = new MenuSlider("ofsgues", "Guess the offset", 10, 1, 250);
            }
        }

        public static void InitializeMenu()
        {
            VisualsMenu = new Menu("visualsmenu", "Visuals Menu")
            {
                
                Components.VisualsComponent.DrawBoxThic.SetToolTip("Setting thickness to 0 will let the assembly auto-adjust itself depending on model distance"),
                Components.VisualsComponent.DrawBoxBorder.SetToolTip("Drawing borders may take extra performance (FPS) on low-end computers"),
                //Components.VisualsComponent.OffsetGuesser,
            };


            RootMenu = new Menu("RocketLeague", "WeScript.app RocketLeague 1.6 Assembly", true)
            {
                Components.MainAssemblyToggle.SetToolTip("The magical boolean which completely disables/enables the assembly!"),
                VisualsMenu,
            };
            RootMenu.Attach();
        }




        static void Main(string[] args)
        {
            Console.WriteLine("WeScript.app RocketLeague 1.6 Loaded!");
            InitializeMenu();
            
            Memory.OnTick += OnTick;
        }

        

        private static void OnTick(int counter, EventArgs args)
        {
            if (processHandle == IntPtr.Zero) //if we still don't have a handle to the process
            {
                var wndHnd = Memory.FindWindowName("RocketLeague"); //why the devs added spaces after the name?!
                if (wndHnd != IntPtr.Zero) //if it exists
                {
                    //Console.WriteLine("weheree");
                    var calcPid = Memory.GetPIDFromHWND(wndHnd); //get the PID of that same process
                    if (calcPid > 0) //if we got the PID
                    {
                        processHandle = Memory.OpenProcess(PROCESS_ALL_ACCESS, calcPid); //the driver will get a stripped handle, but doesn't matter, it's still OK
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
                var wndHnd = Memory.FindWindowName("RocketLeague"); //why the devs added spaces after the name?!
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
                        GameBase = Memory.GetModule(processHandle, null, isWow64Process); //if not, find it
                    }
                    else
                    {
                        if (GameSize == IntPtr.Zero)
                        {
                            GameSize = Memory.GetModuleSize(processHandle, null, isWow64Process);
                        }
                        else
                        {
                            Console.WriteLine($"GameBase: {GameBase.ToString("X")}"); //easy way to check if we got reading rights
                            Console.WriteLine($"GameSize: {GameSize.ToString("X")}"); //easy way to check if we got reading rights
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
                }
            }
        }

        
    }
}