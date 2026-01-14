# Standards de Code - ESN-WebApi

**Date de création** : 2026-01-12
**Dernière mise à jour** : 2026-01-12
**Statut** : ✅ Document de référence

---

## 📋 Objectif

Ce document définit les standards de code **obligatoires** pour le projet ESN-WebApi (backend C# .NET) et ESN-Nuxt (frontend Nuxt.js/Vue). Tous les développements futurs DOIVENT respecter ces standards.

---

## 🎯 Principes Généraux

### Architecture en Couches (Backend)

**Règle STRICTE** : Respecter le flux de dépendances
```
Web → Business → Dal → Bo
        ↓
       Dto
```

**❌ INTERDIT** :
- Logique métier dans les Controllers
- Injection de DbContext dans les Services
- Retourner des entités Bo directement depuis l'API
- Dépendances circulaires entre couches

**✅ OBLIGATOIRE** :
- Controllers appellent uniquement les Services (interfaces)
- Services utilisent IUnitOfWork pour accéder aux repositories
- Toute logique métier dans la couche Business
- Toujours passer par des DTOs pour les Request/Response

---

## 📁 Conventions de Nommage

### Backend (C# .NET)

#### Fichiers

| Type | Format | Exemple |
|------|--------|---------|
| Controllers | `{EntityName}sController.cs` | `EventsController.cs`, `UsersController.cs` |
| Services Interface | `I{EntityName}Service.cs` | `IEventService.cs` |
| Services Impl | `{EntityName}Service.cs` | `EventService.cs` |
| Repositories Interface | `I{EntityName}Repository.cs` | `IEventRepository.cs` |
| Repositories Impl | `{EntityName}Repository.cs` | `EventRepository.cs` |
| DTOs Request | `Create{EntityName}Dto.cs` | `CreateEventDto.cs` |
| DTOs Response | `{EntityName}Dto.cs` | `EventDto.cs` |
| Entités Bo | `{EntityName}Bo.cs` | `EventBo.cs` |
| Tests | `{ClassName}Tests.cs` | `EventServiceTests.cs` |
| Validateurs | `{Dto}Validator.cs` | `CreateEventDtoValidator.cs` |
| Middlewares | `{Purpose}Middleware.cs` | `GlobalExceptionHandler.cs` |

#### Code C#

```csharp
// ✅ Classes : PascalCase
public class EventService { }
public class UserRepository { }

// ✅ Interfaces : I + PascalCase
public interface IEventService { }
public interface IRepository<T> { }

// ✅ Méthodes : PascalCase + Async suffix
public async Task<EventDto> GetByIdAsync(int id) { }
public async Task CreateAsync(EventBo entity) { }

// ✅ Propriétés : PascalCase
public string Title { get; set; }
public int RegisteredCount { get; set; }
public bool IsCurrentUserRegistered { get; set; }

// ✅ Paramètres : camelCase
public async Task CreateEventAsync(CreateEventDto createEventDto, string userEmail) { }

// ✅ Variables privées : camelCase avec underscore
private readonly IUnitOfWork _unitOfWork;
private readonly IMapper _mapper;
private readonly ILogger<EventService> _logger;

// ✅ Variables locales : camelCase
var eventDto = mapper.Map<EventDto>(evt);
var totalCount = await query.CountAsync();

// ✅ Constantes : PascalCase
public const string EsnMember = "esn_member";
public const int MaxCapacity = 100;
```

#### Tests

**Format des méthodes de test** :
```
MethodName_Scenario_ExpectedResult
```

Exemples :
```csharp
[TestMethod]
public async Task GetByIdAsync_WhenEventExists_ShouldReturnEvent() { }

[TestMethod]
public async Task CreateEventAsync_WhenUnauthorized_ShouldThrowException() { }

[TestMethod]
public async Task RegisterForEventAsync_WhenEventFull_ShouldThrowInvalidOperationException() { }
```

### Frontend (Vue/TypeScript)

#### Fichiers

| Type | Format | Exemple |
|------|--------|---------|
| Composants | `ComponentName.vue` | `EventCard.vue`, `LoginForm.vue` |
| Pages | `page-name.vue` ou `[id].vue` | `index.vue`, `events.vue`, `event/[id].vue` |
| Composables | `useFunctionName.ts` | `useAuth.ts`, `useEventApi.ts` |
| Types | `name.ts` | `event.ts`, `user.ts`, `calendar.ts` |
| Layouts | `layoutname.vue` | `default.vue`, `administration.vue` |
| Middleware | `middleware-name.ts` | `auth.ts`, `admin.ts` |

#### Code TypeScript/Vue

```typescript
// ✅ Interfaces/Types : PascalCase avec suffix Dto
export interface EventDto {
    id: number;
    title: string;
}

export interface PagedResult<T> {
    items: T[];
    totalCount: number;
}

// ✅ Composables : camelCase avec prefix 'use'
export const useAuth = () => { }
export const useEventApi = () => { }

// ✅ Fonctions : camelCase
const handleSubmit = async () => { }
const loadEvents = async () => { }
const formatDate = (date: string) => { }

// ✅ Variables : camelCase
const isLoading = ref(false);
const currentUser = ref<UserDto | null>(null);
const eventsList = ref<EventDto[]>([]);

// ✅ Constantes : UPPERCASE ou PascalCase
const MAX_PARTICIPANTS = 100;
const ApiBaseUrl = 'https://localhost:7173/api';
```

---

## 🏗️ Patterns Obligatoires

### Backend

#### 1. Repository Pattern

**Interface générique** :
```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(int id);
}
```

**Implémentation générique** :
```csharp
public class Repository<T>(EsnDevContext context) : IRepository<T> where T : class
{
    protected readonly EsnDevContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    // Implémentation des méthodes
}
```

**Spécialisations** : Hériter de `Repository<T>` pour ajouter des méthodes spécifiques
```csharp
public interface IEventRepository : IRepository<EventBo>
{
    Task<EventBo?> GetEventWithDetailsAsync(int eventId);
    Task<(List<EventBo> Events, int TotalCount)> GetEventsPagedAsync(int skip, int take);
}

public class EventRepository(EsnDevContext context)
    : Repository<EventBo>(context), IEventRepository
{
    // Méthodes spécialisées
}
```

#### 2. Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IEventRepository Events { get; }
    IUserRepository Users { get; }
    ICalendarRepository Calendars { get; }
    // ... autres repositories

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

public class UnitOfWork(EsnDevContext context) : IUnitOfWork
{
    // Lazy initialization avec field initializers (C# 12)
    public IEventRepository Events =>
        field ??= new EventRepository(context);

    public IUserRepository Users =>
        field ??= new UserRepository(context);
}
```

#### 3. Service Pattern

**Interface** :
```csharp
/// <summary>
/// Service de gestion des événements
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Récupère tous les événements avec pagination
    /// </summary>
    /// <param name="pagination">Paramètres de pagination</param>
    /// <param name="userEmail">Email de l'utilisateur authentifié (optionnel)</param>
    /// <returns>Résultat paginé</returns>
    Task<PagedResult<EventDto>> GetAllEventsAsync(PaginationParams pagination, string? userEmail = null);

    Task<EventDto?> GetByIdAsync(int id, string? userEmail = null);
    Task<EventDto> CreateAsync(CreateEventDto dto, string userEmail);
    Task<EventDto?> UpdateAsync(int id, EventDto dto, string userEmail);
    Task<bool> DeleteAsync(int id, string userEmail);
}
```

**Implémentation avec Primary Constructor (C# 12)** :
```csharp
public class EventService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<EventService> logger)
    : IEventService
{
    // Utilisation directe des paramètres (pas de champs privés nécessaires)
    public async Task<EventDto?> GetByIdAsync(int id, string? userEmail = null)
    {
        logger.LogInformation("EventService.GetByIdAsync called for {Id}", id);

        var evt = await unitOfWork.Events.GetEventWithDetailsAsync(id);
        if (evt == null)
        {
            logger.LogWarning("Event {Id} not found", id);
            return null;
        }

        var eventDto = mapper.Map<EventDto>(evt);
        return eventDto;
    }
}
```

#### 4. Dependency Injection

**Program.cs** :
```csharp
// DbContext (Scoped)
builder.Services.AddDbContext<EsnDevContext>(options =>
    options.UseSqlServer(connectionString));

