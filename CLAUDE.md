# CLAUDE.md - Mémoire Projet ESN-WebApi

> **Ce fichier est lu automatiquement par Claude à chaque démarrage de session**
> Il contient toute la connaissance du projet pour assurer la cohérence entre les sessions

**Dernière mise à jour** : 2026-01-16

---

## 📋 Vue d'Ensemble du Projet

### Qu'est-ce que c'est ?

API REST ASP.NET Core pour la gestion des événements et activités de l'association ESN (Erasmus Student Network).

### Objectif

Permettre la gestion complète de :
- Utilisateurs avec authentification JWT et rôles
- Événements avec inscriptions et capacité maximale
- Calendriers avec organisateurs multiples
- Propositions d'activités avec vote
- Templates d'événements réutilisables

---

## 🏗️ Architecture du Projet

### Structure en Couches (STRICTEMENT RESPECTÉE)

```
ESN-WebApi/
├── Web/              # Controllers API, Middleware, Configuration
├── Business/         # Services, Logique métier
├── Dal/              # Data Access Layer (Repositories, DbContext)
├── Dto/              # Data Transfer Objects (Request/Response)
├── Bo/               # Business Objects (Entités du domaine)
├── Tests/            # Tests unitaires et d'intégration
├── doc/              # Documentation et spécifications
│   ├── specs/        # Documents de spec (doc-first workflow)
│   └── SKILLS_GUIDE.md
└── .claude/          # Configuration Claude et Skills
    └── skills/       # Skills personnalisés
```

### Règles Architecturales IMPORTANTES

**Flux de Dépendances** (TOUJOURS respecter) :
```
Web → Business → Dal → Bo
        ↓
       Dto
```

**Principes** :
1. **Web** : Seulement des contrôleurs, aucune logique métier
2. **Business** : TOUTE la logique métier, validation, orchestration
3. **Dal** : SEULEMENT l'accès aux données, pas de logique métier
4. **Dto** : Objets de transfert pour l'API (Request/Response)
5. **Bo** : Entités du domaine, jamais exposées directement dans l'API

**JAMAIS** :
- ❌ Logique métier dans les Controllers
- ❌ Logique métier dans les Repositories
- ❌ Retourner des entités Bo directement depuis l'API
- ❌ Injection de DbContext dans Business (utiliser les repositories)

---

## 🛠️ Stack Technique

### Backend
- **.NET 10.0** (dernière version)
- **Entity Framework Core 10.0**
- **SQL Server**
- **JWT Bearer** pour l'authentification

### Tests
- **MSTest** (framework de test)
- **Moq** (mocking)
- **InMemory Database** pour les tests d'intégration
- **Coverlet** pour la couverture de code

### Outils
- **Swagger/OpenAPI** (documentation API)
- **Script PowerShell** pour tests avec couverture (`run-coverage.ps1`)

---

## 📐 Conventions et Standards

### Conventions de Nommage

#### C# Code
- **Classes** : PascalCase (`EventService`, `EventRepository`)
- **Méthodes** : PascalCase (`GetAllAsync`, `CreateAsync`)
- **Paramètres** : camelCase (`userId`, `eventDto`)
- **Propriétés** : PascalCase (`Name`, `Email`)
- **Variables privées** : camelCase avec `_` (`_repository`, `_context`)
- **Constantes** : PascalCase (`MaxCapacity`)

#### Fichiers
- **Controllers** : `{EntityName}sController.cs` (pluriel)
- **Services** : `I{EntityName}Service.cs` + `{EntityName}Service.cs`
- **Repositories** : `I{EntityName}Repository.cs` + `{EntityName}Repository.cs`
- **DTOs** : `{EntityName}Dto.cs`, `Create{EntityName}Dto.cs`, `Update{EntityName}Dto.cs`
- **Entités** : `{EntityName}.cs` (singulier)
- **Tests** : `{ClassName}Tests.cs`

#### Tests
- **Méthode de test** : `MethodName_Scenario_ExpectedResult`
  - Exemple : `GetByIdAsync_WhenEntityExists_ShouldReturnEntity`

