using System.ComponentModel;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Services;
using ClinicManagementSystem.Ui;

namespace ClinicManagementSystem.Forms;

internal static class EditorForms
{
    public static bool EditPatient(IWin32Window owner, Patient patient)
    {
        using var form = new PatientEditorForm(patient);
        return form.ShowDialog(owner) == DialogResult.OK;
    }

    public static bool EditAppointment(IWin32Window owner, Appointment appointment, ClinicLookupService lookupService)
    {
        using var form = new AppointmentEditorForm(appointment, lookupService);
        return form.ShowDialog(owner) == DialogResult.OK;
    }

    public static bool EditConsultation(
        IWin32Window owner,
        Consultation consultation,
        ClinicLookupService lookupService,
        IEnumerable<PrescriptionItem> existingItems,
        out List<PrescriptionItem> updatedItems)
    {
        using var form = new ConsultationEditorForm(consultation, lookupService, existingItems);
        var accepted = form.ShowDialog(owner) == DialogResult.OK;
        updatedItems = accepted ? form.GetPrescriptionItems() : existingItems.Select(ClonePrescriptionItem).ToList();
        return accepted;
    }

    public static bool EditBilling(IWin32Window owner, BillingRecord billing, ClinicLookupService lookupService, PrescriptionService prescriptionService)
    {
        using var form = new BillingEditorForm(billing, lookupService, prescriptionService);
        return form.ShowDialog(owner) == DialogResult.OK;
    }

    public static bool EditMedicine(IWin32Window owner, Medicine medicine)
    {
        using var form = new MedicineEditorForm(medicine);
        return form.ShowDialog(owner) == DialogResult.OK;
    }

    public static bool EditUser(IWin32Window owner, User user, UserRulesService userRulesService, User? currentUser)
    {
        using var form = new UserEditorForm(user, userRulesService, currentUser);
        return form.ShowDialog(owner) == DialogResult.OK;
    }

