using System;

[Serializable]
public class MoveLockData
{
    public Move moveToLock;
    public bool moveLocked;
    //This will likely be expanded in future when more 
    //complex move restriction mechanisms are introduced
    public MoveLockData(Move moveToLock)
    {
        this.moveToLock = moveToLock;
        moveLocked = true;
    }
}