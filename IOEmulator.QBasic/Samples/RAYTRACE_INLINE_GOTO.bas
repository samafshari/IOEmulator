SCREEN 13
COLOR 15, 0
CLS

' Inline IF with multiple actions and GOTO label
HIT = 0
IF 1 THEN HIT = 1: GOTO SKIP
PSET 0,0, 15 ' should be skipped by GOTO
SKIP:
IF HIT = 1 THEN PSET 2,2, 15 ELSE PSET 1,1, 15

END