    private static PrescriptionItem ClonePrescriptionItem(PrescriptionItem item)
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

internal sealed class PatientEditorForm : Form
{
    private readonly Patient patient;
    private readonly TextBox firstNameTextBox = new() { Width = 240 };
    private readonly TextBox lastNameTextBox = new() { Width = 240 };
    private readonly DateTimePicker birthdatePicker = new() { Width = 240, Format = DateTimePickerFormat.Short };
    private readonly ComboBox sexComboBox = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox addressTextBox = new() { Width = 240, Multiline = true, Height = 72, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox contactTextBox = new() { Width = 240 };
    private readonly DateTimePicker registeredPicker = new() { Width = 240, Format = DateTimePickerFormat.Short };

    public PatientEditorForm(Patient patient)
    {
        this.patient = patient;
        sexComboBox.Items.AddRange(new object[] { "Male", "Female", "Other", "Unspecified" });
        sexComboBox.SelectedItem = patient.Sex;
        firstNameTextBox.Text = patient.FirstName;
        lastNameTextBox.Text = patient.LastName;
        birthdatePicker.Value = patient.Birthdate == default ? DateTime.Today.AddYears(-18) : patient.Birthdate;
        addressTextBox.Text = patient.Address;
        contactTextBox.Text = patient.ContactNumber;
        registeredPicker.Value = patient.DateRegistered == default ? DateTime.Today : patient.DateRegistered;
        BuildForm("Patient Details", SavePatient);
    }

    private void SavePatient()
    {
        var draft = new Patient
        {
            PatientId = patient.PatientId,
            FirstName = firstNameTextBox.Text.Trim(),
            LastName = lastNameTextBox.Text.Trim(),
            Birthdate = birthdatePicker.Value.Date,
            Sex = sexComboBox.SelectedItem?.ToString() ?? "Unspecified",
            Address = addressTextBox.Text.Trim(),
            ContactNumber = contactTextBox.Text.Trim(),
            DateRegistered = registeredPicker.Value.Date
        };

        var error = ClinicValidator.ValidatePatient(draft);
        if (ShowError(error))
        {
            return;
        }

        patient.FirstName = draft.FirstName;
        patient.LastName = draft.LastName;
        patient.Birthdate = draft.Birthdate;
        patient.Sex = draft.Sex;
        patient.Address = draft.Address;
        patient.ContactNumber = draft.ContactNumber;
        patient.DateRegistered = draft.DateRegistered;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void BuildForm(string title, Action saveAction)
    {
        var layout = EditorLayoutFactory.CreateRoot();
        EditorLayoutFactory.AddRow(layout, 0, "First Name", firstNameTextBox);
        EditorLayoutFactory.AddRow(layout, 1, "Last Name", lastNameTextBox);
        EditorLayoutFactory.AddRow(layout, 2, "Birthdate", birthdatePicker);
        EditorLayoutFactory.AddRow(layout, 3, "Sex", sexComboBox);
        EditorLayoutFactory.AddRow(layout, 4, "Address", addressTextBox);
        EditorLayoutFactory.AddRow(layout, 5, "Contact No.", contactTextBox);
        EditorLayoutFactory.AddRow(layout, 6, "Registered", registeredPicker);
        EditorLayoutFactory.ApplyDialogShell(
            this,
            title,
            "Capture the patient identity and registration details used across the clinic workflow.",
            new Size(520, 520),
            layout,
            saveAction,
            Close);
    }

    private bool ShowError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        MessageBox.Show(this, message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }
}

internal sealed class AppointmentEditorForm : Form
{
    private readonly Appointment appointment;
    private readonly List<Patient> patients;
    private readonly List<User> doctors;
    private readonly ComboBox patientComboBox = EditorLayoutFactory.CreateLookupComboBox();
    private readonly ComboBox doctorComboBox = EditorLayoutFactory.CreateLookupComboBox();
    private readonly DateTimePicker datePicker = new() { Width = 240, Format = DateTimePickerFormat.Short };
    private readonly ComboBox timeComboBox = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox statusComboBox = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };

    public AppointmentEditorForm(Appointment appointment, ClinicLookupService lookupService)
    {
        this.appointment = appointment;
        patients = lookupService.GetPatients();
        doctors = lookupService.GetDoctors();

        patientComboBox.DataSource = patients;
        patientComboBox.DisplayMember = nameof(Patient.FullName);
        doctorComboBox.DataSource = doctors;
        doctorComboBox.DisplayMember = nameof(User.FullName);
        timeComboBox.Items.AddRange(new object[] { "08:00 AM", "09:00 AM", "10:00 AM", "11:00 AM", "01:00 PM", "02:00 PM", "03:00 PM", "04:00 PM" });
        statusComboBox.DataSource = Enum.GetValues(typeof(AppointmentStatus));

        SelectPatient(appointment.PatientId);
        SelectDoctor(appointment.DoctorAssigned);
        datePicker.Value = appointment.AppointmentDate == default ? DateTime.Today : appointment.AppointmentDate;
        timeComboBox.SelectedItem = timeComboBox.Items.Contains(appointment.AppointmentTime) ? appointment.AppointmentTime : "09:00 AM";
        statusComboBox.SelectedItem = appointment.Status;

        BuildForm("Appointment Details", SaveAppointment);
    }

    private void SaveAppointment()
    {
        var selectedPatient = patientComboBox.SelectedItem as Patient;
        var selectedDoctor = doctorComboBox.SelectedItem as User;
        var draft = new Appointment
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = selectedPatient?.PatientId ?? string.Empty,
            PatientName = selectedPatient?.FullName ?? string.Empty,
            DoctorAssigned = selectedDoctor?.FullName ?? string.Empty,
            AppointmentDate = datePicker.Value.Date,
            AppointmentTime = timeComboBox.SelectedItem?.ToString() ?? string.Empty,
            Status = statusComboBox.SelectedItem is AppointmentStatus status ? status : AppointmentStatus.Pending
        };

        var error = ClinicValidator.ValidateAppointment(draft);
        if (ShowError(error))
        {
            return;
        }

        appointment.PatientId = draft.PatientId;
        appointment.PatientName = draft.PatientName;
        appointment.DoctorAssigned = draft.DoctorAssigned;
        appointment.AppointmentDate = draft.AppointmentDate;
        appointment.AppointmentTime = draft.AppointmentTime;
        appointment.Status = draft.Status;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void SelectPatient(string patientId)
    {
        patientComboBox.SelectedItem = patients.FirstOrDefault(patient => patient.PatientId == patientId);
    }

    private void SelectDoctor(string doctorName)
    {
        doctorComboBox.SelectedItem = doctors.FirstOrDefault(doctor => doctor.FullName == doctorName);
    }

    private void BuildForm(string title, Action saveAction)
    {
        var layout = EditorLayoutFactory.CreateRoot();
        EditorLayoutFactory.AddRow(layout, 0, "Patient", patientComboBox);
        EditorLayoutFactory.AddRow(layout, 1, "Doctor", doctorComboBox);
        EditorLayoutFactory.AddRow(layout, 2, "Date", datePicker);
        EditorLayoutFactory.AddRow(layout, 3, "Time", timeComboBox);
        EditorLayoutFactory.AddRow(layout, 4, "Status", statusComboBox);
        EditorLayoutFactory.ApplyDialogShell(
            this,
            title,
            "Assign the patient, doctor, schedule, and visit status before the appointment enters the desk queue.",
            new Size(520, 440),
            layout,
            saveAction,
            Close);
    }

    private bool ShowError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        MessageBox.Show(this, message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }
}

internal sealed class ConsultationEditorForm : Form
{
    private readonly Consultation consultation;
    private readonly ClinicLookupService lookupService;
    private readonly List<Patient> patients;
    private readonly List<User> doctors;
    private readonly BindingList<PrescriptionItem> prescriptionItems;
    private readonly ComboBox patientComboBox = EditorLayoutFactory.CreateLookupComboBox();
    private readonly ComboBox doctorComboBox = EditorLayoutFactory.CreateLookupComboBox();
    private readonly DateTimePicker visitDatePicker = new() { Width = 240, Format = DateTimePickerFormat.Short };
    private readonly ComboBox statusComboBox = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox complaintTextBox = new() { Width = 240, Multiline = true, Height = 58, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox diagnosisTextBox = new() { Width = 240, Multiline = true, Height = 58, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox treatmentTextBox = new() { Width = 240, Multiline = true, Height = 88, ScrollBars = ScrollBars.Vertical };
    private readonly DataGridView prescriptionGrid = new()
    {
        Dock = DockStyle.Fill,
        MinimumSize = new Size(0, 140),
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ReadOnly = true,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false
    };
    private readonly Label prescriptionSummaryLabel = new() { AutoSize = true, ForeColor = Color.DimGray };

    public ConsultationEditorForm(Consultation consultation, ClinicLookupService lookupService, IEnumerable<PrescriptionItem> existingItems)
    {
        this.consultation = consultation;
        this.lookupService = lookupService;
        patients = lookupService.GetPatients();
        doctors = lookupService.GetDoctors();
        prescriptionItems = new BindingList<PrescriptionItem>(existingItems.Select(ClonePrescriptionItem).ToList());

        patientComboBox.DataSource = patients;
        patientComboBox.DisplayMember = nameof(Patient.FullName);
        doctorComboBox.DataSource = doctors;
        doctorComboBox.DisplayMember = nameof(User.FullName);
        statusComboBox.DataSource = Enum.GetValues(typeof(ConsultationStatus));

        patientComboBox.SelectedItem = patients.FirstOrDefault(patient => patient.PatientId == consultation.PatientId);
        doctorComboBox.SelectedItem = doctors.FirstOrDefault(doctor => doctor.FullName == consultation.Doctor);
        visitDatePicker.Value = consultation.DateOfVisit == default ? DateTime.Today : consultation.DateOfVisit;
        statusComboBox.SelectedItem = consultation.Status;
        complaintTextBox.Text = consultation.ChiefComplaint;
        diagnosisTextBox.Text = consultation.Diagnosis;
        treatmentTextBox.Text = consultation.TreatmentNotes;
        ConfigurePrescriptionGrid();
        prescriptionGrid.DataSource = prescriptionItems;
        prescriptionGrid.CellDoubleClick += (_, _) => EditSelectedPrescription();
        UpdatePrescriptionSummary();

        BuildForm();
    }

    public List<PrescriptionItem> GetPrescriptionItems()
    {
        return prescriptionItems.Select(ClonePrescriptionItem).ToList();
    }

    private void SaveConsultation()
    {
        var selectedPatient = patientComboBox.SelectedItem as Patient;
        var selectedDoctor = doctorComboBox.SelectedItem as User;
        var draft = new Consultation
        {
            ConsultationId = consultation.ConsultationId,
            PatientId = selectedPatient?.PatientId ?? string.Empty,
            PatientName = selectedPatient?.FullName ?? string.Empty,
            Doctor = selectedDoctor?.FullName ?? string.Empty,
            DateOfVisit = visitDatePicker.Value.Date,
            Status = statusComboBox.SelectedItem is ConsultationStatus status ? status : ConsultationStatus.Pending,
            ChiefComplaint = complaintTextBox.Text.Trim(),
            Diagnosis = diagnosisTextBox.Text.Trim(),
            TreatmentNotes = treatmentTextBox.Text.Trim(),
            PrescribedMedicines = PrescriptionService.BuildSummary(prescriptionItems)
        };

        var error = ClinicValidator.ValidateConsultation(draft)
            ?? prescriptionItems.Select(ClinicValidator.ValidatePrescriptionItem).FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
        if (ShowError(error))
        {
            return;
        }

        consultation.PatientId = draft.PatientId;
        consultation.PatientName = draft.PatientName;
        consultation.Doctor = draft.Doctor;
        consultation.DateOfVisit = draft.DateOfVisit;
        consultation.Status = draft.Status;
        consultation.ChiefComplaint = draft.ChiefComplaint;
        consultation.Diagnosis = draft.Diagnosis;
        consultation.TreatmentNotes = draft.TreatmentNotes;
        consultation.PrescribedMedicines = draft.PrescribedMedicines;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void AddPrescription()
    {
        var item = new PrescriptionItem();
        using var form = new PrescriptionItemEditorForm(item, lookupService);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        prescriptionItems.Add(item);
        UpdatePrescriptionSummary();
    }

    private void EditSelectedPrescription()
    {
        if (prescriptionGrid.CurrentRow?.DataBoundItem is not PrescriptionItem item)
        {
            return;
        }

        using var form = new PrescriptionItemEditorForm(item, lookupService);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        prescriptionGrid.Refresh();
        UpdatePrescriptionSummary();
    }

    private void RemoveSelectedPrescription()
    {
        if (prescriptionGrid.CurrentRow?.DataBoundItem is not PrescriptionItem item)
        {
            return;
        }

        if (MessageBox.Show(this, "Remove the selected prescription item?", "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        prescriptionItems.Remove(item);
        UpdatePrescriptionSummary();
    }

    private void UpdatePrescriptionSummary()
    {
        prescriptionSummaryLabel.Text = $"{prescriptionItems.Count} item(s) | Total medicine cost {prescriptionItems.Sum(item => item.TotalCost):C}";
    }

    private void ConfigurePrescriptionGrid()
    {
        prescriptionGrid.AutoGenerateColumns = false;
        prescriptionGrid.Columns.Clear();
        prescriptionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PrescriptionItem.MedicineName),
            HeaderText = "Medicine"
        });
        prescriptionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PrescriptionItem.Dosage),
            HeaderText = "Dosage"
        });
        prescriptionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PrescriptionItem.Quantity),
            HeaderText = "Qty"
        });
        prescriptionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PrescriptionItem.UnitPrice),
            HeaderText = "Unit Price",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
        });
        prescriptionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PrescriptionItem.TotalCost),
            HeaderText = "Total",
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" }
        });
    }

    private void BuildForm()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = ClinicTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 436));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 326));

        var detailsLayout = EditorLayoutFactory.CreateRoot();
        EditorLayoutFactory.AddRow(detailsLayout, 0, "Patient", patientComboBox);
        EditorLayoutFactory.AddRow(detailsLayout, 1, "Doctor", doctorComboBox);
        EditorLayoutFactory.AddRow(detailsLayout, 2, "Visit Date", visitDatePicker);
        EditorLayoutFactory.AddRow(detailsLayout, 3, "Status", statusComboBox);
        EditorLayoutFactory.AddRow(detailsLayout, 4, "Complaint", complaintTextBox);
        EditorLayoutFactory.AddRow(detailsLayout, 5, "Diagnosis", diagnosisTextBox);
        EditorLayoutFactory.AddRow(detailsLayout, 6, "Treatment", treatmentTextBox);
        var detailsCard = EditorLayoutFactory.WrapInSectionCard(
            "Clinical Details",
            "Capture the encounter, diagnosis, and treatment notes before finalizing prescriptions.",
            ClinicTheme.Brand,
            detailsLayout);

        var prescriptionToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            WrapContents = false,
            BackColor = ClinicTheme.SurfaceRaised,
            Padding = new Padding(0, 0, 0, 8)
        };
        var addButton = CreateSmallButton("Add");
        var editButton = CreateSmallButton("Edit");
        var removeButton = CreateSmallButton("Remove");
        ClinicTheme.StylePrimaryButton(addButton);
        ClinicTheme.StyleSecondaryButton(editButton);
        ClinicTheme.StyleDangerButton(removeButton);
        addButton.Click += (_, _) => AddPrescription();
        editButton.Click += (_, _) => EditSelectedPrescription();
        removeButton.Click += (_, _) => RemoveSelectedPrescription();
        prescriptionToolbar.Controls.Add(addButton);
        prescriptionToolbar.Controls.Add(editButton);
        prescriptionToolbar.Controls.Add(removeButton);
        prescriptionToolbar.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Prescription Items",
            Padding = new Padding(16, 9, 0, 0),
            ForeColor = ClinicTheme.BrandDark,
            Font = ClinicTheme.BodyBold
        });

        ClinicTheme.StyleGrid(prescriptionGrid);
        prescriptionSummaryLabel.ForeColor = ClinicTheme.TextSecondary;
        prescriptionSummaryLabel.Font = ClinicTheme.Body;
        prescriptionSummaryLabel.AutoSize = false;
        prescriptionSummaryLabel.Dock = DockStyle.Fill;
        prescriptionSummaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        var summaryHost = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 44,
            Padding = new Padding(4, 6, 4, 4),
            BackColor = ClinicTheme.SurfaceRaised
        };
        summaryHost.Controls.Add(prescriptionSummaryLabel);

        var prescriptionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = ClinicTheme.SurfaceRaised
        };
        prescriptionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        prescriptionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        prescriptionPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        prescriptionPanel.Controls.Add(prescriptionToolbar, 0, 0);
        prescriptionPanel.Controls.Add(prescriptionGrid, 0, 1);
        prescriptionPanel.Controls.Add(summaryHost, 0, 2);
        var prescriptionCard = EditorLayoutFactory.WrapInSectionCard(
            "Prescription Ledger",
            "Manage structured medicine items and keep the total visible while editing the consultation.",
            ClinicTheme.Success,
            prescriptionPanel);

        root.Controls.Add(detailsCard, 0, 0);
        root.Controls.Add(prescriptionCard, 0, 1);
        EditorLayoutFactory.ApplyDialogShell(
            this,
            "Consultation Details",
            "Record the clinical encounter and manage prescription items in one place.",
            new Size(980, 820),
            root,
            SaveConsultation,
            Close);
    }

    private static Button CreateSmallButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 92,
            Height = 34,
            Margin = new Padding(0, 0, 8, 0)
        };
    }

    private static PrescriptionItem ClonePrescriptionItem(PrescriptionItem item)
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

    private bool ShowError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        MessageBox.Show(this, message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }
}

