# Schéma de Base de Données ESN-WebApi

## Vue d'ensemble

ESN-WebApi utilise **SQL Server** comme système de gestion de base de données relationnel, avec **Entity Framework Core 9.0** comme ORM.

## Diagramme Relationnel

```
┌────────────┐       ┌────────────────────┐       ┌───────────┐
│    Roles   │◄──────│       Users        │──────►│  Events   │
│            │1     *│                    │1     *│           │
└────────────┘       └────────────────────┘       └───────────┘
                              │                          │
                              │                          │
                             *│                         *│
                     ┌────────┴──────────┐      ┌────────┴──────────┐
                     │  Propositions     │      │ EventRegistrations│
                     │  PropositionVotes │      └───────────────────┘
                     └───────────────────┘
                              │
                             *│
                     ┌────────┴──────────┐
                     │     Calendars     │
                     │CalendarSubOrgs    │
                     └───────────────────┘
```

---

## Tables Principales

### Users
**Rôle:** Gestion des utilisateurs de l'application

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | int | PK, Identity | Identifiant unique |
| Email | nvarchar(255) | NOT NULL, UNIQUE | Adresse email (login) |
| PasswordHash | nvarchar(255) | NOT NULL | Hash PBKDF2 du mot de passe |
| FirstName | nvarchar(100) | NOT NULL | Prénom |
| LastName | nvarchar(100) | NOT NULL | Nom |
| BirthDate | datetime | NOT NULL | Date de naissance |
| PhoneNumber | nvarchar(20) | NULL | Numéro de téléphone |
| ESNCardNumber | nvarchar(50) | NULL | Numéro de carte ESN |
| UniversityName | nvarchar(255) | NULL | Nom de l'université |
| StudentType | nvarchar(50) | NOT NULL | Type: exchange/local/esn_member |
| TransportPass | nvarchar(100) | NULL | Pass de transport |
| CreatedAt | datetime | DEFAULT getdate() | Date de création |
| LastLoginAt | datetime | DEFAULT getdate() | Dernière connexion |
| RoleId | int | FK → Roles(Id) | Rôle de l'utilisateur |

**Index:**
- UNIQUE sur Email

**Relations:**
- 1 User → * Events (créateur)
- 1 User → * Propositions (auteur)
- 1 User → * PropositionVotes (votant)
- 1 User → * EventRegistrations (inscrit)
- 1 User → * Calendars (organisateur principal)
- 1 User → * Calendars (event manager)
- 1 User → * Calendars (responsable communication)
- * User ↔ * Calendars (sous-organisateurs, many-to-many)

---

### Roles
**Rôle:** Gestion des rôles et permissions

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | int | PK, Identity | Identifiant unique |
| Name | nvarchar(50) | NOT NULL, UNIQUE | Nom du rôle (User, Admin) |
| CanCreateEvents | bit | NOT NULL | Permission création événements |
| CanModifyEvents | bit | NOT NULL | Permission modification événements |
| CanDeleteEvents | bit | NOT NULL | Permission suppression événements |
| CanCreateUsers | bit | NOT NULL | Permission création utilisateurs |
| CanModifyUsers | bit | NOT NULL | Permission modification utilisateurs |
| CanDeleteUsers | bit | NOT NULL | Permission suppression utilisateurs |

**Index:**
- UNIQUE sur Name

**Relations:**
- 1 Role → * Users

---

### Events
**Rôle:** Gestion des événements

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | int | PK, Identity | Identifiant unique |
| Title | nvarchar(255) | NOT NULL | Titre de l'événement |
| Description | ntext | NULL | Description détaillée |
| Location | nvarchar(255) | NULL | Lieu de l'événement |
| StartDate | datetime | NOT NULL | Date et heure de début |
| EndDate | datetime | NULL | Date et heure de fin |
| MaxParticipants | int | NULL | Nombre maximum de participants |
| EventfrogLink | nvarchar(500) | NULL | Lien vers Eventfrog |
| SurveyJsData | ntext | NULL, MAX 100KB | Formulaire JSON (SurveyJS) |
| UserId | int | FK → Users(Id) | Créateur de l'événement |
| CreatedAt | datetime | DEFAULT getdate() | Date de création |

