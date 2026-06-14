using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Utility
{
    public static int RandomRange(int min,int exclusiveLimit)
    {
        return Random.Range(min, exclusiveLimit);
    }

    public static ushort Random16Bit()
    {
        var rand = new System.Random();
        ushort  random16BIT = (ushort)rand.Next(0, 65536);
        return random16BIT;
    }

    public static int Cube(int num)
    {
        return num * num * num;
    }
    public static int Square(int num)
    {
        return num * num;
    }
    public static void ResizeImageToSprite(ref Image image,Vector2 targetImageSize)
    {
        image.SetNativeSize();
        RectTransform rt = image.rectTransform;

        float width = rt.sizeDelta.x;
        float height = rt.sizeDelta.y;

        // Find scale needed to fit inside target box
        float scale = Mathf.Min(
            targetImageSize.x / width,
            targetImageSize.y / height
        );

        rt.sizeDelta = new Vector2(
            width * scale,
            height * scale
        );
    }

    public static Sprite GetGenderSprite(Gender gender)
    {
        return Resources.Load<Sprite>(
            SaveDataHandler.GetDirectory(AssetDirectory.UI) 
            + gender.ToString().ToLower());
    }

    public static string GetAreaName(AreaName areaValue)
    {
        var areaNames = new Dictionary<AreaName, string>
        {
            {AreaName.OverWorld,"Overworld"},
            {AreaName.PlayerGarden,"Garden"},
            {AreaName.PokeMartCoastal,"PokeMart Coastal"},
            {AreaName.PokeCenter,"Poke-Center"},
            {AreaName.SouthBridge,"South Bridge"},
        };
        return areaNames[areaValue];
    }

    public static IEnumerator FadeImage(Image image,Color endColor,float duration=1f)
    {
        Color startColor = new Color(endColor.r, endColor.g, endColor.b,0);//invisible
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            image.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        yield return new WaitUntil(()=>elapsed >= duration);
    }
}
