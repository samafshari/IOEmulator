SCREEN 13
COLOR 15,1
CLS
PRINT "Input test: type to echo. Close window to quit."
A: IF INKEY$ <> "" THEN GOTO B
GOTO A
B: PRINT LASTKEY$
GOTO A
