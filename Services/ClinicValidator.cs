using System.Text.RegularExpressions;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Services;

public static class ClinicValidator
{
    public static string? ValidatePatient(Patient patient)
    {
        if (string.IsNullOrWhiteSpace(patient.FirstName) || string.IsNullOrWhiteSpace(patient.LastName))
        {
            return "Patient first name and last name are required.";
        }

        if (patient.Birthdate.Date > DateTime.Today)
        {
            return "Birthdate cannot be in the future.";
        }

        if (string.IsNullOrWhiteSpace(patient.Address))
        {
            return "Address is required.";
        }

        if (string.IsNullOrWhiteSpace(patient.ContactNumber) || !Regex.IsMatch(patient.ContactNumber, "^[0-9+ -]{7,20}$"))
        {
            return "Contact number must be 7 to 20 characters and contain only digits or phone symbols.";
        }

        return null;
    }

    public static string? ValidateAppointment(Appointment appointment)
    {
        if (string.IsNullOrWhiteSpace(appointment.PatientId))
        {
            return "A patient must be selected for the appointment.";
        }

        if (string.IsNullOrWhiteSpace(appointment.DoctorAssigned))
        {
            return "A doctor must be assigned to the appointment.";
        }

        if (string.IsNullOrWhiteSpace(appointment.AppointmentTime))
        {
            return "Appointment time is required.";
        }

        return null;
    }

    public static string? ValidateConsultation(Consultation consultation)
    {
        if (string.IsNullOrWhiteSpace(consultation.PatientId))
        {
            return "A patient must be selected for the consultation.";
        }

        if (string.IsNullOrWhiteSpace(consultation.Doctor))
        {
            return "A doctor must be selected for the consultation.";
        }

        if (string.IsNullOrWhiteSpace(consultation.ChiefComplaint))
        {
            return "Chief complaint is required.";
        }

        if (string.IsNullOrWhiteSpace(consultation.Diagnosis))
        {
            return "Diagnosis is required.";
        }

        return null;
    }

    public static string? ValidateBilling(BillingRecord billing)
    {
        if (string.IsNullOrWhiteSpace(billing.PatientId))
        {
            return "A patient must be selected for the billing record.";
        }

        if (string.IsNullOrWhiteSpace(billing.ConsultationId))
        {
            return "A consultation must be linked to the billing record.";
        }

        if (billing.ServiceCharges < 0 || billing.MedicineCharges < 0)
        {
            return "Charges cannot be negative.";
        }

        return null;
    }

    public static string? ValidateBilling(BillingRecord billing, Consultation? consultation)
    {
        var basicValidation = ValidateBilling(billing);
        if (!string.IsNullOrWhiteSpace(basicValidation))
        {
            return basicValidation;
        }

        if (consultation is null)
        {
            return "The selected consultation could not be found.";
        }

        if (consultation.Status != ConsultationStatus.Completed)
        {
            return "Billing can only be created from a completed consultation.";
        }

        return null;
    }

    public static string? ValidatePrescriptionItem(PrescriptionItem item)
    {
        if (string.IsNullOrWhiteSpace(item.MedicineId))
        {
            return "A medicine must be selected for each prescription item.";
        }

        if (string.IsNullOrWhiteSpace(item.Dosage))
        {
            return "Dosage instructions are required for each prescription item.";
        }

        if (item.Quantity <= 0)
        {
            return "Prescription quantity must be greater than zero.";
        }

        return null;
    }

    public static string? ValidateMedicine(Medicine medicine)
    {
        if (string.IsNullOrWhiteSpace(medicine.MedicineName))
        {
            return "Medicine name is required.";
        }

        if (string.IsNullOrWhiteSpace(medicine.Category))
        {
            return "Medicine category is required.";
        }

        if (medicine.Quantity < 0)
        {
            return "Quantity cannot be negative.";
        }

        if (medicine.UnitPrice < 0)
        {
            return "Unit price cannot be negative.";
        }

        return null;
    }

    public static string? ValidateUser(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
        {
            return "Username is required.";
        }

        if (string.IsNullOrWhiteSpace(user.Password))
        {
            return "Password is required.";
        }

        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            return "Full name is required.";
        }

        return null;
    }
}
