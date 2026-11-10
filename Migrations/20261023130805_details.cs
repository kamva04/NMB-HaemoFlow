using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NMB_HLabSys.Migrations
{
    /// <inheritdoc />
    public partial class details : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    AdminId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.AdminId);
                });

            migrationBuilder.CreateTable(
                name: "AllergyCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllergyCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "MedicalConditionCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalConditionCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "MedicationCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "ResultReleases",
                columns: table => new
                {
                    ReleaseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotifyType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultReleases", x => x.ReleaseId);
                });

            migrationBuilder.CreateTable(
                name: "SampleTypes",
                columns: table => new
                {
                    SampleTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SampleTypes", x => x.SampleTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierId);
                });

            migrationBuilder.CreateTable(
                name: "TestCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFirstLogin = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PasswordResetTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PasswordResetTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "StockOrders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockOrders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_StockOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestTypes",
                columns: table => new
                {
                    TestTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SampleTypeId = table.Column<int>(type: "int", nullable: false),
                    Units = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalRangeMin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NormalRangeMax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TurnaroundMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTypes", x => x.TestTypeId);
                    table.ForeignKey(
                        name: "FK_TestTypes_SampleTypes_SampleTypeId",
                        column: x => x.SampleTypeId,
                        principalTable: "SampleTypes",
                        principalColumn: "SampleTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestTypes_TestCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TestCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    DoctorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HPCSANumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.DoctorId);
                    table.ForeignKey(
                        name: "FK_Doctors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Managers",
                columns: table => new
                {
                    ManagerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Managers", x => x.ManagerId);
                    table.ForeignKey(
                        name: "FK_Managers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CalendarStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CalendarEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SAIDNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CellNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Street2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Suburb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Town = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                    table.ForeignKey(
                        name: "FK_Patients_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    TechnicianId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SAIDNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePicturePath = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.TechnicianId);
                    table.ForeignKey(
                        name: "FK_Technicians_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consumables",
                columns: table => new
                {
                    ConsumableId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumableName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumables", x => x.ConsumableId);
                    table.ForeignKey(
                        name: "FK_Consumables_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Consumables_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "TestTypeId");
                });

            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    AllergyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    AllergyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergies", x => x.AllergyId);
                    table.ForeignKey(
                        name: "FK_Allergies_AllergyCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AllergyCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Allergies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId");
                });

            migrationBuilder.CreateTable(
                name: "MedicalConditions",
                columns: table => new
                {
                    ConditionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ConditionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalConditions", x => x.ConditionId);
                    table.ForeignKey(
                        name: "FK_MedicalConditions_MedicalConditionCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MedicalConditionCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalConditions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId");
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    MedicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    MedicationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.MedicationId);
                    table.ForeignKey(
                        name: "FK_Medications_MedicationCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MedicationCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Medications_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId");
                });

            migrationBuilder.CreateTable(
                name: "TestRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Urgency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_TestRequests_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId");
                    table.ForeignKey(
                        name: "FK_TestRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId");
                });

            migrationBuilder.CreateTable(
                name: "TechnicianTestTypes",
                columns: table => new
                {
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianTestTypes", x => new { x.TechnicianId, x.TestTypeId });
                    table.ForeignKey(
                        name: "FK_TechnicianTestTypes_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicianTestTypes_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "TestTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsumableRequests",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    QuantityRequested = table.Column<int>(type: "int", nullable: false),
                    QuantityApproved = table.Column<int>(type: "int", nullable: true),
                    QuantityFulfilled = table.Column<int>(type: "int", nullable: true),
                    Urgency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedRequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManagerDecisionNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_ConsumableRequests_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "ConsumableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumableRequests_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockOrderItems",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    QuantityOrdered = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateReceived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateCancelled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockOrderOrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockOrderItems", x => x.OrderItemId);
                    table.ForeignKey(
                        name: "FK_StockOrderItems_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "ConsumableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockOrderItems_StockOrders_StockOrderOrderId",
                        column: x => x.StockOrderOrderId,
                        principalTable: "StockOrders",
                        principalColumn: "OrderId");
                });

            migrationBuilder.CreateTable(
                name: "StockProblemReports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: false),
                    IssueType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProblemReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_StockProblemReports_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "ConsumableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockProblemReports_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestTypeConsumables",
                columns: table => new
                {
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    QuantityUsed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTypeConsumables", x => new { x.TestTypeId, x.ConsumableId });
                    table.ForeignKey(
                        name: "FK_TestTypeConsumables_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "ConsumableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestTypeConsumables_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "TestTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAllergies",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    AllergyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDoctorAdded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAllergies", x => new { x.PatientId, x.AllergyId });
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Allergies_AllergyId",
                        column: x => x.AllergyId,
                        principalTable: "Allergies",
                        principalColumn: "AllergyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientConditions",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    ConditionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDoctorAdded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientConditions", x => new { x.PatientId, x.ConditionId });
                    table.ForeignKey(
                        name: "FK_PatientConditions_MedicalConditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "MedicalConditions",
                        principalColumn: "ConditionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientConditions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientMedications",
                columns: table => new
                {
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDoctorAdded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientMedications", x => new { x.PatientId, x.MedicationId });
                    table.ForeignKey(
                        name: "FK_PatientMedications_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "MedicationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientMedications_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAppointments",
                columns: table => new
                {
                    AppointmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: true),
                    ProposedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAppointments", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "FK_PatientAppointments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId");
                    table.ForeignKey(
                        name: "FK_PatientAppointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId");
                    table.ForeignKey(
                        name: "FK_PatientAppointments_TestRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "TestRequests",
                        principalColumn: "RequestId");
                });

            migrationBuilder.CreateTable(
                name: "RequestCancellations",
                columns: table => new
                {
                    CancellationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    CancelledBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestCancellations", x => x.CancellationId);
                    table.ForeignKey(
                        name: "FK_RequestCancellations_TestRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "TestRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestTestTypes",
                columns: table => new
                {
                    RequestTestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnicianId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ResultNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAbnormal = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedById = table.Column<int>(type: "int", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresReVerification = table.Column<bool>(type: "bit", nullable: false),
                    ReVerificationNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentBackById = table.Column<int>(type: "int", nullable: true),
                    SentBackAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestTestTypes", x => x.RequestTestId);
                    table.ForeignKey(
                        name: "FK_RequestTestTypes_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "TechnicianId");
                    table.ForeignKey(
                        name: "FK_RequestTestTypes_TestRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "TestRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestTestTypes_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "TestTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestSamples",
                columns: table => new
                {
                    SampleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SampleTypeId = table.Column<int>(type: "int", nullable: false),
                    RequestTestId = table.Column<int>(type: "int", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedBy = table.Column<int>(type: "int", nullable: true),
                    EditedByTechnicianId = table.Column<int>(type: "int", nullable: true),
                    EditNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<int>(type: "int", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestSamples", x => x.SampleId);
                    table.ForeignKey(
                        name: "FK_RequestSamples_RequestTestTypes_RequestTestId",
                        column: x => x.RequestTestId,
                        principalTable: "RequestTestTypes",
                        principalColumn: "RequestTestId");
                    table.ForeignKey(
                        name: "FK_RequestSamples_SampleTypes_SampleTypeId",
                        column: x => x.SampleTypeId,
                        principalTable: "SampleTypes",
                        principalColumn: "SampleTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestSamples_TestRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "TestRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestConsumableUsages",
                columns: table => new
                {
                    UsageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTestId = table.Column<int>(type: "int", nullable: false),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    QuantityUsed = table.Column<int>(type: "int", nullable: false),
                    UsedByTechnicianId = table.Column<int>(type: "int", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestConsumableUsages", x => x.UsageId);
                    table.ForeignKey(
                        name: "FK_TestConsumableUsages_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "ConsumableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestConsumableUsages_RequestTestTypes_RequestTestId",
                        column: x => x.RequestTestId,
                        principalTable: "RequestTestTypes",
                        principalColumn: "RequestTestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestResultHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestTestId = table.Column<int>(type: "int", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionBy = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequestTestTypeRequestTestId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResultHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_TestResultHistories_RequestTestTypes_RequestTestTypeRequestTestId",
                        column: x => x.RequestTestTypeRequestTestId,
                        principalTable: "RequestTestTypes",
                        principalColumn: "RequestTestId");
                });

            migrationBuilder.CreateTable(
                name: "DoctorPatientConsents",
                columns: table => new
                {
                    ConsentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    RequestTestId = table.Column<int>(type: "int", nullable: true),
                    SampleId = table.Column<int>(type: "int", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DoctorId1 = table.Column<int>(type: "int", nullable: true),
                    PatientId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPatientConsents", x => x.ConsentId);
                    table.ForeignKey(
                        name: "FK_DoctorPatientConsents_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId");
                    table.ForeignKey(
                        name: "FK_DoctorPatientConsents_Doctors_DoctorId1",
                        column: x => x.DoctorId1,
                        principalTable: "Doctors",
                        principalColumn: "DoctorId");
                    table.ForeignKey(
                        name: "FK_DoctorPatientConsents_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId");
                    table.ForeignKey(
                        name: "FK_DoctorPatientConsents_Patients_PatientId1",
                        column: x => x.PatientId1,
                        principalTable: "Patients",
                        principalColumn: "PatientId");
                    table.ForeignKey(
                        name: "FK_DoctorPatientConsents_RequestSamples_SampleId",
                        column: x => x.SampleId,
                        principalTable: "RequestSamples",
                        principalColumn: "SampleId");
                    table.ForeignKey(
                        name: "FK_DoctorPatientConsents_RequestTestTypes_RequestTestId",
                        column: x => x.RequestTestId,
                        principalTable: "RequestTestTypes",
                        principalColumn: "RequestTestId");
                    table.ForeignKey(
                        name: "FK_DoctorPatientConsents_TestRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "TestRequests",
                        principalColumn: "RequestId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_CategoryId",
                table: "Allergies",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_PatientId",
                table: "Allergies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableRequests_ConsumableId",
                table: "ConsumableRequests",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableRequests_TechnicianId",
                table: "ConsumableRequests",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_SupplierId",
                table: "Consumables",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_TestTypeId",
                table: "Consumables",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConsents_DoctorId",
                table: "DoctorPatientConsents",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConsents_DoctorId1",
                table: "DoctorPatientConsents",
                column: "DoctorId1");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConsents_PatientId_DoctorId_RequestId_RequestTestId_SampleId",
                table: "DoctorPatientConsents",
                columns: new[] { "PatientId", "DoctorId", "RequestId", "RequestTestId", "SampleId" },
                unique: true,
                filter: "[RequestTestId] IS NOT NULL AND [SampleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConsents_PatientId1",
                table: "DoctorPatientConsents",
                column: "PatientId1");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConsents_RequestId",
                table: "DoctorPatientConsents",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConsents_RequestTestId",
                table: "DoctorPatientConsents",
                column: "RequestTestId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConsents_SampleId",
                table: "DoctorPatientConsents",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserId",
                table: "Doctors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_UserId",
                table: "Managers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_CategoryId",
                table: "MedicalConditions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalConditions_PatientId",
                table: "MedicalConditions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_CategoryId",
                table: "Medications",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_PatientId",
                table: "Medications",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_AllergyId",
                table: "PatientAllergies",
                column: "AllergyId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAppointments_DoctorId",
                table: "PatientAppointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAppointments_PatientId",
                table: "PatientAppointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAppointments_RequestId",
                table: "PatientAppointments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientConditions_ConditionId",
                table: "PatientConditions",
                column: "ConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientMedications_MedicationId",
                table: "PatientMedications",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestCancellations_RequestId",
                table: "RequestCancellations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSamples_RequestId",
                table: "RequestSamples",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSamples_RequestTestId",
                table: "RequestSamples",
                column: "RequestTestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestSamples_SampleTypeId",
                table: "RequestSamples",
                column: "SampleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTestTypes_RequestId",
                table: "RequestTestTypes",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTestTypes_TechnicianId",
                table: "RequestTestTypes",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestTestTypes_TestTypeId",
                table: "RequestTestTypes",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockOrderItems_ConsumableId",
                table: "StockOrderItems",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_StockOrderItems_StockOrderOrderId",
                table: "StockOrderItems",
                column: "StockOrderOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockOrders_SupplierId",
                table: "StockOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProblemReports_ConsumableId",
                table: "StockProblemReports",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProblemReports_TechnicianId",
                table: "StockProblemReports",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_UserId",
                table: "Technicians",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianTestTypes_TestTypeId",
                table: "TechnicianTestTypes",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TestConsumableUsages_ConsumableId",
                table: "TestConsumableUsages",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_TestConsumableUsages_RequestTestId",
                table: "TestConsumableUsages",
                column: "RequestTestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_DoctorId",
                table: "TestRequests",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRequests_PatientId",
                table: "TestRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResultHistories_RequestTestTypeRequestTestId",
                table: "TestResultHistories",
                column: "RequestTestTypeRequestTestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTypeConsumables_ConsumableId",
                table: "TestTypeConsumables",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTypes_CategoryId",
                table: "TestTypes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTypes_SampleTypeId",
                table: "TestTypes",
                column: "SampleTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "ConsumableRequests");

            migrationBuilder.DropTable(
                name: "DoctorPatientConsents");

            migrationBuilder.DropTable(
                name: "Managers");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PatientAllergies");

            migrationBuilder.DropTable(
                name: "PatientAppointments");

            migrationBuilder.DropTable(
                name: "PatientConditions");

            migrationBuilder.DropTable(
                name: "PatientMedications");

            migrationBuilder.DropTable(
                name: "RequestCancellations");

            migrationBuilder.DropTable(
                name: "ResultReleases");

            migrationBuilder.DropTable(
                name: "StockOrderItems");

            migrationBuilder.DropTable(
                name: "StockProblemReports");

            migrationBuilder.DropTable(
                name: "TechnicianTestTypes");

            migrationBuilder.DropTable(
                name: "TestConsumableUsages");

            migrationBuilder.DropTable(
                name: "TestResultHistories");

            migrationBuilder.DropTable(
                name: "TestTypeConsumables");

            migrationBuilder.DropTable(
                name: "RequestSamples");

            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "MedicalConditions");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "StockOrders");

            migrationBuilder.DropTable(
                name: "Consumables");

            migrationBuilder.DropTable(
                name: "RequestTestTypes");

            migrationBuilder.DropTable(
                name: "AllergyCategories");

            migrationBuilder.DropTable(
                name: "MedicalConditionCategories");

            migrationBuilder.DropTable(
                name: "MedicationCategories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Technicians");

            migrationBuilder.DropTable(
                name: "TestRequests");

            migrationBuilder.DropTable(
                name: "TestTypes");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "SampleTypes");

            migrationBuilder.DropTable(
                name: "TestCategories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
