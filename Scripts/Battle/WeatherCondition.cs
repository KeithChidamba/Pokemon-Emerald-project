using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class WeatherCondition
{
    public int turnDuration;
    public bool isInfinite;
    public enum Weather{Rain,Sunlight,Hail,Sandstorm, Clear}

    public Weather weather;
    public Func<IEnumerator> weatherEffect;
    public string weatherBegunMessage;
    public string weatherTurnEndMessage;
    public string weatherDamageMessage;
    public string weatherEndMessage;
    public List<Battle_Participant> buffedParticipants = new();
    public WeatherCondition(Weather weather)
    {
        this.weather = weather;
        isInfinite = false;
    }
}
