SCREEN 13
COLOR 15, 0
CLS

' Mandelbrot fractal (progressive render, integer fixed-point)
' - No sound
' - Integer math only; fixed-point scale S
' - Renders a few scanlines per frame; use Speed menu to accelerate

' Fixed-point scale
S = 10000

' Zoom state (fixed-point): center and half-spans
CXF = -7500 ' center X = -0.75 (prettier spot)
CYF = 1000  ' center Y = 0.1
SPANX = 20000 ' half-width = 2.0
SPANY = 20000 ' half-height = 2.0

' Zoom factor per full frame (e.g., 999/1000 => 0.1% in)
ZN = 999
ZD = 1000

' Derived per-frame view and steps (initialized; recomputed each frame)
XMIN = 0
XMAX = 0
YMIN = 0
YMAX = 0
DX = 0
DY = 0

' Iteration limit and bailout radius^2 (in fixed-point). Since ZR2+ZI2 is scaled by S,
' compare against 4*S (not 4*S*S).
MAXIT = 64
R2 = 4 * S

' State: current row index
ROW = 0

MAIN:
  ' Recompute view when starting a new frame
  ' IF ROW = 0 THEN GOTO UPDATEVIEW  ' Removed to fix infinite loop

  ' Draw N scanlines per frame for smooth progressive update
  N = 4
  K = 0
NEXTROW:
  YY = ROW + K
  IF YY >= 200 THEN GOTO ADVANCE
  CRI = YMIN + YY * DY
  XX = 0
NEXTCOL:
  ' 2x2 pixel block for speed
  CR = XMIN + XX * DX
  ZR = 0
  ZI = 0
  I = 0
ITER:
  ZR2 = (ZR * ZR) / S
  ZI2 = (ZI * ZI) / S
  MAG = ZR2 + ZI2
  IF MAG > R2 THEN GOTO COLOR
  IF I >= MAXIT THEN GOTO COLOR
  T = (2 * ZR * ZI) / S
  ZR = ZR2 - ZI2 + CR
  ZI = T + CRI
  I = I + 1
  GOTO ITER
COLOR:
  ' Map iterations to color (simple gradient); inside set -> black
  IF I >= MAXIT THEN COL = 0
  COL = (I * 255) / MAXIT
  IF COL < 0 THEN COL = 0
  IF COL > 255 THEN COL = 255
  PSET XX, YY, COL
  PSET XX+1, YY, COL
  PSET XX, YY+1, COL
  PSET XX+1, YY+1, COL
  XX = XX + 2
  IF XX < 320 THEN GOTO NEXTCOL
ADVANCE:
  K = K + 2
  IF K < N THEN GOTO NEXTROW
  ROW = ROW + N
  IF ROW >= 200 THEN GOTO ENDFRAME
  'SLEEP 0
  GOTO MAIN

UPDATEVIEW:
  XMIN = CXF - SPANX
  XMAX = CXF + SPANX
  YMIN = CYF - SPANY
  YMAX = CYF + SPANY
  DX = (XMAX - XMIN) / 320
  DY = (YMAX - YMIN) / 200
  ' Prevent zero step due to integer division at deep zoom
  IF DX = 0 THEN DX = 1
  IF DY = 0 THEN DY = 1
  GOTO MAIN

ENDFRAME:
  ROW = 0
  ' Apply zoom-in
  SPANX = (SPANX * ZN) / ZD
  SPANY = (SPANY * ZN) / ZD
  ' If too deep, reset to initial view to loop
  IF SPANX < 50 THEN SPANX = 20000
  IF SPANY < 50 THEN SPANY = 20000
  GOTO UPDATEVIEW
END
