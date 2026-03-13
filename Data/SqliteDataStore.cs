using ClinicManagementSystem.Models;
using Microsoft.Data.Sqlite;

namespace ClinicManagementSystem.Data;

public class SqliteDataStore : IClinicDataStore
{
    private const int CurrentSchemaVersion = 4;
    private readonly string databasePath;

    public SqliteDataStore(string databasePath)
    {
        this.databasePath = databasePath;
    }

    public AppData Load()
    {
        EnsureDatabase();

        using var connection = OpenConnection();
        var data = new AppData
        {
            Users = new System.ComponentModel.BindingList<User>(LoadUsers(connection)),
            Patients = new System.ComponentModel.BindingList<Patient>(LoadPatients(connection)),
            Appointments = new System.ComponentModel.BindingList<Appointment>(LoadAppointments(connection)),
            Consultations = new System.ComponentModel.BindingList<Consultation>(LoadConsultations(connection)),
            PrescriptionItems = new System.ComponentModel.BindingList<PrescriptionItem>(LoadPrescriptionItems(connection)),
            BillingRecords = new System.ComponentModel.BindingList<BillingRecord>(LoadBilling(connection)),
            Medicines = new System.ComponentModel.BindingList<Medicine>(LoadMedicines(connection))
        };

        if (data.Users.Count == 0)
        {
            data = SeedDataFactory.Create();
            Save(data);
            return data;
        }

        if (!data.Users.Any(user => user.IsActive))
        {
            RestoreRecoverableAdmin(data);
            Save(data);
        }

        return data;
    }

    private static void RestoreRecoverableAdmin(AppData data)
    {
        var adminUser = data.Users.FirstOrDefault(user => string.Equals(user.Username, "admin", StringComparison.OrdinalIgnoreCase));
        if (adminUser is not null)
        {
            adminUser.IsActive = true;
            if (string.IsNullOrWhiteSpace(adminUser.Password))
            {
                adminUser.Password = "admin123";
            }

            if (string.IsNullOrWhiteSpace(adminUser.FullName))
            {
                adminUser.FullName = "System Administrator";
            }

            adminUser.Role = UserRole.Administrator;
            return;
        }

        var rescueId = $"USR-{(data.Users.Count + 1):000}";
        while (data.Users.Any(user => string.Equals(user.UserId, rescueId, StringComparison.OrdinalIgnoreCase)))
        {
            rescueId = $"USR-{(int.Parse(rescueId[4..]) + 1):000}";
        }

        data.Users.Add(
            new User
            {
                UserId = rescueId,
                Username = "admin",
                Password = "admin123",
                FullName = "System Administrator",
                Role = UserRole.Administrator,
                IsActive = true
            });
    }

    public void Save(AppData data)
    {
        EnsureDatabase();

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        UpsertUsers(connection, transaction, data.Users);
        UpsertPatients(connection, transaction, data.Patients);
        UpsertMedicines(connection, transaction, data.Medicines);
        UpsertAppointments(connection, transaction, data.Appointments);
        UpsertConsultations(connection, transaction, data.Consultations);
        UpsertPrescriptionItems(connection, transaction, data.PrescriptionItems);
        UpsertBillingRecords(connection, transaction, data.BillingRecords);

        DeleteMissingRecords(connection, transaction, "BillingRecords", "BillingId", data.BillingRecords.Select(billing => billing.BillingId));
        DeleteMissingRecords(connection, transaction, "PrescriptionItems", "PrescriptionItemId", data.PrescriptionItems.Select(item => item.PrescriptionItemId));
        DeleteMissingRecords(connection, transaction, "Consultations", "ConsultationId", data.Consultations.Select(consultation => consultation.ConsultationId));
        DeleteMissingRecords(connection, transaction, "Appointments", "AppointmentId", data.Appointments.Select(appointment => appointment.AppointmentId));
        DeleteMissingRecords(connection, transaction, "Medicines", "MedicineId", data.Medicines.Select(medicine => medicine.MedicineId));
        DeleteMissingRecords(connection, transaction, "Patients", "PatientId", data.Patients.Select(patient => patient.PatientId));
        DeleteMissingRecords(connection, transaction, "Users", "UserId", data.Users.Select(user => user.UserId));

        transaction.Commit();
    }

