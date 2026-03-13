using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Services;

public class ClinicLookupService
{
    private readonly AppData data;

    public ClinicLookupService(AppData data)
    {
        this.data = data;
    }

    public List<Patient> GetPatients()
    {
        return data.Patients.OrderBy(patient => patient.LastName).ThenBy(patient => patient.FirstName).ToList();
    }

    public List<User> GetDoctors()
    {
        return data.Users
            .Where(user => user.IsActive && user.Role == UserRole.Doctor)
            .OrderBy(user => user.FullName)
            .ToList();
    }

    public List<Consultation> GetConsultations()
    {
        return data.Consultations.OrderByDescending(consultation => consultation.DateOfVisit).ToList();
    }

    public List<Consultation> GetCompletedConsultations()
    {
        return data.Consultations
            .Where(consultation => consultation.Status == ConsultationStatus.Completed)
            .OrderByDescending(consultation => consultation.DateOfVisit)
            .ToList();
    }

    public List<Medicine> GetMedicines()
    {
        return data.Medicines
            .OrderBy(medicine => medicine.MedicineName)
            .ToList();
    }

    public List<PrescriptionItem> GetPrescriptionItemsForConsultation(string consultationId)
    {
        return data.PrescriptionItems
            .Where(item => item.ConsultationId == consultationId)
            .OrderBy(item => item.MedicineName)
            .ThenBy(item => item.Dosage)
            .ToList();
    }

    public List<PrescriptionItem> GetPrescriptionItemsForPatient(string patientId)
    {
        return data.PrescriptionItems
            .Where(item => item.PatientId == patientId)
            .OrderBy(item => item.MedicineName)
            .ThenBy(item => item.Dosage)
            .ToList();
    }

    public Patient? FindPatient(string patientId)
    {
        return data.Patients.FirstOrDefault(patient => patient.PatientId == patientId);
    }

    public Consultation? FindConsultation(string consultationId)
    {
        return data.Consultations.FirstOrDefault(consultation => consultation.ConsultationId == consultationId);
    }

    public void SyncPatientReferences(Patient patient)
    {
        foreach (var appointment in data.Appointments.Where(appointment => appointment.PatientId == patient.PatientId))
        {
            appointment.PatientName = patient.FullName;
        }

        foreach (var consultation in data.Consultations.Where(consultation => consultation.PatientId == patient.PatientId))
        {
            consultation.PatientName = patient.FullName;
        }

        foreach (var billing in data.BillingRecords.Where(billing => billing.PatientId == patient.PatientId))
        {
            billing.PatientName = patient.FullName;
        }
    }

    public void SyncConsultationReferences(Consultation consultation)
    {
        foreach (var billing in data.BillingRecords.Where(billing => billing.ConsultationId == consultation.ConsultationId))
        {
            billing.PatientId = consultation.PatientId;
            billing.PatientName = consultation.PatientName;
        }
    }

    public void SyncDoctorReferences(string previousFullName, User updatedDoctor)
    {
        if (string.Equals(previousFullName, updatedDoctor.FullName, StringComparison.Ordinal))
        {
            return;
        }

        foreach (var appointment in data.Appointments.Where(appointment => appointment.DoctorAssigned == previousFullName))
        {
            appointment.DoctorAssigned = updatedDoctor.FullName;
        }

        foreach (var consultation in data.Consultations.Where(consultation => consultation.Doctor == previousFullName))
        {
            consultation.Doctor = updatedDoctor.FullName;
        }
    }
}