internal sealed class PrescriptionItemEditorForm : Form
{
    private readonly PrescriptionItem prescriptionItem;
    private readonly List<Medicine> medicines;
    private readonly ComboBox medicineComboBox = EditorLayoutFactory.CreateLookupComboBox();
    private readonly TextBox dosageTextBox = new() { Width = 240 };
    private readonly NumericUpDown quantityInput = new() { Width = 240, Maximum = 100000, Minimum = 1 };
    private readonly Label unitPriceLabel = new() { AutoSize = true };
    private readonly Label stockLabel = new() { AutoSize = true };

    public PrescriptionItemEditorForm(PrescriptionItem prescriptionItem, ClinicLookupService lookupService)
    {
        this.prescriptionItem = prescriptionItem;
        medicines = lookupService.GetMedicines();

        medicineComboBox.DataSource = medicines;
        medicineComboBox.DisplayMember = nameof(Medicine.MedicineName);
        medicineComboBox.SelectedItem = medicines.FirstOrDefault(medicine => medicine.MedicineId == prescriptionItem.MedicineId);
        medicineComboBox.SelectedIndexChanged += (_, _) => UpdateMedicineDetails();
        dosageTextBox.Text = prescriptionItem.Dosage;
        quantityInput.Value = Math.Max(1, prescriptionItem.Quantity);
        UpdateMedicineDetails();
        BuildForm();
    }

