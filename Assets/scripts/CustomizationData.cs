using System;

[Serializable]
public class CustomizationData
{
    public int hair     = 0;
    public int facehair = 0;
    public int eyes     = 0;
    public int chests   = 0;
    public int legs     = 0;
    public int feet     = 0;

    public const string PREFS_KEY = "CharacterCustomization";
}