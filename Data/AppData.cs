using System.ComponentModel;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Data;

public class AppData
{
    public BindingList<User> Users { get; init; } = new();
    public BindingList<Patient> Patients { get; init; } = new();
    public BindingList<Appointment> Appointments { get; init; } = new();
    public BindingList<Consultation> Consultations { get; init; } = new();
    public BindingList<PrescriptionItem> PrescriptionItems { get; init; } = new();
    public BindingList<BillingRecord> BillingRecords { get; init; } = new();
    public BindingList<Medicine> Medicines { get; init; } = new();

    public AppDataSnapshot ToSnapshot() => new()
    {
        Users = Users.ToList(),
        Patients = Patients.ToList(),
        Appointments = Appointments.ToList(),
        Consultations = Consultations.ToList(),
        PrescriptionItems = PrescriptionItems.ToList(),
        BillingRecords = BillingRecords.ToList(),
        Medicines = Medicines.ToList()
    };

    public static AppData FromSnapshot(AppDataSnapshot snapshot) => new()
    {
        Users = new BindingList<User>(snapshot.Users ?? new List<User>()),
        Patients = new BindingList<Patient>(snapshot.Patients ?? new List<Patient>()),
        Appointments = new BindingList<Appointment>(snapshot.Appointments ?? new List<Appointment>()),
        Consultations = new BindingList<Consultation>(snapshot.Consultations ?? new List<Consultation>()),
        PrescriptionItems = new BindingList<PrescriptionItem>(snapshot.PrescriptionItems ?? new List<PrescriptionItem>()),
        BillingRecords = new BindingList<BillingRecord>(snapshot.BillingRecords ?? new List<BillingRecord>()),
        Medicines = new BindingList<Medicine>(snapshot.Medicines ?? new List<Medicine>())
    };
}

public class AppDataSnapshot
{
    public List<User>? Users { get; set; }
    public List<Patient>? Patients { get; set; }
    public List<Appointment>? Appointments { get; set; }
    public List<Consultation>? Consultations { get; set; }
    public List<PrescriptionItem>? PrescriptionItems { get; set; }
    public List<BillingRecord>? BillingRecords { get; set; }
    public List<Medicine>? Medicines { get; set; }
}
