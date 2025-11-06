SCREEN 13
COLOR 15, 0
CLS

W = 320: H = 200

' Playfield border (white)
LINE 0, 0, W - 1, 0, 15
LINE 0, H - 1, W - 1, H - 1, 15
LINE 0, 0, 0, H - 1, 15
LINE W - 1, 0, W - 1, H - 1, 15

' Paddle
PX = 10: PY = H \ 2 - 15: PH = 30: PW = 3

' Ball (3x3) centered at BX,BY
BX = W \ 2: BY = H \ 2: VX = 2: VY = 1

' Previous positions for smooth erase
PPX = PX: PPY = PY
PBX = BX: PBY = BY

' Inner bounds so 3x3 ball doesn't overlap the border
MINX = 2: MAXX = W - 3
MINY = 2: MAXY = H - 3

DO
  ' Input: arrow keys to move paddle
  IF KEY("UP") THEN PY = PY - 2
  IF KEY("DOWN") THEN PY = PY + 2
  IF PY < 1 THEN PY = 1
  IF PY + PH > H - 2 THEN PY = H - 2 - PH

  ' Move ball (center)
  BX = BX + VX: BY = BY + VY
  IF BY <= MINY THEN BY = MINY: VY = -VY
  IF BY >= MAXY THEN BY = MAXY: VY = -VY

  ' Paddle collision (account for 3px ball thickness)
  IF BX <= PX + PW + 1 AND BY >= PY - 1 AND BY <= PY + PH + 1 THEN
    BX = PX + PW + 2
    VX = -VX
  END IF
  ' Right wall bounce (keep ball inside border)
  IF BX >= MAXX THEN BX = MAXX: VX = -VX

  ' Erase previous paddle with black
  FOR I = 0 TO PH
    YI = PPY + I
    IF YI > 0 AND YI < H - 1 THEN
      PSET PPX, YI, 0
      PSET PPX + 1, YI, 0
      PSET PPX + 2, YI, 0
    END IF
  NEXT I

  ' Erase previous ball (3x3) with black; avoid erasing the white border
  FOR DY = -1 TO 1
    FOR DX = -1 TO 1
      EX = PBX + DX: EY = PBY + DY
      IF EX > 0 AND EX < W - 1 AND EY > 0 AND EY < H - 1 THEN PSET EX, EY, 0
    NEXT DX
  NEXT DY

  ' Draw paddle (yellow)
  FOR I = 0 TO PH
    PSET PX, PY + I, 14
    PSET PX + 1, PY + I, 14
    PSET PX + 2, PY + I, 14
  NEXT I

  ' Draw ball as 3x3 (red)
  FOR DY = -1 TO 1
    FOR DX = -1 TO 1
      X = BX + DX: Y = BY + DY
      IF X > 0 AND X < W - 1 AND Y > 0 AND Y < H - 1 THEN PSET X, Y, 12
    NEXT DX
  NEXT DY

  ' Save positions for next erase
  PPX = PX: PPY = PY
  PBX = BX: PBY = BY

  SLEEP 0.01
LOOP

END
