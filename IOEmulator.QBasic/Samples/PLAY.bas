SCREEN 13
COLOR 15,1
CLS
PRINT "QBASIC PLAY demo"
PRINT "Enter music strings like: T120 O4 L8 C D E F G A B"
PRINT "Examples: \"CDEFGAB\", \"T150 O3 L4 C C G G A A G2\""
PRINT "Press Enter after typing to play. Close the window to exit."

10: LINE INPUT "PLAY> "; A$
PLAY A$
GOTO 10
