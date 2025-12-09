using System;
using System.Collections.Generic;

namespace MSContinuus.Types;

public enum MigrationStatus
{
    Pending,
    Exporting,
    Exported,
    Failed
}

public class Migration(int id, string guid, string state, DateTime? started = null, List<string> repositories = null)
{
    public readonly string Guid = guid;
    public readonly int Id = id;
    public readonly List<string> Repositories = repositories;
    public readonly MigrationStatus State = Enum.Parse<MigrationStatus>(state, true);
    public DateTime? Started = started;


    public override string ToString()
    {
        return $"Migration: {{ id: {Id}, guid: {Guid}, state: {State}, started: {Started} }}";
    }
}