### Patterns Utilisés

#### Repository Pattern
```csharp
public interface I{EntityName}Repository
{
    Task<IEnumerable<{Entity}>> GetAllAsync();
    Task<{Entity}?> GetByIdAsync(int id);
    Task<{Entity}> CreateAsync({Entity} entity);
    Task UpdateAsync({Entity} entity);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
```

#### Service Pattern
```csharp
public interface I{EntityName}Service
{
    Task<IEnumerable<{Entity}ResponseDto>> GetAllAsync();
    Task<{Entity}ResponseDto?> GetByIdAsync(int id);
    Task<{Entity}ResponseDto> CreateAsync(Create{Entity}Dto dto);
    Task<{Entity}ResponseDto?> UpdateAsync(int id, Update{Entity}Dto dto);
    Task<bool> DeleteAsync(int id);
}
```

#### Controller Pattern
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Selon les besoins
public class {EntityName}sController : ControllerBase
{
    private readonly I{EntityName}Service _service;
    private readonly ILogger<{EntityName}sController> _logger;

    // GET, POST, PUT, DELETE avec gestion d'erreurs complète
}
```

---

## 🎯 Entités Principales (Bo)

### Event
- Événements avec inscriptions
- Capacité maximale
- Formulaires personnalisés (SurveyJS)
- Lien avec Calendar

### Calendar
- Organisateurs multiples (principal + sous-organisateurs)
- Event Manager et Responsable Communication
- Collection d'Events

### User
- Authentification JWT
- Rôles (User, Admin)
- **Statut de compte** : Pending, Approved, Rejected
- **Validation par admin** : Les nouveaux comptes doivent être approuvés
- Inscriptions aux événements (seulement si compte Approved)

### Proposal
- Propositions d'activités
- Système de vote Up/Down
- Soft delete

### Template
- Templates d'événements réutilisables

---

## 🔒 Sécurité

### Authentification & Autorisation
- **JWT Bearer** (durée: 30 min)
- **Refresh tokens** (validité: 7 jours)
- **Role-Based Access Control** (RBAC)
- Attribut `[Authorize]` sur endpoints protégés
- Attribut `[Authorize(Roles = "Admin")]` pour admin seulement

### Validation
- **Attributs de validation** sur tous les DTOs :
  - `[Required]`
  - `[MaxLength(X)]`
  - `[EmailAddress]`
  - `[Range(min, max)]`
- **Validation côté serveur** TOUJOURS (jamais se fier au client)
- **ModelState.IsValid** vérifié dans les controllers

### Protection OWASP Top 10
- ✅ SQL Injection : EF Core avec paramètres
- ✅ XSS : Validation et encodage automatique ASP.NET Core
- ✅ Authentication : JWT Bearer
- ✅ Sensitive Data : Pas de secrets en dur, utilisation de User Secrets
- ✅ Access Control : Rôles et permissions

---

## 🧪 Tests

### Stratégie de Tests

#### Couverture Requise
- **Minimum** : 80%
- **Objectif** : 90%+

#### Types de Tests

1. **Tests Unitaires (Business Layer)**
   - Tester TOUTE la logique métier
   - Mock des repositories
   - AAA Pattern (Arrange, Act, Assert)

2. **Tests Unitaires (Dal Layer)**
   - InMemory Database
   - Tester les requêtes complexes

3. **Tests d'Intégration (Web Layer)**
   - Tester les controllers
   - Mock des services

#### Exécution des Tests

```powershell
# Exécuter tests avec couverture
pwsh -File run-coverage.ps1