**Relations:**
- 1 Event → 1 User (créateur)
- 1 Event → * EventRegistrations
- 1 Event → * Calendars (optionnel)

---

### EventRegistrations
**Rôle:** Inscriptions des utilisateurs aux événements

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | int | PK, Identity | Identifiant unique |
| UserId | int | FK → Users(Id) | Utilisateur inscrit |
| EventId | int | FK → Events(Id) | Événement concerné |
| SurveyJsData | ntext | NULL, MAX 100KB | Réponses au formulaire (JSON) |
| RegisteredAt | datetime | NOT NULL | Date d'inscription |
| Status | nvarchar(50) | NOT NULL | Statut: Registered/Cancelled |

**Index:**
- UNIQUE sur (UserId, EventId)

**Relations:**
- 1 EventRegistration → 1 User
- 1 EventRegistration → 1 Event

**Règles:**
- Status par défaut: "Registered"
- Soft delete: Passage à "Cancelled"

---

### EventTemplates
**Rôle:** Templates d'événements réutilisables

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | int | PK, Identity | Identifiant unique |
| Title | nvarchar(255) | NOT NULL | Titre du template |
| Description | ntext | NULL | Description du template |
| SurveyJsData | ntext | NULL, MAX 100KB | Formulaire JSON (SurveyJS) |
| CreatedAt | datetime | DEFAULT getdate() | Date de création |

**Utilisation:**
- Création rapide d'événements similaires
- Réutilisation de formulaires

---

### Calendars
**Rôle:** Planification et organisation des événements

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | int | PK, Identity | Identifiant unique |
| Title | nvarchar(255) | NOT NULL | Titre de l'entrée calendrier |
| EventDate | datetime | NOT NULL | Date de l'événement au calendrier |
| EventId | int | FK → Events(Id), NULL | Événement associé (optionnel) |
| MainOrganizerId | int | FK → Users(Id) | Organisateur principal |
| EventManagerId | int | FK → Users(Id), NULL | Gestionnaire de l'événement |
| ResponsableComId | int | FK → Users(Id), NULL | Responsable communication |

**Relations:**
- 1 Calendar → 1 Event (optionnel)
- 1 Calendar → 1 User (MainOrganizer)
- 1 Calendar → 1 User (EventManager, optionnel)
- 1 Calendar → 1 User (ResponsableCom, optionnel)
- 1 Calendar ↔ * Users (SubOrganizers via table intermédiaire)

---

### CalendarSubOrganizers
**Rôle:** Relation many-to-many entre Calendars et Users (sous-organisateurs)

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| CalendarId | int | FK → Calendars(Id) | Calendrier concerné |
| UserId | int | FK → Users(Id) | Sous-organisateur |

**Clé primaire composite:** (CalendarId, UserId)

**Relations:**
- * CalendarSubOrganizers → 1 Calendar
- * CalendarSubOrganizers → 1 User

---

### Propositions
**Rôle:** Propositions d'événements/activités par la communauté

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | int | PK, Identity | Identifiant unique |
| Title | nvarchar(255) | NOT NULL | Titre de la proposition |
| Description | ntext | NULL | Description détaillée |
| UserId | int | FK → Users(Id) | Auteur de la proposition |
| CreatedAt | datetime | DEFAULT getdate() | Date de création |
| ModifiedAt | datetime | DEFAULT getdate() | Date de modification |
| DeletedAt | datetime | NULL | Date de suppression (soft delete) |
| IsDeleted | bit | NOT NULL, DEFAULT 0 | Marqueur de suppression |
| VotesUp | int | NOT NULL, DEFAULT 0 | Nombre de votes positifs |
| VotesDown | int | NOT NULL, DEFAULT 0 | Nombre de votes négatifs |

**Relations:**
- 1 Proposition → 1 User (auteur)
- 1 Proposition → * PropositionVotes

