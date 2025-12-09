using System;

namespace MSContinuus.Types;

public class Archive(string name, string retentionClass, DateTimeOffset created)
{
    public DateTimeOffset Created = created;
    public string Name = name;
    public string RetentionClass = retentionClass;
}