SCREEN 13
COLOR 15, 0
CLS

W = 320: H = 200

' Paddle
PX = 10: PY = H \ 2 - 15: PH = 30: PW = 3

' Ball
BX = W \ 2: BY = H \ 2: VX = 2: VY = 1

DO
  CLS
  ' Input: arrow keys to move paddle
  IF KEY("UP") THEN PY = PY - 2
  IF KEY("DOWN") THEN PY = PY + 2
  IF PY < 0 THEN PY = 0
  IF PY + PH >= H THEN PY = H - PH - 1

  ' Move ball
  BX = BX + VX: BY = BY + VY
  IF BY <= 0 THEN BY = 0: VY = -VY
  IF BY >= H-1 THEN BY = H-1: VY = -VY

  ' Paddle collision
  IF BX <= PX + PW AND BY >= PY AND BY <= PY + PH THEN
    BX = PX + PW + 1
    VX = -VX
  END IF
  ' Right wall bounce
  IF BX >= W-1 THEN BX = W-1: VX = -VX

  ' Draw paddle
  FOR I = 0 TO PH
    PSET PX, PY + I, 14
    PSET PX + 1, PY + I, 14
    PSET PX + 2, PY + I, 14
  NEXT I

  ' Draw ball
  PSET BX, BY, 12

  SLEEP 0.01
LOOP

END
