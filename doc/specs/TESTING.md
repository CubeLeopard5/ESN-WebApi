# Standards de Tests - ESN-WebApi

**Date de création** : 2026-01-12
**Dernière mise à jour** : 2026-01-12
**Statut** : ✅ Document de référence

---

## 📋 Objectif

Ce document définit les standards de tests **obligatoires** pour le projet ESN-WebApi. Tous les nouveaux développements DOIVENT inclure des tests respectant ces standards.

---

## 🎯 Objectifs de Couverture

### Cibles Obligatoires

| Couche | Minimum | Objectif | Priorité |
|--------|---------|----------|----------|
| **Business** | 90% | 100% | ⭐⭐⭐ Critique |
| **Dal** | 80% | 95% | ⭐⭐⭐ Critique |
| **Web** | 70% | 85% | ⭐⭐ Important |
| **Global** | 80% | 90% | ⭐⭐⭐ Obligatoire |

**Règle STRICTE** : Aucun PR ne peut être mergé avec une couverture < 80%

---

## 🛠️ Framework et Outils

### Stack de Tests

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.x" />
<PackageReference Include="MSTest.TestFramework" Version="3.x" />
<PackageReference Include="MSTest.TestAdapter" Version="3.x" />
<PackageReference Include="Moq" Version="4.x" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.x" />
<PackageReference Include="coverlet.collector" Version="6.x" />
```

### Pourquoi ce Stack ?

- **MSTest** : Framework officiel Microsoft, bien intégré Visual Studio
- **Moq** : Library de mocking simple et puissante
- **InMemory DB** : Base de données en mémoire pour tester les repositories sans SQL Server
- **Coverlet** : Génération de rapports de couverture

---

## 📐 Pattern AAA (Arrange-Act-Assert)

**Règle OBLIGATOIRE** : Tous les tests DOIVENT suivre le pattern AAA

### Structure

```csharp
[TestMethod]
public async Task MethodName_Scenario_ExpectedResult()
{
    // ===== ARRANGE =====
    // Setup : Créer les dépendances, mocks, données de test

    // ===== ACT =====
    // Action : Exécuter la méthode à tester

    // ===== ASSERT =====
    // Vérification : Vérifier les résultats attendus
}
```

### Exemple Complet

```csharp
[TestClass]
public class EventServiceTests
{
    [TestMethod]
    public async Task GetByIdAsync_WhenEventExists_ShouldReturnEvent()
    {
        // ===== ARRANGE =====
        // Mock du UnitOfWork
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockLogger = new Mock<ILogger<EventService>>();

        // Données de test
        var eventBo = new EventBo
        {
            Id = 1,
            Title = "Test Event",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7),
            UserId = 1
        };

        var eventDto = new EventDto
        {
            Id = 1,
            Title = "Test Event"
        };

        // Configuration des mocks
        mockUnitOfWork
            .Setup(u => u.Events.GetEventWithDetailsAsync(1))
            .ReturnsAsync(eventBo);

        mockMapper
            .Setup(m => m.Map<EventDto>(eventBo))
            .Returns(eventDto);

        var service = new EventService(
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockLogger.Object
        );

        // ===== ACT =====
        var result = await service.GetByIdAsync(1, "test@test.com");

