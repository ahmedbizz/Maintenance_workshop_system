namespace WorkShop.Enums
{
    public enum MaintenanceStatus
    {
        New,
        Pending,
        AwaitingTechnician,
        AwaitingEngineer,
        ApprovedByEngineer,
            RejectedByEngineer,
            AwaitingOfficer,
            ApprovedByOfficer,
            RejectedByOfficer,
            NeedsParts,
            UnderDiagnosis,
            UnderRepair,
        Received, AssignedToEngineer, InRepair,
            Repaired,
        Delivered,
            Closed
    }
}
