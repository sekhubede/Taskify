using MFilesAPI;
using Taskify.Domain.Interfaces;
using Taskify.Domain.Entities;
using Taskify.Infrastructure.Mappers;

namespace Taskify.Infrastructure.MFilesInterop;

public class MFilesAssignmentRepository : IAssignmentRepository
{
    private readonly IVaultConnectionManager _connectionManager;

    public MFilesAssignmentRepository(IVaultConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public List<Assignment> GetUserAssignments()
    {
        try
        {
            var vault = GetVault();

            // Create search conditions
            var searchConditions = new SearchConditions();

            // Condition 1: Not deleted
            var deletedCondition = new SearchCondition
            {
                ConditionType = MFConditionType.MFConditionTypeEqual
            };
            deletedCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeDeleted);
            deletedCondition.TypedValue.SetValue(MFDataType.MFDatatypeBoolean, false);
            searchConditions.Add(-1, deletedCondition);

            // Condition 2: Object type = Assignment
            var objectTypeCondition = new SearchCondition
            {
                ConditionType = MFConditionType.MFConditionTypeEqual
            };
            objectTypeCondition.Expression.SetStatusValueExpression(MFStatusType.MFStatusTypeObjectTypeID);
            objectTypeCondition.TypedValue.SetValue(
                MFDataType.MFDatatypeLookup,
                (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment);
            searchConditions.Add(-1, objectTypeCondition);

            // Condition 3: Assigned to current user
            var assignedToCondition = new SearchCondition
            {
                ConditionType = MFConditionType.MFConditionTypeEqual
            };
            assignedToCondition.Expression.SetPropertyValueExpression(
                (int)MFBuiltInPropertyDef.MFBuiltInPropertyDefAssignedTo,
                MFParentChildBehavior.MFParentChildBehaviorNone);
            assignedToCondition.TypedValue.SetValue(
                MFDataType.MFDatatypeLookup,
                vault.CurrentLoggedInUserID);
            searchConditions.Add(-1, assignedToCondition);

            // Execute search
            var searchResults = vault.ObjectSearchOperations.SearchForObjectsByConditions(
                searchConditions,
                MFSearchFlags.MFSearchFlagNone,
                SortResults: false);

            var assignments = new List<Assignment>();

            for (int i = 1; i <= searchResults.Count; i++)
            {
                try
                {
                    var objVersion = searchResults[i];
                    var assignment = MFilesDataMapper.MapToDomainAssignment(vault, objVersion);
                    assignments.Add(assignment);
                }
                catch (Exception ex)
                {
                    // Log but don't fail entire operation for one bad assignment
                    Console.WriteLine($"Warning: Failed to map assignment {searchResults[i].ObjVer.ID}: {ex.Message}");
                }
            }

            return assignments;

        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to get user assignments from M-Files", ex);
        }
    }

    public Assignment? GetAssignmentById(int assignmentId)
    {
        try
        {
            var vault = GetVault();

            var objID = new ObjID();
            objID.SetIDs(
                ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
                ID: assignmentId);

            var objVersion = vault.ObjectOperations.GetLatestObjectVersionAndProperties(objID, AllowCheckedOut: true);

            return MFilesDataMapper.MapToDomainAssignment(vault, objVersion.VersionData);
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to get assignment by ID from M-Files", ex);
        }
    }

    public bool MarkAssignmentComplete(int assignmentId)
    {
        var vault = GetVault();

        var objID = new ObjID();
        objID.SetIDs(
            ObjType: (int)MFBuiltInObjectType.MFBuiltInObjectTypeAssignment,
            ID: assignmentId);

        // Check out the object
        var objVersion = vault.ObjectOperations.CheckOut(objID);

        // Update the assignment status property
        vault.ObjectPropertyOperations.MarkAssignmentComplete(objVersion.ObjVer);

        try
        {
            // Check in the object
            vault.ObjectOperations.CheckIn(objVersion.ObjVer);

            return true;
        }
        catch (Exception ex)
        {
            if (null != objVersion)
                vault.ObjectOperations.UndoCheckout(objVersion.ObjVer);

            throw new ApplicationException("Failed to mark assignment as complete in M-Files", ex);
        }
    }

    private MFilesAPI.Vault GetVault()
    {
        var vaultConnection = _connectionManager.GetVaultConnection();

        if (vaultConnection is not MFilesAPI.Vault vault)
            throw new InvalidOperationException("Failed to get M-Files vault connection");

        return vault;
    }
}