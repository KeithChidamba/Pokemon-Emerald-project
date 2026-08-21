using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviousMove
{
    public Move move;
    public int numRepetitions;
    public bool failedAttempt;
    public PreviousMove(Move move, int numRepetitions)
    {
        failedAttempt = false;
        this.move = move;
        this.numRepetitions = numRepetitions;
    }
}
