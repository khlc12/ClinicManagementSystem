namespace ClinicManagementSystem.Data;

public interface IClinicDataStore
{
    AppData Load();
    void Save(AppData data);
}
