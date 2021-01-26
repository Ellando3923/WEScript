using System;
using SharpDX;
using WeScriptWrapper;


namespace DeadByDaylight
{
    public class Functions
    {
        public static void Ppc(out IntPtr ControllerRotation, out float Score, out IntPtr USkillCheck)
        {
            ControllerRotation = IntPtr.Zero;
            Score = 0;
            USkillCheck = IntPtr.Zero;
            //var UWorld = Memory.ZwReadPointer(processHandle, GWorldPtr, isWow64Process);

            if (Program.GWorldPtr != IntPtr.Zero)
            {
                var UGameInstance = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(Program.GWorldPtr.ToInt64() + Offsets.UE.UWorld.OwningGameInstance), true);
                if (UGameInstance != IntPtr.Zero)
                {
                    var localPlayerArray = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(UGameInstance.ToInt64() + Offsets.UE.UGameInstance.LocalPlayers), true);
                    if (localPlayerArray != IntPtr.Zero)
                    {
                        var ULocalPlayer = Memory.ZwReadPointer(Program.processHandle, localPlayerArray, true);
                        if (ULocalPlayer != IntPtr.Zero)
                        {
                            var ULocalPlayerControler = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(ULocalPlayer.ToInt64() + Offsets.UE.UPlayer.PlayerController), true);

                            if (ULocalPlayerControler != IntPtr.Zero)
                            {
                                var Upawn = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(ULocalPlayerControler.ToInt64() + Offsets.UE.APlayerController.AcknowledgedPawn), true);
                                var UplayerState = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(Upawn.ToInt64() + Offsets.UE.APawn.PlayerState), true);
                                Score = Memory.ZwReadFloat(Program.processHandle, (IntPtr)(UplayerState.ToInt64() + Offsets.UE.APlayerState.Score));



                                ControllerRotation = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(ULocalPlayerControler.ToInt64() + Offsets.UE.AController.ControlRotation), true);
                                //var ULocalPlayerPawn = Memory.ZwReadPointer(processHandle, (IntPtr)(ULocalPlayerControler.ToInt64() + Offsets.UE.AController.Character), true);

                                if (Upawn != IntPtr.Zero)
                                {
                                    var UInteractionHandler = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(Upawn.ToInt64() + Offsets.UE.UdbdPlayer._interactionHandler), true);

                                    if (UInteractionHandler != IntPtr.Zero)
                                    {
                                        USkillCheck = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(UInteractionHandler.ToInt64() + Offsets.UE.UPlayerInteractionHandler._skillCheck), true);
                                    }

                                }

                                var APlayerCameraManager = Memory.ZwReadPointer(Program.processHandle, (IntPtr)ULocalPlayerControler.ToInt64() + 0x2D0, true);
                                if (APlayerCameraManager != IntPtr.Zero)
                                {
                                    Program.FMinimalViewInfo_Location = Memory.ZwReadVector3(Program.processHandle, (IntPtr)APlayerCameraManager.ToInt64() + 0x1A80 + 0x0000);

                                    Program.FMinimalViewInfo_Rotation = Memory.ZwReadVector3(Program.processHandle, (IntPtr)APlayerCameraManager.ToInt64() + 0x1A80 + 0x000C);

                                    Program.FMinimalViewInfo_FOV = Memory.ZwReadFloat(Program.processHandle, (IntPtr)APlayerCameraManager.ToInt64() + 0x1A80 + 0x0018);

                                    
                                }

                            }

                        }
                    }


                }
            }
        }
    }
}
