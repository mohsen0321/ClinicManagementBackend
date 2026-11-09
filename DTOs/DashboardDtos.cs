namespace clinicsystembackend.DTOs
{
    
    public class AdminDashboardStatsDto
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalClinics { get; set; }
        public int TotalSpecialties { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int AcceptedAppointments { get; set; }
        public int RejectedAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public List<AppointmentDetailDto> RecentAppointments { get; set; } = new();
    }

    public class PatientDashboardStatsDto
    {
        public string PatientName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public int TotalAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int AcceptedAppointments { get; set; }
        public int RejectedAppointments { get; set; }
        public AppointmentDetailDto? UpcomingAppointment { get; set; }
        public List<AppointmentDetailDto> RecentAppointments { get; set; } = new();
    }
}