    private void SavePrescription()
    {
        var selectedMedicine = medicineComboBox.SelectedItem as Medicine;
        var draft = new PrescriptionItem
        {
            PrescriptionItemId = prescriptionItem.PrescriptionItemId,
            ConsultationId = prescriptionItem.ConsultationId,
            PatientId = prescriptionItem.PatientId,
            MedicineId = selectedMedicine?.MedicineId ?? string.Empty,
            MedicineName = selectedMedicine?.MedicineName ?? string.Empty,
            Dosage = dosageTextBox.Text.Trim(),
            Quantity = (int)quantityInput.Value,
            UnitPrice = selectedMedicine?.UnitPrice ?? 0m
        };

        var error = ClinicValidator.ValidatePrescriptionItem(draft);
        if (ShowError(error))
        {
            return;
        }

        prescriptionItem.MedicineId = draft.MedicineId;
        prescriptionItem.MedicineName = draft.MedicineName;
        prescriptionItem.Dosage = draft.Dosage;
        prescriptionItem.Quantity = draft.Quantity;
        prescriptionItem.UnitPrice = draft.UnitPrice;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdateMedicineDetails()
    {
        if (medicineComboBox.SelectedItem is Medicine medicine)
        {
            unitPriceLabel.Text = medicine.UnitPrice.ToString("C");
            stockLabel.Text = $"{medicine.Quantity} in stock";
            return;
        }

        unitPriceLabel.Text = 0m.ToString("C");
        stockLabel.Text = "No medicine selected";
    }

    private void BuildForm()
    {
        var layout = EditorLayoutFactory.CreateRoot();
        EditorLayoutFactory.AddRow(layout, 0, "Medicine", medicineComboBox);
        EditorLayoutFactory.AddRow(layout, 1, "Dosage", dosageTextBox);
        EditorLayoutFactory.AddRow(layout, 2, "Quantity", quantityInput);
        EditorLayoutFactory.AddRow(layout, 3, "Unit Price", unitPriceLabel);
        EditorLayoutFactory.AddRow(layout, 4, "Stock", stockLabel);
        EditorLayoutFactory.ApplyDialogShell(
            this,
            "Prescription Item",
            "Select the medicine, dosage, and quantity that will be stored under the consultation.",
            new Size(520, 400),
            layout,
            SavePrescription,
            Close);
    }

    private bool ShowError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        MessageBox.Show(this, message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }
}

internal sealed class BillingEditorForm : Form
{
    private readonly BillingRecord billing;
    private readonly PrescriptionService prescriptionService;
    private readonly List<ConsultationOption> consultationOptions;
    private readonly bool isNewBilling;
    private readonly ComboBox consultationComboBox = EditorLayoutFactory.CreateLookupComboBox();
    private readonly TextBox patientTextBox = new() { Width = 240, ReadOnly = true };
    private readonly NumericUpDown serviceChargeInput = new() { Width = 240, DecimalPlaces = 2, Maximum = 1000000 };
    private readonly NumericUpDown medicineChargeInput = new() { Width = 240, DecimalPlaces = 2, Maximum = 1000000 };
    private readonly ComboBox paymentStatusComboBox = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label suggestedChargeLabel = new() { AutoSize = true, ForeColor = Color.DimGray };
    private string lastSelectedConsultationId = string.Empty;

