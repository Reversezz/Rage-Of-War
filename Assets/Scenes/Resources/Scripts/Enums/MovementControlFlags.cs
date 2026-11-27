using System;

[Flags]
public enum MovementControlFlags
{
    Idle,

    MoveForward = 1 << 0,
    MoveBackward = 1 << 1,
    MoveRight = 1 << 2,
    MoveLeft = 1 << 3,

    RunForward = 1 << 4,
    RunBackward = 1 << 5,
    RunRight = 1 << 6,
    RunLeft = 1 << 7
}