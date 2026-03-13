namespace ClinicManagementSystem.Models;

public class Appointment
{
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorAssigned { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; } = DateTime.Today;
    public string AppointmentTime { get; set; } = "09:00 AM";
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
}
