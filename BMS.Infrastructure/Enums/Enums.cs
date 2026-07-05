namespace BMS.Infrastructure.Enums;

public enum UnitType
{
    Shop,
    Office
}

public enum UnitStatus
{
    Available,
    Occupied,
    UnderMaintenance,
    Reserved
}

public enum LeaseStatus
{
    Active,
    Expired,
    Terminated,
    PendingRenewal
}

public enum InvoiceStatus
{
    Draft,
    Issued,
    Paid,
    Overdue,
    Cancelled
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    Cheque,
    MobileMoney
}

public enum BusinessType
{
    Retail,
    Restaurant,
    Office,
    Services,
    Healthcare,
    Education,
    Other
}

public enum DocumentType
{
    BusinessLicense,
    TradeLicense,
    TaxClearance,
    NationalID,
    LeaseContract,
    Other
}

public enum NotificationType
{
    PaymentDue,
    PaymentOverdue,
    LeaseExpiry,
    DocumentExpiry,
    General
}