    public BillingEditorForm(BillingRecord billing, ClinicLookupService lookupService, PrescriptionService prescriptionService)
    {
        this.billing = billing;
        this.prescriptionService = prescriptionService;
        consultationOptions = lookupService.GetCompletedConsultations().Select(consultation => new ConsultationOption(consultation)).ToList();
        isNewBilling = string.IsNullOrWhiteSpace(billing.BillingId);

        consultationComboBox.DataSource = consultationOptions;
        consultationComboBox.DisplayMember = nameof(ConsultationOption.DisplayText);
        consultationComboBox.ValueMember = nameof(ConsultationOption.Id);
        consultationComboBox.SelectedIndexChanged += (_, _) => UpdatePatientDisplay();

        paymentStatusComboBox.DataSource = Enum.GetValues(typeof(PaymentStatus))
            .Cast<PaymentStatus>()
            .ToList();
        lastSelectedConsultationId = billing.ConsultationId;
        if (!string.IsNullOrWhiteSpace(billing.ConsultationId))
        {
            consultationComboBox.SelectedItem = consultationOptions.FirstOrDefault(option => option.Id == billing.ConsultationId);
        }
        else if (consultationOptions.Count > 0 && consultationComboBox.SelectedItem is null)
        {
            consultationComboBox.SelectedItem = consultationOptions[0];
        }

        serviceChargeInput.Value = billing.ServiceCharges;
        medicineChargeInput.Value = billing.MedicineCharges;
        SetPaymentStatusSelection(billing.PaymentStatus);
        UpdatePatientDisplay();

        BuildForm("Billing Details", SaveBilling);
        SetPaymentStatusSelection(billing.PaymentStatus);
    }

    private void SetPaymentStatusSelection(PaymentStatus status)
    {
        if (paymentStatusComboBox.Items.Count == 0)
        {
            return;
        }

        for (var index = 0; index < paymentStatusComboBox.Items.Count; index++)
        {
            if (paymentStatusComboBox.Items[index] is PaymentStatus itemStatus && itemStatus == status)
            {
                paymentStatusComboBox.SelectedIndex = index;
                return;
            }
        }

        paymentStatusComboBox.SelectedIndex = 0;
    }

    private void UpdatePatientDisplay()
    {
        var option = ResolveSelectedConsultationOption();
        if (option is not null)
        {
            patientTextBox.Text = option.PatientName;
            var prescriptionItems = prescriptionService.GetByConsultation(option.Id);
            var suggestedCharge = prescriptionService.GetTotalCostForConsultation(option.Id);
            suggestedChargeLabel.Text = prescriptionItems.Count == 0
                ? "Suggested from prescription: none"
                : $"Suggested from prescription: {suggestedCharge:C} ({prescriptionItems.Count} item(s))";

            var consultationChanged = !string.Equals(lastSelectedConsultationId, option.Id, StringComparison.Ordinal);
            if (consultationChanged || isNewBilling || medicineChargeInput.Value == 0)
            {
                medicineChargeInput.Value = Math.Min(medicineChargeInput.Maximum, suggestedCharge);
            }

            lastSelectedConsultationId = option.Id;
        }
        else
        {
            patientTextBox.Text = string.Empty;
            suggestedChargeLabel.Text = "Suggested from prescription: none";
            medicineChargeInput.Value = 0;
            lastSelectedConsultationId = string.Empty;
        }
    }

    private ConsultationOption? ResolveSelectedConsultationOption()
    {
        if (consultationComboBox.SelectedItem is ConsultationOption selectedOption)
        {
            return selectedOption;
        }

        if (consultationComboBox.SelectedValue is string selectedId)
        {
            return consultationOptions.FirstOrDefault(option => option.Id == selectedId);
        }

        if (consultationComboBox.SelectedIndex >= 0 && consultationComboBox.SelectedIndex < consultationOptions.Count)
        {
            return consultationOptions[consultationComboBox.SelectedIndex];
        }

        if (consultationOptions.Count > 0)
        {
            return consultationOptions[0];
        }

        return null;
    }

    private void SaveBilling()
    {
        var selectedConsultation = ResolveSelectedConsultationOption();
        var draft = new BillingRecord
        {
            BillingId = billing.BillingId,
            ConsultationId = selectedConsultation?.Id ?? string.Empty,
            PatientId = selectedConsultation?.PatientId ?? string.Empty,
            PatientName = selectedConsultation?.PatientName ?? string.Empty,
            ServiceCharges = serviceChargeInput.Value,
            MedicineCharges = medicineChargeInput.Value,
            PaymentStatus = paymentStatusComboBox.SelectedItem is PaymentStatus status ? status : PaymentStatus.Unpaid
        };

        var error = ClinicValidator.ValidateBilling(draft, selectedConsultation?.Consultation);
        if (ShowError(error))
        {
            return;
        }

        billing.ConsultationId = draft.ConsultationId;
        billing.PatientId = draft.PatientId;
        billing.PatientName = draft.PatientName;
        billing.ServiceCharges = draft.ServiceCharges;
        billing.MedicineCharges = draft.MedicineCharges;
        billing.PaymentStatus = draft.PaymentStatus;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void BuildForm(string title, Action saveAction)
    {
        var layout = EditorLayoutFactory.CreateRoot();
        EditorLayoutFactory.AddRow(layout, 0, "Consultation", consultationComboBox);
        EditorLayoutFactory.AddRow(layout, 1, "Patient", patientTextBox);
        EditorLayoutFactory.AddRow(layout, 2, "Service Charges", serviceChargeInput);
        EditorLayoutFactory.AddRow(layout, 3, "Medicine Charges", medicineChargeInput);
        EditorLayoutFactory.AddRow(layout, 4, "Suggested", suggestedChargeLabel);
        EditorLayoutFactory.AddRow(layout, 5, "Payment Status", paymentStatusComboBox);
        EditorLayoutFactory.ApplyDialogShell(
            this,
            title,
            "Link the completed consultation, review suggested medicine totals, and set the payment status.",
            new Size(560, 500),
            layout,
            saveAction,
            Close);
    }

    private bool ShowError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        MessageBox.Show(this, message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }

    private sealed class ConsultationOption
    {
        public ConsultationOption(Consultation consultation)
        {
            Consultation = consultation;
            Id = consultation.ConsultationId;
            PatientId = consultation.PatientId;
            PatientName = consultation.PatientName;
            DisplayText = $"{consultation.ConsultationId} - {consultation.PatientName} ({consultation.DateOfVisit:d}, {consultation.Status})";
        }

        public Consultation Consultation { get; }
        public string Id { get; }
        public string PatientId { get; }
        public string PatientName { get; }
        public string DisplayText { get; }
    }
}

internal sealed class MedicineEditorForm : Form
{
    private readonly Medicine medicine;
    private readonly TextBox nameTextBox = new() { Width = 240 };
    private readonly TextBox categoryTextBox = new() { Width = 240 };
    private readonly NumericUpDown quantityInput = new() { Width = 240, Maximum = 100000 };
    private readonly NumericUpDown priceInput = new() { Width = 240, DecimalPlaces = 2, Maximum = 1000000 };
    private readonly DateTimePicker expirationPicker = new() { Width = 240, Format = DateTimePickerFormat.Short };

