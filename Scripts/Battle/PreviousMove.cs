using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviousMove
{
    public Move move;
    public int numRepetitions;

    public PreviousMove(Move move, int numRepetitions)
    {
        this.move = move;
        this.numRepetitions = numRepetitions;
    }
}
