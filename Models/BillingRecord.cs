namespace ClinicManagementSystem.Models;

public class BillingRecord
{
    public string BillingId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string ConsultationId { get; set; } = string.Empty;
    public decimal ServiceCharges { get; set; }
    public decimal MedicineCharges { get; set; }
    public decimal TotalAmount => ServiceCharges + MedicineCharges;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
}
