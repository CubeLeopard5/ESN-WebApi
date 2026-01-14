# Validation des Comptes Utilisateurs

**Date** : 2026-01-10
**Auteur** : Claude + Utilisateur
**Type** : Feature
**Statut** : 🟡 En Documentation

---

## 📋 Contexte et Objectif

### Problème/Besoin

Actuellement, tous les utilisateurs qui s'inscrivent peuvent se connecter immédiatement. Il n'y a pas de processus de modération pour valider les nouvelles inscriptions.

**Besoin identifié** :
- Empêcher l'accès immédiat après inscription
- Permettre aux administrateurs de valider ou refuser les inscriptions
- Gérer différents statuts de compte (en attente, approuvé, refusé)

### Objectif

Implémenter un système de validation des comptes utilisateurs où :
1. Les nouveaux utilisateurs créent un compte avec statut "Pending"
2. Ils ne peuvent pas se connecter tant que le compte n'est pas approuvé
3. Les administrateurs peuvent approuver, refuser ou modifier le statut
4. Les utilisateurs reçoivent un feedback approprié selon leur statut

### Impact

- **Utilisateurs affectés** : Tous les nouveaux utilisateurs + Administrateurs
- **Modules impactés** :
  - Backend : Bo (User entity), Business (UserService), Dal (UserRepository), Web (Controllers)
  - Frontend : Pages login/register, page admin/pending-users
- **Breaking changes** : Non (les utilisateurs existants auront statut Approved par défaut)

---

## 🎯 Spécifications Fonctionnelles

### User Stories

1. **En tant qu'utilisateur**, je veux créer un compte afin de participer aux événements ESN
   - Après inscription, je suis informé que mon compte est en attente de validation
   - Je ne peux pas me connecter tant que mon compte n'est pas approuvé

2. **En tant qu'utilisateur avec compte en attente**, je veux être informé clairement pourquoi je ne peux pas me connecter
   - Message : "Votre compte est en attente de validation par un administrateur"

3. **En tant qu'administrateur**, je veux voir la liste des inscriptions en attente afin de les valider
   - Je vois : email, nom, prénom, date d'inscription
   - Je peux : approuver, refuser, ou modifier le statut

4. **En tant qu'administrateur**, je veux pouvoir approuver un compte afin que l'utilisateur puisse se connecter
   - Action en un clic
   - L'utilisateur est notifié (optionnel : email)

5. **En tant qu'administrateur**, je veux pouvoir refuser un compte avec une raison
   - Modal avec champ raison (optionnel)
   - L'utilisateur est notifié (optionnel : email)

### Règles Métier

1. **Statut par défaut** : Tout nouveau compte a le statut "Pending"
2. **Connexion bloquée** : Seuls les comptes "Approved" peuvent se connecter
3. **Utilisateurs existants** : Les comptes existants sont automatiquement "Approved" lors de la migration
4. **Modification statut** : Seuls les administrateurs peuvent modifier le statut
5. **Réactivation** : Un compte "Rejected" peut être remis en "Pending" ou "Approved" par un admin

### Comportement Attendu

#### Inscription (POST /api/users)
- User remplit formulaire inscription
- Backend crée User avec `Status = Pending`
- Réponse : HTTP 201 Created
- Message frontend : "Compte créé ! En attente de validation par un administrateur"

#### Tentative de connexion (POST /api/users/login)

**Cas 1 : Statut = Approved**
- Login réussi
- Retour JWT token

**Cas 2 : Statut = Pending**
- Login refusé
- HTTP 403 Forbidden
- Message : "Votre compte est en attente de validation par un administrateur"

**Cas 3 : Statut = Rejected**
- Login refusé
- HTTP 403 Forbidden
- Message : "Votre compte a été refusé. Contactez l'administrateur."

**Cas 4 : Credentials invalides**
- HTTP 401 Unauthorized
- Message : "Email ou mot de passe incorrect"

