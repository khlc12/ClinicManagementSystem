namespace ClinicManagementSystem.Models;

public class Patient
{
    public string PatientId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime Birthdate { get; set; } = DateTime.Today.AddYears(-20);
    public string Sex { get; set; } = "Unspecified";
    public string Address { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public DateTime DateRegistered { get; set; } = DateTime.Today;
    public string FullName => $"{LastName}, {FirstName}";
    public int Age => Math.Max(0, DateTime.Today.Year - Birthdate.Year - (Birthdate.Date > DateTime.Today.AddYears(-(DateTime.Today.Year - Birthdate.Year)) ? 1 : 0));
}