    public MedicineEditorForm(Medicine medicine)
    {
        this.medicine = medicine;
        nameTextBox.Text = medicine.MedicineName;
        categoryTextBox.Text = medicine.Category;
        quantityInput.Value = medicine.Quantity;
        priceInput.Value = medicine.UnitPrice;
        expirationPicker.Value = medicine.ExpirationDate == default ? DateTime.Today.AddYears(1) : medicine.ExpirationDate;
        BuildForm("Medicine Details", SaveMedicine);
    }

    private void SaveMedicine()
    {
        var draft = new Medicine
        {
            MedicineId = medicine.MedicineId,
            MedicineName = nameTextBox.Text.Trim(),
            Category = categoryTextBox.Text.Trim(),
            Quantity = (int)quantityInput.Value,
            UnitPrice = priceInput.Value,
            ExpirationDate = expirationPicker.Value.Date
        };

        var error = ClinicValidator.ValidateMedicine(draft);
        if (ShowError(error))
        {
            return;
        }

        medicine.MedicineName = draft.MedicineName;
        medicine.Category = draft.Category;
        medicine.Quantity = draft.Quantity;
        medicine.UnitPrice = draft.UnitPrice;
        medicine.ExpirationDate = draft.ExpirationDate;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void BuildForm(string title, Action saveAction)
    {
        var layout = EditorLayoutFactory.CreateRoot();
        EditorLayoutFactory.AddRow(layout, 0, "Medicine Name", nameTextBox);
        EditorLayoutFactory.AddRow(layout, 1, "Category", categoryTextBox);
        EditorLayoutFactory.AddRow(layout, 2, "Quantity", quantityInput);
        EditorLayoutFactory.AddRow(layout, 3, "Unit Price", priceInput);
        EditorLayoutFactory.AddRow(layout, 4, "Expiration", expirationPicker);
        EditorLayoutFactory.ApplyDialogShell(
            this,
            title,
            "Maintain stock, pricing, and expiration data for the clinic inventory.",
            new Size(520, 430),
            layout,
            saveAction,
            Close);
    }

    private bool ShowError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        MessageBox.Show(this, message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }
}

internal sealed class UserEditorForm : Form
{
    private readonly User user;
    private readonly UserRulesService userRulesService;
    private readonly User? currentUser;
    private readonly TextBox usernameTextBox = new() { Width = 240 };
    private readonly TextBox passwordTextBox = new() { Width = 240 };
    private readonly TextBox fullNameTextBox = new() { Width = 240 };
    private readonly ComboBox roleComboBox = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox activeCheckBox = new() { AutoSize = true, Text = "Active user" };
    private readonly CheckBox showPasswordCheckBox = new() { AutoSize = true, Text = "Show password" };

    public UserEditorForm(User user, UserRulesService userRulesService, User? currentUser)
    {
        this.user = user;
        this.userRulesService = userRulesService;
        this.currentUser = currentUser;
        usernameTextBox.Text = user.Username;
        passwordTextBox.Text = user.Password;
        passwordTextBox.UseSystemPasswordChar = true;
        fullNameTextBox.Text = user.FullName;
        roleComboBox.DataSource = Enum.GetValues(typeof(UserRole));
        roleComboBox.SelectedItem = user.Role;
        activeCheckBox.Checked = user.IsActive;
        showPasswordCheckBox.CheckedChanged += (_, _) => passwordTextBox.UseSystemPasswordChar = !showPasswordCheckBox.Checked;
        BuildForm("User Details", SaveUser);
    }

    private void SaveUser()
    {
        var draft = new User
        {
            UserId = user.UserId,
            Username = usernameTextBox.Text.Trim(),
            Password = passwordTextBox.Text,
            FullName = fullNameTextBox.Text.Trim(),
            Role = roleComboBox.SelectedItem is UserRole role ? role : UserRole.Receptionist,
            IsActive = activeCheckBox.Checked
        };

        var error = userRulesService.Validate(draft, user, currentUser);
        if (ShowError(error))
        {
            return;
        }

        user.Username = draft.Username;
        user.Password = draft.Password;
        user.FullName = draft.FullName;
        user.Role = draft.Role;
        user.IsActive = draft.IsActive;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void BuildForm(string title, Action saveAction)
    {
        var layout = EditorLayoutFactory.CreateRoot();
        EditorLayoutFactory.AddRow(layout, 0, "Username", usernameTextBox);
        EditorLayoutFactory.AddRow(layout, 1, "Password", passwordTextBox);
        EditorLayoutFactory.AddRow(layout, 2, "Full Name", fullNameTextBox);
        EditorLayoutFactory.AddRow(layout, 3, "Role", roleComboBox);
        EditorLayoutFactory.AddRow(layout, 4, string.Empty, activeCheckBox);
        EditorLayoutFactory.AddRow(layout, 5, string.Empty, showPasswordCheckBox);
        EditorLayoutFactory.ApplyDialogShell(
            this,
            title,
            "Configure the account credentials, role, and activation state used for clinic access.",
            new Size(520, 450),
            layout,
            saveAction,
            Close);
    }

    private bool ShowError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        MessageBox.Show(this, message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }
}

internal static class EditorLayoutFactory
{
    public static TableLayoutPanel CreateRoot()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 0,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
            BackColor = ClinicTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return layout;
    }

