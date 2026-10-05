using System;
using System.Collections.Generic;

[Serializable]
public struct PlayerBoundary
{
    public bool isRestricting;
    public List<Boundary> boundaries;
}
[Serializable]
public struct Boundary
{
    public MovementDirection direction;
    public int limit;
}