    private void EnsureDatabase()
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var connection = OpenConnection();
        var version = GetUserVersion(connection);
        var hasUsersTable = TableExists(connection, "Users");

        if (!hasUsersTable)
        {
            CreateSchema(connection);
            SetUserVersion(connection, CurrentSchemaVersion);
            return;
        }

        if (version < CurrentSchemaVersion)
        {
            MigrateToCurrentSchema(connection);
        }
        else
        {
            CreateIndexes(connection);
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        return connection;
    }

    private static int GetUserVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void SetUserVersion(SqliteConnection connection, int version)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version};";
        command.ExecuteNonQuery();
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName;";
        command.Parameters.AddWithValue("@tableName", tableName);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void ExecuteNonQuery(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private void MigrateToCurrentSchema(SqliteConnection connection)
    {
        ExecuteNonQuery(connection, "PRAGMA foreign_keys = OFF;");
        using var transaction = connection.BeginTransaction();

        RenameTableIfExists(connection, transaction, "Users", "Users_legacy");
        RenameTableIfExists(connection, transaction, "Patients", "Patients_legacy");
        RenameTableIfExists(connection, transaction, "Appointments", "Appointments_legacy");
        RenameTableIfExists(connection, transaction, "Consultations", "Consultations_legacy");
        RenameTableIfExists(connection, transaction, "PrescriptionItems", "PrescriptionItems_legacy");
        RenameTableIfExists(connection, transaction, "BillingRecords", "BillingRecords_legacy");
        RenameTableIfExists(connection, transaction, "Medicines", "Medicines_legacy");

        CreateSchema(connection, transaction);

        CopyTable(connection, transaction, "Users_legacy", "Users", "UserId, Username, Password, FullName, Role, IsActive");
        CopyTable(connection, transaction, "Patients_legacy", "Patients", "PatientId, FirstName, LastName, Birthdate, Sex, Address, ContactNumber, DateRegistered");
        CopyTable(connection, transaction, "Appointments_legacy", "Appointments", "AppointmentId, PatientId, PatientName, DoctorAssigned, AppointmentDate, AppointmentTime, Status");
        CopyLegacyConsultations(connection, transaction);
        CopyLegacyPrescriptionItems(connection, transaction);
        CopyTable(connection, transaction, "BillingRecords_legacy", "BillingRecords", "BillingId, PatientId, PatientName, ConsultationId, ServiceCharges, MedicineCharges, PaymentStatus");
        CopyTable(connection, transaction, "Medicines_legacy", "Medicines", "MedicineId, MedicineName, Category, Quantity, UnitPrice, ExpirationDate");

        DropTableIfExists(connection, transaction, "Users_legacy");
        DropTableIfExists(connection, transaction, "Patients_legacy");
        DropTableIfExists(connection, transaction, "Appointments_legacy");
        DropTableIfExists(connection, transaction, "Consultations_legacy");
        DropTableIfExists(connection, transaction, "PrescriptionItems_legacy");
        DropTableIfExists(connection, transaction, "BillingRecords_legacy");
        DropTableIfExists(connection, transaction, "Medicines_legacy");

        ExecuteNonQuery(connection, transaction, $"PRAGMA user_version = {CurrentSchemaVersion};");
        transaction.Commit();
        ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");
    }

    private static void RenameTableIfExists(SqliteConnection connection, SqliteTransaction transaction, string from, string to)
    {
        if (!TableExists(connection, from))
        {
            return;
        }

        DropTableIfExists(connection, transaction, to);
        ExecuteNonQuery(connection, transaction, $"ALTER TABLE {from} RENAME TO {to};");
    }

    private static void DropTableIfExists(SqliteConnection connection, SqliteTransaction transaction, string tableName)
    {
        ExecuteNonQuery(connection, transaction, $"DROP TABLE IF EXISTS {tableName};");
    }

    private static void CopyTable(SqliteConnection connection, SqliteTransaction transaction, string sourceTable, string destinationTable, string columns)
    {
        if (!TableExists(connection, sourceTable))
        {
            return;
        }

        ExecuteNonQuery(connection, transaction, $"INSERT INTO {destinationTable} ({columns}) SELECT {columns} FROM {sourceTable};");
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        CreateSchema(connection, transaction: null);
    }

    private static void CreateSchema(SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Users (
    UserId TEXT PRIMARY KEY,
    Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
    Password TEXT NOT NULL,
    FullName TEXT NOT NULL,
    Role INTEGER NOT NULL,
    IsActive INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS Patients (
    PatientId TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Birthdate TEXT NOT NULL,
    Sex TEXT NOT NULL,
    Address TEXT NOT NULL,
    ContactNumber TEXT NOT NULL,
    DateRegistered TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Medicines (
    MedicineId TEXT PRIMARY KEY,
    MedicineName TEXT NOT NULL,
    Category TEXT NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    ExpirationDate TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Appointments (
    AppointmentId TEXT PRIMARY KEY,
    PatientId TEXT NOT NULL,
    PatientName TEXT NOT NULL,
    DoctorAssigned TEXT NOT NULL,
    AppointmentDate TEXT NOT NULL,
    AppointmentTime TEXT NOT NULL,
    Status INTEGER NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES Patients(PatientId) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS Consultations (
    ConsultationId TEXT PRIMARY KEY,
    PatientId TEXT NOT NULL,
    PatientName TEXT NOT NULL,
    Doctor TEXT NOT NULL,
    DateOfVisit TEXT NOT NULL,
    Status INTEGER NOT NULL,
    ChiefComplaint TEXT NOT NULL,
    Diagnosis TEXT NOT NULL,
    TreatmentNotes TEXT NOT NULL,
    PrescribedMedicines TEXT NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES Patients(PatientId) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS PrescriptionItems (
    PrescriptionItemId TEXT PRIMARY KEY,
    ConsultationId TEXT NOT NULL,
    PatientId TEXT NOT NULL,
    MedicineId TEXT NOT NULL,
    MedicineName TEXT NOT NULL,
    Dosage TEXT NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    FOREIGN KEY (ConsultationId) REFERENCES Consultations(ConsultationId) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (PatientId) REFERENCES Patients(PatientId) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (MedicineId) REFERENCES Medicines(MedicineId) ON UPDATE CASCADE ON DELETE RESTRICT
);
CREATE TABLE IF NOT EXISTS BillingRecords (
    BillingId TEXT PRIMARY KEY,
    PatientId TEXT NOT NULL,
    PatientName TEXT NOT NULL,
    ConsultationId TEXT NOT NULL,
    ServiceCharges REAL NOT NULL,
    MedicineCharges REAL NOT NULL,
    PaymentStatus INTEGER NOT NULL,
    FOREIGN KEY (PatientId) REFERENCES Patients(PatientId) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (ConsultationId) REFERENCES Consultations(ConsultationId) ON UPDATE CASCADE ON DELETE RESTRICT
);";
        command.ExecuteNonQuery();
        CreateIndexes(connection, transaction);
    }

    private static void CreateIndexes(SqliteConnection connection)
    {
        CreateIndexes(connection, transaction: null);
    }

    private static void CreateIndexes(SqliteConnection connection, SqliteTransaction? transaction)
    {
        ExecuteIndex(connection, transaction, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username ON Users(Username COLLATE NOCASE);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Patients_LastName_FirstName ON Patients(LastName, FirstName);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Appointments_PatientId ON Appointments(PatientId);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Appointments_DateTime ON Appointments(AppointmentDate, AppointmentTime);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Consultations_PatientId ON Consultations(PatientId);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Consultations_DateOfVisit ON Consultations(DateOfVisit);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Consultations_Status ON Consultations(Status);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PrescriptionItems_ConsultationId ON PrescriptionItems(ConsultationId);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PrescriptionItems_PatientId ON PrescriptionItems(PatientId);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_PrescriptionItems_MedicineId ON PrescriptionItems(MedicineId);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_BillingRecords_PatientId ON BillingRecords(PatientId);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_BillingRecords_ConsultationId ON BillingRecords(ConsultationId);");
        ExecuteIndex(connection, transaction, "CREATE INDEX IF NOT EXISTS IX_Medicines_Name ON Medicines(MedicineName);");
    }

    private static void ExecuteIndex(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static List<User> LoadUsers(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT UserId, Username, Password, FullName, Role, IsActive FROM Users ORDER BY Username;";
        using var reader = command.ExecuteReader();
        var items = new List<User>();
        while (reader.Read())
        {
            items.Add(new User
            {
                UserId = reader.GetString(0),
                Username = reader.GetString(1),
                Password = reader.GetString(2),
                FullName = reader.GetString(3),
                Role = (UserRole)reader.GetInt32(4),
                IsActive = reader.GetInt32(5) == 1
            });
        }

        return items;
    }

    private static List<Patient> LoadPatients(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PatientId, FirstName, LastName, Birthdate, Sex, Address, ContactNumber, DateRegistered FROM Patients ORDER BY LastName, FirstName;";
        using var reader = command.ExecuteReader();
        var items = new List<Patient>();
        while (reader.Read())
        {
            items.Add(new Patient
            {
                PatientId = reader.GetString(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Birthdate = DateTime.Parse(reader.GetString(3)),
                Sex = reader.GetString(4),
                Address = reader.GetString(5),
                ContactNumber = reader.GetString(6),
                DateRegistered = DateTime.Parse(reader.GetString(7))
            });
        }

        return items;
    }

    private static List<Appointment> LoadAppointments(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT AppointmentId, PatientId, PatientName, DoctorAssigned, AppointmentDate, AppointmentTime, Status FROM Appointments ORDER BY AppointmentDate, AppointmentTime;";
        using var reader = command.ExecuteReader();
        var items = new List<Appointment>();
        while (reader.Read())
        {
            items.Add(new Appointment
            {
                AppointmentId = reader.GetString(0),
                PatientId = reader.GetString(1),
                PatientName = reader.GetString(2),
                DoctorAssigned = reader.GetString(3),
                AppointmentDate = DateTime.Parse(reader.GetString(4)),
                AppointmentTime = reader.GetString(5),
                Status = (AppointmentStatus)reader.GetInt32(6)
            });
        }

        return items;
    }

    private static List<Consultation> LoadConsultations(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT ConsultationId, PatientId, PatientName, Doctor, DateOfVisit, Status, ChiefComplaint, Diagnosis, TreatmentNotes, PrescribedMedicines FROM Consultations ORDER BY DateOfVisit DESC;";
        using var reader = command.ExecuteReader();
        var items = new List<Consultation>();
        while (reader.Read())
        {
            items.Add(new Consultation
            {
                ConsultationId = reader.GetString(0),
                PatientId = reader.GetString(1),
                PatientName = reader.GetString(2),
                Doctor = reader.GetString(3),
                DateOfVisit = DateTime.Parse(reader.GetString(4)),
                Status = (ConsultationStatus)reader.GetInt32(5),
                ChiefComplaint = reader.GetString(6),
                Diagnosis = reader.GetString(7),
                TreatmentNotes = reader.GetString(8),
                PrescribedMedicines = reader.GetString(9)
            });
        }

        return items;
    }

    private static List<PrescriptionItem> LoadPrescriptionItems(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PrescriptionItemId, ConsultationId, PatientId, MedicineId, MedicineName, Dosage, Quantity, UnitPrice FROM PrescriptionItems ORDER BY ConsultationId, PrescriptionItemId;";
        using var reader = command.ExecuteReader();
        var items = new List<PrescriptionItem>();
        while (reader.Read())
        {
            items.Add(new PrescriptionItem
            {
                PrescriptionItemId = reader.GetString(0),
                ConsultationId = reader.GetString(1),
                PatientId = reader.GetString(2),
                MedicineId = reader.GetString(3),
                MedicineName = reader.GetString(4),
                Dosage = reader.GetString(5),
                Quantity = reader.GetInt32(6),
                UnitPrice = Convert.ToDecimal(reader.GetValue(7))
            });
        }

        return items;
    }

    private static List<BillingRecord> LoadBilling(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT BillingId, PatientId, PatientName, ConsultationId, ServiceCharges, MedicineCharges, PaymentStatus FROM BillingRecords ORDER BY BillingId;";
        using var reader = command.ExecuteReader();
        var items = new List<BillingRecord>();
        while (reader.Read())
        {
            items.Add(new BillingRecord
            {
                BillingId = reader.GetString(0),
                PatientId = reader.GetString(1),
                PatientName = reader.GetString(2),
                ConsultationId = reader.GetString(3),
                ServiceCharges = Convert.ToDecimal(reader.GetValue(4)),
                MedicineCharges = Convert.ToDecimal(reader.GetValue(5)),
                PaymentStatus = (PaymentStatus)reader.GetInt32(6)
            });
        }

        return items;
    }

    private static List<Medicine> LoadMedicines(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MedicineId, MedicineName, Category, Quantity, UnitPrice, ExpirationDate FROM Medicines ORDER BY MedicineName;";
        using var reader = command.ExecuteReader();
        var items = new List<Medicine>();
        while (reader.Read())
        {
            items.Add(new Medicine
            {
                MedicineId = reader.GetString(0),
                MedicineName = reader.GetString(1),
                Category = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                UnitPrice = Convert.ToDecimal(reader.GetValue(4)),
                ExpirationDate = DateTime.Parse(reader.GetString(5))
            });
        }

        return items;
    }

    private static void UpsertUsers(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<User> users)
    {
        foreach (var user in users)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO Users (UserId, Username, Password, FullName, Role, IsActive)
VALUES (@UserId, @Username, @Password, @FullName, @Role, @IsActive)
ON CONFLICT(UserId) DO UPDATE SET
    Username = excluded.Username,
    Password = excluded.Password,
    FullName = excluded.FullName,
    Role = excluded.Role,
    IsActive = excluded.IsActive;";
            command.Parameters.AddWithValue("@UserId", user.UserId);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@Role", (int)user.Role);
            command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    private static void UpsertPatients(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<Patient> patients)
    {
        foreach (var patient in patients)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO Patients (PatientId, FirstName, LastName, Birthdate, Sex, Address, ContactNumber, DateRegistered)
VALUES (@PatientId, @FirstName, @LastName, @Birthdate, @Sex, @Address, @ContactNumber, @DateRegistered)
ON CONFLICT(PatientId) DO UPDATE SET
    FirstName = excluded.FirstName,
    LastName = excluded.LastName,
    Birthdate = excluded.Birthdate,
    Sex = excluded.Sex,
    Address = excluded.Address,
    ContactNumber = excluded.ContactNumber,
    DateRegistered = excluded.DateRegistered;";
            command.Parameters.AddWithValue("@PatientId", patient.PatientId);
            command.Parameters.AddWithValue("@FirstName", patient.FirstName);
            command.Parameters.AddWithValue("@LastName", patient.LastName);
            command.Parameters.AddWithValue("@Birthdate", patient.Birthdate.ToString("O"));
            command.Parameters.AddWithValue("@Sex", patient.Sex);
            command.Parameters.AddWithValue("@Address", patient.Address);
            command.Parameters.AddWithValue("@ContactNumber", patient.ContactNumber);
            command.Parameters.AddWithValue("@DateRegistered", patient.DateRegistered.ToString("O"));
            command.ExecuteNonQuery();
        }
    }

    private static void UpsertMedicines(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<Medicine> medicines)
    {
        foreach (var medicine in medicines)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO Medicines (MedicineId, MedicineName, Category, Quantity, UnitPrice, ExpirationDate)
VALUES (@MedicineId, @MedicineName, @Category, @Quantity, @UnitPrice, @ExpirationDate)
ON CONFLICT(MedicineId) DO UPDATE SET
    MedicineName = excluded.MedicineName,
    Category = excluded.Category,
    Quantity = excluded.Quantity,
    UnitPrice = excluded.UnitPrice,
    ExpirationDate = excluded.ExpirationDate;";
            command.Parameters.AddWithValue("@MedicineId", medicine.MedicineId);
            command.Parameters.AddWithValue("@MedicineName", medicine.MedicineName);
            command.Parameters.AddWithValue("@Category", medicine.Category);
            command.Parameters.AddWithValue("@Quantity", medicine.Quantity);
            command.Parameters.AddWithValue("@UnitPrice", medicine.UnitPrice);
            command.Parameters.AddWithValue("@ExpirationDate", medicine.ExpirationDate.ToString("O"));
            command.ExecuteNonQuery();
        }
    }

    private static void UpsertAppointments(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<Appointment> appointments)
    {
        foreach (var appointment in appointments)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO Appointments (AppointmentId, PatientId, PatientName, DoctorAssigned, AppointmentDate, AppointmentTime, Status)
VALUES (@AppointmentId, @PatientId, @PatientName, @DoctorAssigned, @AppointmentDate, @AppointmentTime, @Status)
ON CONFLICT(AppointmentId) DO UPDATE SET
    PatientId = excluded.PatientId,
    PatientName = excluded.PatientName,
    DoctorAssigned = excluded.DoctorAssigned,
    AppointmentDate = excluded.AppointmentDate,
    AppointmentTime = excluded.AppointmentTime,
    Status = excluded.Status;";
            command.Parameters.AddWithValue("@AppointmentId", appointment.AppointmentId);
            command.Parameters.AddWithValue("@PatientId", appointment.PatientId);
            command.Parameters.AddWithValue("@PatientName", appointment.PatientName);
            command.Parameters.AddWithValue("@DoctorAssigned", appointment.DoctorAssigned);
            command.Parameters.AddWithValue("@AppointmentDate", appointment.AppointmentDate.ToString("O"));
            command.Parameters.AddWithValue("@AppointmentTime", appointment.AppointmentTime);
            command.Parameters.AddWithValue("@Status", (int)appointment.Status);
            command.ExecuteNonQuery();
        }
    }

    private static void UpsertConsultations(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<Consultation> consultations)
    {
        foreach (var consultation in consultations)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO Consultations (ConsultationId, PatientId, PatientName, Doctor, DateOfVisit, Status, ChiefComplaint, Diagnosis, TreatmentNotes, PrescribedMedicines)
VALUES (@ConsultationId, @PatientId, @PatientName, @Doctor, @DateOfVisit, @Status, @ChiefComplaint, @Diagnosis, @TreatmentNotes, @PrescribedMedicines)
ON CONFLICT(ConsultationId) DO UPDATE SET
    PatientId = excluded.PatientId,
    PatientName = excluded.PatientName,
    Doctor = excluded.Doctor,
    DateOfVisit = excluded.DateOfVisit,
    Status = excluded.Status,
    ChiefComplaint = excluded.ChiefComplaint,
    Diagnosis = excluded.Diagnosis,
    TreatmentNotes = excluded.TreatmentNotes,
    PrescribedMedicines = excluded.PrescribedMedicines;";
            command.Parameters.AddWithValue("@ConsultationId", consultation.ConsultationId);
            command.Parameters.AddWithValue("@PatientId", consultation.PatientId);
            command.Parameters.AddWithValue("@PatientName", consultation.PatientName);
            command.Parameters.AddWithValue("@Doctor", consultation.Doctor);
            command.Parameters.AddWithValue("@DateOfVisit", consultation.DateOfVisit.ToString("O"));
            command.Parameters.AddWithValue("@Status", (int)consultation.Status);
            command.Parameters.AddWithValue("@ChiefComplaint", consultation.ChiefComplaint);
            command.Parameters.AddWithValue("@Diagnosis", consultation.Diagnosis);
            command.Parameters.AddWithValue("@TreatmentNotes", consultation.TreatmentNotes);
            command.Parameters.AddWithValue("@PrescribedMedicines", consultation.PrescribedMedicines);
            command.ExecuteNonQuery();
        }
    }

    private static void UpsertPrescriptionItems(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<PrescriptionItem> prescriptionItems)
    {
        foreach (var item in prescriptionItems)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO PrescriptionItems (PrescriptionItemId, ConsultationId, PatientId, MedicineId, MedicineName, Dosage, Quantity, UnitPrice)
VALUES (@PrescriptionItemId, @ConsultationId, @PatientId, @MedicineId, @MedicineName, @Dosage, @Quantity, @UnitPrice)
ON CONFLICT(PrescriptionItemId) DO UPDATE SET
    ConsultationId = excluded.ConsultationId,
    PatientId = excluded.PatientId,
    MedicineId = excluded.MedicineId,
    MedicineName = excluded.MedicineName,
    Dosage = excluded.Dosage,
    Quantity = excluded.Quantity,
    UnitPrice = excluded.UnitPrice;";
            command.Parameters.AddWithValue("@PrescriptionItemId", item.PrescriptionItemId);
            command.Parameters.AddWithValue("@ConsultationId", item.ConsultationId);
            command.Parameters.AddWithValue("@PatientId", item.PatientId);
            command.Parameters.AddWithValue("@MedicineId", item.MedicineId);
            command.Parameters.AddWithValue("@MedicineName", item.MedicineName);
            command.Parameters.AddWithValue("@Dosage", item.Dosage);
            command.Parameters.AddWithValue("@Quantity", item.Quantity);
            command.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
            command.ExecuteNonQuery();
        }
    }

    private static void UpsertBillingRecords(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<BillingRecord> billingRecords)
    {
        foreach (var billing in billingRecords)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
INSERT INTO BillingRecords (BillingId, PatientId, PatientName, ConsultationId, ServiceCharges, MedicineCharges, PaymentStatus)
VALUES (@BillingId, @PatientId, @PatientName, @ConsultationId, @ServiceCharges, @MedicineCharges, @PaymentStatus)
ON CONFLICT(BillingId) DO UPDATE SET
    PatientId = excluded.PatientId,
    PatientName = excluded.PatientName,
    ConsultationId = excluded.ConsultationId,
    ServiceCharges = excluded.ServiceCharges,
    MedicineCharges = excluded.MedicineCharges,
    PaymentStatus = excluded.PaymentStatus;";
            command.Parameters.AddWithValue("@BillingId", billing.BillingId);
            command.Parameters.AddWithValue("@PatientId", billing.PatientId);
            command.Parameters.AddWithValue("@PatientName", billing.PatientName);
            command.Parameters.AddWithValue("@ConsultationId", billing.ConsultationId);
            command.Parameters.AddWithValue("@ServiceCharges", billing.ServiceCharges);
            command.Parameters.AddWithValue("@MedicineCharges", billing.MedicineCharges);
            command.Parameters.AddWithValue("@PaymentStatus", (int)billing.PaymentStatus);
            command.ExecuteNonQuery();
        }
    }

    private static void DeleteMissingRecords(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        string keyColumn,
        IEnumerable<string> currentIds)
    {
        var ids = currentIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();

        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        if (ids.Count == 0)
        {
            command.CommandText = $"DELETE FROM {tableName};";
            command.ExecuteNonQuery();
            return;
        }

        var parameterNames = new List<string>();
        for (var index = 0; index < ids.Count; index++)
        {
            var parameterName = $"@id{index}";
            parameterNames.Add(parameterName);
            command.Parameters.AddWithValue(parameterName, ids[index]);
        }

        command.CommandText = $"DELETE FROM {tableName} WHERE {keyColumn} NOT IN ({string.Join(", ", parameterNames)});";
        command.ExecuteNonQuery();
    }

    private static void CopyLegacyConsultations(SqliteConnection connection, SqliteTransaction transaction)
    {
        if (!TableExists(connection, "Consultations_legacy"))
        {
            return;
        }

        var hasStatusColumn = ColumnExists(connection, "Consultations_legacy", "Status");
        var statusExpression = hasStatusColumn ? "Status" : $"{(int)ConsultationStatus.Completed}";
        ExecuteNonQuery(
            connection,
            transaction,
            $@"INSERT INTO Consultations (ConsultationId, PatientId, PatientName, Doctor, DateOfVisit, Status, ChiefComplaint, Diagnosis, TreatmentNotes, PrescribedMedicines)
SELECT ConsultationId, PatientId, PatientName, Doctor, DateOfVisit, {statusExpression}, ChiefComplaint, Diagnosis, TreatmentNotes, PrescribedMedicines
FROM Consultations_legacy;");
    }

    private static void CopyLegacyPrescriptionItems(SqliteConnection connection, SqliteTransaction transaction)
    {
        if (!TableExists(connection, "PrescriptionItems_legacy"))
        {
            return;
        }

        var requiredColumns = new[] { "PrescriptionItemId", "ConsultationId", "PatientId", "MedicineId", "MedicineName", "Dosage", "Quantity" };
        if (requiredColumns.Any(column => !ColumnExists(connection, "PrescriptionItems_legacy", column)))
        {
            return;
        }

        var unitPriceExpression = ColumnExists(connection, "PrescriptionItems_legacy", "UnitPrice") ? "UnitPrice" : "0";
        ExecuteNonQuery(
            connection,
            transaction,
            $@"INSERT INTO PrescriptionItems (PrescriptionItemId, ConsultationId, PatientId, MedicineId, MedicineName, Dosage, Quantity, UnitPrice)
SELECT PrescriptionItemId, ConsultationId, PatientId, MedicineId, MedicineName, Dosage, Quantity, {unitPriceExpression}
FROM PrescriptionItems_legacy;");
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
