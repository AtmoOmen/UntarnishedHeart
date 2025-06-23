using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Config;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using static UntarnishedHeart.Utils.PathFindHelper;

namespace UntarnishedHeart.Utils;

public unsafe class PathFindHelper : IDisposable
{
    public bool Enabled
    {
        get => RMIWalkHook.IsEnabled;
        set
        {
            if (value)
            {
                RMIWalkHook.Enable();
                RMIFlyHook.Enable();
            }
            else
            {
                RMIWalkHook.Disable();
                RMIFlyHook.Disable();
            }
        }
    }

    public bool    IsAutoMove      { get; set; }
    public Vector3 DesiredPosition { get; set; }
    public float   Precision       { get; set; } = 0.01f;

    private bool IsLegacyMode;

    private delegate bool RMIWalkIsInputEnabled(void* self);
    private readonly RMIWalkIsInputEnabled _rmiWalkIsInputEnabled1;
    private readonly RMIWalkIsInputEnabled _rmiWalkIsInputEnabled2;

    private static readonly CompSig RMIWalkSig = new("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D");
    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, 
                                          byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    private readonly Hook<RMIWalkDelegate>? RMIWalkHook;

    private static readonly CompSig RMIFlySig = new("E8 ?? ?? ?? ?? 0F B6 0D ?? ?? ?? ?? B8");
    private delegate void RMIFlyDelegate(void* self, PlayerMoveControllerFlyInput* result);
    private readonly Hook<RMIFlyDelegate>? RMIFlyHook;

    public PathFindHelper()
    {
        RMIWalkHook ??= DService.Hook.HookFromSignature<RMIWalkDelegate>(RMIWalkSig.Get(), RMIWalkDetour);
        RMIFlyHook ??= DService.Hook.HookFromSignature<RMIFlyDelegate>(RMIFlySig.Get(), RMIFlyDetour);

        var rmiWalkIsInputEnabled1Addr = DService.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 10 38 43 3C");
        var rmiWalkIsInputEnabled2Addr = DService.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 03 88 47 3F");

        _rmiWalkIsInputEnabled1 = Marshal.GetDelegateForFunctionPointer<RMIWalkIsInputEnabled>(rmiWalkIsInputEnabled1Addr);
        _rmiWalkIsInputEnabled2 = Marshal.GetDelegateForFunctionPointer<RMIWalkIsInputEnabled>(rmiWalkIsInputEnabled2Addr);

        DService.GameConfig.UiControlChanged += OnConfigChanged;
        UpdateLegacyMode();
    }

    public void Dispose()
    {
        DService.GameConfig.UiControlChanged -= OnConfigChanged;

        RMIWalkHook.Dispose();
        RMIFlyHook.Dispose();
    }

