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
}
