SCREEN 13
COLOR 15, 0
CLS

' Show a crosshair pointer at the mouse location
DO
  CLS
  X = MOUSEX()
  Y = MOUSEY()
  ' Crosshair size
  S = 5
  LINE X-S, Y, X+S, Y, 14
  LINE X, Y-S, X, Y+S, 14
  ' Draw a small box when left button is held
  IF MOUSE_LEFT() THEN
    LINE X-3, Y-3, X+3, Y-3, 12
    LINE X+3, Y-3, X+3, Y+3, 12
    LINE X+3, Y+3, X-3, Y+3, 12
    LINE X-3, Y+3, X-3, Y-3, 12
  END IF
  SLEEP 0.02
LOOP

END
