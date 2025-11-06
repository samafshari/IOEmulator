SCREEN 13
COLOR 15, 0

print "Simple Raycasting Maze Renderer in QBasic"
' Fixed-point scale
S = 1000

' Maze dimensions
MW = 20
MH = 20
DIM MAZE(MW-1, MH-1) AS INTEGER

' Generate simple maze: border walls, some internal
FOR I = 0 TO MW-1
  MAZE(I, 0) = 1
  MAZE(I, MH-1) = 1
NEXT
FOR J = 0 TO MH-1
  MAZE(0, J) = 1
  MAZE(MW-1, J) = 1
NEXT
' Add some walls
FOR I = 5 TO 15: MAZE(I, 5) = 1: NEXT
FOR I = 5 TO 15: MAZE(I, 10) = 1: NEXT
FOR I = 5 TO 15: MAZE(I, 15) = 1: NEXT
FOR J = 5 TO 15: MAZE(5, J) = 1: NEXT
FOR J = 5 TO 15: MAZE(10, J) = 1: NEXT
FOR J = 5 TO 15: MAZE(15, J) = 1: NEXT

' Sin/Cos lookup tables (scaled by 100)
DIM SINL(359) AS INTEGER
DIM COSL(359) AS INTEGER
' Generate sin/cos tables at runtime
FOR I = 0 TO 359
  SINL(I) = SIN(I)
  COSL(I) = COS(I)
NEXT

' Player
PX = 10 * S
PY = 10 * S
ANGLE = 0
SPEED = 50
TURN_SPEED = 1

' Colors
CEILING_COLOR = 9 ' blue
WALL_COLOR = 7 ' white
GROUND_COLOR = 6 ' brown

MAIN_LOOP:
  ' Clear screen (optional, since we draw all)
  CLS

  FOR SX = 0 TO 319
    ' Ray angle (degrees)
    RAYANG = ANGLE + (SX - 160) * 2 ' 2 degrees per pixel
    ' Normalize to 0-359 using MOD for safety
    RAYANG = RAYANG MOD 360
    IF RAYANG < 0 THEN RAYANG = RAYANG + 360

    ' Cast ray
    DX = COSL(RAYANG)
    DY = SINL(RAYANG)
    RX = PX
    RY = PY
    DIST = 0
    HIT = 0
    WHILE DIST < 20000 AND HIT = 0
      RX = RX + DX * S / 100
      RY = RY + DY * S / 100
      MX = RX / S
      MY = RY / S
      ' Bounds and wall check using single-line IFs for interpreter compatibility
      IF MX < 0 OR MX >= MW OR MY < 0 OR MY >= MH THEN HIT = 1
      IF HIT = 0 THEN IF MAZE(MX, MY) = 1 THEN HIT = 1
      DIST = DIST + 1
    WEND

    ' Height based on distance
    IF DIST = 0 THEN DIST = 1
    HEIGHT = 20000 / DIST
    IF HEIGHT > 200 THEN HEIGHT = 200
    TOP = 100 - HEIGHT / 2
    BOTTOM = 100 + HEIGHT / 2

    ' Draw column
    FOR SY = 0 TO 199
      COL = WALL_COLOR
      IF SY < TOP THEN COL = CEILING_COLOR
      IF SY > BOTTOM THEN COL = GROUND_COLOR
      PSET SX, SY, COL
    NEXT SY
    ' Periodically yield to allow screen updates in long frames
    IF SX MOD 40 = 0 THEN SLEEP 0
  NEXT SX

  ' Move player
  PX = PX + COSL(ANGLE) * SPEED / 100
  PY = PY + SINL(ANGLE) * SPEED / 100
  ANGLE = ANGLE + TURN_SPEED
  ' Normalize angle to 0-359 using MOD
  ANGLE = ANGLE MOD 360
  IF ANGLE < 0 THEN ANGLE = ANGLE + 360

  ' Check collision (single-line IFs only)
  MX = PX / S
  MY = PY / S
  IF MX >= 0 AND MX < MW AND MY >= 0 AND MY < MH THEN IF MAZE(MX, MY) = 1 THEN PX = PX - COSL(ANGLE) * SPEED / 100 : PY = PY - SINL(ANGLE) * SPEED / 100

  SLEEP 0
  GOTO MAIN_LOOP

END