    public static ComboBox CreateLookupComboBox()
    {
        var comboBox = new ComboBox
        {
            Width = 240,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true
        };
        StyleEditorControl(comboBox);
        return comboBox;
    }

    public static void AddRow(TableLayoutPanel layout, int row, string labelText, Control control)
    {
        while (layout.RowStyles.Count <= row)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        layout.RowCount = Math.Max(layout.RowCount, row + 1);

        StyleEditorControl(control);

        var label = new Label
        {
            AutoSize = true,
            Text = labelText,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(0, 8, 0, 0),
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary
        };
        label.Visible = !string.IsNullOrWhiteSpace(labelText);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        control.Margin = new Padding(3, 3, 3, 8);
        if (string.IsNullOrWhiteSpace(labelText))
        {
            control.Margin = new Padding(0, 6, 0, 8);
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
            return;
        }

        if (control is not CheckBox)
        {
            control.Width = Math.Max(control.Width, 280);
        }

        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    public static void ApplyDialogShell(
        Form form,
        string title,
        string subtitle,
        Size clientSize,
        Control content,
        Action saveAction,
        Action cancelAction)
    {
        form.Text = title;
        form.FormBorderStyle = FormBorderStyle.Sizable;
        form.StartPosition = FormStartPosition.CenterParent;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.SizeGripStyle = SizeGripStyle.Show;
        form.AutoScaleMode = AutoScaleMode.Font;
        ClinicTheme.StyleSurface(form);

        var workingArea = Screen.FromControl(form).WorkingArea;
        var maxClientWidth = Math.Max(460, workingArea.Width - 80);
        var maxClientHeight = Math.Max(440, workingArea.Height - 24);
        var adjustedClientSize = new Size(
            Math.Min(clientSize.Width, maxClientWidth),
            Math.Min(clientSize.Height, maxClientHeight));
        form.ClientSize = adjustedClientSize;

        var frameWidth = form.Width - form.ClientSize.Width;
        var frameHeight = form.Height - form.ClientSize.Height;
        var minClientWidth = Math.Min(Math.Min(clientSize.Width, 520), maxClientWidth);
        var minClientHeight = Math.Min(Math.Min(clientSize.Height, 430), maxClientHeight);
        form.MinimumSize = new Size(minClientWidth + frameWidth, minClientHeight + frameHeight);
        form.MaximumSize = new Size(maxClientWidth + frameWidth, maxClientHeight + frameHeight);

        var useScrollableContent = false;
        if (content is TableLayoutPanel rootLayout)
        {
            useScrollableContent = !HasPercentRows(rootLayout);
            if (useScrollableContent)
            {
                rootLayout.AutoSize = true;
                rootLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                content.Dock = DockStyle.Top;
            }
            else
            {
                content.Dock = DockStyle.Fill;
            }
        }
        else
        {
            content.Dock = DockStyle.Fill;
        }

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = ClinicTheme.AppBackground
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

        var header = new GradientPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(20, 18, 20, 18),
            StartColor = ClinicTheme.BrandDark,
            EndColor = ClinicTheme.Brand,
            ShapeColor = Color.FromArgb(18, 255, 255, 255)
        };
        ClinicTheme.RoundControl(header, 24);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Font = ClinicTheme.DisplayMedium,
            ForeColor = Color.White,
            Location = new Point(0, 0),
            BackColor = Color.Transparent
        };
        var subtitleLabel = new Label
        {
            AutoSize = true,
            Text = subtitle,
            Font = ClinicTheme.Body,
            ForeColor = Color.FromArgb(232, 243, 250),
            MaximumSize = new Size(Math.Max(120, adjustedClientSize.Width - 92), 0),
            Location = new Point(0, 40),
            BackColor = Color.Transparent
        };
        void SyncHeaderWrapping()
        {
            subtitleLabel.MaximumSize = new Size(Math.Max(120, header.ClientSize.Width - 40), 0);
        }

        header.Resize += (_, _) => SyncHeaderWrapping();
        header.Controls.Add(titleLabel);
        header.Controls.Add(subtitleLabel);
        SyncHeaderWrapping();

        var bodyCard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            BackColor = ClinicTheme.Surface
        };
        ClinicTheme.StyleCard(bodyCard, ClinicTheme.Surface, 24);
        var bodyViewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = useScrollableContent,
            BackColor = ClinicTheme.Surface
        };
        bodyViewport.Controls.Add(content);
        bodyCard.Controls.Add(bodyViewport);

        void SyncContentWidth()
        {
            var reserved = bodyViewport.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            content.Width = Math.Max(320, bodyViewport.ClientSize.Width - reserved);
        }

        if (useScrollableContent)
        {
            bodyViewport.Resize += (_, _) => SyncContentWidth();
            bodyViewport.Layout += (_, _) => SyncContentWidth();
            SyncContentWidth();
        }

        var actionsHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ClinicTheme.AppBackground,
            Padding = new Padding(0, 8, 0, 6)
        };
        var actions = CreateButtonPanel(saveAction, cancelAction, out var saveButton, out var cancelButton);
        actions.Dock = DockStyle.None;
        actionsHost.Controls.Add(actions);
        shell.RowStyles[2].Height = Math.Max(actions.Height + actionsHost.Padding.Vertical + 2, 78);