// Unit of Work (Scoped)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services (Scoped)
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IUserService, UserService>();

// AutoMapper (Singleton)
builder.Services.AddAutoMapper(cfg => {
    cfg.AddProfile<MappingProfile>();
});

// FluentValidation (Scoped)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<UserCreateDtoValidator>();
```

**Scopes** :
- `Scoped` : Services, Repositories, UnitOfWork (une instance par requête HTTP)
- `Singleton` : Configuration, Logging, AutoMapper
- `Transient` : Rarement utilisé

#### 5. Controller Pattern

```csharp
[Route("api/[controller]")]
[ApiController]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class EventsController(
    IEventService eventService,
    ILogger<EventsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEvents(
        [FromQuery] PaginationParams pagination)
    {
        logger.LogInformation("GetEvents called - Page {Page}", pagination.PageNumber);

        var userEmail = User.GetUserEmail(); // Nullable
        var events = await eventService.GetAllEventsAsync(pagination, userEmail);

        return Ok(events);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventDto>> PostEvent(CreateEventDto createEventDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var eventDto = await eventService.CreateEventAsync(createEventDto, email);

            logger.LogInformation("Event {Title} created successfully", eventDto.Title);

            return CreatedAtAction(nameof(GetEvent), new { id = eventDto.Id }, eventDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access");
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid argument");
            return BadRequest(ex.Message);
        }
    }
}
```

**Règles Controllers** :
- ✅ Attributs de route et HTTP method
- ✅ `[ProducesResponseType]` pour documentation Swagger
- ✅ Gestion d'erreurs avec try-catch
- ✅ Logging des opérations importantes
- ✅ Validation ModelState
- ✅ Retourner codes HTTP appropriés (200, 201, 400, 401, 404, 500)
- ❌ PAS de logique métier
- ❌ PAS d'accès direct au DbContext

### Frontend (Vue 3 Composition API)

#### 1. Structure Composant

```vue
<template>
    <div class="p-4 sm:p-6">
        <div v-if="isLoading" class="text-center">
            <Loading />
        </div>

        <div v-else-if="events.length === 0" class="text-center py-12">
            <p class="text-muted-light dark:text-muted-dark">No events found.</p>
        </div>

        <div v-else>
            <!-- Contenu -->
        </div>
    </div>
</template>

<script setup lang="ts">
// 1. Imports de types
import type { EventDto } from '~/types/event';

// 2. Définition de metadata (si page)
definePageMeta({
    middleware: 'auth',
    layout: 'default'
});

// 3. Injection de composables
const { getAllEvents } = useEventApi();
const { formatDate } = useFormatDate();
const toast = useToast();
const router = useRouter();

// 4. État local
const events = ref<EventDto[]>([]);
const isLoading = ref(false);

// 5. Computed properties
const filteredEvents = computed(() =>
    events.value.filter(e => e.isActive)
);

// 6. Fonctions
const loadEvents = async () => {
    isLoading.value = true;
    try {
        events.value = await getAllEvents();
    } catch (error) {
        console.error('Failed to load events:', error);
        toast.add({
            title: 'Error',
            description: 'Failed to load events',
            color: 'error'
        });
    } finally {
        isLoading.value = false;
    }
};

// 7. Lifecycle hooks
onMounted(async () => {
    await loadEvents();
});
</script>

<style scoped>
/* Styles spécifiques au composant */
</style>
```

#### 2. Composable Pattern

```typescript
// composables/useAuth.ts
export const useAuth = () => {
    // État global partagé
    const token = useState<string | null>('token', () => null);

    // Fonctions
    const login = async (email: string, password: string) => {
        const response = await useApi().post('/users/login', { email, password });
        token.value = response.token;
        localStorage.setItem('token', response.token);
        return response;
    };

    const logout = () => {
        token.value = null;
        localStorage.removeItem('token');
        navigateTo('/login');
    };

    const whoAmI = (): string | null => {
        if (!import.meta.client) return null;
        const token = localStorage.getItem('token');
        if (!token) return null;

        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            return payload.studentType;
        } catch {
            return null;
        }
    };

    // Retour des fonctions publiques
    return {
        token,
        login,
        logout,
        whoAmI
    };
};
```

#### 3. API Communication

```typescript
// composables/useEventApi.ts
export const useEventApi = () => {
    const api = useApi();

    const getAllEvents = async (pagination?: PaginationParams): Promise<PagedResult<EventDto>> => {
        const params = pagination
            ? `?pageNumber=${pagination.pageNumber}&pageSize=${pagination.pageSize}`
            : '?pageNumber=1&pageSize=10';
        return await api.get(`/Events${params}`) as PagedResult<EventDto>;
    };

    const getEventById = async (id: number): Promise<EventDto> => {
        return await api.get(`/Events/${id}`) as EventDto;
    };

    const createEvent = async (data: CreateEventDto): Promise<EventDto> => {
        return await api.post('/Events', data) as EventDto;
    };

    return {
        getAllEvents,
        getEventById,
        createEvent
    };
};
```

---

## 📝 Documentation

### Backend (XML Comments)

**OBLIGATOIRE** sur toutes les classes et méthodes publiques :

```csharp
/// <summary>
/// Service de gestion des événements avec inscriptions
/// </summary>
public class EventService : IEventService
{
    /// <summary>
    /// Récupère un événement par son identifiant
    /// </summary>
    /// <param name="id">Identifiant unique de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur authentifié (optionnel, pour calculer isCurrentUserRegistered)</param>
    /// <returns>Événement complet ou null si non trouvé</returns>
    /// <remarks>
    /// Inclut toutes les informations : titre, description, lieu, dates, capacité, formulaire SurveyJS
    /// Si userEmail fourni, EventDto aura IsCurrentUserRegistered renseigné
    /// </remarks>
    public async Task<EventDto?> GetByIdAsync(int id, string? userEmail = null)
    {
        // Implémentation
    }
}
```

### Frontend (JSDoc)

**Recommandé** pour composables et fonctions complexes :

```typescript
/**
 * Composable pour gérer l'authentification JWT
 * @returns Méthodes et état d'authentification
 */