#### Liste users en attente (GET /api/admin/users/pending)
- Authentification : Admin requis
- Retourne : Liste des users avec statut Pending
- Tri : Date d'inscription décroissante (plus récents en premier)

#### Approuver user (PUT /api/admin/users/{id}/approve)
- Authentification : Admin requis
- Change statut → Approved
- Retour : HTTP 204 No Content
- Optionnel : Envoi email notification

#### Refuser user (PUT /api/admin/users/{id}/reject)
- Authentification : Admin requis
- Body : `{ "reason": "..." }` (optionnel)
- Change statut → Rejected
- Retour : HTTP 204 No Content
- Optionnel : Envoi email notification avec raison


### Cas Limites

- **User inexistant** : HTTP 404 Not Found
- **Admin non authentifié** : HTTP 401 Unauthorized
- **User non admin** : HTTP 403 Forbidden
- **Statut invalide** : HTTP 400 Bad Request
- **Email déjà existant** : HTTP 409 Conflict (existant)
- **Tentative de modifier son propre statut** : Autorisé (admin peut se modifier)

---

## 🏗️ Conception Technique

### Architecture

#### Couches Impactées

- [x] **Bo** : Ajout enum UserStatus + propriété Status dans User
- [x] **Dto** : Ajout RejectUserDto, UpdateStatusDto
- [x] **Dal** : Migration pour colonne Status
- [x] **Business** : Logique validation statut dans UserService
- [x] **Web** : Nouveau AdminController avec endpoints validation

#### Diagramme de Flux

```
Inscription
┌─────────┐     ┌─────────────┐     ┌──────────────┐     ┌────────┐
│ Client  │────▶│  POST /users │────▶│  UserService │────▶│  User  │
│         │     │              │     │  Create()    │     │Status=0│
└─────────┘     └─────────────┘     └──────────────┘     └────────┘

Connexion (Pending)
┌─────────┐     ┌──────────────┐     ┌──────────────┐
│ Client  │────▶│POST /login   │────▶│ UserService  │
│         │◀────│403 Forbidden │◀────│ CheckStatus()│
└─────────┘     └──────────────┘     └──────────────┘

Validation Admin
┌─────────┐     ┌──────────────────┐     ┌──────────────┐     ┌────────┐
│  Admin  │────▶│PUT /admin/users/ │────▶│ UserService  │────▶│  User  │
│         │     │   {id}/approve   │     │ Approve()    │     │Status=1│
└─────────┘     └──────────────────┘     └──────────────┘     └────────┘
```

### Interfaces Publiques

#### Enum UserStatus (Bo/UserStatus.cs)

```csharp
namespace Bo;

/// <summary>
/// Statut d'un compte utilisateur
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Compte en attente de validation
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Compte approuvé, peut se connecter
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Compte refusé, ne peut pas se connecter
    /// </summary>
    Rejected = 2
}
```

#### Entity User (Bo/User.cs)

```csharp
// Ajouter propriété
/// <summary>
/// Statut du compte utilisateur
/// </summary>
public UserStatus Status { get; set; } = UserStatus.Pending;
```

#### DTOs

**RejectUserDto.cs**
```csharp
namespace Dto;

/// <summary>
/// DTO pour refuser un utilisateur avec une raison
/// </summary>
public class RejectUserDto
{
    /// <summary>
    /// Raison du refus (optionnel)
    /// </summary>
    public string? Reason { get; set; }
}
```

#### Service Interface (Business/Services/IUserService.cs)

```csharp
// Ajouter méthodes

/// <summary>
/// Récupère les utilisateurs par statut
/// </summary>
/// <param name="status">Statut à filtrer</param>
/// <returns>Liste des utilisateurs avec ce statut</returns>
Task<IEnumerable<UserResponseDto>> GetUsersByStatusAsync(UserStatus status);

/// <summary>
/// Approuve un utilisateur
/// </summary>
/// <param name="userId">ID de l'utilisateur</param>
Task ApproveUserAsync(int userId);

/// <summary>
/// Refuse un utilisateur
/// </summary>
/// <param name="userId">ID de l'utilisateur</param>
/// <param name="reason">Raison du refus (optionnel)</param>
Task RejectUserAsync(int userId, string? reason = null);
```

