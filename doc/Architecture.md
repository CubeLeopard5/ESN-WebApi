# Architecture du Projet ESN-WebApi

## Vue d'ensemble

ESN-WebApi est une API RESTful ASP.NET Core 9.0 structurée selon une **architecture en couches** (Layered Architecture) avec une séparation claire des responsabilités.

## Structure des projets

### Organisation modulaire

```
ESN-WebApi/
├── Web/                    # Couche Présentation
├── Business/               # Couche Logique Métier
├── Dal/                    # Couche Accès aux Données
├── Bo/                     # Business Objects (Entités)
├── Dto/                    # Data Transfer Objects
└── Tests/                  # Tests Unitaires et d'Intégration
```

### Responsabilités par couche

#### Web - Couche Présentation
**Rôle:** Point d'entrée de l'application, gestion des requêtes HTTP

**Contenu:**
- **Controllers:** Endpoints API REST
  - `UsersController` - Gestion des utilisateurs
  - `EventsController` - Gestion des événements
  - `CalendarsController` - Gestion des calendriers
  - `PropositionsController` - Gestion des propositions

- **Middlewares:** Traitement transversal des requêtes
  - `GlobalExceptionHandler` - Gestion centralisée des erreurs
  - `SecurityHeadersMiddleware` - Headers de sécurité HTTP
  - `RequestLoggingMiddleware` - Journalisation des requêtes
  - `ValidateIdMiddleware` - Validation des paramètres de route

- **Validators:** Validation des données entrantes
  - FluentValidation pour tous les DTOs de création
  - Validation du format JSON pour SurveyJsData

- **Extensions:** Méthodes d'extension utilitaires
  - `ClaimsPrincipalExtensions` - Extraction de l'email utilisateur

#### Business - Couche Logique Métier
**Rôle:** Implémentation des règles métier et orchestration

**Contenu:**
- **Services:** Implémentation de la logique métier
  - `UserService` - Authentification JWT, gestion utilisateurs
  - `EventService` - CRUD événements, inscriptions
  - `CalendarService` - Gestion calendriers et organisateurs
  - `PropositionService` - Propositions et système de vote
  - `EventTemplateService` - Templates d'événements réutilisables

- **Interfaces:** Contrats de services
  - Une interface par service pour l'injection de dépendances
  - Facilite les tests unitaires (mocking)

**Responsabilités:**
- Validation des règles métier
- Orchestration des opérations complexes
- Gestion des transactions
- Mapping Bo ↔ Dto (via AutoMapper)
- Vérification des autorisations (ownership)

#### Dal - Couche Accès aux Données
**Rôle:** Abstraction de l'accès à la base de données

**Contenu:**
- **Repositories:** Implémentation du pattern Repository
  - `Repository<T>` - Repository générique
  - Repositories spécialisés pour chaque entité
  - Méthodes optimisées (Include, pagination)

- **UnitOfWork:** Implémentation du pattern Unit of Work
  - Coordination des repositories
  - Gestion transactionnelle
  - `SaveChangesAsync()` unique

- **Migrations:** Historique des évolutions de schéma
  - Entity Framework Core Migrations
  - Versioning automatique de la base

- **Specifications:** Pattern Specification
  - Encapsulation de la logique de requêtage
  - Réutilisabilité des filtres complexes

**Technologies:**
- Entity Framework Core 9.0
- SQL Server
- Requêtes LINQ

#### Bo - Business Objects
**Rôle:** Modèles de domaine représentant les entités métier

**Contenu:**
- Entités mappées en base de données
- Annotations Data Annotations pour validation
- Relations entre entités (Navigation Properties)
- Pas de logique métier (objets anémiques)

**Entités principales:**
- `UserBo` - Utilisateurs
- `EventBo` - Événements
- `CalendarBo` - Calendriers
- `PropositionBo` - Propositions
- `EventRegistrationBo` - Inscriptions
- `EventTemplateBo` - Templates
- `RoleBo` - Rôles et permissions
- `PropositionVoteBo` - Votes
- `CalendarSubOrganizerBo` - Sous-organisateurs

