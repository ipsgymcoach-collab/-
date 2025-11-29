using System;

[Serializable]
public class TeamData
{
    public string id;
    public string name;
    public int level;
    public float mood = 100f; // Новое поле: настроение (0–100)
    public bool isHired;

    public TeamData(string id, string name, int level)
    {
        this.id = id;
        this.name = name;
        this.level = level;
        this.mood = 100f;
        this.isHired = false;
    }
}
