using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Data;

public static class SeedDataFactory
{
    public static AppData Create()
    {
        var today = DateTime.Today;
        return new AppData
        {
            Users = new System.ComponentModel.BindingList<User>(new List<User>
            {
                new() { UserId = "USR-001", Username = "admin", Password = "admin123", FullName = "System Administrator", Role = UserRole.Administrator },
                new() { UserId = "USR-002", Username = "doctor", Password = "doc123", FullName = "Dr. Maria Santos", Role = UserRole.Doctor },
                new() { UserId = "USR-003", Username = "reception", Password = "recep123", FullName = "Ana Cruz", Role = UserRole.Receptionist }
            }),
            Patients = new System.ComponentModel.BindingList<Patient>(new List<Patient>
            {
                new() { PatientId = "PAT-001", FirstName = "Juan", LastName = "Dela Cruz", Birthdate = today.AddYears(-27), Sex = "Male", Address = "Tandag City", ContactNumber = "09171234567", DateRegistered = today.AddDays(-14) },
                new() { PatientId = "PAT-002", FirstName = "Liza", LastName = "Mercado", Birthdate = today.AddYears(-34), Sex = "Female", Address = "Bislig City", ContactNumber = "09987654321", DateRegistered = today.AddDays(-8) }
            }),
            Appointments = new System.ComponentModel.BindingList<Appointment>(new List<Appointment>
            {
                new() { AppointmentId = "APT-001", PatientId = "PAT-001", PatientName = "Dela Cruz, Juan", DoctorAssigned = "Dr. Maria Santos", AppointmentDate = today, AppointmentTime = "09:00 AM", Status = AppointmentStatus.Pending },
                new() { AppointmentId = "APT-002", PatientId = "PAT-002", PatientName = "Mercado, Liza", DoctorAssigned = "Dr. Maria Santos", AppointmentDate = today.AddDays(1), AppointmentTime = "01:30 PM", Status = AppointmentStatus.Pending }
            }),
            Consultations = new System.ComponentModel.BindingList<Consultation>(new List<Consultation>
            {
                new() { ConsultationId = "CON-001", PatientId = "PAT-001", PatientName = "Dela Cruz, Juan", Doctor = "Dr. Maria Santos", DateOfVisit = today.AddDays(-2), Status = ConsultationStatus.Completed, ChiefComplaint = "Fever and cough", Diagnosis = "Upper respiratory infection", TreatmentNotes = "Rest, hydration, and follow-up after 3 days.", PrescribedMedicines = "Paracetamol 500mg 1 tab, 3x daily x24" }
            }),
            PrescriptionItems = new System.ComponentModel.BindingList<PrescriptionItem>(new List<PrescriptionItem>
            {
                new() { PrescriptionItemId = "PRX-001", ConsultationId = "CON-001", PatientId = "PAT-001", MedicineId = "MED-001", MedicineName = "Paracetamol 500mg", Dosage = "1 tab, 3x daily", Quantity = 24, UnitPrice = 5m }
            }),
            BillingRecords = new System.ComponentModel.BindingList<BillingRecord>(new List<BillingRecord>
            {
                new() { BillingId = "BIL-001", PatientId = "PAT-001", PatientName = "Dela Cruz, Juan", ConsultationId = "CON-001", ServiceCharges = 500m, MedicineCharges = 120m, PaymentStatus = PaymentStatus.PartiallyPaid }
            }),
            Medicines = new System.ComponentModel.BindingList<Medicine>(new List<Medicine>
            {
                new() { MedicineId = "MED-001", MedicineName = "Paracetamol 500mg", Category = "Tablet", Quantity = 76, UnitPrice = 5m, ExpirationDate = today.AddMonths(18) },
                new() { MedicineId = "MED-002", MedicineName = "Amoxicillin 500mg", Category = "Capsule", Quantity = 8, UnitPrice = 12m, ExpirationDate = today.AddMonths(8) }
            })
        };
    }
}
