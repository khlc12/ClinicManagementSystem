using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Services;

public class UserRulesService
{
    private readonly AppData data;

    public UserRulesService(AppData data)
    {
        this.data = data;
    }

    public string? Validate(User draft, User original, User? currentUser)
    {
        var basicValidation = ClinicValidator.ValidateUser(draft);
        if (!string.IsNullOrWhiteSpace(basicValidation))
        {
            return basicValidation;
        }

        var duplicate = data.Users.FirstOrDefault(user =>
            user.UserId != original.UserId &&
            string.Equals(user.Username, draft.Username, StringComparison.OrdinalIgnoreCase));
        if (duplicate is not null)
        {
            return "Username already exists. Please choose a different username.";
        }

        if (currentUser is not null && draft.UserId == currentUser.UserId && !draft.IsActive)
        {
            return "You cannot deactivate the currently logged-in user.";
        }

        var doctorAssignmentsExist = data.Appointments.Any(appointment => appointment.DoctorAssigned == original.FullName)
            || data.Consultations.Any(consultation => consultation.Doctor == original.FullName);

        if (original.Role == UserRole.Doctor && draft.Role != UserRole.Doctor && doctorAssignmentsExist)
        {
            return "Cannot change this doctor to another role while appointments or consultations are assigned.";
        }

        if (original.Role == UserRole.Doctor && !draft.IsActive && doctorAssignmentsExist)
        {
            return "Cannot deactivate this doctor while appointments or consultations are assigned.";
        }

        return null;
    }
}
