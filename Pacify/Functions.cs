using System;
using WeScriptWrapper;


namespace Pacify
{
    public class Functions
    {
        public static void Ppc()
        {
            
            //var UWorld = Memory.ReadPointer(Program.processHandle, Program.GWorldPtr, Program.isWow64Process);

            if (Program.GWorldPtr != IntPtr.Zero)
            {

                //Console.WriteLine("Inside");

                var UGameInstance = Memory.ReadPointer(Program.processHandle, (IntPtr)(Program.GWorldPtr.ToInt64() + Offsets.UE.UWorld.OwningGameInstance), true);
                if (UGameInstance != IntPtr.Zero)
                {
                    //Console.WriteLine("Inside");
                    var localPlayerArray = Memory.ReadPointer(Program.processHandle, (IntPtr)(UGameInstance.ToInt64() + Offsets.UE.UGameInstance.LocalPlayers), true);
                    if (localPlayerArray != IntPtr.Zero)
                    {
                        var ULocalPlayer = Memory.ReadPointer(Program.processHandle, localPlayerArray, true);
                        if (ULocalPlayer != IntPtr.Zero)
                        {
                            var ULocalPlayerControler = Memory.ReadPointer(Program.processHandle, (IntPtr)(ULocalPlayer.ToInt64() + Offsets.UE.UPlayer.PlayerController), true);

                            if (ULocalPlayerControler != IntPtr.Zero)
                            {
                                var Upawn = Memory.ReadPointer(Program.processHandle, (IntPtr)(ULocalPlayerControler.ToInt64() + Offsets.UE.APlayerController.AcknowledgedPawn), true);                                
                                //ControllerRotation = Memory.ReadPointer(Program.processHandle, (IntPtr)(ULocalPlayerControler.ToInt64() + Offsets.UE.AController.ControlRotation), true);
                                //var ULocalPlayerPawn = Memory.ReadPointer(processHandle, (IntPtr)(ULocalPlayerControler.ToInt64() + Offsets.UE.AController.Character), true);                               

                                var APlayerCameraManager = Memory.ReadPointer(Program.processHandle, (IntPtr)ULocalPlayerControler.ToInt64() + 0x2B0, true);
                                if (APlayerCameraManager != IntPtr.Zero)
                                {
                                    Program.FMinimalViewInfo_Location = Memory.ReadVector3(Program.processHandle, (IntPtr)APlayerCameraManager.ToInt64() + 0x1A20 + 0x0000);

                                    Program.FMinimalViewInfo_Rotation = Memory.ReadVector3(Program.processHandle, (IntPtr)APlayerCameraManager.ToInt64() + 0x1A20 + 0x000C);

                                    Program.FMinimalViewInfo_FOV = Memory.ReadFloat(Program.processHandle, (IntPtr)APlayerCameraManager.ToInt64() + 0x0230);
                                    


                                }

                            }

                        }
                    }


                }

            }
        }
    }
}