**Soft Delete:**
- IsDeleted = 1 quand supprimée
- DeletedAt = date de suppression
- Filtrée automatiquement dans les listes

---

### PropositionVotes
**Rôle:** Votes sur les propositions

| Colonne | Type | Contraintes | Description |
|---------|------|-------------|-------------|
| Id | int | PK, Identity | Identifiant unique |
| PropositionId | int | FK → Propositions(Id) | Proposition concernée |
| UserId | int | FK → Users(Id) | Utilisateur votant |
| VoteType | int | NOT NULL | Type: 1 (Up), -1 (Down) |
| CreatedAt | datetime | NOT NULL | Date du vote initial |
| UpdatedAt | datetime | NULL | Date de changement de vote |

**Index:**
- UNIQUE sur (PropositionId, UserId)

**Relations:**
- 1 PropositionVote → 1 Proposition
- 1 PropositionVote → 1 User

**Règles:**
- Un utilisateur ne peut voter qu'une fois par proposition
- Changement de vote autorisé (Up ↔ Down)
- Recalcul automatique des compteurs dans Propositions

---

## Migrations Entity Framework

### Historique des Migrations

**Migrations principales:**
1. `InitialCreate` - Création schéma initial
2. `UpdateEventUserIdColumn` - Modification colonne UserId dans Events
3. `AddPropositionVotesManual` - Ajout table PropositionVotes

### Application des Migrations

**Développement:**
```bash
dotnet ef migrations add NomMigration
dotnet ef database update
```

**Production:**
```bash
dotnet ef database update --connection "ConnectionString"
```

**Rollback:**
```bash
dotnet ef database update PreviousMigrationName
```

---

## Procédures Stockées et Vues

**Actuellement:** Aucune procédure stockée ou vue.

**Toutes les requêtes via EF Core LINQ:**
- Requêtes paramétrées automatiques
- Protection SQL injection native
- Maintenance simplifiée

---

## Indexation et Performance

### Index Existants

**Index UNIQUE:**
- Users(Email)
- Roles(Name)
- EventRegistrations(UserId, EventId)
- PropositionVotes(PropositionId, UserId)

**Index de Clés Étrangères:**
- Automatiques par EF Core sur toutes les FK

### Optimisations Recommandées

**Index à ajouter pour performance:**
```sql
-- Recherche par créateur
CREATE INDEX IX_Events_UserId ON Events(UserId);

-- Recherche par événement
CREATE INDEX IX_Calendars_EventId ON Calendars(EventId);

-- Recherche par statut inscription
CREATE INDEX IX_EventRegistrations_Status ON EventRegistrations(Status);

-- Recherche propositions actives
CREATE INDEX IX_Propositions_IsDeleted ON Propositions(IsDeleted);
```

**Index composites:**
```sql
-- Votes par proposition
CREATE INDEX IX_PropositionVotes_PropositionId_VoteType
ON PropositionVotes(PropositionId, VoteType);
```

---

## Gestion des Transactions

### Transactions Automatiques
EF Core garantit l'atomicité de `SaveChangesAsync()`.

### Transactions Explicites
Implémentées via `IUnitOfWork`:

```csharp
await unitOfWork.BeginTransactionAsync();
try
{
    // Opérations multiples
    await unitOfWork.SaveChangesAsync();
    await unitOfWork.CommitTransactionAsync();
}
catch
{
    await unitOfWork.RollbackTransactionAsync();
    throw;
}
```

**Utilisées dans:**
- `RegisterForEventAsync` (vérification capacité + inscription)
- `CreateCalendarAsync` (création + sous-organisateurs)
- `UpdateCalendarAsync` (modification + sous-organisateurs)

---

## Contraintes d'Intégrité

### Clés Étrangères

**Cascade DELETE:**
- EventRegistrations supprimées si Event supprimé
- PropositionVotes supprimés si Proposition supprimée
- CalendarSubOrganizers supprimés si Calendar supprimé

