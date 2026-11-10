namespace NMB_HLabSys.Helpers
{
    public static class StatusDisplay
    {
        public static string Format(string? status) => status switch
        {
            "Submitted" => "Submitted",
            "SamplesReceived" => "Sample(s) Received",
            "InProgress" => "In Progress",
            "Completed" => "Completed",
            "ReleasedByDoctor" => "Released by Doctor",
            "Cancelled" => "Cancelled",
            "Verified" => "Verified",
            "ToBeReviewed" => "To Be Reviewed",
            _ => status ?? "—"
        };
    }
}
