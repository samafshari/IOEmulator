SCREEN 13
COLOR 15,1
CLS

' Simple wavefront pathfinding visualization (gridless, expands in steps)
' Uses PC(x,y) to check occupancy; 0 means background (empty)

S = 6
SX = 12 : SY = 12
GX = 300 : GY = 180

' Start/Goal
PSET SX, SY, 10
PSET GX, GY, 12

CF = 14 : NF = 11
FOUND = 0

' Seed frontier at start
PSET SX, SY, CF

LOOP:
I = 0
YSCAN:
IF I >= 200 THEN GOTO ENDROW
J = 0
XSCAN:
IF J >= 320 THEN GOTO NEXTROW
IF PC(J, I) = CF THEN GOTO EXPAND ELSE GOTO ADVANCE

EXPAND:
IF PC(J+S, I) = 0 THEN PSET J+S, I, NF
IF PC(J-S, I) = 0 THEN PSET J-S, I, NF
IF PC(J, I+S) = 0 THEN PSET J, I+S, NF
IF PC(J, I-S) = 0 THEN PSET J, I-S, NF
IF J+S = GX THEN IF I = GY THEN FOUND = 1
IF J-S = GX THEN IF I = GY THEN FOUND = 1
IF J = GX THEN IF I+S = GY THEN FOUND = 1
IF J = GX THEN IF I-S = GY THEN FOUND = 1

ADVANCE:
J = J + S
GOTO XSCAN

NEXTROW:
I = I + S
GOTO YSCAN

ENDROW:
' Swap frontier colors
T = CF : CF = NF : NF = T

IF FOUND = 1 THEN GOTO DONE ELSE GOTO LOOP

DONE:
PRINT "Done!"