export const useAuth = () => {
    /**
     * Connecte un utilisateur avec email et password
     * @param email - Email de l'utilisateur
     * @param password - Mot de passe
     * @returns Token JWT et informations utilisateur
     * @throws {Error} Si les credentials sont invalides
     */
    const login = async (email: string, password: string) => {
        // Implémentation
    };

    return { login };
};
```

---

## ✅ Validation

### Backend (FluentValidation)

```csharp
public class CreateEventDtoValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Le titre est requis")
            .MaximumLength(255).WithMessage("Maximum 255 caractères");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("La date de début est requise");

        RuleFor(x => x.EndDate)
            .Must((dto, endDate) => !endDate.HasValue || endDate.Value >= dto.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("La date de fin doit être >= date de début");

        RuleFor(x => x.SurveyJsData)
            .Must(BeValidJson)
            .When(x => !string.IsNullOrEmpty(x.SurveyJsData))
            .WithMessage("Les données doivent être au format JSON valide");
    }

    private bool BeValidJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return true;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### Frontend (UForm)

```vue
<script setup lang="ts">
import type { FormError } from '@nuxt/ui';

const loginState = reactive({
    email: undefined,
    password: undefined
});

const validateLogin = (state: any): FormError[] => {
    const errors: FormError[] = [];

    if (!state.email) {
        errors.push({ path: 'email', message: 'Email is required' });
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(state.email)) {
        errors.push({ path: 'email', message: 'Invalid email format' });
    }

    if (!state.password) {
        errors.push({ path: 'password', message: 'Password is required' });
    } else if (state.password.length < 6) {
        errors.push({ path: 'password', message: 'Minimum 6 characters' });
    }

    return errors;
};
</script>

<template>
    <UForm :state="loginState" :validate="validateLogin" @submit="onLogin">
        <UFormField name="email" label="Email">
            <UInput v-model="loginState.email" type="email" />
        </UFormField>

        <UFormField name="password" label="Password">
            <UInput v-model="loginState.password" type="password" />
        </UFormField>

        <UButton type="submit">Login</UButton>
    </UForm>
</template>
```