# Le script :
# 1. Lance tous les tests
# 2. Génère rapport de couverture
# 3. Ouvre le rapport HTML dans le navigateur
```

---

## 🚀 Skills Claude Configurés

### Skills Activés (4 au total)

#### 1. code-review
- **Trigger** : "fais une code review", "/code-review"
- **Fonction** : Analyse complète (qualité, tests, sécurité, architecture, doc)
- **Quand** : Avant PR, après feature, audit périodique

#### 2. doc-first
- **Trigger** : Automatique sur toute demande d'implémentation
- **Fonction** : Force documentation AVANT code
- **Processus** : Doc → Validation → Tests (TDD) → Implémentation → Tests passent
- **Fichiers** : Crée specs dans `doc/specs/YYYYMMDD-nom.md`

#### 3. crud-generator
- **Trigger** : "/crud-generator EntityName"
- **Fonction** : Génère stack CRUD complète (Entity, DTOs, Repo, Service, Controller, Tests)
- **Gain** : 30-45 minutes par entité
- **Usage** : Utilisé automatiquement quand un nouveau controller est nécessaire

#### 4. performance-audit
- **Trigger** : "audit de performance", "/performance-audit"
- **Fonction** : Détecte N+1 queries, index manquants, AsNoTracking absent, etc.
- **Résultat** : Rapport avec solutions et gains estimés

### Hooks Configurés

**PostToolUse (Write/Edit)** :
- Rappel d'exécuter les tests après modification de fichiers .cs

---

## 📝 Workflow de Développement

### Workflow Standard pour Nouvelle Feature (Process Complet en 16 Étapes)

**IMPORTANT : Ce workflow DOIT être suivi pour toute nouvelle fonctionnalité**

#### Phase 0 : Création de Branche Git (Étape 0)

```
0. Créer une Branche Git Dédiée (OBLIGATOIRE)
   → AVANT toute implémentation, créer une nouvelle branche :
     git checkout -b feature/<nom-feature>
   → Convention de nommage :
     • feature/<nom>   - pour nouvelles fonctionnalités
     • fix/<nom>       - pour corrections de bugs
     • refactor/<nom>  - pour refactoring
   → Se positionner sur cette branche
   → JAMAIS travailler directement sur master
```

#### Phase 1 : Planification et Documentation (Étapes 1-2)

```
1. Mode Planification
   → Utiliser EnterPlanMode automatiquement
   → Étudier l'intégration de la fonctionnalité au projet
   → Poser des questions en cas de besoin
   → Sortir un plan d'action détaillé
   ↓
2. Documentation de la Spec
   → Sur validation du plan par l'utilisateur
   → Créer doc/specs/YYYYMMDD-nom-feature.md
   → Documentation CONCISE (pas de code, pas de SQL)
   → Décrire la fonctionnalité, les endpoints, les DTOs, le comportement attendu
```

#### Phase 2 : Développement Backend TDD (Étapes 3-5)

```
3. Tests Unitaires EN PREMIER (TDD)
   → Créer TOUS les tests AVANT l'implémentation
   → Tests pour Service, Repository, Controller
   → Couvrir succès, échecs, edge cases
   → Les tests échouent (normal, pas encore implémenté)
   ↓
4. Implémentation Backend
   → Créer les INTERFACES avec commentaires XML COMPLETS
     • IXxxService.cs
     • IXxxRepository.cs
     • /// <summary>, /// <param>, /// <returns>, /// <remarks>
   → Créer les implémentations avec /// <inheritdoc />
   → Respecter strictement l'architecture en couches (Web → Business → Dal → Bo)
   → DTOs pour tous les Request/Response
   ↓
5. Validation des Tests
   → Exécuter : dotnet test Tests/Tests.csproj
   → TOUS les tests DOIVENT passer (0 échec)
   → Coverage ≥ 80% obligatoire
```

#### Phase 3 : Audits et Optimisation (Étape 6)

```
6. Refactoring + Audits (OBLIGATOIRE)
   → Refactoring du code
     • Principes SOLID, DRY, Clean Code
     • Supprimer duplication
     • Simplifier complexité
   → Audit de Performance (/performance-audit)
     • Vérifier N+1 queries
     • AsNoTracking() sur lectures seules
     • Index manquants
     • Pagination appropriée
   → Audit de Sécurité
     • OWASP Top 10
     • Validation des entrées
     • Pas de secrets en dur
     • Authorization/Authentication correcte
   → IMPLÉMENTER les corrections suggérées par les audits
   → RE-RUN Tests Unitaires
     • Exécuter : dotnet test Tests/Tests.csproj
     • Tous les tests DOIVENT passer
     • Vérifier qu'aucune régression n'a été introduite
