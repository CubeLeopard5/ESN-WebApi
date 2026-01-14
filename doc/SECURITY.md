# 🔒 Guide de Sécurité - ESN-WebApi

> **Vue d'ensemble** : ESN-WebApi implémente une approche de **sécurité multicouche** (Defense in Depth) pour protéger les données et les utilisateurs.

---

## Architecture de Sécurité

### Modèle de Défense en Profondeur

```
┌─────────────────────────────────────────┐
│ Couche 1: Réseau & Transport            │
│ - HTTPS obligatoire (HSTS)              │
│ - CORS restreint                        │
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│ Couche 2: Application                   │
│ - Rate Limiting                         │
│ - Taille requêtes limitée               │
│ - Headers de sécurité HTTP              │
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│ Couche 3: Authentification              │
│ - JWT avec expiration                   │
│ - Refresh tokens contrôlés              │
│ - Protection timing attacks             │
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│ Couche 4: Autorisation                  │
│ - RBAC (Role-Based Access Control)      │
│ - Ownership verification                │
│ - Permissions granulaires               │
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│ Couche 5: Données                       │
│ - Hashage mots de passe (PBKDF2)        │
│ - Validation inputs (FluentValidation)  │
│ - Protection SQL injection (EF Core)    │
└─────────────────────────────────────────┘
┌─────────────────────────────────────────┐
│ Couche 6: Monitoring                    │
│ - Logging structuré (Serilog)          │
│ - Masquage données sensibles            │
│ - Health checks                         │
└─────────────────────────────────────────┘
```

---

## 1. Authentification & Autorisation

### 1.1 JSON Web Tokens (JWT)

**Configuration**
- **Algorithme** : HMAC-SHA256 (HS256)
- **Durée de vie** : 30 minutes (dev), 15 minutes (prod)
- **Stockage client** : localStorage ou sessionStorage (PAS de cookie)
- **Transmission** : Header `Authorization: Bearer <token>`
- **Issuer** : YourApp
- **Audience** : YourAppUsers

**Claims inclus**
- `sub` (Subject): Email utilisateur
- `userId`: ID interne
- `name`: Nom complet
- `studentType`: Type d'étudiant
- `role`: Rôle (User, Admin)
- Permissions : `CanCreateEvents`, `CanModifyEvents`, etc.

**Validation**
- Signature vérifiée
- Issuer et Audience validés
- Expiration vérifiée
- ClockSkew = 0 (aucune tolérance temporelle)

### 1.2 Refresh Tokens

**Fonctionnement**
- Permet de renouveler un token expiré
- **Validité** : 7 jours après émission
- Validation du token d'origine (signature, issuer, audience)
- Récupération des données utilisateur à jour
- Génération d'un nouveau token frais

**Sécurité**
- Stateless (pas de stockage serveur)
- Limite temporelle stricte (pas de refresh perpétuel)
- Vérification existence utilisateur en DB

### 1.3 Protection Mots de Passe

**Hashage**
- **Algorithme** : PBKDF2 avec salt aléatoire
- **Framework** : ASP.NET Core Identity PasswordHasher
- **Iterations** : ~10,000
- **Salt** : Généré aléatoirement
- **Stockage** : Hash uniquement, jamais en clair

**Protection Timing Attacks**
```csharp
// Si utilisateur inexistant: hash dummy exécuté
// Temps de réponse constant
// Message générique "Invalid credentials"
```

### 1.4 Role-Based Access Control (RBAC)

**Rôles**
- **User** : Rôle standard
- **Admin** : Accès administrateur complet

**Permissions granulaires (via claims JWT)**
- CanCreateEvents, CanModifyEvents, CanDeleteEvents
- CanCreateUsers, CanModifyUsers, CanDeleteUsers

**Ownership Verification**
```csharp
// Un utilisateur ne peut modifier que ses ressources OU être admin
if (resource.UserId != currentUserId && !User.IsInRole("Admin"))
    return Forbid(); // HTTP 403
```

---

## 2. Protection OWASP Top 10

### 2.1 SQL Injection ✅ Protégé

**Entity Framework Core**
- Requêtes paramétrées automatiques
- Pas de concaténation SQL directe

```csharp
// ✅ Sécurisé (paramétré)
context.Users.Where(u => u.Email == userEmail)

// ❌ À éviter (SQL raw)
context.Users.FromSqlRaw($"SELECT * FROM Users WHERE Email = '{userEmail}'")
```

### 2.2 XSS (Cross-Site Scripting) ✅ Protégé

**Headers de sécurité**
```
X-Content-Type-Options: nosniff
X-XSS-Protection: 1; mode=block
Content-Security-Policy: default-src 'self'; frame-ancestors 'none'
```

