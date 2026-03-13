using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Services;

public class AuthenticationService
{
    private readonly AppData data;

    public AuthenticationService(AppData data)
    {
        this.data = data;
    }

    public User? Authenticate(string username, string password)
    {
        return data.Users.FirstOrDefault(user =>
            user.IsActive &&
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase) &&
            user.Password == password);
    }
}