```

#### Phase 4 : Frontend (Étapes 7-9)

```
7. Implémentation Frontend
   → Nuxt 3 / Vue 3 Composition API
   → TypeScript strict
   → Composables pour API calls
   → Suivre les patterns existants du projet ESN-Nuxt
   → **Nuxt UI** : Utiliser `:items` (et non `:options`) pour les composants USelect
   ↓
8. Notification Utilisateur
   → Dire explicitement : "✅ Implémentation terminée, prêt pour test manuel"
   → Lister les fonctionnalités à tester
   → Attendre feedback utilisateur
   ↓
9. Attente Validation Utilisateur
   → Attendre que l'utilisateur dise "c'est bon" ou similaire
   → Si bugs signalés → corriger et revenir à l'étape 8
   → Si validation OK → passer à l'étape 10
```

#### Phase 5 : SonarCloud (Étapes 10-12)

```
10. Lancer SonarScanner
    → Exécuter ces commandes dans l'ordre :

    dotnet sonarscanner begin /o:"cubeleopard5" /k:"CubeLeopard5_ESN-WebApi" /d:sonar.token="b794def3a5389f65a580c0c7edf2560c90aaf3d8"
    dotnet build
    dotnet sonarscanner end /d:sonar.token="b794def3a5389f65a580c0c7edf2560c90aaf3d8"

    ↓
11. Corriger Issues SonarCloud
    → Vérifier : https://sonarcloud.io/summary/overall?id=CubeLeopard5_ESN-WebApi&branch=master
    → Corriger UNIQUEMENT :
      • Issues SECURITY (toutes)
        https://sonarcloud.io/project/issues?impactSoftwareQualities=SECURITY&issueStatuses=OPEN%2CCONFIRMED&id=CubeLeopard5_ESN-WebApi
      • Issues Blocker severity
      • Issues High severity
    → NE PAS corriger Minor/Info sauf si trivial
    ↓
12. Re-run Tests Finale
    → Exécuter : dotnet test Tests/Tests.csproj
    → Tous les tests DOIVENT passer
    → Aucune régression introduite par les corrections SonarCloud
```

#### Phase 6 : Git Commit, Push & Merge (Étapes 13-16)

```
13. Demander Retest Final
    → Dire : "✅ Corrections SonarCloud terminées, merci de retester la fonctionnalité"
    → Attendre validation utilisateur
    ↓
14. Git Commit et Push (sur validation utilisateur)
    → Exécuter :

    git add *
    git commit -m "claude - <Titre de la fonctionnalité> - <Description>"
    git push -u origin feature/<nom-feature>

    ↓
15. Demander Validation pour Merge
    → Dire : "✅ Push effectué sur la branche feature/<nom-feature>"
    → Attendre validation utilisateur pour le merge sur master
    ↓
16. Merge sur Master (sur validation utilisateur)
    → Exécuter :

    git checkout master
    git merge feature/<nom-feature>
    git push origin master

    → Optionnel : Supprimer la branche feature après merge :
    git branch -d feature/<nom-feature>
    git push origin --delete feature/<nom-feature>

    → ✅ Feature complète et mergée sur master !
```

---

### Résumé du Workflow

| Phase | Étapes | Description | Validation |
|-------|--------|-------------|------------|
| **0. Git Branch** | 0 | Créer branche feature | Branche créée |
| **1. Plan & Doc** | 1-2 | Planification et spec | Utilisateur valide plan |
| **2. Backend TDD** | 3-5 | Tests puis implémentation | Tests passent 100% |
| **3. Audits** | 6 | Refactoring + audits | Audits OK |
| **4. Frontend** | 7-9 | Implémentation frontend | Utilisateur teste et valide |
| **5. SonarCloud** | 10-12 | Scan et corrections | Issues corrigées, tests passent |
| **6. Git Merge** | 13-16 | Commit, push et merge master | Merge OK sur master |

### Workflow CRUD Rapide

```
1. /crud-generator EntityName
   ↓