#### Controller Admin (Web/Controllers/AdminUsersController.cs - NOUVEAU)

```csharp
using Business.Services;
using Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

/// <summary>
/// Contrôleur pour la gestion administrative des utilisateurs
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IUserService userService,
        ILogger<AdminUsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère les utilisateurs en attente de validation
    /// </summary>
    /// <returns>Liste des utilisateurs avec statut Pending</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), 200)]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetPendingUsers()
    {
        _logger.LogInformation("Récupération des utilisateurs en attente");
        var users = await _userService.GetUsersByStatusAsync(UserStatus.Pending);
        return Ok(users);
    }

    /// <summary>
    /// Approuve un utilisateur
    /// </summary>
    /// <param name="id">ID de l'utilisateur</param>
    [HttpPut("{id}/approve")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApproveUser(int id)
    {
        _logger.LogInformation("Approbation de l'utilisateur {UserId}", id);

        try
        {
            await _userService.ApproveUserAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Utilisateur {id} non trouvé" });
        }
    }

    /// <summary>
    /// Refuse un utilisateur
    /// </summary>
    /// <param name="id">ID de l'utilisateur</param>
    /// <param name="dto">Raison du refus</param>
    [HttpPut("{id}/reject")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RejectUser(int id, [FromBody] RejectUserDto dto)
    {
        _logger.LogInformation("Refus de l'utilisateur {UserId}", id);

        try
        {
            await _userService.RejectUserAsync(id, dto.Reason);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Utilisateur {id} non trouvé" });
        }
    }
}
```

#### Implémentation Frontend - Page Admin All Users

**Fichier** : `app/pages/admin/data/all-users.vue`

Cette page existante affiche déjà tous les utilisateurs. Nous allons la modifier pour ajouter :
1. Une colonne "Status" avec badge coloré
2. Une colonne "Actions" avec boutons Approve/Reject (visible uniquement pour users Pending)
3. Optionnel : Filtre pour afficher seulement les users Pending

**Modifications à apporter** :

