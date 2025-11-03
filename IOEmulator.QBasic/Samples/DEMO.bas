SCREEN 13
COLOR 15, 0
CLS

' DEMOSCENE Plasma + Beat (integer math, no MOD)
' Notes:
' - SIN/COS return scaled values (-100..100)
' - Angles are in degrees
' - No FOR/NEXT; use labels + IF / GOTO
' - No MOD operator; use integer division trick: (K*D = N) means N divisible by D

T = 0

MAIN:
CLS

' --- Plasma effect (blocky for speed) ---
Y = 0
YLOOP:
  X = 0
XLOOP:
  A = (X + T) / 4
  B = (Y + T) / 3
  C1 = SIN(A)
  C2 = COS(B)
  C3 = SIN((X + Y + T) / 5)
  COL = C1 + C2 + C3 + 128 ' base bias
  IF COL < 0 THEN COL = 0
  IF COL > 255 THEN COL = 255
  PSET X, Y, COL
  PSET X+1, Y, COL
  PSET X, Y+1, COL
  PSET X+1, Y+1, COL
  X = X + 2
  IF X < 320 THEN GOTO XLOOP
  Y = Y + 2
  IF Y < 200 THEN GOTO YLOOP

' --- Overlay simple moving lines ---
CX = 160
CY = 100
R = 40 + (SIN(T*2) + 100) / 5
ANG = T * 3
' Endpoints using simple trig rings
LX = CX + (COS(ANG) * R) / 50
LY = CY + (SIN(ANG) * R) / 50
LINE 0,0, LX,LY, 14
LINE 319,0, 319-LX,LY, 12
LINE 0,199, LX,199-LY, 10
LINE 319,199, 319-LX,199-LY, 11

' --- Beat: fire tones when T divisible by 20/40 ---
K20 = T / 20
IF K20 * 20 = T THEN SOUND 200 + (SIN(T*9) + 100), 50
K40 = T / 40
IF K40 * 40 = T THEN SOUND 400 + (COS(T*7) + 100), 50

T = T + 1
' short nap: integer seconds only; zero returns immediately
SLEEP 0
GOTO MAIN
END
