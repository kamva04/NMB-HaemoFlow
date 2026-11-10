namespace NMB_HLabSys.Models
{
    public enum UserRole
    {
        Admin,
        Manager,
        Doctor,
        Technician,
        Patient
    }

    public enum Urgency
    {
        Routine,
        Urgent,
        Stat
    }

    public enum RequestStatus
    {
        Submitted,
        SamplesReceived,
        InProgress,
        Completed,
        ReleasedByDoctor,
        Cancelled
    }

    public enum TestStatus
    {
        Submitted,
        InProgress,
        Completed,
        Verified,
        ToBeReviewed,
        Cancelled
    }

    public enum OrderStatus
    {
        Ordered,
        PartiallyComplete,
        Complete,
        Cancelled
    }

    public enum OrderItemStatus
    {
        Ordered,
        Received,
        Cancelled
    }

    public enum NotifyType
    {
        Email,
        Appointment
    }
}