```vue
<script setup lang="ts">
// ... imports existants
import type { UserDto, UserStatus } from '~/types/user';

const users = ref<PagedResult<UserDto>>();
const { getAllUsers } = useUserApi();
const { approveUser, rejectUser } = useAdminUsers(); // Nouveau composable
const { formatDate } = useFormatDate();

const globalFilter = ref('');
const statusFilter = ref<UserStatus | 'all'>('all'); // Nouveau filtre

// ... onMounted existant

// Filtrer users selon statut
const filteredUsers = computed(() => {
    if (!users.value) return [];
    if (statusFilter.value === 'all') return users.value.items;
    return users.value.items.filter(u => u.status === statusFilter.value);
});

// Fonction approve
const handleApprove = async (userId: number) => {
    await approveUser(userId);
    users.value = await getAllUsers(); // Rafraîchir
};

// Fonction reject
const handleReject = async (userId: number) => {
    const reason = prompt('Raison du refus (optionnel)');
    await rejectUser(userId, reason);
    users.value = await getAllUsers(); // Rafraîchir
};

const columns: ColumnDef<UserDto>[] = [
    // ... colonnes existantes (id, email, firstName, etc.)

    // NOUVELLE COLONNE : Status
    {
        accessorKey: 'status',
        header: 'Status',
        meta: {
            class: {
                th: 'text-center font-semibold',
                td: 'text-center'
            }
        },
        cell: ({ row }) => {
            const status = row.original.status;
            const colorMap = {
                0: 'warning',  // Pending
                1: 'success',  // Approved
                2: 'error'     // Rejected
            };
            const labelMap = {
                0: 'Pending',
                1: 'Approved',
                2: 'Rejected'
            };
            return h(UBadge, {
                color: colorMap[status],
                label: labelMap[status]
            });
        }
    },

    // NOUVELLE COLONNE : Actions
    {
        id: 'actions',
        header: 'Actions',
        meta: {
            class: {
                th: 'text-center font-semibold',
                td: 'text-center'
            }
        },
        cell: ({ row }) => {
            const user = row.original;

            // Afficher boutons seulement si Pending
            if (user.status !== 0) return null;

            return h('div', { class: 'flex gap-2 justify-center' }, [
                h(UButton, {
                    color: 'green',
                    size: 'xs',
                    label: 'Approve',
                    onClick: () => handleApprove(user.id)
                }),
                h(UButton, {
                    color: 'red',
                    size: 'xs',
                    label: 'Reject',
                    onClick: () => handleReject(user.id)
                })
            ]);
        }
    }
];
</script>

<template>
    <div class="p-4 sm:p-6 w-full">
        <HeadPage title="Users" description="List of all users."></HeadPage>

        <div v-if="!users" class="text-center">
            <Loading />
        </div>

        <div v-else-if="users.items.length === 0" class="text-center py-12">
            <p class="text-muted-light dark:text-muted-dark">No users found.</p>
        </div>

        <div v-else>
            <div class="flex gap-4 pb-4 sm:pb-6 border-b border-accented">
                <UInput v-model="globalFilter" class="max-w-sm" placeholder="search..." />

                <!-- NOUVEAU : Filtre par statut -->
                <USelect v-model="statusFilter" :options="[
                    { label: 'All', value: 'all' },
                    { label: 'Pending', value: 0 },
                    { label: 'Approved', value: 1 },
                    { label: 'Rejected', value: 2 }
                ]" />
            </div>

            <UTable v-model:global-filter="globalFilter" :data="filteredUsers" :columns="columns" />
        </div>
    </div>
</template>
```

**Composable useAdminUsers** (`app/composables/useAdminUsers.ts`) :

```typescript
export const useAdminUsers = () => {
    const config = useRuntimeConfig();
    const baseURL = config.public.apiBaseUrl;

    const approveUser = async (userId: number) => {
        try {
            await $fetch(`${baseURL}/api/admin/users/${userId}/approve`, {
                method: 'PUT',
                headers: {
                    Authorization: `Bearer ${useCookie('token').value}`
                }
            });

            // Toast de succès
            useToast().add({
                title: 'User approved',
                description: `User ${userId} has been approved successfully`,
                color: 'green'
            });
        } catch (error) {
            console.error('Error approving user:', error);
            useToast().add({
                title: 'Error',
                description: 'Failed to approve user',
                color: 'red'
            });
            throw error;
        }
    };

    const rejectUser = async (userId: number, reason?: string | null) => {
        try {
            await $fetch(`${baseURL}/api/admin/users/${userId}/reject`, {
                method: 'PUT',
                headers: {
                    Authorization: `Bearer ${useCookie('token').value}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ reason })
            });

            // Toast de succès
            useToast().add({
                title: 'User rejected',
                description: `User ${userId} has been rejected`,
                color: 'orange'
            });
        } catch (error) {
            console.error('Error rejecting user:', error);
            useToast().add({
                title: 'Error',
                description: 'Failed to reject user',
                color: 'red'
            });
            throw error;
        }
    };

    return {
        approveUser,
        rejectUser
    };
};
```

### Modèles de Données

#### Migration Base de Données

**Nom** : `AddUserStatusColumn`

**Up** :
```sql
-- Ajouter colonne Status (default 0 = Pending)
ALTER TABLE Users
ADD Status INT NOT NULL DEFAULT 0;

