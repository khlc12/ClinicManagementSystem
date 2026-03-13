namespace ClinicManagementSystem.Models;

public class Consultation
{
    public string ConsultationId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string Doctor { get; set; } = string.Empty;
    public DateTime DateOfVisit { get; set; } = DateTime.Today;
    public ConsultationStatus Status { get; set; } = ConsultationStatus.Pending;
    public string ChiefComplaint { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatmentNotes { get; set; } = string.Empty;
    public string PrescribedMedicines { get; set; } = string.Empty;
}
