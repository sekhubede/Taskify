using MFilesAPI;

namespace Taskify.Connectors.MFiles;

/// <summary>
/// Translates M-Files objects into Taskify's TaskDTO model.
/// Every source system gets its own mapper. The messiness stays here.
/// </summary>
public static class MFilesTaskMapper
{
    public static TaskDTO Map(Vault vault, ObjectVersion objVersion)
    {
        var properties = vault.ObjectPropertyOperations.GetProperties(objVersion.ObjVer);

        return new TaskDTO
        {
            Id = objVersion.ObjVer.ID.ToString(),
            Title = GetPropertyValue<string>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefNameOrTitle) ?? "Untitled",
            Description = GetPropertyValue<string>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignmentDescription) ?? string.Empty,
            DueDate = GetPropertyValue<DateTime?>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefDeadline),
            Status = MapStatus(properties),
            AssigneeId = vault.CurrentLoggedInUserID.ToString(),
            AssigneeName = GetAssignedToName(vault, properties),
            CreatedAt = GetPropertyValue<DateTime>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCreated),
            LastUpdatedAt = GetPropertyValue<DateTime>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefLastModified),
            CompletedAt = null,
            SourceSystem = "MFiles",
            SourceId = objVersion.ObjVer.ID.ToString()
        };
    }

    public static IReadOnlyList<TaskDTO> MapAll(Vault vault, ObjectSearchResults searchResults)
    {
        var tasks = new List<TaskDTO>();

        for (int i = 1; i <= searchResults.Count; i++)
        {
            try
            {
                var objVersion = searchResults[i];
                tasks.Add(Map(vault, objVersion));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to map assignment {searchResults[i].ObjVer.ID}: {ex.Message}");
            }
        }

        return tasks;
    }

    private static TaskItemStatus MapStatus(PropertyValues properties)
    {
        var completed = GetPropertyValue<bool>(properties, (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefCompleted);
        if (completed)
            return TaskItemStatus.Completed;

        return TaskItemStatus.InProgress;
    }

    private static string GetAssignedToName(Vault vault, PropertyValues properties)
    {
        try
        {
            var assignedToProp = properties.SearchForProperty((int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo);
            if (assignedToProp == null || assignedToProp.TypedValue.IsNULL())
                return "Unassigned";

            var userName = assignedToProp.TypedValue.GetValueAsLookups()
                .Cast<Lookup>()
                .FirstOrDefault(a => int.Parse(a.DisplayID) == vault.CurrentLoggedInUserID)
                ?.DisplayValue;

            return userName ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static T? GetPropertyValue<T>(PropertyValues properties, int propertyDef)
    {
        try
        {
            var propValue = properties.SearchForProperty(propertyDef);
            if (propValue == null || propValue.TypedValue.IsNULL() || propValue.TypedValue.IsUninitialized())
                return default;

            var value = propValue.TypedValue.Value;

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                if (value is DateTime dt)
                    return (T)(object)dt;
                return default;
            }

            return (T)value;
        }
        catch
        {
            return default;
        }
    }
}