-- Mettre les utilisateurs existants en Approved (1)
-- Car ils étaient déjà actifs avant cette feature
UPDATE Users
SET Status = 1
WHERE Status = 0;
```

**Down** :
```sql
ALTER TABLE Users
DROP COLUMN Status;
```

### Flux de Données

1. **Inscription**
   - Client → POST /api/users
   - Controller → UserService.CreateAsync()
   - UserService crée User avec Status = Pending
   - Repository → INSERT INTO Users (Status = 0)
   - Retour : UserResponseDto

2. **Connexion (Pending)**
   - Client → POST /api/users/login
   - Controller → UserService.LoginAsync()
   - UserService vérifie credentials
   - UserService vérifie Status
   - Si Status = Pending → throw ForbiddenException
   - Retour : HTTP 403 avec message

3. **Liste users pending**
   - Admin Client → GET /api/admin/users/pending
   - AdminController → UserService.GetUsersByStatusAsync(Pending)
   - Repository → SELECT * FROM Users WHERE Status = 0
   - Retour : List<UserResponseDto>

4. **Approbation**
   - Admin Client → PUT /api/admin/users/{id}/approve
   - AdminController → UserService.ApproveUserAsync(id)
   - Repository GetById → User
   - User.Status = Approved
   - Repository Update
   - Retour : HTTP 204

### Dépendances

- **Packages NuGet** : Aucun nouveau package requis
- **Services externes** : Optionnel - Service d'envoi d'email (à implémenter plus tard)
- **Migrations DB** : Une migration pour ajouter colonne Status

---

## 🔒 Sécurité

### Authentification & Autorisation

**Endpoints publics** :
- POST /api/users (inscription)
- POST /api/users/login (connexion)

**Endpoints protégés Admin** :
- GET /api/admin/users/pending
- PUT /api/admin/users/{id}/approve
- PUT /api/admin/users/{id}/reject

**Vérifications** :
- `[Authorize(Roles = "Admin")]` sur tous les endpoints admin
- Vérification status dans LoginAsync() AVANT génération JWT

### Validation des Données

**RejectUserDto** :
- Reason est optionnel
- Si fourni : max 500 caractères

### Audit

**Événements à logger** :
- Inscription nouvelle (Info)
- Tentative connexion compte Pending (Warning)
- Tentative connexion compte Rejected (Warning)
- Approbation par admin (Info) avec ID admin
- Refus par admin (Info) avec ID admin et raison

**Format log** :
```
[Info] User {UserId} registered with Pending status
[Warning] User {UserId} attempted login with Pending status
[Info] Admin {AdminId} approved user {UserId}
[Info] Admin {AdminId} rejected user {UserId}. Reason: {Reason}
```

---

## 🧪 Stratégie de Tests

### Tests Unitaires - UserService

**Fichier** : `Tests/Business/UserServiceTests.cs`

```csharp
[TestClass]
public class UserServiceStatusTests
{
    // Test connexion compte Pending
    [TestMethod]
    public async Task LoginAsync_WhenUserStatusPending_ShouldThrowForbiddenException()
    {
        // Arrange
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = _hashedPassword,
            Status = UserStatus.Pending
        };
        _mockRepository.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ForbiddenException>(
            () => _service.LoginAsync(new UserLoginDto
            {
                Email = "test@test.com",
                Password = "password"
            })
        );

