SCREEN 13
COLOR 15,1
CLS
PRINT "I'm thinking of a number between 1 and 100."
RANDOMIZE
T = RND(100)
10: LINE INPUT "> "; A$
N = VAL(A$)
IF N = T THEN PRINT "You got it!" : END
IF N < T THEN PRINT "Too low" : GOTO 10
IF N > T THEN PRINT "Too high" : GOTO 10