---

## 🔒 Sécurité

### Validation Obligatoire

✅ **TOUJOURS valider côté serveur** (jamais se fier au client)
✅ **TOUJOURS utiliser paramètres EF Core** (pas de SQL brut)
✅ **TOUJOURS vérifier les autorisations** (rôle, ownership)
✅ **TOUJOURS logger les tentatives d'accès non autorisées**
❌ **JAMAIS** exposer les détails d'erreur en production
❌ **JAMAIS** logger les mots de passe ou données sensibles

### Authentification JWT

```csharp
// Backend : Génération token
private string GenerateJwtToken(UserBo user)
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Email),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new("userId", user.Id.ToString()),
        new("name", $"{user.FirstName} {user.LastName}"),
        new("studentType", user.StudentType)
    };

    if (user.Role != null)
    {
        claims.Add(new Claim(ClaimTypes.Role, user.Role.Name));
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig["SecretKey"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _jwtConfig["Issuer"],
        audience: _jwtConfig["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(30),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

// Frontend : Extraction payload
const getUserId = (): number | null => {
    if (!import.meta.client) return null;

    const token = localStorage.getItem('token');
    if (!token) return null;

    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload.userId ? parseInt(payload.userId) : null;
    } catch {
        return null;
    }
};
```

---

## 🧪 Logging