**Sérialisation JSON**
- Encodage automatique par ASP.NET Core
- Pas d'insertion HTML directe

⚠️ **ATTENTION** : Stockage JWT en localStorage expose au risque XSS côté client.

**Bonnes pratiques client**
- Ne jamais injecter de contenu non sanitizé dans le DOM
- Utiliser frameworks modernes avec échappement automatique (Vue, React, Angular)
- Implémenter Content Security Policy stricte

### 2.3 CSRF (Cross-Site Request Forgery) ✅ Risque Minimal

**Pourquoi protégé**
- JWT via Authorization header (pas de cookie)
- Token n'est pas envoyé automatiquement par le navigateur
- Requête explicite requise du client

⚠️ **Si cookies d'authentification ajoutés à l'avenir**, implémenter :
- Tokens anti-CSRF (`[ValidateAntiForgeryToken]`)
- Pattern Double Submit Cookie
- Header `X-Requested-With`

### 2.4 Clickjacking ✅ Protégé

```
X-Frame-Options: DENY
Content-Security-Policy: frame-ancestors 'none'
```

### 2.5 Broken Access Control ✅ Protégé

- Vérification `[Authorize]` sur endpoints protégés
- Vérification ownership sur ressources
- Principe du moindre privilège

---

## 3. Rate Limiting

**Politiques configurées**

| Endpoint | Limite | Fenêtre | Protection |
|----------|--------|---------|------------|
| Global | 100 requêtes | 1 minute | DoS applicatif |
| `/api/users/login` | 5 tentatives | 5 minutes | Brute force |
| `/api/users` (POST) | 3 créations | 1 heure | Spam comptes |
| `/api/propositions/{id}/vote-*` | 30 votes | 1 minute | Manipulation votes |

**Configuration**
```csharp
[EnableRateLimiting("login")]
public async Task<ActionResult> Login(UserLoginDto loginDto)
```

**Réponse** : HTTP 429 Too Many Requests

---

## 4. Headers de Sécurité HTTP

**Middleware** : `SecurityHeadersMiddleware`

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'; frame-ancestors 'none'
Permissions-Policy: geolocation=(), microphone=(), camera=()
Strict-Transport-Security: max-age=31536000; includeSubDomains (HTTPS uniquement)
```

---

## 5. CORS (Cross-Origin Resource Sharing)

**Configuration**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNuxt", policy =>
    {
        policy.WithOrigins(allowedOrigins) // depuis appsettings.json
              .WithHeaders("Content-Type", "Authorization")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithExposedHeaders("Content-Type");
    });
});
```

**Environnement**
- **Dev** : `http://localhost:3000`
- **Prod** : Origins explicites (pas de wildcard `*`)

---

## 6. Protection des Données

### 6.1 Données Sensibles

**Mots de passe**
- Jamais stockés en clair
- Hash PBKDF2 uniquement
- Pas de récupération (reset uniquement)

**Tokens JWT**
- Stockage client uniquement
- Jamais persistés serveur
- Transmission via header Authorization

**Données personnelles**
- Accès restreint (propriétaire ou admin)
- AutoMapper filtre PasswordHash des DTOs

### 6.2 Validation des Entrées

**FluentValidation**
- Validation automatique avant contrôleur
- Règles métier déclaratives

**Validateurs implémentés**
- UserCreateDtoValidator
- UserPasswordChangeDtoValidator
- CreateEventDtoValidator
- RegisterEventDtoValidator
- SurveyJsData validator (JSON + max 50KB)

### 6.3 Limite Taille Requêtes

```csharp
options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
```

---

## 7. Gestion des Secrets

### 7.1 Développement - User Secrets

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "votre_cle_secrete_tres_longue"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
```

**Avantages**
- Pas de commit dans Git
- Isolation par développeur
- Facile à regénérer

### 7.2 Production - Variables d'Environnement

```bash
az webapp config appsettings set --name myapp --resource-group mygroup \
  --settings Jwt__Key="prod_secret" \
             ConnectionStrings__DefaultConnection="Server=..."
