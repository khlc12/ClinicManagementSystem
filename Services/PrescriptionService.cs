using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Services;

public class PrescriptionService
{
    private readonly AppData data;

    public PrescriptionService(AppData data)
    {
        this.data = data;
    }

    public List<PrescriptionItem> GetByConsultation(string consultationId)
    {
        return data.PrescriptionItems
            .Where(item => item.ConsultationId == consultationId)
            .Select(Clone)
            .ToList();
    }

    public List<PrescriptionItem> GetByPatient(string patientId)
    {
        return data.PrescriptionItems
            .Where(item => item.PatientId == patientId)
            .OrderBy(item => item.MedicineName)
            .ThenBy(item => item.Dosage)
            .Select(Clone)
            .ToList();
    }

    public decimal GetTotalCostForConsultation(string consultationId)
    {
        return data.PrescriptionItems
            .Where(item => item.ConsultationId == consultationId)
            .Sum(item => item.TotalCost);
    }

    public bool TryApplyToConsultation(Consultation consultation, IEnumerable<PrescriptionItem> draftItems, out string? error)
    {
        var normalizedDraft = draftItems.Select(Clone).ToList();
        if (normalizedDraft.Any(item => string.IsNullOrWhiteSpace(item.MedicineId)))
        {
            error = "Each prescription item must have a selected medicine.";
            return false;
        }

        if (normalizedDraft.Any(item => string.IsNullOrWhiteSpace(item.Dosage)))
        {
            error = "Each prescription item must have dosage instructions.";
            return false;
        }

        if (normalizedDraft.Any(item => item.Quantity <= 0))
        {
            error = "Each prescription item must have a quantity greater than zero.";
            return false;
        }

        var existingItems = data.PrescriptionItems
            .Where(item => item.ConsultationId == consultation.ConsultationId)
            .ToList();

        var effectiveStock = data.Medicines.ToDictionary(medicine => medicine.MedicineId, medicine => medicine.Quantity, StringComparer.Ordinal);
        foreach (var item in existingItems)
        {
            if (effectiveStock.ContainsKey(item.MedicineId))
            {
                effectiveStock[item.MedicineId] += item.Quantity;
            }
        }

        foreach (var item in normalizedDraft)
        {
            var medicine = data.Medicines.FirstOrDefault(entry => entry.MedicineId == item.MedicineId);
            if (medicine is null)
            {
                error = $"Medicine {item.MedicineName} could not be found.";
                return false;
            }

            item.MedicineName = medicine.MedicineName;
            item.UnitPrice = medicine.UnitPrice;

            if (!effectiveStock.TryGetValue(item.MedicineId, out var availableQuantity) || availableQuantity < item.Quantity)
            {
                error = $"Not enough stock for {medicine.MedicineName}. Available: {Math.Max(0, availableQuantity)}.";
                return false;
            }

            effectiveStock[item.MedicineId] -= item.Quantity;
        }

        foreach (var item in existingItems)
        {
            var medicine = data.Medicines.FirstOrDefault(entry => entry.MedicineId == item.MedicineId);
            if (medicine is not null)
            {
                medicine.Quantity += item.Quantity;
            }

            data.PrescriptionItems.Remove(item);
        }

        foreach (var item in normalizedDraft)
        {
            item.PrescriptionItemId = string.IsNullOrWhiteSpace(item.PrescriptionItemId)
                ? IdGenerator.Next("PRX", data.PrescriptionItems.Select(entry => entry.PrescriptionItemId))
                : item.PrescriptionItemId;
            item.ConsultationId = consultation.ConsultationId;
            item.PatientId = consultation.PatientId;
            data.PrescriptionItems.Add(item);

            var medicine = data.Medicines.First(entry => entry.MedicineId == item.MedicineId);
            medicine.Quantity -= item.Quantity;
        }

        consultation.PrescribedMedicines = BuildSummary(normalizedDraft);
        error = null;
        return true;
    }

    public void RemoveForConsultation(string consultationId)
    {
        var items = data.PrescriptionItems
            .Where(item => item.ConsultationId == consultationId)
            .ToList();

        foreach (var item in items)
        {
            var medicine = data.Medicines.FirstOrDefault(entry => entry.MedicineId == item.MedicineId);
            if (medicine is not null)
            {
                medicine.Quantity += item.Quantity;
            }

            data.PrescriptionItems.Remove(item);
        }
    }

    public static string BuildSummary(IEnumerable<PrescriptionItem> items)
    {
        var summary = items
            .Where(item => !string.IsNullOrWhiteSpace(item.MedicineName))
            .Select(item => $"{item.MedicineName} {item.Dosage} x{item.Quantity}")
            .ToList();

        return summary.Count == 0 ? string.Empty : string.Join(", ", summary);
    }

    private static PrescriptionItem Clone(PrescriptionItem item)
    {
        return new PrescriptionItem
        {
            PrescriptionItemId = item.PrescriptionItemId,
            ConsultationId = item.ConsultationId,
            PatientId = item.PatientId,
            MedicineId = item.MedicineId,
            MedicineName = item.MedicineName,
            Dosage = item.Dosage,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        };
    }
}