**Restrict DELETE:**
- Users ne peuvent être supprimés s'ils ont des ressources associées (à vérifier)

### Contraintes CHECK

**Recommandations à implémenter:**
```sql
-- Vérifier que EndDate >= StartDate
ALTER TABLE Events
ADD CONSTRAINT CK_Events_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate);

-- Vérifier MaxParticipants > 0
ALTER TABLE Events
ADD CONSTRAINT CK_Events_MaxParticipants CHECK (MaxParticipants IS NULL OR MaxParticipants > 0);

-- Vérifier VoteType dans {-1, 1}
ALTER TABLE PropositionVotes
ADD CONSTRAINT CK_PropositionVotes_VoteType CHECK (VoteType IN (-1, 1));
```

---

## Sauvegarde et Récupération

### Stratégie de Sauvegarde

**Recommandations:**
- **Sauvegarde complète:** Quotidienne (nuit)
- **Sauvegarde différentielle:** Toutes les 6 heures
- **Sauvegarde logs transactionnels:** Toutes les 15 minutes
- **Rétention:** 30 jours minimum

### Script de Sauvegarde

```sql
-- Sauvegarde complète
BACKUP DATABASE EsnDevDb
TO DISK = 'C:\Backups\EsnDevDb_Full.bak'
WITH FORMAT, INIT, NAME = 'Full Backup';

-- Sauvegarde différentielle
BACKUP DATABASE EsnDevDb
TO DISK = 'C:\Backups\EsnDevDb_Diff.bak'
WITH DIFFERENTIAL, INIT, NAME = 'Differential Backup';
```

### Test de Récupération

**Procédure mensuelle:**
1. Restaurer backup sur environnement test
2. Vérifier intégrité des données
3. Tester requêtes critiques
4. Documenter le résultat

---

## Statistiques et Monitoring

### Requêtes Fréquentes

**Top 5 des requêtes:**
1. SELECT Users WHERE Email = @email (login)
2. SELECT Events avec pagination
3. SELECT EventRegistrations WHERE EventId = @id
4. SELECT Propositions WHERE IsDeleted = 0
5. SELECT Calendars avec Includes

### Monitoring Recommandé

**Métriques à surveiller:**
- Temps de réponse moyen des requêtes
- Nombre de connexions actives
- Taille de la base de données
- Fragmentation des index
- Deadlocks

**Outils:**
- SQL Server Profiler
- Dynamic Management Views (DMVs)
- Azure SQL Analytics (si Azure)

---

## Maintenance

### Maintenance Régulière

**Hebdomadaire:**
- Mise à jour des statistiques
```sql
EXEC sp_updatestats;
```

**Mensuel:**
- Reconstruction des index fragmentés
```sql
ALTER INDEX ALL ON [TableName] REBUILD;
```

**Trimestriel:**
- Vérification intégrité base
```sql
DBCC CHECKDB (EsnDevDb) WITH NO_INFOMSGS;
```

### Purge des Données

**Soft-deleted Propositions:**
```sql
-- Purge propositions supprimées > 1 an
DELETE FROM Propositions
WHERE IsDeleted = 1 AND DeletedAt < DATEADD(YEAR, -1, GETDATE());
```

**Logs:**
- Rotation automatique via Serilog (31 jours)

---

## Sécurité Base de Données

### Compte Application

**Principe du Moindre Privilège:**
```sql
CREATE LOGIN EsnApiApp WITH PASSWORD = 'StrongPassword';
CREATE USER EsnApiApp FOR LOGIN EsnApiApp;

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO EsnApiApp;
-- Pas de CREATE, DROP, ALTER
```

### Chiffrement

**Recommandations:**
- **TDE (Transparent Data Encryption):** Chiffrement au repos
- **SSL/TLS:** Chiffrement en transit
- **Always Encrypted:** Pour colonnes sensibles (future)

### Audit