        // ===== ASSERT =====
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Id);
        Assert.AreEqual("Test Event", result.Title);

        // Vérifier que les méthodes mockées ont été appelées
        mockUnitOfWork.Verify(
            u => u.Events.GetEventWithDetailsAsync(1),
            Times.Once
        );
    }
}
```

---

## 🧪 Tests par Couche

### 1. Tests Business Layer (Services)

**Responsabilité** : Tester la logique métier

#### Structure Type

```csharp
[TestClass]
public class EventServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IMapper> _mockMapper;
    private Mock<ILogger<EventService>> _mockLogger;
    private EventService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<EventService>>();

        _service = new EventService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    [TestMethod]
    public async Task CreateEventAsync_WhenValid_ShouldReturnEventDto()
    {
        // Test implementation
    }

    [TestMethod]
    public async Task CreateEventAsync_WhenUserNotFound_ShouldThrowKeyNotFoundException()
    {
        // Test implementation
    }

    [TestMethod]
    public async Task UpdateEventAsync_WhenNotOwner_ShouldThrowUnauthorizedAccessException()
    {
        // Test implementation
    }
}
```

#### Scénarios à Tester

**✅ Cas nominaux (Happy Path)** :
- Opération réussit avec données valides
- Retourne le résultat attendu
- Appelle les bonnes méthodes du repository

**✅ Cas d'erreur** :
- Entité non trouvée → `KeyNotFoundException`
- Données invalides → `ArgumentException`
- Non autorisé → `UnauthorizedAccessException`
- Opération invalide → `InvalidOperationException`

**✅ Règles métier** :
- Capacité maximale respectée
- Dates de registration valides
- Permissions vérifiées
- Transactions gérées

#### Exemple Complet

```csharp
[TestMethod]
public async Task RegisterForEventAsync_WhenEventFull_ShouldThrowInvalidOperationException()
{
    // Arrange
    var eventBo = new EventBo
    {
        Id = 1,
        MaxParticipants = 10,
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddDays(7),
        EventRegistrations = Enumerable.Range(1, 10)
            .Select(i => new EventRegistrationBo
            {
                Status = RegistrationStatus.Registered
            })
            .ToList()
    };

    var userBo = new UserBo { Id = 1, Email = "test@test.com" };

    _mockUnitOfWork
        .Setup(u => u.Events.GetEventWithDetailsAsync(1))
        .ReturnsAsync(eventBo);

    _mockUnitOfWork
        .Setup(u => u.Users.GetByEmailAsync("test@test.com"))
        .ReturnsAsync(userBo);

    // Act & Assert
    await Assert.ThrowsExceptionAsync<InvalidOperationException>(
        () => _service.RegisterForEventAsync(1, "test@test.com", "{}")
    );
}
```

### 2. Tests Dal Layer (Repositories)

**Responsabilité** : Tester l'accès aux données avec InMemory DB

#### Configuration InMemory

```csharp
[TestClass]
public class EventRepositoryTests
{
    private EsnDevContext _context;
    private EventRepository _repository;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<EsnDevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EsnDevContext(options);
        _repository = new EventRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenExists_ShouldReturnEntity()
    {
        // Arrange
        var eventBo = new EventBo
        {
            Title = "Test Event",
            StartDate = DateTime.UtcNow,
            UserId = 1
        };

        _context.Events.Add(eventBo);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(eventBo.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Event", result.Title);
    }

    [TestMethod]
    public async Task GetEventsPagedAsync_ShouldReturnCorrectPage()
    {
        // Arrange - Créer 15 événements
        for (int i = 1; i <= 15; i++)
        {
            _context.Events.Add(new EventBo
            {
                Title = $"Event {i}",
                StartDate = DateTime.UtcNow,
                UserId = 1
            });
        }
        await _context.SaveChangesAsync();

        // Act - Récupérer page 2 (5 éléments par page)
        var (events, totalCount) = await _repository.GetEventsPagedAsync(skip: 5, take: 5);

        // Assert
        Assert.AreEqual(15, totalCount);
        Assert.AreEqual(5, events.Count);
    }
}
```

#### Scénarios à Tester

**✅ CRUD de base** :
- GetByIdAsync retourne l'entité
- GetAllAsync retourne toutes les entités
- AddAsync ajoute l'entité
- Update modifie l'entité
- Delete supprime l'entité

**✅ Requêtes complexes** :
- Pagination fonctionne correctement
- Filtres appliqués correctement
- Includes chargent les relations
- Tri appliqué correctement

**✅ Cas limites** :
- GetByIdAsync avec ID inexistant retourne null
- Pagination avec page hors limite retourne liste vide
- Filtres sans résultats retournent liste vide

### 3. Tests Web Layer (Controllers)

**Responsabilité** : Tester les endpoints API

#### Structure Type

```csharp
[TestClass]
public class EventsControllerTests
{
    private Mock<IEventService> _mockService;
    private Mock<ILogger<EventsController>> _mockLogger;
    private EventsController _controller;

