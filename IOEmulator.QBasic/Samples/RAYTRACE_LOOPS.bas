SCREEN 13
COLOR 15, 0
CLS

' Nested loops demo: draw a small 10x10 grid in the top-left
FOR Y = 0 TO 9
FOR X = 0 TO 9
  PSET X, Y, 15
NEXT X
NEXT Y

END