**Configuration Audit SQL Server:**
```sql
CREATE SERVER AUDIT EsnApiAudit
TO FILE (FILEPATH = 'C:\Audits\');

CREATE DATABASE AUDIT SPECIFICATION EsnApiDatabaseAudit
FOR SERVER AUDIT EsnApiAudit
ADD (SELECT, INSERT, UPDATE, DELETE ON DATABASE::EsnDevDb BY public);
```

---

## Évolutions Futures

### Améliorations Suggérées

**Performance:**
- Ajout d'index composites stratégiques
- Mise en cache des requêtes fréquentes (Redis)
- Partitionnement pour tables volumineuses

**Fonctionnalités:**
- Table Comments pour système de commentaires
- Table Notifications pour notifications utilisateurs
- Table Files pour gestion de fichiers/photos
- Table AuditLog pour traçabilité complète

**Scalabilité:**
- Read replicas pour lecture
- Sharding par région géographique
- Event Sourcing pour historique complet

---

## Diagramme ERD Complet

```
                                    ┌────────────┐
                                    │   Roles    │
                                    │────────────│
                                    │ Id (PK)    │
                                    │ Name       │
                                    │ Permissions│
                                    └─────┬──────┘
                                          │1
                                          │
                                         *│
                    ┌─────────────────────┴──────────────────┐
                    │             Users                      │
                    │────────────────────────────────────────│
                    │ Id (PK)                                │
                    │ Email (UNIQUE)                         │
                    │ PasswordHash                           │
                    │ FirstName, LastName, BirthDate         │
                    │ PhoneNumber, ESNCardNumber             │
                    │ UniversityName, StudentType            │
                    │ TransportPass                          │
                    │ CreatedAt, LastLoginAt                 │
                    │ RoleId (FK)                            │
                    └─┬─────┬──────┬───────┬───────┬─────────┘
                     1│    1│     1│      1│      1│
                      │     │      │       │       │
                     *│    *│     *│      *│      *│
        ┌─────────────┴┐ ┌──┴──────┴──┐ ┌─┴───────┴─────┐
        │  Events      │ │Propositions │ │   Calendars   │
        │──────────────│ │─────────────│ │───────────────│
        │ Id (PK)      │ │ Id (PK)     │ │ Id (PK)       │
        │ Title        │ │ Title       │ │ Title         │
        │ Description  │ │ Description │ │ EventDate     │
        │ Location     │ │ VotesUp     │ │ EventId (FK)  │
        │ StartDate    │ │ VotesDown   │ │ MainOrg (FK)  │
        │ EndDate      │ │ IsDeleted   │ │ EventMgr (FK) │
        │ MaxPart      │ │ UserId (FK) │ │ RespCom (FK)  │
        │ SurveyJsData │ │──────────┬──│ └───────┬───────┘
        │ UserId (FK)  │           *│  │        *│
        └──────┬───────┘            │  │         │
              1│                    │  │         │
               │                   1│ *│        1│
              *│          ┌─────────┴──┴─┐   ┌──┴────────────────┐
        ┌──────┴────────┐ │Proposition   │   │CalendarSubOrgs    │
        │EventRegistr   │ │Votes         │   │───────────────────│
        │───────────────│ │──────────────│   │ CalendarId (PK,FK)│
        │ Id (PK)       │ │ Id (PK)      │   │ UserId (PK,FK)    │
        │ UserId (FK)   │ │ PropId (FK)  │   └───────────────────┘
        │ EventId (FK)  │ │ UserId (FK)  │
        │ SurveyJsData  │ │ VoteType     │
        │ RegisteredAt  │ │ CreatedAt    │
        │ Status        │ │ UpdatedAt    │
        └───────────────┘ └──────────────┘

                    ┌──────────────────┐
                    │EventTemplates    │
                    │──────────────────│
                    │ Id (PK)          │
                    │ Title            │
                    │ Description      │
                    │ SurveyJsData     │
                    │ CreatedAt        │
                    └──────────────────┘
```

---

**Note:** Ce schéma est géré automatiquement par Entity Framework Core Migrations. Toute modification doit passer par le processus de migration pour garantir la cohérence et la traçabilité.
