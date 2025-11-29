using System;
using System.Collections.Generic;

[Serializable]
public class ResourceItem
{
    public string id;
    public string name;
    public int price;

}

[Serializable]
public class ResourceCategory
{
    public string category;
    public List<ResourceItem> items;
}

[Serializable]
public class ResourceDatabase
{
    public List<ResourceCategory> categories;
}
