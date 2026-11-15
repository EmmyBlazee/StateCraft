using System;

[Serializable]
public class NewsItem
{
    public string title;

    public string description;

    public NewsCategory category;

    public NewsSeverity severity;

    public DateTime timestamp;
}