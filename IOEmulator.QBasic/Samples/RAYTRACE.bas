SCREEN 13
COLOR 15, 0
CLS

' Ray marching demo: sphere with basic shading
' Fixed-point scale
S = 1000

' Camera position
CX = 0
CY = 0
CZ = -5 * S

' Sphere center
SX = 0
SY = 0
SZ = 0

' Sphere radius
R = S

' Light position
LX = 2 * S
LY = 2 * S
LZ = -2 * S

' Max distance to march
MAXDIST = 10 * S

' Max steps
STEPS = 100

print "Ray marching demo..."
' For each pixel
FOR Y = 0 TO 199
FOR X = 0 TO 319
print "Rendering pixel ("; X; ","; Y; ")"
  ' Compute ray direction (unnormalized)
  SXMIN = -16 * S / 10
  SXMAX = 16 * S / 10
  SYMIN = -10 * S / 10
  SYMAX = 10 * S / 10
  DXF = SXMIN + (SXMAX - SXMIN) * X / 320
  DYF = SYMIN + (SYMAX - SYMIN) * (199 - Y) / 200
  RX = DXF
  RY = DYF
  RZ = S  ' towards Z=0

  ' Normalize ray direction
  RLEN = SQR(RX * RX + RY * RY + RZ * RZ)
  IF RLEN = 0 THEN RLEN = 1
  RX = RX * S / RLEN
  RY = RY * S / RLEN
  RZ = RZ * S / RLEN

  ' March along ray
  PosX = CX
  PosY = CY
  PosZ = CZ
  HIT = 0
  FOR STP = 1 TO STEPS
    DX = PosX - SX
    DY = PosY - SY
    DZ = PosZ - SZ
    DIST = SQR(DX * DX + DY * DY + DZ * DZ) - R
    IF DIST <= 0 THEN HIT = 1: GOTO MARCHEND
    PosX = PosX + RX * DIST / S
    PosY = PosY + RY * DIST / S
    PosZ = PosZ + RZ * DIST / S
    IF SQR(PosX * PosX + PosY * PosY + PosZ * PosZ) > MAXDIST THEN GOTO MARCHEND
  NEXT STP
  MARCHEND:

  IF HIT = 0 THEN
    COL = 0
    GOTO NEXTPIX
  END IF

  ' HIT: Compute normal
  NX = DX
  NY = DY
  NZ = DZ
  NLEN = SQR(NX * NX + NY * NY + NZ * NZ)
  IF NLEN = 0 THEN NLEN = 1
  NX = NX * S / NLEN
  NY = NY * S / NLEN
  NZ = NZ * S / NLEN

  ' Light direction
  L2X = LX - PosX
  L2Y = LY - PosY
  L2Z = LZ - PosZ
  LLEN = SQR(L2X * L2X + L2Y * L2Y + L2Z * L2Z)
  IF LLEN = 0 THEN LLEN = 1
  L2X = L2X * S / LLEN
  L2Y = L2Y * S / LLEN
  L2Z = L2Z * S / LLEN

  ' Diffuse shading
  DOT = (NX * L2X + NY * L2Y + NZ * L2Z) / S
  IF DOT < 0 THEN DOT = 0
  COL = DOT * 255 / S
  IF COL > 255 THEN COL = 255
  NEXTPIX:
  PSET X, Y, COL
NEXT X
NEXT Y

END