#### Dto - Data Transfer Objects
**Rôle:** Objets pour le transfert de données entre couches

**Contenu:**
- DTOs de lecture (ex: `UserDto`)
- DTOs de création (ex: `UserCreateDto`)
- DTOs de mise à jour (ex: `UserUpdateDto`)
- DTOs de réponse (ex: `UserLoginResponseDto`)
- DTOs communs (Pagination, Error)

**Avantages:**
- Séparation entre modèle interne et API publique
- Contrôle précis des données exposées
- Évite l'over-posting
- Facilite le versioning de l'API

#### Tests
**Rôle:** Assurer la qualité et la non-régression

**Contenu:**
- Tests unitaires des services
- Tests unitaires des repositories
- Tests d'intégration des contrôleurs
- Tests des middlewares
- Tests des specifications

**Framework:** MSTest

## Patterns architecturaux

### 1. Repository Pattern
**Objectif:** Abstraction de l'accès aux données

**Implémentation:**
- Interface générique `IRepository<T>`
- Classe de base `Repository<T>`
- Repositories spécialisés héritant de `Repository<T>`
- Méthodes génériques + méthodes métier spécifiques

**Avantages:**
- Testabilité (mocking facile)
- Changement de source de données simplifié
- Centralisation de la logique d'accès

### 2. Unit of Work Pattern
**Objectif:** Coordination des modifications transactionnelles

**Implémentation:**
- Interface `IUnitOfWork`
- Classe `UnitOfWork` gérant tous les repositories
- `SaveChangesAsync()` unique pour atomicité
- Support transactions explicites (Begin/Commit/Rollback)

**Avantages:**
- Garantie de cohérence transactionnelle
- Gestion simplifiée des dépendances entre repositories
- Support de scénarios complexes multi-entités

### 3. Dependency Injection
**Objectif:** Inversion de contrôle et couplage faible

**Configuration:** `Program.cs`
- Services: Scoped
- Repositories: Scoped (via UnitOfWork)
- DbContext: Scoped
- AutoMapper: Singleton
- Validators: Scoped

**Avantages:**
- Testabilité maximale
- Flexibilité de configuration
- Gestion automatique du cycle de vie

### 4. Specification Pattern
**Objectif:** Encapsulation de la logique de requêtage

**Implémentation:**
- Classe `Specification<T>`
- `SpecificationEvaluator<T>` pour application
- Specifications métier (ex: `EventSpecifications`)

**Avantages:**
- Réutilisabilité des filtres
- Composition de critères complexes
- Séparation logique de requêtage

## Flux de traitement d'une requête

### Requête typique (exemple: GET /api/events/{id})

```
1. Réception HTTP
   └─> Kestrel Server (ASP.NET Core)

2. Pipeline de Middlewares
   ├─> SecurityHeadersMiddleware (ajout headers sécurité)
   ├─> UseAuthentication() (validation token JWT)
   ├─> RequestLoggingMiddleware (log requête)
   ├─> ValidateIdMiddleware (validation paramètre id)
   ├─> UseAuthorization() (vérification [Authorize])
   └─> UseExceptionHandler() (capture erreurs)

3. Routage
   └─> EventsController.GetEvent(int id)

4. Validation
   └─> ModelState validation automatique

5. Couche Service
   ├─> EventService.GetEventByIdAsync(id)
   └─> Logique métier, vérifications

6. Couche Repository
   ├─> UnitOfWork.Events.GetEventWithDetailsAsync(id)
   └─> Requête EF Core + Includes

7. Base de données
   └─> Exécution SQL Server

8. Mapping
   └─> AutoMapper: EventBo → EventDto

9. Réponse
   ├─> return Ok(eventDto)
   └─> Sérialisation JSON

10. Logging
    └─> RequestLoggingMiddleware (log réponse)
```

### Gestion des erreurs

Toute exception levée est capturée par `GlobalExceptionHandler` :
- Mapping exception → code HTTP approprié
- Création d'un `ErrorResponse` standardisé
- Logging de l'erreur avec stack trace
- Masquage des détails sensibles en production

## Principes de conception

### SOLID

