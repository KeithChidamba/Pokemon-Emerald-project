using UnityEngine;

[CreateAssetMenu(fileName = "weatherChange", menuName = "weatherChange")]

public class ChangeWeatherInfo : AdditionalInfoModule
{
    public WeatherCondition.Weather newWeatherCondition;
}