### Backend (Serilog)

```csharp
// Logs structurés avec placeholders
logger.LogInformation("EventService.CreateEventAsync called with Title {Title} by {Email}",
    createEventDto.Title, userEmail);

logger.LogWarning("EventService.CreateEventAsync - Calendar {CalendarId} already linked",
    calendar.Id);

logger.LogError("EventService.CreateEventAsync failed - user not found for {Email}", userEmail);

// ❌ INTERDIT : Log de données sensibles
logger.LogInformation("User logged in with password {Password}", password); // NON!

// ✅ CORRECT
logger.LogInformation("User {Email} logged in successfully", email);
```

### Frontend (Console)

```typescript
// Development uniquement
if (process.env.NODE_ENV === 'development') {
    console.log('Event loaded:', event);
}

// Erreurs à logger
console.error('Failed to load event:', error);

// ❌ INTERDIT en production
console.log('User password:', password);
```

---

## ⚡ Performance

### Backend

```csharp
// ✅ AsNoTracking pour lectures seules
var events = await _dbSet
    .AsNoTracking()
    .Include(e => e.User)
    .ToListAsync();

// ✅ Pagination TOUJOURS
var query = _dbSet.OrderByDescending(e => e.CreatedAt);
var totalCount = await query.CountAsync();
var events = await query
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// ✅ Eviter N+1 queries avec Include
var events = await _dbSet
    .Include(e => e.User)
    .Include(e => e.EventRegistrations)
    .ToListAsync();

// ❌ INTERDIT : Charger toutes les données sans pagination
var allEvents = await _dbSet.ToListAsync(); // Potentiel OutOfMemoryException
```

### Frontend

```typescript
// ✅ Lazy loading des pages
const EventDetailsPage = defineAsyncComponent(() =>
    import('./pages/event/[id].vue')
);

// ✅ Debounce des recherches
const searchQuery = ref('');
const debouncedSearch = useDebounceFn(() => {
    loadEvents(searchQuery.value);
}, 300);

watch(searchQuery, () => debouncedSearch());
```

---

## 🎨 Frontend Styling

### Tailwind CSS Classes

```vue
<template>
    <!-- Mobile-first responsive -->
    <div class="p-4 sm:p-6">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-6">
            <!-- Cards -->
            <div class="rounded-lg bg-card-light dark:bg-card-dark p-4 shadow-lg">
                <h3 class="text-lg font-semibold mb-2">Title</h3>
                <p class="text-muted-light dark:text-muted-dark">Content</p>
            </div>
        </div>
    </div>
</template>
```

**Classes standardisées** :
- Spacing : `p-4 sm:p-6`, `gap-4 sm:gap-6`, `mb-4 sm:mb-6`
- Cards : `rounded-lg bg-card-light dark:bg-card-dark shadow-lg`
- Text : `text-muted-light dark:text-muted-dark`
- Buttons : Utiliser `<UButton>` de Nuxt UI

---

## ✅ Checklist

### Avant de Committer

- [ ] Code compile sans warnings
- [ ] Tests passent (coverage ≥ 80%)
- [ ] Commentaires XML sur publics (backend)
- [ ] Pas de `console.log` non nécessaires (frontend)
- [ ] Validation côté serveur en place
- [ ] Gestion d'erreurs appropriée
- [ ] Logging des opérations importantes
- [ ] Respect des conventions de nommage
- [ ] Pas de dépendances circulaires
- [ ] Code respecte l'architecture en couches

---

**Ce document est la référence pour tous les futurs développements.**
**En cas de doute, consulter ce document.**