**S - Single Responsibility**
- Chaque service a une responsabilité unique
- Séparation stricte des couches

**O - Open/Closed**
- Extensibilité via interfaces
- Pattern Specification pour requêtes

**L - Liskov Substitution**
- Utilisation d'interfaces partout
- Repository générique substituable

**I - Interface Segregation**
- Interfaces spécifiques par service
- Pas d'interfaces monolithiques

**D - Dependency Inversion**
- Dépendance vers abstractions (interfaces)
- Injection de dépendances généralisée

### DRY (Don't Repeat Yourself)
- Repository générique pour CRUD standard
- Méthodes privées partagées dans services
- AutoMapper pour éliminer mapping manuel
- Middlewares pour logique transversale

### Separation of Concerns
- Couches indépendantes
- Controllers sans logique métier
- Services sans connaissance HTTP

### Convention over Configuration
- Structure de projet standardisée
- Nommage cohérent (suffixes: Controller, Service, Repository, Dto, Bo)
- Configuration centralisée dans `Program.cs`

## Technologies et frameworks

### Backend
- **ASP.NET Core 9.0** - Framework web
- **Entity Framework Core 9.0** - ORM
- **SQL Server** - Base de données

### Sécurité
- **JWT Bearer Authentication** - Authentification
- **ASP.NET Core Identity** - Hashage mots de passe
- **Rate Limiting** - Protection DoS

### Outils
- **AutoMapper** - Mapping objet-objet
- **FluentValidation** - Validation déclarative
- **Serilog** - Logging structuré
- **Swagger/OpenAPI** - Documentation API

### Tests
- **MSTest** - Framework de tests
- **Moq** - Mocking

## Scalabilité et performance

### Optimisations implémentées

**1. Pagination systématique**
- Toutes les listes paginées
- Évite le chargement de milliers d'enregistrements
- DTO `PagedResult<T>` standardisé

**2. Eager Loading stratégique**
- `.Include()` pour prévenir N+1
- Méthodes dédiées "WithDetails" dans repositories
- Projections optimisées

**3. Requêtes asynchrones**
- Toutes les opérations I/O async
- Libération des threads pendant attente DB
- Meilleure scalabilité verticale

**4. Rate Limiting**
- Protection contre abus
- Préservation des ressources serveur
- Politiques par endpoint

**5. Transactions explicites**
- Uniquement pour opérations critiques
- Minimisation de la durée des locks
- Rollback automatique en cas d'erreur

### Points d'amélioration potentiels

**Caching**
- Implémenter cache distribué (Redis)
- Cacher les données rarement modifiées
- Invalidation intelligente

**Background Jobs**
- Traitement asynchrone (Hangfire)
- Nettoyage automatique
- Envoi d'emails différé

**CQRS**
- Séparation lecture/écriture
- Optimisation des requêtes complexes
- Event Sourcing pour audit

**API Gateway**
- Point d'entrée unique
- Load balancing
- Rate limiting global

## Sécurité architecture

### Défense en profondeur (Defense in Depth)

**Couche 1: Réseau**
- HTTPS obligatoire (HSTS)
- CORS restreint

**Couche 2: Application**
- Rate Limiting
- Validation taille requêtes (10 MB max)
- Headers de sécurité

**Couche 3: Authentification**
- JWT avec expiration courte
- Refresh tokens avec limite de validité
- Protection timing attacks

**Couche 4: Autorisation**
- Role-based access control
- Ownership verification
- Permissions granulaires

**Couche 5: Données**
- Hashage mots de passe (PBKDF2)
- Validation FluentValidation
- Paramétrage requêtes (EF Core prévient SQL injection)

**Couche 6: Logging & Monitoring**
- Logging structuré de toutes les actions
- Masquage données sensibles
- Health checks

### Principe du moindre privilège

- Utilisateurs: Accès limité à leurs ressources
- Services: Accès DB via compte dédié
- Roles: Permissions minimales nécessaires

## Conclusion

L'architecture d'ESN-WebApi est **robuste, maintenable et évolutive**, suivant les meilleures pratiques de l'industrie. La séparation en couches garantit la testabilité et facilite les évolutions futures.