    [TestInitialize]
    public void Setup()
    {
        _mockService = new Mock<IEventService>();
        _mockLogger = new Mock<ILogger<EventsController>>();

        _controller = new EventsController(
            _mockService.Object,
            _mockLogger.Object
        );

        // Mock du User (ClaimsPrincipal)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test@test.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    [TestMethod]
    public async Task GetEvent_WhenExists_ShouldReturnOk()
    {
        // Arrange
        var eventDto = new EventDto { Id = 1, Title = "Test Event" };

        _mockService
            .Setup(s => s.GetByIdAsync(1, It.IsAny<string>()))
            .ReturnsAsync(eventDto);

        // Act
        var result = await _controller.GetEvent(1);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        var okResult = result.Result as OkObjectResult;
        Assert.AreEqual(eventDto, okResult.Value);
    }

    [TestMethod]
    public async Task GetEvent_WhenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetByIdAsync(999, It.IsAny<string>()))
            .ReturnsAsync((EventDto?)null);

        // Act
        var result = await _controller.GetEvent(999);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task PostEvent_WhenValid_ShouldReturnCreated()
    {
        // Arrange
        var createDto = new CreateEventDto
        {
            Title = "New Event",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7)
        };

        var eventDto = new EventDto { Id = 1, Title = "New Event" };

        _mockService
            .Setup(s => s.CreateEventAsync(createDto, "test@test.com"))
            .ReturnsAsync(eventDto);

        // Act
        var result = await _controller.PostEvent(createDto);

        // Assert
        Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult));
        var createdResult = result.Result as CreatedAtActionResult;
        Assert.AreEqual(eventDto, createdResult.Value);
    }
}
```

---

## 📋 Conventions de Nommage

### Noms de Méthodes de Test

**Format** :
```
MethodName_Scenario_ExpectedResult
```

**Exemples** :
```csharp
GetByIdAsync_WhenEventExists_ShouldReturnEvent()
GetByIdAsync_WhenEventNotFound_ShouldReturnNull()
CreateEventAsync_WhenValid_ShouldReturnEventDto()
CreateEventAsync_WhenUserNotFound_ShouldThrowKeyNotFoundException()
RegisterForEventAsync_WhenEventFull_ShouldThrowInvalidOperationException()
UpdateEventAsync_WhenNotOwner_ShouldThrowUnauthorizedAccessException()
DeleteEventAsync_WhenEventHasRegistrations_ShouldDeleteCascade()
```

### Noms de Classes de Test

**Format** :
```
{ClassName}Tests
```

**Exemples** :
```csharp
EventServiceTests
UserServiceTests
EventRepositoryTests
UserRepositoryTests
EventsControllerTests
UsersControllerTests
```

---

## 🎯 Scénarios à Tester

### Checklist Complète

#### ✅ Cas Nominaux (Happy Path)
- [ ] Opération réussit avec données valides
- [ ] Retourne le résultat attendu (bonne structure, bonnes valeurs)
- [ ] Appelle les bonnes dépendances (Verify des mocks)

#### ✅ Cas d'Erreur
- [ ] Entité non trouvée → retourne null ou throw KeyNotFoundException
- [ ] Données invalides → throw ArgumentException
- [ ] Validation échoue → throw ValidationException
- [ ] Non autorisé → throw UnauthorizedAccessException
- [ ] Opération invalide → throw InvalidOperationException

#### ✅ Règles Métier
- [ ] Capacité maximale respectée
- [ ] Dates valides (start < end)
- [ ] Permissions vérifiées (ownership, rôles)
- [ ] Statuts corrects (Pending, Approved, Registered, etc.)
- [ ] Soft delete fonctionnel

#### ✅ Cas Limites (Edge Cases)
- [ ] Valeur null
- [ ] String vide
- [ ] Liste vide
- [ ] Valeurs min/max
- [ ] Concurrence (si applicable)
- [ ] Pagination (première page, dernière page, page hors limites)

#### ✅ Intégration
- [ ] Transactions rollback en cas d'erreur
- [ ] Cascades (delete, update)
- [ ] Includes chargent les bonnes relations
- [ ] AsNoTracking ne modifie pas les entités

---

## 🔧 Exécution des Tests

### Commandes

```bash
# Exécuter tous les tests
dotnet test