        Assert.IsTrue(exception.Message.Contains("attente"));
    }

    // Test connexion compte Rejected
    [TestMethod]
    public async Task LoginAsync_WhenUserStatusRejected_ShouldThrowForbiddenException()
    {
        // Arrange
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = _hashedPassword,
            Status = UserStatus.Rejected
        };
        _mockRepository.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<ForbiddenException>(
            () => _service.LoginAsync(new UserLoginDto
            {
                Email = "test@test.com",
                Password = "password"
            })
        );

        Assert.IsTrue(exception.Message.Contains("refusé"));
    }

    // Test connexion compte Approved
    [TestMethod]
    public async Task LoginAsync_WhenUserStatusApproved_ShouldReturnToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = _hashedPassword,
            Status = UserStatus.Approved
        };
        _mockRepository.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(user);

        // Act
        var result = await _service.LoginAsync(new UserLoginDto
        {
            Email = "test@test.com",
            Password = "password"
        });

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Token);
    }

    // Test inscription (statut par défaut)
    [TestMethod]
    public async Task CreateAsync_WhenCalled_ShouldSetStatusToPending()
    {
        // Arrange
        var dto = new UserCreateDto
        {
            Email = "new@test.com",
            Password = "password"
        };

        // Act
        await _service.CreateAsync(dto);

        // Assert
        _mockRepository.Verify(r => r.CreateAsync(
            It.Is<User>(u => u.Status == UserStatus.Pending)
        ), Times.Once);
    }

    // Test GetUsersByStatusAsync
    [TestMethod]
    public async Task GetUsersByStatusAsync_WhenCalled_ShouldReturnFilteredUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Status = UserStatus.Pending },
            new() { Id = 2, Status = UserStatus.Pending },
            new() { Id = 3, Status = UserStatus.Approved }
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await _service.GetUsersByStatusAsync(UserStatus.Pending);

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(u => u.Status == UserStatus.Pending));
    }

    // Test ApproveUserAsync
    [TestMethod]
    public async Task ApproveUserAsync_WhenUserExists_ShouldUpdateStatusToApproved()
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

    // Test ApproveUserAsync - user inexistant
    [TestMethod]
    public async Task ApproveUserAsync_WhenUserNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
            () => _service.ApproveUserAsync(999)
        );
    }

    // Test RejectUserAsync
    [TestMethod]
    public async Task RejectUserAsync_WhenUserExists_ShouldUpdateStatusToRejected()
    {
        // Arrange
        var user = new User { Id = 1, Status = UserStatus.Pending };
        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        await _service.RejectUserAsync(1, "Test reason");

        // Assert
        Assert.AreEqual(UserStatus.Rejected, user.Status);
        _mockRepository.Verify(r => r.UpdateAsync(user), Times.Once);
    }
}
```

### Tests d'Intégration - AdminUsersController

**Fichier** : `Tests/Web/AdminUsersControllerTests.cs`

```csharp
[TestClass]
public class AdminUsersControllerTests
{
    // Test GetPendingUsers - success
    [TestMethod]
    public async Task GetPendingUsers_WhenCalled_ShouldReturnPendingUsers()
    {
        // Arrange
        var users = new List<UserResponseDto>
        {
            new() { Id = 1, Email = "user1@test.com", Status = UserStatus.Pending },
            new() { Id = 2, Email = "user2@test.com", Status = UserStatus.Pending }
        };
        _mockUserService.Setup(s => s.GetUsersByStatusAsync(UserStatus.Pending))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetPendingUsers();

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.IsNotNull(okResult);
        var returnedUsers = okResult.Value as IEnumerable<UserResponseDto>;
        Assert.AreEqual(2, returnedUsers.Count());
    }

    // Test ApproveUser - success
    [TestMethod]
    public async Task ApproveUser_WhenUserExists_ShouldReturnNoContent()
    {
        // Arrange
        _mockUserService.Setup(s => s.ApproveUserAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ApproveUser(1);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }

    // Test ApproveUser - user not found
    [TestMethod]
    public async Task ApproveUser_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _mockUserService.Setup(s => s.ApproveUserAsync(999))
            .ThrowsAsync(new KeyNotFoundException());

        // Act
        var result = await _controller.ApproveUser(999);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
    }