2. Répondre aux questions (propriétés, relations, sécurité)
   ↓
3. Génération automatique de tous les fichiers
   → Interfaces avec commentaires XML complets
   → Implémentations avec /// <inheritdoc />
   → Tests unitaires
   ↓
4. Créer migration :
   dotnet ef migrations add Add{EntityName}Entity --project Dal --startup-project Web
   dotnet ef database update --project Dal --startup-project Web
   ↓
5. Tests doivent PASSER
   → dotnet test Tests/Tests.csproj
   → Tous les tests doivent être ✅
   ↓
6. Refactoring & Audits
   → /performance-audit
   → Audit de sécurité
   → Validation finale ✅
```

---

## 🎨 Bonnes Pratiques OBLIGATOIRES

### Code

1. **Interfaces** : OBLIGATOIRES pour Services et Repositories
   - Toujours créer une interface (IXxxService, IXxxRepository)
   - Commentaires XML COMPLETS sur l'interface
   - Utiliser `/// <inheritdoc />` sur l'implémentation

   ```csharp
   // Interface - Commentaires XML complets
   public interface IUserService
   {
       /// <summary>
       /// Récupère un utilisateur par son ID
       /// </summary>
       /// <param name="id">L'identifiant de l'utilisateur</param>
       /// <returns>Les détails de l'utilisateur ou null si non trouvé</returns>
       Task<UserDto?> GetByIdAsync(int id);
   }

   // Implémentation - Utiliser inheritdoc
   public class UserService : IUserService
   {
       /// <inheritdoc />
       public async Task<UserDto?> GetByIdAsync(int id)
       {
           // Implémentation
       }
   }
   ```

2. **Commentaires XML** : OBLIGATOIRES sur toutes les interfaces et classes publiques
   - Sur INTERFACES : Commentaires complets (summary, param, returns, remarks si nécessaire)
   - Sur IMPLÉMENTATIONS : `/// <inheritdoc />` uniquement
   - Ne JAMAIS dupliquer la documentation

3. **Async/Await** : TOUJOURS pour les opérations I/O
   - Jamais `.Result` ou `.Wait()`
   - Toujours `async Task<T>`

4. **AsNoTracking** : TOUJOURS pour les requêtes en lecture seule
   ```csharp
   await _context.Events.AsNoTracking().ToListAsync();
   ```

5. **Pagination** : TOUJOURS pour les listes
   ```csharp
   .Skip((pageNumber - 1) * pageSize).Take(pageSize)
   ```

6. **Gestion d'erreurs** :
   - Try-catch dans les services si nécessaire
   - Logging des erreurs
   - Retourner des codes HTTP appropriés (400, 404, 500, etc.)

### Tests (TDD - Test-Driven Development)

1. **TDD Obligatoire** : Écrire les tests AVANT l'implémentation
2. **AAA Pattern** : Arrange, Act, Assert
3. **Noms descriptifs** : `MethodName_Scenario_ExpectedResult`
4. **Tests isolés** : Pas de dépendances entre tests
5. **Mock approprié** : Moq pour les dépendances
6. **Coverage** : Minimum 80%, objectif 90%+
7. **Tous les tests doivent PASSER** : 0 échec toléré avant de considérer la feature terminée

---

## 🔧 Commandes Utiles

### Tests
```powershell
# Tests avec couverture complète
pwsh -File run-coverage.ps1

# Tests uniquement
dotnet test Tests/Tests.csproj
```

### Migrations
```bash
# Créer migration
dotnet ef migrations add MigrationName --project Dal --startup-project Web

# Appliquer migration
dotnet ef database update --project Dal --startup-project Web

# Rollback
dotnet ef database update PreviousMigrationName --project Dal --startup-project Web
```

### Build
```bash
# Build
dotnet build

# Run
dotnet run --project Web
```

---

## ❌ Erreurs Courantes à Éviter

### Architecture
- ❌ Mettre de la logique métier dans Controllers
- ❌ Injecter DbContext directement dans Services
- ❌ Retourner des entités Bo depuis l'API
- ✅ TOUJOURS passer par DTOs

