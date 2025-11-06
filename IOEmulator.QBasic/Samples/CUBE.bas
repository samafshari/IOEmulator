SCREEN 13
COLOR 15, 0
CLS

' Rotating wireframe cube (efficient)
' Uses integer math and LINE drawing only

S = 1000              ' fixed-point scale for geometry
F = 300               ' focal length in pixels
CAM = 5 * S           ' camera distance in world units
SIZE = 1200           ' half-size of cube in world units (1.2 * S)

' Cube vertices (-SIZE or +SIZE on each axis)
DIM VX(7) AS INTEGER
DIM VY(7) AS INTEGER
DIM VZ(7) AS INTEGER

VX(0) = -SIZE: VY(0) = -SIZE: VZ(0) = -SIZE
VX(1) =  SIZE: VY(1) = -SIZE: VZ(1) = -SIZE
VX(2) =  SIZE: VY(2) =  SIZE: VZ(2) = -SIZE
VX(3) = -SIZE: VY(3) =  SIZE: VZ(3) = -SIZE
VX(4) = -SIZE: VY(4) = -SIZE: VZ(4) =  SIZE
VX(5) =  SIZE: VY(5) = -SIZE: VZ(5) =  SIZE
VX(6) =  SIZE: VY(6) =  SIZE: VZ(6) =  SIZE
VX(7) = -SIZE: VY(7) =  SIZE: VZ(7) =  SIZE

' Edge list (12 edges)
DIM E(11, 1) AS INTEGER
E(0,0) = 0: E(0,1) = 1
E(1,0) = 1: E(1,1) = 2
E(2,0) = 2: E(2,1) = 3
E(3,0) = 3: E(3,1) = 0

E(4,0) = 4: E(4,1) = 5
E(5,0) = 5: E(5,1) = 6
E(6,0) = 6: E(6,1) = 7
E(7,0) = 7: E(7,1) = 4

E(8,0) = 0: E(8,1) = 4
E(9,0) = 1: E(9,1) = 5
E(10,0) = 2: E(10,1) = 6
E(11,0) = 3: E(11,1) = 7

' Transformed 2D screen coordinates
DIM SX(7) AS INTEGER
DIM SY(7) AS INTEGER
DIM D(7) AS INTEGER ' per-vertex depth (Z2 + CAM)

ANGY = 0
ANGX = 20

DO
  CLS
  ' Precompute rotation trig (scaled by 100 per SIN/COS semantics)
  CAY = COS(ANGY): SAY = SIN(ANGY)
  CAX = COS(ANGX): SAX = SIN(ANGX)

  ' Transform and project each vertex
  FOR I = 0 TO 7
    X0 = VX(I)
    Y0 = VY(I)
    Z0 = VZ(I)

    ' Rotate around Y (right-handed, camera looking +Z):
    ' x' = x*cos - z*sin
    ' z' = x*sin + z*cos
    XR = (X0 * CAY - Z0 * SAY) \ 100
    ZR = (X0 * SAY + Z0 * CAY) \ 100

    ' Rotate around X: affects Y and Z
    YR = (Y0 * CAX - ZR * SAX) \ 100
    Z2 = (Y0 * SAX + ZR * CAX) \ 100

  DEN = Z2 + CAM
  IF DEN < 1 THEN DEN = 1
  D(I) = DEN

    ' Project to screen (160,100 is center)
    SX(I) = 160 + (XR * F) \ DEN
    SY(I) = 100 - (YR * F) \ DEN
  NEXT I

  ' Compute depth range for gradient
  MINZ = D(0)
  MAXZ = D(0)
  FOR I = 1 TO 7
    IF D(I) < MINZ THEN MINZ = D(I)
    IF D(I) > MAXZ THEN MAXZ = D(I)
  NEXT I
  RANGEZ = MAXZ - MINZ
  IF RANGEZ < 1 THEN RANGEZ = 1

  ' Draw edges with depth-based color (near = brighter)
  FOR EI = 0 TO 11
    A = E(EI, 0)
    B = E(EI, 1)
    AVGZ = (D(A) + D(B)) \ 2
    C = 32 + (MAXZ - AVGZ) * 223 \ RANGEZ ' map depth to [32..255]
    IF C < 1 THEN C = 1
    IF C > 255 THEN C = 255
    LINE SX(A), SY(A), SX(B), SY(B), C
  NEXT EI

  ' Advance rotation
  ANGY = ANGY + 3: IF ANGY >= 360 THEN ANGY = ANGY - 360
  ANGX = ANGX + 2: IF ANGX >= 360 THEN ANGX = ANGX - 360

  sleep 0.05
LOOP

END
