# CLAUDE.md - Mémoire Projet ESN-WebApi

> **Ce fichier est lu automatiquement par Claude à chaque démarrage de session**
> Il contient toute la connaissance du projet pour assurer la cohérence entre les sessions

**Dernière mise à jour** : 2026-01-14

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

### Workflow Standard pour Nouvelle Feature (TDD)

```
1. Demande utilisateur
   ↓
2. Mode Planification (doc-first s'active)
   → Crée doc/specs/YYYYMMDD-feature-name.md
   → Présente pour validation
   ↓
3. Validation utilisateur du plan
   ↓
4. Création des TESTS EN PREMIER (TDD)
   → Tests unitaires pour la feature
   → Tests couvrent tous les cas (succès, échec, edge cases)
   → Les tests échouent (feature pas encore implémentée)
   ↓
5. Si nouveau controller nécessaire
   → Utiliser /crud-generator EntityName
   → Génère interfaces, DTOs, Repo, Service, Controller, Tests
   ↓
6. Implémentation selon la doc
   → Respect strict de l'architecture
   → Créer INTERFACES (IXxxService, IXxxRepository)
   → Commentaires XML complets sur INTERFACES
   → Utiliser /// <inheritdoc /> sur implémentations
   ↓
7. Exécution tests
   → Tous les tests doivent PASSER (100%)
   → dotnet test Tests/Tests.csproj
   → Coverage ≥ 80%
   ↓
8. Refactoring & Audits (OBLIGATOIRE)
   → Refactoring du code (DRY, SOLID, Clean Code)
   → Audit de performance (/performance-audit)
     • Vérifier N+1 queries
     • AsNoTracking sur lectures seules
     • Index appropriés
   → Audit de sécurité
     • OWASP Top 10
     • Validation des entrées
     • Gestion des erreurs sécurisée
   ↓
9. Validation finale
   → Tests passent ✅
   → Audits OK ✅
   → Code review si nécessaire
```

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
3. ✅ Mode planification AVANT implémentation (/doc-first)
4. ✅ TDD : Tests AVANT implémentation
5. ✅ Créer INTERFACES avec commentaires XML complets
6. ✅ Utiliser /// <inheritdoc /> sur implémentations
7. ✅ Tous les tests doivent PASSER (0 échec)
8. ✅ Audits obligatoires (/performance-audit + sécurité)

### Ne JAMAIS

1. ❌ Coder sans documentation préalable (sauf typos/formatting)
2. ❌ Implémenter AVANT d'écrire les tests (TDD strict)
3. ❌ Mettre de la logique métier hors de Business Layer
4. ❌ Retourner des entités Bo dans l'API
5. ❌ Oublier les commentaires XML sur INTERFACES
6. ❌ Dupliquer la documentation (utiliser inheritdoc)
7. ❌ Implémenter sans validation utilisateur de la doc
8. ❌ Terminer une feature sans audits (performance + sécurité)

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

## 📞 Support

Pour toute question sur :
- **Skills** : Voir `doc/SKILLS_GUIDE.md`
- **Architecture** : Relire section Architecture ci-dessus
- **Workflow** : Voir section Workflow de Développement
- **Problème** : Demander à Claude en mentionnant ce fichier

---

**Ce fichier garantit la cohérence entre toutes les sessions Claude. Ne jamais le supprimer !**