# Exécuter avec couverture
dotnet test /p:CollectCoverage=true

# Script PowerShell avec rapport HTML
pwsh -File run-coverage.ps1
```

### Script run-coverage.ps1

```powershell
# Exécuter les tests avec couverture
dotnet test Tests/Tests.csproj `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=cobertura `
    /p:CoverletOutput=./TestResults/

# Générer rapport HTML
reportgenerator `
    -reports:"Tests/TestResults/coverage.cobertura.xml" `
    -targetdir:"Tests/TestResults/html" `
    -reporttypes:Html

# Ouvrir le rapport
Start-Process "Tests/TestResults/html/index.html"
```

---

## ⚡ Bonnes Pratiques

### ✅ À FAIRE

```csharp
// ✅ Tests isolés (pas de dépendances entre tests)
[TestMethod]
public async Task Test1() { /* Indépendant */ }

[TestMethod]
public async Task Test2() { /* Indépendant */ }

// ✅ Données de test explicites
var eventBo = new EventBo
{
    Title = "Test Event",
    StartDate = DateTime.UtcNow,
    MaxParticipants = 10
};

// ✅ Assertions claires
Assert.IsNotNull(result);
Assert.AreEqual(expected, actual);
Assert.IsTrue(condition, "Message descriptif");

// ✅ Verify des mocks
mockRepository.Verify(
    r => r.GetByIdAsync(1),
    Times.Once
);

// ✅ Test des exceptions
await Assert.ThrowsExceptionAsync<InvalidOperationException>(
    () => service.MethodAsync()
);
```

### ❌ À ÉVITER

```csharp
// ❌ Tests dépendants
[TestMethod]
public async Task Test1_CreateUser() { /* Crée user ID=1 */ }

[TestMethod]
public async Task Test2_UpdateUser() { /* Dépend de Test1 */ }

// ❌ Magic numbers
Assert.AreEqual(42, result.Count); // Quoi 42 ?

// ❌ Assertions sans message
Assert.IsTrue(result.IsValid); // Pourquoi ça échoue ?

// ❌ Tests trop longs (> 50 lignes)
[TestMethod]
public async Task VeryLongTest() { /* 100+ lignes */ }

// ❌ Pas de Cleanup
[TestMethod]
public async Task Test()
{
    var context = new EsnDevContext(options);
    // Test
    // ❌ Pas de context.Dispose()
}
```

---

## 📊 Rapport de Couverture

### Vérification de la Couverture

Après `pwsh -File run-coverage.ps1` :

1. Ouvrir `Tests/TestResults/html/index.html`
2. Vérifier par couche :
   - **Business** : ≥ 90%
   - **Dal** : ≥ 80%
   - **Web** : ≥ 70%
   - **Global** : ≥ 80%

3. Identifier les lignes non couvertes (rouge dans le rapport)
4. Ajouter des tests pour couvrir les cas manquants

### Exemples de Rapport

```
+---------+--------+--------+--------+
| Module  | Line % | Branch %| Method %|
+---------+--------+--------+--------+
| Business| 95.2%  | 91.3%  | 100%   |
| Dal     | 88.7%  | 84.2%  | 95.1%  |
| Web     | 76.5%  | 70.8%  | 82.3%  |
+---------+--------+--------+--------+
| Total   | 86.8%  | 82.1%  | 92.5%  |
+---------+--------+--------+--------+
```

---

## ✅ Checklist Avant Commit

- [ ] Tous les tests passent (`dotnet test`)
- [ ] Couverture globale ≥ 80% (`pwsh -File run-coverage.ps1`)
- [ ] Business layer ≥ 90%
- [ ] Nouveaux tests suivent pattern AAA
- [ ] Noms de tests respectent convention `Method_Scenario_Result`
- [ ] Pas de tests commentés ou ignorés sans raison
- [ ] Pas de `Console.WriteLine` ou code debug
- [ ] Tests isolés (pas de dépendances entre eux)
- [ ] Cleanup approprié (Dispose, EnsureDeleted)

---

**Ce document est la référence pour tous les tests du projet.**
**En cas de doute, consulter ce document.**
