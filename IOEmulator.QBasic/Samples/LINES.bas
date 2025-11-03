SCREEN 13
COLOR 15, 0
CLS

' Simple line-based demoscene animation (no sound)
' - Uses integer SIN/COS that return values in range -100..100
' - Angles are in degrees
' - No FOR/NEXT; uses label loops
' - SLEEP 0 yields between frames; use the app Speed menu to accelerate

T = 0

MAIN:
' Do not clear the screen every frame; periodically reset to avoid fade-to-white
K120 = T / 120
IF K120 * 120 = T THEN CLS

CX = 160
CY = 100
' Base radius slowly pulses
R = 60 + (SIN(T*3) + 100) / 4   ' range roughly 60..110

' Number of elements
N = 64
I = 0
LOOPI:
	' Two angular phases for chord lines (lissajous-style)
	ANG1 = T*3 + I*7
	ANG2 = T*5 + I*11
	' Slight independent wobble radii
	RX1 = R + (SIN(T*2 + I*5) + 100) / 6
	RY1 = R + (COS(T*2 + I*7) + 100) / 6
	RX2 = R + (SIN(T*3 + I*4) + 100) / 7
	RY2 = R + (COS(T*4 + I*6) + 100) / 7
	' Endpoints
	X1 = CX + (COS(ANG1) * RX1) / 50
	Y1 = CY + (SIN(ANG1) * RY1) / 50
	X2 = CX + (COS(ANG2) * RX2) / 50
	Y2 = CY + (SIN(ANG2) * RY2) / 50
	' Color: cyclic, non-monotonic mix of trigs to avoid bleaching out
	COL = 128 + SIN(I*8 + T*5) + COS(I*5 + T*7) + SIN(T*3)
	IF COL < 0 THEN COL = 0
	IF COL > 255 THEN COL = 255
	' Draw chord and a spoke for extra detail
	LINE X1, Y1, X2, Y2, COL
	LINE CX, CY, X1, Y1, COL
	I = I + 1
	IF I < N THEN GOTO LOOPI

T = T + 1
SLEEP 0
GOTO MAIN
END