```

**Bonnes pratiques**
- Clés différentes par environnement
- Rotation régulière des secrets
- Accès restreint

### 7.3 Protection appsettings.json

❌ **JAMAIS de secrets en clair**
- Valeurs par défaut ou placeholders uniquement
- Documentation des clés requises

---

## 8. Logging et Monitoring

### 8.1 Logging Structuré (Serilog)

**Niveaux**
- **Debug** : Détails (Dev uniquement)
- **Information** : Flux normal
- **Warning** : Situations anormales
- **Error** : Erreurs nécessitant attention

**Cibles**
- Console (toujours)
- Fichiers rotatifs (quotidien, 31 jours)

**Enrichissement**
- MachineName, ThreadId, EnvironmentUserName

### 8.2 Événements de Sécurité Loggés

- Tentatives de connexion échouées
- Violations d'autorisation (403 Forbidden)
- Modifications de profil
- Actions administratives
- Exceptions non gérées

⚠️ **Jamais loggés** : Mots de passe, Tokens, Données sensibles

### 8.3 Masquage des Erreurs

**Production**
```csharp
if (!env.IsDevelopment())
{
    errorResponse.Details = null; // Masquer stack trace
    errorResponse.Message = "An error occurred processing your request.";
}
```

**Développement**
- Stack traces complètes
- Details d'exceptions

---

## 9. Base de Données

### 9.1 Connexions Sécurisées

- Connection string dans User Secrets / Variables d'environnement
- Windows Auth ou SQL Auth
- SSL/TLS pour connexions distantes

### 9.2 Principe du Moindre Privilège

- Compte DB dédié à l'application
- Permissions limitées (INSERT, UPDATE, SELECT, DELETE)
- Pas de CREATE DATABASE, DROP TABLE

### 9.3 Migrations EF Core

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

**Sécurité**
- Pas d'exécution automatique en production
- Revue manuelle avant application
- Backup avant migration

---

## 10. Bonnes Pratiques

### ✅ À FAIRE

1. **Validation** : FluentValidation pour toutes les entrées
2. **Autorisation** : Vérifier permissions sur chaque action
3. **HTTPS** : Forcer en production via reverse proxy
4. **Secrets** : User Secrets (dev) / Variables d'environnement (prod)
5. **Logging** : Logger événements de sécurité sans données sensibles

### ❌ À ÉVITER

1. ❌ Secrets dans appsettings.json
2. ❌ Désactiver validation du modèle
3. ❌ Exposer stack traces en production
4. ❌ JWT en cookie sans protection anti-CSRF
5. ❌ `AllowAnyOrigin()` avec `AllowCredentials()`

---

## 11. Checklist Pré-Production

### Configuration

- [ ] Tous les secrets en variables d'environnement
- [ ] HTTPS uniquement (désactiver HTTP)
- [ ] CORS avec domaines de production uniquement
- [ ] JWT ExpireMinutes = 15 minutes
- [ ] HSTS activé
- [ ] Rate limiting approprié
- [ ] Connection strings sécurisées

### Validation

- [ ] Logs ne contiennent pas de données sensibles
- [ ] `IsDevelopment()` retourne false
- [ ] Code reviews effectués
- [ ] Tests de sécurité passés
- [ ] Dépendances à jour (dotnet list package --vulnerable)
- [ ] Health checks actifs
- [ ] Backup configuré

---

## 12. Conformité RGPD

### Données Personnelles Collectées

- Email, nom, prénom, date de naissance
- Numéro de téléphone (optionnel)
- Université (optionnel)

### Droits des Utilisateurs

- **Droit d'accès** : `GET /api/users/me`
- **Droit de rectification** : `PUT /api/users/{id}`
- **Droit à l'effacement** : `DELETE /api/users/{id}` (Admin)

### Rétention

- **Logs** : 31 jours (rotation automatique)
- **Soft delete** : Propositions marquées supprimées (pas effacées)

---

## 13. Réponse aux Incidents

### Indicateurs de Compromission

- Pic de tentatives de connexion échouées
- Requêtes anormales dans logs
- Accès non autorisés
- Changements de mots de passe massifs

### En Cas de Compromission

1. Isoler le système si nécessaire
2. Analyser les logs
3. Identifier l'origine (IP, user, endpoint)
4. Révoquer tokens (à implémenter)
5. Forcer reset mots de passe si nécessaire
6. Corriger la faille
7. Documenter l'incident

---

## 14. Maintenance Régulière

- [ ] Mise à jour dépendances (mensuel)
- [ ] Revue logs de sécurité (hebdomadaire)
- [ ] Rotation secrets (trimestriel)
- [ ] Backup données (quotidien)
- [ ] Test restauration (mensuel)
- [ ] Scan vulnérabilités (mensuel)

---

## Références

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [JWT Security Best Practices](https://tools.ietf.org/html/rfc8725)
- [CORS Security](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)

---

**Contact Sécurité** : En cas de découverte d'une vulnérabilité, contacter l'équipe de sécurité plutôt que créer une issue publique.

**Note** : La sécurité est un processus continu. Ce document doit être régulièrement revu et mis à jour.
