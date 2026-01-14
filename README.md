# ESN-WebApi

API REST pour la gestion des événements et activités de l'association ESN (Erasmus Student Network).

## 📋 Table des Matières

- [À propos](#à-propos)
- [Fonctionnalités](#fonctionnalités)
- [Technologies](#technologies)
- [Démarrage Rapide](#démarrage-rapide)
- [Documentation](#documentation)
- [Architecture](#architecture)
- [Sécurité](#sécurité)
- [Tests](#tests)
- [Contribution](#contribution)

---

## À propos

ESN-WebApi est une API RESTful ASP.NET Core permettant de gérer:
- **Utilisateurs** avec authentification JWT et gestion des rôles
- **Événements** avec inscriptions et capacité maximale
- **Calendriers** avec organisateurs multiples
- **Propositions** d'activités avec système de vote
- **Templates** d'événements réutilisables

---

## Fonctionnalités

### 🔐 Authentification & Autorisation
- JWT Bearer Authentication (durée: 30 min)
- Refresh tokens (validité: 7 jours)
- Role-Based Access Control (RBAC)
- Permissions granulaires par rôle
- Protection contre les timing attacks

### 👥 Gestion des Utilisateurs
- Inscription et connexion sécurisées
- Profils utilisateur complets
- Gestion des rôles (User, Admin)
- Changement de mot de passe
- Liste des membres ESN

### 📅 Gestion des Événements
- Création/Modification/Suppression d'événements
- Système d'inscriptions avec limite de places
- Formulaires personnalisés (SurveyJS)
- Templates d'événements réutilisables
- Gestion des participants

### 🗓️ Gestion des Calendriers
- Planification d'événements
- Organisateurs multiples (principal + sous-organisateurs)
- Event Manager et Responsable Communication
- Association avec événements

### 💡 Propositions & Votes
- Propositions d'activités par la communauté
- Système de vote Up/Down
- Soft delete pour préservation historique
- Protection anti-spam (rate limiting)

---

## Technologies

### Backend
- **ASP.NET Core 9.0** - Framework web
- **Entity Framework Core 9.0** - ORM
- **SQL Server** - Base de données
- **JWT Bearer** - Authentification
- **AutoMapper** - Mapping objet-objet
- **FluentValidation** - Validation des données
- **Serilog** - Logging structuré

### Sécurité
- HTTPS obligatoire (HSTS)
- Headers de sécurité (CSP, X-Frame-Options, etc.)
- Rate Limiting (login, registration, voting)
- CORS configuré
- Protection CSRF, XSS, SQL Injection

### Outils
- **Swagger/OpenAPI** - Documentation API
- **MSTest** - Tests unitaires
- **Moq** - Mocking

---

## Démarrage Rapide

### Prérequis

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB ou Express minimum)
- [Git](https://git-scm.com/)

### Installation

1. **Cloner le repository**
```bash
git clone https://github.com/your-org/ESN-WebApi.git
cd ESN-WebApi
```

2. **Configurer les secrets utilisateur**
```bash
cd Web
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "VotreCleSecreteDeMinimum32Caracteres"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=EsnDevDb;Trusted_Connection=True;"
```

3. **Restaurer les dépendances**
```bash
dotnet restore
```

4. **Appliquer les migrations**
```bash
dotnet ef database update --project Dal --startup-project Web
```

5. **Lancer l'application**
```bash
dotnet run --project Web
```

6. **Accéder à Swagger**
- Ouvrir https://localhost:5001/swagger
- L'API est accessible sur https://localhost:5001/api

### Première utilisation

1. **Créer un utilisateur**
```bash
POST /api/users
{
  "email": "admin@esn.com",
  "password": "Admin123!",
  "firstName": "Admin",
  "lastName": "ESN",
  "birthDate": "1990-01-01",
  "studentType": "esn_member"
}
```

2. **Se connecter**
```bash
POST /api/users/login
{
  "email": "admin@esn.com",
  "password": "Admin123!"
}
```

3. **Utiliser le token**
Copier le token reçu et l'ajouter dans Swagger:
- Cliquer sur "Authorize"
- Entrer: `Bearer <votre_token>`

---

## Documentation

### Documents Disponibles

La documentation complète est disponible dans le dossier `docs/`:

- **[Architecture.md](docs/Architecture.md)** - Architecture du projet, patterns utilisés
- **[Fonctionnalites.md](docs/Fonctionnalites.md)** - Description détaillée de toutes les fonctionnalités
- **[API-Endpoints.md](docs/API-Endpoints.md)** - Documentation complète des endpoints API
- **[Securite.md](docs/Securite.md)** - Guide de sécurité et bonnes pratiques
- **[Base-de-donnees.md](docs/Base-de-donnees.md)** - Schéma de base de données et gestion

### Swagger/OpenAPI

Documentation interactive disponible en développement:
- URL: https://localhost:5001/swagger
- Permet de tester directement les endpoints
- Schémas des DTOs inclus

---

## Architecture

### Structure du Projet

```
ESN-WebApi/
├── Web/                # Controllers, Middlewares, Validators
├── Business/           # Services métier
├── Dal/                # Repositories, UnitOfWork, DbContext
├── Bo/                 # Business Objects (entités)
├── Dto/                # Data Transfer Objects
└── Tests/              # Tests unitaires et d'intégration
```

### Patterns Utilisés

- **Repository Pattern** - Abstraction de l'accès aux données
- **Unit of Work** - Gestion transactionnelle
- **Dependency Injection** - Inversion de contrôle
- **Specification Pattern** - Encapsulation de la logique de requêtage

Voir [Architecture.md](docs/Architecture.md) pour plus de détails.

---

## Sécurité

### Mesures Implémentées

✅ **Authentification**
- JWT avec signature HMAC-SHA256
- Refresh tokens avec limite de validité
- Hashage PBKDF2 pour les mots de passe

✅ **Autorisation**
- Role-Based Access Control (RBAC)
- Ownership verification
- Permissions granulaires

✅ **Protection des Données**
- HTTPS obligatoire (HSTS)
- Validation FluentValidation
- Protection SQL injection (EF Core)
- Pas d'exposition de données sensibles

✅ **Rate Limiting**
- Login: 5 tentatives / 5 min
- Registration: 3 créations / heure
- Voting: 30 votes / min
- Global: 100 requêtes / min

Voir [Securite.md](docs/Securite.md) pour le guide complet.

---

## Tests

### Exécuter les Tests

```bash
# Tous les tests
dotnet test

# Tests d'un projet spécifique
dotnet test Tests/Tests.csproj

# Avec couverture
dotnet test /p:CollectCoverage=true
```

### Couverture de Tests

Les tests couvrent:
- ✅ Services métier
- ✅ Repositories
- ✅ Contrôleurs
- ✅ Middlewares
- ✅ Specifications

---

## Configuration

### appsettings.json

```json
{
  "Jwt": {
    "Key": "dotnet_secrets",
    "Issuer": "YourApp",
    "Audience": "YourAppUsers",
    "ExpireMinutes": 30
  },
  "ConnectionStrings": {
    "DefaultConnection": "dotnet_secrets"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  }
}
```

### Variables d'Environnement

**Production:**
- `Jwt__Key` - Clé secrète JWT
- `ConnectionStrings__DefaultConnection` - Connection string SQL Server

---

## Déploiement

### Déploiement Azure App Service

1. Publier l'application
```bash
dotnet publish -c Release -o ./publish
```

2. Configurer les variables d'environnement dans Azure
3. Activer HTTPS dans Azure App Service
4. Configurer la connection string

### Déploiement Docker (futur)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "Web.dll"]
```

---

## Contribution

### Standards de Code

- **Naming:** PascalCase pour classes, camelCase pour variables
- **Async:** Suffixe `Async` pour méthodes asynchrones
- **Logging:** Logs structurés pour toutes les actions importantes
- **Comments:** XML comments pour méthodes publiques

### Processus de Contribution

1. Fork le repository
2. Créer une branche feature (`git checkout -b feature/AmazingFeature`)
3. Commit les changements (`git commit -m 'Add AmazingFeature'`)
4. Push vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrir une Pull Request

### Commits

Format de commit:
```
type(scope): description courte

Description détaillée si nécessaire
```

Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`

---

## Roadmap

### Version Actuelle: 1.0

✅ Authentification JWT
✅ CRUD complet des entités
✅ Système de vote
✅ Templates d'événements
✅ Rate limiting
✅ Logging structuré

### Prochaines Versions

**v1.1 - Notifications**
- [ ] Service d'envoi d'emails
- [ ] Notifications d'inscription
- [ ] Rappels d'événements

**v1.2 - Fichiers**
- [ ] Upload de photos d'événements
- [ ] Avatars utilisateurs
- [ ] Documents joints

**v1.3 - Statistiques**
- [ ] Dashboard organisateur
- [ ] Rapports de participation
- [ ] Analytics

---

## Licence

Ce projet est sous licence MIT - voir le fichier [LICENSE](LICENSE) pour plus de détails.

---

## Support

- **Email:** support@esn.org
- **Issues:** [GitHub Issues](https://github.com/your-org/ESN-WebApi/issues)
- **Documentation:** Dossier `docs/`

---

## Remerciements

- **ESN International** pour le support
- **Communauté ASP.NET Core** pour les outils excellents
- Tous les contributeurs au projet

---

**Développé avec ❤️ par l'équipe ESN**