    // Test RejectUser - success
    [TestMethod]
    public async Task RejectUser_WhenUserExists_ShouldReturnNoContent()
    {
        // Arrange
        var dto = new RejectUserDto { Reason = "Test" };
        _mockUserService.Setup(s => s.RejectUserAsync(1, "Test"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RejectUser(1, dto);

        // Assert
        Assert.IsInstanceOfType(result, typeof(NoContentResult));
    }
}
```

### Scénarios à Tester

- [x] Inscription crée compte avec statut Pending
- [x] Connexion compte Pending retourne 403
- [x] Connexion compte Rejected retourne 403
- [x] Connexion compte Approved retourne JWT
- [x] GetPendingUsers retourne seulement users Pending
- [x] ApproveUser change statut à Approved
- [x] RejectUser change statut à Rejected
- [x] Endpoints admin requièrent authentification
- [x] Users non-admin ne peuvent pas accéder endpoints admin
- [x] User inexistant retourne 404

### Couverture Cible

- **Minimum** : 80%
- **Objectif** : 90%+
- **Focus** : UserService (100%), AdminUsersController (90%+)

---

## 📦 Plan d'Implémentation

### Étapes Backend

1. [ ] Créer enum UserStatus dans Bo/
2. [ ] Ajouter propriété Status à User entity
3. [ ] Créer DTO RejectUserDto
4. [ ] Créer migration AddUserStatusColumn
5. [ ] Implémenter méthodes dans IUserService
6. [ ] Implémenter méthodes dans UserService
7. [ ] Modifier LoginAsync pour vérifier statut
8. [ ] Créer AdminUsersController
9. [ ] Écrire tests unitaires UserService
10. [ ] Écrire tests intégration AdminUsersController
11. [ ] Exécuter migration
12. [ ] Tester manuellement avec Swagger

### Étapes Frontend

1. [ ] Ajouter UserStatus enum dans types/User.ts
2. [ ] Mettre à jour type UserDto avec propriété status
3. [ ] Modifier page /register pour afficher message après inscription
4. [ ] Modifier page /login pour gérer erreur 403 (status)
5. [ ] Modifier page /admin/data/all-users.vue :
   - Ajouter colonne "Status" avec badge coloré (Pending/Approved/Rejected)
   - Ajouter colonne "Actions" avec boutons Approve/Reject pour users Pending
   - Ajouter filtre pour afficher seulement users Pending (optionnel)
6. [ ] Créer composable useAdminUsers pour actions approve/reject
7. [ ] Tester workflow complet

### Fichiers à Créer/Modifier

#### Backend
- [ ] `Bo/UserStatus.cs` (NOUVEAU)
- [ ] `Bo/User.cs` (MODIFIER - ajouter Status)
- [ ] `Dto/RejectUserDto.cs` (NOUVEAU)
- [ ] `Business/Services/IUserService.cs` (MODIFIER - ajouter méthodes)
- [ ] `Business/Services/UserService.cs` (MODIFIER - implémenter méthodes)
- [ ] `Web/Controllers/AdminUsersController.cs` (NOUVEAU)
- [ ] `Dal/Migrations/YYYYMMDDHHMMSS_AddUserStatusColumn.cs` (GÉNÉRÉ)
- [ ] `Tests/Business/UserServiceStatusTests.cs` (NOUVEAU)
- [ ] `Tests/Web/AdminUsersControllerTests.cs` (NOUVEAU)

#### Frontend
- [ ] `app/types/user.ts` (MODIFIER - ajouter enum UserStatus et propriété status)
- [ ] `app/pages/register.vue` (MODIFIER - message après inscription)
- [ ] `app/pages/login.vue` (MODIFIER - gérer erreur 403)
- [ ] `app/pages/admin/data/all-users.vue` (MODIFIER - ajouter colonnes Status et Actions)
- [ ] `app/composables/useAdminUsers.ts` (NOUVEAU)

### Ordre de Dépendance

1. **Backend d'abord** (car frontend dépend de l'API)
   - Bo (UserStatus) → entité User
   - Dto → Service → Controller
   - Tests
   - Migration

2. **Frontend ensuite**
   - Types (UserStatus enum, UserDto avec status)
   - Composable (useAdminUsers)
   - Pages (all-users.vue, register.vue, login.vue)

---

## 🚀 Déploiement

### Prérequis

- Backend ESN-WebApi en cours d'exécution
- Base de données accessible
- Permissions admin sur la base de données

### Migrations

```bash
# Créer la migration
dotnet ef migrations add AddUserStatusColumn --project Dal --startup-project Web

# Appliquer la migration
dotnet ef database update --project Dal --startup-project Web

# La migration va :
# 1. Ajouter colonne Status INT DEFAULT 0
# 2. Mettre users existants à Status = 1 (Approved)
```

### Configuration

Aucune configuration supplémentaire requise.

### Rollback (si nécessaire)

```bash
# Revenir à la migration précédente
dotnet ef database update <PreviousMigrationName> --project Dal --startup-project Web

# Supprimer la migration
dotnet ef migrations remove --project Dal --startup-project Web
```

---

## 📚 Documentation à Mettre à Jour

- [ ] README.md : Ajouter feature validation comptes
- [ ] doc/API-Endpoints.md : Documenter nouveaux endpoints admin
- [ ] Swagger : Commentaires XML sur nouveaux endpoints (automatique)
- [ ] CLAUDE.md : Déjà mis à jour ✅

---

## ✅ Checklist de Validation

### Avant Implémentation

- [x] Tous les cas d'usage identifiés
- [x] Architecture respecte séparation en couches
- [x] Interfaces claires et complètes
- [x] Sécurité prise en compte
- [x] Stratégie de tests définie
- [ ] **Utilisateur a validé l'approche** ← EN ATTENTE

### Après Implémentation (Backend)

- [ ] Code suit conventions C#
- [ ] Tous les tests passent
- [ ] Couverture ≥ 80%
- [ ] Pas de warnings du compilateur
- [ ] Commentaires XML présents
- [ ] Migration testée
- [ ] Tests manuels avec Swagger OK

### Après Implémentation (Frontend)

- [ ] Code suit conventions Vue/TypeScript
- [ ] Composants fonctionnels
- [ ] Gestion d'erreurs OK
- [ ] UI responsive
- [ ] Tests E2E (optionnel)
- [ ] Workflow complet testé

---

## 🎯 Critères d'Acceptation

### Fonctionnels

- [ ] Nouvel utilisateur ne peut pas se connecter après inscription
- [ ] Message clair affiché expliquant l'attente de validation
- [ ] Admin voit liste des inscriptions en attente
- [ ] Admin peut approuver un compte
- [ ] Admin peut refuser un compte avec raison
- [ ] Utilisateur approuvé peut se connecter
- [ ] Utilisateur refusé ne peut pas se connecter
- [ ] Admin peut modifier le statut d'un compte

### Techniques

- [ ] Endpoints admin protégés par [Authorize(Roles = "Admin")]
- [ ] Statut vérifié AVANT génération JWT
- [ ] Migration DB s'exécute sans erreur
- [ ] Users existants ont statut Approved après migration
- [ ] Logs des actions admin
- [ ] Tests unitaires passent (coverage ≥ 80%)
- [ ] Pas de breaking changes pour users existants

### Non-Fonctionnels

- [ ] Performance : Pas de dégradation notable (<50ms overhead)
- [ ] Sécurité : Pas de bypass possible du check statut
- [ ] UX : Messages clairs et informatifs
- [ ] Maintenabilité : Code propre et documenté

---

## 📊 Suivi

| Date | Statut | Commentaire |
|------|--------|-------------|
| 2026-01-10 | 🟡 En Documentation | Création du document de spec |
| | 🔵 Validé | ← EN ATTENTE validation utilisateur |
| | 🟢 Implémenté | |
| | ✅ Testé | |

---

**Ce document sera le référentiel pour l'implémentation de la feature de validation des comptes utilisateurs.**
