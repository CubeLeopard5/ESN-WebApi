# Spec : Validation des Présences aux Événements

**Date** : 2026-01-16
**Statut** : En cours d'implémentation
**Branche** : `feature/event-attendance`

---

## Objectif

Permettre aux organisateurs (esn_member/Admin) de valider la présence des participants inscrits à un événement.

---

## Fonctionnalités

### 1. Validation de Présence
- Marquer un participant comme : **Présent**, **Absent**, ou **Excusé**
- Enregistrer qui a validé et quand
- Possibilité de réinitialiser une validation

### 2. Validation en Masse
- Valider plusieurs participants en une seule requête
- Optimisé pour les grands événements

### 3. Statistiques
- Nombre total d'inscrits
- Nombre de présents, absents, excusés
- Taux de présence (présents / inscrits)
- Taux de validation (validés / inscrits)

---

## Endpoints API

| Méthode | Route | Description | Accès |
|---------|-------|-------------|-------|
| GET | `/api/events/{eventId}/attendance` | Liste inscriptions avec présences | esn_member/Admin |
| GET | `/api/events/{eventId}/attendance/stats` | Statistiques de présence | esn_member/Admin |
| PUT | `/api/events/{eventId}/attendance/{registrationId}` | Valider présence unitaire | esn_member/Admin |
| PUT | `/api/events/{eventId}/attendance` | Valider présences en masse | esn_member/Admin |
| DELETE | `/api/events/{eventId}/attendance/{registrationId}` | Réinitialiser présence | esn_member/Admin |

---

## Modèle de Données

### Enum AttendanceStatus
```
Present = 1
Absent = 2
Excused = 3
```

### Modification EventRegistrationBo
Nouveaux champs :
- `AttendanceStatus? AttendanceStatus` - Statut (null = non validé)
- `DateTime? AttendanceValidatedAt` - Timestamp validation
- `int? AttendanceValidatedById` - FK User validateur
- `UserBo? AttendanceValidatedBy` - Navigation property

---

## DTOs

### Request
- `ValidateAttendanceDto` : { status: AttendanceStatus }
- `BulkValidateAttendanceDto` : { attendances: [{ registrationId, status }] }

### Response
- `AttendanceDto` : Inscription avec infos présence
- `EventAttendanceDto` : Événement + liste présences + stats
- `AttendanceStatsDto` : Statistiques agrégées

---

## Règles Métier

1. **Autorisation** : Seuls les `esn_member` et `Admin` peuvent valider/consulter
2. **Validation** : Uniquement sur inscriptions avec status "registered"
3. **Reset** : Remet tous les champs présence à null
4. **Stats** : Calculées dynamiquement, pas stockées

---

## Tests Requis

### Service (AttendanceServiceTests)
- ValidateAttendanceAsync_ValidRequest_ShouldUpdateRegistration
- ValidateAttendanceAsync_NonEsnMember_ShouldThrowUnauthorized
- ValidateAttendanceAsync_RegistrationNotFound_ShouldThrowKeyNotFound
- BulkValidateAttendanceAsync_ValidRequest_ShouldUpdateAll
- GetEventAttendanceAsync_ExistingEvent_ShouldReturnData
- GetAttendanceStatsAsync_MixedAttendance_ShouldCalculateCorrectly
- ResetAttendanceAsync_ValidRequest_ShouldResetToNull

### Controller (AttendanceControllerTests)
- Tests HTTP 200, 403, 404 pour chaque endpoint

---

## Frontend (ESN-Nuxt)

### Page : `/admin/events/[id]/attendance`
- Tableau des inscrits avec select de statut
- Bouton sauvegarde en masse
- Affichage statistiques en temps réel
- Protection middleware : auth + admin