### Performance
- ❌ Oublier `.AsNoTracking()` pour lecture seule
- ❌ N+1 queries (oublier `.Include()`)
- ❌ Pas de pagination sur les listes
- ❌ Include excessifs
- ✅ Utiliser /performance-audit régulièrement

### Sécurité
- ❌ Secrets en dur dans le code
- ❌ Endpoints sensibles sans `[Authorize]`
- ❌ Validation uniquement côté client
- ✅ TOUJOURS valider côté serveur

### Tests
- ❌ Coverage < 80%
- ❌ Tests qui dépendent les uns des autres
- ❌ Tests sans assertions claires
- ✅ AAA Pattern + noms descriptifs

---

## 📚 Documentation du Projet

### Fichiers Importants

- **README.md** : Introduction et démarrage rapide
- **CLAUDE.md** : Ce fichier - mémoire du projet (LU À CHAQUE SESSION)
- **doc/SKILLS_GUIDE.md** : Guide complet des skills
- **doc/NEW_SKILLS_SUMMARY.md** : Résumé des nouveaux skills
- **doc/specs/** : Documents de spécification (doc-first)
- **doc/specs/TEMPLATE.md** : Template pour nouvelles specs
- **.claude/skills/** : Définitions des skills

### Structure de Documentation

Toute feature DOIT avoir :
1. Document de spec dans `doc/specs/`
2. Commentaires XML dans le code
3. Tests documentant le comportement
4. Update du README si nécessaire

---

## 🔐 Validation des Comptes Utilisateurs

### Workflow de Validation

**1. Inscription (POST /api/users)**
- User crée un compte avec email + password
- Statut automatique : **Pending**
- User reçoit email de confirmation (optionnel)
- User **NE PEUT PAS** se connecter

**2. Tentative de Connexion (POST /api/users/login)**
- Si statut = Pending : Retourner **403 Forbidden**
  - Message : "Votre compte est en attente de validation par un administrateur"
- Si statut = Rejected : Retourner **403 Forbidden**
  - Message : "Votre compte a été refusé. Contactez l'administrateur."
- Si statut = Approved : Login OK, retourne JWT

**3. Modération Admin (GET /api/users/pending)**
- Liste tous les users avec statut Pending
- Affiche : email, nom, prénom, date inscription
- **Requis** : Rôle Admin

**4. Actions Admin**
- **Approuver** : `PUT /api/users/{id}/approve`
  - Change statut → Approved
  - User peut maintenant se connecter
  - Envoyer email notification (optionnel)
  - **Requis** : Rôle Admin

- **Refuser** : `PUT /api/users/{id}/reject`
  - Change statut → Rejected
  - User ne peut pas se connecter
  - Envoyer email notification avec raison (optionnel)
  - **Requis** : Rôle Admin

### Implémentation Backend

**Entité User (Bo/User.cs)**
```csharp
public enum UserStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending; // Default
    // ... autres propriétés
}
```

**Service (Business/UserService.cs)**
```csharp
public async Task<LoginResponse?> LoginAsync(LoginDto dto)
{
    var user = await _repository.GetByEmailAsync(dto.Email);

    if (user == null || !VerifyPassword(user, dto.Password))
        return null; // Invalid credentials

    // Vérifier le statut
    if (user.Status == UserStatus.Pending)
        throw new ForbiddenException("Votre compte est en attente de validation");

    if (user.Status == UserStatus.Rejected)
        throw new ForbiddenException("Votre compte a été refusé");

    // OK, générer JWT
    return GenerateToken(user);
}

public async Task ApproveUserAsync(int userId)
{
    var user = await _repository.GetByIdAsync(userId);
    if (user == null)
        throw new NotFoundException();

    user.Status = UserStatus.Approved;
    await _repository.UpdateAsync(user);

    // Optionnel : envoyer email
    await _emailService.SendApprovalEmailAsync(user.Email);
}
```

**Controller Admin (Web/Controllers/AdminController.cs)**
```csharp
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    /// <summary>
    /// Liste des users en attente de validation
    /// </summary>
    [HttpGet("pending-users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetPendingUsers()
    {
        var users = await _userService.GetUsersByStatusAsync(UserStatus.Pending);
        return Ok(users);
    }

    /// <summary>
    /// Approuver un user
    /// </summary>
    [HttpPut("users/{id}/approve")]
    public async Task<ActionResult> ApproveUser(int id)
    {
        await _userService.ApproveUserAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Refuser un user
    /// </summary>
    [HttpPut("users/{id}/reject")]
    public async Task<ActionResult> RejectUser(int id, [FromBody] RejectDto dto)
    {
        await _userService.RejectUserAsync(id, dto.Reason);
        return NoContent();
    }

    /// <summary>
    /// Modifier le statut d'un user
    /// </summary>
    [HttpPut("users/{id}/status")]
    public async Task<ActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        await _userService.UpdateStatusAsync(id, dto.Status);
        return NoContent();
    }
}
```

### Migration Base de Données

```bash
# Ajouter colonne Status à la table Users
dotnet ef migrations add AddUserStatusColumn --project Dal --startup-project Web
dotnet ef database update --project Dal --startup-project Web
```

**Migration SQL généré** :
```sql
ALTER TABLE Users
ADD Status INT NOT NULL DEFAULT 0; -- 0 = Pending

-- Mettre les users existants en Approved
UPDATE Users SET Status = 1 WHERE Status = 0;
```

### Tests Requis

**UserServiceTests.cs**
```csharp
[TestMethod]
public async Task LoginAsync_WhenUserPending_ShouldThrowForbiddenException()
{
    // Arrange
    var user = new User { Email = "test@test.com", Status = UserStatus.Pending };
    _mockRepository.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

    // Act & Assert
    await Assert.ThrowsExceptionAsync<ForbiddenException>(
        () => _service.LoginAsync(new LoginDto { Email = "test@test.com", Password = "pass" })
    );
}

[TestMethod]
public async Task ApproveUserAsync_WhenCalled_ShouldUpdateStatus()
{
    // Arrange
    var user = new User { Id = 1, Status = UserStatus.Pending };
    _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

    // Act
    await _service.ApproveUserAsync(1);

    // Assert
    Assert.AreEqual(UserStatus.Approved, user.Status);
    _mockRepository.Verify(r => r.UpdateAsync(user), Times.Once);
}
```

---

## 🎯 Objectifs de Qualité

### Métriques
- **Couverture de code** : ≥ 80% (objectif 90%+)
- **Warnings** : 0 (tolérance zéro)
- **Sécurité** : Scan OWASP Top 10 régulier
- **Performance** : Audit régulier avec /performance-audit

### Code Review
Chaque changement significatif DOIT passer par :
1. Auto-review avec /code-review
2. Tests passants avec coverage OK
3. Commit message structuré
4. Documentation à jour

---

## 🚨 Rappels Importants pour Claude

### À CHAQUE Session

1. ✅ Lire ce fichier CLAUDE.md ENTIÈREMENT
2. ✅ Respecter l'architecture en couches STRICTEMENT
3. ✅ **Suivre le Workflow en 16 Étapes pour TOUTE nouvelle fonctionnalité**
4. ✅ **Créer une branche git AVANT toute implémentation** (feature/, fix/, refactor/)
5. ✅ Mode planification AVANT implémentation (EnterPlanMode)
6. ✅ TDD : Tests AVANT implémentation
7. ✅ Créer INTERFACES avec commentaires XML complets
8. ✅ Utiliser /// <inheritdoc /> sur implémentations
9. ✅ Tous les tests doivent PASSER (0 échec) après implémentation ET après audits
10. ✅ Audits obligatoires (refactoring + /performance-audit + sécurité)
11. ✅ Implémenter le frontend après validation backend
12. ✅ Attendre validation utilisateur AVANT SonarCloud
13. ✅ Corriger issues SonarCloud (Security + Blocker + High)
14. ✅ Attendre validation finale AVANT git commit/push
15. ✅ **Merge sur master APRÈS push et validation utilisateur**

### Ne JAMAIS

1. ❌ **Travailler directement sur la branche master** (TOUJOURS créer une branche)
2. ❌ Coder sans documentation préalable (sauf typos/formatting)
3. ❌ Implémenter AVANT d'écrire les tests (TDD strict)
4. ❌ Mettre de la logique métier hors de Business Layer
5. ❌ Retourner des entités Bo dans l'API
6. ❌ Oublier les commentaires XML sur INTERFACES
7. ❌ Dupliquer la documentation (utiliser inheritdoc)
8. ❌ Implémenter sans validation utilisateur de la doc
9. ❌ Terminer une feature sans audits (performance + sécurité)
10. ❌ Passer au frontend sans que backend soit validé
11. ❌ Lancer SonarCloud sans validation utilisateur du test manuel
12. ❌ Git commit/push sans validation finale de l'utilisateur
13. ❌ Ignorer les issues Security/Blocker/High de SonarCloud
14. ❌ Merger sur master sans validation utilisateur

### En Cas de Doute

1. Relire ce CLAUDE.md
2. Consulter les skills dans `.claude/skills/`
3. Regarder le code existant pour les patterns
4. Demander à l'utilisateur

---

## 📊 État Actuel du Projet

### Modules Existants

- ✅ Authentification (JWT, Refresh tokens)
- ✅ Users (CRUD + rôles)
- ✅ Events (CRUD + inscriptions)
- ✅ Calendars (CRUD + organisateurs)
- ✅ Proposals (CRUD + vote)
- ✅ Templates

### Skills Configurés

- ✅ code-review
- ✅ doc-first (TDD workflow)
- ✅ crud-generator
- ✅ performance-audit

### Tests

- ✅ Framework configuré (MSTest + Moq)
- ✅ Script de couverture (`run-coverage.ps1`)
- ✅ InMemory DB configurée

---

## 🔄 Maintenance de ce Fichier

**Ce fichier DOIT être mis à jour quand** :
- Nouveau module ajouté
- Changement architectural important
- Nouvelle convention adoptée
- Nouveau skill installé
- Pattern modifié

**Format de mise à jour** :
```markdown
**Dernière mise à jour** : YYYY-MM-DD

## Changelog
- YYYY-MM-DD : [Description du changement]
```

---

## 📅 Changelog

### 2026-01-16 : Gestion des Branches Git
- **Ajout** : Phase 0 - Création de branche git obligatoire AVANT toute implémentation
- **Ajout** : Étapes 15-16 - Validation et merge sur master après push
- **Convention** : Nommage des branches (feature/, fix/, refactor/)
- **Workflow** : Passe de 14 à 16 étapes
- **Règle** : JAMAIS travailler directement sur master

### 2026-01-14 : Workflow Complet en 14 Étapes
- **Ajout** : Nouveau workflow complet pour toute nouvelle fonctionnalité
- **Phases** :
  1. Planification & Documentation (EnterPlanMode + doc specs)
  2. Backend TDD (Tests → Implémentation → Validation)
  3. Audits & Optimisation (Refactoring + Performance + Sécurité + Re-run tests)
  4. Frontend (Nuxt/Vue + Test manuel utilisateur)
  5. SonarCloud (Scan + Correction Security/Blocker/High + Re-run tests)
  6. Git (Validation finale + Commit + Push)
- **Intégration SonarCloud** : Commandes et URLs documentées
- **Validation utilisateur** : Checkpoints obligatoires avant frontend, avant SonarCloud, et avant commit
- **Total estimé** : 1h30-3h par feature complète

---

## 📞 Support

Pour toute question sur :
- **Skills** : Voir `doc/SKILLS_GUIDE.md`
- **Architecture** : Relire section Architecture ci-dessus
- **Workflow** : Voir section Workflow de Développement
- **Problème** : Demander à Claude en mentionnant ce fichier

---

**Ce fichier garantit la cohérence entre toutes les sessions Claude. Ne jamais le supprimer !**
