namespace ClinicManagementSystem.Models;

public class PrescriptionItem
{
    public string PrescriptionItemId { get; set; } = string.Empty;
    public string ConsultationId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string MedicineId { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalCost => UnitPrice * Quantity;
}
