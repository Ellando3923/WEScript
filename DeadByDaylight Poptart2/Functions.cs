using System;
using WeScriptWrapper;


namespace DeadByDaylight
{
    public class Functions
    {
        public static void Ppc(out IntPtr ControllerRotation, out float Score, out UInt32 _currentHealthStateCount, out UInt32 Name, out IntPtr USkillCheck)
        {
            ControllerRotation = IntPtr.Zero;
            Score = 0;
            USkillCheck = IntPtr.Zero;
            Name = 0;
            _currentHealthStateCount = 0;
            //var UWorld = Memory.ZwReadPointer(processHandle, GWorldPtr, isWow64Process);

            if (Program.GWorldPtr != IntPtr.Zero)
            {
                var gameState = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(Program.GWorldPtr.ToInt64() + Offsets.UE.UWorld.gameState), true);
                var _meatHooks = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._meatHooks), true);
                var _baseTraps = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._baseTraps), true);
                var _escapeDoors = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._escapeDoors), true);
                var _generators = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._generators), true);
                var _hatches = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._hatches), true);
                var _lockers = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._lockers), true);
                var _pallets = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._pallets), true);
                var _searchables = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._searchables), true);
                var _totems = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._totems), true);
                var _windows = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(gameState.ToInt64() + Offsets.UE.GameState._windows), true);

                

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
                                Name = Memory.ZwReadUInt32(Program.processHandle, (IntPtr)(UplayerState.ToInt64() + Offsets.UE.APlayerState.Name));


                                ControllerRotation = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(ULocalPlayerControler.ToInt64() + Offsets.UE.AController.ControlRotation), true);
                                //var ULocalPlayerPawn = Memory.ZwReadPointer(processHandle, (IntPtr)(ULocalPlayerControler.ToInt64() + Offsets.UE.AController.Character), true);

                                if (Upawn != IntPtr.Zero)
                                {
                                    var UInteractionHandler = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(Upawn.ToInt64() + Offsets.UE.UdbdPlayer._interactionHandler), true);

                                    if (UInteractionHandler != IntPtr.Zero)
                                    {
                                        USkillCheck = Memory.ZwReadPointer(Program.processHandle, (IntPtr)(UInteractionHandler.ToInt64() + Offsets.UE.UPlayerInteractionHandler._skillCheck), true);
                                        Console.WriteLine(USkillCheck.ToString("X"));
                                        _currentHealthStateCount = Memory.ZwReadUInt32(Program.processHandle, (IntPtr)(UInteractionHandler.ToInt64() + Offsets.UE.UPlayerInteractionHandler._currentHealthStateCount));
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
