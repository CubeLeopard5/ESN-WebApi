# [Nom de la Feature/Changement]

**Date** : YYYY-MM-DD
**Auteur** : Claude + [Nom de l'utilisateur]
**Type** : [Feature/Bugfix/Refactor/Enhancement]
**Statut** : 🟡 En Documentation → 🔵 Validé → 🟢 Implémenté → ✅ Testé

---

## 📋 Contexte et Objectif

### Problème/Besoin
[Décrire le problème à résoudre ou le besoin identifié]

### Objectif
[Objectif clair et mesurable]

### Impact
- **Utilisateurs affectés** : [Qui sera impacté par ce changement]
- **Modules impactés** : [Quels modules du projet : Web, Business, Dal, Dto, Bo]
- **Breaking changes** : [Oui/Non - Si oui, détailler]

---

## 🎯 Spécifications Fonctionnelles

### User Stories / Cas d'Usage
1. En tant que [rôle], je veux [action] afin de [bénéfice]
2. ...

### Règles Métier
- [Règle métier 1]
- [Règle métier 2]

### Comportement Attendu
[Description détaillée du comportement souhaité]

### Cas Limites
- [Cas limite 1 : ex. valeur null]
- [Cas limite 2 : ex. valeur maximale]
- [Cas limite 3 : ex. concurrence]

---

## 🏗️ Conception Technique

### Architecture

#### Couches Impactées
- [ ] **Web** : [Détails si applicable - contrôleurs, endpoints]
- [ ] **Business** : [Détails si applicable - services, logique métier]
- [ ] **Dal** : [Détails si applicable - repositories, requêtes]
- [ ] **Dto** : [Détails si applicable - modèles request/response]
- [ ] **Bo** : [Détails si applicable - entités domaine]

#### Diagramme de Flux
```
[Diagramme ASCII ou description de l'architecture]

Exemple :
┌─────────┐     ┌─────────┐     ┌────────────┐     ┌──────────┐
│ Client  │────▶│ API     │────▶│  Service   │────▶│   Repo   │
│         │◀────│Controller│◀────│  (Business)│◀────│  (Dal)   │
└─────────┘     └─────────┘     └────────────┘     └──────────┘
```

### Interfaces Publiques

#### API Endpoints (si Web)
```csharp
/// <summary>
/// [Description de l'endpoint]
/// </summary>
/// <param name="param">[Description]</param>
/// <returns>[Description de la réponse]</returns>
[HttpGet("api/resource/{id}")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<ResponseDto>> MethodName(int id)
{
    // Implementation
}
```

#### Services (si Business)
```csharp
/// <summary>
/// [Description du service]
/// </summary>
public interface IServiceName
{
    /// <summary>
    /// [Description de la méthode]
    /// </summary>
    /// <param name="param">[Description]</param>
    /// <returns>[Description du retour]</returns>
    Task<Result<TData>> MethodName(TParam param);
}
```

#### Repositories (si Dal)
```csharp
public interface IRepositoryName : IRepository<Entity>
{
    Task<Entity?> GetByIdAsync(int id);
    Task<IEnumerable<Entity>> GetAllAsync();
    // ...
}
```

### Modèles de Données

#### Entités (Bo)
```csharp
/// <summary>
/// [Description de l'entité]
/// </summary>
public class EntityName
{
    public int Id { get; set; }
    // [Liste des propriétés avec types et descriptions]
}
```

#### DTOs
```csharp
/// <summary>
/// Request DTO pour [opération]
/// </summary>
public class EntityRequestDto
{
    // [Propriétés de requête]
}

/// <summary>
/// Response DTO pour [opération]
/// </summary>
public class EntityResponseDto
{
    // [Propriétés de réponse]
}
```

#### Validation
- [Règle de validation 1 : ex. [Required] sur propriété X]
- [Règle de validation 2 : ex. [MaxLength(100)] sur propriété Y]
- [Règle de validation 3 : ex. validation personnalisée si...]

### Flux de Données
1. [Étape 1 : Controller reçoit la requête]
2. [Étape 2 : Validation des données d'entrée]
3. [Étape 3 : Service traite la logique métier]
4. [Étape 4 : Repository accède à la base de données]
5. [Étape 5 : Mapping des résultats en DTO]
6. [Étape 6 : Retour de la réponse]

### Dépendances
- **Packages NuGet** : [Si nouveaux packages sont requis]
- **Services externes** : [Si intégration avec API externe]
- **Migrations DB** : [Si changements dans la structure de base de données]

---

## 🔒 Sécurité

### Authentification & Autorisation
- **Rôles requis** : [Admin, User, etc.]
- **Claims nécessaires** : [Si applicable]
- **Endpoints publics** : [Oui/Non - Si oui, justification]

### Validation des Données
- **Validation côté serveur** : [Attributs de validation utilisés]
- **Sanitization** : [Comment les entrées sont nettoyées]
- **Limites** : [Taille max, longueur, etc.]

### Protection Contre les Vulnérabilités
- [ ] Injection SQL : [Protection via EF Core paramétré]
- [ ] XSS : [Validation et encodage]
- [ ] CSRF : [Tokens anti-CSRF si nécessaire]
- [ ] Exposition de données : [Pas de données sensibles dans les logs/réponses]

### Audit et Logging
- [Quelles opérations sont loggées]
- [Niveau de log : Information, Warning, Error]
- [Données sensibles exclues des logs]

---

## 🧪 Stratégie de Tests

### Tests Unitaires

#### Services (Business)
```csharp
[TestClass]
public class ServiceNameTests
{
    [TestMethod]
    public async Task MethodName_WhenCondition_ShouldExpectedBehavior()
    {
        // Arrange
        // [Setup des mocks et données de test]

        // Act
        // [Exécution de la méthode à tester]

        // Assert
        // [Vérifications des résultats]
    }
}
```

#### Repositories (Dal)
```csharp
[TestClass]
public class RepositoryNameTests
{
    [TestMethod]
    public async Task GetById_WhenEntityExists_ShouldReturnEntity()
    {
        // Arrange (avec InMemory database)
        // Act
        // Assert
    }
}
```

### Scénarios à Tester

#### Cas Nominaux (Happy Path)
- [ ] [Scénario 1 : opération réussie avec données valides]
- [ ] [Scénario 2 : ...]

#### Cas d'Erreur
- [ ] [Validation échoue : données invalides]
- [ ] [Entité non trouvée]
- [ ] [Permissions insuffisantes]

#### Cas Limites
- [ ] [Valeur null]
- [ ] [String vide]
- [ ] [Valeurs limites (min/max)]
- [ ] [Concurrence (si applicable)]

### Couverture Cible
- **Minimum** : 80%
- **Objectif** : 90%+
- **Focus** : Business et Dal à 100%, Web à 80%+

---

## 📦 Plan d'Implémentation

### Étapes d'Implémentation
1. [ ] [Étape 1 : Créer les entités (Bo)]
2. [ ] [Étape 2 : Créer les DTOs]
3. [ ] [Étape 3 : Créer les interfaces]
4. [ ] [Étape 4 : Implémenter le repository]
5. [ ] [Étape 5 : Implémenter le service]
6. [ ] [Étape 6 : Créer le controller]
7. [ ] [Étape 7 : Migration DB si nécessaire]
8. [ ] [Étape 8 : Écrire les tests]
9. [ ] [Étape 9 : Exécuter tests et vérifier coverage]
10. [ ] [Étape 10 : Documentation API (commentaires XML)]

### Fichiers à Créer/Modifier

#### Nouveau Fichiers
- [ ] `Bo/[EntityName].cs`
- [ ] `Dto/[EntityName]Dto.cs`
- [ ] `Dal/Repositories/I[EntityName]Repository.cs`
- [ ] `Dal/Repositories/[EntityName]Repository.cs`
- [ ] `Business/Services/I[ServiceName].cs`
- [ ] `Business/Services/[ServiceName].cs`
- [ ] `Web/Controllers/[ControllerName].cs`
- [ ] `Tests/Business/[ServiceName]Tests.cs`
- [ ] `Tests/Dal/[RepositoryName]Tests.cs`
- [ ] `Tests/Web/[ControllerName]Tests.cs`

#### Fichiers Modifiés
- [ ] `Dal/ApplicationDbContext.cs` (si nouvelle entité)
- [ ] `Web/Program.cs` (si nouvelle injection de dépendance)

### Ordre de Dépendance
[Indiquer l'ordre si certaines parties dépendent d'autres]
Exemple :
1. Bo (entités) → pas de dépendances
2. Dto → peut dépendre de Bo
3. Dal interfaces → dépend de Bo
4. Dal implémentation → dépend de Dal interfaces
5. Business interfaces → dépend de Bo et Dto
6. Business implémentation → dépend de Business interfaces et Dal
7. Web → dépend de tout ce qui précède

---

## 🚀 Déploiement

### Prérequis
- [Prérequis 1 : ex. .NET 10 SDK]
- [Prérequis 2 : ex. SQL Server]
- [Prérequis 3 : ...]

### Migrations de Base de Données
```bash
# Si changements dans le modèle de données
dotnet ef migrations add [MigrationName] --project Dal --startup-project Web
dotnet ef database update --project Dal --startup-project Web
```

### Configuration
- [Paramètres appsettings.json à ajouter]
- [Variables d'environnement nécessaires]
- [Permissions à configurer]

### Ordre de Déploiement
1. [Déployer les migrations DB]
2. [Déployer l'application]
3. [Vérifier les logs]

---

## 📚 Documentation à Mettre à Jour

- [ ] Commentaires XML sur toutes les classes et méthodes publiques
- [ ] README.md si nouveaux endpoints ou fonctionnalités
- [ ] Documentation API (Swagger sera auto-généré)
- [ ] Guide utilisateur si impact sur l'utilisation
- [ ] CHANGELOG.md avec les changements notables

---

## ✅ Checklist de Validation

### Avant Implémentation
- [ ] Tous les cas d'usage sont identifiés et documentés
- [ ] L'architecture respecte la séparation en couches
- [ ] Les interfaces sont claires et complètes
- [ ] La sécurité est prise en compte dans la conception
- [ ] La stratégie de tests est définie
- [ ] L'utilisateur a validé l'approche

### Après Implémentation
- [ ] Le code suit les conventions C#
- [ ] Tous les tests passent
- [ ] La couverture est ≥ 80%
- [ ] Pas de warnings du compilateur
- [ ] Les commentaires XML sont présents
- [ ] Code review effectué
- [ ] Documentation mise à jour

---

## 🎯 Critères d'Acceptation

### Fonctionnels
- [ ] [Critère 1 : La fonctionnalité X fonctionne comme attendu]
- [ ] [Critère 2 : Les règles métier sont respectées]
- [ ] [Critère 3 : Les cas limites sont gérés]

### Techniques
- [ ] Code respecte les conventions C# et SOLID
- [ ] Tests unitaires et d'intégration écrits et passent
- [ ] Couverture de code ≥ 80%
- [ ] Pas de warnings du compilateur
- [ ] Code review approuvé
- [ ] Performance acceptable

### Non-Fonctionnels
- [ ] Performance : Temps de réponse < [X]ms pour [Y]% des requêtes
- [ ] Sécurité : Pas de vulnérabilités OWASP Top 10
- [ ] Maintenabilité : Code lisible et bien structuré
- [ ] Logs : Opérations importantes loggées
- [ ] Documentation : Commentaires XML complets

---

## 📝 Notes et Décisions

### Décisions de Conception
[Documenter les décisions importantes et leur justification]

### Alternatives Considérées
[Lister les alternatives étudiées et pourquoi elles n'ont pas été choisies]

### Points d'Attention
[Points nécessitant une attention particulière lors de l'implémentation]

### Questions Ouvertes
[Questions restant à clarifier]

---

## 📊 Suivi

| Date | Statut | Commentaire |
|------|--------|-------------|
| [Date] | 🟡 En Documentation | Création du document de spec |
| [Date] | 🔵 Validé | Approuvé par [Nom] |
| [Date] | 🟢 Implémenté | Implémentation terminée |
| [Date] | ✅ Testé | Tests passent, coverage OK |

---

**Ce template est généré par le skill `/doc-first`**
**Ne pas supprimer les sections, adapter le contenu selon les besoins**
