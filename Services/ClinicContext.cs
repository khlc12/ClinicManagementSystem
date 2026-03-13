using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Services;

public class ClinicContext
{
    public ClinicContext(AppData data, IClinicDataStore dataStore)
    {
        Data = data;
        DataStore = dataStore;
    }

    public AppData Data { get; }
    public IClinicDataStore DataStore { get; }
    public User? CurrentUser { get; private set; }

    public void SetCurrentUser(User user)
    {
        CurrentUser = user;
    }

    public void ClearCurrentUser()
    {
        CurrentUser = null;
    }

    public void Save()
    {
        DataStore.Save(Data);
    }
}