    private void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, 
                               byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        RMIWalkHook.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);

        var movementAllowed = bAdditiveUnk == 0 && _rmiWalkIsInputEnabled1(self) && _rmiWalkIsInputEnabled2(self);
        if (movementAllowed && (IsAutoMove || (*sumLeft == 0 && *sumForward == 0)) &&
            DirectionToDestination(false) is { } relDir)
        {
            var dir = relDir.h.ToDirection();
            *sumLeft = dir.X;
            *sumForward = dir.Y;
        }
    }

    private void RMIFlyDetour(void* self, PlayerMoveControllerFlyInput* result)
    {
        RMIFlyHook.Original(self, result);

        if ((IsAutoMove || result->Forward != 0 || result->Left != 0 || result->Up != 0) && 
            DirectionToDestination(true) is { } relDir)
        {
            var dir = relDir.h.ToDirection();
            result->Forward = dir.Y;
            result->Left = dir.X;
            result->Up = relDir.v.Rad;
        }
    }

    private (Angle h, Angle v)? DirectionToDestination(bool allowVertical)
    {
        var player = DService.ObjectTable.LocalPlayer;
        if (player == null)
            return null;

        var dist = DesiredPosition - player.Position;
        if (dist.LengthSquared() <= Precision * Precision)
            return null;

        var dirH = Angle.FromDirectionXZ(dist);
        var dirV = allowVertical ? Angle.FromDirection(new(dist.Y, new Vector2(dist.X, dist.Z).Length())) : default;

        var refDir = IsLegacyMode
            ? ((CameraEx*)CameraManager.Instance()->GetActiveCamera())->DirH.Radians() + 180.Degrees()
            : player.Rotation.Radians();
        return (dirH - refDir, dirV);
    }

    private void OnConfigChanged(object? sender, ConfigChangeEvent evt) => UpdateLegacyMode();

    private void UpdateLegacyMode()
        => IsLegacyMode = DService.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1;

    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public struct PlayerMoveControllerFlyInput
    {
        [FieldOffset(0x0)] public float Forward;
        [FieldOffset(0x4)] public float Left;
        [FieldOffset(0x8)] public float Up;
        [FieldOffset(0xC)] public float Turn;
        [FieldOffset(0x10)] public float u10;
        [FieldOffset(0x14)] public byte DirMode;
        [FieldOffset(0x15)] public byte HaveBackwardOrStrafe;
    }

    public struct Angle(float radians = 0)
    {
        public const float RadToDeg = 180 / MathF.PI;
        public const float DegToRad = MathF.PI / 180;

        public float Rad = radians;
        public readonly float Deg => Rad * RadToDeg;

        public static Angle FromDirection(Vector2 dir) => new(MathF.Atan2(dir.X, dir.Y));
        public static Angle FromDirection(float x, float z) => new(MathF.Atan2(x, z));
        public static Angle FromDirectionXZ(Vector3 dir) => new(MathF.Atan2(dir.X, dir.Z));
        public Vector2 ToDirection() => new(Sin(), Cos());
        public Vector3 ToDirectionXZ() => new(Sin(), 0, Cos());

        public static Angle operator +(Angle a, Angle b) => new(a.Rad + b.Rad);
        public static Angle operator -(Angle a, Angle b) => new(a.Rad - b.Rad);
        public static Angle operator -(Angle a) => new(-a.Rad);
        public static Angle operator *(Angle a, float b) => new(a.Rad * b);
        public static Angle operator *(float a, Angle b) => new(a * b.Rad);
        public static Angle operator /(Angle a, float b) => new(a.Rad / b);
        public Angle Abs() => new(Math.Abs(Rad));
        public float Sin() => MathF.Sin(Rad);
        public float Cos() => MathF.Cos(Rad);
        public float Tan() => MathF.Tan(Rad);
        public static Angle Asin(float x) => new(MathF.Asin(x));
        public static Angle Acos(float x) => new(MathF.Acos(x));

        public Angle Normalized()
        {
            var r = Rad;
            while (r < -MathF.PI)
                r += 2 * MathF.PI;
            while (r > MathF.PI)
                r -= 2 * MathF.PI;
            return new(r);
        }

        public bool AlmostEqual(Angle other, float epsRad) => Math.Abs((this - other).Normalized().Rad) <= epsRad;

        public static bool operator ==(Angle l, Angle r) => l.Rad == r.Rad;
        public static bool operator !=(Angle l, Angle r) => l.Rad != r.Rad;
        public override bool Equals(object? obj) => obj is Angle angle && this == angle;
        public override int GetHashCode() => Rad.GetHashCode();
        public override string ToString() => Deg.ToString("f0");
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x2B0)]
    public struct CameraEx
    {
        [FieldOffset(0x130)]
        public float DirH; // 0 is north, increases CW

        [FieldOffset(0x134)]
        public float DirV; // 0 is horizontal, positive is looking up, negative looking down

        [FieldOffset(0x138)]
        public float InputDeltaHAdjusted;

        [FieldOffset(0x13C)]
        public float InputDeltaVAdjusted;

        [FieldOffset(0x140)]
        public float InputDeltaH;

        [FieldOffset(0x144)]
        public float InputDeltaV;

        [FieldOffset(0x148)]
        public float DirVMin; // -85deg by default

        [FieldOffset(0x14C)]
        public float DirVMax; // +45deg by default
    }
}

public static class AngleExtensions
{
    public static Angle Radians(this float radians) => new(radians);
    public static Angle Degrees(this float degrees) => new(degrees * Angle.DegToRad);
    public static Angle Degrees(this int degrees) => new(degrees * Angle.DegToRad);
}
