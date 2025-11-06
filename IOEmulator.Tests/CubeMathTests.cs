using System;
using Xunit;

namespace Neat.Test;

public class CubeMathTests
{
    // Helper: runs a minimal QBASIC program that computes projection from one point
    // using the same equations as Samples/CUBE.bas and PSETs the projected pixel.
    private static void RunProjectionAndMarkPixel(int x0, int y0, int z0, int angY, int angX, out IOEmulator io)
    {
        io = new IOEmulator();
        var qb = new QBasicApi(io);
        var interp = new QBasicInterpreter(qb);
        // Angles in degrees; SIN/COS return scaled by 100 in interpreter
        // Fixed-point params follow CUBE.bas defaults
        string src = $@"SCREEN 13
COLOR 15,0
CLS
S = 1000
F = 300
CAM = 4 * S
X0 = {x0}
Y0 = {y0}
Z0 = {z0}
ANGY = {angY}
ANGX = {angX}
CAY = COS(ANGY): SAY = SIN(ANGY)
CAX = COS(ANGX): SAX = SIN(ANGX)
XR = (X0 * CAY - Z0 * SAY) \ 100
ZR = (X0 * SAY + Z0 * CAY) \ 100
YR = (Y0 * CAX - ZR * SAX) \ 100
Z2 = (Y0 * SAX + ZR * CAX) \ 100
DEN = Z2 + CAM
IF DEN < 1 THEN DEN = 1
SX = 160 + (XR * F) \ DEN
SY = 100 - (YR * F) \ DEN
PSET SX, SY, 15
";
        interp.Run(src);
    }

    [Fact]
    public void ZeroRotation_PointOnX_Axes_ProjectsRightOfCenter()
    {
        // With ANGX=0, ANGY=0 and point at (S,0,0), expected SX = 160 + (S*F)/CAM = 160 + 75 = 235
        // SY stays 100
        RunProjectionAndMarkPixel(x0: 1000, y0: 0, z0: 0, angY: 0, angX: 0, out var io);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(235, 100));
    }

    [Fact]
    public void RotateY_90_FromXAxis_ProjectsToCenterX()
    {
        // Rotating (S,0,0) by 90 degrees around Y should land on +Z axis -> XR=0, projects to x center (160)
        RunProjectionAndMarkPixel(x0: 1000, y0: 0, z0: 0, angY: 90, angX: 0, out var io);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(160, 100));
    }

    [Fact]
    public void ZeroRotation_PointOnY_Axes_ProjectsUpFromCenter()
    {
        // With ANGX=0, ANGY=0 and point at (0,S,0), expected SY = 100 - (S*F)/CAM = 100 - 75 = 25
        RunProjectionAndMarkPixel(x0: 0, y0: 1000, z0: 0, angY: 0, angX: 0, out var io);
        var bg = io.GetColor(io.BackgroundColorIndex);
        Assert.NotEqual(bg, io.ReadPixelAt(160, 25));
    }
}