        void PositionActions()
        {
            var x = Math.Max(0, actionsHost.ClientSize.Width - actions.Width);
            var y = Math.Max(0, actionsHost.Padding.Top + ((actionsHost.ClientSize.Height - actionsHost.Padding.Vertical - actions.Height) / 2));
            actions.Location = new Point(x, y);
        }

        actionsHost.Resize += (_, _) => PositionActions();
        PositionActions();
        form.AcceptButton = saveButton;
        form.CancelButton = cancelButton;

        shell.Controls.Add(header, 0, 0);
        shell.Controls.Add(bodyCard, 0, 1);
        shell.Controls.Add(actionsHost, 0, 2);
        form.Controls.Clear();
        form.Controls.Add(shell);
    }

    public static Panel WrapInSectionCard(string title, string subtitle, Color accent, Control content)
    {
        var useScrollableContent = false;
        if (content is TableLayoutPanel sectionLayout)
        {
            useScrollableContent = !HasPercentRows(sectionLayout);
            if (useScrollableContent)
            {
                sectionLayout.AutoSize = true;
                sectionLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                content.Dock = DockStyle.Top;
            }
            else
            {
                content.Dock = DockStyle.Fill;
            }
        }
        else
        {
            content.Dock = DockStyle.Fill;
        }

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 16),
            BackColor = ClinicTheme.SurfaceRaised
        };
        ClinicTheme.StyleCard(card, ClinicTheme.SurfaceRaised, 22);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ClinicTheme.SurfaceRaised
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = ClinicTheme.SurfaceRaised
        };
        var accentBar = new Panel
        {
            Width = 34,
            Height = 4,
            BackColor = accent,
            Location = new Point(0, 0)
        };
        ClinicTheme.RoundControl(accentBar, 2);
        var titleLabel = new Label
        {
            AutoSize = true,
            Text = title,
            Font = ClinicTheme.BodyBold,
            ForeColor = ClinicTheme.TextPrimary,
            Location = new Point(0, 14)
        };
        var subtitleLabel = new Label
        {
            AutoSize = true,
            Text = subtitle,
            Font = ClinicTheme.Caption,
            ForeColor = ClinicTheme.TextSecondary,
            MaximumSize = new Size(760, 0),
            Location = new Point(0, 34)
        };
        void SyncHeaderWrapping()
        {
            subtitleLabel.MaximumSize = new Size(Math.Max(120, header.ClientSize.Width - 8), 0);
        }

        header.Resize += (_, _) => SyncHeaderWrapping();
        header.Controls.Add(accentBar);
        header.Controls.Add(titleLabel);
        header.Controls.Add(subtitleLabel);
        SyncHeaderWrapping();

        var contentViewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = useScrollableContent,
            BackColor = ClinicTheme.SurfaceRaised
        };
        contentViewport.Controls.Add(content);

        void SyncContentWidth()
        {
            var reserved = contentViewport.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            content.Width = Math.Max(280, contentViewport.ClientSize.Width - reserved);
        }

        if (useScrollableContent)
        {
            contentViewport.Resize += (_, _) => SyncContentWidth();
            contentViewport.Layout += (_, _) => SyncContentWidth();
            SyncContentWidth();
        }

        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(contentViewport, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    public static FlowLayoutPanel CreateButtonPanel(Action saveAction, Action cancelAction, out Button saveButton, out Button cancelButton)
    {
        saveButton = new Button
        {
            Text = "Save",
            Width = 112,
            Height = 38,
            Margin = new Padding(0, 0, 10, 0)
        };
        ClinicTheme.StylePrimaryButton(saveButton);
        saveButton.Click += (_, _) => saveAction();

        cancelButton = new Button
        {
            Text = "Cancel",
            Width = 112,
            Height = 38
        };
        ClinicTheme.StyleSecondaryButton(cancelButton);
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Click += (_, _) => cancelAction();

        return new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Controls = { saveButton, cancelButton },
            Margin = new Padding(0)
        };
    }

    private static void StyleEditorControl(Control control)
    {
        switch (control)
        {
            case TextBox textBox:
                ClinicTheme.StyleTextBox(textBox);
                textBox.Font = ClinicTheme.Body;
                if (textBox.ReadOnly)
                {
                    textBox.BackColor = ClinicTheme.SurfaceMuted;
                }
                break;
            case ComboBox comboBox:
                comboBox.FlatStyle = FlatStyle.Flat;
                comboBox.BackColor = ClinicTheme.Surface;
                comboBox.ForeColor = ClinicTheme.TextPrimary;
                comboBox.Font = ClinicTheme.Body;
                comboBox.IntegralHeight = false;
                comboBox.MaxDropDownItems = 12;
                comboBox.DropDownHeight = 280;
                break;
            case NumericUpDown numericUpDown:
                numericUpDown.BorderStyle = BorderStyle.FixedSingle;
                numericUpDown.BackColor = ClinicTheme.Surface;
                numericUpDown.ForeColor = ClinicTheme.TextPrimary;
                numericUpDown.Font = ClinicTheme.Body;
                numericUpDown.Height = 34;
                numericUpDown.TextAlign = HorizontalAlignment.Right;
                break;
            case DateTimePicker dateTimePicker:
                dateTimePicker.CalendarForeColor = ClinicTheme.TextPrimary;
                dateTimePicker.CalendarMonthBackground = Color.White;
                dateTimePicker.CalendarTitleBackColor = ClinicTheme.Brand;
                dateTimePicker.CalendarTitleForeColor = Color.White;
                dateTimePicker.Font = ClinicTheme.Body;
                break;
            case CheckBox checkBox:
                checkBox.ForeColor = ClinicTheme.TextPrimary;
                checkBox.Font = ClinicTheme.BodyBold;
                checkBox.BackColor = ClinicTheme.Surface;
                break;
            case Label label:
                label.ForeColor = ClinicTheme.TextSecondary;
                label.Font = ClinicTheme.Body;
                break;
        }
    }

    private static bool HasPercentRows(TableLayoutPanel layout)
    {
        return layout.RowStyles.Cast<RowStyle>().Any(style => style.SizeType == SizeType.Percent);
    }
}
