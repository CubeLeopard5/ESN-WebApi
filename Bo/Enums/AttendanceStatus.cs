namespace Bo.Enums;

/// <summary>
/// Statuts de présence pour les inscriptions aux événements
/// </summary>
public enum AttendanceStatus
{
    /// <summary>
    /// Participant présent à l'événement
    /// </summary>
    Present = 1,

    /// <summary>
    /// Participant absent à l'événement
    /// </summary>
    Absent = 2,

    /// <summary>
    /// Participant excusé (absence justifiée)
    /// </summary>
    Excused = 3
}
