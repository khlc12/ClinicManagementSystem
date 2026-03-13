namespace ClinicManagementSystem.Models;

public class Medicine
{
    public string MedicineId { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime ExpirationDate { get; set; } = DateTime.Today.AddYears(1);
    public bool IsLowStock => Quantity <= 10;
}
